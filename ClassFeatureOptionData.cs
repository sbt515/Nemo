using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>Category of level-up selectable class options.</summary>
    public enum ClassFeatureOptionKind
    {
        FightingStyle = 0,
        EldritchInvocation = 1,
        Metamagic = 2,
        PactBoon = 3
    }

    /// <summary>One selectable option (fighting style, invocation, metamagic, …).</summary>
    public sealed class ClassFeatureOption
    {
        public string Name { get; init; } = "";
        public ClassFeatureOptionKind Kind { get; init; }
        /// <summary>Minimum class level required (warlock class level for invocations).</summary>
        public int MinClassLevel { get; init; } = 1;
        /// <summary>Optional requirement text (e.g. "Pact of the Blade", "Eldritch Blast").</summary>
        public string Prerequisite { get; init; } = "";
        public string Description { get; init; } = "";
        /// <summary>Classes that can take this option (empty = kind default).</summary>
        public string[] Classes { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Official 5e selectable class options and how many are known by class level.
    /// Fighting Styles (Fighter + Champion), Eldritch Invocations (Warlock), Metamagic (Sorcerer).
    /// </summary>
    public static class ClassFeatureOptionData
    {
        // ───────────────────────── Counts by class level ─────────────────────────

        /// <summary>PHB Warlock: Invocations Known column.</summary>
        public static int GetWarlockInvocationsKnown(int warlockLevel)
        {
            if (warlockLevel < 2) return 0;
            int lvl = Math.Clamp(warlockLevel, 1, 20);
            // 2:2, 5:3, 7:4, 9:5, 12:6, 15:7, 18:8
            return lvl switch
            {
                >= 18 => 8,
                >= 15 => 7,
                >= 12 => 6,
                >= 9 => 5,
                >= 7 => 4,
                >= 5 => 3,
                _ => 2 // 2–4
            };
        }

        /// <summary>PHB Sorcerer: 2 at 3rd, +1 at 10th, +1 at 17th.</summary>
        public static int GetSorcererMetamagicKnown(int sorcererLevel)
        {
            if (sorcererLevel < 3) return 0;
            int n = 2;
            if (sorcererLevel >= 10) n++;
            if (sorcererLevel >= 17) n++;
            return n;
        }

        /// <summary>
        /// Fighter: 1 Fighting Style at 1st.
        /// Champion subclass: Additional Fighting Style at 10th (+1).
        /// </summary>
        public static int GetFighterFightingStylesKnown(int fighterLevel, string? subclass)
        {
            if (fighterLevel < 1) return 0;
            int n = 1;
            if (fighterLevel >= 10 &&
                !string.IsNullOrWhiteSpace(subclass) &&
                subclass.Contains("Champion", StringComparison.OrdinalIgnoreCase))
                n++;
            return n;
        }

        /// <summary>Paladin / Ranger gain Fighting Style at class level 2.</summary>
        public static int GetPaladinOrRangerFightingStylesKnown(int classLevel) =>
            classLevel >= 2 ? 1 : 0;

        // ───────────────────────── Catalogs ─────────────────────────

        public static IReadOnlyList<ClassFeatureOption> AllFightingStyles { get; } = BuildFightingStyles();
        public static IReadOnlyList<ClassFeatureOption> AllMetamagic { get; } = BuildMetamagic();
        public static IReadOnlyList<ClassFeatureOption> AllInvocations { get; } = BuildInvocations();
        public static IReadOnlyList<ClassFeatureOption> AllPactBoons { get; } = BuildPactBoons();

        public static IReadOnlyList<ClassFeatureOption> GetFightingStylesForClass(string className)
        {
            string c = (className ?? "").Trim();
            return AllFightingStyles
                .Where(o => o.Classes.Length == 0 ||
                            o.Classes.Any(x => x.Equals(c, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(o => o.Name)
                .ToList();
        }

        public static IReadOnlyList<ClassFeatureOption> GetAvailableInvocations(
            int warlockLevel,
            string? pactBoon,
            IEnumerable<string>? alreadyChosen = null)
        {
            var chosen = new HashSet<string>(
                alreadyChosen ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            return AllInvocations
                .Where(inv => inv.MinClassLevel <= warlockLevel)
                .Where(inv => MeetsInvocationPrerequisite(inv, warlockLevel, pactBoon))
                .OrderBy(o => o.MinClassLevel)
                .ThenBy(o => o.Name)
                .ToList();
        }

        private static bool MeetsInvocationPrerequisite(
            ClassFeatureOption inv, int warlockLevel, string? pactBoon)
        {
            if (string.IsNullOrWhiteSpace(inv.Prerequisite))
                return true;

            string p = inv.Prerequisite;
            if (p.Contains("Pact of the Blade", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(pactBoon) &&
                       pactBoon.Contains("Blade", StringComparison.OrdinalIgnoreCase);
            if (p.Contains("Pact of the Chain", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(pactBoon) &&
                       pactBoon.Contains("Chain", StringComparison.OrdinalIgnoreCase);
            if (p.Contains("Pact of the Tome", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(pactBoon) &&
                       pactBoon.Contains("Tome", StringComparison.OrdinalIgnoreCase);
            // Eldritch Blast prerequisite is soft (player may know it)
            return true;
        }

        /// <summary>
        /// Resize a pick list to <paramref name="expectedCount"/>, keeping prior valid choices.
        /// </summary>
        public static List<string> ReconcilePicks(
            IEnumerable<string>? existing,
            int expectedCount,
            Func<string, bool>? stillValid = null)
        {
            var kept = new List<string>();
            if (existing != null)
            {
                foreach (var name in existing)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (stillValid != null && !stillValid(name)) continue;
                    if (kept.Any(k => k.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
                    kept.Add(name.Trim());
                }
            }

            while (kept.Count > expectedCount)
                kept.RemoveAt(kept.Count - 1);
            while (kept.Count < expectedCount)
                kept.Add("");
            return kept;
        }

        // ───────────────────────── Builders ─────────────────────────

        private static List<ClassFeatureOption> BuildFightingStyles() => new()
        {
            FS("Archery", "Fighter,Ranger",
                "You gain a +2 bonus to attack rolls you make with ranged weapons."),
            FS("Blind Fighting", "Fighter,Paladin,Ranger",
                "You have blindsight with a range of 10 feet."),
            FS("Defense", "Fighter,Paladin,Ranger",
                "While you are wearing armor, you gain a +1 bonus to AC."),
            FS("Dueling", "Fighter,Paladin,Ranger",
                "When you are wielding a melee weapon in one hand and no other weapons, " +
                "you gain a +2 bonus to damage rolls with that weapon."),
            FS("Great Weapon Fighting", "Fighter,Paladin",
                "When you roll a 1 or 2 on a damage die for an attack you make with a melee weapon " +
                "that you are wielding with two hands, you can reroll the die and must use the new roll."),
            FS("Interception", "Fighter,Paladin",
                "When a creature you can see hits a target (other than you) within 5 feet of you with an attack, " +
                "you can use your reaction to reduce the damage by 1d10 + your proficiency bonus (must be wielding a shield or simple/martial weapon)."),
            FS("Protection", "Fighter,Paladin",
                "When a creature you can see attacks a target other than you that is within 5 feet of you, " +
                "you can use your reaction to impose disadvantage on the attack roll (must be wielding a shield)."),
            FS("Two-Weapon Fighting", "Fighter,Ranger",
                "When you engage in two-weapon fighting, you can add your ability modifier to the damage of the second attack."),
            FS("Thrown Weapon Fighting", "Fighter,Ranger",
                "You can draw a weapon that has the thrown property as part of the attack you make with the weapon. " +
                "+2 damage when you hit with a thrown weapon."),
            FS("Unarmed Fighting", "Fighter",
                "Your unarmed strikes can deal bludgeoning damage equal to 1d6 + your Strength modifier " +
                "(1d8 if you aren't wielding any weapons or a shield)."),
            FS("Blessed Warrior", "Paladin",
                "You learn two cantrips of your choice from the cleric spell list. They count as paladin spells for you."),
            FS("Druidic Warrior", "Ranger",
                "You learn two cantrips of your choice from the druid spell list. They count as ranger spells for you."),
            FS("Superior Technique", "Fighter",
                "You learn one maneuver of your choice from among those available to the Battle Master archetype. " +
                "If a maneuver requires a saving throw, DC = 8 + PB + Str or Dex mod."),
        };

        private static List<ClassFeatureOption> BuildMetamagic() => new()
        {
            MM("Careful Spell",
                "When you cast a spell that forces other creatures to make a saving throw, spend 1 sorcery point " +
                "to protect a number of those creatures equal to your Charisma modifier (min 1); they auto-succeed."),
            MM("Distant Spell",
                "Spend 1 sorcery point to double the range of a spell (touch becomes 30 feet)."),
            MM("Empowered Spell",
                "When you roll damage for a spell, spend 1 sorcery point to reroll a number of damage dice " +
                "up to your Charisma modifier (min 1); you must use the new rolls. Can combine with other Metamagic."),
            MM("Extended Spell",
                "Spend 1 sorcery point to double a spell's duration (max 24 hours) if duration is 1 minute or longer."),
            MM("Heightened Spell",
                "Spend 3 sorcery points so one target of a spell has disadvantage on its first saving throw against the spell."),
            MM("Quickened Spell",
                "Spend 2 sorcery points to change a spell's casting time from 1 action to 1 bonus action."),
            MM("Subtle Spell",
                "Spend 1 sorcery point to cast without verbal or somatic components."),
            MM("Twinned Spell",
                "When you cast a spell that targets only one creature and doesn't have a range of self, " +
                "spend sorcery points equal to the spell's level (1 for cantrips) to target a second creature in range."),
        };

        private static List<ClassFeatureOption> BuildPactBoons() => new()
        {
            new ClassFeatureOption
            {
                Name = "Pact of the Chain",
                Kind = ClassFeatureOptionKind.PactBoon,
                MinClassLevel = 3,
                Description = "Learn find familiar; special forms (imp, pseudodragon, quasit, or sprite). " +
                              "When you take the Attack action, you can forgo one attack to let your familiar attack with its reaction.",
                Classes = new[] { "Warlock" }
            },
            new ClassFeatureOption
            {
                Name = "Pact of the Blade",
                Kind = ClassFeatureOptionKind.PactBoon,
                MinClassLevel = 3,
                Description = "Create a pact weapon as a bonus action (or bind a magic weapon). " +
                              "You are proficient with it; it counts as magical for overcoming resistance.",
                Classes = new[] { "Warlock" }
            },
            new ClassFeatureOption
            {
                Name = "Pact of the Tome",
                Kind = ClassFeatureOptionKind.PactBoon,
                MinClassLevel = 3,
                Description = "Your Book of Shadows grants three cantrips from any class list " +
                              "(they don't count against your number of cantrips known).",
                Classes = new[] { "Warlock" }
            },
        };

        private static List<ClassFeatureOption> BuildInvocations() => new()
        {
            // No / low prereq
            Inv("Agonizing Blast", 2, "Eldritch Blast cantrip",
                "Add your Charisma modifier to the damage of each beam of eldritch blast."),
            Inv("Armor of Shadows", 2, "",
                "Cast mage armor on yourself at will, without expending a spell slot or material components."),
            Inv("Beast Speech", 2, "",
                "Cast speak with animals at will, without expending a spell slot."),
            Inv("Beguiling Influence", 2, "",
                "Proficiency in Deception and Persuasion."),
            Inv("Devil's Sight", 2, "",
                "See normally in darkness, both magical and nonmagical, to a distance of 120 feet."),
            Inv("Eldritch Mind", 2, "",
                "Advantage on Constitution saving throws to maintain concentration on a spell."),
            Inv("Eldritch Sight", 2, "",
                "Cast detect magic at will, without expending a spell slot."),
            Inv("Eldritch Spear", 2, "Eldritch Blast cantrip",
                "Eldritch blast's range is 300 feet."),
            Inv("Eyes of the Rune Keeper", 2, "",
                "You can read all writing."),
            Inv("Fiendish Vigor", 2, "",
                "Cast false life on yourself at will as a 1st-level spell, without expending a spell slot or material components."),
            Inv("Gaze of Two Minds", 2, "",
                "Use your action to touch a willing humanoid and perceive through its senses until the end of your next turn " +
                "(extend each of your turns by using your action)."),
            Inv("Grasp of Hadar", 2, "Eldritch Blast cantrip",
                "Once on each of your turns when you hit with eldritch blast, pull the creature up to 10 feet closer to you."),
            Inv("Improved Pact Weapon", 2, "Pact of the Blade",
                "Pact weapon can be a shortbow/longbow/light/heavy crossbow. +1 to attack and damage if not already magical. " +
                "Use as spellcasting focus."),
            Inv("Investment of the Chain Master", 2, "Pact of the Chain",
                "Enhanced familiar: temp HP, special attack as bonus action, force/necrotic/radiant damage option, " +
                "or reaction to impose disadvantage."),
            Inv("Lance of Lethargy", 2, "Eldritch Blast cantrip",
                "Once on each of your turns when you hit with eldritch blast, reduce the creature's speed by 10 feet until your next turn."),
            Inv("Mask of Many Faces", 2, "",
                "Cast disguise self at will, without expending a spell slot."),
            Inv("Misty Visions", 2, "",
                "Cast silent image at will, without expending a spell slot or material components."),
            Inv("Rebuke of the Talisman", 2, "Pact of the Talisman",
                "When wearer is hit, use reaction to deal psychic damage equal to your proficiency bonus and push 10 feet."),
            Inv("Repelling Blast", 2, "Eldritch Blast cantrip",
                "When you hit with eldritch blast, push the creature up to 10 feet away from you in a straight line."),
            Inv("Thief of Five Fates", 2, "",
                "Cast bane once using a warlock spell slot; regain after long rest."),
            Inv("Voice of the Chain Master", 2, "Pact of the Chain",
                "Communicate telepathically with your familiar, perceive through its senses, and speak through it in your own voice."),

            // 5th+
            Inv("Gift of the Depths", 5, "",
                "Swim speed equal to walking speed; breathe underwater. Cast water breathing once per long rest without a slot."),
            Inv("One with Shadows", 5, "",
                "When you are in an area of dim light or darkness, use your action to become invisible until you move/act/react."),
            Inv("Sign of Ill Omen", 5, "",
                "Cast bestow curse once using a warlock slot; regain after long rest."),
            Inv("Thirsting Blade", 5, "Pact of the Blade",
                "Attack twice with your pact weapon when you take the Attack action."),
            Inv("Tomb of Levistus", 5, "",
                "As a reaction when you take damage, encase yourself in ice: temp HP = warlock level × 10, " +
                "speed 0, vulnerability to fire until end of next turn. Once per short/long rest."),
            Inv("Undying Servitude", 5, "",
                "Cast animate dead once without a slot; regain after long rest."),

            // 7th+
            Inv("Bewitching Whispers", 7, "",
                "Cast compulsion once using a warlock slot; regain after long rest."),
            Inv("Dreadful Word", 7, "",
                "Cast confusion once using a warlock slot; regain after long rest."),
            Inv("Ghostly Gaze", 7, "",
                "As an action, gain 30-foot darkvision and see through solid objects (1 SP / until end of next turn; concentration)."),
            Inv("Sculptor of Flesh", 7, "",
                "Cast polymorph once using a warlock slot; regain after long rest."),
            Inv("Trickster's Escape", 7, "",
                "Cast freedom of movement once on yourself without a slot; regain after long rest."),

            // 9th+
            Inv("Ascendant Step", 9, "",
                "Cast levitate on yourself at will, without expending a spell slot or material components."),
            Inv("Minions of Chaos", 9, "",
                "Cast conjure elemental once using a warlock slot; regain after long rest."),
            Inv("Otherworldly Leap", 9, "",
                "Cast jump on yourself at will, without expending a spell slot or material components."),
            Inv("Whispers of the Grave", 9, "",
                "Cast speak with dead at will, without expending a spell slot."),

            // 12th+
            Inv("Lifedrinker", 12, "Pact of the Blade",
                "When you hit with your pact weapon, deal extra necrotic damage equal to your Charisma modifier (min 1)."),

            // 15th+
            Inv("Chains of Carceri", 15, "Pact of the Chain",
                "Cast hold monster at will targeting a celestial, fiend, or elemental — without a slot. " +
                "Once per long rest per creature."),
            Inv("Master of Myriad Forms", 15, "",
                "Cast alter self at will, without expending a spell slot."),
            Inv("Shroud of Shadow", 15, "",
                "Cast invisibility at will, without expending a spell slot."),
            Inv("Visions of Distant Realms", 15, "",
                "Cast arcane eye at will, without expending a spell slot."),
            Inv("Witch Sight", 15, "",
                "See the true form of any shapechanger or creature concealed by illusion or transmutation magic " +
                "while within 30 feet and within line of sight."),
        };

        private static ClassFeatureOption FS(string name, string classesCsv, string desc) => new()
        {
            Name = name,
            Kind = ClassFeatureOptionKind.FightingStyle,
            MinClassLevel = 1,
            Description = desc,
            Classes = classesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        };

        private static ClassFeatureOption MM(string name, string desc) => new()
        {
            Name = name,
            Kind = ClassFeatureOptionKind.Metamagic,
            MinClassLevel = 3,
            Description = desc,
            Classes = new[] { "Sorcerer" }
        };

        private static ClassFeatureOption Inv(string name, int minLevel, string prereq, string desc) => new()
        {
            Name = name,
            Kind = ClassFeatureOptionKind.EldritchInvocation,
            MinClassLevel = minLevel,
            Prerequisite = prereq,
            Description = desc,
            Classes = new[] { "Warlock" }
        };
    }
}
