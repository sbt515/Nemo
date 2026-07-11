using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>
    /// Base-class feature progression (levels 1–20) for all PHB/official classes.
    /// Source: Player's Handbook, Tasha's optional features, Artificer (ERftLW/Tasha's).
    /// Reference: https://dnd5e.wikidot.com/ (e.g. /cleric, /fighter, …)
    /// Subclass-specific features remain in <see cref="GameData.SubclassLevel1Features"/> (expand later).
    /// </summary>
    public static partial class GameData
    {
        /// <summary>
        /// Full base-class progression: class name → features (each with <see cref="ClassFeature.Level"/>).
        /// Does not include subclass features (domains, oaths, colleges, etc.) except a marker row
        /// like "Divine Domain" / "Martial Archetype" where the class grants the choice.
        /// </summary>
        public static readonly Dictionary<string, List<ClassFeature>> ClassProgression = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Cleric"] = BuildCleric(),
            ["Fighter"] = BuildFighter(),
            ["Wizard"] = BuildWizard(),
            ["Barbarian"] = BuildBarbarian(),
            ["Bard"] = BuildBard(),
            ["Druid"] = BuildDruid(),
            ["Monk"] = BuildMonk(),
            ["Paladin"] = BuildPaladin(),
            ["Ranger"] = BuildRanger(),
            ["Rogue"] = BuildRogue(),
            ["Sorcerer"] = BuildSorcerer(),
            ["Warlock"] = BuildWarlock(),
            ["Artificer"] = BuildArtificer(),
        };

        // ───────────────────────── Query helpers ─────────────────────────

        /// <summary>Features gained exactly at the given class level.</summary>
        public static List<ClassFeature> GetClassFeaturesAtLevel(
            string className, int level, bool includeOptional = true)
        {
            if (string.IsNullOrWhiteSpace(className) || level < 1 || level > 20)
                return new List<ClassFeature>();

            if (!ClassProgression.TryGetValue(className.Trim(), out var all) || all == null)
                return new List<ClassFeature>();

            return all
                .Where(f => f.Level == level && (includeOptional || !f.IsOptional))
                .Select(CloneFeature)
                .ToList();
        }

        /// <summary>All features gained from level 1 through <paramref name="maxLevel"/> (inclusive).</summary>
        public static List<ClassFeature> GetClassFeaturesUpToLevel(
            string className, int maxLevel, bool includeOptional = true)
        {
            if (string.IsNullOrWhiteSpace(className) || maxLevel < 1)
                return new List<ClassFeature>();

            if (!ClassProgression.TryGetValue(className.Trim(), out var all) || all == null)
                return new List<ClassFeature>();

            int cap = Math.Min(20, maxLevel);
            return all
                .Where(f => f.Level >= 1 && f.Level <= cap && (includeOptional || !f.IsOptional))
                .OrderBy(f => f.Level)
                .ThenBy(f => f.Name)
                .Select(CloneFeature)
                .ToList();
        }

        /// <summary>
        /// Compact level→feature-name map for tables/UI (e.g. "2: Channel Divinity, Turn Undead").
        /// </summary>
        public static Dictionary<int, List<string>> GetClassFeatureNameTable(
            string className, bool includeOptional = true)
        {
            var result = new Dictionary<int, List<string>>();
            if (!ClassProgression.TryGetValue(className?.Trim() ?? "", out var all) || all == null)
                return result;

            foreach (var f in all.Where(x => includeOptional || !x.IsOptional))
            {
                if (!result.TryGetValue(f.Level, out var list))
                {
                    list = new List<string>();
                    result[f.Level] = list;
                }
                list.Add(f.IsOptional ? $"{f.Name} (Optional)" : f.Name);
            }
            return result;
        }

        private static ClassFeature CloneFeature(ClassFeature f) => new()
        {
            Name = f.Name,
            Description = f.Description,
            Uses = f.Uses,
            Level = f.Level,
            IsOptional = f.IsOptional,
            IsSubclassFeature = f.IsSubclassFeature
        };

        private static ClassFeature F(
            int level, string name, string description,
            string uses = "Passive", bool optional = false, bool subclass = false) => new()
        {
            Level = level,
            Name = name,
            Description = description,
            Uses = uses,
            IsOptional = optional,
            IsSubclassFeature = subclass
        };

        private static ClassFeature Asi(int level) => F(level, "Ability Score Improvement",
            "Increase one ability score by 2, or two ability scores by 1 (maximum 20). " +
            "You may take a feat instead if your table allows.",
            "Passive");

        // ───────────────────────── Cleric (PHB + Tasha's optional) ─────────────────────────
        // https://dnd5e.wikidot.com/cleric

        private static List<ClassFeature> BuildCleric() => new()
        {
            F(1, "Spellcasting",
                "Cast cleric spells using Wisdom. Prepare a number of cleric spells equal to your Wisdom modifier + cleric level (minimum 1). " +
                "You know three cantrips at 1st level (more at higher levels). Ritual casting. Holy symbol as focus.",
                "Spell slots (long rest)"),
            F(1, "Divine Domain",
                "Choose a divine domain. You gain domain spells and domain features at 1st level, and further domain features at 2nd, 6th, 8th, and 17th level.",
                "Varies by domain", subclass: true),

            F(2, "Channel Divinity",
                "Channel divine energy to fuel magical effects. Start with Turn Undead and a domain option. " +
                "1 use per short or long rest; 2 uses at 6th level; 3 uses at 18th level.",
                "1/short rest (scales)"),
            F(2, "Channel Divinity: Turn Undead",
                "As an action, present your holy symbol. Each undead that can see or hear you within 30 feet must succeed on a Wisdom save or be turned for 1 minute or until it takes damage.",
                "Channel Divinity"),
            F(2, "Harness Divine Power",
                "As a bonus action, expend a use of Channel Divinity to regain one expended spell slot of a level no higher than half your proficiency bonus (rounded up). " +
                "Uses: 1 at 2nd level, 2 at 6th, 3 at 18th (long rest).",
                "Channel Divinity", optional: true),

            Asi(4),
            F(4, "Cantrip Versatility",
                "When you gain an ASI in this class, you can replace one cleric cantrip with another from the cleric spell list.",
                "On ASI", optional: true),

            F(5, "Destroy Undead",
                "When an undead fails its save against your Turn Undead, it is destroyed if its CR is at or below a threshold: " +
                "CR 1/2 (5th), CR 1 (8th), CR 2 (11th), CR 3 (14th), CR 4 (17th).",
                "Passive (with Turn Undead)"),

            F(6, "Channel Divinity (2 uses)",
                "You can use Channel Divinity twice between rests.",
                "2/short rest"),
            F(6, "Divine Domain Feature",
                "You gain the 6th-level feature of your Divine Domain.",
                "Varies by domain", subclass: true),

            Asi(8),
            F(8, "Divine Domain Feature",
                "You gain the 8th-level domain feature (typically Divine Strike or Potent Spellcasting).",
                "Varies by domain", subclass: true),
            F(8, "Blessed Strikes",
                "Optional replacement for Divine Strike/Potent Spellcasting: when a creature takes damage from your cantrip or weapon attack, deal extra 1d8 radiant damage (once per turn).",
                "1/turn", optional: true),
            F(8, "Destroy Undead (CR 1)",
                "Destroy Undead threshold improves to CR 1.",
                "Passive"),

            F(10, "Divine Intervention",
                "As an action, describe aid you seek and roll percentile dice. If you roll equal to or less than your cleric level, your deity intervenes (DM chooses the nature). " +
                "On success, can't use again for 7 days; otherwise recharge on long rest.",
                "Special"),

            F(11, "Destroy Undead (CR 2)",
                "Destroy Undead threshold improves to CR 2.",
                "Passive"),

            Asi(12),

            F(14, "Destroy Undead (CR 3)",
                "Destroy Undead threshold improves to CR 3.",
                "Passive"),

            Asi(16),

            F(17, "Divine Domain Feature",
                "You gain the 17th-level feature of your Divine Domain.",
                "Varies by domain", subclass: true),
            F(17, "Destroy Undead (CR 4)",
                "Destroy Undead threshold improves to CR 4.",
                "Passive"),

            F(18, "Channel Divinity (3 uses)",
                "You can use Channel Divinity three times between rests.",
                "3/short rest"),

            Asi(19),

            F(20, "Divine Intervention Improvement",
                "Your Divine Intervention call succeeds automatically, no roll required.",
                "Special"),
        };

        // ───────────────────────── Fighter ─────────────────────────

        private static List<ClassFeature> BuildFighter() => new()
        {
            F(1, "Fighting Style",
                "Adopt a fighting style (Archery, Defense, Dueling, Great Weapon Fighting, Protection, Two-Weapon Fighting; " +
                "Tasha's also adds Blind Fighting, Interception, Superior Technique, Thrown Weapon Fighting, Unarmed Fighting, etc.).",
                "Passive"),
            F(1, "Second Wind",
                "On your turn, use a bonus action to regain hit points equal to 1d10 + your fighter level.",
                "1/short rest"),

            F(2, "Action Surge",
                "On your turn, take one additional action. (2 uses per short rest starting at 17th level.)",
                "1/short rest (2 at 17th)"),

            F(3, "Martial Archetype",
                "Choose a Martial Archetype (Champion, Battle Master, Eldritch Knight, etc.). You gain archetype features at 3rd, 7th, 10th, 15th, and 18th level.",
                "Varies by archetype", subclass: true),

            Asi(4),
            F(4, "Martial Versatility",
                "When you gain an ASI in this class, you can replace one Fighting Style with another.",
                "On ASI", optional: true),

            F(5, "Extra Attack",
                "You can attack twice, instead of once, whenever you take the Attack action on your turn. (Three times at 11th, four at 20th.)",
                "Passive"),

            Asi(6),

            F(7, "Martial Archetype Feature",
                "You gain the 7th-level feature of your Martial Archetype.",
                "Varies by archetype", subclass: true),

            Asi(8),

            F(9, "Indomitable",
                "You can reroll a saving throw that you fail. You must use the new roll. 1 use per long rest; 2 at 13th; 3 at 17th.",
                "1/long rest (scales)"),

            F(10, "Martial Archetype Feature",
                "You gain the 10th-level feature of your Martial Archetype.",
                "Varies by archetype", subclass: true),

            F(11, "Extra Attack (2)",
                "You can attack three times whenever you take the Attack action on your turn.",
                "Passive"),

            Asi(12),

            F(13, "Indomitable (2 uses)",
                "You can use Indomitable twice per long rest.",
                "2/long rest"),

            Asi(14),

            F(15, "Martial Archetype Feature",
                "You gain the 15th-level feature of your Martial Archetype.",
                "Varies by archetype", subclass: true),

            Asi(16),

            F(17, "Action Surge (2 uses)",
                "You can use Action Surge twice before a rest, but only once on the same turn.",
                "2/short rest"),
            F(17, "Indomitable (3 uses)",
                "You can use Indomitable three times per long rest.",
                "3/long rest"),

            F(18, "Martial Archetype Feature",
                "You gain the 18th-level feature of your Martial Archetype.",
                "Varies by archetype", subclass: true),

            Asi(19),

            F(20, "Extra Attack (3)",
                "You can attack four times whenever you take the Attack action on your turn.",
                "Passive"),
        };

        // ───────────────────────── Wizard ─────────────────────────

        private static List<ClassFeature> BuildWizard() => new()
        {
            F(1, "Spellcasting",
                "Cast wizard spells using Intelligence. Spellbook starts with six 1st-level spells. " +
                "Prepare Int modifier + wizard level spells. Ritual casting from the book. Arcane focus allowed.",
                "Spell slots (long rest)"),
            F(1, "Arcane Recovery",
                "Once per day when you finish a short rest, recover expended spell slots with a combined level equal to or less than half your wizard level (rounded up), and none of 6th level or higher.",
                "1/day"),

            F(2, "Arcane Tradition",
                "Choose an Arcane Tradition (school). You gain tradition features at 2nd, 6th, 10th, and 14th level.",
                "Varies by tradition", subclass: true),

            Asi(4),
            F(4, "Cantrip Formulas",
                "When you gain an ASI in this class, you can replace one wizard cantrip with another from the wizard spell list.",
                "On ASI", optional: true),

            F(6, "Arcane Tradition Feature",
                "You gain the 6th-level feature of your Arcane Tradition.",
                "Varies by tradition", subclass: true),

            Asi(8),

            F(10, "Arcane Tradition Feature",
                "You gain the 10th-level feature of your Arcane Tradition.",
                "Varies by tradition", subclass: true),

            Asi(12),

            F(14, "Arcane Tradition Feature",
                "You gain the 14th-level feature of your Arcane Tradition.",
                "Varies by tradition", subclass: true),

            Asi(16),

            F(18, "Spell Mastery",
                "Choose a 1st-level and a 2nd-level wizard spell from your spellbook. You can cast those spells at their lowest level without expending a slot when you have them prepared. " +
                "You can cast them with a higher slot as normal. After a long rest you may swap either spell by studying your book for 8 hours.",
                "At will (prepared)"),

            Asi(19),

            F(20, "Signature Spells",
                "Choose two 3rd-level wizard spells in your spellbook as signature spells. They are always prepared (don't count against prepared) and you can cast each once at 3rd level without a slot; regain those uses on short or long rest.",
                "1 free cast each/short rest"),
        };

        // ───────────────────────── Barbarian ─────────────────────────

        private static List<ClassFeature> BuildBarbarian() => new()
        {
            F(1, "Rage",
                "Bonus action to rage: advantage on Strength checks/saves, bonus rage melee damage, resistance to bludgeoning/piercing/slashing. " +
                "Ends early if you don't attack or take damage. Uses: 2 (1st), 3 (3rd), 4 (6th), 5 (12th), 6 (17th), unlimited (20th).",
                "2/long rest (scales)"),
            F(1, "Unarmored Defense",
                "While not wearing armor, AC = 10 + Dexterity modifier + Constitution modifier. You can use a shield and still gain this benefit.",
                "Passive"),

            F(2, "Reckless Attack",
                "When you make your first attack on your turn, you can attack recklessly: advantage on melee weapon attacks using Strength this turn, " +
                "but attack rolls against you have advantage until your next turn.",
                "At will"),
            F(2, "Danger Sense",
                "Advantage on Dexterity saving throws against effects you can see (traps, spells, etc.), if not blinded, deafened, or incapacitated.",
                "Passive"),

            F(3, "Primal Path",
                "Choose a Primal Path. You gain path features at 3rd, 6th, 10th, and 14th level.",
                "Varies by path", subclass: true),
            F(3, "Primal Knowledge",
                "You gain proficiency in one skill from the barbarian skill list (or another skill of your choice per Tasha's).",
                "Passive", optional: true),

            Asi(4),

            F(5, "Extra Attack",
                "You can attack twice, instead of once, whenever you take the Attack action on your turn.",
                "Passive"),
            F(5, "Fast Movement",
                "Your speed increases by 10 feet while you aren't wearing heavy armor.",
                "Passive"),

            F(6, "Primal Path Feature",
                "You gain the 6th-level feature of your Primal Path.",
                "Varies by path", subclass: true),

            F(7, "Feral Instinct",
                "You have advantage on initiative rolls. Additionally, if you are surprised at the start of combat and aren't incapacitated, " +
                "you can act normally on your first turn if you enter your rage before doing anything else on that turn.",
                "Passive"),
            F(7, "Instinctive Pounce",
                "As part of the bonus action you take to enter your rage, you can move up to half your speed.",
                "With Rage", optional: true),

            Asi(8),

            F(9, "Brutal Critical",
                "You can roll one additional weapon damage die when determining the extra damage for a critical hit with a melee attack. " +
                "Two extra dice at 13th level, three at 17th.",
                "Passive"),

            F(10, "Primal Path Feature",
                "You gain the 10th-level feature of your Primal Path.",
                "Varies by path", subclass: true),

            F(11, "Relentless Rage",
                "If you drop to 0 hit points while raging and don't die outright, you can make a DC 10 Constitution saving throw. " +
                "On a success, you drop to 1 hit point instead. Each time you use this after the first before a long rest, the DC increases by 5. Resets on long rest.",
                "While raging"),

            Asi(12),

            F(13, "Brutal Critical (2 dice)",
                "Brutal Critical improves to two extra weapon damage dice on a melee critical hit.",
                "Passive"),

            F(14, "Primal Path Feature",
                "You gain the 14th-level feature of your Primal Path.",
                "Varies by path", subclass: true),

            F(15, "Persistent Rage",
                "Your rage is so fierce that it ends early only if you fall unconscious or if you choose to end it.",
                "Passive"),

            Asi(16),

            F(17, "Brutal Critical (3 dice)",
                "Brutal Critical improves to three extra weapon damage dice on a melee critical hit.",
                "Passive"),

            F(18, "Indomitable Might",
                "If your total for a Strength check is less than your Strength score, you can use that score in place of the total.",
                "Passive"),

            Asi(19),

            F(20, "Primal Champion",
                "Your Strength and Constitution scores increase by 4. Your maximum for those scores is now 24.",
                "Passive"),
        };

        // ───────────────────────── Bard ─────────────────────────

        private static List<ClassFeature> BuildBard() => new()
        {
            F(1, "Spellcasting",
                "Cast bard spells using Charisma. Ritual casting. Musical instrument as focus. " +
                "Know a limited number of spells; slots per bard table.",
                "Spell slots (long rest)"),
            F(1, "Bardic Inspiration",
                "Bonus action: give one creature within 60 feet a Bardic Inspiration die (d6; d8 at 5th, d10 at 10th, d12 at 15th) " +
                "to add to one ability check, attack roll, or saving throw within 10 minutes. Uses = Charisma modifier (min 1) per long rest; " +
                "short rest recovery from 5th level (Font of Inspiration).",
                "Cha mod / long rest (scales)"),

            F(2, "Jack of All Trades",
                "Add half your proficiency bonus (rounded down) to any ability check that doesn't already include your proficiency bonus.",
                "Passive"),
            F(2, "Song of Rest",
                "If you or friendly creatures who can hear your performance regain hit points at the end of a short rest by spending Hit Dice, " +
                "each regains extra hit points: d6 (2nd), d8 (9th), d10 (13th), d12 (17th).",
                "Short rest"),
            F(2, "Magical Inspiration",
                "A creature with a Bardic Inspiration die can use it when casting a spell that restores hit points or deals damage, adding the die to one roll of the spell.",
                "With Bardic Inspiration", optional: true),

            F(3, "Bard College",
                "Choose a Bard College. You gain college features at 3rd, 6th, and 14th level.",
                "Varies by college", subclass: true),
            F(3, "Expertise",
                "Choose two of your skill proficiencies. Your proficiency bonus is doubled for any ability check using either. " +
                "Choose two more at 10th level.",
                "Passive"),

            Asi(4),
            F(4, "Bardic Versatility",
                "When you gain an ASI in this class, you can replace one skill expertise choice or one cantrip from this class.",
                "On ASI", optional: true),

            F(5, "Bardic Inspiration (d8)",
                "Your Bardic Inspiration die becomes a d8.",
                "Passive"),
            F(5, "Font of Inspiration",
                "You regain all expended uses of Bardic Inspiration when you finish a short or long rest.",
                "Short rest"),

            F(6, "Countercharm",
                "As an action, you and friendly creatures within 30 feet that can hear you have advantage on saving throws against being frightened or charmed until the end of your next turn.",
                "Action"),
            F(6, "Bard College Feature",
                "You gain the 6th-level feature of your Bard College.",
                "Varies by college", subclass: true),

            Asi(8),

            F(10, "Bardic Inspiration (d10)",
                "Your Bardic Inspiration die becomes a d10.",
                "Passive"),
            F(10, "Expertise",
                "Choose two more skill proficiencies to gain Expertise.",
                "Passive"),
            F(10, "Magical Secrets",
                "Choose two spells from any class (including this one), of a level you can cast. They count as bard spells for you and don't count against spells known. " +
                "Again at 14th and 18th level.",
                "Passive"),

            Asi(12),

            F(14, "Magical Secrets",
                "Learn two additional spells from any class as Magical Secrets.",
                "Passive"),
            F(14, "Bard College Feature",
                "You gain the 14th-level feature of your Bard College.",
                "Varies by college", subclass: true),

            F(15, "Bardic Inspiration (d12)",
                "Your Bardic Inspiration die becomes a d12.",
                "Passive"),

            Asi(16),

            F(18, "Magical Secrets",
                "Learn two additional spells from any class as Magical Secrets.",
                "Passive"),

            Asi(19),

            F(20, "Superior Inspiration",
                "When you roll initiative and have no uses of Bardic Inspiration left, you regain one use.",
                "Passive"),
        };

        // ───────────────────────── Druid ─────────────────────────

        private static List<ClassFeature> BuildDruid() => new()
        {
            F(1, "Druidic",
                "You know Druidic, the secret language of druids. You can speak it and leave hidden messages that others spot with a DC 15 Perception check but only druids understand.",
                "Passive"),
            F(1, "Spellcasting",
                "Cast druid spells using Wisdom. Prepare Wis modifier + druid level spells. Ritual casting. Druidic focus allowed. " +
                "Cannot wear metal armor or use metal shields (tradition).",
                "Spell slots (long rest)"),

            F(2, "Wild Shape",
                "As an action, magically assume the shape of a beast you have seen. Uses: 2 per short or long rest. " +
                "CR and limitations improve with level (no fly/swim early; see PHB table). Duration = half druid level hours.",
                "2/short rest"),
            F(2, "Druid Circle",
                "Choose a Druid Circle. You gain circle features at 2nd, 6th, 10th, and 14th level.",
                "Varies by circle", subclass: true),
            F(2, "Wild Companion",
                "You can expend a use of Wild Shape to cast Find Familiar as a ritual (without material components); the familiar is a fey instead of a beast.",
                "Wild Shape use", optional: true),

            Asi(4),
            F(4, "Wild Shape Improvement",
                "You can transform into beasts with a swim speed (CR limits per PHB table).",
                "Passive"),
            F(4, "Cantrip Versatility",
                "When you gain an ASI in this class, you can replace one druid cantrip with another from the druid list.",
                "On ASI", optional: true),

            F(6, "Druid Circle Feature",
                "You gain the 6th-level feature of your Druid Circle.",
                "Varies by circle", subclass: true),

            Asi(8),
            F(8, "Wild Shape Improvement",
                "You can transform into beasts with a fly speed (CR limits per PHB table).",
                "Passive"),

            F(10, "Druid Circle Feature",
                "You gain the 10th-level feature of your Druid Circle.",
                "Varies by circle", subclass: true),

            Asi(12),

            F(14, "Druid Circle Feature",
                "You gain the 14th-level feature of your Druid Circle.",
                "Varies by circle", subclass: true),

            Asi(16),

            F(18, "Timeless Body",
                "The primal magic you wield causes you to age more slowly. For every 10 years that pass, your body ages only 1 year.",
                "Passive"),
            F(18, "Beast Spells",
                "You can cast many of your druid spells in any shape you assume using Wild Shape. You can perform the somatic and verbal components while in a beast shape, " +
                "but you aren't able to provide material components.",
                "Passive"),

            Asi(19),

            F(20, "Archdruid",
                "You can use Wild Shape an unlimited number of times. Additionally, you can ignore the verbal and somatic components of your druid spells, " +
                "as well as any material components that lack a cost and aren't consumed. You gain this benefit in both your normal shape and your beast shape from Wild Shape.",
                "Passive"),
        };

        // ───────────────────────── Monk ─────────────────────────

        private static List<ClassFeature> BuildMonk() => new()
        {
            F(1, "Unarmored Defense",
                "While you are wearing no armor and not wielding a shield, your AC equals 10 + Dexterity modifier + Wisdom modifier.",
                "Passive"),
            F(1, "Martial Arts",
                "While unarmed or wielding only monk weapons and unarmored: use Dex for attack/damage; martial arts die for unarmed/monk weapon damage (d4→d10 by level); " +
                "bonus action unarmed strike after Attack action.",
                "Passive"),

            F(2, "Ki",
                "You have ki points equal to your monk level. Spend them on Flurry of Blows, Patient Defense, and Step of the Wind. Regain all on short or long rest. Save DC = 8 + prof + Wis.",
                "Monk level points / short rest"),
            F(2, "Unarmored Movement",
                "Your speed increases by 10 feet while not wearing armor or wielding a shield. Improves at 6th (+15), 10th (+20), 14th (+25), 18th (+30). " +
                "At 9th level you can move along vertical surfaces and across liquids on your turn without falling during the move.",
                "Passive"),
            F(2, "Dedicated Weapon",
                "You can touch one simple or martial weapon to make it a monk weapon until you use this again or aren't proficient with it.",
                "Short rest", optional: true),

            F(3, "Monastic Tradition",
                "Choose a Monastic Tradition. You gain tradition features at 3rd, 6th, 11th, and 17th level.",
                "Varies by tradition", subclass: true),
            F(3, "Deflect Missiles",
                "Reaction when hit by a ranged weapon attack: reduce damage by 1d10 + Dex mod + monk level. If reduced to 0, you can catch the missile (free hand, fitting size) " +
                "and spend 1 ki to make a ranged attack with it as part of the same reaction.",
                "Reaction"),
            F(3, "Ki-Fueled Attack",
                "If you spend 1 ki or more as part of your action on your turn, you can make one unarmed strike or monk weapon attack as a bonus action.",
                "Bonus action", optional: true),

            Asi(4),
            F(4, "Slow Fall",
                "You can use your reaction when you fall to reduce any falling damage you take by an amount equal to five times your monk level.",
                "Reaction"),
            F(4, "Quickened Healing",
                "As an action, spend 2 ki points to regain hit points equal to your martial arts die + proficiency bonus.",
                "2 ki", optional: true),

            F(5, "Extra Attack",
                "You can attack twice, instead of once, whenever you take the Attack action on your turn.",
                "Passive"),
            F(5, "Stunning Strike",
                "When you hit another creature with a melee weapon attack, you can spend 1 ki point to attempt a stunning strike. Target Constitution save or be stunned until the end of your next turn.",
                "1 ki"),
            F(5, "Focused Aim",
                "When you miss with an attack roll, you can spend 1 to 3 ki points to increase your attack roll by 2 per ki spent, potentially turning the miss into a hit.",
                "1–3 ki", optional: true),

            F(6, "Ki-Empowered Strikes",
                "Your unarmed strikes count as magical for the purpose of overcoming resistance and immunity to nonmagical attacks and damage.",
                "Passive"),
            F(6, "Monastic Tradition Feature",
                "You gain the 6th-level feature of your Monastic Tradition.",
                "Varies by tradition", subclass: true),
            F(6, "Unarmored Movement Improvement",
                "Unarmored Movement bonus increases to +15 feet.",
                "Passive"),

            F(7, "Evasion",
                "When you are subjected to an effect that allows a Dexterity saving throw to take only half damage, you instead take no damage if you succeed, " +
                "and only half damage if you fail (unless you are incapacitated).",
                "Passive"),
            F(7, "Stillness of Mind",
                "You can use your action to end one effect on yourself that is causing you to be charmed or frightened.",
                "Action"),

            Asi(8),

            F(9, "Unarmored Movement Improvement",
                "You gain the ability to move along vertical surfaces and across liquids on your turn without falling during the move.",
                "Passive"),

            F(10, "Purity of Body",
                "Your mastery of the ki flowing through you makes you immune to disease and poison.",
                "Passive"),
            F(10, "Unarmored Movement Improvement",
                "Unarmored Movement bonus increases to +20 feet.",
                "Passive"),

            F(11, "Monastic Tradition Feature",
                "You gain the 11th-level feature of your Monastic Tradition.",
                "Varies by tradition", subclass: true),

            Asi(12),

            F(13, "Tongue of the Sun and Moon",
                "You learn to touch the ki of other minds so that you understand all spoken languages. Any creature that can understand a language can understand what you say.",
                "Passive"),

            F(14, "Diamond Soul",
                "You gain proficiency in all saving throws. Additionally, whenever you make a saving throw and fail, you can spend 1 ki point to reroll it and take the second result.",
                "Passive + 1 ki to reroll"),
            F(14, "Unarmored Movement Improvement",
                "Unarmored Movement bonus increases to +25 feet.",
                "Passive"),

            Asi(16),

            F(17, "Monastic Tradition Feature",
                "You gain the 17th-level feature of your Monastic Tradition.",
                "Varies by tradition", subclass: true),

            F(18, "Empty Body",
                "You can use your action to spend 4 ki points to become invisible for 1 minute. During that time, you also have resistance to all damage but force damage. " +
                "Also, you can spend 8 ki points to cast Astral Projection without material components, but you can't take any other creatures with you.",
                "4 or 8 ki"),
            F(18, "Unarmored Movement Improvement",
                "Unarmored Movement bonus increases to +30 feet.",
                "Passive"),

            Asi(19),

            F(20, "Perfect Self",
                "When you roll for initiative and have no ki points remaining, you regain 4 ki points.",
                "Passive"),
        };

        // ───────────────────────── Paladin ─────────────────────────

        private static List<ClassFeature> BuildPaladin() => new()
        {
            F(1, "Divine Sense",
                "As an action, open your awareness to detect celestials, fiends, and undead within 60 feet (not behind total cover) until the end of your next turn, " +
                "and sense consecrated/desecrated places or objects. Uses = 1 + Charisma modifier per long rest.",
                "1 + Cha mod / long rest"),
            F(1, "Lay on Hands",
                "You have a pool of healing power that replenishes on a long rest: 5 × paladin level hit points. As an action, touch a creature to restore HP from the pool, " +
                "or expend 5 HP from the pool to cure one disease or neutralize one poison.",
                "5 × level HP / long rest"),

            F(2, "Fighting Style",
                "Adopt a fighting style (Defense, Dueling, Great Weapon Fighting, Protection; Tasha's options include Blessed Warrior, Blind Fighting, Interception).",
                "Passive"),
            F(2, "Spellcasting",
                "Cast paladin spells using Charisma. Prepare Cha modifier + half paladin level (rounded down) spells (minimum 1). Holy symbol as focus. Half-caster slots from 2nd level.",
                "Spell slots (long rest)"),
            F(2, "Divine Smite",
                "When you hit a creature with a melee weapon attack, you can expend one spell slot to deal radiant damage to the target, in addition to the weapon's damage. " +
                "Extra 2d8 for a 1st-level slot, +1d8 per slot level above 1st (max 5d8), +1d8 vs undead or fiends.",
                "Spell slot"),

            F(3, "Divine Health",
                "The divine magic flowing through you makes you immune to disease.",
                "Passive"),
            F(3, "Sacred Oath",
                "Swear a Sacred Oath. You gain oath spells and Channel Divinity options, plus features at 3rd, 7th, 15th, and 20th level.",
                "Varies by oath", subclass: true),
            F(3, "Harness Divine Power",
                "As a bonus action, touch your holy symbol and expend a use of Channel Divinity to regain one expended spell slot of a level no higher than half your proficiency bonus (rounded up).",
                "Channel Divinity", optional: true),

            Asi(4),
            F(4, "Martial Versatility",
                "When you gain an ASI in this class, you can replace one Fighting Style with another available to paladins.",
                "On ASI", optional: true),

            F(5, "Extra Attack",
                "You can attack twice, instead of once, whenever you take the Attack action on your turn.",
                "Passive"),

            F(6, "Aura of Protection",
                "Whenever you or a friendly creature within 10 feet of you must make a saving throw, the creature gains a bonus equal to your Charisma modifier (minimum +1). " +
                "You must be conscious. Radius becomes 30 feet at 18th level.",
                "Passive"),

            F(7, "Sacred Oath Feature",
                "You gain the 7th-level feature of your Sacred Oath (often an aura).",
                "Varies by oath", subclass: true),

            Asi(8),

            F(10, "Aura of Courage",
                "You and friendly creatures within 10 feet of you can't be frightened while you are conscious. Radius becomes 30 feet at 18th level.",
                "Passive"),

            F(11, "Improved Divine Smite",
                "Whenever you hit a creature with a melee weapon, the creature takes an extra 1d8 radiant damage. If you also use Divine Smite, you add this damage to the extra damage of your Divine Smite.",
                "Passive"),

            Asi(12),

            F(14, "Cleansing Touch",
                "You can use your action to end one spell on yourself or on one willing creature that you touch. Uses = Charisma modifier (minimum 1) per long rest.",
                "Cha mod / long rest"),

            F(15, "Sacred Oath Feature",
                "You gain the 15th-level feature of your Sacred Oath.",
                "Varies by oath", subclass: true),

            Asi(16),

            F(18, "Aura Improvements",
                "The range of your auras increases to 30 feet.",
                "Passive"),

            Asi(19),

            F(20, "Sacred Oath Feature",
                "You gain the 20th-level feature of your Sacred Oath.",
                "Varies by oath", subclass: true),
        };

        // ───────────────────────── Ranger ─────────────────────────

        private static List<ClassFeature> BuildRanger() => new()
        {
            F(1, "Favored Enemy",
                "Choose a type of favored enemy. You have advantage on Wisdom (Survival) checks to track them and on Intelligence checks to recall information about them. " +
                "Learn a language of your choice associated with them. Additional enemies at 6th and 14th level.",
                "Passive"),
            F(1, "Natural Explorer",
                "Choose a favored terrain. Benefits while traveling for an hour or more in that terrain (difficult terrain doesn't slow group, advantage vs becoming lost, etc.). " +
                "Additional terrains at 6th and 10th level.",
                "Passive"),
            F(1, "Favored Foe",
                "Optional alternative to Favored Enemy (Tasha's): mark a creature when you hit it; deal extra damage once per turn equal to your proficiency bonus. " +
                "Concentration, 1 minute. Uses = proficiency bonus per long rest.",
                "Prof / long rest", optional: true),
            F(1, "Deft Explorer",
                "Optional alternative to Natural Explorer (Tasha's): Canny (expertise in one skill + two languages), Roving (speed +5, climb/swim speed at 6th), " +
                "Tireless (temp HP and reduce exhaustion at 10th).",
                "Passive", optional: true),

            F(2, "Fighting Style",
                "Adopt a fighting style (Archery, Defense, Dueling, Two-Weapon Fighting; Tasha's adds Blind Fighting, Druidic Warrior, Thrown Weapon Fighting).",
                "Passive"),
            F(2, "Spellcasting",
                "Cast ranger spells using Wisdom. Know spells from ranger list (slots as half caster from 2nd level).",
                "Spell slots (long rest)"),

            F(3, "Ranger Conclave",
                "Choose a Ranger Conclave (archetype). You gain features at 3rd, 7th, 11th, and 15th level.",
                "Varies by conclave", subclass: true),
            F(3, "Primeval Awareness",
                "Expend a spell slot as an action to sense certain creature types within 1 mile (6 in favored terrain) for 1 minute per slot level.",
                "Spell slot"),
            F(3, "Primal Awareness",
                "Optional replacement for Primeval Awareness: you learn additional spells that don't count against spells known (Speak with Animals, Beast Sense, etc. by level).",
                "Passive", optional: true),

            Asi(4),
            F(4, "Martial Versatility",
                "When you gain an ASI in this class, you can replace one Fighting Style with another available to rangers.",
                "On ASI", optional: true),

            F(5, "Extra Attack",
                "You can attack twice, instead of once, whenever you take the Attack action on your turn.",
                "Passive"),

            F(6, "Favored Enemy / Natural Explorer Improvement",
                "You choose an additional favored enemy and an additional favored terrain (if using PHB features).",
                "Passive"),

            F(7, "Ranger Conclave Feature",
                "You gain the 7th-level feature of your Ranger Conclave.",
                "Varies by conclave", subclass: true),

            Asi(8),
            F(8, "Land's Stride",
                "Moving through nonmagical difficult terrain costs you no extra movement. You can also pass through nonmagical plants without being slowed or taking damage from them. " +
                "Advantage on saves against plants magically created or manipulated to impede movement.",
                "Passive"),

            F(10, "Hide in Plain Sight",
                "You can spend 1 minute creating camouflage for yourself. Once camouflaged, you gain +10 on Dexterity (Stealth) checks as long as you remain motionless without taking actions.",
                "1 minute prep"),
            F(10, "Nature's Veil",
                "Optional replacement for Hide in Plain Sight: as a bonus action, become invisible until the start of your next turn. Uses = proficiency bonus per long rest.",
                "Prof / long rest", optional: true),
            F(10, "Natural Explorer Improvement",
                "You choose an additional favored terrain (PHB feature).",
                "Passive"),

            F(11, "Ranger Conclave Feature",
                "You gain the 11th-level feature of your Ranger Conclave.",
                "Varies by conclave", subclass: true),

            Asi(12),

            F(14, "Vanish",
                "You can use the Hide action as a bonus action on your turn. Also, you can't be tracked by nonmagical means, unless you choose to leave a trail.",
                "Passive / bonus action Hide"),
            F(14, "Favored Enemy Improvement",
                "You choose an additional favored enemy (PHB feature).",
                "Passive"),

            F(15, "Ranger Conclave Feature",
                "You gain the 15th-level feature of your Ranger Conclave.",
                "Varies by conclave", subclass: true),

            Asi(16),

            F(18, "Feral Senses",
                "When you attack a creature you can't see, your inability to see it doesn't impose disadvantage on your attack rolls against it. " +
                "You are also aware of the location of any invisible creature within 30 feet, provided the creature isn't hidden from you and you aren't blinded or deafened.",
                "Passive"),

            Asi(19),

            F(20, "Foe Slayer",
                "Once on each of your turns, you can add your Wisdom modifier to the attack roll or the damage roll of an attack you make against one of your favored enemies " +
                "(or any creature if using Favored Foe). You can choose to use this feature before or after the roll, but before any effects of the roll are applied.",
                "1/turn"),
        };

        // ───────────────────────── Rogue ─────────────────────────

        private static List<ClassFeature> BuildRogue() => new()
        {
            F(1, "Expertise",
                "Choose two of your skill proficiencies, or one skill and thieves' tools. Your proficiency bonus is doubled for any check using either. " +
                "Choose two more skills (or thieves' tools) at 6th level.",
                "Passive"),
            F(1, "Sneak Attack",
                "Once per turn, deal extra damage to one creature you hit with an attack if you have advantage, or if an ally is within 5 feet of the target " +
                "(and you don't have disadvantage). Attack must use a finesse or ranged weapon. Damage starts at 1d6 and scales with rogue level.",
                "1/turn"),
            F(1, "Thieves' Cant",
                "You know thieves' cant and can hide messages in seemingly normal conversation. Also understand thieves' guild symbols and signs.",
                "Passive"),

            F(2, "Cunning Action",
                "You can take a bonus action on each of your turns to take the Dash, Disengage, or Hide action.",
                "Bonus action"),

            F(3, "Roguish Archetype",
                "Choose a Roguish Archetype. You gain features at 3rd, 9th, 13th, and 17th level.",
                "Varies by archetype", subclass: true),
            F(3, "Steady Aim",
                "As a bonus action, give yourself advantage on your next attack roll on the current turn. You can use this only if you haven't moved during this turn, " +
                "and after you use it your speed is 0 until the end of the turn.",
                "Bonus action", optional: true),

            Asi(4),

            F(5, "Uncanny Dodge",
                "When an attacker that you can see hits you with an attack, you can use your reaction to halve the attack's damage against you.",
                "Reaction"),

            F(6, "Expertise",
                "Choose two more of your skill proficiencies (or one and thieves' tools) to gain Expertise.",
                "Passive"),

            F(7, "Evasion",
                "When you are subjected to an effect that allows a Dexterity saving throw to take only half damage, you instead take no damage if you succeed on the saving throw, " +
                "and only half damage if you fail. You can't benefit if incapacitated.",
                "Passive"),

            Asi(8),

            F(9, "Roguish Archetype Feature",
                "You gain the 9th-level feature of your Roguish Archetype.",
                "Varies by archetype", subclass: true),

            F(10, "Ability Score Improvement",
                "You gain an additional Ability Score Improvement (rogues gain ASIs at 10th as well as the usual levels).",
                "Passive"),

            F(11, "Reliable Talent",
                "Whenever you make an ability check that lets you add your proficiency bonus, you can treat a d20 roll of 9 or lower as a 10.",
                "Passive"),

            Asi(12),

            F(13, "Roguish Archetype Feature",
                "You gain the 13th-level feature of your Roguish Archetype.",
                "Varies by archetype", subclass: true),

            F(14, "Blindsense",
                "If you are able to hear, you are aware of the location of any hidden or invisible creature within 10 feet of you.",
                "Passive"),

            F(15, "Slippery Mind",
                "You gain proficiency in Wisdom saving throws.",
                "Passive"),

            Asi(16),

            F(17, "Roguish Archetype Feature",
                "You gain the 17th-level feature of your Roguish Archetype.",
                "Varies by archetype", subclass: true),

            F(18, "Elusive",
                "No attack roll has advantage against you while you aren't incapacitated.",
                "Passive"),

            Asi(19),

            F(20, "Stroke of Luck",
                "If your attack misses a target within range, you can turn the miss into a hit. Alternatively, if you fail an ability check, you can treat the d20 roll as a 20. " +
                "Once you use this feature, you can't use it again until you finish a short or long rest.",
                "1/short rest"),
        };

        // ───────────────────────── Sorcerer ─────────────────────────

        private static List<ClassFeature> BuildSorcerer() => new()
        {
            F(1, "Spellcasting",
                "Cast sorcerer spells using Charisma. You know a limited list of spells and cantrips. Arcane focus allowed. Slots per sorcerer table.",
                "Spell slots (long rest)"),
            F(1, "Sorcerous Origin",
                "Choose a Sorcerous Origin. You gain origin features at 1st, 6th, 14th, and 18th level.",
                "Varies by origin", subclass: true),

            F(2, "Font of Magic",
                "You have sorcery points equal to your sorcerer level. Convert between sorcery points and spell slots; fuel Metamagic. Regain points on long rest.",
                "Sorcerer level points / long rest"),

            F(3, "Metamagic",
                "You gain two Metamagic options of your choice (Careful, Distant, Empowered, Extended, Heightened, Quickened, Subtle, Twinned, etc.). " +
                "Gain one more at 10th and 17th level.",
                "Sorcery points"),

            Asi(4),
            F(4, "Sorcerous Versatility",
                "When you gain an ASI in this class, you can replace one cantrip from this class or one Metamagic option.",
                "On ASI", optional: true),
            F(4, "Magical Guidance",
                "When you make an ability check that fails, you can spend 1 sorcery point to reroll the d20, and you must use the new roll.",
                "1 SP", optional: true),

            F(6, "Sorcerous Origin Feature",
                "You gain the 6th-level feature of your Sorcerous Origin.",
                "Varies by origin", subclass: true),

            Asi(8),

            F(10, "Metamagic",
                "You learn one additional Metamagic option.",
                "Sorcery points"),

            Asi(12),

            F(14, "Sorcerous Origin Feature",
                "You gain the 14th-level feature of your Sorcerous Origin.",
                "Varies by origin", subclass: true),

            Asi(16),

            F(17, "Metamagic",
                "You learn one additional Metamagic option.",
                "Sorcery points"),

            F(18, "Sorcerous Origin Feature",
                "You gain the 18th-level feature of your Sorcerous Origin.",
                "Varies by origin", subclass: true),

            Asi(19),

            F(20, "Sorcerous Restoration",
                "You regain 4 expended sorcery points whenever you finish a short rest.",
                "Short rest"),
        };

        // ───────────────────────── Warlock ─────────────────────────

        private static List<ClassFeature> BuildWarlock() => new()
        {
            F(1, "Otherworldly Patron",
                "You have struck a bargain with an otherworldly being. Choose a patron. You gain patron features at 1st, 6th, 10th, and 14th level, plus an expanded spell list.",
                "Varies by patron", subclass: true),
            F(1, "Pact Magic",
                "Cast warlock spells using Charisma. You have a small number of spell slots that are all the same level and recharge on a short or long rest. " +
                "Know a limited number of cantrips and spells. Arcane focus allowed.",
                "Pact slots (short rest)"),

            F(2, "Eldritch Invocations",
                "You gain two eldritch invocations of your choice (prerequisites apply). Gain additional invocations at higher levels (2→5→7→9→12→15→18). " +
                "You can replace one invocation when you gain a level in this class.",
                "Passive / varies"),

            F(3, "Pact Boon",
                "Your patron bestows a gift: Pact of the Chain, Pact of the Blade, or Pact of the Tome (Tasha's also includes Pact of the Talisman).",
                "Passive"),

            Asi(4),
            F(4, "Eldritch Versatility",
                "When you gain an ASI in this class, you can replace one cantrip, one Pact Boon choice (with restrictions), or one spell from Pact of the Tome/Talisman options.",
                "On ASI", optional: true),

            F(5, "Eldritch Invocations",
                "You learn one additional eldritch invocation.",
                "Passive / varies"),

            F(6, "Otherworldly Patron Feature",
                "You gain the 6th-level feature of your Otherworldly Patron.",
                "Varies by patron", subclass: true),

            Asi(8),

            F(7, "Eldritch Invocations",
                "You learn one additional eldritch invocation.",
                "Passive / varies"),

            F(9, "Eldritch Invocations",
                "You learn one additional eldritch invocation.",
                "Passive / varies"),

            F(10, "Otherworldly Patron Feature",
                "You gain the 10th-level feature of your Otherworldly Patron.",
                "Varies by patron", subclass: true),

            F(11, "Mystic Arcanum (6th level)",
                "Choose one 6th-level spell from the warlock spell list as an arcanum. You can cast it once without a slot; regain the ability on a long rest. " +
                "Higher arcanums at 13th (7th), 15th (8th), and 17th (9th).",
                "1/long rest"),

            Asi(12),
            F(12, "Eldritch Invocations",
                "You learn one additional eldritch invocation.",
                "Passive / varies"),

            F(13, "Mystic Arcanum (7th level)",
                "Choose one 7th-level warlock spell as a Mystic Arcanum (1/long rest).",
                "1/long rest"),

            F(14, "Otherworldly Patron Feature",
                "You gain the 14th-level feature of your Otherworldly Patron.",
                "Varies by patron", subclass: true),

            F(15, "Mystic Arcanum (8th level)",
                "Choose one 8th-level warlock spell as a Mystic Arcanum (1/long rest).",
                "1/long rest"),
            F(15, "Eldritch Invocations",
                "You learn one additional eldritch invocation.",
                "Passive / varies"),

            Asi(16),

            F(17, "Mystic Arcanum (9th level)",
                "Choose one 9th-level warlock spell as a Mystic Arcanum (1/long rest).",
                "1/long rest"),

            F(18, "Eldritch Invocations",
                "You learn one additional eldritch invocation.",
                "Passive / varies"),

            Asi(19),

            F(20, "Eldritch Master",
                "You can spend 1 minute entreating your patron to regain all expended pact magic spell slots. Once you regain slots with this feature, you must finish a long rest before you can do so again.",
                "1/long rest"),
        };

        // ───────────────────────── Artificer (ERftLW / Tasha's) ─────────────────────────
        // https://dnd5e.wikidot.com/artificer

        private static List<ClassFeature> BuildArtificer() => new()
        {
            F(1, "Magical Tinkering",
                "Touch a Tiny nonmagical object and give it one of several minor properties (light, message, odor, picture, etc.). " +
                "You can affect a number of objects equal to your Intelligence modifier (minimum 1).",
                "At will"),
            F(1, "Spellcasting",
                "Cast artificer spells using Intelligence. Prepare Int modifier + half artificer level (rounded up) spells. " +
                "Focus: thieves' tools or artisan's tools you're proficient with (or an infused item). Half-caster; spell slots from 1st level.",
                "Spell slots (long rest)"),

            F(2, "Infuse Item",
                "You learn a number of infusions and can imbue nonmagical objects with them at the end of a long rest. " +
                "Infused items count as magic items. Infusions known and items infused increase with level (see Artificer table).",
                "After long rest"),

            F(3, "Artificer Specialist",
                "Choose an Artificer Specialist (Alchemist, Artillerist, Battle Smith, Armorer, etc.). You gain specialist features at 3rd, 5th, 9th, and 15th level.",
                "Varies by specialist", subclass: true),
            F(3, "The Right Tool for the Job",
                "With thieves' tools or artisan's tools in hand, you can magically create one set of artisan's tools in an unoccupied space within 5 feet. " +
                "They vanish when you use this feature again, after 1 hour, or if you die.",
                "At will (replaces previous)"),

            Asi(4),

            F(5, "Artificer Specialist Feature",
                "You gain the 5th-level feature of your Artificer Specialist (often Extra Attack or similar).",
                "Varies by specialist", subclass: true),

            F(6, "Tool Expertise",
                "Your proficiency bonus is doubled for any ability check you make that uses your proficiency with a tool.",
                "Passive"),

            Asi(8),

            F(7, "Flash of Genius",
                "When you or another creature you can see within 30 feet makes an ability check or saving throw, you can use your reaction to add your Intelligence modifier to the roll. " +
                "Uses = Intelligence modifier (minimum 1) per long rest.",
                "Int mod / long rest"),

            F(9, "Artificer Specialist Feature",
                "You gain the 9th-level feature of your Artificer Specialist.",
                "Varies by specialist", subclass: true),

            F(10, "Magic Item Adept",
                "You can attune to up to four magic items at once. Crafting a common or uncommon magic item takes a quarter of the normal time and half the gold cost.",
                "Passive"),

            Asi(12),

            F(11, "Spell-Storing Item",
                "At the end of a long rest, store one artificer spell of 1st or 2nd level in an item (must have spell slots of that level). " +
                "A creature holding the item can use an action to produce the spell's effect using your spellcasting ability; the item can cast it Int mod times (min 1), then the spell is lost until you use this feature again.",
                "1 stored spell / long rest"),

            F(14, "Magic Item Savant",
                "You can attune to up to five magic items at once. You ignore all class, race, spell, and level requirements on attuning to or using a magic item.",
                "Passive"),

            F(15, "Artificer Specialist Feature",
                "You gain the 15th-level feature of your Artificer Specialist.",
                "Varies by specialist", subclass: true),

            Asi(16),

            F(18, "Magic Item Master",
                "You can attune to up to six magic items at once.",
                "Passive"),

            Asi(19),

            F(20, "Soul of Artifice",
                "You gain a +1 bonus to all saving throws per magic item you are currently attuned to. " +
                "If you drop to 0 hit points but aren't killed outright, you can use your reaction to end one of your artificer infusions, drop to 1 hit point instead, and expend that infusion.",
                "Passive + reaction"),
        };
    }
}
