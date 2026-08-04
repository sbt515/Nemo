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

    /// <summary>
    /// One class contribution on a build template (single-class or multiclass).
    /// </summary>
    public sealed class TemplateClassLevel
    {
        public string ClassName { get; init; } = "";
        public string Subclass { get; init; } = "";
        public int Levels { get; init; } = 1;
    }

    /// <summary>A named race/class/background package used by optimized &amp; general generation.</summary>
    public sealed class CharacterBuildTemplate
    {
        public string Name { get; init; } = "";
        public TemplateCategory Category { get; init; }
        public TemplateRole Role { get; init; }
        /// <summary>Primary / starting class (also first entry when <see cref="ClassLevels"/> is set).</summary>
        public string Class { get; init; } = "";
        public string Subclass { get; init; } = "";
        public string Race { get; init; } = "";
        public string Subrace { get; init; } = "";
        public string Background { get; init; } = "";
        /// <summary>
        /// Total character level for generation. When <see cref="ClassLevels"/> is non-empty,
        /// generation uses the sum of those levels (this field is kept in sync for display).
        /// Default 1 preserves legacy single-class level-1 templates.
        /// </summary>
        public int TargetLevel { get; init; } = 1;
        /// <summary>
        /// Multiclass / multi-level breakdown. Empty means single-class using
        /// <see cref="Class"/> / <see cref="Subclass"/> at <see cref="TargetLevel"/>.
        /// </summary>
        public TemplateClassLevel[] ClassLevels { get; init; } = Array.Empty<TemplateClassLevel>();
        /// <summary>Ability names in priority order (highest score first).</summary>
        public string[] AbilityPriority { get; init; } = Array.Empty<string>();
        public string[] PreferredSkills { get; init; } = Array.Empty<string>();
        public string[] PreferredCantrips { get; init; } = Array.Empty<string>();
        public string[] PreferredSpells { get; init; } = Array.Empty<string>();
        /// <summary>
        /// Optional cantrip → class ownership for multiclass (e.g. "Fire Bolt" → "Wizard").
        /// </summary>
        public Dictionary<string, string>? CantripClassAssignments { get; init; }
        /// <summary>
        /// Optional ASI/feat decisions captured from a saved character.
        /// When empty, generation auto-applies ASI along <see cref="AbilityPriority"/>.
        /// </summary>
        public AsiOrFeatDecision[] AsiOrFeatDecisions { get; init; } = Array.Empty<AsiOrFeatDecision>();
        /// <summary>Preferred fighting style names (Fighter / Paladin / Ranger).</summary>
        public string[] PreferredFightingStyles { get; init; } = Array.Empty<string>();
        /// <summary>Preferred Warlock eldritch invocations.</summary>
        public string[] PreferredEldritchInvocations { get; init; } = Array.Empty<string>();
        /// <summary>Preferred Sorcerer metamagic options.</summary>
        public string[] PreferredMetamagic { get; init; } = Array.Empty<string>();
        /// <summary>Preferred Warlock Pact Boon (Chain / Blade / Tome), if any.</summary>
        public string PreferredPactBoon { get; init; } = "";
        /// <summary>Fighting style from Fighting Initiate feat, if any.</summary>
        public string PreferredFightingInitiateStyle { get; init; } = "";
        /// <summary>
        /// When set, the template picker can offer these character levels (e.g. 1, 3, 5, 8 for General).
        /// Empty means derive from category / fixed <see cref="TargetLevel"/> / <see cref="ClassLevels"/>.
        /// </summary>
        public int[] SupportedLevels { get; init; } = Array.Empty<int>();
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
    /// Builds complete characters from themed templates or pure randomness.
    /// Supports single-class and multiclass builds at levels 1–20 (Custom templates
    /// capture the source character's full class breakdown; built-ins default to level 1).
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
        /// Common campaign breakpoints for General and Optimized built-in template lines.
        /// </summary>
        public static readonly int[] GeneralLevelLadder = { 1, 3, 5, 8 };

        /// <summary>Alias for the shared built-in tier ladder (1, 3, 5, 8).</summary>
        public static readonly int[] BuiltInLevelLadder = GeneralLevelLadder;

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
                    "Optimized: 5e classics and meta (Peace/Twilight, Hexadin, Sorlock, Sorcadin, Gloomstalker Assassin, Bear-Barian, Lifeberry, etc.). Generate each kit at level 1, 3, 5, or 8 via the Level dropdown.",
                TemplateCategory.General =>
                    "General: popular single-class kits that are reliable and easy to play. Each build can be generated at level 1, 3, 5, or 8 via the Level dropdown.",
                TemplateCategory.Random =>
                    "Random: rolls within a role (or pure chaos with True Random) at the target level you set (1–20).",
                TemplateCategory.Custom =>
                    "Custom: your saved builds (including level and multiclass), captured from characters you created. Reuse them any time.",
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
        /// General kits keep one row each; multi-level choice is via the picker's Level dropdown
        /// (<see cref="GetSupportedLevels"/>).
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
        /// Levels the user may pick for this template (picker Level dropdown).
        /// Built-in Optimized and General kits default to <see cref="BuiltInLevelLadder"/> (1, 3, 5, 8).
        /// Concrete applied tiers (SupportedLevels length 1) and Custom saves stay fixed.
        /// </summary>
        public static IReadOnlyList<int> GetSupportedLevels(CharacterBuildTemplate? template)
        {
            if (template == null)
                return new[] { 1 };

            if (template.SupportedLevels is { Length: > 0 })
            {
                return template.SupportedLevels
                    .Where(l => l >= 1 && l <= 20)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToArray();
            }

            // Built-in Optimized / General (single-class or multiclass blueprint) → L1/3/5/8
            if (template.Category is TemplateCategory.General or TemplateCategory.Optimized)
                return BuiltInLevelLadder;

            // Custom / other: fixed to current total
            int fixedLevel = GetTemplateTotalLevel(template);
            return new[] { Math.Clamp(fixedLevel > 0 ? fixedLevel : 1, 1, 20) };
        }

        /// <summary>
        /// True when the picker should show a Level dropdown (more than one supported tier).
        /// </summary>
        public static bool HasLevelChoices(CharacterBuildTemplate? template) =>
            GetSupportedLevels(template).Count > 1;

        /// <summary>
        /// Resolve a template to a concrete level for generation (clones ladder kits to that tier).
        /// Multiclass blueprints scale class levels along their level-up order.
        /// </summary>
        public static CharacterBuildTemplate ApplyTemplateLevel(
            CharacterBuildTemplate template,
            int level)
        {
            ArgumentNullException.ThrowIfNull(template);
            var supported = GetSupportedLevels(template);
            int lvl = supported.Contains(level)
                ? level
                : supported[0];

            if (supported.Count == 1 &&
                template.ClassLevels is { Length: > 0 } &&
                template.ClassLevels.Sum(e => e.Levels) == lvl &&
                template.TargetLevel == lvl)
            {
                return template;
            }

            return CloneTemplateAtLevel(template, StripLevelSuffix(template.Name), lvl);
        }

        /// <summary>Remove a trailing " (L#)" suffix from a template display name.</summary>
        public static string StripLevelSuffix(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            string n = name.Trim();
            // "Village Healer (L5)" or "Village Healer (L12)"
            int open = n.LastIndexOf(" (L", StringComparison.OrdinalIgnoreCase);
            if (open <= 0 || !n.EndsWith(')'))
                return n;
            string mid = n.Substring(open + 3, n.Length - open - 4);
            if (int.TryParse(mid, out _))
                return n.Substring(0, open).TrimEnd();
            return n;
        }

        /// <summary>
        /// Clone a template at a specific character level (ClassLevels + TargetLevel + name tag).
        /// Single-class: all levels in the primary class.
        /// Multiclass blueprint: levels are spent in blueprint order (e.g. Warlock 1 then Paladin 6
        /// at L5 → Warlock 1 / Paladin 4).
        /// </summary>
        public static CharacterBuildTemplate CloneTemplateAtLevel(
            CharacterBuildTemplate source,
            string? baseName,
            int level)
        {
            ArgumentNullException.ThrowIfNull(source);
            int lvl = Math.Clamp(level <= 0 ? 1 : level, 1, 20);
            string nameRoot = string.IsNullOrWhiteSpace(baseName)
                ? StripLevelSuffix(source.Name)
                : baseName.Trim();
            if (string.IsNullOrEmpty(nameRoot))
                nameRoot = source.Name?.Trim() ?? "Build";

            var classLevels = BuildClassLevelsAtTotal(source, lvl);

            string className = classLevels.Length > 0
                ? classLevels[0].ClassName
                : (source.Class ?? "").Trim();
            string subclass = classLevels.Length > 0
                ? (classLevels[0].Subclass ?? "")
                : (source.Subclass ?? "").Trim();

            return new CharacterBuildTemplate
            {
                Name = $"{nameRoot} (L{lvl})",
                Category = source.Category,
                Role = source.Role,
                Class = className,
                Subclass = subclass,
                Race = source.Race ?? "",
                Subrace = source.Subrace ?? "",
                Background = source.Background ?? "",
                TargetLevel = lvl,
                ClassLevels = classLevels,
                AbilityPriority = source.AbilityPriority ?? Array.Empty<string>(),
                PreferredSkills = source.PreferredSkills ?? Array.Empty<string>(),
                PreferredCantrips = source.PreferredCantrips ?? Array.Empty<string>(),
                PreferredSpells = source.PreferredSpells ?? Array.Empty<string>(),
                CantripClassAssignments = source.CantripClassAssignments,
                AsiOrFeatDecisions = source.AsiOrFeatDecisions ?? Array.Empty<AsiOrFeatDecision>(),
                PreferredFightingStyles = source.PreferredFightingStyles ?? Array.Empty<string>(),
                PreferredEldritchInvocations = source.PreferredEldritchInvocations ?? Array.Empty<string>(),
                PreferredMetamagic = source.PreferredMetamagic ?? Array.Empty<string>(),
                PreferredPactBoon = source.PreferredPactBoon ?? "",
                PreferredFightingInitiateStyle = source.PreferredFightingInitiateStyle ?? "",
                // Concrete tier — no further multi-level choices
                SupportedLevels = new[] { lvl },
                Description = source.Description ?? ""
            };
        }

        /// <summary>
        /// Build ClassLevels for <paramref name="targetTotal"/> from a template blueprint.
        /// Multiclass: spend levels in blueprint order up to the original package total, then
        /// extra levels go into the last class (or first if scaling above blueprint).
        /// </summary>
        public static TemplateClassLevel[] BuildClassLevelsAtTotal(
            CharacterBuildTemplate source,
            int targetTotal)
        {
            int target = Math.Clamp(targetTotal <= 0 ? 1 : targetTotal, 1, 20);

            var blueprint = (source.ClassLevels ?? Array.Empty<TemplateClassLevel>())
                .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                .Select(e => new TemplateClassLevel
                {
                    ClassName = e.ClassName.Trim(),
                    Subclass = e.Subclass?.Trim() ?? "",
                    Levels = Math.Clamp(e.Levels, 1, 20)
                })
                .ToList();

            // Single-class kit (no ClassLevels): all levels in Class / Subclass
            if (blueprint.Count == 0)
            {
                string className = (source.Class ?? "").Trim();
                if (string.IsNullOrEmpty(className))
                    className = "Fighter";
                string subclass = (source.Subclass ?? "").Trim();
                return new[]
                {
                    new TemplateClassLevel
                    {
                        ClassName = className,
                        Subclass = subclass,
                        Levels = target
                    }
                };
            }

            if (blueprint.Count == 1)
            {
                return new[]
                {
                    new TemplateClassLevel
                    {
                        ClassName = blueprint[0].ClassName,
                        Subclass = blueprint[0].Subclass,
                        Levels = target
                    }
                };
            }

            // Multiclass: expand blueprint into level-up order, take first `target` steps
            var progression = new List<(string ClassName, string Subclass)>();
            foreach (var e in blueprint)
            {
                for (int i = 0; i < e.Levels; i++)
                    progression.Add((e.ClassName, e.Subclass));
            }

            if (progression.Count == 0)
            {
                return new[]
                {
                    new TemplateClassLevel
                    {
                        ClassName = blueprint[0].ClassName,
                        Subclass = blueprint[0].Subclass,
                        Levels = target
                    }
                };
            }

            // If target exceeds blueprint total, extra levels go into the last class in the package
            var taken = new List<(string ClassName, string Subclass)>();
            for (int i = 0; i < target; i++)
            {
                if (i < progression.Count)
                    taken.Add(progression[i]);
                else
                    taken.Add(progression[^1]);
            }

            // Collapse consecutive same class (order preserved as first-seen)
            var collapsed = new List<TemplateClassLevel>();
            foreach (var step in taken)
            {
                if (collapsed.Count > 0 &&
                    collapsed[^1].ClassName.Equals(step.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    var prev = collapsed[^1];
                    collapsed[^1] = new TemplateClassLevel
                    {
                        ClassName = prev.ClassName,
                        // Prefer non-empty subclass from blueprint
                        Subclass = string.IsNullOrEmpty(prev.Subclass) ? step.Subclass : prev.Subclass,
                        Levels = prev.Levels + 1
                    };
                }
                else
                {
                    collapsed.Add(new TemplateClassLevel
                    {
                        ClassName = step.ClassName,
                        Subclass = step.Subclass,
                        Levels = 1
                    });
                }
            }

            return collapsed.ToArray();
        }

        /// <summary>
        /// Build a Custom template from the current character sheet values.
        /// Captures multiclass levels, total level, ASI decisions, and ability priority
        /// (derived from base scores, highest first).
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

            var resolvedLevels = LevelUpCalculator.GetClassLevelsFromCharacter(character);
            if (resolvedLevels.Count == 0 && !string.IsNullOrWhiteSpace(character.Class))
            {
                resolvedLevels = new List<ClassLevelEntry>
                {
                    new(character.Class.Trim(), Math.Max(1, character.Level), character.Subclass ?? "")
                };
            }

            string className = resolvedLevels.Count > 0
                ? (resolvedLevels[0].ClassName ?? "").Trim()
                : (character.Class ?? "").Trim();
            string subclass = resolvedLevels.Count > 0
                ? (resolvedLevels[0].Subclass ?? "").Trim()
                : (character.Subclass ?? "").Trim();
            string race = (character.Race ?? "").Trim();
            string subrace = (character.Subrace ?? "").Trim();
            string background = (character.Background ?? "").Trim();

            int levelSum = resolvedLevels.Sum(e => e.Levels);
            int targetLevel = Math.Clamp(
                levelSum > 0 ? levelSum : Math.Max(1, character.Level),
                1, 20);

            var templateClassLevels = resolvedLevels
                .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                .Select(e => new TemplateClassLevel
                {
                    ClassName = e.ClassName.Trim(),
                    Subclass = (e.Subclass ?? "").Trim(),
                    Levels = Math.Clamp(e.Levels, 1, 20)
                })
                .ToArray();

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
            // Level1Spells holds all selected leveled spells (1st–9th) on the sheet
            var spells = (character.Level1Spells ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Dictionary<string, string>? cantripAssign = null;
            if (character.CantripClassAssignments is { Count: > 0 })
            {
                cantripAssign = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in character.CantripClassAssignments)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))
                        continue;
                    cantripAssign[kv.Key.Trim()] = kv.Value.Trim();
                }
                if (cantripAssign.Count == 0)
                    cantripAssign = null;
            }

            var asi = (character.AsiOrFeatDecisions ?? new List<AsiOrFeatDecision>())
                .Where(d => d != null && !string.IsNullOrWhiteSpace(d.ClassName) && d.ClassLevel >= 1)
                .Select(CloneAsiDecision)
                .ToArray();

            var fightingStyles = (character.FightingStyles ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var invocations = (character.EldritchInvocations ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var metamagic = (character.MetamagicOptions ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string pactBoon = (character.WarlockPactBoon ?? "").Trim();
            string fightInit = (character.FightingInitiateStyle ?? "").Trim();

            string desc = (description ?? "").Trim();
            if (string.IsNullOrEmpty(desc))
            {
                string kit = FormatClassLevelsLine(templateClassLevels, className, subclass, targetLevel);
                desc = $"Saved custom build (L{targetLevel}): {race}" +
                       (string.IsNullOrEmpty(subrace) ? "" : $" ({subrace})") +
                       $" · {kit}" +
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
                TargetLevel = targetLevel,
                ClassLevels = templateClassLevels,
                AbilityPriority = priority,
                PreferredSkills = skills,
                PreferredCantrips = cantrips,
                PreferredSpells = spells,
                CantripClassAssignments = cantripAssign,
                AsiOrFeatDecisions = asi,
                PreferredFightingStyles = fightingStyles,
                PreferredEldritchInvocations = invocations,
                PreferredMetamagic = metamagic,
                PreferredPactBoon = pactBoon,
                PreferredFightingInitiateStyle = fightInit,
                Description = desc
            };
        }

        private static AsiOrFeatDecision CloneAsiDecision(AsiOrFeatDecision d) => new()
        {
            ClassName = d.ClassName ?? "",
            ClassLevel = d.ClassLevel,
            Kind = d.Kind,
            AbilityPlusOneA = d.AbilityPlusOneA ?? "",
            AbilityPlusOneB = d.AbilityPlusOneB ?? ""
        };

        /// <summary>
        /// Resolve class levels for a template: explicit <see cref="CharacterBuildTemplate.ClassLevels"/>
        /// when present, otherwise single-class at <see cref="CharacterBuildTemplate.TargetLevel"/>.
        /// </summary>
        public static List<ClassLevelEntry> ResolveTemplateClassLevels(CharacterBuildTemplate template)
        {
            if (template == null)
                return new List<ClassLevelEntry> { new("Fighter", 1, "") };

            if (template.ClassLevels is { Length: > 0 })
            {
                var fromTemplate = template.ClassLevels
                    .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                    .Select(e => new ClassLevelEntry(
                        e.ClassName.Trim(),
                        Math.Clamp(e.Levels, 1, 20),
                        string.IsNullOrWhiteSpace(e.Subclass) ? null : e.Subclass.Trim()))
                    .ToList();
                if (fromTemplate.Count > 0)
                    return fromTemplate;
            }

            string className = string.IsNullOrWhiteSpace(template.Class) ? "Fighter" : template.Class.Trim();
            int level = template.TargetLevel > 0 ? Math.Clamp(template.TargetLevel, 1, 20) : 1;
            string? subclass = string.IsNullOrWhiteSpace(template.Subclass) ? null : template.Subclass.Trim();
            return new List<ClassLevelEntry> { new(className, level, subclass) };
        }

        public static int GetTemplateTotalLevel(CharacterBuildTemplate template)
        {
            var levels = ResolveTemplateClassLevels(template);
            int sum = levels.Sum(e => e.Levels);
            return Math.Clamp(sum > 0 ? sum : 1, 1, 20);
        }

        /// <summary>
        /// Compact class line: "Cleric 5 (Peace)", "Fighter 5 / Warlock 2 (Hexblade)", etc.
        /// </summary>
        public static string FormatClassLevelsLine(
            IReadOnlyList<TemplateClassLevel>? classLevels,
            string? fallbackClass = null,
            string? fallbackSubclass = null,
            int fallbackLevel = 1)
        {
            if (classLevels is { Count: > 0 })
            {
                var valid = classLevels
                    .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                    .ToList();
                if (valid.Count > 0)
                {
                    // Omit level numbers for a single-class level-1 kit (legacy "Cleric (Peace)" look)
                    bool showLevels = valid.Count > 1 || valid.Sum(e => e.Levels) > 1;
                    var parts = new List<string>();
                    foreach (var e in valid)
                    {
                        string part = e.ClassName.Trim();
                        if (showLevels)
                            part += $" {e.Levels}";
                        if (!string.IsNullOrWhiteSpace(e.Subclass))
                            part += $" ({e.Subclass.Trim()})";
                        parts.Add(part);
                    }
                    return string.Join(" / ", parts);
                }
            }

            string cls = (fallbackClass ?? "").Trim();
            if (string.IsNullOrEmpty(cls))
                return "";
            int lvl = Math.Clamp(fallbackLevel > 0 ? fallbackLevel : 1, 1, 20);
            string line = cls;
            if (lvl > 1)
                line += $" {lvl}";
            if (!string.IsNullOrWhiteSpace(fallbackSubclass))
                line += $" ({fallbackSubclass.Trim()})";
            return line;
        }

        public static string FormatClassLevelsLine(IReadOnlyList<ClassLevelEntry>? classLevels)
        {
            if (classLevels == null || classLevels.Count == 0)
                return "";
            var mapped = classLevels
                .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                .Select(e => new TemplateClassLevel
                {
                    ClassName = e.ClassName,
                    Subclass = e.Subclass ?? "",
                    Levels = e.Levels
                })
                .ToList();
            return FormatClassLevelsLine(mapped);
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
        /// One-line kit label for the picker, e.g. "Custom Lineage · Cleric 5 (Peace) · Acolyte · L5"
        /// or multiclass "Human · Fighter 5 / Warlock 2 (Hexblade) · Soldier · L7".
        /// </summary>
        public static string FormatKitLine(CharacterBuildTemplate t)
        {
            if (t == null) return "";
            string race = t.Race ?? "";
            if (!string.IsNullOrWhiteSpace(t.Subrace))
                race = string.IsNullOrWhiteSpace(race) ? t.Subrace : $"{t.Race} ({t.Subrace})";

            int totalLevel = GetTemplateTotalLevel(t);
            string kit = FormatClassLevelsLine(t.ClassLevels, t.Class, t.Subclass, totalLevel);

            // Legacy single-class templates without ClassLevels used "Class (Subclass)" without level.
            // Prefer the level-aware line always; if empty, fall back.
            if (string.IsNullOrWhiteSpace(kit) && !string.IsNullOrWhiteSpace(t.Class))
            {
                kit = t.Class;
                if (!string.IsNullOrWhiteSpace(t.Subclass))
                    kit = $"{kit} ({t.Subclass})";
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(race)) parts.Add(race);
            if (!string.IsNullOrWhiteSpace(kit)) parts.Add(kit);
            if (!string.IsNullOrWhiteSpace(t.Background)) parts.Add(t.Background);

            var supported = GetSupportedLevels(t);
            if (supported.Count > 1)
                parts.Add("L" + string.Join("/", supported)); // e.g. L1/3/5/8
            else if (totalLevel > 1 || t.ClassLevels is { Length: > 0 })
                parts.Add($"L{totalLevel}");

            return string.Join(" · ", parts);
        }

        /// <summary>Generate a full <see cref="Character"/> for the given category and role.</summary>
        /// <param name="targetLevel">
        /// Character level for Random / True Random (and fallback role-random).
        /// Clamped to 1–20. Optimized/General/Custom ignore this and use the template picker tier.
        /// </param>
        public static GeneratedCharacterResult Generate(
            TemplateCategory category,
            TemplateRole role,
            Random? rng = null,
            int targetLevel = 1)
        {
            rng ??= new Random();
            int level = Math.Clamp(targetLevel <= 0 ? 1 : targetLevel, 1, 20);

            if (category == TemplateCategory.Random)
            {
                if (role == TemplateRole.None)
                    return GenerateTrueRandom(rng, level);
                return GenerateRoleRandom(role, rng, level);
            }

            var pool = GetTemplates(category, role).ToList();
            if (pool.Count == 0)
            {
                if (category == TemplateCategory.Custom)
                    throw new InvalidOperationException("No custom templates saved for this role yet. Create one first.");
                return GenerateRoleRandom(role == TemplateRole.None ? TemplateRole.Support : role, rng, level);
            }

            var template = pool[rng.Next(pool.Count)];
            // Multi-tier kits (e.g. General L1/3/5/8): pick a random supported level
            var tiers = GetSupportedLevels(template);
            int tier = tiers[rng.Next(tiers.Count)];
            template = ApplyTemplateLevel(template, tier);
            return BuildFromTemplate(template, category, rng);
        }

        /// <summary>Generate from a specific template the user picked in the build list.</summary>
        /// <param name="level">
        /// Optional level override for multi-tier templates. When null, uses the template's
        /// current <see cref="CharacterBuildTemplate.TargetLevel"/> if supported, else the first tier.
        /// </param>
        public static GeneratedCharacterResult GenerateFromTemplate(
            CharacterBuildTemplate template,
            Random? rng = null,
            int? level = null)
        {
            ArgumentNullException.ThrowIfNull(template);
            rng ??= new Random();

            var tiers = GetSupportedLevels(template);
            int tier;
            if (level.HasValue && tiers.Contains(level.Value))
                tier = level.Value;
            else if (tiers.Contains(GetTemplateTotalLevel(template)))
                tier = GetTemplateTotalLevel(template);
            else
                tier = tiers[0];

            // Always materialize ClassLevels / name for the chosen tier when multi-level
            if (HasLevelChoices(template) || template.ClassLevels is not { Length: > 0 })
                template = ApplyTemplateLevel(template, tier);

            return BuildFromTemplate(template, template.Category, rng);
        }

        private static GeneratedCharacterResult BuildFromTemplate(
            CharacterBuildTemplate template,
            TemplateCategory category,
            Random rng)
        {
            var classLevels = ResolveTemplateClassLevels(template);
            // Validate / normalize each class entry against live data
            for (int i = 0; i < classLevels.Count; i++)
            {
                var e = classLevels[i];
                string cn = e.ClassName?.Trim() ?? "";
                if (!GameData.ClassData.ContainsKey(cn))
                    cn = i == 0 ? "Fighter" : cn; // keep secondary names if unknown but primary must be valid
                if (!GameData.ClassData.ContainsKey(cn) && i == 0)
                    cn = "Fighter";
                if (!GameData.ClassData.ContainsKey(cn))
                    continue;
                string sub = NormalizeSubclass(cn, e.Subclass ?? "");
                classLevels[i] = new ClassLevelEntry(cn, Math.Clamp(e.Levels, 1, 20), sub);
            }
            classLevels = classLevels
                .Where(e => !string.IsNullOrWhiteSpace(e.ClassName) && GameData.ClassData.ContainsKey(e.ClassName))
                .ToList();
            if (classLevels.Count == 0)
                classLevels = new List<ClassLevelEntry> { new("Fighter", 1, "") };

            // Cap total character level at 20 (trim from the end)
            int total = classLevels.Sum(e => e.Levels);
            if (total > 20)
            {
                int over = total - 20;
                for (int i = classLevels.Count - 1; i >= 0 && over > 0; i--)
                {
                    int cut = Math.Min(over, classLevels[i].Levels - 1);
                    if (cut <= 0 && classLevels[i].Levels > 0 && over > 0 && classLevels.Count > 1)
                    {
                        // Drop trailing 1-level entry if needed
                        over -= classLevels[i].Levels;
                        classLevels.RemoveAt(i);
                        continue;
                    }
                    if (cut > 0)
                    {
                        classLevels[i] = new ClassLevelEntry(
                            classLevels[i].ClassName,
                            classLevels[i].Levels - cut,
                            classLevels[i].Subclass);
                        over -= cut;
                    }
                }
            }

            string className = classLevels[0].ClassName;
            string subclass = classLevels[0].Subclass ?? "";
            int targetLevel = Math.Clamp(classLevels.Sum(e => e.Levels), 1, 20);

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
            var (cantrips, spells, cantripAssign) = PickSpellsForLevels(
                classLevels,
                template.PreferredCantrips,
                template.PreferredSpells,
                template.CantripClassAssignments,
                template.Role,
                abilityBases,
                rng);

            var asiDecisions = BuildAsiDecisions(classLevels, template.AsiOrFeatDecisions, priority, abilityBases);
            var featurePicks = PickClassFeatureOptions(
                classLevels,
                template.PreferredFightingStyles,
                template.PreferredEldritchInvocations,
                template.PreferredMetamagic,
                template.PreferredPactBoon,
                template.PreferredFightingInitiateStyle,
                template.Role,
                rng);

            string name = MakeName(rng);
            var character = AssembleCharacter(
                name, race, subrace, background, classLevels, abilityBases,
                skills, cantrips, spells, cantripAssign, asiDecisions, featurePicks);

            string classLine = FormatClassLevelsLine(classLevels);
            string summary =
                $"{template.Name}\n" +
                $"{name} — {race}{(string.IsNullOrEmpty(subrace) ? "" : " (" + subrace + ")")} {classLine}" +
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

        private static GeneratedCharacterResult GenerateRoleRandom(
            TemplateRole role,
            Random rng,
            int targetLevel = 1)
        {
            int level = Math.Clamp(targetLevel <= 0 ? 1 : targetLevel, 1, 20);

            if (!RoleClassPools.TryGetValue(role, out var pool) || pool.Length == 0)
                return GenerateTrueRandom(rng, level);

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
            var classLevels = new List<ClassLevelEntry> { new(className, level, subclass) };
            var (cantrips, spells, cantripAssign) = PickSpellsForLevels(
                classLevels, Array.Empty<string>(), Array.Empty<string>(), null, role, abilityBases, rng);
            var asiDecisions = BuildAsiDecisions(classLevels, null, priority, abilityBases);
            var featurePicks = PickClassFeatureOptions(
                classLevels, null, null, null, null, null, role, rng);

            string name = MakeName(rng);
            var character = AssembleCharacter(
                name, race, subrace, background, classLevels, abilityBases,
                skills, cantrips, spells, cantripAssign, asiDecisions, featurePicks);

            string classLine = FormatClassLevelsLine(classLevels);
            string summary =
                $"Random {role} (L{level})\n" +
                $"{name} — {race}{(string.IsNullOrEmpty(subrace) ? "" : " (" + subrace + ")")} {classLine}" +
                $" · {background}\n" +
                DescribeRole(role);

            return new GeneratedCharacterResult
            {
                Character = character,
                TemplateName = $"Random {role} L{level}",
                Summary = summary,
                Category = TemplateCategory.Random,
                Role = role
            };
        }

        private static GeneratedCharacterResult GenerateTrueRandom(Random rng, int targetLevel = 1)
        {
            int level = Math.Clamp(targetLevel <= 0 ? 1 : targetLevel, 1, 20);

            var classes = GameData.ClassData.Keys.ToList();
            string className = PickRandom(classes, rng) ?? "Fighter";
            var subclassNames = GameData.GetSubclassNames(className);
            string subclass = subclassNames.Count > 0
                ? subclassNames[rng.Next(subclassNames.Count)]
                : "";

            // At higher levels, true random may multiclass for extra chaos
            var classLevels = BuildTrueRandomClassLevels(className, subclass, level, rng);

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

            // Skills from starting (first) class
            string primaryClass = classLevels[0].ClassName;
            var skills = PickSkills(primaryClass, background, Array.Empty<string>(), TemplateRole.None, rng);
            var (cantrips, spells, cantripAssign) = PickSpellsForLevels(
                classLevels, Array.Empty<string>(), Array.Empty<string>(), null, TemplateRole.None, abilityBases, rng);
            var asiDecisions = BuildAsiDecisions(classLevels, null, shuffledAbilities, abilityBases);
            var featurePicks = PickClassFeatureOptions(
                classLevels, null, null, null, null, null, TemplateRole.None, rng);

            string name = MakeName(rng);
            var character = AssembleCharacter(
                name, race, subrace, background, classLevels, abilityBases,
                skills, cantrips, spells, cantripAssign, asiDecisions, featurePicks);

            string classLine = FormatClassLevelsLine(classLevels);
            string summary =
                $"True Random (L{level})\n" +
                $"{name} — {race}{(string.IsNullOrEmpty(subrace) ? "" : " (" + subrace + ")")} {classLine}" +
                $" · {background}\n" +
                "No role filter — pure chaos.";

            return new GeneratedCharacterResult
            {
                Character = character,
                TemplateName = $"True Random L{level}",
                Summary = summary,
                Category = TemplateCategory.Random,
                Role = TemplateRole.None
            };
        }

        /// <summary>
        /// True Random class split: usually single-class; at L3+ sometimes a chaotic multiclass.
        /// </summary>
        private static List<ClassLevelEntry> BuildTrueRandomClassLevels(
            string primaryClass,
            string primarySubclass,
            int totalLevel,
            Random rng)
        {
            int level = Math.Clamp(totalLevel, 1, 20);
            primaryClass = string.IsNullOrWhiteSpace(primaryClass) ? "Fighter" : primaryClass.Trim();
            primarySubclass = NormalizeSubclass(primaryClass, primarySubclass ?? "");

            // L1–2 always single-class; L3+ ~35% chance to multiclass
            bool multiclass = level >= 3 && rng.NextDouble() < 0.35;
            if (!multiclass)
            {
                return new List<ClassLevelEntry>
                {
                    new(primaryClass, level, primarySubclass)
                };
            }

            var otherClasses = GameData.ClassData.Keys
                .Where(c => !c.Equals(primaryClass, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (otherClasses.Count == 0)
            {
                return new List<ClassLevelEntry>
                {
                    new(primaryClass, level, primarySubclass)
                };
            }

            string second = PickRandom(otherClasses, rng) ?? "Rogue";
            var secondSubs = GameData.GetSubclassNames(second);
            string secondSub = secondSubs.Count > 0
                ? secondSubs[rng.Next(secondSubs.Count)]
                : "";
            secondSub = NormalizeSubclass(second, secondSub);

            // Primary keeps majority of levels (at least half, min 1)
            int primaryLevels = Math.Max(1, (level + 1) / 2 + rng.Next(0, Math.Max(1, level / 4)));
            primaryLevels = Math.Min(level - 1, primaryLevels);
            if (primaryLevels < 1) primaryLevels = 1;
            int secondLevels = level - primaryLevels;
            if (secondLevels < 1)
            {
                secondLevels = 1;
                primaryLevels = level - 1;
            }

            return new List<ClassLevelEntry>
            {
                new(primaryClass, primaryLevels, primarySubclass),
                new(second, secondLevels, secondSub)
            };
        }

        private sealed class ClassFeaturePicks
        {
            public List<string> FightingStyles { get; init; } = new();
            public List<string> EldritchInvocations { get; init; } = new();
            public List<string> MetamagicOptions { get; init; } = new();
            public string PactBoon { get; init; } = "";
            public string FightingInitiateStyle { get; init; } = "";
        }

        /// <summary>
        /// Fill fighting styles / invocations / metamagic / pact boon for the class breakdown.
        /// Prefers template-captured names, then role bias, then first valid catalog option.
        /// </summary>
        private static ClassFeaturePicks PickClassFeatureOptions(
            IReadOnlyList<ClassLevelEntry> classLevels,
            string[]? preferredFightingStyles,
            string[]? preferredInvocations,
            string[]? preferredMetamagic,
            string? preferredPactBoon,
            string? preferredFightingInitiate,
            TemplateRole role,
            Random rng)
        {
            int fighterLv = 0, warlockLv = 0, sorcererLv = 0, paladinLv = 0, rangerLv = 0;
            string? fighterSub = null;
            foreach (var e in classLevels ?? Array.Empty<ClassLevelEntry>())
            {
                if (e == null || e.Levels <= 0 || string.IsNullOrWhiteSpace(e.ClassName))
                    continue;
                string cn = e.ClassName.Trim();
                if (cn.Equals("Fighter", StringComparison.OrdinalIgnoreCase))
                {
                    fighterLv += e.Levels;
                    if (!string.IsNullOrWhiteSpace(e.Subclass)) fighterSub = e.Subclass;
                }
                else if (cn.Equals("Warlock", StringComparison.OrdinalIgnoreCase))
                    warlockLv += e.Levels;
                else if (cn.Equals("Sorcerer", StringComparison.OrdinalIgnoreCase))
                    sorcererLv += e.Levels;
                else if (cn.Equals("Paladin", StringComparison.OrdinalIgnoreCase))
                    paladinLv += e.Levels;
                else if (cn.Equals("Ranger", StringComparison.OrdinalIgnoreCase))
                    rangerLv += e.Levels;
            }

            // ── Pact Boon ──
            string pact = "";
            if (warlockLv >= 3)
            {
                var boons = ClassFeatureOptionData.AllPactBoons.Select(b => b.Name).ToList();
                pact = MatchPreferred(preferredPactBoon, boons)
                       ?? RolePactBoonBias(role, boons, rng)
                       ?? PickRandom(boons, rng)
                       ?? "";
            }

            // ── Fighting styles ──
            int fsSlots = ClassFeatureOptionData.GetFighterFightingStylesKnown(fighterLv, fighterSub)
                          + ClassFeatureOptionData.GetPaladinOrRangerFightingStylesKnown(paladinLv)
                          + ClassFeatureOptionData.GetPaladinOrRangerFightingStylesKnown(rangerLv);
            var allowedStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (fighterLv >= 1)
                foreach (var o in ClassFeatureOptionData.GetFightingStylesForClass("Fighter"))
                    allowedStyles.Add(o.Name);
            if (paladinLv >= 2)
                foreach (var o in ClassFeatureOptionData.GetFightingStylesForClass("Paladin"))
                    allowedStyles.Add(o.Name);
            if (rangerLv >= 2)
                foreach (var o in ClassFeatureOptionData.GetFightingStylesForClass("Ranger"))
                    allowedStyles.Add(o.Name);

            string[] fsBias = role switch
            {
                TemplateRole.Damage => new[]
                {
                    "Archery", "Great Weapon Fighting", "Dueling", "Two-Weapon Fighting", "Thrown Weapon Fighting"
                },
                TemplateRole.Tank => new[] { "Defense", "Protection", "Interception", "Dueling", "Great Weapon Fighting" },
                TemplateRole.Support => new[] { "Defense", "Protection", "Blessed Warrior", "Dueling" },
                _ => Array.Empty<string>()
            };

            var fightingStyles = SelectNamedOptions(
                allowedStyles.ToList(),
                preferredFightingStyles,
                fsBias,
                fsSlots,
                rng);

            // Fighting Initiate (feat) — only if template captured one
            string fightInit = "";
            if (!string.IsNullOrWhiteSpace(preferredFightingInitiate))
            {
                var fighterList = ClassFeatureOptionData.GetFightingStylesForClass("Fighter")
                    .Select(o => o.Name).ToList();
                fightInit = MatchPreferred(preferredFightingInitiate, fighterList) ?? "";
            }

            // ── Invocations ──
            int invSlots = ClassFeatureOptionData.GetWarlockInvocationsKnown(warlockLv);
            var invAvailable = ClassFeatureOptionData.GetAvailableInvocations(warlockLv, pact)
                .Select(o => o.Name).ToList();
            string[] invBias = role switch
            {
                TemplateRole.Damage => new[]
                {
                    "Agonizing Blast", "Eldritch Spear", "Repelling Blast", "Thirsting Blade",
                    "Improved Pact Weapon", "Lifedrinker", "Devil's Sight"
                },
                TemplateRole.Support => new[]
                {
                    "Agonizing Blast", "Book of Ancient Secrets", "Gift of the Protectors",
                    "Armor of Shadows", "Mask of Many Faces", "Devil's Sight"
                },
                TemplateRole.Tank => new[]
                {
                    "Armor of Shadows", "Fiendish Vigor", "Devil's Sight", "Agonizing Blast",
                    "Thirsting Blade", "Improved Pact Weapon"
                },
                _ => new[] { "Agonizing Blast", "Devil's Sight", "Mask of Many Faces", "Eldritch Mind" }
            };
            // Soft-filter bias names that aren't in catalog (Hexer may not exist)
            invBias = invBias.Where(n => invAvailable.Any(a => a.Equals(n, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (invBias.Length == 0)
                invBias = new[] { "Agonizing Blast", "Devil's Sight", "Eldritch Mind", "Repelling Blast" };

            var invocations = SelectNamedOptions(
                invAvailable,
                preferredInvocations,
                invBias,
                invSlots,
                rng);

            // ── Metamagic ──
            int metaSlots = ClassFeatureOptionData.GetSorcererMetamagicKnown(sorcererLv);
            var metaAvailable = ClassFeatureOptionData.AllMetamagic.Select(o => o.Name).ToList();
            string[] metaBias = role switch
            {
                TemplateRole.Support => new[] { "Twinned Spell", "Quickened Spell", "Extended Spell", "Subtle Spell", "Careful Spell" },
                TemplateRole.Damage => new[] { "Quickened Spell", "Twinned Spell", "Empowered Spell", "Heightened Spell", "Transmuted Spell" },
                TemplateRole.Tank => new[] { "Careful Spell", "Quickened Spell", "Twinned Spell", "Subtle Spell" },
                _ => new[] { "Twinned Spell", "Quickened Spell", "Empowered Spell", "Heightened Spell" }
            };
            var metamagic = SelectNamedOptions(
                metaAvailable,
                preferredMetamagic,
                metaBias,
                metaSlots,
                rng);

            return new ClassFeaturePicks
            {
                FightingStyles = fightingStyles,
                EldritchInvocations = invocations,
                MetamagicOptions = metamagic,
                PactBoon = pact,
                FightingInitiateStyle = fightInit
            };
        }

        private static string? MatchPreferred(string? preferred, IList<string> available)
        {
            if (string.IsNullOrWhiteSpace(preferred) || available == null || available.Count == 0)
                return null;
            return available.FirstOrDefault(a =>
                a.Equals(preferred.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string? RolePactBoonBias(TemplateRole role, List<string> boons, Random rng)
        {
            string[] order = role switch
            {
                TemplateRole.Damage => new[] { "Pact of the Blade", "Pact of the Tome", "Pact of the Chain" },
                TemplateRole.Support => new[] { "Pact of the Tome", "Pact of the Chain", "Pact of the Blade" },
                TemplateRole.Tank => new[] { "Pact of the Blade", "Pact of the Chain", "Pact of the Tome" },
                _ => Array.Empty<string>()
            };
            foreach (var name in order)
            {
                var m = MatchPreferred(name, boons);
                if (m != null) return m;
            }
            return null;
        }

        private static List<string> SelectNamedOptions(
            List<string> available,
            string[]? preferred,
            string[] bias,
            int count,
            Random rng)
        {
            var result = new List<string>();
            if (count <= 0 || available == null || available.Count == 0)
                return result;

            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(string? name)
            {
                if (result.Count >= count || string.IsNullOrWhiteSpace(name)) return;
                var match = available.FirstOrDefault(a =>
                    a.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
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
                TryAdd(a);
            }

            return result;
        }

        private static Character AssembleCharacter(
            string name,
            string race,
            string subrace,
            string background,
            IReadOnlyList<ClassLevelEntry> classLevels,
            Dictionary<string, int> abilityBases,
            List<string> skills,
            List<string> cantrips,
            List<string> spells,
            Dictionary<string, string>? cantripClassAssignments,
            List<AsiOrFeatDecision>? asiDecisions,
            ClassFeaturePicks? featurePicks = null)
        {
            var levels = (classLevels ?? Array.Empty<ClassLevelEntry>())
                .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                .Select(e => new ClassLevelEntry(e.ClassName, Math.Clamp(e.Levels, 1, 20), e.Subclass ?? ""))
                .ToList();
            if (levels.Count == 0)
                levels.Add(new ClassLevelEntry("Fighter", 1, ""));

            string className = levels[0].ClassName;
            string subclass = levels[0].Subclass ?? "";
            int totalLevel = Math.Clamp(levels.Sum(e => e.Levels), 1, 20);
            int proficiency = LevelUpCalculator.GetProficiencyBonus(totalLevel);

            var c = new Character
            {
                Name = name,
                PlayerName = "",
                Race = race,
                Subrace = subrace ?? "",
                Background = background,
                Class = className,
                Subclass = subclass,
                Level = totalLevel,
                ClassLevels = levels,
                AsiOrFeatDecisions = asiDecisions ?? new List<AsiOrFeatDecision>(),
                FightingStyles = featurePicks?.FightingStyles?.ToList() ?? new List<string>(),
                EldritchInvocations = featurePicks?.EldritchInvocations?.ToList() ?? new List<string>(),
                MetamagicOptions = featurePicks?.MetamagicOptions?.ToList() ?? new List<string>(),
                WarlockPactBoon = featurePicks?.PactBoon ?? "",
                FightingInitiateStyle = featurePicks?.FightingInitiateStyle ?? "",
                HpGainMethod = HpGainMethod.FixedAverage,
                HitPointRolls = new List<int>(),
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
                CantripClassAssignments = cantripClassAssignments != null
                    ? new Dictionary<string, string>(cantripClassAssignments, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                Level1Spells = spells,
                ProficiencyBonus = proficiency,
                Speed = GameData.RaceData.TryGetValue(race, out var rd) ? rd.Speed : 30
            };

            // Primary spellcasting ability from first casting class
            foreach (var e in levels)
            {
                if (GameData.ClassData.TryGetValue(e.ClassName, out var cd) && cd.Spellcasting)
                {
                    c.SpellcastingAbility = cd.SpellAbility ?? "";
                    break;
                }
                if (SpellProgressionCalculator.IsThirdCasterSubclass(e.ClassName, e.Subclass))
                {
                    c.SpellcastingAbility = "Intelligence";
                    break;
                }
            }

            return c;
        }

        /// <summary>
        /// Build ASI decisions: prefer template-captured choices, fill remaining slots with
        /// +2 to the highest-priority ability that still has room under 20.
        /// </summary>
        private static List<AsiOrFeatDecision> BuildAsiDecisions(
            IReadOnlyList<ClassLevelEntry> classLevels,
            AsiOrFeatDecision[]? preferred,
            string[] abilityPriority,
            Dictionary<string, int> abilityBases)
        {
            var reconciled = LevelUpCalculator.ReconcileAsiOrFeatDecisions(
                classLevels,
                preferred?.Select(CloneAsiDecision));

            // Running totals (base + ASI so far) for auto-fill caps
            var running = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var ab in AllAbilities)
                running[ab] = abilityBases.GetValueOrDefault(ab, 10);

            // Apply already-chosen ASI from preferred into running
            foreach (var d in reconciled)
            {
                if (d.Kind == AsiOrFeatKind.AbilityScoreImprovement)
                {
                    ApplyAsiPoint(running, d.AbilityPlusOneA);
                    ApplyAsiPoint(running, d.AbilityPlusOneB);
                }
            }

            string[] priority = (abilityPriority is { Length: > 0 } ? abilityPriority : AllAbilities)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToArray();
            if (priority.Length == 0)
                priority = AllAbilities;

            foreach (var d in reconciled)
            {
                if (d.Kind != AsiOrFeatKind.Unchosen)
                    continue;

                // Auto ASI: try +2 to top priority under 20; else split +1/+1 across top two
                string? a = null;
                string? b = null;
                foreach (var ab in priority)
                {
                    if (running.GetValueOrDefault(ab, 10) <= 18)
                    {
                        a = ab;
                        b = ab;
                        break;
                    }
                }
                if (a == null)
                {
                    // All near cap — find any two that can take +1
                    var open = priority.Where(ab => running.GetValueOrDefault(ab, 10) < 20).Take(2).ToList();
                    if (open.Count == 0)
                        open = AllAbilities.Where(ab => running.GetValueOrDefault(ab, 10) < 20).Take(2).ToList();
                    if (open.Count >= 2)
                    {
                        a = open[0];
                        b = open[1];
                    }
                    else if (open.Count == 1)
                    {
                        a = open[0];
                        b = open[0];
                    }
                    else
                    {
                        a = priority[0];
                        b = priority[0];
                    }
                }

                d.Kind = AsiOrFeatKind.AbilityScoreImprovement;
                d.AbilityPlusOneA = a!;
                d.AbilityPlusOneB = b!;
                ApplyAsiPoint(running, a!);
                ApplyAsiPoint(running, b!);
            }

            return reconciled;
        }

        private static void ApplyAsiPoint(Dictionary<string, int> running, string? ability)
        {
            if (string.IsNullOrWhiteSpace(ability)) return;
            if (!running.ContainsKey(ability))
                running[ability] = 10;
            running[ability] = Math.Min(20, running[ability] + 1);
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

        /// <summary>
        /// Pick cantrips and leveled spells for the full class breakdown (single or multiclass).
        /// Counts scale with class level via <see cref="SpellProgressionCalculator"/>.
        /// </summary>
        private static (List<string> Cantrips, List<string> Spells, Dictionary<string, string> CantripAssign)
            PickSpellsForLevels(
                IReadOnlyList<ClassLevelEntry> classLevels,
                string[]? preferredCantrips,
                string[]? preferredSpells,
                Dictionary<string, string>? preferredCantripAssign,
                TemplateRole role,
                Dictionary<string, int> abilityBases,
                Random rng)
        {
            var cantrips = new List<string>();
            var cantripAssign = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var spells = new List<string>();
            var spellTaken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] cantripBias = role != TemplateRole.None && RoleCantripBias.TryGetValue(role, out var cb)
                ? cb : Array.Empty<string>();
            string[] spellBias = role != TemplateRole.None && RoleSpellBias.TryGetValue(role, out var sb)
                ? sb : Array.Empty<string>();

            // Prefer list from template first so saved builds stick
            var preferredCantripList = (preferredCantrips ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
            var preferredSpellList = (preferredSpells ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

            foreach (var entry in classLevels ?? Array.Empty<ClassLevelEntry>())
            {
                if (entry == null || entry.Levels <= 0 || string.IsNullOrWhiteSpace(entry.ClassName))
                    continue;

                string cls = entry.ClassName.Trim();
                string? sub = entry.Subclass;
                int classLevel = Math.Clamp(entry.Levels, 1, 20);

                // ── Cantrips (per class budget) ──
                int cantripBudget = SpellProgressionCalculator.GetCantripsKnown(cls, classLevel, sub);
                if (cantripBudget > 0)
                {
                    string listClass = SpellProgressionCalculator.GetCantripSpellListClass(cls, sub);
                    var pool = SpellCatalog.GetForClass(listClass, maxLevel: 0)
                        .Where(s => s.Level == 0)
                        .Select(s => s.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Prefer cantrips assigned to this class, then general preferred, then bias
                    var classPreferred = new List<string>();
                    if (preferredCantripAssign != null)
                    {
                        foreach (var name in preferredCantripList)
                        {
                            if (preferredCantripAssign.TryGetValue(name, out var owner) &&
                                owner.Equals(cls, StringComparison.OrdinalIgnoreCase))
                                classPreferred.Add(name);
                        }
                    }
                    foreach (var name in preferredCantripList)
                    {
                        if (!classPreferred.Contains(name, StringComparer.OrdinalIgnoreCase))
                            classPreferred.Add(name);
                    }

                    var picked = SelectFromPool(
                        pool,
                        classPreferred.ToArray(),
                        cantripBias,
                        cantripBudget,
                        rng,
                        alreadyTaken: new HashSet<string>(cantrips, StringComparer.OrdinalIgnoreCase));

                    foreach (var c in picked)
                    {
                        if (!cantrips.Contains(c, StringComparer.OrdinalIgnoreCase))
                            cantrips.Add(c);
                        cantripAssign[c] = cls;
                    }
                }

                // ── Leveled spells ──
                int highest = Math.Max(
                    SpellProgressionCalculator.GetHighestAvailableSpellLevel(new[] { entry }),
                    // Multiclass slots may unlock higher levels than single-class alone
                    0);

                // Use full multiclass highest for spell-level pool (slots shared)
                int multiHighest = SpellProgressionCalculator.GetHighestAvailableSpellLevel(classLevels);
                if (multiHighest > highest)
                    highest = multiHighest;

                if (highest <= 0)
                    continue;

                string spellListClass = SpellProgressionCalculator.IsThirdCasterSubclass(cls, sub)
                    ? "Wizard"
                    : cls;

                // Only classes that contribute spells (known or prepared)
                bool isKnown = SpellProgressionCalculator.IsKnownCaster(cls, sub);
                bool isPrepared = SpellProgressionCalculator.IsPreparedCaster(cls);
                if (!isKnown && !isPrepared)
                    continue;

                int spellBudget;
                if (isKnown)
                {
                    spellBudget = SpellProgressionCalculator.GetSpellsKnown(cls, classLevel, sub);
                }
                else
                {
                    int mod = GetSpellcastingMod(cls, abilityBases);
                    spellBudget = SpellProgressionCalculator.GetPreparedCapacity(cls, classLevel, mod);
                    // Sensible sheet prep set when mod is low
                    if (spellBudget <= 0)
                        spellBudget = Math.Max(2, EstimatePreparedCount(cls, classLevel));
                }

                if (spellBudget <= 0)
                    continue;

                var spellPool = SpellCatalog.GetForClass(spellListClass, maxLevel: highest)
                    .Where(s => s.Level >= 1 && s.Level <= highest)
                    .Select(s => s.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var pickedSpells = SelectFromPool(
                    spellPool,
                    preferredSpellList.ToArray(),
                    spellBias,
                    spellBudget,
                    rng,
                    alreadyTaken: spellTaken);

                foreach (var s in pickedSpells)
                {
                    if (spellTaken.Add(s))
                        spells.Add(s);
                }
            }

            return (cantrips, spells, cantripAssign);
        }

        private static int GetSpellcastingMod(string className, Dictionary<string, int> abilityBases)
        {
            if (!GameData.ClassData.TryGetValue(className, out var cd) ||
                string.IsNullOrWhiteSpace(cd.SpellAbility))
                return 0;
            int score = abilityBases.GetValueOrDefault(cd.SpellAbility, 10);
            return (score - 10) / 2;
        }

        private static int EstimatePreparedCount(string className, int classLevel = 1) =>
            className.ToLowerInvariant() switch
            {
                "cleric" or "druid" or "wizard" or "artificer" => Math.Max(4, 3 + classLevel / 2),
                "paladin" => Math.Max(2, classLevel / 2 + 1),
                "sorcerer" or "warlock" or "bard" => 2,
                _ => 2
            };

        private static List<string> SelectFromPool(
            List<string> available,
            string[] preferred,
            string[] bias,
            int count,
            Random rng,
            HashSet<string>? alreadyTaken = null)
        {
            var result = new List<string>();
            if (count <= 0 || available.Count == 0) return result;

            var taken = alreadyTaken != null
                ? new HashSet<string>(alreadyTaken, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

            // ---------- OPTIMIZED MULTICLASS (5e classics) ----------
            // Named packages people actually recognize from 2014 optimization culture.
            // ClassLevels order: starting class first (equipment / primary skills).

            // —— Support classics ——
            list.Add(OptimizedMulticlass(
                name: "Peacecleric Divine Soul",
                role: TemplateRole.Support,
                levels: new[]
                {
                    ("Cleric", "Peace", 1),
                    ("Sorcerer", "Divine Soul", 6)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Acolyte",
                ability: new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                skills: new[] { "Insight", "Persuasion" },
                cantrips: new[] { "Guidance", "Mind Sliver", "Fire Bolt", "Sacred Flame", "Mage Hand" },
                spells: new[] { "Bless", "Healing Word", "Sanctuary", "Aid", "Spiritual Weapon" },
                description:
                    "5e classic (L7): Peace 1 Emboldening Bond + Divine Soul metamagic (Twinned Healing Word, " +
                    "Quickened Bless). The Tasha's-era optimized support stack."));

            list.Add(OptimizedMulticlass(
                name: "Lifeberry (Life Cleric / Shepherd)",
                role: TemplateRole.Support,
                levels: new[]
                {
                    ("Cleric", "Life", 1),
                    ("Druid", "Circle of the Shepherd", 6)
                },
                race: "Firbolg",
                subrace: "",
                background: "Hermit",
                ability: new[] { "Wisdom", "Constitution", "Dexterity", "Intelligence", "Charisma", "Strength" },
                skills: new[] { "Medicine", "Nature", "Perception" },
                cantrips: new[] { "Guidance", "Shillelagh", "Thorn Whip" },
                spells: new[] { "Goodberry", "Healing Word", "Cure Wounds", "Faerie Fire", "Pass without Trace", "Conjure Animals" },
                description:
                    "5e classic Lifeberry (L7): Life 1 Disciple of Life turns Goodberry into a 40-HP snack bag " +
                    "(10 berries x 1d4+4). Shepherd spirits + Conjure Animals for full support/summoner value."));

            list.Add(OptimizedMulticlass(
                name: "Life Dip Divine Soul",
                role: TemplateRole.Support,
                levels: new[]
                {
                    ("Cleric", "Life", 1),
                    ("Sorcerer", "Divine Soul", 6)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Hermit",
                ability: new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                skills: new[] { "Medicine", "Insight" },
                cantrips: new[] { "Guidance", "Sacred Flame", "Mind Sliver", "Mage Hand", "Light" },
                spells: new[] { "Cure Wounds", "Healing Word", "Bless", "Aid" },
                description:
                    "5e classic healer (L7): Life Disciple of Life on every Twinned Healing Word / Cure from " +
                    "Divine Soul. Peak heal efficiency with metamagic."));

            list.Add(OptimizedMulticlass(
                name: "Orderbard (Order Cleric / Lore Bard)",
                role: TemplateRole.Support,
                levels: new[]
                {
                    ("Cleric", "Order", 1),
                    ("Bard", "College of Lore", 6)
                },
                race: "Half-Elf",
                subrace: "",
                background: "Noble",
                ability: new[] { "Charisma", "Wisdom", "Constitution", "Dexterity", "Intelligence", "Strength" },
                skills: new[] { "Persuasion", "Insight", "Perception" },
                cantrips: new[] { "Guidance", "Vicious Mockery", "Sacred Flame", "Minor Illusion" },
                spells: new[] { "Healing Word", "Bless", "Faerie Fire", "Dissonant Whispers", "Command" },
                description:
                    "5e classic martial-enabler (L7): Order Voice of Authority (heal/buff -> free ally attack) + " +
                    "Lore Bard skills, Cutting Words, Magical Secrets path."));

            list.Add(OptimizedMulticlass(
                name: "Celestial Sorlock (Heal + EB)",
                role: TemplateRole.Support,
                levels: new[]
                {
                    ("Warlock", "The Celestial", 2),
                    ("Sorcerer", "Divine Soul", 5)
                },
                race: "Aasimar",
                subrace: "Protector Aasimar",
                background: "Acolyte",
                ability: new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                skills: new[] { "Arcana", "Insight" },
                cantrips: new[] { "Eldritch Blast", "Sacred Flame", "Mind Sliver", "Guidance", "Mage Hand" },
                spells: new[] { "Hex", "Cure Wounds", "Healing Word", "Bless", "Aid" },
                description:
                    "5e classic short-rest support (L7): Celestial Healing Light + Agonizing Blast, with Divine " +
                    "Soul twin heals. Support Sorlock when the party needs both heals and cantrip damage."));

            // —— Damage classics ——
            list.Add(OptimizedMulticlass(
                name: "Hexadin / Padlock (Vengeance)",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Warlock", "The Hexblade", 1),
                    ("Paladin", "Oath of Vengeance", 6)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Soldier",
                ability: new[] { "Charisma", "Constitution", "Dexterity", "Strength", "Wisdom", "Intelligence" },
                skills: new[] { "Intimidation", "Athletics" },
                cantrips: new[] { "Booming Blade", "Eldritch Blast" },
                spells: new[] { "Shield", "Hex", "Wrathful Smite", "Hunter's Mark", "Misty Step" },
                description:
                    "5e classic Hexadin / Padlock (L7): Hexblade 1 SAD (Cha weapons, Shield, Curse) + Vengeance 6 " +
                    "Aura, Vow of Enmity, Extra Attack, Divine Smite. The poster-child Cha nova multiclass."));

            list.Add(OptimizedMulticlass(
                name: "Sorlock (Hexblade / Clockwork)",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Warlock", "The Hexblade", 2),
                    ("Sorcerer", "Clockwork Soul", 5)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Sage",
                ability: new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                skills: new[] { "Arcana", "Intimidation" },
                cantrips: new[] { "Eldritch Blast", "Mind Sliver", "Booming Blade", "Mage Hand", "Minor Illusion" },
                spells: new[] { "Hex", "Shield", "Absorb Elements", "Misty Step", "Counterspell" },
                description:
                    "5e classic Sorlock (L7): Agonizing Blast + Quickened Eldritch Blast (double beam volleys), " +
                    "Hexblade armor/Shield, Clockwork Restore Balance. The defining Cha blaster multiclass."));

            // Broken by design (short-rest pact slots → sorcery points → spell slots). Still a real player build.
            list.Add(OptimizedMulticlass(
                name: "Coffeelock (Hexblade / Clockwork)",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Warlock", "The Hexblade", 2),
                    ("Sorcerer", "Clockwork Soul", 6)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Sage",
                ability: new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                skills: new[] { "Arcana", "Deception" },
                cantrips: new[] { "Eldritch Blast", "Mind Sliver", "Minor Illusion", "Mage Hand", "Booming Blade" },
                spells: new[] { "Hex", "Shield", "Absorb Elements", "Misty Step", "Counterspell", "Fireball" },
                description:
                    "5e infamous Coffeelock (L8): convert short-rest Warlock pact slots into sorcery points, then " +
                    "into sorcerer spell slots (Flexible Casting). With enough short rests you bank far more slots " +
                    "than a long rest allows — broken, table-dependent, and explicitly supported here as player choice. " +
                    "Aspect of the Moon (never sleep) is the classic invocation for full cheese."));

            list.Add(OptimizedMulticlass(
                name: "Sorcadin Classic (Paladin 2 / Divine Soul)",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Paladin", "Oath of Vengeance", 2),
                    ("Sorcerer", "Divine Soul", 5)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Soldier",
                ability: new[] { "Strength", "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence" },
                skills: new[] { "Athletics", "Persuasion" },
                cantrips: new[] { "Booming Blade", "Mind Sliver", "Fire Bolt", "Mage Hand" },
                spells: new[] { "Shield", "Absorb Elements", "Bless", "Healing Word", "Guiding Bolt" },
                description:
                    "5e classic early Sorcadin (L7): Paladin 2 for Divine Smite + Fighting Style, then full " +
                    "Divine Soul progression and metamagic to Quickened cast while dumping slots into smites."));

            list.Add(OptimizedMulticlass(
                name: "Sorcadin Aura (Vengeance 6 / Clockwork)",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Paladin", "Oath of Vengeance", 6),
                    ("Sorcerer", "Clockwork Soul", 3)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Soldier",
                ability: new[] { "Strength", "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence" },
                skills: new[] { "Athletics", "Intimidation" },
                cantrips: new[] { "Booming Blade", "Mind Sliver", "Green-Flame Blade", "Mage Hand" },
                spells: new[] { "Shield", "Absorb Elements", "Hunter's Mark", "Misty Step", "Aid" },
                description:
                    "5e classic late Sorcadin (L9): hold for Aura of Protection at Paladin 6, then Clockwork " +
                    "metamagic. Higher durability than the Paladin-2 dip; still full smite nova."));

            list.Add(OptimizedMulticlass(
                name: "Gloomstalker Assassin",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Ranger", "Gloom Stalker", 5),
                    ("Rogue", "Assassin", 3)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Criminal",
                ability: new[] { "Dexterity", "Constitution", "Wisdom", "Intelligence", "Charisma", "Strength" },
                skills: new[] { "Stealth", "Perception", "Acrobatics", "Investigation" },
                cantrips: Array.Empty<string>(),
                spells: new[] { "Hunter's Mark", "Pass without Trace", "Absorb Elements" },
                description:
                    "5e classic ambush nova (L8): Gloom Stalker Dread Ambusher + Assassin auto-crit on surprise. " +
                    "Round-1 delete button when the table runs surprise cleanly."));

            list.Add(OptimizedMulticlass(
                name: "Gloomstalker Battlemaster",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Ranger", "Gloom Stalker", 5),
                    ("Fighter", "Battle Master", 3)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Outlander",
                ability: new[] { "Dexterity", "Constitution", "Wisdom", "Strength", "Intelligence", "Charisma" },
                skills: new[] { "Stealth", "Perception", "Survival" },
                cantrips: Array.Empty<string>(),
                spells: new[] { "Hunter's Mark", "Absorb Elements", "Zephyr Strike" },
                description:
                    "5e classic archery nova (L8): Gloom 5 Extra Attack + Dread Ambusher, Fighter 3 Action Surge " +
                    "and Precision Attack. The Sharpshooter / Crossbow Expert optimized archer."));

            list.Add(OptimizedMulticlass(
                name: "Bardadin (Swords / Paladin)",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Bard", "College of Swords", 6),
                    ("Paladin", "Oath of Vengeance", 2)
                },
                race: "Half-Elf",
                subrace: "",
                background: "Entertainer",
                ability: new[] { "Charisma", "Dexterity", "Constitution", "Strength", "Wisdom", "Intelligence" },
                skills: new[] { "Acrobatics", "Performance", "Persuasion" },
                cantrips: new[] { "Booming Blade", "Vicious Mockery", "Minor Illusion" },
                spells: new[] { "Faerie Fire", "Heat Metal", "Mirror Image", "Hold Person" },
                description:
                    "5e classic Bardadin (L8): Swords 6 flourishes + Extra Attack, Paladin 2 Divine Smite on " +
                    "bard slots. Cha face with smite nova and full bard spell list."));

            list.Add(OptimizedMulticlass(
                name: "Hexblade Swords Bard",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Warlock", "The Hexblade", 1),
                    ("Bard", "College of Swords", 6)
                },
                race: "Half-Elf",
                subrace: "",
                background: "Entertainer",
                ability: new[] { "Charisma", "Dexterity", "Constitution", "Wisdom", "Intelligence", "Strength" },
                skills: new[] { "Acrobatics", "Performance", "Persuasion" },
                cantrips: new[] { "Booming Blade", "Eldritch Blast", "Minor Illusion" },
                spells: new[] { "Shield", "Hex", "Faerie Fire", "Heat Metal", "Mirror Image" },
                description:
                    "5e classic SAD skirmisher (L7): Hexblade medium armor + Cha attacks, Swords Blade Flourishes. " +
                    "Often preferred over pure Swords when you want Shield and Hex Warrior early."));

            list.Add(OptimizedMulticlass(
                name: "EB Fighter (Hexblade / Action Surge)",
                role: TemplateRole.Damage,
                levels: new[]
                {
                    ("Warlock", "The Hexblade", 5),
                    ("Fighter", "Battle Master", 2)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Soldier",
                ability: new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                skills: new[] { "Intimidation", "Arcana" },
                cantrips: new[] { "Eldritch Blast", "Booming Blade", "Mind Sliver" },
                spells: new[] { "Hex", "Shield", "Misty Step", "Hunger of Hadar", "Counterspell" },
                description:
                    "5e classic EB nova (L7): Agonizing Blast volleys doubled with Action Surge. Simple, " +
                    "brutal, and still one of the highest single-turn cantrip novas in the game."));

            // —— Tank classics ——
            list.Add(OptimizedMulticlass(
                name: "Bear-Barian (Moon / Bear Totem)",
                role: TemplateRole.Tank,
                levels: new[]
                {
                    ("Barbarian", "Path of the Totem Warrior", 3),
                    ("Druid", "Circle of the Moon", 6)
                },
                race: "Firbolg",
                subrace: "",
                background: "Outlander",
                ability: new[] { "Wisdom", "Constitution", "Strength", "Dexterity", "Intelligence", "Charisma" },
                skills: new[] { "Athletics", "Perception", "Survival" },
                cantrips: new[] { "Shillelagh", "Thorn Whip", "Guidance" },
                spells: new[] { "Goodberry", "Absorb Elements", "Faerie Fire", "Healing Word", "Pass without Trace", "Conjure Animals" },
                description:
                    "5e classic Bear-Barian (L9): Bear Totem rage (resist all but psychic) + Moon Combat Wild Shape. " +
                    "Rage, then brown bear / elemental forms — ablative beast HP with near-full resistance."));

            list.Add(OptimizedMulticlass(
                name: "Barbarian Fighter (Bear Totem / BM)",
                role: TemplateRole.Tank,
                levels: new[]
                {
                    ("Barbarian", "Path of the Totem Warrior", 5),
                    ("Fighter", "Battle Master", 2)
                },
                race: "Half-Orc",
                subrace: "",
                background: "Soldier",
                ability: new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                skills: new[] { "Athletics", "Intimidation" },
                cantrips: Array.Empty<string>(),
                spells: Array.Empty<string>(),
                description:
                    "5e classic martial tank (L7): Bear Totem resistance + Barb Extra Attack, Fighter Action Surge. " +
                    "Not the Moon Bear-Barian — pure weapon sponge with Reckless Attack economy."));

            list.Add(OptimizedMulticlass(
                name: "Ancients Padlock (Aura Tank)",
                role: TemplateRole.Tank,
                levels: new[]
                {
                    ("Warlock", "The Hexblade", 1),
                    ("Paladin", "Oath of the Ancients", 6)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Noble",
                ability: new[] { "Charisma", "Constitution", "Strength", "Dexterity", "Wisdom", "Intelligence" },
                skills: new[] { "Athletics", "Persuasion" },
                cantrips: new[] { "Booming Blade", "Eldritch Blast" },
                spells: new[] { "Shield", "Hex", "Compelled Duel", "Heroism", "Misty Step" },
                description:
                    "5e classic aura tank (L7): Hexblade SAD + Ancients Aura of Protection, on track for Aura of " +
                    "Warding (half spell damage to nearby allies). Frontline face with smites."));

            list.Add(OptimizedMulticlass(
                name: "Ancestral Guardian / Rune Knight",
                role: TemplateRole.Tank,
                levels: new[]
                {
                    ("Barbarian", "Path of the Ancestral Guardian", 3),
                    ("Fighter", "Rune Knight", 3)
                },
                race: "Half-Orc",
                subrace: "",
                background: "Soldier",
                ability: new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                skills: new[] { "Athletics", "Perception" },
                cantrips: Array.Empty<string>(),
                spells: Array.Empty<string>(),
                description:
                    "5e classic protect-the-caster tank (L6): Ancestral Protectors (disadv + half damage vs allies) " +
                    "+ Rune Knight size/control. Modern Tasha's / EGtW frontline package."));

            list.Add(OptimizedMulticlass(
                name: "Twilight Fighter",
                role: TemplateRole.Tank,
                levels: new[]
                {
                    ("Cleric", "Twilight", 1),
                    ("Fighter", "Battle Master", 5)
                },
                race: "Dwarf",
                subrace: "Hill Dwarf",
                background: "Soldier",
                ability: new[] { "Strength", "Constitution", "Wisdom", "Dexterity", "Charisma", "Intelligence" },
                skills: new[] { "Athletics", "Insight" },
                cantrips: new[] { "Guidance", "Toll the Dead", "Sacred Flame" },
                spells: new[] { "Bless", "Shield of Faith", "Healing Word", "Faerie Fire" },
                description:
                    "5e classic temp-HP frontliner (L6): Twilight domain toolkit + Fighter Extra Attack / Action " +
                    "Surge. Bless uptime with martial attack economy."));

            list.Add(OptimizedMulticlass(
                name: "Armorer Peace Tank",
                role: TemplateRole.Tank,
                levels: new[]
                {
                    ("Artificer", "Armorer", 5),
                    ("Cleric", "Peace", 1)
                },
                race: "Custom Lineage",
                subrace: "",
                background: "Sage",
                ability: new[] { "Intelligence", "Constitution", "Wisdom", "Dexterity", "Strength", "Charisma" },
                skills: new[] { "Arcana", "Insight" },
                cantrips: new[] { "Fire Bolt", "Mending", "Guidance" },
                spells: new[] { "Shield", "Absorb Elements", "Faerie Fire", "Cure Wounds", "Sanctuary", "Bless" },
                description:
                    "5e classic Int tank (L6): Armorer Guardian marks + Extra Attack, Peace Emboldening Bond. " +
                    "Infusions for AC while the party rides Bond."));

            FixBackgrounds(list);

            // ---------- GENERAL SUPPORT ----------
            // Popular, reliable single-class kits: easy to play, fill the role, common at tables.

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
                Description = "Classic Life Cleric: the reliable party healer every table understands."
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
                Description = "Glamour Bard: charms, heals, and party face without complex resource tracking."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Lore Keeper",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Bard",
                Subclass = "College of Lore",
                Race = "Half-Elf",
                Subrace = "",
                Background = "Sage",
                AbilityPriority = new[] { "Charisma", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Strength" },
                PreferredSkills = new[] { "Arcana", "History", "Persuasion" },
                PreferredCantrips = new[] { "Vicious Mockery", "Minor Illusion" },
                PreferredSpells = new[] { "Healing Word", "Faerie Fire", "Identify", "Detect Magic" },
                Description = "Lore Bard: skill monkey and flexible caster — great all-rounder for exploration and support."
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
                Description = "Dreams Druid: soft heals and nature utility without wild-shape micromanagement."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Stars Guide",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Druid",
                Subclass = "Circle of Stars",
                Race = "Human",
                Subrace = "",
                Background = "Sage",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Dexterity", "Intelligence", "Charisma", "Strength" },
                PreferredSkills = new[] { "Arcana", "Perception" },
                PreferredCantrips = new[] { "Guidance", "Produce Flame" },
                PreferredSpells = new[] { "Guiding Bolt", "Healing Word", "Faerie Fire", "Goodberry" },
                Description = "Stars Druid: Dragon form free Guiding Bolts and solid heals — simple, strong support turns."
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
                Description = "Devotion Paladin: Lay on Hands, Bless, and frontline support smites."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Grave Tender",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Cleric",
                Subclass = "Grave",
                Race = "Human",
                Subrace = "",
                Background = "Acolyte",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Dexterity", "Charisma", "Strength", "Intelligence" },
                PreferredSkills = new[] { "Medicine", "Religion" },
                PreferredCantrips = new[] { "Spare the Dying", "Toll the Dead", "Guidance" },
                PreferredSpells = new[] { "Healing Word", "Bless", "Inflict Wounds", "Bane" },
                Description = "Grave Cleric: keep allies from dying and set up big crits — popular support with a clear identity."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Celestial Patron",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Warlock",
                Subclass = "The Celestial",
                Race = "Aasimar",
                Subrace = "Protector Aasimar",
                Background = "Acolyte",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Insight", "Religion" },
                PreferredCantrips = new[] { "Eldritch Blast", "Sacred Flame" },
                PreferredSpells = new[] { "Cure Wounds", "Hex" },
                Description = "Celestial Warlock: short-rest Healing Light plus Eldritch Blast. Easy support when the cleric is busy."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Field Alchemist",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Artificer",
                Subclass = "Alchemist",
                Race = "Gnome",
                Subrace = "Rock Gnome",
                Background = "Sage",
                AbilityPriority = new[] { "Intelligence", "Constitution", "Dexterity", "Wisdom", "Charisma", "Strength" },
                PreferredSkills = new[] { "Medicine", "Arcana" },
                PreferredCantrips = new[] { "Guidance", "Mending" },
                PreferredSpells = new[] { "Cure Wounds", "Healing Word", "Faerie Fire", "Identify" },
                Description = "Alchemist Artificer: Experimental Elixirs and tools for a fun, support-forward crafter."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Fey Emissary",
                Category = TemplateCategory.General,
                Role = TemplateRole.Support,
                Class = "Ranger",
                Subclass = "Fey Wanderer",
                Race = "Elf",
                Subrace = "Wood Elf",
                Background = "Far Traveler",
                AbilityPriority = new[] { "Dexterity", "Charisma", "Wisdom", "Constitution", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Persuasion", "Perception", "Survival" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = new[] { "Hunter's Mark", "Cure Wounds", "Charm Person" },
                Description = "Fey Wanderer: scout who also works as a secondary face — solid exploration all-rounder."
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
                Description = "Swashbuckler Rogue: mobile single-target Sneak Attack without needing an ally adjacent."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Thief of Opportunity",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Rogue",
                Subclass = "Thief",
                Race = "Halfling",
                Subrace = "Lightfoot Halfling",
                Background = "Urchin",
                AbilityPriority = new[] { "Dexterity", "Constitution", "Wisdom", "Intelligence", "Charisma", "Strength" },
                PreferredSkills = new[] { "Stealth", "Sleight of Hand", "Acrobatics", "Perception" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Classic Thief: Fast Hands and Second-Story Work for dungeon utility and steady damage."
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
                Description = "Evoker Wizard: the straightforward blaster every party recognizes."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Bladesinger Duelist",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Wizard",
                Subclass = "Bladesinging",
                Race = "Elf",
                Subrace = "High Elf",
                Background = "Sage",
                AbilityPriority = new[] { "Intelligence", "Dexterity", "Constitution", "Wisdom", "Charisma", "Strength" },
                PreferredSkills = new[] { "Arcana", "Performance" },
                PreferredCantrips = new[] { "Booming Blade", "Fire Bolt", "Minor Illusion" },
                PreferredSpells = new[] { "Shield", "Absorb Elements", "Magic Missile", "Find Familiar" },
                Description = "Bladesinger: popular gish — AC from song, cantrips and spells in melee without multiclassing."
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
                Description = "Wild Magic Sorcerer: flashy metamagic damage with table-friendly chaos."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Draconic Bloodline",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Sorcerer",
                Subclass = "Draconic Bloodline",
                Race = "Dragonborn",
                Subrace = "",
                Background = "Noble",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Wisdom", "Intelligence", "Strength" },
                PreferredSkills = new[] { "Intimidation", "Arcana" },
                PreferredCantrips = new[] { "Fire Bolt", "Ray of Frost", "Mage Hand", "Prestidigitation" },
                PreferredSpells = new[] { "Chromatic Orb", "Shield", "Magic Missile" },
                Description = "Draconic Sorcerer: tough CHA caster with elemental flavor — simple nova kit."
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
                Description = "Hunter Ranger: versatile archery and melee — the PHB classic outdoors striker."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Beast Companion",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Ranger",
                Subclass = "Beast Master",
                Race = "Human",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Dexterity", "Wisdom", "Constitution", "Strength", "Intelligence", "Charisma" },
                PreferredSkills = new[] { "Animal Handling", "Perception", "Survival" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Beast Master: popular pet-ranger fantasy. Straightforward damage with a companion on the board."
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
                Description = "Fiend Warlock: Eldritch Blast chassis and temp HP on kills — short-rest friendly damage."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Hexblade Knight",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Warlock",
                Subclass = "The Hexblade",
                Race = "Human",
                Subrace = "",
                Background = "Soldier",
                AbilityPriority = new[] { "Charisma", "Constitution", "Dexterity", "Strength", "Wisdom", "Intelligence" },
                PreferredSkills = new[] { "Intimidation", "Arcana" },
                PreferredCantrips = new[] { "Eldritch Blast", "Booming Blade" },
                PreferredSpells = new[] { "Hex", "Shield" },
                Description = "Hexblade Warlock: Cha-only weapon attacks and medium armor — popular martial warlock without multiclass dips."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Open Hand Adept",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Monk",
                Subclass = "Way of the Open Hand",
                Race = "Human",
                Subrace = "",
                Background = "Hermit",
                AbilityPriority = new[] { "Dexterity", "Wisdom", "Constitution", "Strength", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Acrobatics", "Athletics" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Open Hand Monk: flurry, stuns, and mobility. Light on gear, easy to run every round."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Zealot Crusader",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Barbarian",
                Subclass = "Path of the Zealot",
                Race = "Human",
                Subrace = "",
                Background = "Acolyte",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Intimidation" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Zealot Barbarian: rage damage that never quits and hard-to-kill flavor — simple melee DPR."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Battle Master Captain",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Fighter",
                Subclass = "Battle Master",
                Race = "Human",
                Subrace = "",
                Background = "Soldier",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Perception" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Battle Master Fighter: maneuvers for trip, precision, and control. Reliable martial damage with choices."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Samurai Duelist",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Fighter",
                Subclass = "Samurai",
                Race = "Human",
                Subrace = "",
                Background = "Noble",
                AbilityPriority = new[] { "Strength", "Constitution", "Wisdom", "Dexterity", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Persuasion" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Samurai Fighter: Fighting Spirit advantage when you need it. Clean, cinematic melee striker."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Light Domain Blaster",
                Category = TemplateCategory.General,
                Role = TemplateRole.Damage,
                Class = "Cleric",
                Subclass = "Light",
                Race = "Human",
                Subrace = "",
                Background = "Acolyte",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Dexterity", "Charisma", "Strength", "Intelligence" },
                PreferredSkills = new[] { "Religion", "Insight" },
                PreferredCantrips = new[] { "Sacred Flame", "Guidance", "Light" },
                PreferredSpells = new[] { "Burning Hands", "Guiding Bolt", "Bless", "Faerie Fire" },
                Description = "Light Cleric: fire-and-radiant blaster who can still Bless and heal. Popular hybrid damage caster."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Artillerist Engineer",
                Category = TemplateCategory.General,
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
                Description = "Artillerist: Eldritch Cannon bonus-action damage every turn. Fun gadget damage without min-maxing."
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
                Description = "Champion Fighter: the simplest durable frontliner in the game."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Cavalier Protector",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Fighter",
                Subclass = "Cavalier",
                Race = "Human",
                Subrace = "",
                Background = "Knight",
                AbilityPriority = new[] { "Strength", "Constitution", "Wisdom", "Dexterity", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Animal Handling" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Cavalier: mark foes and punish attacks on allies — clear tank job for new players."
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
                Description = "Crown Paladin: compel enemies and share defenses — classic armored guardian."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Ancients Warden",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Paladin",
                Subclass = "Oath of the Ancients",
                Race = "Human",
                Subrace = "",
                Background = "Folk Hero",
                AbilityPriority = new[] { "Strength", "Charisma", "Constitution", "Wisdom", "Dexterity", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Nature" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Ancients Paladin: nature knight with strong auras and smites. Durable frontline all-rounder."
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
                Description = "Berserker Barbarian: high HP, rage resistance, and reckless offense as defense."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Totem Guardian",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Barbarian",
                Subclass = "Path of the Totem Warrior",
                Race = "Human",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Perception" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Totem Barbarian: Bear spirit for broad damage resistance while raging — the popular tough barb."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Ancestral Sentinel",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Barbarian",
                Subclass = "Path of the Ancestral Guardian",
                Race = "Human",
                Subrace = "",
                Background = "Folk Hero",
                AbilityPriority = new[] { "Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Insight" },
                PreferredCantrips = Array.Empty<string>(),
                PreferredSpells = Array.Empty<string>(),
                Description = "Ancestral Guardian: hit a foe and protect the backline. Easy protect-the-party tank fantasy."
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
                Description = "Forge Cleric: heavy armor, +1 forge blessing, and frontline spells. Reliable AC tank."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "War Priest",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Cleric",
                Subclass = "War",
                Race = "Human",
                Subrace = "",
                Background = "Soldier",
                AbilityPriority = new[] { "Strength", "Wisdom", "Constitution", "Dexterity", "Charisma", "Intelligence" },
                PreferredSkills = new[] { "Athletics", "Religion" },
                PreferredCantrips = new[] { "Sacred Flame", "Guidance", "Toll the Dead" },
                PreferredSpells = new[] { "Shield of Faith", "Bless", "Divine Favor", "Cure Wounds" },
                Description = "War Cleric: martial weapons, bonus attacks, and cleric utility. Frontline holy warrior without multiclassing."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Moon Circle Tank",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Druid",
                Subclass = "Circle of the Moon",
                Race = "Human",
                Subrace = "",
                Background = "Outlander",
                AbilityPriority = new[] { "Wisdom", "Constitution", "Dexterity", "Strength", "Intelligence", "Charisma" },
                PreferredSkills = new[] { "Perception", "Survival" },
                PreferredCantrips = new[] { "Shillelagh", "Thorn Whip" },
                PreferredSpells = new[] { "Goodberry", "Entangle", "Faerie Fire", "Healing Word" },
                Description = "Moon Druid: Combat Wild Shape as ablative HP. Iconic tank that is fun and easy to grasp."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Armorer Guardian",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Artificer",
                Subclass = "Armorer",
                Race = "Human",
                Subrace = "",
                Background = "Sage",
                AbilityPriority = new[] { "Intelligence", "Constitution", "Dexterity", "Wisdom", "Strength", "Charisma" },
                PreferredSkills = new[] { "Arcana", "Athletics" },
                PreferredCantrips = new[] { "Fire Bolt", "Mending" },
                PreferredSpells = new[] { "Shield", "Absorb Elements", "Cure Wounds", "Faerie Fire" },
                Description = "Armorer Artificer: Guardian model marks foes and tanks on Intelligence — modern popular frontliner."
            });

            list.Add(new CharacterBuildTemplate
            {
                Name = "Battle Smith Escort",
                Category = TemplateCategory.General,
                Role = TemplateRole.Tank,
                Class = "Artificer",
                Subclass = "Battle Smith",
                Race = "Human",
                Subrace = "",
                Background = "Folk Hero",
                AbilityPriority = new[] { "Intelligence", "Constitution", "Dexterity", "Strength", "Wisdom", "Charisma" },
                PreferredSkills = new[] { "Athletics", "Animal Handling" },
                PreferredCantrips = new[] { "Fire Bolt", "Mending" },
                PreferredSpells = new[] { "Shield", "Cure Wounds", "Heroism", "Absorb Elements" },
                Description = "Battle Smith: Steel Defender soaks hits while you fight beside it. Friendly tank all-rounder."
            });

            // Validate races exist — swap missing exotic races to common ones
            return list.Select(SanitizeTemplate).ToList();
        }

        /// <summary>
        /// Build a multiclass template. <paramref name="levels"/> order is starting class first.
        /// </summary>
        private static CharacterBuildTemplate OptimizedMulticlass(
            string name,
            TemplateRole role,
            (string ClassName, string Subclass, int Levels)[] levels,
            string race,
            string subrace,
            string background,
            string[] ability,
            string[] skills,
            string[] cantrips,
            string[] spells,
            string description,
            TemplateCategory category = TemplateCategory.Optimized,
            string[]? fightingStyles = null,
            string[]? invocations = null,
            string[]? metamagic = null,
            string? pactBoon = null)
        {
            if (levels == null || levels.Length == 0)
                throw new ArgumentException("Multiclass templates need at least one class level.", nameof(levels));

            var classLevels = levels
                .Where(l => l.Levels > 0 && !string.IsNullOrWhiteSpace(l.ClassName))
                .Select(l => new TemplateClassLevel
                {
                    ClassName = l.ClassName.Trim(),
                    Subclass = l.Subclass?.Trim() ?? "",
                    Levels = Math.Clamp(l.Levels, 1, 20)
                })
                .ToArray();

            int total = Math.Clamp(classLevels.Sum(e => e.Levels), 1, 20);
            var primary = classLevels[0];

            return new CharacterBuildTemplate
            {
                Name = name,
                Category = category,
                Role = role,
                Class = primary.ClassName,
                Subclass = primary.Subclass,
                Race = race ?? "",
                Subrace = subrace ?? "",
                Background = background ?? "",
                TargetLevel = total,
                ClassLevels = classLevels,
                AbilityPriority = ability ?? AllAbilities.ToArray(),
                PreferredSkills = skills ?? Array.Empty<string>(),
                PreferredCantrips = cantrips ?? Array.Empty<string>(),
                PreferredSpells = spells ?? Array.Empty<string>(),
                PreferredFightingStyles = fightingStyles ?? Array.Empty<string>(),
                PreferredEldritchInvocations = invocations ?? Array.Empty<string>(),
                PreferredMetamagic = metamagic ?? Array.Empty<string>(),
                PreferredPactBoon = pactBoon ?? "",
                Description = description ?? ""
            };
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
                TargetLevel = t.TargetLevel,
                ClassLevels = t.ClassLevels ?? Array.Empty<TemplateClassLevel>(),
                AbilityPriority = t.AbilityPriority,
                PreferredSkills = t.PreferredSkills,
                PreferredCantrips = t.PreferredCantrips,
                PreferredSpells = t.PreferredSpells,
                CantripClassAssignments = t.CantripClassAssignments,
                AsiOrFeatDecisions = t.AsiOrFeatDecisions ?? Array.Empty<AsiOrFeatDecision>(),
                PreferredFightingStyles = t.PreferredFightingStyles ?? Array.Empty<string>(),
                PreferredEldritchInvocations = t.PreferredEldritchInvocations ?? Array.Empty<string>(),
                PreferredMetamagic = t.PreferredMetamagic ?? Array.Empty<string>(),
                PreferredPactBoon = t.PreferredPactBoon ?? "",
                PreferredFightingInitiateStyle = t.PreferredFightingInitiateStyle ?? "",
                SupportedLevels = t.SupportedLevels ?? Array.Empty<int>(),
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
                TargetLevel = t.TargetLevel > 0
                    ? t.TargetLevel
                    : (t.ClassLevels is { Length: > 0 } ? t.ClassLevels.Sum(e => e.Levels) : 1),
                ClassLevels = t.ClassLevels ?? Array.Empty<TemplateClassLevel>(),
                AbilityPriority = t.AbilityPriority,
                PreferredSkills = t.PreferredSkills,
                PreferredCantrips = t.PreferredCantrips,
                PreferredSpells = t.PreferredSpells,
                CantripClassAssignments = t.CantripClassAssignments,
                AsiOrFeatDecisions = t.AsiOrFeatDecisions ?? Array.Empty<AsiOrFeatDecision>(),
                PreferredFightingStyles = t.PreferredFightingStyles ?? Array.Empty<string>(),
                PreferredEldritchInvocations = t.PreferredEldritchInvocations ?? Array.Empty<string>(),
                PreferredMetamagic = t.PreferredMetamagic ?? Array.Empty<string>(),
                PreferredPactBoon = t.PreferredPactBoon ?? "",
                PreferredFightingInitiateStyle = t.PreferredFightingInitiateStyle ?? "",
                SupportedLevels = t.SupportedLevels ?? Array.Empty<int>(),
                Description = t.Description
            };
        }
    }
}
