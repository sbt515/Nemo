using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Annot;
using iTextRectangle = iText.Kernel.Geom.Rectangle;

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
        /// Page 3 spell-list field names, in visual top-to-bottom order for the official sheet.
        /// Geometry on the template places <c>Spells 1015</c> under 1st-level (not cantrips).
        /// </summary>
        private static readonly string[] CantripFields =
        {
            "Spells 1014", "Spells 1016", "Spells 1017", "Spells 1018",
            "Spells 1019", "Spells 1020", "Spells 1021", "Spells 1022"
        };

        private static readonly string[] Level1SpellFields =
        {
            "Spells 1015",
            "Spells 1023", "Spells 1024", "Spells 1025", "Spells 1026", "Spells 1027",
            "Spells 1028", "Spells 1029", "Spells 1030", "Spells 1031", "Spells 1032", "Spells 1033"
        };

        /// <summary>Prepared checkboxes aligned with <see cref="Level1SpellFields"/> (null = no box on that row).</summary>
        private static readonly string?[] Level1PreparedChecks =
        {
            null, // Spells 1015 has no prep checkbox on this template
            "Check Box 309", "Check Box 3010", "Check Box 3011", "Check Box 3012", "Check Box 3013",
            "Check Box 3014", "Check Box 3015", "Check Box 3016", "Check Box 3017", "Check Box 3018", "Check Box 3019"
        };

        private static readonly string[] Level2SpellFields =
        {
            "Spells 1046",
            "Spells 1034", "Spells 1035", "Spells 1036", "Spells 1037", "Spells 1038",
            "Spells 1039", "Spells 1040", "Spells 1041", "Spells 1042", "Spells 1043",
            "Spells 1044", "Spells 1045"
        };

        private static readonly string?[] Level2PreparedChecks =
        {
            "Check Box 313", "Check Box 310", "Check Box 3020", "Check Box 3021", "Check Box 3022",
            "Check Box 3023", "Check Box 3024", "Check Box 3025", "Check Box 3026", "Check Box 3027",
            "Check Box 3028", "Check Box 3029", "Check Box 3030"
        };

        // ── Levels 3–9 (middle + right columns on page 3; visual top→bottom order) ──

        private static readonly string[] Level3SpellFields =
        {
            "Spells 1048", "Spells 1047", "Spells 1049", "Spells 1050", "Spells 1051",
            "Spells 1052", "Spells 1053", "Spells 1054", "Spells 1055", "Spells 1056",
            "Spells 1057", "Spells 1058", "Spells 1059"
        };

        private static readonly string?[] Level3PreparedChecks =
        {
            "Check Box 315", "Check Box 314", "Check Box 3031", "Check Box 3032", "Check Box 3033",
            "Check Box 3034", "Check Box 3035", "Check Box 3036", "Check Box 3037", "Check Box 3038",
            "Check Box 3039", "Check Box 3040", "Check Box 3041"
        };

        private static readonly string[] Level4SpellFields =
        {
            "Spells 1061", "Spells 1060", "Spells 1062", "Spells 1063", "Spells 1064",
            "Spells 1065", "Spells 1066", "Spells 1067", "Spells 1068", "Spells 1069",
            "Spells 1070", "Spells 1071", "Spells 1072"
        };

        private static readonly string?[] Level4PreparedChecks =
        {
            "Check Box 317", "Check Box 316", "Check Box 3042", "Check Box 3043", "Check Box 3044",
            "Check Box 3045", "Check Box 3046", "Check Box 3047", "Check Box 3048", "Check Box 3049",
            "Check Box 3050", "Check Box 3051", "Check Box 3052"
        };

        private static readonly string[] Level5SpellFields =
        {
            "Spells 1074", "Spells 1073", "Spells 1075", "Spells 1076", "Spells 1077",
            "Spells 1078", "Spells 1079", "Spells 1080", "Spells 1081"
        };

        private static readonly string?[] Level5PreparedChecks =
        {
            "Check Box 319", "Check Box 318", "Check Box 3053", "Check Box 3054", "Check Box 3055",
            "Check Box 3056", "Check Box 3057", "Check Box 3058", "Check Box 3059"
        };

        private static readonly string[] Level6SpellFields =
        {
            "Spells 1083", "Spells 1082", "Spells 1084", "Spells 1085", "Spells 1086",
            "Spells 1087", "Spells 1088", "Spells 1089", "Spells 1090"
        };

        private static readonly string?[] Level6PreparedChecks =
        {
            "Check Box 321", "Check Box 320", "Check Box 3060", "Check Box 3061", "Check Box 3062",
            "Check Box 3063", "Check Box 3064", "Check Box 3065", "Check Box 3066"
        };

        private static readonly string[] Level7SpellFields =
        {
            "Spells 1092", "Spells 1091", "Spells 1093", "Spells 1094", "Spells 1095",
            "Spells 1096", "Spells 1097", "Spells 1098", "Spells 1099"
        };

        private static readonly string?[] Level7PreparedChecks =
        {
            "Check Box 323", "Check Box 322", "Check Box 3067", "Check Box 3068", "Check Box 3069",
            "Check Box 3070", "Check Box 3071", "Check Box 3072", "Check Box 3073"
        };

        private static readonly string[] Level8SpellFields =
        {
            "Spells 10101", "Spells 10100", "Spells 10102", "Spells 10103",
            "Spells 10104", "Spells 10105", "Spells 10106"
        };

        private static readonly string?[] Level8PreparedChecks =
        {
            "Check Box 325", "Check Box 324", "Check Box 3074", "Check Box 3075",
            "Check Box 3076", "Check Box 3077", "Check Box 3078"
        };

        private static readonly string[] Level9SpellFields =
        {
            "Spells 10108", "Spells 10107", "Spells 10109", "Spells 101010",
            "Spells 101011", "Spells 101012", "Spells 101013"
        };

        private static readonly string?[] Level9PreparedChecks =
        {
            "Check Box 327", "Check Box 326", "Check Box 3079", "Check Box 3080",
            "Check Box 3081", "Check Box 3082", "Check Box 3083"
        };

        /// <summary>
        /// Spell name fields per spell level (index 1–9). Top-to-bottom visual order on page 3.
        /// </summary>
        private static readonly string[][] SpellFieldsByLevel =
        {
            Array.Empty<string>(), // 0 = cantrips handled separately
            Level1SpellFields,
            Level2SpellFields,
            Level3SpellFields,
            Level4SpellFields,
            Level5SpellFields,
            Level6SpellFields,
            Level7SpellFields,
            Level8SpellFields,
            Level9SpellFields
        };

        /// <summary>
        /// Prepared checkboxes aligned with <see cref="SpellFieldsByLevel"/> (index 1–9).
        /// </summary>
        private static readonly string?[][] PreparedChecksByLevel =
        {
            Array.Empty<string?>(),
            Level1PreparedChecks,
            Level2PreparedChecks,
            Level3PreparedChecks,
            Level4PreparedChecks,
            Level5PreparedChecks,
            Level6PreparedChecks,
            Level7PreparedChecks,
            Level8PreparedChecks,
            Level9PreparedChecks
        };

        /// <summary>
        /// Spell-slot total/remaining field pairs by spell level (1–9).
        /// Template names: SlotsTotal/Remaining 19 = 1st … 27 = 9th.
        /// </summary>
        private static readonly (string Total, string Remaining)[] SlotFieldsByLevel =
        {
            default!, // index 0 unused
            ("SlotsTotal 19", "SlotsRemaining 19"), // 1st
            ("SlotsTotal 20", "SlotsRemaining 20"), // 2nd
            ("SlotsTotal 21", "SlotsRemaining 21"), // 3rd
            ("SlotsTotal 22", "SlotsRemaining 22"), // 4th
            ("SlotsTotal 23", "SlotsRemaining 23"), // 5th
            ("SlotsTotal 24", "SlotsRemaining 24"), // 6th
            ("SlotsTotal 25", "SlotsRemaining 25"), // 7th
            ("SlotsTotal 26", "SlotsRemaining 26"), // 8th
            ("SlotsTotal 27", "SlotsRemaining 27"), // 9th
        };

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
            /// <summary>Legacy override for 1st-level slots only. Prefer full calculation when null.</summary>
            public int? Level1SpellSlots { get; set; }
            /// <summary>
            /// Optional override: slots per spell level (indices 1–9). When null, derived via
            /// <see cref="SpellSlotCalculator"/> from the character's class levels.
            /// </summary>
            public int[]? SpellSlotsByLevel { get; set; }
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

            // Overlay clickable wiki links on spell name fields (value stays the short display name).
            ApplySpellHyperlinks(pdf, fields, values.SpellHyperlinks);

            // Keep the form editable for the player
            // form.FlattenFields(); // intentionally NOT flattened

            pdf.Close();
        }

        private sealed class FieldPayload
        {
            public Dictionary<string, string> TextFields { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, bool> Checkboxes { get; } = new(StringComparer.Ordinal);
            /// <summary>Form field name → wikidot URL for overlay link annotations.</summary>
            public Dictionary<string, string> SpellHyperlinks { get; } = new(StringComparer.Ordinal);
        }

        /// <summary>One spell line to write into a page-3 spell field.</summary>
        private sealed class SpellExportLine
        {
            public string SpellName { get; init; } = "";
            public string DisplayText { get; init; } = "";
            public bool IsPrepared { get; init; }
            public string? SubclassTag { get; init; }
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
            FillSpellcastingPage(c, extras, payload, T, C);

            return payload;
        }

        /// <summary>
        /// Populates page-3 spellcasting class header, cantrips, leveled spell lists (subclass-first),
        /// spell slots, and records wiki URLs for hyperlink overlays.
        /// </summary>
        private static void FillSpellcastingPage(
            Character c,
            ExportExtras extras,
            FieldPayload payload,
            Action<string, string?> T,
            Action<string, bool> C)
        {
            var classLevels = LevelUpCalculator.GetClassLevelsFromCharacter(c);
            bool hasSpellContent =
                !string.IsNullOrWhiteSpace(c.SpellcastingAbility) ||
                (c.Cantrips?.Count > 0) ||
                (c.Level1Spells?.Count > 0) ||
                classLevels.Any(e =>
                    SpellSlotCalculator.GetProgressionKind(e.ClassName, e.Subclass) !=
                    CasterProgressionKind.None);

            if (!hasSpellContent)
                return;

            // Header: class (subclass) — multiclass lists all casters briefly
            string spellClass = BuildSpellcastingClassLabel(c, classLevels);
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
            {
                string atk = c.SpellAttackBonus >= 0
                    ? $"+{c.SpellAttackBonus}"
                    : c.SpellAttackBonus.ToString();
                T("SpellAtkBonus 2", atk);
            }

            // Spell slots (1st–9th)
            int[] slotsByLevel = extras.SpellSlotsByLevel ?? DeriveSpellSlotsByLevel(c, extras);
            for (int lvl = 1; lvl <= 9; lvl++)
            {
                int n = lvl < slotsByLevel.Length ? Math.Max(0, slotsByLevel[lvl]) : 0;
                if (n <= 0) continue;
                var (totalField, remainField) = SlotFieldsByLevel[lvl];
                T(totalField, n.ToString());
                T(remainField, n.ToString());
            }

            // Cantrips (no subclass auto-list; player + racial picks already on character)
            var cantrips = BuildCantripExportLines(c);
            WriteSpellLines(payload, T, C, cantrips, CantripFields, preparedChecks: null);

            // Leveled spells 1–9: subclass always-prepared/known first (tagged), then selected spells
            var byLevel = BuildLeveledSpellExportLines(c, classLevels);
            for (int lvl = 1; lvl <= 9; lvl++)
            {
                if (!byLevel.TryGetValue(lvl, out var lines) || lines.Count == 0)
                    continue;
                if (lvl >= SpellFieldsByLevel.Length)
                    continue;
                WriteSpellLines(
                    payload, T, C, lines,
                    SpellFieldsByLevel[lvl],
                    PreparedChecksByLevel[lvl]);
            }
        }

        private static void WriteSpellLines(
            FieldPayload payload,
            Action<string, string?> T,
            Action<string, bool> C,
            IReadOnlyList<SpellExportLine> lines,
            string[] fieldNames,
            string?[]? preparedChecks)
        {
            for (int i = 0; i < fieldNames.Length && i < lines.Count; i++)
            {
                var line = lines[i];
                string field = fieldNames[i];
                T(field, line.DisplayText);

                if (preparedChecks != null &&
                    i < preparedChecks.Length &&
                    !string.IsNullOrEmpty(preparedChecks[i]) &&
                    line.IsPrepared)
                {
                    C(preparedChecks[i]!, true);
                }

                string? url = BuildSpellWikidotUrl(line.SpellName);
                if (!string.IsNullOrEmpty(url))
                    payload.SpellHyperlinks[field] = url!;
            }
        }

        private static List<SpellExportLine> BuildCantripExportLines(Character c)
        {
            var lines = new List<SpellExportLine>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // High Elf / racial cantrip first if present
            if (!string.IsNullOrWhiteSpace(c.HighElfCantrip) && seen.Add(c.HighElfCantrip.Trim()))
            {
                string name = c.HighElfCantrip.Trim();
                lines.Add(new SpellExportLine
                {
                    SpellName = name,
                    DisplayText = name,
                    IsPrepared = true
                });
            }

            foreach (var name in c.Cantrips ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name.Trim()))
                    continue;
                string n = name.Trim();
                lines.Add(new SpellExportLine
                {
                    SpellName = n,
                    DisplayText = n,
                    IsPrepared = true
                });
            }

            return lines;
        }

        /// <summary>
        /// Builds per-level spell lines: subclass always-prepared/known first with
        /// <c>Faerie Fire (Twilight)</c>-style tags, then remaining selected spells from
        /// <see cref="Character.Level1Spells"/> (which holds all selected leveled spells 1–9).
        /// </summary>
        private static Dictionary<int, List<SpellExportLine>> BuildLeveledSpellExportLines(
            Character c,
            IReadOnlyList<ClassLevelEntry> classLevels)
        {
            var byLevel = new Dictionary<int, List<SpellExportLine>>();
            List<SpellExportLine> ListFor(int level)
            {
                if (!byLevel.TryGetValue(level, out var list))
                {
                    list = new List<SpellExportLine>();
                    byLevel[level] = list;
                }
                return list;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Subclass grants (always prepared / always known) — not expanded list
            foreach (var entry in classLevels)
            {
                if (string.IsNullOrWhiteSpace(entry.Subclass) || entry.Levels < 1)
                    continue;

                string tag = ShortSubclassLabel(entry.Subclass);
                var grants = SubclassSpellCalculator.GetGrantsUpToLevel(entry.Subclass, entry.Levels)
                    .Where(g =>
                        g.SpellLevel >= 1 &&
                        (g.Kind == SubclassSpellGrantKind.AlwaysPrepared ||
                         g.Kind == SubclassSpellGrantKind.AlwaysKnown))
                    .OrderBy(g => g.SpellLevel)
                    .ThenBy(g => g.SpellName, StringComparer.OrdinalIgnoreCase);

                foreach (var g in grants)
                {
                    if (string.IsNullOrWhiteSpace(g.SpellName) || !seen.Add(g.SpellName))
                        continue;

                    string display = string.IsNullOrEmpty(tag)
                        ? g.SpellName
                        : $"{g.SpellName} ({tag})";

                    ListFor(g.SpellLevel).Add(new SpellExportLine
                    {
                        SpellName = g.SpellName,
                        DisplayText = display,
                        IsPrepared = g.Kind == SubclassSpellGrantKind.AlwaysPrepared ||
                                     g.Kind == SubclassSpellGrantKind.AlwaysKnown,
                        SubclassTag = tag
                    });
                }
            }

            // 2) Player-selected leveled spells (stored on Level1Spells regardless of level)
            foreach (var raw in c.Level1Spells ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                // Strip any existing "(Tag)" so we resolve the real spell name
                string spellName = StripSubclassTag(raw.Trim());
                if (!seen.Add(spellName))
                    continue;

                int level = ResolveSpellLevel(spellName);
                if (level < 1)
                    continue; // cantrips belong in the cantrip list

                ListFor(level).Add(new SpellExportLine
                {
                    SpellName = spellName,
                    DisplayText = spellName,
                    IsPrepared = true
                });
            }

            return byLevel;
        }

        private static int ResolveSpellLevel(string spellName)
        {
            var spell = SpellCatalog.Find(spellName);
            if (spell != null)
                return spell.Level;

            // Fallback: treat unknown names as 1st-level so they still appear somewhere useful
            return 1;
        }

        private static string StripSubclassTag(string display)
        {
            // "Faerie Fire (Twilight)" → "Faerie Fire"
            int open = display.LastIndexOf(" (", StringComparison.Ordinal);
            if (open > 0 && display.EndsWith(")", StringComparison.Ordinal))
                return display.Substring(0, open).Trim();
            return display;
        }

        /// <summary>
        /// Short label for sheet notation, e.g. "Twilight Domain" → "Twilight",
        /// "College of Lore" → "Lore", "The Archfey" → "Archfey".
        /// </summary>
        public static string ShortSubclassLabel(string? subclass)
        {
            if (string.IsNullOrWhiteSpace(subclass))
                return "";

            string s = subclass.Trim();
            s = Regex.Replace(s, @"\s+Domain$", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^College of (the\s+)?", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^Circle of (the\s+)?", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^Oath of (the\s+)?", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^Path of (the\s+)?", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^School of (the\s+)?", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^The\s+", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\s+Sorcery$", "", RegexOptions.IgnoreCase);
            return s.Trim();
        }

        private static string BuildSpellcastingClassLabel(
            Character c,
            IReadOnlyList<ClassLevelEntry> classLevels)
        {
            if (classLevels != null && classLevels.Count > 1)
            {
                var parts = classLevels
                    .Where(e => e.Levels > 0)
                    .Select(e =>
                    {
                        string name = e.ClassName;
                        if (!string.IsNullOrWhiteSpace(e.Subclass))
                            name += $" ({ShortSubclassLabel(e.Subclass)})";
                        return $"{name} {e.Levels}";
                    });
                return string.Join(" / ", parts);
            }

            string spellClass = c.Class ?? "";
            if (!string.IsNullOrWhiteSpace(c.Subclass))
                spellClass += $" ({c.Subclass})";
            return spellClass;
        }

        private static int[] DeriveSpellSlotsByLevel(Character c, ExportExtras extras)
        {
            var slots = new int[10];
            var classLevels = LevelUpCalculator.GetClassLevelsFromCharacter(c);
            if (classLevels.Count == 0 && !string.IsNullOrWhiteSpace(c.Class))
            {
                classLevels = new List<ClassLevelEntry>
                {
                    new(c.Class, Math.Max(1, c.Level), c.Subclass)
                };
            }

            if (classLevels.Count > 0)
            {
                var result = SpellSlotCalculator.Calculate(classLevels);
                bool useShared = result.SharedSlots.HighestSlotLevel > 0;
                for (int lvl = 1; lvl <= 9; lvl++)
                {
                    if (useShared)
                        slots[lvl] = result.SharedSlots.GetSlots(lvl);
                    else
                        slots[lvl] = result.PactMagicSlots.GetSlots(lvl);
                }
            }

            // Legacy single-field override for level-1 only when nothing else was computed
            if (extras.Level1SpellSlots.HasValue && slots[1] == 0 &&
                classLevels.Count == 0)
            {
                slots[1] = extras.Level1SpellSlots.Value;
            }
            else if (extras.Level1SpellSlots.HasValue && slots.All(s => s == 0))
            {
                slots[1] = extras.Level1SpellSlots.Value;
            }

            // Last-resort single-class level-1 table for pure level-1 exports
            if (slots.All(s => s == 0))
            {
                int l1 = DeriveLevel1Slots(c.Class);
                if (l1 > 0) slots[1] = l1;
            }

            return slots;
        }

        private static string? BuildSpellWikidotUrl(string spellName)
        {
            if (string.IsNullOrWhiteSpace(spellName))
                return null;
            string slug = SlugifySpellName(spellName);
            if (string.IsNullOrEmpty(slug))
                return null;
            return $"https://dnd5e.wikidot.com/spell:{slug}";
        }

        private static string SlugifySpellName(string spellName)
        {
            return spellName
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace(",", "")
                .Replace("(", "")
                .Replace(")", "");
        }

        /// <summary>
        /// Adds invisible URI link annotations over each spell text field so the displayed
        /// name (not the URL) remains in the field while clicks open the wiki page.
        /// </summary>
        private static void ApplySpellHyperlinks(
            PdfDocument pdf,
            IDictionary<string, iText.Forms.Fields.PdfFormField> fields,
            Dictionary<string, string> fieldToUrl)
        {
            if (fieldToUrl == null || fieldToUrl.Count == 0)
                return;

            foreach (var (fieldName, url) in fieldToUrl)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                if (!fields.TryGetValue(fieldName, out var field) || field == null) continue;

                IList<PdfWidgetAnnotation>? widgets;
                try
                {
                    widgets = field.GetWidgets();
                }
                catch
                {
                    continue;
                }

                if (widgets == null || widgets.Count == 0)
                    continue;

                foreach (var widget in widgets)
                {
                    try
                    {
                        var pdfArray = widget.GetRectangle();
                        if (pdfArray == null || pdfArray.Size() < 4)
                            continue;

                        float llx = pdfArray.GetAsNumber(0).FloatValue();
                        float lly = pdfArray.GetAsNumber(1).FloatValue();
                        float urx = pdfArray.GetAsNumber(2).FloatValue();
                        float ury = pdfArray.GetAsNumber(3).FloatValue();
                        var rect = new iTextRectangle(llx, lly, urx - llx, ury - lly);

                        var link = new PdfLinkAnnotation(rect);
                        link.SetAction(PdfAction.CreateURI(url));
                        link.SetBorder(new PdfArray(new[] { 0, 0, 0 }));
                        // Highlight mode: invert on click (subtle)
                        link.SetHighlightMode(PdfAnnotation.HIGHLIGHT_INVERT);

                        PdfPage? page = widget.GetPage();
                        if (page == null && pdf.GetNumberOfPages() >= 3)
                            page = pdf.GetPage(3); // spell sheet is page 3 (1-based)
                        page?.AddAnnotation(link);
                    }
                    catch
                    {
                        // Best-effort: skip broken widgets
                    }
                }
            }
        }

        private static int Mod(int score) => (int)Math.Floor((score - 10) / 2.0);

        private static string BuildClassLevel(Character c)
        {
            var levels = LevelUpCalculator.GetClassLevelsFromCharacter(c);
            if (levels.Count > 1)
            {
                return string.Join(" / ", levels.Select(e =>
                {
                    string label = e.ClassName;
                    if (!string.IsNullOrWhiteSpace(e.Subclass))
                        label = $"{ShortSubclassLabel(e.Subclass)} {label}";
                    return $"{label} {e.Levels}";
                }));
            }

            if (string.IsNullOrWhiteSpace(c.Class)) return "Level 1";

            int lvl = c.Level > 0 ? c.Level : (levels.Count == 1 ? levels[0].Levels : 1);
            if (!string.IsNullOrWhiteSpace(c.Subclass))
                return $"{c.Subclass} {c.Class} {lvl}";
            return $"{c.Class} {lvl}";
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

            if (!string.IsNullOrEmpty(c.Subclass))
            {
                // Prefer full progression; fall back to legacy level-1-only table
                var subFeats = GameData.GetSubclassProgression(c.Subclass);
                if (subFeats.Count == 0 &&
                    GameData.SubclassLevel1Features.TryGetValue(c.Subclass, out var legacy))
                    subFeats = legacy;

                if (subFeats.Count > 0)
                {
                    sb.AppendLine($"— {c.Subclass} —");
                    foreach (var f in subFeats)
                    {
                        string label = f.Level > 0 ? $"(Lv {f.Level}) {f.Name}" : f.Name;
                        sb.AppendLine("• " + label);
                        if (!string.IsNullOrWhiteSpace(f.Description))
                            sb.AppendLine("  " + f.Description);
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}
