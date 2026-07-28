using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Nemo
{
    /// <summary>
    /// Lightweight user preferences persisted next to the executable (or under LocalAppData fallback).
    /// </summary>
    public sealed class AppSettings
    {
        public const string DefaultTemplateCategory = "General";
        public const string DefaultTemplateRole = "Support";

        /// <summary>Quick Generate category: Optimized / General / Random / Custom.</summary>
        public string TemplateCategory { get; set; } = DefaultTemplateCategory;

        /// <summary>Quick Generate role: Support / Damage / Tank / True Random.</summary>
        public string TemplateRole { get; set; } = DefaultTemplateRole;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        private static string? _resolvedPath;
        private static AppSettings? _cache;

        /// <summary>Preferred path: app base directory; falls back to LocalAppData\Nemo.</summary>
        public static string GetSettingsPath()
        {
            if (!string.IsNullOrEmpty(_resolvedPath))
                return _resolvedPath;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string primary = Path.Combine(baseDir, "nemo-settings.json");
                // Prefer base dir when writable (dev / portable); otherwise LocalAppData
                try
                {
                    File.WriteAllText(primary + ".writetest", "ok");
                    File.Delete(primary + ".writetest");
                    _resolvedPath = primary;
                    return _resolvedPath;
                }
                catch
                {
                    // not writable
                }
            }
            catch
            {
                // ignore
            }

            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Nemo");
            Directory.CreateDirectory(appData);
            _resolvedPath = Path.Combine(appData, "nemo-settings.json");
            return _resolvedPath;
        }

        public static AppSettings Load()
        {
            if (_cache != null)
                return _cache;

            try
            {
                string path = GetSettingsPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);
                    if (loaded != null)
                    {
                        Normalize(loaded);
                        _cache = loaded;
                        return _cache;
                    }
                }
            }
            catch
            {
                // corrupt / unreadable — use defaults
            }

            _cache = new AppSettings();
            return _cache;
        }

        public static void Save(AppSettings settings)
        {
            if (settings == null) return;
            Normalize(settings);
            _cache = settings;

            try
            {
                string path = GetSettingsPath();
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOpts));
            }
            catch
            {
                // non-fatal: preferences are best-effort
            }
        }

        /// <summary>Update only the Quick Generate selections and persist.</summary>
        public static void SaveTemplateSelection(string? category, string? role)
        {
            var s = Load();
            if (!string.IsNullOrWhiteSpace(category))
                s.TemplateCategory = category.Trim();
            if (!string.IsNullOrWhiteSpace(role))
                s.TemplateRole = role.Trim();
            Save(s);
        }

        private static void Normalize(AppSettings s)
        {
            string cat = (s.TemplateCategory ?? "").Trim();
            if (!CharacterTemplateGenerator.CategoryNames.Any(c =>
                    c.Equals(cat, StringComparison.OrdinalIgnoreCase)))
                cat = DefaultTemplateCategory;
            else
                cat = CharacterTemplateGenerator.CategoryNames.First(c =>
                    c.Equals(cat, StringComparison.OrdinalIgnoreCase));

            // Custom is only valid when the user has saved at least one template
            if (cat.Equals("Custom", StringComparison.OrdinalIgnoreCase) &&
                !CustomTemplateStore.HasAny())
                cat = DefaultTemplateCategory;

            s.TemplateCategory = cat;

            bool isRandom = cat.Equals("Random", StringComparison.OrdinalIgnoreCase);
            string[] validRoles = isRandom
                ? CharacterTemplateGenerator.RandomRoleNames
                : CharacterTemplateGenerator.RoleNames;

            string role = (s.TemplateRole ?? "").Trim();
            if (!validRoles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
            {
                // e.g. "True Random" saved under Optimized → fall back to Support
                if (CharacterTemplateGenerator.RoleNames.Any(r =>
                        r.Equals(role, StringComparison.OrdinalIgnoreCase)))
                    role = CharacterTemplateGenerator.RoleNames.First(r =>
                        r.Equals(role, StringComparison.OrdinalIgnoreCase));
                else
                    role = DefaultTemplateRole;
            }
            else
            {
                role = validRoles.First(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
            }

            s.TemplateRole = role;
        }
    }
}
