import re
import urllib.request
from pathlib import Path

import fetch_wikidot_spells as F

UA = "Mozilla/5.0"
for slug in ("spell:fireball", "spell:toll-the-dead", "spell:booming-blade"):
    url = f"https://dnd5e.wikidot.com/{slug}"
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    html = urllib.request.urlopen(req, timeout=60).read().decode("utf-8", "replace")
    Path(f"_sample_{slug.replace(':','_')}.html").write_text(html, encoding="utf-8")
    content = F.extract_page_content(html)
    text = F.strip_tags(content)
    print("=" * 60, slug)
    print("content html len", len(content))
    print("text preview:")
    print(text[:1500])
    print("---")
    parsed = F.parse_spell_page(slug, html, include_ua=True)
    print("parsed:", parsed and {k: parsed[k] for k in ("name", "level", "school", "castingTime", "damageDice", "rollType", "source")})
    print("fullDescription len", len((parsed or {}).get("fullDescription") or ""))
