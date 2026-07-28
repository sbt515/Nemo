using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nemo
{
    /// <summary>
    /// Loads the full spell database from <c>Data/spells.json</c>
    /// (original 5e spell list from https://dnd5e.wikidot.com/spells).
    /// Provides indexed lookups used by UI and character tools.
    /// </summary>
    public static class SpellCatalog
    {
        private static readonly object Gate = new();
        private static bool _loaded;
        private static List<Spell> _all = new();
        private static Dictionary<string, Spell> _byName =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            // Required on .NET 8+ so options can be marked read-only after first use
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };

        /// <summary>All spells (cantrips through 9th), sorted by level then name.</summary>
        public static IReadOnlyList<Spell> All
        {
            get
            {
                EnsureLoaded();
                return _all;
            }
        }

        public static IReadOnlyList<Spell> Cantrips =>
            All.Where(s => s.Level == 0).ToList();

        public static IReadOnlyList<Spell> Level1 =>
            All.Where(s => s.Level == 1).ToList();

        public static IReadOnlyList<Spell> GetByLevel(int level)
        {
            if (level < 0 || level > 9) return Array.Empty<Spell>();
            return All.Where(s => s.Level == level).ToList();
        }

        public static Spell? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            EnsureLoaded();
            string key = name.Trim();
            if (_byName.TryGetValue(key, out var s)) return s;

            // Common PHB / SRD name variants
            foreach (var alias in GetAliases(key))
            {
                if (_byName.TryGetValue(alias, out s)) return s;
            }
            return null;
        }

        private static IEnumerable<string> GetAliases(string name)
        {
            // Bidirectional common renames between books and SRD
            var pairs = new (string A, string B)[]
            {
                ("Tasha's Hideous Laughter", "Hideous Laughter"),
                ("Nystul's Magic Aura", "Arcanist's Magic Aura"),
                ("Bigby's Hand", "Arcane Hand"),
                ("Mordenkainen's Sword", "Arcane Sword"),
                ("Leomund's Tiny Hut", "Tiny Hut"),
                ("Leomund's Secret Chest", "Secret Chest"),
                ("Otiluke's Resilient Sphere", "Resilient Sphere"),
                ("Otiluke's Freezing Sphere", "Freezing Sphere"),
                ("Mordenkainen's Faithful Hound", "Faithful Hound"),
                ("Mordenkainen's Magnificent Mansion", "Magnificent Mansion"),
                ("Mordenkainen's Private Sanctum", "Private Sanctum"),
                ("Drawmij's Instant Summons", "Instant Summons"),
                ("Evard's Black Tentacles", "Black Tentacles"),
                ("Rary's Telepathic Bond", "Telepathic Bond"),
                ("Otto's Irresistible Dance", "Irresistible Dance"),
                ("Tenser's Floating Disk", "Floating Disk"),
            };
            foreach (var (a, b) in pairs)
            {
                if (name.Equals(a, StringComparison.OrdinalIgnoreCase)) yield return b;
                if (name.Equals(b, StringComparison.OrdinalIgnoreCase)) yield return a;
            }
        }

        public static IReadOnlyList<Spell> GetForClass(string className, int? maxLevel = null)
        {
            if (string.IsNullOrWhiteSpace(className)) return Array.Empty<Spell>();
            return All
                .Where(s => s.Classes.Any(c =>
                    c.Equals(className.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Where(s => maxLevel == null || s.Level <= maxLevel.Value)
                .ToList();
        }

        /// <summary>
        /// Damage or healing dice string when cast at the given slot level (or character level for cantrips).
        /// Falls back to <see cref="Spell.DamageDice"/>.
        /// </summary>
        public static string GetDiceAtSlot(Spell spell, int slotOrCharacterLevel)
        {
            if (spell == null) return "";

            if (spell.Level == 0 && spell.DamageAtCharacterLevel.Count > 0)
            {
                // Cantrip scaling breakpoints: 1, 5, 11, 17
                int best = 1;
                foreach (var key in spell.DamageAtCharacterLevel.Keys)
                {
                    if (int.TryParse(key, out int lvl) &&
                        lvl <= slotOrCharacterLevel &&
                        lvl >= best)
                        best = lvl;
                }
                if (spell.DamageAtCharacterLevel.TryGetValue(best.ToString(), out var cantripDice))
                    return cantripDice;
            }

            if (spell.DamageAtSlotLevel.Count > 0)
            {
                string key = slotOrCharacterLevel.ToString();
                if (spell.DamageAtSlotLevel.TryGetValue(key, out var d))
                    return d;
                // Highest defined ≤ requested
                int best = 0;
                string? bestVal = null;
                foreach (var kv in spell.DamageAtSlotLevel)
                {
                    if (int.TryParse(kv.Key, out int lvl) &&
                        lvl <= slotOrCharacterLevel &&
                        lvl >= best)
                    {
                        best = lvl;
                        bestVal = kv.Value;
                    }
                }
                if (bestVal != null) return bestVal;
            }

            if (spell.HealAtSlotLevel.Count > 0)
            {
                string key = slotOrCharacterLevel.ToString();
                if (spell.HealAtSlotLevel.TryGetValue(key, out var h))
                    return h;
                int best = 0;
                string? bestVal = null;
                foreach (var kv in spell.HealAtSlotLevel)
                {
                    if (int.TryParse(kv.Key, out int lvl) &&
                        lvl <= slotOrCharacterLevel &&
                        lvl >= best)
                    {
                        best = lvl;
                        bestVal = kv.Value;
                    }
                }
                if (bestVal != null) return bestVal;
            }

            return spell.DamageDice ?? "";
        }

        /// <summary>Force reload (e.g. after replacing Data/spells.json).</summary>
        public static void Reload()
        {
            lock (Gate)
            {
                _loaded = false;
                _all = new List<Spell>();
                _byName = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
            }
            EnsureLoaded();
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            lock (Gate)
            {
                if (_loaded) return;
                LoadCore();
                _loaded = true;
            }
        }

        private static void LoadCore()
        {
            string path = ResolveDataPath();
            if (path == null || !File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SpellCatalog] spells.json not found. Tried base: {AppDomain.CurrentDomain.BaseDirectory}");
                _all = new List<Spell>();
                _byName = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var dtos = JsonSerializer.Deserialize<List<SpellJsonDto>>(json, JsonOptions)
                           ?? new List<SpellJsonDto>();

                _all = dtos.Select(ToSpell)
                    .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                    .OrderBy(s => s.Level)
                    .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _byName = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in _all)
                {
                    if (!_byName.ContainsKey(s.Name))
                        _byName[s.Name] = s;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpellCatalog] Failed to load spells: {ex.Message}");
                _all = new List<Spell>();
                _byName = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string? ResolveDataPath()
        {
            // Prefer output directory (CopyToOutputDirectory), then project-relative walk-up
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "spells.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spells.json"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "spells.json")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Data", "spells.json")),
            };

            foreach (var c in candidates)
            {
                try
                {
                    if (File.Exists(c)) return c;
                }
                catch { /* ignore invalid paths */ }
            }
            return candidates[0];
        }

        private static Spell ToSpell(SpellJsonDto d)
        {
            var spell = new Spell
            {
                Name = d.Name?.Trim() ?? "",
                Level = d.Level,
                School = d.School ?? "",
                CastingTime = d.CastingTime ?? "",
                Range = d.Range ?? "",
                Components = d.Components ?? "",
                Material = d.Material ?? "",
                Duration = d.Duration ?? "",
                IsConcentration = d.IsConcentration,
                IsRitual = d.IsRitual,
                DamageType = d.DamageType ?? "",
                DamageDice = d.DamageDice ?? "",
                RollType = d.RollType ?? "",
                SaveAbility = d.SaveAbility ?? "",
                DcSuccess = d.DcSuccess ?? "",
                AttackType = d.AttackType ?? "",
                Description = d.Description ?? "",
                FullDescription = d.FullDescription ?? d.Description ?? "",
                HigherLevel = d.HigherLevel ?? "",
                CanUpcast = d.CanUpcast,
                UpcastIncrement = d.UpcastIncrement ?? "",
                DamageAtSlotLevel = d.DamageAtSlotLevel != null
                    ? new Dictionary<string, string>(d.DamageAtSlotLevel)
                    : new Dictionary<string, string>(),
                DamageAtCharacterLevel = d.DamageAtCharacterLevel != null
                    ? new Dictionary<string, string>(d.DamageAtCharacterLevel)
                    : new Dictionary<string, string>(),
                HealAtSlotLevel = d.HealAtSlotLevel != null
                    ? new Dictionary<string, string>(d.HealAtSlotLevel)
                    : new Dictionary<string, string>(),
                AreaOfEffect = d.AreaOfEffect ?? "",
                Classes = d.Classes != null ? new List<string>(d.Classes) : new List<string>(),
                Source = d.Source ?? "https://dnd5e.wikidot.com/spells"
            };

            // Prefer full text for Description when short was truncated without FullDescription set
            if (string.IsNullOrWhiteSpace(spell.FullDescription))
                spell.FullDescription = spell.Description;

            return spell;
        }

        private sealed class SpellJsonDto
        {
            public string? Name { get; set; }
            public int Level { get; set; }
            public string? School { get; set; }
            public string? CastingTime { get; set; }
            public string? Range { get; set; }
            public string? Components { get; set; }
            public string? Material { get; set; }
            public string? Duration { get; set; }
            public bool IsConcentration { get; set; }
            public bool IsRitual { get; set; }
            public string? DamageType { get; set; }
            public string? DamageDice { get; set; }
            public string? RollType { get; set; }
            public string? SaveAbility { get; set; }
            public string? DcSuccess { get; set; }
            public string? AttackType { get; set; }
            public string? Description { get; set; }
            public string? FullDescription { get; set; }
            public string? HigherLevel { get; set; }
            public bool CanUpcast { get; set; }
            public string? UpcastIncrement { get; set; }
            public Dictionary<string, string>? DamageAtSlotLevel { get; set; }
            public Dictionary<string, string>? DamageAtCharacterLevel { get; set; }
            public Dictionary<string, string>? HealAtSlotLevel { get; set; }
            public string? AreaOfEffect { get; set; }
            public List<string>? Classes { get; set; }
            public string? Source { get; set; }
        }
    }
}
