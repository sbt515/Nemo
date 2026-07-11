using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace Nemo
{
    /// <summary>
    /// Imports a D&amp;D 5e fillable character sheet PDF into Nemo's <see cref="Character"/> model.
    /// Field indices match the standard multi-page fillable sheet layout used by CharacterSheetImporter
    /// (positional AcroForm field order via PdfPig).
    /// Nemo currently supports level 1 fully — higher-level data is still read, but only cantrips
    /// and 1st-level spells are imported.
    /// </summary>
    public static class CharacterSheetImporter
    {
        /// <summary>
        /// Import a character from a fillable PDF. Returns the character and an optional user-facing note.
        /// </summary>
        public static (Character Character, string? Note) ImportFromPdf(string pdfPath)
        {
            var indexed = ExtractIndexedFormFields(pdfPath);
            var formFields = ExtractFormFields(pdfPath);
            var character = BuildCharacter(indexed, formFields, pdfPath);

            string? note = null;
            int level = GetCharacterLevel(Get(indexed, "ClassAndLevel"));
            if (level > 1)
            {
                note = $"This sheet is level {level}. Nemo currently supports level 1 only — " +
                       "core identity, ability scores, skills, equipment, cantrips, and 1st-level spells were imported. " +
                       "Higher-level features and spell slots were ignored.";
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

                        // Keep empty fields so indices stay stable
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
        /// Semantic field map for this specific PDF layout (positional indices).
        /// </summary>
        public static Dictionary<string, string> ExtractIndexedFormFields(string pdfPath)
        {
            var indexed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var fields = ExtractFormFields(pdfPath);

            string GetAt(int index) =>
                (index >= 0 && index < fields.Count) ? fields[index].Value : "";

            // === Character & Core Info ===
            indexed["CharacterName"] = GetAt(17);

            // === Ability Scores ===
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

            // === Saving Throws ===
            indexed["StrengthSave"] = GetAt(30);
            indexed["DexteritySave"] = GetAt(49);
            indexed["ConstitutionSave"] = GetAt(50);
            indexed["IntelligenceSave"] = GetAt(51);
            indexed["WisdomSave"] = GetAt(52);
            indexed["CharismaSave"] = GetAt(53);

            // === Ability Checks / Skills ===
            indexed["Acrobatics"] = GetAt(54);
            indexed["AnimalHandling"] = GetAt(55);
            indexed["Arcana"] = GetAt(78);
            indexed["Athletics"] = GetAt(56);
            indexed["Deception"] = GetAt(57);
            indexed["History"] = GetAt(58);
            indexed["Insight"] = GetAt(59);
            indexed["Intimidation"] = GetAt(60);
            indexed["Investigation"] = GetAt(74);
            indexed["Medicine"] = GetAt(81);
            indexed["Nature"] = GetAt(83);
            indexed["Perception"] = GetAt(80);
            indexed["Performance"] = GetAt(84);
            indexed["Persuasion"] = GetAt(106);
            indexed["Religion"] = GetAt(86);
            indexed["SleightOfHand"] = GetAt(107);
            indexed["Stealth"] = GetAt(87);
            indexed["Survival"] = GetAt(109);

            // === Misc ===
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
            indexed["PassivePerception"] = GetAt(111); // "Passive" on this template

            // === Open text areas ===
            indexed["ExtraWeaponSpellDetails"] = GetAt(110);
            indexed["ExtraProficiencesLanguages"] = GetAt(113);
            indexed["Equipment"] = GetAt(118);
            indexed["FeaturesAndTraits"] = GetAt(119);
            indexed["AlliesAndOrgs"] = GetAt(7);
            indexed["Backstory"] = GetAt(9);
            indexed["AdditionalFeaturesAndTraits"] = GetAt(10);
            indexed["Treasure"] = GetAt(11);

            // === Attacks ===
            indexed["WeaponSpell1Name"] = GetAt(67);
            indexed["WeaponSpell1AttackBonus"] = GetAt(68);
            indexed["WeaponSpell1Damage"] = GetAt(69);
            indexed["WeaponSpell2Name"] = GetAt(71);
            indexed["WeaponSpell2AttackBonus"] = GetAt(72);
            indexed["WeaponSpell2Damage"] = GetAt(73);
            indexed["WeaponSpell3Name"] = GetAt(76);
            indexed["WeaponSpell3AttackBonus"] = GetAt(77);
            indexed["WeaponSpell3Damage"] = GetAt(79);

            // === Magic ===
            indexed["SpellcastingAbility"] = GetAt(121);
            indexed["SpellSaveDC"] = GetAt(122);
            indexed["SpellAttackBonus"] = GetAt(123);

            // Spell slots (read for completeness; level-1 app does not use slots 2–9)
            indexed["Level1SlotsTotal"] = GetAt(124);
            indexed["Level1SlotsExpended"] = GetAt(125);

            // Name-based overrides when the sheet uses standard field names (or Nemo fillable export)
            ApplyNameBasedOverrides(fields, indexed);

            return indexed;
        }

        /// <summary>
        /// If positional indices look empty / wrong, fill from known field names.
        /// Also fills any missing key when a matching named field has a value.
        /// </summary>
        private static void ApplyNameBasedOverrides(
            List<(string Name, string Value)> fields,
            Dictionary<string, string> indexed)
        {
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in fields)
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                    continue;
                // First non-empty wins (stable). Index both raw and trimmed names —
                // this PDF uses trailing spaces on several field names (e.g. "Race ").
                if (!byName.ContainsKey(name))
                    byName[name] = value.Trim();
                string trimmed = name.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !byName.ContainsKey(trimmed))
                    byName[trimmed] = value.Trim();
            }

            void Prefer(string key, params string[] fieldNames)
            {
                if (indexed.TryGetValue(key, out var existing) && !string.IsNullOrWhiteSpace(existing))
                    return;

                foreach (var fn in fieldNames)
                {
                    if (byName.TryGetValue(fn, out var v) && !string.IsNullOrWhiteSpace(v))
                    {
                        indexed[key] = v;
                        return;
                    }
                    // also try trimmed lookup
                    if (byName.TryGetValue(fn.Trim(), out v) && !string.IsNullOrWhiteSpace(v))
                    {
                        indexed[key] = v;
                        return;
                    }
                }
            }

            Prefer("CharacterName", "CharacterName", "Character Name");
            Prefer("PlayerName", "PlayerName", "Player Name");
            Prefer("ClassAndLevel", "ClassLevel", "ClassAndLevel", "Class Level", "Class");
            Prefer("Background", "Background");
            Prefer("Race", "Race");
            Prefer("Alignment", "Alignment");
            Prefer("ArmorClass", "AC", "ArmorClass", "Armor Class");
            Prefer("ProficiencyBonus", "ProficiencyBonus", "ProfBonus");
            Prefer("InitiativeBonus", "Initiative", "InitiativeBonus");
            Prefer("Speed", "Speed");
            Prefer("MaxHitPoints", "HPMax", "MaxHitPoints", "HitPoints", "HP");
            Prefer("CurrentHitPoints", "HPCurrent", "CurrentHitPoints");
            Prefer("Equipment", "Equipment", "EquipmentText");
            Prefer("FeaturesAndTraits", "Features and Traits", "FeaturesAndTraits", "FeaturesTraits");
            Prefer("SpellcastingAbility", "SpellcastingAbility", "SpellAbility");
            Prefer("SpellSaveDC", "SpellSaveDC", "SpellDC");
            Prefer("SpellAttackBonus", "SpellAttackBonus", "SpellAttack");

            Prefer("StrengthScore", "STR", "Strength", "StrengthScore");
            Prefer("DexterityScore", "DEX", "Dexterity", "DexterityScore");
            Prefer("ConstitutionScore", "CON", "Constitution", "ConstitutionScore");
            Prefer("IntelligenceScore", "INT", "Intelligence", "IntelligenceScore");
            Prefer("WisdomScore", "WIS", "Wisdom", "WisdomScore");
            Prefer("CharismaScore", "CHA", "Charisma", "CharismaScore");

            Prefer("StrengthBonus", "STRmod", "StrengthBonus", "StrengthModifier");
            Prefer("DexterityBonus", "DEXmod", "DEXmod ", "DexterityBonus", "DexterityModifier");
            Prefer("ConstitutionBonus", "CONmod", "ConstitutionBonus", "ConstitutionModifier");
            Prefer("IntelligenceBonus", "INTmod", "IntelligenceBonus", "IntelligenceModifier");
            Prefer("WisdomBonus", "WISmod", "WisdomBonus", "WisdomModifier");
            Prefer("CharismaBonus", "CHamod", "CHAmod", "CharismaBonus", "CharismaModifier");

            // Skill bonuses (Nemo fillable export uses "{Skill}Bonus")
            Prefer("Acrobatics", "Acrobatics", "AcrobaticsBonus");
            Prefer("AnimalHandling", "Animal Handling", "AnimalHandling", "Animal HandlingBonus", "AnimalHandlingBonus");
            Prefer("Arcana", "Arcana", "ArcanaBonus");
            Prefer("Athletics", "Athletics", "AthleticsBonus");
            Prefer("Deception", "Deception", "DeceptionBonus");
            Prefer("History", "History", "HistoryBonus");
            Prefer("Insight", "Insight", "InsightBonus");
            Prefer("Intimidation", "Intimidation", "IntimidationBonus");
            Prefer("Investigation", "Investigation", "InvestigationBonus");
            Prefer("Medicine", "Medicine", "MedicineBonus");
            Prefer("Nature", "Nature", "NatureBonus");
            Prefer("Perception", "Perception", "PerceptionBonus");
            Prefer("Performance", "Performance", "PerformanceBonus");
            Prefer("Persuasion", "Persuasion", "PersuasionBonus");
            Prefer("Religion", "Religion", "ReligionBonus");
            Prefer("SleightOfHand", "Sleight of Hand", "SleightOfHand", "Sleight of HandBonus", "SleightOfHandBonus");
            Prefer("Stealth", "Stealth", "StealthBonus");
            Prefer("Survival", "Survival", "SurvivalBonus");

            Prefer("StrengthSave", "StrengthSave", "STRsave");
            Prefer("DexteritySave", "DexteritySave", "DEXsave");
            Prefer("ConstitutionSave", "ConstitutionSave", "CONsave");
            Prefer("IntelligenceSave", "IntelligenceSave", "INTsave");
            Prefer("WisdomSave", "WisdomSave", "WISsave");
            Prefer("CharismaSave", "CharismaSave", "CHAsave");

            Prefer("WeaponSpell1Name", "Wpn Name", "WpnName", "WeaponSpell1Name");
            Prefer("WeaponSpell1AttackBonus", "Wpn1 AtkBonus", "Wpn1AtkBonus");
            Prefer("WeaponSpell1Damage", "Wpn1 Damage", "Wpn1Damage");
            Prefer("WeaponSpell2Name", "Wpn Name 2", "WpnName2");
            Prefer("WeaponSpell2AttackBonus", "Wpn2 AtkBonus", "Wpn2AtkBonus");
            Prefer("WeaponSpell2Damage", "Wpn2 Damage", "Wpn2Damage");
            Prefer("WeaponSpell3Name", "Wpn Name 3", "WpnName3");
            Prefer("WeaponSpell3AttackBonus", "Wpn3 AtkBonus", "Wpn3AtkBonus");
            Prefer("WeaponSpell3Damage", "Wpn3 Damage", "Wpn3Damage");
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
                // Information sometimes ends with a period after the name
                return rest;
            }

            return "";
        }

        private static string Get(Dictionary<string, string> indexed, string key) =>
            indexed.TryGetValue(key, out var v) ? (v ?? "") : "";

        private static int ParseInt(string? raw, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            // Strip trailing units like "ft", " feet"
            string cleaned = Regex.Replace(raw.Trim(), @"[^\d+\-]", "");
            if (cleaned.Length == 0) return fallback;
            return int.TryParse(cleaned, out int n) ? n : fallback;
        }

        private static int ParseBonus(string? raw, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            string cleaned = raw.Trim().Replace("+", "");
            // Keep leading minus if present
            cleaned = Regex.Match(cleaned, @"-?\d+").Value;
            return int.TryParse(cleaned, out int n) ? n : fallback;
        }

        // ───────────────────────── Character building ─────────────────────────

        private static Character BuildCharacter(
            Dictionary<string, string> indexed,
            List<(string Name, string Value)> formFields,
            string pdfPath)
        {
            var character = new Character();

            character.Name = Get(indexed, "CharacterName");
            if (string.IsNullOrWhiteSpace(character.Name))
                character.Name = ExtractCharacterNameFallback(formFields) ?? "Imported Character";

            character.PlayerName = Get(indexed, "PlayerName");

            // Race / subrace
            string rawRace = Get(indexed, "Race");
            var (race, subrace) = ParseRace(rawRace);
            character.Race = race;
            character.Subrace = subrace;

            // Class / subclass / level
            string classAndLevel = Get(indexed, "ClassAndLevel");
            var (cls, subclass, _) = ParseClassAndLevel(classAndLevel);
            character.Class = cls;
            character.Subclass = subclass;

            character.Background = MatchBackground(Get(indexed, "Background"));

            // Ability scores — store PDF totals as Final; Base is filled as Final for load
            // (UI restore will reverse-engineer base after race bonuses apply when possible)
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

            character.ProficiencyBonus = Math.Max(2, ParseBonus(Get(indexed, "ProficiencyBonus"), 2));
            character.Initiative = ParseBonus(Get(indexed, "InitiativeBonus"),
                character.AbilityScores.Dexterity.Modifier);
            character.ArmorClass = ParseInt(Get(indexed, "ArmorClass"), 10);
            character.EquippedACDisplay = Get(indexed, "ArmorClass");
            character.HitPoints = ParseInt(Get(indexed, "MaxHitPoints"),
                ParseInt(Get(indexed, "CurrentHitPoints"), 0));
            character.Speed = ParseSpeed(Get(indexed, "Speed"));

            // Spellcasting
            character.SpellcastingAbility = NormalizeSpellAbility(Get(indexed, "SpellcastingAbility"));
            character.SpellSaveDC = ParseInt(Get(indexed, "SpellSaveDC"), 0);
            character.SpellAttackBonus = ParseBonus(Get(indexed, "SpellAttackBonus"), 0);

            // Skills — proficient when bonus exceeds bare ability mod by ~proficiency
            character.Skills = ParseSkills(indexed, character);

            // Saving throws
            character.SavingThrows = ParseSavingThrows(indexed, character);

            // Equipment + weapon slots
            character.Equipment = ParseEquipment(indexed);

            // Feat guess from features text
            character.SelectedFeat = DetectFeat(
                Get(indexed, "FeaturesAndTraits") + "\n" + Get(indexed, "AdditionalFeaturesAndTraits"));

            // Spells / cantrips from high-index form fields (level 1 only)
            var (cantrips, level1) = ParseSpellsAndCantrips(formFields, indexed);
            character.Cantrips = cantrips;
            character.Level1Spells = level1;

            return character;
        }

        private static AbilityScore BuildAbility(string scoreRaw, string bonusRaw)
        {
            int final = ParseInt(scoreRaw, 10);
            if (final < 1) final = 10;
            int mod = ParseBonus(bonusRaw, CalculateModifier(final));
            return new AbilityScore
            {
                Base = final,   // temporarily; UI may adjust after racial apply
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
                _ => raw.Trim()
            };
        }

        // ───────────────────────── Race / class / background matching ─────────────────────────

        private static (string Race, string Subrace) ParseRace(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return ("", "");

            string text = raw.Trim();

            // Exact race match
            foreach (var raceKey in GameData.RaceData.Keys.OrderByDescending(k => k.Length))
            {
                if (text.Equals(raceKey, StringComparison.OrdinalIgnoreCase))
                    return (raceKey, "");
            }

            // Subrace contains full name like "Lightfoot Halfling" or "High Elf"
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

            // Race name appears inside text (e.g. "Hill Dwarf", "Wood Elf")
            foreach (var raceKey in GameData.RaceData.Keys.OrderByDescending(k => k.Length))
            {
                if (text.Contains(raceKey, StringComparison.OrdinalIgnoreCase))
                {
                    string sub = "";
                    if (GameData.RaceSubraces.TryGetValue(raceKey, out var subs))
                    {
                        foreach (var s in subs)
                        {
                            // Match "Hill" from "Hill Dwarf", etc.
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

            // Half-Elf / Half-Orc variants of wording
            if (text.Contains("half", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("elf", StringComparison.OrdinalIgnoreCase))
                return ("Half-Elf", "");
            if (text.Contains("half", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("orc", StringComparison.OrdinalIgnoreCase))
                return ("Half-Orc", "");

            return (text, ""); // keep raw so user can see it
        }

        /// <summary>
        /// Parses strings like "War Cleric 9", "Fighter 1", "Sorcerer 3 / Warlock 2".
        /// For multiclass, uses the first class entry. Subclass is best-effort.
        /// </summary>
        public static (string Class, string Subclass, int Level) ParseClassAndLevel(string classAndLevel)
        {
            if (string.IsNullOrWhiteSpace(classAndLevel))
                return ("", "", 1);

            int totalLevel = GetCharacterLevel(classAndLevel);

            // Use first segment for multiclass
            string primary = classAndLevel.Split(new[] { '/', '|' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

            // Strip trailing level number(s)
            primary = Regex.Replace(primary, @"\s+\d+\s*$", "").Trim();

            string matchedClass = "";
            string matchedSubclass = "";

            // Prefer longest class name match
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
                // Remainder might be subclass, e.g. "War Cleric" → "War"
                string remainder = primary
                    .Replace(matchedClass, "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (!string.IsNullOrEmpty(remainder) && classData.Subclasses != null)
                {
                    foreach (var sub in classData.Subclasses.OrderByDescending(s => s.Length))
                    {
                        if (remainder.Contains(sub, StringComparison.OrdinalIgnoreCase) ||
                            sub.Contains(remainder, StringComparison.OrdinalIgnoreCase) ||
                            // "War" matches "War Domain" style names in some sheets
                            sub.StartsWith(remainder, StringComparison.OrdinalIgnoreCase) ||
                            remainder.Split(' ').Any(w =>
                                w.Length >= 3 && sub.Contains(w, StringComparison.OrdinalIgnoreCase)))
                        {
                            matchedSubclass = sub;
                            break;
                        }
                    }

                    // Cleric domains / Warlock patrons may also be in dedicated dictionaries
                    if (string.IsNullOrEmpty(matchedSubclass) && matchedClass == "Cleric")
                    {
                        foreach (var domain in GameData.ClericSubclasses.Keys)
                        {
                            if (remainder.Contains(domain, StringComparison.OrdinalIgnoreCase) ||
                                domain.Contains(remainder, StringComparison.OrdinalIgnoreCase))
                            {
                                matchedSubclass = domain;
                                break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(matchedSubclass) && matchedClass == "Warlock")
                    {
                        foreach (var patron in GameData.WarlockSubclasses.Keys)
                        {
                            if (remainder.Contains(patron, StringComparison.OrdinalIgnoreCase) ||
                                patron.Contains(remainder, StringComparison.OrdinalIgnoreCase))
                            {
                                matchedSubclass = patron;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                // Unknown class — keep cleaned primary text without level
                matchedClass = primary;
            }

            return (matchedClass, matchedSubclass, totalLevel);
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

            return totalLevel > 0 ? totalLevel : 1;
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

        private static readonly Dictionary<string, (string Display, string Ability)> SkillMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Acrobatics"] = ("Acrobatics", "Dex"),
            ["AnimalHandling"] = ("Animal Handling", "Wis"),
            ["Arcana"] = ("Arcana", "Int"),
            ["Athletics"] = ("Athletics", "Str"),
            ["Deception"] = ("Deception", "Cha"),
            ["History"] = ("History", "Int"),
            ["Insight"] = ("Insight", "Wis"),
            ["Intimidation"] = ("Intimidation", "Cha"),
            ["Investigation"] = ("Investigation", "Int"),
            ["Medicine"] = ("Medicine", "Wis"),
            ["Nature"] = ("Nature", "Int"),
            ["Perception"] = ("Perception", "Wis"),
            ["Performance"] = ("Performance", "Cha"),
            ["Persuasion"] = ("Persuasion", "Cha"),
            ["Religion"] = ("Religion", "Int"),
            ["SleightOfHand"] = ("Sleight of Hand", "Dex"),
            ["Stealth"] = ("Stealth", "Dex"),
            ["Survival"] = ("Survival", "Wis"),
        };

        private static List<SkillEntry> ParseSkills(Dictionary<string, string> indexed, Character character)
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

            foreach (var (key, (display, ability)) in SkillMap)
            {
                string raw = Get(indexed, key);
                if (string.IsNullOrWhiteSpace(raw)) continue;

                int bonus = ParseBonus(raw, int.MinValue);
                if (bonus == int.MinValue) continue;

                int abilityMod = AbilityMod(ability);
                // Proficient if listed bonus is at least ability + prof - 1 (tolerate off-by-one sheets)
                bool proficient = bonus >= abilityMod + Math.Max(1, prof - 1);

                if (proficient)
                {
                    skills.Add(new SkillEntry
                    {
                        Name = display,
                        Ability = ability,
                        IsProficient = true,
                        Bonus = bonus
                    });
                }
            }

            return skills;
        }

        private static List<SavingThrow> ParseSavingThrows(Dictionary<string, string> indexed, Character character)
        {
            var result = new List<SavingThrow>();
            string[] abilities = { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
            int prof = character.ProficiencyBonus;

            foreach (string ability in abilities)
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

                string raw = Get(indexed, ability + "Save");
                int bonus = string.IsNullOrWhiteSpace(raw) ? abilityMod : ParseBonus(raw, abilityMod);
                bool proficient = bonus >= abilityMod + Math.Max(1, prof - 1);

                result.Add(new SavingThrow
                {
                    Name = ability,
                    Bonus = bonus,
                    IsProficient = proficient
                });
            }

            return result;
        }

        // ───────────────────────── Equipment ─────────────────────────

        private static List<string> ParseEquipment(Dictionary<string, string> indexed)
        {
            var items = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string? item)
            {
                if (string.IsNullOrWhiteSpace(item)) return;
                string cleaned = item.Trim().TrimStart('•', '-', '*', '·').Trim();
                if (cleaned.Length == 0 || !seen.Add(cleaned)) return;
                items.Add(cleaned);
            }

            // Weapon / spell attack slots
            foreach (var key in new[] { "WeaponSpell1Name", "WeaponSpell2Name", "WeaponSpell3Name" })
            {
                string name = Get(indexed, key);
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Skip pure spell names that look like known cantrips/spells (weapons only prefer)
                bool looksLikeSpell = GameData.AllCantrips.Any(c =>
                                          c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ||
                                      GameData.All1stLevelSpells.Any(s =>
                                          s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (!looksLikeSpell)
                    Add(name);
            }

            // Free-text equipment block
            string equipmentText = Get(indexed, "Equipment");
            if (!string.IsNullOrWhiteSpace(equipmentText))
            {
                // Split on newlines, commas, or bullets
                var parts = Regex.Split(equipmentText, @"[\r\n,;•·|]+")
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 1);

                foreach (var part in parts)
                    Add(part);
            }

            return items;
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

        // ───────────────────────── Spells (cantrips + 1st only) ─────────────────────────

        private static (List<string> Cantrips, List<string> Level1) ParseSpellsAndCantrips(
            List<(string Name, string Value)> formFields,
            Dictionary<string, string> indexed)
        {
            var cantrips = new List<string>();
            var level1 = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var cantripNames = new HashSet<string>(
                GameData.AllCantrips.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
            var level1Names = new HashSet<string>(
                GameData.All1stLevelSpells.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);

            // Values that are slot totals / DCs etc. should be skipped
            var skipValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in new[]
                     {
                         "Level1SlotsTotal", "Level1SlotsExpended",
                         "SpellcastingAbility", "SpellSaveDC", "SpellAttackBonus",
                         "CharacterName", "PlayerName", "ClassAndLevel", "Race", "Background"
                     })
            {
                string v = Get(indexed, key);
                if (!string.IsNullOrWhiteSpace(v))
                    skipValues.Add(v);
            }

            // High-index spell list fields start around 126 on this sheet layout
            int start = Math.Min(126, formFields.Count);
            for (int i = start; i < formFields.Count; i++)
            {
                string value = formFields[i].Value;
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (skipValues.Contains(value)) continue;
                if (Regex.IsMatch(value.Trim(), @"^[+\-]?\d+$")) continue; // pure numbers

                TryAddSpell(value.Trim(), cantripNames, level1Names, cantrips, level1, seen);
            }

            // Also scan weapon/spell name slots and free-text areas for known spells
            foreach (var key in new[]
                     {
                         "WeaponSpell1Name", "WeaponSpell2Name", "WeaponSpell3Name",
                         "ExtraWeaponSpellDetails", "FeaturesAndTraits", "AdditionalFeaturesAndTraits"
                     })
            {
                string text = Get(indexed, key);
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Whole value as a spell name
                TryAddSpell(text.Trim(), cantripNames, level1Names, cantrips, level1, seen);

                // Scan known spell names inside longer text
                foreach (var name in cantripNames.Concat(level1Names).OrderByDescending(n => n.Length))
                {
                    if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
                        TryAddSpell(name, cantripNames, level1Names, cantrips, level1, seen);
                }
            }

            return (cantrips, level1);
        }

        private static void TryAddSpell(
            string value,
            HashSet<string> cantripNames,
            HashSet<string> level1Names,
            List<string> cantrips,
            List<string> level1,
            HashSet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                return;

            // Exact / case-insensitive match against known lists; normalize to official casing
            string? cantrip = GameData.AllCantrips
                .FirstOrDefault(c => c.Name.Equals(value, StringComparison.OrdinalIgnoreCase))?.Name;
            if (cantrip != null)
            {
                cantrips.Add(cantrip);
                return;
            }

            string? spell = GameData.All1stLevelSpells
                .FirstOrDefault(s => s.Name.Equals(value, StringComparison.OrdinalIgnoreCase))?.Name;
            if (spell != null)
            {
                level1.Add(spell);
            }
            // Higher-level spells intentionally ignored (level-1 support only)
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
