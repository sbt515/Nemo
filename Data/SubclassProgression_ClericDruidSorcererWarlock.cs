using System.Collections.Generic;

namespace Nemo
{
    /// <summary>
    /// Subclass level-up features for Cleric domains, Druid circles, Sorcerer origins, and Warlock patrons.
    /// Sources: PHB, XGE, TCE, SCAG, DMG, Dragonlance: SotDQ. Style: dnd5e.wikidot concise.
    /// Uses shared <c>SF</c> factory from SubclassProgressionRangerRogueWizard (IsSubclassFeature = true).
    /// </summary>
    public static partial class GameData
    {
        // ═══════════════════════════════════════════════════════════════════
        // CLERIC DOMAINS (levels 1, 2, 6, 8, 17)
        // ═══════════════════════════════════════════════════════════════════

        private static List<ClassFeature> BuildArcanaDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Detect Magic, Magic Missile; 2nd: Magic Weapon, Nystul's Magic Aura; 3rd: Dispel Magic, Magic Circle; 4th: Arcane Eye, Leomund's Secret Chest; 5th: Planar Binding, Teleportation Circle."),
            SF(1, "Arcane Initiate",
                "Gain proficiency in Arcana. Learn two cantrips of your choice from the wizard spell list; they count as cleric cantrips for you."),
            SF(2, "Channel Divinity: Arcane Abjuration",
                "As an action, present your holy symbol. One celestial, elemental, fey, or fiend within 30 feet that can see or hear you must succeed on a Wisdom save or be turned for 1 minute or until it takes damage. At 5th+ level, failed saves can banish extraplanar targets of low CR (as Banishment, no concentration).",
                "Channel Divinity"),
            SF(6, "Spell Breaker",
                "When you restore hit points to an ally with a spell of 1st level or higher, you can also end one spell on that creature of a level equal to or lower than the slot used."),
            SF(8, "Potent Spellcasting",
                "Add your Wisdom modifier to the damage you deal with any cleric cantrip."),
            SF(17, "Arcane Mastery",
                "Choose four wizard spells (one each of 6th, 7th, 8th, and 9th level). They become domain spells for you: always prepared and count as cleric spells.")
        };

        private static List<ClassFeature> BuildDeathDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: False Life, Ray of Sickness; 2nd: Blindness/Deafness, Ray of Enfeeblement; 3rd: Animate Dead, Vampiric Touch; 4th: Blight, Death Ward; 5th: Antilife Shell, Cloudkill."),
            SF(1, "Bonus Proficiency",
                "Gain proficiency with martial weapons."),
            SF(1, "Reaper",
                "Learn one necromancy cantrip of your choice from any spell list (counts as a cleric cantrip). When you cast a necromancy cantrip that normally targets only one creature, you can target a second creature within 5 feet of the first."),
            SF(2, "Channel Divinity: Touch of Death",
                "When you hit a creature with a melee attack, you can use Channel Divinity to deal extra necrotic damage equal to 5 + twice your cleric level.",
                "Channel Divinity"),
            SF(6, "Inescapable Destruction",
                "Your cleric spells and Channel Divinity options that deal necrotic damage ignore resistance to necrotic damage."),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 necrotic damage (2d8 at 14th level)."),
            SF(17, "Improved Reaper",
                "When you cast a necromancy spell of 1st–5th level that targets only one creature, you can target a second creature within 5 feet of the first (if the spell requires concentration, you concentrate on both).")
        };

        private static List<ClassFeature> BuildForgeDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Identify, Searing Smite; 2nd: Heat Metal, Magic Weapon; 3rd: Elemental Weapon, Protection from Energy; 4th: Fabricate, Wall of Fire; 5th: Animate Objects, Creation."),
            SF(1, "Bonus Proficiencies",
                "Gain proficiency with heavy armor and smith's tools."),
            SF(1, "Blessing of the Forge",
                "At the end of a long rest, touch one nonmagical weapon or suit of armor. Until your next long rest it becomes a magic item with a +1 bonus to AC (armor) or attack and damage rolls (weapon). You can bless only one item at a time.",
                "1/long rest"),
            SF(2, "Channel Divinity: Artisan's Blessing",
                "Conduct a 1-hour ritual using metal material worth at least as much as the finished item (including coins). Create a nonmagical item that includes metal, worth no more than 100 gp.",
                "Channel Divinity"),
            SF(6, "Soul of the Forge",
                "Gain resistance to fire damage. While wearing heavy armor, gain a +1 bonus to AC."),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 fire damage (2d8 at 14th level)."),
            SF(17, "Saint of Forge and Fire",
                "Gain immunity to fire damage. While wearing heavy armor, resistance to bludgeoning, piercing, and slashing damage from nonmagical attacks.")
        };

        private static List<ClassFeature> BuildGraveDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Bane, False Life; 2nd: Gentle Repose, Ray of Enfeeblement; 3rd: Revivify, Vampiric Touch; 4th: Blight, Death Ward; 5th: Antilife Shell, Raise Dead."),
            SF(1, "Circle of Mortality",
                "When you restore hit points with a spell to a creature at 0 HP, use the maximum result for each healing die. Learn Spare the Dying (doesn't count against cantrips known); for you it has 30-foot range and can be cast as a bonus action."),
            SF(1, "Eyes of the Grave",
                "As an action, until the end of your next turn sense the location of any undead within 60 feet that isn't behind total cover or protected from divination.",
                "Wis mod times/long rest (min 1)"),
            SF(2, "Channel Divinity: Path to the Grave",
                "As an action, curse a creature you can see within 30 feet until the end of your next turn. The next time you or an ally hits it with an attack, the creature has vulnerability to all of that attack's damage, then the curse ends.",
                "Channel Divinity"),
            SF(6, "Sentinel at Death's Door",
                "As a reaction when you or an ally you can see within 30 feet suffers a critical hit, turn that hit into a normal hit (cancel critical effects).",
                "Wis mod times/long rest (min 1)"),
            SF(8, "Potent Spellcasting",
                "Add your Wisdom modifier to the damage you deal with any cleric cantrip."),
            SF(17, "Keeper of Souls",
                "When an enemy you can see dies within 30 feet of you, you or one ally within 30 feet regains hit points equal to the enemy's number of Hit Dice. Once per enemy death; only if you aren't incapacitated.")
        };

        private static List<ClassFeature> BuildKnowledgeDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Command, Identify; 2nd: Augury, Suggestion; 3rd: Nondetection, Speak with Dead; 4th: Arcane Eye, Confusion; 5th: Legend Lore, Scrying."),
            SF(1, "Blessings of Knowledge",
                "Learn two languages of your choice. Gain proficiency and expertise (double proficiency bonus) in two skills of your choice from Arcana, History, Nature, or Religion."),
            SF(2, "Channel Divinity: Knowledge of the Ages",
                "As an action, choose one skill or tool. For 10 minutes you have proficiency with the chosen skill or tool.",
                "Channel Divinity"),
            SF(6, "Channel Divinity: Read Thoughts",
                "As an action, choose a creature within 60 feet that you can see. It makes a Wisdom save; on a failure you can read its surface thoughts for 1 minute while it is within 60 feet. As an action during that time you can cast Suggestion on it without a slot (it automatically fails its save). On a successful save, the creature is unaffected and you can't use this on it again until you finish a long rest.",
                "Channel Divinity"),
            SF(8, "Potent Spellcasting",
                "Add your Wisdom modifier to the damage you deal with any cleric cantrip."),
            SF(17, "Visions of the Past",
                "Meditate for at least 1 minute to call up visions of the past. Object Reading: hold an object and learn visions of its previous owner. Area Reading: in a location, sense significant events from the past. Duration and detail scale with meditation time.",
                "1/short or long rest")
        };

        private static List<ClassFeature> BuildLifeDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Bless, Cure Wounds; 2nd: Lesser Restoration, Spiritual Weapon; 3rd: Beacon of Hope, Revivify; 4th: Death Ward, Guardian of Faith; 5th: Mass Cure Wounds, Raise Dead."),
            SF(1, "Bonus Proficiency",
                "Gain proficiency with heavy armor."),
            SF(1, "Disciple of Life",
                "Whenever you use a spell of 1st level or higher to restore hit points to a creature, the creature regains additional hit points equal to 2 + the spell's level."),
            SF(2, "Channel Divinity: Preserve Life",
                "As an action, restore a total number of hit points equal to five times your cleric level, divided among any creatures within 30 feet of you. A creature can be restored to no more than half its hit point maximum. Can't affect undead or constructs.",
                "Channel Divinity"),
            SF(6, "Blessed Healer",
                "When you cast a spell of 1st level or higher that restores hit points to a creature other than you, you regain hit points equal to 2 + the spell's level."),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 radiant damage (2d8 at 14th level)."),
            SF(17, "Supreme Healing",
                "When you would normally roll one or more dice to restore hit points with a spell, use the highest number possible for each die.")
        };

        private static List<ClassFeature> BuildLightDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Burning Hands, Faerie Fire; 2nd: Flaming Sphere, Scorching Ray; 3rd: Daylight, Fireball; 4th: Guardian of Faith, Wall of Fire; 5th: Flame Strike, Scrying."),
            SF(1, "Bonus Cantrip",
                "You know the Light cantrip, which doesn't count against your number of cleric cantrips known."),
            SF(1, "Warding Flare",
                "When you are attacked by a creature within 30 feet that you can see, use your reaction to impose disadvantage on the attack roll (doesn't work if the attacker is immune to being blinded).",
                "Wis mod times/long rest (min 1)"),
            SF(2, "Channel Divinity: Radiance of the Dawn",
                "As an action, present your holy symbol. Magical darkness within 30 feet is dispelled. Each hostile creature within 30 feet must make a Constitution save, taking 2d10 + your cleric level radiant damage on a failed save, or half on a success (creatures with total cover are unaffected).",
                "Channel Divinity"),
            SF(6, "Improved Warding Flare",
                "You can also use Warding Flare when a creature you can see within 30 feet attacks a creature other than you."),
            SF(8, "Potent Spellcasting",
                "Add your Wisdom modifier to the damage you deal with any cleric cantrip."),
            SF(17, "Corona of Light",
                "As an action, activate an aura of sunlight for 1 minute: bright light 60 feet, dim light 30 feet beyond. Enemies in the bright light have disadvantage on saves against your spells that deal fire or radiant damage.",
                "1/long rest (action to activate)")
        };

        private static List<ClassFeature> BuildNatureDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Animal Friendship, Speak with Animals; 2nd: Barkskin, Spike Growth; 3rd: Plant Growth, Wind Wall; 4th: Dominate Beast, Grasping Vine; 5th: Insect Plague, Tree Stride."),
            SF(1, "Acolyte of Nature",
                "Learn one druid cantrip of your choice. Gain proficiency in one of Animal Handling, Nature, or Survival."),
            SF(1, "Bonus Proficiency",
                "Gain proficiency with heavy armor."),
            SF(2, "Channel Divinity: Charm Animals and Plants",
                "As an action, present your holy symbol. Each beast or plant creature that can see you within 30 feet must succeed on a Wisdom save or be charmed by you for 1 minute or until it takes damage.",
                "Channel Divinity"),
            SF(6, "Dampen Elements",
                "When you or a creature within 30 feet takes acid, cold, fire, lightning, or thunder damage, use your reaction to grant resistance to that instance of damage.",
                "Reaction"),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 cold, fire, or lightning damage (your choice; 2d8 at 14th level)."),
            SF(17, "Master of Nature",
                "You can command charmed beasts and plants. While a beast or plant is charmed by your Channel Divinity, you can take a bonus action on your turn to verbally command what it will do on its next turn.")
        };

        private static List<ClassFeature> BuildOrderDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Command, Heroism; 2nd: Hold Person, Zone of Truth; 3rd: Mass Healing Word, Slow; 4th: Compulsion, Locate Creature; 5th: Commune, Dominate Person."),
            SF(1, "Bonus Proficiencies",
                "Gain proficiency with heavy armor. Gain proficiency in Intimidation or Persuasion (your choice)."),
            SF(1, "Voice of Authority",
                "If you cast a spell of 1st level or higher with a casting time of 1 action that targets an ally, that ally can use their reaction immediately after the spell to make one weapon attack against a creature of your choice that you can see."),
            SF(2, "Channel Divinity: Order's Demand",
                "As an action, present your holy symbol. Each creature of your choice that can see or hear you within 30 feet must succeed on a Wisdom save or be charmed by you until the end of your next turn or until it takes damage. You can also cause any charmed targets to drop what they are holding.",
                "Channel Divinity"),
            SF(6, "Embodiment of the Law",
                "When you cast an enchantment spell of 1st level or higher using a spell slot, you can change the casting time to 1 bonus action for this casting.",
                "Wis mod times/long rest (min 1)"),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 psychic damage (2d8 at 14th level)."),
            SF(17, "Order's Wrath",
                "If you deal your Divine Strike damage to a creature, it becomes cursed until the start of your next turn. The next time one of your allies hits the cursed creature with an attack, the target also takes 2d8 psychic damage, and the curse ends.")
        };

        private static List<ClassFeature> BuildPeaceDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Heroism, Sanctuary; 2nd: Aid, Warding Bond; 3rd: Beacon of Hope, Sending; 4th: Aura of Purity, Otiluke's Resilient Sphere; 5th: Greater Restoration, Rary's Telepathic Bond."),
            SF(1, "Implement of Peace",
                "Gain proficiency in Insight, Performance, or Persuasion (your choice)."),
            SF(1, "Emboldening Bond",
                "As an action, choose a number of willing creatures within 30 feet equal to your proficiency bonus (you can include yourself). For 10 minutes (or until you use this again), while any bonded creature is within 30 feet of another, it can add 1d4 to an attack roll, ability check, or saving throw it makes (once per turn).",
                "PB times/long rest"),
            SF(2, "Channel Divinity: Balm of Peace",
                "As an action, move up to your speed without provoking opportunity attacks. When you move within 5 feet of any other creature during this action, you can restore hit points equal to 2d6 + your Wisdom modifier (each creature once per use).",
                "Channel Divinity"),
            SF(6, "Protective Bond",
                "When a creature affected by your Emboldening Bond takes damage, a different bonded creature within 30 feet can use its reaction to teleport next to the injured creature and take the damage instead."),
            SF(8, "Potent Spellcasting",
                "Add your Wisdom modifier to the damage you deal with any cleric cantrip."),
            SF(17, "Expansive Bond",
                "The range of Emboldening Bond and Protective Bond increases to 60 feet. When a creature uses Protective Bond to take damage for another, it has resistance to that damage.")
        };

        private static List<ClassFeature> BuildTempestDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Fog Cloud, Thunderwave; 2nd: Gust of Wind, Shatter; 3rd: Call Lightning, Sleet Storm; 4th: Control Water, Ice Storm; 5th: Destructive Wave, Insect Plague."),
            SF(1, "Bonus Proficiencies",
                "Gain proficiency with martial weapons and heavy armor."),
            SF(1, "Wrath of the Storm",
                "When a creature within 5 feet of you that you can see hits you with an attack, you can use your reaction to deal 2d8 lightning or thunder damage (your choice) to the attacker. Dexterity save for half damage.",
                "Wis mod times/long rest (min 1)"),
            SF(2, "Channel Divinity: Destructive Wrath",
                "When you roll lightning or thunder damage, you can use Channel Divinity to deal maximum damage instead of rolling.",
                "Channel Divinity"),
            SF(6, "Thunderbolt Strike",
                "When you deal lightning damage to a Large or smaller creature, you can also push it up to 10 feet away from you."),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 thunder damage (2d8 at 14th level)."),
            SF(17, "Stormborn",
                "You have a flying speed equal to your walking speed whenever you are not underground or indoors.")
        };

        private static List<ClassFeature> BuildTrickeryDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Charm Person, Disguise Self; 2nd: Mirror Image, Pass without Trace; 3rd: Blink, Dispel Magic; 4th: Dimension Door, Polymorph; 5th: Dominate Person, Modify Memory."),
            SF(1, "Blessing of the Trickster",
                "As an action, give one creature within 30 feet (including yourself) advantage on Dexterity (Stealth) checks. Lasts 1 hour or until you use this feature again."),
            SF(2, "Channel Divinity: Invoke Duplicity",
                "As an action, create a perfect visual illusion of yourself that lasts 1 minute or until you lose concentration (as concentrating on a spell). The illusion appears in an unoccupied space within 30 feet. As a bonus action, move it up to 30 feet (max 120 feet from you). You can cast spells as though you were in the illusion's space; you have advantage on attack rolls against creatures if you and the illusion are within 5 feet of the target.",
                "Channel Divinity"),
            SF(6, "Channel Divinity: Cloak of Shadows",
                "As an action, become invisible until the end of your next turn. The invisibility ends early if you attack or cast a spell.",
                "Channel Divinity"),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 poison damage (2d8 at 14th level)."),
            SF(17, "Improved Duplicity",
                "You can create up to four duplicates with Invoke Duplicity instead of one. As a bonus action, move any number of them up to 30 feet each (max 120 feet from you).")
        };

        private static List<ClassFeature> BuildTwilightDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Faerie Fire, Sleep; 2nd: Moonbeam, See Invisibility; 3rd: Aura of Vitality, Leomund's Tiny Hut; 4th: Aura of Life, Greater Invisibility; 5th: Circle of Power, Mislead."),
            SF(1, "Bonus Proficiencies",
                "Gain proficiency with martial weapons and heavy armor."),
            SF(1, "Eyes of Night",
                "You have darkvision out to 300 feet. As an action, share the darkvision with willing creatures you can see within 10 feet (number equal to your Wisdom modifier, min 1) for 1 hour.",
                "Share: 1/long rest or expend a spell slot"),
            SF(1, "Vigilant Blessing",
                "As an action, give one creature you touch (including yourself) advantage on the next initiative roll it makes. Ends when used or when you use this feature again."),
            SF(2, "Channel Divinity: Twilight Sanctuary",
                "As an action, create a 30-foot-radius sphere of twilight centered on you for 1 minute (moves with you). Whenever a creature (including you) ends its turn in the sphere, you can grant it temporary hit points equal to 1d6 + your cleric level, or end one effect causing it to be charmed or frightened.",
                "Channel Divinity"),
            SF(6, "Steps of Night",
                "As a bonus action when you are in dim light or darkness, gain a flying speed equal to your walking speed for 1 minute.",
                "PB times/long rest"),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 radiant damage (2d8 at 14th level)."),
            SF(17, "Twilight Shroud",
                "The twilight of your Twilight Sanctuary gives you and your allies half cover while in the sphere.")
        };

        private static List<ClassFeature> BuildWarDomain() => new()
        {
            SF(1, "Domain Spells",
                "Always prepared (don't count against prepared spells). 1st: Divine Favor, Shield of Faith; 2nd: Magic Weapon, Spiritual Weapon; 3rd: Crusader's Mantle, Spirit Guardians; 4th: Freedom of Movement, Stoneskin; 5th: Flame Strike, Hold Monster."),
            SF(1, "Bonus Proficiencies",
                "Gain proficiency with martial weapons and heavy armor."),
            SF(1, "War Priest",
                "When you use the Attack action, you can make one weapon attack as a bonus action.",
                "Wis mod times/long rest (min 1)"),
            SF(2, "Channel Divinity: Guided Strike",
                "When you make an attack roll, you can use Channel Divinity to gain a +10 bonus to the roll. You can use this after seeing the roll but before knowing whether it hits.",
                "Channel Divinity"),
            SF(6, "Channel Divinity: War God's Blessing",
                "When a creature within 30 feet of you makes an attack roll, you can use your reaction to grant a +10 bonus to that roll (after seeing the roll, before knowing the result).",
                "Channel Divinity"),
            SF(8, "Divine Strike",
                "Once on each of your turns when you hit a creature with a weapon attack, deal an extra 1d8 damage of the same type as the weapon (2d8 at 14th level)."),
            SF(17, "Avatar of Battle",
                "You gain resistance to bludgeoning, piercing, and slashing damage from nonmagical attacks.")
        };

        // ═══════════════════════════════════════════════════════════════════
        // DRUID CIRCLES (levels 2, 6, 10, 14)
        // ═══════════════════════════════════════════════════════════════════

        private static List<ClassFeature> BuildCircleOfDreams() => new()
        {
            SF(2, "Balm of the Summer Court",
                "You have a pool of fey energy equal to your druid level. As a bonus action, choose a creature within 120 feet and spend dice from the pool (d6s) to restore hit points equal to the dice spent and grant temporary hit points equal to the number of dice spent. Maximum dice spent per use = half your druid level (rounded up).",
                "Pool = druid level d6s / long rest"),
            SF(6, "Hearth of Moonlight and Shadow",
                "During a short or long rest, create a 30-foot-radius sphere of magic for the rest. You and allies gain a +5 bonus to Dexterity (Stealth) and Wisdom (Perception) checks within the sphere, and light from open flames in the sphere is not visible from outside."),
            SF(10, "Hidden Paths",
                "As a bonus action, teleport up to 60 feet to an unoccupied space you can see. Alternatively, teleport a willing creature you touch up to 30 feet to an unoccupied space you can see.",
                "Wis mod times/long rest (min 1)"),
            SF(14, "Walker in Dreams",
                "When you finish a short rest, cast Dream, Scrying, or Teleportation Circle once without a spell slot (Teleportation Circle only opens to the last place you finished a long rest on your current plane). Casting Dream or Scrying this way no longer requires components.",
                "1/long rest")
        };

        private static List<ClassFeature> BuildCircleOfSpores() => new()
        {
            SF(2, "Circle Spells",
                "Always prepared (don't count against prepared spells). Learn Chill Touch. 2nd-level slots: Blindness/Deafness, Gentle Repose; 3rd: Animate Dead, Gaseous Form; 4th: Blight, Confusion; 5th: Cloudkill, Contagion."),
            SF(2, "Halo of Spores",
                "When a creature you can see moves into a space within 10 feet of you or starts its turn there, you can use your reaction to deal 1d4 necrotic damage (Con save vs your spell save DC negates). Damage die increases: 1d6 (6th), 1d8 (10th), 1d10 (14th).",
                "Reaction"),
            SF(2, "Symbiotic Entity",
                "As an action, expend a use of Wild Shape to awaken your spores instead of transforming: gain temporary hit points equal to 4 × your druid level for 10 minutes. While active, Halo of Spores damage is rolled twice and added together, and your melee weapon attacks deal an extra 1d6 necrotic damage. Ends early if you lose all temp HP or use Wild Shape again.",
                "Wild Shape use"),
            SF(6, "Fungal Infestation",
                "If a Small or Medium beast or humanoid dies within 10 feet of you, use your reaction to raise it as a zombie (1 HP) for 1 hour. It takes only the Attack action (one melee attack) and obeys your mental commands. Turn order immediately after yours.",
                "Wis mod times/long rest (min 1)"),
            SF(10, "Spreading Spores",
                "While Symbiotic Entity is active, as a bonus action hurl spores to a point within 30 feet: a 10-foot cube for 1 minute. Creatures moving into or starting turns in the cube take Halo of Spores damage (Con save negates; once per turn). You can't use Halo of Spores reaction while the cube persists.",
                "Bonus action (Symbiotic Entity)"),
            SF(14, "Fungal Body",
                "You can't be blinded, deafened, frightened, or poisoned, and any critical hit against you counts as a normal hit unless you're incapacitated.")
        };

        private static List<ClassFeature> BuildCircleOfStars() => new()
        {
            SF(2, "Star Map",
                "You've created a star chart as a spellcasting focus. While holding it, you know Guidance, and you always have Guiding Bolt prepared. You can cast Guiding Bolt a number of times equal to your proficiency bonus without a spell slot (regain on long rest). Changing maps requires a night under the open sky and 8 hours of work.",
                "PB free Guiding Bolts/long rest"),
            SF(2, "Starry Form",
                "As a bonus action, expend a use of Wild Shape to take a starry form for 10 minutes: glowing constellation figures. Choose Archer (bonus action luminous arrow: ranged spell attack 60 ft, 1d8 + Wis mod radiant), Chalice (when you cast a healing spell of 1st+, you or a creature within 30 feet also heals 1d8 + Wis mod), or Dragon (treat Int/Wis/Cha check or Con save rolls of 9 or lower as 10).",
                "Wild Shape use"),
            SF(6, "Cosmic Omen",
                "Whenever you finish a long rest, roll a die for Weal (even) or Woe (odd). As a reaction when a creature you can see within 30 feet makes an attack roll, ability check, or save: Weal — add 1d6; Woe — subtract 1d6.",
                "PB times/long rest"),
            SF(10, "Twinkling Constellations",
                "Archer and Chalice dice become 2d8. While in Dragon form, you have a flying speed of 20 feet and can hover. At the start of each of your turns while in Starry Form, you can change which constellation you manifest."),
            SF(14, "Full of Stars",
                "While in your Starry Form, you become partially incorporeal: resistance to bludgeoning, piercing, and slashing damage.")
        };

        private static List<ClassFeature> BuildCircleOfTheLand() => new()
        {
            SF(2, "Bonus Cantrip",
                "Learn one additional druid cantrip of your choice."),
            SF(2, "Natural Recovery",
                "During a short rest, recover expended spell slots with a combined level equal to or less than half your druid level (rounded up), and none of the slots can be 6th level or higher.",
                "1/long rest"),
            SF(2, "Circle Spells",
                "Choose a land type (Arctic, Coast, Desert, Forest, Grassland, Mountain, Swamp, or Underdark). You gain always-prepared circle spells at 3rd, 5th, 7th, and 9th level based on that land (e.g. Forest: Barkskin, Spider Climb; Call Lightning, Plant Growth; Divination, Freedom of Movement; Commune with Nature, Tree Stride)."),
            SF(6, "Land's Stride",
                "Moving through nonmagical difficult terrain costs you no extra movement. You can pass through nonmagical plants without being slowed or taking damage from them. Advantage on saves against plants magically created or manipulated to impede movement."),
            SF(10, "Nature's Ward",
                "You can't be charmed or frightened by elementals or fey, and you are immune to poison and disease."),
            SF(14, "Nature's Sanctuary",
                "When a beast or plant creature attacks you, that creature must make a Wisdom save against your spell save DC. On a failed save, it must choose a different target or the attack misses. On a success, it is immune to this effect for 24 hours. The creature is aware of this difficulty before it tries to attack you.")
        };

        private static List<ClassFeature> BuildCircleOfTheMoon() => new()
        {
            SF(2, "Combat Wild Shape",
                "You can use Wild Shape as a bonus action. While transformed, you can expend a spell slot as a bonus action to regain 1d8 hit points per level of the spell slot expended."),
            SF(2, "Circle Forms",
                "Your beast form CR limit is 1 at 2nd level (ignoring the normal 1/4 limit). From 6th level, you can transform into a beast with CR as high as your druid level divided by 3, rounded down."),
            SF(6, "Primal Strike",
                "Your attacks in beast form count as magical for the purpose of overcoming resistance and immunity to nonmagical attacks and damage."),
            SF(10, "Elemental Wild Shape",
                "You can expend two uses of Wild Shape at the same time to transform into an air, earth, fire, or water elemental."),
            SF(14, "Thousand Forms",
                "You can cast Alter Self at will.")
        };

        private static List<ClassFeature> BuildCircleOfTheShepherd() => new()
        {
            SF(2, "Speech of the Woods",
                "You learn to speak, read, and write Sylvan. Beasts can understand your speech, and you gain the ability to decipher their noises and motions (not true telepathy; limited by the beast's Intelligence)."),
            SF(2, "Spirit Totem",
                "As a bonus action, summon an incorporeal spirit to a point within 60 feet for 1 minute (30-foot aura). Bear (temp HP = 5 + druid level when summoned; advantage on Strength checks and saves), Hawk (reaction for advantage on an attack if attacker and target are in aura), or Unicorn (when you heal with a spell, creatures of your choice in aura also heal your druid level; advantage on ability checks to detect creatures in aura).",
                "1/short or long rest"),
            SF(6, "Mighty Summoner",
                "Beasts and fey that you summon or create with a spell gain 2 extra hit points per Hit Die and their natural weapon attacks count as magical."),
            SF(10, "Guardian Spirit",
                "When a beast or fey that you summoned or created with a spell ends its turn in your Spirit Totem aura, it regains hit points equal to half your druid level."),
            SF(14, "Faithful Summons",
                "If you are reduced to 0 hit points or are incapacitated against your will, you can immediately gain the benefits of Conjure Animals as if cast with a 9th-level slot (4 beasts of CR 2 or lower). They appear within 20 feet of you. If you don't dismiss them or regain consciousness, they disappear after 1 hour.",
                "1/long rest")
        };

        private static List<ClassFeature> BuildCircleOfWildfire() => new()
        {
            SF(2, "Circle Spells",
                "Always prepared (don't count against prepared spells). Druid 2nd: Burning Hands, Cure Wounds; 3rd: Flaming Sphere, Scorching Ray; 5th: Plant Growth, Revivify; 7th: Aura of Life, Fire Shield; 9th: Flame Strike, Mass Cure Wounds."),
            SF(2, "Summon Wildfire Spirit",
                "As an action, expend a use of Wild Shape to summon a wildfire spirit in an unoccupied space within 30 feet for 1 hour. Creatures within 10 feet of the appearance point (other than you) Dex save or take 2d6 fire damage. The spirit is friendly, shares your initiative (acts after you), and obeys your bonus-action commands (Flame Seed, Fiery Teleportation, etc.).",
                "Wild Shape use"),
            SF(6, "Enhanced Bond",
                "While your wildfire spirit is present, when you cast a spell that deals fire damage or restores hit points, add 1d8 to one roll of the spell. You can also cast spells with a range other than self as if originating from the spirit (must be within 100 feet of you)."),
            SF(10, "Cauterizing Flames",
                "When a Small or larger creature dies within 30 feet of you or your wildfire spirit, a harmless spectral flame erupts in its space for 1 minute. As a reaction when a creature enters the flame or the flame appears under a creature, the flame vanishes and that creature takes 2d10 + Wis mod fire damage or regains that many hit points (your choice).",
                "PB times/long rest"),
            SF(14, "Blazing Revival",
                "If the wildfire spirit is within 120 feet of you when you are reduced to 0 hit points and don't die outright, you can cause the spirit to drop to 0 hit points. You regain half your hit points and immediately rise.",
                "1/long rest")
        };

        // ═══════════════════════════════════════════════════════════════════
        // SORCERER ORIGINS (levels 1, 6, 14, 18)
        // ═══════════════════════════════════════════════════════════════════

        private static List<ClassFeature> BuildAberrantMind() => new()
        {
            SF(1, "Psionic Spells",
                "You learn additional spells that count as sorcerer spells and don't count against spells known: cantrip Mind Sliver; 1st Arms of Hadar, Dissonant Whispers; 2nd Calm Emotions, Detect Thoughts; 3rd Hunger of Hadar, Sending; 4th Evard's Black Tentacles, Summon Aberration; 5th Rary's Telepathic Bond, Telekinesis. When you gain a sorcerer level, you can replace one with a divination or enchantment spell from the sorcerer, warlock, or wizard list of the same level."),
            SF(1, "Telepathic Speech",
                "As a bonus action, choose one creature you can see within 30 feet to create a telepathic link for a number of minutes equal to your sorcerer level. While within a number of miles equal to your Charisma modifier (min 1 mile) you can communicate telepathically. Doesn't require a shared language; creature must understand at least one language."),
            SF(6, "Psionic Sorcery",
                "When you cast any spell from your Psionic Spells feature, you can cast it by spending sorcery points equal to the spell's level instead of a spell slot. If cast with only sorcery points, it needs no verbal or somatic components, and no material components without a cost."),
            SF(6, "Psychic Defenses",
                "You gain resistance to psychic damage, and advantage on saving throws against being charmed or frightened."),
            SF(14, "Revelation in Flesh",
                "As a bonus action, spend 1 or more sorcery points to transform for 10 minutes. For each point, choose a benefit: aquatic adaptation (swim speed + breathe water); gaze of two minds (see through a willing creature's senses); soft sticky body (climb speed, including ceilings); or malleable form (squeeze, opportunity attacks against you have disadvantage).",
                "Sorcery points"),
            SF(18, "Warping Implosion",
                "As an action, teleport to an unoccupied space you can see within 120 feet. Each creature within 30 feet of the space you left makes a Strength save or takes 3d10 force damage and is pulled to the nearest unoccupied space to the origin point (half damage and no pull on success).",
                "1/long rest or 5 sorcery points")
        };

        private static List<ClassFeature> BuildClockworkSoul() => new()
        {
            SF(1, "Clockwork Magic",
                "You learn additional spells that count as sorcerer spells and don't count against spells known: 1st Alarm, Protection from Evil and Good; 2nd Aid, Lesser Restoration; 3rd Dispel Magic, Protection from Energy; 4th Freedom of Movement, Summon Construct; 5th Greater Restoration, Wall of Force. When you gain a sorcerer level, you can replace one with an abjuration or transmutation spell from the sorcerer, warlock, or wizard list of the same level."),
            SF(1, "Restore Balance",
                "When a creature you can see within 60 feet is about to roll a d20 with advantage or disadvantage, you can use your reaction to prevent the advantage or disadvantage from applying to that roll.",
                "PB times/long rest"),
            SF(6, "Bastion of Law",
                "As an action, spend 1–5 sorcery points to create a ward on a creature you touch. Roll a number of d8s equal to the points spent and record the total. When the warded creature takes damage, it can use its reaction to reduce the damage by that total, then the ward ends. Ward lasts until you finish a long rest or use this feature again.",
                "Sorcery points"),
            SF(14, "Trance of Order",
                "As a bonus action, enter a trance for 1 minute. Attack rolls against you can't benefit from advantage, and whenever you make an attack roll, ability check, or saving throw, you can treat a roll of 9 or lower on the d20 as a 10.",
                "1/long rest or 5 sorcery points"),
            SF(18, "Clockwork Cavalcade",
                "As an action, summon spirit constructs in a 30-foot cube originating from you. They restore up to 100 hit points, divided as you choose among creatures in the cube; repair damaged objects; and end every spell of 6th level or lower on creatures of your choice in the cube. Then they vanish.",
                "1/long rest or 7 sorcery points")
        };

        private static List<ClassFeature> BuildDivineSoul() => new()
        {
            SF(1, "Divine Magic",
                "Your link to the divine lets you learn cleric spells as sorcerer spells. When your Spellcasting feature lets you learn or replace a sorcerer cantrip or spell, you can choose from the cleric list as well. Also learn one bonus spell based on affinity: Good — Cure Wounds; Evil — Inflict Wounds; Law — Bless; Chaos — Bane; Neutrality — Protection from Evil and Good. It doesn't count against spells known."),
            SF(1, "Favored by the Gods",
                "If you fail a saving throw or miss with an attack roll, you can roll 2d4 and add it to the total, possibly changing the outcome.",
                "1/short or long rest"),
            SF(6, "Empowered Healing",
                "When you or an ally within 5 feet rolls dice to determine hit points restored by a spell, you can spend 1 sorcery point to reroll any number of those dice once.",
                "1 sorcery point"),
            SF(14, "Otherworldly Wings",
                "As a bonus action, manifest spectral wings for 1 minute, gaining a flying speed of 30 feet. Appearance depends on divine affinity. Wings disappear early if you are incapacitated or die. Dismiss as a bonus action.",
                "1/long rest"),
            SF(18, "Unearthly Recovery",
                "As a bonus action when you have fewer than half your hit points remaining, regain hit points equal to half your hit point maximum.",
                "1/long rest")
        };

        private static List<ClassFeature> BuildDraconicBloodline() => new()
        {
            SF(1, "Dragon Ancestor",
                "Choose a dragon type (chromatic or metallic). You can speak, read, and write Draconic. When you make a Charisma check interacting with dragons, double your proficiency bonus if it applies."),
            SF(1, "Draconic Resilience",
                "Your hit point maximum increases by 1, and increases by 1 again whenever you gain a sorcerer level. When you aren't wearing armor, your AC equals 13 + your Dexterity modifier."),
            SF(6, "Elemental Affinity",
                "When you cast a spell that deals damage of the type associated with your draconic ancestry, add your Charisma modifier to one damage roll of that spell. At the same time, you can spend 1 sorcery point to gain resistance to that damage type for 1 hour."),
            SF(14, "Dragon Wings",
                "As a bonus action, sprout draconic wings, gaining a flying speed equal to your walking speed. They last until you dismiss them as a bonus action. You can't manifest them while wearing armor unless the armor is made to accommodate wings; clothes may be destroyed when the wings appear."),
            SF(18, "Draconic Presence",
                "As an action, spend 5 sorcery points to create a 60-foot aura of awe or fear for 1 minute (concentration). Each hostile creature that starts its turn in the aura must succeed on a Wisdom save or be charmed (awe) or frightened (fear) until the aura ends. A creature that succeeds is immune for 24 hours.",
                "5 sorcery points")
        };

        private static List<ClassFeature> BuildLunarSorcery() => new()
        {
            SF(1, "Lunar Embodiment",
                "Learn additional phase spells (don't count against spells known). Full / New / Crescent — 1st: Shield / Ray of Sickness / Color Spray; 3rd: Lesser Restoration / Blindness/Deafness / Alter Self; 5th: Dispel Magic / Vampiric Touch / Phantom Steed; 7th: Death Ward / Confusion / Hallucinatory Terrain; 9th: Rary's Telepathic Bond / Hold Monster / Mislead. After a long rest, choose Full, New, or Crescent Moon. Once per long rest while in that phase, cast that phase's 1st-level spell without a slot."),
            SF(1, "Moon Fire",
                "You learn Sacred Flame (doesn't count against cantrips known). When you cast it, you can target one creature as normal or two creatures within range that are within 5 feet of each other."),
            SF(6, "Lunar Boons",
                "When you use Metamagic on a spell of a school associated with your current phase, reduce the sorcery point cost by 1 (minimum 0). Full: abjuration & divination; New: enchantment & necromancy; Crescent: illusion & transmutation.",
                "PB times/long rest"),
            SF(6, "Waxing and Waning",
                "As a bonus action, spend 1 sorcery point to change your Lunar Embodiment phase. You can now cast each phase's 1st-level lunar spell once without a slot per long rest (while in that phase).",
                "1 sorcery point to change phase"),
            SF(14, "Lunar Empowerment",
                "While in a phase: Full — bonus action shed/douse bright light 10 ft (dim +10 ft); you and chosen creatures have advantage on Investigation and Perception in that bright light. New — advantage on Stealth; while entirely in darkness, attack rolls against you have disadvantage. Crescent — resistance to necrotic and radiant damage."),
            SF(18, "Lunar Phenomenon",
                "As a bonus action (or when changing phase), use your phase's power once per long rest (or spend 5 SP to use again): Full — chosen creatures in 30 ft Con save or blinded until end of their next turn; one chosen creature regains 3d8 HP. New — chosen creatures in 30 ft Dex save or 3d10 necrotic and speed 0 until end of next turn; you turn invisible until end of your next turn (ends early if you attack or cast). Crescent — teleport up to 60 ft with one willing creature within 5 ft; both gain resistance to all damage until start of your next turn.",
                "1/long rest per benefit (or 5 SP)")
        };

        private static List<ClassFeature> BuildShadowMagic() => new()
        {
            SF(1, "Eyes of the Dark",
                "You have darkvision with a range of 120 feet. When you reach 3rd level, you learn Darkness; it doesn't count against spells known. You can also cast it by spending 2 sorcery points. If you cast it with sorcery points or a spell slot, you can see through the darkness created by that casting."),
            SF(1, "Strength of the Grave",
                "When damage reduces you to 0 hit points, you can make a Charisma saving throw (DC 5 + damage taken). On a success, you drop to 1 hit point instead. Can't use if the damage is radiant or from a critical hit.",
                "1/long rest"),
            SF(6, "Hound of Ill Omen",
                "As a bonus action, spend 3 sorcery points to summon a hound of ill omen (dire wolf stat block, shadowy traits) within 30 feet of a creature you can see. It gains temp HP equal to half your sorcerer level, hunts only that target, and while within 5 feet of the target the target has disadvantage on saves against your spells.",
                "3 sorcery points"),
            SF(14, "Shadow Walk",
                "When you are in dim light or darkness, as a bonus action you can magically teleport up to 120 feet to an unoccupied space you can see that is also in dim light or darkness."),
            SF(18, "Umbral Form",
                "As a bonus action, spend 6 sorcery points to transform for 1 minute: resistance to all damage except force and radiant, and you can move through creatures and objects as if they were difficult terrain (end your turn inside a creature or object: 1d10 force damage and ejected).",
                "6 sorcery points")
        };

        private static List<ClassFeature> BuildStormSorcery() => new()
        {
            SF(1, "Wind Speaker",
                "You can speak, read, and write Primordial. Knowing Primordial lets you understand and be understood by those who speak its dialects: Aquan, Auran, Ignan, and Terran."),
            SF(1, "Tempestuous Magic",
                "Immediately before or after you cast a spell of 1st level or higher, you can use a bonus action to fly up to 10 feet without provoking opportunity attacks."),
            SF(6, "Heart of the Storm",
                "You gain resistance to lightning and thunder damage. When you cast a spell of 1st level or higher that deals lightning or thunder damage, stormy magic erupts: creatures of your choice within 10 feet of you take lightning or thunder damage (your choice) equal to half your sorcerer level."),
            SF(6, "Storm Guide",
                "If it is raining, you can use an action to cause the rain to stop falling in a 20-foot-radius sphere centered on you (moves with you; ends as a bonus action or if you are incapacitated). If it is windy, as a bonus action choose the wind's direction within 100 feet of you; the wind blows in that direction until the end of your next turn."),
            SF(14, "Storm's Fury",
                "When you are hit by a melee attack, you can use your reaction to deal lightning damage to the attacker equal to your sorcerer level. The attacker must also make a Strength save against your spell save DC or be pushed up to 20 feet away from you.",
                "Reaction"),
            SF(18, "Wind Soul",
                "You gain immunity to lightning and thunder damage and a magical flying speed of 60 feet. As an action, reduce your flying speed to 30 feet for 1 hour and choose a number of creatures within 30 feet equal to 3 + your Charisma modifier. The chosen creatures gain a magical flying speed of 30 feet for 1 hour.",
                "Flying grant: 1/long rest")
        };

        private static List<ClassFeature> BuildWildMagic() => new()
        {
            SF(1, "Wild Magic Surge",
                "Immediately after you cast a sorcerer spell of 1st level or higher, your DM can have you roll a d20. If you roll a 1, roll on the Wild Magic Surge table for a random magical effect."),
            SF(1, "Tides of Chaos",
                "You can manipulate chance to gain advantage on one attack roll, ability check, or saving throw. Once you do so, you must finish a long rest before using it again. Any time before you regain the use, your DM can have you roll on the Wild Magic Surge table immediately after you cast a sorcerer spell of 1st level or higher; you then regain the use of this feature.",
                "1/long rest (refreshes on forced surge)"),
            SF(6, "Bend Luck",
                "When another creature you can see makes an attack roll, ability check, or saving throw, you can use your reaction and spend 2 sorcery points to roll 1d4 and apply it as a bonus or penalty (your choice) to the creature's roll (after seeing the roll, before knowing success or failure).",
                "Reaction + 2 sorcery points"),
            SF(14, "Controlled Chaos",
                "Whenever you roll on the Wild Magic Surge table, you can roll twice and use either number."),
            SF(18, "Spell Bombardment",
                "Once per turn when you roll damage for a spell and roll the highest number on a die, choose one of those dice and roll it again, adding that roll to the damage.")
        };

        // ═══════════════════════════════════════════════════════════════════
        // WARLOCK PATRONS (levels 1, 6, 10, 14)
        // ═══════════════════════════════════════════════════════════════════

        private static List<ClassFeature> BuildTheArchfey() => new()
        {
            SF(1, "Expanded Spell List",
                "The following spells are added to the warlock spell list for you. 1st: Faerie Fire, Sleep; 2nd: Calm Emotions, Phantasmal Force; 3rd: Blink, Plant Growth; 4th: Dominate Beast, Greater Invisibility; 5th: Dominate Person, Seeming."),
            SF(1, "Fey Presence",
                "As an action, cause each creature in a 10-foot cube originating from you to make a Wisdom saving throw against your warlock spell save DC or be charmed or frightened by you (your choice) until the end of your next turn.",
                "1/short or long rest"),
            SF(6, "Misty Escape",
                "When you take damage, you can use your reaction to turn invisible and teleport up to 60 feet to an unoccupied space you can see. You remain invisible until the start of your next turn or until you attack or cast a spell.",
                "1/short or long rest"),
            SF(10, "Beguiling Defenses",
                "You are immune to being charmed. When a creature attempts to charm you, you can use your reaction to turn the charm back: the creature must succeed on a Wisdom save against your warlock spell save DC or be charmed by you for 1 minute or until it takes damage."),
            SF(14, "Dark Delirium",
                "As an action, choose a creature within 60 feet that you can see. It makes a Wisdom save or is charmed or frightened by you (your choice) for 1 minute or until your concentration ends. While affected, it sees itself lost in a misty realm; it is deafened and blinded to all but you and the illusory environment.",
                "1/short or long rest")
        };

        private static List<ClassFeature> BuildTheCelestial() => new()
        {
            SF(1, "Expanded Spell List",
                "The following spells are added to the warlock spell list for you. 1st: Cure Wounds, Guiding Bolt; 2nd: Flaming Sphere, Lesser Restoration; 3rd: Daylight, Revivify; 4th: Guardian of Faith, Wall of Fire; 5th: Flame Strike, Greater Restoration."),
            SF(1, "Bonus Cantrips",
                "You learn the Light and Sacred Flame cantrips. They count as warlock cantrips for you but don't count against your number of cantrips known."),
            SF(1, "Healing Light",
                "You have a pool of d6s equal to 1 + your warlock level. As a bonus action, heal one creature you can see within 60 feet, spending dice from the pool (maximum dice per use = your Charisma modifier, min 1). The creature regains hit points equal to the total rolled.",
                "Pool = 1 + warlock level d6s / long rest"),
            SF(6, "Radiant Soul",
                "You gain resistance to radiant damage. When you cast a spell that deals radiant or fire damage, add your Charisma modifier to one radiant or fire damage roll of that spell against one of its targets."),
            SF(10, "Celestial Resilience",
                "When you finish a short or long rest, you gain temporary hit points equal to your warlock level + your Charisma modifier. Additionally, choose up to five creatures you can see at the end of the rest; each gains temporary hit points equal to half your warlock level + your Charisma modifier."),
            SF(14, "Searing Vengeance",
                "When you must make a death saving throw at the start of your turn, you can instead spring back: regain hit points equal to half your hit point maximum, then stand if you choose. Each creature of your choice within 30 feet takes radiant damage equal to 2d8 + your Charisma modifier and is blinded until the end of the current turn.",
                "1/long rest")
        };

        private static List<ClassFeature> BuildTheFathomless() => new()
        {
            SF(1, "Expanded Spell List",
                "The following spells are added to the warlock spell list for you. 1st: Create or Destroy Water, Thunderwave; 2nd: Gust of Wind, Silence; 3rd: Lightning Bolt, Sleet Storm; 4th: Control Water, Summon Elemental (water only); 5th: Bigby's Hand, Cone of Cold."),
            SF(1, "Tentacle of the Deeps",
                "As a bonus action, create a 10-foot tentacle at a point within 60 feet for 1 minute. When you create it, you can make a melee spell attack against one creature within 10 feet of it (1d8 cold damage and −10 ft speed until your next turn; 2d8 at 10th level). As a bonus action, move the tentacle up to 30 feet and repeat the attack.",
                "PB times/long rest"),
            SF(1, "Gift of the Sea",
                "You gain a swimming speed of 40 feet, and you can breathe underwater."),
            SF(6, "Oceanic Soul",
                "You gain resistance to cold damage. When you are fully submerged, any creature that is also fully submerged can understand your speech, and you can understand theirs."),
            SF(6, "Guardian Coil",
                "When you or a creature you can see takes damage while within 10 feet of your tentacle, you can use your reaction to reduce that damage by 1d8 (2d8 at 10th level).",
                "Reaction"),
            SF(10, "Grasping Tentacles",
                "You learn Evard's Black Tentacles (warlock spell for you; doesn't count against spells known). You can cast it once without a spell slot per long rest. When you cast it, you gain temporary hit points equal to your warlock level, and damage can't break your concentration on this spell."),
            SF(14, "Fathomless Plunge",
                "As an action, teleport yourself and up to five willing creatures within 30 feet. You all reappear up to 1 mile away in or within 30 feet of a body of water you've seen (pond-sized or larger).",
                "1/short or long rest")
        };

        private static List<ClassFeature> BuildTheFiend() => new()
        {
            SF(1, "Expanded Spell List",
                "The following spells are added to the warlock spell list for you. 1st: Burning Hands, Command; 2nd: Blindness/Deafness, Scorching Ray; 3rd: Fireball, Stinking Cloud; 4th: Fire Shield, Wall of Fire; 5th: Flame Strike, Hallow."),
            SF(1, "Dark One's Blessing",
                "When you reduce a hostile creature to 0 hit points, you gain temporary hit points equal to your Charisma modifier + your warlock level (minimum of 1)."),
            SF(6, "Dark One's Own Luck",
                "When you make an ability check or saving throw, you can add a d10 to the roll (after rolling, before knowing success or failure).",
                "1/short or long rest"),
            SF(10, "Fiendish Resilience",
                "Choose one damage type when you finish a short or long rest. You gain resistance to that damage type until you choose a different one with this feature. Damage from magical weapons or silver weapons ignores this resistance."),
            SF(14, "Hurl Through Hell",
                "When you hit a creature with an attack, you can use this feature to instantly transport the target through the lower planes. The creature disappears and hurtles through a nightmare landscape. At the end of your next turn, it returns to the space it previously occupied or the nearest unoccupied space, having taken 10d10 psychic damage if it isn't a fiend.",
                "1/long rest")
        };

        private static List<ClassFeature> BuildTheGenie() => new()
        {
            SF(1, "Expanded Spell List",
                "Spells added to your warlock list (plus a spell by genie kind). Shared: 1st Detect Evil and Good; 2nd Phantasmal Force; 3rd Create Food and Water; 4th Phantasmal Killer; 5th Creation; 9th Wish. Dao: Sanctuary, Spike Growth, Stone Shape, Stoneskin, Wall of Stone. Djinni: Thunderwave, Gust of Wind, Wind Wall, Greater Invisibility, Seeming. Efreeti: Burning Hands, Scorching Ray, Fireball, Fire Shield, Flame Strike. Marid: Fog Cloud, Blur, Sleet Storm, Control Water, Cone of Cold."),
            SF(1, "Genie's Vessel",
                "Your patron gifts a Tiny vessel (spellcasting focus). Bottled Respite: as an action, enter the vessel's extradimensional space for hours equal to twice your proficiency bonus (1/long rest). Genie's Wrath: once per turn when you hit with an attack, deal extra damage equal to your proficiency bonus (bludgeoning/thunder/fire/cold by genie kind). Vessel AC = your spell save DC; HP = warlock level + PB."),
            SF(6, "Elemental Gift",
                "Gain resistance to the damage type of your genie kind (bludgeoning dao, thunder djinni, fire efreeti, cold marid). As a bonus action, gain a flying speed of 30 feet that lasts 10 minutes (you can hover).",
                "PB times/long rest (flight)"),
            SF(10, "Sanctuary Vessel",
                "When you enter your vessel, you can bring up to five willing creatures within 30 feet. Anyone who remains inside for at least 10 minutes gains the benefits of a short rest, and can add your proficiency bonus to Hit Dice HP recovered there."),
            SF(14, "Limited Wish",
                "As an action, speak a desire to your vessel to produce the effect of one spell of 6th level or lower that has a casting time of 1 action (any class list; no components required). Once used, you can't use it again until you finish 1d4 long rests.",
                "1 per 1d4 long rests")
        };

        private static List<ClassFeature> BuildTheGreatOldOne() => new()
        {
            SF(1, "Expanded Spell List",
                "The following spells are added to the warlock spell list for you. 1st: Dissonant Whispers, Tasha's Hideous Laughter; 2nd: Detect Thoughts, Phantasmal Force; 3rd: Clairvoyance, Sending; 4th: Dominate Beast, Evard's Black Tentacles; 5th: Dominate Person, Telekinesis."),
            SF(1, "Awakened Mind",
                "You can telepathically speak to any creature you can see within 30 feet of you. You don't need to share a language, but the creature must be able to understand at least one language."),
            SF(6, "Entropic Ward",
                "When a creature makes an attack roll against you, you can use your reaction to impose disadvantage on that roll. If the attack misses you, your next attack roll against the creature has advantage if you make it before the end of your next turn.",
                "1/short or long rest"),
            SF(10, "Thought Shield",
                "Your thoughts can't be read by telepathy or other means unless you allow it. You have resistance to psychic damage, and whenever a creature deals psychic damage to you, that creature takes the same amount of damage that you do."),
            SF(14, "Create Thrall",
                "You can use your action to touch an incapacitated humanoid. That creature is then charmed by you until a Remove Curse spell is cast on it, the charmed condition is removed, or you use this feature again. While charmed, you can communicate telepathically with it as long as you are on the same plane.")
        };

        private static List<ClassFeature> BuildTheHexblade() => new()
        {
            SF(1, "Expanded Spell List",
                "The following spells are added to the warlock spell list for you. 1st: Shield, Wrathful Smite; 2nd: Blur, Branding Smite; 3rd: Blink, Elemental Weapon; 4th: Phantasmal Killer, Staggering Smite; 5th: Banishing Smite, Cone of Cold."),
            SF(1, "Hexblade's Curse",
                "As a bonus action, place a curse on a creature you can see within 30 feet for 1 minute. Bonus to damage rolls against the cursed target equal to your proficiency bonus; critical hits against it on a 19–20; if it dies, you regain hit points equal to your warlock level + Charisma modifier.",
                "1/short or long rest"),
            SF(1, "Hex Warrior",
                "Gain proficiency with medium armor, shields, and martial weapons. At the end of a long rest, touch one weapon lacking the two-handed property; while you are bonded and wielding it, you can use Charisma for attack and damage rolls. Pact of the Blade weapons automatically qualify (including two-handed)."),
            SF(6, "Accursed Specter",
                "When you slay a humanoid, you can cause its spirit to rise as a specter under your control. Roll initiative for it; it obeys your verbal commands. It gains temporary hit points equal to half your warlock level and adds your Charisma modifier to its attack rolls. It remains until it drops to 0 HP or you finish a long rest.",
                "1/long rest"),
            SF(10, "Armor of Hexes",
                "If the target cursed by your Hexblade's Curse hits you with an attack roll, you can use your reaction to roll a d6. On a 4 or higher, the attack instead misses you, regardless of its roll.",
                "Reaction"),
            SF(14, "Master of Hexes",
                "When the creature cursed by your Hexblade's Curse dies, you can apply the curse to a different creature you can see within 30 feet (no HP regain from that transition). You can't do so while incapacitated.")
        };

        private static List<ClassFeature> BuildTheUndead() => new()
        {
            SF(1, "Expanded Spell List",
                "The following spells are added to the warlock spell list for you. 1st: Bane, False Life; 2nd: Blindness/Deafness, Phantasmal Force; 3rd: Speak with Dead, Phantom Steed; 4th: Death Ward, Greater Invisibility; 5th: Antilife Shell, Cloudkill."),
            SF(1, "Form of Dread",
                "As a bonus action, transform for 1 minute: gain temporary hit points equal to 1d10 + your warlock level; once per turn when you hit a creature, force a Wisdom save or frighten it until the end of your next turn; you are immune to the frightened condition.",
                "PB times/long rest"),
            SF(6, "Grave Touched",
                "You don't need to eat, drink, or breathe. Once per turn when you hit with an attack and roll damage, you can replace the damage type with necrotic. While in your Form of Dread, once per turn you can roll one extra damage die when you deal necrotic damage with an attack."),
            SF(10, "Necrotic Husk",
                "You have resistance to necrotic damage. If you are reduced to 0 hit points, you can cause your body to explode: each creature within 30 feet takes 2d10 + warlock level necrotic damage, then you regain hit points equal to your hit point maximum and gain 1 level of exhaustion (1/long rest; form ends). While in Form of Dread you are immune to necrotic damage."),
            SF(14, "Spirit Projection",
                "As an action, project your spirit from your body for 1 hour or until you end it (concentration, incapacitation, or body reduced to 0 HP). Your body is unconscious and can't move. Your spirit has resistance to bludgeoning, piercing, and slashing damage; fly speed equal to your walking speed (hover); can move through creatures and objects as difficult terrain.",
                "1/long rest")
        };

        private static List<ClassFeature> BuildTheUndying() => new()
        {
            SF(1, "Expanded Spell List",
                "The following spells are added to the warlock spell list for you. 1st: False Life, Ray of Sickness; 2nd: Blindness/Deafness, Silence; 3rd: Feign Death, Speak with Dead; 4th: Aura of Life, Death Ward; 5th: Contagion, Legend Lore."),
            SF(1, "Among the Dead",
                "Learn Spare the Dying (warlock cantrip for you). Advantage on saving throws against disease. If an undead targets you directly with an attack or harmful spell, it must make a Wisdom save against your spell save DC or choose a new target (or waste the attack/spell). On a success, that undead is immune to this effect for 24 hours. Also immune for 24 hours if you target it with an attack or harmful spell."),
            SF(6, "Defy Death",
                "You can regain hit points equal to 1d8 + your Constitution modifier (minimum 1) when you succeed on a death saving throw or stabilize a creature with Spare the Dying.",
                "1/long rest"),
            SF(10, "Undying Nature",
                "You can hold your breath indefinitely, and you don't require food, water, or sleep (you still need rest to reduce exhaustion and to gain short/long rest benefits). You age more slowly: for every 10 years that pass, your body ages only 1 year, and you are immune to being magically aged."),
            SF(14, "Indestructible Life",
                "As a bonus action on your turn, regain hit points equal to 1d8 + your warlock level. If you reattach a severed body part when you use this feature, the part reattaches.",
                "1/short or long rest")
        };

        // ═══════════════════════════════════════════════════════════════════
        // REGISTRATION
        // ═══════════════════════════════════════════════════════════════════

        private static void AddClericDruidSorcererWarlock(Dictionary<string, List<ClassFeature>> d)
        {
            // Cleric domains (short keys)
            d["Arcana"] = BuildArcanaDomain();
            d["Death"] = BuildDeathDomain();
            d["Forge"] = BuildForgeDomain();
            d["Grave"] = BuildGraveDomain();
            d["Knowledge"] = BuildKnowledgeDomain();
            d["Life"] = BuildLifeDomain();
            d["Light"] = BuildLightDomain();
            d["Nature"] = BuildNatureDomain();
            d["Order"] = BuildOrderDomain();
            d["Peace"] = BuildPeaceDomain();
            d["Tempest"] = BuildTempestDomain();
            d["Trickery"] = BuildTrickeryDomain();
            d["Twilight"] = BuildTwilightDomain();
            d["War"] = BuildWarDomain();

            // Druid circles
            d["Circle of Dreams"] = BuildCircleOfDreams();
            d["Circle of Spores"] = BuildCircleOfSpores();
            d["Circle of Stars"] = BuildCircleOfStars();
            d["Circle of the Land"] = BuildCircleOfTheLand();
            d["Circle of the Moon"] = BuildCircleOfTheMoon();
            d["Circle of the Shepherd"] = BuildCircleOfTheShepherd();
            d["Circle of Wildfire"] = BuildCircleOfWildfire();

            // Sorcerer origins
            d["Aberrant Mind"] = BuildAberrantMind();
            d["Clockwork Soul"] = BuildClockworkSoul();
            d["Divine Soul"] = BuildDivineSoul();
            d["Draconic Bloodline"] = BuildDraconicBloodline();
            d["Lunar"] = BuildLunarSorcery();
            d["Shadow"] = BuildShadowMagic();
            d["Storm"] = BuildStormSorcery();
            d["Wild Magic"] = BuildWildMagic();

            // Warlock patrons
            d["The Archfey"] = BuildTheArchfey();
            d["The Celestial"] = BuildTheCelestial();
            d["The Fathomless"] = BuildTheFathomless();
            d["The Fiend"] = BuildTheFiend();
            d["The Genie"] = BuildTheGenie();
            d["The Great Old One"] = BuildTheGreatOldOne();
            d["The Hexblade"] = BuildTheHexblade();
            d["The Undead"] = BuildTheUndead();
            d["The Undying"] = BuildTheUndying();
        }
    }
}
