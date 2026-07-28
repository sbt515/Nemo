using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Nemo
{
    /// <summary>
    /// Persists user-authored character build templates next to app settings.
    /// </summary>
    public static class CustomTemplateStore
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        private static List<CharacterBuildTemplate>? _cache;
        private static string? _resolvedPath;

        public static string GetStorePath()
        {
            if (!string.IsNullOrEmpty(_resolvedPath))
                return _resolvedPath!;

            // Same directory as nemo-settings.json (base dir or LocalAppData\Nemo)
            string settingsPath = AppSettings.GetSettingsPath();
            string? dir = Path.GetDirectoryName(settingsPath);
            if (string.IsNullOrEmpty(dir))
                dir = AppDomain.CurrentDomain.BaseDirectory;

            _resolvedPath = Path.Combine(dir, "nemo-custom-templates.json");
            return _resolvedPath;
        }

        public static bool HasAny() => GetAll().Count > 0;

        public static IReadOnlyList<CharacterBuildTemplate> GetAll()
        {
            EnsureLoaded();
            return _cache!;
        }

        /// <summary>
        /// Adds a custom template and persists. Replaces an existing entry with the same name (case-insensitive).
        /// </summary>
        public static void Save(CharacterBuildTemplate template)
        {
            ArgumentNullException.ThrowIfNull(template);
            if (string.IsNullOrWhiteSpace(template.Name))
                throw new ArgumentException("Template name is required.", nameof(template));

            // Always force Custom category on user saves
            var toSave = new CharacterBuildTemplate
            {
                Name = template.Name.Trim(),
                Category = TemplateCategory.Custom,
                Role = template.Role == TemplateRole.None ? TemplateRole.Support : template.Role,
                Class = template.Class ?? "",
                Subclass = template.Subclass ?? "",
                Race = template.Race ?? "",
                Subrace = template.Subrace ?? "",
                Background = template.Background ?? "",
                AbilityPriority = template.AbilityPriority ?? Array.Empty<string>(),
                PreferredSkills = template.PreferredSkills ?? Array.Empty<string>(),
                PreferredCantrips = template.PreferredCantrips ?? Array.Empty<string>(),
                PreferredSpells = template.PreferredSpells ?? Array.Empty<string>(),
                Description = template.Description ?? ""
            };

            EnsureLoaded();
            int idx = _cache!.FindIndex(t =>
                t.Name.Equals(toSave.Name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
                _cache[idx] = toSave;
            else
                _cache.Add(toSave);

            Persist();
        }

        public static bool Remove(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            EnsureLoaded();
            int removed = _cache!.RemoveAll(t =>
                t.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
                Persist();
            return removed > 0;
        }

        /// <summary>Drop in-memory cache so the next read reloads from disk.</summary>
        public static void InvalidateCache()
        {
            _cache = null;
        }

        private static void EnsureLoaded()
        {
            if (_cache != null) return;

            try
            {
                string path = GetStorePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<List<CharacterBuildTemplateDto>>(json, JsonOpts);
                    if (loaded != null)
                    {
                        _cache = loaded
                            .Where(d => d != null && !string.IsNullOrWhiteSpace(d.Name))
                            .Select(d => d.ToTemplate())
                            .ToList();
                        return;
                    }
                }
            }
            catch
            {
                // corrupt / unreadable — start empty
            }

            _cache = new List<CharacterBuildTemplate>();
        }

        private static void Persist()
        {
            try
            {
                string path = GetStorePath();
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var dtos = (_cache ?? new List<CharacterBuildTemplate>())
                    .Select(CharacterBuildTemplateDto.FromTemplate)
                    .ToList();
                File.WriteAllText(path, JsonSerializer.Serialize(dtos, JsonOpts));
            }
            catch
            {
                // non-fatal: custom templates are best-effort
            }
        }

        /// <summary>
        /// Serializable DTO (settable props) so System.Text.Json can round-trip cleanly.
        /// </summary>
        private sealed class CharacterBuildTemplateDto
        {
            public string Name { get; set; } = "";
            public string Category { get; set; } = "Custom";
            public string Role { get; set; } = "Support";
            public string Class { get; set; } = "";
            public string Subclass { get; set; } = "";
            public string Race { get; set; } = "";
            public string Subrace { get; set; } = "";
            public string Background { get; set; } = "";
            public string[] AbilityPriority { get; set; } = Array.Empty<string>();
            public string[] PreferredSkills { get; set; } = Array.Empty<string>();
            public string[] PreferredCantrips { get; set; } = Array.Empty<string>();
            public string[] PreferredSpells { get; set; } = Array.Empty<string>();
            public string Description { get; set; } = "";

            public CharacterBuildTemplate ToTemplate() => new()
            {
                Name = Name?.Trim() ?? "",
                Category = TemplateCategory.Custom,
                Role = CharacterTemplateGenerator.ParseRole(Role),
                Class = Class ?? "",
                Subclass = Subclass ?? "",
                Race = Race ?? "",
                Subrace = Subrace ?? "",
                Background = Background ?? "",
                AbilityPriority = AbilityPriority ?? Array.Empty<string>(),
                PreferredSkills = PreferredSkills ?? Array.Empty<string>(),
                PreferredCantrips = PreferredCantrips ?? Array.Empty<string>(),
                PreferredSpells = PreferredSpells ?? Array.Empty<string>(),
                Description = Description ?? ""
            };

            public static CharacterBuildTemplateDto FromTemplate(CharacterBuildTemplate t) => new()
            {
                Name = t.Name,
                Category = "Custom",
                Role = t.Role == TemplateRole.None ? "Support" : t.Role.ToString(),
                Class = t.Class,
                Subclass = t.Subclass,
                Race = t.Race,
                Subrace = t.Subrace,
                Background = t.Background,
                AbilityPriority = t.AbilityPriority ?? Array.Empty<string>(),
                PreferredSkills = t.PreferredSkills ?? Array.Empty<string>(),
                PreferredCantrips = t.PreferredCantrips ?? Array.Empty<string>(),
                PreferredSpells = t.PreferredSpells ?? Array.Empty<string>(),
                Description = t.Description ?? ""
            };
        }
    }
}
