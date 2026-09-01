using System.Collections.Generic;

namespace Nemo
{
    /// <summary>
    /// Subclass progression for Fighter archetypes, Monk traditions, and Paladin oaths.
    /// Reference: https://dnd5e.wikidot.com/
    /// </summary>
    public static partial class GameData
    {
        private static void AddFighterMonkPaladin(Dictionary<string, List<ClassFeature>> d)
        {
            // ── Fighter (3, 7, 10, 15, 18) ──
            d["Arcane Archer"] = new List<ClassFeature>
            {
                SF(3, "Arcane Archer Lore",
                    "You learn Prestidigitation or Druidcraft. You also gain proficiency in Arcana or Nature (your choice)."),
                SF(3, "Arcane Shot",
                    "When you fire a nonmagical arrow, you can apply one Arcane Shot option (Banishing, Beguiling, Bursting, Enfeebling, Grasping, Piercing, Seeking, or Shadow Arrow). " +
                    "2 uses per short or long rest (3 at 7th, 4 at 15th). Learn two options at 3rd; one more at 7th, 10th, 15th, and 18th.",
                    "2/short rest (scales)"),
                SF(7, "Magic Arrow",
                    "Whenever you fire a nonmagical arrow from a shortbow or longbow, you can make it magical for the purpose of overcoming resistance and immunity to nonmagical attacks and damage."),
                SF(7, "Curving Shot",
                    "When you miss with a magic arrow, you can use a bonus action to reroll the attack roll against a different target within 60 feet of the original."),
                SF(10, "Arcane Shot Improvement",
                    "You learn an additional Arcane Shot option. Some options improve at higher levels as noted in their descriptions."),
                SF(15, "Ever-Ready Shot",
                    "If you roll initiative and have no uses of Arcane Shot remaining, you regain one use."),
                SF(18, "Arcane Shot Mastery",
                    "You learn an additional Arcane Shot option. Your Arcane Shot damage dice increase where the option notes (typically from 2d6 to 4d6)."),
            };

            d["Banneret"] = new List<ClassFeature>
            {
                SF(3, "Rallying Cry",
                    "When you use Second Wind, you can choose up to three creatures within 60 feet that can see or hear you. Each regains hit points equal to your fighter level."),
                SF(7, "Royal Envoy",
                    "You gain proficiency in Persuasion (or another skill if already proficient). Your proficiency bonus is doubled for Persuasion checks."),
                SF(10, "Inspiring Surge",
                    "When you use Action Surge, you can choose one ally within 60 feet that can see or hear you. That ally can make one melee or ranged weapon attack as a reaction. " +
                    "At 18th level, you can choose two allies."),
                SF(15, "Bulwark",
                    "When you use Indomitable to reroll a saving throw and it is not a death save, you can choose an ally within 60 feet that also failed the same save. They can reroll it and must use the new roll."),
                SF(18, "Inspiring Surge (2 allies)",
                    "When you use Action Surge, you can grant the reaction weapon attack to two allies instead of one."),
            };

            d["Battle Master"] = new List<ClassFeature>
            {
                SF(3, "Combat Superiority",
                    "You learn three maneuvers of your choice and gain four superiority dice (d8). A die is expended when you use a maneuver; regain all on a short or long rest. " +
                    "Save DC = 8 + proficiency bonus + Strength or Dexterity modifier (your choice). Learn two more maneuvers at 7th and 10th, and one more at 15th.",
                    "4 superiority dice (scale)"),
                SF(3, "Student of War",
                    "You gain proficiency with one type of artisan's tools of your choice."),
                SF(7, "Know Your Enemy",
                    "If you spend at least 1 minute observing or interacting with a creature outside combat, you learn whether it is equal, superior, or inferior to you in two of: Strength, Dexterity, Constitution, AC, current HP, total class levels (if any), or fighter levels (if any).",
                    "1/short or long rest observation"),
                SF(10, "Improved Combat Superiority",
                    "Your superiority dice turn into d10s. You learn two additional maneuvers."),
                SF(15, "Relentless",
                    "When you roll initiative and have no superiority dice remaining, you regain one superiority die."),
                SF(18, "Improved Combat Superiority (d12)",
                    "Your superiority dice turn into d12s."),
            };

            d["Cavalier"] = new List<ClassFeature>
            {
                SF(3, "Bonus Proficiency",
                    "You gain proficiency in one of Animal Handling, History, Insight, Performance, or Persuasion. Alternatively, learn one language of your choice."),
                SF(3, "Born to the Saddle",
                    "Advantage on saving throws to avoid falling off your mount. Mounting or dismounting costs only 5 feet of movement. If you fall off and drop no more than 10 feet, you land on your feet if not incapacitated."),
                SF(3, "Unwavering Mark",
                    "When you hit a creature with a melee weapon attack, you can mark it until the end of your next turn. While marked and within 5 feet of you, it has disadvantage on attacks against creatures other than you. " +
                    "If it deals damage to someone else, you can make a special melee weapon attack against it as a bonus action on your next turn (extra damage = half fighter level). Uses = Strength modifier (min 1) per long rest.",
                    "Str mod/long rest"),
                SF(7, "Warding Maneuver",
                    "If you or a creature you can see within 5 feet is hit by an attack while you wield a melee weapon or shield, use your reaction to add 1d8 to the target's AC against that attack. " +
                    "If the attack still hits, the target has resistance against that attack's damage. Uses = Constitution modifier (min 1) per long rest.",
                    "Con mod/long rest"),
                SF(10, "Hold the Line",
                    "Creatures provoke an opportunity attack from you when they move 5 feet or more while within your reach. On a hit, their speed becomes 0 until the end of the current turn."),
                SF(15, "Ferocious Charger",
                    "If you move at least 10 feet in a straight line toward a target before hitting it with a melee weapon attack as part of the Attack action, you can attempt to knock it prone (Strength save). Once per turn."),
                SF(18, "Vigilant Defender",
                    "You can make opportunity attacks without using your reaction. You still only get one reaction per round for other reaction options."),
            };

            d["Champion"] = new List<ClassFeature>
            {
                SF(3, "Improved Critical",
                    "Your weapon attacks score a critical hit on a roll of 19 or 20."),
                SF(7, "Remarkable Athlete",
                    "Add half your proficiency bonus (rounded up) to any Strength, Dexterity, or Constitution check you make that doesn't already use your proficiency bonus. " +
                    "When you make a running long jump, the distance increases by a number of feet equal to your Strength modifier."),
                SF(10, "Additional Fighting Style",
                    "You can choose a second option from the Fighting Style class feature."),
                SF(15, "Superior Critical",
                    "Your weapon attacks score a critical hit on a roll of 18–20."),
                SF(18, "Survivor",
                    "At the start of each of your turns, you regain hit points equal to 5 + your Constitution modifier if you have no more than half of your hit points left and are not at 0 hit points."),
            };

            d["Echo Knight"] = new List<ClassFeature>
            {
                SF(3, "Manifest Echo",
                    "As a bonus action, create a magical, translucent echo of yourself in an unoccupied space within 15 feet (AC = 14 + PB, 1 HP, immune to conditions). " +
                    "Bonus action to teleport swap (swap places if within 30 feet) or to move the echo up to 30 feet. Attacks can originate from you or the echo. Opportunity attacks from either position.",
                    "Bonus action"),
                SF(3, "Unleash Incarnation",
                    "When you take the Attack action, you can make one additional melee attack from your echo's position.",
                    "Con mod times/long rest (min 1)"),
                SF(7, "Echo Avatar",
                    "As an action, see and hear through your echo for up to 10 minutes (deaf and blind to your own senses). Echo can be up to 1,000 feet away.",
                    "Action"),
                SF(10, "Shadow Martyr",
                    "When a creature you can see within 30 feet is attacked, use your reaction to teleport your echo to an unoccupied space within 5 feet of the target. The attack targets the echo instead.",
                    "1/short or long rest"),
                SF(15, "Reclaim Potential",
                    "When your echo is destroyed by taking damage, you gain temporary hit points equal to 2d6 + Constitution modifier (if you don't already have temp HP)."),
                SF(18, "Legion of One",
                    "You can create two echoes with Manifest Echo, and can make two attacks with Unleash Incarnation when you take the Attack action. You can use Unleash Incarnation freely when you roll initiative with no uses left (regains one use)."),
            };

            d["Eldritch Knight"] = new List<ClassFeature>
            {
                SF(3, "Spellcasting",
                    "You learn wizard cantrips and spells (primarily abjuration and evocation). Intelligence is your spellcasting ability. " +
                    "2 cantrips at 3rd (3 at 10th). Spells known and slots follow the Eldritch Knight table (third-caster progression)."),
                SF(3, "Weapon Bond",
                    "Over 1 hour, bond with a weapon. You can't be disarmed of it unless incapacitated. You can summon it to your hand as a bonus action if on the same plane. Up to two bonded weapons.",
                    "Bonus action to summon"),
                SF(7, "War Magic",
                    "When you use your action to cast a cantrip, you can make one weapon attack as a bonus action."),
                SF(10, "Eldritch Strike",
                    "When you hit a creature with a weapon attack, that creature has disadvantage on the next saving throw it makes against a spell you cast before the end of your next turn."),
                SF(15, "Arcane Charge",
                    "When you use Action Surge, you can teleport up to 30 feet to an unoccupied space you can see (before or after the extra action)."),
                SF(18, "Improved War Magic",
                    "When you use your action to cast a spell, you can make one weapon attack as a bonus action."),
            };

            d["Psi Warrior"] = new List<ClassFeature>
            {
                SF(3, "Psionic Power",
                    "You have a pool of Psionic Energy dice (d6; number = twice proficiency bonus). Regain one on a short rest (all on long rest). " +
                    "Protective Field (reaction, reduce damage by die + Int mod), Psionic Strike (extra force damage once per turn after a weapon hit), Telekinetic Movement (move a large object or willing creature).",
                    "Psionic Energy dice"),
                SF(7, "Telekinetic Adept",
                    "Psi-Powered Leap: as a bonus action, jump high/long equal to twice your walking speed (PB times/long rest). Telekinetic Thrust: when you deal Psionic Strike damage, force a Strength save or shove 10 feet / knock prone."),
                SF(10, "Guarded Mind",
                    "You have resistance to psychic damage. If you start your turn charmed or frightened, you can expend a Psionic Energy die and end both conditions."),
                SF(15, "Bulwark of Force",
                    "As a bonus action, create a 10-foot telekinetic barrier for 1 minute granting half cover to you and chosen creatures in the area (doesn't require concentration).",
                    "1/long rest (or expend a Psionic Energy die)"),
                SF(18, "Telekinetic Master",
                    "You can cast Telekinesis without components (Intelligence). When you cast it this way, you can make one weapon attack as a bonus action on each of your turns while concentrating. " +
                    "Once free per long rest; then by expending a Psionic Energy die.",
                    "1/long rest or Psionic die"),
            };

            d["Rune Knight"] = new List<ClassFeature>
            {
                SF(3, "Bonus Proficiencies",
                    "You gain proficiency with smith's tools, and you learn to speak, read, and write Giant."),
                SF(3, "Rune Carver",
                    "You learn two runes (Cloud, Fire, Frost, Stone, Hill, Storm, etc.). Invoke each once per short or long rest for magical benefits. Learn additional runes at 7th and 10th; each rune's passive and active effects are as published.",
                    "1 invoke each/short rest"),
                SF(3, "Giant's Might",
                    "As a bonus action, become Large (if space allows) for 1 minute: advantage on Strength checks and saves, and once per turn extra 1d6 damage on a weapon or unarmed hit (1d8 at 10th, 1d10 at 18th).",
                    "PB times/long rest"),
                SF(7, "Runic Shield",
                    "When another creature you can see within 60 feet is hit by an attack roll, you can use your reaction to force the attacker to reroll and use the new roll.",
                    "PB times/long rest"),
                SF(10, "Great Stature",
                    "When you use Giant's Might, your size can become Huge. The extra damage die increases to 1d8. Your height increases by 3d4 inches permanently."),
                SF(15, "Master of Runes",
                    "You can invoke each rune twice per short or long rest (instead of once)."),
                SF(18, "Runic Juggernaut",
                    "Giant's Might extra damage becomes 1d10. Your size can become Huge, and your reach increases by 5 feet while the feature is active."),
            };

            d["Samurai"] = new List<ClassFeature>
            {
                SF(3, "Bonus Proficiency",
                    "You gain proficiency in one of History, Insight, Performance, or Persuasion. Alternatively, learn one language of your choice."),
                SF(3, "Fighting Spirit",
                    "As a bonus action, give yourself advantage on weapon attack rolls until the end of the current turn, and gain 5 temporary hit points (10 at 10th, 15 at 15th).",
                    "3/long rest"),
                SF(7, "Elegant Courtier",
                    "Add your Wisdom modifier to any Charisma (Persuasion) check. You also gain proficiency in Wisdom saving throws (or, if already proficient, another saving throw of your choice)."),
                SF(10, "Tireless Spirit",
                    "When you roll initiative and have no uses of Fighting Spirit remaining, you regain one use."),
                SF(15, "Rapid Strike",
                    "If you have advantage on a weapon attack against a target on your turn, you can forgo advantage to make one additional weapon attack against that target as part of the same action. Once per turn."),
                SF(18, "Strength before Death",
                    "If you take damage that reduces you to 0 hit points and doesn't kill you outright, you can use your reaction to delay falling unconscious and immediately take an extra turn. " +
                    "You die only if still at 0 HP after that turn, and attacks against you have advantage during it. Once per long rest.",
                    "1/long rest"),
            };

            // ── Monk (3, 6, 11, 17) ──
            d["Way of Mercy"] = new List<ClassFeature>
            {
                SF(3, "Implements of Mercy",
                    "You gain proficiency in Insight and Medicine, and with herbalism kit. You also gain a special mask that you often wear when using the features of this tradition."),
                SF(3, "Hand of Healing",
                    "As an action, spend 1 ki to touch a creature and restore hit points equal to a Martial Arts die + Wisdom modifier. As part of Flurry of Blows, replace one unarmed strike with Hand of Healing (no extra ki).",
                    "1 ki"),
                SF(3, "Hand of Harm",
                    "When you hit with an unarmed strike, spend 1 ki to deal extra necrotic damage equal to a Martial Arts die + Wisdom modifier (once per turn).",
                    "1 ki (1/turn)"),
                SF(6, "Physician's Touch",
                    "Hand of Healing can also end one disease or the blinded, deafened, paralyzed, poisoned, or stunned condition. Hand of Harm can also impose the poisoned condition until the end of your next turn."),
                SF(11, "Flurry of Healing and Harm",
                    "When you use Flurry of Blows, you can replace each unarmed strike with Hand of Healing (no extra ki per strike). You can also use Hand of Harm without spending ki a number of times equal to your Wisdom modifier per long rest (min 1)."),
                SF(17, "Hand of Ultimate Mercy",
                    "As an action, spend 5 ki to touch a creature that died within the last 24 hours and return it to life with 4d10 + Wisdom modifier hit points. Once per long rest. Removes some conditions if the creature is living.",
                    "5 ki; 1/long rest"),
            };

            d["Way of Shadow"] = new List<ClassFeature>
            {
                SF(3, "Shadow Arts",
                    "You can spend 2 ki points to cast Darkness, Darkvision, Pass without Trace, or Silence (no material components). You also gain the Minor Illusion cantrip if you don't already know it.",
                    "2 ki"),
                SF(6, "Shadow Step",
                    "When you are in dim light or darkness, as a bonus action teleport up to 60 feet to an unoccupied space you can see that is also in dim light or darkness. You then have advantage on the first melee attack before the end of the turn.",
                    "Bonus action"),
                SF(11, "Cloak of Shadows",
                    "When you are in an area of dim light or darkness, you can use your action to become invisible until you make an attack, cast a spell, or are in bright light."),
                SF(17, "Opportunist",
                    "When a creature within 5 feet of you is hit by an attack made by a creature other than you, you can use your reaction to make a melee attack against that creature.",
                    "Reaction"),
            };

            d["Way of the Ascendant Dragon"] = new List<ClassFeature>
            {
                SF(3, "Draconic Disciple",
                    "You learn Draconic (or another language). When you hit with an unarmed strike, you can change its damage type to acid, cold, fire, lightning, or poison. " +
                    "You can also make a special Wisdom (Intimidation) check adding your Wisdom modifier when trying to impress or intimidate."),
                SF(3, "Breath of the Dragon",
                    "As an action, or as part of one attack when you use Flurry of Blows, create a 20-foot cone or 30-foot line of elemental damage (Martial Arts die, scales). Dex save for half. " +
                    "Uses = proficiency bonus per long rest, then 2 ki thereafter.",
                    "PB/long rest then 2 ki"),
                SF(6, "Wings Unfurled",
                    "When you use Step of the Wind, you can unfurl spectral dragon wings and gain a flying speed equal to your walking speed until the end of the turn. " +
                    "Uses = proficiency bonus per long rest, then normal Step of the Wind ki.",
                    "PB/long rest free with Step of the Wind"),
                SF(11, "Aspect of the Wyrm",
                    "As a bonus action, create a 10-foot aura for 1 minute: resistance to one of acid/cold/fire/lightning/poison, and when a creature in the aura hits you or an ally, reaction to deal Martial Arts die damage of that type. " +
                    "Once per long rest free; then 3 ki.",
                    "1/long rest or 3 ki"),
                SF(17, "Ascendant Aspect",
                    "You gain blindsight 10 feet. When you use Aspect of the Wyrm, allies in the aura also gain the resistance, and once per turn on a hit with an unarmed strike you can force a Strength save or push the target 20 feet and knock it prone. " +
                    "Breath of the Dragon damage increases by one Martial Arts die."),
            };

            d["Way of the Astral Self"] = new List<ClassFeature>
            {
                SF(3, "Arms of the Astral Self",
                    "As a bonus action, spend 1 ki to summon spectral arms for 10 minutes. You can use Wisdom instead of Strength for Athletics and for unarmed attack and damage rolls. " +
                    "Reach +5 feet with unarmed strikes; damage type force (or radiant/necrotic). When you attack with them on your turn, once you can make an extra unarmed strike as part of the Attack action.",
                    "1 ki"),
                SF(6, "Visage of the Astral Self",
                    "As a bonus action (or when summoning arms), spend 1 ki to summon a spectral visage for 10 minutes: see in darkness (including magical) to 120 feet; as a bonus action word of force or silence in a 30-foot cone (Wis save or deafened/disadvantage on Wisdom checks for 1 minute).",
                    "1 ki"),
                SF(11, "Body of the Astral Self",
                    "When you have both arms and visage active, you can also summon the body (included when you spend ki for arms/visage together for 1 minute cost variant per Tasha's). " +
                    "Deflect Energy: when you take acid, cold, fire, force, lightning, or thunder damage, use reaction to deflect as Deflect Missiles but for that damage. Empowered Arms: once per turn extra damage = Martial Arts die on an arms hit."),
                SF(17, "Awakened Astral Self",
                    "As a bonus action, spend 5 ki to summon arms, visage, and body for 10 minutes. Gain +2 AC, and on Flurry of Blows you can make three unarmed strikes instead of two (with the astral arms benefits).",
                    "5 ki"),
            };

            d["Way of the Drunken Master"] = new List<ClassFeature>
            {
                SF(3, "Bonus Proficiencies",
                    "You gain proficiency in Performance and with brewer's supplies."),
                SF(3, "Drunken Technique",
                    "Whenever you use Flurry of Blows, you gain the benefit of the Disengage action, and your walking speed increases by 10 feet until the end of the current turn."),
                SF(6, "Tipsy Sway",
                    "Leap to Your Feet: standing from prone costs only 5 feet of movement. Redirect Attack: when a creature misses you with a melee attack roll, you can spend 1 ki as a reaction to cause that attack to hit one creature of your choice within 5 feet other than the attacker.",
                    "1 ki (Redirect)"),
                SF(11, "Drunkard's Luck",
                    "When you make an ability check, attack roll, or saving throw with disadvantage, you can spend 2 ki to cancel the disadvantage for that roll.",
                    "2 ki"),
                SF(17, "Intoxicated Frenzy",
                    "When you use Flurry of Blows, you can make up to three additional unarmed strikes with it (five total), provided each extra strike targets a different creature than the previous strike this turn."),
            };

            d["Way of the Four Elements"] = new List<ClassFeature>
            {
                SF(3, "Disciple of the Elements",
                    "You learn elemental disciplines that spend ki to create magical effects (Elemental Attunement plus others such as Fangs of the Fire Snake, Fist of Four Thunders, Shape the Flowing River, Water Whip). " +
                    "Learn additional disciplines at 6th, 11th, and 17th. Some require minimum monk levels. Cast-as-spell disciplines use your ki save DC."),
                SF(6, "Extra Elemental Discipline",
                    "You learn one additional elemental discipline of your choice (meeting any level prerequisite)."),
                SF(11, "Extra Elemental Discipline",
                    "You learn one additional elemental discipline of your choice (meeting any level prerequisite)."),
                SF(17, "Extra Elemental Discipline",
                    "You learn one additional elemental discipline of your choice (meeting any level prerequisite). High-level options include Wave of Rolling Earth, Flames of the Phoenix, and Eternal Mountain Defense."),
            };

            d["Way of the Kensei"] = new List<ClassFeature>
            {
                SF(3, "Path of the Kensei",
                    "Choose two kensei weapons (melee simple/martial not heavy/special; ranged simple/martial not heavy). They are monk weapons for you. " +
                    "Agile Parry: if you make an unarmed strike while holding a kensei melee weapon and take the Attack action, +2 AC until the start of your next turn if still holding it. " +
                    "Kensei's Shot: bonus action to deal +1d4 damage with kensei ranged weapon that turn. Gain proficiency with calligrapher's or painter's supplies."),
                SF(6, "One with the Blade",
                    "Your attacks with kensei weapons count as magical. Deft Strike: once per turn when you hit with a kensei weapon, spend 1 ki to deal extra Martial Arts die damage.",
                    "1 ki (Deft Strike)"),
                SF(11, "Sharpen the Blade",
                    "As a bonus action, spend up to 3 ki to grant a kensei weapon a bonus to attack and damage rolls equal to the ki spent (1–3) for 1 minute. Magic weapons that already grant a bonus are unaffected.",
                    "1–3 ki"),
                SF(17, "Unerring Accuracy",
                    "If you miss with an attack roll using a monk weapon on your turn, you can reroll it. You can use this feature only once on each of your turns."),
            };

            d["Way of the Long Death"] = new List<ClassFeature>
            {
                SF(3, "Touch of Death",
                    "When you reduce a creature within 5 feet of you to 0 hit points, you gain temporary hit points equal to your Wisdom modifier + your monk level (minimum 1)."),
                SF(6, "Hour of Reaping",
                    "As an action, each creature within 30 feet of you that can see you must succeed on a Wisdom saving throw or be frightened of you until the end of your next turn.",
                    "Action"),
                SF(11, "Mastery of Death",
                    "When you are reduced to 0 hit points, you can expend 1 ki point (no action) to drop to 1 hit point instead.",
                    "1 ki"),
                SF(17, "Touch of the Long Death",
                    "As an action, touch one creature within 5 feet and spend 1 to 10 ki points. The target makes a Constitution saving throw. It takes 2d10 necrotic damage per ki spent on a failed save, or half as much on a successful one.",
                    "1–10 ki"),
            };

            d["Way of the Open Hand"] = new List<ClassFeature>
            {
                SF(3, "Open Hand Technique",
                    "Whenever you hit a creature with one of the attacks granted by Flurry of Blows, you can impose one effect: it must succeed on a Dexterity save or be knocked prone; " +
                    "a Strength save or be pushed up to 15 feet away; or it can't take reactions until the end of your next turn."),
                SF(6, "Wholeness of Body",
                    "As an action, regain hit points equal to three times your monk level. Once you use this feature, you must finish a long rest before you can use it again.",
                    "1/long rest"),
                SF(11, "Tranquility",
                    "At the end of a long rest, you gain the effect of a Sanctuary spell (save DC = ki save DC) that lasts until the start of your next long rest (ends early as the spell)."),
                SF(17, "Quivering Palm",
                    "When you hit a creature with an unarmed strike, you can spend 3 ki points to start imperceptible vibrations lasting a number of days equal to your monk level. " +
                    "End them as an action: Constitution save or drop to 0 hit points (half your monk level d10 necrotic on success). Only one creature at a time.",
                    "3 ki"),
            };

            d["Way of the Sun Soul"] = new List<ClassFeature>
            {
                SF(3, "Radiant Sun Bolt",
                    "You gain a ranged spell attack (range 30 feet) that you can use with the Attack action: radiant damage equal to your Martial Arts die + Dexterity modifier. " +
                    "When you take the Attack action and only make this special attack, you can spend 1 ki to make two additional as a bonus action (as Flurry of Blows).",
                    "1 ki for bonus bolts"),
                SF(6, "Searing Arc Strike",
                    "Immediately after you take the Attack action on your turn, you can spend 2 ki to cast Burning Hands as a bonus action (radiant damage instead of fire). " +
                    "You can spend extra ki to cast it at higher levels (max = proficiency bonus).",
                    "2+ ki"),
                SF(11, "Searing Sunburst",
                    "As an action, create a brilliant flash in a 20-foot radius centered on a point within 150 feet. Creatures in the area make a Constitution save or take 2d6 radiant (half on success). " +
                    "You can spend ki to increase damage by 2d6 per ki (max 3 ki). Creatures with total cover are unaffected.",
                    "Action; optional ki"),
                SF(17, "Sun Shield",
                    "You shed bright light in a 30-foot radius and dim light for an additional 30 feet (toggle with free action). " +
                    "You have resistance to radiant damage. As a reaction when hit by a melee attack, deal radiant damage equal to 5 + Wisdom modifier to the attacker."),
            };

            // ── Paladin (3, 7, 15, 20) ──
            d["Oath of Conquest"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Armor of Agathys, Command; 5th — Hold Person, Spiritual Weapon; 9th — Bestow Curse, Fear; 13th — Dominate Beast, Stoneskin; 17th — Cloudkill, Dominate Person."),
                SF(3, "Channel Divinity: Conquering Presence",
                    "As an action, each creature of your choice within 30 feet that can see or hear you must succeed on a Wisdom save or be frightened of you for 1 minute (repeat save at end of turns).",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Guided Strike",
                    "When you make an attack roll, you can use Channel Divinity to gain a +10 bonus to the roll (after seeing the roll, before the outcome).",
                    "Channel Divinity"),
                SF(7, "Aura of Conquest",
                    "While conscious, creatures frightened by you within 10 feet have speed 0 and take psychic damage equal to half your paladin level if they start their turn there (30 feet at 18th)."),
                SF(15, "Scornful Rebuke",
                    "Whenever a creature hits you with an attack, it takes psychic damage equal to your Charisma modifier (minimum 1) if you're not incapacitated."),
                SF(20, "Invincible Conqueror",
                    "As an action, for 1 minute: resistance to all damage; when you take the Attack action, make one additional weapon attack; your melee weapon critical hits score on 19–20. Once per long rest.",
                    "1/long rest"),
            };

            d["Oath of Devotion"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Protection from Evil and Good, Sanctuary; 5th — Lesser Restoration, Zone of Truth; 9th — Beacon of Hope, Dispel Magic; 13th — Freedom of Movement, Guardian of Faith; 17th — Commune, Flame Strike."),
                SF(3, "Channel Divinity: Sacred Weapon",
                    "As an action, for 1 minute add your Charisma modifier to attack rolls with one weapon you are holding (min +1). It emits bright light 20 ft / dim 20 ft and is magical for the duration.",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Turn the Unholy",
                    "As an action, each fiend or undead within 30 feet that can see or hear you must succeed on a Wisdom save or be turned for 1 minute or until it takes damage.",
                    "Channel Divinity"),
                SF(7, "Aura of Devotion",
                    "You and friendly creatures within 10 feet can't be charmed while you are conscious (30 feet at 18th level)."),
                SF(15, "Purity of Spirit",
                    "You are always under the effects of a Protection from Evil and Good spell."),
                SF(20, "Holy Nimbus",
                    "As an action, for 1 minute emit bright sunlight in a 30-foot radius (dim 30 ft beyond). Enemy creatures starting their turn in the bright light take 10 radiant damage. " +
                    "Advantage on saves against spells cast by fiends or undead. Once per long rest.",
                    "1/long rest"),
            };

            d["Oath of Glory"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Guiding Bolt, Heroism; 5th — Enhance Ability, Magic Weapon; 9th — Haste, Protection from Energy; 13th — Compulsion, Freedom of Movement; 17th — Commune, Flame Strike."),
                SF(3, "Channel Divinity: Peerless Athlete",
                    "As a bonus action, for 10 minutes advantage on Strength (Athletics) and Dexterity (Acrobatics); carry/lift/push/pull capacity doubles; long and high jump distance increase by 10 feet.",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Inspiring Smite",
                    "Immediately after you deal damage with Divine Smite, distribute temporary hit points equal to 2d8 + paladin level among creatures of your choice within 30 feet (including you).",
                    "Channel Divinity"),
                SF(7, "Aura of Alacrity",
                    "Your walking speed increases by 10 feet. If a friendly creature starts its turn within 5 feet of you (10 feet at 18th), its speed increases by 10 feet until the end of its next turn."),
                SF(15, "Glorious Defense",
                    "When you or a creature you can see within 10 feet is hit by an attack, use your reaction to grant a bonus to AC equal to your Charisma modifier (min +1) against that attack. " +
                    "If it misses and the target is within 5 feet of you, you can make one weapon attack against the attacker as part of the reaction. Cha mod times/long rest.",
                    "Cha mod/long rest"),
                SF(20, "Living Legend",
                    "As a bonus action, for 1 minute: advantage on Charisma checks; once on each turn when you miss, you can cause that attack to hit instead; when you fail a saving throw, use reaction to reroll it. Once per long rest (or 5th-level slot).",
                    "1/long rest"),
            };

            d["Oath of Redemption"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Sanctuary, Sleep; 5th — Calm Emotions, Hold Person; 9th — Counterspell, Hypnotic Pattern; 13th — Otiluke's Resilient Sphere, Stoneskin; 17th — Hold Monster, Wall of Force."),
                SF(3, "Channel Divinity: Emissary of Peace",
                    "As a bonus action, gain a +5 bonus to the next Charisma (Persuasion) check you make within the next 10 minutes.",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Rebuke the Violent",
                    "When an attacker within 30 feet deals damage with an attack against a creature other than you, use your reaction to force a Wisdom save. On a failure, the attacker takes radiant damage equal to the damage it just dealt (half on success).",
                    "Channel Divinity"),
                SF(7, "Aura of the Guardian",
                    "When a creature within 10 feet of you (30 at 18th) takes damage, you can use your reaction to magically take that damage instead (not transferable further; immunities/resistances still apply to you)."),
                SF(15, "Protective Spirit",
                    "At the end of your turn if you are below half your hit point maximum, you regain hit points equal to 1d6 + half your paladin level."),
                SF(20, "Emissary of Redemption",
                    "You have resistance to all damage dealt by other creatures (not yourself). When a creature hits you, it takes radiant damage equal to half the damage you take from the attack. " +
                    "If you attack or deal damage to a creature (except with this feature) or force an enemy to make a save, you lose these benefits until you finish a long rest."),
            };

            d["Oath of Vengeance"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Bane, Hunter's Mark; 5th — Hold Person, Misty Step; 9th — Haste, Protection from Energy; 13th — Banishment, Dimension Door; 17th — Hold Monster, Scrying."),
                SF(3, "Channel Divinity: Abjure Enemy",
                    "As an action, one creature within 60 feet that can see or hear you must succeed on a Wisdom save or be frightened for 1 minute (speed 0 while frightened this way). Fiends and undead have disadvantage on the save.",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Vow of Enmity",
                    "As a bonus action, choose a creature you can see within 10 feet. You gain advantage on attack rolls against it for 1 minute or until it drops to 0 hit points or falls unconscious.",
                    "Channel Divinity"),
                SF(7, "Relentless Avenger",
                    "When you hit a creature with an opportunity attack, you can move up to half your speed immediately after as part of the same reaction. This movement doesn't provoke opportunity attacks."),
                SF(15, "Soul of Vengeance",
                    "When a creature under your Vow of Enmity makes an attack, you can use your reaction to make a melee weapon attack against it if it is within range.",
                    "Reaction"),
                SF(20, "Avenging Angel",
                    "As an action, for 1 hour: fly speed 60 feet; emanate a 30-foot aura of menace. Enemies starting their turn or entering the aura for the first time on a turn must succeed on a Wisdom save or be frightened and have attacks against them with advantage while frightened this way. Once per long rest.",
                    "1/long rest"),
            };

            d["Oath of the Ancients"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Ensnaring Strike, Speak with Animals; 5th — Moonbeam, Misty Step; 9th — Plant Growth, Protection from Energy; 13th — Ice Storm, Stoneskin; 17th — Commune with Nature, Tree Stride."),
                SF(3, "Channel Divinity: Nature's Wrath",
                    "As an action, spectral vines grapple a creature you can see within 10 feet (Strength or Dexterity save to avoid). While grappled, the creature is restrained. It can repeat the save at the end of each of its turns.",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Turn the Faithless",
                    "As an action, each fey or fiend within 30 feet that can see or hear you must succeed on a Wisdom save or be turned for 1 minute or until it takes damage.",
                    "Channel Divinity"),
                SF(7, "Aura of Warding",
                    "You and friendly creatures within 10 feet have resistance to damage from spells (30 feet at 18th)."),
                SF(15, "Undying Sentinel",
                    "When you are reduced to 0 hit points and not killed outright, you can drop to 1 hit point instead. Once per long rest. You also suffer none of the frailty of old age and can't be aged magically.",
                    "1/long rest"),
                SF(20, "Elder Champion",
                    "As an action, for 1 minute: regrow 10 hit points at the start of each of your turns; cast paladin spells with a casting time of 1 action as a bonus action; enemies within 10 feet have disadvantage on saves against your paladin spells and Channel Divinity. Once per long rest.",
                    "1/long rest"),
            };

            d["Oath of the Crown"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Command, Compelled Duel; 5th — Warding Bond, Zone of Truth; 9th — Aura of Vitality, Spirit Guardians; 13th — Banishment, Guardian of Faith; 17th — Circle of Power, Geas."),
                SF(3, "Channel Divinity: Champion Challenge",
                    "As a bonus action, each creature of your choice within 30 feet that can see or hear you must succeed on a Wisdom save or can't willingly move more than 30 feet away from you (ends if they take damage from you or an ally, or if you are incapacitated).",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Turn the Tide",
                    "As a bonus action, each creature of your choice within 30 feet that can hear you regains hit points equal to 1d6 + your Charisma modifier (min 1) if it has no more than half of its hit points.",
                    "Channel Divinity"),
                SF(7, "Divine Allegiance",
                    "When a creature within 5 feet of you takes damage, you can use your reaction to magically substitute your own body: the damage is transferred to you instead."),
                SF(15, "Unyielding Spirit",
                    "You have advantage on saving throws to avoid becoming paralyzed or stunned."),
                SF(20, "Exalted Champion",
                    "As an action, for 1 hour: resistance to bludgeoning, piercing, and slashing damage from nonmagical attacks; allies within 30 feet (including you) have advantage on death saves and Wisdom saves; you have advantage on death saves. Once per long rest.",
                    "1/long rest"),
            };

            d["Oath of the Open Sea"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Create or Destroy Water, Expeditious Retreat; 5th — Augury, Misty Step; 9th — Call Lightning, Water Walk; 13th — Control Water, Freedom of Movement; 17th — Commune with Nature, Freedom of the Waves (or Conjure Elemental per table)."),
                SF(3, "Channel Divinity: Marine Layer",
                    "As an action, create a 20-foot-radius sphere of magical fog centered on a point within 30 feet for 10 minutes (heavily obscured). You and creatures of your choice can see through it.",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Fury of the Tides",
                    "As a bonus action, the next weapon attack you make this turn that hits can shove the target 10 feet away; if pushed into an obstacle or another creature, it takes bludgeoning damage equal to your Charisma modifier (min 1) and falls prone.",
                    "Channel Divinity"),
                SF(7, "Aura of Origin",
                    "You and friendly creatures within 10 feet (30 at 18th) gain a swimming speed equal to walking speed and can breathe underwater while you are conscious."),
                SF(15, "Stormy Waters",
                    "When a creature within 5 feet of you hits you with an attack, you can use your reaction to force a Strength save or be knocked prone and pushed 10 feet away. Also, when a creature moves into or out of your reach, you can use a reaction to deal 1d8 bludgeoning (as if waves crash)."),
                SF(20, "Mythic Swashbuckler",
                    "As a bonus action, for 1 minute: you can Dash as a bonus action; opportunity attacks against you have disadvantage; when you hit with a melee attack, deal extra 1d6 force and you can shove 10 feet. Once per long rest.",
                    "1/long rest"),
            };

            d["Oath of the Watchers"] = new List<ClassFeature>
            {
                SF(3, "Oath Spells",
                    "Always prepared: 3rd — Alarm, Detect Magic; 5th — Moonbeam, See Invisibility; 9th — Counterspell, Nondetection; 13th — Aura of Purity, Banishment; 17th — Hold Monster, Scrying."),
                SF(3, "Channel Divinity: Watcher's Will",
                    "As an action, choose a number of creatures you can see within 30 feet up to your Charisma modifier (min 1). For 1 minute, they have advantage on Intelligence, Wisdom, and Charisma saving throws.",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Abjure the Extraplanar",
                    "As an action, each aberration, celestial, elemental, fey, or fiend within 30 feet that can hear you must succeed on a Wisdom save or be turned for 1 minute or until it takes damage.",
                    "Channel Divinity"),
                SF(7, "Aura of the Sentinel",
                    "You and friendly creatures within 10 feet (30 at 18th) gain a bonus to initiative rolls equal to your proficiency bonus while you are conscious."),
                SF(15, "Vigilant Rebuke",
                    "When you or a creature you can see within 30 feet succeeds on an Intelligence, Wisdom, or Charisma save, you can use your reaction to deal 2d8 + Charisma modifier force damage to the attacker that forced the save.",
                    "Reaction"),
                SF(20, "Mortal Bulwark",
                    "As a bonus action, for 1 minute: advantage on attacks against aberrations, celestials, elementals, fey, and fiends; on a hit, you can banish them (Charisma save) to their plane until the end of your next turn if not native; " +
                    "you and allies within 30 feet have truesight 120 feet. Once per long rest.",
                    "1/long rest"),
            };

            d["Oathbreaker"] = new List<ClassFeature>
            {
                SF(3, "Oathbreaker Spells",
                    "Always prepared: 3rd — Hellish Rebuke, Inflict Wounds; 5th — Crown of Madness, Darkness; 9th — Animate Dead, Bestow Curse; 13th — Blight, Confusion; 17th — Contagion, Dominate Person."),
                SF(3, "Channel Divinity: Control Undead",
                    "As an action, one undead within 30 feet that can see or hear you must succeed on a Wisdom save or obey your commands for 24 hours (CR must be ≤ your paladin level). Intelligent undead get another save after each hour.",
                    "Channel Divinity"),
                SF(3, "Channel Divinity: Dreadful Aspect",
                    "As an action, each creature of your choice within 30 feet that can see you must succeed on a Wisdom save or be frightened of you for 1 minute. While frightened, if it is more than 30 feet away it must move closer; ends if it ends its turn more than 30 feet away with no line of sight.",
                    "Channel Divinity"),
                SF(7, "Aura of Hate",
                    "You and any fiends and undead within 10 feet (30 at 18th) gain a bonus to melee weapon damage rolls equal to your Charisma modifier (min +1) while you are conscious. Multiple auras don't stack."),
                SF(15, "Supernatural Resistance",
                    "You gain resistance to bludgeoning, piercing, and slashing damage from nonmagical attacks."),
                SF(20, "Dread Lord",
                    "As an action, for 1 minute: create a 30-foot aura of gloom (dim light); enemies in the aura have disadvantage on saves against being frightened; as a bonus action cause a frightened creature in the aura to take 4d10 psychic; " +
                    "when a creature under your Dreadful Aspect is hit by an attack, you can use a reaction to make a melee weapon attack against it. Once per long rest.",
                    "1/long rest"),
            };
        }
    }
}
