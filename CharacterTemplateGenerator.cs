using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>Top-level generation style for themed character templates.</summary>
    public enum TemplateCategory
    {
        Optimized,
        General,
        Random,
        /// <summary>User-authored builds saved on disk for reuse.</summary>
        Custom
    }

    /// <summary>
    /// Combat role focus. <see cref="None"/> is true-random (no role bias).
    /// </summary>
    public enum TemplateRole
    {
        Support,
        Damage,
        Tank,
        None
    }

    /// <summary>A named race/class/background package used by optimized &amp; general generation.</summary>
    public sealed class CharacterBuildTemplate
    {
        public string Name { get; init; } = "";
        public TemplateCategory Category { get; init; }
        public TemplateRole Role { get; init; }
        public string Class { get; init; } = "";
        public string Subclass { get; init; } = "";
        public string Race { get; init; } = "";
        public string Subrace { get; init; } = "";
        public string Background { get; init; } = "";
        /// <summary>Ability names in priority order (highest score first).</summary>
        public string[] AbilityPriority { get; init; } = Array.Empty<string>();
        public string[] PreferredSkills { get; init; } = Array.Empty<string>();
        public string[] PreferredCantrips { get; init; } = Array.Empty<string>();
        public string[] PreferredSpells { get; init; } = Array.Empty<string>();
        public string Description { get; init; } = "";
    }

    /// <summary>Result of a template / random generation pass.</summary>
    public sealed class GeneratedCharacterResult
    {
        public Character Character { get; init; } = new();
        public string TemplateName { get; init; } = "";
        public string Summary { get; init; } = "";
        public TemplateCategory Category { get; init; }
        public TemplateRole Role { get; init; }
    }

    /// <summary>
    /// Builds complete level-1 characters from themed templates or pure randomness.
    /// Categories: Optimized (strong synergies), General (solid thematic builds), Random (role-biased or true random).
    /// Roles: Support (heal/CC/buff), Damage (DPR/AoE), Tank (HP/AC/aggro).
    /// </summary>
    public static class CharacterTemplateGenerator
    {
        private static readonly int[] StandardArray = { 15, 14, 13, 12, 10, 8 };
        private static readonly string[] AllAbilities =
            { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };

        private static readonly string[] FirstNames =
        {
            "Aelar", "Bryn", "Corin", "Dara", "Elias", "Faye", "Garrick", "Hilda",
            "Ivor", "Jora", "Kael", "Lira", "Mira", "Nox", "Orin", "Perrin",
            "Quinn", "Rhea", "Sable", "Thane", "Uma", "Vesper", "Wren", "Xander",
            "Yara", "Zara", "Aldric", "Brielle", "Cedric", "Dahlia", "Ember", "Finn"
        };

        private static readonly string[] LastNames =
        {
            "Ashwood", "Blackthorn", "Coldwater", "Dawnbringer", "Evershade", "Firebrand",
            "Goldleaf", "Ironhide", "Jadefist", "Keeneye", "Lightfoot", "Moonwhisper",
            "Nightbloom", "Oakenshield", "Proudmane", "Ravencrest", "Stormborn", "Thornwick",
            "Underbough", "Valeforge", "Winterborn", "Shadowmend", "Brightsong", "Stoneheart"
        };

        // Role-weighted pools for random generation (class → preferred subclasses).
        // Subclass lists are ordered with stronger/meta options first so Random+role still skews viable.
        private static readonly Dictionary<TemplateRole, (string Class, string[] Subclasses)[]> RoleClassPools =
            new()
            {
                [TemplateRole.Support] = new[]
                {
                    ("Cleric", new[] { "Peace", "Twilight", "Order", "Life", "Knowledge" }),
                    ("Bard", new[] { "College of Lore", "College of Eloquence", "College of Glamour", "College of Creation" }),
                    ("Druid", new[] { "Circle of the Shepherd", "Circle of Stars", "Circle of Dreams" }),
                    ("Sorcerer", new[] { "Divine Soul", "Clockwork Soul" }),
                    ("Warlock", new[] { "The Celestial", "The Archfey" }),
                    ("Paladin", new[] { "Oath of the Ancients", "Oath of Devotion", "Oath of Redemption", "Oath of the Crown" }),
                    ("Artificer", new[] { "Alchemist", "Battle Smith" }),
                    ("Ranger", new[] { "Fey Wanderer", "Horizon Walker", "Beast Master" }),
                },
                [TemplateRole.Damage] = new[]
                {
                    ("Warlock", new[] { "The Hexblade", "The Fiend", "The Genie", "The Undead" }),
                    ("Ranger", new[] { "Gloom Stalker", "Hunter", "Drakewarden", "Monster Slayer" }),
                    ("Wizard", new[] { "Chronurgy", "Evocation", "War Magic", "Bladesinging" }),
                    ("Sorcerer", new[] { "Clockwork Soul", "Draconic Bloodline", "Aberrant Mind", "Storm" }),
                    ("Paladin", new[] { "Oath of Vengeance", "Oath of Conquest", "Oath of Glory", "Oathbreaker" }),
                    ("Fighter", new[] { "Battle Master", "Echo Knight", "Eldritch Knight", "Samurai" }),
                    ("Barbarian", new[] { "Path of the Beast", "Path of the Zealot", "Path of the Giant", "Path of the Berserker" }),
                    ("Rogue", new[] { "Phantom", "Soulknife", "Swashbuckler", "Arcane Trickster" }),
                    ("Artificer", new[] { "Artillerist", "Armorer", "Battle Smith" }),
                    ("Cleric", new[] { "Light", "Tempest", "War", "Death" }),
                    ("Bard", new[] { "College of Swords", "College of Valor", "College of Whispers" }),
                    ("Druid", new[] { "Circle of the Moon", "Circle of Spores", "Circle of Wildfire" }),
                    ("Monk", new[] { "Way of the Open Hand", "Way of the Kensei", "Way of Mercy", "Way of Shadow" }),
                },
                [TemplateRole.Tank] = new[]
                {
                    ("Barbarian", new[] { "Path of the Totem Warrior", "Path of the Ancestral Guardian", "Path of the Zealot", "Path of the Beast" }),
                    ("Cleric", new[] { "Twilight", "Forge", "Life", "War", "Tempest" }),
                    ("Paladin", new[] { "Oath of the Ancients", "Oath of the Crown", "Oath of Devotion", "Oath of Conquest" }),
                    ("Fighter", new[] { "Rune Knight", "Cavalier", "Battle Master", "Samurai" }),
                    ("Artificer", new[] { "Armorer", "Battle Smith" }),
                    ("Druid", new[] { "Circle of the Moon", "Circle of Stars" }),
                    ("Monk", new[] { "Way of the Open Hand", "Way of Mercy", "Way of the Long Death" }),
                    ("Ranger", new[] { "Horizon Walker", "Beast Master" }),
                }
            };

        private static readonly Dictionary<TemplateRole, string[]> RoleSkillBias = new()
        {
            [TemplateRole.Support] = new[]
            {
                "Medicine", "Insight", "Persuasion", "Religion", "Arcana", "Perception",
                "Animal Handling", "Performance", "History"
            },
            [TemplateRole.Damage] = new[]
            {
                "Athletics", "Acrobatics", "Stealth", "Intimidation", "Arcana", "Perception",
                "Investigation", "Sleight of Hand", "Survival"
            },
            [TemplateRole.Tank] = new[]
            {
                "Athletics", "Intimidation", "Perception", "Insight", "Survival",
                "Medicine", "Animal Handling", "History"
            }
        };

        private static readonly Dictionary<TemplateRole, string[]> RoleCantripBias = new()
        {
            [TemplateRole.Support] = new[]
            {
                "Guidance", "Spare the Dying", "Resistance", "Light", "Sacred Flame",
                "Thaumaturgy", "Druidcraft", "Mending", "Friends", "Message", "Minor Illusion",
                "Vicious Mockery", "Shillelagh"
            },
            [TemplateRole.Damage] = new[]
            {
                "Fire Bolt", "Eldritch Blast", "Ray of Frost", "Toll the Dead", "Sacred Flame",
                "Produce Flame", "Chill Touch", "Acid Splash", "Poison Spray", "Booming Blade",
                "Green-Flame Blade", "Shocking Grasp", "Mind Sliver", "Sword Burst", "Thorn Whip"
            },
            [TemplateRole.Tank] = new[]
            {
                "Spare the Dying", "Resistance", "Blade Ward", "Sacred Flame", "Thorn Whip",
                "Booming Blade", "Shillelagh", "Light", "Guidance", "Toll the Dead"
            }
        };

        private static readonly Dictionary<TemplateRole, string[]> RoleSpellBias = new()
        {
            [TemplateRole.Support] = new[]
            {
                "Cure Wounds", "Healing Word", "Bless", "Shield of Faith", "Sanctuary",
                "Detect Magic", "Faerie Fire", "Sleep", "Charm Person", "Heroism",
                "Bane", "Protection from Evil and Good", "Goodberry", "Entangle", "Identify"
            },
            [TemplateRole.Damage] = new[]
            {
                "Magic Missile", "Burning Hands", "Chromatic Orb", "Guiding Bolt", "Inflict Wounds",
                "Hex", "Hunter's Mark", "Thunderwave", "Ice Knife", "Witch Bolt",
                "Chaos Bolt", "Catapult", "Hail of Thorns", "Searing Smite", "Wrathful Smite"
            },
            [TemplateRole.Tank] = new[]
            {
                "Shield", "Absorb Elements", "Armor of Agathys", "Compelled Duel", "Sanctuary",
                "Shield of Faith", "Heroism", "False Life", "Expeditious Retreat", "Jump",
                "Longstrider", "Detect Evil and Good", "Protection from Evil and Good"
            }
        };

        private static readonly Dictionary<string, string[]> ClassAbilityPriority = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Artificer"] = new[] { "Intelligence", "Constitution", "Dexterity", "Wisdom", "Charisma", "Strength" },
            ["Barbarian"] = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
            ["Bard"] = new[] { "Charisma", "Dexterity", "Constitution", "Wisdom", "Intelligence", "Strength" },
            ["Cleric"] = new[] { "Wisdom", "Constitution", "Strength", "Charisma", "Dexterity", "Intelligence" },
            ["Druid"] = new[] { "Wisdom", "Constitution", "Dexterity", "Intelligence", "Charisma", "Strength" },
            ["Fighter"] = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
            ["Monk"] = new[] { "Dexterity", "Wisdom", "Constitution", "Strength", "Charisma", "Intelligence" },
            ["Paladin"] = new[] { "Strength", "Charisma", "Constitution", "Wisdom", "Dexterity", "Intelligence" },
            ["Ranger"] = new[] { "Dexterity", "Wisdom", "Constitution", "Strength", "Intelligence", "Charisma" },
            ["Rogue"] = new[] { "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma", "Strength" },
            ["Sorcerer"] = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
            ["Warlock"] = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
            ["Wizard"] = new[] { "Intelligence", "Constitution", "Dexterity", "Wisdom", "Charisma", "Strength" },
        };

        // Dex-based fighter / ranger / paladin variants use this when flagged
        private static readonly string[] DexMartialPriority =
            { "Dexterity", "Constitution", "Wisdom", "Strength", "Charisma", "Intelligence" };

        private static readonly Lazy<List<CharacterBuildTemplate>> Templates =
            new(BuildTemplates);

        public static IReadOnlyList<CharacterBuildTemplate> AllTemplates => Templates.Value;

        public static string[] CategoryNames { get; } = { "Optimized", "General", "Random", "Custom" };

        public static string[] RoleNames { get; } = { "Support", "Damage", "Tank" };

        public static string[] RandomRoleNames { get; } =
            { "Support", "Damage", "Tank", "True Random" };

        /// <summary>
        /// Categories shown in Quick Generate. Custom is only listed when the user has saved templates.
        /// </summary>
        public static IReadOnlyList<string> GetAvailableCategoryNames()
        {
            if (CustomTemplateStore.HasAny())
                return CategoryNames;
            return new[] { "Optimized", "General", "Random" };
        }

        public static TemplateCategory ParseCategory(string? text) =>
            (text ?? "").Trim() switch
            {
                "Optimized" => TemplateCategory.Optimized,
                "General" => TemplateCategory.General,
                "Random" => TemplateCategory.Random,
                "Custom" => TemplateCategory.Custom,
                _ => TemplateCategory.General
            };

        public static TemplateRole ParseRole(string? text) =>
            (text ?? "").Trim() switch
            {
                "Support" => TemplateRole.Support,
                "Damage" => TemplateRole.Damage,
                "Tank" => TemplateRole.Tank,
                "True Random" or "None" or "TrueRandom" => TemplateRole.None,
                _ => TemplateRole.Support
            };

        public static string DescribeCategory(TemplateCategory category) =>
            category switch
            {
                TemplateCategory.Optimized =>
                    "Optimized: known 5e meta builds (Peace/Twilight, Hexblade, Gloom Stalker, Bear Totem, etc.) with standard-array primaries and role-focused picks.",
                TemplateCategory.General =>
                    "General: solid thematic builds that work well without hard min-maxing.",
                TemplateCategory.Random =>
                    "Random: rolls within a role (or pure chaos with True Random).",
                TemplateCategory.Custom =>
                    "Custom: your saved builds, captured from characters you created. Reuse them any time.",
                _ => ""
            };

        public static string DescribeRole(TemplateRole role) =>
            role switch
            {
                TemplateRole.Support =>
                    "Support: healing, buffs, crowd control, and keeping the party alive.",
                TemplateRole.Damage =>
                    "Damage: high single-target or AoE damage, extra attacks, and offensive spells.",
                TemplateRole.Tank =>
                    "Tank: hit points, armor class, damage reduction, and drawing enemy attention.",
                TemplateRole.None =>
                    "True Random: any race, class, and stats with no role filter.",
                _ => ""
            };

        /// <summary>
        /// Templates available for a category + role (Optimized / General / Custom pick lists).
        /// Ordered by name. Empty for Random (no fixed list).
        /// </summary>
        public static IReadOnlyList<CharacterBuildTemplate> GetTemplates(
            TemplateCategory category,
            TemplateRole role)
        {
            if (category == TemplateCategory.Random || role == TemplateRole.None)
                return Array.Empty<CharacterBuildTemplate>();

            if (category == TemplateCategory.Custom)
            {
                return CustomTemplateStore.GetAll()
                    .Where(t => t.Role == role)
                    .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var pool = Templates.Value
                .Where(t => t.Category == category && t.Role == role)
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pool.Count == 0)
            {
                // Fall back to any built-in templates for this role (e.g. if only one category filled)
                pool = Templates.Value
                    .Where(t => t.Role == role)
                    .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return pool;
        }

        /// <summary>
        /// Build a Custom template from the current character sheet values.
        /// Ability priority is derived from base scores (highest first).
        /// </summary>
        public static CharacterBuildTemplate CreateCustomTemplateFromCharacter(
            Character character,
            string name,
            TemplateRole role,
            string? description = null)
        {
            ArgumentNullException.ThrowIfNull(character);
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Template name is required.", nameof(name));
            if (role == TemplateRole.None)
                role = TemplateRole.Support;

            string className = (character.Class ?? "").Trim();
            string subclass = (character.Subclass ?? "").Trim();
            string race = (character.Race ?? "").Trim();
            string subrace = (character.Subrace ?? "").Trim();
            string background = (character.Background ?? "").Trim();

            var priority = DeriveAbilityPriority(character);
            var skills = (character.Skills ?? new List<SkillEntry>())
                .Where(s => s != null && s.IsProficient && !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var cantrips = (character.Cantrips ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var spells = (character.Level1Spells ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            string desc = (description ?? "").Trim();
            if (string.IsNullOrEmpty(desc))
            {
                desc = $"Saved custom build: {race}" +
                       (string.IsNullOrEmpty(subrace) ? "" : $" ({subrace})") +
                       $" {className}" +
                       (string.IsNullOrEmpty(subclass) ? "" : $" ({subclass})") +
                       (string.IsNullOrEmpty(background) ? "" : $" · {background}");
            }

            return new CharacterBuildTemplate
            {
                Name = name.Trim(),
                Category = TemplateCategory.Custom,
                Role = role,
                Class = className,
                Subclass = subclass,
                Race = race,
                Subrace = subrace,
                Background = background,
                AbilityPriority = priority,
                PreferredSkills = skills,
                PreferredCantrips = cantrips,
                PreferredSpells = spells,
                Description = desc
            };
        }

        private static string[] DeriveAbilityPriority(Character character)
        {
            var block = character?.AbilityScores;
            if (block == null)
                return AllAbilities.ToArray();

            var scored = new (string Name, int Base)[]
            {
                ("Strength", block.Strength?.Base ?? 0),
                ("Dexterity", block.Dexterity?.Base ?? 0),
                ("Constitution", block.Constitution?.Base ?? 0),
                ("Intelligence", block.Intelligence?.Base ?? 0),
                ("Wisdom", block.Wisdom?.Base ?? 0),
                ("Charisma", block.Charisma?.Base ?? 0),
            };

            // Highest base first; stable order among ties via AllAbilities index
            return scored
                .OrderByDescending(x => x.Base)
                .ThenBy(x => Array.IndexOf(AllAbilities, x.Name))
                .Select(x => x.Name)
                .ToArray();
        }

        /// <summary>
        /// One-line kit label for the picker, e.g. "Custom Lineage · Cleric (Peace) · Acolyte".
        /// </summary>
        public static string FormatKitLine(CharacterBuildTemplate t)
        {
            if (t == null) return "";
            string race = t.Race ?? "";
            if (!string.IsNullOrWhiteSpace(t.Subrace))
                race = string.IsNullOrWhiteSpace(race) ? t.Subrace : $"{t.Race} ({t.Subrace})";

            string kit = t.Class ?? "";
            if (!string.IsNullOrWhiteSpace(t.Subclass))
                kit = $"{kit} ({t.Subclass})";

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(race)) parts.Add(race);
            if (!string.IsNullOrWhiteSpace(kit)) parts.Add(kit);
            if (!string.IsNullOrWhiteSpace(t.Background)) parts.Add(t.Background);
            return string.Join(" · ", parts);
        }

        /// <summary>Generate a full level-1 <see cref="Character"/> for the given category and role.</summary>
        public static GeneratedCharacterResult Generate(
            TemplateCategory category,
            TemplateRole role,
            Random? rng = null)
        {
            rng ??= new Random();

            if (category == TemplateCategory.Random)
            {
                if (role == TemplateRole.None)
                    return GenerateTrueRandom(rng);
                return GenerateRoleRandom(role, rng);
            }

            var pool = GetTemplates(category, role).ToList();
            if (pool.Count == 0)
            {
                if (category == TemplateCategory.Custom)
                    throw new InvalidOperationException("No custom templates saved for this role yet. Create one first.");
                return GenerateRoleRandom(role == TemplateRole.None ? TemplateRole.Support : role, rng);
            }

            var template = pool[rng.Next(pool.Count)];
            return BuildFromTemplate(template, category, rng);
        }

        /// <summary>Generate from a specific template the user picked in the build list.</summary>
        public static GeneratedCharacterResult GenerateFromTemplate(
            CharacterBuildTemplate template,
            Random? rng = null)
        {
            ArgumentNullException.ThrowIfNull(template);
            rng ??= new Random();
            return BuildFromTemplate(template, template.Category, rng);
        }

        private static GeneratedCharacterResult BuildFromTemplate(
            CharacterBuildTemplate template,
            TemplateCategory category,
            Random rng)
        {
            string className = template.Class;
            string subclass = NormalizeSubclass(className, template.Subclass);
            string race = template.Race;
            string subrace = template.Subrace;
            string background = template.Background;

            // Validate against live data; fall back if a key was renamed
            if (!GameData.RaceData.ContainsKey(race))
                race = PickRandom(GameData.RaceData.Keys.ToList(), rng);
            if (!string.IsNullOrEmpty(subrace) &&
                GameData.RaceSubraces.TryGetValue(race, out var subs) &&
                !subs.Any(s => s.Name.Equals(subrace, StringComparison.OrdinalIgnoreCase)))
            {
                subrace = PickRandom(subs.Select(s => s.Name).ToList(), rng) ?? "";
            }
            else if (string.IsNullOrEmpty(subrace) &&
                     GameData.RaceSubraces.TryGetValue(race, out var forcedSubs) &&
                     forcedSubs.Count > 0 &&
                     category == TemplateCategory.Optimized)
            {
                // Prefer a subrace if one exists for optimized builds that omitted it
                subrace = forcedSubs[0].Name;
            }

            if (!GameData.ClassData.ContainsKey(className))
                className = "Fighter";
            if (!GameData.AllBackgrounds.Contains(background, StringComparer.OrdinalIgnoreCase))
                background = PickRandom(GameData.AllBackgrounds, rng) ?? "Folk Hero";

            var priority = template.AbilityPriority?.Length == 6
                ? template.AbilityPriority
                : GetAbilityPriority(className, subclass, template.Role);

            // Custom and Optimized keep standard-array primaries so saved priorities stick.
            int[] scores = category is TemplateCategory.Optimized or TemplateCategory.Custom
                ? (int[])StandardArray.Clone()
                : SoftShuffleArray(StandardArray, rng);

            var abilityBases = AssignScores(priority, scores);

            var skills = PickSkills(className, background, template.PreferredSkills, template.Role, rng);
            var (cantrips, spells) = PickSpells(className, template.PreferredCantrips, template.PreferredSpells, template.Role, rng);

            string name = MakeName(rng);
            var character = AssembleCharacter(
                name, race, subrace, background, className, subclass, abilityBases, skills, cantrips, spells);

            string summary =
                $"{template.Name}\n" +
                $"{name} — {race}{(string.IsNullOrEmpty(subrace) ? "" : " (" + subrace + ")")} {className}" +
                (string.IsNullOrEmpty(subclass) ? "" : $" ({subclass})") +
                $" · {background}\n" +
                (string.IsNullOrEmpty(template.Description) ? DescribeRole(template.Role) : template.Description);

            return new GeneratedCharacterResult
            {
                Character = character,
                TemplateName = template.Name,
                Summary = summary,
                Category = category,
                Role = template.Role
            };
        }

        private static GeneratedCharacterResult GenerateRoleRandom(TemplateRole role, Random rng)
        {
            if (!RoleClassPools.TryGetValue(role, out var pool) || pool.Length == 0)
                return GenerateTrueRandom(rng);

            var entry = pool[rng.Next(pool.Length)];
            string className = entry.Class;
            string subclass = entry.Subclasses.Length > 0
                ? entry.Subclasses[rng.Next(entry.Subclasses.Length)]
                : "";
            subclass = NormalizeSubclass(className, subclass);

            // Prefer races that boost the primary ability when possible
            string primary = GetAbilityPriority(className, subclass, role)[0];
            string race = PickRaceBiasedToward(primary, rng);
            string subrace = PickSubrace(race, primary, rng);
            string background = PickBackgroundForRole(role, rng);

            int[] scores = Roll4d6DropLowest(rng);
            // Assign highest rolls to role/class priorities
            Array.Sort(scores);
            Array.Reverse(scores);
            var priority = GetAbilityPriority(className, subclass, role);
            var abilityBases = AssignScores(priority, scores);

            var skills = PickSkills(className, background, Array.Empty<string>(), role, rng);
            var (cantrips, spells) = PickSpells(className, Array.Empty<string>(), Array.Empty<string>(), role, rng);

            string name = MakeName(rng);
            var character = AssembleCharacter(
                name, race, subrace, background, className, subclass, abilityBases, skills, cantrips, spells);

            string summary =
                $"Random {role}\n" +
                $"{name} — {race}{(string.IsNullOrEmpty(subrace) ? "" : " (" + subrace + ")")} {className}" +
                (string.IsNullOrEmpty(subclass) ? "" : $" ({subclass})") +
                $" · {background}\n" +
                DescribeRole(role);

            return new GeneratedCharacterResult
            {
                Character = character,
                TemplateName = $"Random {role}",
                Summary = summary,
                Category = TemplateCategory.Random,
                Role = role
            };
        }

        private static GeneratedCharacterResult GenerateTrueRandom(Random rng)
        {
            var classes = GameData.ClassData.Keys.ToList();
            string className = PickRandom(classes, rng) ?? "Fighter";
            var subclassNames = GameData.GetSubclassNames(className);
            string subclass = subclassNames.Count > 0
                ? subclassNames[rng.Next(subclassNames.Count)]
                : "";

            var races = GameData.RaceData.Keys.ToList();
            string race = PickRandom(races, rng) ?? "Human";
            string subrace = "";
            if (GameData.RaceSubraces.TryGetValue(race, out var subs) && subs.Count > 0)
                subrace = subs[rng.Next(subs.Count)].Name;

            string background = PickRandom(GameData.AllBackgrounds, rng) ?? "Folk Hero";

            // Fully random ability scores + random assignment
            int[] scores = Roll4d6DropLowest(rng);
            var shuffledAbilities = AllAbilities.OrderBy(_ => rng.Next()).ToArray();
            var abilityBases = AssignScores(shuffledAbilities, scores);

            // Random skills from class list
            var skills = PickSkills(className, background, Array.Empty<string>(), TemplateRole.None, rng);
            var (cantrips, spells) = PickSpells(className, Array.Empty<string>(), Array.Empty<string>(), TemplateRole.None, rng);

            string name = MakeName(rng);
            var character = AssembleCharacter(
                name, race, subrace, background, className, subclass, abilityBases, skills, cantrips, spells);

            string summary =
                $"True Random\n" +
                $"{name} — {race}{(string.IsNullOrEmpty(subrace) ? "" : " (" + subrace + ")")} {className}" +
                (string.IsNullOrEmpty(subclass) ? "" : $" ({subclass})") +
                $" · {background}\n" +
                "No role filter — pure chaos.";

            return new GeneratedCharacterResult
            {
                Character = character,
                TemplateName = "True Random",
                Summary = summary,
                Category = TemplateCategory.Random,
                Role = TemplateRole.None
            };
        }

        private static Character AssembleCharacter(
            string name,
            string race,
            string subrace,
            string background,
            string className,
            string subclass,
            Dictionary<string, int> abilityBases,
            List<string> skills,
            List<string> cantrips,
            List<string> spells)
        {
            var c = new Character
            {
                Name = name,
                PlayerName = "",
                Race = race,
                Subrace = subrace ?? "",
                Background = background,
                Class = className,
                Subclass = subclass ?? "",
                Level = 1,
                ClassLevels = new List<ClassLevelEntry>
                {
                    new(className, 1, subclass ?? "")
                },
                AbilityScores = new AbilityScoreBlock
                {
                    Strength = new AbilityScore { Base = abilityBases.GetValueOrDefault("Strength", 10) },
                    Dexterity = new AbilityScore { Base = abilityBases.GetValueOrDefault("Dexterity", 10) },
                    Constitution = new AbilityScore { Base = abilityBases.GetValueOrDefault("Constitution", 10) },
                    Intelligence = new AbilityScore { Base = abilityBases.GetValueOrDefault("Intelligence", 10) },
                    Wisdom = new AbilityScore { Base = abilityBases.GetValueOrDefault("Wisdom", 10) },
                    Charisma = new AbilityScore { Base = abilityBases.GetValueOrDefault("Charisma", 10) },
                },
                Skills = skills.Select(s => new SkillEntry
                {
                    Name = s,
                    IsProficient = true
                }).ToList(),
                Cantrips = cantrips,
                Level1Spells = spells,
                ProficiencyBonus = 2,
                Speed = GameData.RaceData.TryGetValue(race, out var rd) ? rd.Speed : 30
            };

            if (GameData.ClassData.TryGetValue(className, out var cd) && cd.Spellcasting)
                c.SpellcastingAbility = cd.SpellAbility ?? "";

            return c;
        }

        private static Dictionary<string, int> AssignScores(string[] priority, int[] scoresDescending)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int i = 0;

            foreach (var ab in priority)
            {
                if (used.Contains(ab) || i >= scoresDescending.Length) continue;
                result[ab] = scoresDescending[i++];
                used.Add(ab);
            }

            foreach (var ab in AllAbilities)
            {
                if (used.Contains(ab)) continue;
                result[ab] = i < scoresDescending.Length ? scoresDescending[i++] : 10;
                used.Add(ab);
            }

            return result;
        }

        private static int[] SoftShuffleArray(int[] source, Random rng)
        {
            // Keep top two roughly high; shuffle the rest so general builds feel less min-maxed
            var copy = (int[])source.Clone();
            // Swap pairs among the lower four sometimes
            for (int n = 0; n < 2; n++)
            {
                int a = rng.Next(2, copy.Length);
                int b = rng.Next(2, copy.Length);
                (copy[a], copy[b]) = (copy[b], copy[a]);
            }
            // Occasional swap of #2 and #3
            if (rng.NextDouble() < 0.45 && copy.Length >= 3)
                (copy[1], copy[2]) = (copy[2], copy[1]);
            return copy;
        }

        private static int[] Roll4d6DropLowest(Random rng)
        {
            var scores = new int[6];
            for (int i = 0; i < 6; i++)
            {
                var rolls = new[] { rng.Next(1, 7), rng.Next(1, 7), rng.Next(1, 7), rng.Next(1, 7) };
                Array.Sort(rolls);
                scores[i] = rolls[1] + rolls[2] + rolls[3];
            }
            return scores;
        }

        private static string[] GetAbilityPriority(string className, string subclass, TemplateRole role)
        {
            // Role can nudge martial tanks toward Con, etc.
            if (ClassAbilityPriority.TryGetValue(className, out var basePri))
            {
                var list = basePri.ToList();

                // Dex-leaning subclasses
                if (className.Equals("Fighter", StringComparison.OrdinalIgnoreCase) &&
                    (subclass.Contains("Arcane Archer", StringComparison.OrdinalIgnoreCase) ||
                     subclass.Contains("Eldritch Knight", StringComparison.OrdinalIgnoreCase) == false &&
                     role == TemplateRole.Damage && subclass.Contains("Champion", StringComparison.OrdinalIgnoreCase)))
                {
                    // leave Str default for Champion tank/damage; Arcane Archer uses Dex
                }
                if (className.Equals("Fighter", StringComparison.OrdinalIgnoreCase) &&
                    subclass.Contains("Arcane Archer", StringComparison.OrdinalIgnoreCase))
                    list = DexMartialPriority.ToList();

                if (className.Equals("Ranger", StringComparison.OrdinalIgnoreCase))
                    list = DexMartialPriority.ToList();

                if (className.Equals("Rogue", StringComparison.OrdinalIgnoreCase))
                    list = new List<string> { "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma", "Strength" };

                if (role == TemplateRole.Tank)
                {
                    // Bump Constitution after primary
                    Promote(list, "Constitution", 1);
                }
                else if (role == TemplateRole.Support &&
                         (className.Equals("Cleric", StringComparison.OrdinalIgnoreCase) ||
                          className.Equals("Druid", StringComparison.OrdinalIgnoreCase)))
                {
                    Promote(list, "Constitution", 1);
                }

                return list.ToArray();
            }

            return AllAbilities.ToArray();
        }

        private static void Promote(List<string> list, string ability, int targetIndex)
        {
            int idx = list.FindIndex(a => a.Equals(ability, StringComparison.OrdinalIgnoreCase));
            if (idx < 0 || idx == targetIndex) return;
            list.RemoveAt(idx);
            if (targetIndex > list.Count) targetIndex = list.Count;
            list.Insert(targetIndex, ability);
        }

        private static List<string> PickSkills(
            string className,
            string background,
            string[] preferred,
            TemplateRole role,
            Random rng)
        {
            var result = new List<string>();
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Background skills first
            if (GameData.BackgroundSkillMap.TryGetValue(background, out var bgSkills))
            {
                foreach (var s in bgSkills)
                {
                    if (taken.Add(s))
                        result.Add(s);
                }
            }

            if (!GameData.ClassData.TryGetValue(className, out var cd))
                return result;

            var allowed = cd.SkillChoices ?? new List<string>();
            int need = cd.SkillChoiceCount;
            var classPicks = new List<string>();

            // Preferred that are on the class list
            foreach (var p in preferred ?? Array.Empty<string>())
            {
                if (classPicks.Count >= need) break;
                if (allowed.Contains(p, StringComparer.OrdinalIgnoreCase) && taken.Add(p))
                    classPicks.Add(p);
            }

            // Role bias
            if (role != TemplateRole.None && RoleSkillBias.TryGetValue(role, out var bias))
            {
                foreach (var p in bias)
                {
                    if (classPicks.Count >= need) break;
                    if (allowed.Contains(p, StringComparer.OrdinalIgnoreCase) && taken.Add(p))
                        classPicks.Add(p);
                }
            }

            // Fill randomly from remaining class options
            var remaining = allowed
                .Where(s => !taken.Contains(s))
                .OrderBy(_ => rng.Next())
                .ToList();
            foreach (var s in remaining)
            {
                if (classPicks.Count >= need) break;
                if (taken.Add(s))
                    classPicks.Add(s);
            }

            result.AddRange(classPicks);
            return result;
        }

        private static (List<string> Cantrips, List<string> Spells) PickSpells(
            string className,
            string[] preferredCantrips,
            string[] preferredSpells,
            TemplateRole role,
            Random rng)
        {
            if (!GameData.ClassData.TryGetValue(className, out var cd) || !cd.Spellcasting)
                return (new List<string>(), new List<string>());

            // Classes that don't start with spells known at level 1
            int cantripCount = cd.CantripsKnown;
            // Prepared casters still pick a sensible starting prep set for the sheet
            int spellCount = cd.SpellsKnown > 0
                ? cd.SpellsKnown
                : (className.Equals("Ranger", StringComparison.OrdinalIgnoreCase) ||
                   className.Equals("Paladin", StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : Math.Max(2, EstimatePreparedCount(className)));

            var classCantrips = SpellCatalog.GetForClass(className, maxLevel: 0)
                .Where(s => s.Level == 0)
                .Select(s => s.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var classL1 = SpellCatalog.GetForClass(className, maxLevel: 1)
                .Where(s => s.Level == 1)
                .Select(s => s.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var cantrips = SelectFromPool(
                classCantrips,
                preferredCantrips,
                role != TemplateRole.None && RoleCantripBias.TryGetValue(role, out var cb) ? cb : Array.Empty<string>(),
                cantripCount,
                rng);

            var spells = SelectFromPool(
                classL1,
                preferredSpells,
                role != TemplateRole.None && RoleSpellBias.TryGetValue(role, out var sb) ? sb : Array.Empty<string>(),
                spellCount,
                rng);

            return (cantrips, spells);
        }

        private static int EstimatePreparedCount(string className) =>
            className.ToLowerInvariant() switch
            {
                "cleric" or "druid" or "wizard" or "artificer" => 4,
                "sorcerer" or "warlock" or "bard" => 2,
                _ => 2
            };

        private static List<string> SelectFromPool(
            List<string> available,
            string[] preferred,
            string[] bias,
            int count,
            Random rng)
        {
            var result = new List<string>();
            if (count <= 0 || available.Count == 0) return result;

            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(string name)
            {
                if (result.Count >= count) return;
                var match = available.FirstOrDefault(a =>
                    a.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match != null && taken.Add(match))
                    result.Add(match);
            }

            foreach (var p in preferred ?? Array.Empty<string>())
                TryAdd(p);
            foreach (var p in bias ?? Array.Empty<string>())
                TryAdd(p);

            foreach (var a in available.OrderBy(_ => rng.Next()))
            {
                if (result.Count >= count) break;
                if (taken.Add(a))
                    result.Add(a);
            }

            return result;
        }

        private static string PickRaceBiasedToward(string ability, Random rng)
        {
            // Races with a fixed +2 or +1 to the ability score
            var scored = new List<(string Race, int Weight)>();
            foreach (var kv in GameData.RaceData)
            {
                int w = 1;
                if (kv.Value.AbilityBonuses != null &&
                    kv.Value.AbilityBonuses.TryGetValue(ability, out int bonus))
                    w += bonus * 4;
                // Subrace bonuses
                if (GameData.RaceSubraces.TryGetValue(kv.Key, out var subs))
                {
                    foreach (var s in subs)
                    {
                        if (s.AbilityBonus != null &&
                            s.AbilityBonus.TryGetValue(ability, out int sb))
                            w = Math.Max(w, 1 + sb * 4);
                    }
                }
                scored.Add((kv.Key, w));
            }

            int total = scored.Sum(s => s.Weight);
            int roll = rng.Next(total);
            int acc = 0;
            foreach (var (race, w) in scored)
            {
                acc += w;
                if (roll < acc) return race;
            }
            return scored[0].Race;
        }

        private static string PickSubrace(string race, string primaryAbility, Random rng)
        {
            if (!GameData.RaceSubraces.TryGetValue(race, out var subs) || subs.Count == 0)
                return "";

            var matching = subs
                .Where(s => s.AbilityBonus != null &&
                            s.AbilityBonus.ContainsKey(primaryAbility))
                .ToList();
            if (matching.Count > 0)
                return matching[rng.Next(matching.Count)].Name;
            return subs[rng.Next(subs.Count)].Name;
        }

        private static string PickBackgroundForRole(TemplateRole role, Random rng)
        {
            string[] prefs = role switch
            {
                TemplateRole.Support => new[] { "Acolyte", "Hermit", "Sage", "Folk Hero", "Noble", "Entertainer" },
                TemplateRole.Damage => new[] { "Soldier", "Criminal", "Outlander", "Urchin", "Spy", "Gladiator" },
                TemplateRole.Tank => new[] { "Soldier", "City Watch", "Knight", "Folk Hero", "Marine", "Noble" },
                _ => GameData.AllBackgrounds.ToArray()
            };

            var valid = prefs.Where(b => GameData.AllBackgrounds.Contains(b, StringComparer.OrdinalIgnoreCase)).ToList();
            if (valid.Count == 0)
                return PickRandom(GameData.AllBackgrounds, rng) ?? "Folk Hero";
            return valid[rng.Next(valid.Count)];
        }

        private static string NormalizeSubclass(string className, string subclass)
        {
            if (string.IsNullOrWhiteSpace(subclass)) return "";
            var names = GameData.GetSubclassNames(className);
            return names.FirstOrDefault(n => n.Equals(subclass.Trim(), StringComparison.OrdinalIgnoreCase))
                   ?? subclass.Trim();
        }

        private static string MakeName(Random rng) =>
            $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}";

        private static T? PickRandom<T>(IList<T> list, Random rng) =>
            list == null || list.Count == 0 ? default : list[rng.Next(list.Count)];

        // ==================== TEMPLATE CATALOG ====================

        private static List<CharacterBuildTemplate> BuildTemplates()
        {
            var list = new List<CharacterBuildTemplate>();

            // ---------- OPTIMIZED SUPPORT ----------
            // Drawn from widely cited 5e (2014) "meta" / high-tier builds (Tasha's, Xanathar's, SCAG, etc.):
            // Peace/Twilight/Order Cleric, Lore Bard, Shepherd Druid, Divine Soul, Celestial Warlock.

            list.Add(new CharacterBuildTemplate
            {
                Name = "Peace Cleric (Emboldening Bond)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Support,
                Class = "Cleric",
                Subclass = "Peace",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Acolyte",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Dexterity", "Charisma", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Insight", "Persuasion" },
                PreferredCantrips = new[] { "Guidance", "Toll the Dead", "Sacred Flame" },
                PreferredSpells = new[] { "Bless", "Healing Word", "Sanctuary", "Shield of Faith" },
                Description = "Meta S-tier support: Emboldening Bond is one of the strongest party buffs in 5e. Custom Lineage for an early feat (Resilient Con / Fey Touched / War Caster later)."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Twilight Cleric (Sanctuary Aura)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Support,
                Class = "Cleric",
                Subclass = "Twilight",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Acolyte",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Strength", "Dexterity", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Insight", "Medicine" },
                PreferredCantrips = new[] { "Guidance", "Toll the Dead", "Sacred Flame" },
                PreferredSpells = new[] { "Bless", "Healing Word", "Faerie Fire", "Shield of Faith" },
                Description = "Meta S-tier: Twilight Sanctuary free temp HP every round plus heavy armor and 300 ft darkvision. Staple optimized support."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Order Cleric (Voice of Authority)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Support,
                Class = "Cleric",
                Subclass = "Order",
                Race = "Variant Human",
                Subrace = "",
                Background = "Noble",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Strength", "Charisma", "Dexterity", "Intelligence" },
                PreferredSkills = new[] { "Persuasion", "Insight" },
                PreferredCantrips = new[] { "Guidance", "Sacred Flame", "Toll the Dead" },
                PreferredSpells = new[] { "Bless", "Healing Word", "Command", "Heroism" },
                Description = "Meta support with martials: heal/buff an ally → they get a free weapon attack (Voice of Authority). Core of many optimized parties."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Lore Bard (Magical Secrets)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Support,
                Class = "Bard",
                Subclass = "College of Lore",
                Race = "Half-Elf",
                Subrace = "",
                Background = "Entertainer",
                AbilityPriority = new[] { "Charisma", "Dexterity", "Constitution", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Persuasion", "Insight", "Perception" },
                PreferredCantrips = new[] { "Vicious Mockery", "Minor Illusion" },
                PreferredSpells = new[] { "Healing Word", "Faerie Fire", "Dissonant Whispers", "Sleep" },
                Description = "Meta full-caster support: Cutting Words, best skill coverage (Half-Elf), and Magical Secrets for top-tier spells (Counterspell, Fireball, etc.)."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Shepherd Druid (Spirit Totem)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Support,
                Class = "Druid",
                Subclass = "Circle of the Shepherd",
                Race = "Firbolg",
                Subrace = "",
                Background = "Hermit",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Dexterity", "Intelligence", "Charisma", "Strength" },
                PreferredSkills = new[] { "Perception", "Medicine" },
                PreferredCantrips = new[] { "Guidance", "Thorn Whip" },
                PreferredSpells = new[] { "Goodberry", "Entangle", "Faerie Fire", "Healing Word" },
                Description = "Meta summon/support: Unicorn Spirit heals, Bear/Hawk totems buff the party, and Conjure Animals remains one of 5e's strongest level 5 options."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Divine Soul Sorcerer (Twin Heal)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Support,
                Class = "Sorcerer",
                Subclass = "Divine Soul",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Hermit",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Insight", "Persuasion" },
                PreferredCantrips = new[] { "Mind Sliver", "Fire Bolt", "Minor Illusion", "Mage Hand" },
                PreferredSpells = new[] { "Bless", "Healing Word" },
                Description = "Meta flexible support: full cleric list + metamagic (Twinned Healing Word, Quickened Bless). Favored by the Gods as a safety net."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Celestial Warlock (Healing Light)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Support,
                Class = "Warlock",
                Subclass = "The Celestial",
                Race = "Aasimar",
                Subrace = "Protector Aasimar",
                Background = "Acolyte",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Arcana", "Religion" },
                PreferredCantrips = new[] { "Eldritch Blast", "Sacred Flame" },
                PreferredSpells = new[] { "Hex", "Cure Wounds" },
                Description = "Meta short-rest support: Healing Light dice + Eldritch Blast chassis. Strong when the party needs heals without a full cleric."
            });

            // ---------- OPTIMIZED DAMAGE ----------
            // Classic 5e DPR / nova meta: Hexblade, Gloom Stalker, Chronurgy/Evoker,
            // Clockwork Soul, Vengeance Paladin, Battle Master, Beast Barb, Phantom Rogue, Artillerist.

            list.Add(new CharacterBuildTemplate
            {
                Name = "Hexblade Warlock (SAD Blaster)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Warlock",
                Subclass = "The Hexblade",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Soldier",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Intimidation", "Arcana" },
                PreferredCantrips = new[] { "Eldritch Blast", "Booming Blade" },
                PreferredSpells = new[] { "Hex", "Shield" },
                Description = "Meta staple: Charisma-only attacks (Hex Warrior) + Eldritch Blast. Agonizing Blast / Hex / later multiclass dips define optimized warlock damage."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Gloom Stalker Ranger (First-Round Nova)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Ranger",
                Subclass = "Gloom Stalker",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Dexterity", "Wisdom", "Constitution", "Strength", "Intelligence", "Charisma" },
                PreferredSkills = new[] { "Stealth", "Perception", "Survival" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Meta ambush DPR: Dread Ambusher extra attack on round 1, darkvision immunity, and Sharpshooter/Crossbow Expert feat path. Core of many optimized martial stacks."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Chronurgy Wizard (Control Nuke)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Wizard",
                Subclass = "Chronurgy",
                Race = "Elf",
                Subrace = "High Elf",
                Background = "Sage",
                AbilityPriority = new[] { "Intelligence", "Constitution", "Dexterity", "Wisdom", "Charisma", "Strength" },
                PreferredSkills = new[] { "Arcana", "Investigation" },
                PreferredCantrips = new[] { "Fire Bolt", "Mind Sliver", "Minor Illusion" },
                PreferredSpells = new[] { "Magic Missile", "Shield", "Absorb Elements", "Find Familiar" },
                Description = "Meta caster (EGtW): Chronal Shift / Arcane Abeyance plus full wizard list. Favored over pure Evoker when control + save-or-suck damage is the goal."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Clockwork Soul Sorcerer (Reliable Nova)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Sorcerer",
                Subclass = "Clockwork Soul",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Sage",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Arcana", "Persuasion" },
                PreferredCantrips = new[] { "Fire Bolt", "Mind Sliver", "Minor Illusion", "Mage Hand" },
                PreferredSpells = new[] { "Shield", "Absorb Elements" },
                Description = "Meta blaster/controller: Restore Balance strips advantage/disadvantage; clockwork spells + metamagic (Quickened / Twinned) for consistent high-impact turns."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Vengeance Paladin (Nova Smite)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Paladin",
                Subclass = "Oath of Vengeance",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Soldier",
                AbilityPriority = new[] { "Strength", "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Intimidation" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Meta nova martial: Vow of Enmity advantage + Divine Smite burst. Great Weapon Master / PAM feat path is a standard optimized damage route."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Battle Master Fighter (Precision DPR)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Fighter",
                Subclass = "Battle Master",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Soldier",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Perception" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Meta martial chassis: Action Surge + Superiority Dice (Trip, Precision, Menacing). Highest baseline attack economy in 5e with GWM/SS feats."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Beast Barbarian (Multiattack Rage)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Barbarian",
                Subclass = "Path of the Beast",
                Race = "Half-Orc",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Perception" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Meta rage striker: Form of the Beast claws give bonus multiattack while raging. Pairs with Reckless Attack and Half-Orc crits for high sustained melee DPR."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Phantom Rogue (Soul Trinkets)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Rogue",
                Subclass = "Phantom",
                Race = "Halfling",
                Subrace = "Lightfoot Halfling",
                Background = "Criminal",
                AbilityPriority = new[] { "Dexterity", "Constitution", "Wisdom", "Charisma", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Stealth", "Perception", "Acrobatics", "Sleight of Hand" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Meta rogue (over Assassin): Wails from the Grave adds extra Sneak Attack damage to a second target. Reliable single-target + splash without setup-dependent Assassinate."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Artillerist Artificer (Eldritch Cannon)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Damage,
                Class = "Artificer",
                Subclass = "Artillerist",
                Race = "Gnome",
                Subrace = "Rock Gnome",
                Background = "Sage",
                AbilityPriority = new[] { "Intelligence", "Constitution", "Dexterity", "Wisdom", "Charisma", "Strength" },
                PreferredSkills = new[] { "Arcana", "Investigation" },
                PreferredCantrips = new[] { "Fire Bolt", "Mending" },
                PreferredSpells = new[] { "Shield", "Catapult", "Absorb Elements", "Faerie Fire" },
                Description = "Meta Int damage: Eldritch Cannon (Force Ballista / Flamethrower) bonus-action damage every turn plus infusions and Shield. Strong from level 3 onward."
            });

            // ---------- OPTIMIZED TANK ----------
            // Meta frontline: Bear Totem, Ancestral Guardian, Twilight Cleric, Ancients Paladin,
            // Armorer Guardian, Rune Knight, Moon Druid, Forge Cleric.

            list.Add(new CharacterBuildTemplate
            {
                Name = "Bear Totem Barbarian (Damage Sponge)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Tank,
                Class = "Barbarian",
                Subclass = "Path of the Totem Warrior",
                Race = "Half-Orc",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Survival" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Classic 5e tank meta: Bear Totem = resistance to all damage except psychic while raging. Combine with Relentless Endurance (Half-Orc) and Reckless Attack."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Ancestral Guardian Barbarian (Redirect)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Tank,
                Class = "Barbarian",
                Subclass = "Path of the Ancestral Guardian",
                Race = "Half-Orc",
                Subrace = "",
                Background = "Soldier",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Intimidation" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Meta aggro tank: hit a foe → they have disadvantage on attacks vs allies and deal half damage to them. Best pure 'protect the party' barbarian path."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Twilight Cleric Tank (Temp HP Engine)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Tank,
                Class = "Cleric",
                Subclass = "Twilight",
                Race = "Dwarf",
                Subrace = "Hill Dwarf",
                Background = "Acolyte",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Strength", "Dexterity", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Insight", "Medicine" },
                PreferredCantrips = new[] { "Guidance", "Toll the Dead", "Sacred Flame" },
                PreferredSpells = new[] { "Bless", "Shield of Faith", "Healing Word", "Sanctuary" },
                Description = "Meta frontline caster-tank: heavy armor, Steps of Night, and Twilight Sanctuary flooding the party with temp HP. Often ranked above pure martial tanks."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Ancients Paladin (Aura of Warding)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Tank,
                Class = "Paladin",
                Subclass = "Oath of the Ancients",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Noble",
                AbilityPriority = new[] { "Strength", "Charisma", "Constitution", "Wisdom", "Dexterity", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Persuasion" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Meta aura tank: Aura of Warding halves spell damage for nearby allies. Lay on Hands + heavy armor + smites make it the premier anti-magic frontliner."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Armorer Artificer (Guardian Model)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Tank,
                Class = "Artificer",
                Subclass = "Armorer",
                Race = "Custom Lineage",
                Subrace = "",
                Background = "Sage",
                AbilityPriority = new[] { "Intelligence", "Constitution", "Dexterity", "Wisdom", "Strength", "Charisma" },
                PreferredSkills = new[] { "Arcana", "Investigation" },
                PreferredCantrips = new[] { "Fire Bolt", "Mending" },
                PreferredSpells = new[] { "Shield", "Absorb Elements", "Faerie Fire", "Cure Wounds" },
                Description = "Meta Int tank: Guardian Armor thunder gauntlets impose disadvantage if foes attack someone else; infusions (Enhanced Defense, Shield) stack AC. Fully SAD on Intelligence."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Rune Knight Fighter (Giant Tank)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Tank,
                Class = "Fighter",
                Subclass = "Rune Knight",
                Race = "Dwarf",
                Subrace = "Mountain Dwarf",
                Background = "Soldier",
                AbilityPriority = new[] { "Strength", "Constitution", "Wisdom", "Dexterity", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Perception" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Meta tank/control martial: Giant's Might size/damage, Cloud/Fire/Stone runes for lockdown and defense. Outperforms Champion; rivals Cavalier for modern tables."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Moon Druid (Wild Shape HP)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Tank,
                Class = "Druid",
                Subclass = "Circle of the Moon",
                Race = "Firbolg",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Dexterity", "Intelligence", "Charisma", "Strength" },
                PreferredSkills = new[] { "Perception", "Survival" },
                PreferredCantrips = new[] { "Shillelagh", "Thorn Whip" },
                PreferredSpells = new[] { "Goodberry", "Entangle", "Faerie Fire", "Absorb Elements" },
                Description = "Meta early-game tank: Combat Wild Shape turns beast HP into ablative armor (brown bear, elemental forms later). Still one of the tankiest options at low–mid levels."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Forge Cleric (Blessing of the Forge)",
                Category = TemplateCategory.Optimized,
                Role = TemplateRole.Tank,
                Class = "Cleric",
                Subclass = "Forge",
                Race = "Dwarf",
                Subrace = "Mountain Dwarf",
                Background = "Folk Hero",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Strength", "Dexterity", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Insight", "Athletics" },
                PreferredCantrips = new[] { "Guidance", "Sacred Flame", "Word of Radiance" },
                PreferredSpells = new[] { "Shield of Faith", "Bless", "Absorb Elements", "Cure Wounds" },
                Description = "Meta AC tank: heavy armor + Blessing of the Forge (+1 weapon/armor) + Shield of Faith for extreme AC. Strong simple frontliner when Twilight isn't available."
            });

            FixBackgrounds(list);

            // ---------- GENERAL SUPPORT ----------
            list.Add(new CharacterBuildTemplate
            {
                Name = "Village Healer",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Cleric",
                Subclass = "Life",
                Race = "Human",
                Subrace = "",
                Background = "Folk Hero",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Charisma", "Strength", "Dexterity", "Intelligence" },
                PreferredSkills = new[] { "Medicine", "Persuasion" },
                PreferredCantrips = new[] { "Guidance", "Sacred Flame", "Light" },
                PreferredSpells = new[] { "Cure Wounds", "Bless", "Detect Magic", "Sanctuary" },
                Description = "Friendly Human Life Cleric — reliable party healer."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Wandering Minstrel",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Bard",
                Subclass = "College of Glamour",
                Race = "Half-Elf",
                Subrace = "",
                Background = "Entertainer",
                AbilityPriority = new[] { "Charisma", "Dexterity", "Constitution", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Performance", "Persuasion", "Insight" },
                PreferredCantrips = new[] { "Vicious Mockery", "Friends" },
                PreferredSpells = new[] { "Healing Word", "Charm Person", "Heroism", "Sleep" },
                Description = "Glamour Bard: charms, heals, and party face."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Grove Tender",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Druid",
                Subclass = "Circle of Dreams",
                Race = "Elf",
                Subrace = "Wood Elf",
                Background = "Hermit",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Dexterity", "Intelligence", "Charisma", "Strength" },
                PreferredSkills = new[] { "Medicine", "Animal Handling" },
                PreferredCantrips = new[] { "Druidcraft", "Guidance" },
                PreferredSpells = new[] { "Goodberry", "Healing Word", "Entangle", "Speak with Animals" },
                Description = "Dreams Druid: soft heals and nature utility."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Oathbound Protector",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Paladin",
                Subclass = "Oath of Devotion",
                Race = "Human",
                Subrace = "",
                Background = "Knight",
                AbilityPriority = new[] { "Strength", "Charisma", "Constitution", "Wisdom", "Dexterity", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Persuasion" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Devotion Paladin: lay on hands and buff the front line."
            });

            // ---------- GENERAL DAMAGE ----------
            list.Add(new CharacterBuildTemplate
            {
                Name = "Street Duelist",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Rogue",
                Subclass = "Swashbuckler",
                Race = "Human",
                Subrace = "",
                Background = "Criminal",
                AbilityPriority = new[] { "Dexterity", "Charisma", "Constitution", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Acrobatics", "Deception", "Stealth", "Persuasion" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Swashbuckler Rogue: mobile single-target striker."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Battle Mage",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Wizard",
                Subclass = "Evocation",
                Race = "Human",
                Subrace = "",
                Background = "Sage",
                AbilityPriority = new[] { "Intelligence", "Constitution", "Dexterity", "Wisdom", "Charisma", "Strength" },
                PreferredSkills = new[] { "Arcana", "History" },
                PreferredCantrips = new[] { "Fire Bolt", "Ray of Frost", "Light" },
                PreferredSpells = new[] { "Magic Missile", "Thunderwave", "Shield", "Detect Magic" },
                Description = "Straightforward Human Evoker for reliable damage spells."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Wild Magic Daredevil",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Sorcerer",
                Subclass = "Wild Magic",
                Race = "Tiefling",
                Subrace = "",
                Background = "Entertainer",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Persuasion", "Deception" },
                PreferredCantrips = new[] { "Fire Bolt", "Shocking Grasp", "Prestidigitation", "Light" },
                PreferredSpells = new[] { "Chaos Bolt", "Witch Bolt" },
                Description = "Wild Magic Sorcerer: flashy and unpredictable damage."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Hunter Scout",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Ranger",
                Subclass = "Hunter",
                Race = "Human",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Dexterity", "Wisdom", "Constitution", "Strength", "Intelligence", "Charisma" },
                PreferredSkills = new[] { "Perception", "Survival", "Stealth" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Classic Hunter Ranger: versatile ranged and melee damage."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Fiendish Blaster",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Warlock",
                Subclass = "The Fiend",
                Race = "Human",
                Subrace = "",
                Background = "Urchin",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Deception", "Intimidation" },
                PreferredCantrips = new[] { "Eldritch Blast", "Minor Illusion" },
                PreferredSpells = new[] { "Hex", "Burning Hands" },
                Description = "Fiend Warlock: Eldritch Blast and temporary HP on kills."
            });

            // ---------- GENERAL TANK ----------
            list.Add(new CharacterBuildTemplate
            {
                Name = "City Guard",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Fighter",
                Subclass = "Champion",
                Race = "Human",
                Subrace = "",
                Background = "City Watch",
                AbilityPriority = new[] { "Strength", "Constitution", "Wisdom", "Dexterity", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Insight" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Champion Fighter: simple, durable frontliner."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Crown Knight",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Paladin",
                Subclass = "Oath of the Crown",
                Race = "Human",
                Subrace = "",
                Background = "Soldier",
                AbilityPriority = new[] { "Strength", "Constitution", "Charisma", "Wisdom", "Dexterity", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Intimidation" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Crown Paladin: compel foes and protect allies."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Berserker Wall",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Barbarian",
                Subclass = "Path of the Berserker",
                Race = "Human",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Survival" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Berserker Barbarian: high HP and reckless defense through offense."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Forge Warden",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Cleric",
                Subclass = "Forge",
                Race = "Dwarf",
                Subrace = "Mountain Dwarf",
                Background = "Folk Hero",
                AbilityPriority = new[] { "Wisdom", "Strength", "Constitution", "Charisma", "Dexterity", "Intelligence" },
                PreferredSkills = new[] { "Medicine", "Insight" },
                PreferredCantrips = new[] { "Sacred Flame", "Guidance", "Mending" },
                PreferredSpells = new[] { "Shield of Faith", "Cure Wounds", "Bless", "Identify" },
                Description = "Forge Cleric: armor blessings and frontline presence."
            });

            // Validate races exist — swap missing exotic races to common ones
            return list.Select(SanitizeTemplate).ToList();
        }

        private static void FixBackgrounds(List<CharacterBuildTemplate> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (GameData.AllBackgrounds.Contains(t.Background, StringComparer.OrdinalIgnoreCase))
                    continue;

                string fallback = t.Role switch
                {
                    TemplateRole.Support => "Acolyte",
                    TemplateRole.Damage => "Soldier",
                    TemplateRole.Tank => "Soldier",
                    _ => "Folk Hero"
                };
                list[i] = CloneWithBackground(t, fallback);
            }
        }

        private static CharacterBuildTemplate CloneWithBackground(CharacterBuildTemplate t, string bg) =>
            new()
            {
                Name = t.Name,
                Category = t.Category,
                Role = t.Role,
                Class = t.Class,
                Subclass = t.Subclass,
                Race = t.Race,
                Subrace = t.Subrace,
                Background = bg,
                AbilityPriority = t.AbilityPriority,
                PreferredSkills = t.PreferredSkills,
                PreferredCantrips = t.PreferredCantrips,
                PreferredSpells = t.PreferredSpells,
                Description = t.Description
            };

        private static CharacterBuildTemplate SanitizeTemplate(CharacterBuildTemplate t)
        {
            string race = t.Race;
            string subrace = t.Subrace ?? "";

            // Hill/Mountain dwarf, Lightfoot, etc. sometimes written as race name
            if (!GameData.RaceData.ContainsKey(race))
            {
                // Try as subrace of a known parent
                foreach (var kv in GameData.RaceSubraces)
                {
                    var match = kv.Value.FirstOrDefault(s =>
                        s.Name.Equals(race, StringComparison.OrdinalIgnoreCase) ||
                        s.Name.Equals(subrace, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        race = kv.Key;
                        subrace = match.Name;
                        break;
                    }
                }
            }

            if (!GameData.RaceData.ContainsKey(race))
            {
                race = t.Role switch
                {
                    TemplateRole.Tank => "Dwarf",
                    TemplateRole.Damage => "Human",
                    _ => "Human"
                };
                subrace = GameData.RaceSubraces.TryGetValue(race, out var s) && s.Count > 0
                    ? s[0].Name
                    : "";
            }

            if (!string.IsNullOrEmpty(subrace) &&
                GameData.RaceSubraces.TryGetValue(race, out var subs))
            {
                var sm = subs.FirstOrDefault(x => x.Name.Equals(subrace, StringComparison.OrdinalIgnoreCase));
                if (sm == null)
                {
                    // Fuzzy: "Lightfoot Halfling" vs list
                    sm = subs.FirstOrDefault(x =>
                        x.Name.Contains(subrace, StringComparison.OrdinalIgnoreCase) ||
                        subrace.Contains(x.Name, StringComparison.OrdinalIgnoreCase));
                    subrace = sm?.Name ?? (subs.Count > 0 ? subs[0].Name : "");
                }
                else
                {
                    subrace = sm.Name;
                }
            }

            // Firbolg / other missing races
            if (!GameData.RaceData.ContainsKey(race))
            {
                race = "Half-Elf";
                subrace = "";
            }

            string bg = t.Background;
            if (!GameData.AllBackgrounds.Contains(bg, StringComparer.OrdinalIgnoreCase))
                bg = "Folk Hero";

            // Lightfoot name normalization
            if (race.Equals("Halfling", StringComparison.OrdinalIgnoreCase) &&
                GameData.RaceSubraces.TryGetValue("Halfling", out var hsubs))
            {
                if (string.IsNullOrEmpty(subrace) ||
                    !hsubs.Any(s => s.Name.Equals(subrace, StringComparison.OrdinalIgnoreCase)))
                {
                    subrace = hsubs.FirstOrDefault(s =>
                                  s.Name.Contains("Lightfoot", StringComparison.OrdinalIgnoreCase))?.Name
                              ?? hsubs[0].Name;
                }
            }

            return new CharacterBuildTemplate
            {
                Name = t.Name,
                Category = t.Category,
                Role = t.Role,
                Class = t.Class,
                Subclass = t.Subclass,
                Race = race,
                Subrace = subrace,
                Background = bg,
                AbilityPriority = t.AbilityPriority,
                PreferredSkills = t.PreferredSkills,
                PreferredCantrips = t.PreferredCantrips,
                PreferredSpells = t.PreferredSpells,
                Description = t.Description
            };
        }
    }
}
