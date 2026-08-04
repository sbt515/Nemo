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
            var classLevels = (template.ClassLevels ?? Array.Empty<TemplateClassLevel>())
                .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                .Select(e => new TemplateClassLevel
                {
                    ClassName = e.ClassName.Trim(),
                    Subclass = e.Subclass?.Trim() ?? "",
                    Levels = Math.Clamp(e.Levels, 1, 20)
                })
                .ToArray();

            int targetLevel = classLevels.Length > 0
                ? Math.Clamp(classLevels.Sum(e => e.Levels), 1, 20)
                : Math.Clamp(template.TargetLevel > 0 ? template.TargetLevel : 1, 1, 20);

            Dictionary<string, string>? cantripAssign = null;
            if (template.CantripClassAssignments is { Count: > 0 })
            {
                cantripAssign = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in template.CantripClassAssignments)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))
                        continue;
                    cantripAssign[kv.Key.Trim()] = kv.Value.Trim();
                }
                if (cantripAssign.Count == 0)
                    cantripAssign = null;
            }

            var asi = (template.AsiOrFeatDecisions ?? Array.Empty<AsiOrFeatDecision>())
                .Where(d => d != null && !string.IsNullOrWhiteSpace(d.ClassName) && d.ClassLevel >= 1)
                .Select(d => new AsiOrFeatDecision
                {
                    ClassName = d.ClassName.Trim(),
                    ClassLevel = d.ClassLevel,
                    Kind = d.Kind,
                    AbilityPlusOneA = d.AbilityPlusOneA ?? "",
                    AbilityPlusOneB = d.AbilityPlusOneB ?? ""
                })
                .ToArray();

            static string[] CleanNames(string[]? src) =>
                (src ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

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
                TargetLevel = targetLevel,
                ClassLevels = classLevels,
                AbilityPriority = template.AbilityPriority ?? Array.Empty<string>(),
                PreferredSkills = template.PreferredSkills ?? Array.Empty<string>(),
                PreferredCantrips = template.PreferredCantrips ?? Array.Empty<string>(),
                PreferredSpells = template.PreferredSpells ?? Array.Empty<string>(),
                CantripClassAssignments = cantripAssign,
                AsiOrFeatDecisions = asi,
                PreferredFightingStyles = CleanNames(template.PreferredFightingStyles),
                PreferredEldritchInvocations = CleanNames(template.PreferredEldritchInvocations),
                PreferredMetamagic = CleanNames(template.PreferredMetamagic),
                PreferredPactBoon = template.PreferredPactBoon?.Trim() ?? "",
                PreferredFightingInitiateStyle = template.PreferredFightingInitiateStyle?.Trim() ?? "",
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
        private sealed class TemplateClassLevelDto
        {
            public string ClassName { get; set; } = "";
            public string Subclass { get; set; } = "";
            public int Levels { get; set; } = 1;
        }

        private sealed class AsiOrFeatDecisionDto
        {
            public string ClassName { get; set; } = "";
            public int ClassLevel { get; set; }
            public string Kind { get; set; } = "Unchosen";
            public string AbilityPlusOneA { get; set; } = "";
            public string AbilityPlusOneB { get; set; } = "";
        }

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
            public int TargetLevel { get; set; } = 1;
            public TemplateClassLevelDto[]? ClassLevels { get; set; }
            public string[] AbilityPriority { get; set; } = Array.Empty<string>();
            public string[] PreferredSkills { get; set; } = Array.Empty<string>();
            public string[] PreferredCantrips { get; set; } = Array.Empty<string>();
            public string[] PreferredSpells { get; set; } = Array.Empty<string>();
            public Dictionary<string, string>? CantripClassAssignments { get; set; }
            public AsiOrFeatDecisionDto[]? AsiOrFeatDecisions { get; set; }
            public string[] PreferredFightingStyles { get; set; } = Array.Empty<string>();
            public string[] PreferredEldritchInvocations { get; set; } = Array.Empty<string>();
            public string[] PreferredMetamagic { get; set; } = Array.Empty<string>();
            public string PreferredPactBoon { get; set; } = "";
            public string PreferredFightingInitiateStyle { get; set; } = "";
            public string Description { get; set; } = "";

            public CharacterBuildTemplate ToTemplate()
            {
                var levels = (ClassLevels ?? Array.Empty<TemplateClassLevelDto>())
                    .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                    .Select(e => new TemplateClassLevel
                    {
                        ClassName = e.ClassName.Trim(),
                        Subclass = e.Subclass?.Trim() ?? "",
                        Levels = Math.Clamp(e.Levels, 1, 20)
                    })
                    .ToArray();

                int target = levels.Length > 0
                    ? Math.Clamp(levels.Sum(e => e.Levels), 1, 20)
                    : Math.Clamp(TargetLevel > 0 ? TargetLevel : 1, 1, 20);

                // Legacy templates: no ClassLevels → single-class at TargetLevel (or 1)
                if (levels.Length == 0 && !string.IsNullOrWhiteSpace(Class))
                {
                    levels = new[]
                    {
                        new TemplateClassLevel
                        {
                            ClassName = Class.Trim(),
                            Subclass = Subclass?.Trim() ?? "",
                            Levels = target
                        }
                    };
                }

                AsiOrFeatDecision[] asi = Array.Empty<AsiOrFeatDecision>();
                if (AsiOrFeatDecisions is { Length: > 0 })
                {
                    asi = AsiOrFeatDecisions
                        .Where(d => d != null && !string.IsNullOrWhiteSpace(d.ClassName) && d.ClassLevel >= 1)
                        .Select(d => new AsiOrFeatDecision
                        {
                            ClassName = d.ClassName.Trim(),
                            ClassLevel = d.ClassLevel,
                            Kind = ParseAsiKind(d.Kind),
                            AbilityPlusOneA = d.AbilityPlusOneA ?? "",
                            AbilityPlusOneB = d.AbilityPlusOneB ?? ""
                        })
                        .ToArray();
                }

                Dictionary<string, string>? cantripAssign = null;
                if (CantripClassAssignments is { Count: > 0 })
                {
                    cantripAssign = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in CantripClassAssignments)
                    {
                        if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))
                            continue;
                        cantripAssign[kv.Key.Trim()] = kv.Value.Trim();
                    }
                    if (cantripAssign.Count == 0)
                        cantripAssign = null;
                }

                static string[] Clean(string[]? src) =>
                    (src ?? Array.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new CharacterBuildTemplate
                {
                    Name = Name?.Trim() ?? "",
                    Category = TemplateCategory.Custom,
                    Role = CharacterTemplateGenerator.ParseRole(Role),
                    Class = Class ?? "",
                    Subclass = Subclass ?? "",
                    Race = Race ?? "",
                    Subrace = Subrace ?? "",
                    Background = Background ?? "",
                    TargetLevel = target,
                    ClassLevels = levels,
                    AbilityPriority = AbilityPriority ?? Array.Empty<string>(),
                    PreferredSkills = PreferredSkills ?? Array.Empty<string>(),
                    PreferredCantrips = PreferredCantrips ?? Array.Empty<string>(),
                    PreferredSpells = PreferredSpells ?? Array.Empty<string>(),
                    CantripClassAssignments = cantripAssign,
                    AsiOrFeatDecisions = asi,
                    PreferredFightingStyles = Clean(PreferredFightingStyles),
                    PreferredEldritchInvocations = Clean(PreferredEldritchInvocations),
                    PreferredMetamagic = Clean(PreferredMetamagic),
                    PreferredPactBoon = PreferredPactBoon?.Trim() ?? "",
                    PreferredFightingInitiateStyle = PreferredFightingInitiateStyle?.Trim() ?? "",
                    Description = Description ?? ""
                };
            }

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
                TargetLevel = t.TargetLevel > 0
                    ? t.TargetLevel
                    : CharacterTemplateGenerator.GetTemplateTotalLevel(t),
                ClassLevels = (t.ClassLevels ?? Array.Empty<TemplateClassLevel>())
                    .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                    .Select(e => new TemplateClassLevelDto
                    {
                        ClassName = e.ClassName,
                        Subclass = e.Subclass ?? "",
                        Levels = e.Levels
                    })
                    .ToArray(),
                AbilityPriority = t.AbilityPriority ?? Array.Empty<string>(),
                PreferredSkills = t.PreferredSkills ?? Array.Empty<string>(),
                PreferredCantrips = t.PreferredCantrips ?? Array.Empty<string>(),
                PreferredSpells = t.PreferredSpells ?? Array.Empty<string>(),
                CantripClassAssignments = t.CantripClassAssignments,
                AsiOrFeatDecisions = (t.AsiOrFeatDecisions ?? Array.Empty<AsiOrFeatDecision>())
                    .Where(d => d != null && !string.IsNullOrWhiteSpace(d.ClassName))
                    .Select(d => new AsiOrFeatDecisionDto
                    {
                        ClassName = d.ClassName,
                        ClassLevel = d.ClassLevel,
                        Kind = d.Kind.ToString(),
                        AbilityPlusOneA = d.AbilityPlusOneA ?? "",
                        AbilityPlusOneB = d.AbilityPlusOneB ?? ""
                    })
                    .ToArray(),
                PreferredFightingStyles = t.PreferredFightingStyles ?? Array.Empty<string>(),
                PreferredEldritchInvocations = t.PreferredEldritchInvocations ?? Array.Empty<string>(),
                PreferredMetamagic = t.PreferredMetamagic ?? Array.Empty<string>(),
                PreferredPactBoon = t.PreferredPactBoon ?? "",
                PreferredFightingInitiateStyle = t.PreferredFightingInitiateStyle ?? "",
                Description = t.Description ?? ""
            };

            private static AsiOrFeatKind ParseAsiKind(string? kind) =>
                (kind ?? "").Trim() switch
                {
                    "AbilityScoreImprovement" => AsiOrFeatKind.AbilityScoreImprovement,
                    "Feat" => AsiOrFeatKind.Feat,
                    _ => AsiOrFeatKind.Unchosen
                };
        }
    }
}
