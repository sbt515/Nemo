#!/usr/bin/env python3
"""
Scrape original 5e spells from https://dnd5e.wikidot.com/spells
and write Data/spells.json for Nemo.

Excludes Unearthed Arcana (UA) by default unless --include-ua is passed.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import time
import urllib.error
import urllib.request
from collections import Counter
from html import unescape
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Data" / "spells.json"
INDEX_URL = "https://dnd5e.wikidot.com/spells"
BASE = "https://dnd5e.wikidot.com"
UA = "Mozilla/5.0 (compatible; NemoSpellFetcher/1.0; +local character creator)"

LEVEL_FROM_WORD = {
    "cantrip": 0,
    "1st": 1,
    "2nd": 2,
    "3rd": 3,
    "4th": 4,
    "5th": 5,
    "6th": 6,
    "7th": 7,
    "8th": 8,
    "9th": 9,
    "first": 1,
    "second": 2,
    "third": 3,
    "fourth": 4,
    "fifth": 5,
    "sixth": 6,
    "seventh": 7,
    "eighth": 8,
    "ninth": 9,
}

SCHOOLS = {
    "abjuration",
    "conjuration",
    "divination",
    "enchantment",
    "evocation",
    "illusion",
    "necromancy",
    "transmutation",
}


def fetch(url: str, retries: int = 4) -> str:
    last_err: Exception | None = None
    for attempt in range(retries):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": UA, "Accept": "text/html"})
            with urllib.request.urlopen(req, timeout=60) as r:
                return r.read().decode("utf-8", "replace")
        except Exception as e:
            last_err = e
            time.sleep(1.5 * (attempt + 1))
    raise RuntimeError(f"Failed to fetch {url}: {last_err}")


def strip_tags(html: str) -> str:
    text = re.sub(r"(?is)<script[^>]*>.*?</script>", "", html)
    text = re.sub(r"(?is)<style[^>]*>.*?</style>", "", text)
    text = re.sub(r"(?i)<br\s*/?>", "\n", text)
    text = re.sub(r"(?i)</p\s*>", "\n\n", text)
    text = re.sub(r"(?i)</div\s*>", "\n", text)
    text = re.sub(r"(?i)</li\s*>", "\n", text)
    text = re.sub(r"(?i)<li[^>]*>", "• ", text)
    text = re.sub(r"(?is)<[^>]+>", "", text)
    text = unescape(text)
    text = text.replace("\xa0", " ").replace("\u2019", "'").replace("\u2018", "'")
    text = text.replace("\u201c", '"').replace("\u201d", '"').replace("\u2013", "-").replace("\u2014", "-")
    # Collapse whitespace but keep paragraph breaks
    text = re.sub(r"[ \t]+", " ", text)
    text = re.sub(r"\n[ \t]+", "\n", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def extract_page_content(html: str) -> str:
    # Prefer #page-content block
    m = re.search(
        r'(?is)<div[^>]+id=["\']page-content["\'][^>]*>(.*?)</div>\s*<div[^>]+(?:class=["\']page-tags|id=["\']page-info)',
        html,
    )
    if m:
        return m.group(1)
    m = re.search(r'(?is)<div[^>]+id=["\']page-content["\'][^>]*>(.*)', html)
    if m:
        # truncate wiki chrome
        chunk = m.group(1)
        chunk = re.split(r'(?is)<div[^>]+class=["\']page-tags', chunk)[0]
        return chunk
    return html


def collect_spell_slugs(html: str) -> list[str]:
    links = re.findall(r'href=["\'](/spell:[^"\'#?]+)["\']', html, flags=re.I)
    slugs: list[str] = []
    seen: set[str] = set()
    for href in links:
        slug = href.split("/")[-1]  # spell:fireball
        if not slug.lower().startswith("spell:"):
            continue
        key = slug.lower()
        if key in seen:
            continue
        seen.add(key)
        slugs.append(slug)
    return slugs


def parse_level_school(header_line: str) -> tuple[int, str]:
    """
    Examples:
      Necromancy cantrip
      3rd-level evocation
      2nd-level abjuration (ritual)
    """
    line = header_line.strip().strip("*").strip()
    low = line.lower()

    # Cantrip: "Necromancy cantrip"
    m = re.search(r"\b(" + "|".join(SCHOOLS) + r")\s+cantrip\b", low)
    if m:
        return 0, m.group(1).title()

    # "cantrip" alone
    if re.search(r"\bcantrip\b", low) and not re.search(r"\d", low):
        for sch in SCHOOLS:
            if sch in low:
                return 0, sch.title()
        return 0, ""

    # "3rd-level evocation"
    m = re.search(
        r"\b(1st|2nd|3rd|4th|5th|6th|7th|8th|9th|first|second|third|fourth|fifth|sixth|seventh|eighth|ninth)"
        r"[-\s]?level\s+(" + "|".join(SCHOOLS) + r")\b",
        low,
    )
    if m:
        return LEVEL_FROM_WORD[m.group(1)], m.group(2).title()

    # fallback school anywhere
    school = ""
    for sch in SCHOOLS:
        if sch in low:
            school = sch.title()
            break
    # level digit
    m = re.search(r"\b([1-9])(?:st|nd|rd|th)?[-\s]?level\b", low)
    if m:
        return int(m.group(1)), school
    return -1, school


def field_value(text: str, label: str) -> str:
    # **Casting Time:** 1 action
    pat = rf"(?im)^\s*\*{{0,3}}{re.escape(label)}\*{{0,3}}\s*:\s*(.+?)\s*$"
    m = re.search(pat, text)
    if m:
        return m.group(1).strip().strip("*").strip()
    # inline without line start
    pat2 = rf"(?i){re.escape(label)}\s*:\s*([^\n]+)"
    m = re.search(pat2, text)
    return m.group(1).strip().strip("*").strip() if m else ""


def parse_spell_lists(text: str) -> list[str]:
    m = re.search(r"(?is)Spell Lists?\.?\s*(.+?)(?:\n\n|\Z|Click here)", text)
    if not m:
        return []
    chunk = m.group(1)
    classes = []
    known = [
        "Artificer",
        "Bard",
        "Cleric",
        "Druid",
        "Paladin",
        "Ranger",
        "Sorcerer",
        "Warlock",
        "Wizard",
    ]
    for c in known:
        # Match "Sorcerer" and "Sorcerer (Optional)"
        if re.search(rf"\b{c}\b", chunk, re.I):
            classes.append(c)
    return classes


def split_body_and_higher(body: str) -> tuple[str, str]:
    # At Higher Levels variants
    m = re.search(
        r"(?is)\*{0,3}At Higher Levels\.?\*{0,3}\s*(.+?)(?=\*{0,3}Spell Lists|\Z)",
        body,
    )
    if m:
        higher = m.group(1).strip()
        main = body[: m.start()].strip()
        return main, higher
    return body.strip(), ""


def extract_main_description(text: str) -> str:
    """
    Body after Duration / Components block, before Spell Lists / site chrome.
    """
    # Cut off site chrome
    cut_markers = [
        r"Click here to edit contents",
        r"Click here to toggle editing",
        r"Append content without editing",
        r"Check out how this page has evolved",
        r"If you want to discuss contents",
        r"View and manage file attachments",
        r"A few useful tools to manage this Site",
        r"See pages that link to",
        r"Change the name \(also URL",
        r"View wiki source",
        r"View/set parent page",
        r"Notify administrators",
        r"Something does not work as expected",
        r"General Wikidot\.com documentation",
        r"Wikidot\.com Terms of Service",
        r"Wikidot\.com Privacy Policy",
    ]
    cleaned = text
    for mk in cut_markers:
        cleaned = re.split(mk, cleaned, maxsplit=1, flags=re.I)[0]

    # Start after last of Casting Time/Range/Components/Duration header block
    # Find Duration line and take remainder
    m = re.search(r"(?im)^\s*\*{0,3}Duration\*{0,3}\s*:.+$", cleaned)
    if m:
        body = cleaned[m.end() :].strip()
    else:
        # after school/level italic line
        m2 = re.search(
            r"(?im)^\s*.*\b(cantrip|level)\b.*$",
            cleaned,
        )
        body = cleaned[m2.end() :].strip() if m2 else cleaned

    # Remove leading Source if still present mid-body
    body = re.sub(r"(?im)^\s*Source:\s*.+$", "", body).strip()

    # Drop spell lists section for main+higher split later
    return body


def infer_roll_type(full_desc: str, higher: str) -> str:
    text = f"{full_desc}\n{higher}".lower()
    if re.search(r"regains?\s+(?:a number of )?hit points|hit points equal to", text):
        if re.search(r"\d+d\d+", text):
            # healing often; don't override pure damage
            if "damage" not in text.split("hit points")[0][-40:]:
                pass
    if re.search(r"ranged spell attack", text):
        return "Ranged Spell Attack"
    if re.search(r"melee spell attack", text):
        return "Melee Spell Attack"
    if re.search(r"spell attack", text):
        return "Spell Attack"
    for abil, label in [
        ("strength", "Str"),
        ("dexterity", "Dex"),
        ("constitution", "Con"),
        ("intelligence", "Int"),
        ("wisdom", "Wis"),
        ("charisma", "Cha"),
    ]:
        if re.search(rf"{abil} saving throw", text):
            return f"{label} Save"
    if re.search(r"regains?\s+.*hit points|hit points equal to", text) and re.search(
        r"\d+d\d+", text
    ):
        return "Healing"
    if "magic missile" in text and "automatically" in text:
        return "Automatic Hit"
    return "None"


def infer_damage(full_desc: str, higher: str) -> tuple[str, str, dict, dict, dict]:
    """
    Returns damageType, damageDice, damageAtSlotLevel, damageAtCharacterLevel, healAtSlotLevel
    """
    text = full_desc
    dmg_type = ""
    types = [
        "acid",
        "bludgeoning",
        "cold",
        "fire",
        "force",
        "lightning",
        "necrotic",
        "piercing",
        "poison",
        "psychic",
        "radiant",
        "slashing",
        "thunder",
    ]
    low = text.lower()
    for t in types:
        if re.search(rf"\b{t} damage\b", low):
            dmg_type = t.title()
            break

    damage_dice = ""
    # Common: takes 8d6 fire damage / 1d8 necrotic damage
    m = re.search(
        r"(?:takes?|deal(?:s|ing)?)\s+(\d+d\d+(?:\s*\+\s*(?:MOD|your spellcasting ability modifier|\d+))?)\s+(?:[a-z]+ )?damage",
        low,
    )
    if m:
        damage_dice = m.group(1).replace("your spellcasting ability modifier", "MOD")
    else:
        m = re.search(r"\b(\d+d\d+)\s+(?:" + "|".join(types) + r")\s+damage\b", low)
        if m:
            damage_dice = m.group(1)

    heal_at: dict[str, str] = {}
    # Healing: regains hit points equal to 1d8 + your spellcasting ability modifier
    hm = re.search(
        r"hit points equal to\s+(\d+d\d+(?:\s*\+\s*(?:your spellcasting ability modifier|MOD|\d+))?)",
        low,
    )
    if hm:
        expr = hm.group(1).replace("your spellcasting ability modifier", "MOD")
        damage_dice = expr
        # Guess base level from context later; store as slot 1 for now if leveled
        heal_at = {}

    damage_at_slot: dict[str, str] = {}
    damage_at_char: dict[str, str] = {}

    # Upcast: increases by 1d6 for each slot level above 3rd
    hlow = higher.lower()
    up = re.search(
        r"increases by\s+(\d+d\d+|\d+)\s+for each (?:slot level|spell slot level) above\s+(\d+)(?:st|nd|rd|th)?",
        hlow,
    )
    if up and damage_dice:
        inc = up.group(1)
        base_lvl = int(up.group(2))
        base_m = re.match(r"(\d+)d(\d+)", damage_dice.replace(" ", ""))
        if base_m and re.match(r"\d+d\d+", inc):
            n, die = int(base_m.group(1)), base_m.group(2)
            inc_n = int(inc.split("d")[0])
            for slot in range(base_lvl, 10):
                extra = (slot - base_lvl) * inc_n
                damage_at_slot[str(slot)] = f"{n + extra}d{die}"
        elif base_m is None and re.match(r"^\d+$", damage_dice.strip()):
            # flat number e.g. Armor of Agathys 5
            base = int(re.search(r"\d+", damage_dice).group())
            step = int(inc) if inc.isdigit() else 0
            if step:
                for slot in range(base_lvl, 10):
                    damage_at_slot[str(slot)] = str(base + (slot - base_lvl) * step)

    # Cantrip scaling: 5th level (2d8), 11th level (3d8), 17th level (4d8)
    if re.search(r"when you reach 5th level", hlow) or re.search(
        r"5th level \(2d", hlow
    ):
        if damage_dice:
            base_m = re.match(r"(\d+)d(\d+)", damage_dice.replace(" ", ""))
            if base_m:
                die = base_m.group(2)
                damage_at_char = {
                    "1": f"1d{die}",
                    "5": f"2d{die}",
                    "11": f"3d{die}",
                    "17": f"4d{die}",
                }
                # Prefer explicit if present
                for lvl, label in [(5, "5"), (11, "11"), (17, "17")]:
                    mm = re.search(rf"{lvl}th level \((\d+d\d+)", hlow)
                    if mm:
                        damage_at_char[label] = mm.group(1)

    # Healing upcast: healing increases by 1d8 for each slot level above 1st
    if "healing increases" in hlow or (
        "hit points" in low and "increases by" in hlow and "slot level" in hlow
    ):
        bm = re.search(
            r"hit points equal to\s+(\d+d\d+)\s*\+\s*(?:your spellcasting ability modifier|MOD)",
            low,
        )
        um = re.search(
            r"increases by\s+(\d+d\d+)\s+for each (?:slot level|spell slot level) above\s+(\d+)",
            hlow,
        )
        if bm and um:
            base_n = int(bm.group(1).split("d")[0])
            die = bm.group(1).split("d")[1]
            inc_n = int(um.group(1).split("d")[0])
            base_lvl = int(um.group(2))
            for slot in range(base_lvl, 10):
                n = base_n + (slot - base_lvl) * inc_n
                heal_at[str(slot)] = f"{n}d{die} + MOD"
            damage_dice = f"{base_n}d{die} + MOD"

    return dmg_type, damage_dice, damage_at_slot, damage_at_char, heal_at


def parse_upcast_increment(higher: str) -> str:
    if not higher:
        return ""
    m = re.search(
        r"increases by\s+([^.]+?)\s+for each (?:slot level|spell slot level) above",
        higher,
        re.I,
    )
    if m:
        return m.group(1).strip()
    m = re.search(r"increases by\s+([^.]+)", higher, re.I)
    if m:
        return m.group(1).strip()
    return higher.split(".")[0].strip()


def name_from_slug(slug: str) -> str:
    raw = slug.split(":", 1)[-1]
    # Keep common apostrophe forms
    specials = {
        "tashas-hideous-laughter": "Tasha's Hideous Laughter",
        "tashas-caustic-brew": "Tasha's Caustic Brew",
        "tashas-mind-whip": "Tasha's Mind Whip",
        "tashas-otherworldly-guise": "Tasha's Otherworldly Guise",
        "leomunds-tiny-hut": "Leomund's Tiny Hut",
        "leomunds-secret-chest": "Leomund's Secret Chest",
        "melfs-acid-arrow": "Melf's Acid Arrow",
        "melfs-minute-meteors": "Melf's Minute Meteors",
        "mordenkainens-sword": "Mordenkainen's Sword",
        "mordenkainens-magnificent-mansion": "Mordenkainen's Magnificent Mansion",
        "mordenkainens-private-sanctum": "Mordenkainen's Private Sanctum",
        "mordenkainens-faithful-hound": "Mordenkainen's Faithful Hound",
        "bigbys-hand": "Bigby's Hand",
        "otilukes-resilient-sphere": "Otiluke's Resilient Sphere",
        "otilukes-freezing-sphere": "Otiluke's Freezing Sphere",
        "evards-black-tentacles": "Evard's Black Tentacles",
        "rarys-telepathic-bond": "Rary's Telepathic Bond",
        "ottos-irresistible-dance": "Otto's Irresistible Dance",
        "tensers-floating-disk": "Tenser's Floating Disk",
        "tensers-transformation": "Tenser's Transformation",
        "nystuls-magic-aura": "Nystul's Magic Aura",
        "drawmijs-instant-summons": "Drawmij's Instant Summons",
        "hunters-mark": "Hunter's Mark",
        "blindness-deafness": "Blindness/Deafness",
        "enlarge-reduce": "Enlarge/Reduce",
        "dragons-breath": "Dragon's Breath",
        "ashardalons-stride": "Ashardalon's Stride",
        "maximillians-earthen-grasp": "Maximilian's Earthen Grasp",
        "snillocs-snowball-swarm": "Snilloc's Snowball Swarm",
        "aganazzars-scorcher": "Aganazzar's Scorcher",
        "nathairs-mischief": "Nathair's Mischief",
        "rimes-binding-ice": "Rime's Binding Ice",
        "jims-magic-missile": "Jim's Magic Missile",
        "jims-glowing-coin": "Jim's Glowing Coin",
        "galders-tower": "Galder's Tower",
        "galders-speedy-courier": "Galder's Speedy Courier",
        "icingdeath-s-frost": "Icingdeath's Frost",
        "heroes-feast": "Heroes' Feast",
        "//": "",
    }
    key = raw.lower()
    if key in specials:
        return specials[key]
    # Title-case hyphenated slug
    parts = raw.replace("_", "-").split("-")
    small = {"of", "and", "the", "to", "a", "an", "or", "from", "with", "into", "via"}
    out = []
    for i, p in enumerate(parts):
        if not p:
            continue
        pl = p.lower()
        if i > 0 and pl in small:
            out.append(pl)
        else:
            out.append(pl[:1].upper() + pl[1:])
    return " ".join(out)


def extract_title_name(html: str, slug: str) -> str:
    # <title>Fireball - DND 5th Edition</title>
    m = re.search(r"(?is)<title>\s*([^<]+?)\s*</title>", html)
    if m:
        title = unescape(m.group(1)).strip()
        title = re.sub(r"\s*[-|].*$", "", title).strip()
        if title and title.lower() not in {"spells", "dnd 5th edition", "home"}:
            return title
    # <div id="page-title">...</div>
    m = re.search(r'(?is)<div[^>]+id=["\']page-title["\'][^>]*>\s*([^<]+?)\s*</div>', html)
    if m:
        t = unescape(m.group(1)).strip()
        if t:
            return t
    return name_from_slug(slug)


def parse_spell_page(slug: str, html: str, include_ua: bool) -> dict | None:
    content_html = extract_page_content(html)
    text = strip_tags(content_html)

    lines = [ln.strip() for ln in text.splitlines() if ln.strip()]
    if not lines:
        return None

    name = extract_title_name(html, slug)

    # Source
    source = ""
    sm = re.search(r"(?im)^\s*Source:\s*(.+)$", text)
    if sm:
        source = sm.group(1).strip()

    is_ua = (
        bool(re.search(r"\bUA\b|Unearthed Arcana", source, re.I))
        or "(UA)" in name
        or re.search(r"-ua$", slug, re.I)
        or "/ua" in slug.lower()
    )
    if is_ua and not include_ua:
        return None

    # Level/school line: look for cantrip / Nth-level
    level, school = -1, ""
    for ln in lines[:12]:
        lv, sch = parse_level_school(ln)
        if lv >= 0 and (sch or "cantrip" in ln.lower() or "level" in ln.lower()):
            level, school = lv, sch
            if school or lv == 0:
                break
    if level < 0:
        # try whole text near top
        head = "\n".join(lines[:15])
        level, school = parse_level_school(head)
    if level < 0:
        level = 0 if re.search(r"\bcantrip\b", text, re.I) else 1

    casting = field_value(text, "Casting Time") or field_value(text, "Casting time")
    range_ = field_value(text, "Range")
    components = field_value(text, "Components")
    duration = field_value(text, "Duration")

    is_concentration = bool(re.search(r"concentration", duration, re.I))
    is_ritual = bool(
        re.search(r"\britual\b", casting, re.I)
        or re.search(r"\(ritual\)", "\n".join(lines[:15]), re.I)
        or re.search(r"\bR\b", casting)
        or "*R*" in casting
        or casting.endswith("R")
        or " R" in casting
    )
    # Clean casting time markers like "1 Action *R*"
    casting = re.sub(r"\s*\*R\*\s*", " ", casting).strip()
    casting = re.sub(r"\s+R\s*$", "", casting).strip()
    if is_ritual and "ritual" not in casting.lower():
        casting = f"{casting} (ritual)" if casting else "Ritual"

    material = ""
    mm = re.search(r"\bM\s*\(([^)]+)\)", components)
    if mm:
        material = mm.group(1).strip()

    body = extract_main_description(text)
    # Remove spell lists from body before split
    body_no_lists = re.split(r"(?i)\*{0,3}Spell Lists?", body)[0].strip()
    full_desc, higher = split_body_and_higher(body_no_lists)

    # Clean residual headers from full_desc
    full_desc = re.sub(
        r"(?im)^\s*\*{0,3}(Casting Time|Range|Components|Duration)\*{0,3}\s*:.*$",
        "",
        full_desc,
    ).strip()
    full_desc = re.sub(r"\n{3,}", "\n\n", full_desc).strip()

    classes = parse_spell_lists(text)
    roll_type = infer_roll_type(full_desc, higher)
    dmg_type, damage_dice, dmg_slot, dmg_char, heal_slot = infer_damage(full_desc, higher)

    # If healing inferred but heal_slot empty and higher has slot scaling
    if roll_type == "Healing" and not heal_slot and damage_dice:
        pass

    can_upcast = bool(higher.strip())
    upcast_increment = parse_upcast_increment(higher) if can_upcast else ""

    short = full_desc.split("\n\n")[0] if full_desc else ""
    if len(short) > 200:
        short_ui = short[:197] + "..."
    else:
        short_ui = short

    pretty = name.strip()
    if (
        not pretty
        or pretty.lower().startswith("source:")
        or pretty.lower() in {"edit", "history", "spells"}
        or len(pretty) > 80
    ):
        pretty = name_from_slug(slug)

    return {
        "name": pretty,
        "level": level,
        "school": school,
        "castingTime": casting,
        "range": range_,
        "components": components,
        "material": material,
        "duration": duration,
        "isConcentration": is_concentration,
        "isRitual": is_ritual,
        "damageType": dmg_type,
        "damageDice": damage_dice,
        "rollType": roll_type,
        "saveAbility": roll_type.replace(" Save", "") if roll_type.endswith("Save") else "",
        "dcSuccess": "half"
        if re.search(r"half as much", full_desc, re.I)
        else ("none" if "Save" in roll_type else ""),
        "attackType": "ranged"
        if "Ranged" in roll_type
        else ("melee" if "Melee" in roll_type else ""),
        "description": short_ui,
        "fullDescription": full_desc,
        "higherLevel": higher,
        "canUpcast": can_upcast,
        "upcastIncrement": upcast_increment,
        "damageAtSlotLevel": dmg_slot,
        "damageAtCharacterLevel": dmg_char,
        "healAtSlotLevel": heal_slot,
        "areaOfEffect": "",
        "classes": classes,
        "source": source or "https://dnd5e.wikidot.com/spells",
        "slug": slug,
    }


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--include-ua", action="store_true", help="Include Unearthed Arcana spells")
    ap.add_argument("--delay", type=float, default=0.35, help="Delay between page fetches")
    ap.add_argument("--limit", type=int, default=0, help="Debug: only first N spells")
    ap.add_argument("--resume", action="store_true", help="Merge with existing spells.json by slug/name")
    args = ap.parse_args()

    print("Fetching spell index...", flush=True)
    index_html = fetch(INDEX_URL)
    slugs = collect_spell_slugs(index_html)
    print(f"Found {len(slugs)} spell links", flush=True)
    if args.limit:
        slugs = slugs[: args.limit]

    existing_by_name: dict[str, dict] = {}
    if args.resume and OUT.exists():
        try:
            for s in json.loads(OUT.read_text(encoding="utf-8")):
                existing_by_name[s.get("name", "").lower()] = s
        except Exception:
            pass

    spells: list[dict] = []
    errors: list[tuple[str, str]] = []
    skipped_ua = 0

    for i, slug in enumerate(slugs, 1):
        url = f"{BASE}/{slug}"
        try:
            html = fetch(url)
            spell = parse_spell_page(slug, html, include_ua=args.include_ua)
            if spell is None:
                skipped_ua += 1
            else:
                spells.append(spell)
        except Exception as e:
            errors.append((slug, str(e)))
            print(f"  ERR {slug}: {e}", flush=True)
        if i % 25 == 0 or i == len(slugs):
            print(f"  {i}/{len(slugs)} (ok={len(spells)} ua_skip={skipped_ua} err={len(errors)})", flush=True)
        time.sleep(args.delay)

    # Deduplicate by slug first, then by name
    by_key: dict[str, dict] = {}
    for s in spells:
        key = (s.get("slug") or s["name"]).lower()
        prev = by_key.get(key)
        if prev is None or len(s.get("fullDescription") or "") > len(
            prev.get("fullDescription") or ""
        ):
            by_key[key] = s
    # Second pass: collapse exact same display name if one is clearly better
    by_name: dict[str, dict] = {}
    for s in by_key.values():
        key = s["name"].lower()
        prev = by_name.get(key)
        if prev is None or len(s.get("fullDescription") or "") > len(
            prev.get("fullDescription") or ""
        ):
            by_name[key] = s
    spells = list(by_name.values())
    spells.sort(key=lambda s: (s["level"], s["name"].lower()))

    # Drop internal scrape fields
    for s in spells:
        s.pop("slug", None)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(spells, indent=2, ensure_ascii=False), encoding="utf-8")

    levels = Counter(s["level"] for s in spells)
    print("Levels:", dict(sorted(levels.items())))
    print(f"Wrote {len(spells)} spells to {OUT}")
    print(f"Skipped UA: {skipped_ua}")
    print(f"Errors: {len(errors)}")
    print(f"Upcastable: {sum(1 for s in spells if s.get('canUpcast'))}")
    print(f"With damage dice: {sum(1 for s in spells if s.get('damageDice'))}")

    for sample in ("Fireball", "Toll the Dead", "Booming Blade", "Absorb Elements"):
        s = next((x for x in spells if x["name"] == sample), None)
        if s:
            print(
                f"SAMPLE {sample}: L{s['level']} {s['school']} | {s['rollType']} | "
                f"{s['damageDice']} {s['damageType']} | upcast={s['upcastIncrement']!r} | "
                f"desc_len={len(s['fullDescription'])} | src={s['source']}"
            )
        else:
            print(f"SAMPLE {sample}: MISSING")

    return 0 if len(errors) < max(10, len(slugs) // 20) else 1


if __name__ == "__main__":
    sys.exit(main())
