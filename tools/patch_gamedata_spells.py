from pathlib import Path

path = Path(__file__).resolve().parents[1] / "GameData.cs"
text = path.read_text(encoding="utf-8")
start = text.find("        // ==================== MASTER CANTRIP LIST")
idx_armor = text.find("public class Armor")
if start < 0 or idx_armor < 0:
    raise SystemExit(f"markers not found start={start} idx_armor={idx_armor}")

replacement = r'''        // ==================== SPELL DATABASE (SRD 5.1 — loaded from Data/spells.json) ====================
        // Full text, school, casting time, range, components, duration, roll type, damage dice,
        // upcast rules. See SpellCatalog and tools/fetch_srd_spells.py.

        /// <summary>Every SRD spell (cantrips–9th). Prefer this over level-specific lists.</summary>
        public static System.Collections.Generic.IReadOnlyList<Spell> AllSpells => Nemo.SpellCatalog.All;

        /// <summary>Cantrips from the spell catalog.</summary>
        public static System.Collections.Generic.List<Spell> AllCantrips => Nemo.SpellCatalog.Cantrips.ToList();

        /// <summary>1st-level spells (as LeveledSpell for existing UI).</summary>
        public static System.Collections.Generic.List<LeveledSpell> All1stLevelSpells =>
            Nemo.SpellCatalog.Level1.Select(AsLeveled).ToList();

        /// <summary>Spells of a given level (0–9).</summary>
        public static System.Collections.Generic.List<Spell> GetSpellsAtLevel(int level) =>
            Nemo.SpellCatalog.GetByLevel(level).ToList();

        public static Spell? FindSpell(string name) => Nemo.SpellCatalog.Find(name);

        private static LeveledSpell AsLeveled(Spell s) => new()
        {
            Name = s.Name,
            Level = s.Level,
            School = s.School,
            CastingTime = s.CastingTime,
            Range = s.Range,
            Components = s.Components,
            Material = s.Material,
            Duration = s.Duration,
            IsConcentration = s.IsConcentration,
            IsRitual = s.IsRitual,
            DamageType = s.DamageType,
            DamageDice = s.DamageDice,
            RollType = s.RollType,
            SaveAbility = s.SaveAbility,
            DcSuccess = s.DcSuccess,
            AttackType = s.AttackType,
            Description = s.Description,
            FullDescription = s.FullDescription,
            HigherLevel = s.HigherLevel,
            CanUpcast = s.CanUpcast,
            UpcastIncrement = s.UpcastIncrement,
            DamageAtSlotLevel = s.DamageAtSlotLevel != null
                ? new System.Collections.Generic.Dictionary<string, string>(s.DamageAtSlotLevel)
                : new System.Collections.Generic.Dictionary<string, string>(),
            DamageAtCharacterLevel = s.DamageAtCharacterLevel != null
                ? new System.Collections.Generic.Dictionary<string, string>(s.DamageAtCharacterLevel)
                : new System.Collections.Generic.Dictionary<string, string>(),
            HealAtSlotLevel = s.HealAtSlotLevel != null
                ? new System.Collections.Generic.Dictionary<string, string>(s.HealAtSlotLevel)
                : new System.Collections.Generic.Dictionary<string, string>(),
            AreaOfEffect = s.AreaOfEffect,
            Classes = s.Classes != null ? new System.Collections.Generic.List<string>(s.Classes) : new System.Collections.Generic.List<string>(),
            Source = s.Source
        };

    }
}

'''

new_text = text[:start] + replacement + text[idx_armor:]
path.write_text(new_text, encoding="utf-8")
print("OK. length", len(new_text))
print("AllSpells present:", "AllSpells" in new_text)
print("Hardcoded Guidance gone:", 'Name = "Guidance"' not in new_text or "MASTER CANTRIP" not in new_text)
