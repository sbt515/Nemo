using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>
    /// Exotic &amp; Monstrous lineages (dnd5e.wikidot.com #toc2) plus category helpers.
    /// Merged into <see cref="GameData.RaceData"/> on first access via static constructor.
    /// </summary>
    public static partial class GameData
    {
        public static readonly string[] RaceCategories = { "Common", "Exotic", "Monstrous" };

        static GameData()
        {
            foreach (var kv in BuildExpandedRaces())
            {
                if (!RaceData.ContainsKey(kv.Key))
                    RaceData[kv.Key] = kv.Value;
            }

            foreach (var kv in BuildExpandedSubraces())
            {
                if (!RaceSubraces.ContainsKey(kv.Key))
                    RaceSubraces[kv.Key] = kv.Value;
            }

            ApplyRaceCategories();
        }

        /// <summary>Race names in a category, sorted alphabetically.</summary>
        public static List<string> GetRacesInCategory(string? category)
        {
            string cat = string.IsNullOrWhiteSpace(category) ? "Common" : category.Trim();
            return RaceData
                .Where(kv => string.Equals(kv.Value.Category, cat, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Looks up which category a race belongs to (defaults to Common).</summary>
        public static string GetRaceCategory(string? raceName)
        {
            if (string.IsNullOrWhiteSpace(raceName)) return "Common";
            if (RaceData.TryGetValue(raceName.Trim(), out var data) &&
                !string.IsNullOrWhiteSpace(data.Category))
                return data.Category;
            return "Common";
        }

        private static void ApplyRaceCategories()
        {
            // ── Common (PHB core + Tasha custom) ──
            string[] common =
            {
                "Dragonborn", "Dwarf", "Elf", "Gnome", "Half-Elf", "Half-Orc",
                "Halfling", "Human", "Variant Human", "Tiefling", "Custom Lineage"
            };
            foreach (var n in common)
                if (RaceData.TryGetValue(n, out var d)) d.Category = "Common";

            // ── Exotic ──
            string[] exotic =
            {
                "Aarakocra", "Aasimar", "Changeling", "Deep Gnome", "Duergar",
                "Fairy", "Firbolg", "Genasi", "Gith", "Goliath", "Harengon",
                "Kenku", "Locathah", "Owlin", "Satyr", "Tabaxi", "Tortle",
                "Triton", "Verdan"
            };
            foreach (var n in exotic)
                if (RaceData.TryGetValue(n, out var d)) d.Category = "Exotic";

            // ── Monstrous ──
            string[] monstrous =
            {
                "Bugbear", "Centaur", "Goblin", "Grung", "Hobgoblin", "Kobold",
                "Lizardfolk", "Minotaur", "Orc", "Shifter", "Yuan-ti Pureblood"
            };
            foreach (var n in monstrous)
                if (RaceData.TryGetValue(n, out var d)) d.Category = "Monstrous";
        }

        private static Dictionary<string, RaceData> BuildExpandedRaces()
        {
            var d = new Dictionary<string, RaceData>(StringComparer.OrdinalIgnoreCase);

            // ═══════════════ EXOTIC ═══════════════

            d["Aarakocra"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Dexterity"] = 2, ["Wisdom"] = 1 },
                Speed = 25,
                Traits = new()
                {
                    "Size: Medium",
                    "Speed: Your base walking speed is 25 feet.",
                    "Flight: You have a flying speed of 50 feet. To use this speed, you can't be wearing medium or heavy armor.",
                    "Talons: Your talons are natural weapons, which you can use to make unarmed strikes. If you hit with them, you deal slashing damage equal to 1d4 + your Strength modifier, instead of the bludgeoning damage normal for an unarmed strike."
                },
                Languages = new() { "Common", "Aarakocra", "Auran" }
            };

            d["Changeling"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Charisma"] = 2 },
                Traits = new()
                {
                    "Ability Score Increase: Your Charisma score increases by 2. In addition, one ability score of your choice increases by 1.",
                    "Size: Medium",
                    "Shapechanger: As an action, you can change your appearance and your voice. You determine the specifics of the changes, including your coloration, hair length, and sex. You can make yourself appear as a member of another race, though none of your statistics change. You can't appear as a creature of a different size than you, and your basic shape stays the same. Your clothing and equipment aren't changed by this trait. You stay in the new form until you use an action to revert to your true form or until you die.",
                    "Changeling Instincts: You gain proficiency with two of the following skills of your choice: Deception, Insight, Intimidation, and Persuasion."
                },
                Languages = new() { "Common", "Two other languages of your choice" }
            };

            d["Fairy"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new(),
                Traits = new()
                {
                    "Creature Type: You are a Fey.",
                    "Size: Small",
                    "Speed: 30 feet",
                    "Ability Score Increase (Tasha-style): Increase one score by 2 and another by 1, or three different scores by 1.",
                    "Fairy Magic: You know the Druidcraft cantrip. Starting at 3rd level, you can cast Faerie Fire with this trait. Starting at 5th level, you can also cast Enlarge/Reduce with it. Once you cast Faerie Fire or Enlarge/Reduce with this trait, you can't cast that spell with it again until you finish a long rest. You can also cast either spell using any spell slots you have. Intelligence, Wisdom, or Charisma is your spellcasting ability for these spells (choose when you select this race).",
                    "Flight: Because of your wings, you have a flying speed equal to your walking speed. You can't use this flying speed if you're wearing medium or heavy armor."
                },
                Languages = new() { "Common", "One other language of your choice" },
                HasInnateSpellcasting = true
            };

            d["Firbolg"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Wisdom"] = 2, ["Strength"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Firbolg Magic: You can cast Detect Magic and Disguise Self with this trait, using Wisdom as your spellcasting ability for them. Once you cast either spell, you can't cast it again with this trait until you finish a short or long rest. When you use this version of Disguise Self, you can seem up to 3 feet shorter than normal, allowing you to more easily blend in with humans and elves.",
                    "Hidden Step: As a bonus action, you can magically turn invisible until the start of your next turn or until you attack, make a damage roll, or force someone to make a saving throw. Once you use this trait, you can't use it again until you finish a short or long rest.",
                    "Powerful Build: You count as one size larger when determining your carrying capacity and the weight you can push, drag, or lift.",
                    "Speech of Beast and Leaf: You have the ability to communicate in a limited manner with Beasts, Plants, and vegetation. They can understand the meaning of your words, though you have no special ability to understand them in return. You have advantage on all Charisma checks you make to influence them."
                },
                Languages = new() { "Common", "Elvish", "Giant" },
                HasInnateSpellcasting = true
            };

            d["Genasi"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Constitution"] = 2 },
                Traits = new()
                {
                    "Size: Medium",
                    "Ability Score Increase: Your Constitution score increases by 2. Subrace grants an additional +1.",
                    "Languages: You can speak, read, and write Common and Primordial. Primordial is a guttural language, filled with harsh syllables and hard consonants."
                },
                Languages = new() { "Common", "Primordial" }
            };

            d["Gith"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new(),
                Traits = new()
                {
                    "Size: Medium",
                    "Choose a subrace: Githyanki or Githzerai. Your subrace determines ability bonuses, traits, and languages."
                },
                Languages = new() { "Common", "Gith" }
            };

            d["Goliath"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Strength"] = 2, ["Constitution"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Natural Athlete: You have proficiency in the Athletics skill.",
                    "Stone's Endurance: You can focus yourself to occasionally shrug off injury. When you take damage, you can use your reaction to roll a d12. Add your Constitution modifier to the number rolled, and reduce the damage by that total. After you use this trait, you can't use it again until you finish a short or long rest.",
                    "Powerful Build: You count as one size larger when determining your carrying capacity and the weight you can push, drag, or lift.",
                    "Mountain Born: You're acclimated to high altitude, including elevations above 20,000 feet. You're also naturally adapted to cold climates, as described in chapter 5 of the Dungeon Master's Guide."
                },
                Languages = new() { "Common", "Giant" },
                SkillProficiencies = new() { "Athletics" }
            };

            d["Harengon"] = new RaceData
            {
                // WBtW p.13 / MPMM p.22 — Tasha-style floating ASI, no subraces
                Category = "Exotic",
                AbilityBonuses = new(),
                Speed = 30,
                Traits = new()
                {
                    "Creature Type: You are a Humanoid.",
                    "Life Span: Harengons have a life span of about a century.",
                    "Size: You are Medium or Small. You choose the size when you select this race.",
                    "Speed: Your walking speed is 30 feet.",
                    "Ability Score Increase: When determining your character's ability scores, increase one score by 2 and increase a different score by 1, or increase three different scores by 1. You can't raise any of your scores above 20.",
                    "Hare-Trigger: You can add your proficiency bonus to your initiative rolls.",
                    "Leporine Senses: You have proficiency in the Perception skill.",
                    "Lucky Footwork: When you fail a Dexterity saving throw, you can use your reaction to roll a d4 and add it to the save, potentially turning the failure into a success. You can't use this reaction if you're prone or your speed is 0.",
                    "Rabbit Hop: As a bonus action, you can jump a number of feet equal to five times your proficiency bonus without provoking opportunity attacks. You can use this trait only if your speed is greater than 0. You can use it a number of times equal to your proficiency bonus, and you regain all expended uses when you finish a long rest."
                },
                Languages = new() { "Common", "One other language of your choice" },
                SkillProficiencies = new() { "Perception" }
            };

            d["Locathah"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Strength"] = 2, ["Dexterity"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Natural Armor: You have tough, scaly skin. When you aren't wearing armor, your AC is 12 + your Dexterity modifier. You can use your natural armor to determine your AC if the armor you wear would leave you with a lower AC. A shield's benefits apply as normal while you use your natural armor.",
                    "Observant & Athletic: You have proficiency in the Athletics and Perception skills.",
                    "Leviathan Will: You have advantage on saving throws against being charmed, frightened, paralyzed, poisoned, stunned, or put to sleep.",
                    "Limited Amphibiousness: You can breathe air and water, but you need to be submerged at least once every 4 hours to avoid suffocating."
                },
                Languages = new() { "Common", "Aquan" },
                SkillProficiencies = new() { "Athletics", "Perception" }
            };

            d["Owlin"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new(),
                Traits = new()
                {
                    "Creature Type: You are a Humanoid.",
                    "Size: Small or Medium (your choice)",
                    "Speed: 30 feet",
                    "Ability Score Increase (Tasha-style): Increase one score by 2 and another by 1, or three different scores by 1.",
                    "Darkvision: You can see in dim light within 120 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Flight: Thanks to your wings, you have a flying speed equal to your walking speed. You can't use this flying speed if you're wearing medium or heavy armor.",
                    "Silent Feathers: You have proficiency in the Stealth skill."
                },
                Languages = new() { "Common", "One other language of your choice" },
                SkillProficiencies = new() { "Stealth" }
            };

            d["Satyr"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Charisma"] = 2, ["Dexterity"] = 1 },
                Traits = new()
                {
                    "Creature Type: You are a Fey.",
                    "Size: Medium",
                    "Ram: You can use your head and horns to make unarmed strikes. If you hit with them, you deal bludgeoning damage equal to 1d4 + your Strength modifier.",
                    "Magic Resistance: You have advantage on saving throws against spells.",
                    "Mirthful Leaps: Whenever you make a long or high jump, you can roll a d8 and add the number to the number of feet you cover, even when making a standing jump. This extra distance costs movement as normal.",
                    "Reveler: You have proficiency in the Performance and Persuasion skills, and you have proficiency with one musical instrument of your choice."
                },
                Languages = new() { "Common", "Sylvan" },
                SkillProficiencies = new() { "Performance", "Persuasion" }
            };

            d["Triton"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Strength"] = 1, ["Constitution"] = 1, ["Charisma"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Amphibious: You can breathe air and water.",
                    "Control Air and Water: You can cast Fog Cloud with this trait. Starting at 3rd level, you can cast Gust of Wind with it, and starting at 5th level, you can also cast Wall of Water with it. Once you cast a spell with this trait, you can't cast that spell with it again until you finish a long rest. Charisma is your spellcasting ability for these spells.",
                    "Emissary of the Sea: Aquatic beasts have an extraordinary affinity with your people. You can communicate simple ideas with beasts that can breathe water. They can understand the meaning of your words, though you have no special ability to understand them in return.",
                    "Guardians of the Depths: Adapted to even the most extreme ocean depths, you have resistance to cold damage, and you ignore any of the drawbacks caused by a deep, underwater environment."
                },
                Languages = new() { "Common", "Primordial" },
                HasInnateSpellcasting = true
            };

            d["Verdan"] = new RaceData
            {
                Category = "Exotic",
                AbilityBonuses = new() { ["Charisma"] = 2, ["Constitution"] = 1 },
                Traits = new()
                {
                    "Size: Small (you can grow into Medium as you age — see Blackstaff's Book of Unknown)",
                    "Speed: 30 feet",
                    "Black Blood Healing: When you roll a 1 or 2 on any Hit Die you spend at the end of a short rest, you can reroll the die and must use the new roll.",
                    "Limited Telepathy: You can magically speak telepathically to any creature you can see within 30 feet of you. You don't need to share a language with the creature for it to understand your telepathic utterances, but the creature must be able to understand at least one language.",
                    "Persuasive: You have proficiency in the Persuasion skill.",
                    "Telepathic Insight: You have advantage on all Wisdom and Charisma saving throws."
                },
                Languages = new() { "Common", "Goblin", "One other language of your choice" },
                SkillProficiencies = new() { "Persuasion" },
                Speed = 30
            };

            // ═══════════════ MONSTROUS ═══════════════

            d["Bugbear"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Strength"] = 2, ["Dexterity"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Long-Limbed: When you make a melee attack on your turn, your reach for it is 5 feet greater than normal.",
                    "Powerful Build: You count as one size larger when determining your carrying capacity and the weight you can push, drag, or lift.",
                    "Sneaky: You are proficient in the Stealth skill.",
                    "Surprise Attack: If you surprise a creature and hit it with an attack on your first turn in combat, the attack deals an extra 2d6 damage to it. You can use this trait only once per combat."
                },
                Languages = new() { "Common", "Goblin" },
                SkillProficiencies = new() { "Stealth" }
            };

            d["Centaur"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Strength"] = 2, ["Wisdom"] = 1 },
                Traits = new()
                {
                    "Creature Type: You are a Fey.",
                    "Size: Medium",
                    "Speed: 40 feet",
                    "Charge: If you move at least 30 feet straight toward a target and then hit it with a melee weapon attack on the same turn, you can immediately follow that attack with a bonus action, making one attack against the target with your hooves.",
                    "Hooves: Your hooves are natural melee weapons, which you can use to make unarmed strikes. If you hit with them, you deal bludgeoning damage equal to 1d4 + your Strength modifier, instead of the bludgeoning damage normal for an unarmed strike.",
                    "Equine Build: You count as one size larger when determining your carrying capacity and the weight you can push or drag. In addition, any climb that requires hands and feet is especially difficult for you because of your equine legs. When you make such a climb, each foot of movement costs you 4 extra feet, instead of the normal 1 extra foot.",
                    "Survivor: You have proficiency in one of the following skills of your choice: Animal Handling, Medicine, Nature, or Survival."
                },
                Languages = new() { "Common", "Sylvan" },
                Speed = 40
            };

            d["Goblin"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Dexterity"] = 2, ["Constitution"] = 1 },
                Traits = new()
                {
                    "Size: Small",
                    "Speed: 30 feet",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Fury of the Small: When you damage a creature with an attack or a spell and the creature's size is larger than yours, you can cause the attack or spell to deal extra damage to the creature. The extra damage equals your level. Once you use this trait, you can't use it again until you finish a short or long rest.",
                    "Nimble Escape: You can take the Disengage or Hide action as a bonus action on each of your turns."
                },
                Languages = new() { "Common", "Goblin" },
                Speed = 30
            };

            d["Grung"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Dexterity"] = 2, ["Constitution"] = 1 },
                Traits = new()
                {
                    "Size: Small",
                    "Speed: 25 feet; climb 25 feet",
                    "Arboreal Alertness: You have proficiency in the Perception skill.",
                    "Amphibious: You can breathe air and water.",
                    "Poison Immunity: You are immune to poison damage and the poisoned condition.",
                    "Poisonous Skin: Any creature that grapples you or otherwise comes into direct contact with your skin must succeed on a DC 12 Constitution saving throw or become poisoned for 1 minute. A poisoned creature no longer in direct contact with you can repeat the saving throw at the end of each of its turns, ending the effect on itself on a success. You can also apply this poison to any piercing weapon as part of an attack with that weapon, though when you hit the poison reacts differently. The target must succeed on a DC 12 Constitution saving throw or take 2d4 poison damage.",
                    "Standing Leap: Your long jump is up to 25 feet and your high jump is up to 15 feet, with or without a running start.",
                    "Water Dependency: If you fail to immerse yourself in water for at least 1 hour during a day, you suffer one level of exhaustion at the end of that day. You can only recover from this exhaustion through magic or by immersing yourself in water for at least 1 hour."
                },
                Languages = new() { "Grung" },
                SkillProficiencies = new() { "Perception" },
                Speed = 25
            };

            d["Hobgoblin"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Constitution"] = 2, ["Intelligence"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Martial Training: You are proficient with two martial weapons of your choice and with light armor.",
                    "Saving Face: Hobgoblins are careful not to show weakness in front of their allies, for fear of losing status. If you miss with an attack roll or fail an ability check or a saving throw, you can gain a bonus to the roll equal to the number of allies you can see within 30 feet of you (maximum bonus of +5). Once you use this trait, you can't use it again until you finish a short or long rest."
                },
                Languages = new() { "Common", "Goblin" }
            };

            d["Kobold"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Dexterity"] = 2 },
                Traits = new()
                {
                    "Size: Small",
                    "Speed: 30 feet",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Grovel, Cower, and Beg: As an action on your turn, you can cower pathetically to distract nearby foes. Until the end of your next turn, your allies gain advantage on attack rolls against enemies within 10 feet of you that can see you. Once you use this trait, you can't use it again until you finish a short or long rest.",
                    "Pack Tactics: You have advantage on an attack roll against a creature if at least one of your allies is within 5 feet of the creature and the ally isn't incapacitated.",
                    "Sunlight Sensitivity: You have disadvantage on attack rolls and on Wisdom (Perception) checks that rely on sight when you, the target of your attack, or whatever you are trying to perceive is in direct sunlight."
                },
                Languages = new() { "Common", "Draconic" },
                Speed = 30
            };

            d["Lizardfolk"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Constitution"] = 2, ["Wisdom"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Speed: 30 feet; swim 30 feet",
                    "Bite: Your fanged maw is a natural weapon, which you can use to make unarmed strikes. If you hit with it, you deal piercing damage equal to 1d6 + your Strength modifier, instead of the bludgeoning damage normal for an unarmed strike.",
                    "Cunning Artisan: As part of a short rest, you can harvest bone and hide from a slain beast, construct, dragon, monstrosity, or plant creature of size Small or larger to create one of the following items: a shield, a club, a javelin, or 1d4 darts or blowgun needles. To use this trait, you need a blade, such as a dagger, or appropriate artisan's tools, such as leatherworker's tools.",
                    "Hold Breath: You can hold your breath for up to 15 minutes at a time.",
                    "Hunter's Lore: You gain proficiency with two of the following skills of your choice: Animal Handling, Nature, Perception, Stealth, and Survival.",
                    "Natural Armor: You have tough, scaly skin. When you aren't wearing armor, your AC is 13 + your Dexterity modifier. You can use your natural armor to determine your AC if the armor you wear would leave you with a lower AC. A shield's benefits apply as normal while you use your natural armor.",
                    "Hungry Jaws: In battle, you can throw yourself into a vicious feeding frenzy. As a bonus action, you can make a special attack with your bite. If the attack hits, it deals its normal damage, and you gain temporary hit points (minimum of 1) equal to your Constitution modifier, and you can't use this trait again until you finish a short or long rest."
                },
                Languages = new() { "Common", "Draconic" }
            };

            d["Minotaur"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Strength"] = 2, ["Constitution"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Horns: Your horns are natural melee weapons, which you can use to make unarmed strikes. If you hit with them, you deal piercing damage equal to 1d6 + your Strength modifier, instead of the bludgeoning damage normal for an unarmed strike.",
                    "Goring Rush: Immediately after you use the Dash action on your turn and move at least 20 feet, you can make one melee attack with your horns as a bonus action.",
                    "Hammering Horns: Immediately after you hit a creature with a melee attack as part of the Attack action on your turn, you can use a bonus action to attempt to shove that target with your horns. The target must be no more than one size larger than you and within 5 feet of you. Unless it succeeds on a Strength saving throw against a DC equal to 8 + your proficiency bonus + your Strength modifier, you push it up to 10 feet away from you.",
                    "Imposing Presence: You have proficiency in one of the following skills of your choice: Intimidation or Persuasion."
                },
                Languages = new() { "Common", "Minotaur" }
            };

            d["Orc"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Strength"] = 2, ["Constitution"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Speed: 30 feet",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Aggressive: As a bonus action, you can move up to your speed toward an enemy of your choice that you can see or hear. You must end this move closer to the enemy than you started.",
                    "Powerful Build: You count as one size larger when determining your carrying capacity and the weight you can push, drag, or lift.",
                    "Primal Intuition: You have proficiency in two of the following skills of your choice: Animal Handling, Insight, Intimidation, Medicine, Nature, Perception, and Survival."
                },
                Languages = new() { "Common", "Orc" }
            };

            d["Shifter"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new(),
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Shifting: As a bonus action, you can assume a more bestial appearance. This transformation lasts for 1 minute, until you die, or until you revert to your normal appearance as a bonus action. When you shift, you gain temporary hit points equal to your level + your Constitution modifier (minimum of 1 temporary hit point). You also gain benefits that depend on your subrace, described below. Once you shift, you can't do so again until you finish a short or long rest.",
                    "Languages: You can speak, read, and write Common."
                },
                Languages = new() { "Common" }
            };

            d["Yuan-ti Pureblood"] = new RaceData
            {
                Category = "Monstrous",
                AbilityBonuses = new() { ["Charisma"] = 2, ["Intelligence"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Innate Spellcasting: You know the Poison Spray cantrip. You can cast Animal Friendship an unlimited number of times with this trait, but you can target only snakes with it. Starting at 3rd level, you can also cast Suggestion with this trait. Once you cast it, you can't do so again until you finish a long rest. Charisma is your spellcasting ability for these spells.",
                    "Magic Resistance: You have advantage on saving throws against spells and other magical effects.",
                    "Poison Immunity: You are immune to poison damage and the poisoned condition."
                },
                Languages = new() { "Common", "Abyssal", "Draconic" },
                HasInnateSpellcasting = true
            };

            return d;
        }

        private static Dictionary<string, List<SubraceData>> BuildExpandedSubraces()
        {
            var d = new Dictionary<string, List<SubraceData>>(StringComparer.OrdinalIgnoreCase);

            d["Genasi"] = new List<SubraceData>
            {
                new()
                {
                    Name = "Air Genasi",
                    AbilityBonus = new() { ["Dexterity"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Dexterity score increases by 1.",
                        "Unending Breath: You can hold your breath indefinitely while you're not incapacitated.",
                        "Mingle with the Wind: You can cast Levitate with this trait, without requiring a material component. Once you cast it, you can't cast it again with this trait until you finish a long rest. Constitution is your spellcasting ability for this spell."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Earth Genasi",
                    AbilityBonus = new() { ["Strength"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Strength score increases by 1.",
                        "Earth Walk: You can move across difficult terrain made of earth or stone without expending extra movement.",
                        "Merge with Stone: You can cast Pass without Trace with this trait, without requiring a material component. Once you cast it, you can't cast it again with this trait until you finish a long rest. Constitution is your spellcasting ability for this spell."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Fire Genasi",
                    AbilityBonus = new() { ["Intelligence"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Intelligence score increases by 1.",
                        "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. Your ties to the Elemental Plane of Fire make your darkvision unusual: everything you see in darkness is in a shade of red.",
                        "Fire Resistance: You have resistance to fire damage.",
                        "Reach to the Blaze: You know the Produce Flame cantrip. Once you reach 3rd level, you can cast Burning Hands once with this trait as a 1st-level spell, and you regain the ability to cast it this way when you finish a long rest. Constitution is your spellcasting ability for these spells."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Water Genasi",
                    AbilityBonus = new() { ["Wisdom"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Wisdom score increases by 1.",
                        "Acid Resistance: You have resistance to acid damage.",
                        "Amphibious: You can breathe air and water.",
                        "Swim: You have a swimming speed of 30 feet.",
                        "Call to the Wave: You know the Shape Water cantrip (EE). When you reach 3rd level, you can cast Create or Destroy Water as a 2nd-level spell once with this trait, and you regain the ability to cast it this way when you finish a long rest. Constitution is your spellcasting ability for these spells."
                    },
                    HasInnateSpellcasting = true
                }
            };

            d["Gith"] = new List<SubraceData>
            {
                new()
                {
                    Name = "Githyanki",
                    AbilityBonus = new() { ["Strength"] = 2, ["Intelligence"] = 1 },
                    ReplacesAbilityBonuses = true,
                    Traits = new()
                    {
                        "Ability Score Increase: Your Strength score increases by 2, and your Intelligence score increases by 1.",
                        "Decadent Mastery: You learn one language of your choice, and you are proficient with one skill or tool of your choice.",
                        "Martial Prodigy: You are proficient with light and medium armor and with shortswords, longswords, and greatswords.",
                        "Githyanki Psionics: You know the Mage Hand cantrip, and the hand is invisible when you cast it with this trait. When you reach 3rd level, you can cast Jump once with this trait, and you regain the ability to do so when you finish a long rest. When you reach 5th level, you can cast Misty Step once with this trait, and you regain the ability to do so when you finish a long rest. Intelligence is your spellcasting ability for these spells. You can cast them without components."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Githzerai",
                    AbilityBonus = new() { ["Wisdom"] = 2, ["Intelligence"] = 1 },
                    ReplacesAbilityBonuses = true,
                    Traits = new()
                    {
                        "Ability Score Increase: Your Wisdom score increases by 2, and your Intelligence score increases by 1.",
                        "Mental Discipline: You have advantage on saving throws against the charmed and frightened conditions.",
                        "Githzerai Psionics: You know the Mage Hand cantrip, and the hand is invisible when you cast it with this trait. When you reach 3rd level, you can cast Shield once with this trait, and you regain the ability to do so when you finish a long rest. When you reach 5th level, you can cast Detect Thoughts once with this trait, and you regain the ability to do so when you finish a long rest. Wisdom is your spellcasting ability for these spells. You can cast them without components."
                    },
                    HasInnateSpellcasting = true
                }
            };

            d["Shifter"] = new List<SubraceData>
            {
                new()
                {
                    Name = "Beasthide",
                    AbilityBonus = new() { ["Constitution"] = 2, ["Strength"] = 1 },
                    ReplacesAbilityBonuses = true,
                    Traits = new()
                    {
                        "Ability Score Increase: Your Constitution score increases by 2, and your Strength score increases by 1.",
                        "Natural Athlete: You have proficiency in the Athletics skill.",
                        "Shifting Feature: Whenever you shift, you gain 1d6 additional temporary hit points. While shifted, you have a +1 bonus to your Armor Class."
                    }
                },
                new()
                {
                    Name = "Longtooth",
                    AbilityBonus = new() { ["Strength"] = 2, ["Dexterity"] = 1 },
                    ReplacesAbilityBonuses = true,
                    Traits = new()
                    {
                        "Ability Score Increase: Your Strength score increases by 2, and your Dexterity score increases by 1.",
                        "Fierce: You have proficiency in the Intimidation skill.",
                        "Shifting Feature: While shifted, you can use your elongated fangs to make an unarmed strike as a bonus action. If you hit with your fangs, you can deal piercing damage equal to 1d6 + your Strength modifier, instead of the bludgeoning damage normal for an unarmed strike."
                    }
                },
                new()
                {
                    Name = "Swiftstride",
                    AbilityBonus = new() { ["Dexterity"] = 2, ["Charisma"] = 1 },
                    ReplacesAbilityBonuses = true,
                    Traits = new()
                    {
                        "Ability Score Increase: Your Dexterity score increases by 2, and your Charisma score increases by 1.",
                        "Graceful: You have proficiency in the Acrobatics skill.",
                        "Swift Stride: Your walking speed increases by 5 feet.",
                        "Shifting Feature: While shifted, your walking speed increases by an additional 5 feet. Additionally, you can move up to 10 feet as a reaction when a creature ends its turn within 5 feet of you. This reactive movement doesn't provoke opportunity attacks."
                    },
                    Speed = 35
                },
                new()
                {
                    Name = "Wildhunt",
                    AbilityBonus = new() { ["Wisdom"] = 2, ["Dexterity"] = 1 },
                    ReplacesAbilityBonuses = true,
                    Traits = new()
                    {
                        "Ability Score Increase: Your Wisdom score increases by 2, and your Dexterity score increases by 1.",
                        "Natural Tracker: You have proficiency in the Survival skill.",
                        "Shifting Feature: While shifted, you have advantage on Wisdom checks, and no creature within 30 feet of you can make an attack roll with advantage against you, unless you're incapacitated."
                    }
                }
            };

            return d;
        }
    }
}
