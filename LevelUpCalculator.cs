using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nemo
{
    /// <summary>How hit points are gained for levels after 1st.</summary>
    public enum HpGainMethod
    {
        /// <summary>
        /// PHB average: floor(hitDie / 2) + 1 + Constitution modifier
        /// (d6→4, d8→5, d10→6, d12→7).
        /// </summary>
        FixedAverage = 0,

        /// <summary>Roll the class hit die and add Constitution modifier (minimum 1 HP per level).</summary>
        Rolled = 1
    }

    /// <summary>One class's hit dice in the character's total pool (e.g. 3d10 for Fighter 3).</summary>
    public sealed class HitDicePoolEntry
    {
        public string ClassName { get; init; } = "";
        public int DieSize { get; init; }
        public int Count { get; init; }

        public override string ToString() => Count <= 0 ? "" : $"{Count}d{DieSize}";
    }

    /// <summary>A class-unique resource that scales with class level (Rage, Ki, Action Surge, …).</summary>
    public sealed class ClassResourceValue
    {
        public string Name { get; init; } = "";
        /// <summary>Human-readable current amount (e.g. "3", "Unlimited", "2d6", "d8").</summary>
        public string Value { get; init; } = "";
        /// <summary>Optional numeric max when applicable (uses, points, pool size).</summary>
        public int? NumericMax { get; init; }
        /// <summary>e.g. "long rest", "short rest", "passive", "scales with level".</summary>
        public string Recharge { get; init; } = "";
        public string Notes { get; init; } = "";

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name).Append(": ").Append(Value);
            if (!string.IsNullOrWhiteSpace(Recharge) &&
                !Recharge.Equals("passive", StringComparison.OrdinalIgnoreCase))
                sb.Append(" (").Append(Recharge).Append(')');
            if (!string.IsNullOrWhiteSpace(Notes))
                sb.Append(" — ").Append(Notes);
            return sb.ToString();
        }
    }

    /// <summary>One ASI / feat choice granted by a class level.</summary>
    public sealed class AsiOrFeatChoice
    {
        public string ClassName { get; init; } = "";
        public int ClassLevel { get; init; }
        public override string ToString() => $"{ClassName} {ClassLevel}: ASI or Feat";
    }

    /// <summary>Player decision for an ASI/feat milestone.</summary>
    public enum AsiOrFeatKind
    {
        Unchosen = 0,
        AbilityScoreImprovement = 1,
        Feat = 2
    }

    /// <summary>
    /// Persisted ASI or Feat pick for a specific class level (e.g. Fighter 4).
    /// ASI applies two +1 increments (same ability twice = +2).
    /// </summary>
    public sealed class AsiOrFeatDecision
    {
        public string ClassName { get; set; } = "";
        public int ClassLevel { get; set; }
        public AsiOrFeatKind Kind { get; set; } = AsiOrFeatKind.Unchosen;
        /// <summary>First +1 ability (e.g. "Strength").</summary>
        public string AbilityPlusOneA { get; set; } = "";
        /// <summary>Second +1 ability (same as A for +2 to one score).</summary>
        public string AbilityPlusOneB { get; set; } = "";

        public string Key => $"{ClassName?.Trim() ?? ""}|{ClassLevel}";

        public override string ToString() => Kind switch
        {
            AsiOrFeatKind.Feat => $"{ClassName} {ClassLevel}: Feat",
            AsiOrFeatKind.AbilityScoreImprovement =>
                $"{ClassName} {ClassLevel}: ASI ({AbilityPlusOneA}/ {AbilityPlusOneB})",
            _ => $"{ClassName} {ClassLevel}: (unchosen)"
        };
    }

    /// <summary>
    /// Complete calculated snapshot for a character's levels (single-class or multiclass).
    /// Pure rules math — no UI dependency.
    /// </summary>
    public sealed class CharacterLevelSnapshot
    {
        public IReadOnlyList<ClassLevelEntry> ClassLevels { get; init; } = Array.Empty<ClassLevelEntry>();
        public int TotalCharacterLevel { get; init; }
        public int ProficiencyBonus { get; init; }
        public int ConstitutionModifier { get; init; }
        public HpGainMethod HpMethod { get; init; }

        /// <summary>Hit point maximum (before temporary HP; includes racial bonuses if provided).</summary>
        public int HitPointMaximum { get; init; }

        /// <summary>Per-level HP gained (index 0 = level 1 of character, after that each gained level).</summary>
        public IReadOnlyList<int> HitPointsByLevelGain { get; init; } = Array.Empty<int>();

        /// <summary>Hit dice pool, e.g. Fighter 3 / Cleric 2 → 3d10 + 2d8.</summary>
        public IReadOnlyList<HitDicePoolEntry> HitDicePool { get; init; } = Array.Empty<HitDicePoolEntry>();
        public string HitDicePoolDisplay { get; init; } = "";

        /// <summary>ASI/feat choices available from all class levels taken so far.</summary>
        public IReadOnlyList<AsiOrFeatChoice> AsiOrFeatChoices { get; init; } = Array.Empty<AsiOrFeatChoice>();

        /// <summary>Class resources from each class (keyed by class name).</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<ClassResourceValue>> ResourcesByClass { get; init; }
            = new Dictionary<string, IReadOnlyList<ClassResourceValue>>();

        /// <summary>Flat list of all class resources.</summary>
        public IReadOnlyList<ClassResourceValue> AllResources { get; init; } = Array.Empty<ClassResourceValue>();

        public MulticlassSpellSlotResult SpellSlots { get; init; } = new();

        /// <summary>Features gained from base class + subclass up to each class's level (not full text dump by default).</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<ClassFeature>> ClassFeaturesByClass { get; init; }
            = new Dictionary<string, IReadOnlyList<ClassFeature>>();

        /// <summary>
        /// Per-class subclass spell grants (always prepared / always known / expanded list)
        /// and prepared-spell capacity. Always-prepared spells do not count against capacity.
        /// </summary>
        public IReadOnlyList<SubclassSpellSnapshot> SubclassSpellsByClass { get; init; }
            = Array.Empty<SubclassSpellSnapshot>();

        /// <summary>Flat list of all always-prepared subclass spells across classes.</summary>
        public IReadOnlyList<SubclassSpellGrant> AllAlwaysPreparedSpells { get; init; }
            = Array.Empty<SubclassSpellGrant>();
    }

    /// <summary>
    /// Delta when taking one class level — for level-up UI (fixed vs roll HP, ASI, features, slots).
    /// </summary>
    public sealed class LevelUpDelta
    {
        public string ClassName { get; init; } = "";
        public string? Subclass { get; init; }
        public int NewClassLevel { get; init; }
        public int TotalCharacterLevelAfter { get; init; }
        public int ProficiencyBonusAfter { get; init; }
        public bool ProficiencyBonusIncreased { get; init; }
        public int HitDieSize { get; init; }
        public int HitPointsGained { get; init; }
        /// <summary>HP you would gain if choosing fixed average (after 1st level).</summary>
        public int FixedAverageHpOption { get; init; }
        public bool IsCharacterLevel1 { get; init; }
        public bool GrantsAsiOrFeat { get; init; }
        public IReadOnlyList<ClassFeature> FeaturesGained { get; init; } = Array.Empty<ClassFeature>();
        public IReadOnlyList<ClassResourceValue> ResourcesAfter { get; init; } = Array.Empty<ClassResourceValue>();
        public IReadOnlyList<ClassResourceValue> ResourcesBefore { get; init; } = Array.Empty<ClassResourceValue>();
        public MulticlassSpellSlotResult SpellSlotsAfter { get; init; } = new();
        public MulticlassSpellSlotResult SpellSlotsBefore { get; init; } = new();

        /// <summary>
        /// Subclass spells newly gained at this class level (domain/oath/etc.).
        /// Always-prepared entries do not count against prepared capacity.
        /// </summary>
        public IReadOnlyList<SubclassSpellGrant> SubclassSpellsGained { get; init; }
            = Array.Empty<SubclassSpellGrant>();

        /// <summary>All always-prepared subclass spells after this level-up.</summary>
        public IReadOnlyList<SubclassSpellGrant> AlwaysPreparedSpellsAfter { get; init; }
            = Array.Empty<SubclassSpellGrant>();

        /// <summary>All always-prepared subclass spells before this level-up.</summary>
        public IReadOnlyList<SubclassSpellGrant> AlwaysPreparedSpellsBefore { get; init; }
            = Array.Empty<SubclassSpellGrant>();

        /// <summary>
        /// How many spells the player may still choose to prepare after this level
        /// (excludes free always-prepared subclass spells). Null if ability mod not provided
        /// or class is not a prepared caster.
        /// </summary>
        public int? PreparedSpellCapacityAfter { get; init; }

        /// <summary>Prepared capacity before this level-up (same notes as After).</summary>
        public int? PreparedSpellCapacityBefore { get; init; }
    }

    /// <summary>
    /// Official 5e level-up math: hit points, proficiency, ASI/feats, class resources, hit dice, spell slots.
    /// Multiclass spell slots delegate to <see cref="SpellSlotCalculator"/>.
    /// </summary>
    public static class LevelUpCalculator
    {
        // ═══════════════════════════════════════════════════════════════
        // Public API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Build a full snapshot for the given class levels.
        /// </summary>
        /// <param name="classLevels">One or more class contributions (Fighter 3, Cleric 2, …).</param>
        /// <param name="constitutionModifier">Constitution ability modifier.</param>
        /// <param name="hpMethod">Fixed average or rolled.</param>
        /// <param name="rolledHpResults">
        /// When <paramref name="hpMethod"/> is <see cref="HpGainMethod.Rolled"/>, the die result
        /// (before Con) for each level after the character's 1st level, in order of levels gained.
        /// If shorter than needed, remaining levels use fixed average. If longer, extras are ignored.
        /// Level 1 always uses max hit die (never rolled).
        /// </param>
        /// <param name="extraHpPerLevel">
        /// Flat bonus each character level (Hill Dwarf +1, Draconic Resilience +1, Tough feat +2, etc.).
        /// Applied at every level including 1st.
        /// </param>
        /// <param name="includeFeatures">When true, attach base + subclass features up to each class level.</param>
        /// <param name="spellcastingAbilityModifierForClass">
        /// Optional: maps class name → spellcasting ability modifier for prepared-spell capacity.
        /// When null, capacity is left unset on subclass-spell snapshots.
        /// </param>
        /// <param name="landCircleVariant">
        /// Optional Circle of the Land terrain (Arctic, Forest, …) for land-circle spell grants.
        /// </param>
        public static CharacterLevelSnapshot Calculate(
            IEnumerable<ClassLevelEntry> classLevels,
            int constitutionModifier,
            HpGainMethod hpMethod = HpGainMethod.FixedAverage,
            IReadOnlyList<int>? rolledHpResults = null,
            int extraHpPerLevel = 0,
            bool includeFeatures = false,
            Func<string, int>? spellcastingAbilityModifierForClass = null,
            string? landCircleVariant = null)
        {
            var entries = Normalize(classLevels);
            int totalLevel = Math.Clamp(entries.Sum(e => e.Levels), 0, 20);

            var hp = CalculateHitPoints(entries, constitutionModifier, hpMethod, rolledHpResults, extraHpPerLevel);
            var hitDice = GetHitDicePool(entries);
            var asi = GetAsiOrFeatChoices(entries);
            var resources = GetAllResources(entries);
            var slots = SpellSlotCalculator.Calculate(entries);
            var subclassSpells = SubclassSpellCalculator.CalculateAll(
                entries, spellcastingAbilityModifierForClass, landCircleVariant);

            Dictionary<string, IReadOnlyList<ClassFeature>> features = new(StringComparer.OrdinalIgnoreCase);
            if (includeFeatures)
            {
                foreach (var e in entries)
                {
                    var list = new List<ClassFeature>();
                    list.AddRange(GameData.GetClassFeaturesUpToLevel(e.ClassName, e.Levels, includeOptional: true));
                    string? effectiveSub = GameData.GetEffectiveSubclass(e);
                    if (!string.IsNullOrWhiteSpace(effectiveSub))
                        list.AddRange(GameData.GetSubclassFeaturesUpToLevel(effectiveSub, e.Levels));
                    features[e.ClassName] = list;
                }
            }

            return new CharacterLevelSnapshot
            {
                ClassLevels = entries,
                TotalCharacterLevel = totalLevel,
                ProficiencyBonus = GetProficiencyBonus(totalLevel),
                ConstitutionModifier = constitutionModifier,
                HpMethod = hpMethod,
                HitPointMaximum = hp.Total,
                HitPointsByLevelGain = hp.PerLevel,
                HitDicePool = hitDice,
                HitDicePoolDisplay = FormatHitDicePool(hitDice),
                AsiOrFeatChoices = asi,
                ResourcesByClass = resources,
                AllResources = resources.Values.SelectMany(x => x).ToList(),
                SpellSlots = slots,
                ClassFeaturesByClass = features,
                SubclassSpellsByClass = subclassSpells,
                AllAlwaysPreparedSpells = subclassSpells
                    .SelectMany(s => s.AlwaysPreparedSpells)
                    .ToList()
            };
        }

        /// <summary>Single-class convenience overload.</summary>
        public static CharacterLevelSnapshot Calculate(
            string className,
            int classLevel,
            int constitutionModifier,
            string? subclass = null,
            HpGainMethod hpMethod = HpGainMethod.FixedAverage,
            IReadOnlyList<int>? rolledHpResults = null,
            int extraHpPerLevel = 0,
            bool includeFeatures = false,
            int? spellcastingAbilityModifier = null,
            string? landCircleVariant = null) =>
            Calculate(
                new[] { new ClassLevelEntry(className, classLevel, subclass) },
                constitutionModifier,
                hpMethod,
                rolledHpResults,
                extraHpPerLevel,
                includeFeatures,
                spellcastingAbilityModifier.HasValue
                    ? _ => spellcastingAbilityModifier.Value
                    : null,
                landCircleVariant);

        /// <summary>
        /// Resolve class levels from a <see cref="Character"/> (multiclass list, or single Class/Level).
        /// </summary>
        public static List<ClassLevelEntry> GetClassLevelsFromCharacter(Character character)
        {
            if (character == null)
                return new List<ClassLevelEntry>();

            if (character.ClassLevels != null && character.ClassLevels.Count > 0)
            {
                var fromLevels = character.ClassLevels
                    .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                    .Select(e => new ClassLevelEntry(e.ClassName, e.Levels, e.Subclass))
                    .ToList();
                // If every row was empty/invalid, fall through to Class + Level
                if (fromLevels.Count > 0)
                    return fromLevels;
            }

            if (string.IsNullOrWhiteSpace(character.Class))
                return new List<ClassLevelEntry>();

            int lvl = character.Level > 0 ? character.Level : 1;
            return new List<ClassLevelEntry>
            {
                new(character.Class, lvl, character.Subclass)
            };
        }

        /// <summary>
        /// What you gain when you take a specific class level (delta from previous class level).
        /// Used by a future level-up UI: HP options, ASI/feat, new features, resource changes, spell slots,
        /// and newly unlocked always-prepared subclass spells (domain/oath/etc.).
        /// </summary>
        /// <param name="spellcastingAbilityModifier">
        /// Optional spellcasting ability modifier for prepared-spell capacity on this class.
        /// </param>
        /// <param name="landCircleVariant">
        /// Optional Circle of the Land terrain for land-circle spell grants.
        /// </param>
        public static LevelUpDelta GetLevelUpDelta(
            string className,
            int newClassLevel,
            int constitutionModifier,
            string? subclass = null,
            IEnumerable<ClassLevelEntry>? otherClassLevels = null,
            HpGainMethod hpMethod = HpGainMethod.FixedAverage,
            int? rolledDieResult = null,
            int extraHpPerLevel = 0,
            int? spellcastingAbilityModifier = null,
            string? landCircleVariant = null)
        {
            if (newClassLevel < 1 || newClassLevel > 20)
                throw new ArgumentOutOfRangeException(nameof(newClassLevel));

            int prevClassLevel = newClassLevel - 1;

            // Character level 1 only when no other class levels and this is the first level in this class
            int otherLevels = otherClassLevels?
                .Where(e => e != null &&
                            e.Levels > 0 &&
                            !e.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.Levels) ?? 0;

            bool isFirstCharacterLevel = otherLevels == 0 && prevClassLevel == 0;

            int hpGain = isFirstCharacterLevel
                ? GetLevel1HitPoints(className, constitutionModifier, extraHpPerLevel)
                : GetHitPointsForLevel(className, constitutionModifier, hpMethod, rolledDieResult, extraHpPerLevel);

            int fixedAvgOption = isFirstCharacterLevel
                ? GetLevel1HitPoints(className, constitutionModifier, extraHpPerLevel)
                : GetHitPointsForLevel(
                    className, constitutionModifier, HpGainMethod.FixedAverage, null, extraHpPerLevel);

            var beforeEntries = BuildEntriesAfterLevel(className, prevClassLevel, subclass, otherClassLevels);
            var afterEntries = BuildEntriesAfterLevel(className, newClassLevel, subclass, otherClassLevels);

            var slotsBefore = SpellSlotCalculator.Calculate(beforeEntries);
            var slotsAfter = SpellSlotCalculator.Calculate(afterEntries);

            var resourcesBefore = prevClassLevel > 0
                ? GetClassResources(className, prevClassLevel, 0, subclass)
                : new List<ClassResourceValue>();
            var resourcesAfter = GetClassResources(className, newClassLevel, 0, subclass);

            string? effectiveSub = GameData.GetEffectiveSubclass(className, newClassLevel, subclass);
            string? effectiveSubBefore = GameData.GetEffectiveSubclass(className, prevClassLevel, subclass);

            var featuresAtLevel = GameData.GetClassFeaturesAtLevel(className, newClassLevel, includeOptional: true);
            if (!string.IsNullOrWhiteSpace(effectiveSub))
                featuresAtLevel.AddRange(GameData.GetSubclassFeaturesAtLevel(effectiveSub!, newClassLevel));

            // Subclass always-prepared / always-known spells gained at this level
            string? variant = (!string.IsNullOrWhiteSpace(effectiveSub) &&
                               effectiveSub!.Contains("Land", StringComparison.OrdinalIgnoreCase))
                ? landCircleVariant
                : null;

            var spellsGained = SubclassSpellCalculator.GetGrantsGainedAtLevel(
                effectiveSub, newClassLevel, kindFilter: null, variant);
            var preparedBefore = prevClassLevel > 0 && !string.IsNullOrWhiteSpace(effectiveSubBefore)
                ? SubclassSpellCalculator.GetAlwaysPreparedSpells(effectiveSubBefore, prevClassLevel, variant)
                : Array.Empty<SubclassSpellGrant>();
            var preparedAfter = !string.IsNullOrWhiteSpace(effectiveSub)
                ? SubclassSpellCalculator.GetAlwaysPreparedSpells(effectiveSub, newClassLevel, variant)
                : Array.Empty<SubclassSpellGrant>();

            int? prepCapBefore = null;
            int? prepCapAfter = null;
            if (spellcastingAbilityModifier.HasValue &&
                SubclassSpellCalculator.IsPreparedCaster(className))
            {
                if (prevClassLevel > 0)
                    prepCapBefore = SubclassSpellCalculator.GetPreparedSpellCapacity(
                        className, prevClassLevel, spellcastingAbilityModifier.Value);
                prepCapAfter = SubclassSpellCalculator.GetPreparedSpellCapacity(
                    className, newClassLevel, spellcastingAbilityModifier.Value);
            }

            int totalAfter = afterEntries.Sum(e => e.Levels);
            int totalBefore = Math.Max(0, totalAfter - 1);

            return new LevelUpDelta
            {
                ClassName = className,
                Subclass = subclass,
                NewClassLevel = newClassLevel,
                TotalCharacterLevelAfter = totalAfter,
                ProficiencyBonusAfter = GetProficiencyBonus(totalAfter),
                ProficiencyBonusIncreased =
                    GetProficiencyBonus(totalAfter) > GetProficiencyBonus(totalBefore),
                HitDieSize = GetHitDieSize(className),
                HitPointsGained = hpGain,
                FixedAverageHpOption = fixedAvgOption,
                IsCharacterLevel1 = isFirstCharacterLevel,
                GrantsAsiOrFeat = GrantsAsiOrFeat(className, newClassLevel),
                FeaturesGained = featuresAtLevel,
                ResourcesAfter = resourcesAfter,
                ResourcesBefore = resourcesBefore,
                SpellSlotsAfter = slotsAfter,
                SpellSlotsBefore = slotsBefore,
                SubclassSpellsGained = spellsGained,
                AlwaysPreparedSpellsAfter = preparedAfter,
                AlwaysPreparedSpellsBefore = preparedBefore,
                PreparedSpellCapacityAfter = prepCapAfter,
                PreparedSpellCapacityBefore = prepCapBefore
            };
        }

        private static List<ClassLevelEntry> BuildEntriesAfterLevel(
            string className,
            int classLevel,
            string? subclass,
            IEnumerable<ClassLevelEntry>? otherClassLevels)
        {
            var list = new List<ClassLevelEntry>();
            if (otherClassLevels != null)
            {
                foreach (var e in otherClassLevels)
                {
                    if (e == null || e.Levels <= 0 || string.IsNullOrWhiteSpace(e.ClassName))
                        continue;
                    if (e.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase))
                        continue; // replaced below
                    list.Add(new ClassLevelEntry(e.ClassName, e.Levels, e.Subclass));
                }
            }
            if (classLevel > 0)
                list.Add(new ClassLevelEntry(className, classLevel, subclass));
            return list;
        }

        // ═══════════════════════════════════════════════════════════════
        // Proficiency bonus (character level)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// PHB proficiency bonus by total character level:
        /// 1–4 → +2, 5–8 → +3, 9–12 → +4, 13–16 → +5, 17–20 → +6.
        /// </summary>
        public static int GetProficiencyBonus(int characterLevel)
        {
            if (characterLevel <= 0) return 0;
            int lvl = Math.Clamp(characterLevel, 1, 20);
            return (lvl - 1) / 4 + 2;
        }

        // ═══════════════════════════════════════════════════════════════
        // Hit dice
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Hit die size for a class (6, 8, 10, or 12).</summary>
        public static int GetHitDieSize(string className)
        {
            if (string.IsNullOrWhiteSpace(className)) return 8;

            // Prefer ClassData when present
            if (GameData.ClassData.TryGetValue(className.Trim(), out var data) &&
                !string.IsNullOrWhiteSpace(data.HitDie))
            {
                // Expect "1d8", "1d10", etc.
                int d = data.HitDie.IndexOf('d');
                if (d >= 0 && int.TryParse(data.HitDie.AsSpan(d + 1), out int size) && size > 0)
                    return size;
            }

            return className.Trim() switch
            {
                "Sorcerer" or "Wizard" => 6,
                "Artificer" or "Bard" or "Cleric" or "Druid" or "Monk" or "Rogue" or "Warlock" => 8,
                "Fighter" or "Paladin" or "Ranger" => 10,
                "Barbarian" => 12,
                _ => 8
            };
        }

        /// <summary>
        /// Fixed average HP from the hit die alone (no Con): floor(die/2)+1.
        /// d6→4, d8→5, d10→6, d12→7.
        /// </summary>
        public static int GetFixedAverageHitDieValue(int hitDieSize) =>
            Math.Max(1, hitDieSize / 2 + 1);

        /// <summary>Hit dice pool for multiclass (e.g. 3d10 + 2d8).</summary>
        public static List<HitDicePoolEntry> GetHitDicePool(IEnumerable<ClassLevelEntry> classLevels)
        {
            return Normalize(classLevels)
                .Where(e => e.Levels > 0)
                .Select(e => new HitDicePoolEntry
                {
                    ClassName = e.ClassName,
                    DieSize = GetHitDieSize(e.ClassName),
                    Count = e.Levels
                })
                .OrderByDescending(h => h.DieSize)
                .ThenBy(h => h.ClassName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Character-sheet style hit dice total, e.g. <c>5d10</c> or <c>5d10/3d8</c>.
        /// </summary>
        public static string FormatHitDicePool(IEnumerable<HitDicePoolEntry> pool)
        {
            var parts = pool.Where(p => p.Count > 0).Select(p => p.ToString()).Where(s => s.Length > 0);
            string s = string.Join("/", parts);
            return string.IsNullOrEmpty(s) ? "—" : s;
        }

        // ═══════════════════════════════════════════════════════════════
        // Expertise / Jack of All Trades
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// How many skill Expertise picks the character may have (Rogue 2@1 + 2@6; Bard 2@3 + 2@10).
        /// </summary>
        public static int GetExpertiseSkillSlots(IEnumerable<ClassLevelEntry>? classLevels)
        {
            int n = 0;
            foreach (var e in Normalize(classLevels))
            {
                string cn = e.ClassName.Trim();
                if (cn.Equals("Rogue", StringComparison.OrdinalIgnoreCase))
                {
                    if (e.Levels >= 1) n += 2;
                    if (e.Levels >= 6) n += 2;
                }
                else if (cn.Equals("Bard", StringComparison.OrdinalIgnoreCase))
                {
                    if (e.Levels >= 3) n += 2;
                    if (e.Levels >= 10) n += 2;
                }
            }
            return n;
        }

        /// <summary>Bard 2+: half proficiency (round down) on ability checks that lack proficiency.</summary>
        public static bool HasJackOfAllTrades(IEnumerable<ClassLevelEntry>? classLevels) =>
            Normalize(classLevels).Any(e =>
                e.ClassName.Equals("Bard", StringComparison.OrdinalIgnoreCase) && e.Levels >= 2);

        /// <summary>
        /// Skill / ability-check bonus: ability mod + proficiency (×2 if expertise), or half prof if JoAT.
        /// </summary>
        public static int ComputeSkillBonus(
            int abilityModifier,
            int proficiencyBonus,
            bool isProficient,
            bool isExpertise,
            bool jackOfAllTrades)
        {
            if (isProficient)
            {
                int mult = isExpertise ? 2 : 1;
                return abilityModifier + proficiencyBonus * mult;
            }

            if (jackOfAllTrades)
                return abilityModifier + proficiencyBonus / 2;

            return abilityModifier;
        }

        // ═══════════════════════════════════════════════════════════════
        // Hit points
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// HP gained at 1st level in a class: max hit die + Con mod (+ optional flat extras).
        /// Minimum contribution still applies Con (can be negative) but total HP floor is 1 at level 1.
        /// </summary>
        public static int GetLevel1HitPoints(string className, int constitutionModifier, int extraHpPerLevel = 0)
        {
            int die = GetHitDieSize(className);
            int hp = die + constitutionModifier + extraHpPerLevel;
            return Math.Max(1, hp);
        }

        /// <summary>
        /// HP gained for one level after 1st in <paramref name="className"/>.
        /// Fixed: average + Con + extras. Rolled: roll + Con + extras.
        /// PHB: you gain at least 1 hit point whenever you gain a level.
        /// </summary>
        public static int GetHitPointsForLevel(
            string className,
            int constitutionModifier,
            HpGainMethod method,
            int? rolledDieResult = null,
            int extraHpPerLevel = 0)
        {
            int die = GetHitDieSize(className);
            // Valid die results are 1..die. Missing/0/null while Rolled → fall back to fixed average.
            bool useRoll = method == HpGainMethod.Rolled &&
                           rolledDieResult.HasValue &&
                           rolledDieResult.Value >= 1;
            int baseFromDie = useRoll
                ? Math.Clamp(rolledDieResult!.Value, 1, die)
                : GetFixedAverageHitDieValue(die);

            int hp = baseFromDie + constitutionModifier + extraHpPerLevel;
            return Math.Max(1, hp);
        }

        /// <summary>
        /// Full HP max for a multiclass character.
        /// Level order: first entry in the list is assumed to be the class taken at character level 1
        /// (max hit die). Subsequent class levels use fixed/rolled progression.
        /// When multiple classes exist, levels are applied in list order, spending each class's levels
        /// sequentially (first class's levels first, then the next, …).
        /// </summary>
        public static (int Total, List<int> PerLevel) CalculateHitPoints(
            IEnumerable<ClassLevelEntry> classLevels,
            int constitutionModifier,
            HpGainMethod method = HpGainMethod.FixedAverage,
            IReadOnlyList<int>? rolledHpResults = null,
            int extraHpPerLevel = 0)
        {
            var entries = Normalize(classLevels);
            var perLevel = new List<int>();
            if (entries.Count == 0)
                return (0, perLevel);

            int rollIndex = 0;
            bool isFirstCharacterLevel = true;
            int total = 0;

            foreach (var entry in entries)
            {
                for (int i = 0; i < entry.Levels; i++)
                {
                    int gain;
                    if (isFirstCharacterLevel)
                    {
                        // Character level 1: always max hit die of this class
                        gain = GetLevel1HitPoints(entry.ClassName, constitutionModifier, extraHpPerLevel);
                        isFirstCharacterLevel = false;
                    }
                    else
                    {
                        int? roll = null;
                        if (method == HpGainMethod.Rolled &&
                            rolledHpResults != null &&
                            rollIndex < rolledHpResults.Count)
                        {
                            roll = rolledHpResults[rollIndex];
                        }
                        rollIndex++;
                        gain = GetHitPointsForLevel(
                            entry.ClassName, constitutionModifier, method, roll, extraHpPerLevel);
                    }

                    perLevel.Add(gain);
                    total += gain;
                }
            }

            return (total, perLevel);
        }

        // ═══════════════════════════════════════════════════════════════
        // ASI / Feat (per class level, not character level)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Class levels that grant Ability Score Improvement (or a feat, if allowed).
        /// Standard: 4, 8, 12, 16, 19.
        /// Fighter also: 6, 14.
        /// Rogue also: 10.
        /// </summary>
        public static IReadOnlyList<int> GetAsiLevels(string className)
        {
            string cls = (className ?? "").Trim();
            if (cls.Equals("Fighter", StringComparison.OrdinalIgnoreCase))
                return new[] { 4, 6, 8, 12, 14, 16, 19 };
            if (cls.Equals("Rogue", StringComparison.OrdinalIgnoreCase))
                return new[] { 4, 8, 10, 12, 16, 19 };
            // All other PHB/official classes
            return new[] { 4, 8, 12, 16, 19 };
        }

        /// <summary>True if taking this class level grants an ASI or feat choice.</summary>
        public static bool GrantsAsiOrFeat(string className, int classLevel) =>
            classLevel >= 1 && GetAsiLevels(className).Contains(classLevel);

        /// <summary>All ASI/feat choices earned across the given class levels.</summary>
        public static List<AsiOrFeatChoice> GetAsiOrFeatChoices(IEnumerable<ClassLevelEntry> classLevels)
        {
            var result = new List<AsiOrFeatChoice>();
            foreach (var e in Normalize(classLevels))
            {
                foreach (int lvl in GetAsiLevels(e.ClassName))
                {
                    if (lvl <= e.Levels)
                    {
                        result.Add(new AsiOrFeatChoice
                        {
                            ClassName = e.ClassName,
                            ClassLevel = lvl
                        });
                    }
                }
            }
            return result
                .OrderBy(a => a.ClassName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.ClassLevel)
                .ToList();
        }

        /// <summary>
        /// Ensure a decisions list has one entry per earned ASI/feat slot; drop obsolete slots.
        /// Preserves existing Kind / ability picks when the slot still exists.
        /// </summary>
        public static List<AsiOrFeatDecision> ReconcileAsiOrFeatDecisions(
            IEnumerable<ClassLevelEntry> classLevels,
            IEnumerable<AsiOrFeatDecision>? existing)
        {
            var earned = GetAsiOrFeatChoices(classLevels);
            var map = new Dictionary<string, AsiOrFeatDecision>(StringComparer.OrdinalIgnoreCase);
            if (existing != null)
            {
                foreach (var d in existing)
                {
                    if (d == null || string.IsNullOrWhiteSpace(d.ClassName) || d.ClassLevel < 1)
                        continue;
                    map[d.Key] = d;
                }
            }

            var result = new List<AsiOrFeatDecision>();
            foreach (var slot in earned)
            {
                string key = $"{slot.ClassName}|{slot.ClassLevel}";
                if (map.TryGetValue(key, out var prior))
                {
                    prior.ClassName = slot.ClassName;
                    prior.ClassLevel = slot.ClassLevel;
                    result.Add(prior);
                }
                else
                {
                    result.Add(new AsiOrFeatDecision
                    {
                        ClassName = slot.ClassName,
                        ClassLevel = slot.ClassLevel,
                        Kind = AsiOrFeatKind.Unchosen
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// Sum of ability score increases from ASI decisions (not feats).
        /// Keys: Strength, Dexterity, …
        /// </summary>
        public static Dictionary<string, int> GetAsiStatBonuses(IEnumerable<AsiOrFeatDecision>? decisions)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (decisions == null) return result;

            foreach (var d in decisions)
            {
                if (d == null || d.Kind != AsiOrFeatKind.AbilityScoreImprovement)
                    continue;
                AddAsiPoint(result, d.AbilityPlusOneA);
                AddAsiPoint(result, d.AbilityPlusOneB);
            }
            return result;
        }

        private static void AddAsiPoint(Dictionary<string, int> map, string? ability)
        {
            if (string.IsNullOrWhiteSpace(ability)) return;
            string a = ability.Trim();
            map[a] = map.GetValueOrDefault(a, 0) + 1;
        }

        /// <summary>How many feat picks were granted by ASI/feat milestones (not racial origin feats).</summary>
        public static int CountFeatPicksFromAsi(IEnumerable<AsiOrFeatDecision>? decisions) =>
            decisions?.Count(d => d != null && d.Kind == AsiOrFeatKind.Feat) ?? 0;

        // ═══════════════════════════════════════════════════════════════
        // Multiclass proficiencies (PHB table — armor/weapons only for Nemo UI)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// When you gain your <em>first</em> level in a class other than your initial class,
        /// you gain only these starting proficiencies (PHB Multiclassing Proficiencies table).
        /// You do <strong>not</strong> gain the class's full 1st-level armor/weapon/skill/save list.
        /// Class features at that level (e.g. Divine Domain bonus proficiencies) still apply normally.
        /// </summary>
        public sealed class MulticlassProficiencyGrant
        {
            public IReadOnlyList<string> Armor { get; init; } = Array.Empty<string>();
            public IReadOnlyList<string> Weapons { get; init; } = Array.Empty<string>();
            /// <summary>Tools / skills called out by the table (for display; skills not auto-picked).</summary>
            public IReadOnlyList<string> Other { get; init; } = Array.Empty<string>();
        }

        /// <summary>
        /// PHB ( + Artificer from Tasha's/ERftLW) Multiclassing Proficiencies table.
        /// </summary>
        public static MulticlassProficiencyGrant GetMulticlassProficiencies(string className)
        {
            return (className ?? "").Trim() switch
            {
                "Barbarian" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Shields" },
                    Weapons = new[] { "Simple weapons", "Martial weapons" }
                },
                "Bard" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor" },
                    Other = new[] { "One skill of your choice", "One musical instrument of your choice" }
                },
                "Cleric" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor", "Medium armor", "Shields" }
                    // No weapons on multiclass table (not simple weapons)
                },
                "Druid" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor", "Medium armor", "Shields (non-metal)" }
                },
                "Fighter" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor", "Medium armor", "Shields" },
                    Weapons = new[] { "Simple weapons", "Martial weapons" }
                },
                "Monk" => new MulticlassProficiencyGrant
                {
                    Weapons = new[] { "Simple weapons", "Shortswords" }
                },
                "Paladin" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor", "Medium armor", "Shields" },
                    Weapons = new[] { "Simple weapons", "Martial weapons" }
                },
                "Ranger" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor", "Medium armor", "Shields" },
                    Weapons = new[] { "Simple weapons", "Martial weapons" },
                    Other = new[] { "One skill from the class's skill list" }
                },
                "Rogue" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor" },
                    Other = new[] { "One skill from the class's skill list", "Thieves' tools" }
                },
                "Sorcerer" => new MulticlassProficiencyGrant(), // none
                "Warlock" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor" },
                    Weapons = new[] { "Simple weapons" }
                },
                "Wizard" => new MulticlassProficiencyGrant(), // none
                "Artificer" => new MulticlassProficiencyGrant
                {
                    Armor = new[] { "Light armor", "Medium armor", "Shields" },
                    Other = new[] { "Thieves' tools", "Tinker's tools" }
                },
                _ => new MulticlassProficiencyGrant()
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // Multiclass ability prerequisites (PHB)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// PHB multiclass minimums. Returns empty list if the class has no special prereq (or unknown).
        /// Each inner list is an OR group of ability names that must all be ≥ 13 within that option;
        /// outer list is AND of groups, except Fighter uses OR between Str and Dex (encoded specially).
        /// Simpler API: <see cref="MeetsMulticlassPrerequisites"/>.
        /// </summary>
        public static bool MeetsMulticlassPrerequisites(
            string className,
            Func<string, int> getAbilityScore,
            out string requirementText)
        {
            requirementText = GetMulticlassPrerequisiteText(className);
            if (string.IsNullOrWhiteSpace(className) || getAbilityScore == null)
                return true;

            int Score(string ab) => getAbilityScore(ab);

            return (className ?? "").Trim() switch
            {
                "Barbarian" => Score("Strength") >= 13,
                "Bard" => Score("Charisma") >= 13,
                "Cleric" => Score("Wisdom") >= 13,
                "Druid" => Score("Wisdom") >= 13,
                "Fighter" => Score("Strength") >= 13 || Score("Dexterity") >= 13,
                "Monk" => Score("Dexterity") >= 13 && Score("Wisdom") >= 13,
                "Paladin" => Score("Strength") >= 13 && Score("Charisma") >= 13,
                "Ranger" => Score("Dexterity") >= 13 && Score("Wisdom") >= 13,
                "Rogue" => Score("Dexterity") >= 13,
                "Sorcerer" => Score("Charisma") >= 13,
                "Warlock" => Score("Charisma") >= 13,
                "Wizard" => Score("Intelligence") >= 13,
                "Artificer" => Score("Intelligence") >= 13,
                _ => true
            };
        }

        public static string GetMulticlassPrerequisiteText(string className) =>
            (className ?? "").Trim() switch
            {
                "Barbarian" => "Strength 13+",
                "Bard" => "Charisma 13+",
                "Cleric" => "Wisdom 13+",
                "Druid" => "Wisdom 13+",
                "Fighter" => "Strength 13+ or Dexterity 13+",
                "Monk" => "Dexterity 13+ and Wisdom 13+",
                "Paladin" => "Strength 13+ and Charisma 13+",
                "Ranger" => "Dexterity 13+ and Wisdom 13+",
                "Rogue" => "Dexterity 13+",
                "Sorcerer" => "Charisma 13+",
                "Warlock" => "Charisma 13+",
                "Wizard" => "Intelligence 13+",
                "Artificer" => "Intelligence 13+",
                _ => "—"
            };

        // ═══════════════════════════════════════════════════════════════
        // Class resources
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Resources for a single class at the given level.
        /// <paramref name="abilityModifier"/> is used when a resource depends on an ability
        /// (e.g. Bardic Inspiration uses = Charisma modifier). Pass the relevant mod, or 0.
        /// </summary>
        public static List<ClassResourceValue> GetClassResources(
            string className,
            int classLevel,
            int abilityModifier = 0,
            string? subclass = null)
        {
            if (string.IsNullOrWhiteSpace(className) || classLevel <= 0)
                return new List<ClassResourceValue>();

            int lvl = Math.Clamp(classLevel, 1, 20);
            string cls = className.Trim();

            return cls.ToLowerInvariant() switch
            {
                "artificer" => ResourcesArtificer(lvl),
                "barbarian" => ResourcesBarbarian(lvl),
                "bard" => ResourcesBard(lvl, abilityModifier),
                "cleric" => ResourcesCleric(lvl),
                "druid" => ResourcesDruid(lvl),
                "fighter" => ResourcesFighter(lvl),
                "monk" => ResourcesMonk(lvl),
                "paladin" => ResourcesPaladin(lvl),
                "ranger" => ResourcesRanger(lvl),
                "rogue" => ResourcesRogue(lvl),
                "sorcerer" => ResourcesSorcerer(lvl),
                "warlock" => ResourcesWarlock(lvl),
                "wizard" => ResourcesWizard(lvl),
                _ => new List<ClassResourceValue>()
            };
        }

        public static Dictionary<string, IReadOnlyList<ClassResourceValue>> GetAllResources(
            IEnumerable<ClassLevelEntry> classLevels)
        {
            var map = new Dictionary<string, IReadOnlyList<ClassResourceValue>>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in Normalize(classLevels))
            {
                // ability mod left 0 here; callers can recompute with specific mods per class
                map[e.ClassName] = GetClassResources(e.ClassName, e.Levels, 0, e.Subclass);
            }
            return map;
        }

        // ── per-class resource tables ────────────────────────────────

        private static List<ClassResourceValue> ResourcesBarbarian(int lvl)
        {
            int rages = lvl switch
            {
                >= 20 => -1, // unlimited
                >= 17 => 6,
                >= 12 => 5,
                >= 6 => 4,
                >= 3 => 3,
                _ => 2
            };
            int rageDamage = lvl >= 16 ? 4 : lvl >= 9 ? 3 : 2;

            var list = new List<ClassResourceValue>
            {
                new()
                {
                    Name = "Rage",
                    Value = rages < 0 ? "Unlimited" : rages.ToString(),
                    NumericMax = rages < 0 ? null : rages,
                    Recharge = "long rest",
                    Notes = "Bonus action; 1 minute"
                },
                new()
                {
                    Name = "Rage Damage",
                    Value = $"+{rageDamage}",
                    NumericMax = rageDamage,
                    Recharge = "passive",
                    Notes = "Bonus to Strength melee weapon damage while raging"
                }
            };
            return list;
        }

        private static List<ClassResourceValue> ResourcesBard(int lvl, int chaMod)
        {
            // When chaMod is 0, still display formula so UI can substitute the real mod later
            bool hasMod = chaMod != 0;
            int uses = Math.Max(1, chaMod);
            string die = lvl >= 15 ? "d12" : lvl >= 10 ? "d10" : lvl >= 5 ? "d8" : "d6";
            string recharge = lvl >= 5 ? "short or long rest" : "long rest";
            string value = hasMod ? $"{uses} × {die}" : $"(Cha mod, min 1) × {die}";

            return new List<ClassResourceValue>
            {
                new()
                {
                    Name = "Bardic Inspiration",
                    Value = value,
                    NumericMax = hasMod ? uses : null,
                    Recharge = recharge,
                    Notes = "Uses = Charisma modifier (minimum 1). Die: d6 (1st), d8 (5th), d10 (10th), d12 (15th). Short rest recovery from 5th level."
                }
            };
        }

        private static List<ClassResourceValue> ResourcesCleric(int lvl)
        {
            var list = new List<ClassResourceValue>();
            if (lvl < 2) return list;

            int uses = lvl >= 18 ? 3 : lvl >= 6 ? 2 : 1;
            list.Add(new ClassResourceValue
            {
                Name = "Channel Divinity",
                Value = uses.ToString(),
                NumericMax = uses,
                Recharge = "short or long rest",
                Notes = "Turn Undead + domain option"
            });

            if (lvl >= 10)
            {
                list.Add(new ClassResourceValue
                {
                    Name = "Divine Intervention",
                    Value = lvl >= 20 ? "Automatic success" : $"≤ {lvl}% chance",
                    Recharge = "long rest (7 days on success)",
                    Notes = "Action; percentile ≤ cleric level"
                });
            }
            return list;
        }

        private static List<ClassResourceValue> ResourcesDruid(int lvl)
        {
            var list = new List<ClassResourceValue>();
            if (lvl < 2) return list;

            // PHB Wild Shape uses: 2 / short rest (never scales in base PHB)
            string crLimit = lvl switch
            {
                >= 8 => "CR 1 (swim + fly)",
                >= 4 => "CR 1/2 (swim, no fly)",
                _ => "CR 1/4 (no swim/fly)"
            };
            // Moon circle improves CR further — base table only here
            list.Add(new ClassResourceValue
            {
                Name = "Wild Shape",
                Value = "2",
                NumericMax = 2,
                Recharge = "short or long rest",
                Notes = $"Max form: {crLimit}. Circle of the Moon improves CR and enables elemental forms."
            });
            return list;
        }

        private static List<ClassResourceValue> ResourcesFighter(int lvl)
        {
            var list = new List<ClassResourceValue>
            {
                new()
                {
                    Name = "Second Wind",
                    Value = "1",
                    NumericMax = 1,
                    Recharge = "short or long rest",
                    Notes = "Bonus action: regain 1d10 + fighter level HP"
                }
            };

            if (lvl >= 2)
            {
                int surges = lvl >= 17 ? 2 : 1;
                list.Add(new ClassResourceValue
                {
                    Name = "Action Surge",
                    Value = surges.ToString(),
                    NumericMax = surges,
                    Recharge = "short or long rest",
                    Notes = "Extra action; only one Action Surge per turn"
                });
            }

            if (lvl >= 9)
            {
                int indom = lvl >= 17 ? 3 : lvl >= 13 ? 2 : 1;
                list.Add(new ClassResourceValue
                {
                    Name = "Indomitable",
                    Value = indom.ToString(),
                    NumericMax = indom,
                    Recharge = "long rest",
                    Notes = "Reroll a failed saving throw"
                });
            }

            // Extra Attack count (not a "resource" but useful)
            if (lvl >= 5)
            {
                int attacks = lvl >= 20 ? 4 : lvl >= 11 ? 3 : 2;
                list.Add(new ClassResourceValue
                {
                    Name = "Extra Attack",
                    Value = $"{attacks} attacks",
                    NumericMax = attacks,
                    Recharge = "passive",
                    Notes = "When you take the Attack action"
                });
            }

            return list;
        }

        private static List<ClassResourceValue> ResourcesMonk(int lvl)
        {
            string martialDie = lvl >= 17 ? "d10" : lvl >= 11 ? "d8" : lvl >= 5 ? "d6" : "d4";
            int unarmoredMove = lvl >= 18 ? 30 : lvl >= 14 ? 25 : lvl >= 10 ? 20 : lvl >= 6 ? 15 : lvl >= 2 ? 10 : 0;

            var list = new List<ClassResourceValue>
            {
                new()
                {
                    Name = "Ki Points",
                    Value = lvl.ToString(),
                    NumericMax = lvl,
                    Recharge = "short or long rest",
                    Notes = "Flurry of Blows, Patient Defense, Step of the Wind, and tradition features"
                },
                new()
                {
                    Name = "Martial Arts Die",
                    Value = martialDie,
                    Recharge = "passive",
                    Notes = "Unarmed strike / monk weapon damage"
                }
            };

            if (unarmoredMove > 0)
            {
                list.Add(new ClassResourceValue
                {
                    Name = "Unarmored Movement",
                    Value = $"+{unarmoredMove} ft.",
                    NumericMax = unarmoredMove,
                    Recharge = "passive",
                    Notes = "While not wearing armor or a shield"
                });
            }

            return list;
        }

        private static List<ClassResourceValue> ResourcesPaladin(int lvl)
        {
            var list = new List<ClassResourceValue>
            {
                new()
                {
                    Name = "Lay on Hands",
                    Value = $"{5 * lvl} HP pool",
                    NumericMax = 5 * lvl,
                    Recharge = "long rest",
                    Notes = "Touch to heal; 5 HP to cure one disease or poison"
                }
            };

            if (lvl >= 1)
            {
                // Divine Sense: 1 + Cha mod — show formula
                list.Add(new ClassResourceValue
                {
                    Name = "Divine Sense",
                    Value = "1 + Cha mod",
                    Recharge = "long rest",
                    Notes = "Detect celestials, fiends, undead within 60 ft."
                });
            }

            if (lvl >= 3)
            {
                list.Add(new ClassResourceValue
                {
                    Name = "Channel Divinity",
                    Value = "1",
                    NumericMax = 1,
                    Recharge = "short or long rest",
                    Notes = "Oath options (e.g. Sacred Weapon, Turn the Unholy)"
                });
            }

            if (lvl >= 6)
            {
                int aura = lvl >= 18 ? 30 : 10;
                list.Add(new ClassResourceValue
                {
                    Name = "Aura of Protection",
                    Value = $"{aura} ft.",
                    NumericMax = aura,
                    Recharge = "passive",
                    Notes = "You and allies in range add Cha mod to saves (min +1) while you are conscious"
                });
            }

            return list;
        }

        private static List<ClassResourceValue> ResourcesRanger(int lvl)
        {
            // Favored Enemy / Deft Explorer etc. are mostly passive; focus on clear combat resources
            var list = new List<ClassResourceValue>();
            if (lvl >= 5)
            {
                list.Add(new ClassResourceValue
                {
                    Name = "Extra Attack",
                    Value = "2 attacks",
                    NumericMax = 2,
                    Recharge = "passive",
                    Notes = "When you take the Attack action"
                });
            }
            // Spell slots come from SpellSlotCalculator
            return list;
        }

        private static List<ClassResourceValue> ResourcesRogue(int lvl)
        {
            int dice = (lvl + 1) / 2; // 1→1, 2→1, 3→2, … 19→10, 20→10
            return new List<ClassResourceValue>
            {
                new()
                {
                    Name = "Sneak Attack",
                    Value = $"{dice}d6",
                    NumericMax = dice,
                    Recharge = "1/turn",
                    Notes = "Once per turn with advantage (or ally within 5 ft of target) using finesse or ranged weapon"
                }
            };
        }

        private static List<ClassResourceValue> ResourcesSorcerer(int lvl)
        {
            var list = new List<ClassResourceValue>();
            if (lvl < 2) return list;

            list.Add(new ClassResourceValue
            {
                Name = "Sorcery Points",
                Value = lvl.ToString(),
                NumericMax = lvl,
                Recharge = "long rest",
                Notes = "Flexible Casting + Metamagic (from level 3)"
            });

            if (lvl >= 3)
            {
                int metaKnown = lvl >= 17 ? 4 : lvl >= 10 ? 3 : 2;
                list.Add(new ClassResourceValue
                {
                    Name = "Metamagic Options Known",
                    Value = metaKnown.ToString(),
                    NumericMax = metaKnown,
                    Recharge = "passive"
                });
            }

            return list;
        }

        private static List<ClassResourceValue> ResourcesWarlock(int lvl)
        {
            var pact = SpellSlotCalculator.GetWarlockPactMagicSlots(lvl);
            int slotLevel = pact.PactSlotLevel ?? 0;
            int slotCount = slotLevel > 0 ? pact.GetSlots(slotLevel) : 0;

            // Eldritch Invocations known (PHB)
            int inv = lvl switch
            {
                >= 18 => 8,
                >= 15 => 7,
                >= 12 => 6,
                >= 9 => 5,
                >= 7 => 4,
                >= 5 => 3,
                >= 2 => 2,
                _ => 0
            };

            var list = new List<ClassResourceValue>();
            if (slotCount > 0)
            {
                list.Add(new ClassResourceValue
                {
                    Name = "Pact Magic Slots",
                    Value = $"{slotCount} × {Ordinal(slotLevel)}-level",
                    NumericMax = slotCount,
                    Recharge = "short or long rest",
                    Notes = "All pact slots are the same level"
                });
            }

            if (inv > 0)
            {
                list.Add(new ClassResourceValue
                {
                    Name = "Eldritch Invocations Known",
                    Value = inv.ToString(),
                    NumericMax = inv,
                    Recharge = "passive"
                });
            }

            if (lvl >= 11)
            {
                // Mystic Arcanum: one each of 6th–9th by level
                int arcanum = lvl >= 17 ? 4 : lvl >= 15 ? 3 : lvl >= 13 ? 2 : 1;
                list.Add(new ClassResourceValue
                {
                    Name = "Mystic Arcanum",
                    Value = $"{arcanum} spell(s)",
                    NumericMax = arcanum,
                    Recharge = "long rest each",
                    Notes = "6th at 11, 7th at 13, 8th at 15, 9th at 17 — each once per long rest"
                });
            }

            return list;
        }

        private static List<ClassResourceValue> ResourcesWizard(int lvl)
        {
            // Arcane Recovery: regain slots totaling ≤ half wizard level (rounded up), no 6th+
            int recovery = (lvl + 1) / 2;
            return new List<ClassResourceValue>
            {
                new()
                {
                    Name = "Arcane Recovery",
                    Value = $"slot levels ≤ {recovery}",
                    NumericMax = recovery,
                    Recharge = "1/day (on short rest)",
                    Notes = "Combined level of recovered slots ≤ half wizard level (rounded up); no slot of 6th level or higher"
                }
            };
        }

        private static List<ClassResourceValue> ResourcesArtificer(int lvl)
        {
            // Infusions known / infused items (Tasha's / ERftLW)
            // Level: known / infused
            // 2: 4/2, 6: 6/3, 10: 8/4, 14: 10/5, 18: 12/6
            int known = 0, infused = 0;
            if (lvl >= 18) { known = 12; infused = 6; }
            else if (lvl >= 14) { known = 10; infused = 5; }
            else if (lvl >= 10) { known = 8; infused = 4; }
            else if (lvl >= 6) { known = 6; infused = 3; }
            else if (lvl >= 2) { known = 4; infused = 2; }

            var list = new List<ClassResourceValue>();
            if (known > 0)
            {
                list.Add(new ClassResourceValue
                {
                    Name = "Infusions Known",
                    Value = known.ToString(),
                    NumericMax = known,
                    Recharge = "passive"
                });
                list.Add(new ClassResourceValue
                {
                    Name = "Infused Items",
                    Value = infused.ToString(),
                    NumericMax = infused,
                    Recharge = "on long rest (when you change infusions)",
                    Notes = "Maximum number of items that can bear your infusions at once"
                });
            }
            return list;
        }

        // ═══════════════════════════════════════════════════════════════
        // Display helpers
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Compact multi-line summary for UI / debugging.</summary>
        public static string FormatSnapshotSummary(CharacterLevelSnapshot snap)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Character Level: {snap.TotalCharacterLevel}");
            sb.AppendLine($"Proficiency Bonus: +{snap.ProficiencyBonus}");
            sb.Append("Classes: ");
            sb.AppendLine(string.Join(", ",
                snap.ClassLevels.Select(c =>
                    string.IsNullOrWhiteSpace(c.Subclass)
                        ? $"{c.ClassName} {c.Levels}"
                        : $"{c.ClassName} ({c.Subclass}) {c.Levels}")));
            sb.AppendLine($"Hit Dice: {snap.HitDicePoolDisplay}");
            sb.AppendLine($"HP Maximum ({snap.HpMethod}): {snap.HitPointMaximum}");
            if (snap.HitPointsByLevelGain.Count > 0)
                sb.AppendLine($"  Per level: {string.Join(" + ", snap.HitPointsByLevelGain)}");

            if (snap.AsiOrFeatChoices.Count > 0)
            {
                sb.AppendLine("ASI / Feat choices:");
                foreach (var a in snap.AsiOrFeatChoices)
                    sb.AppendLine($"  • {a}");
            }

            if (snap.AllResources.Count > 0)
            {
                sb.AppendLine("Class resources:");
                foreach (var r in snap.AllResources)
                    sb.AppendLine($"  • {r}");
            }

            if (snap.SpellSlots.HasAnySlots)
            {
                sb.AppendLine("Spell slots:");
                if (snap.SpellSlots.SharedSlots.HighestSlotLevel > 0)
                    sb.AppendLine($"  Shared: {snap.SpellSlots.SharedSlots}");
                if (snap.SpellSlots.PactMagicSlots.HighestSlotLevel > 0)
                    sb.AppendLine($"  {snap.SpellSlots.PactMagicSlots}");
                if (snap.SpellSlots.MulticlassCasterLevel > 0)
                    sb.AppendLine($"  Multiclass caster level: {snap.SpellSlots.MulticlassCasterLevel}");
            }

            return sb.ToString().TrimEnd();
        }

        // ═══════════════════════════════════════════════════════════════
        // Internals
        // ═══════════════════════════════════════════════════════════════

        private static List<ClassLevelEntry> Normalize(IEnumerable<ClassLevelEntry>? classLevels)
        {
            if (classLevels == null)
                return new List<ClassLevelEntry>();

            // Preserve order of first appearance; merge duplicates
            var order = new List<string>();
            var map = new Dictionary<string, ClassLevelEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in classLevels)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.ClassName) || e.Levels <= 0)
                    continue;

                string key = e.ClassName.Trim();
                if (map.TryGetValue(key, out var existing))
                {
                    existing.Levels = Math.Min(20, existing.Levels + e.Levels);
                    if (!string.IsNullOrWhiteSpace(e.Subclass))
                        existing.Subclass = e.Subclass;
                }
                else
                {
                    var copy = new ClassLevelEntry(key, Math.Min(20, e.Levels), e.Subclass);
                    map[key] = copy;
                    order.Add(key);
                }
            }

            // Cap total character level at 20
            int total = map.Values.Sum(x => x.Levels);
            if (total > 20)
            {
                int over = total - 20;
                // Trim from the end of order
                for (int i = order.Count - 1; i >= 0 && over > 0; i--)
                {
                    var e = map[order[i]];
                    int cut = Math.Min(e.Levels, over);
                    e.Levels -= cut;
                    over -= cut;
                    if (e.Levels <= 0)
                    {
                        map.Remove(order[i]);
                        order.RemoveAt(i);
                    }
                }
            }

            return order.Where(map.ContainsKey).Select(k => map[k]).ToList();
        }

        private static string Ordinal(int n) => n switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => n + "th"
        };
    }
}
