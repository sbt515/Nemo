using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Controls.Primitives;

namespace Nemo
{
    public static partial class GameData
    {

        // ==================== COMPLETE 5e DATA (from dnd5e.wikidot.com + PHB) ====================

        // Race/subrace trait text and ASIs aligned to official 5e sources (PHB, SCAG, Volo's, EE, Tortle Package, Tasha's).
        // Reference: https://dnd5e.wikidot.com/#toc2
        public static readonly Dictionary<string, RaceData> RaceData = new()
        {
            ["Dragonborn"] = new()
            {
                AbilityBonuses = new() { ["Strength"] = 2, ["Charisma"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Draconic Ancestry: Choose one dragon type. This determines your breath weapon damage type/area and damage resistance:\n  • Black — Acid; 5×30 ft line (Dex save)\n  • Blue — Lightning; 5×30 ft line (Dex save)\n  • Brass — Fire; 5×30 ft line (Dex save)\n  • Bronze — Lightning; 5×30 ft line (Dex save)\n  • Copper — Acid; 5×30 ft line (Dex save)\n  • Gold — Fire; 15 ft cone (Dex save)\n  • Green — Poison; 15 ft cone (Con save)\n  • Red — Fire; 15 ft cone (Dex save)\n  • Silver — Cold; 15 ft cone (Con save)\n  • White — Cold; 15 ft cone (Con save)",
                    "Breath Weapon: As an action, exhale energy in the area of your ancestry. Each creature in the area makes the listed save (DC = 8 + your Constitution modifier + your proficiency bonus). 2d6 damage on a failed save, half on a success. Damage increases to 3d6 at 6th level, 4d6 at 11th, and 5d6 at 16th. Recharges on a short or long rest.",
                    "Damage Resistance: Resistance to the damage type associated with your Draconic Ancestry."
                },
                Languages = new() { "Common", "Draconic" }
            },
            ["Dwarf"] = new()
            {
                AbilityBonuses = new() { ["Constitution"] = 2 },
                Traits = new()
                {
                    "Size: Medium",
                    "Speed: 25 feet. Your speed is not reduced by wearing heavy armor.",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Dwarven Resilience: You have advantage on saving throws against poison, and you have resistance against poison damage.",
                    "Dwarven Combat Training: You have proficiency with the battleaxe, handaxe, light hammer, and warhammer.",
                    "Tool Proficiency: You gain proficiency with the artisan's tools of your choice: smith's tools, brewer's supplies, or mason's tools.",
                    "Stonecunning: Whenever you make an Intelligence (History) check related to the origin of stonework, you are considered proficient in the History skill and add double your proficiency bonus to the check, instead of your normal proficiency bonus."
                },
                Languages = new() { "Common", "Dwarvish" },
                Speed = 25
            },
            ["Elf"] = new()
            {
                AbilityBonuses = new() { ["Dexterity"] = 2 },
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Keen Senses: You have proficiency in the Perception skill.",
                    "Fey Ancestry: You have advantage on saving throws against being charmed, and magic can't put you to sleep.",
                    "Trance: Elves don't need to sleep. Instead, they meditate deeply, remaining semiconscious, for 4 hours a day. After resting in this way, you gain the same benefit that a human does from 8 hours of sleep."
                },
                Languages = new() { "Common", "Elvish" },
                SkillProficiencies = new() { "Perception" },
                HasInnateSpellcasting = false
            },
            ["Gnome"] = new()
            {
                AbilityBonuses = new() { ["Intelligence"] = 2 },
                Traits = new()
                {
                    "Size: Small",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Gnome Cunning: You have advantage on all Intelligence, Wisdom, and Charisma saving throws against magic."
                },
                Languages = new() { "Common", "Gnomish" },
                Speed = 25
            },
            ["Half-Elf"] = new()
            {
                AbilityBonuses = new() { ["Charisma"] = 2 },
                Traits = new()
                {
                    "Ability Score Increase: Your Charisma score increases by 2, and two other ability scores of your choice each increase by 1.",
                    "Size: Medium",
                    "Darkvision: Thanks to your elven heritage, you can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Fey Ancestry: You have advantage on saving throws against being charmed, and magic can't put you to sleep.",
                    "Skill Versatility: You gain proficiency in two skills of your choice."
                },
                Languages = new() { "Common", "Elvish", "One extra language of your choice" }
            },
            ["Half-Orc"] = new()
            {
                AbilityBonuses = new() { ["Strength"] = 2, ["Constitution"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: Thanks to your orc blood, you can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Menacing: You gain proficiency in the Intimidation skill.",
                    "Relentless Endurance: When you are reduced to 0 hit points but not killed outright, you can drop to 1 hit point instead. You can't use this feature again until you finish a long rest.",
                    "Savage Attacks: When you score a critical hit with a melee weapon attack, you can roll one of the weapon's damage dice one additional time and add it to the extra damage of the critical hit."
                },
                Languages = new() { "Common", "Orc" },
                SkillProficiencies = new() { "Intimidation" }
            },
            ["Halfling"] = new()
            {
                AbilityBonuses = new() { ["Dexterity"] = 2 },
                Traits = new()
                {
                    "Size: Small",
                    "Lucky: When you roll a 1 on an attack roll, ability check, or saving throw, you can reroll the die and must use the new roll.",
                    "Brave: You have advantage on saving throws against being frightened.",
                    "Halfling Nimbleness: You can move through the space of any creature that is of a size larger than yours."
                },
                Languages = new() { "Common", "Halfling" },
                Speed = 25
            },
            ["Human"] = new()
            {
                AbilityBonuses = new()
                {
                    ["Strength"] = 1, ["Dexterity"] = 1, ["Constitution"] = 1,
                    ["Intelligence"] = 1, ["Wisdom"] = 1, ["Charisma"] = 1
                },
                Traits = new()
                {
                    "Ability Score Increase: Your ability scores each increase by 1.",
                    "Size: Medium",
                    "Languages: You can speak, read, and write Common and one extra language of your choice."
                },
                Languages = new() { "Common", "One extra language of your choice" }
            },
            ["Variant Human"] = new()
            {
                AbilityBonuses = new(),
                Traits = new()
                {
                    "Ability Score Increase: Two different ability scores of your choice increase by 1. (This replaces the standard human Ability Score Increase.)",
                    "Skills: You gain proficiency in one skill of your choice.",
                    "Feat: You gain one feat of your choice.",
                    "Size: Medium",
                    "Languages: You can speak, read, and write Common and one extra language of your choice."
                },
                Languages = new() { "Common", "One extra language of your choice" }
            },
            ["Tiefling"] = new()
            {
                // PHB: +2 Cha and +1 Int (Asmodeus). Variants may replace ASI or Infernal Legacy.
                AbilityBonuses = new() { ["Charisma"] = 2, ["Intelligence"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: Thanks to your infernal heritage, you can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Hellish Resistance: You have resistance to fire damage.",
                    "Infernal Legacy: You know the Thaumaturgy cantrip. Once you reach 3rd level, you can cast Hellish Rebuke once as a 2nd-level spell. Once you reach 5th level, you can also cast Darkness once. You must finish a long rest to cast these spells again with this trait. Charisma is your spellcasting ability for these spells. (SCAG variants may replace this trait — see subrace.)"
                },
                Languages = new() { "Common", "Infernal" },
                HasInnateSpellcasting = true
            },
            ["Tabaxi"] = new()
            {
                AbilityBonuses = new() { ["Dexterity"] = 2, ["Charisma"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Feline Agility: When you move on your turn in combat, you can double your speed until the end of the turn. Once you use this trait, you can't use it again until you move 0 feet on one of your turns.",
                    "Cat's Claws: Because of your claws, you have a climbing speed of 20 feet. In addition, your claws are natural weapons, which you can use to make unarmed strikes. If you hit with them, you deal slashing damage equal to 1d4 + your Strength modifier, instead of the bludgeoning damage normal for an unarmed strike.",
                    "Cat's Talent: You have proficiency in the Perception and Stealth skills."
                },
                Languages = new() { "Common", "One extra language of your choice" },
                SkillProficiencies = new() { "Perception", "Stealth" }
            },
            ["Kenku"] = new()
            {
                AbilityBonuses = new() { ["Dexterity"] = 2, ["Wisdom"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Expert Forgery: You can duplicate other creatures' handwriting and craftwork. You have advantage on all checks made to produce forgeries or duplicates of existing objects.",
                    "Kenku Training: You are proficient in your choice of two of the following skills: Acrobatics, Deception, Stealth, and Sleight of Hand.",
                    "Mimicry: You can mimic sounds you have heard, including voices. A creature that hears the sounds you make can tell they are imitations with a successful Wisdom (Insight) check opposed by your Charisma (Deception) check.",
                    "Languages: You can read and write Common and Auran, but you can speak only by using your Mimicry trait."
                },
                Languages = new() { "Common (read/write; speak via Mimicry only)", "Auran (read/write; speak via Mimicry only)" }
            },
            ["Tortle"] = new()
            {
                AbilityBonuses = new() { ["Strength"] = 2, ["Wisdom"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Claws: Your claws are natural weapons, which you can use to make unarmed strikes. If you hit with them, you deal slashing damage equal to 1d4 + your Strength modifier, instead of the bludgeoning damage normal for an unarmed strike.",
                    "Hold Breath: You can hold your breath for up to 1 hour at a time.",
                    "Natural Armor: Due to your shell and the shape of your body, you are ill-suited to wearing armor. Your shell provides a base AC of 17 (your Dexterity modifier doesn't affect this number). You gain no benefit from wearing armor, but if you are using a shield, you can apply the shield's bonus as normal.",
                    "Shell Defense: You can withdraw into your shell as an action. Until you emerge, you gain a +4 bonus to AC, and you have advantage on Strength and Constitution saving throws. While in your shell, you are prone, your speed is 0 and can't increase, you have disadvantage on Dexterity saving throws, you can't take reactions, and the only action you can take is a bonus action to emerge from your shell.",
                    "Survival Instinct: You gain proficiency in the Survival skill."
                },
                Languages = new() { "Common", "Aquan" },
                SkillProficiencies = new() { "Survival" }
            },
            ["Deep Gnome"] = new()
            {
                // Elemental Evil Player's Companion (svirfneblin)
                AbilityBonuses = new() { ["Intelligence"] = 2, ["Dexterity"] = 1 },
                Traits = new()
                {
                    "Size: Small",
                    "Superior Darkvision: Your darkvision has a radius of 120 feet.",
                    "Gnome Cunning: You have advantage on all Intelligence, Wisdom, and Charisma saving throws against magic.",
                    "Stone Camouflage: You have advantage on Dexterity (Stealth) checks to hide in rocky terrain."
                },
                Languages = new() { "Common", "Gnomish" },
                Speed = 25
            },
            ["Duergar"] = new()
            {
                // SCAG full-race presentation
                AbilityBonuses = new() { ["Constitution"] = 2, ["Strength"] = 1 },
                Traits = new()
                {
                    "Size: Medium",
                    "Speed: 25 feet. Your speed is not reduced by wearing heavy armor.",
                    "Superior Darkvision: You can see in dim light within 120 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Dwarven Resilience: You have advantage on saving throws against poison, and you have resistance against poison damage.",
                    "Duergar Resilience: You have advantage on saving throws against illusions and against being charmed or paralyzed.",
                    "Dwarven Combat Training: You have proficiency with the battleaxe, handaxe, light hammer, and warhammer.",
                    "Tool Proficiency: You gain proficiency with the artisan's tools of your choice: smith's tools, brewer's supplies, or mason's tools.",
                    "Stonecunning: Whenever you make an Intelligence (History) check related to the origin of stonework, you are considered proficient in the History skill and add double your proficiency bonus to the check, instead of your normal proficiency bonus.",
                    "Duergar Magic: When you reach 3rd level, you can cast Enlarge/Reduce on yourself once with this trait, using only the spell's enlarge option. When you reach 5th level, you can cast Invisibility on yourself once with this trait. You don't need material components for either spell, and you can't cast them while you're in direct sunlight, although sunlight has no effect on them once cast. You regain the ability to cast these spells with this trait when you finish a long rest. Intelligence is your spellcasting ability for these spells.",
                    "Sunlight Sensitivity: You have disadvantage on attack rolls and on Wisdom (Perception) checks that rely on sight when you, the target of your attack, or whatever you are trying to perceive is in direct sunlight."
                },
                Languages = new() { "Common", "Dwarvish" },
                HasInnateSpellcasting = true,
                Speed = 25
            },
            ["Aasimar"] = new()
            {
                // Volo's Guide base (subraces below)
                AbilityBonuses = new() { ["Charisma"] = 2 },
                Traits = new()
                {
                    "Size: Medium",
                    "Darkvision: Blessed with a radiant soul, you can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You can't discern color in darkness, only shades of gray.",
                    "Celestial Resistance: You have resistance to necrotic damage and radiant damage.",
                    "Healing Hands: As an action, you can touch a creature and cause it to regain a number of hit points equal to your level. Once you use this trait, you can't use it again until you finish a long rest.",
                    "Light Bearer: You know the Light cantrip. Charisma is your spellcasting ability for it."
                },
                Languages = new() { "Common", "Celestial" },
                HasInnateSpellcasting = true
            },
            ["Custom Lineage"] = new()
            {
                // Tasha's Cauldron of Everything
                AbilityBonuses = new(),
                Traits = new()
                {
                    "Creature Type: You are a Humanoid. You determine your appearance and whether you resemble any of your kin.",
                    "Size: You are Small or Medium (your choice).",
                    "Speed: Your base walking speed is 30 feet.",
                    "Ability Score Increase: One ability score of your choice increases by 2.",
                    "Feat: You gain one feat of your choice for which you qualify.",
                    "Variable Trait: Choose one — Darkvision (you can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light; you can't discern color in darkness, only shades of gray) OR proficiency in one skill of your choice.",
                    "Languages: You can speak, read, and write Common and one other language that you and your DM agree is appropriate for the character."
                },
                Languages = new() { "Common", "One other language of your choice" }
            }
        };

        public static readonly Dictionary<string, List<SubraceData>> RaceSubraces = new()
        {
            ["Aasimar"] = new()
            {
                new()
                {
                    Name = "Protector Aasimar",
                    AbilityBonus = new() { ["Wisdom"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Wisdom score increases by 1.",
                        "Radiant Soul (3rd level): As an action, unleash the divine energy within yourself, causing your eyes to glimmer and two luminous, incorporeal wings to sprout from your back. Your transformation lasts for 1 minute or until you end it as a bonus action. During it, you have a flying speed of 30 feet, and once on each of your turns, you can deal extra radiant damage to one target when you deal damage to it with an attack or a spell. The extra radiant damage equals your level. Once you use this trait, you can't use it again until you finish a long rest."
                    }
                },
                new()
                {
                    Name = "Scourge Aasimar",
                    AbilityBonus = new() { ["Constitution"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Constitution score increases by 1.",
                        "Radiant Consumption (3rd level): As an action, unleash the divine energy within yourself, causing a searing light to radiate from you, pour out of your eyes and mouth, and threaten to char you. Your transformation lasts for 1 minute or until you end it as a bonus action. During it, you shed bright light in a 10-foot radius and dim light for an additional 10 feet, and at the end of each of your turns, you and each creature within 10 feet of you take radiant damage equal to half your level (rounded up). In addition, once on each of your turns, you can deal extra radiant damage to one target when you deal damage to it with an attack or a spell. The extra radiant damage equals your level. Once you use this trait, you can't use it again until you finish a long rest."
                    }
                },
                new()
                {
                    Name = "Fallen Aasimar",
                    AbilityBonus = new() { ["Strength"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Strength score increases by 1.",
                        "Necrotic Shroud (3rd level): As an action, unleash the divine energy within yourself, causing your eyes to turn into pools of darkness and two skeletal, ghostly, flightless wings to sprout from your back. The instant you transform, other creatures within 10 feet of you that can see you must each succeed on a Charisma saving throw (DC = 8 + your proficiency bonus + your Charisma modifier) or become frightened of you until the end of your next turn. Your transformation lasts for 1 minute or until you end it as a bonus action. During it, once on each of your turns, you can deal extra necrotic damage to one target when you deal damage to it with an attack or a spell. The extra necrotic damage equals your level. Once you use this trait, you can't use it again until you finish a long rest."
                    }
                }
            },

            ["Dwarf"] = new()
            {
                new()
                {
                    Name = "Hill Dwarf",
                    AbilityBonus = new() { ["Wisdom"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Wisdom score increases by 1.",
                        "Dwarven Toughness: Your hit point maximum increases by 1, and it increases by 1 every time you gain a level."
                    }
                },
                new()
                {
                    Name = "Mountain Dwarf",
                    AbilityBonus = new() { ["Strength"] = 2 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Strength score increases by 2.",
                        "Dwarven Armor Training: You have proficiency with light and medium armor."
                    }
                }
            },

            ["Elf"] = new()
            {
                new()
                {
                    Name = "High Elf",
                    AbilityBonus = new() { ["Intelligence"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Intelligence score increases by 1.",
                        "Elf Weapon Training: You have proficiency with the longsword, shortsword, shortbow, and longbow.",
                        "Cantrip: You know one cantrip of your choice from the wizard spell list. Intelligence is your spellcasting ability for it.",
                        "Extra Language: You can speak, read, and write one extra language of your choice."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Wood Elf",
                    AbilityBonus = new() { ["Wisdom"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Wisdom score increases by 1.",
                        "Elf Weapon Training: You have proficiency with the longsword, shortsword, shortbow, and longbow.",
                        "Fleet of Foot: Your base walking speed increases to 35 feet.",
                        "Mask of the Wild: You can attempt to hide even when you are only lightly obscured by foliage, heavy rain, falling snow, mist, and other natural phenomena."
                    },
                    Speed = 35
                },
                new()
                {
                    Name = "Drow (Dark Elf)",
                    AbilityBonus = new() { ["Charisma"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Charisma score increases by 1.",
                        "Superior Darkvision: Your darkvision has a range of 120 feet, instead of 60.",
                        "Sunlight Sensitivity: You have disadvantage on attack rolls and on Wisdom (Perception) checks that rely on sight when you, the target of your attack, or whatever you are trying to perceive is in direct sunlight.",
                        "Drow Magic: You know the Dancing Lights cantrip. When you reach 3rd level, you can cast Faerie Fire once with this trait and regain the ability to do so when you finish a long rest. When you reach 5th level, you can cast Darkness once and regain the ability to do so when you finish a long rest. Charisma is your spellcasting ability for these spells.",
                        "Drow Weapon Training: You have proficiency with rapiers, shortswords, and hand crossbows."
                    },
                    HasInnateSpellcasting = true
                }
            },

            ["Gnome"] = new()
            {
                new()
                {
                    Name = "Forest Gnome",
                    AbilityBonus = new() { ["Dexterity"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Dexterity score increases by 1.",
                        "Natural Illusionist: You know the Minor Illusion cantrip. Intelligence is your spellcasting ability for it.",
                        "Speak with Small Beasts: Through sounds and gestures, you can communicate simple ideas with Small or smaller beasts."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Rock Gnome",
                    AbilityBonus = new() { ["Constitution"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Constitution score increases by 1.",
                        "Artificer's Lore: Whenever you make an Intelligence (History) check related to magic items, alchemical objects, or technological devices, you can add twice your proficiency bonus, instead of any proficiency bonus you normally apply.",
                        "Tinker: You have proficiency with artisan's tools (tinker's tools). Using those tools, you can spend 1 hour and 10 gp worth of materials to construct a Tiny clockwork device (AC 5, 1 hp). The device ceases to function after 24 hours (unless you spend 1 hour repairing it to keep the device functioning), or when you use your action to dismantle it; at that time, you can reclaim the materials used to create it. You can have up to three such devices active at a time. When you create a device, choose one of the following options: Clockwork Toy, Fire Starter, or Music Box."
                    }
                }
            },

            ["Halfling"] = new()
            {
                new()
                {
                    Name = "Lightfoot Halfling",
                    AbilityBonus = new() { ["Charisma"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Charisma score increases by 1.",
                        "Naturally Stealthy: You can attempt to hide even when you are obscured only by a creature that is at least one size larger than you."
                    }
                },
                new()
                {
                    Name = "Stout Halfling",
                    AbilityBonus = new() { ["Constitution"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Constitution score increases by 1.",
                        "Stout Resilience: You have advantage on saving throws against poison, and you have resistance against poison damage."
                    }
                },
                new()
                {
                    Name = "Ghostwise Halfling",
                    AbilityBonus = new() { ["Wisdom"] = 1 },
                    Traits = new()
                    {
                        "Ability Score Increase: Your Wisdom score increases by 1.",
                        "Silent Speech: You can speak telepathically to any creature within 30 feet of you. The creature understands you only if the two of you share a language. You can speak telepathically in this way to one creature at a time."
                    }
                }
            },

            ["Tiefling"] = new()
            {
                // SCAG variants. Base race already has PHB +2 Cha / +1 Int and Infernal Legacy.
                new()
                {
                    Name = "Asmodeus (Default)",
                    AbilityBonus = new(),
                    Traits = new()
                    {
                        "Bloodline of Asmodeus (PHB default): Uses the base tiefling Ability Score Increase (+2 Charisma, +1 Intelligence) and Infernal Legacy (Thaumaturgy; Hellish Rebuke at 3rd; Darkness at 5th)."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Feral",
                    AbilityBonus = new() { ["Dexterity"] = 2, ["Intelligence"] = 1 },
                    ReplacesAbilityBonuses = true,
                    Traits = new()
                    {
                        "Feral (SCAG): Your Intelligence score increases by 1, and your Dexterity score increases by 2. This trait replaces the Ability Score Increase trait of the tiefling.",
                        "You retain Hellish Resistance and Infernal Legacy unless you also take another mutually exclusive legacy variant."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Devil's Tongue",
                    AbilityBonus = new(),
                    Traits = new()
                    {
                        "Devil's Tongue (SCAG): Replaces Infernal Legacy. You know the Vicious Mockery cantrip. Once you reach 3rd level, you can cast Charm Person once as a 2nd-level spell. Once you reach 5th level, you can also cast Enthrall once. You must finish a long rest to cast these spells again with this trait. Charisma is your spellcasting ability for these spells.",
                        "Ability Score Increase remains +2 Charisma and +1 Intelligence. Hellish Resistance is unchanged. Devil's Tongue, Hellfire, and Winged are mutually exclusive."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Hellfire",
                    AbilityBonus = new(),
                    Traits = new()
                    {
                        "Hellfire (SCAG): Once you reach 3rd level, you can cast Burning Hands once as a 2nd-level spell. This trait replaces the Hellish Rebuke spell of the Infernal Legacy trait (you still get Thaumaturgy and, at 5th level, Darkness).",
                        "Ability Score Increase remains +2 Charisma and +1 Intelligence. Hellish Resistance is unchanged. Devil's Tongue, Hellfire, and Winged are mutually exclusive."
                    },
                    HasInnateSpellcasting = true
                },
                new()
                {
                    Name = "Winged",
                    AbilityBonus = new(),
                    Traits = new()
                    {
                        "Winged (SCAG): You have bat-like wings sprouting from your shoulders. You have a flying speed of 30 feet while you aren't wearing heavy armor. This trait replaces the Infernal Legacy trait.",
                        "Ability Score Increase remains +2 Charisma and +1 Intelligence. Hellish Resistance is unchanged. Devil's Tongue, Hellfire, and Winged are mutually exclusive."
                    },
                    HasInnateSpellcasting = false
                }
            }
        };

        public static readonly Dictionary<string, ClassData> ClassData = new()
        {
            // Subclass lists: official published options (PHB, XGE, TCE, SCAG, etc.). See also AllSubclasses.
            ["Artificer"] = new() { HitDie = "1d8", HP1stLevel = "8 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Intelligence", CantripsKnown = 2, SpellsPrepared = "Int mod + 1", Subclasses = new() { "Alchemist", "Armorer", "Artillerist", "Battle Smith" }, SkillChoices = new() { "Arcana", "Deception", "History", "Investigation", "Medicine", "Nature", "Perception", "Sleight of Hand" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Constitution", "Intelligence" }, ArmorProficiencies = new() { "Light armor", "Medium armor", "Shields" }, WeaponProficiencies = new() { "Simple weapons" } },
            ["Barbarian"] = new() { HitDie = "1d12", HP1stLevel = "12 + Con mod", Proficiencies = "...", Spellcasting = false, Subclasses = new() { "Path of the Ancestral Guardian", "Path of the Battlerager", "Path of the Beast", "Path of the Berserker", "Path of the Giant", "Path of the Storm Herald", "Path of the Totem Warrior", "Path of Wild Magic", "Path of the Zealot" }, SkillChoices = new() { "Animal Handling", "Athletics", "Intimidation", "Nature", "Perception", "Survival" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Strength", "Constitution" }, ArmorProficiencies = new() { "Light armor", "Medium armor", "Shields" }, WeaponProficiencies = new() { "Simple weapons", "Martial weapons" } },
            ["Bard"] = new() { HitDie = "1d8", HP1stLevel = "8 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Charisma", CantripsKnown = 2, SpellsKnown = 4, Subclasses = new() { "College of Creation", "College of Eloquence", "College of Glamour", "College of Lore", "College of Spirits", "College of Swords", "College of Valor", "College of Whispers" }, SkillChoices = new() { "Acrobatics", "Animal Handling", "Arcana", "Athletics", "Deception", "History", "Insight", "Intimidation", "Investigation", "Medicine", "Nature", "Perception", "Performance", "Persuasion", "Religion", "Sleight of Hand", "Stealth", "Survival" }, SkillChoiceCount = 3, SavingThrowProficiencies = new() { "Dexterity", "Charisma" }, ArmorProficiencies = new() { "Light armor" }, WeaponProficiencies = new() { "Simple weapons", "Hand crossbows", "Longswords", "Rapiers", "Shortswords" } },
            ["Cleric"] = new() { HitDie = "1d8", HP1stLevel = "8 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Wisdom", CantripsKnown = 3, SpellsPrepared = "Wis mod + 1", Subclasses = new() { "Arcana", "Death", "Forge", "Grave", "Knowledge", "Life", "Light", "Nature", "Order", "Peace", "Tempest", "Trickery", "Twilight", "War" }, SkillChoices = new() { "History", "Insight", "Medicine", "Persuasion", "Religion" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Wisdom", "Charisma" }, ArmorProficiencies = new() { "Light armor", "Medium armor", "Shields" }, WeaponProficiencies = new() { "Simple weapons" } },
            ["Druid"] = new() { HitDie = "1d8", HP1stLevel = "8 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Wisdom", CantripsKnown = 2, SpellsPrepared = "Wis mod + 1", Subclasses = new() { "Circle of Dreams", "Circle of Spores", "Circle of Stars", "Circle of the Land", "Circle of the Moon", "Circle of the Shepherd", "Circle of Wildfire" }, SkillChoices = new() { "Arcana", "Animal Handling", "Insight", "Medicine", "Nature", "Perception", "Religion", "Survival" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Intelligence", "Wisdom" }, ArmorProficiencies = new() { "Light armor", "Medium armor", "Shields (non-metal)" }, WeaponProficiencies = new() { "Clubs", "Daggers", "Darts", "Javelins", "Maces", "Quarterstaffs", "Scimitars", "Sickles", "Slings", "Spears" } },
            ["Fighter"] = new() { HitDie = "1d10", HP1stLevel = "10 + Con mod", Proficiencies = "...", Spellcasting = false, Subclasses = new() { "Arcane Archer", "Banneret", "Battle Master", "Cavalier", "Champion", "Echo Knight", "Eldritch Knight", "Psi Warrior", "Rune Knight", "Samurai" }, SkillChoices = new() { "Acrobatics", "Animal Handling", "Athletics", "History", "Insight", "Intimidation", "Perception", "Survival" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Strength", "Constitution" }, ArmorProficiencies = new() { "All armor", "Shields" }, WeaponProficiencies = new() { "Simple weapons", "Martial weapons" } },
            ["Monk"] = new() { HitDie = "1d8", HP1stLevel = "8 + Con mod", Proficiencies = "...", Spellcasting = false, Subclasses = new() { "Way of Mercy", "Way of Shadow", "Way of the Ascendant Dragon", "Way of the Astral Self", "Way of the Drunken Master", "Way of the Four Elements", "Way of the Kensei", "Way of the Long Death", "Way of the Open Hand", "Way of the Sun Soul" }, SkillChoices = new() { "Acrobatics", "Athletics", "History", "Insight", "Religion", "Stealth" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Strength", "Dexterity" }, ArmorProficiencies = new() { "None" }, WeaponProficiencies = new() { "Simple weapons", "Shortswords" } },
            ["Paladin"] = new() { HitDie = "1d10", HP1stLevel = "10 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Charisma", CantripsKnown = 0, SpellsPrepared = "Cha mod + 1", Subclasses = new() { "Oath of Conquest", "Oath of Devotion", "Oath of Glory", "Oath of Redemption", "Oath of Vengeance", "Oath of the Ancients", "Oath of the Crown", "Oath of the Open Sea", "Oath of the Watchers", "Oathbreaker" }, SkillChoices = new() { "Athletics", "Insight", "Intimidation", "Medicine", "Persuasion", "Religion" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Wisdom", "Charisma" }, ArmorProficiencies = new() { "All armor", "Shields" }, WeaponProficiencies = new() { "Simple weapons", "Martial weapons" } },
            ["Ranger"] = new() { HitDie = "1d10", HP1stLevel = "10 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Wisdom", CantripsKnown = 0, SpellsKnown = 0, Subclasses = new() { "Beast Master", "Drakewarden", "Fey Wanderer", "Gloom Stalker", "Horizon Walker", "Hunter", "Monster Slayer", "Swarmkeeper" }, SkillChoices = new() { "Animal Handling", "Athletics", "Insight", "Investigation", "Nature", "Perception", "Stealth", "Survival" }, SkillChoiceCount = 3, SavingThrowProficiencies = new() { "Strength", "Dexterity" }, ArmorProficiencies = new() { "Light armor", "Medium armor", "Shields" }, WeaponProficiencies = new() { "Simple weapons", "Martial weapons" } },
            ["Rogue"] = new() { HitDie = "1d8", HP1stLevel = "8 + Con mod", Proficiencies = "...", Spellcasting = false, Subclasses = new() { "Arcane Trickster", "Assassin", "Inquisitive", "Mastermind", "Phantom", "Scout", "Soulknife", "Swashbuckler", "Thief" }, SkillChoices = new() { "Acrobatics", "Athletics", "Deception", "Insight", "Intimidation", "Investigation", "Perception", "Performance", "Persuasion", "Sleight of Hand", "Stealth" }, SkillChoiceCount = 4, SavingThrowProficiencies = new() { "Dexterity", "Intelligence" }, ArmorProficiencies = new() { "Light armor" }, WeaponProficiencies = new() { "Simple weapons", "Hand crossbows", "Longswords", "Rapiers", "Shortswords" } },
            ["Sorcerer"] = new() { HitDie = "1d6", HP1stLevel = "6 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Charisma", CantripsKnown = 4, SpellsKnown = 2, Subclasses = new() { "Aberrant Mind", "Clockwork Soul", "Divine Soul", "Draconic Bloodline", "Lunar", "Shadow", "Storm", "Wild Magic" }, SkillChoices = new() { "Arcana", "Deception", "Insight", "Intimidation", "Persuasion", "Religion" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Constitution", "Charisma" }, ArmorProficiencies = new() { "None" }, WeaponProficiencies = new() { "Daggers", "Darts", "Slings", "Quarterstaffs", "Light crossbows" } },
            ["Warlock"] = new() { HitDie = "1d8", HP1stLevel = "8 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Charisma", CantripsKnown = 2, SpellsKnown = 2, Subclasses = new() { "The Archfey", "The Celestial", "The Fathomless", "The Fiend", "The Genie", "The Great Old One", "The Hexblade", "The Undead", "The Undying" }, SkillChoices = new() { "Arcana", "Deception", "History", "Intimidation", "Investigation", "Nature", "Religion" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Wisdom", "Charisma" }, ArmorProficiencies = new() { "Light armor" }, WeaponProficiencies = new() { "Simple weapons" } },
            ["Wizard"] = new() { HitDie = "1d6", HP1stLevel = "6 + Con mod", Proficiencies = "...", Spellcasting = true, SpellAbility = "Intelligence", CantripsKnown = 3, SpellsPrepared = "Int mod + 1", Subclasses = new() { "Abjuration", "Bladesinging", "Chronurgy", "Conjuration", "Divination", "Enchantment", "Evocation", "Graviturgy", "Illusion", "Necromancy", "Order of Scribes", "Transmutation", "War Magic" }, SkillChoices = new() { "Arcana", "History", "Insight", "Investigation", "Medicine", "Religion" }, SkillChoiceCount = 2, SavingThrowProficiencies = new() { "Intelligence", "Wisdom" }, ArmorProficiencies = new() { "None" }, WeaponProficiencies = new() { "Daggers", "Darts", "Slings", "Quarterstaffs", "Light crossbows" } }
        };

        // ==================== CLASS LEVEL 1 FEATURES (legacy L1-only summaries for PDF/UI) ====================
        // Full 1–20 base-class progression lives in ClassProgression (ClassProgressionData.cs).
        // Prefer GetClassFeaturesAtLevel / GetClassFeaturesUpToLevel for new code.
        public static readonly Dictionary<string, List<ClassFeature>> ClassLevel1Features = new()
        {
            ["Artificer"] = new()
            {
                new ClassFeature { Name = "Magical Tinkering", Description = "Infuse small objects with minor magical effects (light, sound, etc.).", Uses = "At will" },
                new ClassFeature { Name = "Spellcasting", Description = "Cast artificer spells using Intelligence. Tools can be used as spellcasting focus.", Uses = "Spell slots (see Spells section)" }
            },
            ["Barbarian"] = new()
            {
                new ClassFeature { Name = "Rage", Description = "Enter a primal rage granting bonus melee damage, resistance to bludgeoning/piercing/slashing damage, and advantage on Strength checks and saves.", Uses = "2 times per long rest" },
                new ClassFeature { Name = "Unarmored Defense", Description = "While not wearing any armor, your AC equals 10 + your Dexterity modifier + your Constitution modifier.", Uses = "Passive" }
            },
            ["Bard"] = new()
            {
                new ClassFeature { Name = "Bardic Inspiration", Description = "Use a bonus action to give one creature a d6 inspiration die to add to ability checks, attacks, or saves within the next 10 minutes.", Uses = "Charisma modifier times per long rest" },
                new ClassFeature { Name = "Spellcasting", Description = "Cast bard spells using Charisma. Know a number of spells equal to your level + Charisma modifier.", Uses = "Spell slots (see Spells section)" }
            },
            ["Cleric"] = new()
            {
                new ClassFeature { Name = "Spellcasting", Description = "Cast cleric spells using Wisdom. Prepare a number of spells equal to Wisdom modifier + cleric level.", Uses = "Spell slots (see Spells section)" },
                new ClassFeature { Name = "Divine Domain", Description = "Choose a domain that grants domain spells and special abilities (see subclass for details).", Uses = "Varies by domain" }
            },
            ["Druid"] = new()
            {
                new ClassFeature { Name = "Spellcasting", Description = "Cast druid spells using Wisdom. Prepare spells equal to Wisdom modifier + druid level (except cantrips).", Uses = "Spell slots (see Spells section)" },
                new ClassFeature { Name = "Wild Shape", Description = "Transform into beasts of CR 1/4 or lower (no flying or swimming speed yet).", Uses = "2 times per short rest" },
                new ClassFeature { Name = "Druidic", Description = "You know Druidic, the secret language of druids. You can speak it and use it to leave hidden messages.", Uses = "Passive" }
            },
            ["Fighter"] = new()
            {
                new ClassFeature { Name = "Second Wind", Description = "Regain hit points equal to 1d10 + your fighter level as a bonus action. Regain on short or long rest.", Uses = "1 time per short rest" },
                new ClassFeature { Name = "Fighting Style", Description = "Adopt a particular style of fighting (Archery, Defense, Dueling, Great Weapon Fighting, Protection, or Two-Weapon Fighting).", Uses = "Passive" }
            },
            ["Monk"] = new()
            {
                new ClassFeature { Name = "Martial Arts", Description = "Use Dexterity for unarmed strikes and monk weapons. Unarmed strikes deal 1d4 + Dex damage. Can make bonus action unarmed strike after Attack action.", Uses = "Passive" },
                new ClassFeature { Name = "Unarmored Defense", Description = "While not wearing armor and not wielding a shield, your AC equals 10 + your Dexterity modifier + your Wisdom modifier.", Uses = "Passive" },
                new ClassFeature { Name = "Ki", Description = "Harness mystical energy. Start with 1 ki point. Ki powers include Flurry of Blows, Patient Defense, and Step of the Wind.", Uses = "1 point per short or long rest (regain all on short/long rest)" }
            },
            ["Paladin"] = new()
            {
                new ClassFeature { Name = "Divine Sense", Description = "Detect celestials, fiends, and undead within 60 ft as an action. Also detect consecrated or desecrated objects/places.", Uses = "1 + Charisma modifier times per long rest" },
                new ClassFeature { Name = "Lay on Hands", Description = "Touch a creature to restore hit points from a pool of 5 \u00d7 paladin level HP. Can also cure disease or poison.", Uses = "5 \u00d7 level HP pool per long rest" },
                new ClassFeature { Name = "Fighting Style", Description = "Adopt a fighting style (Blessed Warrior, Blind Fighting, Defense, Dueling, Great Weapon Fighting, Interception, Protection).", Uses = "Passive" }
            },
            ["Ranger"] = new()
            {
                new ClassFeature { Name = "Favored Enemy", Description = "Choose a type of enemy. Gain advantage on Wisdom (Survival) checks to track them and Intelligence checks to recall info about them.", Uses = "Passive" },
                new ClassFeature { Name = "Natural Explorer", Description = "Choose a favored terrain. While in it, difficult terrain doesn't slow you, and you have advantage on initiative and can't be surprised.", Uses = "Passive" },
                new ClassFeature { Name = "Spellcasting", Description = "Cast ranger spells using Wisdom (from level 2 normally, but some archetypes grant earlier).", Uses = "Spell slots (see Spells section)" }
            },
            ["Rogue"] = new()
            {
                new ClassFeature { Name = "Sneak Attack", Description = "Deal extra 1d6 damage once per turn when you have advantage or an ally is within 5 ft of the target.", Uses = "1 time per turn" },
                new ClassFeature { Name = "Thieves' Cant", Description = "Know thieves' cant, a secret mix of dialect, jargon, and code that allows you to hide messages in seemingly normal conversation.", Uses = "Passive" },
                new ClassFeature { Name = "Expertise", Description = "Double your proficiency bonus for two skills you are proficient in (Stealth and one other).", Uses = "Passive" }
            },
            ["Sorcerer"] = new()
            {
                new ClassFeature { Name = "Spellcasting", Description = "Cast sorcerer spells using Charisma. Know a limited number of spells; can use sorcery points (from level 2) for metamagic later.", Uses = "Spell slots (see Spells section)" },
                new ClassFeature { Name = "Sorcerous Origin", Description = "Choose your sorcerous origin (Draconic Bloodline, Wild Magic, etc.) granting unique level 1 abilities.", Uses = "Varies by origin (see subclass)" }
            },
            ["Warlock"] = new()
            {
                new ClassFeature { Name = "Spellcasting", Description = "Cast warlock spells using Charisma. Regain all spell slots on short rest. Know a small number of spells permanently.", Uses = "Spell slots (short rest recharge)" },
                new ClassFeature { Name = "Pact Magic", Description = "Your arcane research and the magic bestowed on you by your patron have given you facility with spells.", Uses = "Spell slots (see Spells section)" },
                new ClassFeature { Name = "Otherworldly Patron", Description = "Choose a patron (Fiend, Archfey, Great Old One, etc.) granting unique abilities and expanded spell list.", Uses = "Varies by patron (see subclass)" }
            },
            ["Wizard"] = new()
            {
                new ClassFeature { Name = "Spellcasting", Description = "Cast wizard spells using Intelligence. Prepare a number of spells equal to Intelligence modifier + wizard level. Ritual casting.", Uses = "Spell slots (see Spells section)" },
                new ClassFeature { Name = "Arcane Recovery", Description = "Once per day after a short rest, recover expended spell slots totaling no more than half your wizard level (rounded up).", Uses = "1 time per day" }
            }
        };

        // ==================== SUBCLASS LEVEL 1 FEATURES (Cleric Domains, Sorcerer Origins, Warlock Patrons) ====================
        // Used only for PDF export when the character has a subclass selected for one of the three classes.
        public static readonly Dictionary<string, List<ClassFeature>> SubclassLevel1Features = new()
        {
            // === CLERIC DOMAINS (keyed by the exact subclass name used in UI) ===
            ["Forge"] = new()
            {
                new ClassFeature { Name = "Blessing of the Forge", Description = "At the end of a long rest you can touch a nonmagical weapon or suit of armor and imbue it with +1 magical bonus until the end of your next long rest.", Uses = "Proficiency bonus times per long rest" },
                new ClassFeature { Name = "Domain Spells", Description = "Always have Identify and Searing Smite prepared. They do not count against the number of spells you can prepare.", Uses = "Passive" }
            },
            ["Life"] = new()
            {
                new ClassFeature { Name = "Disciple of Life", Description = "Whenever you cast a 1st-level or higher spell that restores hit points, the target regains additional hit points equal to 2 + the spell's level.", Uses = "Passive (applies to all healing spells)" },
                new ClassFeature { Name = "Domain Spells", Description = "Always have Bless and Cure Wounds prepared. They do not count against the number of spells you can prepare.", Uses = "Passive" }
            },
            ["Light"] = new()
            {
                new ClassFeature { Name = "Warding Flare", Description = "When a creature attacks you or an ally you can see within 30 ft, you can use your reaction to impose disadvantage on the attack roll.", Uses = "Wisdom modifier times per long rest" },
                new ClassFeature { Name = "Domain Spells", Description = "Always have Burning Hands and Faerie Fire prepared. They do not count against the number of spells you can prepare.", Uses = "Passive" }
            },
            ["Twilight"] = new()
            {
                new ClassFeature { Name = "Eye of Night", Description = "You gain darkvision out to 300 feet. You can share this darkvision with willing creatures within 10 feet (no action required).", Uses = "Passive" },
                new ClassFeature { Name = "Vigilant Blessing", Description = "You can grant a creature within 30 feet (including yourself) advantage on its next initiative roll.", Uses = "Proficiency bonus times per long rest" }
            },
            ["Grave"] = new()
            {
                new ClassFeature { Name = "Circle of Mortality", Description = "You gain the ability to cast Spare the Dying as a bonus action. When you cast it on a creature with 0 hit points, it has advantage on death saves until the start of your next turn.", Uses = "At will (bonus action)" },
                new ClassFeature { Name = "Eyes of the Grave", Description = "As a bonus action you can sense the presence of undead within 60 feet that are not behind total cover.", Uses = "Wisdom modifier times per long rest" }
            },
            ["War"] = new()
            {
                new ClassFeature { Name = "War Priest", Description = "When you take the Attack action, you can make one weapon attack as a bonus action. You can do this a number of times equal to your Wisdom modifier.", Uses = "Wisdom modifier times per long rest (recharge on long rest)" },
                new ClassFeature { Name = "Domain Spells", Description = "Always have Divine Favor and Shield of Faith prepared.", Uses = "Passive" }
            },
            ["Tempest"] = new()
            {
                new ClassFeature { Name = "Wrath of the Storm", Description = "When a creature within 5 feet hits you with an attack, you can use your reaction to deal 2d8 lightning or thunder damage (Dex save for half).", Uses = "Wisdom modifier times per long rest" },
                new ClassFeature { Name = "Domain Spells", Description = "Always have Fog Cloud and Thunderwave prepared.", Uses = "Passive" }
            },
            ["Trickery"] = new()
            {
                new ClassFeature { Name = "Blessing of the Trickster", Description = "You can grant a creature within 30 feet (including yourself) advantage on Dexterity (Stealth) checks. The blessing lasts up to 1 hour or until you use this feature on another creature.", Uses = "Proficiency bonus times per long rest" }
            },
            ["Peace"] = new()
            {
                new ClassFeature { Name = "Emboldening Bond", Description = "You can magically bond up to two creatures (including yourself) within 30 feet. While bonded, when one makes an attack roll, ability check, or saving throw, the other can add 1d4 to the roll.", Uses = "Proficiency bonus times per long rest" }
            },
            ["Order"] = new()
            {
                new ClassFeature { Name = "Voice of Authority", Description = "When you cast a 1st-level or higher spell that targets only one creature, you can use your reaction to allow an ally within 30 feet to use their reaction to make one weapon attack against a target of the spell.", Uses = "Passive (works once per spell cast)" }
            },
            ["Nature"] = new()
            {
                new ClassFeature { Name = "Acolyte of Nature", Description = "You learn one Druid cantrip of your choice. You also gain proficiency in one skill from Animal Handling, Nature, or Survival.", Uses = "Passive" }
            },
            ["Knowledge"] = new()
            {
                new ClassFeature { Name = "Blessings of Knowledge", Description = "You gain proficiency in two skills of your choice from Arcana, History, Nature, or Religion. Your proficiency bonus is doubled for those skills.", Uses = "Passive (Expertise)" }
            },
            ["Death"] = new()
            {
                new ClassFeature { Name = "Reaper", Description = "You learn one Necromancy cantrip. When you cast a cantrip that deals necrotic damage, you can deal damage to two creatures within range instead of one (both must be within 5 feet of each other).", Uses = "Passive" }
            },
            ["Arcana"] = new()
            {
                new ClassFeature { Name = "Arcane Initiate", Description = "You gain proficiency in Arcana. You also learn one Wizard cantrip of your choice.", Uses = "Passive" }
            },

            // === SORCERER ORIGINS ===
            ["Clockwork Soul"] = new()
            {
                new ClassFeature { Name = "Clockwork Magic", Description = "You learn additional spells: Alarm, Protection from Evil and Good, and others at higher levels. These count as sorcerer spells.", Uses = "Passive (always known)" },
                new ClassFeature { Name = "Restore Balance", Description = "When a creature you can see within 60 feet is about to roll a d20 with advantage or disadvantage, you can use your reaction to prevent the advantage or disadvantage for that roll.", Uses = "Charisma modifier times per long rest" }
            },
            ["Draconic Bloodline"] = new()
            {
                new ClassFeature { Name = "Draconic Resilience", Description = "Your hit point maximum increases by 1 for each sorcerer level. When not wearing armor, your AC equals 13 + your Dexterity modifier.", Uses = "Passive" },
                new ClassFeature { Name = "Draconic Ancestry", Description = "Choose one dragon type. You gain resistance to the damage type associated with that dragon (acid, cold, fire, lightning, or poison).", Uses = "Passive" }
            },
            ["Wild Magic"] = new()
            {
                new ClassFeature { Name = "Wild Magic Surge", Description = "Immediately after you cast a sorcerer spell of 1st level or higher, the DM can have you roll a d20. If you roll a 1, roll on the Wild Magic Surge table.", Uses = "Passive (triggers on spell cast)" },
                new ClassFeature { Name = "Tides of Chaos", Description = "You can manipulate the forces of chance and chaos to gain advantage on one attack roll, ability check, or saving throw. The DM can then force a Wild Magic Surge.", Uses = "1 time per long rest (regain on long rest or when you roll on surge table)" }
            },
            ["Shadow"] = new()
            {
                new ClassFeature { Name = "Eyes of the Dark", Description = "You gain darkvision out to 120 feet. You can cast Darkness by spending 2 sorcery points (no spell slot). You can see through that Darkness.", Uses = "Passive darkvision + 2 SP for Darkness" },
                new ClassFeature { Name = "Strength of the Grave", Description = "When reduced to 0 hit points, you can make a Charisma saving throw (DC 5 + damage taken). On success you drop to 1 hit point instead.", Uses = "1 time per long rest" }
            },
            ["Storm"] = new()
            {
                new ClassFeature { Name = "Tempestuous Magic", Description = "Immediately after casting a 1st-level or higher spell, you can use a bonus action to fly up to 10 feet without provoking opportunity attacks.", Uses = "Passive (after casting)" },
                new ClassFeature { Name = "Heart of the Storm", Description = "You gain resistance to lightning and thunder damage. When you cast a spell that deals lightning or thunder damage, creatures within 10 feet take extra damage equal to your Charisma modifier.", Uses = "Passive" }
            },
            ["Divine Soul"] = new()
            {
                new ClassFeature { Name = "Divine Magic", Description = "You learn an additional spell based on your chosen divine alignment (e.g. Cure Wounds for Good, etc.). This counts as a sorcerer spell.", Uses = "Passive (always known)" },
                new ClassFeature { Name = "Favored by the Gods", Description = "When you fail a saving throw or miss with an attack roll, you can roll 2d4 and add the total to the roll, potentially turning failure into success.", Uses = "1 time per short or long rest" }
            },
            ["Aberrant Mind"] = new()
            {
                new ClassFeature { Name = "Psionic Spells", Description = "You learn additional spells (Mind Sliver, Dissonant Whispers, etc.) that count as sorcerer spells for you. They are always known.", Uses = "Passive" },
                new ClassFeature { Name = "Telepathic Speech", Description = "You can communicate telepathically with any creature within 30 feet that can understand a language. You don't need to share a language.", Uses = "At will (while conscious)" }
            },
            ["Lunar"] = new()
            {
                new ClassFeature { Name = "Lunar Magic", Description = "You learn the Moonbeam spell (and others at higher levels). These count as sorcerer spells.", Uses = "Passive" },
                new ClassFeature { Name = "Moonlight", Description = "You can shed bright light in a 30-foot radius and dim light for an additional 30 feet (bonus action to toggle).", Uses = "At will" }
            },

            // === WARLOCK PATRONS ===
            ["The Great Old One"] = new()
            {
                new ClassFeature { Name = "Awakened Mind", Description = "You can telepathically speak to any creature within 30 feet that can understand a language. You don't need to share a language.", Uses = "At will" },
                new ClassFeature { Name = "Expanded Spell List", Description = "You always have Dissonant Whispers, Tasha's Hideous Laughter, and other Great Old One spells available to learn.", Uses = "Passive" }
            },
            ["The Fiend"] = new()
            {
                new ClassFeature { Name = "Dark One's Blessing", Description = "When you reduce a hostile creature to 0 hit points, you gain temporary hit points equal to your Charisma modifier + warlock level.", Uses = "Passive (triggers on kill)" },
                new ClassFeature { Name = "Expanded Spell List", Description = "You always have access to Burning Hands, Command, and other Fiend patron spells.", Uses = "Passive" }
            },
            ["The Archfey"] = new()
            {
                new ClassFeature { Name = "Fey Presence", Description = "As an action you can project an aura that charms or frightens creatures of your choice within 10 feet (Wisdom save negates).", Uses = "1 time per short or long rest" },
                new ClassFeature { Name = "Expanded Spell List", Description = "You always have Faerie Fire, Sleep, and other Archfey spells available.", Uses = "Passive" }
            },
            ["The Celestial"] = new()
            {
                new ClassFeature { Name = "Healing Light", Description = "As a bonus action you can heal a creature within 60 feet for 1d6 + your Charisma modifier hit points. You can use this a number of times equal to your proficiency bonus.", Uses = "Proficiency bonus times per long rest" },
                new ClassFeature { Name = "Expanded Spell List", Description = "You always have Cure Wounds, Guiding Bolt, and other Celestial spells available.", Uses = "Passive" }
            },
            ["The Hexblade"] = new()
            {
                new ClassFeature { Name = "Hex Warrior", Description = "You gain proficiency with medium armor, shields, and martial weapons. When you attack with a one-handed weapon, you can use Charisma instead of Strength or Dexterity for the attack and damage rolls.", Uses = "Passive" },
                new ClassFeature { Name = "Expanded Spell List", Description = "You always have Shield, Wrathful Smite, and other Hexblade spells available.", Uses = "Passive" }
            },
            ["The Fathomless"] = new()
            {
                new ClassFeature { Name = "Tentacle of the Deeps", Description = "You can summon a spectral tentacle that can attack or grapple. It lasts 1 minute or until you dismiss it.", Uses = "1 time per short or long rest (scales with level)" },
                new ClassFeature { Name = "Gifts of the Deep", Description = "You gain a swim speed of 40 feet and the ability to breathe underwater. You also gain resistance to cold damage.", Uses = "Passive" }
            },
            ["The Genie"] = new()
            {
                new ClassFeature { Name = "Genie's Vessel", Description = "You gain a magical vessel (a lamp, bottle, etc.). You can enter it as an action and remain there for up to 1 hour. You can also use it to cast the Genie's vessel spells.", Uses = "1 hour per long rest inside vessel" },
                new ClassFeature { Name = "Expanded Spell List", Description = "You always have Phantasmal Force, Detect Evil and Good, and other Genie spells available.", Uses = "Passive" }
            },
            ["The Undead"] = new()
            {
                new ClassFeature { Name = "Form of Dread", Description = "As a bonus action you can transform for 1 minute, gaining temporary hit points and frightening creatures when you hit them.", Uses = "1 time per short or long rest" },
                new ClassFeature { Name = "Grave Touched", Description = "You don't need to eat, drink, or breathe. You are immune to disease. You also learn the Spare the Dying cantrip.", Uses = "Passive" }
            },
            ["The Undying"] = new()
            {
                new ClassFeature { Name = "Among the Dead", Description = "You have advantage on saving throws against effects from undead and on death saving throws. You can also speak with the dead once per long rest.", Uses = "Passive + 1/day speak with dead" },
                new ClassFeature { Name = "Undying Sentinel", Description = "When you are reduced to 0 hit points, you can immediately regain hit points equal to your warlock level + your Charisma modifier.", Uses = "1 time per long rest" }
            }
        };

        public static readonly Dictionary<string, ClericSubclassData> ClericSubclasses = new()
        {
            ["Arcana"] = new() { Name = "Arcana Domain", AdditionalCantrips = new() { "Any 1 Wizard cantrip" }, DomainSpells = new() { "Detect Magic", "Magic Missile" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Arcane Initiate", "Arcane Ward" } },
            ["Death"] = new() { Name = "Death Domain", AdditionalCantrips = new() { "Any 1 Necromancy cantrip" }, DomainSpells = new() { "False Life", "Ray of Sickness" }, ArmorProficiencies = new(), WeaponProficiencies = new() { "Martial weapons" }, UniqueAbilities = new() { "Reaper" } },
            ["Forge"] = new() { Name = "Forge Domain", AdditionalCantrips = new(), DomainSpells = new() { "Identify", "Searing Smite" }, ArmorProficiencies = new() { "Heavy armor" }, WeaponProficiencies = new(), UniqueAbilities = new() { "Blessing of the Forge" } },
            ["Grave"] = new() { Name = "Grave Domain", AdditionalCantrips = new(), DomainSpells = new() { "False Life", "Gentle Repose" }, ArmorProficiencies = new(), WeaponProficiencies = new() { "Martial weapons" }, UniqueAbilities = new() { "Circle of Mortality", "Eyes of the Grave" } },
            ["Knowledge"] = new() { Name = "Knowledge Domain", AdditionalCantrips = new(), DomainSpells = new() { "Command", "Identify" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Blessings of Knowledge" } },
            ["Life"] = new() { Name = "Life Domain", AdditionalCantrips = new(), DomainSpells = new() { "Bless", "Cure Wounds" }, ArmorProficiencies = new() { "Heavy armor" }, WeaponProficiencies = new(), UniqueAbilities = new() { "Disciple of Life" } },
            ["Light"] = new() { Name = "Light Domain", AdditionalCantrips = new() { "Light" }, DomainSpells = new() { "Burning Hands", "Faerie Fire" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Warding Flare" } },
            ["Nature"] = new() { Name = "Nature Domain", AdditionalCantrips = new() { "Any 1 Druid cantrip" }, DomainSpells = new() { "Animal Friendship", "Speak with Animals" }, ArmorProficiencies = new() { "Heavy armor" }, WeaponProficiencies = new(), UniqueAbilities = new() { "Acolyte of Nature" } },
            ["Order"] = new() { Name = "Order Domain", AdditionalCantrips = new(), DomainSpells = new() { "Command", "Heroism" }, ArmorProficiencies = new() { "Heavy armor" }, WeaponProficiencies = new(), UniqueAbilities = new() { "Voice of Authority" } },
            ["Peace"] = new() { Name = "Peace Domain", AdditionalCantrips = new(), DomainSpells = new() { "Heroism", "Sanctuary" }, ArmorProficiencies = new() { "Heavy armor" }, WeaponProficiencies = new(), UniqueAbilities = new() { "Emboldening Bond" } },
            ["Tempest"] = new() { Name = "Tempest Domain", AdditionalCantrips = new(), DomainSpells = new() { "Fog Cloud", "Thunderwave" }, ArmorProficiencies = new() { "Heavy armor" }, WeaponProficiencies = new() { "Martial weapons" }, UniqueAbilities = new() { "Wrath of the Storm" } },
            ["Trickery"] = new() { Name = "Trickery Domain", AdditionalCantrips = new(), DomainSpells = new() { "Charm Person", "Disguise Self" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Blessing of the Trickster" } },
            ["Twilight"] = new() { Name = "Twilight Domain", AdditionalCantrips = new(), DomainSpells = new() { "Faerie Fire", "Sleep" }, ArmorProficiencies = new() { "Heavy armor" }, WeaponProficiencies = new() { "Martial weapons" }, UniqueAbilities = new() { "Eye of Night", "Vigilant Blessing" } },
            ["War"] = new() { Name = "War Domain", AdditionalCantrips = new(), DomainSpells = new() { "Divine Favor", "Shield of Faith" }, ArmorProficiencies = new() { "Heavy armor" }, WeaponProficiencies = new() { "Martial weapons" }, UniqueAbilities = new() { "War Priest" } }
        };

        public static readonly Dictionary<string, WarlockSubclassData> WarlockSubclasses = new()
        {
            ["The Archfey"] = new() { Name = "The Archfey", AdditionalCantrips = new(), DomainSpells = new() { "Faerie Fire", "Sleep" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Fey Presence (charm or frighten creatures within 10 ft)" } },
            ["The Celestial"] = new() { Name = "The Celestial", AdditionalCantrips = new(), DomainSpells = new() { "Cure Wounds", "Guiding Bolt" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Healing Light (bonus action healing)" } },
            ["The Fathomless"] = new() { Name = "The Fathomless", AdditionalCantrips = new(), DomainSpells = new() { "Create or Destroy Water", "Tasha's Hideous Laughter" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Tentacle of the Deeps", "Gifts of the Deep (swim speed + breathing)" } },
            ["The Fiend"] = new() { Name = "The Fiend", AdditionalCantrips = new(), DomainSpells = new() { "Burning Hands", "Command" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Dark One's Blessing (temp HP on kills)" } },
            ["The Genie"] = new() { Name = "The Genie", AdditionalCantrips = new(), DomainSpells = new() { "Phantasmal Force", "Detect Evil and Good" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Genie's Vessel", "Elemental resistance based on genie type" } },
            ["The Great Old One"] = new() { Name = "The Great Old One", AdditionalCantrips = new(), DomainSpells = new() { "Dissonant Whispers", "Tasha's Hideous Laughter" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Awakened Mind (telepathic communication)" } },
            ["The Hexblade"] = new() { Name = "The Hexblade", AdditionalCantrips = new(), DomainSpells = new() { "Shield", "Wrathful Smite" }, ArmorProficiencies = new() { "Medium armor", "Shields" }, WeaponProficiencies = new() { "Martial weapons" }, UniqueAbilities = new() { "Hex Warrior (Cha for weapon attacks)" } },
            ["The Undead"] = new() { Name = "The Undead", AdditionalCantrips = new(), DomainSpells = new() { "False Life", "Ray of Sickness" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Form of Dread", "Grave Touched" } },
            ["The Undying"] = new() { Name = "The Undying", AdditionalCantrips = new(), DomainSpells = new() { "False Life", "Inflict Wounds" }, ArmorProficiencies = new(), WeaponProficiencies = new(), UniqueAbilities = new() { "Among the Dead (advantage vs undead)", "Undying Sentinel" } }
        };

        public static readonly Dictionary<string, SorcererSubclassData> SorcererSubclasses = new()
        {
            ["Aberrant Mind"] = new() { Name = "Aberrant Mind", AdditionalSpells = new() { "Mind Sliver", "Tasha's Hideous Laughter", "Detect Thoughts" }, UniqueAbilities = new() { "Psionic Spells", "Telepathic Speech" } },
            ["Clockwork Soul"] = new() { Name = "Clockwork Soul", AdditionalSpells = new() { "Alarm", "Protection from Evil and Good" }, UniqueAbilities = new() { "Clockwork Magic", "Restore Balance" } },
            ["Divine Soul"] = new() { Name = "Divine Soul", AdditionalSpells = new() { "Cure Wounds", "Guiding Bolt" }, UniqueAbilities = new() { "Divine Magic", "Favored by the Gods" } },
            ["Draconic Bloodline"] = new() { Name = "Draconic Bloodline", AdditionalSpells = new(), UniqueAbilities = new() { "Draconic Resilience (+1 HP per level, natural armor 13 + Dex mod)", "Draconic Ancestry (choose dragon type)" } },
            ["Lunar"] = new() { Name = "Lunar Sorcery", AdditionalSpells = new() { "Moonbeam" }, UniqueAbilities = new() { "Lunar Magic", "Moonlight" } },
            ["Shadow"] = new() { Name = "Shadow Magic", AdditionalSpells = new(), UniqueAbilities = new() { "Eyes of the Dark (darkvision + see in magical darkness)", "Strength of the Grave" } },
            ["Storm"] = new() { Name = "Storm Sorcery", AdditionalSpells = new(), UniqueAbilities = new() { "Tempestuous Magic", "Heart of the Storm" } },
            ["Wild Magic"] = new() { Name = "Wild Magic", AdditionalSpells = new(), UniqueAbilities = new() { "Wild Magic Surge", "Tides of Chaos" } }
        };

        // ==================== SPELL LISTS (Cantrips + 1st-Level) ====================
        public static readonly Dictionary<string, List<string>> ClassCantrips = new()
        {
            ["Artificer"] = new() { "Acid Splash", "Booming Blade", "Create Bonfire", "Dancing Lights", "Fire Bolt", "Frostbite", "Green-Flame Blade", "Guidance", "Light", "Lightning Lure", "Mending", "Message", "Poison Spray", "Prestidigitation", "Ray of Frost", "Shocking Grasp", "Spare the Dying", "Sword Burst", "Thorn Whip", "Thunderclap" },
            ["Bard"] = new() { "Blade Ward", "Dancing Lights", "Friends", "Light", "Mage Hand", "Mending", "Message", "Minor Illusion", "Prestidigitation", "True Strike", "Vicious Mockery" },
            ["Cleric"] = new() { "Guidance", "Light", "Mending", "Resistance", "Sacred Flame", "Spare the Dying", "Thaumaturgy" },
            ["Druid"] = new() { "Druidcraft", "Guidance", "Mending", "Poison Spray", "Produce Flame", "Resistance", "Shillelagh", "Thorn Whip" },
            ["Sorcerer"] = new() { "Acid Splash", "Blade Ward", "Booming Blade", "Chill Touch", "Create Bonfire", "Dancing Lights", "Fire Bolt", "Friends", "Frostbite", "Green-Flame Blade", "Gust", "Light", "Lightning Lure", "Mage Hand", "Mending", "Message", "Minor Illusion", "Poison Spray", "Prestidigitation", "Ray of Frost", "Shocking Grasp", "Sword Burst", "Thunderclap", "True Strike" },
            ["Warlock"] = new() { "Blade Ward", "Chill Touch", "Create Bonfire", "Eldritch Blast", "Friends", "Frostbite", "Green-Flame Blade", "Lightning Lure", "Mage Hand", "Minor Illusion", "Poison Spray", "Prestidigitation", "Sword Burst", "Thunderclap", "True Strike" },
            ["Wizard"] = new() { "Acid Splash", "Blade Ward", "Booming Blade", "Chill Touch", "Create Bonfire", "Dancing Lights", "Fire Bolt", "Friends", "Frostbite", "Green-Flame Blade", "Gust", "Light", "Lightning Lure", "Mage Hand", "Mending", "Message", "Minor Illusion", "Poison Spray", "Prestidigitation", "Ray of Frost", "Shocking Grasp", "Sword Burst", "Thunderclap", "True Strike" }
        };

        public static readonly Dictionary<string, List<string>> ClassLevel1Spells = new()
        {
            ["Cleric"] = new()
    {
        "Bane", "Bless", "Command", "Create or Destroy Water", "Cure Wounds",
        "Detect Evil and Good", "Detect Magic", "Detect Poison and Disease",
        "Divine Favor", "Guiding Bolt", "Healing Word", "Inflict Wounds",
        "Protection from Evil and Good", "Purify Food and Drink", "Sanctuary",
        "Shield of Faith", "Ceremony", "Detect Magic", "Bless", "Sanctuary",
        "Wrathful Smite", "Searing Smite", "Thunderous Smite", "Compelled Duel",
        "Heroism", "Bless", "Shield of Faith"
    },

            ["Druid"] = new()
    {
        "Animal Friendship", "Charm Person", "Create or Destroy Water", "Cure Wounds",
        "Detect Magic", "Detect Poison and Disease", "Entangle", "Faerie Fire",
        "Fog Cloud", "Goodberry", "Healing Word", "Jump", "Longstrider",
        "Purify Food and Drink", "Speak with Animals", "Thunderwave", "Absorb Elements",
        "Beast Bond", "Detect Magic", "Earth Tremor", "Fog Cloud", "Healing Word",
        "Ice Knife", "Longstrider", "Snare", "Thunderwave"
    },

            ["Bard"] = new()
    {
        "Animal Friendship", "Charm Person", "Comprehend Languages", "Cure Wounds",
        "Detect Magic", "Disguise Self", "Dissonant Whispers", "Faerie Fire",
        "Feather Fall", "Healing Word", "Heroism", "Identify", "Longstrider",
        "Mage Armor", "Sleep", "Tasha's Hideous Laughter", "Thunderwave",
        "Unseen Servant", "Bless", "Charm Person", "Cure Wounds", "Detect Magic",
        "Disguise Self", "Healing Word", "Heroism", "Identify", "Longstrider",
        "Mage Armor", "Sleep", "Tasha's Hideous Laughter"
    },

            ["Sorcerer"] = new()
    {
        "Burning Hands", "Charm Person", "Chromatic Orb", "Color Spray",
        "Comprehend Languages", "Detect Magic", "Disguise Self", "Expeditious Retreat",
        "False Life", "Feather Fall", "Fog Cloud", "Ice Knife", "Jump",
        "Mage Armor", "Magic Missile", "Shield", "Sleep", "Thunderwave", "Witch Bolt",
        "Absorb Elements", "Burning Hands", "Charm Person", "Chromatic Orb",
        "Color Spray", "Comprehend Languages", "Detect Magic", "Disguise Self",
        "Expeditious Retreat", "False Life", "Feather Fall", "Fog Cloud",
        "Ice Knife", "Jump", "Mage Armor", "Magic Missile", "Shield", "Sleep",
        "Thunderwave", "Witch Bolt"
    },

            ["Warlock"] = new()
    {
        "Armor of Agathys", "Burning Hands", "Charm Person", "Comprehend Languages",
        "Expeditious Retreat", "Hellish Rebuke", "Hex", "Protection from Evil and Good",
        "Unseen Servant", "Witch Bolt", "Arms of Hadar", "Dissonant Whispers",
        "Hex", "Hellish Rebuke", "Armor of Agathys", "Burning Hands",
        "Charm Person", "Comprehend Languages", "Expeditious Retreat",
        "Hellish Rebuke", "Hex", "Protection from Evil and Good", "Unseen Servant",
        "Witch Bolt", "Arms of Hadar", "Dissonant Whispers"
    },

            ["Wizard"] = new()
    {
        "Alarm", "Burning Hands", "Charm Person", "Chromatic Orb", "Color Spray",
        "Comprehend Languages", "Detect Magic", "Disguise Self", "Expeditious Retreat",
        "False Life", "Feather Fall", "Find Familiar", "Fog Cloud", "Grease",
        "Ice Knife", "Identify", "Jump", "Mage Armor", "Magic Missile", "Shield",
        "Sleep", "Thunderwave", "Witch Bolt", "Absorb Elements", "Alarm",
        "Burning Hands", "Charm Person", "Chromatic Orb", "Color Spray",
        "Comprehend Languages", "Detect Magic", "Disguise Self", "Expeditious Retreat",
        "False Life", "Feather Fall", "Find Familiar", "Fog Cloud", "Grease",
        "Ice Knife", "Identify", "Jump", "Mage Armor", "Magic Missile", "Shield",
        "Sleep", "Thunderwave", "Witch Bolt"
    },

            ["Artificer"] = new()
    {
        "Absorb Elements", "Catapult", "Cure Wounds", "Detect Magic", "Disguise Self",
        "Expeditious Retreat", "Faerie Fire", "False Life", "Feather Fall", "Grease",
        "Identify", "Jump", "Longstrider", "Mage Armor", "Magic Missile", "Purify Food and Drink",
        "Sanctuary", "Shield", "Thunderwave", "Unseen Servant"
    },

            ["Paladin"] = new()
    {
        "Bless", "Command", "Cure Wounds", "Detect Evil and Good", "Detect Magic",
        "Detect Poison and Disease", "Divine Favor", "Heroism", "Protection from Evil and Good",
        "Purify Food and Drink", "Sanctuary", "Shield of Faith", "Wrathful Smite",
        "Searing Smite", "Thunderous Smite", "Compelled Duel", "Bless", "Command",
        "Cure Wounds", "Detect Evil and Good", "Divine Favor", "Heroism",
        "Protection from Evil and Good", "Sanctuary", "Shield of Faith"
    },

            ["Ranger"] = new()
    {
        "Absorb Elements", "Alarm", "Animal Friendship", "Cure Wounds", "Detect Magic",
        "Detect Poison and Disease", "Ensnaring Strike", "Entangle", "Fog Cloud",
        "Goodberry", "Hail of Thorns", "Hunter's Mark", "Jump", "Longstrider",
        "Speak with Animals", "Zephyr Strike", "Absorb Elements", "Alarm",
        "Animal Friendship", "Cure Wounds", "Detect Magic", "Ensnaring Strike",
        "Hunter's Mark", "Jump", "Longstrider", "Speak with Animals"
    }
        };

        // ==================== FULL STARTING EQUIPMENT FOR ALL 13 CLASSES ====================
        public static Dictionary<string, List<EquipmentChoice>> StartingEquipment = new()
        {
            ["Artificer"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Armor", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Studded leather armor" },
            new EquipmentOption { Text = "Scale mail" }
        }},
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Any simple weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 2 }
        }},
        new EquipmentChoice { Label = "Ranged Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Light crossbow and 20 bolts" }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Dungeoneer's pack" },
            new EquipmentOption { Text = "Scholar's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Thieves' tools" }
        }}
    },

            ["Barbarian"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Greataxe" },
            new EquipmentOption { Text = "Any martial melee weapon", IsAnyWeapon = true, WeaponType = "Martial", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Secondary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Handaxe" },
            new EquipmentOption { Text = "Any simple weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Four javelins" }
        }}
    },

            ["Bard"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Rapier" },
            new EquipmentOption { Text = "Longsword" },
            new EquipmentOption { Text = "Any simple weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Diplomat's pack" },
            new EquipmentOption { Text = "Entertainer's pack" }
        }},
        new EquipmentChoice { Label = "Musical Instrument", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Any musical instrument of your choice" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Leather armor" },
            new EquipmentOption { Text = "Dagger" }
        }}
    },

            ["Cleric"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Mace" },
            new EquipmentOption { Text = "Warhammer (if proficient with martial weapons)" }
        }},
        new EquipmentChoice { Label = "Armor", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Scale mail" },
            new EquipmentOption { Text = "Leather armor" },
            new EquipmentOption { Text = "Chain mail (if proficient)" }
        }},
        new EquipmentChoice { Label = "Ranged / Simple Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Light crossbow and 20 bolts" },
            new EquipmentOption { Text = "Any simple weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Priest's pack" },
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Shield and a holy symbol" }
        }}
    },

            ["Druid"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Shield or Simple Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Wooden shield" },
            new EquipmentOption { Text = "Any simple weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Melee Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Scimitar" },
            new EquipmentOption { Text = "Any simple melee weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Leather armor" },
            new EquipmentOption { Text = "Druidic focus" }
        }}
    },

            ["Fighter"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Armor", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Chain mail" },
            new EquipmentOption { Text = "Leather armor + longbow and 20 arrows" }
        }},
        new EquipmentChoice { Label = "Primary Martial Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Any martial weapon and shield", IsAnyWeapon = true, WeaponType = "Martial", RequiredCount = 1, ExtraItem = "Shield" },
            new EquipmentOption { Text = "Two martial weapons", IsAnyWeapon = true, WeaponType = "Martial", RequiredCount = 2 }
        }},
        new EquipmentChoice { Label = "Secondary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Two handaxes" },
            new EquipmentOption { Text = "Light crossbow and 20 bolts" }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Dungeoneer's pack" },
            new EquipmentOption { Text = "Explorer's pack" }
        }}
    },

            ["Monk"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Shortsword" },
            new EquipmentOption { Text = "Any simple weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Dungeoneer's pack" },
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "10 darts" }
        }}
    },

            ["Paladin"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Martial weapon and shield", IsAnyWeapon = true, WeaponType = "Martial", RequiredCount = 1, ExtraItem = "Shield" },
            new EquipmentOption { Text = "Two martial weapons", IsAnyWeapon = true, WeaponType = "Martial", RequiredCount = 2 }
        }},
        new EquipmentChoice { Label = "Secondary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Five javelins" },
            new EquipmentOption { Text = "Any simple melee weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Priest's pack" },
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Chain mail" },
            new EquipmentOption { Text = "Holy symbol" }
        }}
    },

            ["Ranger"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Armor", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Scale mail" },
            new EquipmentOption { Text = "Leather armor" }
        }},
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Two shortswords" },
            new EquipmentOption { Text = "Two simple melee weapons", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 2 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Dungeoneer's pack" },
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Longbow and 20 arrows" }
        }}
    },

            ["Rogue"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Rapier" },
            new EquipmentOption { Text = "Shortsword" }
        }},
        new EquipmentChoice { Label = "Ranged Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Shortbow and 20 arrows" },
            new EquipmentOption { Text = "Light crossbow and 20 bolts" }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Burglar's pack" },
            new EquipmentOption { Text = "Dungeoneer's pack" },
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Leather armor" },
            new EquipmentOption { Text = "Two daggers" },
            new EquipmentOption { Text = "Thieves' tools" }
        }}
    },

            ["Sorcerer"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Light crossbow and 20 bolts" },
            new EquipmentOption { Text = "Any simple weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Dungeoneer's pack" },
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Two daggers" }
        }}
    },

            ["Warlock"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Light crossbow and 20 bolts" },
            new EquipmentOption { Text = "Any simple weapon", IsAnyWeapon = true, WeaponType = "Simple", RequiredCount = 1 }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Dungeoneer's pack" },
            new EquipmentOption { Text = "Scholar's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Leather armor" },
            new EquipmentOption { Text = "Two daggers" }
        }}
    },

            ["Wizard"] = new List<EquipmentChoice>
    {
        new EquipmentChoice { Label = "Primary Weapon", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Quarterstaff" },
            new EquipmentOption { Text = "Dagger" }
        }},
        new EquipmentChoice { Label = "Pack", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Scholar's pack" },
            new EquipmentOption { Text = "Explorer's pack" }
        }},
        new EquipmentChoice { Label = "Automatic Equipment", Options = new List<EquipmentOption> {
            new EquipmentOption { Text = "Spellbook" }
        }}
    }
        };

        // ==================== FULL 5E WEAPON LISTS ====================
        public static readonly List<Weapon> SimpleWeapons = new()
{
    new() { Name = "Club", Damage = "1d4", Type = "Bludgeoning", Range = "-", Properties = "Light" },
    new() { Name = "Dagger", Damage = "1d4", Type = "Piercing", Range = "20/60", Properties = "Finesse, Light, Thrown" },
    new() { Name = "Greatclub", Damage = "1d8", Type = "Bludgeoning", Range = "-", Properties = "Two-handed" },
    new() { Name = "Handaxe", Damage = "1d6", Type = "Slashing", Range = "20/60", Properties = "Light, Thrown" },
    new() { Name = "Javelin", Damage = "1d6", Type = "Piercing", Range = "30/120", Properties = "Thrown" },
    new() { Name = "Light Hammer", Damage = "1d4", Type = "Bludgeoning", Range = "20/60", Properties = "Light, Thrown" },
    new() { Name = "Mace", Damage = "1d6", Type = "Bludgeoning", Range = "-", Properties = "-" },
    new() { Name = "Quarterstaff", Damage = "1d6", Type = "Bludgeoning", Range = "-", Properties = "Versatile (1d8)" },
    new() { Name = "Sickle", Damage = "1d4", Type = "Slashing", Range = "-", Properties = "Light" },
    new() { Name = "Spear", Damage = "1d6", Type = "Piercing", Range = "20/60", Properties = "Thrown, Versatile (1d8)" },
    new() { Name = "Light Crossbow", Damage = "1d8", Type = "Piercing", Range = "80/320", Properties = "Ammunition, Loading, Two-handed" },
    new() { Name = "Dart", Damage = "1d4", Type = "Piercing", Range = "20/60", Properties = "Finesse, Thrown" },
    new() { Name = "Shortbow", Damage = "1d6", Type = "Piercing", Range = "80/320", Properties = "Ammunition, Two-handed" },
    new() { Name = "Sling", Damage = "1d4", Type = "Bludgeoning", Range = "30/120", Properties = "Ammunition" }
};

        public static readonly List<Weapon> MartialWeapons = new()
{
    new() { Name = "Battleaxe", Damage = "1d8", Type = "Slashing", Range = "-", Properties = "Versatile (1d10)" },
    new() { Name = "Flail", Damage = "1d8", Type = "Bludgeoning", Range = "-", Properties = "-" },
    new() { Name = "Glaive", Damage = "1d10", Type = "Slashing", Range = "-", Properties = "Heavy, Reach, Two-handed" },
    new() { Name = "Greataxe", Damage = "1d12", Type = "Slashing", Range = "-", Properties = "Heavy, Two-handed" },
    new() { Name = "Greatsword", Damage = "2d6", Type = "Slashing", Range = "-", Properties = "Heavy, Two-handed" },
    new() { Name = "Halberd", Damage = "1d10", Type = "Slashing", Range = "-", Properties = "Heavy, Reach, Two-handed" },
    new() { Name = "Lance", Damage = "1d12", Type = "Piercing", Range = "-", Properties = "Reach, Special" },
    new() { Name = "Longsword", Damage = "1d8", Type = "Slashing", Range = "-", Properties = "Versatile (1d10)" },
    new() { Name = "Maul", Damage = "2d6", Type = "Bludgeoning", Range = "-", Properties = "Heavy, Two-handed" },
    new() { Name = "Morningstar", Damage = "1d8", Type = "Piercing", Range = "-", Properties = "-" },
    new() { Name = "Pike", Damage = "1d10", Type = "Piercing", Range = "-", Properties = "Heavy, Reach, Two-handed" },
    new() { Name = "Rapier", Damage = "1d8", Type = "Piercing", Range = "-", Properties = "Finesse" },
    new() { Name = "Scimitar", Damage = "1d6", Type = "Slashing", Range = "-", Properties = "Finesse, Light" },
    new() { Name = "Shortsword", Damage = "1d6", Type = "Piercing", Range = "-", Properties = "Finesse, Light" },
    new() { Name = "Trident", Damage = "1d6", Type = "Piercing", Range = "20/60", Properties = "Thrown, Versatile (1d8)" },
    new() { Name = "War Pick", Damage = "1d8", Type = "Piercing", Range = "-", Properties = "-" },
    new() { Name = "Warhammer", Damage = "1d8", Type = "Bludgeoning", Range = "-", Properties = "Versatile (1d10)" },
    new() { Name = "Whip", Damage = "1d4", Type = "Slashing", Range = "-", Properties = "Finesse, Reach" },
    new() { Name = "Blowgun", Damage = "1", Type = "Piercing", Range = "25/100", Properties = "Ammunition, Loading" },
    new() { Name = "Hand Crossbow", Damage = "1d6", Type = "Piercing", Range = "30/120", Properties = "Ammunition, Light, Loading" },
    new() { Name = "Heavy Crossbow", Damage = "1d10", Type = "Piercing", Range = "100/400", Properties = "Ammunition, Heavy, Loading, Two-handed" },
    new() { Name = "Longbow", Damage = "1d8", Type = "Piercing", Range = "150/600", Properties = "Ammunition, Heavy, Two-handed" },
    new() { Name = "Net", Damage = "-", Type = "-", Range = "5/15", Properties = "Special, Thrown" }
};

        // Carrying Capacity Weights (lbs) - Base table, expandable
        public static readonly Dictionary<string, double> ItemWeights = new(StringComparer.OrdinalIgnoreCase)
        {
            // === WEAPONS ===
            ["Club"] = 2,
            ["Dagger"] = 1,
            ["Greatclub"] = 10,
            ["Handaxe"] = 2,
            ["Javelin"] = 2,
            ["Light Hammer"] = 2,
            ["Mace"] = 4,
            ["Quarterstaff"] = 4,
            ["Sickle"] = 2,
            ["Spear"] = 3,
            ["Light Crossbow"] = 5,
            ["Shortbow"] = 2,
            ["Sling"] = 0,
            ["Blowgun"] = 1,
            ["Hand Crossbow"] = 3,
            ["Heavy Crossbow"] = 18,
            ["Longbow"] = 2,
            ["Battleaxe"] = 4,
            ["Flail"] = 2,
            ["Glaive"] = 6,
            ["Greataxe"] = 7,
            ["Greatsword"] = 6,
            ["Halberd"] = 6,
            ["Lance"] = 6,
            ["Longsword"] = 3,
            ["Maul"] = 10,
            ["Morningstar"] = 4,
            ["Pike"] = 18,
            ["Rapier"] = 2,
            ["Scimitar"] = 3,
            ["Shortsword"] = 2,
            ["Trident"] = 4,
            ["War Pick"] = 2,
            ["Warhammer"] = 2,
            ["Whip"] = 3,

            // === ARMOR ===
            ["Padded"] = 8,
            ["Leather"] = 10,
            ["Studded Leather"] = 13,
            ["Hide"] = 12,
            ["Chain Shirt"] = 20,
            ["Scale Mail"] = 45,
            ["Breastplate"] = 20,
            ["Half Plate"] = 40,
            ["Ring Mail"] = 40,
            ["Chain Mail"] = 65,
            ["Splint"] = 60,
            ["Plate"] = 65,
            ["Shield"] = 6,

            // === COMMON BACKGROUND / ADVENTURING GEAR ===
            ["Backpack"] = 5,
            ["Bedroll"] = 7,
            ["Blanket"] = 3,
            ["Block and Tackle"] = 5,
            ["Book"] = 5,
            ["Candle"] = 0,
            ["Case, Map or Scroll"] = 1,
            ["Chain (10 ft)"] = 10,
            ["Chalk (1 piece)"] = 0,
            ["Chest"] = 25,
            ["Clothes, Common"] = 3,
            ["Clothes, Fine"] = 6,
            ["Clothes, Traveler's"] = 4,
            ["Component Pouch"] = 2,
            ["Crowbar"] = 5,
            ["Fishing Tackle"] = 4,
            ["Flask or Tankard"] = 1,
            ["Grappling Hook"] = 4,
            ["Hammer"] = 3,
            ["Healer’s Kit"] = 3,
            ["Holy Symbol"] = 1,
            ["Hourglass"] = 1,
            ["Hunting Trap"] = 25,
            ["Ink (1 oz bottle)"] = 0,
            ["Ink Pen"] = 0,
            ["Jug or Pitcher"] = 4,
            ["Ladder (10 ft)"] = 25,
            ["Lantern, Bullseye"] = 3,
            ["Lantern, Hooded"] = 2,
            ["Lock"] = 1,
            ["Magnifying Glass"] = 0,
            ["Manacles"] = 6,
            ["Mess Kit"] = 1,
            ["Mirror, Steel"] = 0.5,
            ["Oil (flask)"] = 1,
            ["Paper (one sheet)"] = 0,
            ["Parchment (one sheet)"] = 0,
            ["Perfume (vial)"] = 0,
            ["Pick, Miner’s"] = 10,
            ["Piton"] = 0.25,
            ["Poison, Basic (vial)"] = 0,
            ["Pole (10 ft)"] = 7,
            ["Pot, Iron"] = 1,
            ["Pouch"] = 1,
            ["Quiver"] = 1,
            ["Ram, Portable"] = 35,
            ["Rations (1 day)"] = 2,
            ["Robes"] = 4,
            ["Rope, Hempen (50 ft)"] = 10,
            ["Rope, Silk (50 ft)"] = 5,
            ["Sack"] = 0.5,
            ["Scale, Merchant’s"] = 6,
            ["Sealing Wax"] = 0,
            ["Shovel"] = 5,
            ["Signal Whistle"] = 0,
            ["Signet Ring"] = 0,
            ["Soap"] = 0,
            ["Spellbook"] = 3,
            ["Spike, Iron"] = 1,
            ["Spyglass"] = 1,
            ["Tent, Two-Person"] = 20,
            ["Tinderbox"] = 1,
            ["Torch"] = 1,
            ["Vial"] = 0,
            ["Waterskin"] = 5,
            ["Whetstone"] = 1,

            // === AMMUNITION ===
            ["Arrows (20)"] = 1,
            ["Bolts (20)"] = 1.5,
            ["Sling Bullets (20)"] = 1.5,
            ["Blowgun Needles (50)"] = 1,

            // === PACKS (flat weights for now) ===
            ["Explorer's Pack"] = 59,
            ["Dungeoneer's Pack"] = 61,
            ["Priest's Pack"] = 29,
            ["Scholar's Pack"] = 21,
            ["Diplomat's Pack"] = 39,
            ["Entertainer's Pack"] = 38,
            ["Burglar's Pack"] = 44
        };

        // Official 5e Musical Instruments
        public static readonly List<string> MusicalInstruments = new()
{
    "Bagpipes", "Drum", "Dulcimer", "Flute", "Horn", "Lute", "Lyre",
    "Pan flute", "Shawm", "Viol"
};

        public static readonly List<string> AllLanguages = new()
        {
            "Common", "Dwarvish", "Elvish", "Giant", "Gnomish", "Goblin", "Halfling", "Orc",
            "Abyssal", "Celestial", "Draconic", "Deep Speech", "Infernal", "Primordial", "Sylvan", "Undercommon",
            "Aquan", "Auran", "Ignan", "Terran"
        };

        // ==================== ALL SKILLS (Single Source of Truth) ====================
        public static List<SkillProficiency> CreateAllSkills()
        {
            return new List<SkillProficiency>
            {
                new("Acrobatics", "Dex"),
                new("Animal Handling", "Wis"),
                new("Arcana", "Int"),
                new("Athletics", "Str"),
                new("Deception", "Cha"),
                new("History", "Int"),
                new("Insight", "Wis"),
                new("Intimidation", "Cha"),
                new("Investigation", "Int"),
                new("Medicine", "Wis"),
                new("Nature", "Int"),
                new("Perception", "Wis"),
                new("Performance", "Cha"),
                new("Persuasion", "Cha"),
                new("Religion", "Int"),
                new("Sleight of Hand", "Dex"),
                new("Stealth", "Dex"),
                new("Survival", "Wis")
            };
        }

        // ==================== ABILITY SCORE HELPER (Single Source of Truth) ====================
        public static int GetAbilityScore(string ability, Dictionary<string, int> finalStats)
        {
            if (finalStats == null || string.IsNullOrEmpty(ability))
                return 10;

            return finalStats.TryGetValue(ability, out int value) ? value : 10;
        }

        // ==================== CENTRALIZED LISTS (Single Source of Truth) ====================

        public static readonly List<string> AllBackgrounds = new()
{
    "Acolyte", "Athlete", "City Watch", "Courtier", "Criminal", "Entertainer",
    "Faction Agent", "Far Traveler", "Feylost", "Folk Hero", "Gladiator", "Hermit",
    "Inheritor", "Knight", "Marine", "Noble", "Outlander", "Pirate", "Rune Carver",
    "Sage", "Sailor", "Shipwright", "Smuggler", "Soldier", "Spy",
    "Urban Bounty Hunter", "Urchin"
};

        public static readonly List<string> FeatGrantingRaces = new()
{
    "Variant Human", "Custom Lineage"
};

        // Skill lists grant auto-proficiencies on the Skills tab.
        // For backgrounds with player skill choices, only the FIXED skill(s) are listed here;
        // the optional second skill is documented in BackgroundDetails for the player to select.
        // Source: https://dnd5e.wikidot.com/#toc3 (PHB, SCAG, other official books)
        public static readonly Dictionary<string, List<string>> BackgroundSkillMap = new()
        {
            ["Acolyte"] = new() { "Insight", "Religion" },
            ["Athlete"] = new() { "Acrobatics", "Athletics" },
            ["City Watch"] = new() { "Athletics", "Insight" },
            ["Courtier"] = new() { "Insight", "Persuasion" },
            ["Criminal"] = new() { "Deception", "Stealth" },
            ["Entertainer"] = new() { "Acrobatics", "Performance" },
            // Faction Agent: Insight + one Int/Wis/Cha skill of your choice (only Insight auto-granted)
            ["Faction Agent"] = new() { "Insight" },
            ["Far Traveler"] = new() { "Insight", "Perception" },
            ["Feylost"] = new() { "Deception", "Survival" },
            ["Folk Hero"] = new() { "Animal Handling", "Survival" },
            ["Gladiator"] = new() { "Acrobatics", "Performance" },
            ["Hermit"] = new() { "Medicine", "Religion" },
            // Inheritor: Survival + one of Arcana, History, or Religion (only Survival auto-granted)
            ["Inheritor"] = new() { "Survival" },
            ["Knight"] = new() { "History", "Persuasion" },
            ["Marine"] = new() { "Athletics", "Survival" },
            ["Noble"] = new() { "History", "Persuasion" },
            ["Outlander"] = new() { "Athletics", "Survival" },
            ["Pirate"] = new() { "Athletics", "Perception" },
            ["Rune Carver"] = new() { "History", "Perception" },
            ["Sage"] = new() { "Arcana", "History" },
            ["Sailor"] = new() { "Athletics", "Perception" },
            ["Shipwright"] = new() { "History", "Perception" },
            ["Smuggler"] = new() { "Athletics", "Deception" },
            ["Soldier"] = new() { "Athletics", "Intimidation" },
            ["Spy"] = new() { "Deception", "Stealth" },
            // Urban Bounty Hunter: choose two from Deception, Insight, Persuasion, Stealth (player choice)
            ["Urban Bounty Hunter"] = new() { },
            ["Urchin"] = new() { "Sleight of Hand", "Stealth" }
        };

        /// <summary>
        /// Full official background summary shown on the Background tab.
        /// Aligned to dnd5e.wikidot.com (PHB / SCAG / GGtR / Theros / Witchlight / Glory of the Giants / Ghosts of Saltmarsh).
        /// </summary>
        public static readonly Dictionary<string, string> BackgroundDetails = new()
        {
            ["Acolyte"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Insight, Religion\n" +
                "TOOL PROFICIENCIES: None\n" +
                "LANGUAGES: Two of your choice\n" +
                "EQUIPMENT: A holy symbol (a gift when you entered the priesthood), a prayer book or prayer wheel, 5 sticks of incense, vestments, a set of common clothes, and a pouch containing 15 gp\n\n" +
                "FEATURE — Shelter of the Faithful:\n" +
                "As an acolyte, you command the respect of those who share your faith, and you can perform the religious ceremonies of your deity. You and your adventuring companions can expect to receive free healing and care at a temple, shrine, or other established presence of your faith, though you must provide any material components needed for spells. Those who share your religion will support you (but only you) at a modest lifestyle.\n\n" +
                "You might also have ties to a specific temple dedicated to your chosen deity or pantheon, and you have a residence there. While near your temple, you can call upon the priests for assistance, provided the assistance you ask for is not hazardous and you remain in good standing with your temple.",

            ["Athlete"] =
                "SOURCE: Mythic Odysseys of Theros\n\n" +
                "SKILL PROFICIENCIES: Acrobatics, Athletics\n" +
                "TOOL PROFICIENCIES: Vehicles (land)\n" +
                "LANGUAGES: One of your choice\n" +
                "EQUIPMENT: A bronze discus or leather ball, a lucky charm or past trophy, a set of traveler's clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Echoes of Victory:\n" +
                "You have attracted admiration among spectators, fellow athletes, and trainers in the region that hosted your past athletic victories. When visiting any settlement within 100 miles of where you grew up, there is a 50 percent chance you can find someone there who admires you and is willing to provide information and temporary shelter.\n\n" +
                "Between adventures, you might compete in athletic events sufficient enough to maintain a comfortable lifestyle, as per the \"Practicing a Profession\" downtime activity in the Player's Handbook.",

            ["City Watch"] =
                "SOURCE: Sword Coast Adventurer's Guide\n\n" +
                "SKILL PROFICIENCIES: Athletics, Insight\n" +
                "TOOL PROFICIENCIES: None\n" +
                "LANGUAGES: Two of your choice\n" +
                "EQUIPMENT: A uniform in the style of your unit and indicative of your rank, a horn with which to summon help, a set of manacles, and a pouch containing 10 gp\n\n" +
                "VARIANT — Investigator:\n" +
                "If your prior experience is as an investigator, you have proficiency in Investigation rather than Athletics.\n\n" +
                "FEATURE — Watcher's Eye:\n" +
                "Your experience in enforcing the law, and dealing with lawbreakers, gives you a feel for local laws and criminals. You can easily find the local outpost of the watch or a similar organization, and just as easily pick out the dens of criminal activity in a community, although you're more likely to be welcome in the former locations rather than the latter.",

            ["Courtier"] =
                "SOURCE: Sword Coast Adventurer's Guide\n\n" +
                "SKILL PROFICIENCIES: Insight, Persuasion\n" +
                "TOOL PROFICIENCIES: None\n" +
                "LANGUAGES: Two of your choice\n" +
                "EQUIPMENT: A set of fine clothes and a pouch containing 5 gp\n\n" +
                "FEATURE — Court Functionary:\n" +
                "Your knowledge of how bureaucracies function lets you gain access to the records and inner workings of any noble court or government you encounter. You know who the movers and shakers are, whom to go to for the favors you seek, and what the current intrigues of interest in the group are.",

            ["Criminal"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Deception, Stealth\n" +
                "TOOL PROFICIENCIES: One type of gaming set, thieves' tools\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A crowbar, a set of dark common clothes including a hood, and a pouch containing 15 gp\n\n" +
                "FEATURE — Criminal Contact:\n" +
                "You have a reliable and trustworthy contact who acts as your liaison to a network of other criminals. You know how to get messages to and from your contact, even over great distances; specifically, you know the local messengers, corrupt caravan masters, and seedy sailors who can deliver messages for you.",

            ["Entertainer"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Acrobatics, Performance\n" +
                "TOOL PROFICIENCIES: Disguise kit, one type of musical instrument\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A musical instrument (one of your choice), the favor of an admirer (love letter, lock of hair, or trinket), a costume, and a pouch containing 15 gp\n\n" +
                "FEATURE — By Popular Demand:\n" +
                "You can always find a place to perform, usually in an inn or tavern but possibly with a circus, at a theater, or even in a noble's court. At such a place, you receive free lodging and food of a modest or comfortable standard (depending on the quality of the establishment), as long as you perform each night. In addition, your performance makes you something of a local figure. When strangers recognize you in a town where you have performed, they typically take a liking to you.",

            ["Faction Agent"] =
                "SOURCE: Sword Coast Adventurer's Guide\n\n" +
                "SKILL PROFICIENCIES: Insight, and one Intelligence, Wisdom, or Charisma skill of your choice (as appropriate to your faction)\n" +
                "  → Nemo auto-grants Insight; choose and mark your second skill on the Skills tab.\n" +
                "TOOL PROFICIENCIES: None\n" +
                "LANGUAGES: Two of your choice\n" +
                "EQUIPMENT: The badge or emblem of your faction, a copy of a seminal faction text (or a code-book for a covert faction), a set of common clothes, and a pouch containing 15 gp\n\n" +
                "FEATURE — Safe Haven:\n" +
                "As a faction agent, you have access to a secret network of supporters and operatives who can provide assistance on your adventures. You know a set of secret signs and passwords you can use to identify such operatives, who can provide you with access to a hidden safe house, free room and board, or assistance in finding information. These agents never risk their lives for you or risk revealing their true identities.",

            ["Far Traveler"] =
                "SOURCE: Sword Coast Adventurer's Guide\n\n" +
                "SKILL PROFICIENCIES: Insight, Perception\n" +
                "TOOL PROFICIENCIES: Any one musical instrument or gaming set of your choice (likely something native to your homeland)\n" +
                "LANGUAGES: Any one of your choice\n" +
                "EQUIPMENT: One set of traveler's clothes, any one musical instrument or gaming set you are proficient with, poorly wrought maps from your homeland that depict where you are in Faerûn, a small piece of jewelry worth 10 gp in the style of your homeland's craftsmanship, and a pouch containing 5 gp\n\n" +
                "FEATURE — All Eyes on You:\n" +
                "Your accent, mannerisms, figures of speech, and perhaps even your appearance all mark you as foreign. Curious glances are directed your way wherever you go, which can be a nuisance, but you also gain the friendly interest of scholars and others intrigued by far-off lands, to say nothing of everyday folk who are eager to hear stories of your homeland.\n\n" +
                "You can parley this attention into access to people and places you might not otherwise have, for you and your traveling companions. Noble lords, scholars, and merchant princes, to name a few, might be interested in hearing about your distant homeland and people.",

            ["Feylost"] =
                "SOURCE: The Wild Beyond the Witchlight\n\n" +
                "SKILL PROFICIENCIES: Deception, Survival\n" +
                "TOOL PROFICIENCIES: One type of musical instrument\n" +
                "LANGUAGES: One of your choice of Elvish, Gnomish, Goblin, or Sylvan\n" +
                "EQUIPMENT: A musical instrument (one of your choice), a set of traveler's clothes, three trinkets (each determined by rolling on the Feywild Trinkets table), and a pouch containing 8 gp\n\n" +
                "FEATURE — Fey Mark:\n" +
                "You were transformed in some small way by your stay in the Feywild and gained a fey mark (work with your DM; examples include iridescent eyes, a sweet scent like nectar, cat-like whiskers, sparkling skin in moonlight, a tail, etc.).\n\n" +
                "FEATURE — Feywild Visitor:\n" +
                "Whenever you're sound asleep or in a deep trance during a long rest, a spirit of the Feywild might pay you a visit, if the DM wishes it. No harm ever comes to you as a result of such visits, which can last for minutes or hours, and you remember each visit when you wake up. Conversations can contain messages, insights, nonsense, or red herrings, at the DM's discretion.\n\n" +
                "FEATURE — Feywild Connection:\n" +
                "Your mannerisms and knowledge of fey customs are recognized by natives of the Feywild, who see you as one of their own. Because of this, friendly Fey creatures are inclined to come to your aid if you are lost or need help in the Feywild.",

            ["Folk Hero"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Animal Handling, Survival\n" +
                "TOOL PROFICIENCIES: One type of artisan's tools, vehicles (land)\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A set of artisan's tools (one of your choice), a shovel, an iron pot, a set of common clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Rustic Hospitality:\n" +
                "Since you come from the ranks of the common folk, you fit in among them with ease. You can find a place to hide, rest, or recuperate among other commoners, unless you have shown yourself to be a danger to them. They will shield you from the law or anyone else searching for you, though they will not risk their lives for you.",

            ["Gladiator"] =
                "SOURCE: Player's Handbook (Entertainer variant)\n\n" +
                "SKILL PROFICIENCIES: Acrobatics, Performance\n" +
                "TOOL PROFICIENCIES: Disguise kit, one type of musical instrument\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: An inexpensive but unusual weapon (such as a trident or net) instead of a musical instrument if you wish, the favor of an admirer, a costume, and a pouch containing 15 gp\n\n" +
                "FEATURE — By Popular Demand (combat venues):\n" +
                "A gladiator is as much an entertainer as any minstrel. Using your By Popular Demand feature, you can find a place to perform in any place that features combat for entertainment — perhaps a gladiatorial arena or secret pit fighting club. You receive free lodging and food of a modest or comfortable standard as long as you perform each night, and your performances make you a local figure strangers often take a liking to.",

            ["Hermit"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Medicine, Religion\n" +
                "TOOL PROFICIENCIES: Herbalism kit\n" +
                "LANGUAGES: One of your choice\n" +
                "EQUIPMENT: A scroll case stuffed full of notes from your studies or prayers, a winter blanket, a set of common clothes, an herbalism kit, and 5 gp\n\n" +
                "FEATURE — Discovery:\n" +
                "The quiet seclusion of your extended hermitage gave you access to a unique and powerful discovery. The exact nature of this revelation depends on the nature of your seclusion. It might be a great truth about the cosmos, the deities, the powerful beings of the outer planes, or the forces of nature. It could be a site that no one else has ever seen, a long-forgotten fact, a relic of the past, or information damaging to those who consigned you to exile.\n\n" +
                "Work with your DM to determine the details of your discovery and its impact on the campaign.",

            ["Inheritor"] =
                "SOURCE: Sword Coast Adventurer's Guide\n\n" +
                "SKILL PROFICIENCIES: Survival, plus one from among Arcana, History, and Religion\n" +
                "  → Nemo auto-grants Survival; choose and mark Arcana, History, or Religion on the Skills tab.\n" +
                "TOOL PROFICIENCIES: Your choice of a gaming set or a musical instrument\n" +
                "LANGUAGES: Any one of your choice\n" +
                "EQUIPMENT: Your inheritance, a set of traveler's clothes, the tool you choose for this background's tool proficiency, and a pouch containing 15 gp\n\n" +
                "FEATURE — Inheritance:\n" +
                "You are the heir to something of great value — not mere coin, but an object entrusted to you alone (a document, trinket, clothing, jewelry, arcane book, story/song/poem/secret, tattoo, etc.). Work with your DM to determine why it matters, its full story, and its properties. The DM may use it as a story hook; foes may covet it. You can decide whether to tell companions about it right away or keep it secret until you learn more.",

            ["Knight"] =
                "SOURCE: Player's Handbook (Noble variant)\n\n" +
                "SKILL PROFICIENCIES: History, Persuasion\n" +
                "TOOL PROFICIENCIES: One type of gaming set\n" +
                "LANGUAGES: One of your choice\n" +
                "EQUIPMENT: A set of fine clothes, a signet ring, a scroll of pedigree, and a purse containing 25 gp (you might also include a banner or token from a noble to whom you have given your heart)\n\n" +
                "FEATURE — Retainers (instead of Position of Privilege):\n" +
                "You have the service of three retainers loyal to your family. These retainers can be attendants or messengers, and one might be a majordomo. As a knight, one retainer is typically a noble who serves as your squire; the others might include a groom and a servant who polishes your armor.\n\n" +
                "Your retainers are commoners who can perform mundane tasks for you, but they do not fight for you, will not follow you into obviously dangerous areas (such as dungeons), and will leave if they are frequently endangered or abused.",

            ["Marine"] =
                "SOURCE: Ghosts of Saltmarsh\n\n" +
                "SKILL PROFICIENCIES: Athletics, Survival\n" +
                "TOOL PROFICIENCIES: Vehicles (land & water)\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A dagger that belonged to a fallen comrade, a folded rag emblazoned with the symbol of your ship or company, a set of traveler's clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Steady:\n" +
                "You can move twice the normal amount of time (up to 16 hours) each day before being subject to the effect of a forced march (see \"Travel Pace\" in the Player's Handbook). Additionally, you can automatically find a safe route to land a boat on shore, provided such a route exists.",

            ["Noble"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: History, Persuasion\n" +
                "TOOL PROFICIENCIES: One type of gaming set\n" +
                "LANGUAGES: One of your choice\n" +
                "EQUIPMENT: A set of fine clothes, a signet ring, a scroll of pedigree, and a purse containing 25 gp\n\n" +
                "FEATURE — Position of Privilege:\n" +
                "Thanks to your noble birth, people are inclined to think the best of you. You are welcome in high society, and people assume you have the right to be wherever you are. The common folk make every effort to accommodate you and avoid your displeasure, and other people of high birth treat you as a member of the same social sphere. You can secure an audience with a local noble if you need to.\n\n" +
                "VARIANT FEATURE — Retainers: If you prefer, you may take the Knight/Retainers feature instead of Position of Privilege.",

            ["Outlander"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Athletics, Survival\n" +
                "TOOL PROFICIENCIES: One type of musical instrument\n" +
                "LANGUAGES: One of your choice\n" +
                "EQUIPMENT: A staff, a hunting trap, a trophy from an animal you killed, a set of traveler's clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Wanderer:\n" +
                "You have an excellent memory for maps and geography, and you can always recall the general layout of terrain, settlements, and other features around you. In addition, you can find food and fresh water for yourself and up to five other people each day, provided that the land offers berries, small game, water, and so forth.",

            ["Pirate"] =
                "SOURCE: Player's Handbook (Sailor variant)\n\n" +
                "SKILL PROFICIENCIES: Athletics, Perception\n" +
                "TOOL PROFICIENCIES: Navigator's tools, vehicles (water)\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A belaying pin (club), 50 feet of silk rope, a lucky charm (such as a rabbit foot or a small stone with a hole in the center), a set of common clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Bad Reputation (instead of Ship's Passage):\n" +
                "No matter where you go, people are afraid of you due to your reputation. When you are in a civilized settlement, you can get away with minor criminal offenses, such as refusing to pay for food at a tavern or breaking down doors at a local shop, since most people will not report your activity to the authorities.",

            ["Rune Carver"] =
                "SOURCE: Bigby Presents: Glory of the Giants\n\n" +
                "SKILL PROFICIENCIES: History, Perception\n" +
                "TOOL PROFICIENCIES: One set of artisan's tools\n" +
                "LANGUAGES: Giant\n" +
                "EQUIPMENT: A set of artisan's tools (one of your choice), a small knife, a whetstone, a set of common clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Rune Shaper:\n" +
                "You gain the Rune Shaper feat.",

            ["Sage"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Arcana, History\n" +
                "TOOL PROFICIENCIES: None\n" +
                "LANGUAGES: Two of your choice\n" +
                "EQUIPMENT: A bottle of black ink, a quill, a small knife, a letter from a dead colleague posing a question you have not yet been able to answer, a set of common clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Researcher:\n" +
                "When you attempt to learn or recall a piece of lore, if you do not know that information, you often know where and from whom you can obtain it. Usually, this information comes from a library, scriptorium, university, or a sage or other learned person or creature. Your DM might rule that the knowledge you seek is secreted away in an almost inaccessible place, or that it simply cannot be found. Unearthing the deepest secrets of the multiverse can require an adventure or even a whole campaign.",

            ["Sailor"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Athletics, Perception\n" +
                "TOOL PROFICIENCIES: Navigator's tools, vehicles (water)\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A belaying pin (club), 50 feet of silk rope, a lucky charm (such as a rabbit foot or a small stone with a hole in the center), a set of common clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Ship's Passage:\n" +
                "When you need to, you can secure free passage on a sailing ship for yourself and your adventuring companions. You might sail on the ship you served on, or another ship you have good relations with (perhaps one captained by a former crewmate). Because you're calling in a favor, you can't be certain of a schedule or route that will meet your every need. Your DM will determine how long it takes to get where you need to go. In return for your free passage, you and your companions are expected to assist the crew during the voyage.",

            ["Shipwright"] =
                "SOURCE: Ghosts of Saltmarsh\n\n" +
                "SKILL PROFICIENCIES: History, Perception\n" +
                "TOOL PROFICIENCIES: Carpenter's tools, vehicles (water)\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A set of well-loved carpenter's tools, a blank book, 1 ounce of ink, an ink pen, a set of traveler's clothes, and a leather pouch with 10 gp\n\n" +
                "FEATURE — I'll Patch It!:\n" +
                "Provided you have carpenter's tools and wood, you can perform repairs on a water vehicle. When you use this ability, you restore a number of hit points to the hull of a water vehicle equal to 5 × your proficiency bonus. A vehicle cannot be patched by you in this way again until after it has been pulled ashore and fully repaired.",

            ["Smuggler"] =
                "SOURCE: Ghosts of Saltmarsh\n\n" +
                "SKILL PROFICIENCIES: Athletics, Deception\n" +
                "TOOL PROFICIENCIES: Vehicles (water)\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A fancy leather vest or a pair of leather boots, a set of common clothes, and a leather pouch with 15 gp\n\n" +
                "FEATURE — Down Low:\n" +
                "You are acquainted with a network of smugglers who are willing to help you out of tight situations. While in a particular town, city, or other similarly sized community (DM's discretion), you and your companions can stay for free in safe houses. Safe houses provide a poor lifestyle. While staying at a safe house, you can choose to keep your presence (and that of your companions) a secret.",

            ["Soldier"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Athletics, Intimidation\n" +
                "TOOL PROFICIENCIES: One type of gaming set, vehicles (land)\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: An insignia of rank, a trophy taken from a fallen enemy (a dagger, broken blade, or piece of a banner), a set of bone dice or a deck of cards, a set of common clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — Military Rank:\n" +
                "You have a military rank from your career as a soldier. Soldiers loyal to your former military organization still recognize your authority and influence, and they defer to you if they are of a lower rank. You can invoke your rank to exert influence over other soldiers and requisition simple equipment or horses for temporary use. You can also usually gain access to friendly military encampments and fortresses where your rank is recognized.",

            ["Spy"] =
                "SOURCE: Player's Handbook (Criminal variant)\n\n" +
                "SKILL PROFICIENCIES: Deception, Stealth\n" +
                "TOOL PROFICIENCIES: One type of gaming set, thieves' tools\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A crowbar, a set of dark common clothes including a hood, and a pouch containing 15 gp\n\n" +
                "FEATURE — Criminal Contact:\n" +
                "Although your capabilities are not much different from those of a burglar or smuggler, you learned them as an espionage agent. You have a reliable and trustworthy contact who acts as your liaison to a network of other criminals (or intelligence operatives). You know how to get messages to and from your contact, even over great distances; specifically, you know the local messengers, corrupt caravan masters, and seedy sailors who can deliver messages for you.",

            ["Urban Bounty Hunter"] =
                "SOURCE: Sword Coast Adventurer's Guide\n\n" +
                "SKILL PROFICIENCIES: Choose two from among Deception, Insight, Persuasion, and Stealth\n" +
                "  → Nemo does not auto-grant these; mark your two chosen skills on the Skills tab.\n" +
                "TOOL PROFICIENCIES: Choose two from among one type of gaming set, one musical instrument, and thieves' tools\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A set of clothes appropriate to your duties and a pouch containing 20 gp\n\n" +
                "FEATURE — Ear to the Ground:\n" +
                "You are in frequent contact with people in the segment of society that your chosen quarries move through. These people might be associated with the criminal underworld, the rough-and-tumble folk of the streets, or members of high society. This connection comes in the form of a contact in any city you visit, a person who provides information about the people and places of the local area.",

            ["Urchin"] =
                "SOURCE: Player's Handbook\n\n" +
                "SKILL PROFICIENCIES: Sleight of Hand, Stealth\n" +
                "TOOL PROFICIENCIES: Disguise kit, thieves' tools\n" +
                "LANGUAGES: None\n" +
                "EQUIPMENT: A small knife, a map of the city you grew up in, a pet mouse, a token to remember your parents by, a set of common clothes, and a pouch containing 10 gp\n\n" +
                "FEATURE — City Secrets:\n" +
                "You know the secret patterns and flow to cities and can find passages through the urban sprawl that others would miss. When you are not in combat, you (and companions you lead) can travel between any two locations in the city twice as fast as your speed would normally allow."
        };

        // ==================== BACKGROUND EQUIPMENT (Single Source of Truth) ====================
        // Matches official equipment packages from PHB / SCAG / etc.
        public static readonly Dictionary<string, string> BackgroundEquipment = new()
        {
            ["Acolyte"] = "Holy symbol, prayer book or prayer wheel, 5 sticks of incense, vestments, common clothes, pouch with 15 gp",
            ["Athlete"] = "Bronze discus or leather ball, lucky charm or past trophy, traveler's clothes, pouch with 10 gp",
            ["City Watch"] = "Uniform (unit style/rank), horn, manacles, pouch with 10 gp",
            ["Courtier"] = "Fine clothes, pouch with 5 gp",
            ["Criminal"] = "Crowbar, dark common clothes with hood, pouch with 15 gp",
            ["Entertainer"] = "Musical instrument, favor of an admirer, costume, pouch with 15 gp",
            ["Faction Agent"] = "Faction badge or emblem, faction text or code-book, common clothes, pouch with 15 gp",
            ["Far Traveler"] = "Traveler's clothes, musical instrument or gaming set, poorly wrought homeland maps, jewelry worth 10 gp, pouch with 5 gp",
            ["Feylost"] = "Musical instrument, traveler's clothes, three Feywild trinkets, pouch with 8 gp",
            ["Folk Hero"] = "Artisan's tools (one of your choice), shovel, iron pot, common clothes, pouch with 10 gp",
            ["Gladiator"] = "Unusual inexpensive weapon (e.g. trident or net) or musical instrument, favor of an admirer, costume, pouch with 15 gp",
            ["Hermit"] = "Scroll case of notes, winter blanket, common clothes, herbalism kit, 5 gp",
            ["Inheritor"] = "Your inheritance, traveler's clothes, gaming set or musical instrument, pouch with 15 gp",
            ["Knight"] = "Fine clothes, signet ring, scroll of pedigree, purse with 25 gp",
            ["Marine"] = "Dagger (fallen comrade's), folded rag with ship/company symbol, traveler's clothes, pouch with 10 gp",
            ["Noble"] = "Fine clothes, signet ring, scroll of pedigree, purse with 25 gp",
            ["Outlander"] = "Staff, hunting trap, animal trophy, traveler's clothes, pouch with 10 gp",
            ["Pirate"] = "Belaying pin (club), 50 feet of silk rope, lucky charm, common clothes, pouch with 10 gp",
            ["Rune Carver"] = "Artisan's tools (one of your choice), small knife, whetstone, common clothes, pouch with 10 gp",
            ["Sage"] = "Bottle of black ink, quill, small knife, letter from a dead colleague, common clothes, pouch with 10 gp",
            ["Sailor"] = "Belaying pin (club), 50 feet of silk rope, lucky charm, common clothes, pouch with 10 gp",
            ["Shipwright"] = "Carpenter's tools, blank book, 1 ounce of ink, ink pen, traveler's clothes, leather pouch with 10 gp",
            ["Smuggler"] = "Fancy leather vest or leather boots, common clothes, leather pouch with 15 gp",
            ["Soldier"] = "Insignia of rank, trophy from a fallen enemy, bone dice or deck of cards, common clothes, pouch with 10 gp",
            ["Spy"] = "Crowbar, dark common clothes with hood, pouch with 15 gp",
            ["Urban Bounty Hunter"] = "Clothes appropriate to your duties, pouch with 20 gp",
            ["Urchin"] = "Small knife, map of your home city, pet mouse, token to remember your parents by, common clothes, pouch with 10 gp"
        };

        public static string GetBackgroundEquipment(string background)
        {
            return BackgroundEquipment.TryGetValue(background, out var equipment)
                ? equipment
                : "See Background tab for full equipment details";
        }



        public static readonly List<Armor> AllArmors = new()
{
    // Light Armor
    new Armor { Name = "Padded", Type = "Light", AC = "11 + Dex", StealthDisadvantage = "Yes" },
    new Armor { Name = "Leather", Type = "Light", AC = "11 + Dex" },
    new Armor { Name = "Studded Leather", Type = "Light", AC = "12 + Dex" },

    // Medium Armor
    new Armor { Name = "Hide", Type = "Medium", AC = "12 + Dex (max 2)" },
    new Armor { Name = "Chain Shirt", Type = "Medium", AC = "13 + Dex (max 2)" },
    new Armor { Name = "Scale Mail", Type = "Medium", AC = "14 + Dex (max 2)", StealthDisadvantage = "Yes" },
    new Armor { Name = "Breastplate", Type = "Medium", AC = "14 + Dex (max 2)" },
    new Armor { Name = "Half-Plate", Type = "Medium", AC = "15 + Dex (max 2)", StealthDisadvantage = "Yes" },

    // Heavy Armor
    new Armor { Name = "Ring Mail", Type = "Heavy", AC = "14", StrengthRequirement = "Str 13", StealthDisadvantage = "Yes" },
    new Armor { Name = "Chain Mail", Type = "Heavy", AC = "16", StrengthRequirement = "Str 13", StealthDisadvantage = "Yes" },
    new Armor { Name = "Splint", Type = "Heavy", AC = "17", StrengthRequirement = "Str 15", StealthDisadvantage = "Yes" },
    new Armor { Name = "Plate", Type = "Heavy", AC = "18", StrengthRequirement = "Str 15", StealthDisadvantage = "Yes" }
};

        public static List<Feat> AllFeats { get; private set; } = new();

        public static void InitializeFeats()
        {
            AllFeats = new List<Feat>
    {
        // === CORE FEATS ===
        new Feat {
            Name = "Actor",
            ShortDescription = "Skilled at mimicry and dramatic performance.",
            Prerequisites = "None",
            FullDescription = "Increase your Charisma score by 1, to a maximum of 20.\n\nYou have advantage on Charisma (Deception) and Charisma (Performance) checks when trying to pass yourself off as a different person.\n\nYou can mimic the speech of another person or the sounds made by other creatures. You must have heard the person speaking, or heard the creature make the sound, for at least 1 minute."
        },
        new Feat {
            Name = "Alert",
            ShortDescription = "Always on the lookout for danger.",
            Prerequisites = "None",
            FullDescription = "Increase your Dexterity score by 1, to a maximum of 20.\n\nYou gain a +5 bonus to initiative.\n\nYou can't be surprised while you are conscious.\n\nOther creatures don't gain advantage on attack rolls against you as a result of being hidden from you."
        },
        new Feat {
            Name = "Athlete",
            ShortDescription = "You have undergone extensive physical training.",
            Prerequisites = "None",
            HasDynamicStatChoice = true,
            FullDescription = "Increase your Strength, Dexterity, or Constitution score by 1, to a maximum of 20.\n\nWhen you are prone, standing up uses only 5 feet of your movement.\n\nClimbing doesn't halve your speed.\n\nYou can make a running long jump or a running high jump after moving only 5 feet on foot, rather than 10 feet."
        },
        new Feat {
            Name = "Charger",
            ShortDescription = "You can take the Dash action as a bonus action.",
            Prerequisites = "None",
            FullDescription = "When you use the Dash action, you can make one melee weapon attack or shove a creature as a bonus action.\n\nIf you move at least 10 feet in a straight line immediately before taking this bonus action, you either gain a +5 bonus to the attack's damage roll (if it was a melee attack) or push the target up to 10 feet away from you (if you shove and you chose to push)."
        },
        new Feat {
            Name = "Crossbow Expert",
            ShortDescription = "Thanks to extensive practice, you can reload crossbows with ease.",
            Prerequisites = "None",
            FullDescription = "You ignore the loading property of crossbows with which you are proficient.\n\nBeing within 5 feet of a hostile creature doesn't impose disadvantage on your ranged attack rolls.\n\nWhen you use the Attack action and attack with a one-handed weapon, you can use a bonus action to attack with a loaded hand crossbow you are holding."
        },
        new Feat {
            Name = "Defensive Duelist",
            ShortDescription = "You excel at using a weapon to deflect attacks.",
            Prerequisites = "Proficiency with a finesse weapon",
            FullDescription = "Increase your Dexterity score by 1, to a maximum of 20.\n\nWhen you are wielding a finesse weapon with which you are proficient and another creature hits you with a melee attack, you can use your reaction to add your proficiency bonus to your AC for that attack, potentially causing the attack to miss you."
        },
        new Feat {
            Name = "Dual Wielder",
            ShortDescription = "You master fighting with two weapons.",
            Prerequisites = "None",
            HasDynamicStatChoice = true,
            FullDescription = "Increase your Strength or Dexterity score by 1, to a maximum of 20.\n\nYou can use two-weapon fighting even when the one-handed melee weapons you are wielding aren't light.\n\nYou can draw or stow two one-handed weapons when you would normally be able to draw or stow only one."
        },
        new Feat {
            Name = "Elemental Adept",
            ShortDescription = "Your spells ignore resistance to one damage type.",
            Prerequisites = "Spellcasting feature",
            FullDescription = "Choose one of the following damage types: acid, cold, fire, lightning, or thunder.\n\nSpells you cast ignore resistance to damage of the chosen type. In addition, when you roll damage for a spell you cast that deals damage of that type, you can treat any 1 on a damage die as a 2."
        },
        new Feat {
            Name = "Fey Touched",
            ShortDescription = "You have been touched by the feywild.",
            Prerequisites = "None",
            HasDynamicStatChoice = true,
            FullDescription = "Increase your Intelligence, Wisdom, or Charisma score by 1, to a maximum of 20.\n\nYou learn the misty step spell and one 1st-level spell of your choice. The 1st-level spell must be from the enchantment or illusion school of magic. You can cast each of these spells without expending a spell slot once per long rest."
        },
        new Feat {
            Name = "Fighting Initiate",
            ShortDescription = "Your martial training has given you a fighting style.",
            Prerequisites = "Proficiency with a martial weapon or Unarmed Strike",
            FullDescription = "You learn one Fighting Style option of your choice from the fighter class. If you already have a fighting style, you can replace it with another one.\n\nYou can replace this feat's fighting style with another one when you gain a level."
        },
        new Feat {
            Name = "Great Weapon Master",
            ShortDescription = "You've learned to put the weight of a weapon to your advantage.",
            Prerequisites = "Proficiency with a martial weapon",
            FullDescription = "On your turn, when you score a critical hit with a melee weapon or reduce a creature to 0 hit points with one, you can make one melee weapon attack as a bonus action.\n\nBefore you make a melee attack with a heavy weapon that you are proficient with, you can choose to take a -5 penalty to the attack roll. If the attack hits, you add +10 to the attack's damage roll."
        },
        new Feat {
            Name = "Healer",
            ShortDescription = "You are an able physician.",
            Prerequisites = "Proficiency with a healer's kit",
            FullDescription = "When you use a healer's kit to stabilize a dying creature, that creature also regains 1 hit point.\n\nAs an action, you can spend one use of a healer's kit to tend to a creature and restore 1d6 + 4 hit points to it, plus additional hit points equal to the creature's maximum number of Hit Dice. The creature can't regain hit points from this feat again until it finishes a short or long rest."
        },
        new Feat {
            Name = "Heavy Armor Master",
            ShortDescription = "You can use your armor to deflect blows.",
            Prerequisites = "Proficiency with heavy armor",
            FullDescription = "Increase your Strength score by 1, to a maximum of 20.\n\nWhile you are wearing heavy armor, bludgeoning, piercing, and slashing damage that you take from nonmagical weapons is reduced by 3."
        },
        new Feat {
            Name = "Inspiring Leader",
            ShortDescription = "You can inspire others through stirring words.",
            Prerequisites = "Charisma 13 or higher",
            FullDescription = "You can spend 10 minutes inspiring your companions. Choose up to six friendly creatures (including yourself) within 30 feet of you who can hear and understand you. Each creature can gain temporary hit points equal to your level + your Charisma modifier. A creature can't gain temporary hit points from this feat again until it has finished a short or long rest."
        },
        new Feat {
            Name = "Lucky",
            ShortDescription = "You have inexplicable luck that seems to kick in at just the right moment.",
            Prerequisites = "None",
            FullDescription = "You have 3 luck points. Whenever you make an attack roll, an ability check, or a saving throw, you can spend one luck point to roll an additional d20. You can choose to spend one of your luck points after you roll the die, but before the outcome is determined.\n\nYou regain your expended luck points when you finish a long rest."
        },
        new Feat {
            Name = "Mobile",
            ShortDescription = "You are exceptionally speedy and agile.",
            Prerequisites = "None",
            FullDescription = "Your speed increases by 10 feet.\n\nWhen you use the Dash action, difficult terrain doesn't cost you extra movement on that turn.\n\nWhen you make a melee attack against a creature, you don't provoke opportunity attacks from that creature for the rest of the turn, whether you hit or not."
        },
        new Feat {
            Name = "Piercer",
            ShortDescription = "You have a knack for finding the weak points in your opponents.",
            Prerequisites = "Proficiency with a weapon that has the thrown property or is a ranged weapon",
            HasDynamicStatChoice = true,
            FullDescription = "Increase your Strength or Dexterity score by 1, to a maximum of 20.\n\nOnce per turn, when you hit a creature with an attack that deals piercing damage, you can reroll one of the attack's damage dice and use the new roll.\n\nWhen you score a critical hit that deals piercing damage to a creature, you can roll one additional damage die when determining the extra piercing damage the target takes."
        },
        new Feat {
            Name = "Polearm Master",
            ShortDescription = "You can keep your enemies at bay with reach weapons.",
            Prerequisites = "Proficiency with a glaive, halberd, quarterstaff, or spear",
            FullDescription = "When you take the Attack action and attack with a glaive, halberd, quarterstaff, or spear, you can use a bonus action to make a melee attack with the opposite end of the weapon. This attack uses the same ability modifier as the primary attack. The weapon's damage die for this attack is a d4, and it deals bludgeoning damage.\n\nWhile you are wielding a glaive, halberd, pike, quarterstaff, or spear, other creatures provoke an opportunity attack from you when they enter the reach you have with that weapon."
        },
        new Feat {
            Name = "Prodigy",
            ShortDescription = "You have a knack for learning new things.",
            Prerequisites = "Half-elf, half-orc, or human (or Custom Lineage)",
            HasDynamicStatChoice = true,
            FullDescription = "Increase one ability score of your choice by 1, to a maximum of 20.\n\nYou gain proficiency in one skill of your choice, one tool of your choice, and fluency in one language of your choice.\n\nChoose one skill in which you have proficiency. You gain expertise with that skill, which means your proficiency bonus is doubled for any ability check you make with it."
        },
        new Feat {
            Name = "Resilient",
            ShortDescription = "Choose one ability score. You gain proficiency in saving throws using that ability.",
            Prerequisites = "None",
            HasDynamicStatChoice = true,
            FullDescription = "Increase the chosen ability score by 1, to a maximum of 20.\n\nYou gain proficiency in saving throws using the chosen ability."
        },
        new Feat {
            Name = "Sentinel",
            ShortDescription = "You have mastered techniques to take advantage of every drop in any enemy's guard.",
            Prerequisites = "None",
            FullDescription = "When you hit a creature with an opportunity attack, the creature's speed becomes 0 for the rest of the turn.\n\nCreatures provoke opportunity attacks from you even if they take the Disengage action before leaving your reach.\n\nWhen a creature within 5 feet of you makes an attack against a target other than you (and that target doesn't have this feat), you can use your reaction to make a melee weapon attack against the attacking creature."
        },
        new Feat {
            Name = "Shadow Touched",
            ShortDescription = "You have been touched by the Shadowfell.",
            Prerequisites = "None",
            HasDynamicStatChoice = true,
            FullDescription = "Increase your Intelligence, Wisdom, or Charisma score by 1, to a maximum of 20.\n\nYou learn the invisibility spell and one 1st-level spell of your choice. The 1st-level spell must be from the illusion or necromancy school of magic. You can cast each of these spells without expending a spell slot once per long rest."
        },
        new Feat {
            Name = "Sharpshooter",
            ShortDescription = "You have mastered ranged weapons and can make shots that others find impossible.",
            Prerequisites = "None",
            FullDescription = "Attacking at long range doesn't impose disadvantage on your ranged weapon attack rolls.\n\nYour ranged attacks ignore half cover and three-quarters cover.\n\nBefore you make an attack with a ranged weapon that you are proficient with, you can choose to take a -5 penalty to the attack roll. If the attack hits, you add +10 to the attack's damage roll."
        },
        new Feat {
            Name = "Shield Master",
            ShortDescription = "You use shields not just for protection but also for offense.",
            Prerequisites = "Proficiency with shields",
            FullDescription = "If you take the Attack action on your turn, you can use a bonus action to try to shove a creature within 5 feet of you with your shield.\n\nIf you aren't incapacitated, you can add your shield's AC bonus to any Dexterity saving throw you make against a spell or other harmful effect that targets only you.\n\nIf you are subjected to an effect that allows you to make a Dexterity saving throw to take only half damage, you can use your reaction to take no damage if you succeed on the saving throw."
        },
        new Feat {
            Name = "Skill Expert",
            ShortDescription = "You have honed your proficiency with particular skills.",
            Prerequisites = "None",
            HasDynamicStatChoice = true,
            FullDescription = "Increase one ability score of your choice by 1, to a maximum of 20.\n\nYou gain proficiency in one skill of your choice.\n\nChoose one skill in which you have proficiency. You gain expertise with that skill, which means your proficiency bonus is doubled for any ability check you make with it."
        },
        new Feat {
            Name = "Skulker",
            ShortDescription = "You are expert at slinking through shadows.",
            Prerequisites = "Dexterity 13 or higher + proficiency in Stealth",
            FullDescription = "You can try to hide when you are lightly obscured from the creature from which you are hiding.\n\nWhen you are hidden from a creature and miss it with a ranged weapon attack, making the attack doesn't reveal your position.\n\nDim light doesn't impose disadvantage on your Wisdom (Perception) checks relying on sight."
        },
        new Feat {
            Name = "Slasher",
            ShortDescription = "You know how to cut deep and make it hurt.",
            Prerequisites = "Proficiency with a weapon that deals slashing damage",
            HasDynamicStatChoice = true,
            FullDescription = "Increase your Strength or Dexterity score by 1, to a maximum of 20.\n\nOnce per turn, when you hit a creature with an attack that deals slashing damage, you can reduce the target's speed by 10 feet until the start of your next turn.\n\nWhen you score a critical hit that deals slashing damage to a creature, you can roll one additional damage die when determining the extra slashing damage the target takes."
        },
        new Feat {
            Name = "Spell Sniper",
            ShortDescription = "You have learned techniques to enhance your attacks with certain spells.",
            Prerequisites = "Spellcasting feature",
            FullDescription = "When you cast a spell that requires you to make an attack roll, the spell's range is doubled.\n\nYour ranged spell attacks ignore half cover and three-quarters cover.\n\nYou learn one cantrip that requires an attack roll. Choose the cantrip from the bard, cleric, druid, sorcerer, warlock, or wizard spell list."
        },
        new Feat {
            Name = "Telekinetic",
            ShortDescription = "You have learned to move things with your mind.",
            Prerequisites = "None",
            FullDescription = "Increase your Intelligence, Wisdom, or Charisma score by 1, to a maximum of 20.\n\nYou learn the mage hand spell. You can cast it without verbal or somatic components, and you can make the spectral hand invisible. The hand lasts until it is dismissed as a bonus action.\n\nAs a bonus action, you can try to shove one creature you can see within 30 feet of you. When you do so, the target must succeed on a Strength saving throw (DC 8 + your proficiency bonus + the ability modifier of the score increased by this feat) or be moved 5 feet toward or away from you."
        },
        new Feat {
            Name = "Tough",
            ShortDescription = "Your hit point maximum increases by an amount equal to twice your level when you gain this feat.",
            Prerequisites = "None",
            FullDescription = "Your hit point maximum increases by an amount equal to twice your level when you gain this feat. Thereafter, it increases by 2 hit points each time you gain a level."
        },
        new Feat {
            Name = "War Caster",
            ShortDescription = "You've practiced casting spells in the midst of combat.",
            Prerequisites = "Spellcasting feature",
            FullDescription = "You have advantage on Constitution saving throws that you make to maintain your concentration on a spell when you take damage.\n\nYou can perform the somatic components of spells even when you have weapons or a shield in one or both hands.\n\nWhen a hostile creature's movement provokes an opportunity attack from you, you can use your reaction to cast a spell at the creature, rather than making an opportunity attack. The spell must have a casting time of 1 action and must target only that creature."
        },
        new Feat {
            Name = "Weapon Master",
            ShortDescription = "You have practiced extensively with a variety of weapons.",
            Prerequisites = "None",
            HasDynamicStatChoice = true,
            FullDescription = "Increase your Strength or Dexterity score by 1, to a maximum of 20.\n\nYou gain proficiency with four weapons of your choice. Each weapon must be a simple or a martial weapon."
        },
        new Feat
        {
            Name = "Magic Initiate",
            ShortDescription = "You learn two cantrips and one 1st-level spell from another class's spell list.",
            Prerequisites = "None",
            FullDescription = "Choose a class: Bard, Cleric, Druid, Sorcerer, Warlock, or Wizard.\n\n" +
                              "• You learn two cantrips of your choice from that class's spell list.\n" +
                              "• You learn one 1st-level spell from that same list. You can cast this spell once per long rest without expending a spell slot.\n\n" +
                              "Your spellcasting ability for these spells is the same as the class you chose."
        },

        new Feat
        {
            Name = "Artificer Initiate",
            ShortDescription = "You gain rudimentary knowledge of arcane magic through study and experimentation.",
            Prerequisites = "None",
            FullDescription = "You learn one cantrip of your choice from the artificer spell list.\n\n" +
                              "You learn one 1st-level spell of your choice from the artificer spell list. You can cast this spell once per long rest without expending a spell slot.\n\n" +
                              "You gain proficiency with one type of artisan's tools of your choice."
        },

        new Feat
        {
            Name = "Ritual Caster",
            ShortDescription = "You can cast spells as rituals.",
            Prerequisites = "Intelligence or Wisdom 13 or higher",
            FullDescription = "You have learned a number of spells that you can cast as rituals. " +
                              "These spells are written in a ritual book, which you must have in hand while casting one of them.\n\n" +
                              "When you choose this feat, you acquire a ritual book holding two 1st-level spells of your choice. " +
                              "You must have the spells in your ritual book to cast them as rituals.\n\n" +
                              "If you come across a spell in written form (such as a magical spell scroll or a wizard's spellbook), " +
                              "you might be able to add it to your ritual book."
        },

        new Feat
        {
            Name = "Gift of the Metallic Dragon",
            ShortDescription = "You have been blessed by a metallic dragon.",
            Prerequisites = "None",
            FullDescription = "You learn the *cure wounds* spell. You can cast it once per long rest without expending a spell slot.\n\n" +
                              "You can also cast *detect magic* and *cure wounds* using spell slots you have of the appropriate level.\n\n" +
                              "Increase your Charisma, Intelligence, or Wisdom score by 1, to a maximum of 20."
        },

        new Feat
        {
            Name = "Gift of the Chromatic Dragon",
            ShortDescription = "You have been blessed by a chromatic dragon.",
            Prerequisites = "None",
            FullDescription = "You learn the *chromatic orb* spell. You can cast it once per long rest without expending a spell slot.\n\n" +
                              "You can also cast *chromatic orb* using spell slots you have of 1st level or higher.\n\n" +
                              "Increase your Charisma, Intelligence, or Wisdom score by 1, to a maximum of 20."
        },

        new Feat
        {
            Name = "Gift of the Gem Dragon",
            ShortDescription = "You have been blessed by a gem dragon.",
            Prerequisites = "None",
            FullDescription = "You learn the *detect thoughts* spell. You can cast it once per long rest without expending a spell slot.\n\n" +
                              "You can also cast *detect thoughts* using spell slots you have of 2nd level or higher.\n\n" +
                              "Increase your Charisma, Intelligence, or Wisdom score by 1, to a maximum of 20."
        },
        new Feat
        {
            Name = "Telepathic",
            ShortDescription = "You can speak telepathically and probe minds.",
            Prerequisites = "None",
            HasDynamicStatChoice = true,
            FullDescription = "Increase your Intelligence, Wisdom, or Charisma score by 1, to a maximum of 20.\n\n" +
                              "You can speak telepathically to any creature you can see within 60 feet of you. " +
                              "The creature doesn't need to share a language with you, but it must be able to understand at least one language. " +
                              "This telepathy doesn't give the creature the ability to respond telepathically.\n\n" +
                              "As a bonus action, you can try to telepathically contact one creature you can see within 60 feet of you. " +
                              "The target must succeed on a Wisdom saving throw (DC 8 + your proficiency bonus + the ability modifier of the score increased by this feat) " +
                              "or be affected as if by the *detect thoughts* spell for 1 minute. The target can repeat the saving throw at the end of each of its turns."
        }
    };
            // === SORT ALPHABETICALLY ===
            AllFeats = AllFeats.OrderBy(f => f.Name).ToList();
        }


        // ==================== SPELL DATABASE (original 5e via dnd5e.wikidot.com) ====================
        // Full text, school, casting time, range, components, duration, roll type, damage dice,
        // upcast rules. Loaded from Data/spells.json — regenerate with tools/fetch_wikidot_spells.py.

        /// <summary>Every SRD spell (cantrips–9th). Prefer this over level-specific lists.</summary>
        public static System.Collections.Generic.IReadOnlyList<Spell> AllSpells => Nemo.SpellCatalog.All;

        /// <summary>Cantrips from the spell catalog.</summary>
        public static System.Collections.Generic.List<Spell> AllCantrips => Nemo.SpellCatalog.Cantrips.ToList();

        /// <summary>1st-level spells (as LeveledSpell for existing UI).</summary>
        public static System.Collections.Generic.List<LeveledSpell> All1stLevelSpells =>
            Nemo.SpellCatalog.Level1.Select(AsLeveled).ToList();

        /// <summary>Spells of a given level (0–9).</summary>
        public static System.Collections.Generic.List<Spell> GetSpellsAtLevel(int level) =>
            Nemo.SpellCatalog.GetByLevel(level).ToList();

        public static Spell? FindSpell(string name) => Nemo.SpellCatalog.Find(name);

        private static LeveledSpell AsLeveled(Spell s) => new()
        {
            Name = s.Name,
            Level = s.Level,
            School = s.School,
            CastingTime = s.CastingTime,
            Range = s.Range,
            Components = s.Components,
            Material = s.Material,
            Duration = s.Duration,
            IsConcentration = s.IsConcentration,
            IsRitual = s.IsRitual,
            DamageType = s.DamageType,
            DamageDice = s.DamageDice,
            RollType = s.RollType,
            SaveAbility = s.SaveAbility,
            DcSuccess = s.DcSuccess,
            AttackType = s.AttackType,
            Description = s.Description,
            FullDescription = s.FullDescription,
            HigherLevel = s.HigherLevel,
            CanUpcast = s.CanUpcast,
            UpcastIncrement = s.UpcastIncrement,
            DamageAtSlotLevel = s.DamageAtSlotLevel != null
                ? new System.Collections.Generic.Dictionary<string, string>(s.DamageAtSlotLevel)
                : new System.Collections.Generic.Dictionary<string, string>(),
            DamageAtCharacterLevel = s.DamageAtCharacterLevel != null
                ? new System.Collections.Generic.Dictionary<string, string>(s.DamageAtCharacterLevel)
                : new System.Collections.Generic.Dictionary<string, string>(),
            HealAtSlotLevel = s.HealAtSlotLevel != null
                ? new System.Collections.Generic.Dictionary<string, string>(s.HealAtSlotLevel)
                : new System.Collections.Generic.Dictionary<string, string>(),
            AreaOfEffect = s.AreaOfEffect,
            Classes = s.Classes != null ? new System.Collections.Generic.List<string>(s.Classes) : new System.Collections.Generic.List<string>(),
            Source = s.Source
        };

    }
}

public class Armor
{
    public string Name { get; set; }
    public string Type { get; set; }           // Light / Medium / Heavy
    public string AC { get; set; }
    public string StrengthRequirement { get; set; } = "—";
    public string StealthDisadvantage { get; set; } = "No";
    public string StealthDisplay => StealthDisadvantage == "Yes" ? "Disadvantage" : "";
}

// ==================== CHARACTER DATA MODEL ====================

public class Character
{
    // === Identity ===
    public string Name { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string AvatarBase64 { get; set; } = "";

    // === Core Choices ===
    public string Race { get; set; } = "";
    public string Subrace { get; set; } = "";
    public string Background { get; set; } = "";
    public string Class { get; set; } = "";
    public string Subclass { get; set; } = "";
    /// <summary>
    /// Total character level (sum of all class levels). For single-class, matches levels in <see cref="Class"/>.
    /// </summary>
    public int Level { get; set; } = 1;
    /// <summary>
    /// Multiclass support: each entry is one class with levels and optional subclass.
    /// When empty, treat as single-class using <see cref="Class"/> / <see cref="Subclass"/> / <see cref="Level"/>.
    /// </summary>
    public List<Nemo.ClassLevelEntry> ClassLevels { get; set; } = new();
    /// <summary>
    /// ASI vs Feat decisions for each milestone earned from class levels
    /// (e.g. Fighter 4, Wizard 4).
    /// </summary>
    public List<Nemo.AsiOrFeatDecision> AsiOrFeatDecisions { get; set; } = new();
    /// <summary>Fighter/Paladin/Ranger fighting style picks (names from ClassFeatureOptionData).</summary>
    public List<string> FightingStyles { get; set; } = new();
    /// <summary>Warlock eldritch invocations known.</summary>
    public List<string> EldritchInvocations { get; set; } = new();
    /// <summary>Sorcerer metamagic options known.</summary>
    public List<string> MetamagicOptions { get; set; } = new();
    /// <summary>Warlock Pact Boon at 3rd level: Pact of the Chain / Blade / Tome.</summary>
    public string WarlockPactBoon { get; set; } = "";
    public string SelectedFeat { get; set; } = "";
    public int Speed { get; set; } = 30;

    /// <summary>How HP is calculated for levels after 1st (fixed average vs rolled).</summary>
    public Nemo.HpGainMethod HpGainMethod { get; set; } = Nemo.HpGainMethod.FixedAverage;
    /// <summary>
    /// Raw hit-die rolls (before Con) for each level after 1st, in order gained.
    /// Used when <see cref="HpGainMethod"/> is Rolled.
    /// </summary>
    public List<int> HitPointRolls { get; set; } = new();

    // === Ability Scores (Detailed) ===
    public AbilityScoreBlock AbilityScores { get; set; } = new AbilityScoreBlock();

    // === Proficiencies ===
    public List<SkillEntry> Skills { get; set; } = new();
    public List<SavingThrow> SavingThrows { get; set; } = new();

    // === Equipment ===
    public List<string> Equipment { get; set; } = new();
    public string BackgroundEquipment { get; set; } = "";

    // === Spells ===
    public List<string> Cantrips { get; set; } = new();
    /// <summary>
    /// Multiclass cantrip ownership: cantrip name → class key (e.g. "Fire Bolt" → "Wizard").
    /// Used so dual-list cantrips count against the correct class budget.
    /// </summary>
    public Dictionary<string, string> CantripClassAssignments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Level1Spells { get; set; } = new();

    // === Derived / Calculated Values (Important for PDF & other apps) ===
    public int ProficiencyBonus { get; set; } = 2;
    public int Initiative { get; set; }
    public int ArmorClass { get; set; }
    public string EquippedACDisplay { get; set; } = "";
    public int HitPoints { get; set; }
    public string SpellcastingAbility { get; set; } = "";
    public int SpellSaveDC { get; set; }
    public int SpellAttackBonus { get; set; }

    // === Special Choices ===
    public string HighElfCantrip { get; set; } = "";
    public string RaceGrantedSkill { get; set; } = "";
    public List<string> BackgroundLanguages { get; set; } = new();
}

// Helper class for structured ability scores
public class AbilityScoreBlock
{
    public AbilityScore Strength { get; set; } = new();
    public AbilityScore Dexterity { get; set; } = new();
    public AbilityScore Constitution { get; set; } = new();
    public AbilityScore Intelligence { get; set; } = new();
    public AbilityScore Wisdom { get; set; } = new();
    public AbilityScore Charisma { get; set; } = new();
}

public class AbilityScore
{
    public int Base { get; set; }
    public int Racial { get; set; }
    public int Feat { get; set; }
    public int Final { get; set; }
    public int Modifier { get; set; }
}

// Helper class for skills with calculated bonuses
public class SkillEntry
{
    public string Name { get; set; } = "";
    public string Ability { get; set; } = "";
    public bool IsProficient { get; set; }
    /// <summary>Double proficiency (Rogue/Bard Expertise). Requires <see cref="IsProficient"/>.</summary>
    public bool IsExpertise { get; set; }
    public int Bonus { get; set; }
}

public class RaceData
{
    public Dictionary<string, int> AbilityBonuses { get; set; } = new();
    public List<string> Traits { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public List<string> SkillProficiencies { get; set; } = new();
    public bool HasInnateSpellcasting { get; set; } = false;   // ← NEW
    public int Speed { get; set; } = 30;
}

public class SubraceData
{
    public string Name { get; set; } = "";
    public Dictionary<string, int> AbilityBonus { get; set; } = new();
    /// <summary>When true, subrace ability bonuses replace the base race ASI instead of stacking (e.g. Feral Tiefling).</summary>
    public bool ReplacesAbilityBonuses { get; set; } = false;
    public List<string> Traits { get; set; } = new();
    public bool HasInnateSpellcasting { get; set; } = false;
    public int? Speed { get; set; } = null;
}


public class ClassData
{
    public string HitDie { get; set; } = "";
    public string HP1stLevel { get; set; } = "";
    public string Proficiencies { get; set; } = "";
    public bool Spellcasting { get; set; }
    public string SpellAbility { get; set; } = "";
    public int CantripsKnown { get; set; }
    public string SpellsPrepared { get; set; } = "";
    public int SpellsKnown { get; set; }
    public List<string> Subclasses { get; set; } = new();
    public List<string> SkillChoices { get; set; } = new();
    public int SkillChoiceCount { get; set; }
    public string Description { get; set; } = "";
    public List<string> SavingThrowProficiencies { get; set; } = new();
    public List<string> ArmorProficiencies { get; set; } = new();
    public List<string> WeaponProficiencies { get; set; } = new();

    /// <summary>Numeric hit die size (6, 8, 10, 12) derived from <see cref="HitDie"/> when possible.</summary>
    public int HitDieSize
    {
        get
        {
            if (string.IsNullOrWhiteSpace(HitDie)) return 8;
            int d = HitDie.IndexOf('d');
            if (d >= 0 && int.TryParse(HitDie.AsSpan(d + 1), out int size) && size > 0)
                return size;
            return 8;
        }
    }
}



public class EquipmentChoice
{
    public string Label { get; set; } = "";
    public List<EquipmentOption> Options { get; set; } = new();
}

public class EquipmentOption
{
    public string Text { get; set; } = "";
    public bool IsProficientRequired { get; set; } = false;
    public bool RequiresMartial { get; set; } = false;
    public bool IsAnyWeapon { get; set; } = false;
    public string WeaponType { get; set; } = "";
    public int RequiredCount { get; set; } = 1;
    public string ExtraItem { get; set; } = "";   // ← NEW: for "Shield", etc.
}

public class Weapon : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string Damage { get; set; } = "";
    public string Type { get; set; } = "";
    public string Range { get; set; } = "";
    public string Properties { get; set; } = "";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => $"{Name} ({Damage} {Type})";
}

public class ClericSubclassData
{
    public string Name { get; set; } = "";
    public List<string> AdditionalCantrips { get; set; } = new();
    public List<string> DomainSpells { get; set; } = new();           // 1st-level domain spells
    public List<string> ArmorProficiencies { get; set; } = new();
    public List<string> WeaponProficiencies { get; set; } = new();
    public List<string> UniqueAbilities { get; set; } = new();
}

public class WarlockSubclassData
{
    public string Name { get; set; } = "";
    public List<string> AdditionalCantrips { get; set; } = new();
    public List<string> DomainSpells { get; set; } = new();   // Patron spells at level 1
    public List<string> ArmorProficiencies { get; set; } = new();
    public List<string> WeaponProficiencies { get; set; } = new();
    public List<string> UniqueAbilities { get; set; } = new();
}

public class SorcererSubclassData
{
    public string Name { get; set; } = "";
    public List<string> AdditionalSpells { get; set; } = new();   // level 1 origin spells
    public List<string> UniqueAbilities { get; set; } = new();
}

/// <summary>
/// A class feature gained at a specific level (base class or subclass progression).
/// </summary>
public class ClassFeature
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>e.g. "2 / long rest", "1 / short rest", "Passive", "At will"</summary>
    public string Uses { get; set; } = "";
    /// <summary>Class level at which this feature is gained (1–20).</summary>
    public int Level { get; set; } = 1;
    /// <summary>True for optional/Tasha's features (Harness Divine Power, Martial Versatility, etc.).</summary>
    public bool IsOptional { get; set; } = false;
    /// <summary>When true, concrete benefits come from the chosen subclass (domain, oath, etc.).</summary>
    public bool IsSubclassFeature { get; set; } = false;
}

public class Feat : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string FullDescription { get; set; } = "";
    public string Prerequisites { get; set; } = "None";
    public bool HasDynamicStatChoice { get; set; } = false;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));

                if (Application.Current.MainWindow is global::Nemo.MainWindow mainWindow)
                {
                    if (value) // Trying to select the feat
                    {
                        // Count other already-selected feats (this one is already set true above)
                        int others = Nemo.GameData.AllFeats?.Count(f => f != null && f.IsSelected && !ReferenceEquals(f, this)) ?? 0;
                        int max = mainWindow.GetMaxFeatSelections();
                        if (others >= max)
                        {
                            _isSelected = false;
                            OnPropertyChanged(nameof(IsSelected));
                            mainWindow.dgFeats?.Items.Refresh();
                            MessageBox.Show(
                                $"You can only select {max} feat(s).\n\n" +
                                "Origin feats (Variant Human / Custom Lineage) and ASI→Feat choices on the Level & Multiclass tab increase this limit.",
                                "Feat Limit Reached",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else if (mainWindow.MeetsPrerequisite(this))
                        {
                            mainWindow.ApplyFeatBonus(this);
                        }
                        else
                        {
                            _isSelected = false;                    // Revert the model
                            OnPropertyChanged(nameof(IsSelected));

                            // === FORCE IMMEDIATE UI REFRESH ===
                            mainWindow.dgFeats.Items.Refresh();     // ← This is the key fix
                            mainWindow.dgFeats.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                            MessageBox.Show($"You do not meet the prerequisite for **{Name}**.\n\n" +
                                            $"Prerequisite: {Prerequisites}",
                                            "Prerequisite Not Met",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Warning);
                        }
                    }
                    else // Deselecting
                    {
                        mainWindow.RemoveFeatBonus(this);
                    }

                    mainWindow.UpdateFeatSelectionLimitLabel();
                    mainWindow.UpdateStatDisplays();
                    mainWindow.UpdateInitiative();
                }
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class Spell
{
    public string Name { get; set; } = "";
    /// <summary>0 = cantrip, 1–9 = leveled spell.</summary>
    public int Level { get; set; } = 0;
    public string School { get; set; } = "";
    public string CastingTime { get; set; } = "";
    public string Range { get; set; } = "";
    public string Components { get; set; } = "";
    /// <summary>Material component detail (when Components includes M).</summary>
    public string Material { get; set; } = "";
    public string Duration { get; set; } = "";
    public bool IsConcentration { get; set; } = false;
    public bool IsRitual { get; set; } = false;
    public string DamageType { get; set; } = "";
    /// <summary>Base damage/healing dice at the spell's minimum level (e.g. "8d6", "1d8 + MOD").</summary>
    public string DamageDice { get; set; } = "";
    /// <summary>e.g. "Dex Save", "Ranged Spell Attack", "Healing", "None".</summary>
    public string RollType { get; set; } = "";
    /// <summary>Save ability when RollType is a save (Str/Dex/Con/Int/Wis/Cha).</summary>
    public string SaveAbility { get; set; } = "";
    /// <summary>What happens on a successful save (e.g. "half", "none").</summary>
    public string DcSuccess { get; set; } = "";
    /// <summary>melee / ranged when the spell uses a spell attack.</summary>
    public string AttackType { get; set; } = "";
    /// <summary>Short blurb for list UIs.</summary>
    public string Description { get; set; } = "";
    /// <summary>Full exact spell description text (all paragraphs).</summary>
    public string FullDescription { get; set; } = "";
    /// <summary>Official "At Higher Levels" text.</summary>
    public string HigherLevel { get; set; } = "";
    /// <summary>True when the spell has an At Higher Levels entry.</summary>
    public bool CanUpcast { get; set; } = false;
    /// <summary>
    /// Parsed upcast increment, e.g. "1d6" or "1d8 for each slot level above 1st" summary.
    /// Prefer <see cref="HigherLevel"/> for the full wording.
    /// </summary>
    public string UpcastIncrement { get; set; } = "";
    /// <summary>Slot level → damage dice (e.g. "3" → "8d6").</summary>
    public Dictionary<string, string> DamageAtSlotLevel { get; set; } = new();
    /// <summary>Character level → cantrip damage dice (e.g. "5" → "2d10").</summary>
    public Dictionary<string, string> DamageAtCharacterLevel { get; set; } = new();
    /// <summary>Slot level → healing expression (e.g. "1" → "1d8 + MOD").</summary>
    public Dictionary<string, string> HealAtSlotLevel { get; set; } = new();
    public string AreaOfEffect { get; set; } = "";
    public List<string> Classes { get; set; } = new();
    public string Source { get; set; } = "";

    /// <summary>Display label: "Cantrip" or "1st-level", etc.</summary>
    public string LevelLabel => Level switch
    {
        0 => "Cantrip",
        1 => "1st-level",
        2 => "2nd-level",
        3 => "3rd-level",
        _ when Level >= 4 && Level <= 9 => $"{Level}th-level",
        _ => $"Level {Level}"
    };

    /// <summary>
    /// Multi-line detail block for previews (school, casting, range, damage, full text, upcast).
    /// </summary>
    public string FormatDetails(bool includeFullText = true)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"**{Name}** ({LevelLabel} {School})");
        sb.AppendLine($"Casting Time: {CastingTime}");
        sb.AppendLine($"Range: {Range}");
        sb.AppendLine($"Components: {Components}");
        string dur = Duration;
        if (IsConcentration && !dur.Contains("Concentration", StringComparison.OrdinalIgnoreCase))
            dur += " (Concentration)";
        if (IsRitual)
            dur += " [Ritual]";
        sb.AppendLine($"Duration: {dur}");

        if (!string.IsNullOrWhiteSpace(AreaOfEffect))
            sb.AppendLine($"Area: {AreaOfEffect}");

        if (!string.IsNullOrWhiteSpace(RollType) &&
            !RollType.Equals("None", StringComparison.OrdinalIgnoreCase))
            sb.AppendLine($"Roll: {RollType}");

        if (!string.IsNullOrWhiteSpace(DamageDice))
        {
            string kind = HealAtSlotLevel.Count > 0 ? "Healing" : "Damage";
            string line = $"{kind}: {DamageDice}";
            if (!string.IsNullOrWhiteSpace(DamageType) && kind == "Damage")
                line += $" {DamageType}";
            sb.AppendLine(line);
        }

        if (CanUpcast)
        {
            if (!string.IsNullOrWhiteSpace(UpcastIncrement))
                sb.AppendLine($"Upcast: +{UpcastIncrement.TrimStart('+')} per higher slot (see At Higher Levels)");
            else
                sb.AppendLine("Upcast: Yes (see At Higher Levels)");
        }

        if (Classes != null && Classes.Count > 0)
            sb.AppendLine($"Classes: {string.Join(", ", Classes)}");

        if (includeFullText)
        {
            sb.AppendLine();
            string body = !string.IsNullOrWhiteSpace(FullDescription) ? FullDescription : Description;
            sb.AppendLine(body);
            if (!string.IsNullOrWhiteSpace(HigherLevel))
            {
                sb.AppendLine();
                sb.AppendLine("At Higher Levels:");
                sb.AppendLine(HigherLevel);
            }
        }

        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Leveled spells (1st level and higher). Prefer using <see cref="Spell"/> with Level set;
/// kept for backward compatibility with existing UI code.
/// </summary>
public class LeveledSpell : Spell
{
    public LeveledSpell() { Level = 1; }
}

public class SavingThrow
{
    public string Name { get; set; } = "";
    public int Bonus { get; set; }
    public bool IsProficient { get; set; }
}