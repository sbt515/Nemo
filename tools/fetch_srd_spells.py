#!/usr/bin/env python3
"""Download all 5e SRD spells from dnd5eapi.co and write Data/spells.json for Nemo."""

from __future__ import annotations

import json
import re
import sys
import urllib.request
from collections import Counter
from pathlib import Path

BASE = "https://www.dnd5eapi.co/api/2014/spells"
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Data" / "spells.json"


def fetch_json(url: str) -> dict:
    req = urllib.request.Request(url, headers={"User-Agent": "NemoSpellFetcher/1.0"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.load(r)


def format_components(comps: list[str], material: str) -> str:
    if not comps:
        return ""
    parts = list(comps)
    if material and "M" in parts:
        mat = material.rstrip(".")
        parts = [f"M ({mat})" if p == "M" else p for p in parts]
    return ", ".join(parts)


def infer_roll_type(
    name: str,
    full_desc: str,
    attack_type: str,
    save_ability: str,
    heal_at_slot: dict,
) -> str:
    full_lower = full_desc.lower()
    if heal_at_slot:
        return "Healing"
    if attack_type == "ranged":
        return "Ranged Spell Attack"
    if attack_type == "melee":
        return "Melee Spell Attack"
    if save_ability:
        abil_map = {
            "STR": "Str",
            "DEX": "Dex",
            "CON": "Con",
            "INT": "Int",
            "WIS": "Wis",
            "CHA": "Cha",
        }
        ab = abil_map.get(save_ability.upper(), save_ability.title())
        return f"{ab} Save"
    if "ranged spell attack" in full_lower:
        return "Ranged Spell Attack"
    if "melee spell attack" in full_lower:
        return "Melee Spell Attack"
    if "spell attack" in full_lower:
        return "Spell Attack"
    if name.lower() == "magic missile":
        return "Automatic Hit"
    return "None"


def parse_upcast_increment(higher_text: str) -> str:
    if not higher_text:
        return ""
    m = re.search(
        r"increases by ([^.]+?) for each (?:slot level|spell slot level) above",
        higher_text,
        re.I,
    )
    if m:
        return m.group(1).strip()
    m2 = re.search(r"increases by ([^.]+)", higher_text, re.I)
    if m2:
        return m2.group(1).strip()
    # e.g. "The temporary hit points increase by 5..."
    first = higher_text.split(".")[0].strip()
    return first


def convert(raw: dict, fallback_name: str) -> dict:
    desc_parts = raw.get("desc") or []
    full_desc = "\n\n".join(desc_parts)
    higher = raw.get("higher_level") or []
    higher_text = "\n\n".join(higher) if higher else ""

    material = raw.get("material") or ""
    comps = raw.get("components") or []
    components_str = format_components(comps, material)

    school = (raw.get("school") or {}).get("name") or ""
    classes = [c.get("name", "") for c in (raw.get("classes") or []) if c.get("name")]

    casting = raw.get("casting_time") or ""
    range_ = raw.get("range") or ""
    duration = raw.get("duration") or ""
    concentration = bool(raw.get("concentration"))
    ritual = bool(raw.get("ritual"))
    level = int(raw.get("level") or 0)

    damage = raw.get("damage") or {}
    dmg_type = ""
    if isinstance(damage.get("damage_type"), dict):
        dmg_type = damage["damage_type"].get("name") or ""
    damage_at_slot = damage.get("damage_at_slot_level") or {}
    damage_at_char = damage.get("damage_at_character_level") or {}
    heal_at_slot = raw.get("heal_at_slot_level") or {}

    base_dice = ""
    if damage_at_slot:
        keys = sorted(damage_at_slot.keys(), key=lambda k: int(k))
        base_dice = damage_at_slot[keys[0]]
    elif damage_at_char:
        keys = sorted(damage_at_char.keys(), key=lambda k: int(k))
        base_dice = damage_at_char[keys[0]]
    elif heal_at_slot:
        keys = sorted(heal_at_slot.keys(), key=lambda k: int(k))
        base_dice = heal_at_slot[keys[0]]

    dc = raw.get("dc") or {}
    save_ability = ""
    if isinstance(dc.get("dc_type"), dict):
        save_ability = (dc["dc_type"].get("name") or "").upper()
    dc_success = dc.get("dc_success") or ""
    attack_type = raw.get("attack_type") or ""

    name = raw.get("name") or fallback_name
    roll_type = infer_roll_type(name, full_desc, attack_type, save_ability, heal_at_slot)

    can_upcast = bool(higher_text)
    upcast_increment = parse_upcast_increment(higher_text)

    short = desc_parts[0] if desc_parts else ""
    short_ui = short[:197] + "..." if len(short) > 200 else short

    aoe = raw.get("area_of_effect")
    aoe_str = ""
    if aoe:
        aoe_str = f"{aoe.get('size')} ft {aoe.get('type')}"

    return {
        "name": name,
        "level": level,
        "school": school,
        "castingTime": casting,
        "range": range_,
        "components": components_str,
        "material": material,
        "duration": duration,
        "isConcentration": concentration,
        "isRitual": ritual,
        "damageType": dmg_type,
        "damageDice": base_dice,
        "rollType": roll_type,
        "saveAbility": save_ability.title() if save_ability else "",
        "dcSuccess": dc_success,
        "attackType": attack_type,
        "description": short_ui,
        "fullDescription": full_desc,
        "higherLevel": higher_text,
        "canUpcast": can_upcast,
        "upcastIncrement": upcast_increment,
        "damageAtSlotLevel": {str(k): v for k, v in damage_at_slot.items()}
        if damage_at_slot
        else {},
        "damageAtCharacterLevel": {str(k): v for k, v in damage_at_char.items()}
        if damage_at_char
        else {},
        "healAtSlotLevel": {str(k): v for k, v in heal_at_slot.items()}
        if heal_at_slot
        else {},
        "areaOfEffect": aoe_str,
        "classes": classes,
        "source": "SRD 5.1",
    }


def main() -> int:
    print("Fetching spell index...", flush=True)
    index = fetch_json(BASE)
    results = index["results"]
    print(f"Found {len(results)} spells", flush=True)

    spells: list[dict] = []
    errors: list[tuple[str, str]] = []

    for i, item in enumerate(results, 1):
        url = "https://www.dnd5eapi.co" + item["url"]
        try:
            raw = fetch_json(url)
            spells.append(convert(raw, item["name"]))
        except Exception as e:
            errors.append((item["index"], str(e)))
            print(f"  ERR {item['index']}: {e}", flush=True)
        if i % 25 == 0 or i == len(results):
            print(f"  {i}/{len(results)}", flush=True)

    spells.sort(key=lambda s: (s["level"], s["name"].lower()))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    with OUT.open("w", encoding="utf-8") as f:
        json.dump(spells, f, indent=2, ensure_ascii=False)

    levels = Counter(s["level"] for s in spells)
    print("Levels:", dict(sorted(levels.items())))
    print(f"Wrote {len(spells)} spells to {OUT}")
    print(f"Errors: {len(errors)}")
    print(f"Upcastable: {sum(1 for s in spells if s['canUpcast'])}")
    print(f"With damage/heal dice: {sum(1 for s in spells if s['damageDice'])}")

    fb = next((s for s in spells if s["name"] == "Fireball"), None)
    if fb:
        sample = {
            k: fb[k]
            for k in (
                "name",
                "level",
                "damageDice",
                "rollType",
                "canUpcast",
                "upcastIncrement",
                "damageAtSlotLevel",
            )
        }
        print("Fireball sample:", json.dumps(sample, indent=2))

    return 0 if not errors else 1


if __name__ == "__main__":
    sys.exit(main())
