using System.Collections.Generic;

namespace Nemo
{
    /// <summary>
    /// Subclass feature progression for Ranger, Rogue, and Wizard.
    /// Keys are exact subclass display names from <see cref="GameData.AllSubclasses"/>.
    /// Reference: https://dnd5e.wikidot.com/
    /// </summary>
    public static partial class GameData
    {
        /// <summary>
        /// Subclass-only feature factory (always sets <see cref="ClassFeature.IsSubclassFeature"/>).
        /// Distinct from the base-class <c>F</c> helper in ClassProgressionData.
        /// </summary>
        private static ClassFeature SF(int level, string name, string description, string uses = "Passive") => new()
        {
            Level = level,
            Name = name,
            Description = description,
            Uses = uses,
            IsSubclassFeature = true
        };

        private static void AddRangerRogueWizard(Dictionary<string, List<ClassFeature>> d)
        {
            // ═══════════════════════════════════════════════════════════════
            // RANGER (levels 3, 7, 11, 15)
            // ═══════════════════════════════════════════════════════════════

            d["Beast Master"] = new List<ClassFeature>
            {
                SF(3, "Ranger's Companion",
                    "Gain a beast companion (Medium or smaller, CR 1/4 or lower). Add your proficiency bonus to its AC, attacks, damage, and proficient saves/skills. " +
                    "Its HP maximum is its normal maximum or 4 × your ranger level (whichever is higher). It acts on your initiative; command it with your action (Attack, Dash, Disengage, Help). " +
                    "Optional (Tasha's): replace this with Primal Companion — summon a Beast of the Land, Sea, or Sky and command it with a bonus action."),
                SF(7, "Exceptional Training",
                    "When your companion doesn't attack on your turn, you can use a bonus action to command it to Dash, Disengage, or Help. Its attacks count as magical for overcoming resistance/immunity to nonmagical attacks and damage."),
                SF(11, "Bestial Fury",
                    "When you command your beast to take the Attack action, it can make two attacks, or take the Multiattack action if it has that action."),
                SF(15, "Share Spells",
                    "When you cast a spell targeting yourself, you can also affect your beast companion if it is within 30 feet of you."),
            };

            d["Drakewarden"] = new List<ClassFeature>
            {
                SF(3, "Draconic Gift",
                    "You learn the Thaumaturgy cantrip (a ranger spell for you). You also learn to speak, read, and write Draconic or one other language of your choice."),
                SF(3, "Drake Companion",
                    "As an action, summon your bonded drake in an unoccupied space within 30 feet (once per long rest, or by expending a spell slot of 1st level or higher). " +
                    "Choose its Draconic Essence damage type (acid, cold, fire, lightning, or poison). It acts after you on your initiative; command it with a bonus action. " +
                    "It can bite and use Infused Strikes to add 1d6 essence damage to a nearby ally's weapon hit."),
                SF(7, "Bond of Fang and Scale",
                    "While summoned, your drake grows to Medium, gains a flying speed equal to its walking speed (you can ride it if Medium or smaller, but then it can't fly), " +
                    "its Bite deals +1d6 essence damage, and you gain resistance to its essence damage type."),
                SF(11, "Drake's Breath",
                    "As an action, you or your drake exhales a 30-foot cone (Dex save vs. your spell save DC). Creatures take 8d6 (10d6 at 15th level) acid, cold, fire, lightning, or poison damage (your choice) on a failed save, or half on a success. " +
                    "Once per long rest, or again by expending a spell slot of 3rd level or higher.",
                    "1/long rest (or 3rd+ slot)"),
                SF(15, "Perfected Bond",
                    "While summoned: the drake's Bite deals +2d6 essence damage total, it grows to Large and can fly while mounted, and when you or the drake take damage within 30 feet of each other " +
                    "you can use your reaction to gain resistance to that instance of damage (proficiency bonus times per long rest).",
                    "PB/long rest (Reflexive Resistance)"),
            };

            d["Fey Wanderer"] = new List<ClassFeature>
            {
                SF(3, "Dreadful Strikes",
                    "Once per turn when you hit with a weapon, deal an extra 1d4 psychic damage (1d6 at 11th level)."),
                SF(3, "Fey Wanderer Magic",
                    "You learn additional ranger spells that don't count against spells known: Charm Person (3rd), Misty Step (5th), Dispel Magic (9th), Dimension Door (13th), Mislead (17th)."),
                SF(3, "Otherworldly Glamour",
                    "Add your Wisdom modifier (minimum +1) to Charisma checks. Gain proficiency in Deception, Performance, or Persuasion."),
                SF(7, "Beguiling Twist",
                    "Advantage on saves against being charmed or frightened. When you or a creature you can see within 120 feet succeeds on a save against charm or fright, " +
                    "you can use your reaction to force a different creature within 120 feet to make a Wisdom save or be charmed or frightened by you for 1 minute."),
                SF(11, "Fey Reinforcements",
                    "You know Summon Fey (doesn't count against spells known; no material component). Cast it once without a slot per long rest. " +
                    "When you cast it, you can drop concentration and set the duration to 1 minute.",
                    "1 free cast/long rest"),
                SF(15, "Misty Wanderer",
                    "Cast Misty Step without a spell slot a number of times equal to your Wisdom modifier (minimum once) per long rest. " +
                    "Whenever you cast Misty Step, you can bring one willing creature within 5 feet along with you.",
                    "Wis mod/long rest (free Misty Step)"),
            };

            d["Gloom Stalker"] = new List<ClassFeature>
            {
                SF(3, "Gloom Stalker Magic",
                    "You learn additional ranger spells that don't count against spells known: Disguise Self (3rd), Rope Trick (5th), Fear (9th), Greater Invisibility (13th), Seeming (17th)."),
                SF(3, "Dread Ambusher",
                    "Add your Wisdom modifier to initiative. On your first turn of combat, your walking speed increases by 10 feet until the end of the turn, " +
                    "and if you take the Attack action you can make one additional weapon attack that deals +1d8 damage of the weapon's type on a hit."),
                SF(3, "Umbral Sight",
                    "Gain darkvision 60 feet (or +30 feet if you already have it). While in darkness, you are invisible to any creature that relies on darkvision to see you in that darkness."),
                SF(7, "Iron Mind",
                    "Gain proficiency in Wisdom saving throws. If you already have it, gain proficiency in Intelligence or Charisma saving throws (your choice)."),
                SF(11, "Stalker's Flurry",
                    "Once on each of your turns when you miss with a weapon attack, you can make another weapon attack as part of the same action."),
                SF(15, "Shadowy Dodge",
                    "When a creature attacks you without advantage, you can use your reaction (before knowing the result) to impose disadvantage on the attack roll.",
                    "Reaction"),
            };

            d["Horizon Walker"] = new List<ClassFeature>
            {
                SF(3, "Horizon Walker Magic",
                    "You learn additional ranger spells that don't count against spells known: Protection from Evil and Good (3rd), Misty Step (5th), Haste (9th), Banishment (13th), Teleportation Circle (17th)."),
                SF(3, "Detect Portal",
                    "As an action, detect the distance and direction to the closest planar portal within 1 mile. Recharges on a short or long rest.",
                    "1/short rest"),
                SF(3, "Planar Warrior",
                    "As a bonus action, choose a creature you can see within 30 feet. The next time you hit it with a weapon attack this turn, all damage from the attack becomes force damage " +
                    "and it takes an extra 1d8 force damage (2d8 at 11th level).",
                    "Bonus action"),
                SF(7, "Ethereal Step",
                    "As a bonus action, cast Etherealness without a slot; the spell ends at the end of the current turn. Recharges on a short or long rest.",
                    "1/short rest"),
                SF(11, "Distant Strike",
                    "When you take the Attack action, you can teleport up to 10 feet before each attack to an unoccupied space you can see. " +
                    "If you attack at least two different creatures with the action, you can make one additional attack against a third creature."),
                SF(15, "Spectral Defense",
                    "When you take damage from an attack, you can use your reaction to gain resistance to all of that attack's damage.",
                    "Reaction"),
            };

            d["Hunter"] = new List<ClassFeature>
            {
                SF(3, "Hunter's Prey",
                    "Choose one: Colossus Slayer (once per turn, +1d8 damage to a wounded creature you hit), Giant Killer (reaction attack when a Large+ creature within 5 feet hits or misses you), " +
                    "or Horde Breaker (once per turn, extra weapon attack against a different creature within 5 feet of the original target)."),
                SF(7, "Defensive Tactics",
                    "Choose one: Escape the Horde (opportunity attacks against you have disadvantage), Multiattack Defense (+4 AC against subsequent attacks from a creature that hit you this turn), " +
                    "or Steel Will (advantage on saves against being frightened)."),
                SF(11, "Multiattack",
                    "Choose one: Volley (action — ranged attack against any number of creatures within 10 feet of a point in range) " +
                    "or Whirlwind Attack (action — melee attack against any number of creatures within 5 feet)."),
                SF(15, "Superior Hunter's Defense",
                    "Choose one: Evasion (Dex save for half → no damage on success, half on failure), Stand Against the Tide (when a foe misses you with melee, reaction to force the attack against another creature), " +
                    "or Uncanny Dodge (reaction to halve an attack's damage against you)."),
            };

            d["Monster Slayer"] = new List<ClassFeature>
            {
                SF(3, "Monster Slayer Magic",
                    "You learn additional ranger spells that don't count against spells known: Protection from Evil and Good (3rd), Zone of Truth (5th), Magic Circle (9th), Banishment (13th), Hold Monster (17th)."),
                SF(3, "Hunter's Sense",
                    "As an action, choose a creature you can see within 60 feet and learn its damage immunities, resistances, and vulnerabilities (or sense that divination is blocked). " +
                    "Uses equal to your Wisdom modifier (minimum 1) per long rest.",
                    "Wis mod/long rest"),
                SF(3, "Slayer's Prey",
                    "As a bonus action, mark a creature you can see within 60 feet. The first time each turn you hit it with a weapon attack, it takes an extra 1d6 damage. " +
                    "Lasts until you finish a short or long rest or mark a different creature.",
                    "Bonus action"),
                SF(7, "Supernatural Defense",
                    "When the target of your Slayer's Prey forces you to make a saving throw, or when you make an ability check to escape its grapple, add 1d6 to your roll."),
                SF(11, "Magic-User's Nemesis",
                    "When you see a creature within 60 feet casting a spell or teleporting, you can use your reaction to force a Wisdom save against your spell save DC; on a failure the spell or teleport fails and is wasted. " +
                    "Recharges on a short or long rest.",
                    "1/short rest"),
                SF(15, "Slayer's Counter",
                    "If the target of your Slayer's Prey forces you to make a saving throw, you can use your reaction to make one weapon attack against it before the save. " +
                    "On a hit, you automatically succeed on the saving throw (in addition to the attack's normal effects).",
                    "Reaction"),
            };

            d["Swarmkeeper"] = new List<ClassFeature>
            {
                SF(3, "Gathered Swarm",
                    "A swarm of nature spirits occupies your space. Once on each of your turns when you hit a creature with an attack, choose one: the target takes 1d6 piercing damage; " +
                    "the target must succeed on a Strength save or be moved up to 15 feet horizontally; or you are moved 5 feet horizontally."),
                SF(3, "Swarmkeeper Magic",
                    "You learn Mage Hand (hand appears as your swarm) and additional ranger spells that don't count against spells known: Faerie Fire (3rd), Web (5th), Gaseous Form (9th), Arcane Eye (13th), Insect Plague (17th)."),
                SF(7, "Writhing Tide",
                    "As a bonus action, gain a flying speed of 10 feet and can hover for 1 minute (or until incapacitated). Uses equal to your proficiency bonus per long rest.",
                    "PB/long rest"),
                SF(11, "Mighty Swarm",
                    "Gathered Swarm improves: damage becomes 1d8; a failed save against being moved also knocks the target prone; when the swarm moves you, you gain half cover until the start of your next turn."),
                SF(15, "Swarming Dispersal",
                    "When you take damage, you can use your reaction to gain resistance to that damage, vanish into your swarm, and teleport to an unoccupied space you can see within 30 feet. " +
                    "Uses equal to your proficiency bonus per long rest.",
                    "PB/long rest"),
            };

            // ═══════════════════════════════════════════════════════════════
            // ROGUE (levels 3, 9, 13, 17)
            // ═══════════════════════════════════════════════════════════════

            d["Arcane Trickster"] = new List<ClassFeature>
            {
                SF(3, "Spellcasting",
                    "You cast wizard spells using Intelligence (third-caster progression). You know Mage Hand plus two other wizard cantrips, and three 1st-level wizard spells " +
                    "(at least two must be enchantment or illusion). You learn more spells and gain higher-level slots as you gain rogue levels.",
                    "Spell slots (long rest)"),
                SF(3, "Mage Hand Legerdemain",
                    "When you cast Mage Hand, you can make the hand invisible. It can stow or retrieve objects on other creatures, and use thieves' tools to pick locks and disarm traps at range. " +
                    "You can control the hand with your Cunning Action bonus action. Sleight of Hand vs. Perception to go unnoticed."),
                SF(9, "Magical Ambush",
                    "If you are hidden from a creature when you cast a spell on it, the creature has disadvantage on any saving throw it makes against the spell this turn."),
                SF(13, "Versatile Trickster",
                    "As a bonus action, designate a creature within 5 feet of your Mage Hand; you have advantage on attack rolls against that creature until the end of the turn.",
                    "Bonus action"),
                SF(17, "Spell Thief",
                    "When a creature casts a spell that targets you or includes you in its area, you can use your reaction to force a save with its spellcasting ability against your spell save DC. " +
                    "On a failure you negate the spell's effect on you and, if it is a 1st-level or higher spell you can cast, you steal knowledge of it for 8 hours (cast with your slots; the creature can't cast it). " +
                    "Once per long rest.",
                    "1/long rest"),
            };

            d["Assassin"] = new List<ClassFeature>
            {
                SF(3, "Bonus Proficiencies",
                    "You gain proficiency with the disguise kit and the poisoner's kit."),
                SF(3, "Assassinate",
                    "You have advantage on attack rolls against any creature that hasn't taken a turn in the combat yet. Any hit you score against a surprised creature is a critical hit."),
                SF(9, "Infiltration Expertise",
                    "You can create false identities. Spend 7 days and 25 gp to establish an identity's history, profession, and affiliations (not someone else's identity). " +
                    "While disguised as that identity, others believe you are that person until given an obvious reason not to."),
                SF(13, "Impostor",
                    "After studying a person's speech, writing, and behavior for at least 3 hours, you can unerringly mimic them. Casual observers can't tell the difference; " +
                    "against a wary creature you have advantage on Charisma (Deception) checks to avoid detection."),
                SF(17, "Death Strike",
                    "When you attack and hit a surprised creature, it must make a Constitution saving throw (DC 8 + Dexterity modifier + proficiency bonus). " +
                    "On a failed save, double the damage of your attack against the creature."),
            };

            d["Inquisitive"] = new List<ClassFeature>
            {
                SF(3, "Ear for Deceit",
                    "When you make a Wisdom (Insight) check to determine whether a creature is lying, treat a d20 roll of 7 or lower as an 8."),
                SF(3, "Eye for Detail",
                    "You can use a bonus action to make a Wisdom (Perception) check to spot a hidden creature or object, or an Intelligence (Investigation) check to uncover or decipher clues.",
                    "Bonus action"),
                SF(3, "Insightful Fighting",
                    "As a bonus action, make a Wisdom (Insight) check contested by a creature's Charisma (Deception). On a success, you can use Sneak Attack against that target even without advantage " +
                    "(but not if you have disadvantage) for 1 minute or until you use this feature on another target.",
                    "Bonus action"),
                SF(9, "Steady Eye",
                    "You have advantage on Wisdom (Perception) and Intelligence (Investigation) checks if you move no more than half your speed on the same turn."),
                SF(13, "Unerring Eye",
                    "As an action, sense the presence of illusions, shapechangers not in their original form, and other magic designed to deceive the senses within 30 feet " +
                    "(you sense deception magic but not true nature). Uses equal to your Wisdom modifier (minimum 1) per long rest.",
                    "Wis mod/long rest"),
                SF(17, "Eye for Weakness",
                    "While Insightful Fighting applies to a creature, your Sneak Attack damage against that creature increases by 3d6."),
            };

            d["Mastermind"] = new List<ClassFeature>
            {
                SF(3, "Master of Intrigue",
                    "Gain proficiency with the disguise kit, the forgery kit, and one gaming set of your choice, and learn two languages. " +
                    "You can unerringly mimic the speech patterns and accent of a creature you hear speak for at least 1 minute (if you know the language)."),
                SF(3, "Master of Tactics",
                    "You can use the Help action as a bonus action. When you Help an ally attack a creature, the target can be within 30 feet of you (instead of 5 feet) if it can see or hear you.",
                    "Bonus action"),
                SF(9, "Insightful Manipulator",
                    "If you spend at least 1 minute observing or interacting with a creature outside combat, learn whether it is your equal, superior, or inferior in two of: " +
                    "Intelligence, Wisdom, Charisma, or class levels. The DM may also reveal a piece of history or a personality trait."),
                SF(13, "Misdirection",
                    "When you are targeted by an attack while a creature within 5 feet of you is granting you cover against that attack, you can use your reaction to have the attack target that creature instead.",
                    "Reaction"),
                SF(17, "Soul of Deceit",
                    "Your thoughts can't be read by telepathy or other means unless you allow it; you can present false thoughts (Deception vs. Insight). " +
                    "Magic that detects lies indicates you are truthful if you choose, and you can't be compelled to tell the truth by magic."),
            };

            d["Phantom"] = new List<ClassFeature>
            {
                SF(3, "Whispers of the Dead",
                    "Whenever you finish a short or long rest, gain one skill or tool proficiency of your choice (ghostly knowledge). You lose it when you choose a different proficiency with this feature."),
                SF(3, "Wails from the Grave",
                    "Immediately after you deal Sneak Attack damage to a creature on your turn, you can target a second creature you can see within 30 feet of the first. " +
                    "It takes necrotic damage equal to half your Sneak Attack dice (rounded up). Uses equal to your proficiency bonus per long rest.",
                    "PB/long rest"),
                SF(9, "Tokens of the Departed",
                    "As a reaction when a creature you can see dies within 30 feet, create a Tiny soul trinket (max = proficiency bonus). While you have a soul trinket: advantage on death saves and Constitution saves; " +
                    "destroy one when you deal Sneak Attack to use Wails without expending a use; or destroy one as an action to ask the spirit one question."),
                SF(13, "Ghost Walk",
                    "As a bonus action, assume a spectral form for 10 minutes: flying speed 10 feet (hover), attack rolls against you have disadvantage, and you can move through creatures and objects as difficult terrain " +
                    "(1d10 force damage if you end your turn inside one). Recharge on long rest, or destroy a soul trinket as part of the bonus action.",
                    "1/long rest (or soul trinket)"),
                SF(17, "Death's Friend",
                    "When you use Wails from the Grave, you deal the necrotic damage to both the first and the second creature. At the end of a long rest, if you have no soul trinkets, one appears in your hand."),
            };

            d["Scout"] = new List<ClassFeature>
            {
                SF(3, "Skirmisher",
                    "When an enemy ends its turn within 5 feet of you, you can move up to half your speed as a reaction. This movement doesn't provoke opportunity attacks.",
                    "Reaction"),
                SF(3, "Survivalist",
                    "Gain proficiency in Nature and Survival (if you don't already have them). Your proficiency bonus is doubled for any ability check that uses either proficiency."),
                SF(9, "Superior Mobility",
                    "Your walking speed increases by 10 feet. If you have a climbing or swimming speed, this increase applies to those speeds as well."),
                SF(13, "Ambush Master",
                    "You have advantage on initiative rolls. The first creature you hit during the first round of combat is marked: attack rolls against that target have advantage until the start of your next turn."),
                SF(17, "Sudden Strike",
                    "If you take the Attack action on your turn, you can make one additional attack as a bonus action. This attack can benefit from Sneak Attack even if you already used it this turn, " +
                    "but you can't use Sneak Attack against the same target more than once in a turn.",
                    "Bonus action"),
            };

            d["Soulknife"] = new List<ClassFeature>
            {
                SF(3, "Psionic Power",
                    "You gain Psionic Energy dice (d6; number = 2 × proficiency bonus; die size increases at 5th/11th/17th). Regain all on a long rest; as a bonus action regain one die (once per short or long rest). " +
                    "Psi-Bolstered Knack: on a failed proficient skill/tool check, add a die (expend only on success). Psychic Whispers: telepathy with up to PB creatures for hours equal to a die roll (first use free after a long rest)."),
                SF(3, "Psychic Blades",
                    "When you take the Attack action, you can manifest a psychic blade (simple melee, finesse, thrown 60/—) dealing 1d6 + ability modifier psychic damage. " +
                    "After attacking with it, you can make a bonus-action attack with a second blade (1d4) if your other hand is free. Blades vanish after they hit or miss."),
                SF(9, "Soul Blades",
                    "Homing Strikes: on a miss with Psychic Blades, roll a Psionic Energy die and add it to the attack (expend only if you hit). " +
                    "Psychic Teleportation: as a bonus action, expend and roll a die, throw a blade up to 10 × the roll feet, and teleport to that space."),
                SF(13, "Psychic Veil",
                    "As an action, become invisible (with worn/carried gear) for 1 hour or until you dismiss it, deal damage, or force a save. Once per long rest, or again by expending a Psionic Energy die.",
                    "1/long rest (or 1 die)"),
                SF(17, "Rend Mind",
                    "When you deal Sneak Attack damage with Psychic Blades, force a Wisdom save (DC 8 + proficiency + Dexterity). On a failure the target is stunned for 1 minute (repeat save at end of each turn). " +
                    "Once per long rest, or again by expending three Psionic Energy dice.",
                    "1/long rest (or 3 dice)"),
            };

            d["Swashbuckler"] = new List<ClassFeature>
            {
                SF(3, "Fancy Footwork",
                    "If you make a melee attack against a creature on your turn, that creature can't make opportunity attacks against you for the rest of your turn."),
                SF(3, "Rakish Audacity",
                    "Add your Charisma modifier to initiative rolls. You can use Sneak Attack without advantage if you are within 5 feet of the target, no other creatures are within 5 feet of you, " +
                    "and you don't have disadvantage on the attack roll."),
                SF(9, "Panache",
                    "As an action, make a Charisma (Persuasion) check contested by a creature's Wisdom (Insight) (must hear you and share a language). " +
                    "If hostile and you succeed: disadvantage on attacks against others and can't opportunity-attack others for 1 minute (ends if an ally harms it or you are more than 60 feet apart). " +
                    "If not hostile: charmed for 1 minute as a friendly acquaintance."),
                SF(13, "Elegant Maneuver",
                    "As a bonus action, gain advantage on the next Dexterity (Acrobatics) or Strength (Athletics) check you make during the same turn.",
                    "Bonus action"),
                SF(17, "Master Duelist",
                    "If you miss with an attack roll, you can reroll it with advantage. Once per short or long rest.",
                    "1/short rest"),
            };

            d["Thief"] = new List<ClassFeature>
            {
                SF(3, "Fast Hands",
                    "You can use the bonus action granted by Cunning Action to make a Dexterity (Sleight of Hand) check, use your thieves' tools to disarm a trap or open a lock, or take the Use an Object action."),
                SF(3, "Second-Story Work",
                    "Climbing no longer costs you extra movement. When you make a running jump, the distance you cover increases by a number of feet equal to your Dexterity modifier."),
                SF(9, "Supreme Sneak",
                    "You have advantage on a Dexterity (Stealth) check if you move no more than half your speed on the same turn."),
                SF(13, "Use Magic Device",
                    "You ignore all class, race, and level requirements on the use of magic items."),
                SF(17, "Thief's Reflexes",
                    "You can take two turns during the first round of any combat. You take your first turn at your normal initiative and your second turn at your initiative minus 10. " +
                    "You can't use this feature when you are surprised."),
            };

            // ═══════════════════════════════════════════════════════════════
            // WIZARD (levels 2, 6, 10, 14)
            // ═══════════════════════════════════════════════════════════════

            d["Abjuration"] = new List<ClassFeature>
            {
                SF(2, "Abjuration Savant",
                    "The gold and time you must spend to copy an abjuration spell into your spellbook is halved."),
                SF(2, "Arcane Ward",
                    "When you cast an abjuration spell of 1st level or higher, create a magical ward on yourself (lasts until a long rest) with HP equal to twice your wizard level + Intelligence modifier. " +
                    "Damage is taken by the ward first. Casting abjuration spells of 1st+ level restores HP to the ward equal to twice the spell's level. Create the ward once per long rest.",
                    "1 ward/long rest"),
                SF(6, "Projected Ward",
                    "When a creature you can see within 30 feet takes damage, you can use your reaction to have your Arcane Ward absorb that damage instead.",
                    "Reaction"),
                SF(10, "Improved Abjuration",
                    "When you cast an abjuration spell that requires an ability check as part of casting it (such as Counterspell or Dispel Magic), you add your proficiency bonus to that check."),
                SF(14, "Spell Resistance",
                    "You have advantage on saving throws against spells, and resistance to damage from spells."),
            };

            d["Bladesinging"] = new List<ClassFeature>
            {
                SF(2, "Training in War and Song",
                    "Gain proficiency with light armor and one type of one-handed melee weapon of your choice. Gain proficiency in the Performance skill if you don't already have it."),
                SF(2, "Bladesong",
                    "As a bonus action (not in medium/heavy armor or using a shield), start Bladesong for 1 minute: +Intelligence modifier to AC (min +1), +10 feet walking speed, " +
                    "advantage on Dexterity (Acrobatics), and +Intelligence modifier (min +1) to Constitution saves to maintain concentration. " +
                    "Ends early if you are incapacitated, don restricted gear, or attack with two hands. Uses equal to proficiency bonus per long rest.",
                    "PB/long rest"),
                SF(6, "Extra Attack",
                    "You can attack twice, instead of once, whenever you take the Attack action on your turn. You can cast one of your cantrips in place of one of those attacks."),
                SF(10, "Song of Defense",
                    "While Bladesong is active, when you take damage you can use your reaction to expend a spell slot and reduce the damage by 5 × the slot's level.",
                    "Reaction"),
                SF(14, "Song of Victory",
                    "While Bladesong is active, add your Intelligence modifier (minimum +1) to the damage of your melee weapon attacks."),
            };

            d["Chronurgy"] = new List<ClassFeature>
            {
                SF(2, "Chronal Shift",
                    "As a reaction after you or a creature you can see within 30 feet makes an attack roll, ability check, or saving throw, force a reroll (after you know success or failure). " +
                    "The target must use the second roll. Twice per long rest.",
                    "2/long rest"),
                SF(2, "Temporal Awareness",
                    "You can add your Intelligence modifier to your initiative rolls."),
                SF(6, "Momentary Stasis",
                    "As an action, force a Large or smaller creature you can see within 60 feet to make a Constitution save. On a failure it is incapacitated with speed 0 until the end of your next turn or until it takes damage. " +
                    "Uses equal to your Intelligence modifier (minimum 1) per long rest.",
                    "Int mod/long rest"),
                SF(10, "Arcane Abeyance",
                    "When you cast a spell using a slot of 4th level or lower, you can freeze it in a Tiny gray bead (AC 15, 1 HP) for 1 hour. A creature holding the bead can use an action to release the spell " +
                    "(uses your attack bonus and save DC). Once per short or long rest.",
                    "1/short rest"),
                SF(14, "Convergent Future",
                    "As a reaction when you or a creature you can see within 60 feet makes an attack, check, or save, ignore the die roll and decide whether the result is the minimum needed to succeed or one less. " +
                    "You gain one level of exhaustion (removable only by finishing a long rest for exhaustion gained this way).",
                    "Reaction (gains exhaustion)"),
            };

            d["Conjuration"] = new List<ClassFeature>
            {
                SF(2, "Conjuration Savant",
                    "The gold and time you must spend to copy a conjuration spell into your spellbook is halved."),
                SF(2, "Minor Conjuration",
                    "As an action, conjure an inanimate nonmagical object you have seen (no larger than 3 feet on a side, up to 10 pounds) in your hand or an unoccupied space within 10 feet. " +
                    "It radiates dim light out to 5 feet and disappears after 1 hour, when you use this feature again, or if it takes or deals any damage.",
                    "At will"),
                SF(6, "Benign Transposition",
                    "As an action, teleport up to 30 feet to an unoccupied space you can see, or swap places with a willing Small or Medium creature in range. " +
                    "Recharges on a long rest or when you cast a conjuration spell of 1st level or higher.",
                    "1/long rest (or cast conjuration)"),
                SF(10, "Focused Conjuration",
                    "While you are concentrating on a conjuration spell, your concentration can't be broken as a result of taking damage."),
                SF(14, "Durable Summons",
                    "Any creature you summon or create with a conjuration spell has 30 temporary hit points."),
            };

            d["Divination"] = new List<ClassFeature>
            {
                SF(2, "Divination Savant",
                    "The gold and time you must spend to copy a divination spell into your spellbook is halved."),
                SF(2, "Portent",
                    "When you finish a long rest, roll two d20s and record the results. You can replace any attack roll, saving throw, or ability check made by you or a creature you can see with one of these rolls " +
                    "(choose before the roll; once per turn; each foretelling roll once). Lose unused rolls when you finish a long rest."),
                SF(6, "Expert Divination",
                    "When you cast a divination spell of 2nd level or higher using a spell slot, you regain one expended spell slot of a lower level than the spell you cast (maximum 5th level)."),
                SF(10, "The Third Eye",
                    "As an action, choose one benefit until you are incapacitated or finish a short or long rest: darkvision 60 feet; ethereal sight 60 feet; read any language; " +
                    "or see invisible creatures and objects within 10 feet. Once per short or long rest.",
                    "1/short rest"),
                SF(14, "Greater Portent",
                    "You roll three d20s for your Portent feature, rather than two."),
            };

            d["Enchantment"] = new List<ClassFeature>
            {
                SF(2, "Enchantment Savant",
                    "The gold and time you must spend to copy an enchantment spell into your spellbook is halved."),
                SF(2, "Hypnotic Gaze",
                    "As an action, choose a creature within 5 feet that can see or hear you. It must succeed on a Wisdom save or be charmed until the end of your next turn " +
                    "(speed 0, incapacitated, visibly dazed). You can use your action each turn to maintain it (ends if you move more than 5 feet away, it can't see or hear you, or it takes damage). " +
                    "Once the effect ends or it succeeds, you can't use this on that creature again until a long rest.",
                    "1/creature/long rest"),
                SF(6, "Instinctive Charm",
                    "When a creature you can see within 30 feet attacks you, you can use your reaction to divert the attack (before knowing hit/miss). " +
                    "The attacker must succeed on a Wisdom save or target the closest creature to it other than you or itself. On a success, you can't use this on that attacker until a long rest. " +
                    "Creatures immune to charm are immune.",
                    "Reaction"),
                SF(10, "Split Enchantment",
                    "When you cast an enchantment spell of 1st level or higher that targets only one creature, you can have it target a second creature."),
                SF(14, "Alter Memories",
                    "When you cast an enchantment spell to charm one or more creatures, you can make one creature unaware it is charmed. " +
                    "Once before the spell ends, you can use your action to force an Intelligence save; on a failure it forgets hours of the charmed time equal to 1 + your Charisma modifier (minimum 1)."),
            };

            d["Evocation"] = new List<ClassFeature>
            {
                SF(2, "Evocation Savant",
                    "The gold and time you must spend to copy an evocation spell into your spellbook is halved."),
                SF(2, "Sculpt Spells",
                    "When you cast an evocation spell that affects other creatures you can see, choose a number of them equal to 1 + the spell's level. " +
                    "The chosen creatures automatically succeed on their saving throws against the spell, and they take no damage if they would normally take half damage on a successful save."),
                SF(6, "Potent Cantrip",
                    "When a creature succeeds on a saving throw against your cantrip, the creature takes half the cantrip's damage (if any) but suffers no additional effect from the cantrip."),
                SF(10, "Empowered Evocation",
                    "You can add your Intelligence modifier (minimum of +1) to one damage roll of any wizard evocation spell you cast."),
                SF(14, "Overchannel",
                    "When you cast a wizard spell of 1st through 5th level that deals damage, you can deal maximum damage with that spell. The first time you do so, there is no adverse effect. " +
                    "If you use it again before a long rest, you take 2d12 necrotic damage per spell level (increases by 1d12 each subsequent use), ignoring resistance and immunity."),
            };

            d["Graviturgy"] = new List<ClassFeature>
            {
                SF(2, "Adjust Density",
                    "As an action, alter the weight of one Large or smaller object or creature you can see within 30 feet for up to 1 minute (concentration). " +
                    "Halved weight: +10 feet speed, double jump distance, disadvantage on Strength checks and saves. Doubled weight: −10 feet speed, advantage on Strength checks and saves. " +
                    "At 10th level you can target Huge or smaller.",
                    "At will (concentration)"),
                SF(6, "Gravity Well",
                    "Whenever you cast a spell on a creature, you can move the target 5 feet to an unoccupied space of your choice if it is willing, the spell hits it with an attack, " +
                    "or it fails a saving throw against the spell."),
                SF(10, "Violent Attraction",
                    "As a reaction when another creature you can see within 60 feet hits with a weapon attack, the target takes an extra 1d10 damage of the weapon's type; " +
                    "or when a creature within 60 feet takes fall damage, increase that damage by 2d10. Uses equal to your Intelligence modifier (minimum 1) per long rest.",
                    "Int mod/long rest"),
                SF(14, "Event Horizon",
                    "As an action, emit a gravitational field for up to 1 minute (concentration). Hostile creatures that start their turn within 30 feet make a Strength save: " +
                    "2d10 force damage and speed 0 until their next turn on a failure, or half damage and every foot of movement costs 2 extra feet on a success. " +
                    "Once per long rest, or again by expending a spell slot of 3rd level or higher.",
                    "1/long rest (or 3rd+ slot)"),
            };

            d["Illusion"] = new List<ClassFeature>
            {
                SF(2, "Illusion Savant",
                    "The gold and time you must spend to copy an illusion spell into your spellbook is halved."),
                SF(2, "Improved Minor Illusion",
                    "You learn the Minor Illusion cantrip (or another wizard cantrip if you already know it); it doesn't count against cantrips known. " +
                    "When you cast Minor Illusion, you can create both a sound and an image with a single casting."),
                SF(6, "Malleable Illusions",
                    "When you cast an illusion spell with a duration of 1 minute or longer, you can use your action to change the nature of that illusion (within the spell's normal parameters), " +
                    "provided you can see the illusion."),
                SF(10, "Illusory Self",
                    "When a creature makes an attack roll against you, you can use your reaction to interpose an illusory duplicate; the attack automatically misses you and the illusion dissipates. " +
                    "Once per short or long rest.",
                    "1/short rest"),
                SF(14, "Illusory Reality",
                    "When you cast an illusion spell of 1st level or higher, you can use a bonus action on your turn while the spell is ongoing to make one inanimate, nonmagical object that is part of the illusion real for 1 minute. " +
                    "The object can't deal damage or otherwise directly harm anyone."),
            };

            d["Necromancy"] = new List<ClassFeature>
            {
                SF(2, "Necromancy Savant",
                    "The gold and time you must spend to copy a necromancy spell into your spellbook is halved."),
                SF(2, "Grim Harvest",
                    "Once per turn when you kill one or more creatures with a spell of 1st level or higher, you regain hit points equal to twice the spell's level " +
                    "(three times its level if the spell is necromancy). You don't gain this benefit for killing constructs or undead."),
                SF(6, "Undead Thralls",
                    "Add Animate Dead to your spellbook if it isn't there. When you cast Animate Dead, you can target one additional corpse or pile of bones. " +
                    "Undead you create with a necromancy spell gain bonus HP equal to your wizard level and add your proficiency bonus to weapon damage rolls."),
                SF(10, "Inured to Undeath",
                    "You have resistance to necrotic damage, and your hit point maximum can't be reduced."),
                SF(14, "Command Undead",
                    "As an action, choose one undead you can see within 60 feet. It must make a Charisma save against your spell save DC. On a failure it becomes friendly and obeys your commands until you use this feature again. " +
                    "Intelligent undead (Int 8+) have advantage; Int 12+ can repeat the save every hour."),
            };

            d["Order of Scribes"] = new List<ClassFeature>
            {
                SF(2, "Wizardly Quill",
                    "As a bonus action, create a Tiny magic quill that needs no ink, copies spells into your spellbook in 2 minutes per spell level, " +
                    "and can erase text you wrote with it as a bonus action (within 5 feet).",
                    "Bonus action"),
                SF(2, "Awakened Spellbook",
                    "Your spellbook is a spellcasting focus. When you cast a wizard spell with a slot, you can temporarily replace its damage type with a type from another spell in your book of the same level as the slot. " +
                    "Once per long rest, cast a ritual using its normal casting time instead of adding 10 minutes. You can transfer the book's consciousness to a new blank book or attuned magic spellbook over a short rest.",
                    "Passive (+ 1 free ritual speedup/long rest)"),
                SF(6, "Manifest Mind",
                    "As a bonus action while the book is on your person, manifest its mind as a Tiny spectral object within 60 feet (senses, darkvision 60 feet, telepathically shares what it sees and hears). " +
                    "You can cast wizard spells as if in its space (proficiency bonus times per long rest). Move it up to 30 feet as a bonus action. " +
                    "Conjure once per long rest, or again by expending a spell slot.",
                    "1/long rest (or any slot)"),
                SF(10, "Master Scrivener",
                    "When you finish a long rest, create one magic scroll of a 1st- or 2nd-level spell from your Awakened Spellbook (casting time 1 action); the scroll's spell counts as one level higher. " +
                    "Only you can cast it; it vanishes when cast or at the end of your next long rest. Gold and time to craft spell scrolls are halved when using your Wizardly Quill."),
                SF(14, "One with the Word",
                    "While the book is on your person, advantage on Intelligence (Arcana) checks. If you take damage while the mind is manifested, you can use your reaction to dismiss it and prevent all that damage, " +
                    "then temporarily lose spells from the book whose combined level is at least 3d6 (incapable of casting them for 1d6 long rests). Once per long rest.",
                    "1/long rest"),
            };

            d["Transmutation"] = new List<ClassFeature>
            {
                SF(2, "Transmutation Savant",
                    "The gold and time you must spend to copy a transmutation spell into your spellbook is halved."),
                SF(2, "Minor Alchemy",
                    "Over 10 minutes per cubic foot, transform one nonmagical object of wood, stone (not gemstone), iron, copper, or silver into a different one of those materials " +
                    "(up to the volume you process). After 1 hour or if you lose concentration, it reverts."),
                SF(6, "Transmuter's Stone",
                    "Spend 8 hours to create a stone that grants one benefit while in a creature's possession: darkvision 60 feet; +10 feet speed while unencumbered; " +
                    "proficiency in Constitution saving throws; or resistance to acid, cold, fire, lightning, or thunder. " +
                    "When you cast a transmutation spell of 1st level or higher with the stone on your person, you can change its benefit. Creating a new stone ends the previous one."),
                SF(10, "Shapechanger",
                    "Add Polymorph to your spellbook if it isn't there. You can cast Polymorph without a slot once per short or long rest, targeting only yourself and transforming into a beast of CR 1 or lower. " +
                    "You can still cast it normally with slots.",
                    "1 free self-Polymorph/short rest"),
                SF(14, "Master Transmuter",
                    "As an action, consume your Transmuter's Stone (destroyed; remake after a long rest) for one effect: Major Transformation (10 minutes, transmute a nonmagical object up to a 5-foot cube); " +
                    "Panacea (remove curses, diseases, and poisons and restore all HP); Restore Life (cast Raise Dead without a slot); or Restore Youth (reduce apparent age by 3d10 years, minimum 13)."),
            };

            d["War Magic"] = new List<ClassFeature>
            {
                SF(2, "Arcane Deflection",
                    "When you are hit by an attack or fail a saving throw, you can use your reaction to gain +2 AC against that attack or +4 to that saving throw. " +
                    "Until the end of your next turn, you can't cast spells other than cantrips.",
                    "Reaction"),
                SF(2, "Tactical Wit",
                    "You can give yourself a bonus to initiative rolls equal to your Intelligence modifier."),
                SF(6, "Power Surge",
                    "Store power surges (max = Intelligence modifier, minimum 1). Reset to one on a long rest; gain one when you end a spell with Dispel Magic or Counterspell, " +
                    "or when you finish a short rest with none. Once per turn when you deal damage with a wizard spell, spend a surge to deal extra force damage equal to half your wizard level."),
                SF(10, "Durable Magic",
                    "While you maintain concentration on a spell, you have a +2 bonus to AC and all saving throws."),
                SF(14, "Deflecting Shroud",
                    "When you use Arcane Deflection, you can cause magical energy to arc from you. Up to three creatures of your choice within 60 feet each take force damage equal to half your wizard level."),
            };
        }
    }
}
