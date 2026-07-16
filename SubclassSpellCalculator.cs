using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>
    /// How a subclass grants a spell.
    /// </summary>
    public enum SubclassSpellGrantKind
    {
        /// <summary>
        /// Always prepared; does not count against the number of spells the character can prepare
        /// (Cleric domain, Paladin oath, Artificer specialist, some Druid circle spells).
        /// </summary>
        AlwaysPrepared = 0,

        /// <summary>
        /// Always known; does not count against spells known
        /// (Ranger archetype magic, Sorcerer Aberrant Mind / Clockwork Soul, etc.).
        /// </summary>
        AlwaysKnown = 1,

        /// <summary>
        /// Added to the class spell list only; not automatically prepared or known (Warlock expanded list).
        /// </summary>
        ExpandedList = 2
    }

    /// <summary>
    /// One spell granted by a subclass feature, with the class level at which it becomes available.
    /// </summary>
    public sealed class SubclassSpellGrant
    {
        public string SpellName { get; init; } = "";
        /// <summary>0 = cantrip; 1–9 = leveled spell.</summary>
        public int SpellLevel { get; init; }
        /// <summary>Minimum class level required to have this spell from the subclass.</summary>
        public int MinClassLevel { get; init; }
        public SubclassSpellGrantKind Kind { get; init; }
        /// <summary>e.g. "Domain Spells", "Oath Spells", "Alchemist Spells".</summary>
        public string SourceFeature { get; init; } = "";
        /// <summary>Optional variant key (e.g. Circle of the Land terrain: "Forest").</summary>
        public string? Variant { get; init; }

        public override string ToString()
        {
            string lvl = SpellLevel <= 0 ? "cantrip" : $"{SpellLevel}";
            return $"{SpellName} (L{lvl} @ class {MinClassLevel}, {Kind})";
        }
    }

    /// <summary>
    /// Snapshot of subclass-granted spells and prepared-spell capacity for one class contribution.
    /// </summary>
    public sealed class SubclassSpellSnapshot
    {
        public string ClassName { get; init; } = "";
        public string? Subclass { get; init; }
        public int ClassLevel { get; init; }

        /// <summary>Always-prepared subclass spells available at this class level (free; not counted against capacity).</summary>
        public IReadOnlyList<SubclassSpellGrant> AlwaysPreparedSpells { get; init; }
            = Array.Empty<SubclassSpellGrant>();

        /// <summary>Always-known subclass spells (don't count against spells known).</summary>
        public IReadOnlyList<SubclassSpellGrant> AlwaysKnownSpells { get; init; }
            = Array.Empty<SubclassSpellGrant>();

        /// <summary>Expanded list only (Warlock patrons, etc.).</summary>
        public IReadOnlyList<SubclassSpellGrant> ExpandedListSpells { get; init; }
            = Array.Empty<SubclassSpellGrant>();

        /// <summary>
        /// How many spells the player may choose to prepare from their class list
        /// (domain/oath/etc. always-prepared spells are <em>not</em> included in this number).
        /// Null when the class is not a prepared caster or ability modifier was not supplied.
        /// </summary>
        public int? PreparedSpellCapacity { get; init; }

        /// <summary>True when this class prepares spells (Cleric, Druid, Wizard, Artificer, Paladin).</summary>
        public bool IsPreparedCaster { get; init; }
    }

    /// <summary>
    /// Official 5e subclass spell grants: domain / oath / specialist / circle spells that are
    /// always prepared (and do not count against prepared totals), always-known archetype spells,
    /// and expanded spell lists. Pure rules math — no UI dependency.
    /// </summary>
    public static class SubclassSpellCalculator
    {
        // ═══════════════════════════════════════════════════════════════
        // Public API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// All structured grants defined for a subclass (all levels), optionally filtered by variant
        /// (e.g. Circle of the Land terrain name).
        /// </summary>
        public static IReadOnlyList<SubclassSpellGrant> GetAllGrants(
            string? subclassName,
            string? variant = null)
        {
            if (string.IsNullOrWhiteSpace(subclassName))
                return Array.Empty<SubclassSpellGrant>();

            if (!Catalog.TryGetValue(subclassName.Trim(), out var all) || all == null)
                return Array.Empty<SubclassSpellGrant>();

            IEnumerable<SubclassSpellGrant> q = all;
            if (!string.IsNullOrWhiteSpace(variant))
            {
                string v = variant.Trim();
                // Include grants with matching variant OR no variant (shared across variants)
                q = all.Where(g =>
                    string.IsNullOrWhiteSpace(g.Variant) ||
                    string.Equals(g.Variant, v, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // No variant requested: exclude variant-specific grants (Land circle terrains)
                q = all.Where(g => string.IsNullOrWhiteSpace(g.Variant));
            }

            return q
                .OrderBy(g => g.MinClassLevel)
                .ThenBy(g => g.SpellLevel)
                .ThenBy(g => g.SpellName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Grants available at the given class level (MinClassLevel ≤ classLevel).
        /// </summary>
        public static IReadOnlyList<SubclassSpellGrant> GetGrantsUpToLevel(
            string? subclassName,
            int classLevel,
            SubclassSpellGrantKind? kindFilter = null,
            string? variant = null)
        {
            if (classLevel < 1)
                return Array.Empty<SubclassSpellGrant>();

            int cap = Math.Min(20, classLevel);
            return GetAllGrants(subclassName, variant)
                .Where(g => g.MinClassLevel <= cap)
                .Where(g => kindFilter == null || g.Kind == kindFilter)
                .ToList();
        }

        /// <summary>
        /// Grants that become available exactly when reaching <paramref name="classLevel"/>
        /// (i.e. MinClassLevel == classLevel). Use on level-up to auto-add free prepared/known spells.
        /// </summary>
        public static IReadOnlyList<SubclassSpellGrant> GetGrantsGainedAtLevel(
            string? subclassName,
            int classLevel,
            SubclassSpellGrantKind? kindFilter = null,
            string? variant = null)
        {
            if (classLevel < 1 || classLevel > 20)
                return Array.Empty<SubclassSpellGrant>();

            return GetAllGrants(subclassName, variant)
                .Where(g => g.MinClassLevel == classLevel)
                .Where(g => kindFilter == null || g.Kind == kindFilter)
                .ToList();
        }

        /// <summary>Always-prepared spells at this class level (free; not counted against capacity).</summary>
        public static IReadOnlyList<SubclassSpellGrant> GetAlwaysPreparedSpells(
            string? subclassName,
            int classLevel,
            string? variant = null) =>
            GetGrantsUpToLevel(subclassName, classLevel, SubclassSpellGrantKind.AlwaysPrepared, variant);

        /// <summary>Always-prepared spells newly gained at this class level.</summary>
        public static IReadOnlyList<SubclassSpellGrant> GetAlwaysPreparedSpellsGainedAtLevel(
            string? subclassName,
            int classLevel,
            string? variant = null) =>
            GetGrantsGainedAtLevel(subclassName, classLevel, SubclassSpellGrantKind.AlwaysPrepared, variant);

        /// <summary>Always-known spells at this class level (don't count against known total).</summary>
        public static IReadOnlyList<SubclassSpellGrant> GetAlwaysKnownSpells(
            string? subclassName,
            int classLevel,
            string? variant = null) =>
            GetGrantsUpToLevel(subclassName, classLevel, SubclassSpellGrantKind.AlwaysKnown, variant);

        /// <summary>
        /// Number of spells the character may prepare from their class list.
        /// Subclass always-prepared spells are <em>not</em> included — they are free extras.
        /// </summary>
        /// <param name="className">Class name.</param>
        /// <param name="classLevel">Levels in that class.</param>
        /// <param name="spellcastingAbilityModifier">Relevant ability modifier (Wis for Cleric/Druid, etc.).</param>
        /// <returns>
        /// Prepared capacity, or 0 if the class does not prepare spells / has no preparation at this level.
        /// </returns>
        public static int GetPreparedSpellCapacity(
            string className,
            int classLevel,
            int spellcastingAbilityModifier)
        {
            if (string.IsNullOrWhiteSpace(className) || classLevel <= 0)
                return 0;

            int lvl = Math.Clamp(classLevel, 1, 20);
            int mod = spellcastingAbilityModifier;
            string cls = className.Trim();

            // Full prepared casters: modifier + class level (minimum 1)
            if (EqualsAny(cls, "Cleric", "Druid", "Wizard"))
                return Math.Max(1, mod + lvl);

            // Artificer: Int mod + half artificer level (rounded down), minimum 1
            if (EqualsAny(cls, "Artificer"))
                return Math.Max(1, mod + lvl / 2);

            // Paladin: Cha mod + half paladin level (rounded down), minimum 1; spells from level 2
            if (EqualsAny(cls, "Paladin"))
                return lvl < 2 ? 0 : Math.Max(1, mod + lvl / 2);

            return 0;
        }

        /// <summary>True if this class prepares spells (as opposed to knowing a fixed list).</summary>
        public static bool IsPreparedCaster(string className) =>
            EqualsAny(className?.Trim() ?? "", "Cleric", "Druid", "Wizard", "Artificer", "Paladin");

        /// <summary>
        /// Build a full subclass-spell snapshot for one class (optional ability mod for capacity).
        /// </summary>
        public static SubclassSpellSnapshot Calculate(
            string className,
            int classLevel,
            string? subclass = null,
            int? spellcastingAbilityModifier = null,
            string? variant = null)
        {
            int lvl = Math.Clamp(classLevel, 0, 20);
            bool prepared = IsPreparedCaster(className);
            int? capacity = null;
            if (prepared && spellcastingAbilityModifier.HasValue && lvl > 0)
                capacity = GetPreparedSpellCapacity(className, lvl, spellcastingAbilityModifier.Value);

            var all = GetGrantsUpToLevel(subclass, lvl, kindFilter: null, variant);
            return new SubclassSpellSnapshot
            {
                ClassName = className?.Trim() ?? "",
                Subclass = subclass,
                ClassLevel = lvl,
                AlwaysPreparedSpells = all.Where(g => g.Kind == SubclassSpellGrantKind.AlwaysPrepared).ToList(),
                AlwaysKnownSpells = all.Where(g => g.Kind == SubclassSpellGrantKind.AlwaysKnown).ToList(),
                ExpandedListSpells = all.Where(g => g.Kind == SubclassSpellGrantKind.ExpandedList).ToList(),
                PreparedSpellCapacity = capacity,
                IsPreparedCaster = prepared
            };
        }

        /// <summary>
        /// Snapshot for every class level entry (multiclass-friendly).
        /// </summary>
        public static List<SubclassSpellSnapshot> CalculateAll(
            IEnumerable<ClassLevelEntry> classLevels,
            Func<string, int>? spellcastingAbilityModifierForClass = null,
            string? landCircleVariant = null)
        {
            var result = new List<SubclassSpellSnapshot>();
            if (classLevels == null) return result;

            foreach (var e in classLevels)
            {
                if (e == null || e.Levels <= 0 || string.IsNullOrWhiteSpace(e.ClassName))
                    continue;

                int? mod = spellcastingAbilityModifierForClass?.Invoke(e.ClassName);
                string? variant = IsLandCircle(e.Subclass) ? landCircleVariant : null;
                result.Add(Calculate(e.ClassName, e.Levels, e.Subclass, mod, variant));
            }

            return result;
        }

        /// <summary>
        /// Given a player-chosen prepared list, return how many count against capacity
        /// (excludes always-prepared subclass spells). Useful for validation.
        /// </summary>
        public static int CountAgainstPreparedCapacity(
            IEnumerable<string> preparedSpellNames,
            IEnumerable<SubclassSpellGrant> alwaysPreparedGrants)
        {
            var free = new HashSet<string>(
                (alwaysPreparedGrants ?? Array.Empty<SubclassSpellGrant>())
                    .Where(g => g.Kind == SubclassSpellGrantKind.AlwaysPrepared)
                    .Select(g => g.SpellName),
                StringComparer.OrdinalIgnoreCase);

            return (preparedSpellNames ?? Array.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(n => !free.Contains(n));
        }

        /// <summary>
        /// Merge player-prepared names with always-prepared grants (deduped, free spells tagged last).
        /// Does not cap by capacity — caller should validate player choices separately.
        /// </summary>
        public static List<string> BuildFullPreparedList(
            IEnumerable<string> playerPrepared,
            IEnumerable<SubclassSpellGrant> alwaysPreparedGrants)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in playerPrepared ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                string n = name.Trim();
                if (seen.Add(n))
                    result.Add(n);
            }

            foreach (var g in alwaysPreparedGrants ?? Array.Empty<SubclassSpellGrant>())
            {
                if (g == null || string.IsNullOrWhiteSpace(g.SpellName)) continue;
                string n = g.SpellName.Trim();
                if (seen.Add(n))
                    result.Add(n);
            }

            return result;
        }

        public static bool HasSubclassSpellData(string? subclassName) =>
            !string.IsNullOrWhiteSpace(subclassName) &&
            Catalog.ContainsKey(subclassName.Trim());

        // ═══════════════════════════════════════════════════════════════
        // Catalog
        // ═══════════════════════════════════════════════════════════════

        private static readonly Dictionary<string, List<SubclassSpellGrant>> Catalog =
            BuildCatalog();

        private static Dictionary<string, List<SubclassSpellGrant>> BuildCatalog()
        {
            var d = new Dictionary<string, List<SubclassSpellGrant>>(StringComparer.OrdinalIgnoreCase);
            AddClericDomains(d);
            AddPaladinOaths(d);
            AddArtificerSpecialists(d);
            AddDruidCircles(d);
            AddRangerArchetypes(d);
            AddSorcererOrigins(d);
            AddWarlockPatrons(d);
            return d;
        }

        // ── helpers ──────────────────────────────────────────────────

        private static SubclassSpellGrant G(
            string name,
            int spellLevel,
            int minClassLevel,
            SubclassSpellGrantKind kind,
            string source,
            string? variant = null) => new()
        {
            SpellName = name,
            SpellLevel = spellLevel,
            MinClassLevel = minClassLevel,
            Kind = kind,
            SourceFeature = source,
            Variant = variant
        };

        /// <summary>
        /// Full-caster domain-style table: spell level 1@1, 2@3, 3@5, 4@7, 5@9.
        /// <paramref name="bySpellLevel"/> index 0 unused; [1]–[5] are spell name pairs (or lists).
        /// </summary>
        private static void AddDomainStyle(
            Dictionary<string, List<SubclassSpellGrant>> d,
            string subclassKey,
            string source,
            string[][] bySpellLevel)
        {
            var list = new List<SubclassSpellGrant>();
            int[] minClass = { 0, 1, 3, 5, 7, 9 };
            for (int spellLvl = 1; spellLvl <= 5; spellLvl++)
            {
                if (spellLvl >= bySpellLevel.Length || bySpellLevel[spellLvl] == null)
                    continue;
                foreach (var spell in bySpellLevel[spellLvl])
                {
                    if (string.IsNullOrWhiteSpace(spell)) continue;
                    list.Add(G(spell.Trim(), spellLvl, minClass[spellLvl],
                        SubclassSpellGrantKind.AlwaysPrepared, source));
                }
            }
            d[subclassKey] = list;
        }

        /// <summary>
        /// Half-caster oath/specialist table: class 3→1st, 5→2nd, 9→3rd, 13→4th, 17→5th.
        /// </summary>
        private static void AddHalfCasterStyle(
            Dictionary<string, List<SubclassSpellGrant>> d,
            string subclassKey,
            string source,
            // (classLevel, spellLevel, spells...)
            params (int ClassLevel, int SpellLevel, string[] Spells)[] rows)
        {
            var list = new List<SubclassSpellGrant>();
            foreach (var row in rows)
            {
                foreach (var spell in row.Spells)
                {
                    if (string.IsNullOrWhiteSpace(spell)) continue;
                    list.Add(G(spell.Trim(), row.SpellLevel, row.ClassLevel,
                        SubclassSpellGrantKind.AlwaysPrepared, source));
                }
            }
            d[subclassKey] = list;
        }

        private static void AddAlwaysKnownSchedule(
            Dictionary<string, List<SubclassSpellGrant>> d,
            string subclassKey,
            string source,
            params (int ClassLevel, int SpellLevel, string[] Spells)[] rows)
        {
            var list = new List<SubclassSpellGrant>();
            foreach (var row in rows)
            {
                foreach (var spell in row.Spells)
                {
                    if (string.IsNullOrWhiteSpace(spell)) continue;
                    list.Add(G(spell.Trim(), row.SpellLevel, row.ClassLevel,
                        SubclassSpellGrantKind.AlwaysKnown, source));
                }
            }
            d[subclassKey] = list;
        }

        private static void AddExpandedList(
            Dictionary<string, List<SubclassSpellGrant>> d,
            string subclassKey,
            string source,
            // spell level → names; available from class level 1 (when slots allow)
            params (int SpellLevel, string[] Spells)[] rows)
        {
            var list = new List<SubclassSpellGrant>();
            // Warlock expanded list is on the list from level 1; actual casting still needs slots
            foreach (var row in rows)
            {
                foreach (var spell in row.Spells)
                {
                    if (string.IsNullOrWhiteSpace(spell)) continue;
                    list.Add(G(spell.Trim(), row.SpellLevel, 1,
                        SubclassSpellGrantKind.ExpandedList, source));
                }
            }
            d[subclassKey] = list;
        }

        private static bool IsLandCircle(string? subclass) =>
            !string.IsNullOrWhiteSpace(subclass) &&
            subclass.Contains("Land", StringComparison.OrdinalIgnoreCase);

        private static bool EqualsAny(string value, params string[] options) =>
            options.Any(o => value.Equals(o, StringComparison.OrdinalIgnoreCase));

        // ═══════════════════════════════════════════════════════════════
        // CLERIC DOMAINS — always prepared; free; unlock with full-caster slot levels
        // ═══════════════════════════════════════════════════════════════

        private static void AddClericDomains(Dictionary<string, List<SubclassSpellGrant>> d)
        {
            const string src = "Domain Spells";

            AddDomainStyle(d, "Arcana", src, new[]
            {
                null!,
                new[] { "Detect Magic", "Magic Missile" },
                new[] { "Magic Weapon", "Nystul's Magic Aura" },
                new[] { "Dispel Magic", "Magic Circle" },
                new[] { "Arcane Eye", "Leomund's Secret Chest" },
                new[] { "Planar Binding", "Teleportation Circle" }
            });

            AddDomainStyle(d, "Death", src, new[]
            {
                null!,
                new[] { "False Life", "Ray of Sickness" },
                new[] { "Blindness/Deafness", "Ray of Enfeeblement" },
                new[] { "Animate Dead", "Vampiric Touch" },
                new[] { "Blight", "Death Ward" },
                new[] { "Antilife Shell", "Cloudkill" }
            });

            AddDomainStyle(d, "Forge", src, new[]
            {
                null!,
                new[] { "Identify", "Searing Smite" },
                new[] { "Heat Metal", "Magic Weapon" },
                new[] { "Elemental Weapon", "Protection from Energy" },
                new[] { "Fabricate", "Wall of Fire" },
                new[] { "Animate Objects", "Creation" }
            });

            AddDomainStyle(d, "Grave", src, new[]
            {
                null!,
                new[] { "Bane", "False Life" },
                new[] { "Gentle Repose", "Ray of Enfeeblement" },
                new[] { "Revivify", "Vampiric Touch" },
                new[] { "Blight", "Death Ward" },
                new[] { "Antilife Shell", "Raise Dead" }
            });

            AddDomainStyle(d, "Knowledge", src, new[]
            {
                null!,
                new[] { "Command", "Identify" },
                new[] { "Augury", "Suggestion" },
                new[] { "Nondetection", "Speak with Dead" },
                new[] { "Arcane Eye", "Confusion" },
                new[] { "Legend Lore", "Scrying" }
            });

            AddDomainStyle(d, "Life", src, new[]
            {
                null!,
                new[] { "Bless", "Cure Wounds" },
                new[] { "Lesser Restoration", "Spiritual Weapon" },
                new[] { "Beacon of Hope", "Revivify" },
                new[] { "Death Ward", "Guardian of Faith" },
                new[] { "Mass Cure Wounds", "Raise Dead" }
            });

            AddDomainStyle(d, "Light", src, new[]
            {
                null!,
                new[] { "Burning Hands", "Faerie Fire" },
                new[] { "Flaming Sphere", "Scorching Ray" },
                new[] { "Daylight", "Fireball" },
                new[] { "Guardian of Faith", "Wall of Fire" },
                new[] { "Flame Strike", "Scrying" }
            });

            AddDomainStyle(d, "Nature", src, new[]
            {
                null!,
                new[] { "Animal Friendship", "Speak with Animals" },
                new[] { "Barkskin", "Spike Growth" },
                new[] { "Plant Growth", "Wind Wall" },
                new[] { "Dominate Beast", "Grasping Vine" },
                new[] { "Insect Plague", "Tree Stride" }
            });

            AddDomainStyle(d, "Order", src, new[]
            {
                null!,
                new[] { "Command", "Heroism" },
                new[] { "Hold Person", "Zone of Truth" },
                new[] { "Mass Healing Word", "Slow" },
                new[] { "Compulsion", "Locate Creature" },
                new[] { "Commune", "Dominate Person" }
            });

            AddDomainStyle(d, "Peace", src, new[]
            {
                null!,
                new[] { "Heroism", "Sanctuary" },
                new[] { "Aid", "Warding Bond" },
                new[] { "Beacon of Hope", "Sending" },
                new[] { "Aura of Purity", "Otiluke's Resilient Sphere" },
                new[] { "Greater Restoration", "Rary's Telepathic Bond" }
            });

            AddDomainStyle(d, "Tempest", src, new[]
            {
                null!,
                new[] { "Fog Cloud", "Thunderwave" },
                new[] { "Gust of Wind", "Shatter" },
                new[] { "Call Lightning", "Sleet Storm" },
                new[] { "Control Water", "Ice Storm" },
                new[] { "Destructive Wave", "Insect Plague" }
            });

            AddDomainStyle(d, "Trickery", src, new[]
            {
                null!,
                new[] { "Charm Person", "Disguise Self" },
                new[] { "Mirror Image", "Pass without Trace" },
                new[] { "Blink", "Dispel Magic" },
                new[] { "Dimension Door", "Polymorph" },
                new[] { "Dominate Person", "Modify Memory" }
            });

            AddDomainStyle(d, "Twilight", src, new[]
            {
                null!,
                new[] { "Faerie Fire", "Sleep" },
                new[] { "Moonbeam", "See Invisibility" },
                new[] { "Aura of Vitality", "Leomund's Tiny Hut" },
                new[] { "Aura of Life", "Greater Invisibility" },
                new[] { "Circle of Power", "Mislead" }
            });

            AddDomainStyle(d, "War", src, new[]
            {
                null!,
                new[] { "Divine Favor", "Shield of Faith" },
                new[] { "Magic Weapon", "Spiritual Weapon" },
                new[] { "Crusader's Mantle", "Spirit Guardians" },
                new[] { "Freedom of Movement", "Stoneskin" },
                new[] { "Flame Strike", "Hold Monster" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // PALADIN OATHS — always prepared; free; half-caster unlock levels
        // ═══════════════════════════════════════════════════════════════

        private static void AddPaladinOaths(Dictionary<string, List<SubclassSpellGrant>> d)
        {
            void Oath(string key, string source,
                string s3a, string s3b,
                string s5a, string s5b,
                string s9a, string s9b,
                string s13a, string s13b,
                string s17a, string s17b) =>
                AddHalfCasterStyle(d, key, source,
                    (3, 1, new[] { s3a, s3b }),
                    (5, 2, new[] { s5a, s5b }),
                    (9, 3, new[] { s9a, s9b }),
                    (13, 4, new[] { s13a, s13b }),
                    (17, 5, new[] { s17a, s17b }));

            Oath("Oath of Conquest", "Oath Spells",
                "Armor of Agathys", "Command",
                "Hold Person", "Spiritual Weapon",
                "Bestow Curse", "Fear",
                "Dominate Beast", "Stoneskin",
                "Cloudkill", "Dominate Person");

            Oath("Oath of Devotion", "Oath Spells",
                "Protection from Evil and Good", "Sanctuary",
                "Lesser Restoration", "Zone of Truth",
                "Beacon of Hope", "Dispel Magic",
                "Freedom of Movement", "Guardian of Faith",
                "Commune", "Flame Strike");

            Oath("Oath of Glory", "Oath Spells",
                "Guiding Bolt", "Heroism",
                "Enhance Ability", "Magic Weapon",
                "Haste", "Protection from Energy",
                "Compulsion", "Freedom of Movement",
                "Commune", "Flame Strike");

            Oath("Oath of Redemption", "Oath Spells",
                "Sanctuary", "Sleep",
                "Calm Emotions", "Hold Person",
                "Counterspell", "Hypnotic Pattern",
                "Otiluke's Resilient Sphere", "Stoneskin",
                "Hold Monster", "Wall of Force");

            Oath("Oath of Vengeance", "Oath Spells",
                "Bane", "Hunter's Mark",
                "Hold Person", "Misty Step",
                "Haste", "Protection from Energy",
                "Banishment", "Dimension Door",
                "Hold Monster", "Scrying");

            Oath("Oath of the Ancients", "Oath Spells",
                "Ensnaring Strike", "Speak with Animals",
                "Moonbeam", "Misty Step",
                "Plant Growth", "Protection from Energy",
                "Ice Storm", "Stoneskin",
                "Commune with Nature", "Tree Stride");

            Oath("Oath of the Crown", "Oath Spells",
                "Command", "Compelled Duel",
                "Warding Bond", "Zone of Truth",
                "Aura of Vitality", "Spirit Guardians",
                "Banishment", "Guardian of Faith",
                "Circle of Power", "Geas");

            Oath("Oath of the Open Sea", "Oath Spells",
                "Create or Destroy Water", "Expeditious Retreat",
                "Augury", "Misty Step",
                "Call Lightning", "Water Walk",
                "Control Water", "Freedom of Movement",
                "Commune with Nature", "Freedom of the Waves");

            Oath("Oath of the Watchers", "Oath Spells",
                "Alarm", "Detect Magic",
                "Moonbeam", "See Invisibility",
                "Counterspell", "Nondetection",
                "Aura of Purity", "Banishment",
                "Hold Monster", "Scrying");

            Oath("Oathbreaker", "Oathbreaker Spells",
                "Hellish Rebuke", "Inflict Wounds",
                "Crown of Madness", "Darkness",
                "Animate Dead", "Bestow Curse",
                "Blight", "Confusion",
                "Contagion", "Dominate Person");
        }

        // ═══════════════════════════════════════════════════════════════
        // ARTIFICER SPECIALISTS — always prepared; free; half-caster levels
        // ═══════════════════════════════════════════════════════════════

        private static void AddArtificerSpecialists(Dictionary<string, List<SubclassSpellGrant>> d)
        {
            void Spec(string key, string source,
                string s3a, string s3b,
                string s5a, string s5b,
                string s9a, string s9b,
                string s13a, string s13b,
                string s17a, string s17b) =>
                AddHalfCasterStyle(d, key, source,
                    (3, 1, new[] { s3a, s3b }),
                    (5, 2, new[] { s5a, s5b }),
                    (9, 3, new[] { s9a, s9b }),
                    (13, 4, new[] { s13a, s13b }),
                    (17, 5, new[] { s17a, s17b }));

            Spec("Alchemist", "Alchemist Spells",
                "Healing Word", "Ray of Sickness",
                "Flaming Sphere", "Melf's Acid Arrow",
                "Gaseous Form", "Mass Healing Word",
                "Blight", "Death Ward",
                "Cloudkill", "Raise Dead");

            Spec("Armorer", "Armorer Spells",
                "Magic Missile", "Thunderwave",
                "Mirror Image", "Shatter",
                "Hypnotic Pattern", "Lightning Bolt",
                "Fire Shield", "Greater Invisibility",
                "Passwall", "Wall of Force");

            Spec("Artillerist", "Artillerist Spells",
                "Shield", "Thunderwave",
                "Scorching Ray", "Shatter",
                "Fireball", "Wind Wall",
                "Ice Storm", "Wall of Fire",
                "Cone of Cold", "Wall of Force");

            Spec("Battle Smith", "Battle Smith Spells",
                "Heroism", "Shield",
                "Branding Smite", "Warding Bond",
                "Aura of Vitality", "Conjure Barrage",
                "Aura of Purity", "Fire Shield",
                "Banishing Smite", "Mass Cure Wounds");
        }

        // ═══════════════════════════════════════════════════════════════
        // DRUID CIRCLES
        // ═══════════════════════════════════════════════════════════════

        private static void AddDruidCircles(Dictionary<string, List<SubclassSpellGrant>> d)
        {
            // Circle of Spores — Chill Touch at 2; leveled circle spells with slot access
            d["Circle of Spores"] = new List<SubclassSpellGrant>
            {
                G("Chill Touch", 0, 2, SubclassSpellGrantKind.AlwaysKnown, "Circle Spells"),
                G("Blindness/Deafness", 2, 3, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Gentle Repose", 2, 3, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Animate Dead", 3, 5, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Gaseous Form", 3, 5, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Blight", 4, 7, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Confusion", 4, 7, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Cloudkill", 5, 9, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Contagion", 5, 9, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
            };

            // Circle of Wildfire — 1st-level circle spells at druid 2
            d["Circle of Wildfire"] = new List<SubclassSpellGrant>
            {
                G("Burning Hands", 1, 2, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Cure Wounds", 1, 2, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Flaming Sphere", 2, 3, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Scorching Ray", 2, 3, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Plant Growth", 3, 5, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Revivify", 3, 5, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Aura of Life", 4, 7, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Fire Shield", 4, 7, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Flame Strike", 5, 9, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
                G("Mass Cure Wounds", 5, 9, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells"),
            };

            // Circle of Stars — Guiding Bolt always prepared (free)
            d["Circle of Stars"] = new List<SubclassSpellGrant>
            {
                G("Guidance", 0, 2, SubclassSpellGrantKind.AlwaysKnown, "Star Map"),
                G("Guiding Bolt", 1, 2, SubclassSpellGrantKind.AlwaysPrepared, "Star Map"),
            };

            // Circle of the Land — terrain variants; first spells at 3rd
            void Land(string terrain,
                string a3, string b3,
                string a5, string b5,
                string a7, string b7,
                string a9, string b9)
            {
                const string key = "Circle of the Land";
                if (!d.TryGetValue(key, out var list))
                {
                    list = new List<SubclassSpellGrant>();
                    d[key] = list;
                }

                void AddPair(int classLvl, int spellLvl, string x, string y)
                {
                    list.Add(G(x, spellLvl, classLvl, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells", terrain));
                    list.Add(G(y, spellLvl, classLvl, SubclassSpellGrantKind.AlwaysPrepared, "Circle Spells", terrain));
                }

                AddPair(3, 2, a3, b3);
                AddPair(5, 3, a5, b5);
                AddPair(7, 4, a7, b7);
                AddPair(9, 5, a9, b9);
            }

            Land("Arctic",
                "Hold Person", "Spike Growth",
                "Sleet Storm", "Slow",
                "Freedom of Movement", "Ice Storm",
                "Commune with Nature", "Cone of Cold");
            Land("Coast",
                "Mirror Image", "Misty Step",
                "Water Breathing", "Water Walk",
                "Control Water", "Freedom of Movement",
                "Conjure Elemental", "Scrying");
            Land("Desert",
                "Blur", "Silence",
                "Create Food and Water", "Protection from Energy",
                "Blight", "Hallucinatory Terrain",
                "Insect Plague", "Wall of Stone");
            Land("Forest",
                "Barkskin", "Spider Climb",
                "Call Lightning", "Plant Growth",
                "Divination", "Freedom of Movement",
                "Commune with Nature", "Tree Stride");
            Land("Grassland",
                "Invisibility", "Pass without Trace",
                "Daylight", "Haste",
                "Divination", "Freedom of Movement",
                "Dream", "Insect Plague");
            Land("Mountain",
                "Spider Climb", "Spike Growth",
                "Lightning Bolt", "Meld into Stone",
                "Stone Shape", "Stoneskin",
                "Passwall", "Wall of Stone");
            Land("Swamp",
                "Darkness", "Melf's Acid Arrow",
                "Water Walk", "Stinking Cloud",
                "Freedom of Movement", "Locate Creature",
                "Insect Plague", "Scrying");
            Land("Underdark",
                "Spider Climb", "Web",
                "Gaseous Form", "Stinking Cloud",
                "Greater Invisibility", "Stone Shape",
                "Cloudkill", "Insect Plague");
        }

        // ═══════════════════════════════════════════════════════════════
        // RANGER ARCHETYPES — always known (not prepared); free known slots
        // ═══════════════════════════════════════════════════════════════

        private static void AddRangerArchetypes(Dictionary<string, List<SubclassSpellGrant>> d)
        {
            void Mag(string key, string source,
                string s3, string s5, string s9, string s13, string s17) =>
                AddAlwaysKnownSchedule(d, key, source,
                    (3, 1, new[] { s3 }),
                    (5, 2, new[] { s5 }),
                    (9, 3, new[] { s9 }),
                    (13, 4, new[] { s13 }),
                    (17, 5, new[] { s17 }));

            Mag("Fey Wanderer", "Fey Wanderer Magic",
                "Charm Person", "Misty Step", "Dispel Magic", "Dimension Door", "Mislead");
            Mag("Gloom Stalker", "Gloom Stalker Magic",
                "Disguise Self", "Rope Trick", "Fear", "Greater Invisibility", "Seeming");
            Mag("Horizon Walker", "Horizon Walker Magic",
                "Protection from Evil and Good", "Misty Step", "Haste", "Banishment", "Teleportation Circle");
            Mag("Monster Slayer", "Monster Slayer Magic",
                "Protection from Evil and Good", "Zone of Truth", "Magic Circle", "Banishment", "Hold Monster");
            Mag("Swarmkeeper", "Swarmkeeper Magic",
                "Faerie Fire", "Web", "Gaseous Form", "Arcane Eye", "Insect Plague");

            d["Drakewarden"] = new List<SubclassSpellGrant>
            {
                G("Thaumaturgy", 0, 3, SubclassSpellGrantKind.AlwaysKnown, "Draconic Gift")
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // SORCERER — always known; free (don't count against known)
        // ═══════════════════════════════════════════════════════════════

        private static void AddSorcererOrigins(Dictionary<string, List<SubclassSpellGrant>> d)
        {
            // Full-caster unlock: 1@1, 2@3, 3@5, 4@7, 5@9 (+ cantrip at 1 for Aberrant)
            d["Aberrant Mind"] = new List<SubclassSpellGrant>
            {
                G("Mind Sliver", 0, 1, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Arms of Hadar", 1, 1, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Dissonant Whispers", 1, 1, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Calm Emotions", 2, 3, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Detect Thoughts", 2, 3, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Hunger of Hadar", 3, 5, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Sending", 3, 5, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Evard's Black Tentacles", 4, 7, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Summon Aberration", 4, 7, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Rary's Telepathic Bond", 5, 9, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
                G("Telekinesis", 5, 9, SubclassSpellGrantKind.AlwaysKnown, "Psionic Spells"),
            };

            d["Clockwork Soul"] = new List<SubclassSpellGrant>
            {
                G("Alarm", 1, 1, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Protection from Evil and Good", 1, 1, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Aid", 2, 3, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Lesser Restoration", 2, 3, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Dispel Magic", 3, 5, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Protection from Energy", 3, 5, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Freedom of Movement", 4, 7, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Summon Construct", 4, 7, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Greater Restoration", 5, 9, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
                G("Wall of Force", 5, 9, SubclassSpellGrantKind.AlwaysKnown, "Clockwork Magic"),
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // WARLOCK — expanded list only (not auto-prepared / known)
        // ═══════════════════════════════════════════════════════════════

        private static void AddWarlockPatrons(Dictionary<string, List<SubclassSpellGrant>> d)
        {
            const string src = "Expanded Spell List";

            AddExpandedList(d, "The Archfey", src,
                (1, new[] { "Faerie Fire", "Sleep" }),
                (2, new[] { "Calm Emotions", "Phantasmal Force" }),
                (3, new[] { "Blink", "Plant Growth" }),
                (4, new[] { "Dominate Beast", "Greater Invisibility" }),
                (5, new[] { "Dominate Person", "Seeming" }));

            AddExpandedList(d, "The Celestial", src,
                (1, new[] { "Cure Wounds", "Guiding Bolt" }),
                (2, new[] { "Flaming Sphere", "Lesser Restoration" }),
                (3, new[] { "Daylight", "Revivify" }),
                (4, new[] { "Guardian of Faith", "Wall of Fire" }),
                (5, new[] { "Flame Strike", "Greater Restoration" }));

            AddExpandedList(d, "The Fathomless", src,
                (1, new[] { "Create or Destroy Water", "Tasha's Hideous Laughter" }),
                (2, new[] { "Gust of Wind", "Silence" }),
                (3, new[] { "Lightning Bolt", "Sleet Storm" }),
                (4, new[] { "Control Water", "Summon Elemental" }),
                (5, new[] { "Bigby's Hand", "Cone of Cold" }));

            AddExpandedList(d, "The Fiend", src,
                (1, new[] { "Burning Hands", "Command" }),
                (2, new[] { "Blindness/Deafness", "Scorching Ray" }),
                (3, new[] { "Fireball", "Stinking Cloud" }),
                (4, new[] { "Fire Shield", "Wall of Fire" }),
                (5, new[] { "Flame Strike", "Hallow" }));

            AddExpandedList(d, "The Great Old One", src,
                (1, new[] { "Dissonant Whispers", "Tasha's Hideous Laughter" }),
                (2, new[] { "Detect Thoughts", "Phantasmal Force" }),
                (3, new[] { "Clairvoyance", "Sending" }),
                (4, new[] { "Dominate Beast", "Evard's Black Tentacles" }),
                (5, new[] { "Dominate Person", "Telekinesis" }));

            AddExpandedList(d, "The Hexblade", src,
                (1, new[] { "Shield", "Wrathful Smite" }),
                (2, new[] { "Blur", "Branding Smite" }),
                (3, new[] { "Blink", "Elemental Weapon" }),
                (4, new[] { "Phantasmal Killer", "Staggering Smite" }),
                (5, new[] { "Banishing Smite", "Cone of Cold" }));

            AddExpandedList(d, "The Undead", src,
                (1, new[] { "Bane", "False Life" }),
                (2, new[] { "Blindness/Deafness", "Phantasmal Force" }),
                (3, new[] { "Phantom Steed", "Speak with Dead" }),
                (4, new[] { "Death Ward", "Greater Invisibility" }),
                (5, new[] { "Antilife Shell", "Cloudkill" }));

            AddExpandedList(d, "The Undying", src,
                (1, new[] { "False Life", "Ray of Sickness" }),
                (2, new[] { "Blindness/Deafness", "Silence" }),
                (3, new[] { "Feign Death", "Speak with Dead" }),
                (4, new[] { "Aura of Life", "Death Ward" }),
                (5, new[] { "Contagion", "Legend Lore" }));

            // Genie shared expanded list (kind-specific extras omitted — shared baseline)
            AddExpandedList(d, "The Genie", src,
                (1, new[] { "Detect Evil and Good" }),
                (2, new[] { "Phantasmal Force" }),
                (3, new[] { "Create Food and Water" }),
                (4, new[] { "Phantasmal Killer" }),
                (5, new[] { "Creation" }),
                (9, new[] { "Wish" }));
        }
    }
}
