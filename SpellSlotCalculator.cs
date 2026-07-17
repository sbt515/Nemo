using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>
    /// One class contribution toward a character's levels (for multiclass spell slot math).
    /// </summary>
    public sealed class ClassLevelEntry
    {
        public string ClassName { get; set; } = "";
        /// <summary>Optional subclass name (required to detect Eldritch Knight / Arcane Trickster).</summary>
        public string? Subclass { get; set; }
        public int Levels { get; set; }

        public ClassLevelEntry() { }

        public ClassLevelEntry(string className, int levels, string? subclass = null)
        {
            ClassName = className;
            Levels = levels;
            Subclass = subclass;
        }
    }

    /// <summary>
    /// How a class contributes to the shared (non-Warlock) multiclass spellcasting pool.
    /// Warlock uses <see cref="CasterProgressionKind.PactMagic"/> and does not add to the pool.
    /// </summary>
    public enum CasterProgressionKind
    {
        /// <summary>No spell slots from this class (Barbarian, non-EK Fighter, etc.).</summary>
        None = 0,
        /// <summary>Bard, Cleric, Druid, Sorcerer, Wizard — full levels toward multiclass caster level.</summary>
        Full = 1,
        /// <summary>Paladin, Ranger — floor(levels / 2) toward multiclass caster level.</summary>
        Half = 2,
        /// <summary>Artificer — ceil(levels / 2) toward multiclass caster level (Tasha's / ERftLW).</summary>
        HalfRoundUp = 3,
        /// <summary>Eldritch Knight / Arcane Trickster — floor(levels / 3) toward multiclass caster level.</summary>
        Third = 4,
        /// <summary>Warlock — separate Pact Magic pool; never added to multiclass slots.</summary>
        PactMagic = 5
    }

    /// <summary>
    /// Spell slots of one "pool" (shared multiclass slots, or Warlock pact slots).
    /// Index 0 unused; indices 1–9 are slot levels.
    /// </summary>
    public sealed class SpellSlotPool
    {
        /// <summary>Slots per spell level. Length 10; [0] unused, [1] = 1st-level slots, … [9] = 9th.</summary>
        public int[] SlotsByLevel { get; }

        /// <summary>
        /// For Pact Magic: every slot is this level (1–5). Null for the standard multiclass/full-caster pool
        /// where slots exist at multiple levels.
        /// </summary>
        public int? PactSlotLevel { get; init; }

        /// <summary>True when this pool is Warlock Pact Magic (short rest, all slots same level).</summary>
        public bool IsPactMagic => PactSlotLevel.HasValue;

        public SpellSlotPool(int[] slotsByLevel, int? pactSlotLevel = null)
        {
            if (slotsByLevel == null || slotsByLevel.Length < 10)
                throw new ArgumentException("slotsByLevel must have length >= 10 (indices 1–9 used).", nameof(slotsByLevel));

            SlotsByLevel = (int[])slotsByLevel.Clone();
            PactSlotLevel = pactSlotLevel;
        }

        public int GetSlots(int spellLevel)
        {
            if (spellLevel < 1 || spellLevel > 9) return 0;
            return Math.Max(0, slotsSafe(spellLevel));
        }

        private int slotsSafe(int level) =>
            level >= 0 && level < SlotsByLevel.Length ? SlotsByLevel[level] : 0;

        /// <summary>Highest spell level that has at least one slot (0 if none).</summary>
        public int HighestSlotLevel
        {
            get
            {
                for (int i = 9; i >= 1; i--)
                    if (GetSlots(i) > 0) return i;
                return 0;
            }
        }

        public static SpellSlotPool Empty { get; } = new(new int[10]);

        public override string ToString()
        {
            if (IsPactMagic)
            {
                int n = GetSlots(PactSlotLevel!.Value);
                return n <= 0
                    ? "Pact Magic: none"
                    : $"Pact Magic: {n} × {Ordinal(PactSlotLevel.Value)}-level slot(s)";
            }

            var parts = new List<string>();
            for (int lvl = 1; lvl <= 9; lvl++)
            {
                int n = GetSlots(lvl);
                if (n > 0) parts.Add($"{Ordinal(lvl)}: {n}");
            }
            return parts.Count == 0 ? "No spell slots" : string.Join(", ", parts);
        }

        private static string Ordinal(int n) => n switch
        {
            1 => "1st", 2 => "2nd", 3 => "3rd",
            _ => n + "th"
        };
    }

    /// <summary>
    /// Full result of slot calculation: shared long-rest slots + optional Warlock pact pool.
    /// </summary>
    public sealed class MulticlassSpellSlotResult
    {
        /// <summary>
        /// Shared spell slots from the Multiclass Spellcaster table (or a single class's own table).
        /// Empty if the character has no non-Warlock spellcasting contribution.
        /// </summary>
        public SpellSlotPool SharedSlots { get; init; } = SpellSlotPool.Empty;

        /// <summary>
        /// Warlock Pact Magic only. Independent of <see cref="SharedSlots"/>; short-rest recharge.
        /// Empty if no Warlock levels.
        /// </summary>
        public SpellSlotPool PactMagicSlots { get; init; } = SpellSlotPool.Empty;

        /// <summary>
        /// Multiclass caster level used for the shared pool (PHB multiclass formula).
        /// 0 if only Warlock or no casters. When a single non-warlock class is used alone,
        /// this equals that class's level (class table path) rather than a reduced half/third value.
        /// </summary>
        public int MulticlassCasterLevel { get; init; }

        /// <summary>Sum of all class levels on the character.</summary>
        public int TotalCharacterLevel { get; init; }

        /// <summary>Warlock class levels (0 if none).</summary>
        public int WarlockLevels { get; init; }

        /// <summary>True when more than one class contributes to spellcasting (including Warlock + another).</summary>
        public bool IsMulticlass { get; init; }

        /// <summary>Per-class contribution breakdown (for debugging / future UI).</summary>
        public IReadOnlyList<CasterContribution> Contributions { get; init; } = Array.Empty<CasterContribution>();

        public bool HasAnySlots =>
            SharedSlots.HighestSlotLevel > 0 || PactMagicSlots.HighestSlotLevel > 0;
    }

    public sealed class CasterContribution
    {
        public string ClassName { get; init; } = "";
        public string? Subclass { get; init; }
        public int Levels { get; init; }
        public CasterProgressionKind Progression { get; init; }
        /// <summary>Levels added to the multiclass caster total (0 for None / PactMagic).</summary>
        public int MulticlassCasterLevelsAdded { get; init; }
    }

    /// <summary>
    /// Official D&amp;D 5e spell slot calculation (PHB multiclassing + Tasha's Artificer note + Warlock Pact Magic).
    /// No UI dependency — pure rules math for future multiclass support.
    /// </summary>
    public static class SpellSlotCalculator
    {
        // ───────────────────────── Public API ─────────────────────────

        /// <summary>
        /// Calculate spell slots for a character with one or more class levels.
        /// Warlock levels produce a separate <see cref="MulticlassSpellSlotResult.PactMagicSlots"/> pool
        /// and never contribute to the shared multiclass table.
        /// </summary>
        public static MulticlassSpellSlotResult Calculate(IEnumerable<ClassLevelEntry> classLevels)
        {
            var entries = NormalizeEntries(classLevels);
            int totalCharacterLevel = entries.Sum(e => e.Levels);
            int warlockLevels = entries
                .Where(e => IsWarlock(e.ClassName))
                .Sum(e => e.Levels);

            var contributions = new List<CasterContribution>();
            int multiclassCasterLevelSum = 0;
            int nonWarlockCasterClasses = 0;

            foreach (var entry in entries)
            {
                var kind = GetProgressionKind(entry.ClassName, entry.Subclass);
                int added = GetMulticlassCasterLevelsAdded(kind, entry.Levels);
                if (kind is not CasterProgressionKind.None and not CasterProgressionKind.PactMagic)
                    nonWarlockCasterClasses++;

                multiclassCasterLevelSum += added;
                contributions.Add(new CasterContribution
                {
                    ClassName = entry.ClassName,
                    Subclass = entry.Subclass,
                    Levels = entry.Levels,
                    Progression = kind,
                    MulticlassCasterLevelsAdded = added
                });
            }

            bool hasWarlock = warlockLevels > 0;
            bool isMulticlass = entries.Count > 1;

            // Shared (long-rest) slots
            SpellSlotPool shared;
            int reportedCasterLevel;

            if (nonWarlockCasterClasses == 0)
            {
                // Pure martial, or pure Warlock — no shared pool
                shared = SpellSlotPool.Empty;
                reportedCasterLevel = 0;
            }
            else if (nonWarlockCasterClasses == 1)
            {
                // Exactly one non-Warlock spellcasting class (e.g. pure Ranger, or Ranger 5 / Rogue 3).
                // Use that class's own slot table at its class levels — not floor(levels/2) on the
                // multiclass table — so Ranger 5 still gets 4×1st + 2×2nd when multiclassed with martials.
                var sole = entries.First(e =>
                {
                    var k = GetProgressionKind(e.ClassName, e.Subclass);
                    return k is not CasterProgressionKind.None and not CasterProgressionKind.PactMagic;
                });
                var kind = GetProgressionKind(sole.ClassName, sole.Subclass);
                shared = GetSingleClassSlots(kind, sole.Levels);
                reportedCasterLevel = sole.Levels;
            }
            else
            {
                // Two or more non-Warlock casters: PHB Multiclass Spellcaster table
                int mcLevel = Math.Clamp(multiclassCasterLevelSum, 0, 20);
                shared = GetMulticlassSpellcasterSlots(mcLevel);
                reportedCasterLevel = mcLevel;
            }

            var pact = hasWarlock
                ? GetWarlockPactMagicSlots(warlockLevels)
                : SpellSlotPool.Empty;

            return new MulticlassSpellSlotResult
            {
                SharedSlots = shared,
                PactMagicSlots = pact,
                MulticlassCasterLevel = reportedCasterLevel,
                TotalCharacterLevel = totalCharacterLevel,
                WarlockLevels = warlockLevels,
                IsMulticlass = isMulticlass,
                Contributions = contributions
            };
        }

        /// <summary>Convenience overload for a single class (optional subclass).</summary>
        public static MulticlassSpellSlotResult Calculate(string className, int levels, string? subclass = null) =>
            Calculate(new[] { new ClassLevelEntry(className, levels, subclass) });

        /// <summary>
        /// PHB multiclass formula: how many caster levels this class/subclass adds to the shared pool.
        /// Warlock always returns 0.
        /// </summary>
        public static int GetMulticlassCasterLevelsAdded(CasterProgressionKind kind, int classLevels)
        {
            if (classLevels <= 0) return 0;
            return kind switch
            {
                CasterProgressionKind.Full => classLevels,
                CasterProgressionKind.Half => classLevels / 2,                 // floor
                CasterProgressionKind.HalfRoundUp => (classLevels + 1) / 2,    // ceil
                CasterProgressionKind.Third => classLevels / 3,                // floor
                CasterProgressionKind.PactMagic => 0,
                _ => 0
            };
        }

        /// <summary>
        /// Classify a class (and optional subclass) for spell-slot progression.
        /// </summary>
        public static CasterProgressionKind GetProgressionKind(string className, string? subclass = null)
        {
            if (string.IsNullOrWhiteSpace(className))
                return CasterProgressionKind.None;

            string cls = className.Trim();
            string sub = (subclass ?? "").Trim();

            if (IsWarlock(cls))
                return CasterProgressionKind.PactMagic;

            // Third casters — subclass required
            if (IsFighter(cls) && IsEldritchKnight(sub))
                return CasterProgressionKind.Third;
            if (IsRogue(cls) && IsArcaneTrickster(sub))
                return CasterProgressionKind.Third;

            // Named full / half casters
            if (EqualsAny(cls, "Bard", "Cleric", "Druid", "Sorcerer", "Wizard"))
                return CasterProgressionKind.Full;

            if (EqualsAny(cls, "Paladin", "Ranger"))
                return CasterProgressionKind.Half;

            if (EqualsAny(cls, "Artificer"))
                return CasterProgressionKind.HalfRoundUp;

            // Fighter / Rogue without the spellcasting subclass, barbarian, monk, etc.
            return CasterProgressionKind.None;
        }

        // ───────────────────────── Slot tables ─────────────────────────

        /// <summary>
        /// Multiclass Spellcaster table (PHB) — identical to the full-caster (Wizard/Cleric/etc.) table.
        /// <paramref name="casterLevel"/> is the combined multiclass caster level (1–20).
        /// </summary>
        public static SpellSlotPool GetMulticlassSpellcasterSlots(int casterLevel) =>
            GetFullCasterSlots(casterLevel);

        /// <summary>Full caster progression (Bard, Cleric, Druid, Sorcerer, Wizard) and multiclass table.</summary>
        public static SpellSlotPool GetFullCasterSlots(int level)
        {
            // [level][spellLevel 1-9]
            // Rows for character/caster levels 1–20
            int[][] table =
            {
                /* 1  */ new[] { 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 2  */ new[] { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 3  */ new[] { 0, 4, 2, 0, 0, 0, 0, 0, 0, 0 },
                /* 4  */ new[] { 0, 4, 3, 0, 0, 0, 0, 0, 0, 0 },
                /* 5  */ new[] { 0, 4, 3, 2, 0, 0, 0, 0, 0, 0 },
                /* 6  */ new[] { 0, 4, 3, 3, 0, 0, 0, 0, 0, 0 },
                /* 7  */ new[] { 0, 4, 3, 3, 1, 0, 0, 0, 0, 0 },
                /* 8  */ new[] { 0, 4, 3, 3, 2, 0, 0, 0, 0, 0 },
                /* 9  */ new[] { 0, 4, 3, 3, 3, 1, 0, 0, 0, 0 },
                /* 10 */ new[] { 0, 4, 3, 3, 3, 2, 0, 0, 0, 0 },
                /* 11 */ new[] { 0, 4, 3, 3, 3, 2, 1, 0, 0, 0 },
                /* 12 */ new[] { 0, 4, 3, 3, 3, 2, 1, 0, 0, 0 },
                /* 13 */ new[] { 0, 4, 3, 3, 3, 2, 1, 1, 0, 0 },
                /* 14 */ new[] { 0, 4, 3, 3, 3, 2, 1, 1, 0, 0 },
                /* 15 */ new[] { 0, 4, 3, 3, 3, 2, 1, 1, 1, 0 },
                /* 16 */ new[] { 0, 4, 3, 3, 3, 2, 1, 1, 1, 0 },
                /* 17 */ new[] { 0, 4, 3, 3, 3, 2, 1, 1, 1, 1 },
                /* 18 */ new[] { 0, 4, 3, 3, 3, 3, 1, 1, 1, 1 },
                /* 19 */ new[] { 0, 4, 3, 3, 3, 3, 2, 1, 1, 1 },
                /* 20 */ new[] { 0, 4, 3, 3, 3, 3, 2, 2, 1, 1 },
            };
            return FromTableRow(table, level);
        }

        /// <summary>
        /// Half-caster class table (Paladin, Ranger) — PHB. No slots at level 1.
        /// </summary>
        public static SpellSlotPool GetHalfCasterSlots(int classLevel)
        {
            // Level 1: none; 2–20 from PHB Paladin/Ranger tables
            int[][] table =
            {
                /* 1  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 2  */ new[] { 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 3  */ new[] { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 4  */ new[] { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 5  */ new[] { 0, 4, 2, 0, 0, 0, 0, 0, 0, 0 },
                /* 6  */ new[] { 0, 4, 2, 0, 0, 0, 0, 0, 0, 0 },
                /* 7  */ new[] { 0, 4, 3, 0, 0, 0, 0, 0, 0, 0 },
                /* 8  */ new[] { 0, 4, 3, 0, 0, 0, 0, 0, 0, 0 },
                /* 9  */ new[] { 0, 4, 3, 2, 0, 0, 0, 0, 0, 0 },
                /* 10 */ new[] { 0, 4, 3, 2, 0, 0, 0, 0, 0, 0 },
                /* 11 */ new[] { 0, 4, 3, 3, 0, 0, 0, 0, 0, 0 },
                /* 12 */ new[] { 0, 4, 3, 3, 0, 0, 0, 0, 0, 0 },
                /* 13 */ new[] { 0, 4, 3, 3, 1, 0, 0, 0, 0, 0 },
                /* 14 */ new[] { 0, 4, 3, 3, 1, 0, 0, 0, 0, 0 },
                /* 15 */ new[] { 0, 4, 3, 3, 2, 0, 0, 0, 0, 0 },
                /* 16 */ new[] { 0, 4, 3, 3, 2, 0, 0, 0, 0, 0 },
                /* 17 */ new[] { 0, 4, 3, 3, 3, 1, 0, 0, 0, 0 },
                /* 18 */ new[] { 0, 4, 3, 3, 3, 1, 0, 0, 0, 0 },
                /* 19 */ new[] { 0, 4, 3, 3, 3, 2, 0, 0, 0, 0 },
                /* 20 */ new[] { 0, 4, 3, 3, 3, 2, 0, 0, 0, 0 },
            };
            return FromTableRow(table, classLevel);
        }

        /// <summary>
        /// Artificer spell slot table (ERftLW / Tasha's). Half-caster that starts at level 1.
        /// </summary>
        public static SpellSlotPool GetArtificerSlots(int classLevel)
        {
            int[][] table =
            {
                /* 1  */ new[] { 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 2  */ new[] { 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 3  */ new[] { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 4  */ new[] { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 5  */ new[] { 0, 4, 2, 0, 0, 0, 0, 0, 0, 0 },
                /* 6  */ new[] { 0, 4, 2, 0, 0, 0, 0, 0, 0, 0 },
                /* 7  */ new[] { 0, 4, 3, 0, 0, 0, 0, 0, 0, 0 },
                /* 8  */ new[] { 0, 4, 3, 0, 0, 0, 0, 0, 0, 0 },
                /* 9  */ new[] { 0, 4, 3, 2, 0, 0, 0, 0, 0, 0 },
                /* 10 */ new[] { 0, 4, 3, 2, 0, 0, 0, 0, 0, 0 },
                /* 11 */ new[] { 0, 4, 3, 3, 0, 0, 0, 0, 0, 0 },
                /* 12 */ new[] { 0, 4, 3, 3, 0, 0, 0, 0, 0, 0 },
                /* 13 */ new[] { 0, 4, 3, 3, 1, 0, 0, 0, 0, 0 },
                /* 14 */ new[] { 0, 4, 3, 3, 1, 0, 0, 0, 0, 0 },
                /* 15 */ new[] { 0, 4, 3, 3, 2, 0, 0, 0, 0, 0 },
                /* 16 */ new[] { 0, 4, 3, 3, 2, 0, 0, 0, 0, 0 },
                /* 17 */ new[] { 0, 4, 3, 3, 3, 1, 0, 0, 0, 0 },
                /* 18 */ new[] { 0, 4, 3, 3, 3, 1, 0, 0, 0, 0 },
                /* 19 */ new[] { 0, 4, 3, 3, 3, 2, 0, 0, 0, 0 },
                /* 20 */ new[] { 0, 4, 3, 3, 3, 2, 0, 0, 0, 0 },
            };
            return FromTableRow(table, classLevel);
        }

        /// <summary>
        /// Third-caster table (Eldritch Knight, Arcane Trickster) — PHB. No slots before level 3.
        /// </summary>
        public static SpellSlotPool GetThirdCasterSlots(int classLevel)
        {
            int[][] table =
            {
                /* 1  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 2  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 3  */ new[] { 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 4  */ new[] { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 5  */ new[] { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 6  */ new[] { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0 },
                /* 7  */ new[] { 0, 4, 2, 0, 0, 0, 0, 0, 0, 0 },
                /* 8  */ new[] { 0, 4, 2, 0, 0, 0, 0, 0, 0, 0 },
                /* 9  */ new[] { 0, 4, 2, 0, 0, 0, 0, 0, 0, 0 },
                /* 10 */ new[] { 0, 4, 3, 0, 0, 0, 0, 0, 0, 0 },
                /* 11 */ new[] { 0, 4, 3, 0, 0, 0, 0, 0, 0, 0 },
                /* 12 */ new[] { 0, 4, 3, 0, 0, 0, 0, 0, 0, 0 },
                /* 13 */ new[] { 0, 4, 3, 2, 0, 0, 0, 0, 0, 0 },
                /* 14 */ new[] { 0, 4, 3, 2, 0, 0, 0, 0, 0, 0 },
                /* 15 */ new[] { 0, 4, 3, 2, 0, 0, 0, 0, 0, 0 },
                /* 16 */ new[] { 0, 4, 3, 3, 0, 0, 0, 0, 0, 0 },
                /* 17 */ new[] { 0, 4, 3, 3, 0, 0, 0, 0, 0, 0 },
                /* 18 */ new[] { 0, 4, 3, 3, 0, 0, 0, 0, 0, 0 },
                /* 19 */ new[] { 0, 4, 3, 3, 1, 0, 0, 0, 0, 0 },
                /* 20 */ new[] { 0, 4, 3, 3, 1, 0, 0, 0, 0, 0 },
            };
            return FromTableRow(table, classLevel);
        }

        /// <summary>
        /// Warlock Pact Magic (PHB). All slots are the same level; short rest recovery.
        /// Returned as a pool with only that slot level populated, and <see cref="SpellSlotPool.PactSlotLevel"/> set.
        /// </summary>
        public static SpellSlotPool GetWarlockPactMagicSlots(int warlockLevel)
        {
            if (warlockLevel <= 0)
                return SpellSlotPool.Empty;

            // (slotLevel, slotCount) by warlock level
            var (slotLevel, slotCount) = warlockLevel switch
            {
                1 => (1, 1),
                2 => (1, 2),
                3 or 4 => (2, 2),
                5 or 6 => (3, 2),
                7 or 8 => (4, 2),
                >= 9 and <= 10 => (5, 2),
                >= 11 and <= 16 => (5, 3),
                _ => (5, 4) // 17–20
            };

            var slots = new int[10];
            slots[slotLevel] = slotCount;
            return new SpellSlotPool(slots, pactSlotLevel: slotLevel);
        }

        // ───────────────────────── Internals ─────────────────────────

        private static SpellSlotPool GetSingleClassSlots(CasterProgressionKind kind, int levels) =>
            kind switch
            {
                CasterProgressionKind.Full => GetFullCasterSlots(levels),
                CasterProgressionKind.Half => GetHalfCasterSlots(levels),
                CasterProgressionKind.HalfRoundUp => GetArtificerSlots(levels),
                CasterProgressionKind.Third => GetThirdCasterSlots(levels),
                _ => SpellSlotPool.Empty
            };

        private static SpellSlotPool FromTableRow(int[][] table, int level)
        {
            if (level <= 0)
                return SpellSlotPool.Empty;
            int idx = Math.Clamp(level, 1, 20) - 1;
            return new SpellSlotPool(table[idx]);
        }

        private static List<ClassLevelEntry> NormalizeEntries(IEnumerable<ClassLevelEntry> classLevels)
        {
            if (classLevels == null)
                return new List<ClassLevelEntry>();

            // Merge duplicate class names (keep last non-empty subclass)
            var map = new Dictionary<string, ClassLevelEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in classLevels)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.ClassName) || e.Levels <= 0)
                    continue;

                string key = e.ClassName.Trim();
                if (map.TryGetValue(key, out var existing))
                {
                    existing.Levels += e.Levels;
                    if (!string.IsNullOrWhiteSpace(e.Subclass))
                        existing.Subclass = e.Subclass;
                }
                else
                {
                    map[key] = new ClassLevelEntry(key, e.Levels, e.Subclass);
                }
            }

            return map.Values.ToList();
        }

        private static bool IsWarlock(string className) =>
            EqualsAny(className, "Warlock");

        private static bool IsFighter(string className) =>
            EqualsAny(className, "Fighter");

        private static bool IsRogue(string className) =>
            EqualsAny(className, "Rogue");

        private static bool IsEldritchKnight(string subclass) =>
            subclass.Contains("Eldritch Knight", StringComparison.OrdinalIgnoreCase);

        private static bool IsArcaneTrickster(string subclass) =>
            subclass.Contains("Arcane Trickster", StringComparison.OrdinalIgnoreCase);

        private static bool EqualsAny(string value, params string[] options) =>
            options.Any(o => value.Equals(o, StringComparison.OrdinalIgnoreCase));
    }
}
