using System.Collections.Generic;

namespace Nemo
{
    /// <summary>
    /// Subclass progression for Barbarian paths, Bard colleges, and Artificer specialists.
    /// Reference: https://dnd5e.wikidot.com/ (e.g. /barbarian:berserker).
    /// </summary>
    public static partial class GameData
    {
        private static void AddBarbarianBardArtificer(Dictionary<string, List<ClassFeature>> d)
        {
            // ── Barbarian (3, 6, 10, 14) ──
            d["Path of the Ancestral Guardian"] = new List<ClassFeature>
            {
                SF(3, "Ancestral Protectors",
                    "While raging, the first creature you hit with an attack on your turn is hindered by spectral warriors until the start of your next turn: it has disadvantage on attack rolls against anyone but you, and when it hits another creature with an attack, that creature has resistance to the damage dealt."),
                SF(6, "Spirit Shield",
                    "While raging, when another creature you can see within 30 feet takes damage, you can use your reaction to reduce that damage by 2d6 (3d6 at 10th level, 4d6 at 14th level).",
                    "Reaction while raging"),
                SF(10, "Consult the Spirits",
                    "Cast Augury or Clairvoyance without a spell slot or material components (Clairvoyance summons an ancestral spirit instead of a sensor). Wisdom is your spellcasting ability for these spells.",
                    "1/short or long rest"),
                SF(14, "Vengeful Ancestors",
                    "When you use Spirit Shield to reduce damage from an attack, the attacker takes force damage equal to the amount of damage prevented."),
            };

            d["Path of the Battlerager"] = new List<ClassFeature>
            {
                SF(3, "Battlerager Armor",
                    "While wearing spiked armor and raging, you can use a bonus action to make one melee attack with your armor spikes (1d4 piercing, Strength). When you use the Attack action to grapple a creature and succeed, the target takes 3 piercing damage."),
                SF(6, "Reckless Abandon",
                    "When you use Reckless Attack while raging, you gain temporary hit points equal to your Constitution modifier (minimum 1). They vanish if any remain when your rage ends."),
                SF(10, "Battlerager Charge",
                    "While raging, you can take the Dash action as a bonus action.",
                    "Bonus action while raging"),
                SF(14, "Spiked Retribution",
                    "While raging, wearing spiked armor, and not incapacitated, when a creature within 5 feet hits you with a melee attack, the attacker takes 3 piercing damage."),
            };

            d["Path of the Beast"] = new List<ClassFeature>
            {
                SF(3, "Form of the Beast",
                    "When you enter your rage, you can manifest a natural weapon until the rage ends: Bite (1d8 piercing; heal PB HP if below half when you hit, once per turn), Claws (1d6 slashing; extra claw attack as part of the Attack action once per turn), or Tail (1d8 piercing, reach; reaction d8 to AC when hit within 10 feet)."),
                SF(6, "Bestial Soul",
                    "Your Form of the Beast natural weapons count as magical. When you finish a short or long rest, choose swim and breathe underwater, climb (including ceilings), or extended jumps via a Strength (Athletics) check, lasting until your next short or long rest."),
                SF(10, "Infectious Fury",
                    "When you hit with your natural weapons while raging, force a Wisdom save (DC 8 + Con mod + PB) or the target uses its reaction to melee-attack a creature you choose, or takes 2d12 psychic damage (your choice).",
                    "PB times/long rest"),
                SF(14, "Call the Hunt",
                    "When you enter your rage, choose up to your Constitution modifier (min 1) willing creatures within 30 feet. Gain 5 temporary HP per accepter. Until the rage ends, each chosen creature can once per turn add 1d6 to damage on a hit.",
                    "PB times/long rest"),
            };

            d["Path of the Berserker"] = new List<ClassFeature>
            {
                SF(3, "Frenzy",
                    "When you rage, you can enter a frenzy: for the duration, you can make a single melee weapon attack as a bonus action on each of your turns after this one. When the rage ends, you suffer one level of exhaustion.",
                    "Optional with Rage"),
                SF(6, "Mindless Rage",
                    "You can't be charmed or frightened while raging. If you are charmed or frightened when you enter your rage, the effect is suspended for the duration of the rage."),
                SF(10, "Intimidating Presence",
                    "As an action, choose one creature you can see within 30 feet that can see or hear you. It must succeed on a Wisdom save (DC 8 + PB + Cha mod) or be frightened of you until the end of your next turn. You can use your action on subsequent turns to extend the effect. Ends if the creature ends its turn out of line of sight or more than 60 feet away. On a successful save, that creature is immune for 24 hours.",
                    "Action"),
                SF(14, "Retaliation",
                    "When you take damage from a creature within 5 feet of you, you can use your reaction to make a melee weapon attack against that creature.",
                    "Reaction"),
            };

            d["Path of the Giant"] = new List<ClassFeature>
            {
                SF(3, "Giant's Power",
                    "You learn to speak, read, and write Giant (or another language if you already know Giant). You also learn Druidcraft or Thaumaturgy (your choice). Wisdom is your spellcasting ability for it."),
                SF(3, "Giant's Havoc",
                    "While raging: Crushing Throw — add your Rage Damage bonus to successful Strength thrown-weapon attacks; Giant Stature — your reach increases by 5 feet, and if you are smaller than Large you become Large (if space allows)."),
                SF(6, "Elemental Cleaver",
                    "When you enter your rage, infuse a held weapon with acid, cold, fire, thunder, or lightning: damage type becomes that type, +1d6 of that damage, gains thrown (20/60) and returns to your hand after a throw. Bonus action while raging to change the damage type."),
                SF(10, "Mighty Impel",
                    "While raging, as a bonus action move one Medium or smaller creature within your reach to an unoccupied space within 30 feet (unwilling: Strength save DC 8 + PB + Str mod). If it lands unsupported, it falls prone and takes fall damage as normal.",
                    "Bonus action while raging"),
                SF(14, "Demiurgic Colossus",
                    "When you rage, your reach increases by 10 feet, your size can become Large or Huge (your choice), Mighty Impel works on Large or smaller creatures, and Elemental Cleaver extra damage increases to 2d6."),
            };

            d["Path of the Storm Herald"] = new List<ClassFeature>
            {
                SF(3, "Storm Aura",
                    "While raging, you emanate a 10-foot storm aura (not through total cover). Activate its effect when you enter rage and again as a bonus action each turn. Choose Desert (fire damage to others in aura), Sea (one creature Dex save or lightning damage), or Tundra (temp HP to chosen creatures). Change environment when you gain a barbarian level. Save DC = 8 + PB + Con mod. Damage/temp HP scale with level."),
                SF(6, "Storm Soul",
                    "Permanent benefits based on your Storm Aura environment: Desert — fire resistance, ignore extreme heat, ignite unattended flammables; Sea — lightning resistance, swim speed 30 ft., breathe underwater; Tundra — cold resistance, ignore extreme cold, freeze a 5-ft cube of water as an action."),
                SF(10, "Shielding Storm",
                    "While in your Storm Aura, creatures of your choice gain the damage resistance from your Storm Soul feature."),
                SF(14, "Raging Storm",
                    "Based on environment: Desert — reaction after a creature in your aura hits you forces a Dex save or fire damage equal to half your barbarian level; Sea — when you hit a creature in your aura, reaction to force a Strength save or knock it prone; Tundra — when you activate your aura, one creature you can see in it must succeed on a Strength save or have speed 0 until the start of your next turn."),
            };

            d["Path of the Totem Warrior"] = new List<ClassFeature>
            {
                SF(3, "Spirit Seeker",
                    "You can cast Beast Sense and Speak with Animals, but only as rituals."),
                SF(3, "Totem Spirit",
                    "Choose a totem spirit (Bear, Eagle, Wolf, or SCAG options Elk/Tiger). While raging: Bear — resistance to all damage except psychic; Eagle — enemies have disadvantage on opportunity attacks against you and you can Dash as a bonus action (no heavy armor); Wolf — allies have advantage on melee attacks against hostiles within 5 feet of you; Elk — +15 ft. speed (no heavy armor); Tiger — longer jumps."),
                SF(6, "Aspect of the Beast",
                    "Magical benefit from a totem of your choice (same or different): Bear (double carry/lift, advantage on Strength checks to push/pull/lift/break), Eagle (keen sight to 1 mile; dim light doesn't impose Perception disadvantage), Wolf (track at fast pace, stealth at normal pace), Elk (doubled travel pace for you and up to 10 companions within 60 ft.), Tiger (proficiency in two of Athletics, Acrobatics, Stealth, Survival)."),
                SF(10, "Spirit Walker",
                    "You can cast Commune with Nature as a ritual. A spiritual version of one of your chosen totem animals conveys the information."),
                SF(14, "Totemic Attunement",
                    "While raging, gain a totem benefit (same or different animal): Bear — hostiles within 5 feet have disadvantage on attacks against others; Eagle — fly speed equal to walking speed (fall if you end your turn aloft); Wolf — bonus action to knock Large or smaller prone on a melee hit; Elk — bonus action to pass through a Large or smaller creature's space and knock it prone with damage on a failed Strength save; Tiger — after moving 20+ feet straight toward a Large or smaller target, bonus action extra melee attack."),
            };

            d["Path of Wild Magic"] = new List<ClassFeature>
            {
                SF(3, "Magic Awareness",
                    "As an action, until the end of your next turn you know the location of any spell or magic item within 60 feet that isn't behind total cover, and the school of any sensed spell.",
                    "PB times/long rest"),
                SF(3, "Wild Surge",
                    "When you enter your rage, roll on the Wild Magic table for a magical effect (teleport, force damage, protective lights, difficult terrain for enemies, etc.). Save DC = 8 + PB + Con mod."),
                SF(6, "Bolstering Magic",
                    "As an action, touch a creature (or yourself): for 10 minutes it adds 1d3 to attack rolls and ability checks, or it regains one expended spell slot of a level equal to 1d3 or lower (once per creature per long rest for the slot option).",
                    "PB times/long rest"),
                SF(10, "Unstable Backlash",
                    "Immediately after you take damage or fail a saving throw while raging, you can use your reaction to roll on the Wild Magic table and replace your current Wild Surge effect.",
                    "Reaction while raging"),
                SF(14, "Controlled Surge",
                    "Whenever you roll on the Wild Magic table, roll twice and choose which effect to use. If both dice match, you can choose any effect on the table."),
            };

            d["Path of the Zealot"] = new List<ClassFeature>
            {
                SF(3, "Divine Fury",
                    "While raging, the first creature you hit each turn with a weapon attack takes extra damage equal to 1d6 + half your barbarian level. The damage is necrotic or radiant (chosen when you gain this feature)."),
                SF(3, "Warrior of the Gods",
                    "If a spell's sole effect is restoring you to life (not undeath), such as Raise Dead, the caster doesn't need material components to cast it on you."),
                SF(6, "Fanatical Focus",
                    "If you fail a saving throw while raging, you can reroll it and must use the new roll.",
                    "1/rage"),
                SF(10, "Zealous Presence",
                    "As a bonus action, up to ten other creatures of your choice within 60 feet that can hear you gain advantage on attack rolls and saving throws until the start of your next turn.",
                    "1/long rest"),
                SF(14, "Rage Beyond Death",
                    "While raging, having 0 hit points doesn't knock you unconscious. You still make death saving throws and suffer normal effects of damage at 0 HP, but you don't die from failed death saves until your rage ends—and only if you still have 0 hit points then."),
            };

            // ── Bard (3, 6, 14) ──
            d["College of Creation"] = new List<ClassFeature>
            {
                SF(3, "Mote of Potential",
                    "When you give a creature a Bardic Inspiration die, create a Tiny mote of potential orbiting it. When the die is used: Ability Check — reroll the inspiration die and choose either result; Attack Roll — target and chosen creatures within 5 feet Con save or take thunder damage equal to the roll; Saving Throw — gain temp HP equal to the roll + your Charisma modifier (min 1)."),
                SF(3, "Performance of Creation",
                    "As an action, create one nonmagical item (gp value ≤ 20 × bard level, Medium or smaller) in an unoccupied space within 10 feet. It lasts a number of hours equal to your proficiency bonus. Size increases to Large at 6th and Huge at 14th. Only one item at a time.",
                    "1/long rest or 2nd+ level slot"),
                SF(6, "Animating Performance",
                    "As an action, animate a Large or smaller nonmagical item within 30 feet that isn't worn or carried into a Dancing Item companion (1 hour). Command it with a bonus action (or as part of Bardic Inspiration). Only one at a time.",
                    "1/long rest or 3rd+ level slot"),
                SF(14, "Creative Crescendo",
                    "When you use Performance of Creation, create a number of items equal to your Charisma modifier (min 2). Only one can be maximum size; the rest must be Small or Tiny. No longer limited by gp value."),
            };

            d["College of Eloquence"] = new List<ClassFeature>
            {
                SF(3, "Silver Tongue",
                    "When you make a Charisma (Persuasion) or Charisma (Deception) check, treat a d20 roll of 9 or lower as a 10."),
                SF(3, "Unsettling Words",
                    "As a bonus action, expend one Bardic Inspiration and choose a creature you can see within 60 feet. It subtracts the die roll from the next saving throw it makes before the start of your next turn.",
                    "Bardic Inspiration"),
                SF(6, "Unfailing Inspiration",
                    "When a creature adds one of your Bardic Inspiration dice to an ability check, attack roll, or saving throw and the roll fails, the creature can keep the Bardic Inspiration die."),
                SF(6, "Universal Speech",
                    "As an action, choose up to your Charisma modifier creatures within 60 feet (min 1). For 1 hour they magically understand you regardless of language.",
                    "1/long rest or expend a spell slot"),
                SF(14, "Infectious Inspiration",
                    "When a creature within 60 feet adds one of your Bardic Inspiration dice and succeeds, you can use your reaction to give a different creature that can hear you within 60 feet a Bardic Inspiration die without expending a use.",
                    "Cha mod times/long rest (min 1)"),
            };

            d["College of Glamour"] = new List<ClassFeature>
            {
                SF(3, "Mantle of Inspiration",
                    "As a bonus action, expend one Bardic Inspiration to grant a wondrous appearance. Up to your Charisma modifier creatures you can see (and that can see you) within 60 feet each gain 5 temporary hit points (8 at 5th, 11 at 10th, 14 at 15th) and can use a reaction to move up to their speed without provoking opportunity attacks.",
                    "Bardic Inspiration"),
                SF(3, "Enthralling Performance",
                    "After performing for at least 1 minute, up to your Charisma modifier humanoids within 60 feet that watched and listened must succeed on a Wisdom save against your spell save DC or be charmed for 1 hour (idolize you; ends on damage or if you attack them/their allies). Successful saves give no hint.",
                    "1/short or long rest"),
                SF(6, "Mantle of Majesty",
                    "As a bonus action, cast Command without a spell slot and assume unearthly beauty for 1 minute (concentration). During this time you can cast Command as a bonus action each turn without a slot. Creatures charmed by you automatically fail saves against these Commands.",
                    "1/long rest"),
                SF(14, "Unbreakable Majesty",
                    "As a bonus action, assume a majestic presence for 1 minute. Whenever a creature tries to attack you for the first time on a turn, it must make a Charisma save against your spell save DC. On a failure it can't attack you that turn; on a success it has disadvantage on saves against your spells on your next turn.",
                    "1/short or long rest"),
            };

            d["College of Lore"] = new List<ClassFeature>
            {
                SF(3, "Bonus Proficiencies",
                    "You gain proficiency with three skills of your choice."),
                SF(3, "Cutting Words",
                    "When a creature you can see within 60 feet makes an attack roll, ability check, or damage roll, you can use your reaction to expend one Bardic Inspiration, rolling the die and subtracting it from the creature's roll (after the roll, before the result). Immune if it can't hear you or is immune to being charmed.",
                    "Reaction + Bardic Inspiration"),
                SF(6, "Additional Magical Secrets",
                    "Learn two spells of your choice from any class (must be of a level you can cast, or cantrips). They count as bard spells for you and don't count against the number of bard spells you know."),
                SF(14, "Peerless Skill",
                    "When you make an ability check, you can expend one Bardic Inspiration, roll the die, and add it to the check (after rolling, before knowing success or failure).",
                    "Bardic Inspiration"),
            };

            d["College of Spirits"] = new List<ClassFeature>
            {
                SF(3, "Guiding Whispers",
                    "You learn the Guidance cantrip (doesn't count against cantrips known). For you, it has a range of 60 feet."),
                SF(3, "Spiritual Focus",
                    "You can use a candle, crystal ball, skull, spirit board, or tarokka deck as a spellcasting focus for bard spells. Starting at 6th level, when you cast a bard spell that deals damage or restores hit points through the focus, add 1d6 to one damage or healing roll."),
                SF(3, "Tales from Beyond",
                    "While holding your Spiritual Focus, as a bonus action expend one Bardic Inspiration and roll on the Spirit Tales table using your Bardic Inspiration die. As an action, bestow the tale's effect on a creature within 30 feet. Retain only one tale at a time.",
                    "Bardic Inspiration"),
                SF(6, "Spirit Session",
                    "Conduct a 1-hour ritual (during a short or long rest) with a number of willing creatures equal to your proficiency bonus (including you). Temporarily learn one Divination or Necromancy spell of a level ≤ the number of participants (and that you can cast) from any class until you start a long rest.",
                    "1/long rest"),
                SF(14, "Mystical Connection",
                    "Whenever you roll on the Spirit Tales table, roll twice and choose which effect to bestow. If both dice match, you can choose any effect on the table."),
            };

            d["College of Swords"] = new List<ClassFeature>
            {
                SF(3, "Bonus Proficiencies",
                    "You gain proficiency with medium armor and the scimitar. If you're proficient with a simple or martial melee weapon, you can use it as a spellcasting focus for your bard spells."),
                SF(3, "Fighting Style",
                    "Choose Dueling (+2 damage with a one-handed melee weapon and no other weapons) or Two-Weapon Fighting (add ability modifier to the second attack's damage)."),
                SF(3, "Blade Flourish",
                    "When you take the Attack action, your walking speed increases by 10 feet until the end of the turn. On a hit, you may use one flourish (one per turn), expending Bardic Inspiration: Defensive Flourish (extra damage and +AC), Slashing Flourish (extra damage to target and another within 5 feet), or Mobile Flourish (extra damage, push, and reaction move near the target).",
                    "Bardic Inspiration"),
                SF(6, "Extra Attack",
                    "You can attack twice, instead of once, whenever you take the Attack action on your turn."),
                SF(14, "Master's Flourish",
                    "Whenever you use a Blade Flourish option, you can roll a d6 and use it instead of expending a Bardic Inspiration die."),
            };

            d["College of Valor"] = new List<ClassFeature>
            {
                SF(3, "Bonus Proficiencies",
                    "You gain proficiency with medium armor, shields, and martial weapons."),
                SF(3, "Combat Inspiration",
                    "A creature with a Bardic Inspiration die from you can roll it to add to a weapon damage roll it just made, or use its reaction when attacked to add the roll to its AC against that attack (after seeing the attack roll)."),
                SF(6, "Extra Attack",
                    "You can attack twice, instead of once, whenever you take the Attack action on your turn."),
                SF(14, "Battle Magic",
                    "When you use your action to cast a bard spell, you can make one weapon attack as a bonus action.",
                    "Bonus action after casting"),
            };

            d["College of Whispers"] = new List<ClassFeature>
            {
                SF(3, "Psychic Blades",
                    "When you hit a creature with a weapon attack, you can expend one Bardic Inspiration to deal an extra 2d6 psychic damage (once per round on your turn). Increases to 3d6 at 5th, 5d6 at 10th, and 8d6 at 15th level.",
                    "Bardic Inspiration (1/round)"),
                SF(3, "Words of Terror",
                    "If you speak to a humanoid alone for at least 1 minute, it must succeed on a Wisdom save against your spell save DC or be frightened of you or a creature of your choice for 1 hour (ends if attacked or damaged, or if it sees allies attacked). Successful save gives no hint.",
                    "1/short or long rest"),
                SF(6, "Mantle of Whispers",
                    "When a humanoid dies within 30 feet, use your reaction to capture its shadow. As an action, transform into its healthy living likeness for 1 hour, gaining casual-acquaintance knowledge of its life (+5 on contested Deception vs Insight).",
                    "Reaction to capture; 1 shadow/short or long rest"),
                SF(14, "Shadow Lore",
                    "As an action, whisper a phrase only one creature within 30 feet can hear. On a failed Wisdom save (must share a language and hear you), it is charmed for 8 hours, convinced you know its most mortifying secret, and obeys non-suicidal commands. Ends if you or allies attack or damage it.",
                    "1/long rest"),
            };

            // ── Artificer (3, 5, 9, 15) ──
            d["Alchemist"] = new List<ClassFeature>
            {
                SF(3, "Tool Proficiency",
                    "You gain proficiency with alchemist's supplies. If you already have it, gain proficiency with one other type of artisan's tools of your choice."),
                SF(3, "Alchemist Spells",
                    "Always prepared (don't count against prepared spells): 3rd — Healing Word, Ray of Sickness; 5th — Flaming Sphere, Melf's Acid Arrow; 9th — Gaseous Form, Mass Healing Word; 13th — Blight, Death Ward; 17th — Cloudkill, Raise Dead."),
                SF(3, "Experimental Elixir",
                    "When you finish a long rest, magically produce an experimental elixir (roll for effect: Healing, Swiftness, Resilience, Boldness, Flight, or Transformation). Drink or administer as an action. Create more by expending a 1st+ spell slot (choose the effect). Two free elixirs at 6th level, three at 15th. Requires alchemist's supplies; lasts until drunk or end of next long rest.",
                    "Long rest + spell slots"),
                SF(5, "Alchemical Savant",
                    "When you cast a spell using alchemist's supplies as the focus, add your Intelligence modifier (min +1) to one roll that restores hit points or deals acid, fire, necrotic, or poison damage."),
                SF(9, "Restorative Reagents",
                    "Creatures that drink your experimental elixirs gain temporary hit points equal to 2d6 + your Intelligence modifier (min 1). You can cast Lesser Restoration without a slot or preparation using alchemist's supplies as the focus a number of times equal to your Intelligence modifier (min 1) per long rest.",
                    "Int mod Lesser Restorations/long rest"),
                SF(15, "Chemical Mastery",
                    "You gain resistance to acid and poison damage and immunity to the poisoned condition. Cast Greater Restoration and Heal each once per long rest without a slot, preparation, or material components, using alchemist's supplies as the focus.",
                    "1 each/long rest"),
            };

            d["Armorer"] = new List<ClassFeature>
            {
                SF(3, "Tools of the Trade",
                    "You gain proficiency with heavy armor and smith's tools. If you already have smith's tools proficiency, gain proficiency with one other type of artisan's tools of your choice."),
                SF(3, "Armorer Spells",
                    "Always prepared (don't count against prepared spells): 3rd — Magic Missile, Thunderwave; 5th — Mirror Image, Shatter; 9th — Hypnotic Pattern, Lightning Bolt; 13th — Fire Shield, Greater Invisibility; 17th — Passwall, Wall of Force."),
                SF(3, "Arcane Armor",
                    "As an action (smith's tools in hand), turn worn armor into Arcane Armor: no Strength requirement for you, spellcasting focus, can't be removed against your will, covers the whole body (helmet toggle as bonus action), replaces missing limbs, don/doff as an action."),
                SF(3, "Armor Model",
                    "Customize Arcane Armor as Guardian (Thunder Gauntlets 1d8 thunder + mark foes; Defensive Field temp HP = artificer level, PB/long rest) or Infiltrator (Lightning Launcher 1d6/extra 1d6, +5 ft. speed, advantage on Stealth). Special weapons use Intelligence for attack and damage. Change model on a short or long rest with smith's tools."),
                SF(5, "Extra Attack",
                    "You can attack twice, rather than once, whenever you take the Attack action on your turn."),
                SF(9, "Armor Modifications",
                    "Your Arcane Armor counts as separate items for Infuse Item (armor, boots, helmet, special weapon)—each can bear an infusion. Maximum infused items increases by 2, but those extra items must be part of your Arcane Armor."),
                SF(15, "Perfected Armor",
                    "Guardian: when a Huge or smaller creature ends its turn within 30 feet, reaction to force a Strength save or pull it up to 25 feet; if within 5 feet, make a melee weapon attack (PB times/long rest). Infiltrator: creatures hit by Lightning Launcher glimmer until your next turn (disadvantage on attacks against you; next attack against them has advantage and +1d6 lightning on a hit)."),
            };

            d["Artillerist"] = new List<ClassFeature>
            {
                SF(3, "Tool Proficiency",
                    "You gain proficiency with woodcarver's tools. If you already have it, gain proficiency with one other type of artisan's tools of your choice."),
                SF(3, "Artillerist Spells",
                    "Always prepared (don't count against prepared spells): 3rd — Shield, Thunderwave; 5th — Scorching Ray, Shatter; 9th — Fireball, Wind Wall; 13th — Ice Storm, Wall of Fire; 17th — Cone of Cold, Wall of Force."),
                SF(3, "Eldritch Cannon",
                    "As an action (woodcarver's or smith's tools), create a Small or Tiny magical cannon (AC 18, HP = 5 × artificer level, 1 hour). Bonus action within 60 feet to activate: Flamethrower (15-ft cone 2d8 fire), Force Ballista (2d8 force + 5-ft push), or Protector (temp HP 1d8 + Int mod in 10 ft.). Only one cannon at a time.",
                    "1/long rest or expend a spell slot"),
                SF(5, "Arcane Firearm",
                    "When you finish a long rest, use woodcarver's tools to carve sigils into a wand, staff, or rod, turning it into your arcane firearm. Use it as a spellcasting focus; when you cast an artificer spell through it, add 1d8 to one of the spell's damage rolls."),
                SF(9, "Explosive Cannon",
                    "Eldritch Cannon damage rolls increase by 1d8. As an action within 60 feet, command the cannon to detonate (destroys it): creatures within 20 feet Dex save against your spell save DC, taking 3d8 force damage on a fail or half on a success."),
                SF(15, "Fortified Position",
                    "You and allies have half cover within 10 feet of your Eldritch Cannon. You can have two cannons at once; create both with the same action (not the same spell slot) and activate both with the same bonus action."),
            };

            d["Battle Smith"] = new List<ClassFeature>
            {
                SF(3, "Tool Proficiency",
                    "You gain proficiency with smith's tools. If you already have it, gain proficiency with one other type of artisan's tools of your choice."),
                SF(3, "Battle Smith Spells",
                    "Always prepared (don't count against prepared spells): 3rd — Heroism, Shield; 5th — Branding Smite, Warding Bond; 9th — Aura of Vitality, Conjure Barrage; 13th — Aura of Purity, Fire Shield; 17th — Banishing Smite, Mass Cure Wounds."),
                SF(3, "Battle Ready",
                    "You gain proficiency with martial weapons. When you attack with a magic weapon, you can use your Intelligence modifier instead of Strength or Dexterity for the attack and damage rolls."),
                SF(3, "Steel Defender",
                    "You create a Medium construct companion (Force-Empowered Rend, Repair 3/day, Deflect Attack reaction). Shares your initiative (acts after you); command with a bonus action. Mending restores 2d6 HP; revive within 1 hour with smith's tools and a 1st+ spell slot. Create a new one at the end of a long rest with smith's tools."),
                SF(5, "Extra Attack",
                    "You can attack twice, rather than once, whenever you take the Attack action on your turn."),
                SF(9, "Arcane Jolt",
                    "When you hit with a magic weapon or your steel defender hits, once per turn channel energy: extra 2d6 force damage, or restore 2d6 hit points to a creature or object you can see within 30 feet of the target.",
                    "Int mod times/long rest (min 1)"),
                SF(15, "Improved Defender",
                    "Arcane Jolt damage and healing increase to 4d6. Your steel defender gains +2 AC. When it uses Deflect Attack, the attacker takes force damage equal to 1d4 + your Intelligence modifier."),
            };
        }
    }
}
