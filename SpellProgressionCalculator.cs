using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>
    /// Cantrips known / spells known (and prepared capacity) by class level.
    /// Prepared math delegates to <see cref="SubclassSpellCalculator"/> where applicable.
    /// </summary>
    public static class SpellProgressionCalculator
    {
        /// <summary>True for classes that learn a fixed list of spells (not prepare from a full list).</summary>
        public static bool IsKnownCaster(string className) =>
            EqualsAny(className, "Bard", "Sorcerer", "Warlock", "Ranger") ||
            IsThirdCasterSubclass(className, subclass: null); // base class alone isn't third-caster

        public static bool IsKnownCaster(string className, string? subclass)
        {
            if (EqualsAny(className, "Bard", "Sorcerer", "Warlock", "Ranger"))
                return true;
            return IsThirdCasterSubclass(className, subclass);
        }

        public static bool IsPreparedCaster(string className) =>
            SubclassSpellCalculator.IsPreparedCaster(className);

        public static bool IsThirdCasterSubclass(string? className, string? subclass)
        {
            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(subclass))
                return false;
            string c = className.Trim();
            string s = subclass.Trim();
            if (c.Equals("Fighter", StringComparison.OrdinalIgnoreCase) &&
                s.Contains("Eldritch Knight", StringComparison.OrdinalIgnoreCase))
                return true;
            if (c.Equals("Rogue", StringComparison.OrdinalIgnoreCase) &&
                s.Contains("Arcane Trickster", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        /// <summary>Cantrips known for a class at the given class level.</summary>
        public static int GetCantripsKnown(string className, int classLevel, string? subclass = null)
        {
            if (string.IsNullOrWhiteSpace(className) || classLevel <= 0)
                return 0;

            int lvl = Math.Clamp(classLevel, 1, 20);
            string cls = className.Trim();

            if (EqualsAny(cls, "Bard"))
                return Table(lvl, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4);

            if (EqualsAny(cls, "Cleric"))
                return Table(lvl, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5);

            if (EqualsAny(cls, "Druid"))
                return Table(lvl, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4);

            if (EqualsAny(cls, "Sorcerer"))
                return Table(lvl, 4, 4, 4, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6);

            if (EqualsAny(cls, "Warlock"))
                return Table(lvl, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4);

            if (EqualsAny(cls, "Wizard"))
                return Table(lvl, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5);

            if (EqualsAny(cls, "Artificer"))
                return Table(lvl, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4);

            // Ranger: no cantrips by PHB default (optional Tasha's Fighting Style / etc. not modeled here)
            if (EqualsAny(cls, "Ranger") || EqualsAny(cls, "Paladin"))
                return 0;

            if (IsThirdCasterSubclass(cls, subclass))
            {
                // Eldritch Knight / Arcane Trickster cantrips: 2 at 3rd, 3 at 10th
                if (lvl < 3) return 0;
                if (lvl < 10) return 2;
                return 3;
            }

            return 0;
        }

        /// <summary>
        /// Spells known for known-caster classes (Bard, Sorcerer, Warlock, Ranger, EK/AT).
        /// Returns 0 for prepared casters (they use prepared capacity instead).
        /// </summary>
        public static int GetSpellsKnown(string className, int classLevel, string? subclass = null)
        {
            if (string.IsNullOrWhiteSpace(className) || classLevel <= 0)
                return 0;

            int lvl = Math.Clamp(classLevel, 1, 20);
            string cls = className.Trim();

            if (EqualsAny(cls, "Bard"))
                return Table(lvl, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 15, 16, 18, 19, 19, 20, 22, 22, 22);

            if (EqualsAny(cls, "Sorcerer"))
                return Table(lvl, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12, 13, 13, 14, 14, 15, 15, 15, 15);

            if (EqualsAny(cls, "Warlock"))
                return Table(lvl, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15);

            if (EqualsAny(cls, "Ranger"))
            {
                // PHB: no spells at 1st; spells known from 2nd
                return Table(lvl, 0, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11);
            }

            if (IsThirdCasterSubclass(cls, subclass))
            {
                // EK / Arcane Trickster spells known (PHB)
                if (lvl < 3) return 0;
                return Table(lvl,
                    0, 0, 3, 4, 4, 4, 5, 6, 6, 7, 8, 8, 9, 10, 10, 11, 11, 11, 12, 13);
            }

            return 0;
        }

        /// <summary>
        /// Prepared capacity for a prepared caster (domain/oath spells not included).
        /// </summary>
        public static int GetPreparedCapacity(string className, int classLevel, int spellcastingAbilityModifier) =>
            SubclassSpellCalculator.GetPreparedSpellCapacity(className, classLevel, spellcastingAbilityModifier);

        /// <summary>
        /// Highest spell level the character can cast from slots (shared multiclass + pact), 0 if none.
        /// </summary>
        public static int GetHighestAvailableSpellLevel(IEnumerable<ClassLevelEntry> classLevels)
        {
            var slots = SpellSlotCalculator.Calculate(classLevels);
            int highest = 0;
            if (slots.SharedSlots != null)
                highest = Math.Max(highest, slots.SharedSlots.HighestSlotLevel);
            if (slots.PactMagicSlots != null)
                highest = Math.Max(highest, slots.PactMagicSlots.HighestSlotLevel);
            return highest;
        }

        /// <summary>
        /// Aggregate cantrips / prepared / known budgets for the character's class levels.
        /// </summary>
        public static SpellBudgetSnapshot GetBudget(
            IEnumerable<ClassLevelEntry> classLevels,
            Func<string, int> spellcastingAbilityModifierForClass)
        {
            var entries = (classLevels ?? Enumerable.Empty<ClassLevelEntry>())
                .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                .ToList();

            int cantrips = 0;
            int prepared = 0;
            int known = 0;
            bool hasPrepared = false;
            bool hasKnown = false;
            var preparedClasses = new List<string>();
            var knownClasses = new List<string>();

            foreach (var e in entries)
            {
                string cls = e.ClassName.Trim();
                int lvl = e.Levels;
                string? sub = e.Subclass;

                cantrips += GetCantripsKnown(cls, lvl, sub);

                if (IsPreparedCaster(cls))
                {
                    hasPrepared = true;
                    preparedClasses.Add(cls);
                    int mod = spellcastingAbilityModifierForClass?.Invoke(cls) ?? 0;
                    prepared += GetPreparedCapacity(cls, lvl, mod);
                }

                if (IsKnownCaster(cls, sub))
                {
                    hasKnown = true;
                    knownClasses.Add(string.IsNullOrWhiteSpace(sub) ? cls : $"{cls} ({sub})");
                    known += GetSpellsKnown(cls, lvl, sub);
                }
            }

            int highestSpell = GetHighestAvailableSpellLevel(entries);

            return new SpellBudgetSnapshot
            {
                CantripsKnownMax = cantrips,
                PreparedMax = prepared,
                KnownMax = known,
                HasPreparedCaster = hasPrepared,
                HasKnownCaster = hasKnown,
                HighestSpellLevelAvailable = highestSpell,
                PreparedClassNames = preparedClasses,
                KnownClassNames = knownClasses
            };
        }

        /// <summary>
        /// Class names that contribute spells to the character's list (for filtering the grid).
        /// Includes third-casters; excludes pure non-casters.
        /// </summary>
        public static HashSet<string> GetSpellListClassNames(IEnumerable<ClassLevelEntry> classLevels)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (classLevels == null) return set;

            foreach (var e in classLevels)
            {
                if (e == null || e.Levels <= 0 || string.IsNullOrWhiteSpace(e.ClassName))
                    continue;

                string cls = e.ClassName.Trim();
                var kind = SpellSlotCalculator.GetProgressionKind(cls, e.Subclass);
                if (kind == CasterProgressionKind.None)
                    continue;

                // Third casters use Wizard list
                if (kind == CasterProgressionKind.Third)
                    set.Add("Wizard");
                else
                    set.Add(cls);
            }

            return set;
        }

        private static int Table(int level, params int[] valuesByLevel)
        {
            int idx = Math.Clamp(level, 1, 20) - 1;
            if (idx < 0 || idx >= valuesByLevel.Length) return 0;
            return valuesByLevel[idx];
        }

        private static bool EqualsAny(string? value, params string[] options)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (var o in options)
            {
                if (value.Equals(o, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    /// <summary>Aggregate spell selection budgets for the Spells tab.</summary>
    public sealed class SpellBudgetSnapshot
    {
        public int CantripsKnownMax { get; init; }
        public int PreparedMax { get; init; }
        public int KnownMax { get; init; }
        public bool HasPreparedCaster { get; init; }
        public bool HasKnownCaster { get; init; }
        public int HighestSpellLevelAvailable { get; init; }
        public IReadOnlyList<string> PreparedClassNames { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> KnownClassNames { get; init; } = Array.Empty<string>();
    }
}
