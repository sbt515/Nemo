using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>
    /// Full subclass feature progression (all levels) for every subclass in Nemo.
    /// Populated by per-class partials; query helpers live here.
    /// Reference: https://dnd5e.wikidot.com/ (e.g. /barbarian:berserker, /cleric:life).
    /// </summary>
    public static partial class GameData
    {
        /// <summary>
        /// Subclass display name → ordered features with <see cref="ClassFeature.Level"/> set.
        /// Keys match <see cref="SubclassInfo.Name"/> / UI keys (Cleric domains are short: "Life", "War", …).
        /// </summary>
        public static readonly Dictionary<string, List<ClassFeature>> SubclassProgression =
            BuildSubclassProgression();

        private static Dictionary<string, List<ClassFeature>> BuildSubclassProgression()
        {
            var d = new Dictionary<string, List<ClassFeature>>(StringComparer.OrdinalIgnoreCase);
            AddBarbarianBardArtificer(d);
            AddClericDruidSorcererWarlock(d);
            AddFighterMonkPaladin(d);
            AddRangerRogueWizard(d);
            return d;
        }

        /// <summary>Features gained exactly at the given class level for a subclass.</summary>
        public static List<ClassFeature> GetSubclassFeaturesAtLevel(string subclassName, int level)
        {
            if (string.IsNullOrWhiteSpace(subclassName) || level < 1 || level > 20)
                return new List<ClassFeature>();

            if (!SubclassProgression.TryGetValue(subclassName.Trim(), out var all) || all == null)
                return new List<ClassFeature>();

            return all.Where(f => f.Level == level).Select(CloneSubclassFeature).ToList();
        }

        /// <summary>All subclass features from level 1 through <paramref name="maxLevel"/> (inclusive).</summary>
        public static List<ClassFeature> GetSubclassFeaturesUpToLevel(string subclassName, int maxLevel)
        {
            if (string.IsNullOrWhiteSpace(subclassName) || maxLevel < 1)
                return new List<ClassFeature>();

            if (!SubclassProgression.TryGetValue(subclassName.Trim(), out var all) || all == null)
                return new List<ClassFeature>();

            int cap = Math.Min(20, maxLevel);
            return all
                .Where(f => f.Level >= 1 && f.Level <= cap)
                .OrderBy(f => f.Level)
                .ThenBy(f => f.Name)
                .Select(CloneSubclassFeature)
                .ToList();
        }

        /// <summary>Full progression table for a subclass (all levels).</summary>
        public static List<ClassFeature> GetSubclassProgression(string subclassName)
        {
            if (string.IsNullOrWhiteSpace(subclassName))
                return new List<ClassFeature>();

            if (!SubclassProgression.TryGetValue(subclassName.Trim(), out var all) || all == null)
                return new List<ClassFeature>();

            return all.OrderBy(f => f.Level).ThenBy(f => f.Name).Select(CloneSubclassFeature).ToList();
        }

        /// <summary>
        /// Compact level → feature-name map (e.g. 3 → Frenzy, 6 → Mindless Rage).
        /// </summary>
        public static Dictionary<int, List<string>> GetSubclassFeatureNameTable(string subclassName)
        {
            var result = new Dictionary<int, List<string>>();
            if (!SubclassProgression.TryGetValue(subclassName?.Trim() ?? "", out var all) || all == null)
                return result;

            foreach (var f in all)
            {
                if (!result.TryGetValue(f.Level, out var list))
                {
                    list = new List<string>();
                    result[f.Level] = list;
                }
                list.Add(f.Name);
            }
            return result;
        }

        private static ClassFeature CloneSubclassFeature(ClassFeature f) => new()
        {
            Name = f.Name,
            Description = f.Description,
            Uses = f.Uses,
            Level = f.Level,
            IsOptional = f.IsOptional,
            IsSubclassFeature = f.IsSubclassFeature
        };
    }
}
