using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace Nemo
{
    /// <summary>
    /// Builds a Foundry VTT D&amp;D 5e Actor JSON document from a Nemo <see cref="Character"/>.
    /// Compatible with right-click → Import Data on a Character actor in Foundry (dnd5e system).
    /// Full Nemo state is preserved under <c>flags.nemo.character</c> for round-trip load.
    /// </summary>
    public static class FoundryCharacterExporter
    {
        public const string NemoFlagNamespace = "nemo";
        public const int NemoFlagVersion = 1;

        /// <summary>
        /// Shared options for Character serialize/deserialize.
        /// TypeInfoResolver is required on .NET 8+ before options are marked read-only
        /// (e.g. after first Serialize/Deserialize use).
        /// </summary>
        private static readonly JsonSerializerOptions NemoOpts = CreateSerializerOptions(writeIndented: false);

        private static JsonSerializerOptions CreateSerializerOptions(bool writeIndented) => new()
        {
            WriteIndented = writeIndented,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        private static readonly Dictionary<string, string> SkillToKey = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Acrobatics"] = "acr",
            ["Animal Handling"] = "ani",
            ["Arcana"] = "arc",
            ["Athletics"] = "ath",
            ["Deception"] = "dec",
            ["History"] = "his",
            ["Insight"] = "ins",
            ["Intimidation"] = "itm",
            ["Investigation"] = "inv",
            ["Medicine"] = "med",
            ["Nature"] = "nat",
            ["Perception"] = "prc",
            ["Performance"] = "prf",
            ["Persuasion"] = "per",
            ["Religion"] = "rel",
            ["Sleight of Hand"] = "slt",
            ["Stealth"] = "ste",
            ["Survival"] = "sur"
        };

        private static readonly (string Key, string Ability)[] SkillDefs =
        {
            ("acr", "dex"), ("ani", "wis"), ("arc", "int"), ("ath", "str"),
            ("dec", "cha"), ("his", "int"), ("ins", "wis"), ("itm", "cha"),
            ("inv", "int"), ("med", "wis"), ("nat", "int"), ("prc", "wis"),
            ("prf", "cha"), ("per", "cha"), ("rel", "int"), ("slt", "dex"),
            ("ste", "dex"), ("sur", "wis")
        };

        private static readonly Dictionary<string, string> LanguageToKey = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Common"] = "common",
            ["Dwarvish"] = "dwarvish",
            ["Elvish"] = "elvish",
            ["Giant"] = "giant",
            ["Gnomish"] = "gnomish",
            ["Goblin"] = "goblin",
            ["Halfling"] = "halfling",
            ["Orc"] = "orc",
            ["Abyssal"] = "abyssal",
            ["Celestial"] = "celestial",
            ["Draconic"] = "draconic",
            ["Deep Speech"] = "deep",
            ["Infernal"] = "infernal",
            ["Primordial"] = "primordial",
            ["Sylvan"] = "sylvan",
            ["Undercommon"] = "undercommon"
        };

        /// <summary>Serialize a Foundry-compatible actor JSON string.</summary>
        public static string ToJson(Character character)
        {
            var actor = BuildActor(character);
            // Fresh options each call — JsonNode.ToJsonString can lock options as read-only.
            return actor.ToJsonString(CreateSerializerOptions(writeIndented: true));
        }

        /// <summary>
        /// Try to recover a Nemo <see cref="Character"/> from JSON that may be
        /// Foundry actor format (with or without flags.nemo) or legacy Nemo format.
        /// </summary>
        public static Character? TryParseCharacter(string json, out string? note)
        {
            note = null;
            if (string.IsNullOrWhiteSpace(json))
                return null;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Foundry actor: type == "character" and system/data present
            if (root.TryGetProperty("type", out var typeEl) &&
                typeEl.ValueKind == JsonValueKind.String &&
                typeEl.GetString()!.Equals("character", StringComparison.OrdinalIgnoreCase) &&
                (root.TryGetProperty("system", out _) || root.TryGetProperty("data", out _)))
            {
                // Prefer full Nemo payload when present
                if (root.TryGetProperty("flags", out var flags) &&
                    flags.TryGetProperty(NemoFlagNamespace, out var nemo) &&
                    nemo.TryGetProperty("character", out var nemoChar))
                {
                    var c = JsonSerializer.Deserialize<Character>(nemoChar.GetRawText(), NemoOpts);
                    if (c != null)
                    {
                        note = "Loaded full Nemo character data from Foundry JSON (flags.nemo).";
                        return c;
                    }
                }

                note = "Loaded from Foundry actor fields (limited — re-save from Nemo for full fidelity).";
                return ImportFromFoundrySystem(root);
            }

            // Legacy Nemo Character JSON
            return JsonSerializer.Deserialize<Character>(json, NemoOpts);
        }

        public static JsonObject BuildActor(Character c)
        {
            ArgumentNullException.ThrowIfNull(c);

            var classLevels = ResolveClassLevels(c);
            string spellAbility = MapSpellAbilityKey(c.SpellcastingAbility);
            if (string.IsNullOrEmpty(spellAbility))
                spellAbility = InferSpellAbility(classLevels);

            string raceId = NewId();
            string backgroundId = NewId();
            string? primaryClassId = null;

            var items = new JsonArray();

            // Race
            if (!string.IsNullOrWhiteSpace(c.Race) || !string.IsNullOrWhiteSpace(c.Subrace))
            {
                string raceName = string.IsNullOrWhiteSpace(c.Subrace)
                    ? (c.Race ?? "Unknown Race")
                    : (c.Subrace!.Contains(c.Race ?? "", StringComparison.OrdinalIgnoreCase)
                        ? c.Subrace
                        : $"{c.Subrace} {c.Race}".Trim());
                items.Add(BuildRaceItem(raceId, raceName, c));
            }
            else
            {
                raceId = "";
            }

            // Background
            if (!string.IsNullOrWhiteSpace(c.Background))
            {
                items.Add(BuildBackgroundItem(backgroundId, c.Background));
            }
            else
            {
                backgroundId = "";
            }

            // Classes (+ optional subclass items)
            foreach (var entry in classLevels)
            {
                string classId = NewId();
                primaryClassId ??= classId;
                items.Add(BuildClassItem(classId, entry));

                string? subclass = GameData.GetEffectiveSubclass(entry);
                if (!string.IsNullOrWhiteSpace(subclass))
                {
                    items.Add(BuildSubclassItem(NewId(), subclass!, entry.ClassName, classId));
                }
            }

            // Selected feat
            if (!string.IsNullOrWhiteSpace(c.SelectedFeat))
            {
                items.Add(BuildFeatItem(NewId(), c.SelectedFeat, "feat",
                    $"Feat selected in Nemo Character Creator."));
            }

            // Class features (compact descriptions)
            foreach (var entry in classLevels)
            {
                var features = GameData.GetClassFeaturesUpToLevel(entry.ClassName, entry.Levels, includeOptional: true);
                foreach (var f in features)
                {
                    string label = f.Level > 1 ? $"{f.Name} (Lv {f.Level})" : f.Name;
                    items.Add(BuildFeatItem(NewId(), label, "class",
                        f.Description ?? "",
                        requirements: $"{entry.ClassName} {Math.Max(1, f.Level)}"));
                }

                string? sub = GameData.GetEffectiveSubclass(entry);
                if (!string.IsNullOrWhiteSpace(sub))
                {
                    var subFeats = GameData.GetSubclassFeaturesUpToLevel(sub!, entry.Levels);
                    foreach (var f in subFeats)
                    {
                        string label = f.Level > 0 ? $"{f.Name} (Lv {f.Level})" : f.Name;
                        items.Add(BuildFeatItem(NewId(), label, "class",
                            f.Description ?? "",
                            requirements: $"{sub} {Math.Max(1, f.Level)}"));
                    }
                }
            }

            // Fighting styles / invocations / metamagic as feats
            foreach (var style in c.FightingStyles ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(style))
                    items.Add(BuildFeatItem(NewId(), style, "class", "Fighting Style.", requirements: "Fighting Style"));
            }
            if (!string.IsNullOrWhiteSpace(c.FightingInitiateStyle))
            {
                items.Add(BuildFeatItem(NewId(), c.FightingInitiateStyle.Trim(), "feat",
                    "Fighting Style from Fighting Initiate.", requirements: "Fighting Initiate"));
            }
            foreach (var maneuver in c.MartialAdeptManeuvers ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(maneuver)) continue;
                var opt = ClassFeatureOptionData.AllManeuvers
                    .FirstOrDefault(o => o.Name.Equals(maneuver.Trim(), StringComparison.OrdinalIgnoreCase));
                items.Add(BuildFeatItem(NewId(), maneuver.Trim(), "feat",
                    opt?.Description ?? "Battle Master maneuver from Martial Adept.",
                    requirements: "Martial Adept"));
            }
            if (!string.IsNullOrWhiteSpace(c.StrikeOfTheGiantsBenefit))
            {
                var gs = ClassFeatureOptionData.AllGiantStrikes
                    .FirstOrDefault(o => o.Name.Equals(c.StrikeOfTheGiantsBenefit.Trim(),
                        StringComparison.OrdinalIgnoreCase));
                items.Add(BuildFeatItem(NewId(), c.StrikeOfTheGiantsBenefit.Trim(), "feat",
                    gs?.Description ?? "Giant strike from Strike of the Giants.",
                    requirements: "Strike of the Giants"));
            }
            foreach (var inv in c.EldritchInvocations ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(inv))
                    items.Add(BuildFeatItem(NewId(), inv, "class", "Eldritch Invocation.", requirements: "Warlock"));
            }
            foreach (var meta in c.MetamagicOptions ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(meta))
                    items.Add(BuildFeatItem(NewId(), meta, "class", "Metamagic option.", requirements: "Sorcerer"));
            }
            if (!string.IsNullOrWhiteSpace(c.WarlockPactBoon))
            {
                items.Add(BuildFeatItem(NewId(), c.WarlockPactBoon, "class",
                    "Warlock Pact Boon.", requirements: "Warlock 3"));
            }
            if (!string.IsNullOrWhiteSpace(c.SamuraiBonusSkill) &&
                LevelUpCalculator.HasSamuraiBonusProficiency(
                    LevelUpCalculator.GetClassLevelsFromCharacter(c)))
            {
                items.Add(BuildFeatItem(NewId(), "Bonus Proficiency: " + c.SamuraiBonusSkill.Trim(),
                    "class", "Samurai Bonus Proficiency.", requirements: "Samurai 3"));
            }

            // Spells
            var spellNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void AddSpell(string? name, bool cantrip)
            {
                if (string.IsNullOrWhiteSpace(name) || !spellNames.Add(name)) return;
                items.Add(BuildSpellItem(NewId(), name.Trim(), cantrip, c));
            }

            foreach (var name in c.Cantrips ?? Enumerable.Empty<string>())
                AddSpell(name, cantrip: true);
            if (!string.IsNullOrWhiteSpace(c.HighElfCantrip))
                AddSpell(c.HighElfCantrip, cantrip: true);
            foreach (var name in c.Level1Spells ?? Enumerable.Empty<string>())
                AddSpell(name, cantrip: false);
            foreach (var name in c.FeatSpells ?? Enumerable.Empty<string>())
            {
                var sp = GameData.FindSpell(name);
                AddSpell(name, cantrip: sp?.Level == 0);
            }

            // Equipment
            foreach (var line in c.Equipment ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                items.Add(BuildEquipmentItem(NewId(), line.Trim(), c));
            }

            // Nemo payload for round-trip
            var nemoCharacterNode = JsonSerializer.SerializeToNode(c, NemoOpts);

            var actor = new JsonObject
            {
                ["name"] = string.IsNullOrWhiteSpace(c.Name) ? "Unnamed Character" : c.Name.Trim(),
                ["type"] = "character",
                ["img"] = BuildImg(c),
                ["system"] = BuildSystem(c, classLevels, spellAbility, raceId, backgroundId, primaryClassId),
                ["items"] = items,
                ["effects"] = new JsonArray(),
                ["folder"] = null,
                ["sort"] = 0,
                ["ownership"] = new JsonObject { ["default"] = 0 },
                ["flags"] = new JsonObject
                {
                    [NemoFlagNamespace] = new JsonObject
                    {
                        ["version"] = NemoFlagVersion,
                        ["character"] = nemoCharacterNode,
                        ["exportedAt"] = DateTime.UtcNow.ToString("o"),
                        ["source"] = "Nemo D&D Character Creator"
                    },
                    ["exportSource"] = new JsonObject
                    {
                        ["world"] = "Nemo",
                        ["system"] = "dnd5e",
                        ["coreVersion"] = "12.331",
                        ["systemVersion"] = "3.3.1"
                    }
                },
                ["_stats"] = new JsonObject
                {
                    ["systemId"] = "dnd5e",
                    ["systemVersion"] = "3.3.1",
                    ["coreVersion"] = "12.331",
                    ["createdTime"] = null,
                    ["modifiedTime"] = null,
                    ["lastModifiedBy"] = null
                }
            };

            return actor;
        }

        // ---------- system ----------

        private static JsonObject BuildSystem(
            Character c,
            IReadOnlyList<ClassLevelEntry> classLevels,
            string spellAbility,
            string raceId,
            string backgroundId,
            string? primaryClassId)
        {
            var abilities = new JsonObject();
            void AddAbility(string key, AbilityScore score, bool proficient)
            {
                abilities[key] = new JsonObject
                {
                    ["value"] = score?.Final > 0 ? score.Final : (score?.Base ?? 10),
                    ["proficient"] = proficient ? 1 : 0,
                    ["max"] = null,
                    ["bonuses"] = new JsonObject { ["check"] = "", ["save"] = "" }
                };
            }

            var saves = c.SavingThrows ?? new List<SavingThrow>();
            bool SaveProf(string name)
            {
                if (saves.Any(s => s.IsProficient &&
                                   s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (!LevelUpCalculator.HasElegantCourtier(classLevels))
                    return false;
                string extra = string.IsNullOrWhiteSpace(c.ElegantCourtierSave)
                    ? "Wisdom"
                    : c.ElegantCourtierSave.Trim();
                if (string.IsNullOrEmpty(extra)) extra = "Wisdom";
                return extra.Equals(name, StringComparison.OrdinalIgnoreCase);
            }

            AddAbility("str", c.AbilityScores?.Strength ?? new(), SaveProf("Strength"));
            AddAbility("dex", c.AbilityScores?.Dexterity ?? new(), SaveProf("Dexterity"));
            AddAbility("con", c.AbilityScores?.Constitution ?? new(), SaveProf("Constitution"));
            AddAbility("int", c.AbilityScores?.Intelligence ?? new(), SaveProf("Intelligence"));
            AddAbility("wis", c.AbilityScores?.Wisdom ?? new(), SaveProf("Wisdom"));
            AddAbility("cha", c.AbilityScores?.Charisma ?? new(), SaveProf("Charisma"));

            int hp = Math.Max(0, c.HitPoints);
            int walk = c.Speed > 0 ? c.Speed : 30;
            int ac = c.ArmorClass > 0 ? c.ArmorClass : 10;
            int level = Math.Clamp(c.Level > 0 ? c.Level : classLevels.Sum(e => e.Levels), 1, 20);
            if (level < 1) level = 1;

            var skills = new JsonObject();
            var skillLookup = (c.Skills ?? new List<SkillEntry>())
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            int wisMod = c.AbilityScores?.Wisdom?.Modifier ?? 0;
            bool elegantCourtier = LevelUpCalculator.HasElegantCourtier(classLevels);

            foreach (var (key, ability) in SkillDefs)
            {
                double value = 0;
                // Match by foundry key via SkillToKey reverse
                var match = skillLookup.Values.FirstOrDefault(s =>
                    SkillToKey.TryGetValue(s.Name, out var k) && k == key);
                if (match != null)
                {
                    if (match.IsExpertise) value = 2;
                    else if (match.IsProficient) value = 1;
                }

                if (value == 0 &&
                    key == "per" &&
                    !string.IsNullOrWhiteSpace(c.SamuraiBonusSkill) &&
                    c.SamuraiBonusSkill.Equals("Persuasion", StringComparison.OrdinalIgnoreCase) &&
                    LevelUpCalculator.HasSamuraiBonusProficiency(classLevels))
                    value = 1;

                string checkBonus = "";
                if (elegantCourtier && key == "per" && wisMod != 0)
                    checkBonus = wisMod >= 0 ? $"+{wisMod}" : wisMod.ToString();

                skills[key] = new JsonObject
                {
                    ["value"] = value,
                    ["ability"] = ability,
                    ["bonuses"] = new JsonObject { ["check"] = checkBonus, ["passive"] = "" }
                };
            }

            var (armorProfs, weaponProfs, languages, customLangs) = BuildTraitLists(c, classLevels);

            var system = new JsonObject
            {
                ["abilities"] = abilities,
                ["attributes"] = new JsonObject
                {
                    ["ac"] = new JsonObject
                    {
                        ["flat"] = ac,
                        ["calc"] = "flat",
                        ["formula"] = ""
                    },
                    ["hp"] = new JsonObject
                    {
                        ["value"] = hp,
                        ["max"] = hp,
                        ["temp"] = 0,
                        ["tempmax"] = 0,
                        ["bonuses"] = new JsonObject { ["level"] = "", ["overall"] = "" }
                    },
                    ["init"] = new JsonObject
                    {
                        ["ability"] = "",
                        ["bonus"] = c.Initiative.ToString()
                    },
                    ["movement"] = new JsonObject
                    {
                        ["burrow"] = null,
                        ["climb"] = null,
                        ["fly"] = null,
                        ["swim"] = null,
                        ["walk"] = walk,
                        ["units"] = "ft",
                        ["hover"] = false
                    },
                    ["attunement"] = new JsonObject { ["max"] = 3 },
                    ["senses"] = new JsonObject
                    {
                        ["darkvision"] = InferDarkvision(c),
                        ["blindsight"] = null,
                        ["tremorsense"] = null,
                        ["truesight"] = null,
                        ["units"] = "ft",
                        ["special"] = ""
                    },
                    ["spellcasting"] = string.IsNullOrEmpty(spellAbility) ? "" : spellAbility,
                    ["death"] = new JsonObject { ["success"] = 0, ["failure"] = 0 },
                    ["exhaustion"] = 0,
                    ["inspiration"] = false
                },
                ["details"] = new JsonObject
                {
                    ["biography"] = new JsonObject
                    {
                        ["value"] = BuildBiographyHtml(c),
                        ["public"] = ""
                    },
                    ["alignment"] = "",
                    // Prefer item refs when we created them; fall back to display names
                    ["race"] = string.IsNullOrEmpty(raceId) ? (c.Race ?? "") : raceId,
                    ["background"] = string.IsNullOrEmpty(backgroundId) ? (c.Background ?? "") : backgroundId,
                    ["originalClass"] = primaryClassId ?? "",
                    ["xp"] = new JsonObject { ["value"] = 0 },
                    ["appearance"] = "",
                    ["trait"] = "",
                    ["ideal"] = "",
                    ["bond"] = "",
                    ["flaw"] = "",
                    ["level"] = level
                },
                ["traits"] = new JsonObject
                {
                    ["size"] = InferSize(c),
                    ["di"] = EmptyTraitSet(),
                    ["dr"] = EmptyTraitSet(),
                    ["dv"] = EmptyTraitSet(),
                    ["ci"] = EmptyTraitSet(),
                    ["languages"] = new JsonObject
                    {
                        ["value"] = ToJsonArray(languages),
                        ["custom"] = customLangs
                    },
                    ["weaponProf"] = new JsonObject
                    {
                        ["value"] = ToJsonArray(weaponProfs),
                        ["custom"] = ""
                    },
                    ["armorProf"] = new JsonObject
                    {
                        ["value"] = ToJsonArray(armorProfs),
                        ["custom"] = ""
                    },
                    ["toolProf"] = new JsonObject
                    {
                        ["value"] = new JsonArray(),
                        ["custom"] = ""
                    }
                },
                ["currency"] = new JsonObject
                {
                    ["pp"] = 0,
                    ["gp"] = Math.Max(0, c.GoldPieces),
                    ["ep"] = 0,
                    ["sp"] = 0,
                    ["cp"] = 0
                },
                ["skills"] = skills,
                ["tools"] = new JsonObject(),
                ["spells"] = BuildSpellSlots(c, classLevels),
                ["bonuses"] = new JsonObject
                {
                    ["mwak"] = new JsonObject { ["attack"] = "", ["damage"] = "" },
                    ["rwak"] = new JsonObject { ["attack"] = "", ["damage"] = "" },
                    ["msak"] = new JsonObject { ["attack"] = "", ["damage"] = "" },
                    ["rsak"] = new JsonObject { ["attack"] = "", ["damage"] = "" },
                    ["abilities"] = new JsonObject { ["check"] = "", ["save"] = "", ["skill"] = "" },
                    ["spell"] = new JsonObject { ["dc"] = "" }
                },
                ["resources"] = new JsonObject
                {
                    ["primary"] = EmptyResource(),
                    ["secondary"] = EmptyResource(),
                    ["tertiary"] = EmptyResource()
                }
            };

            return system;
        }

        private static JsonObject BuildSpellSlots(Character c, IReadOnlyList<ClassLevelEntry> classLevels)
        {
            // Foundry uses system.spells.spell1..spell9 with value/max/override
            // and pact for warlock.
            int[] slots = new int[10]; // index 1-9
            int pactCount = 0;
            int pactLevel = 0;
            try
            {
                var result = SpellSlotCalculator.Calculate(classLevels);
                if (result.SharedSlots?.SlotsByLevel != null)
                {
                    var shared = result.SharedSlots.SlotsByLevel;
                    for (int i = 1; i <= 9 && i < shared.Length; i++)
                        slots[i] = shared[i];
                }
                if (result.PactMagicSlots != null && result.PactMagicSlots.PactSlotLevel is int pl)
                {
                    pactLevel = pl;
                    pactCount = result.PactMagicSlots.GetSlots(pl);
                }
            }
            catch
            {
                // leave zeros — Foundry will recompute from class items when possible
            }

            var spells = new JsonObject
            {
                ["spell1"] = Slot(slots[1]),
                ["spell2"] = Slot(slots[2]),
                ["spell3"] = Slot(slots[3]),
                ["spell4"] = Slot(slots[4]),
                ["spell5"] = Slot(slots[5]),
                ["spell6"] = Slot(slots[6]),
                ["spell7"] = Slot(slots[7]),
                ["spell8"] = Slot(slots[8]),
                ["spell9"] = Slot(slots[9]),
                ["pact"] = new JsonObject
                {
                    ["value"] = pactCount,
                    ["override"] = null,
                    ["level"] = pactLevel
                }
            };

            return spells;

            static JsonObject Slot(int max) => new()
            {
                ["value"] = max,
                ["override"] = null
            };
        }

        // ---------- items ----------

        private static JsonObject BuildRaceItem(string id, string name, Character c)
        {
            return new JsonObject
            {
                ["_id"] = id,
                ["name"] = name,
                ["type"] = "race",
                ["img"] = "icons/environment/people/group.webp",
                ["system"] = new JsonObject
                {
                    ["description"] = new JsonObject
                    {
                        ["value"] = $"<p>Imported from Nemo: {HtmlEncode(name)}.</p>",
                        ["chat"] = ""
                    },
                    ["identifier"] = Slugify(name),
                    ["movement"] = new JsonObject
                    {
                        ["walk"] = c.Speed > 0 ? c.Speed : 30,
                        ["units"] = "ft",
                        ["hover"] = false
                    },
                    ["type"] = new JsonObject
                    {
                        ["value"] = "humanoid",
                        ["subtype"] = "",
                        ["custom"] = ""
                    },
                    ["senses"] = new JsonObject
                    {
                        ["darkvision"] = InferDarkvision(c),
                        ["units"] = "ft",
                        ["special"] = ""
                    }
                },
                ["effects"] = new JsonArray(),
                ["folder"] = null,
                ["sort"] = 0,
                ["flags"] = new JsonObject()
            };
        }

        private static JsonObject BuildBackgroundItem(string id, string name)
        {
            return new JsonObject
            {
                ["_id"] = id,
                ["name"] = name,
                ["type"] = "background",
                ["img"] = "icons/skills/trades/academics-merchant-scribe.webp",
                ["system"] = new JsonObject
                {
                    ["description"] = new JsonObject
                    {
                        ["value"] = $"<p>Background: {HtmlEncode(name)} (imported from Nemo).</p>",
                        ["chat"] = ""
                    },
                    ["identifier"] = Slugify(name)
                },
                ["effects"] = new JsonArray(),
                ["folder"] = null,
                ["sort"] = 0,
                ["flags"] = new JsonObject()
            };
        }

        private static JsonObject BuildClassItem(string id, ClassLevelEntry entry)
        {
            string className = entry.ClassName ?? "Unknown";
            string hitDice = "d8";
            string spellAbility = "";
            string progression = "none";

            if (GameData.ClassData.TryGetValue(className, out var data))
            {
                hitDice = NormalizeHitDice(data.HitDie);
                if (data.Spellcasting && !string.IsNullOrWhiteSpace(data.SpellAbility))
                {
                    spellAbility = MapSpellAbilityKey(data.SpellAbility);
                    progression = className.Equals("Warlock", StringComparison.OrdinalIgnoreCase)
                        ? "pact"
                        : className is "Paladin" or "Ranger" or "Artificer"
                            ? "half"
                            : "full";
                }
            }

            // Third-casters
            string? subclass = GameData.GetEffectiveSubclass(entry);
            if (SpellProgressionCalculator.IsThirdCasterSubclass(className, subclass ?? ""))
            {
                progression = "third";
                spellAbility = "int";
            }

            return new JsonObject
            {
                ["_id"] = id,
                ["name"] = className,
                ["type"] = "class",
                ["img"] = "icons/skills/melee/weapons-crossed-swords-yellow.webp",
                ["system"] = new JsonObject
                {
                    ["description"] = new JsonObject
                    {
                        ["value"] = $"<p>{HtmlEncode(className)} class (imported from Nemo).</p>",
                        ["chat"] = ""
                    },
                    ["identifier"] = Slugify(className),
                    ["levels"] = Math.Clamp(entry.Levels, 1, 20),
                    ["hitDice"] = hitDice,
                    ["hitDiceUsed"] = 0,
                    ["advancement"] = new JsonArray(),
                    ["spellcasting"] = new JsonObject
                    {
                        ["progression"] = progression,
                        ["ability"] = spellAbility
                    },
                    ["saves"] = BuildClassSaves(className)
                },
                ["effects"] = new JsonArray(),
                ["folder"] = null,
                ["sort"] = 0,
                ["flags"] = new JsonObject()
            };
        }

        private static JsonArray BuildClassSaves(string className)
        {
            var arr = new JsonArray();
            if (!GameData.ClassData.TryGetValue(className, out var data) ||
                data.SavingThrowProficiencies == null)
                return arr;

            foreach (var save in data.SavingThrowProficiencies)
            {
                string key = save.ToLowerInvariant() switch
                {
                    "strength" => "str",
                    "dexterity" => "dex",
                    "constitution" => "con",
                    "intelligence" => "int",
                    "wisdom" => "wis",
                    "charisma" => "cha",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(key))
                    arr.Add(key);
            }
            return arr;
        }

        private static JsonObject BuildSubclassItem(string id, string name, string className, string classItemId)
        {
            return new JsonObject
            {
                ["_id"] = id,
                ["name"] = name,
                ["type"] = "subclass",
                ["img"] = "icons/magic/symbols/rune-sigil-red-pink.webp",
                ["system"] = new JsonObject
                {
                    ["description"] = new JsonObject
                    {
                        ["value"] = $"<p>{HtmlEncode(name)} subclass of {HtmlEncode(className)}.</p>",
                        ["chat"] = ""
                    },
                    ["identifier"] = Slugify(name),
                    ["classIdentifier"] = Slugify(className),
                    ["advancement"] = new JsonArray(),
                    ["spellcasting"] = new JsonObject
                    {
                        ["progression"] = "none",
                        ["ability"] = ""
                    }
                },
                ["effects"] = new JsonArray(),
                ["folder"] = null,
                ["sort"] = 0,
                ["flags"] = new JsonObject
                {
                    ["dnd5e"] = new JsonObject
                    {
                        // Soft link — Foundry may reparent; still useful for humans
                        ["advancementOrigin"] = classItemId
                    }
                }
            };
        }

        private static JsonObject BuildFeatItem(
            string id, string name, string typeValue, string description, string requirements = "")
        {
            return new JsonObject
            {
                ["_id"] = id,
                ["name"] = name,
                ["type"] = "feat",
                ["img"] = "icons/skills/trades/academics-study-reading-book.webp",
                ["system"] = new JsonObject
                {
                    ["description"] = new JsonObject
                    {
                        ["value"] = $"<p>{HtmlEncode(description)}</p>",
                        ["chat"] = ""
                    },
                    ["type"] = new JsonObject
                    {
                        ["value"] = typeValue,
                        ["subtype"] = ""
                    },
                    ["requirements"] = requirements,
                    ["identifier"] = Slugify(name),
                    ["uses"] = new JsonObject
                    {
                        ["max"] = "",
                        ["spent"] = 0,
                        ["recovery"] = new JsonArray()
                    }
                },
                ["effects"] = new JsonArray(),
                ["folder"] = null,
                ["sort"] = 0,
                ["flags"] = new JsonObject()
            };
        }

        private static JsonObject BuildSpellItem(string id, string name, bool forceCantrip, Character c)
        {
            var spell = GameData.FindSpell(name);
            int level = forceCantrip ? 0 : (spell?.Level ?? 1);
            if (spell != null) level = spell.Level;

            string prepMode = "prepared";
            // Warlock known spells use pact; cantrips always available
            bool isWarlock = (c.ClassLevels ?? new List<ClassLevelEntry>())
                .Any(e => e.ClassName.Equals("Warlock", StringComparison.OrdinalIgnoreCase))
                || string.Equals(c.Class, "Warlock", StringComparison.OrdinalIgnoreCase);

            if (level == 0)
                prepMode = "always";
            else if (isWarlock)
                prepMode = "pact";

            string school = MapSchool(spell?.School);
            string desc = spell?.FullDescription ?? spell?.Description ?? "";
            if (string.IsNullOrWhiteSpace(desc))
                desc = $"{name} (imported from Nemo).";

            var properties = new JsonArray();
            if (spell != null)
            {
                string comps = spell.Components ?? "";
                if (comps.Contains('V', StringComparison.OrdinalIgnoreCase)) properties.Add("vocal");
                if (comps.Contains('S', StringComparison.OrdinalIgnoreCase)) properties.Add("somatic");
                if (comps.Contains('M', StringComparison.OrdinalIgnoreCase)) properties.Add("material");
                if (spell.IsConcentration) properties.Add("concentration");
                if (spell.IsRitual) properties.Add("ritual");
            }
            properties.Add("mgc");

            return new JsonObject
            {
                ["_id"] = id,
                ["name"] = name,
                ["type"] = "spell",
                ["img"] = "icons/magic/light/projectile-flare-blue.webp",
                ["system"] = new JsonObject
                {
                    ["description"] = new JsonObject
                    {
                        ["value"] = $"<p>{HtmlEncode(desc)}</p>",
                        ["chat"] = ""
                    },
                    ["level"] = level,
                    ["school"] = school,
                    ["activation"] = new JsonObject
                    {
                        ["type"] = InferActivation(spell?.CastingTime),
                        ["cost"] = 1,
                        ["condition"] = ""
                    },
                    ["duration"] = new JsonObject
                    {
                        ["value"] = "",
                        ["units"] = spell?.IsConcentration == true ? "conc" : "inst"
                    },
                    ["range"] = new JsonObject
                    {
                        ["value"] = ParseLeadingNumber(spell?.Range),
                        ["units"] = InferRangeUnits(spell?.Range)
                    },
                    ["target"] = new JsonObject
                    {
                        ["value"] = null,
                        ["width"] = null,
                        ["units"] = "",
                        ["type"] = ""
                    },
                    ["uses"] = new JsonObject
                    {
                        ["max"] = "",
                        ["spent"] = 0,
                        ["recovery"] = new JsonArray()
                    },
                    ["materials"] = new JsonObject
                    {
                        ["value"] = spell?.Material ?? "",
                        ["consumed"] = false,
                        ["cost"] = 0,
                        ["supply"] = 0
                    },
                    ["preparation"] = new JsonObject
                    {
                        ["mode"] = prepMode,
                        ["prepared"] = true
                    },
                    ["properties"] = properties,
                    ["identifier"] = Slugify(name)
                },
                ["effects"] = new JsonArray(),
                ["folder"] = null,
                ["sort"] = 0,
                ["flags"] = new JsonObject()
            };
        }

        private static JsonObject BuildEquipmentItem(string id, string line, Character c)
        {
            // Try match known weapons
            var allWeapons = GameData.SimpleWeapons.Concat(GameData.MartialWeapons).ToList();
            var weapon = allWeapons.FirstOrDefault(w =>
            {
                string lowerLine = line.ToLowerInvariant();
                string lowerName = w.Name.ToLowerInvariant();
                return lowerLine == lowerName ||
                       lowerLine.StartsWith(lowerName + " ") ||
                       lowerLine.Contains(" " + lowerName + " ") ||
                       lowerLine.EndsWith(" " + lowerName) ||
                       lowerLine.Contains(lowerName);
            });

            if (weapon != null)
            {
                bool isRanged = !string.IsNullOrWhiteSpace(weapon.Range) && weapon.Range != "-";
                bool finesse = weapon.Properties?.Contains("Finesse", StringComparison.OrdinalIgnoreCase) == true;
                string ability = isRanged || finesse ? "dex" : "str";

                var props = new JsonArray();
                if (weapon.Properties != null)
                {
                    if (weapon.Properties.Contains("Light", StringComparison.OrdinalIgnoreCase)) props.Add("lgt");
                    if (weapon.Properties.Contains("Finesse", StringComparison.OrdinalIgnoreCase)) props.Add("fin");
                    if (weapon.Properties.Contains("Heavy", StringComparison.OrdinalIgnoreCase)) props.Add("hvy");
                    if (weapon.Properties.Contains("Two-Handed", StringComparison.OrdinalIgnoreCase)) props.Add("two");
                    if (weapon.Properties.Contains("Reach", StringComparison.OrdinalIgnoreCase)) props.Add("rch");
                    if (weapon.Properties.Contains("Thrown", StringComparison.OrdinalIgnoreCase)) props.Add("thr");
                    if (weapon.Properties.Contains("Versatile", StringComparison.OrdinalIgnoreCase)) props.Add("ver");
                    if (weapon.Properties.Contains("Ammunition", StringComparison.OrdinalIgnoreCase)) props.Add("amm");
                }

                // Parse "1d8 piercing" style damage
                string damageDice = "1d6";
                string damageType = "bludgeoning";
                ParseWeaponDamage(weapon.Damage, out damageDice, out damageType);

                int? rangeShort = null, rangeLong = null;
                if (isRanged && !string.IsNullOrWhiteSpace(weapon.Range))
                {
                    var m = Regex.Match(weapon.Range, @"(\d+)\s*/\s*(\d+)");
                    if (m.Success)
                    {
                        rangeShort = int.Parse(m.Groups[1].Value);
                        rangeLong = int.Parse(m.Groups[2].Value);
                    }
                    else if (int.TryParse(Regex.Match(weapon.Range, @"\d+").Value, out int single))
                    {
                        rangeShort = single;
                    }
                }

                return new JsonObject
                {
                    ["_id"] = id,
                    ["name"] = weapon.Name,
                    ["type"] = "weapon",
                    ["img"] = "icons/weapons/swords/sword-guard-brown.webp",
                    ["system"] = new JsonObject
                    {
                        ["description"] = new JsonObject
                        {
                            ["value"] = $"<p>{HtmlEncode(line)}</p>",
                            ["chat"] = ""
                        },
                        ["quantity"] = 1,
                        ["weight"] = new JsonObject { ["value"] = 0, ["units"] = "lb" },
                        ["price"] = new JsonObject { ["value"] = 0, ["denomination"] = "gp" },
                        ["equipped"] = true,
                        ["identified"] = true,
                        ["rarity"] = "",
                        ["ability"] = ability,
                        ["actionType"] = isRanged ? "rwak" : "mwak",
                        ["proficient"] = true,
                        ["properties"] = props,
                        ["range"] = new JsonObject
                        {
                            ["value"] = rangeShort,
                            ["long"] = rangeLong,
                            ["units"] = "ft"
                        },
                        ["damage"] = new JsonObject
                        {
                            ["parts"] = new JsonArray
                            {
                                new JsonArray { $"{damageDice} + @mod", damageType }
                            },
                            ["versatile"] = ""
                        },
                        ["weaponType"] = isRanged ? "simpleR" : "simpleM",
                        ["identifier"] = Slugify(weapon.Name)
                    },
                    ["effects"] = new JsonArray(),
                    ["folder"] = null,
                    ["sort"] = 0,
                    ["flags"] = new JsonObject()
                };
            }

            // Armor?
            bool looksLikeArmor = line.Contains("armor", StringComparison.OrdinalIgnoreCase) ||
                                  line.Contains("mail", StringComparison.OrdinalIgnoreCase) ||
                                  line.Contains("plate", StringComparison.OrdinalIgnoreCase) ||
                                  line.Contains("leather", StringComparison.OrdinalIgnoreCase) ||
                                  line.Contains("shield", StringComparison.OrdinalIgnoreCase);

            if (looksLikeArmor)
            {
                string armorType = line.Contains("shield", StringComparison.OrdinalIgnoreCase) ? "shield"
                    : line.Contains("plate", StringComparison.OrdinalIgnoreCase) ||
                      line.Contains("splint", StringComparison.OrdinalIgnoreCase) ||
                      line.Contains("chain mail", StringComparison.OrdinalIgnoreCase) ||
                      line.Contains("ring mail", StringComparison.OrdinalIgnoreCase) ? "heavy"
                    : line.Contains("scale", StringComparison.OrdinalIgnoreCase) ||
                      line.Contains("breastplate", StringComparison.OrdinalIgnoreCase) ||
                      line.Contains("half plate", StringComparison.OrdinalIgnoreCase) ||
                      line.Contains("hide", StringComparison.OrdinalIgnoreCase) ||
                      line.Contains("chain shirt", StringComparison.OrdinalIgnoreCase) ? "medium"
                    : "light";

                return new JsonObject
                {
                    ["_id"] = id,
                    ["name"] = line,
                    ["type"] = "equipment",
                    ["img"] = "icons/equipment/chest/breastplate-banded-steel.webp",
                    ["system"] = new JsonObject
                    {
                        ["description"] = new JsonObject
                        {
                            ["value"] = $"<p>{HtmlEncode(line)}</p>",
                            ["chat"] = ""
                        },
                        ["quantity"] = 1,
                        ["weight"] = new JsonObject { ["value"] = 0, ["units"] = "lb" },
                        ["price"] = new JsonObject { ["value"] = 0, ["denomination"] = "gp" },
                        ["equipped"] = true,
                        ["identified"] = true,
                        ["rarity"] = "",
                        ["armor"] = new JsonObject
                        {
                            ["value"] = 0,
                            ["dex"] = null,
                            ["magicalBonus"] = null
                        },
                        ["type"] = new JsonObject
                        {
                            ["value"] = armorType == "shield" ? "clothing" : "light",
                            ["baseItem"] = ""
                        },
                        // dnd5e uses armor.type for armor category in some versions;
                        // also set equipment type value to armor/shield where supported
                        ["identifier"] = Slugify(line)
                    },
                    ["effects"] = new JsonArray(),
                    ["folder"] = null,
                    ["sort"] = 0,
                    ["flags"] = new JsonObject()
                };
            }

            // Generic loot
            return new JsonObject
            {
                ["_id"] = id,
                ["name"] = line,
                ["type"] = "loot",
                ["img"] = "icons/containers/bags/pack-leather-brown.webp",
                ["system"] = new JsonObject
                {
                    ["description"] = new JsonObject
                    {
                        ["value"] = $"<p>{HtmlEncode(line)}</p>",
                        ["chat"] = ""
                    },
                    ["quantity"] = 1,
                    ["weight"] = new JsonObject { ["value"] = 0, ["units"] = "lb" },
                    ["price"] = new JsonObject { ["value"] = 0, ["denomination"] = "gp" },
                    ["identified"] = true,
                    ["rarity"] = "",
                    ["identifier"] = Slugify(line)
                },
                ["effects"] = new JsonArray(),
                ["folder"] = null,
                ["sort"] = 0,
                ["flags"] = new JsonObject()
            };
        }

        // ---------- Foundry → Nemo (limited) ----------

        private static Character ImportFromFoundrySystem(JsonElement root)
        {
            var c = new Character
            {
                Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : ""
            };

            JsonElement system;
            if (!root.TryGetProperty("system", out system) &&
                !root.TryGetProperty("data", out system))
                return c;

            if (system.TryGetProperty("abilities", out var abilities))
            {
                c.AbilityScores ??= new AbilityScoreBlock();
                ApplyAbility(abilities, "str", s => c.AbilityScores.Strength = s);
                ApplyAbility(abilities, "dex", s => c.AbilityScores.Dexterity = s);
                ApplyAbility(abilities, "con", s => c.AbilityScores.Constitution = s);
                ApplyAbility(abilities, "int", s => c.AbilityScores.Intelligence = s);
                ApplyAbility(abilities, "wis", s => c.AbilityScores.Wisdom = s);
                ApplyAbility(abilities, "cha", s => c.AbilityScores.Charisma = s);
            }

            if (system.TryGetProperty("attributes", out var attr))
            {
                if (attr.TryGetProperty("hp", out var hp) &&
                    hp.TryGetProperty("value", out var hpVal) &&
                    hpVal.TryGetInt32(out int hpv))
                    c.HitPoints = hpv;

                if (attr.TryGetProperty("ac", out var acEl))
                {
                    if (acEl.TryGetProperty("flat", out var flat) && flat.ValueKind == JsonValueKind.Number &&
                        flat.TryGetInt32(out int acv))
                        c.ArmorClass = acv;
                    else if (acEl.TryGetProperty("value", out var acVal) && acVal.TryGetInt32(out int acv2))
                        c.ArmorClass = acv2;
                }

                if (attr.TryGetProperty("movement", out var mov) &&
                    mov.TryGetProperty("walk", out var walk) &&
                    walk.ValueKind == JsonValueKind.Number &&
                    walk.TryGetInt32(out int walkV))
                    c.Speed = walkV;

                if (attr.TryGetProperty("spellcasting", out var sc) &&
                    sc.ValueKind == JsonValueKind.String)
                {
                    c.SpellcastingAbility = sc.GetString() switch
                    {
                        "str" => "Strength",
                        "dex" => "Dexterity",
                        "con" => "Constitution",
                        "int" => "Intelligence",
                        "wis" => "Wisdom",
                        "cha" => "Charisma",
                        _ => c.SpellcastingAbility
                    };
                }
            }

            if (system.TryGetProperty("details", out var details))
            {
                if (details.TryGetProperty("background", out var bg) && bg.ValueKind == JsonValueKind.String)
                {
                    string b = bg.GetString() ?? "";
                    // Skip Foundry document ids (16-char alphanumeric)
                    if (b.Length != 16)
                        c.Background = b;
                }
                if (details.TryGetProperty("race", out var race) && race.ValueKind == JsonValueKind.String)
                {
                    string r = race.GetString() ?? "";
                    if (r.Length != 16)
                        c.Race = r;
                }
                if (details.TryGetProperty("level", out var lvl) && lvl.TryGetInt32(out int level))
                    c.Level = Math.Clamp(level, 1, 20);
            }

            if (system.TryGetProperty("currency", out var cur) &&
                cur.TryGetProperty("gp", out var gp) &&
                gp.TryGetInt32(out int gpv))
                c.GoldPieces = gpv;

            if (system.TryGetProperty("skills", out var skills))
            {
                c.Skills = new List<SkillEntry>();
                var reverse = SkillToKey.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);
                foreach (var prop in skills.EnumerateObject())
                {
                    if (!reverse.TryGetValue(prop.Name, out string skillName)) continue;
                    double val = 0;
                    if (prop.Value.TryGetProperty("value", out var v))
                    {
                        if (v.ValueKind == JsonValueKind.Number) val = v.GetDouble();
                    }
                    if (val <= 0) continue;
                    string ability = prop.Value.TryGetProperty("ability", out var ab)
                        ? (ab.GetString() ?? "")
                        : "";
                    c.Skills.Add(new SkillEntry
                    {
                        Name = skillName,
                        Ability = ability switch
                        {
                            "str" => "Str", "dex" => "Dex", "con" => "Con",
                            "int" => "Int", "wis" => "Wis", "cha" => "Cha",
                            _ => ability
                        },
                        IsProficient = val >= 1,
                        IsExpertise = val >= 2
                    });
                }
            }

            // Items: classes, spells, equipment names
            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                c.ClassLevels = new List<ClassLevelEntry>();
                c.Cantrips = new List<string>();
                c.Level1Spells = new List<string>();
                c.Equipment = new List<string>();

                foreach (var item in items.EnumerateArray())
                {
                    string itemType = item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                    string itemName = item.TryGetProperty("name", out var iname) ? iname.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(itemName)) continue;

                    if (itemType == "class")
                    {
                        int levels = 1;
                        if (item.TryGetProperty("system", out var isys) &&
                            isys.TryGetProperty("levels", out var lv) &&
                            lv.TryGetInt32(out int lvi))
                            levels = Math.Clamp(lvi, 1, 20);
                        c.ClassLevels.Add(new ClassLevelEntry(itemName, levels, ""));
                        if (string.IsNullOrWhiteSpace(c.Class))
                            c.Class = itemName;
                    }
                    else if (itemType == "subclass" && c.ClassLevels.Count > 0)
                    {
                        // Attach to last class if empty
                        var last = c.ClassLevels[^1];
                        if (string.IsNullOrWhiteSpace(last.Subclass))
                            last.Subclass = itemName;
                        c.Subclass = itemName;
                    }
                    else if (itemType == "race")
                    {
                        c.Race = itemName;
                    }
                    else if (itemType == "background")
                    {
                        c.Background = itemName;
                    }
                    else if (itemType == "spell")
                    {
                        int level = 0;
                        if (item.TryGetProperty("system", out var ssys) &&
                            ssys.TryGetProperty("level", out var sl) &&
                            sl.TryGetInt32(out int sli))
                            level = sli;
                        if (level == 0) c.Cantrips.Add(itemName);
                        else c.Level1Spells.Add(itemName);
                    }
                    else if (itemType is "weapon" or "equipment" or "loot" or "consumable" or "tool" or "container")
                    {
                        c.Equipment.Add(itemName);
                    }
                    else if (itemType == "feat" &&
                             item.TryGetProperty("system", out var fsys) &&
                             fsys.TryGetProperty("type", out var ftype) &&
                             ftype.TryGetProperty("value", out var ftv) &&
                             ftv.GetString() == "feat" &&
                             string.IsNullOrWhiteSpace(c.SelectedFeat))
                    {
                        c.SelectedFeat = itemName;
                    }
                }

                if (c.ClassLevels.Count > 0)
                    c.Level = Math.Clamp(c.ClassLevels.Sum(e => e.Levels), 1, 20);
            }

            return c;
        }

        private static void ApplyAbility(JsonElement abilities, string key, Action<AbilityScore> set)
        {
            if (!abilities.TryGetProperty(key, out var ab)) return;
            int value = 10;
            if (ab.TryGetProperty("value", out var v) && v.TryGetInt32(out int vi))
                value = vi;
            int mod = (int)Math.Floor((value - 10) / 2.0);
            set(new AbilityScore
            {
                Base = value,
                Racial = 0,
                Feat = 0,
                Final = value,
                Modifier = mod
            });
        }

        // ---------- helpers ----------

        private static List<ClassLevelEntry> ResolveClassLevels(Character c)
        {
            if (c.ClassLevels != null && c.ClassLevels.Count > 0)
                return c.ClassLevels.Where(e => !string.IsNullOrWhiteSpace(e.ClassName) && e.Levels > 0).ToList();

            if (!string.IsNullOrWhiteSpace(c.Class))
            {
                return new List<ClassLevelEntry>
                {
                    new(c.Class, Math.Max(1, c.Level), c.Subclass ?? "")
                };
            }

            return new List<ClassLevelEntry>();
        }

        private static string InferSpellAbility(IReadOnlyList<ClassLevelEntry> classLevels)
        {
            foreach (var e in classLevels)
            {
                if (SpellProgressionCalculator.IsThirdCasterSubclass(e.ClassName, e.Subclass ?? ""))
                    return "int";
                if (GameData.ClassData.TryGetValue(e.ClassName, out var d) &&
                    d.Spellcasting &&
                    !string.IsNullOrWhiteSpace(d.SpellAbility))
                    return MapSpellAbilityKey(d.SpellAbility);
            }
            return "";
        }

        private static string MapSpellAbilityKey(string? ability) =>
            ability?.Trim().ToLowerInvariant() switch
            {
                "strength" or "str" => "str",
                "dexterity" or "dex" => "dex",
                "constitution" or "con" => "con",
                "intelligence" or "int" => "int",
                "wisdom" or "wis" => "wis",
                "charisma" or "cha" => "cha",
                _ => ""
            };

        private static string NormalizeHitDice(string hitDie)
        {
            // "1d12" or "d12" → "d12"
            if (string.IsNullOrWhiteSpace(hitDie)) return "d8";
            int d = hitDie.IndexOf('d');
            if (d >= 0) return "d" + hitDie[(d + 1)..].Trim();
            return "d8";
        }

        private static (List<string> Armor, List<string> Weapons, List<string> Languages, string CustomLangs)
            BuildTraitLists(Character c, IReadOnlyList<ClassLevelEntry> classLevels)
        {
            var armor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var langs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var customLangs = new List<string>();

            foreach (var entry in classLevels)
            {
                if (!GameData.ClassData.TryGetValue(entry.ClassName, out var data)) continue;
                foreach (var a in data.ArmorProficiencies ?? Enumerable.Empty<string>())
                    MapArmorProf(a, armor);
                foreach (var w in data.WeaponProficiencies ?? Enumerable.Empty<string>())
                    MapWeaponProf(w, weapons);
            }

            if (GameData.RaceData.TryGetValue(c.Race ?? "", out var raceData))
            {
                foreach (var lang in raceData.Languages ?? Enumerable.Empty<string>())
                {
                    if (LanguageToKey.TryGetValue(lang, out var key))
                        langs.Add(key);
                    else if (!string.IsNullOrWhiteSpace(lang))
                        customLangs.Add(lang);
                }
            }

            foreach (var lang in c.BackgroundLanguages ?? Enumerable.Empty<string>())
            {
                if (LanguageToKey.TryGetValue(lang, out var key))
                    langs.Add(key);
                else if (!string.IsNullOrWhiteSpace(lang))
                    customLangs.Add(lang);
            }

            if (langs.Count == 0)
                langs.Add("common");

            return (armor.ToList(), weapons.ToList(), langs.ToList(), string.Join(";", customLangs));
        }

        private static void MapArmorProf(string text, HashSet<string> armor)
        {
            string t = text.ToLowerInvariant();
            if (t.Contains("all armor"))
            {
                armor.Add("lgt"); armor.Add("med"); armor.Add("hvy");
            }
            if (t.Contains("light")) armor.Add("lgt");
            if (t.Contains("medium")) armor.Add("med");
            if (t.Contains("heavy")) armor.Add("hvy");
            if (t.Contains("shield")) armor.Add("shl");
        }

        private static void MapWeaponProf(string text, HashSet<string> weapons)
        {
            string t = text.ToLowerInvariant();
            if (t.Contains("simple")) weapons.Add("sim");
            if (t.Contains("martial")) weapons.Add("mar");
        }

        private static string InferSize(Character c)
        {
            string race = $"{c.Race} {c.Subrace}".ToLowerInvariant();
            if (race.Contains("halfling") || race.Contains("gnome") || race.Contains("kobold"))
                return "sm";
            if (race.Contains("bugbear") || race.Contains("goliath") || race.Contains("firbolg"))
                return "med"; // still medium in 2014 PHB for most
            return "med";
        }

        private static int? InferDarkvision(Character c)
        {
            string race = $"{c.Race} {c.Subrace}".ToLowerInvariant();
            if (race.Contains("drow") || race.Contains("deep gnome") || race.Contains("svirfneblin"))
                return 120;
            if (race.Contains("elf") || race.Contains("dwarf") || race.Contains("gnome") ||
                race.Contains("half-orc") || race.Contains("tiefling") || race.Contains("dragonborn") ||
                race.Contains("aarakocra") || race.Contains("genasi") || race.Contains("goliath") ||
                race.Contains("tabaxi") || race.Contains("triton") || race.Contains("yuan") ||
                race.Contains("bugbear") || race.Contains("goblin") || race.Contains("hobgoblin") ||
                race.Contains("kenku") || race.Contains("kobold") || race.Contains("lizardfolk") ||
                race.Contains("orc") || race.Contains("firbolg") || race.Contains("half-elf"))
                return 60;
            return null;
        }

        private static string BuildBiographyHtml(Character c)
        {
            var parts = new List<string>
            {
                $"<p><strong>Player:</strong> {HtmlEncode(c.PlayerName)}</p>",
                $"<p><strong>Race:</strong> {HtmlEncode(c.Race)}" +
                (string.IsNullOrWhiteSpace(c.Subrace) ? "" : $" ({HtmlEncode(c.Subrace)})") +
                $"</p>",
                $"<p><strong>Background:</strong> {HtmlEncode(c.Background)}</p>",
                $"<p><strong>Class:</strong> {HtmlEncode(c.Class)}" +
                (string.IsNullOrWhiteSpace(c.Subclass) ? "" : $" ({HtmlEncode(c.Subclass)})") +
                $" · Level {c.Level}</p>"
            };
            if (!string.IsNullOrWhiteSpace(c.SelectedFeat))
                parts.Add($"<p><strong>Feat:</strong> {HtmlEncode(c.SelectedFeat)}</p>");
            parts.Add("<p><em>Exported from Nemo D&amp;D Character Creator.</em></p>");
            return string.Join("", parts);
        }

        private static string? BuildImg(Character c)
        {
            if (string.IsNullOrWhiteSpace(c.AvatarBase64))
                return "icons/svg/mystery-man.svg";
            string b64 = c.AvatarBase64.Trim();
            if (b64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return b64;
            // Assume PNG if no prefix
            return "data:image/png;base64," + b64;
        }

        private static string MapSchool(string? school)
        {
            if (string.IsNullOrWhiteSpace(school)) return "evo";
            string s = school.Trim().ToLowerInvariant();
            if (s.StartsWith("abj")) return "abj";
            if (s.StartsWith("con")) return "con";
            if (s.StartsWith("div")) return "div";
            if (s.StartsWith("enc")) return "enc";
            if (s.StartsWith("evo")) return "evo";
            if (s.StartsWith("ill")) return "ill";
            if (s.StartsWith("nec")) return "nec";
            if (s.StartsWith("tra")) return "trs";
            return "evo";
        }

        private static string InferActivation(string? castingTime)
        {
            if (string.IsNullOrWhiteSpace(castingTime)) return "action";
            string t = castingTime.ToLowerInvariant();
            if (t.Contains("bonus")) return "bonus";
            if (t.Contains("reaction")) return "reaction";
            if (t.Contains("minute")) return "minute";
            if (t.Contains("hour")) return "hour";
            return "action";
        }

        private static string InferRangeUnits(string? range)
        {
            if (string.IsNullOrWhiteSpace(range)) return "ft";
            string t = range.ToLowerInvariant();
            if (t.Contains("self")) return "self";
            if (t.Contains("touch")) return "touch";
            if (t.Contains("sight")) return "spec";
            if (t.Contains("unlimited")) return "any";
            return "ft";
        }

        private static int? ParseLeadingNumber(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Regex.Match(text, @"\d+");
            if (m.Success && int.TryParse(m.Value, out int n)) return n;
            return null;
        }

        private static void ParseWeaponDamage(string? damage, out string dice, out string type)
        {
            dice = "1d6";
            type = "bludgeoning";
            if (string.IsNullOrWhiteSpace(damage)) return;
            var m = Regex.Match(damage, @"(\d+d\d+)", RegexOptions.IgnoreCase);
            if (m.Success) dice = m.Groups[1].Value.ToLowerInvariant();
            foreach (var t in new[] { "slashing", "piercing", "bludgeoning", "fire", "cold", "lightning", "acid", "poison", "necrotic", "radiant", "force", "psychic", "thunder" })
            {
                if (damage.Contains(t, StringComparison.OrdinalIgnoreCase))
                {
                    type = t;
                    break;
                }
            }
        }

        private static JsonObject EmptyTraitSet() => new()
        {
            ["value"] = new JsonArray(),
            ["custom"] = ""
        };

        private static JsonObject EmptyResource() => new()
        {
            ["value"] = 0,
            ["max"] = 0,
            ["sr"] = false,
            ["lr"] = false,
            ["label"] = ""
        };

        private static JsonArray ToJsonArray(IEnumerable<string> values)
        {
            var arr = new JsonArray();
            foreach (var v in values)
                arr.Add(v);
            return arr;
        }

        private static string NewId()
        {
            // Foundry uses 16-char base64-ish ids; alphanumeric is fine
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var rng = Random.Shared;
            var buf = new char[16];
            for (int i = 0; i < 16; i++)
                buf[i] = chars[rng.Next(chars.Length)];
            return new string(buf);
        }

        private static string Slugify(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "item";
            string s = name.ToLowerInvariant().Trim();
            s = Regex.Replace(s, @"[^a-z0-9]+", "-");
            s = s.Trim('-');
            return string.IsNullOrEmpty(s) ? "item" : s;
        }

        private static string HtmlEncode(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
