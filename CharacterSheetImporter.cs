using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace Nemo
{
    /// <summary>
    /// Imports a D&amp;D 5e fillable character sheet PDF into Nemo's <see cref="Character"/> model.
    /// Field names and layout match <see cref="CharacterSheetExporter"/> (same official template).
    /// Name-based reads are preferred; positional indices remain as a fallback for atypical sheets.
    /// </summary>
    public static class CharacterSheetImporter
    {
        /// <summary>
        /// Import a character from a fillable PDF. Returns the character and an optional user-facing note.
        /// </summary>
        public static (Character Character, string? Note) ImportFromPdf(string pdfPath)
        {
            var fields = ExtractFormFields(pdfPath);
            var byName = BuildNameLookup(fields);
            var indexed = BuildIndexedFields(fields, byName);
            var character = BuildCharacter(indexed, byName, fields);

            string? note = null;
            int level = character.Level > 0 ? character.Level : 1;
            if (level > 1)
            {
                note = $"Imported level {level} sheet (abilities, skills, equipment, class levels, " +
                       "cantrips, and leveled spells 1–9). Review class features and multiclass picks in Nemo.";
            }

            return (character, note);
        }

        // ───────────────────────── Form field extraction ─────────────────────────

        private static List<(string Name, string Value)> ExtractFormFields(string pdfPath)
        {
            var fields = new List<(string Name, string Value)>();

            try
            {
                using var document = PdfDocument.Open(pdfPath);

                if (document.TryGetForm(out var form))
                {
                    foreach (var field in form.Fields)
                    {
                        string name = ExtractFieldName(field);
                        string value = GetPropertyValue(field, "Value") ?? "";
                        // Keep empty fields so positional indices stay stable
                        fields.Add((name, value.Trim()));
                    }
                }
            }
            catch
            {
                // Fall through with empty list
            }

            return fields;
        }

        /// <summary>
        /// Lookup by exact AcroForm partial name and by trimmed name (template has trailing spaces).
        /// Last non-empty write wins for duplicates.
        /// </summary>
        private static Dictionary<string, string> BuildNameLookup(List<(string Name, string Value)> fields)
        {
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in fields)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                // Prefer non-empty values when the same name appears more than once
                void Put(string key, string val)
                {
                    if (string.IsNullOrEmpty(key)) return;
                    if (byName.TryGetValue(key, out var existing) &&
                        !string.IsNullOrWhiteSpace(existing) &&
                        string.IsNullOrWhiteSpace(val))
                        return;
                    byName[key] = val ?? "";
                }

                Put(name, value);
                string trimmed = name.Trim();
                if (!string.Equals(trimmed, name, StringComparison.Ordinal))
                    Put(trimmed, value);
            }
            return byName;
        }

        private static string Field(Dictionary<string, string> byName, params string[] names)
        {
            foreach (var n in names)
            {
                if (string.IsNullOrEmpty(n)) continue;
                if (byName.TryGetValue(n, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v.Trim();
                if (byName.TryGetValue(n.Trim(), out v) && !string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return "";
        }

        private static bool IsCheckboxOn(Dictionary<string, string> byName, string checkName)
        {
            if (string.IsNullOrEmpty(checkName)) return false;
            if (!byName.TryGetValue(checkName, out var raw) &&
                !byName.TryGetValue(checkName.Trim(), out raw))
                return false;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string v = raw.Trim();
            // Common AcroForm on-states
            if (v.Equals("Off", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("No", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            if (v.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("/Yes", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("/On", StringComparison.OrdinalIgnoreCase))
                return true;

            // Some readers store the export value as the on-state name
            return !v.Equals("Off", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Semantic map: exporter field names first, then positional fallbacks for third-party sheets.
        /// </summary>
        public static Dictionary<string, string> ExtractIndexedFormFields(string pdfPath)
        {
            var fields = ExtractFormFields(pdfPath);
            var byName = BuildNameLookup(fields);
            return BuildIndexedFields(fields, byName);
        }

        private static Dictionary<string, string> BuildIndexedFields(
            List<(string Name, string Value)> fields,
            Dictionary<string, string> byName)
        {
            var indexed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string GetAt(int index) =>
                (index >= 0 && index < fields.Count) ? fields[index].Value : "";

            // ── Positional fallbacks (legacy / third-party fills) ──
            indexed["CharacterName"] = GetAt(17);
            indexed["StrengthScore"] = GetAt(22);
            indexed["StrengthBonus"] = GetAt(28);
            indexed["DexterityScore"] = GetAt(31);
            indexed["DexterityBonus"] = GetAt(34);
            indexed["ConstitutionScore"] = GetAt(37);
            indexed["ConstitutionBonus"] = GetAt(42);
            indexed["IntelligenceScore"] = GetAt(48);
            indexed["IntelligenceBonus"] = GetAt(70);
            indexed["WisdomScore"] = GetAt(75);
            indexed["WisdomBonus"] = GetAt(85);
            indexed["CharismaScore"] = GetAt(82);
            indexed["CharismaBonus"] = GetAt(108);
            indexed["StrengthSave"] = GetAt(30);
            indexed["DexteritySave"] = GetAt(49);
            indexed["ConstitutionSave"] = GetAt(50);
            indexed["IntelligenceSave"] = GetAt(51);
            indexed["WisdomSave"] = GetAt(52);
            indexed["CharismaSave"] = GetAt(53);
            indexed["ClassAndLevel"] = GetAt(14);
            indexed["Background"] = GetAt(15);
            indexed["PlayerName"] = GetAt(16);
            indexed["Race"] = GetAt(18);
            indexed["Alignment"] = GetAt(19);
            indexed["ArmorClass"] = GetAt(24);
            indexed["ProficiencyBonus"] = GetAt(23);
            indexed["InitiativeBonus"] = GetAt(25);
            indexed["Speed"] = GetAt(26);
            indexed["MaxHitPoints"] = GetAt(29);
            indexed["CurrentHitPoints"] = GetAt(32);
            indexed["HitDice"] = GetAt(46);
            indexed["PassivePerception"] = GetAt(111);
            indexed["Equipment"] = GetAt(118);
            indexed["FeaturesAndTraits"] = GetAt(119);
            indexed["AdditionalFeaturesAndTraits"] = GetAt(10);
            indexed["WeaponSpell1Name"] = GetAt(67);
            indexed["WeaponSpell1AttackBonus"] = GetAt(68);
            indexed["WeaponSpell1Damage"] = GetAt(69);
            indexed["WeaponSpell2Name"] = GetAt(71);
            indexed["WeaponSpell2AttackBonus"] = GetAt(72);
            indexed["WeaponSpell2Damage"] = GetAt(73);
            indexed["WeaponSpell3Name"] = GetAt(76);
            indexed["WeaponSpell3AttackBonus"] = GetAt(77);
            indexed["WeaponSpell3Damage"] = GetAt(79);
            indexed["SpellcastingAbility"] = GetAt(121);
            indexed["SpellSaveDC"] = GetAt(122);
            indexed["SpellAttackBonus"] = GetAt(123);

            // ── Official template names (same as CharacterSheetExporter) — win when present ──
            void Prefer(string key, params string[] fieldNames)
            {
                string v = Field(byName, fieldNames);
                if (!string.IsNullOrWhiteSpace(v))
                    indexed[key] = v;
            }

            Prefer("CharacterName", "CharacterName", "CharacterName 2", "Character Name");
            Prefer("PlayerName", "PlayerName", "Player Name");
            Prefer("ClassAndLevel", "ClassLevel", "ClassAndLevel", "Class Level", "Class");
            Prefer("Background", "Background");
            Prefer("Race", "Race ", "Race"); // trailing space on template
            Prefer("Alignment", "Alignment");
            Prefer("ArmorClass", "AC", "ArmorClass", "Armor Class");
            Prefer("ProficiencyBonus", "ProfBonus", "ProficiencyBonus");
            Prefer("InitiativeBonus", "Initiative", "InitiativeBonus");
            Prefer("Speed", "Speed");
            Prefer("MaxHitPoints", "HPMax", "MaxHitPoints", "HitPoints", "HP");
            Prefer("CurrentHitPoints", "HPCurrent", "CurrentHitPoints");
            Prefer("HitDice", "HDTotal", "HD", "HitDice");
            Prefer("PassivePerception", "Passive", "PassivePerception");
            Prefer("Equipment", "Equipment", "EquipmentText");
            Prefer("FeaturesAndTraits", "Features and Traits", "FeaturesAndTraits", "FeaturesTraits");
            Prefer("AdditionalFeaturesAndTraits", "Feat+Traits", "AdditionalFeaturesAndTraits");
            Prefer("ProficienciesLang", "ProficienciesLang", "Proficiencies and Languages");
            Prefer("GoldPieces", "GP", "Gold", "GoldPieces");
            Prefer("AttacksSpellcasting", "AttacksSpellcasting");

            Prefer("SpellcastingAbility", "SpellcastingAbility 2", "SpellcastingAbility", "SpellAbility");
            Prefer("SpellSaveDC", "SpellSaveDC  2", "SpellSaveDC 2", "SpellSaveDC", "SpellDC");
            Prefer("SpellAttackBonus", "SpellAtkBonus 2", "SpellAttackBonus", "SpellAttack");
            Prefer("SpellcastingClass", "Spellcasting Class 2", "SpellcastingClass");

            Prefer("StrengthScore", "STR", "Strength", "StrengthScore");
            Prefer("DexterityScore", "DEX", "Dexterity", "DexterityScore");
            Prefer("ConstitutionScore", "CON", "Constitution", "ConstitutionScore");
            Prefer("IntelligenceScore", "INT", "Intelligence", "IntelligenceScore");
            Prefer("WisdomScore", "WIS", "Wisdom", "WisdomScore");
            Prefer("CharismaScore", "CHA", "Charisma", "CharismaScore");

            Prefer("StrengthBonus", "STRmod", "StrengthBonus");
            Prefer("DexterityBonus", "DEXmod ", "DEXmod", "DexterityBonus");
            Prefer("ConstitutionBonus", "CONmod", "ConstitutionBonus");
            Prefer("IntelligenceBonus", "INTmod", "IntelligenceBonus");
            Prefer("WisdomBonus", "WISmod", "WisdomBonus");
            Prefer("CharismaBonus", "CHamod", "CHAmod", "CharismaBonus");

            Prefer("StrengthSave", "ST Strength", "StrengthSave", "STRsave");
            Prefer("DexteritySave", "ST Dexterity", "DexteritySave", "DEXsave");
            Prefer("ConstitutionSave", "ST Constitution", "ConstitutionSave", "CONsave");
            Prefer("IntelligenceSave", "ST Intelligence", "IntelligenceSave", "INTsave");
            Prefer("WisdomSave", "ST Wisdom", "WisdomSave", "WISsave");
            Prefer("CharismaSave", "ST Charisma", "CharismaSave", "CHAsave");

            // Skills — exporter field names (including trailing spaces)
            foreach (var (display, map) in CharacterSheetExporter.SkillFieldMap)
            {
                string key = display.Replace(" ", "");
                if (display.Equals("Sleight of Hand", StringComparison.OrdinalIgnoreCase))
                    key = "SleightOfHand";
                if (display.Equals("Animal Handling", StringComparison.OrdinalIgnoreCase))
                    key = "AnimalHandling";
                Prefer(key, map.Field, display, display + "Bonus");
            }

            Prefer("WeaponSpell1Name", "Wpn Name", "WpnName", "WeaponSpell1Name");
            Prefer("WeaponSpell1AttackBonus", "Wpn1 AtkBonus", "Wpn1AtkBonus");
            Prefer("WeaponSpell1Damage", "Wpn1 Damage", "Wpn1Damage");
            Prefer("WeaponSpell2Name", "Wpn Name 2", "WpnName2");
            Prefer("WeaponSpell2AttackBonus", "Wpn2 AtkBonus ", "Wpn2 AtkBonus", "Wpn2AtkBonus");
            Prefer("WeaponSpell2Damage", "Wpn2 Damage ", "Wpn2 Damage", "Wpn2Damage");
            Prefer("WeaponSpell3Name", "Wpn Name 3", "WpnName3");
            Prefer("WeaponSpell3AttackBonus", "Wpn3 AtkBonus  ", "Wpn3 AtkBonus", "Wpn3AtkBonus");
            Prefer("WeaponSpell3Damage", "Wpn3 Damage ", "Wpn3 Damage", "Wpn3Damage");

            // Spell slot totals (1–9) — exporter writes SlotsTotal 19–27
            for (int lvl = 1; lvl <= 9; lvl++)
            {
                var (total, remaining) = CharacterSheetExporter.SlotFieldsByLevel[lvl];
                Prefer($"Level{lvl}SlotsTotal", total);
                Prefer($"Level{lvl}SlotsRemaining", remaining);
            }

            return indexed;
        }

        private static string? GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) return null;
            var prop = obj.GetType().GetProperty(propertyName);
            return prop?.GetValue(obj)?.ToString();
        }

        /// <summary>
        /// PdfPig exposes the partial field name via Information ("Partial Name: X.") rather than FieldName.
        /// </summary>
        private static string ExtractFieldName(object field)
        {
            string? fromProps = GetPropertyValue(field, "FieldName")
                             ?? GetPropertyValue(field, "Name");
            if (!string.IsNullOrWhiteSpace(fromProps))
                return fromProps;

            string info = GetPropertyValue(field, "Information") ?? "";
            const string prefix = "Partial Name:";
            int idx = info.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                string rest = info.Substring(idx + prefix.Length).Trim().TrimEnd('.');
                return rest;
            }

            return "";
        }

        private static string Get(Dictionary<string, string> indexed, string key) =>
            indexed.TryGetValue(key, out var v) ? (v ?? "") : "";

        private static int ParseInt(string? raw, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            string cleaned = Regex.Replace(raw.Trim(), @"[^\d+\-]", "");
            if (cleaned.Length == 0) return fallback;
            return int.TryParse(cleaned, out int n) ? n : fallback;
        }

        private static int ParseBonus(string? raw, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            string cleaned = raw.Trim().Replace("+", "");
            cleaned = Regex.Match(cleaned, @"-?\d+").Value;
            return int.TryParse(cleaned, out int n) ? n : fallback;
        }

        // ───────────────────────── Character building ─────────────────────────

        private static Character BuildCharacter(
            Dictionary<string, string> indexed,
            Dictionary<string, string> byName,
            List<(string Name, string Value)> formFields)
        {
            var character = new Character();

            character.Name = Get(indexed, "CharacterName");
            if (string.IsNullOrWhiteSpace(character.Name))
                character.Name = ExtractCharacterNameFallback(formFields) ?? "Imported Character";

            character.PlayerName = Get(indexed, "PlayerName");

            // Race / subrace — exporter writes "Race (Subrace)"
            string rawRace = Get(indexed, "Race");
            var (race, subrace) = ParseRace(rawRace);
            character.Race = race;
            character.Subrace = subrace;

            // Class / subclass / level / multiclass ClassLevels
            string classAndLevel = Get(indexed, "ClassAndLevel");
            var classLevels = ParseClassLevels(classAndLevel);
            if (classLevels.Count > 0)
            {
                character.ClassLevels = classLevels;
                character.Class = classLevels[0].ClassName;
                character.Subclass = classLevels[0].Subclass ?? "";
                character.Level = Math.Clamp(classLevels.Sum(e => e.Levels), 1, 20);
            }
            else
            {
                var (cls, subclass, level) = ParseClassAndLevel(classAndLevel);
                character.Class = cls;
                character.Subclass = subclass;
                character.Level = Math.Clamp(level, 1, 20);
                if (!string.IsNullOrWhiteSpace(cls))
                {
                    character.ClassLevels = new List<ClassLevelEntry>
                    {
                        new(cls, character.Level, subclass)
                    };
                }
            }

            // Proficiency from level when sheet omits it
            int profFromLevel = character.Level switch
            {
                <= 4 => 2,
                <= 8 => 3,
                <= 12 => 4,
                <= 16 => 5,
                _ => 6
            };
            character.ProficiencyBonus = Math.Max(
                profFromLevel,
                Math.Max(2, ParseBonus(Get(indexed, "ProficiencyBonus"), profFromLevel)));

            character.Background = MatchBackground(Get(indexed, "Background"));

            character.AbilityScores.Strength = BuildAbility(
                Get(indexed, "StrengthScore"), Get(indexed, "StrengthBonus"));
            character.AbilityScores.Dexterity = BuildAbility(
                Get(indexed, "DexterityScore"), Get(indexed, "DexterityBonus"));
            character.AbilityScores.Constitution = BuildAbility(
                Get(indexed, "ConstitutionScore"), Get(indexed, "ConstitutionBonus"));
            character.AbilityScores.Intelligence = BuildAbility(
                Get(indexed, "IntelligenceScore"), Get(indexed, "IntelligenceBonus"));
            character.AbilityScores.Wisdom = BuildAbility(
                Get(indexed, "WisdomScore"), Get(indexed, "WisdomBonus"));
            character.AbilityScores.Charisma = BuildAbility(
                Get(indexed, "CharismaScore"), Get(indexed, "CharismaBonus"));

            character.Initiative = ParseBonus(Get(indexed, "InitiativeBonus"),
                character.AbilityScores.Dexterity.Modifier);

            // AC — exporter writes "17" or "17(15)" with shield
            ParseArmorClass(Get(indexed, "ArmorClass"), character);

            character.HitPoints = ParseInt(Get(indexed, "MaxHitPoints"),
                ParseInt(Get(indexed, "CurrentHitPoints"), 0));
            character.Speed = ParseSpeed(Get(indexed, "Speed"));

            // Spellcasting header (page 3)
            character.SpellcastingAbility = NormalizeSpellAbility(Get(indexed, "SpellcastingAbility"));
            character.SpellSaveDC = ParseInt(Get(indexed, "SpellSaveDC"), 0);
            character.SpellAttackBonus = ParseBonus(Get(indexed, "SpellAttackBonus"), 0);

            // Skills — prefer proficiency checkboxes when present
            character.Skills = ParseSkills(indexed, byName, character);

            // Saving throws — prefer checkboxes
            character.SavingThrows = ParseSavingThrows(indexed, byName, character);

            // Equipment + weapon rows + gold notes
            character.Equipment = ParseEquipment(indexed, character);

            // GP coin box (exporter notes also parse into gold fields)
            int gp = ParseInt(Get(indexed, "GoldPieces"), 0);
            if (gp > 0)
                character.GoldPieces = gp;

            // Features text → feat, fighting styles, invocations, metamagic, pact boon
            string featuresText =
                Get(indexed, "FeaturesAndTraits") + "\n" +
                Get(indexed, "AdditionalFeaturesAndTraits") + "\n" +
                Get(indexed, "ProficienciesLang");
            ApplyFeatureDetections(character, featuresText);

            // Spells from official page-3 field names (cantrips + levels 1–9)
            ParseSpellsFromSheet(character, byName, indexed);

            return character;
        }

        private static void ParseArmorClass(string raw, Character character)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                character.ArmorClass = 10;
                return;
            }

            // "17(15)" → total 17, without shield 15
            var m = Regex.Match(raw.Trim(), @"^(\d+)\s*\(\s*(\d+)\s*\)");
            if (m.Success &&
                int.TryParse(m.Groups[1].Value, out int total) &&
                int.TryParse(m.Groups[2].Value, out int without))
            {
                character.ArmorClass = total;
                character.EquippedACDisplay = $"({total})  AC without shield: {without} [Shield]";
                return;
            }

            character.ArmorClass = ParseInt(raw, 10);
            character.EquippedACDisplay = character.ArmorClass > 0
                ? $"({character.ArmorClass})"
                : "";
        }

        private static AbilityScore BuildAbility(string scoreRaw, string bonusRaw)
        {
            int final = ParseInt(scoreRaw, 10);
            if (final < 1) final = 10;
            int mod = ParseBonus(bonusRaw, CalculateModifier(final));
            return new AbilityScore
            {
                Base = final,
                Racial = 0,
                Feat = 0,
                Final = final,
                Modifier = mod
            };
        }

        private static int CalculateModifier(int score) => (int)Math.Floor((score - 10) / 2.0);

        private static int ParseSpeed(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 30;
            var m = Regex.Match(raw, @"\d+");
            return m.Success && int.TryParse(m.Value, out int s) ? s : 30;
        }

        private static string NormalizeSpellAbility(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string u = raw.Trim().ToUpperInvariant();
            return u switch
            {
                "INT" or "INTELLIGENCE" => "Intelligence",
                "WIS" or "WISDOM" => "Wisdom",
                "CHA" or "CHARISMA" => "Charisma",
                "STR" or "STRENGTH" => "Strength",
                "DEX" or "DEXTERITY" => "Dexterity",
                "CON" or "CONSTITUTION" => "Constitution",
                _ => raw.Trim()
            };
        }

        // ───────────────────────── Race / class / background ─────────────────────────

        private static (string Race, string Subrace) ParseRace(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return ("", "");

            string text = raw.Trim();

            // Exporter format: "Elf (High Elf)" or "Race (Subrace)"
            var paren = Regex.Match(text, @"^(.+?)\s*\((.+)\)\s*$");
            if (paren.Success)
            {
                string outer = paren.Groups[1].Value.Trim();
                string inner = paren.Groups[2].Value.Trim();
                foreach (var raceKey in GameData.RaceData.Keys.OrderByDescending(k => k.Length))
                {
                    if (!outer.Equals(raceKey, StringComparison.OrdinalIgnoreCase) &&
                        !outer.Contains(raceKey, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (GameData.RaceSubraces.TryGetValue(raceKey, out var subs))
                    {
                        foreach (var s in subs.OrderByDescending(x => x.Name.Length))
                        {
                            if (inner.Equals(s.Name, StringComparison.OrdinalIgnoreCase) ||
                                s.Name.Contains(inner, StringComparison.OrdinalIgnoreCase) ||
                                inner.Contains(s.Name, StringComparison.OrdinalIgnoreCase))
                                return (raceKey, s.Name);
                        }
                    }
                    return (raceKey, inner);
                }
            }

            foreach (var raceKey in GameData.RaceData.Keys.OrderByDescending(k => k.Length))
            {
                if (text.Equals(raceKey, StringComparison.OrdinalIgnoreCase))
                    return (raceKey, "");
            }

            foreach (var kvp in GameData.RaceSubraces)
            {
                foreach (var sub in kvp.Value)
                {
                    if (text.Equals(sub.Name, StringComparison.OrdinalIgnoreCase) ||
                        text.Contains(sub.Name, StringComparison.OrdinalIgnoreCase) ||
                        sub.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                    {
                        return (kvp.Key, sub.Name);
                    }
                }
            }

            foreach (var raceKey in GameData.RaceData.Keys.OrderByDescending(k => k.Length))
            {
                if (text.Contains(raceKey, StringComparison.OrdinalIgnoreCase))
                {
                    string sub = "";
                    if (GameData.RaceSubraces.TryGetValue(raceKey, out var subs))
                    {
                        foreach (var s in subs)
                        {
                            string shortName = s.Name
                                .Replace(raceKey, "", StringComparison.OrdinalIgnoreCase)
                                .Replace("(", "").Replace(")", "")
                                .Trim();
                            if (!string.IsNullOrEmpty(shortName) &&
                                text.Contains(shortName.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                sub = s.Name;
                                break;
                            }
                        }
                    }
                    return (raceKey, sub);
                }
            }

            if (text.Contains("half", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("elf", StringComparison.OrdinalIgnoreCase))
                return ("Half-Elf", "");
            if (text.Contains("half", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("orc", StringComparison.OrdinalIgnoreCase))
                return ("Half-Orc", "");
            if (text.Contains("harengon", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("rabbitfolk", StringComparison.OrdinalIgnoreCase))
                return ("Harengon", "");

            return (text, "");
        }

        /// <summary>
        /// Parses multiclass strings from exporter: <c>Twilight Cleric 9</c>,
        /// <c>Champion Fighter 3 / Lore Bard 2</c>.
        /// </summary>
        public static List<ClassLevelEntry> ParseClassLevels(string classAndLevel)
        {
            var result = new List<ClassLevelEntry>();
            if (string.IsNullOrWhiteSpace(classAndLevel))
                return result;

            var segments = classAndLevel.Split(new[] { '/', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var (cls, subclass, level) = ParseClassAndLevel(segment.Trim());
                if (string.IsNullOrWhiteSpace(cls)) continue;
                // Per-segment level: use number in segment, not sum of all numbers
                int segLevel = 1;
                var m = Regex.Match(segment.Trim(), @"(\d+)\s*$");
                if (m.Success && int.TryParse(m.Groups[1].Value, out int lv))
                    segLevel = Math.Clamp(lv, 1, 20);
                else
                    segLevel = Math.Clamp(level, 1, 20);

                result.Add(new ClassLevelEntry(cls, segLevel, subclass));
            }

            return result;
        }

        /// <summary>
        /// Parses strings like "War Cleric 9", "Fighter 1", "Twilight Domain Cleric 5".
        /// </summary>
        public static (string Class, string Subclass, int Level) ParseClassAndLevel(string classAndLevel)
        {
            if (string.IsNullOrWhiteSpace(classAndLevel))
                return ("", "", 1);

            int totalLevel = GetCharacterLevel(classAndLevel);

            string primary = classAndLevel.Split(new[] { '/', '|' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            primary = Regex.Replace(primary, @"\s+\d+\s*$", "").Trim();

            string matchedClass = "";
            string matchedSubclass = "";

            foreach (var classKey in GameData.ClassData.Keys.OrderByDescending(k => k.Length))
            {
                if (primary.Contains(classKey, StringComparison.OrdinalIgnoreCase) ||
                    primary.Equals(classKey, StringComparison.OrdinalIgnoreCase))
                {
                    matchedClass = classKey;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(matchedClass) &&
                GameData.ClassData.TryGetValue(matchedClass, out var classData))
            {
                string remainder = primary
                    .Replace(matchedClass, "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (!string.IsNullOrEmpty(remainder) && classData.Subclasses != null)
                {
                    matchedSubclass = MatchSubclassName(matchedClass, remainder, classData.Subclasses);
                }
            }
            else
            {
                matchedClass = primary;
            }

            return (matchedClass, matchedSubclass, totalLevel);
        }

        private static string MatchSubclassName(string className, string remainder, List<string> subclasses)
        {
            string rem = remainder.Trim();
            if (string.IsNullOrEmpty(rem)) return "";

            // Exact / contains against full subclass names
            foreach (var sub in subclasses.OrderByDescending(s => s.Length))
            {
                if (rem.Equals(sub, StringComparison.OrdinalIgnoreCase) ||
                    rem.Contains(sub, StringComparison.OrdinalIgnoreCase) ||
                    sub.Contains(rem, StringComparison.OrdinalIgnoreCase))
                    return sub;
            }

            // Short labels used by exporter: "Twilight" for "Twilight Domain", "Lore" for "College of Lore"
            foreach (var sub in subclasses.OrderByDescending(s => s.Length))
            {
                string shortLabel = CharacterSheetExporter.ShortSubclassLabel(sub);
                if (!string.IsNullOrEmpty(shortLabel) &&
                    (rem.Equals(shortLabel, StringComparison.OrdinalIgnoreCase) ||
                     rem.Contains(shortLabel, StringComparison.OrdinalIgnoreCase) ||
                     shortLabel.Equals(rem, StringComparison.OrdinalIgnoreCase)))
                    return sub;
            }

            // Word token match (e.g. "War" → War Domain)
            foreach (var sub in subclasses.OrderByDescending(s => s.Length))
            {
                if (rem.Split(' ').Any(w =>
                        w.Length >= 3 && sub.Contains(w, StringComparison.OrdinalIgnoreCase)))
                    return sub;
            }

            if (className == "Cleric")
            {
                foreach (var domain in GameData.ClericSubclasses.Keys)
                {
                    if (rem.Contains(domain, StringComparison.OrdinalIgnoreCase) ||
                        domain.Contains(rem, StringComparison.OrdinalIgnoreCase) ||
                        CharacterSheetExporter.ShortSubclassLabel(domain)
                            .Equals(rem, StringComparison.OrdinalIgnoreCase))
                        return domain;
                }
            }
            if (className == "Warlock")
            {
                foreach (var patron in GameData.WarlockSubclasses.Keys)
                {
                    if (rem.Contains(patron, StringComparison.OrdinalIgnoreCase) ||
                        patron.Contains(rem, StringComparison.OrdinalIgnoreCase) ||
                        CharacterSheetExporter.ShortSubclassLabel(patron)
                            .Equals(rem, StringComparison.OrdinalIgnoreCase))
                        return patron;
                }
            }

            return "";
        }

        /// <summary>
        /// Sum of all class levels from strings like "Cleric 9" or "Sorcerer 3 / Warlock 2".
        /// </summary>
        public static int GetCharacterLevel(string classAndLevel)
        {
            if (string.IsNullOrWhiteSpace(classAndLevel))
                return 1;

            int totalLevel = 0;
            foreach (Match match in Regex.Matches(classAndLevel, @"\d+"))
            {
                if (int.TryParse(match.Value, out int level))
                    totalLevel += level;
            }

            return totalLevel > 0 ? Math.Clamp(totalLevel, 1, 20) : 1;
        }

        private static string MatchBackground(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            foreach (var bg in GameData.AllBackgrounds)
            {
                if (raw.Equals(bg, StringComparison.OrdinalIgnoreCase) ||
                    raw.Contains(bg, StringComparison.OrdinalIgnoreCase) ||
                    bg.Contains(raw.Trim(), StringComparison.OrdinalIgnoreCase))
                    return bg;
            }

            return raw.Trim();
        }

        // ───────────────────────── Skills / saves ─────────────────────────

        private static readonly Dictionary<string, string> SkillAbility = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Acrobatics"] = "Dex",
            ["Animal Handling"] = "Wis",
            ["Arcana"] = "Int",
            ["Athletics"] = "Str",
            ["Deception"] = "Cha",
            ["History"] = "Int",
            ["Insight"] = "Wis",
            ["Intimidation"] = "Cha",
            ["Investigation"] = "Int",
            ["Medicine"] = "Wis",
            ["Nature"] = "Int",
            ["Perception"] = "Wis",
            ["Performance"] = "Cha",
            ["Persuasion"] = "Cha",
            ["Religion"] = "Int",
            ["Sleight of Hand"] = "Dex",
            ["Stealth"] = "Dex",
            ["Survival"] = "Wis",
        };

        private static List<SkillEntry> ParseSkills(
            Dictionary<string, string> indexed,
            Dictionary<string, string> byName,
            Character character)
        {
            var skills = new List<SkillEntry>();
            int prof = character.ProficiencyBonus;

            int AbilityMod(string ab) => ab switch
            {
                "Str" => character.AbilityScores.Strength.Modifier,
                "Dex" => character.AbilityScores.Dexterity.Modifier,
                "Con" => character.AbilityScores.Constitution.Modifier,
                "Int" => character.AbilityScores.Intelligence.Modifier,
                "Wis" => character.AbilityScores.Wisdom.Modifier,
                "Cha" => character.AbilityScores.Charisma.Modifier,
                _ => 0
            };

            foreach (var (display, map) in CharacterSheetExporter.SkillFieldMap)
            {
                string ability = SkillAbility.TryGetValue(display, out var ab) ? ab : "";
                int abilityMod = AbilityMod(ability);

                string rawBonus = Field(byName, map.Field);
                if (string.IsNullOrWhiteSpace(rawBonus))
                {
                    // Indexed key fallback
                    string key = display switch
                    {
                        "Animal Handling" => "AnimalHandling",
                        "Sleight of Hand" => "SleightOfHand",
                        _ => display.Replace(" ", "")
                    };
                    rawBonus = Get(indexed, key);
                }

                bool checkboxProf = IsCheckboxOn(byName, map.Check);
                int bonus = string.IsNullOrWhiteSpace(rawBonus)
                    ? abilityMod
                    : ParseBonus(rawBonus, abilityMod);

                bool proficient = checkboxProf;
                bool expertise = false;

                if (!proficient && !string.IsNullOrWhiteSpace(rawBonus))
                {
                    // Infer from bonus when checkbox missing
                    if (bonus >= abilityMod + prof * 2 - 1 && prof > 0)
                    {
                        proficient = true;
                        expertise = true;
                    }
                    else if (bonus >= abilityMod + Math.Max(1, prof - 1) && prof > 0)
                    {
                        proficient = true;
                    }
                }
                else if (proficient && bonus >= abilityMod + prof * 2 - 1 && prof > 0)
                {
                    expertise = true;
                }

                if (!proficient)
                    continue;

                skills.Add(new SkillEntry
                {
                    Name = display,
                    Ability = ability,
                    IsProficient = true,
                    IsExpertise = expertise,
                    Bonus = bonus
                });
            }

            return skills;
        }

        private static List<SavingThrow> ParseSavingThrows(
            Dictionary<string, string> indexed,
            Dictionary<string, string> byName,
            Character character)
        {
            var result = new List<SavingThrow>();
            int prof = character.ProficiencyBonus;

            foreach (var (ability, map) in CharacterSheetExporter.SaveFieldMap)
            {
                int abilityMod = ability switch
                {
                    "Strength" => character.AbilityScores.Strength.Modifier,
                    "Dexterity" => character.AbilityScores.Dexterity.Modifier,
                    "Constitution" => character.AbilityScores.Constitution.Modifier,
                    "Intelligence" => character.AbilityScores.Intelligence.Modifier,
                    "Wisdom" => character.AbilityScores.Wisdom.Modifier,
                    "Charisma" => character.AbilityScores.Charisma.Modifier,
                    _ => 0
                };

                string raw = Field(byName, map.Field);
                if (string.IsNullOrWhiteSpace(raw))
                    raw = Get(indexed, ability + "Save");

                int bonus = string.IsNullOrWhiteSpace(raw) ? abilityMod : ParseBonus(raw, abilityMod);
                bool proficient = IsCheckboxOn(byName, map.Check);
                if (!proficient && !string.IsNullOrWhiteSpace(raw))
                    proficient = bonus >= abilityMod + Math.Max(1, prof - 1);

                result.Add(new SavingThrow
                {
                    Name = ability,
                    Bonus = bonus,
                    IsProficient = proficient
                });
            }

            return result;
        }

        // ───────────────────────── Equipment / gold ─────────────────────────

        private static List<string> ParseEquipment(Dictionary<string, string> indexed, Character character)
        {
            var items = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string? item)
            {
                if (string.IsNullOrWhiteSpace(item)) return;
                string cleaned = item.Trim().TrimStart('•', '-', '*', '·').Trim();
                if (cleaned.Length == 0 || !seen.Add(cleaned)) return;

                // Gold note lines written by exporter — parse into gold fields, skip as gear
                if (TryParseGoldNote(cleaned, character))
                    return;

                items.Add(cleaned);
            }

            foreach (var key in new[] { "WeaponSpell1Name", "WeaponSpell2Name", "WeaponSpell3Name" })
            {
                string name = Get(indexed, key);
                if (string.IsNullOrWhiteSpace(name)) continue;

                bool looksLikeSpell = GameData.FindSpell(name) != null;
                if (!looksLikeSpell)
                    Add(name);
            }

            // Overflow attack lines
            string attacksExtra = Get(indexed, "AttacksSpellcasting");
            if (!string.IsNullOrWhiteSpace(attacksExtra))
            {
                foreach (var line in attacksExtra.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string first = line.Split(new[] { "  ", "\t" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    if (!string.IsNullOrWhiteSpace(first) && GameData.FindSpell(first) == null)
                        Add(first.Trim());
                }
            }

            string equipmentText = Get(indexed, "Equipment");
            if (!string.IsNullOrWhiteSpace(equipmentText))
            {
                // Prefer line splits (exporter notes are whole lines); then comma within lines
                foreach (var line in Regex.Split(equipmentText, @"[\r\n]+"))
                {
                    string t = line.Trim();
                    if (t.Length <= 1) continue;
                    if (TryParseGoldNote(t, character))
                        continue;

                    if (t.Contains(','))
                    {
                        foreach (var part in t.Split(','))
                            Add(part);
                    }
                    else
                    {
                        Add(t);
                    }
                }
            }

            return items;
        }

        private static bool TryParseGoldNote(string line, Character character)
        {
            // Starting gold: 4d4 … = 100 gp  OR  Starting gold: 100 gp
            if (line.StartsWith("Starting gold", StringComparison.OrdinalIgnoreCase))
            {
                character.UseRolledGoldInsteadOfEquipment = true;
                character.Level1RolledGoldBreakdown = line;
                var m = Regex.Match(line, @"(\d[\d,]*)\s*gp", RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups[1].Value.Replace(",", ""), out int gp))
                    character.Level1RolledGoldGp = gp;
                return true;
            }
            if (line.StartsWith("Higher-level wealth", StringComparison.OrdinalIgnoreCase))
            {
                character.HigherLevelWealthBreakdown = line;
                var m = Regex.Match(line, @"(\d[\d,]*)\s*gp", RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups[1].Value.Replace(",", ""), out int gp))
                    character.HigherLevelWealthGp = gp;
                return true;
            }
            if (line.StartsWith("Custom gold", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Custom / DM gold", StringComparison.OrdinalIgnoreCase))
            {
                var m = Regex.Match(line, @"(\d[\d,]*)\s*gp", RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups[1].Value.Replace(",", ""), out int gp))
                    character.CustomGoldGp = gp;
                var note = Regex.Match(line, @"\((.+)\)\s*$");
                if (note.Success)
                    character.CustomGoldNote = note.Groups[1].Value.Trim();
                return true;
            }
            return false;
        }

        private static void ApplyFeatureDetections(Character character, string featuresText)
        {
            if (string.IsNullOrWhiteSpace(featuresText))
                return;

            character.SelectedFeat = DetectFeat(featuresText);

            // Fighting styles
            character.FightingStyles ??= new List<string>();
            foreach (var style in ClassFeatureOptionData.AllFightingStyles)
            {
                if (featuresText.Contains(style.Name, StringComparison.OrdinalIgnoreCase) &&
                    !character.FightingStyles.Contains(style.Name, StringComparer.OrdinalIgnoreCase))
                    character.FightingStyles.Add(style.Name);
            }

            // Eldritch invocations
            character.EldritchInvocations ??= new List<string>();
            foreach (var inv in ClassFeatureOptionData.AllInvocations)
            {
                if (featuresText.Contains(inv.Name, StringComparison.OrdinalIgnoreCase) &&
                    !character.EldritchInvocations.Contains(inv.Name, StringComparer.OrdinalIgnoreCase))
                    character.EldritchInvocations.Add(inv.Name);
            }

            // Metamagic
            character.MetamagicOptions ??= new List<string>();
            foreach (var meta in ClassFeatureOptionData.AllMetamagic)
            {
                if (featuresText.Contains(meta.Name, StringComparison.OrdinalIgnoreCase) &&
                    !character.MetamagicOptions.Contains(meta.Name, StringComparer.OrdinalIgnoreCase))
                    character.MetamagicOptions.Add(meta.Name);
            }

            // Pact boon
            foreach (var boon in ClassFeatureOptionData.AllPactBoons)
            {
                if (featuresText.Contains(boon.Name, StringComparison.OrdinalIgnoreCase))
                {
                    character.WarlockPactBoon = boon.Name;
                    break;
                }
            }

            // Martial Adept maneuvers
            if (!string.IsNullOrWhiteSpace(character.SelectedFeat) &&
                character.SelectedFeat.Contains("Martial Adept", StringComparison.OrdinalIgnoreCase))
            {
                character.MartialAdeptManeuvers ??= new List<string>();
                foreach (var mv in ClassFeatureOptionData.AllManeuvers.OrderByDescending(m => m.Name.Length))
                {
                    if (character.MartialAdeptManeuvers.Count >= 2) break;
                    if (featuresText.Contains(mv.Name, StringComparison.OrdinalIgnoreCase) &&
                        !character.MartialAdeptManeuvers.Contains(mv.Name, StringComparer.OrdinalIgnoreCase))
                        character.MartialAdeptManeuvers.Add(mv.Name);
                }
            }

            // Strike of the Giants benefit
            if (!string.IsNullOrWhiteSpace(character.SelectedFeat) &&
                character.SelectedFeat.Contains("Strike of the Giants", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var gs in ClassFeatureOptionData.AllGiantStrikes.OrderByDescending(g => g.Name.Length))
                {
                    if (featuresText.Contains(gs.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        character.StrikeOfTheGiantsBenefit = gs.Name;
                        break;
                    }
                }
            }
        }

        private static string DetectFeat(string featuresText)
        {
            if (string.IsNullOrWhiteSpace(featuresText) || GameData.AllFeats == null)
                return "";

            foreach (var feat in GameData.AllFeats.OrderByDescending(f => f.Name.Length))
            {
                if (featuresText.Contains(feat.Name, StringComparison.OrdinalIgnoreCase))
                    return feat.Name;
            }

            return "";
        }

        // ───────────────────────── Spells (page 3 field names) ─────────────────────────

        private static void ParseSpellsFromSheet(
            Character character,
            Dictionary<string, string> byName,
            Dictionary<string, string> indexed)
        {
            character.Cantrips ??= new List<string>();
            character.Level1Spells ??= new List<string>();
            character.FeatSpells ??= new List<string>();
            var seenCantrip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenLeveled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenFeat = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Ingest(string rawDisplay, bool isCantripSlot)
            {
                if (string.IsNullOrWhiteSpace(rawDisplay)) return;
                if (Regex.IsMatch(rawDisplay.Trim(), @"^[+\-]?\d+$")) return;

                string tag = CharacterSheetExporter.GetSpellDisplayTag(rawDisplay) ?? "";
                string name = CharacterSheetExporter.StripSpellDisplayTag(rawDisplay);
                if (string.IsNullOrWhiteSpace(name)) return;

                // Resolve official casing / known spell
                var spell = GameData.FindSpell(name);
                if (spell != null)
                    name = spell.Name;
                else
                    name = name.Trim();

                bool isFeatTag = IsFeatTag(tag, character);
                bool isHighElf = tag.Contains("High Elf", StringComparison.OrdinalIgnoreCase) ||
                                 tag.Equals("Elf", StringComparison.OrdinalIgnoreCase);

                int level = spell?.Level ?? (isCantripSlot ? 0 : 1);

                if (isFeatTag)
                {
                    if (seenFeat.Add(name))
                        character.FeatSpells.Add(name);
                    if (string.IsNullOrWhiteSpace(character.FeatSpellSource) && !string.IsNullOrWhiteSpace(tag))
                        character.FeatSpellSource = tag;
                    if (string.IsNullOrWhiteSpace(character.SelectedFeat) && !string.IsNullOrWhiteSpace(tag))
                    {
                        // Prefer matching a real feat name
                        var feat = GameData.AllFeats?.FirstOrDefault(f =>
                            f.Name.Equals(tag, StringComparison.OrdinalIgnoreCase) ||
                            f.Name.Contains(tag, StringComparison.OrdinalIgnoreCase));
                        if (feat != null)
                            character.SelectedFeat = feat.Name;
                    }
                    return;
                }

                if (level == 0 || isCantripSlot)
                {
                    if (isHighElf && string.IsNullOrWhiteSpace(character.HighElfCantrip))
                        character.HighElfCantrip = name;
                    if (seenCantrip.Add(name))
                        character.Cantrips.Add(name);
                }
                else
                {
                    // Subclass always-prepared grants are also listed with tags;
                    // keep them in Level1Spells so the spell list is complete on reload.
                    if (seenLeveled.Add(name))
                        character.Level1Spells.Add(name);
                }
            }

            // Cantrip fields (official names)
            foreach (var field in CharacterSheetExporter.CantripFields)
                Ingest(Field(byName, field), isCantripSlot: true);

            // Leveled spell fields 1–9
            for (int lvl = 1; lvl <= 9; lvl++)
            {
                if (lvl >= CharacterSheetExporter.SpellFieldsByLevel.Length) break;
                foreach (var field in CharacterSheetExporter.SpellFieldsByLevel[lvl])
                    Ingest(Field(byName, field), isCantripSlot: false);
            }

            // Fallback: scan any "Spells ####" field not already covered
            foreach (var (name, value) in byName)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (!name.StartsWith("Spells ", StringComparison.OrdinalIgnoreCase)) continue;
                bool knownField =
                    CharacterSheetExporter.CantripFields.Any(f => f.Equals(name, StringComparison.OrdinalIgnoreCase)) ||
                    CharacterSheetExporter.SpellFieldsByLevel.Any(arr =>
                        arr.Any(f => f.Equals(name, StringComparison.OrdinalIgnoreCase)));
                if (knownField) continue;
                Ingest(value, isCantripSlot: false);
            }
        }

        private static bool IsFeatTag(string tag, Character character)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            if (!string.IsNullOrWhiteSpace(character.FeatSpellSource) &&
                tag.Equals(character.FeatSpellSource, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(character.SelectedFeat) &&
                tag.Equals(character.SelectedFeat, StringComparison.OrdinalIgnoreCase))
                return true;
            if (GameData.AllFeats == null) return false;
            return GameData.AllFeats.Any(f =>
                f.Name.Equals(tag, StringComparison.OrdinalIgnoreCase) ||
                f.Name.Contains(tag, StringComparison.OrdinalIgnoreCase) ||
                tag.Contains(f.Name, StringComparison.OrdinalIgnoreCase));
        }

        private static string? ExtractCharacterNameFallback(List<(string Name, string Value)> formFields)
        {
            foreach (var (fieldName, fieldValue) in formFields)
            {
                if (string.IsNullOrWhiteSpace(fieldValue)) continue;

                if (fieldName.Contains("CharacterName", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("Character Name", StringComparison.OrdinalIgnoreCase))
                {
                    return fieldValue.Trim();
                }
            }

            return null;
        }
    }
}
