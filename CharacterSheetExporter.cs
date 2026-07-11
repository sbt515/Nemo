using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;

namespace Nemo
{
    /// <summary>
    /// Fills the official D&amp;D 5e multi-page fillable character sheet
    /// (<c>5E_CharacterSheet_Fillable.pdf</c>) with values from a Nemo <see cref="Character"/>.
    /// Field names match the template (including trailing spaces where the PDF uses them).
    /// This is the reverse of <see cref="CharacterSheetImporter"/>.
    /// </summary>
    public static class CharacterSheetExporter
    {
        public const string TemplateFileName = "5E_CharacterSheet_Fillable.pdf";

        /// <summary>
        /// Optional precomputed values that are easier to assemble from the UI layer.
        /// Everything not supplied is derived from <see cref="Character"/> + <see cref="GameData"/>.
        /// </summary>
        public sealed class ExportExtras
        {
            public List<SkillEntry>? Skills { get; set; }
            public List<SavingThrow>? SavingThrows { get; set; }
            public List<WeaponAttackLine>? Weapons { get; set; }
            public string? FeaturesAndTraits { get; set; }
            public string? ProficienciesAndLanguages { get; set; }
            public string? EquipmentText { get; set; }
            public string? HitDice { get; set; }
            public int? Level1SpellSlots { get; set; }
        }

        public sealed class WeaponAttackLine
        {
            public string Name { get; set; } = "";
            public string AttackBonus { get; set; } = "";
            public string Damage { get; set; } = "";
        }

        /// <summary>
        /// Resolves the bundled template path next to the executable (or in Templates/).
        /// </summary>
        public static string? FindTemplatePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "Templates", TemplateFileName),
                Path.Combine(baseDir, TemplateFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", TemplateFileName),
                // Dev-time fallback: workspace Templates folder
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Templates", TemplateFileName)),
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// Stamp character values into a copy of the official fillable sheet.
        /// </summary>
        public static void ExportToFile(Character character, string outputPath, ExportExtras? extras = null)
        {
            string? template = FindTemplatePath();
            if (template == null)
                throw new FileNotFoundException(
                    $"Could not find {TemplateFileName}. Expected under Templates/ next to the app.");

            ExportToFile(character, template, outputPath, extras);
        }

        public static void ExportToFile(Character character, string templatePath, string outputPath, ExportExtras? extras = null)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Official character sheet template not found.", templatePath);

            extras ??= new ExportExtras();
            var values = BuildFieldValues(character, extras);

            string? outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir))
                Directory.CreateDirectory(outDir);

            using var reader = new PdfReader(templatePath);
            using var writer = new PdfWriter(outputPath);
            using var pdf = new PdfDocument(reader, writer);

            var form = PdfAcroForm.GetAcroForm(pdf, true);
            if (form == null)
                throw new InvalidOperationException("The template PDF has no AcroForm to fill.");

            // Some iText versions need this for appearance refresh in Adobe Reader
            form.SetGenerateAppearance(true);

            var fields = form.GetAllFormFields();

            foreach (var (name, value) in values.TextFields)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (!fields.TryGetValue(name, out var field) || field == null) continue;

                try
                {
                    field.SetValue(value ?? "");
                }
                catch
                {
                    // Skip fields that refuse the value (locked, wrong type, etc.)
                }
            }

            foreach (var (name, isChecked) in values.Checkboxes)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (!fields.TryGetValue(name, out var field) || field == null) continue;

                try
                {
                    if (field is PdfButtonFormField btn)
                    {
                        string[] states = btn.GetAppearanceStates() ?? Array.Empty<string>();
                        string onState = states.FirstOrDefault(s =>
                            !s.Equals("Off", StringComparison.OrdinalIgnoreCase)) ?? "Yes";

                        btn.SetValue(isChecked ? onState : "Off");
                    }
                    else
                    {
                        field.SetValue(isChecked ? "Yes" : "Off");
                    }
                }
                catch
                {
                    // ignore
                }
            }

            // Keep the form editable for the player
            // form.FlattenFields(); // intentionally NOT flattened

            pdf.Close();
        }

        private sealed class FieldPayload
        {
            public Dictionary<string, string> TextFields { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, bool> Checkboxes { get; } = new(StringComparer.Ordinal);
        }

        private static FieldPayload BuildFieldValues(Character c, ExportExtras extras)
        {
            var payload = new FieldPayload();
            void T(string name, string? value)
            {
                if (string.IsNullOrEmpty(name)) return;
                payload.TextFields[name] = value ?? "";
            }
            void C(string name, bool on) => payload.Checkboxes[name] = on;

            string FmtMod(int m) => m >= 0 ? $"+{m}" : m.ToString();

            var scores = c.AbilityScores ?? new AbilityScoreBlock();
            int str = scores.Strength?.Final ?? 10;
            int dex = scores.Dexterity?.Final ?? 10;
            int con = scores.Constitution?.Final ?? 10;
            int intel = scores.Intelligence?.Final ?? 10;
            int wis = scores.Wisdom?.Final ?? 10;
            int cha = scores.Charisma?.Final ?? 10;

            int strMod = scores.Strength?.Modifier ?? Mod(str);
            int dexMod = scores.Dexterity?.Modifier ?? Mod(dex);
            int conMod = scores.Constitution?.Modifier ?? Mod(con);
            int intMod = scores.Intelligence?.Modifier ?? Mod(intel);
            int wisMod = scores.Wisdom?.Modifier ?? Mod(wis);
            int chaMod = scores.Charisma?.Modifier ?? Mod(cha);

            int prof = c.ProficiencyBonus > 0 ? c.ProficiencyBonus : 2;

            // ── Identity ──
            T("CharacterName", c.Name);
            T("CharacterName 2", c.Name);
            T("PlayerName", c.PlayerName);

            string raceDisplay = string.IsNullOrWhiteSpace(c.Subrace)
                ? (c.Race ?? "")
                : $"{c.Race} ({c.Subrace})";
            // NOTE: field name has a trailing space in this PDF
            T("Race ", raceDisplay);

            string classLevel = BuildClassLevel(c);
            T("ClassLevel", classLevel);
            T("Background", c.Background);

            // ── Combat stats ──
            T("ProfBonus", FmtMod(prof));
            string ac = !string.IsNullOrWhiteSpace(c.EquippedACDisplay)
                ? c.EquippedACDisplay
                : (c.ArmorClass > 0 ? c.ArmorClass.ToString() : "10");
            T("AC", ac);
            T("Initiative", FmtMod(c.Initiative != 0 ? c.Initiative : dexMod));
            T("Speed", c.Speed > 0 ? $"{c.Speed} ft." : "30 ft.");
            T("HPMax", c.HitPoints > 0 ? c.HitPoints.ToString() : "");
            T("HPCurrent", c.HitPoints > 0 ? c.HitPoints.ToString() : "");

            string hitDice = extras.HitDice
                ?? DeriveHitDice(c);
            T("HD", hitDice);
            T("HDTotal", hitDice);

            // ── Ability scores ──
            T("STR", str.ToString());
            T("STRmod", FmtMod(strMod));
            T("DEX", dex.ToString());
            T("DEXmod ", FmtMod(dexMod)); // trailing space
            T("CON", con.ToString());
            T("CONmod", FmtMod(conMod));
            T("INT", intel.ToString());
            T("INTmod", FmtMod(intMod));
            T("WIS", wis.ToString());
            T("WISmod", FmtMod(wisMod));
            T("CHA", cha.ToString());
            T("CHamod", FmtMod(chaMod)); // odd casing in template

            // ── Saving throws ──
            var saves = extras.SavingThrows ?? DeriveSavingThrows(c, prof);
            foreach (var save in saves)
            {
                string field = save.Name switch
                {
                    "Strength" => "ST Strength",
                    "Dexterity" => "ST Dexterity",
                    "Constitution" => "ST Constitution",
                    "Intelligence" => "ST Intelligence",
                    "Wisdom" => "ST Wisdom",
                    "Charisma" => "ST Charisma",
                    _ => ""
                };
                if (field.Length > 0)
                    T(field, FmtMod(save.Bonus));
            }

            // Save proficiency checkboxes (standard layout for this PDF)
            C("Check Box 11", saves.Any(s => s.Name == "Strength" && s.IsProficient));
            C("Check Box 18", saves.Any(s => s.Name == "Dexterity" && s.IsProficient));
            C("Check Box 19", saves.Any(s => s.Name == "Constitution" && s.IsProficient));
            C("Check Box 20", saves.Any(s => s.Name == "Intelligence" && s.IsProficient));
            C("Check Box 21", saves.Any(s => s.Name == "Wisdom" && s.IsProficient));
            C("Check Box 22", saves.Any(s => s.Name == "Charisma" && s.IsProficient));

            // ── Skills ──
            var skills = extras.Skills ?? DeriveSkills(c, prof);
            // Map skill display name → (field name, checkbox name)
            var skillFields = new Dictionary<string, (string Field, string Check)>(StringComparer.OrdinalIgnoreCase)
            {
                ["Acrobatics"] = ("Acrobatics", "Check Box 23"),
                ["Animal Handling"] = ("Animal", "Check Box 24"),
                ["Arcana"] = ("Arcana", "Check Box 25"),
                ["Athletics"] = ("Athletics", "Check Box 26"),
                ["Deception"] = ("Deception ", "Check Box 27"),       // trailing space
                ["History"] = ("History ", "Check Box 28"),           // trailing space
                ["Insight"] = ("Insight", "Check Box 29"),
                ["Intimidation"] = ("Intimidation", "Check Box 30"),
                ["Investigation"] = ("Investigation ", "Check Box 31"), // trailing space
                ["Medicine"] = ("Medicine", "Check Box 32"),
                ["Nature"] = ("Nature", "Check Box 33"),
                ["Perception"] = ("Perception ", "Check Box 34"),     // trailing space
                ["Performance"] = ("Performance", "Check Box 35"),
                ["Persuasion"] = ("Persuasion", "Check Box 36"),
                ["Religion"] = ("Religion", "Check Box 37"),
                ["Sleight of Hand"] = ("SleightofHand", "Check Box 38"),
                ["Stealth"] = ("Stealth ", "Check Box 39"),           // trailing space
                ["Survival"] = ("Survival", "Check Box 40"),
            };

            foreach (var skill in skills)
            {
                if (!skillFields.TryGetValue(skill.Name, out var map)) continue;
                T(map.Field, FmtMod(skill.Bonus));
                C(map.Check, skill.IsProficient);
            }

            // Passive Perception = 10 + Perception bonus
            var perception = skills.FirstOrDefault(s =>
                s.Name.Equals("Perception", StringComparison.OrdinalIgnoreCase));
            int passive = 10 + (perception?.Bonus ?? wisMod);
            T("Passive", passive.ToString());

            // ── Weapons (up to 3 attack rows) ──
            var weapons = extras.Weapons ?? DeriveWeapons(c, prof);
            if (weapons.Count > 0)
            {
                T("Wpn Name", weapons[0].Name);
                T("Wpn1 AtkBonus", weapons[0].AttackBonus);
                T("Wpn1 Damage", weapons[0].Damage);
            }
            if (weapons.Count > 1)
            {
                T("Wpn Name 2", weapons[1].Name);
                T("Wpn2 AtkBonus ", weapons[1].AttackBonus); // trailing space
                T("Wpn2 Damage ", weapons[1].Damage);
            }
            if (weapons.Count > 2)
            {
                T("Wpn Name 3", weapons[2].Name);
                T("Wpn3 AtkBonus  ", weapons[2].AttackBonus); // two trailing spaces
                T("Wpn3 Damage ", weapons[2].Damage);
            }

            // Extra attack notes (overflow)
            if (weapons.Count > 3)
            {
                var extra = string.Join("\n", weapons.Skip(3)
                    .Select(w => $"{w.Name}  {w.AttackBonus}  {w.Damage}"));
                T("AttacksSpellcasting", extra);
            }

            // ── Equipment / proficiencies / features ──
            string equipment = extras.EquipmentText
                ?? (c.Equipment != null ? string.Join("\n", c.Equipment) : "");
            if (!string.IsNullOrWhiteSpace(c.BackgroundEquipment))
            {
                if (!string.IsNullOrWhiteSpace(equipment)) equipment += "\n";
                equipment += c.BackgroundEquipment;
            }
            T("Equipment", equipment);

            string profLang = extras.ProficienciesAndLanguages ?? DeriveProficienciesAndLanguages(c);
            T("ProficienciesLang", profLang);

            string features = extras.FeaturesAndTraits ?? DeriveFeaturesAndTraits(c);
            T("Features and Traits", features);
            // Page 2 additional features area
            if (features.Length > 1200)
                T("Feat+Traits", features.Substring(Math.Max(0, features.Length - 800)));

            // ── Spellcasting (page 3) ──
            if (!string.IsNullOrWhiteSpace(c.SpellcastingAbility) ||
                (c.Cantrips?.Count > 0) || (c.Level1Spells?.Count > 0))
            {
                string spellClass = c.Class ?? "";
                if (!string.IsNullOrWhiteSpace(c.Subclass))
                    spellClass += $" ({c.Subclass})";
                T("Spellcasting Class 2", spellClass);

                string abilityAbbr = (c.SpellcastingAbility ?? "").Trim().ToUpperInvariant() switch
                {
                    "INTELLIGENCE" or "INT" => "INT",
                    "WISDOM" or "WIS" => "WIS",
                    "CHARISMA" or "CHA" => "CHA",
                    _ => AbbreviateAbility(c.SpellcastingAbility)
                };
                T("SpellcastingAbility 2", abilityAbbr);

                if (c.SpellSaveDC > 0)
                    T("SpellSaveDC  2", c.SpellSaveDC.ToString()); // two spaces in name
                if (c.SpellAttackBonus != 0 || c.SpellSaveDC > 0)
                    T("SpellAtkBonus 2", FmtMod(c.SpellAttackBonus));

                int slots = extras.Level1SpellSlots ?? DeriveLevel1Slots(c.Class);
                if (slots > 0)
                {
                    T("SlotsTotal 19", slots.ToString());
                    T("SlotsRemaining 19", slots.ToString());
                }
            }

            // Cantrips → Spells 1014–1022
            var cantripSlots = new[]
            {
                "Spells 1014", "Spells 1015", "Spells 1016", "Spells 1017", "Spells 1018",
                "Spells 1019", "Spells 1020", "Spells 1021", "Spells 1022"
            };
            var cantrips = c.Cantrips ?? new List<string>();
            for (int i = 0; i < cantripSlots.Length && i < cantrips.Count; i++)
                T(cantripSlots[i], cantrips[i]);

            // 1st-level spells → Spells 1023 + 1024–1033
            var level1Slots = new[]
            {
                "Spells 1023",
                "Spells 1024", "Spells 1025", "Spells 1026", "Spells 1027", "Spells 1028",
                "Spells 1029", "Spells 1030", "Spells 1031", "Spells 1032", "Spells 1033"
            };
            var level1 = c.Level1Spells ?? new List<string>();
            for (int i = 0; i < level1Slots.Length && i < level1.Count; i++)
                T(level1Slots[i], level1[i]);

            return payload;
        }

        private static int Mod(int score) => (int)Math.Floor((score - 10) / 2.0);

        private static string BuildClassLevel(Character c)
        {
            if (string.IsNullOrWhiteSpace(c.Class)) return "Level 1";

            // Nemo is level-1 focused
            if (!string.IsNullOrWhiteSpace(c.Subclass))
                return $"{c.Subclass} {c.Class} 1";
            return $"{c.Class} 1";
        }

        private static string DeriveHitDice(Character c)
        {
            if (!string.IsNullOrWhiteSpace(c.Class) &&
                GameData.ClassData.TryGetValue(c.Class, out var data) &&
                !string.IsNullOrWhiteSpace(data.HitDie))
            {
                // ClassData stores values like "1d8" or "d8"
                string hd = data.HitDie.Trim();
                if (hd.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                    return "1" + hd;
                if (hd.StartsWith("1d", StringComparison.OrdinalIgnoreCase))
                    return hd;
                return "1" + (hd.StartsWith("d", StringComparison.OrdinalIgnoreCase) ? hd : "d8");
            }
            return "1d8";
        }

        private static int DeriveLevel1Slots(string? className)
        {
            if (string.IsNullOrWhiteSpace(className)) return 0;
            return className switch
            {
                "Paladin" or "Ranger" => 0,
                "Warlock" => 1,
                "Fighter" or "Rogue" or "Monk" or "Barbarian" => 0,
                _ when GameData.ClassData.TryGetValue(className, out var d) && d.Spellcasting => 2,
                _ => 0
            };
        }

        private static string AbbreviateAbility(string? ability)
        {
            if (string.IsNullOrWhiteSpace(ability)) return "";
            return ability.Trim().ToUpperInvariant() switch
            {
                "STRENGTH" => "STR",
                "DEXTERITY" => "DEX",
                "CONSTITUTION" => "CON",
                "INTELLIGENCE" => "INT",
                "WISDOM" => "WIS",
                "CHARISMA" => "CHA",
                _ => ability.Length >= 3 ? ability.Substring(0, 3).ToUpperInvariant() : ability.ToUpperInvariant()
            };
        }

        private static List<SavingThrow> DeriveSavingThrows(Character c, int prof)
        {
            var result = new List<SavingThrow>();
            var proficient = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(c.Class) &&
                GameData.ClassData.TryGetValue(c.Class, out var classData) &&
                classData.SavingThrowProficiencies != null)
            {
                foreach (var s in classData.SavingThrowProficiencies)
                    proficient.Add(s);
            }

            // Prefer saved list if present
            if (c.SavingThrows != null && c.SavingThrows.Count > 0)
                return c.SavingThrows.ToList();

            foreach (var ability in new[] { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" })
            {
                int mod = ability switch
                {
                    "Strength" => c.AbilityScores?.Strength?.Modifier ?? 0,
                    "Dexterity" => c.AbilityScores?.Dexterity?.Modifier ?? 0,
                    "Constitution" => c.AbilityScores?.Constitution?.Modifier ?? 0,
                    "Intelligence" => c.AbilityScores?.Intelligence?.Modifier ?? 0,
                    "Wisdom" => c.AbilityScores?.Wisdom?.Modifier ?? 0,
                    "Charisma" => c.AbilityScores?.Charisma?.Modifier ?? 0,
                    _ => 0
                };
                bool isProf = proficient.Contains(ability);
                result.Add(new SavingThrow
                {
                    Name = ability,
                    IsProficient = isProf,
                    Bonus = mod + (isProf ? prof : 0)
                });
            }
            return result;
        }

        private static List<SkillEntry> DeriveSkills(Character c, int prof)
        {
            var defs = new (string Name, string Ability, Func<Character, int> Mod)[]
            {
                ("Acrobatics", "Dex", ch => ch.AbilityScores?.Dexterity?.Modifier ?? 0),
                ("Animal Handling", "Wis", ch => ch.AbilityScores?.Wisdom?.Modifier ?? 0),
                ("Arcana", "Int", ch => ch.AbilityScores?.Intelligence?.Modifier ?? 0),
                ("Athletics", "Str", ch => ch.AbilityScores?.Strength?.Modifier ?? 0),
                ("Deception", "Cha", ch => ch.AbilityScores?.Charisma?.Modifier ?? 0),
                ("History", "Int", ch => ch.AbilityScores?.Intelligence?.Modifier ?? 0),
                ("Insight", "Wis", ch => ch.AbilityScores?.Wisdom?.Modifier ?? 0),
                ("Intimidation", "Cha", ch => ch.AbilityScores?.Charisma?.Modifier ?? 0),
                ("Investigation", "Int", ch => ch.AbilityScores?.Intelligence?.Modifier ?? 0),
                ("Medicine", "Wis", ch => ch.AbilityScores?.Wisdom?.Modifier ?? 0),
                ("Nature", "Int", ch => ch.AbilityScores?.Intelligence?.Modifier ?? 0),
                ("Perception", "Wis", ch => ch.AbilityScores?.Wisdom?.Modifier ?? 0),
                ("Performance", "Cha", ch => ch.AbilityScores?.Charisma?.Modifier ?? 0),
                ("Persuasion", "Cha", ch => ch.AbilityScores?.Charisma?.Modifier ?? 0),
                ("Religion", "Int", ch => ch.AbilityScores?.Intelligence?.Modifier ?? 0),
                ("Sleight of Hand", "Dex", ch => ch.AbilityScores?.Dexterity?.Modifier ?? 0),
                ("Stealth", "Dex", ch => ch.AbilityScores?.Dexterity?.Modifier ?? 0),
                ("Survival", "Wis", ch => ch.AbilityScores?.Wisdom?.Modifier ?? 0),
            };

            var proficient = new HashSet<string>(
                (c.Skills ?? new List<SkillEntry>()).Select(s => s.Name),
                StringComparer.OrdinalIgnoreCase);

            return defs.Select(d =>
            {
                bool isProf = proficient.Contains(d.Name);
                int bonus = d.Mod(c) + (isProf ? prof : 0);
                return new SkillEntry
                {
                    Name = d.Name,
                    Ability = d.Ability,
                    IsProficient = isProf,
                    Bonus = bonus
                };
            }).ToList();
        }

        private static List<WeaponAttackLine> DeriveWeapons(Character c, int prof)
        {
            var result = new List<WeaponAttackLine>();
            if (c.Equipment == null) return result;

            int strMod = c.AbilityScores?.Strength?.Modifier ?? 0;
            int dexMod = c.AbilityScores?.Dexterity?.Modifier ?? 0;
            var allWeapons = GameData.SimpleWeapons.Concat(GameData.MartialWeapons).ToList();

            foreach (var item in c.Equipment)
            {
                var weapon = allWeapons.FirstOrDefault(w =>
                {
                    string lowerItem = item.ToLowerInvariant();
                    string lowerName = w.Name.ToLowerInvariant();
                    return lowerItem == lowerName ||
                           lowerItem.StartsWith(lowerName + " ") ||
                           lowerItem.Contains(" " + lowerName + " ") ||
                           lowerItem.EndsWith(" " + lowerName);
                });
                if (weapon == null) continue;

                bool hasFinesse = weapon.Properties.Contains("Finesse", StringComparison.OrdinalIgnoreCase);
                bool isRanged = !string.IsNullOrWhiteSpace(weapon.Range) && weapon.Range != "-";

                int attackMod;
                int damageMod;
                if (isRanged)
                {
                    attackMod = prof + dexMod;
                    damageMod = dexMod;
                }
                else if (hasFinesse)
                {
                    int best = Math.Max(strMod, dexMod);
                    attackMod = prof + best;
                    damageMod = best;
                }
                else
                {
                    attackMod = prof + strMod;
                    damageMod = strMod;
                }

                result.Add(new WeaponAttackLine
                {
                    Name = weapon.Name,
                    AttackBonus = attackMod >= 0 ? $"+{attackMod}" : attackMod.ToString(),
                    Damage = $"{weapon.Damage}{(damageMod >= 0 ? "+" : "")}{damageMod} {weapon.Type}".Trim()
                });

                if (result.Count >= 6) break;
            }

            return result;
        }

        private static string DeriveProficienciesAndLanguages(Character c)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(c.Class) &&
                GameData.ClassData.TryGetValue(c.Class, out var classData))
            {
                if (classData.ArmorProficiencies?.Count > 0)
                    sb.AppendLine("Armor: " + string.Join(", ", classData.ArmorProficiencies));
                if (classData.WeaponProficiencies?.Count > 0)
                    sb.AppendLine("Weapons: " + string.Join(", ", classData.WeaponProficiencies));
            }

            if (!string.IsNullOrEmpty(c.Race) &&
                GameData.RaceData.TryGetValue(c.Race, out var raceData) &&
                raceData.Languages?.Count > 0)
            {
                var langs = raceData.Languages.ToList();
                if (c.BackgroundLanguages != null)
                    langs.AddRange(c.BackgroundLanguages);
                sb.AppendLine("Languages: " + string.Join(", ", langs.Distinct(StringComparer.OrdinalIgnoreCase)));
            }

            return sb.ToString().Trim();
        }

        private static string DeriveFeaturesAndTraits(Character c)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(c.SelectedFeat))
                sb.AppendLine("Feat: " + c.SelectedFeat);

            if (!string.IsNullOrEmpty(c.Race) &&
                GameData.RaceData.TryGetValue(c.Race, out var raceData) &&
                raceData.Traits?.Count > 0)
            {
                sb.AppendLine($"— {c.Race} Traits —");
                foreach (var t in raceData.Traits)
                    sb.AppendLine("• " + t);
            }

            if (!string.IsNullOrEmpty(c.Subrace) &&
                !string.IsNullOrEmpty(c.Race) &&
                GameData.RaceSubraces.TryGetValue(c.Race, out var subs))
            {
                var sub = subs.FirstOrDefault(s =>
                    s.Name.Equals(c.Subrace, StringComparison.OrdinalIgnoreCase));
                if (sub?.Traits?.Count > 0)
                {
                    sb.AppendLine($"— {c.Subrace} —");
                    foreach (var t in sub.Traits)
                        sb.AppendLine("• " + t);
                }
            }

            if (!string.IsNullOrEmpty(c.Class))
            {
                var classFeats = GameData.GetClassFeaturesUpToLevel(c.Class, 1, includeOptional: true);
                if (classFeats.Count == 0 && GameData.ClassLevel1Features.TryGetValue(c.Class, out var legacy))
                    classFeats = legacy;
                if (classFeats.Count > 0)
                {
                    sb.AppendLine($"— {c.Class} Features —");
                    foreach (var f in classFeats)
                    {
                        sb.AppendLine("• " + f.Name);
                        if (!string.IsNullOrWhiteSpace(f.Description))
                            sb.AppendLine("  " + f.Description);
                    }
                }
            }

            if (!string.IsNullOrEmpty(c.Subclass) &&
                GameData.SubclassLevel1Features.TryGetValue(c.Subclass, out var subFeats))
            {
                sb.AppendLine($"— {c.Subclass} —");
                foreach (var f in subFeats)
                {
                    sb.AppendLine("• " + f.Name);
                    if (!string.IsNullOrWhiteSpace(f.Description))
                        sb.AppendLine("  " + f.Description);
                }
            }

            return sb.ToString().Trim();
        }
    }
}
