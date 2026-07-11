using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>
    /// Catalog of official published subclasses for every class in Nemo.
    /// Names match <see cref="ClassData.Subclasses"/> entries (and UI keys for Cleric/Warlock/Sorcerer).
    /// Source: PHB, Xanathar's, Tasha's, SCAG, ERftLW, Explorer's Guide to Wildemount, Fizban's, etc.
    /// Reference: https://dnd5e.wikidot.com/ class pages.
    /// </summary>
    public static partial class GameData
    {
        /// <summary>
        /// All subclasses keyed by class name, then ordered list of definitions.
        /// </summary>
        public static readonly Dictionary<string, List<SubclassInfo>> AllSubclasses =
            BuildAllSubclasses();

        /// <summary>Level at which the class first grants its subclass feature.</summary>
        public static int GetSubclassLevel(string className) =>
            (className ?? "").Trim() switch
            {
                "Cleric" or "Sorcerer" or "Warlock" => 1,
                "Druid" or "Wizard" => 2,
                _ => 3 // Artificer, Barbarian, Bard, Fighter, Monk, Paladin, Ranger, Rogue
            };

        /// <summary>Ordered subclass display names for a class (from ClassData or AllSubclasses).</summary>
        public static List<string> GetSubclassNames(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return new List<string>();

            if (ClassData.TryGetValue(className.Trim(), out var data) && data.Subclasses?.Count > 0)
                return data.Subclasses.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

            if (AllSubclasses.TryGetValue(className.Trim(), out var list))
                return list.Select(s => s.Name).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

            return new List<string>();
        }

        /// <summary>Look up catalog entry by class + subclass name (case-insensitive).</summary>
        public static SubclassInfo? GetSubclassInfo(string className, string subclassName)
        {
            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(subclassName))
                return null;

            if (!AllSubclasses.TryGetValue(className.Trim(), out var list))
                return null;

            return list.FirstOrDefault(s =>
                s.Name.Equals(subclassName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, List<SubclassInfo>> BuildAllSubclasses()
        {
            var d = new Dictionary<string, List<SubclassInfo>>(StringComparer.OrdinalIgnoreCase);

            void Add(string cls, int level, string name, string source, string summary) =>
                GetList(d, cls).Add(new SubclassInfo
                {
                    ClassName = cls,
                    Name = name,
                    LevelAvailable = level,
                    Source = source,
                    Summary = summary
                });

            // ── Artificer (3) ──
            Add("Artificer", 3, "Alchemist", "Eberron: Rising from the Last War / Tasha's",
                "Experimental elixirs and transformative reagents; healing and buff potions.");
            Add("Artificer", 3, "Armorer", "Tasha's Cauldron of Everything",
                "Powered armor modes (Guardian/Infiltrator); Intelligence for weapon attacks in armor.");
            Add("Artificer", 3, "Artillerist", "Eberron: Rising from the Last War / Tasha's",
                "Eldritch cannon companion and explosive arcane firearm.");
            Add("Artificer", 3, "Battle Smith", "Eberron: Rising from the Last War / Tasha's",
                "Steel Defender companion; Intelligence for weapon attacks with infused weapons.");

            // ── Barbarian (3) ──
            Add("Barbarian", 3, "Path of the Ancestral Guardian", "Xanathar's Guide to Everything",
                "Spectral ancestors hinder foes and protect allies while you rage.");
            Add("Barbarian", 3, "Path of the Battlerager", "Sword Coast Adventurer's Guide",
                "Spiked armor grappler; bonus attacks with spikes while raging (dwarf-focused).");
            Add("Barbarian", 3, "Path of the Beast", "Tasha's Cauldron of Everything",
                "Manifest natural weapons (bite, claws, tail) while raging.");
            Add("Barbarian", 3, "Path of the Berserker", "Player's Handbook",
                "Frenzy for extra attacks while raging; intimidating presence.");
            Add("Barbarian", 3, "Path of the Giant", "Bigby Presents: Glory of the Giants",
                "Grow in size and hurl elemental power while raging.");
            Add("Barbarian", 3, "Path of the Storm Herald", "Xanathar's Guide to Everything",
                "Aura of desert heat, sea storms, or tundra cold while raging.");
            Add("Barbarian", 3, "Path of the Totem Warrior", "Player's Handbook",
                "Spirit totems (bear, eagle, wolf, and others) grant defensive and utility benefits.");
            Add("Barbarian", 3, "Path of Wild Magic", "Tasha's Cauldron of Everything",
                "Unstable magic surges while raging; magic awareness.");
            Add("Barbarian", 3, "Path of the Zealot", "Xanathar's Guide to Everything",
                "Divine fury damage and improved returns from death.");

            // ── Bard (3) ──
            Add("Bard", 3, "College of Creation", "Tasha's Cauldron of Everything",
                "Performance of Creation and animating objects with song.");
            Add("Bard", 3, "College of Eloquence", "Mythic Odysseys of Theros / Tasha's",
                "Silver tongue; never roll low on Persuasion/Deception; unsettling words.");
            Add("Bard", 3, "College of Glamour", "Xanathar's Guide to Everything",
                "Fey mantle that charms and commands through entrancing presence.");
            Add("Bard", 3, "College of Lore", "Player's Handbook",
                "Extra skills, Cutting Words, and additional Magical Secrets.");
            Add("Bard", 3, "College of Spirits", "Van Richten's Guide to Ravenloft",
                "Tales from beyond; spirit tales grant random spectral benefits.");
            Add("Bard", 3, "College of Swords", "Xanathar's Guide to Everything",
                "Blade flourishes with a weapon as your instrument.");
            Add("Bard", 3, "College of Valor", "Player's Handbook",
                "Medium armor, martial weapons, Combat Inspiration, Extra Attack.");
            Add("Bard", 3, "College of Whispers", "Xanathar's Guide to Everything",
                "Psychic blades and words that unnerve; Mantle of Whispers.");

            // ── Cleric (1) — keys match ClericSubclasses ──
            Add("Cleric", 1, "Arcana", "Sword Coast Adventurer's Guide",
                "Arcane Initiate; wizard cantrip and Arcana proficiency; domain spells.");
            Add("Cleric", 1, "Death", "Dungeon Master's Guide",
                "Reaper necromancy cantrip; martial weapons; necrotic domain magic.");
            Add("Cleric", 1, "Forge", "Xanathar's Guide to Everything",
                "Blessing of the Forge; heavy armor; crafting-focused domain spells.");
            Add("Cleric", 1, "Grave", "Xanathar's Guide to Everything",
                "Circle of Mortality and Eyes of the Grave; undead-sensing domain.");
            Add("Cleric", 1, "Knowledge", "Player's Handbook",
                "Blessings of Knowledge (expertise in two knowledge skills).");
            Add("Cleric", 1, "Life", "Player's Handbook",
                "Disciple of Life; heavy armor; powerful healing domain.");
            Add("Cleric", 1, "Light", "Player's Handbook",
                "Warding Flare; Light cantrip; fire and radiance domain spells.");
            Add("Cleric", 1, "Nature", "Player's Handbook",
                "Acolyte of Nature; heavy armor; druid cantrip and nature skills.");
            Add("Cleric", 1, "Order", "Guildmasters' Guide to Ravnica / Tasha's",
                "Voice of Authority; heavy armor; command-focused domain.");
            Add("Cleric", 1, "Peace", "Tasha's Cauldron of Everything",
                "Emboldening Bond; protective shared bonds.");
            Add("Cleric", 1, "Tempest", "Player's Handbook",
                "Wrath of the Storm; martial weapons and heavy armor; thunder/lightning.");
            Add("Cleric", 1, "Trickery", "Player's Handbook",
                "Blessing of the Trickster; illusion and deception domain magic.");
            Add("Cleric", 1, "Twilight", "Tasha's Cauldron of Everything",
                "Eyes of Night and Vigilant Blessing; darkvision-sharing domain.");
            Add("Cleric", 1, "War", "Player's Handbook",
                "War Priest bonus attacks; martial weapons and heavy armor.");

            // ── Druid (2) ──
            Add("Druid", 2, "Circle of Dreams", "Xanathar's Guide to Everything",
                "Balm of the Summer Court healing and fey-touched rest benefits.");
            Add("Druid", 2, "Circle of Spores", "Guildmasters' Guide to Ravnica / Tasha's",
                "Halo of Spores and Symbiotic Entity fungal combat forms.");
            Add("Druid", 2, "Circle of Stars", "Tasha's Cauldron of Everything",
                "Star map, Starry Form constellations, and cosmic omens.");
            Add("Druid", 2, "Circle of the Land", "Player's Handbook",
                "Bonus cantrip, Natural Recovery, and land-themed circle spells.");
            Add("Druid", 2, "Circle of the Moon", "Player's Handbook",
                "Combat Wild Shape into stronger beasts; later elemental forms.");
            Add("Druid", 2, "Circle of the Shepherd", "Xanathar's Guide to Everything",
                "Spirit Totem auras and superior summoning of beasts and fey.");
            Add("Druid", 2, "Circle of Wildfire", "Tasha's Cauldron of Everything",
                "Summon a Wildfire Spirit companion and fire-based circle magic.");

            // ── Fighter (3) ──
            Add("Fighter", 3, "Arcane Archer", "Xanathar's Guide to Everything",
                "Arcane Shot options that imbue arrows with magical effects.");
            Add("Fighter", 3, "Banneret", "Sword Coast Adventurer's Guide",
                "Purple Dragon Knight: rally allies with inspiring leadership.");
            Add("Fighter", 3, "Battle Master", "Player's Handbook",
                "Combat superiority dice and a suite of martial maneuvers.");
            Add("Fighter", 3, "Cavalier", "Xanathar's Guide to Everything",
                "Mounted combat specialist; mark and protect allies.");
            Add("Fighter", 3, "Champion", "Player's Handbook",
                "Improved Critical and athletic excellence.");
            Add("Fighter", 3, "Echo Knight", "Explorer's Guide to Wildemount",
                "Manifest a magical echo to fight alongside you.");
            Add("Fighter", 3, "Eldritch Knight", "Player's Handbook",
                "Abjuration/evocation wizard spellcasting bonded to weapons (third caster).");
            Add("Fighter", 3, "Psi Warrior", "Tasha's Cauldron of Everything",
                "Psionic power dice for telekinetic strikes and protection.");
            Add("Fighter", 3, "Rune Knight", "Tasha's Cauldron of Everything",
                "Giant runes; grow in size and invoke runic magic.");
            Add("Fighter", 3, "Samurai", "Xanathar's Guide to Everything",
                "Fighting Spirit for temp HP and advantage; elegant resilience.");

            // ── Monk (3) ──
            Add("Monk", 3, "Way of Mercy", "Tasha's Cauldron of Everything",
                "Hands of Healing and Harm; masked physician of life and death.");
            Add("Monk", 3, "Way of Shadow", "Player's Handbook",
                "Shadow Arts, Cloak of Shadows, and teleporting through darkness.");
            Add("Monk", 3, "Way of the Ascendant Dragon", "Fizban's Treasury of Dragons",
                "Draconic presence, breath, and wings through ki.");
            Add("Monk", 3, "Way of the Astral Self", "Tasha's Cauldron of Everything",
                "Manifest astral arms that use Wisdom for attacks.");
            Add("Monk", 3, "Way of the Drunken Master", "Xanathar's Guide to Everything",
                "Disorienting footing, redirect attacks, and fluid movement.");
            Add("Monk", 3, "Way of the Four Elements", "Player's Handbook",
                "Elemental Disciplines that spend ki to cast elemental effects.");
            Add("Monk", 3, "Way of the Kensei", "Xanathar's Guide to Everything",
                "Treat chosen weapons as monk weapons; Agile Parry and Deft Strike.");
            Add("Monk", 3, "Way of the Long Death", "Sword Coast Adventurer's Guide",
                "Harvest life from fallen foes; touch that induces fear of death.");
            Add("Monk", 3, "Way of the Open Hand", "Player's Handbook",
                "Open Hand Technique control on Flurry of Blows; later Wholeness of Body.");
            Add("Monk", 3, "Way of the Sun Soul", "Xanathar's Guide to Everything / SCAG",
                "Radiant sun bolts and searing arcs of light.");

            // ── Paladin (3) ──
            Add("Paladin", 3, "Oath of Conquest", "Xanathar's Guide to Everything",
                "Conquering presence and aura that slows the frightened.");
            Add("Paladin", 3, "Oath of Devotion", "Player's Handbook",
                "Sacred Weapon and Turn the Unholy; classic holy knight.");
            Add("Paladin", 3, "Oath of Glory", "Mythic Odysseys of Theros / Tasha's",
                "Peerless athlete and inspiring leadership in heroic contests.");
            Add("Paladin", 3, "Oath of Redemption", "Xanathar's Guide to Everything",
                "Emissary of peace; protective channel and aura that punishes attackers.");
            Add("Paladin", 3, "Oath of Vengeance", "Player's Handbook",
                "Vow of Enmity and Abjure Enemy; relentless hunter of wrongdoers.");
            Add("Paladin", 3, "Oath of the Ancients", "Player's Handbook",
                "Nature's wrath and aura of warding against spell damage.");
            Add("Paladin", 3, "Oath of the Crown", "Sword Coast Adventurer's Guide",
                "Champion of civilization; compel duty and share burdens.");
            Add("Paladin", 3, "Oath of the Open Sea", "Critical Role / Explorer's Guide-style",
                "Marine-themed oath: thrashing tides and freedom of the sea.");
            Add("Paladin", 3, "Oath of the Watchers", "Tasha's Cauldron of Everything",
                "Ward against extraplanar threats; vigilance auras.");
            Add("Paladin", 3, "Oathbreaker", "Dungeon Master's Guide",
                "Fallen paladin; control undead and dread auras (often villain-oriented).");

            // ── Ranger (3) ──
            Add("Ranger", 3, "Beast Master", "Player's Handbook",
                "Animal companion that fights beside you (revised options in Tasha's).");
            Add("Ranger", 3, "Drakewarden", "Fizban's Treasury of Dragons",
                "Summon and bond with a drake companion that grows with you.");
            Add("Ranger", 3, "Fey Wanderer", "Tasha's Cauldron of Everything",
                "Dreadful strikes and fey-touched charm; otherworldly presence.");
            Add("Ranger", 3, "Gloom Stalker", "Xanathar's Guide to Everything",
                "Umbral Sight, Dread Ambusher; supreme first-round striker in darkness.");
            Add("Ranger", 3, "Horizon Walker", "Xanathar's Guide to Everything",
                "Detect portals; Planar Warrior force damage and planar travel.");
            Add("Ranger", 3, "Hunter", "Player's Handbook",
                "Flexible hunter's prey, defensive tactics, and multiattack options.");
            Add("Ranger", 3, "Monster Slayer", "Xanathar's Guide to Everything",
                "Hunter's Sense and Slayer's Prey against supernatural foes.");
            Add("Ranger", 3, "Swarmkeeper", "Tasha's Cauldron of Everything",
                "Gathered Swarm that moves and damages foes with you.");

            // ── Rogue (3) ──
            Add("Rogue", 3, "Arcane Trickster", "Player's Handbook",
                "Illusion/enchantment wizard spellcasting and Mage Hand Legerdemain (third caster).");
            Add("Rogue", 3, "Assassin", "Player's Handbook",
                "Bonus proficiencies, Assassinate, and infiltration expertise.");
            Add("Rogue", 3, "Inquisitive", "Xanathar's Guide to Everything",
                "Eye for detail; Insightful Fighting to enable Sneak Attack.");
            Add("Rogue", 3, "Mastermind", "Xanathar's Guide to Everything / SCAG",
                "Master of intrigue; Help as a bonus action at range.");
            Add("Rogue", 3, "Phantom", "Tasha's Cauldron of Everything",
                "Whispers of the dead; tokens of the departed and wails from the grave.");
            Add("Rogue", 3, "Scout", "Xanathar's Guide to Everything",
                "Skirmisher mobility and survivalist expertise.");
            Add("Rogue", 3, "Soulknife", "Tasha's Cauldron of Everything",
                "Psychic blades and psionic dice for telepathic and skill tricks.");
            Add("Rogue", 3, "Swashbuckler", "Xanathar's Guide to Everything / SCAG",
                "Fancy Footwork and Rakish Audacity for duelists.");
            Add("Rogue", 3, "Thief", "Player's Handbook",
                "Fast Hands, Second-Story Work, and supreme climbing/using objects.");

            // ── Sorcerer (1) — keys match SorcererSubclasses ──
            Add("Sorcerer", 1, "Aberrant Mind", "Tasha's Cauldron of Everything",
                "Psionic spells and telepathic speech from an alien mind.");
            Add("Sorcerer", 1, "Clockwork Soul", "Tasha's Cauldron of Everything",
                "Order magic; Restore Balance against advantage/disadvantage.");
            Add("Sorcerer", 1, "Divine Soul", "Xanathar's Guide to Everything",
                "Access to cleric spells and Favored by the Gods.");
            Add("Sorcerer", 1, "Draconic Bloodline", "Player's Handbook",
                "Draconic Resilience and ancestry resistance.");
            Add("Sorcerer", 1, "Lunar", "Dragonlance: Shadow of the Dragon Queen",
                "Lunar Sorcery: moon phases and Moonbeam-linked magic.");
            Add("Sorcerer", 1, "Shadow", "Xanathar's Guide to Everything",
                "Shadow Magic: Eyes of the Dark and Strength of the Grave.");
            Add("Sorcerer", 1, "Storm", "Sword Coast Adventurer's Guide",
                "Storm Sorcery: Tempestuous Magic flight after casting.");
            Add("Sorcerer", 1, "Wild Magic", "Player's Handbook",
                "Wild Magic Surge table and Tides of Chaos.");

            // ── Warlock (1) — keys match WarlockSubclasses ──
            Add("Warlock", 1, "The Archfey", "Player's Handbook",
                "Fey Presence; expanded enchantment/illusion spell list.");
            Add("Warlock", 1, "The Celestial", "Xanathar's Guide to Everything",
                "Healing Light and radiant/celestial expanded spells.");
            Add("Warlock", 1, "The Fathomless", "Tasha's Cauldron of Everything",
                "Tentacle of the Deeps and gifts of the deep sea.");
            Add("Warlock", 1, "The Fiend", "Player's Handbook",
                "Dark One's Blessing temp HP; hellish expanded spells.");
            Add("Warlock", 1, "The Genie", "Tasha's Cauldron of Everything",
                "Genie's Vessel sanctuary and elemental genie magic.");
            Add("Warlock", 1, "The Great Old One", "Player's Handbook",
                "Awakened Mind telepathy; alien expanded spells.");
            Add("Warlock", 1, "The Hexblade", "Xanathar's Guide to Everything",
                "Hex Warrior (Cha weapons) and medium armor/shields/martial weapons.");
            Add("Warlock", 1, "The Undead", "Van Richten's Guide to Ravenloft",
                "Form of Dread and Grave Touched undead resilience.");
            Add("Warlock", 1, "The Undying", "Sword Coast Adventurer's Guide",
                "Among the Dead and resistance to death's grasp.");

            // ── Wizard (2) ──
            Add("Wizard", 2, "Abjuration", "Player's Handbook",
                "Arcane Ward that absorbs damage; abjurer defenses.");
            Add("Wizard", 2, "Bladesinging", "Tasha's Cauldron of Everything / SCAG",
                "Bladesong for AC, speed, and concentration; blade cantrips.");
            Add("Wizard", 2, "Chronurgy", "Explorer's Guide to Wildemount",
                "Chronal Shift and temporal manipulation magic.");
            Add("Wizard", 2, "Conjuration", "Player's Handbook",
                "Minor Conjuration and benign transposition.");
            Add("Wizard", 2, "Divination", "Player's Handbook",
                "Portent dice that replace attack rolls, checks, or saves.");
            Add("Wizard", 2, "Enchantment", "Player's Handbook",
                "Hypnotic Gaze and instinctive charm.");
            Add("Wizard", 2, "Evocation", "Player's Handbook",
                "Sculpt Spells to protect allies from your blasts.");
            Add("Wizard", 2, "Graviturgy", "Explorer's Guide to Wildemount",
                "Adjust density and gravity on creatures and objects.");
            Add("Wizard", 2, "Illusion", "Player's Handbook",
                "Improved minor illusion and malleable illusions.");
            Add("Wizard", 2, "Necromancy", "Player's Handbook",
                "Grim Harvest and undead thralls.");
            Add("Wizard", 2, "Order of Scribes", "Tasha's Cauldron of Everything",
                "Awakened spellbook; swap damage types and craft spell scrolls quickly.");
            Add("Wizard", 2, "Transmutation", "Player's Handbook",
                "Minor Alchemy and Transmuter's Stone.");
            Add("Wizard", 2, "War Magic", "Xanathar's Guide to Everything",
                "Arcane Deflection and durable magic for battle mages.");

            return d;
        }

        private static List<SubclassInfo> GetList(Dictionary<string, List<SubclassInfo>> d, string cls)
        {
            if (!d.TryGetValue(cls, out var list))
            {
                list = new List<SubclassInfo>();
                d[cls] = list;
            }
            return list;
        }
    }

    /// <summary>Published subclass metadata for a class.</summary>
    public sealed class SubclassInfo
    {
        public string ClassName { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>Class level when this subclass is chosen.</summary>
        public int LevelAvailable { get; set; } = 3;
        public string Source { get; set; } = "";
        /// <summary>One-line summary for UI/details panels.</summary>
        public string Summary { get; set; } = "";
    }
}
