using System;
using System.Collections.Generic;
using System.Linq;

namespace Nemo
{
    /// <summary>One innate racial spell grant with a minimum character level.</summary>
    public sealed class RacialSpellGrant
    {
        public string SpellName { get; init; } = "";
        /// <summary>0 = cantrip; 1–9 = leveled spell.</summary>
        public int SpellLevel { get; init; }
        /// <summary>Character level at which this spell becomes available.</summary>
        public int MinCharacterLevel { get; init; } = 1;
        public string SourceTrait { get; init; } = "";
        /// <summary>Optional note (e.g. "once per long rest", "as 2nd-level").</summary>
        public string Notes { get; init; } = "";

        public override string ToString()
        {
            string lvl = SpellLevel <= 0 ? "cantrip" : $"{SpellLevel}";
            string avail = MinCharacterLevel <= 1 ? "" : $" @ char {MinCharacterLevel}+";
            return $"{SpellName} ({lvl}{avail})";
        }
    }

    /// <summary>
    /// Racial innate spells (PHB / SCAG / Volo's style), gated by character level.
    /// High Elf's free cantrip is injected by the UI when chosen.
    /// </summary>
    public static partial class GameData
    {
        /// <summary>
        /// Racial spells currently available for the given race/subrace at <paramref name="characterLevel"/>.
        /// </summary>
        public static List<RacialSpellGrant> GetRacialSpells(
            string? race,
            string? subrace,
            int characterLevel,
            string? highElfChosenCantrip = null)
        {
            int lvl = Math.Max(1, characterLevel);
            return GetAllRacialSpellGrants(race, subrace, highElfChosenCantrip)
                .Where(g => g.MinCharacterLevel <= lvl)
                .OrderBy(g => g.MinCharacterLevel)
                .ThenBy(g => g.SpellLevel)
                .ThenBy(g => g.SpellName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>All racial grants (including not-yet-unlocked), for tooltips / future UI.</summary>
        public static List<RacialSpellGrant> GetAllRacialSpellGrants(
            string? race,
            string? subrace,
            string? highElfChosenCantrip = null)
        {
            var list = new List<RacialSpellGrant>();
            if (string.IsNullOrWhiteSpace(race))
                return list;

            string r = race.Trim();
            string s = (subrace ?? "").Trim();

            // ── Aasimar ──
            if (r.Equals("Aasimar", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(G("Light", 0, 1, "Light Bearer"));
            }

            // ── Fairy ──
            if (r.Equals("Fairy", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(G("Druidcraft", 0, 1, "Fairy Magic"));
                list.Add(G("Faerie Fire", 1, 3, "Fairy Magic", "1/long rest"));
                list.Add(G("Enlarge/Reduce", 2, 5, "Fairy Magic", "1/long rest"));
            }

            // ── Firbolg ──
            if (r.Equals("Firbolg", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(G("Detect Magic", 1, 1, "Firbolg Magic", "1/short rest"));
                list.Add(G("Disguise Self", 1, 1, "Firbolg Magic", "1/short rest; up to 3 ft shorter"));
            }

            // ── Genasi subraces ──
            if (r.Equals("Genasi", StringComparison.OrdinalIgnoreCase))
            {
                if (s.Contains("Air", StringComparison.OrdinalIgnoreCase))
                    list.Add(G("Levitate", 2, 1, "Mingle with the Wind", "1/long rest"));
                else if (s.Contains("Earth", StringComparison.OrdinalIgnoreCase))
                    list.Add(G("Pass without Trace", 2, 1, "Merge with Stone", "1/long rest"));
                else if (s.Contains("Fire", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(G("Produce Flame", 0, 1, "Reach to the Blaze"));
                    list.Add(G("Burning Hands", 1, 3, "Reach to the Blaze", "1/long rest"));
                }
                else if (s.Contains("Water", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(G("Shape Water", 0, 1, "Call to the Wave"));
                    list.Add(G("Create or Destroy Water", 1, 3, "Call to the Wave", "as 2nd-level; 1/long rest"));
                }
            }

            // ── Gith ──
            if (r.Equals("Gith", StringComparison.OrdinalIgnoreCase))
            {
                if (s.Contains("Githyanki", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(G("Mage Hand", 0, 1, "Githyanki Psionics", "invisible hand"));
                    list.Add(G("Jump", 1, 3, "Githyanki Psionics", "1/long rest"));
                    list.Add(G("Misty Step", 2, 5, "Githyanki Psionics", "1/long rest"));
                }
                else if (s.Contains("Githzerai", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(G("Mage Hand", 0, 1, "Githzerai Psionics", "invisible hand"));
                    list.Add(G("Shield", 1, 3, "Githzerai Psionics", "1/long rest"));
                    list.Add(G("Detect Thoughts", 2, 5, "Githzerai Psionics", "1/long rest"));
                }
            }

            // ── Triton ──
            if (r.Equals("Triton", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(G("Fog Cloud", 1, 1, "Control Air and Water", "1/long rest"));
                list.Add(G("Gust of Wind", 2, 3, "Control Air and Water", "1/long rest"));
                list.Add(G("Wall of Water", 3, 5, "Control Air and Water", "1/long rest"));
            }

            // ── Yuan-ti Pureblood ──
            if (r.Equals("Yuan-ti Pureblood", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Yuan-Ti", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Yuan-ti", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(G("Poison Spray", 0, 1, "Innate Spellcasting"));
                list.Add(G("Animal Friendship", 1, 1, "Innate Spellcasting", "snakes only; at will"));
                list.Add(G("Suggestion", 2, 3, "Innate Spellcasting", "1/long rest"));
            }

            // ── Elf ──
            if (r.Equals("Elf", StringComparison.OrdinalIgnoreCase))
            {
                if (s.Contains("Drow", StringComparison.OrdinalIgnoreCase) ||
                    s.Contains("Dark Elf", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(G("Dancing Lights", 0, 1, "Drow Magic"));
                    list.Add(G("Faerie Fire", 1, 3, "Drow Magic", "1/long rest"));
                    list.Add(G("Darkness", 2, 5, "Drow Magic", "1/long rest"));
                }
                else if (s.Contains("High Elf", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(highElfChosenCantrip))
                        list.Add(G(highElfChosenCantrip.Trim(), 0, 1, "Cantrip (High Elf)"));
                }
            }

            // ── Gnome ──
            if (r.Equals("Gnome", StringComparison.OrdinalIgnoreCase) &&
                s.Contains("Forest", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(G("Minor Illusion", 0, 1, "Natural Illusionist"));
            }

            // ── Duergar (full race or Dwarf subrace) ──
            if (r.Equals("Duergar", StringComparison.OrdinalIgnoreCase) ||
                (r.Equals("Dwarf", StringComparison.OrdinalIgnoreCase) &&
                 s.Contains("Duergar", StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(G("Enlarge/Reduce", 2, 3, "Duergar Magic", "enlarge only; 1/long rest"));
                list.Add(G("Invisibility", 2, 5, "Duergar Magic", "self only; 1/long rest"));
            }

            // ── Tiefling (and SCAG variants) ──
            if (r.Equals("Tiefling", StringComparison.OrdinalIgnoreCase))
            {
                if (s.Contains("Winged", StringComparison.OrdinalIgnoreCase))
                {
                    // Replaces Infernal Legacy — no racial spells
                }
                else if (s.Contains("Devil's Tongue", StringComparison.OrdinalIgnoreCase) ||
                         s.Contains("Devils Tongue", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(G("Vicious Mockery", 0, 1, "Devil's Tongue"));
                    list.Add(G("Charm Person", 1, 3, "Devil's Tongue", "as 2nd-level; 1/long rest"));
                    list.Add(G("Enthrall", 2, 5, "Devil's Tongue", "1/long rest"));
                }
                else if (s.Contains("Hellfire", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(G("Thaumaturgy", 0, 1, "Infernal Legacy (Hellfire)"));
                    list.Add(G("Burning Hands", 1, 3, "Hellfire", "as 2nd-level; 1/long rest"));
                    list.Add(G("Darkness", 2, 5, "Infernal Legacy (Hellfire)", "1/long rest"));
                }
                else
                {
                    // PHB default / Asmodeus / Feral (retains Infernal Legacy unless other variant)
                    list.Add(G("Thaumaturgy", 0, 1, "Infernal Legacy"));
                    list.Add(G("Hellish Rebuke", 1, 3, "Infernal Legacy", "as 2nd-level; 1/long rest"));
                    list.Add(G("Darkness", 2, 5, "Infernal Legacy", "1/long rest"));
                }
            }

            return list;
        }

        private static RacialSpellGrant G(
            string name, int spellLevel, int minCharLevel, string trait, string notes = "") =>
            new()
            {
                SpellName = name,
                SpellLevel = spellLevel,
                MinCharacterLevel = minCharLevel,
                SourceTrait = trait,
                Notes = notes
            };
    }
}
