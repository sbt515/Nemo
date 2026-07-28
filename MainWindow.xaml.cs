using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.Windows.Input;
using iText.Kernel.Pdf;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

using iTextPdfDocument = iText.Kernel.Pdf.PdfDocument;
using iTextPdfPage = iText.Kernel.Pdf.PdfPage;

namespace Nemo
{
    public partial class MainWindow : Window
    {
        private Character CurrentCharacter = new();
        private string avatarBase64 = string.Empty;
        private Dictionary<string, int> racialBonuses = new();
        public ObservableCollection<SkillProficiency> allSkills = new();
        private int proficiencyBonus = 2;
        private string raceGrantedSkill = "";
        private List<string> currentRaceAutomaticSkills = new List<string>();
        private List<string> pickedWeapons = new List<string>();
        // Tracks active weapon radio button choices
        private Dictionary<string, (int Count, string WeaponType)> activeWeaponChoices = new();
        private string backgroundLanguage1 = "";
        private string backgroundLanguage2 = "";
        private string currentBackgroundEquipmentAdded = "";   // ← NEW
        /// <summary>Suppress wealth-mode radio events while restoring from a saved character.</summary>
        private bool _suppressWealthModeEvents;
        /// <summary>Class used for the last level-1 gold formula / equipment kit (detect class changes).</summary>
        private string _lastStartingWealthClass = "";
        private string highElfCantrip = "";
        private bool raceHasInnateSpellcasting = false;
        private List<Feat> allFeats = new();
        private Dictionary<string, int> featStatBonuses = new();
        private int featInitiativeBonus = 0;
        private string currentFeatAbilityChoice = "";
        /// <summary>Ability name granted save proficiency by Resilient (e.g. "Constitution").</summary>
        private string resilientSaveAbility = "";
        private string featSelectedSpell = "";
        private string baseFeatDescription = "";
        private int featSpeedBonus = 0;   // ← NEW: For Mobile feat (+10 ft)
        // === Magic Initiate Tracking ===
        private string magicInitiateClass = "";
        private List<string> magicInitiateCantrips = new();
        private string magicInitiateSpell = "";
        // === Feat Spell Tracking (generalized) ===
        private string currentFeatSpellSource = "";           // e.g. "Spell Sniper", "Fey Touched", etc.
        private List<string> currentFeatSpells = new();
        public ObservableCollection<SelectableSpell> cantripOptions = new();
        private CollectionViewSource cantripViewSource = new CollectionViewSource();
        /// <summary>All leveled spells (1–9) for the class grid; filtered by <see cref="currentSpellLevelFilter"/>.</summary>
        public ObservableCollection<SelectableSpell> spell1Options = new();
        private CollectionViewSource spell1ViewSource = new CollectionViewSource();
        /// <summary>Currently displayed spell level in the leveled-spells grid (1–9).</summary>
        private int currentSpellLevelFilter = 1;
        private bool _suppressSpellLevelEvent;
        private bool _suppressLevelTabRebuild;
        private bool _suppressHpRollEvents;
        /// <summary>Backing collection for the spell-level combo (avoids WPF ItemsSource stickiness).</summary>
        private readonly ObservableCollection<string> spellLevelComboItems = new();
        private readonly Random _rng = new();
        private readonly Brush AccentGreen = ThemeBrush("Nemo.Brush.Accent", "#7CFC00");
        private readonly Brush AccentGray = ThemeBrush("Nemo.Brush.FieldBg", "#2A2A2A");
        private static readonly string[] AbilityNames =
            { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };

        private static readonly Brush ComboBgBrush = ThemeBrush("Nemo.Brush.FieldBg", "#2A2A2A");
        private static readonly Brush ComboFgBrush = ThemeBrush("Nemo.Brush.TextPrimary", "#F0F0F0");
        private static readonly Brush ComboBorderBrush = ThemeBrush("Nemo.Brush.Border", "#555555");
        private static readonly Brush BrushPrimary = ThemeBrush("Nemo.Brush.Primary", "#3A7CA5");
        private static readonly Brush BrushSuccess = ThemeBrush("Nemo.Brush.Success", "#4A7C59");
        private static readonly Brush BrushDanger = ThemeBrush("Nemo.Brush.Danger", "#E74C3C");

        /// <summary>Resolve a brush from App.xaml design tokens, with a hex fallback.</summary>
        private static Brush ThemeBrush(string resourceKey, string fallbackHex)
        {
            try
            {
                if (Application.Current?.TryFindResource(resourceKey) is Brush fromApp)
                    return fromApp;
            }
            catch
            {
                // design-time / early init
            }

            return (Brush)new BrushConverter().ConvertFromString(fallbackHex)!;
        }

        /// <summary>
        /// Apply app-level ComboBox theme (dark field, light text, readable dropdown items).
        /// Styles are defined globally in App.xaml.
        /// </summary>
        private void StyleAppComboBox(ComboBox cmb)
        {
            if (cmb == null) return;

            // Prefer application/window styles (includes ComboBoxItem template for hover/selected)
            if (TryFindResource(typeof(ComboBox)) is System.Windows.Style comboStyle)
                cmb.Style = comboStyle;
            if (TryFindResource(typeof(ComboBoxItem)) is System.Windows.Style itemStyle)
                cmb.ItemContainerStyle = itemStyle;

            cmb.Background = ComboBgBrush;
            cmb.Foreground = ComboFgBrush;
            cmb.BorderBrush = ComboBorderBrush;
            cmb.Padding = new Thickness(8, 4, 8, 4);
            cmb.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
            cmb.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
        }


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => StatMethod_Changed(null, null);
            this.Loaded += MainWindow_Loaded;
            LoadAllCombos();
            InitializeTemplateGenerateUi();
            InitializeSkills();
            // Bind spell-level combo to an ObservableCollection so level unlocks always refresh
            if (cmbSpellLevel != null)
            {
                spellLevelComboItems.Clear();
                spellLevelComboItems.Add(FormatSpellLevelLabel(1));
                cmbSpellLevel.ItemsSource = spellLevelComboItems;
                cmbSpellLevel.SelectedIndex = 0;
            }
            rbPointBuy.IsChecked = true;
            StatMethod_Changed(null, null);
        }

        private bool _suppressRaceCategoryEvents;

        private void LoadAllCombos()
        {
            if (cmbRaceCategory != null)
            {
                cmbRaceCategory.ItemsSource = GameData.RaceCategories.ToList();
                cmbRaceCategory.SelectedItem = "Common";
            }
            RefreshRaceComboForCategory("Common");
            cmbBackground.ItemsSource = GameData.AllBackgrounds;
            cmbClass.ItemsSource = GameData.ClassData.Keys.OrderBy(c => c).ToList();
        }

        /// <summary>When true, template combo changes do not write preferences (init / restore).</summary>
        private bool _suppressTemplateSettingsSave;

        /// <summary>Wire category / role combos for Quick Generate on the Basic Info tab.</summary>
        private void InitializeTemplateGenerateUi()
        {
            if (cmbTemplateCategory == null || cmbTemplateRole == null) return;

            var prefs = AppSettings.Load();
            string category = prefs.TemplateCategory;
            var available = CharacterTemplateGenerator.GetAvailableCategoryNames();
            if (string.IsNullOrWhiteSpace(category) ||
                !available.Any(c => c.Equals(category, StringComparison.OrdinalIgnoreCase)))
            {
                category = AppSettings.DefaultTemplateCategory; // General
            }
            else
            {
                category = available.First(c =>
                    c.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            _suppressTemplateSettingsSave = true;
            try
            {
                cmbTemplateCategory.ItemsSource = available.ToList();
                cmbTemplateCategory.SelectedItem = category;
                RefreshTemplateRoleCombo(preferredRole: prefs.TemplateRole);
                UpdateTemplateHintText();
            }
            finally
            {
                _suppressTemplateSettingsSave = false;
            }
        }

        /// <summary>
        /// Rebuilds the category combo (e.g. after the first custom template is saved).
        /// Preserves the current selection when still valid.
        /// </summary>
        private void RefreshTemplateCategoryCombo(string? preferredCategory = null)
        {
            if (cmbTemplateCategory == null) return;

            string? previous = preferredCategory ?? cmbTemplateCategory.SelectedItem as string;
            var available = CharacterTemplateGenerator.GetAvailableCategoryNames().ToList();

            bool wasSuppressing = _suppressTemplateSettingsSave;
            _suppressTemplateSettingsSave = true;
            try
            {
                cmbTemplateCategory.ItemsSource = available;

                if (previous != null &&
                    available.Any(c => c.Equals(previous, StringComparison.OrdinalIgnoreCase)))
                {
                    cmbTemplateCategory.SelectedItem = available.First(c =>
                        c.Equals(previous, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    string fallback = AppSettings.DefaultTemplateCategory;
                    if (available.Any(c => c.Equals(fallback, StringComparison.OrdinalIgnoreCase)))
                        cmbTemplateCategory.SelectedItem = fallback;
                    else if (available.Count > 0)
                        cmbTemplateCategory.SelectedIndex = 0;
                }
            }
            finally
            {
                _suppressTemplateSettingsSave = wasSuppressing;
            }
        }

        private void cmbTemplateCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTemplateCategory == null) return;
            // Avoid running before ItemsSource is ready
            if (cmbTemplateCategory.SelectedItem == null && cmbTemplateCategory.Items.Count == 0)
                return;

            RefreshTemplateRoleCombo();
            UpdateTemplateHintText();
            PersistTemplateGenerateSelection();
        }

        private void cmbTemplateRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTemplateHintText();
            PersistTemplateGenerateSelection();
        }

        private void PersistTemplateGenerateSelection()
        {
            if (_suppressTemplateSettingsSave) return;
            if (cmbTemplateCategory?.SelectedItem == null) return;

            AppSettings.SaveTemplateSelection(
                cmbTemplateCategory.SelectedItem as string,
                cmbTemplateRole?.SelectedItem as string);
        }

        private void RefreshTemplateRoleCombo(string? preferredRole = null)
        {
            if (cmbTemplateRole == null) return;

            string? previous = preferredRole ?? cmbTemplateRole.SelectedItem as string;
            var category = CharacterTemplateGenerator.ParseCategory(cmbTemplateCategory?.SelectedItem as string);

            bool wasSuppressing = _suppressTemplateSettingsSave;
            _suppressTemplateSettingsSave = true;
            try
            {
                cmbTemplateRole.ItemsSource = category == TemplateCategory.Random
                    ? CharacterTemplateGenerator.RandomRoleNames.ToList()
                    : CharacterTemplateGenerator.RoleNames.ToList();

                if (previous != null &&
                    cmbTemplateRole.Items.Cast<object>().Any(i =>
                        string.Equals(i?.ToString(), previous, StringComparison.OrdinalIgnoreCase)))
                {
                    cmbTemplateRole.SelectedItem = previous;
                }
                else
                {
                    // Prefer Support as default role when previous is invalid for this category
                    string fallback = AppSettings.DefaultTemplateRole;
                    if (cmbTemplateRole.Items.Cast<object>().Any(i =>
                            string.Equals(i?.ToString(), fallback, StringComparison.OrdinalIgnoreCase)))
                        cmbTemplateRole.SelectedItem = fallback;
                    else
                        cmbTemplateRole.SelectedIndex = 0;
                }
            }
            finally
            {
                _suppressTemplateSettingsSave = wasSuppressing;
            }
        }

        private void UpdateTemplateHintText()
        {
            if (txtTemplateHint == null) return;

            var category = CharacterTemplateGenerator.ParseCategory(cmbTemplateCategory?.SelectedItem as string);
            var role = CharacterTemplateGenerator.ParseRole(cmbTemplateRole?.SelectedItem as string);

            txtTemplateHint.Text =
                CharacterTemplateGenerator.DescribeCategory(category) + "\n" +
                CharacterTemplateGenerator.DescribeRole(role);
        }

        private void GenerateCharacter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var category = CharacterTemplateGenerator.ParseCategory(cmbTemplateCategory?.SelectedItem as string);
                var role = CharacterTemplateGenerator.ParseRole(cmbTemplateRole?.SelectedItem as string);

                GeneratedCharacterResult result;

                // Optimized / General / Custom: pick from the themed build list
                if (category is TemplateCategory.Optimized or TemplateCategory.General or TemplateCategory.Custom)
                {
                    if (category == TemplateCategory.Custom &&
                        CharacterTemplateGenerator.GetTemplates(category, role).Count == 0)
                    {
                        MessageBox.Show(
                            "No custom templates for this role yet.\n\nUse Create Template to save the current character as a reusable build.",
                            "Custom Templates",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    var chosen = TemplatePickerWindow.Pick(this, category, role);
                    if (chosen == null)
                        return; // cancelled or empty list

                    bool hasProgress =
                        !string.IsNullOrWhiteSpace(txtCharacterName?.Text) ||
                        cmbRace?.SelectedItem != null ||
                        cmbClass?.SelectedItem != null ||
                        cmbBackground?.SelectedItem != null;

                    if (hasProgress)
                    {
                        var confirm = MessageBox.Show(
                            $"Generate \"{chosen.Name}\"?\n\nThis will replace your current race, class, background, stats, skills, and spells on all tabs.",
                            "Generate Character",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (confirm != MessageBoxResult.Yes)
                            return;
                    }

                    result = CharacterTemplateGenerator.GenerateFromTemplate(chosen, _rng);
                }
                else
                {
                    // Random / True Random: no picker — roll immediately
                    bool hasProgress =
                        !string.IsNullOrWhiteSpace(txtCharacterName?.Text) ||
                        cmbRace?.SelectedItem != null ||
                        cmbClass?.SelectedItem != null ||
                        cmbBackground?.SelectedItem != null;

                    if (hasProgress)
                    {
                        var confirm = MessageBox.Show(
                            "Generate a new character? This will replace your current race, class, background, stats, skills, and spells on all tabs.",
                            "Generate Character",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (confirm != MessageBoxResult.Yes)
                            return;
                    }

                    result = CharacterTemplateGenerator.Generate(category, role, _rng);
                }

                CurrentCharacter = result.Character;

                // Custom method allows any rolled/array values without point-buy validation fighting us
                if (rbCustom != null)
                    rbCustom.IsChecked = true;
                else if (rbStandardArray != null && category != TemplateCategory.Random)
                    rbStandardArray.IsChecked = true;

                ApplyCharacterToUI(fromPdf: false);
                AutoSelectEquipmentDefaults();
                UpdateStatDisplays();

                if (cmbClass.SelectedItem is string className)
                    UpdateSkillChoices(className);

                if (txtGenerateResult != null)
                    txtGenerateResult.Text = "✅ " + result.Summary.Replace("\n", " · ");

                MessageBox.Show(
                    result.Summary + "\n\nReview the other tabs to tweak skills, equipment, and spells.",
                    "Character Generated",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to generate character:\n" + ex.Message,
                    "Generate Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save the current character as a reusable Custom Quick Generate template
        /// (from Summary &amp; Export).
        /// </summary>
        private void SaveAsTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sync UI → CurrentCharacter so race/class/skills/spells are up to date
                AutoSaveCharacterToJson();

                if (CurrentCharacter == null ||
                    string.IsNullOrWhiteSpace(CurrentCharacter.Race) ||
                    string.IsNullOrWhiteSpace(CurrentCharacter.Class))
                {
                    MessageBox.Show(
                        "Set a race and class on the current character before saving a template.",
                        "Save as Template",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var saved = CreateTemplateWindow.Create(this, CurrentCharacter);
                if (saved == null)
                    return;

                // Custom category becomes available once the first template exists
                RefreshTemplateCategoryCombo(preferredCategory: "Custom");
                RefreshTemplateRoleCombo(preferredRole: saved.Role == TemplateRole.None
                    ? "Support"
                    : saved.Role.ToString());
                UpdateTemplateHintText();
                PersistTemplateGenerateSelection();

                if (txtGenerateResult != null)
                    txtGenerateResult.Text = $"✅ Saved custom template \"{saved.Name}\" · Category: Custom · Role: {saved.Role}";

                MessageBox.Show(
                    $"Template \"{saved.Name}\" saved.\n\n" +
                    "On Basic Info, choose Category: Custom and the matching role, then Generate Character to reuse it.",
                    "Template Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to save template:\n" + ex.Message,
                    "Save as Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// After generation, pick the first radio option in each equipment choice group
        /// so the starting kit is fully filled without manual clicks.
        /// </summary>
        private void AutoSelectEquipmentDefaults()
        {
            if (pnlEquipmentChoices == null) return;

            try
            {
                var groupsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var child in pnlEquipmentChoices.Children)
                {
                    if (child is not StackPanel groupPanel) continue;
                    foreach (var inner in groupPanel.Children.OfType<RadioButton>())
                    {
                        string group = inner.GroupName ?? "";
                        if (string.IsNullOrEmpty(group) || !groupsSeen.Add(group))
                            continue;
                        if (inner.IsChecked != true)
                            inner.IsChecked = true;
                    }
                }
            }
            catch
            {
                // Equipment UI is best-effort; character is still playable without auto-picks.
            }
        }

        private void cmbRaceCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressRaceCategoryEvents) return;
            string cat = cmbRaceCategory?.SelectedItem as string ?? "Common";
            string? previousRace = cmbRace?.SelectedItem as string;
            RefreshRaceComboForCategory(cat);

            // Keep prior race if it still exists in the new category list
            if (previousRace != null && cmbRace?.Items.Cast<object>()
                    .Any(i => string.Equals(i?.ToString(), previousRace, StringComparison.OrdinalIgnoreCase)) == true)
            {
                cmbRace.SelectedItem = previousRace;
            }
            else if (cmbRace != null && cmbRace.Items.Count > 0)
            {
                cmbRace.SelectedIndex = -1; // force user to pick (or leave empty)
            }
        }

        private void RefreshRaceComboForCategory(string category)
        {
            if (cmbRace == null) return;
            var races = GameData.GetRacesInCategory(category);
            cmbRace.ItemsSource = races;
        }

        private void cmbRace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRace.SelectedItem is not string race || !GameData.RaceData.ContainsKey(race)) return;

            var data = GameData.RaceData[race];
            string bonuses = data.AbilityBonuses.Count > 0
                ? string.Join(", ", data.AbilityBonuses.Select(kv => $"+{kv.Value} {kv.Key}"))
                : "(player choice — see traits)";

            // Filter out any trait that is just listing languages (those are shown in LANGUAGES)
            var filteredTraits = data.Traits
                .Where(t => !t.TrimStart().StartsWith("Languages:", StringComparison.OrdinalIgnoreCase) &&
                            !t.Contains("Common +", StringComparison.OrdinalIgnoreCase))
                .ToList();

            txtRaceDetails.Text =
    $"ABILITY SCORE INCREASE\n{bonuses}\n\n" +
    $"TRAITS\n• {string.Join("\n• ", filteredTraits)}\n\n" +
    $"LANGUAGES\n{string.Join(", ", data.Languages)}\n\n" +
    $"SPEED\n{data.Speed} ft";

            racialBonuses = new Dictionary<string, int>(data.AbilityBonuses);
            ApplyRaceProficiencies(race);
            ApplyRacialBonuses();
            UpdateEquipmentProficiencySummary();
            raceHasInnateSpellcasting = data.HasInnateSpellcasting;
            UpdateSpellTabVisibility();
            PopulateSpells();

            // Show/hide subrace
            if (GameData.RaceSubraces.ContainsKey(race))
            {
                cmbSubrace.ItemsSource = GameData.RaceSubraces[race].Select(s => s.Name).ToList();
                cmbSubrace.IsEnabled = true;
                cmbSubrace.SelectedIndex = 0;
            }
            else
            {
                cmbSubrace.ItemsSource = null;
                cmbSubrace.IsEnabled = false;
            }

            // Standard Human has no skill/feat choices (PHB) — only Variant Human, Custom Lineage, and Half-Elf have pickers
            if (race == "Custom Lineage" || race == "Variant Human" || race == "Half-Elf")
            {
                if (race == "Variant Human")
                {
                    pnlRaceSkillChoice.Visibility = Visibility.Visible;
                    lblRaceSkillHeader.Text = "VARIANT HUMAN CHOICES";
                    pnlFlexibleBonuses.Visibility = Visibility.Visible;
                    rbRaceDarkvision.Visibility = Visibility.Collapsed;
                    rbRaceSkill.Visibility = Visibility.Visible;
                    rbRaceSkill.IsChecked = true;
                    cmbRaceSkillChoice.Visibility = Visibility.Visible;

                    // Setup +1 / +1 ability bonuses
                    var abilities = new List<string> { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
                    cmbFlexibleBonus1.ItemsSource = abilities;
                    cmbFlexibleBonus2.ItemsSource = abilities;
                    cmbFlexibleBonus1.SelectedIndex = 0;
                    cmbFlexibleBonus2.SelectedIndex = 1;

                    // Wire up events
                    cmbFlexibleBonus1.SelectionChanged -= FlexibleBonus_Changed;
                    cmbFlexibleBonus1.SelectionChanged += FlexibleBonus_Changed;
                    cmbFlexibleBonus2.SelectionChanged -= FlexibleBonus_Changed;
                    cmbFlexibleBonus2.SelectionChanged += FlexibleBonus_Changed;

                    // Apply the default bonuses immediately
                    FlexibleBonus_Changed(null, null);
                }
                else if (race == "Half-Elf")
                {
                    // Half-Elf: +2 Cha (base) and +1 to two other abilities; Skill Versatility is free-form (noted in traits)
                    pnlRaceSkillChoice.Visibility = Visibility.Collapsed;
                    pnlFlexibleBonuses.Visibility = Visibility.Visible;
                    SetupFlexibleBonusPickers(race);
                    FlexibleBonus_Changed(null, null);
                }
                else // Custom Lineage
                {
                    pnlRaceSkillChoice.Visibility = Visibility.Visible;
                    lblRaceSkillHeader.Text = "CUSTOM LINEAGE CHOICES";
                    pnlFlexibleBonuses.Visibility = Visibility.Visible;
                    rbRaceDarkvision.Visibility = Visibility.Visible;
                    rbRaceSkill.Visibility = Visibility.Visible;
                    rbRaceDarkvision.IsChecked = true;   // Default to Darkvision
                    cmbRaceSkillChoice.Visibility = Visibility.Collapsed;
                }

                // Populate skill dropdown when skill choice is shown
                if (pnlRaceSkillChoice.Visibility == Visibility.Visible)
                {
                    cmbRaceSkillChoice.ItemsSource = allSkills.Select(s => s.SkillName).ToList();
                    if (string.IsNullOrEmpty(raceGrantedSkill))
                        cmbRaceSkillChoice.SelectedIndex = 0;

                    cmbRaceSkillChoice.SelectionChanged -= RaceGrantedSkill_Changed; // Prevent duplicates
                    cmbRaceSkillChoice.SelectionChanged += RaceGrantedSkill_Changed;
                }
            }
            else
            {
                pnlRaceSkillChoice.Visibility = Visibility.Collapsed;
                pnlFlexibleBonuses.Visibility = Visibility.Collapsed;
            }

            // Feats tab: origin race and/or ASI→Feat picks
            EnsureFeatsLoaded();
            UpdateFeatsTabVisibility();

            // Cleanup when switching away from races that grant a skill choice
            if ((race != "Custom Lineage" && race != "Variant Human")
                && !string.IsNullOrEmpty(raceGrantedSkill))
            {
                var oldSkill = allSkills.FirstOrDefault(s => s.SkillName == raceGrantedSkill);
                if (oldSkill != null)
                {
                    oldSkill.IsProficient = false;
                    oldSkill.IsBackgroundProficiency = false;
                }
                raceGrantedSkill = "";
                dgSkills.Items.Refresh();
                lblRaceProficienciesDetail.Visibility = Visibility.Collapsed;
            }
            // Reset High Elf cantrip when changing race
            if (race != "Elf")
            {
                highElfCantrip = "";
                pnlHighElfCantrip.Visibility = Visibility.Collapsed;
                pnlHighElfCantripPreview.Visibility = Visibility.Collapsed;
            }
            UpdateRacialSpellsLabel();
        }

        private void SetupFlexibleBonusPickers(string race)
        {
            var abilities = new List<string> { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };

            if (race == "Half-Elf")
                abilities.Remove("Charisma"); // Half-Elf cannot choose Charisma again for the +1s

            cmbFlexibleBonus1.ItemsSource = abilities;
            cmbFlexibleBonus2.ItemsSource = abilities;

            cmbFlexibleBonus1.SelectedIndex = 0;
            cmbFlexibleBonus2.SelectedIndex = 1;

            cmbFlexibleBonus1.SelectionChanged -= FlexibleBonus_Changed;
            cmbFlexibleBonus2.SelectionChanged -= FlexibleBonus_Changed;
            cmbFlexibleBonus1.SelectionChanged += FlexibleBonus_Changed;
            cmbFlexibleBonus2.SelectionChanged += FlexibleBonus_Changed;
        }

        private void ApplyRaceFlexibleBonus()
        {
            if (cmbRace.SelectedItem?.ToString() != "Custom Lineage") return;

            racialBonuses.Clear();

            if (cmbFlexibleBonus1.SelectedItem is string ability)
            {
                racialBonuses[ability] = 2;
            }

            ApplyRacialBonuses();
        }

        private void FlexibleBonus_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRace.SelectedItem is not string race) return;

            if (race == "Custom Lineage")
            {
                ApplyRaceFlexibleBonus();
            }
            else if (race == "Half-Elf" || race == "Variant Human")
            {
                var baseRace = GameData.RaceData[race];
                racialBonuses = new Dictionary<string, int>(baseRace.AbilityBonuses);

                if (cmbFlexibleBonus1.SelectedItem is string b1)
                    racialBonuses[b1] = racialBonuses.GetValueOrDefault(b1) + 1;

                if (cmbFlexibleBonus2.SelectedItem is string b2 && b2 != cmbFlexibleBonus1.SelectedItem?.ToString())
                    racialBonuses[b2] = racialBonuses.GetValueOrDefault(b2) + 1;

                ApplyRacialBonuses();
            }
        }

        private void RaceSkillChoice_Changed(object sender, RoutedEventArgs e)
        {
            if (cmbRaceSkillChoice == null) return;

            if (rbRaceSkill.IsChecked == true)
            {
                cmbRaceSkillChoice.Visibility = Visibility.Visible;
                cmbRaceSkillChoice.ItemsSource = allSkills.Select(s => s.SkillName).ToList();
                if (string.IsNullOrEmpty(raceGrantedSkill))
                    cmbRaceSkillChoice.SelectedIndex = 0;
            }
            else
            {
                cmbRaceSkillChoice.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrEmpty(raceGrantedSkill))
                {
                    var oldSkill = allSkills.FirstOrDefault(s => s.SkillName == raceGrantedSkill);
                    if (oldSkill != null && oldSkill.IsBackgroundProficiency)
                    {
                        oldSkill.IsProficient = false;
                        oldSkill.IsBackgroundProficiency = false;
                    }
                    raceGrantedSkill = "";
                    dgSkills.Items.Refresh();
                }
            }
        }

        private void RaceGrantedSkill_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRaceSkillChoice.SelectedItem is not string newSkillName || string.IsNullOrWhiteSpace(newSkillName))
                return;

            if (newSkillName == raceGrantedSkill) return;

            // Clear old
            if (!string.IsNullOrEmpty(raceGrantedSkill))
            {
                var old = allSkills.FirstOrDefault(s => s.SkillName == raceGrantedSkill);
                if (old != null)
                {
                    old.IsProficient = false;
                    old.IsBackgroundProficiency = false;
                }
            }

            // Apply new
            var skill = allSkills.FirstOrDefault(s => s.SkillName == newSkillName);
            if (skill != null)
            {
                skill.IsProficient = true;
                skill.IsBackgroundProficiency = true;
                raceGrantedSkill = newSkillName;

                if (lblRaceProficienciesDetail != null)
                {
                    lblRaceProficienciesDetail.Text = newSkillName;
                    lblRaceProficienciesDetail.Visibility = Visibility.Visible;
                }
            }

            dgSkills.Items.Refresh();
            UpdateSkillBonuses();
        }

        private void HighElfCantrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbHighElfCantrip.SelectedItem is not string cantripName)
            {
                pnlHighElfCantripPreview.Visibility = Visibility.Collapsed;
                highElfCantrip = "";
                UpdateRacialSpellsLabel();
                return;
            }

            highElfCantrip = cantripName;

            // Find the full spell data
            var spell = GameData.AllCantrips.FirstOrDefault(s =>
        s.Name.Equals(cantripName, StringComparison.OrdinalIgnoreCase) &&
        s.Classes.Contains("Wizard", StringComparer.OrdinalIgnoreCase));

            if (spell != null)
            {
                txtHighElfCantripPreview.Text = spell.FormatDetails(includeFullText: true);
                pnlHighElfCantripPreview.Visibility = Visibility.Visible;
            }

            UpdateRacialSpellsLabel();
        }

        private string GetRacialCantrip()
        {
            var grants = GetActiveRacialSpells();
            var cantrip = grants.FirstOrDefault(g => g.SpellLevel <= 0);
            return cantrip?.SpellName ?? "";
        }

        /// <summary>Racial innate spells available at the character's current level.</summary>
        private List<RacialSpellGrant> GetActiveRacialSpells()
        {
            string race = cmbRace.SelectedItem?.ToString() ?? "";
            string subrace = cmbSubrace.SelectedItem?.ToString() ?? "";
            int charLevel = GetEffectiveCharacterLevel();
            return GameData.GetRacialSpells(race, subrace, charLevel, highElfCantrip);
        }

        private void UpdateRacialSpellsLabel()
        {
            if (lblRacialSpells == null) return;

            string race = cmbRace.SelectedItem?.ToString() ?? "";
            string subrace = cmbSubrace.SelectedItem?.ToString() ?? "";
            string displayName = !string.IsNullOrEmpty(subrace) ? subrace : race;
            int charLevel = GetEffectiveCharacterLevel();

            var available = GameData.GetRacialSpells(race, subrace, charLevel, highElfCantrip);
            var locked = GameData.GetAllRacialSpellGrants(race, subrace, highElfCantrip)
                .Where(g => g.MinCharacterLevel > charLevel)
                .ToList();

            if (available.Count == 0 && locked.Count == 0)
            {
                lblRacialSpells.Text = "Racial spells: (none yet)";
                lblRacialSpells.Foreground = (Brush)new BrushConverter().ConvertFromString("#AAA");
                return;
            }

            var parts = available.Select(FormatRacialGrant).ToList();
            if (locked.Count > 0)
            {
                parts.Add("locked until higher level: " +
                          string.Join(", ", locked.Select(g =>
                              $"{g.SpellName} (L{Math.Max(g.SpellLevel, 0)} @ char {g.MinCharacterLevel}+)")));
            }

            lblRacialSpells.Text = string.IsNullOrEmpty(displayName)
                ? $"Racial spells: {string.Join("; ", parts)}"
                : $"Racial spells ({displayName}): {string.Join("; ", parts)}";
            lblRacialSpells.Foreground = AccentGreen;
        }

        private static string FormatRacialGrant(RacialSpellGrant g)
        {
            string lvl = g.SpellLevel <= 0 ? "cantrip" : $"L{g.SpellLevel}";
            string note = string.IsNullOrWhiteSpace(g.Notes) ? "" : $" [{g.Notes}]";
            return $"{g.SpellName} ({lvl}{note})";
        }

        /// <summary>Active class levels from character data, or UI class/subclass at character level.</summary>
        private List<ClassLevelEntry> GetActiveClassLevels()
        {
            SyncCharacterClassFromUi();
            return LevelUpCalculator.GetClassLevelsFromCharacter(CurrentCharacter);
        }

        private int GetEffectiveCharacterLevel()
        {
            var levels = GetActiveClassLevels();
            int sum = levels.Sum(e => e.Levels);
            if (sum > 0) return Math.Clamp(sum, 1, 20);
            return CurrentCharacter?.Level > 0 ? Math.Clamp(CurrentCharacter.Level, 1, 20) : 1;
        }

        /// <summary>
        /// Starting class for wealth/equipment: first entry in <see cref="Character.ClassLevels"/>
        /// (the class taken at character creation). Later multiclass rows never affect gold formulas.
        /// </summary>
        private string GetStartingClassName()
        {
            if (CurrentCharacter?.ClassLevels != null)
            {
                var first = CurrentCharacter.ClassLevels
                    .FirstOrDefault(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName));
                if (first != null)
                    return first.ClassName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(CurrentCharacter?.Class))
                return CurrentCharacter.Class.Trim();

            if (cmbClass?.SelectedItem is string uiClass && !string.IsNullOrWhiteSpace(uiClass))
                return uiClass.Trim();

            return "";
        }

        /// <summary>
        /// Total character level for DMG wealth bands (sum of all class levels — not per-class).
        /// </summary>
        private int GetCharacterLevelForWealth() => GetEffectiveCharacterLevel();

        /// <summary>
        /// Keep Character.Class / Subclass mirrored from UI when ClassLevels is empty or single-class.
        /// Multiclass (2+ rows) is owned by the Level &amp; Multiclass tab.
        /// </summary>
        private void SyncCharacterClassFromUi()
        {
            if (CurrentCharacter == null) return;

            if (CurrentCharacter.Level <= 0)
                CurrentCharacter.Level = 1;

            // Multiclass builds: only ensure totals; do not clobber ClassLevels from Class tab combos
            if (CurrentCharacter.ClassLevels != null && CurrentCharacter.ClassLevels.Count > 1)
            {
                CurrentCharacter.Level = Math.Max(1, CurrentCharacter.ClassLevels.Sum(e => e.Levels));
                if (CurrentCharacter.ClassLevels.Count > 0)
                {
                    CurrentCharacter.Class = CurrentCharacter.ClassLevels[0].ClassName;
                    CurrentCharacter.Subclass = CurrentCharacter.ClassLevels[0].Subclass ?? "";
                }
                return;
            }

            if (cmbClass?.SelectedItem is string className && !string.IsNullOrWhiteSpace(className))
                CurrentCharacter.Class = className;

            if (cmbSubclass?.SelectedItem is string sub)
            {
                if (!sub.StartsWith("Requires", StringComparison.OrdinalIgnoreCase) &&
                    !sub.StartsWith("(No", StringComparison.OrdinalIgnoreCase))
                    CurrentCharacter.Subclass = sub;
            }

            if (!string.IsNullOrWhiteSpace(CurrentCharacter.Class))
            {
                int lvl = CurrentCharacter.ClassLevels?.Count == 1
                    ? Math.Max(1, CurrentCharacter.ClassLevels[0].Levels)
                    : (CurrentCharacter.Level > 0 ? CurrentCharacter.Level : 1);

                if (CurrentCharacter.ClassLevels == null || CurrentCharacter.ClassLevels.Count == 0)
                {
                    CurrentCharacter.ClassLevels = new List<ClassLevelEntry>
                    {
                        new(CurrentCharacter.Class, lvl, CurrentCharacter.Subclass)
                    };
                }
                else
                {
                    var only = CurrentCharacter.ClassLevels[0];
                    only.ClassName = CurrentCharacter.Class;
                    only.Subclass = CurrentCharacter.Subclass;
                    if (only.Levels <= 0)
                        only.Levels = lvl;
                    CurrentCharacter.Level = only.Levels;
                }
            }
        }

        private SpellBudgetSnapshot GetSpellBudget()
        {
            var levels = GetActiveClassLevels();
            return SpellProgressionCalculator.GetBudget(levels, cls =>
            {
                if (!GameData.ClassData.TryGetValue(cls, out var data))
                    return 0;
                return CalculateModifier(GetFinalStat(data.SpellAbility));
            });
        }

        // ───────────────────────── Level & Multiclass tab ─────────────────────────

        private int GetAsiBonusForAbility(string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName) || CurrentCharacter?.AsiOrFeatDecisions == null)
                return 0;
            var map = LevelUpCalculator.GetAsiStatBonuses(CurrentCharacter.AsiOrFeatDecisions);
            return map.TryGetValue(abilityName, out int v) ? v : 0;
        }

        /// <summary>
        /// Max feats the player may select: origin race feat (+1) + each ASI milestone taken as Feat.
        /// </summary>
        public int GetMaxFeatSelections()
        {
            int max = 0;
            string race = cmbRace?.SelectedItem?.ToString() ?? CurrentCharacter?.Race ?? "";
            if (GameData.FeatGrantingRaces.Contains(race))
                max += 1;
            max += LevelUpCalculator.CountFeatPicksFromAsi(CurrentCharacter?.AsiOrFeatDecisions);
            return max;
        }

        public void UpdateFeatSelectionLimitLabel()
        {
            if (lblFeatSelectionLimit == null) return;
            int max = GetMaxFeatSelections();
            int selected = GameData.AllFeats?.Count(f => f != null && f.IsSelected) ?? 0;
            lblFeatSelectionLimit.Text = max <= 0
                ? "You cannot select feats yet (need an origin feat race or an ASI→Feat pick)."
                : $"Selected: {selected} / {max} feat(s)";
            if (lblFeatTabHeader != null)
                lblFeatTabHeader.Text = max == 1 ? "SELECT FEAT" : "SELECT FEATS";
        }

        private void EnsureFeatsLoaded()
        {
            if (dgFeats == null) return;
            if (dgFeats.ItemsSource == null)
            {
                GameData.InitializeFeats();
                dgFeats.ItemsSource = GameData.AllFeats;
                dgFeats.SelectionChanged -= DgFeats_SelectionChanged;
                dgFeats.SelectionChanged += DgFeats_SelectionChanged;
            }
        }

        public void UpdateFeatsTabVisibility()
        {
            if (tabFeats == null) return;
            int max = GetMaxFeatSelections();
            if (max > 0)
            {
                EnsureFeatsLoaded();
                tabFeats.Visibility = Visibility.Visible;
            }
            else
            {
                // Hide only if nothing selected (avoid trapping the user with selected feats)
                int selected = GameData.AllFeats?.Count(f => f != null && f.IsSelected) ?? 0;
                tabFeats.Visibility = selected > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            UpdateFeatSelectionLimitLabel();
            TrimFeatsToLimit();
        }

        /// <summary>If the feat budget dropped, deselect extras (most recently kept = first in list order).</summary>
        private void TrimFeatsToLimit()
        {
            if (GameData.AllFeats == null) return;
            int max = GetMaxFeatSelections();
            var selected = GameData.AllFeats.Where(f => f != null && f.IsSelected).ToList();
            if (selected.Count <= max) return;

            foreach (var feat in selected.Skip(max).ToList())
                feat.IsSelected = false;
            dgFeats?.Items.Refresh();
            UpdateFeatSelectionLimitLabel();
        }

        /// <summary>Seed / refresh ClassLevels from the Class tab when empty.</summary>
        /// <summary>Subclass chosen on the Class &amp; Subclass tab, or null if placeholder/none.</summary>
        private string? GetUiSelectedSubclassName()
        {
            if (cmbSubclass?.SelectedItem is not string sub)
                return null;
            if (string.IsNullOrWhiteSpace(sub) ||
                sub.StartsWith("Requires", StringComparison.OrdinalIgnoreCase) ||
                sub.StartsWith("(No", StringComparison.OrdinalIgnoreCase))
                return null;
            return sub.Trim();
        }

        private void EnsureClassLevelsSeeded()
        {
            if (CurrentCharacter == null) return;

            if (CurrentCharacter.ClassLevels == null)
                CurrentCharacter.ClassLevels = new List<ClassLevelEntry>();

            string? uiSub = GetUiSelectedSubclassName();

            if (CurrentCharacter.ClassLevels.Count == 0 &&
                cmbClass?.SelectedItem is string className &&
                !string.IsNullOrWhiteSpace(className))
            {
                // Only apply subclass if unlocked at level 1 (Cleric/Sorcerer/Warlock)
                string? seedSub = GameData.HasUnlockedSubclass(className, 1) ? uiSub : null;
                CurrentCharacter.ClassLevels.Add(new ClassLevelEntry(className, 1, seedSub));
                CurrentCharacter.Class = className;
                CurrentCharacter.Subclass = seedSub ?? "";
                CurrentCharacter.Level = 1;
            }

            // Carry Class-tab subclass onto the primary row only when that class has unlocked subclasses.
            if (CurrentCharacter.ClassLevels.Count >= 1 &&
                cmbClass?.SelectedItem is string primaryClass &&
                !string.IsNullOrWhiteSpace(primaryClass))
            {
                var primary = CurrentCharacter.ClassLevels.FirstOrDefault(c =>
                                  c.ClassName.Equals(primaryClass, StringComparison.OrdinalIgnoreCase))
                              ?? CurrentCharacter.ClassLevels[0];

                if (primary.ClassName.Equals(primaryClass, StringComparison.OrdinalIgnoreCase) ||
                    CurrentCharacter.ClassLevels.Count == 1)
                {
                    if (CurrentCharacter.ClassLevels.Count == 1)
                        primary.ClassName = primaryClass;

                    if (GameData.HasUnlockedSubclass(primary.ClassName, primary.Levels))
                    {
                        if (!string.IsNullOrWhiteSpace(uiSub))
                        {
                            primary.Subclass = uiSub;
                            CurrentCharacter.Subclass = uiSub;
                        }
                        else if (!string.IsNullOrWhiteSpace(primary.Subclass))
                        {
                            CurrentCharacter.Subclass = primary.Subclass ?? "";
                        }
                    }
                    else
                    {
                        // Planned picks are ignored until unlock level
                        primary.Subclass = null;
                        CurrentCharacter.Subclass = "";
                    }
                }
            }

            ReconcileAsiDecisions();
        }

        private void ReconcileAsiDecisions()
        {
            if (CurrentCharacter == null) return;
            CurrentCharacter.AsiOrFeatDecisions = LevelUpCalculator.ReconcileAsiOrFeatDecisions(
                CurrentCharacter.ClassLevels ?? new List<ClassLevelEntry>(),
                CurrentCharacter.AsiOrFeatDecisions);
        }

        /// <summary>
        /// Official 5e proficiency bonus from total character level
        /// (1–4 → +2, 5–8 → +3, 9–12 → +4, 13–16 → +5, 17–20 → +6).
        /// Updates the field, character model, and Skills-tab label.
        /// </summary>
        private void RefreshProficiencyBonus()
        {
            int total = 1;
            try
            {
                total = GetEffectiveCharacterLevel();
            }
            catch
            {
                total = CurrentCharacter?.Level > 0 ? CurrentCharacter.Level : 1;
            }

            total = Math.Clamp(total, 1, 20);
            proficiencyBonus = LevelUpCalculator.GetProficiencyBonus(total);

            if (CurrentCharacter != null)
            {
                CurrentCharacter.Level = Math.Max(CurrentCharacter.Level, total);
                // Keep Level in sync with class levels when those exist
                if (CurrentCharacter.ClassLevels != null && CurrentCharacter.ClassLevels.Count > 0)
                    CurrentCharacter.Level = Math.Clamp(
                        CurrentCharacter.ClassLevels.Sum(e => e.Levels), 1, 20);
                CurrentCharacter.ProficiencyBonus = proficiencyBonus;
            }

            if (lblProficiencyBonus != null)
            {
                int displayLevel = CurrentCharacter?.Level > 0 ? CurrentCharacter.Level : total;
                lblProficiencyBonus.Text =
                    $"Proficiency Bonus: +{proficiencyBonus} (character level {displayLevel})";
            }
        }

        private void ApplyLevelDerivedState()
        {
            if (CurrentCharacter == null) return;

            EnsureClassLevelsSeeded();
            var levels = CurrentCharacter.ClassLevels ?? new List<ClassLevelEntry>();
            int total = Math.Clamp(levels.Sum(e => e.Levels), 0, 20);
            if (total <= 0) total = 1;

            CurrentCharacter.Level = total;
            RefreshProficiencyBonus();

            if (levels.Count > 0)
            {
                CurrentCharacter.Class = levels[0].ClassName;
                CurrentCharacter.Subclass = levels[0].Subclass ?? "";
            }

            if (rbHpAverage?.IsChecked == true)
                CurrentCharacter.HpGainMethod = HpGainMethod.FixedAverage;
            else if (rbHpRolled?.IsChecked == true)
                CurrentCharacter.HpGainMethod = HpGainMethod.Rolled;

            UpdateHitPoints();
            UpdateSkillBonuses();
            UpdateExpertiseSelectableState();
            UpdateSavingThrows();
            UpdateFeatsTabVisibility();
            UpdateSpellTabVisibility();
            UpdateStatDisplays();
            // Multiclass / secondary-class subclasses (e.g. Twilight Cleric) grant armor & weapons
            UpdateEquipmentProficiencySummary();
            // Level band controls equipment-vs-gold and DMG higher-level wealth
            RefreshStartingWealthUi();
            UpdateTotalEquipmentSummary();

            // Always refresh spell level unlocks when levels change (Ranger 5 → 2nd-level slots, etc.)
            // even if the Spells tab is not currently open. Isolated so failures don't break +/− levels.
            // Includes third-caster subclasses (Arcane Trickster / Eldritch Knight), not only base casters.
            try
            {
                if (CharacterHasSpellcastingFeature())
                {
                    if (cantripOptions.Count == 0 || spell1Options.Count == 0)
                    {
                        PopulateSpells();
                    }
                    else
                    {
                        RebalanceCantripAssignments(BuildPreferredCantripAssignments());
                        cantripViewSource.View?.Refresh();
                        RefreshSpellLevelDropdown();
                        ApplyCantripSelectableState();
                        ApplyLeveledSpellSelectableState();
                        UpdateSpellStats();
                        UpdateCantripCounter();
                        UpdateSpellCounter();
                    }
                }
                else
                {
                    RefreshSpellLevelDropdown();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ApplyLevelDerivedState spell refresh: " + ex);
            }
        }

        /// <summary>Snapshot of current UI cantrip → class assignments (for rebalance).</summary>
        private Dictionary<string, string> BuildPreferredCantripAssignments()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (cantripOptions == null) return map;
            foreach (var c in cantripOptions.Where(x => x.IsChecked && !string.IsNullOrWhiteSpace(x.AssignedClassKey)))
                map[c.Name] = c.AssignedClassKey;
            return map;
        }

        private void RefreshLevelMulticlassTab()
        {
            if (pnlClassLevelRows == null) return;
            if (_suppressLevelTabRebuild) return;

            _suppressLevelTabRebuild = true;
            try
            {
                EnsureClassLevelsSeeded();
                RebuildClassLevelRows();
                RebuildAsiFeatChoicePanels();
                RebuildClassFeatureOptionPanels();
                RebuildHpRollRows();
                ApplyLevelDerivedState();
                UpdateLevelSummaryLabels();
            }
            catch (Exception ex)
            {
                // Keep the tab usable; swallow UI-derived failures so +/−/Remove still stick.
                System.Diagnostics.Debug.WriteLine("RefreshLevelMulticlassTab: " + ex);
            }
            finally
            {
                _suppressLevelTabRebuild = false;
            }
        }

        private static Button MakeLevelStepperButton(string content) => new Button
        {
            Content = content,
            Width = 32,
            Height = 28,
            Margin = content == "+" ? new Thickness(4, 0, 0, 0) : new Thickness(0, 0, 4, 0),
            Background = BrushPrimary,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            BorderBrush = ThemeBrush("Nemo.Brush.Border", "#555555"),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            IsHitTestVisible = true,
            Focusable = true
        };

        /// <summary>
        /// +1 / −1 a class row's levels (or remove the row when multiclass hits 0).
        /// </summary>
        private void AdjustClassLevel(int index, int delta)
        {
            if (CurrentCharacter?.ClassLevels == null) return;
            if (index < 0 || index >= CurrentCharacter.ClassLevels.Count) return;

            var row = CurrentCharacter.ClassLevels[index];
            if (delta > 0)
            {
                int sum = CurrentCharacter.ClassLevels.Sum(x => x.Levels);
                if (sum >= 20)
                {
                    MessageBox.Show("Character level cannot exceed 20.", "Max Level",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                row.Levels++;
            }
            else if (delta < 0)
            {
                if (row.Levels <= 1)
                {
                    // Drop the class entirely only when multiclassing
                    if (CurrentCharacter.ClassLevels.Count > 1)
                        CurrentCharacter.ClassLevels.RemoveAt(index);
                    else
                        return; // can't go below 1 for single-class
                }
                else
                {
                    row.Levels--;
                }
            }
            else return;

            // Force suppress off so a stuck flag can't block rebuild after a prior error
            _suppressLevelTabRebuild = false;
            RefreshLevelMulticlassTab();
            SyncClassTabFromLevels();
        }

        private void RemoveClassLevelRow(int index)
        {
            if (CurrentCharacter?.ClassLevels == null) return;
            if (CurrentCharacter.ClassLevels.Count <= 1)
            {
                MessageBox.Show(
                    "Remove is only available for multiclass builds (2+ classes).\n" +
                    "Use + Add class first, or use − to lower this class's levels.",
                    "Remove Class",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            if (index < 0 || index >= CurrentCharacter.ClassLevels.Count) return;

            CurrentCharacter.ClassLevels.RemoveAt(index);
            _suppressLevelTabRebuild = false;
            RefreshLevelMulticlassTab();
            SyncClassTabFromLevels();
        }

        /// <summary>
        /// Fighting Styles, Eldritch Invocations, Metamagic, and Warlock Pact Boon
        /// based on current class levels.
        /// </summary>
        private void RebuildClassFeatureOptionPanels()
        {
            if (pnlClassFeatureOptions == null || CurrentCharacter == null) return;
            pnlClassFeatureOptions.Children.Clear();

            CurrentCharacter.FightingStyles ??= new List<string>();
            CurrentCharacter.EldritchInvocations ??= new List<string>();
            CurrentCharacter.MetamagicOptions ??= new List<string>();

            var entries = CurrentCharacter.ClassLevels?
                .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                .ToList() ?? new List<ClassLevelEntry>();

            int fighterLv = 0;
            string? fighterSub = null;
            int warlockLv = 0;
            int sorcererLv = 0;
            int paladinLv = 0;
            int rangerLv = 0;

            foreach (var e in entries)
            {
                string cn = e.ClassName.Trim();
                if (cn.Equals("Fighter", StringComparison.OrdinalIgnoreCase))
                {
                    fighterLv += e.Levels;
                    if (!string.IsNullOrWhiteSpace(e.Subclass)) fighterSub = e.Subclass;
                }
                else if (cn.Equals("Warlock", StringComparison.OrdinalIgnoreCase))
                    warlockLv += e.Levels;
                else if (cn.Equals("Sorcerer", StringComparison.OrdinalIgnoreCase))
                    sorcererLv += e.Levels;
                else if (cn.Equals("Paladin", StringComparison.OrdinalIgnoreCase))
                    paladinLv += e.Levels;
                else if (cn.Equals("Ranger", StringComparison.OrdinalIgnoreCase))
                    rangerLv += e.Levels;
            }

            bool any = false;

            // ── Warlock Pact Boon (level 3+) ──
            if (warlockLv >= 3)
            {
                any = true;
                BuildPactBoonCard();
            }
            else if (warlockLv > 0 && warlockLv < 3)
            {
                any = true;
                pnlClassFeatureOptions.Children.Add(MakeClassDetailText(
                    "Warlock Pact Boon unlocks at Warlock level 3.",
                    foreground: ClassDetailMutedBrush,
                    margin: new Thickness(0, 0, 0, 8)));
                CurrentCharacter.WarlockPactBoon = "";
            }
            else
            {
                CurrentCharacter.WarlockPactBoon = "";
            }

            // ── Fighting styles ──
            int fsSlots = ClassFeatureOptionData.GetFighterFightingStylesKnown(fighterLv, fighterSub)
                          + ClassFeatureOptionData.GetPaladinOrRangerFightingStylesKnown(paladinLv)
                          + ClassFeatureOptionData.GetPaladinOrRangerFightingStylesKnown(rangerLv);

            // Collect which classes contribute for labeling
            var fsClassLabels = new List<string>();
            if (fighterLv >= 1)
            {
                fsClassLabels.Add(fighterLv >= 10 &&
                                  fighterSub != null &&
                                  fighterSub.Contains("Champion", StringComparison.OrdinalIgnoreCase)
                    ? $"Fighter {fighterLv} (Champion: 2 styles)"
                    : $"Fighter {fighterLv}");
            }
            if (paladinLv >= 2) fsClassLabels.Add($"Paladin {paladinLv}");
            if (rangerLv >= 2) fsClassLabels.Add($"Ranger {rangerLv}");

            if (fsSlots > 0)
            {
                any = true;
                // Allowed styles = union of classes that grant styles
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (fighterLv >= 1)
                    foreach (var o in ClassFeatureOptionData.GetFightingStylesForClass("Fighter"))
                        allowed.Add(o.Name);
                if (paladinLv >= 2)
                    foreach (var o in ClassFeatureOptionData.GetFightingStylesForClass("Paladin"))
                        allowed.Add(o.Name);
                if (rangerLv >= 2)
                    foreach (var o in ClassFeatureOptionData.GetFightingStylesForClass("Ranger"))
                        allowed.Add(o.Name);

                var options = ClassFeatureOptionData.AllFightingStyles
                    .Where(o => allowed.Contains(o.Name))
                    .OrderBy(o => o.Name)
                    .ToList();

                CurrentCharacter.FightingStyles = ClassFeatureOptionData.ReconcilePicks(
                    CurrentCharacter.FightingStyles, fsSlots,
                    name => options.Any(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

                BuildOptionPickCard(
                    title: $"Fighting Styles ({fsSlots})",
                    subtitle: string.Join(" · ", fsClassLabels) + " — each style only once",
                    picks: CurrentCharacter.FightingStyles,
                    catalog: options,
                    onChanged: list =>
                    {
                        CurrentCharacter.FightingStyles = list;
                        // Defense style affects equipped AC (+1 while armored)
                        UpdateEquippedAC();
                    });

                // Reconcile may drop/keep Defense; keep equipped AC in sync
                UpdateEquippedAC();
            }
            else
            {
                CurrentCharacter.FightingStyles = new List<string>();
                UpdateEquippedAC();
            }

            // ── Eldritch Invocations ──
            int invSlots = ClassFeatureOptionData.GetWarlockInvocationsKnown(warlockLv);
            if (invSlots > 0)
            {
                any = true;
                string? pact = CurrentCharacter.WarlockPactBoon;
                var available = ClassFeatureOptionData.GetAvailableInvocations(warlockLv, pact).ToList();

                CurrentCharacter.EldritchInvocations = ClassFeatureOptionData.ReconcilePicks(
                    CurrentCharacter.EldritchInvocations, invSlots,
                    name => available.Any(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

                BuildOptionPickCard(
                    title: $"Eldritch Invocations ({invSlots} known)",
                    subtitle: $"Warlock {warlockLv} — some options require a higher warlock level or a Pact Boon",
                    picks: CurrentCharacter.EldritchInvocations,
                    catalog: available,
                    onChanged: list =>
                    {
                        CurrentCharacter.EldritchInvocations = list;
                        // Refresh so prerequisite-gated options update after picks (no circular rebuild)
                    },
                    showPrerequisite: true);
            }
            else
            {
                if (warlockLv == 1)
                {
                    any = true;
                    pnlClassFeatureOptions.Children.Add(MakeClassDetailText(
                        "Eldritch Invocations unlock at Warlock level 2 (you know 2, then more at 5, 7, 9, …).",
                        foreground: ClassDetailMutedBrush,
                        margin: new Thickness(0, 0, 0, 8)));
                }
                CurrentCharacter.EldritchInvocations = new List<string>();
            }

            // ── Metamagic ──
            int mmSlots = ClassFeatureOptionData.GetSorcererMetamagicKnown(sorcererLv);
            if (mmSlots > 0)
            {
                any = true;
                var mmOptions = ClassFeatureOptionData.AllMetamagic.ToList();
                CurrentCharacter.MetamagicOptions = ClassFeatureOptionData.ReconcilePicks(
                    CurrentCharacter.MetamagicOptions, mmSlots,
                    name => mmOptions.Any(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

                BuildOptionPickCard(
                    title: $"Metamagic ({mmSlots} known)",
                    subtitle: $"Sorcerer {sorcererLv} — 2 at 3rd level, +1 at 10th, +1 at 17th",
                    picks: CurrentCharacter.MetamagicOptions,
                    catalog: mmOptions,
                    onChanged: list => CurrentCharacter.MetamagicOptions = list);
            }
            else
            {
                if (sorcererLv > 0 && sorcererLv < 3)
                {
                    any = true;
                    pnlClassFeatureOptions.Children.Add(MakeClassDetailText(
                        "Metamagic unlocks at Sorcerer level 3 (two options; more at 10th and 17th).",
                        foreground: ClassDetailMutedBrush,
                        margin: new Thickness(0, 0, 0, 8)));
                }
                CurrentCharacter.MetamagicOptions = new List<string>();
            }

            if (lblClassOptionHint != null)
            {
                lblClassOptionHint.Text = any
                    ? "Choose options for each open slot. Duplicates of the same option are not allowed within a category."
                    : "Fighting Styles (Fighter 1+ / Paladin·Ranger 2+), Eldritch Invocations (Warlock 2+), and Metamagic (Sorcerer 3+) appear here as you gain levels.";
            }

            if (!any)
            {
                pnlClassFeatureOptions.Children.Add(MakeClassDetailText(
                    "No fighting style, invocation, or metamagic picks yet for your current class levels.",
                    foreground: ClassDetailMutedBrush));
            }
        }

        private void BuildPactBoonCard()
        {
            var border = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#252525"),
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#555"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                CornerRadius = new CornerRadius(3)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = "Warlock — Pact Boon (level 3)",
                FontWeight = FontWeights.SemiBold,
                Foreground = ClassDetailSectionBrush,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var boons = ClassFeatureOptionData.AllPactBoons.ToList();
            var names = boons.Select(b => b.Name).ToList();
            var cmb = new ComboBox
            {
                ItemsSource = names,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 6)
            };
            StyleAppComboBox(cmb);

            string current = CurrentCharacter?.WarlockPactBoon ?? "";
            if (!string.IsNullOrWhiteSpace(current) &&
                names.Any(n => n.Equals(current, StringComparison.OrdinalIgnoreCase)))
                cmb.SelectedItem = names.First(n => n.Equals(current, StringComparison.OrdinalIgnoreCase));
            else
            {
                cmb.SelectedIndex = 0;
                if (CurrentCharacter != null && names.Count > 0)
                    CurrentCharacter.WarlockPactBoon = names[0];
            }

            var desc = new TextBlock
            {
                Text = boons.FirstOrDefault(b => b.Name == (cmb.SelectedItem as string))?.Description ?? "",
                TextWrapping = TextWrapping.Wrap,
                Foreground = ClassDetailBodyBrush,
                FontSize = 12
            };

            cmb.SelectionChanged += (s, e) =>
            {
                if (_suppressLevelTabRebuild) return;
                if (cmb.SelectedItem is not string pick || CurrentCharacter == null) return;
                CurrentCharacter.WarlockPactBoon = pick;
                var opt = boons.FirstOrDefault(b => b.Name.Equals(pick, StringComparison.OrdinalIgnoreCase));
                desc.Text = opt?.Description ?? "";
                // Rebuild so invocation list filters by new pact
                _suppressLevelTabRebuild = true;
                try { RebuildClassFeatureOptionPanels(); }
                finally { _suppressLevelTabRebuild = false; }
            };

            stack.Children.Add(cmb);
            stack.Children.Add(desc);
            border.Child = stack;
            pnlClassFeatureOptions.Children.Add(border);
        }

        private void BuildOptionPickCard(
            string title,
            string subtitle,
            List<string> picks,
            List<ClassFeatureOption> catalog,
            Action<List<string>> onChanged,
            bool showPrerequisite = false)
        {
            var border = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#252525"),
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#555"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                CornerRadius = new CornerRadius(3)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = ClassDetailSectionBrush,
                Margin = new Thickness(0, 0, 0, 2)
            });
            stack.Children.Add(new TextBlock
            {
                Text = subtitle,
                Foreground = ClassDetailMutedBrush,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var displayNames = catalog.Select(o =>
            {
                if (showPrerequisite && !string.IsNullOrWhiteSpace(o.Prerequisite))
                    return $"{o.Name}  [req: {o.Prerequisite}" +
                           (o.MinClassLevel > 2 ? $", L{o.MinClassLevel}+" : "") + "]";
                if (o.MinClassLevel > 2 && showPrerequisite)
                    return $"{o.Name}  [L{o.MinClassLevel}+]";
                return o.Name;
            }).ToList();

            // Map display label → option name
            var labelToName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < catalog.Count; i++)
                labelToName[displayNames[i]] = catalog[i].Name;

            var nameToLabel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < catalog.Count; i++)
                nameToLabel[catalog[i].Name] = displayNames[i];

            var descBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = ClassDetailBodyBrush,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0)
            };

            for (int slot = 0; slot < picks.Count; slot++)
            {
                int slotIndex = slot;
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
                row.Children.Add(new TextBlock
                {
                    Text = $"Option {slot + 1}:",
                    Width = 72,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Foreground = Brushes.White
                });

                var cmb = new ComboBox
                {
                    Height = 30,
                    ItemsSource = displayNames.ToList()
                };
                StyleAppComboBox(cmb);
                DockPanel.SetDock(cmb, Dock.Right);

                string current = picks[slotIndex] ?? "";
                if (!string.IsNullOrWhiteSpace(current) && nameToLabel.TryGetValue(current, out var lab))
                    cmb.SelectedItem = lab;
                else if (displayNames.Count > 0)
                {
                    // Prefer first not already taken
                    string? free = displayNames.FirstOrDefault(d =>
                    {
                        string n = labelToName[d];
                        return !picks.Where((p, i) => i != slotIndex && !string.IsNullOrWhiteSpace(p))
                            .Any(p => p.Equals(n, StringComparison.OrdinalIgnoreCase));
                    });
                    cmb.SelectedItem = free ?? displayNames[0];
                    if (cmb.SelectedItem is string sel0 && labelToName.TryGetValue(sel0, out var n0))
                        picks[slotIndex] = n0;
                }

                cmb.SelectionChanged += (s, e) =>
                {
                    if (_suppressLevelTabRebuild) return;
                    if (cmb.SelectedItem is not string label) return;
                    if (!labelToName.TryGetValue(label, out string chosen)) return;

                    // Block duplicates in other slots
                    for (int i = 0; i < picks.Count; i++)
                    {
                        if (i == slotIndex) continue;
                        if (picks[i].Equals(chosen, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show(
                                $"You already selected \"{chosen}\" in another slot. Pick a different option.",
                                "Duplicate Option",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            // revert
                            string prev = picks[slotIndex];
                            if (!string.IsNullOrWhiteSpace(prev) && nameToLabel.TryGetValue(prev, out var prevLab))
                                cmb.SelectedItem = prevLab;
                            return;
                        }
                    }

                    picks[slotIndex] = chosen;
                    onChanged(picks.ToList());

                    var opt = catalog.FirstOrDefault(o =>
                        o.Name.Equals(chosen, StringComparison.OrdinalIgnoreCase));
                    descBlock.Text = opt == null
                        ? ""
                        : (string.IsNullOrWhiteSpace(opt.Prerequisite)
                            ? opt.Description
                            : $"Prerequisite: {opt.Prerequisite}\n{opt.Description}");
                };

                // Initial description for this slot when focused via selection
                if (cmb.SelectedItem is string initLab &&
                    labelToName.TryGetValue(initLab, out var initName))
                {
                    var opt0 = catalog.FirstOrDefault(o =>
                        o.Name.Equals(initName, StringComparison.OrdinalIgnoreCase));
                    if (opt0 != null && slotIndex == 0)
                    {
                        descBlock.Text = string.IsNullOrWhiteSpace(opt0.Prerequisite)
                            ? opt0.Description
                            : $"Prerequisite: {opt0.Prerequisite}\n{opt0.Description}";
                    }
                }

                row.Children.Add(cmb);
                stack.Children.Add(row);
            }

            stack.Children.Add(descBlock);
            border.Child = stack;
            pnlClassFeatureOptions.Children.Add(border);
        }

        /// <summary>
        /// Character levels after 1st, in the same order used by <see cref="LevelUpCalculator.CalculateHitPoints"/>.
        /// </summary>
        private List<(int CharLevel, string ClassName, int DieSize)> GetPostLevel1HpSteps()
        {
            var steps = new List<(int CharLevel, string ClassName, int DieSize)>();
            var levels = CurrentCharacter?.ClassLevels;
            if (levels == null || levels.Count == 0)
                return steps;

            int charLevel = 0;
            bool first = true;
            foreach (var entry in levels)
            {
                if (entry == null || entry.Levels <= 0 || string.IsNullOrWhiteSpace(entry.ClassName))
                    continue;
                int die = LevelUpCalculator.GetHitDieSize(entry.ClassName);
                for (int i = 0; i < entry.Levels; i++)
                {
                    charLevel++;
                    if (first)
                    {
                        first = false;
                        continue;
                    }
                    steps.Add((charLevel, entry.ClassName, die));
                }
            }
            return steps;
        }

        private void EnsureHitPointRollsSized()
        {
            if (CurrentCharacter == null) return;
            if (CurrentCharacter.HitPointRolls == null)
                CurrentCharacter.HitPointRolls = new List<int>();

            int needed = GetPostLevel1HpSteps().Count;
            while (CurrentCharacter.HitPointRolls.Count < needed)
                CurrentCharacter.HitPointRolls.Add(0); // 0 = not set yet → calculator falls back to average
            while (CurrentCharacter.HitPointRolls.Count > needed)
                CurrentCharacter.HitPointRolls.RemoveAt(CurrentCharacter.HitPointRolls.Count - 1);
        }

        private int GetExtraHpPerLevelForCalc()
        {
            int extra = 0;
            if (cmbSubrace?.SelectedItem is string subrace && subrace == "Hill Dwarf")
                extra += 1;
            // Draconic Resilience: +1 HP per sorcerer level — treated as +1 per character level when pure Draconic Sorcerer
            string className = cmbClass?.SelectedItem as string ?? CurrentCharacter?.Class ?? "";
            string subclass = cmbSubclass?.SelectedItem as string ?? CurrentCharacter?.Subclass ?? "";
            if (className == "Sorcerer" &&
                subclass.Contains("Draconic", StringComparison.OrdinalIgnoreCase))
                extra += 1;
            return extra;
        }

        private void RebuildHpRollRows()
        {
            if (pnlHpRollRows == null || pnlHpRollsSection == null) return;

            bool rolled = rbHpRolled?.IsChecked == true ||
                          CurrentCharacter?.HpGainMethod == HpGainMethod.Rolled;
            pnlHpRollsSection.Visibility = rolled ? Visibility.Visible : Visibility.Collapsed;
            if (!rolled)
            {
                pnlHpRollRows.Children.Clear();
                return;
            }

            EnsureHitPointRollsSized();
            var steps = GetPostLevel1HpSteps();
            int conMod = CalculateModifier(GetFinalStat("Constitution"));
            int extra = GetExtraHpPerLevelForCalc();

            _suppressHpRollEvents = true;
            try
            {
                pnlHpRollRows.Children.Clear();

                if (steps.Count == 0)
                {
                    pnlHpRollRows.Children.Add(new TextBlock
                    {
                        Text = "No levels after 1st yet — raise a class level above 1 to roll HP.",
                        Foreground = ClassDetailMutedBrush,
                        FontSize = 12
                    });
                    return;
                }

                for (int i = 0; i < steps.Count; i++)
                {
                    int rollIndex = i;
                    var step = steps[i];
                    int stored = (CurrentCharacter.HitPointRolls != null &&
                                  rollIndex < CurrentCharacter.HitPointRolls.Count)
                        ? CurrentCharacter.HitPointRolls[rollIndex]
                        : 0;

                    var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8), LastChildFill = true };

                    var btnRoll = new Button
                    {
                        Content = "Roll",
                        Width = 56,
                        Height = 28,
                        Margin = new Thickness(8, 0, 0, 0),
                        Background = BrushSuccess,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        Tag = rollIndex
                    };
                    DockPanel.SetDock(btnRoll, Dock.Right);

                    var txtDie = new TextBox
                    {
                        Width = 48,
                        Height = 28,
                        Text = stored >= 1 && stored <= step.DieSize ? stored.ToString() : "",
                        TextAlignment = System.Windows.TextAlignment.Center,
                        VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                        Background = (Brush)new BrushConverter().ConvertFromString("#1E1E1E"),
                        Foreground = Brushes.White,
                        BorderBrush = (Brush)new BrushConverter().ConvertFromString("#555"),
                        Tag = rollIndex,
                        ToolTip = $"Hit die result (1–{step.DieSize})"
                    };
                    DockPanel.SetDock(txtDie, Dock.Right);

                    var lblGain = new TextBlock
                    {
                        Name = $"lblHpGain_{rollIndex}",
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Margin = new Thickness(12, 0, 8, 0),
                        Foreground = AccentGreen,
                        FontSize = 12,
                        MinWidth = 160,
                        Tag = rollIndex
                    };
                    DockPanel.SetDock(lblGain, Dock.Right);

                    var lblInfo = new TextBlock
                    {
                        Text = $"Character level {step.CharLevel} — {step.ClassName} (d{step.DieSize})",
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Foreground = Brushes.White,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap
                    };

                    void RefreshGainLabel()
                    {
                        int dieVal = 0;
                        if (int.TryParse(txtDie.Text, out int parsed))
                            dieVal = parsed;
                        bool valid = dieVal >= 1 && dieVal <= step.DieSize;
                        if (!valid)
                        {
                            int avg = LevelUpCalculator.GetFixedAverageHitDieValue(step.DieSize);
                            int avgGain = Math.Max(1, avg + conMod + extra);
                            lblGain.Text = $"HP: (unset → avg {avgGain})";
                            lblGain.Foreground = ClassDetailMutedBrush;
                            return;
                        }
                        int gain = Math.Max(1, dieVal + conMod + extra);
                        string conPart = conMod >= 0 ? $"+{conMod}" : $"{conMod}";
                        string extraPart = extra > 0 ? $"+{extra}" : "";
                        lblGain.Text = $"HP +{gain}  ({dieVal}{conPart}{extraPart} Con)";
                        lblGain.Foreground = AccentGreen;
                    }

                    RefreshGainLabel();

                    txtDie.TextChanged += (s, e) =>
                    {
                        if (_suppressHpRollEvents) return;
                        if (CurrentCharacter?.HitPointRolls == null) return;
                        EnsureHitPointRollsSized();
                        if (rollIndex >= CurrentCharacter.HitPointRolls.Count) return;

                        if (int.TryParse(txtDie.Text.Trim(), out int dieRoll))
                        {
                            dieRoll = Math.Clamp(dieRoll, 0, step.DieSize);
                            CurrentCharacter.HitPointRolls[rollIndex] = dieRoll;
                        }
                        else if (string.IsNullOrWhiteSpace(txtDie.Text))
                        {
                            CurrentCharacter.HitPointRolls[rollIndex] = 0;
                        }
                        RefreshGainLabel();
                        UpdateHitPoints();
                        UpdateLevelSummaryLabels();
                    };

                    btnRoll.Click += (s, e) =>
                    {
                        int dieRoll = _rng.Next(1, step.DieSize + 1);
                        EnsureHitPointRollsSized();
                        if (CurrentCharacter?.HitPointRolls == null ||
                            rollIndex >= CurrentCharacter.HitPointRolls.Count)
                            return;

                        CurrentCharacter.HitPointRolls[rollIndex] = dieRoll;
                        _suppressHpRollEvents = true;
                        try { txtDie.Text = dieRoll.ToString(); }
                        finally { _suppressHpRollEvents = false; }

                        int gain = Math.Max(1, dieRoll + conMod + extra);
                        RefreshGainLabel();
                        UpdateHitPoints();
                        UpdateLevelSummaryLabels();

                        // Brief feedback — die + Con is what the player “gets”
                        btnRoll.ToolTip = $"Rolled {dieRoll} on d{step.DieSize} → +{gain} HP (includes Con)";
                    };

                    // Layout: info | gain label | textbox | roll
                    // DockPanel: last child fills — add docks first (right to left), then fill
                    row.Children.Add(btnRoll);
                    row.Children.Add(txtDie);
                    row.Children.Add(lblGain);
                    row.Children.Add(lblInfo);
                    pnlHpRollRows.Children.Add(row);
                }
            }
            finally
            {
                _suppressHpRollEvents = false;
            }
        }

        /// <summary>
        /// Hard-check PHB multiclass ability prerequisites for every listed class.
        /// Uses final scores (base + racial + feat + ASI).
        /// </summary>
        private bool ValidateMulticlassPrerequisites(IEnumerable<string> classNames, out string failMessage)
        {
            failMessage = "";
            var fails = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in classNames)
            {
                if (string.IsNullOrWhiteSpace(raw) || !seen.Add(raw.Trim()))
                    continue;
                string cls = raw.Trim();
                if (!LevelUpCalculator.MeetsMulticlassPrerequisites(cls, ab => GetFinalStat(ab), out string req))
                {
                    string scoreHint = GetMulticlassScoreHint(cls);
                    fails.Add($"{cls}: needs {req}{scoreHint}");
                }
            }

            if (fails.Count == 0)
                return true;

            failMessage =
                "Multiclass blocked — ability scores (including racial, feat, and ASI bonuses) do not meet the requirements:\n\n• " +
                string.Join("\n• ", fails) +
                "\n\nRaise ability scores or take an ASI first, then try again.";
            return false;
        }

        private string GetMulticlassScoreHint(string className)
        {
            // Short current-score note for the relevant abilities
            return (className ?? "").Trim() switch
            {
                "Barbarian" => $" (Str {GetFinalStat("Strength")})",
                "Bard" or "Sorcerer" or "Warlock" => $" (Cha {GetFinalStat("Charisma")})",
                "Cleric" or "Druid" => $" (Wis {GetFinalStat("Wisdom")})",
                "Fighter" => $" (Str {GetFinalStat("Strength")}, Dex {GetFinalStat("Dexterity")})",
                "Monk" => $" (Dex {GetFinalStat("Dexterity")}, Wis {GetFinalStat("Wisdom")})",
                "Paladin" => $" (Str {GetFinalStat("Strength")}, Cha {GetFinalStat("Charisma")})",
                "Ranger" => $" (Dex {GetFinalStat("Dexterity")}, Wis {GetFinalStat("Wisdom")})",
                "Rogue" => $" (Dex {GetFinalStat("Dexterity")})",
                "Wizard" or "Artificer" => $" (Int {GetFinalStat("Intelligence")})",
                _ => ""
            };
        }

        /// <summary>First unused class that meets multiclass prereqs, or null.</summary>
        private string? FindEligibleMulticlassOption(HashSet<string> used)
        {
            foreach (var cls in GameData.ClassData.Keys.OrderBy(c => c))
            {
                if (used.Contains(cls)) continue;
                // Must pass prereq for this class AND all already-taken classes
                var trial = used.Append(cls);
                if (ValidateMulticlassPrerequisites(trial, out _))
                    return cls;
            }
            return null;
        }

        private void UpdateLevelSummaryLabels()
        {
            if (lblLevelSummary == null) return;

            var levels = CurrentCharacter?.ClassLevels ?? new List<ClassLevelEntry>();
            int total = Math.Max(1, levels.Sum(e => e.Levels));
            int pb = LevelUpCalculator.GetProficiencyBonus(total);
            string dice = LevelUpCalculator.FormatHitDicePool(LevelUpCalculator.GetHitDicePool(levels));
            string hp = txtHitPoints?.Text ?? "—";

            string breakdown = levels.Count == 0
                ? "—"
                : string.Join(" / ", levels.Select(e =>
                    string.IsNullOrWhiteSpace(e.Subclass)
                        ? $"{e.ClassName} {e.Levels}"
                        : $"{e.ClassName} {e.Levels} ({e.Subclass})"));

            lblLevelSummary.Text =
                $"Total level: {total}  |  Proficiency: +{pb}  |  Hit dice: {dice}  |  HP: {hp}\n" +
                $"Classes: {breakdown}";

            if (lblMulticlassPrereqStatus != null)
            {
                if (levels.Count <= 1)
                {
                    lblMulticlassPrereqStatus.Text = "Single-class — multiclass ability prerequisites apply when you add another class.";
                    lblMulticlassPrereqStatus.Foreground = ClassDetailMutedBrush;
                }
                else
                {
                    var fails = new List<string>();
                    foreach (var e in levels)
                    {
                        if (!LevelUpCalculator.MeetsMulticlassPrerequisites(
                                e.ClassName, ab => GetFinalStat(ab), out string req))
                            fails.Add($"{e.ClassName}: needs {req}");
                    }
                    if (fails.Count == 0)
                    {
                        lblMulticlassPrereqStatus.Text = "Multiclass prerequisites: all classes OK with current ability scores.";
                        lblMulticlassPrereqStatus.Foreground = AccentGreen;
                    }
                    else
                    {
                        lblMulticlassPrereqStatus.Text =
                            "Multiclass prerequisites not met (scores include racial/feat/ASI bonuses):\n• " +
                            string.Join("\n• ", fails);
                        lblMulticlassPrereqStatus.Foreground = BrushDanger;
                    }
                }
            }

            var asiMap = LevelUpCalculator.GetAsiStatBonuses(CurrentCharacter?.AsiOrFeatDecisions);
            if (lblAsiBonusSummary != null)
            {
                if (asiMap.Count == 0)
                    lblAsiBonusSummary.Text = "ASI bonuses applied: (none)";
                else
                    lblAsiBonusSummary.Text = "ASI bonuses applied: " +
                        string.Join(", ", asiMap.OrderBy(kv => kv.Key).Select(kv => $"+{kv.Value} {kv.Key}"));
            }
        }

        private void RebuildClassLevelRows()
        {
            if (pnlClassLevelRows == null || CurrentCharacter == null) return;
            pnlClassLevelRows.Children.Clear();

            var levels = CurrentCharacter.ClassLevels ?? new List<ClassLevelEntry>();
            if (levels.Count == 0)
            {
                pnlClassLevelRows.Children.Add(new TextBlock
                {
                    Text = "Select a class on the Class & Subclass tab first.",
                    Foreground = ClassDetailMutedBrush,
                    Margin = new Thickness(0, 0, 0, 8)
                });
                return;
            }

            var allClassNames = GameData.ClassData.Keys.OrderBy(c => c).ToList();
            int totalLevels = levels.Sum(e => e.Levels);

            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                var entry = levels[i];
                var row = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#2A2A2A"),
                    BorderBrush = (Brush)new BrushConverter().ConvertFromString("#444"),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 8),
                    CornerRadius = new CornerRadius(3)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Class combo
                var cmbClassRow = new ComboBox
                {
                    ItemsSource = allClassNames,
                    SelectedItem = allClassNames.FirstOrDefault(c =>
                        c.Equals(entry.ClassName, StringComparison.OrdinalIgnoreCase)) ?? entry.ClassName,
                    Height = 30,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                StyleAppComboBox(cmbClassRow);
                Grid.SetColumn(cmbClassRow, 0);

                // Level steppers
                var levelPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    IsHitTestVisible = true
                };
                var btnMinus = MakeLevelStepperButton("−");
                var txtLvl = new TextBlock
                {
                    Text = entry.Levels.ToString(),
                    Width = 28,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = Brushes.White
                };
                var btnPlus = MakeLevelStepperButton("+");
                levelPanel.Children.Add(btnMinus);
                levelPanel.Children.Add(txtLvl);
                levelPanel.Children.Add(btnPlus);
                Grid.SetColumn(levelPanel, 1);

                // Subclass
                var subPanel = new StackPanel { Margin = new Thickness(8, 0, 8, 0) };
                int subReq = GameData.GetSubclassLevel(entry.ClassName);
                var lblSub = new TextBlock
                {
                    Text = entry.Levels >= subReq ? "Subclass" : $"Subclass (at lvl {subReq})",
                    FontSize = 11,
                    Foreground = ClassDetailMutedBrush
                };
                var subNames = GameData.GetSubclassNames(entry.ClassName);
                var cmbSub = new ComboBox
                {
                    ItemsSource = subNames,
                    Height = 30,
                    // Allow viewing the planned subclass even before unlock level; editing when unlocked
                    IsEnabled = subNames.Count > 0
                };
                StyleAppComboBox(cmbSub);

                // Subclass only applies at unlock level (Cleric/Sorc/Warlock 1, Druid/Wizard 2, else 3).
                // Below unlock: leave empty / show placeholder — do not apply Gloom Stalker etc.
                bool subclassUnlocked = entry.Levels >= subReq;
                string? desiredSub = entry.Subclass;
                if (string.IsNullOrWhiteSpace(desiredSub) && index == 0 && subclassUnlocked)
                    desiredSub = GetUiSelectedSubclassName();

                if (!subclassUnlocked)
                {
                    // Clear any premature application so exports / features stay clean
                    entry.Subclass = null;
                    if (index == 0 && CurrentCharacter != null)
                        CurrentCharacter.Subclass = "";

                    cmbSub.ItemsSource = new List<string>
                    {
                        $"(Unlocks at {entry.ClassName} level {subReq})"
                    };
                    cmbSub.SelectedIndex = 0;
                    cmbSub.IsEnabled = false;
                    cmbSub.ToolTip =
                        $"{entry.ClassName} chooses a subclass at level {subReq}. " +
                        "Subclass features, spells, and sheet labels apply only once unlocked.";
                }
                else if (!string.IsNullOrWhiteSpace(desiredSub))
                {
                    var match = subNames.FirstOrDefault(s =>
                        s.Equals(desiredSub, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        cmbSub.SelectedItem = match;
                        entry.Subclass = match;
                    }
                    else if (subNames.Count > 0)
                    {
                        cmbSub.SelectedIndex = 0;
                        if (cmbSub.SelectedItem is string autoSub)
                            entry.Subclass = autoSub;
                    }
                }
                else if (subNames.Count > 0)
                {
                    cmbSub.SelectedIndex = 0;
                    if (cmbSub.SelectedItem is string autoSub)
                        entry.Subclass = autoSub;
                }

                subPanel.Children.Add(lblSub);
                subPanel.Children.Add(cmbSub);
                Grid.SetColumn(subPanel, 2);

                bool canRemove = levels.Count > 1;
                var btnRemove = new Button
                {
                    Content = "Remove",
                    Padding = new Thickness(10, 4, 10, 4),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    IsEnabled = canRemove,
                    IsHitTestVisible = true,
                    Focusable = true,
                    Cursor = canRemove ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow,
                    Background = canRemove
                        ? (Brush)new BrushConverter().ConvertFromString("#8B3A3A")
                        : (Brush)new BrushConverter().ConvertFromString("#3A3A3A"),
                    Foreground = canRemove
                        ? Brushes.White
                        : (Brush)new BrushConverter().ConvertFromString("#999999"),
                    BorderBrush = canRemove
                        ? (Brush)new BrushConverter().ConvertFromString("#A05050")
                        : (Brush)new BrushConverter().ConvertFromString("#555555"),
                    BorderThickness = new Thickness(1),
                    Opacity = canRemove ? 1.0 : 0.85,
                    ToolTip = canRemove
                        ? "Remove this class from your multiclass build"
                        : "Remove is available after you add a second class (multiclass)"
                };
                Panel.SetZIndex(btnRemove, 10);
                Grid.SetColumn(btnRemove, 3);

                // Events
                cmbClassRow.SelectionChanged += (s, e) =>
                {
                    if (_suppressLevelTabRebuild) return;
                    if (cmbClassRow.SelectedItem is not string newClass) return;
                    if (CurrentCharacter?.ClassLevels == null) return;
                    if (index < 0 || index >= CurrentCharacter.ClassLevels.Count) return;

                    string previousClass = CurrentCharacter.ClassLevels[index].ClassName;

                    // Prevent duplicate classes
                    if (CurrentCharacter.ClassLevels.Where((x, xi) => xi != index)
                        .Any(x => x.ClassName.Equals(newClass, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("That class is already on your character. Increase its level instead.",
                            "Duplicate Class", MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshLevelMulticlassTab();
                        return;
                    }

                    // Multiclass: hard-block if changing class when 2+ classes, or all classes after change fail prereqs
                    if (CurrentCharacter.ClassLevels.Count > 1)
                    {
                        var projected = CurrentCharacter.ClassLevels
                            .Select((x, xi) => xi == index ? newClass : x.ClassName)
                            .ToList();
                        if (!ValidateMulticlassPrerequisites(projected, out string failMsg))
                        {
                            MessageBox.Show(failMsg, "Multiclass Prerequisite",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            // Revert combo
                            _suppressLevelTabRebuild = true;
                            try { cmbClassRow.SelectedItem = previousClass; }
                            finally { _suppressLevelTabRebuild = false; }
                            return;
                        }
                    }

                    var rowEntry = CurrentCharacter.ClassLevels[index];
                    rowEntry.ClassName = newClass;
                    rowEntry.Subclass = null;
                    RefreshLevelMulticlassTab();
                    SyncClassTabFromLevels();
                };

                btnPlus.Click += (s, e) =>
                {
                    e.Handled = true;
                    AdjustClassLevel(index, delta: +1);
                };

                btnMinus.Click += (s, e) =>
                {
                    e.Handled = true;
                    AdjustClassLevel(index, delta: -1);
                };

                cmbSub.SelectionChanged += (s, e) =>
                {
                    if (_suppressLevelTabRebuild) return;
                    if (CurrentCharacter?.ClassLevels == null) return;
                    if (index >= CurrentCharacter.ClassLevels.Count) return;
                    if (cmbSub.SelectedItem is string subName)
                    {
                        CurrentCharacter.ClassLevels[index].Subclass = subName;
                        // Mirror to Class tab when this is the primary class
                        if (index == 0 && cmbSubclass != null)
                        {
                            var match = cmbSubclass.Items.Cast<object>()
                                .Select(o => o?.ToString())
                                .FirstOrDefault(x => x != null &&
                                    x.Equals(subName, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                _suppressLevelTabRebuild = true;
                                try { cmbSubclass.SelectedItem = match; }
                                finally { _suppressLevelTabRebuild = false; }
                            }
                        }
                        UpdateLevelSummaryLabels();
                        ApplyLevelDerivedState();
                        RebuildAsiFeatChoicePanels();
                    }
                };

                btnRemove.Click += (s, e) =>
                {
                    e.Handled = true;
                    RemoveClassLevelRow(index);
                };

                grid.Children.Add(cmbClassRow);
                grid.Children.Add(levelPanel);
                grid.Children.Add(subPanel);
                grid.Children.Add(btnRemove);
                row.Child = grid;
                pnlClassLevelRows.Children.Add(row);

                // Prereq hint under row for multiclass
                if (levels.Count > 1)
                {
                    string req = LevelUpCalculator.GetMulticlassPrerequisiteText(entry.ClassName);
                    bool ok = LevelUpCalculator.MeetsMulticlassPrerequisites(
                        entry.ClassName, ab => GetFinalStat(ab), out _);
                    pnlClassLevelRows.Children.Add(new TextBlock
                    {
                        Text = ok ? $"  ✓ {entry.ClassName} prereq ({req})" : $"  ✗ {entry.ClassName} needs {req}",
                        Foreground = ok ? AccentGreen : BrushDanger,
                        FontSize = 11,
                        Margin = new Thickness(4, -4, 0, 8)
                    });
                }
            }
        }

        private void RebuildAsiFeatChoicePanels()
        {
            if (pnlAsiFeatChoices == null || CurrentCharacter == null) return;
            pnlAsiFeatChoices.Children.Clear();

            ReconcileAsiDecisions();
            var decisions = CurrentCharacter.AsiOrFeatDecisions ?? new List<AsiOrFeatDecision>();

            if (lblAsiFeatHint != null)
            {
                lblAsiFeatHint.Text = decisions.Count == 0
                    ? "No ASI/feat milestones yet. Reach class level 4 (Fighter also 6/14, Rogue also 10, …) to unlock choices."
                    : $"You have {decisions.Count} ASI/feat choice(s). Pick Ability Score Improvement or Feat for each.";
            }

            foreach (var decision in decisions)
            {
                var card = BuildAsiFeatChoiceCard(decision);
                pnlAsiFeatChoices.Children.Add(card);
            }
        }

        private Border BuildAsiFeatChoiceCard(AsiOrFeatDecision decision)
        {
            var border = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#252525"),
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#555"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                CornerRadius = new CornerRadius(3)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"{decision.ClassName} level {decision.ClassLevel} — Ability Score Improvement or Feat",
                FontWeight = FontWeights.SemiBold,
                Foreground = ClassDetailSectionBrush,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 8)
            });

            string groupName = $"AsiFeat_{decision.ClassName}_{decision.ClassLevel}";

            var rbAsi = new RadioButton
            {
                Content = "Ability Score Improvement (+2 to one score, or +1 to two scores)",
                GroupName = groupName,
                IsChecked = decision.Kind == AsiOrFeatKind.AbilityScoreImprovement,
                Margin = new Thickness(0, 0, 0, 4),
                Foreground = Brushes.White
            };
            var rbFeat = new RadioButton
            {
                Content = "Feat (adds +1 to your feat selection limit on the Feats tab)",
                GroupName = groupName,
                IsChecked = decision.Kind == AsiOrFeatKind.Feat,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = Brushes.White
            };

            var asiPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 0, 4),
                Visibility = decision.Kind == AsiOrFeatKind.AbilityScoreImprovement
                    ? Visibility.Visible
                    : Visibility.Collapsed
            };

            var cmbA = new ComboBox
            {
                Width = 130,
                ItemsSource = AbilityNames.ToList(),
                Margin = new Thickness(0, 0, 8, 0),
                Height = 28
            };
            var cmbB = new ComboBox
            {
                Width = 130,
                ItemsSource = AbilityNames.ToList(),
                Height = 28
            };
            StyleAppComboBox(cmbA);
            StyleAppComboBox(cmbB);

            if (!string.IsNullOrWhiteSpace(decision.AbilityPlusOneA) &&
                AbilityNames.Contains(decision.AbilityPlusOneA))
                cmbA.SelectedItem = decision.AbilityPlusOneA;
            else
                cmbA.SelectedIndex = 0;

            if (!string.IsNullOrWhiteSpace(decision.AbilityPlusOneB) &&
                AbilityNames.Contains(decision.AbilityPlusOneB))
                cmbB.SelectedItem = decision.AbilityPlusOneB;
            else
                cmbB.SelectedIndex = 0;

            asiPanel.Children.Add(new TextBlock
            {
                Text = "+1",
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            asiPanel.Children.Add(cmbA);
            asiPanel.Children.Add(new TextBlock
            {
                Text = "and +1",
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 6, 0)
            });
            asiPanel.Children.Add(cmbB);
            asiPanel.Children.Add(new TextBlock
            {
                Text = "(same ability twice = +2; max 20)",
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Foreground = ClassDetailMutedBrush,
                FontSize = 11,
                Margin = new Thickness(10, 0, 0, 0)
            });

            var featNote = new TextBlock
            {
                Text = decision.Kind == AsiOrFeatKind.Feat
                    ? "Open the Feats tab to choose which feat you gain from this milestone."
                    : "",
                Foreground = AccentGreen,
                FontSize = 12,
                Margin = new Thickness(20, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Visibility = decision.Kind == AsiOrFeatKind.Feat ? Visibility.Visible : Visibility.Collapsed
            };

            void CommitAsiAbilities()
            {
                if (decision.Kind != AsiOrFeatKind.AbilityScoreImprovement) return;
                decision.AbilityPlusOneA = cmbA.SelectedItem as string ?? "";
                decision.AbilityPlusOneB = cmbB.SelectedItem as string ?? "";
                // Soft cap notice
                ValidateAsiAgainstCap(decision);
                UpdateLevelSummaryLabels();
                UpdateStatDisplays();
            }

            cmbA.SelectionChanged += (s, e) => CommitAsiAbilities();
            cmbB.SelectionChanged += (s, e) => CommitAsiAbilities();

            rbAsi.Checked += (s, e) =>
            {
                decision.Kind = AsiOrFeatKind.AbilityScoreImprovement;
                if (string.IsNullOrWhiteSpace(decision.AbilityPlusOneA))
                    decision.AbilityPlusOneA = cmbA.SelectedItem as string ?? "Strength";
                if (string.IsNullOrWhiteSpace(decision.AbilityPlusOneB))
                    decision.AbilityPlusOneB = cmbB.SelectedItem as string ?? decision.AbilityPlusOneA;
                asiPanel.Visibility = Visibility.Visible;
                featNote.Visibility = Visibility.Collapsed;
                featNote.Text = "";
                UpdateFeatsTabVisibility();
                UpdateStatDisplays();
                UpdateLevelSummaryLabels();
            };

            rbFeat.Checked += (s, e) =>
            {
                decision.Kind = AsiOrFeatKind.Feat;
                decision.AbilityPlusOneA = "";
                decision.AbilityPlusOneB = "";
                asiPanel.Visibility = Visibility.Collapsed;
                featNote.Visibility = Visibility.Visible;
                featNote.Text = "Open the Feats tab to choose which feat you gain from this milestone.";
                EnsureFeatsLoaded();
                UpdateFeatsTabVisibility();
                UpdateStatDisplays();
                UpdateLevelSummaryLabels();
            };

            stack.Children.Add(rbAsi);
            stack.Children.Add(asiPanel);
            stack.Children.Add(rbFeat);
            stack.Children.Add(featNote);
            border.Child = stack;
            return border;
        }

        private void ValidateAsiAgainstCap(AsiOrFeatDecision decision)
        {
            // Informational only — still apply; PHB max 20 is enforced visually in summary if over
            foreach (var ab in AbilityNames)
            {
                int final = GetFinalStat(ab);
                if (final > 20)
                {
                    // Don't message spam; summary shows final scores on Ability Scores tab
                }
            }
        }

        private void SyncClassTabFromLevels()
        {
            if (CurrentCharacter?.ClassLevels == null || CurrentCharacter.ClassLevels.Count == 0)
                return;

            var primary = CurrentCharacter.ClassLevels[0];
            if (cmbClass != null &&
                cmbClass.Items.Cast<object>().Select(o => o?.ToString())
                    .Any(c => c != null && c.Equals(primary.ClassName, StringComparison.OrdinalIgnoreCase)))
            {
                if (cmbClass.SelectedItem as string != primary.ClassName)
                {
                    _suppressLevelTabRebuild = true;
                    try
                    {
                        cmbClass.SelectedItem = primary.ClassName;
                    }
                    finally
                    {
                        _suppressLevelTabRebuild = false;
                    }
                }
            }

            // Refresh subclass dropdown for primary class then select
            if (!_suppressLevelTabRebuild)
                PopulateSubclassDropdown(primary.ClassName);

            if (cmbSubclass != null && !string.IsNullOrWhiteSpace(primary.Subclass))
            {
                var match = cmbSubclass.Items.Cast<object>()
                    .Select(o => o?.ToString())
                    .FirstOrDefault(x => x != null &&
                        x.Equals(primary.Subclass, StringComparison.OrdinalIgnoreCase));
                if (match != null && !Equals(cmbSubclass.SelectedItem, match))
                {
                    _suppressLevelTabRebuild = true;
                    try { cmbSubclass.SelectedItem = match; }
                    finally { _suppressLevelTabRebuild = false; }
                }
            }
        }

        private void btnAddClassLevel_Click(object sender, RoutedEventArgs e)
        {
            EnsureClassLevelsSeeded();
            if (CurrentCharacter.ClassLevels == null)
                CurrentCharacter.ClassLevels = new List<ClassLevelEntry>();

            int sum = CurrentCharacter.ClassLevels.Sum(x => x.Levels);
            if (sum >= 20)
            {
                MessageBox.Show("Character level cannot exceed 20.", "Max Level",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var used = new HashSet<string>(
                CurrentCharacter.ClassLevels.Select(c => c.ClassName),
                StringComparer.OrdinalIgnoreCase);

            // Existing classes must already qualify before any multiclass is allowed (PHB)
            if (!ValidateMulticlassPrerequisites(used, out string existingFail))
            {
                MessageBox.Show(
                    "Cannot multiclass yet.\n\n" + existingFail,
                    "Multiclass Prerequisite",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Prefer a class that meets prereqs (with all existing classes)
            string? next = FindEligibleMulticlassOption(used);

            if (next == null)
            {
                // Either all classes taken, or no remaining class meets ability scores
                bool anyUnused = GameData.ClassData.Keys.Any(c => !used.Contains(c));
                if (!anyUnused)
                {
                    MessageBox.Show("All classes are already on this character.", "Multiclass",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Build a helpful list of why remaining options fail
                var reasons = new List<string>();
                foreach (var cls in GameData.ClassData.Keys.OrderBy(c => c))
                {
                    if (used.Contains(cls)) continue;
                    if (!LevelUpCalculator.MeetsMulticlassPrerequisites(cls, ab => GetFinalStat(ab), out string req))
                        reasons.Add($"{cls}: needs {req}{GetMulticlassScoreHint(cls)}");
                }

                MessageBox.Show(
                    "No available class meets multiclass ability prerequisites with your current scores " +
                    "(including racial, feat, and ASI bonuses).\n\n" +
                    string.Join("\n", reasons.Take(12)) +
                    (reasons.Count > 12 ? "\n…" : ""),
                    "Multiclass Prerequisite",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Final hard check: existing + new
            var allAfter = used.Append(next).ToList();
            if (!ValidateMulticlassPrerequisites(allAfter, out string failMsg))
            {
                MessageBox.Show(failMsg, "Multiclass Prerequisite",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentCharacter.ClassLevels.Add(new ClassLevelEntry(next, 1, null));
            RefreshLevelMulticlassTab();
        }

        private void HpGainMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (CurrentCharacter == null) return;
            if (rbHpRolled?.IsChecked == true)
                CurrentCharacter.HpGainMethod = HpGainMethod.Rolled;
            else
                CurrentCharacter.HpGainMethod = HpGainMethod.FixedAverage;
            RebuildHpRollRows();
            UpdateHitPoints();
            UpdateLevelSummaryLabels();
        }

        private void cmbSubrace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRace.SelectedItem is not string race ||
                cmbSubrace.SelectedItem is not string subraceName ||
                !GameData.RaceSubraces.ContainsKey(race)) return;

            var subrace = GameData.RaceSubraces[race].FirstOrDefault(s => s.Name == subraceName);
            if (subrace == null) return;

            // IMPORTANT: Start from base race bonuses (unless the subrace fully replaces ASI, e.g. Feral Tiefling)
            var baseRace = GameData.RaceData[race];
            racialBonuses = subrace.ReplacesAbilityBonuses
                ? new Dictionary<string, int>()
                : new Dictionary<string, int>(baseRace.AbilityBonuses);

            // Apply selected subrace bonus (stacking, or alone when ReplacesAbilityBonuses is set)
            foreach (var kv in subrace.AbilityBonus)
            {
                if (racialBonuses.ContainsKey(kv.Key))
                    racialBonuses[kv.Key] += kv.Value;
                else
                    racialBonuses[kv.Key] = kv.Value;
            }

            // === NEW: Determine final speed (subrace override takes priority) ===
            int finalSpeed = baseRace.Speed;
            if (subrace.Speed.HasValue)
            {
                finalSpeed = subrace.Speed.Value;
            }

            // Update the details panel
            string bonuses = racialBonuses.Count > 0
                ? string.Join(", ", racialBonuses.Where(kv => kv.Value != 0).Select(kv => $"+{kv.Value} {kv.Key}"))
                : "(player choice — see traits)";

            var filteredBaseTraits = baseRace.Traits
                .Where(t => !t.TrimStart().StartsWith("Languages:", StringComparison.OrdinalIgnoreCase) &&
                            !t.Contains("Common +", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // When a SCAG tiefling variant replaces Infernal Legacy, omit the base Infernal Legacy line
            bool replacesInfernalLegacy = race == "Tiefling" &&
                (subraceName is "Devil's Tongue" or "Hellfire" or "Winged");
            if (replacesInfernalLegacy)
            {
                filteredBaseTraits = filteredBaseTraits
                    .Where(t => !t.StartsWith("Infernal Legacy", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            txtRaceDetails.Text =
                $"ABILITY SCORE INCREASE\n{bonuses}\n\n" +
                $"SUBRACE: {subrace.Name}\n• {string.Join("\n• ", subrace.Traits)}\n\n" +
                $"BASE TRAITS\n• {string.Join("\n• ", filteredBaseTraits)}\n\n" +
                $"LANGUAGES\n{string.Join(", ", baseRace.Languages)}\n\n" +
                $"SPEED\n{finalSpeed} ft";

            ApplyRacialBonuses(); // Updates Ability Scores tab
            ApplyRaceProficiencies(race);
            raceHasInnateSpellcasting = subrace.HasInnateSpellcasting || baseRace.HasInnateSpellcasting;
            UpdateSpellTabVisibility();
            UpdateEquipmentProficiencySummary();

            // High Elf cantrip handling
            if (subraceName == "High Elf")
            {
                pnlHighElfCantrip.Visibility = Visibility.Visible;

                // === ONLY WIZARD CANTRIPS ===
                var wizardCantrips = GameData.AllCantrips
                    .Where(s => s.Classes != null &&
                                s.Classes.Contains("Wizard", StringComparer.OrdinalIgnoreCase))
                    .Select(s => s.Name)
                    .OrderBy(n => n)
                    .ToList();

                cmbHighElfCantrip.ItemsSource = wizardCantrips;

                if (string.IsNullOrEmpty(highElfCantrip))
                    cmbHighElfCantrip.SelectedIndex = 0;
            }
            else
            {
                pnlHighElfCantrip.Visibility = Visibility.Collapsed;
                pnlHighElfCantripPreview.Visibility = Visibility.Collapsed;
                highElfCantrip = "";
            }

            UpdateRacialSpellsLabel();
        }

        private void cmbBackground_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBackground.SelectedItem is not string bg) return;

            // === NEW: Remove previous background equipment to prevent accumulation ===
            if (!string.IsNullOrEmpty(currentBackgroundEquipmentAdded))
            {
                pickedWeapons.RemoveAll(x => x.Contains(currentBackgroundEquipmentAdded, StringComparison.OrdinalIgnoreCase));
                currentBackgroundEquipmentAdded = "";
            }

            string details = GameData.BackgroundDetails.TryGetValue(bg, out var text)
                ? text
                : "Full details loaded from 5e rules.\n\nSKILL PROFICIENCIES: See official source\nEQUIPMENT: See official source\nFEATURE: Unique background feature";

            txtBackgroundDetails.Text = details;

            ApplyBackgroundProficiencies(bg);
            UpdateSkillTabLabels();

            // === NEW: Language choice logic ===
            SetupBackgroundLanguageChoices(details);

            // === NEW: Add the new background equipment cleanly ===
            string bgEquip = GameData.GetBackgroundEquipment(bg);
            txtBackgroundEquipment.Text = !string.IsNullOrWhiteSpace(bgEquip) && !bgEquip.Contains("See Background tab")
                ? bgEquip
                : "No additional equipment from background.";

            if (!string.IsNullOrWhiteSpace(bgEquip) && !bgEquip.Contains("See Background tab"))
            {
                pickedWeapons.Add(bgEquip);
                currentBackgroundEquipmentAdded = bgEquip;
            }

            UpdateTotalEquipmentSummary();   // ← Refresh the equipment list
        }

        private List<string> GetBackgroundSkillList(string background)
        {
            return GameData.BackgroundSkillMap.TryGetValue(background, out var skills)
                   ? skills
                   : new List<string>();
        }

        private void ApplyBackgroundProficiencies(string background)
        {
            if (allSkills == null || dgSkills == null) return;

            // 1. Clear ONLY previous BACKGROUND proficiencies 
            //    (protect race automatic skills + flexible race skill)
            foreach (var skill in allSkills)
            {
                bool isRaceSkill = currentRaceAutomaticSkills.Contains(skill.SkillName, StringComparer.OrdinalIgnoreCase) ||
                                   skill.SkillName == raceGrantedSkill;

                if (skill.IsBackgroundProficiency && !isRaceSkill)
                {
                    skill.IsProficient = false;
                    skill.IsBackgroundProficiency = false;
                }
            }

            // 2. Apply the new background skills
            var backgroundSkills = GetBackgroundSkillList(background);
            foreach (var skillName in backgroundSkills)
            {
                var skill = allSkills.FirstOrDefault(s => s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));
                if (skill != null)
                {
                    skill.IsProficient = true;
                    skill.IsBackgroundProficiency = true;
                }
            }

            // 3. Re-apply race automatic skills (in case any were accidentally cleared)
            foreach (var skillName in currentRaceAutomaticSkills)
            {
                var skill = allSkills.FirstOrDefault(s => s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));
                if (skill != null)
                {
                    skill.IsProficient = true;
                    skill.IsBackgroundProficiency = true;
                }
            }

            // 4. Re-apply flexible race skill if present
            if (!string.IsNullOrEmpty(raceGrantedSkill))
            {
                var raceSkill = allSkills.FirstOrDefault(s => s.SkillName == raceGrantedSkill);
                if (raceSkill != null)
                {
                    raceSkill.IsProficient = true;
                    raceSkill.IsBackgroundProficiency = true;
                }
            }

            dgSkills.Items.Refresh();
            UpdateSkillBonuses();
            UpdateSkillTabLabels();

            // Refresh class skill counter when background changes
            if (cmbClass.SelectedItem is string className)
                UpdateSkillChoices(className);
        }

        // ───────────────────────── Class & Subclass details panel ─────────────────────────

        private static readonly Brush ClassDetailBodyBrush =
            ThemeBrush("Nemo.Brush.TextPrimary", "#DDDDDD");
        private static readonly Brush ClassDetailHeaderBrush =
            ThemeBrush("Nemo.Brush.Info", "#9CDCFE");
        private static readonly Brush ClassDetailMutedBrush =
            ThemeBrush("Nemo.Brush.TextMuted", "#AAAAAA");
        private static readonly Brush ClassDetailUsesBrush =
            ThemeBrush("Nemo.Brush.Accent", "#7CFC00");
        private static readonly Brush ClassDetailSectionBrush =
            ThemeBrush("Nemo.Brush.Warn", "#E8C36A");

        /// <summary>
        /// Filters progression features for the details panel.
        /// Skips "Spellcasting" (already covered by the SPELLCASTING summary) and optional exclude names.
        /// </summary>
        private static List<ClassFeature> FilterDetailFeatures(
            IEnumerable<ClassFeature> features, IEnumerable<string> excludeNames = null)
        {
            if (features == null) return new List<ClassFeature>();

            var excludeSet = (excludeNames ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return features
                .Where(f => !string.IsNullOrWhiteSpace(f.Name) &&
                            !f.Name.Contains("Spellcasting", StringComparison.OrdinalIgnoreCase) &&
                            !excludeSet.Contains(f.Name.Trim()))
                .ToList();
        }

        private TextBlock MakeClassDetailText(
            string text,
            bool bold = false,
            double fontSize = 13,
            Brush foreground = null,
            Thickness? margin = null)
        {
            return new TextBlock
            {
                Text = text ?? "",
                TextWrapping = TextWrapping.Wrap,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = foreground ?? ClassDetailBodyBrush,
                LineHeight = fontSize + 6,
                Margin = margin ?? new Thickness(0, 0, 0, 2)
            };
        }

        /// <summary>Adds feature bullets (optionally with Uses) to a panel.</summary>
        private void AddFeatureDetailBlocks(Panel parent, IEnumerable<ClassFeature> features, bool showLevelPrefix = true)
        {
            if (parent == null || features == null) return;

            foreach (var f in features)
            {
                string name = f.Name?.Trim() ?? "";
                if (f.IsOptional && !name.Contains("Optional", StringComparison.OrdinalIgnoreCase))
                    name += " (Optional)";

                string title = showLevelPrefix && f.Level > 0
                    ? $"• (Lv {f.Level}) {name}"
                    : $"• {name}";

                parent.Children.Add(MakeClassDetailText(title, bold: true, margin: new Thickness(0, 6, 0, 1)));

                if (!string.IsNullOrWhiteSpace(f.Description))
                {
                    parent.Children.Add(MakeClassDetailText(
                        f.Description,
                        bold: false,
                        fontSize: 12.5,
                        foreground: ClassDetailBodyBrush,
                        margin: new Thickness(14, 0, 0, 1)));
                }

                if (!string.IsNullOrWhiteSpace(f.Uses) &&
                    !string.Equals(f.Uses, "Passive", StringComparison.OrdinalIgnoreCase))
                {
                    parent.Children.Add(MakeClassDetailText(
                        $"Uses: {f.Uses}",
                        bold: false,
                        fontSize: 12,
                        foreground: ClassDetailUsesBrush,
                        margin: new Thickness(14, 0, 0, 2)));
                }
            }
        }

        /// <summary>
        /// Collapsed expander listing features from <paramref name="minLevel"/> upward, grouped by level.
        /// </summary>
        private Expander BuildHigherLevelFeaturesExpander(
            string header,
            IEnumerable<ClassFeature> features,
            int minLevel)
        {
            var higher = features
                .Where(f => f.Level >= minLevel)
                .OrderBy(f => f.Level)
                .ThenBy(f => f.Name)
                .ToList();

            var content = new StackPanel { Margin = new Thickness(8, 4, 0, 4) };

            if (higher.Count == 0)
            {
                content.Children.Add(MakeClassDetailText(
                    "(No additional features listed.)",
                    foreground: ClassDetailMutedBrush));
            }
            else
            {
                foreach (var group in higher.GroupBy(f => f.Level).OrderBy(g => g.Key))
                {
                    content.Children.Add(MakeClassDetailText(
                        $"Level {group.Key}",
                        bold: true,
                        fontSize: 12.5,
                        foreground: ClassDetailHeaderBrush,
                        margin: new Thickness(0, 8, 0, 2)));
                    AddFeatureDetailBlocks(content, group, showLevelPrefix: false);
                }
            }

            var headerBlock = new TextBlock
            {
                Text = header,
                Foreground = ClassDetailHeaderBrush,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };

            return new Expander
            {
                Header = headerBlock,
                IsExpanded = false,
                Foreground = ClassDetailHeaderBrush,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 6, 0, 10),
                Padding = new Thickness(0, 2, 0, 2),
                Content = content
            };
        }

        /// <summary>
        /// Rebuilds the Class &amp; Subclass tab details panel for the current class and subclass selection.
        /// Level 1 (or subclass starting level) features stay open; later levels are in expanders.
        /// </summary>
        private void RefreshClassDetailsPanel()
        {
            if (pnlClassDetails == null) return;
            pnlClassDetails.Children.Clear();

            if (cmbClass.SelectedItem is not string className ||
                !GameData.ClassData.ContainsKey(className))
            {
                pnlClassDetails.Children.Add(MakeClassDetailText(
                    "Select a class to see features and level-up progression.",
                    foreground: ClassDetailMutedBrush));
                return;
            }

            var data = GameData.ClassData[className];

            int hitDie = 8;
            if (!string.IsNullOrEmpty(data.HitDie))
            {
                var parts = data.HitDie.Split('d');
                if (parts.Length == 2 && int.TryParse(parts[1], out int die))
                    hitDie = die;
            }

            // ── Core class stats ──
            pnlClassDetails.Children.Add(MakeClassDetailText($"HIT DICE: {data.HitDie}", bold: true));
            pnlClassDetails.Children.Add(MakeClassDetailText(
                $"HP AT 1ST LEVEL: {hitDie} + Con mod",
                margin: new Thickness(0, 0, 0, 8)));

            if (data.SavingThrowProficiencies.Count > 0)
            {
                pnlClassDetails.Children.Add(MakeClassDetailText("SAVING THROW PROFICIENCIES", bold: true, foreground: ClassDetailSectionBrush));
                pnlClassDetails.Children.Add(MakeClassDetailText(
                    "• " + string.Join(", ", data.SavingThrowProficiencies),
                    margin: new Thickness(0, 0, 0, 8)));
            }

            if (data.ArmorProficiencies.Count > 0)
            {
                pnlClassDetails.Children.Add(MakeClassDetailText("ARMOR PROFICIENCIES", bold: true, foreground: ClassDetailSectionBrush));
                pnlClassDetails.Children.Add(MakeClassDetailText(
                    "• " + string.Join(", ", data.ArmorProficiencies),
                    margin: new Thickness(0, 0, 0, 8)));
            }

            if (data.WeaponProficiencies.Count > 0)
            {
                pnlClassDetails.Children.Add(MakeClassDetailText("WEAPON PROFICIENCIES", bold: true, foreground: ClassDetailSectionBrush));
                pnlClassDetails.Children.Add(MakeClassDetailText(
                    "• " + string.Join(", ", data.WeaponProficiencies),
                    margin: new Thickness(0, 0, 0, 8)));
            }

            pnlClassDetails.Children.Add(MakeClassDetailText("SKILL PROFICIENCIES", bold: true, foreground: ClassDetailSectionBrush));
            pnlClassDetails.Children.Add(MakeClassDetailText(
                $"({data.SkillChoiceCount} skills from {string.Join(", ", data.SkillChoices)})",
                margin: new Thickness(0, 0, 0, 8)));

            if (data.Spellcasting)
            {
                pnlClassDetails.Children.Add(MakeClassDetailText("SPELLCASTING", bold: true, foreground: ClassDetailSectionBrush));
                pnlClassDetails.Children.Add(MakeClassDetailText($"Ability: {data.SpellAbility}"));
                pnlClassDetails.Children.Add(MakeClassDetailText($"Cantrips: {data.CantripsKnown}"));

                int slotsAtLevel1 = className switch
                {
                    "Paladin" or "Ranger" => 0,
                    "Warlock" => 1,
                    _ => 2
                };
                pnlClassDetails.Children.Add(MakeClassDetailText($"1st-level Spell slots: {slotsAtLevel1}"));

                if (data.SpellsKnown > 0)
                    pnlClassDetails.Children.Add(MakeClassDetailText($"Spells Known: {data.SpellsKnown}"));
                else if (!string.IsNullOrEmpty(data.SpellsPrepared))
                    pnlClassDetails.Children.Add(MakeClassDetailText($"Spells Prepared: {data.SpellsPrepared}"));

                pnlClassDetails.Children.Add(new FrameworkElement { Height = 8 });
            }

            // ── Base class features (full 1–20; level 1 open, 2–20 collapsed) ──
            var allClassFeats = FilterDetailFeatures(
                GameData.GetClassProgression(className, includeOptional: true));
            if (allClassFeats.Count == 0 &&
                GameData.ClassLevel1Features.TryGetValue(className, out var legacyClassFeats))
            {
                allClassFeats = FilterDetailFeatures(legacyClassFeats);
            }

            var classLevel1 = allClassFeats.Where(f => f.Level <= 1).ToList();
            var classHigher = allClassFeats.Where(f => f.Level > 1).ToList();

            pnlClassDetails.Children.Add(MakeClassDetailText(
                "CLASS FEATURES (Level 1)",
                bold: true,
                fontSize: 14,
                foreground: ClassDetailSectionBrush,
                margin: new Thickness(0, 4, 0, 2)));

            if (classLevel1.Count > 0)
                AddFeatureDetailBlocks(pnlClassDetails, classLevel1, showLevelPrefix: false);
            else
                pnlClassDetails.Children.Add(MakeClassDetailText(
                    "(No level 1 class features listed.)",
                    foreground: ClassDetailMutedBrush));

            if (classHigher.Count > 0)
            {
                int maxClassLevel = classHigher.Max(f => f.Level);
                pnlClassDetails.Children.Add(BuildHigherLevelFeaturesExpander(
                    $"Class level-up features (Levels 2–{maxClassLevel}) — click to expand",
                    classHigher,
                    minLevel: 2));
            }

            // ── Subclass availability list ──
            string subclassLevelText = className switch
            {
                "Cleric" or "Sorcerer" or "Warlock" => "SUBCLASSES (available at level 1)",
                "Druid" or "Wizard" => "SUBCLASSES (available at level 2)",
                _ => "SUBCLASSES (available at level 3)"
            };

            pnlClassDetails.Children.Add(MakeClassDetailText(
                subclassLevelText,
                bold: true,
                foreground: ClassDetailSectionBrush,
                margin: new Thickness(0, 8, 0, 2)));
            pnlClassDetails.Children.Add(MakeClassDetailText(
                string.Join(", ", data.Subclasses),
                margin: new Thickness(0, 0, 0, 10)));

            // ── Selected subclass details ──
            AppendSelectedSubclassDetails(className);
        }

        /// <summary>Appends header + starting-level features + expander for later subclass features.</summary>
        private void AppendSelectedSubclassDetails(string className)
        {
            if (cmbSubclass?.SelectedItem is not string selectedSub) return;

            if (selectedSub.StartsWith("Requires", StringComparison.OrdinalIgnoreCase) ||
                selectedSub.StartsWith("(No", StringComparison.OrdinalIgnoreCase))
                return;

            int subLevel = GameData.GetSubclassLevel(className);
            var catalog = GameData.GetSubclassInfo(className, selectedSub);

            pnlClassDetails.Children.Add(new Separator
            {
                Margin = new Thickness(0, 4, 0, 10),
                Background = (Brush)new BrushConverter().ConvertFromString("#555")
            });

            string displayName = catalog?.Name ?? selectedSub;
            pnlClassDetails.Children.Add(MakeClassDetailText(
                $"=== {displayName.ToUpperInvariant()} ===",
                bold: true,
                fontSize: 14,
                foreground: ClassDetailSectionBrush,
                margin: new Thickness(0, 0, 0, 4)));

            if (catalog != null)
            {
                pnlClassDetails.Children.Add(MakeClassDetailText(
                    $"Available at: {className} level {catalog.LevelAvailable}"));
                if (!string.IsNullOrWhiteSpace(catalog.Source))
                    pnlClassDetails.Children.Add(MakeClassDetailText($"Source: {catalog.Source}"));
                if (!string.IsNullOrWhiteSpace(catalog.Summary))
                    pnlClassDetails.Children.Add(MakeClassDetailText(
                        catalog.Summary,
                        margin: new Thickness(0, 4, 0, 4)));
                if (subLevel > 1)
                {
                    pnlClassDetails.Children.Add(MakeClassDetailText(
                        $"(Note: Nemo's builder is level-1 focused. This subclass is normally chosen at level {subLevel}.)",
                        fontSize: 12,
                        foreground: ClassDetailMutedBrush,
                        margin: new Thickness(0, 2, 0, 6)));
                }
            }

            // Bonus proficiencies / spell hints from legacy subclass tables
            if (className == "Cleric" && GameData.ClericSubclasses.TryGetValue(selectedSub, out var clericSub))
            {
                if (clericSub.AdditionalCantrips.Count > 0)
                    pnlClassDetails.Children.Add(MakeClassDetailText(
                        $"ADDITIONAL CANTRIPS: {string.Join(", ", clericSub.AdditionalCantrips)}"));
                if (clericSub.ArmorProficiencies.Count > 0)
                    pnlClassDetails.Children.Add(MakeClassDetailText(
                        $"ARMOR: {string.Join(", ", clericSub.ArmorProficiencies)}"));
                if (clericSub.WeaponProficiencies.Count > 0)
                    pnlClassDetails.Children.Add(MakeClassDetailText(
                        $"WEAPONS: {string.Join(", ", clericSub.WeaponProficiencies)}"));
            }
            else if (className == "Warlock" && GameData.WarlockSubclasses.TryGetValue(selectedSub, out var warlockSub))
            {
                if (warlockSub.ArmorProficiencies.Count > 0)
                    pnlClassDetails.Children.Add(MakeClassDetailText(
                        $"ARMOR: {string.Join(", ", warlockSub.ArmorProficiencies)}"));
                if (warlockSub.WeaponProficiencies.Count > 0)
                    pnlClassDetails.Children.Add(MakeClassDetailText(
                        $"WEAPONS: {string.Join(", ", warlockSub.WeaponProficiencies)}"));
            }
            else if (className == "Sorcerer" && GameData.SorcererSubclasses.TryGetValue(selectedSub, out var sorcSub))
            {
                if (sorcSub.AdditionalSpells.Count > 0)
                    pnlClassDetails.Children.Add(MakeClassDetailText(
                        $"ORIGIN SPELLS (summary): {string.Join(", ", sorcSub.AdditionalSpells)}"));
            }

            // Subclass progression: starting-level features open; later levels in expander
            var progression = FilterDetailFeatures(GameData.GetSubclassProgression(selectedSub));
            bool usedLegacy = false;
            if (progression.Count == 0 &&
                GameData.SubclassLevel1Features.TryGetValue(selectedSub, out var legacySubFeats))
            {
                progression = FilterDetailFeatures(legacySubFeats);
                usedLegacy = true;
            }

            if (progression.Count == 0) return;

            // Starting level = earliest feature level, or catalog/class subclass level
            int startLevel = progression.Min(f => f.Level > 0 ? f.Level : subLevel);
            if (catalog != null && catalog.LevelAvailable > 0)
                startLevel = Math.Min(startLevel, catalog.LevelAvailable);
            if (subLevel > 0)
                startLevel = Math.Min(startLevel, subLevel);
            if (startLevel < 1) startLevel = 1;

            var startingFeats = progression.Where(f => f.Level <= startLevel).ToList();
            var laterFeats = progression.Where(f => f.Level > startLevel).ToList();

            string startHeader = usedLegacy
                ? $"{displayName.ToUpperInvariant()} FEATURES"
                : $"{displayName.ToUpperInvariant()} FEATURES (Level {startLevel})";

            pnlClassDetails.Children.Add(MakeClassDetailText(
                startHeader,
                bold: true,
                fontSize: 13.5,
                foreground: ClassDetailSectionBrush,
                margin: new Thickness(0, 10, 0, 2)));

            if (startingFeats.Count > 0)
                AddFeatureDetailBlocks(pnlClassDetails, startingFeats, showLevelPrefix: false);
            else
                pnlClassDetails.Children.Add(MakeClassDetailText(
                    "(No features at subclass start level.)",
                    foreground: ClassDetailMutedBrush));

            if (laterFeats.Count > 0)
            {
                int maxSubLevel = laterFeats.Max(f => f.Level);
                pnlClassDetails.Children.Add(BuildHigherLevelFeaturesExpander(
                    $"{displayName} level-up features (Levels {startLevel + 1}–{maxSubLevel}) — click to expand",
                    laterFeats,
                    minLevel: startLevel + 1));
            }
        }

        private void cmbClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClass.SelectedItem is not string className || !GameData.ClassData.ContainsKey(className)) return;

            var data = GameData.ClassData[className];

            // When the player picks a class on tab 4 (not a programmatic sync), re-seed class levels
            // if we're still single-class or empty.
            if (!_suppressLevelTabRebuild && CurrentCharacter != null)
            {
                if (CurrentCharacter.ClassLevels == null || CurrentCharacter.ClassLevels.Count <= 1)
                {
                    string? keepSub = null;
                    int keepLevels = 1;
                    if (CurrentCharacter.ClassLevels?.Count == 1 &&
                        CurrentCharacter.ClassLevels[0].ClassName.Equals(className, StringComparison.OrdinalIgnoreCase))
                    {
                        keepLevels = Math.Max(1, CurrentCharacter.ClassLevels[0].Levels);
                        keepSub = CurrentCharacter.ClassLevels[0].Subclass;
                    }

                    CurrentCharacter.Class = className;
                    CurrentCharacter.ClassLevels = new List<ClassLevelEntry>
                    {
                        new(className, keepLevels, keepSub)
                    };
                    CurrentCharacter.Level = keepLevels;
                    ReconcileAsiDecisions();
                }
            }

            if (data.Spellcasting)
                PopulateSpells();

            // Subclass dropdown — full official list for every class.
            PopulateSubclassDropdown(className);

            // After subclass list is filled, stamp the Class-tab selection onto ClassLevels
            if (!_suppressLevelTabRebuild && CurrentCharacter?.ClassLevels != null)
            {
                string? uiSub = GetUiSelectedSubclassName();
                if (!string.IsNullOrWhiteSpace(uiSub) &&
                    (CurrentCharacter.ClassLevels.Count == 1 ||
                     CurrentCharacter.ClassLevels[0].ClassName.Equals(className, StringComparison.OrdinalIgnoreCase)))
                {
                    CurrentCharacter.ClassLevels[0].Subclass = uiSub;
                    CurrentCharacter.Subclass = uiSub;
                }
            }

            // Rebuild full details (class + selected subclass)
            RefreshClassDetailsPanel();

            UpdateSpellTabVisibility();
            GenerateEquipmentChoices(className);
            UpdateSkillChoices(className);
            UpdateSkillTabLabels();
            UpdateSavingThrows();
            UpdateEquipmentProficiencySummary();
            UpdateBaseAC();
            UpdateHitPoints();
            UpdateSubclassSpellsLabel();
            UpdateCantripChoices(className);

            if (!_suppressLevelTabRebuild && tabLevelMulticlass != null)
                RefreshLevelMulticlassTab();
        }

        /// <summary>
        /// Fills the subclass combo from <see cref="GameData.GetSubclassNames"/>.
        /// Full catalog is always shown for planning; details panel explains required level.
        /// </summary>
        private void PopulateSubclassDropdown(string className)
        {
            if (cmbSubclass == null) return;

            var names = GameData.GetSubclassNames(className);

            if (names.Count == 0)
            {
                cmbSubclass.IsEnabled = false;
                cmbSubclass.ItemsSource = new List<string> { "(No subclasses listed)" };
                cmbSubclass.SelectedIndex = 0;
                return;
            }

            cmbSubclass.IsEnabled = true;

            // Preserve prior ClassLevels / Character subclass when repopulating the list
            string? prefer = GetUiSelectedSubclassName()
                             ?? CurrentCharacter?.Subclass
                             ?? CurrentCharacter?.ClassLevels?.FirstOrDefault()?.Subclass;

            cmbSubclass.ItemsSource = names;

            if (!string.IsNullOrWhiteSpace(prefer))
            {
                var match = names.FirstOrDefault(n =>
                    n.Equals(prefer, StringComparison.OrdinalIgnoreCase));
                cmbSubclass.SelectedItem = match ?? names[0];
            }
            else
            {
                cmbSubclass.SelectedIndex = 0;
            }
        }

        private void cmbSubclass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClass.SelectedItem is not string className) return;

            // Mirror subclass onto primary ClassLevels row when single-class / primary match
            if (!_suppressLevelTabRebuild &&
                CurrentCharacter?.ClassLevels != null &&
                cmbSubclass.SelectedItem is string subName &&
                !subName.StartsWith("Requires", StringComparison.OrdinalIgnoreCase) &&
                !subName.StartsWith("(No", StringComparison.OrdinalIgnoreCase))
            {
                var primary = CurrentCharacter.ClassLevels.FirstOrDefault(c =>
                    c.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase))
                    ?? CurrentCharacter.ClassLevels.FirstOrDefault();
                if (primary != null)
                    primary.Subclass = subName;
                CurrentCharacter.Subclass = subName;
            }

            RefreshClassDetailsPanel();
            UpdateSpellTabVisibility();
            PopulateSpells();

            UpdateEquipmentProficiencySummary();
            UpdateTotalEquipmentSummary();
            UpdateSubclassSpellsLabel();
        }

        private void Stat_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Prevent execution while window is still loading
            if (!this.IsLoaded || txtBaseAC == null || cmbClass == null)
                return;

            if (rbStandardArray?.IsChecked == true)
            {
                ValidateStandardArray();
            }
            else if (rbPointBuy?.IsChecked == true)
            {
                ValidatePointBuy();
            }
            UpdateStatDisplays();
        }

        /// <summary>Official 5e point-buy cost to set a score (8–15). Returns -1 if invalid.</summary>
        private static int GetPointBuyCost(int score) => score switch
        {
            8 => 0,
            9 => 1,
            10 => 2,
            11 => 3,
            12 => 4,
            13 => 5,
            14 => 7,
            15 => 9,
            _ => -1
        };

        /// <summary>Points needed to raise a score from <paramref name="from"/> to <paramref name="to"/> (to &gt; from).</summary>
        private static int GetPointBuyDeltaCost(int from, int to)
        {
            int a = GetPointBuyCost(from);
            int b = GetPointBuyCost(to);
            if (a < 0 || b < 0) return int.MaxValue;
            return b - a;
        }

        private int GetPointBuyPointsUsed()
        {
            int used = 0;
            foreach (var name in new[] { "Str", "Dex", "Con", "Int", "Wis", "Cha" })
            {
                var txt = this.FindName($"txt{name}Base") as TextBox;
                if (txt == null) continue;
                if (!int.TryParse(txt.Text, out int val))
                    val = 8;
                int cost = GetPointBuyCost(val);
                used += cost >= 0 ? cost : 99;
            }
            return used;
        }

        private int GetPointBuyPointsRemaining() => 27 - GetPointBuyPointsUsed();

        private void ValidatePointBuy()
        {
            if (lblPointBuy == null) return;

            bool hasInvalid = false;
            foreach (var name in new[] { "Str", "Dex", "Con", "Int", "Wis", "Cha" })
            {
                var txt = this.FindName($"txt{name}Base") as TextBox;
                if (txt == null) continue;
                if (!int.TryParse(txt.Text, out int val) || GetPointBuyCost(val) < 0)
                    hasInvalid = true;
            }

            int remaining = GetPointBuyPointsRemaining();

            lblPointBuy.Text = $"Points Remaining: {remaining}";
            lblPointBuy.Foreground = (remaining < 0 || hasInvalid) ? Brushes.Red :
                                     (remaining == 0 ? AccentGreen : Brushes.Yellow);

            UpdatePointBuyStepperButtons();
        }

        /// <summary>
        /// Enable/disable +/− steppers from point-buy rules (8–15, remaining budget).
        /// </summary>
        private void UpdatePointBuyStepperButtons()
        {
            bool isPointBuy = rbPointBuy?.IsChecked == true;
            int remaining = isPointBuy ? GetPointBuyPointsRemaining() : 0;

            foreach (var name in new[] { "Str", "Dex", "Con", "Int", "Wis", "Cha" })
            {
                var dec = this.FindName($"btn{name}BaseDec") as Button;
                var inc = this.FindName($"btn{name}BaseInc") as Button;
                var panel = this.FindName($"pnl{name}BaseStepper") as StackPanel;
                var txt = this.FindName($"txt{name}Base") as TextBox;

                if (panel != null)
                {
                    // Steppers are for Point Buy; hide for other methods so the layout stays clean
                    // (TextBox remains; buttons toggle visibility inside the panel)
                }

                if (dec != null) dec.Visibility = isPointBuy ? Visibility.Visible : Visibility.Collapsed;
                if (inc != null) inc.Visibility = isPointBuy ? Visibility.Visible : Visibility.Collapsed;

                if (!isPointBuy || txt == null)
                {
                    if (dec != null) dec.IsEnabled = false;
                    if (inc != null) inc.IsEnabled = false;
                    continue;
                }

                if (!int.TryParse(txt.Text, out int val))
                    val = 8;

                // − : only if above minimum 8
                if (dec != null)
                    dec.IsEnabled = val > 8;

                // + : only if under 15 and we can afford the next step
                if (inc != null)
                {
                    if (val >= 15)
                        inc.IsEnabled = false;
                    else
                    {
                        int stepCost = GetPointBuyDeltaCost(val, val + 1);
                        inc.IsEnabled = stepCost <= remaining;
                    }
                }
            }
        }

        private void PointBuyStat_Inc(object sender, RoutedEventArgs e)
        {
            AdjustPointBuyStat(sender, delta: +1);
        }

        private void PointBuyStat_Dec(object sender, RoutedEventArgs e)
        {
            AdjustPointBuyStat(sender, delta: -1);
        }

        private void AdjustPointBuyStat(object sender, int delta)
        {
            if (rbPointBuy?.IsChecked != true) return;
            if (sender is not Button btn || btn.Tag is not string statKey) return;

            var txt = this.FindName($"txt{statKey}Base") as TextBox;
            if (txt == null) return;

            if (!int.TryParse(txt.Text, out int val))
                val = 8;

            int next = val + delta;
            if (next < 8 || next > 15)
                return;

            if (delta > 0)
            {
                int remaining = GetPointBuyPointsRemaining();
                int stepCost = GetPointBuyDeltaCost(val, next);
                if (stepCost > remaining)
                    return;
            }

            txt.Text = next.ToString();
            // Stat_TextChanged → ValidatePointBuy → UpdatePointBuyStepperButtons
        }

        private void ValidateStandardArray()
        {
            if (lblPointBuy == null) return;

            var allowedValues = new HashSet<int> { 15, 14, 13, 12, 10, 8 };
            var usedValues = new List<int>();
            bool hasInvalid = false;

            string[] statNames = { "Str", "Dex", "Con", "Int", "Wis", "Cha" };

            foreach (var name in statNames)
            {
                var txt = this.FindName($"txt{name}Base") as TextBox;
                if (txt == null) continue;

                if (!int.TryParse(txt.Text, out int val))
                {
                    hasInvalid = true;
                    continue;
                }

                if (!allowedValues.Contains(val))
                {
                    hasInvalid = true;
                }

                usedValues.Add(val);
            }

            // Check for duplicates
            bool hasDuplicates = usedValues.Count != usedValues.Distinct().Count();

            if (hasInvalid || hasDuplicates)
            {
                lblPointBuy.Text = "Standard Array: Use each value exactly once (15, 14, 13, 12, 10, 8)";
                lblPointBuy.Foreground = Brushes.Red;
            }
            else
            {
                lblPointBuy.Text = "Standard Array ✓ (all values used once)";
                lblPointBuy.Foreground = AccentGreen;
            }
        }

        private void Stat_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox txt) return;

            if (!int.TryParse(txt.Text, out int val))
            {
                txt.Text = "8";
                return;
            }

            if (rbPointBuy?.IsChecked == true || rbStandardArray?.IsChecked == true)
            {
                if (val < 8) txt.Text = "8";
                else if (val > 15) txt.Text = "15";
            }
            else if (rbCustom?.IsChecked == true)
            {
                if (val < 1) txt.Text = "1";
                else if (val > 20) txt.Text = "20";
            }
        }

        private void StatMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (txtStrBase == null || btnRollStats == null) return;

            bool isRollMethod = rbRoll4d6?.IsChecked == true || rbRoll3d6?.IsChecked == true;

            // Show button only for rolling methods
            btnRollStats.Visibility = isRollMethod ? Visibility.Visible : Visibility.Collapsed;

            bool isCustom = rbCustom?.IsChecked == true;
            bool isPointBuy = rbPointBuy?.IsChecked == true;
            bool isStandardArray = rbStandardArray?.IsChecked == true;   // ← This was missing

            if (btnResetPointBuy != null)
                btnResetPointBuy.Visibility = isPointBuy ? Visibility.Visible : Visibility.Collapsed;

            string[] bases = { "txtStrBase", "txtDexBase", "txtConBase", "txtIntBase", "txtWisBase", "txtChaBase" };
            foreach (var name in bases)
            {
                var txt = this.FindName(name) as TextBox;
                if (txt != null)
                {
                    // Allow editing for Point Buy, Custom, AND Standard Array
                    txt.IsReadOnly = !(isCustom || isPointBuy || isStandardArray);
                }
            }

            if (isPointBuy)
                ValidatePointBuy();
            else
            {
                if (lblPointBuy != null && !isStandardArray)
                    lblPointBuy.Text = "";
                UpdatePointBuyStepperButtons();
            }

            if (isStandardArray)
                ValidateStandardArray();

            UpdateStatDisplays();
        }

        private void btnRollStats_Click(object sender, RoutedEventArgs e)
        {
            var rand = new Random();
            string[] statNames = { "Str", "Dex", "Con", "Int", "Wis", "Cha" };

            foreach (var name in statNames)
            {
                var txt = this.FindName($"txt{name}Base") as TextBox;
                var lblRoll = this.FindName($"lbl{name}Roll") as TextBlock;
                if (txt == null || lblRoll == null) continue;

                if (rbRoll4d6.IsChecked == true)
                {
                    var rolls = new List<int> { rand.Next(1, 7), rand.Next(1, 7), rand.Next(1, 7), rand.Next(1, 7) };
                    rolls.Sort();
                    int dropped = rolls[0];
                    int total = rolls.Skip(1).Sum();

                    txt.Text = total.ToString();
                    lblRoll.Text = $"({string.Join(",", rolls)}) dropped {dropped}";
                }
                else if (rbRoll3d6.IsChecked == true)
                {
                    var rolls = new List<int> { rand.Next(1, 7), rand.Next(1, 7), rand.Next(1, 7) };
                    int total = rolls.Sum();

                    txt.Text = total.ToString();
                    lblRoll.Text = $"({string.Join(",", rolls)})";
                }
            }

            UpdateStatDisplays();
            UpdateEquipmentProficiencySummary();
        }

        /// <summary>
        /// True when proficiency comes from race, background, import, or another non-class source.
        /// These stay checked and are not user-toggleable (take priority over class choice slots).
        /// </summary>
        private bool IsGrantedSkillProficiency(SkillProficiency skill)
        {
            if (skill == null) return false;
            if (skill.IsBackgroundProficiency) return true;
            if (currentRaceAutomaticSkills != null &&
                currentRaceAutomaticSkills.Contains(skill.SkillName, StringComparer.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrEmpty(raceGrantedSkill) &&
                skill.SkillName.Equals(raceGrantedSkill, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        public void UpdateSkillChoices(string className)
        {
            if (string.IsNullOrEmpty(className) ||
                !GameData.ClassData.ContainsKey(className) ||
                allSkills == null ||
                lblClassSkillCounter == null)
                return;

            var classData = GameData.ClassData[className];
            var allowedSkills = classData.SkillChoices ?? new List<string>();
            int maxAllowed = classData.SkillChoiceCount;

            // Pass 1: count class skill choices currently selected (exclude granted sources)
            int currentlySelected = 0;
            foreach (var skill in allSkills)
            {
                bool isClassSkill = allowedSkills.Contains(skill.SkillName);
                if (isClassSkill && skill.IsProficient && !IsGrantedSkillProficiency(skill))
                    currentlySelected++;
            }

            // Pass 2: enable only class skills the player may still toggle
            foreach (var skill in allSkills)
            {
                bool isClassSkill = allowedSkills.Contains(skill.SkillName);
                bool isGranted = IsGrantedSkillProficiency(skill);

                if (isGranted)
                {
                    // Race / background / import: locked on (or locked off if not proficient)
                    skill.IsSelectable = false;
                }
                else if (isClassSkill)
                {
                    // Can check if under the class max, or uncheck if already a class selection
                    skill.IsSelectable = currentlySelected < maxAllowed || skill.IsProficient;
                }
                else
                {
                    // Not on the class skill list — not a player choice
                    skill.IsSelectable = false;
                }
            }

            lblClassSkillCounter.Text = $"{currentlySelected} / {maxAllowed} class skills selected";

            if (currentlySelected > maxAllowed)
                lblClassSkillCounter.Foreground = Brushes.Red;
            else
                lblClassSkillCounter.Foreground = Brushes.White;
        }

        /// <summary>
        /// Single-click proficiency toggle for selectable skills.
        /// First click on a DataGrid row normally only selects the row; this handles the checkbox immediately.
        /// </summary>
        private void SkillProficiencyCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CheckBox cb)
                return;

            // Disabled = not a valid class choice (or is a granted proficiency)
            if (!cb.IsEnabled)
                return;

            if (cb.DataContext is not SkillProficiency skill)
                return;

            // Safety: never toggle race/background/import grants
            if (IsGrantedSkillProficiency(skill))
            {
                e.Handled = true;
                return;
            }

            bool willCheck = cb.IsChecked != true;

            if (willCheck)
            {
                // Enforce class skill choice maximum
                if (cmbClass.SelectedItem is string className &&
                    GameData.ClassData.TryGetValue(className, out var classData))
                {
                    var allowed = classData.SkillChoices ?? new List<string>();
                    if (!allowed.Contains(skill.SkillName))
                    {
                        e.Handled = true;
                        return;
                    }

                    int maxAllowed = classData.SkillChoiceCount;
                    int currentlySelected = allSkills.Count(s =>
                        allowed.Contains(s.SkillName) &&
                        s.IsProficient &&
                        !IsGrantedSkillProficiency(s));

                    if (currentlySelected >= maxAllowed)
                    {
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    // No class selected — don't allow freeform proficiency picks
                    e.Handled = true;
                    return;
                }
            }

            // Toggle immediately (prevents the default "select row first, click again to check" behavior)
            skill.IsProficient = willCheck;
            if (!willCheck)
                skill.IsExpertise = false;
            e.Handled = true;

            if (cmbClass.SelectedItem is string cn)
                UpdateSkillChoices(cn);

            UpdateSkillBonuses();
        }

        /// <summary>
        /// Clicking anywhere on the Proficient cell (not just the tiny checkbox hitbox) toggles proficiency.
        /// </summary>
        private void dgSkills_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            var dep = e.OriginalSource as DependencyObject;
            // If the checkbox already handled it, stop
            while (dep != null && dep is not CheckBox && dep is not DataGridCell)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is CheckBox)
                return; // checkbox handler owns this click

            while (dep != null && dep is not DataGridCell)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is not DataGridCell cell)
                return;

            if (cell.Column is not DataGridTemplateColumn templateCol)
                return;

            if (cell.DataContext is not SkillProficiency skill)
                return;

            string? header = templateCol.Header?.ToString();

            // Expertise column cell click
            if (header != null && header.Contains("Expertise", StringComparison.OrdinalIgnoreCase))
            {
                if (!skill.IsExpertiseSelectable || !skill.IsProficient)
                    return;

                bool willExpert = !skill.IsExpertise;
                if (willExpert)
                {
                    int maxSlots = LevelUpCalculator.GetExpertiseSkillSlots(GetActiveClassLevels());
                    int selected = allSkills.Count(s => s.IsExpertise && s.IsProficient);
                    if (selected >= maxSlots)
                        return;
                }

                skill.IsExpertise = willExpert;
                e.Handled = true;
                UpdateExpertiseSelectableState();
                UpdateSkillBonuses();
                return;
            }

            // Proficient column cell click
            if (header == null || !header.Contains("Proficient", StringComparison.OrdinalIgnoreCase))
                return;

            if (!skill.IsSelectable || IsGrantedSkillProficiency(skill))
                return;

            bool willCheck = !skill.IsProficient;

            if (willCheck)
            {
                if (cmbClass.SelectedItem is not string className ||
                    !GameData.ClassData.TryGetValue(className, out var classData))
                    return;

                var allowed = classData.SkillChoices ?? new List<string>();
                if (!allowed.Contains(skill.SkillName))
                    return;

                int currentlySelected = allSkills.Count(s =>
                    allowed.Contains(s.SkillName) &&
                    s.IsProficient &&
                    !IsGrantedSkillProficiency(s));

                if (currentlySelected >= classData.SkillChoiceCount)
                    return;
            }

            skill.IsProficient = willCheck;
            if (!willCheck)
                skill.IsExpertise = false;
            e.Handled = true;

            if (cmbClass.SelectedItem is string cn2)
                UpdateSkillChoices(cn2);

            UpdateSkillBonuses();
        }

        private void dgSkills_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSkillBonuses();

            if (cmbClass.SelectedItem is string className)
            {
                UpdateSkillChoices(className);
            }
        }

        private void ApplyRacialBonuses()
        {
            if (racialBonuses == null)
                racialBonuses = new Dictionary<string, int>();

            // Reset all racial bonuses first
            txtStrRacial.Text = "+0";
            txtDexRacial.Text = "+0";
            txtConRacial.Text = "+0";
            txtIntRacial.Text = "+0";
            txtWisRacial.Text = "+0";
            txtChaRacial.Text = "+0";

            foreach (var kv in racialBonuses)
            {
                string ability = kv.Key;
                int bonus = kv.Value;
                string sign = bonus >= 0 ? "+" : "";

                switch (ability)
                {
                    case "Strength": txtStrRacial.Text = $"{sign}{bonus}"; break;
                    case "Dexterity": txtDexRacial.Text = $"{sign}{bonus}"; break;
                    case "Constitution": txtConRacial.Text = $"{sign}{bonus}"; break;
                    case "Intelligence": txtIntRacial.Text = $"{sign}{bonus}"; break;
                    case "Wisdom": txtWisRacial.Text = $"{sign}{bonus}"; break;
                    case "Charisma": txtChaRacial.Text = $"{sign}{bonus}"; break;
                }
            }

            UpdateStatDisplays();
        }

        private void ApplyRaceProficiencies(string newRace)
        {
            if (allSkills == null || dgSkills == null) return;

            // Clear old race proficiencies
            foreach (var skillName in currentRaceAutomaticSkills)
            {
                var skill = allSkills.FirstOrDefault(s =>
                    s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));

                if (skill != null)
                {
                    bool protectedByBackground = false;
                    if (cmbBackground.SelectedItem is string bg)
                    {
                        var bgSkills = GetBackgroundSkillList(bg);
                        if (bgSkills.Contains(skill.SkillName))
                            protectedByBackground = true;
                    }

                    bool protectedByFlexible = skill.SkillName == raceGrantedSkill;

                    if (!protectedByBackground && !protectedByFlexible)
                    {
                        skill.IsProficient = false;
                        skill.IsBackgroundProficiency = false;
                    }
                }
            }

            // Apply new race proficiencies
            currentRaceAutomaticSkills.Clear();
            if (!string.IsNullOrEmpty(newRace) && GameData.RaceData.TryGetValue(newRace, out var raceData))
            {
                currentRaceAutomaticSkills = new List<string>(raceData.SkillProficiencies);
            }

            foreach (var skillName in currentRaceAutomaticSkills)
            {
                var skill = allSkills.FirstOrDefault(s =>
                    s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));

                if (skill != null)
                {
                    skill.IsProficient = true;
                    skill.IsBackgroundProficiency = true;
                }
            }

            dgSkills.Items.Refresh();
            UpdateSkillTabLabels();
        }

        public void UpdateSkillBonuses()
        {
            if (allSkills == null || dgSkills == null) return;

            RefreshProficiencyBonus();

            bool joat = LevelUpCalculator.HasJackOfAllTrades(GetActiveClassLevels());

            foreach (var skill in allSkills)
            {
                int mod = skill.Ability switch
                {
                    "Str" => GetModifierFromText(txtStrMod?.Text),
                    "Dex" => GetModifierFromText(txtDexMod?.Text),
                    "Con" => GetModifierFromText(txtConMod?.Text),
                    "Int" => GetModifierFromText(txtIntMod?.Text),
                    "Wis" => GetModifierFromText(txtWisMod?.Text),
                    "Cha" => GetModifierFromText(txtChaMod?.Text),
                    _ => 0
                };

                int totalBonus = LevelUpCalculator.ComputeSkillBonus(
                    mod, proficiencyBonus, skill.IsProficient, skill.IsExpertise, joat);
                skill.Bonus = totalBonus >= 0 ? $"+{totalBonus}" : totalBonus.ToString();
            }

            UpdateExpertiseSelectableState();
            UpdateJackOfAllTradesNote();
        }

        /// <summary>
        /// Expertise checkboxes: only on proficient skills, limited by Rogue/Bard Expertise slots.
        /// </summary>
        public void UpdateExpertiseSelectableState()
        {
            if (allSkills == null) return;

            int maxSlots = LevelUpCalculator.GetExpertiseSkillSlots(GetActiveClassLevels());
            int selected = allSkills.Count(s => s.IsExpertise && s.IsProficient);

            // Drop excess expertise if slots shrank (e.g. level down)
            if (selected > maxSlots)
            {
                foreach (var s in allSkills.Where(x => x.IsExpertise).Reverse().ToList())
                {
                    if (selected <= maxSlots) break;
                    s.SetExpertiseQuiet(false);
                    selected--;
                }
            }

            // Clear expertise without proficiency
            foreach (var s in allSkills.Where(x => x.IsExpertise && !x.IsProficient))
                s.SetExpertiseQuiet(false);

            selected = allSkills.Count(s => s.IsExpertise && s.IsProficient);

            foreach (var skill in allSkills)
            {
                if (maxSlots <= 0 || !skill.IsProficient)
                {
                    skill.IsExpertiseSelectable = false;
                    if (skill.IsExpertise)
                        skill.SetExpertiseQuiet(false);
                    continue;
                }

                // Can toggle on if under cap, or toggle off if already expert
                skill.IsExpertiseSelectable = selected < maxSlots || skill.IsExpertise;
            }

            if (lblExpertiseCounter != null)
            {
                if (maxSlots > 0)
                {
                    lblExpertiseCounter.Visibility = Visibility.Visible;
                    lblExpertiseCounter.Text = $"Expertise: {selected} / {maxSlots} (Rogue 1+/6+, Bard 3+/10+)";
                    lblExpertiseCounter.Foreground = selected > maxSlots ? Brushes.Red : Brushes.White;
                }
                else
                {
                    lblExpertiseCounter.Visibility = Visibility.Collapsed;
                    lblExpertiseCounter.Text = "";
                }
            }
        }

        private void UpdateJackOfAllTradesNote()
        {
            if (lblJackOfAllTradesNote == null) return;
            bool joat = LevelUpCalculator.HasJackOfAllTrades(GetActiveClassLevels());
            lblJackOfAllTradesNote.Visibility = joat ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SkillExpertiseCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CheckBox cb)
                return;
            if (!cb.IsEnabled)
                return;
            if (cb.DataContext is not SkillProficiency skill)
                return;
            if (!skill.IsProficient)
            {
                e.Handled = true;
                return;
            }

            int maxSlots = LevelUpCalculator.GetExpertiseSkillSlots(GetActiveClassLevels());
            bool willCheck = cb.IsChecked != true;

            if (willCheck)
            {
                int selected = allSkills.Count(s => s.IsExpertise && s.IsProficient);
                if (selected >= maxSlots)
                {
                    e.Handled = true;
                    return;
                }
            }

            skill.IsExpertise = willCheck;
            e.Handled = true;
            UpdateExpertiseSelectableState();
            UpdateSkillBonuses();
        }

        /// <summary>True if the character has save proficiency from class and/or Resilient (etc.).</summary>
        private bool HasSaveProficiency(string abilityName, IEnumerable<string> classProficientSaves)
        {
            if (string.IsNullOrWhiteSpace(abilityName)) return false;
            if (classProficientSaves != null &&
                classProficientSaves.Contains(abilityName, StringComparer.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrEmpty(resilientSaveAbility) &&
                resilientSaveAbility.Equals(abilityName, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private void UpdateSavingThrows()
        {
            if (txtStrSave == null) return;

            RefreshProficiencyBonus();

            List<string> profSaves = new();
            if (cmbClass.SelectedItem is string className &&
                GameData.ClassData.TryGetValue(className, out var data))
            {
                profSaves = data.SavingThrowProficiencies ?? new List<string>();
            }

            int strMod = GetModifierFromText(txtStrMod?.Text);
            int dexMod = GetModifierFromText(txtDexMod?.Text);
            int conMod = GetModifierFromText(txtConMod?.Text);
            int intMod = GetModifierFromText(txtIntMod?.Text);
            int wisMod = GetModifierFromText(txtWisMod?.Text);
            int chaMod = GetModifierFromText(txtChaMod?.Text);

            txtStrSave.Text = FormatSave(strMod, HasSaveProficiency("Strength", profSaves), "Strength");
            txtDexSave.Text = FormatSave(dexMod, HasSaveProficiency("Dexterity", profSaves), "Dexterity");
            txtConSave.Text = FormatSave(conMod, HasSaveProficiency("Constitution", profSaves), "Constitution");
            txtIntSave.Text = FormatSave(intMod, HasSaveProficiency("Intelligence", profSaves), "Intelligence");
            txtWisSave.Text = FormatSave(wisMod, HasSaveProficiency("Wisdom", profSaves), "Wisdom");
            txtChaSave.Text = FormatSave(chaMod, HasSaveProficiency("Charisma", profSaves), "Charisma");

            UpdateInitiative();
        }

        private string FormatSave(int mod, bool proficient, string abilityName = "")
        {
            int total = mod + (proficient ? proficiencyBonus : 0);
            string text = total >= 0 ? $"+{total}" : total.ToString();
            if (!proficient)
                return text;

            // Note source when proficiency is only from Resilient (not the class)
            bool fromResilient = !string.IsNullOrEmpty(resilientSaveAbility) &&
                resilientSaveAbility.Equals(abilityName, StringComparison.OrdinalIgnoreCase);
            bool fromClass = cmbClass.SelectedItem is string cn &&
                GameData.ClassData.TryGetValue(cn, out var cd) &&
                (cd.SavingThrowProficiencies?.Contains(abilityName) ?? false);

            if (fromResilient && !fromClass)
                return $"{text}  (Proficient — Resilient)";
            return $"{text}  (Proficient)";
        }

        // Helper method to parse modifier text
        private int GetModifierFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            // int.Parse handles "+3" and "-1" perfectly — no manual sign stripping needed
            string cleanText = text.Replace("+", "").Trim();
            return int.TryParse(cleanText, out int mod) ? mod : 0;
        }

        /// <summary>
        /// Base proficiency name without display suffixes like "(multiclass Ranger)" or "(Twilight Domain)".
        /// </summary>
        private static string ProficiencyBaseName(string item)
        {
            if (string.IsNullOrWhiteSpace(item)) return "";
            string clean = item.Trim();
            int paren = clean.IndexOf(" (", StringComparison.Ordinal);
            if (paren > 0) clean = clean.Substring(0, paren).Trim();
            return clean;
        }

        /// <summary>
        /// True if the list already includes this proficiency (by base name, ignoring source suffixes).
        /// Also treats "Shields (non-metal)" as covered when "Shields" is already present.
        /// </summary>
        private static bool HasProficiency(List<string> list, string item)
        {
            string baseName = ProficiencyBaseName(item);
            if (string.IsNullOrEmpty(baseName)) return true;

            if (list.Any(x => ProficiencyBaseName(x).Equals(baseName, StringComparison.OrdinalIgnoreCase)))
                return true;

            // Full shield proficiency already covers non-metal-only shields
            if (baseName.Equals("Shields (non-metal)", StringComparison.OrdinalIgnoreCase) &&
                list.Any(x => ProficiencyBaseName(x).Equals("Shields", StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        /// <summary>
        /// Collect armor/weapon proficiency lines using official 5e multiclass rules:
        /// <list type="bullet">
        /// <item><description>Primary class (first ClassLevels row / character start): full starting armor &amp; weapons.</description></item>
        /// <item><description>Each additional class: only the PHB Multiclassing Proficiencies table (not full 1st-level list).</description></item>
        /// <item><description>Subclass features (e.g. Twilight Domain) still grant their bonus proficiencies at the usual class level.</description></item>
        /// <item><description>Already-owned proficiencies are not re-listed (e.g. Rogue + Ranger does not repeat Simple weapons).</description></item>
        /// </list>
        /// </summary>
        private void CollectClassAndSubclassProficiencies(
            List<string> armor,
            List<string> weapons,
            List<string>? subclassGrantNotes = null)
        {
            // Add by base name only — no redundant "(multiclass X)" / source re-listings
            bool TryAdd(List<string> list, string item)
            {
                if (string.IsNullOrWhiteSpace(item)) return false;
                string baseName = ProficiencyBaseName(item);
                if (string.IsNullOrEmpty(baseName) || HasProficiency(list, baseName))
                    return false;
                list.Add(baseName);
                return true;
            }

            var entries = CurrentCharacter?.ClassLevels?
                .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                .ToList();

            // Fall back to Class tab when ClassLevels not seeded yet
            if (entries == null || entries.Count == 0)
            {
                string? cn = cmbClass?.SelectedItem as string;
                string? sub = GetUiSelectedSubclassName();
                if (!string.IsNullOrWhiteSpace(cn))
                    entries = new List<ClassLevelEntry> { new(cn!, 1, sub) };
            }

            if (entries == null) return;

            // First row = starting class (full proficiencies). Later rows = multiclass dips (table only).
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string className = entry.ClassName.Trim();
                bool isPrimaryClass = i == 0;

                if (isPrimaryClass)
                {
                    if (GameData.ClassData.TryGetValue(className, out var classData))
                    {
                        foreach (var a in classData.ArmorProficiencies ?? new List<string>())
                            TryAdd(armor, a);
                        foreach (var w in classData.WeaponProficiencies ?? new List<string>())
                            TryAdd(weapons, w);
                    }
                }
                else
                {
                    // PHB: secondary classes only get the Multiclassing Proficiencies table
                    var mc = LevelUpCalculator.GetMulticlassProficiencies(className);
                    foreach (var a in mc.Armor)
                        TryAdd(armor, a);
                    foreach (var w in mc.Weapons)
                        TryAdd(weapons, w);
                }

                // Subclass bonus proficiencies only once the class has unlocked its subclass
                string? subName = GameData.GetEffectiveSubclass(entry);
                if (string.IsNullOrWhiteSpace(subName))
                    continue;

                GetSubclassBonusProficiencies(className, subName,
                    out var subArmor, out var subWeapons, out string sourceLabel);

                var newArmor = new List<string>();
                var newWeapons = new List<string>();
                foreach (var a in subArmor)
                {
                    if (TryAdd(armor, a))
                        newArmor.Add(ProficiencyBaseName(a));
                }
                foreach (var w in subWeapons)
                {
                    if (TryAdd(weapons, w))
                        newWeapons.Add(ProficiencyBaseName(w));
                }

                // Only note grants that actually added something new
                if (subclassGrantNotes != null && (newArmor.Count > 0 || newWeapons.Count > 0))
                {
                    var bits = new List<string>();
                    if (newArmor.Count > 0) bits.Add("armor: " + string.Join(", ", newArmor));
                    if (newWeapons.Count > 0) bits.Add("weapons: " + string.Join(", ", newWeapons));
                    subclassGrantNotes.Add($"{className} — {sourceLabel}: {string.Join("; ", bits)}");
                }
            }
        }

        /// <summary>
        /// Armor/weapon grants from a subclass (domain, patron, college, etc.).
        /// </summary>
        private static void GetSubclassBonusProficiencies(
            string className,
            string subclassName,
            out List<string> armor,
            out List<string> weapons,
            out string sourceLabel)
        {
            armor = new List<string>();
            weapons = new List<string>();
            sourceLabel = subclassName;

            // Cleric domains (keys: Life, Twilight, War, …)
            if (className.Equals("Cleric", StringComparison.OrdinalIgnoreCase) &&
                GameData.ClericSubclasses.TryGetValue(subclassName, out var clericSub))
            {
                armor.AddRange(clericSub.ArmorProficiencies ?? new List<string>());
                weapons.AddRange(clericSub.WeaponProficiencies ?? new List<string>());
                sourceLabel = string.IsNullOrWhiteSpace(clericSub.Name) ? subclassName : clericSub.Name;
                return;
            }

            // Warlock patrons
            if (className.Equals("Warlock", StringComparison.OrdinalIgnoreCase) &&
                GameData.WarlockSubclasses.TryGetValue(subclassName, out var warlockSub))
            {
                armor.AddRange(warlockSub.ArmorProficiencies ?? new List<string>());
                weapons.AddRange(warlockSub.WeaponProficiencies ?? new List<string>());
                sourceLabel = string.IsNullOrWhiteSpace(warlockSub.Name) ? subclassName : warlockSub.Name;
                return;
            }

            // Bard colleges that grant medium armor / martial weapons (Valor, Swords)
            if (className.Equals("Bard", StringComparison.OrdinalIgnoreCase))
            {
                if (subclassName.Contains("Valor", StringComparison.OrdinalIgnoreCase) ||
                    subclassName.Contains("Swords", StringComparison.OrdinalIgnoreCase))
                {
                    armor.Add("Medium armor");
                    armor.Add("Shields");
                    weapons.Add("Martial weapons");
                    sourceLabel = subclassName;
                }
                return;
            }

            // Cleric-like heuristics if domain key missing but name contains domain
            // (already handled by ClericSubclasses for official domains)

            // Hexblade-style already in WarlockSubclasses
        }

        private void UpdateEquipmentProficiencySummary()
        {
            if (pnlProficiencySummary == null) return;
            pnlProficiencySummary.Children.Clear();

            var armor = new List<string>();
            var weapons = new List<string>();
            var subclassNotes = new List<string>();

            // === ALL CLASSES + SUBCLASSES (multiclass-aware) ===
            CollectClassAndSubclassProficiencies(armor, weapons, subclassNotes);

            // === RACE / SUBRACE PROFICIENCIES ===
            string race = cmbRace.SelectedItem as string;
            string subrace = cmbSubrace.SelectedItem as string;

            if (race != null)
            {
                if (race.Contains("Dwarf") || (subrace != null && subrace.Contains("Dwarf")))
                {
                    weapons.Add("Dwarven Combat Training (battleaxe, handaxe, light hammer, warhammer)");
                    if (race == "Mountain Dwarf" || subrace == "Mountain Dwarf")
                        armor.Add("Dwarven Armor Training (light & medium armor)");
                }

                if (race.Contains("Elf") || (subrace != null && subrace.Contains("Elf")))
                {
                    weapons.Add("Elf Weapon Training (longsword, shortsword, shortbow, longbow)");
                }
            }

            // === DISPLAY ARMOR ===
            if (armor.Count > 0)
            {
                pnlProficiencySummary.Children.Add(new TextBlock
                {
                    Text = "ARMOR PROFICIENCIES",
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                pnlProficiencySummary.Children.Add(new TextBlock
                {
                    Text = "• " + string.Join(", ", armor.Distinct(StringComparer.OrdinalIgnoreCase)),
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
            }

            // === DISPLAY WEAPONS ===
            if (weapons.Count > 0)
            {
                pnlProficiencySummary.Children.Add(new TextBlock
                {
                    Text = "WEAPON PROFICIENCIES",
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                pnlProficiencySummary.Children.Add(new TextBlock
                {
                    Text = "• " + string.Join(", ", weapons.Distinct(StringComparer.OrdinalIgnoreCase)),
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            // Subclass grants that added new armor/weapons (already merged into lists above)
            if (subclassNotes.Count > 0)
            {
                pnlProficiencySummary.Children.Add(new TextBlock
                {
                    Text = "SUBCLASS PROFICIENCY GRANTS",
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#9CDCFE"),
                    Margin = new Thickness(0, 4, 0, 4)
                });
                foreach (var note in subclassNotes)
                {
                    pnlProficiencySummary.Children.Add(new TextBlock
                    {
                        Text = "• " + note,
                        Foreground = (Brush)new BrushConverter().ConvertFromString("#CCC"),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 2)
                    });
                }
            }
        }

        /// <summary>
        /// Clears feat detail combo boxes so choices from one feat never leak into another.
        /// Must run before configuring UI for the newly selected feat.
        /// </summary>
        private void ResetFeatChoiceUi()
        {
            // Detach Magic Initiate class handler so changing cmb1 later cannot re-show combos 2–4
            if (cmbFeatStatChoice1 != null)
                cmbFeatStatChoice1.SelectionChanged -= MagicInitiateClass_Changed;

            // Collapse first so FeatStatChoice_Changed (if fired while clearing) is a no-op
            pnlFeatStatChoices.Visibility = Visibility.Collapsed;
            lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE(S) TO INCREASE";

            void ClearCombo(ComboBox cmb, bool startVisible = false)
            {
                if (cmb == null) return;
                cmb.ItemsSource = null;
                cmb.SelectedIndex = -1;
                cmb.Visibility = startVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            // cmb1 is the default primary picker when a feat needs choices; keep layout ready but panel hidden
            ClearCombo(cmbFeatStatChoice1, startVisible: true);
            ClearCombo(cmbFeatStatChoice2);
            ClearCombo(cmbFeatStatChoice3);
            ClearCombo(cmbFeatStatChoice4);

            brdFeatSpellPreview.Visibility = Visibility.Collapsed;
            if (txtFeatSpellDetails != null)
                txtFeatSpellDetails.Text = "";
            featSelectedSpell = "";
        }

        private void DgFeats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFeats.SelectedItem is not Feat selectedFeat) return;

            baseFeatDescription = $"**Prerequisites:** {selectedFeat.Prerequisites}\n\n{selectedFeat.FullDescription}";
            txtFeatDetails.Text = baseFeatDescription;

            string name = selectedFeat.Name.ToLowerInvariant();

            // === FULL RESET (every feat switch) ===
            ResetFeatChoiceUi();

            // === 1. RESILIENT → ability score (+1) and save proficiency (cmb1) ===
            if (name == "resilient")
            {
                var allAbilities = new List<string> { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = allAbilities;
                cmbFeatStatChoice1.SelectedIndex = 0;
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY (+1 SCORE & SAVE PROFICIENCY)";
                pnlFeatStatChoices.Visibility = Visibility.Visible;
                // Ensure default Strength pick applies +1 and save proficiency even if SelectionChanged is skipped
                FeatStatChoice_Changed(cmbFeatStatChoice1, null);
            }
            // === 2. SPELL SNIPER → Only cantrip with attack roll (cmb2) ===
            else if (name == "spell sniper")
            {
                var attackCantrips = GameData.AllCantrips
                    .Where(c => !string.IsNullOrWhiteSpace(c.RollType) &&
                                c.RollType.Contains("Spell Attack", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Name)
                    .OrderBy(n => n)
                    .ToList();

                cmbFeatStatChoice1.Visibility = Visibility.Collapsed;
                cmbFeatStatChoice2.ItemsSource = attackCantrips;
                cmbFeatStatChoice2.Visibility = Visibility.Visible;
                if (attackCantrips.Any()) cmbFeatStatChoice2.SelectedIndex = 0;

                lblFeatStatChoiceHeader.Text = "CHOOSE A CANTRIP THAT REQUIRES AN ATTACK ROLL";
                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 3. FEY TOUCHED / SHADOW TOUCHED → Mental stats (cmb1) + Spell (cmb2) ===
            else if (name == "fey touched" || name == "shadow touched")
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE AND SPELL";
                // Ability score dropdown
                var mentalStats = new List<string> { "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = mentalStats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;

                // Spell dropdown
                List<string> allowedSchools = name == "fey touched"
                    ? new List<string> { "Enchantment", "Illusion" }
                    : new List<string> { "Illusion", "Necromancy" };

                var filteredSpells = GameData.All1stLevelSpells
                    .Where(s => allowedSchools.Contains(s.School, StringComparer.OrdinalIgnoreCase))
                    .Select(s => s.Name)
                    .OrderBy(n => n)
                    .ToList();

                cmbFeatStatChoice2.ItemsSource = filteredSpells;
                cmbFeatStatChoice2.Visibility = Visibility.Visible;
                if (filteredSpells.Any()) cmbFeatStatChoice2.SelectedIndex = 0;

                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 4. ARTIFICER INITIATE ===
            else if (name == "artificer initiate")
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ARTIFICER CANTRIP AND 1ST-LEVEL SPELL";

                // Cantrip dropdown
                var artificerCantrips = GameData.ClassCantrips.ContainsKey("Artificer")
                    ? GameData.ClassCantrips["Artificer"]
                    : new List<string>();
                cmbFeatStatChoice1.ItemsSource = artificerCantrips;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;

                // 1st-level spell dropdown
                var artificerSpells = GameData.All1stLevelSpells
                    .Where(s => s.Classes.Contains("Artificer", StringComparer.OrdinalIgnoreCase))
                    .Select(s => s.Name)
                    .OrderBy(n => n)
                    .ToList();

                cmbFeatStatChoice2.ItemsSource = artificerSpells;
                cmbFeatStatChoice2.Visibility = Visibility.Visible;
                if (artificerSpells.Any()) cmbFeatStatChoice2.SelectedIndex = 0;

                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === GIFT OF THE DRAGON FEATS (mental +1 and fixed spells) ===
            // Check specific dragon gifts before the generic mental-stat branch.
            else if (name == "gift of the chromatic dragon" ||
                     name == "gift of the gem dragon" ||
                     name == "gift of the metallic dragon")
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE TO INCREASE";
                var mentalStats = new List<string> { "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = mentalStats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;
                pnlFeatStatChoices.Visibility = Visibility.Visible;

                if (name == "gift of the chromatic dragon")
                {
                    currentFeatSpellSource = "Gift of the Chromatic Dragon";
                    currentFeatSpells = new List<string> { "Chromatic Orb" };
                }
                else if (name == "gift of the gem dragon")
                {
                    currentFeatSpellSource = "Gift of the Gem Dragon";
                    currentFeatSpells = new List<string> { "Detect Thoughts" };
                }
                else
                {
                    currentFeatSpellSource = "Gift of the Metallic Dragon";
                    currentFeatSpells = new List<string> { "Cure Wounds", "Detect Magic" };
                }
                UpdateFeatSpellsLabel();
            }
            // === MENTAL STATS ONLY (Int / Wis / Cha) ===
            // Used by: Telekinetic, Telepathic
            else if (name == "telekinetic" || name == "telepathic")
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE TO INCREASE";

                var mentalStats = new List<string> { "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = mentalStats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;

                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === MAGIC INITIATE ===
            else if (name == "magic initiate")
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE CLASS + 2 CANTRIPS + 1ST LEVEL SPELL";

                // Combo 1: Class selection
                var spellcastingClasses = new List<string>
                {
                    "Bard", "Cleric", "Druid", "Sorcerer", "Warlock", "Wizard"
                };
                cmbFeatStatChoice1.ItemsSource = spellcastingClasses;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;

                // Initially populate the other combos based on first class
                string initialClass = spellcastingClasses[0];
                PopulateMagicInitiateChoices(initialClass);

                // Wire up class change handler (ResetFeatChoiceUi always detaches first)
                cmbFeatStatChoice1.SelectionChanged -= MagicInitiateClass_Changed;
                cmbFeatStatChoice1.SelectionChanged += MagicInitiateClass_Changed;

                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === FIGHTING INITIATE → fighter fighting style (cmb1) ===
            else if (name == "fighting initiate")
            {
                SetupFightingInitiateChoices();
            }
            // === 5. Physical feats (Slasher, Piercer, Dual Wielder, Weapon Master) → cmb1 only ===
            // Exact names: "weapon master" must not match "Great Weapon Master".
            else if (name is "slasher" or "piercer" or "dual wielder" or "weapon master")
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE TO INCREASE";
                var physicalStats = new List<string> { "Strength", "Dexterity" };
                cmbFeatStatChoice1.ItemsSource = physicalStats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;
                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 6. Athlete → cmb1 only ===
            else if (name == "athlete")
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE TO INCREASE";
                var stats = new List<string> { "Strength", "Dexterity", "Constitution" };
                cmbFeatStatChoice1.ItemsSource = stats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;
                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 7. Skill Expert / Prodigy → cmb1 + cmb2 + cmb3 ===
            else if (name is "skill expert" or "prodigy")
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE AND SKILL PROFICIENCY";
                var allAbilities = new List<string> { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = allAbilities;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;

                cmbFeatStatChoice2.ItemsSource = allSkills.Select(s => s.SkillName).ToList();
                cmbFeatStatChoice2.SelectedIndex = 0;
                cmbFeatStatChoice2.Visibility = Visibility.Visible;

                var proficientSkills = allSkills.Where(s => s.IsProficient).Select(s => s.SkillName).ToList();
                cmbFeatStatChoice3.ItemsSource = proficientSkills.Any() ? proficientSkills : allSkills.Select(s => s.SkillName).ToList();
                cmbFeatStatChoice3.SelectedIndex = 0;
                cmbFeatStatChoice3.Visibility = Visibility.Visible;

                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            else
            {
                // Feats with no choices (e.g. Great Weapon Master, Alert, …)
                pnlFeatStatChoices.Visibility = Visibility.Collapsed;
            }
        }

        public void UpdateStatDisplays()
        {
            if (txtStrBase == null || txtDexBase == null) return; // still initializing

            UpdateSingleStat(txtStrBase, txtStrRacial, txtStrFinal, txtStrMod);
            UpdateSingleStat(txtDexBase, txtDexRacial, txtDexFinal, txtDexMod);
            UpdateSingleStat(txtConBase, txtConRacial, txtConFinal, txtConMod);
            UpdateSingleStat(txtIntBase, txtIntRacial, txtIntFinal, txtIntMod);
            UpdateSingleStat(txtWisBase, txtWisRacial, txtWisFinal, txtWisMod);
            UpdateSingleStat(txtChaBase, txtChaRacial, txtChaFinal, txtChaMod);

            UpdateSkillBonuses();
            UpdateSavingThrows();
            UpdateCarryingCapacity();
            UpdateBaseAC();
            UpdateEquippedAC();
            UpdateHitPoints();
            if (tabSpells != null && tabSpells.Visibility == Visibility.Visible)
            {
                UpdateSpellStats();
            }
        }

        private void UpdateSingleStat(TextBox baseBox, TextBlock racialBox, TextBlock finalBox, TextBlock modBox)
        {
            if (baseBox == null || racialBox == null || finalBox == null || modBox == null) return;

            int baseVal = int.TryParse(baseBox.Text, out int b) ? b : 8;

            int racialVal = 0;
            if (int.TryParse(racialBox.Text.Replace("+", "").Replace("-", ""), out int r))
            {
                if (racialBox.Text.StartsWith("-")) r = -r;
                racialVal = r;
            }

            // NEW: Feat ability score bonuses
            int featVal = 0;
            string abilityName = baseBox.Name switch
            {
                string n when n.Contains("Str") => "Strength",
                string n when n.Contains("Dex") => "Dexterity",
                string n when n.Contains("Con") => "Constitution",
                string n when n.Contains("Int") => "Intelligence",
                string n when n.Contains("Wis") => "Wisdom",
                string n when n.Contains("Cha") => "Charisma",
                _ => ""
            };
            featVal = featStatBonuses.TryGetValue(abilityName, out int f) ? f : 0;
            int asiVal = GetAsiBonusForAbility(abilityName);

            int finalVal = baseVal + racialVal + featVal + asiVal;

            int mod = CalculateModifier(finalVal);

            string modSign = mod >= 0 ? "+" : "";

            finalBox.Text = finalVal.ToString();
            modBox.Text = $"{modSign}{mod}";
        }

        private void btnResetPointBuy_Click(object sender, RoutedEventArgs e)
        {
            string[] statNames = { "Str", "Dex", "Con", "Int", "Wis", "Cha" };

            foreach (var name in statNames)
            {
                var txt = this.FindName($"txt{name}Base") as TextBox;
                if (txt != null) txt.Text = "8";

                var lblRoll = this.FindName($"lbl{name}Roll") as TextBlock;
                if (lblRoll != null) lblRoll.Text = "";
            }

            if (rbPointBuy?.IsChecked == true)
                ValidatePointBuy();
            else
                UpdatePointBuyStepperButtons();
            UpdateStatDisplays();
        }

        private void UpdateSkillTabLabels()
        {
            // === SAFETY: Exit early if Class or Background not selected yet ===
            if (cmbClass?.SelectedItem is not string className ||
                cmbBackground?.SelectedItem is not string bg)
                return;

            if (!GameData.ClassData.ContainsKey(className)) return;

            var classData = GameData.ClassData[className];

            // === CLASS SKILL CHOICES (Detail only) ===
            lblClassSkillChoicesDetail.Text = $"({classData.SkillChoiceCount} skills from {string.Join(", ", classData.SkillChoices)})";

            // === RACE PROFICIENCIES (Detail only) ===
            string raceProfs = "";
            if (cmbRace.SelectedItem is string currentRace &&
                GameData.RaceData.TryGetValue(currentRace, out var raceData))
            {
                raceProfs = string.Join(", ", raceData.SkillProficiencies);
            }

            string displayRaceText = !string.IsNullOrEmpty(raceGrantedSkill)
                ? raceGrantedSkill + (string.IsNullOrEmpty(raceProfs) ? "" : ", " + raceProfs)
                : raceProfs;

            lblRaceProficienciesDetail.Text = displayRaceText;
            lblRaceProficienciesDetail.Visibility = string.IsNullOrEmpty(displayRaceText)
                ? Visibility.Collapsed
                : Visibility.Visible;

            // === BACKGROUND PROFICIENCIES (Detail only) ===
            var backgroundSkills = GetBackgroundSkillList(bg);
            lblBackgroundProficienciesDetail.Text = backgroundSkills.Count > 0
                ? string.Join(", ", backgroundSkills)
                : "";
        }

        public void UpdateBaseAC()
        {
            // Safety guard — prevents crash during XAML initialization
            if (txtDexMod == null || txtConMod == null || txtWisMod == null ||
                cmbClass == null || txtBaseAC == null)
                return;

            int dexMod = GetModifierFromText(txtDexMod.Text);
            int conMod = GetModifierFromText(txtConMod.Text);
            int wisMod = GetModifierFromText(txtWisMod.Text);

            int baseAC = 10 + dexMod;

            string className = cmbClass.SelectedItem as string;

            if (className == "Barbarian")
                baseAC += conMod;
            else if (className == "Monk")
                baseAC += wisMod;

            txtBaseAC.Text = baseAC.ToString();
        }

        public void UpdateHitPoints()
        {
            if (txtConMod == null || txtHitPoints == null || cmbClass == null)
                return;

            int conMod = GetModifierFromText(txtConMod.Text);
            string className = cmbClass.SelectedItem as string ?? "";
            string subclass = cmbSubclass?.SelectedItem as string ?? "";

            int level = CurrentCharacter?.Level > 0 ? CurrentCharacter.Level : 1;
            var classLevels = CurrentCharacter?.ClassLevels != null && CurrentCharacter.ClassLevels.Count > 0
                ? CurrentCharacter.ClassLevels
                : new List<ClassLevelEntry> { new(className, Math.Max(1, level), subclass) };

            int extraHpPerLevel = GetExtraHpPerLevelForCalc();

            var method = CurrentCharacter?.HpGainMethod ?? HpGainMethod.FixedAverage;
            // 0 entries mean “not rolled yet” → calculator uses fixed average for that level
            var rolls = method == HpGainMethod.Rolled ? CurrentCharacter?.HitPointRolls : null;

            var snap = LevelUpCalculator.Calculate(
                classLevels,
                conMod,
                method,
                rolls,
                extraHpPerLevel);

            txtHitPoints.Text = snap.HitPointMaximum.ToString();

            // Keep derived character fields in sync when available
            if (CurrentCharacter != null)
            {
                CurrentCharacter.HitPoints = snap.HitPointMaximum;
                if (CurrentCharacter.Level <= 0)
                    CurrentCharacter.Level = snap.TotalCharacterLevel;
                // Prefer level-derived PB (same formula as snap) so skills/saves stay consistent
                RefreshProficiencyBonus();
            }
        }

        private void GenerateEquipmentChoices(string className)
        {
            pickedWeapons.Clear();
            pnlEquipmentChoices.Children.Clear();
            activeWeaponChoices.Clear();
            pnlWeaponChoices.Visibility = Visibility.Visible;   // Always keep middle column visible

            // Equipment kit is for the starting class only (first class at creation).
            // Prefer ClassLevels[0] over the Class-tab combo when multiclassing.
            string startingClass = GetStartingClassName();
            if (!string.IsNullOrWhiteSpace(startingClass))
                className = startingClass;

            RefreshStartingWealthUi();

            if (string.IsNullOrWhiteSpace(className) || !GameData.StartingEquipment.ContainsKey(className))
            {
                UpdateTotalEquipmentSummary();
                UpdateEquipmentProficiencySummary();
                return;
            }

            var choices = GameData.StartingEquipment[className];

            foreach (var choice in choices)
            {
                var groupLabel = new TextBlock
                {
                    Text = choice.Label.ToUpper(),
                    FontWeight = FontWeights.Bold,
                    Foreground = AccentGreen,
                    Margin = new Thickness(0, 12, 0, 4),
                    FontSize = 13
                };
                pnlEquipmentChoices.Children.Add(groupLabel);

                var groupPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 8) };

                foreach (var option in choice.Options)
                {
                    if (option.IsAnyWeapon)
                    {
                        var rb = new RadioButton
                        {
                            GroupName = choice.Label,
                            Content = new TextBlock
                            {
                                Text = option.Text,
                                Foreground = Brushes.White
                            },
                            Margin = new Thickness(0, 2, 0, 2),
                            Tag = option
                        };

                        // === CHECKED: Add to active choices and refresh middle column ===
                        rb.Checked += (s, e) =>
{
    int count = option.Text.Contains("2") ? 2 : 1;
    string type = option.WeaponType;

    activeWeaponChoices[choice.Label] = (count, type);
    RefreshWeaponComboBoxes();

    // === Clear any weapons previously selected via the combo boxes ===
    // This fixes the issue where switching back to a specific weapon didn't remove generic ones (e.g. Sickle)
    pickedWeapons.RemoveAll(x => x.StartsWith("cmbWeaponChoice"));

    // Special case: Martial weapon and shield
    if (option.Text.Contains("Martial weapon and shield", StringComparison.OrdinalIgnoreCase))
    {
        pickedWeapons.RemoveAll(x => x.Contains("Shield", StringComparison.OrdinalIgnoreCase));
        pickedWeapons.Add("Shield");
    }

    UpdateTotalEquipmentSummary();
};

                        // === UNCHECKED: Remove this choice and clean up ===
                        rb.Unchecked += (s, e) =>
{
    if (activeWeaponChoices.ContainsKey(choice.Label))
        activeWeaponChoices.Remove(choice.Label);

    // Remove any weapons that came from this radio button group
    pickedWeapons.RemoveAll(x => x.StartsWith(choice.Label + "_"));

    // Also clear any combo box selections tied to this group
    pickedWeapons.RemoveAll(x => x.StartsWith("cmbWeaponChoice"));

    // === Automatic Shield removal ===
    if (option.Text.Contains("Martial weapon and shield", StringComparison.OrdinalIgnoreCase))
    {
        pickedWeapons.RemoveAll(x => x.Contains("Shield", StringComparison.OrdinalIgnoreCase));
    }

    RefreshWeaponComboBoxes();
    UpdateTotalEquipmentSummary();
};

                        groupPanel.Children.Add(rb);
                    }
                    else
                    {
                        // Normal choices and automatic equipment
                        if (choice.Label == "Automatic Equipment")
                        {
                            var tb = new TextBlock
                            {
                                Text = "• " + option.Text,
                                Foreground = Brushes.White,
                                Margin = new Thickness(0, 2, 0, 2),
                                FontSize = 13
                            };
                            groupPanel.Children.Add(tb);
                        }
                        else
                        {
                            var rb = new RadioButton
                            {
                                GroupName = choice.Label,
                                Content = new TextBlock
                                {
                                    Text = option.Text,
                                    Foreground = Brushes.White
                                },
                                Margin = new Thickness(0, 2, 0, 2),
                                Tag = option
                            };
                            rb.Checked += (s, e) => UpdateTotalEquipmentSummary();
                            groupPanel.Children.Add(rb);
                        }
                    }
                }

                pnlEquipmentChoices.Children.Add(groupPanel);
            }

            UpdateTotalEquipmentSummary();
            UpdateEquipmentProficiencySummary();
            ApplyEquipmentChoicesEnabledState();
        }

        /// <summary>
        /// Level 1: equipment package OR roll gold for the <b>starting class</b> only.
        /// Levels 5+: one DMG wealth roll by <b>total character level</b> (multiclass does not stack extra gold).
        /// Levels 2–4: equipment only (no exclusive gold choice / no DMG band).
        /// </summary>
        private void RefreshStartingWealthUi()
        {
            if (pnlLevel1WealthMode == null) return;

            int level = GetCharacterLevelForWealth();
            string className = GetStartingClassName();

            // Starting class changed (e.g. multiclass row 0) → prior class gold formula no longer applies
            if (CurrentCharacter != null &&
                !string.IsNullOrWhiteSpace(className) &&
                !string.IsNullOrWhiteSpace(_lastStartingWealthClass) &&
                !_lastStartingWealthClass.Equals(className, StringComparison.OrdinalIgnoreCase))
            {
                CurrentCharacter.Level1RolledGoldGp = 0;
                CurrentCharacter.Level1RolledGoldBreakdown = "";
                CurrentCharacter.UseRolledGoldInsteadOfEquipment = false;
            }
            if (!string.IsNullOrWhiteSpace(className))
                _lastStartingWealthClass = className;

            bool isLevel1 = level <= 1;
            bool hasHigherWealth = GameData.GetHigherLevelWealthFormula(level) != null;
            bool startedWithGold = CurrentCharacter?.UseRolledGoldInsteadOfEquipment == true
                && CurrentCharacter.Level1RolledGoldGp > 0;

            if (pnlLevel1WealthMode != null)
                pnlLevel1WealthMode.Visibility = isLevel1 ? Visibility.Visible : Visibility.Collapsed;
            if (pnlHigherLevelWealth != null)
                pnlHigherLevelWealth.Visibility = hasHigherWealth ? Visibility.Visible : Visibility.Collapsed;
            if (txtWealthMidLevelNote != null)
            {
                txtWealthMidLevelNote.Visibility = (!isLevel1 && !hasHigherWealth)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                if (txtWealthMidLevelNote.Visibility == Visibility.Visible)
                {
                    txtWealthMidLevelNote.Text =
                        $"Character level {level} (levels 2–4): use starting-class equipment and background gear. " +
                        "Wealth bands apply from total character level 5+. " +
                        "Multiclassing adds no extra starting gold.";
                }
            }

            // Level-1 formula / result — always the starting class, never later multiclass classes
            var lvl1Formula = GameData.GetLevel1StartingGoldFormula(className);
            if (txtLevel1GoldFormula != null)
            {
                txtLevel1GoldFormula.Text = lvl1Formula != null
                    ? $"Starting class: {className} — {lvl1Formula.Display}  (average {lvl1Formula.AverageGp:0.#} gp)"
                    : "Formula: — (select a starting class)";
            }

            bool useGoldChoice = isLevel1 && (rbRollStartingGold?.IsChecked == true
                || (CurrentCharacter?.UseRolledGoldInsteadOfEquipment == true && rbStartingEquipment?.IsChecked != true));

            if (btnRollLevel1Gold != null)
                btnRollLevel1Gold.IsEnabled = isLevel1 && useGoldChoice && lvl1Formula != null;

            if (txtLevel1GoldResult != null)
            {
                if (CurrentCharacter != null && CurrentCharacter.Level1RolledGoldGp > 0 &&
                    !string.IsNullOrWhiteSpace(CurrentCharacter.Level1RolledGoldBreakdown))
                {
                    txtLevel1GoldResult.Text = CurrentCharacter.Level1RolledGoldBreakdown;
                }
                else if (useGoldChoice)
                {
                    txtLevel1GoldResult.Text = "Not rolled yet";
                }
                else
                {
                    txtLevel1GoldResult.Text = "";
                }
            }

            // Higher-level wealth — one band from total character level (not per multiclass class)
            var higher = GameData.GetHigherLevelWealthFormula(level);
            if (higher != null)
            {
                if (txtHigherLevelWealthFormula != null)
                {
                    txtHigherLevelWealthFormula.Text =
                        $"Character level {level} → {higher.BandLabel}: {higher.Display}\n" +
                        $"(One roll for the whole character; added classes do not grant more gold.)";
                }
                if (txtHigherLevelMagicNote != null)
                    txtHigherLevelMagicNote.Text = higher.MagicItemNote;
                if (txtHigherLevelGoldResult != null)
                {
                    if (CurrentCharacter != null && CurrentCharacter.HigherLevelWealthGp > 0 &&
                        !string.IsNullOrWhiteSpace(CurrentCharacter.HigherLevelWealthBreakdown))
                        txtHigherLevelGoldResult.Text = CurrentCharacter.HigherLevelWealthBreakdown;
                    else
                        txtHigherLevelGoldResult.Text = "Not rolled yet";
                }
            }

            // Past level 1: hide exclusive choice UI. Keep gold if they already rolled at 1;
            // only reset the flag when they never completed a gold roll (were mid-choice).
            if (!isLevel1 && CurrentCharacter != null)
            {
                if (CurrentCharacter.UseRolledGoldInsteadOfEquipment && CurrentCharacter.Level1RolledGoldGp <= 0)
                {
                    CurrentCharacter.UseRolledGoldInsteadOfEquipment = false;
                    CurrentCharacter.Level1RolledGoldBreakdown = "";
                }
                // Do not clear Level1RolledGoldGp — multiclass / level-up keeps starting wealth
                if (!startedWithGold)
                {
                    _suppressWealthModeEvents = true;
                    try
                    {
                        if (rbStartingEquipment != null) rbStartingEquipment.IsChecked = true;
                    }
                    finally { _suppressWealthModeEvents = false; }
                }
            }

            // Clear higher-level wealth only when total character level drops below 5
            if (!hasHigherWealth && CurrentCharacter != null)
            {
                CurrentCharacter.HigherLevelWealthGp = 0;
                CurrentCharacter.HigherLevelWealthBreakdown = "";
            }

            SyncGoldPiecesTotal();
            ApplyEquipmentChoicesEnabledState();
        }

        private void StartingWealthMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressWealthModeEvents) return;
            if (CurrentCharacter == null) return;

            bool useGold = rbRollStartingGold?.IsChecked == true;
            CurrentCharacter.UseRolledGoldInsteadOfEquipment = useGold;

            if (!useGold)
            {
                // Switching back to equipment: clear the exclusive gold roll
                CurrentCharacter.Level1RolledGoldGp = 0;
                CurrentCharacter.Level1RolledGoldBreakdown = "";
                if (txtLevel1GoldResult != null)
                    txtLevel1GoldResult.Text = "";
            }
            else if (CurrentCharacter.Level1RolledGoldGp <= 0 && txtLevel1GoldResult != null)
            {
                txtLevel1GoldResult.Text = "Not rolled yet";
            }

            if (btnRollLevel1Gold != null)
            {
                string className = GetStartingClassName();
                btnRollLevel1Gold.IsEnabled = useGold
                    && GetCharacterLevelForWealth() <= 1
                    && GameData.GetLevel1StartingGoldFormula(className) != null;
            }

            SyncGoldPiecesTotal();
            ApplyEquipmentChoicesEnabledState();
            UpdateTotalEquipmentSummary();
        }

        private void BtnRollLevel1Gold_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentCharacter == null) return;
            // Always the first / starting class — never a later multiclass
            string className = GetStartingClassName();
            if (string.IsNullOrWhiteSpace(className)) return;

            int gp = GameData.RollLevel1StartingGold(className, out string breakdown);
            CurrentCharacter.Level1RolledGoldGp = gp;
            CurrentCharacter.Level1RolledGoldBreakdown = breakdown;
            CurrentCharacter.UseRolledGoldInsteadOfEquipment = true;

            if (txtLevel1GoldResult != null)
                txtLevel1GoldResult.Text = breakdown;

            SyncGoldPiecesTotal();
            UpdateTotalEquipmentSummary();
        }

        private void BtnRollHigherLevelGold_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentCharacter == null) return;
            // Total character level only (sum of all classes) — not per multiclass class
            int level = GetCharacterLevelForWealth();
            int gp = GameData.RollHigherLevelWealthGold(level, out string breakdown);
            if (gp <= 0) return;

            CurrentCharacter.HigherLevelWealthGp = gp;
            CurrentCharacter.HigherLevelWealthBreakdown = breakdown;

            if (txtHigherLevelGoldResult != null)
                txtHigherLevelGoldResult.Text = breakdown;

            SyncGoldPiecesTotal();
            UpdateTotalEquipmentSummary();
        }

        private void SyncGoldPiecesTotal()
        {
            if (CurrentCharacter == null) return;
            // Matches PDF export: rolled + higher-level + custom gold + background pouch (equipment path, level < 5)
            if (txtBackgroundEquipment != null && !string.IsNullOrWhiteSpace(txtBackgroundEquipment.Text))
                CurrentCharacter.BackgroundEquipment = txtBackgroundEquipment.Text;
            ApplyCustomGoldFromUi();
            CurrentCharacter.GoldPieces = GameData.ComputeSheetGoldPieces(CurrentCharacter);
        }

        /// <summary>Reads the custom gold amount into <see cref="Character"/>.</summary>
        private void ApplyCustomGoldFromUi()
        {
            if (CurrentCharacter == null) return;

            if (txtCustomGoldGp != null)
            {
                string raw = (txtCustomGoldGp.Text ?? "").Trim().Replace(",", "");
                if (int.TryParse(raw, out int gp) && gp > 0)
                    CurrentCharacter.CustomGoldGp = gp;
                else
                    CurrentCharacter.CustomGoldGp = 0;
            }
        }

        private void TxtCustomGoldGp_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentCharacter == null) return;
            ApplyCustomGoldFromUi();
            CurrentCharacter.GoldPieces = GameData.ComputeSheetGoldPieces(CurrentCharacter);
            UpdateTotalEquipmentSummary();
        }

        /// <summary>Pushes saved custom gold values into the Starting Wealth text boxes.</summary>
        private void RestoreCustomGoldUi()
        {
            if (txtCustomGoldGp != null)
            {
                txtCustomGoldGp.TextChanged -= TxtCustomGoldGp_TextChanged;
                try
                {
                    txtCustomGoldGp.Text = CurrentCharacter?.CustomGoldGp > 0
                        ? CurrentCharacter.CustomGoldGp.ToString()
                        : "0";
                }
                finally
                {
                    txtCustomGoldGp.TextChanged += TxtCustomGoldGp_TextChanged;
                }
            }
        }

        /// <summary>
        /// When the character started with level-1 gold instead of kit, disable class kit radios and weapon combos.
        /// </summary>
        private void ApplyEquipmentChoicesEnabledState()
        {
            bool useGoldInstead = CurrentCharacter?.UseRolledGoldInsteadOfEquipment == true
                || (GetCharacterLevelForWealth() <= 1 && rbRollStartingGold?.IsChecked == true);

            if (pnlEquipmentChoices != null)
                pnlEquipmentChoices.IsEnabled = !useGoldInstead;

            // Weapon pickers only matter when taking equipment
            for (int i = 1; i <= 6; i++)
            {
                if (this.FindName($"cmbWeaponChoice{i}") is ComboBox cmb)
                    cmb.IsEnabled = !useGoldInstead;
            }

            // Visual cue on the choices panel
            if (pnlEquipmentChoices != null)
                pnlEquipmentChoices.Opacity = useGoldInstead ? 0.45 : 1.0;
        }

        private void UpdateTotalEquipmentSummary()
        {
            if (pnlTotalEquipmentSummary == null) return;
            pnlTotalEquipmentSummary.Children.Clear();

            var totalItems = new List<string>();
            // Started with gold instead of kit (persists after multiclass / level-up)
            bool useGoldInstead = CurrentCharacter?.UseRolledGoldInsteadOfEquipment == true
                || (GetCharacterLevelForWealth() <= 1 && rbRollStartingGold?.IsChecked == true);

            if (!useGoldInstead)
            {
                // === RADIO BUTTONS + AUTOMATIC ITEMS ===
                foreach (var sp in pnlEquipmentChoices.Children.OfType<StackPanel>())
                {
                    foreach (var element in sp.Children)
                    {
                        if (element is RadioButton rb && rb.IsChecked == true)
                        {
                            string text = "";

                            if (rb.Content is TextBlock tb)
                                text = tb.Text;
                            else if (rb.Content is StackPanel spContent)
                            {
                                var firstTextBlock = spContent.Children.OfType<TextBlock>().FirstOrDefault();
                                if (firstTextBlock != null)
                                    text = firstTextBlock.Text;
                            }
                            else
                                text = rb.Content?.ToString() ?? "";

                            if (!string.IsNullOrWhiteSpace(text))
                                totalItems.Add("• " + text);
                        }
                        else if (element is TextBlock tb && tb.Text.StartsWith("• "))
                        {
                            totalItems.Add(tb.Text);
                        }
                    }
                }

                // === WEAPONS FROM COMBOBOXES + SPECIAL CASES ===
                foreach (var weaponEntry in pickedWeapons)
                {
                    string clean = weaponEntry.Contains(": ")
                        ? weaponEntry.Substring(weaponEntry.IndexOf(": ") + 2)
                        : weaponEntry;

                    if (!string.IsNullOrWhiteSpace(clean))
                        totalItems.Add("• " + clean);

                    // === SPECIAL: Martial weapon and shield ===
                    if (weaponEntry.Contains("Martial weapon and shield", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!totalItems.Any(x => x.Contains("Shield", StringComparison.OrdinalIgnoreCase)))
                            totalItems.Add("• Shield");
                    }
                }

                // === REMOVE ALL GENERIC PLACEHOLDER LABELS (this is the fix you asked for) ===
                totalItems.RemoveAll(x => x.Contains("Any simple weapon", StringComparison.OrdinalIgnoreCase));
                totalItems.RemoveAll(x => x.Contains("Any martial weapon", StringComparison.OrdinalIgnoreCase));
                totalItems.RemoveAll(x => x.Contains("Martial weapon and shield", StringComparison.OrdinalIgnoreCase));
                totalItems.RemoveAll(x => x.Contains("Any simple melee weapon", StringComparison.OrdinalIgnoreCase));
                totalItems.RemoveAll(x => x.Contains("Two martial weapons", StringComparison.OrdinalIgnoreCase));
                totalItems.RemoveAll(x => x.Contains("Any martial melee weapon", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Gold instead of class kit
                if (CurrentCharacter != null && CurrentCharacter.Level1RolledGoldGp > 0)
                {
                    totalItems.Add($"• Starting gold: {CurrentCharacter.Level1RolledGoldGp} gp"
                        + (string.IsNullOrWhiteSpace(CurrentCharacter.Level1RolledGoldBreakdown)
                            ? ""
                            : $" ({CurrentCharacter.Level1RolledGoldBreakdown})"));
                }
                else
                {
                    totalItems.Add("• Starting gold: not rolled yet — click Roll Gold");
                }
            }

            // === BACKGROUND EQUIPMENT (as ONE item; always granted) ===
            if (cmbBackground.SelectedItem is string bg)
            {
                string bgEquip = GameData.GetBackgroundEquipment(bg);
                if (!string.IsNullOrWhiteSpace(bgEquip) && !bgEquip.Contains("See Background tab"))
                {
                    totalItems.Add("• " + bgEquip);   // ← Add as single bullet
                }
            }

            // === HIGHER-LEVEL WEALTH (DMG, in addition to equipment) ===
            if (CurrentCharacter != null && CurrentCharacter.HigherLevelWealthGp > 0)
            {
                totalItems.Add($"• Higher-level wealth: {CurrentCharacter.HigherLevelWealthGp:N0} gp"
                    + (string.IsNullOrWhiteSpace(CurrentCharacter.HigherLevelWealthBreakdown)
                        ? ""
                        : $" ({CurrentCharacter.HigherLevelWealthBreakdown})"));
            }
            else if (GameData.GetHigherLevelWealthFormula(GetCharacterLevelForWealth()) != null)
            {
                totalItems.Add("• Higher-level wealth: not rolled yet — click Roll Wealth");
            }

            // === CUSTOM / DM FIXED GOLD ===
            if (CurrentCharacter != null && CurrentCharacter.CustomGoldGp > 0)
            {
                string customLine = $"• Custom gold: {CurrentCharacter.CustomGoldGp:N0} gp";
                if (!string.IsNullOrWhiteSpace(CurrentCharacter.CustomGoldNote))
                    customLine += $" ({CurrentCharacter.CustomGoldNote.Trim()})";
                totalItems.Add(customLine);
            }

            // === FINAL CLEAN DISPLAY ===
            if (totalItems.Count > 0)
            {
                foreach (var item in totalItems.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    bool isGoldLine = item.Contains("Starting gold", StringComparison.OrdinalIgnoreCase)
                        || item.Contains("Higher-level wealth", StringComparison.OrdinalIgnoreCase)
                        || item.Contains("Custom gold", StringComparison.OrdinalIgnoreCase)
                        || item.Contains("Custom / DM gold", StringComparison.OrdinalIgnoreCase);
                    pnlTotalEquipmentSummary.Children.Add(new TextBlock
                    {
                        Text = item,
                        Foreground = isGoldLine
                            ? new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E8C36A"))
                            : Brushes.White,
                        Margin = new Thickness(10, 2, 0, 2),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 13
                    });
                }
            }
            else
            {
                pnlTotalEquipmentSummary.Children.Add(new TextBlock
                {
                    Text = "No equipment selected yet.",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(10, 0, 0, 0)
                });
            }
            UpdateEquippedAC();
            UpdateCarryingCapacity();
        }

        private void RefreshWeaponComboBoxes()
        {
            for (int i = 1; i <= 6; i++)
            {
                var cmb = this.FindName($"cmbWeaponChoice{i}") as ComboBox;
                if (cmb != null)
                {
                    cmb.Visibility = Visibility.Collapsed;
                    cmb.SelectedItem = null;
                }
            }

            if (activeWeaponChoices.Count == 0) return;

            int currentIndex = 1;

            foreach (var kvp in activeWeaponChoices)
            {
                var (count, weaponType) = kvp.Value;

                var weaponList = weaponType == "Simple"
                    ? GameData.SimpleWeapons
                    : GameData.MartialWeapons;

                // Filter to melee only when the radio choice specifies "melee weapon"
                bool isMeleeOnly = kvp.Key.Contains("melee", StringComparison.OrdinalIgnoreCase);
                if (isMeleeOnly)
                {
                    weaponList = weaponList.Where(w => w.Range == "-" || w.Properties.Contains("Thrown", StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // === Convert to simple string list (just names) ===
                var weaponNames = weaponList.Select(w => w.Name).ToList();

                for (int i = 0; i < count && currentIndex <= 6; i++)
                {
                    var cmb = this.FindName($"cmbWeaponChoice{currentIndex}") as ComboBox;
                    if (cmb != null)
                    {
                        cmb.ItemsSource = weaponNames;
                        cmb.SelectedIndex = 0;
                        cmb.Visibility = Visibility.Visible;

                        cmb.SelectionChanged -= WeaponComboBox_SelectionChanged;
                        cmb.SelectionChanged += WeaponComboBox_SelectionChanged;
                    }
                    currentIndex++;
                }
            }
        }

        private void WeaponComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cmb || cmb.SelectedItem is not string weaponName)
                return;

            string comboBoxName = cmb.Name;

            // Remove previous selection from this specific ComboBox
            pickedWeapons.RemoveAll(x => x.StartsWith(comboBoxName + ":"));

            // Add the new selection (just the name)
            pickedWeapons.Add($"{comboBoxName}: {weaponName}");

            UpdateTotalEquipmentSummary();
        }

        private void cmbWeaponType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgWeaponRef == null) return;   // Prevents crash during initialization

            if (cmbWeaponType.SelectedIndex == 0) // Simple Weapons
            {
                dgWeaponRef.ItemsSource = GameData.SimpleWeapons;
            }
            else // Martial Weapons
            {
                dgWeaponRef.ItemsSource = GameData.MartialWeapons;
            }
        }

        // Remove the old ItemWeights field if you had one, then add this:
        private void UpdateCarryingCapacity()
        {
            if (pbCarryingCapacity == null || lblCarryingCapacity == null || lblPushDragLift == null)
                return;

            if (!int.TryParse(txtStrFinal.Text, out int strength) || strength < 1)
                strength = 10;

            double carryingCapacity = strength * 15.0;
            double pushDragLift = strength * 30.0;

            double totalWeight = 0;

            foreach (var child in pnlTotalEquipmentSummary.Children.OfType<TextBlock>())
            {
                string text = child.Text.Replace("• ", "").Trim();

                foreach (var kvp in GameData.ItemWeights)
                {
                    if (text.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        totalWeight += kvp.Value;
                        break;
                    }
                }
            }

            double percentage = carryingCapacity > 0 ? (totalWeight / carryingCapacity) * 100 : 0;
            pbCarryingCapacity.Value = Math.Min(percentage, 100);

            // Color logic
            if (percentage >= 100)
                pbCarryingCapacity.Foreground = Brushes.Red;
            else if (percentage >= 80)
                pbCarryingCapacity.Foreground = Brushes.Yellow;
            else
                pbCarryingCapacity.Foreground = BrushSuccess;

            lblCarryingCapacity.Text = $"{totalWeight:F1} / {carryingCapacity:F0} lbs";
            lblPushDragLift.Text = $"Push / Drag / Lift: {pushDragLift:F0} lbs";
        }

        private void UpdateEquippedAC()
        {
            if (pnlTotalEquipmentSummary == null || txtDexMod == null ||
                lblEquippedACHeader == null || txtEquippedAC == null)
                return;

            // === 1. Calculate Base AC (always available) ===
            int dexMod = GetModifierFromText(txtDexMod.Text);
            int conMod = GetModifierFromText(txtConMod.Text);
            int wisMod = GetModifierFromText(txtWisMod.Text);

            string className = cmbClass.SelectedItem as string;

            int baseAC = 10 + dexMod;

            if (className == "Barbarian")
                baseAC += conMod;
            else if (className == "Monk")
                baseAC += wisMod;

            // === 2. Calculate Equipped Armor AC (if any) ===
            var equippedItems = new List<string>();
            foreach (var child in pnlTotalEquipmentSummary.Children.OfType<TextBlock>())
            {
                string text = child.Text.Replace("• ", "").Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    equippedItems.Add(text.ToLowerInvariant());
            }

            Armor bestArmor = null;
            int bestArmorAC = 0;
            string bestArmorDisplay = "";

            foreach (var armor in GameData.AllArmors)
            {
                if (equippedItems.Any(item => item.Contains(armor.Name.ToLowerInvariant())))
                {
                    bool isHeavy = armor.Type == "Heavy";
                    int effectiveAC = ParseArmorAC(armor.AC, dexMod, isHeavy);
                    int baseArmorAC = ParseBaseAC(armor.AC);

                    if (effectiveAC > bestArmorAC)
                    {
                        bestArmor = armor;
                        bestArmorAC = effectiveAC;
                        bestArmorDisplay = armor.Name;
                    }
                }
            }

            // === 3. Determine the BEST AC source ===
            int finalAC = Math.Max(baseAC, bestArmorAC);

            // === 4. Add Shield bonus if equipped ===
            bool hasShield = equippedItems.Any(i => i.Contains("shield"));
            if (hasShield)
                finalAC += 2;

            // === 5. Defense fighting style: +1 AC while wearing armor ===
            bool wearingArmor = bestArmor != null;
            bool hasDefenseStyle = CharacterHasFightingStyle("Defense");
            if (hasDefenseStyle && wearingArmor)
                finalAC += 1;

            // === 6. Update UI ===
            if (bestArmor != null || hasShield || finalAC > 10)
            {
                lblEquippedACHeader.Visibility = Visibility.Visible;
                txtEquippedAC.Visibility = Visibility.Visible;

                string breakdown = "";

                if (bestArmor != null && bestArmorAC >= baseAC)
                {
                    // Use equipped armor as primary
                    string dexPart = "";
                    if (!bestArmor.Type.Equals("Heavy", StringComparison.OrdinalIgnoreCase))
                    {
                        string sign = dexMod >= 0 ? "+" : "";
                        dexPart = $" {sign}{dexMod} [Dex]";
                    }
                    breakdown = $"{ParseBaseAC(bestArmor.AC)} [{bestArmorDisplay}]{dexPart}";
                }
                else
                {
                    // Use Base AC as primary (or when it's higher)
                    breakdown = $"{baseAC} [Base AC]";
                }

                if (hasShield)
                {
                    breakdown += string.IsNullOrEmpty(breakdown)
                        ? "+2 [Shield]"
                        : " +2 [Shield]";
                }

                if (hasDefenseStyle && wearingArmor)
                {
                    breakdown += string.IsNullOrEmpty(breakdown)
                        ? "+1 [Defense]"
                        : " +1 [Defense]";
                }

                string displayText = $"({finalAC}): {breakdown}";
                txtEquippedAC.Text = displayText;

                if (CurrentCharacter != null)
                {
                    CurrentCharacter.EquippedACDisplay = displayText;
                    CurrentCharacter.ArmorClass = finalAC;
                }
            }
            else
            {
                lblEquippedACHeader.Visibility = Visibility.Collapsed;
                txtEquippedAC.Visibility = Visibility.Collapsed;
            }
        }

        private int ParseBaseAC(string acString)
        {
            var match = System.Text.RegularExpressions.Regex.Match(acString ?? "", @"(\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out int ac) ? ac : 10;
        }

        private int ParseArmorAC(string acString, int dexMod, bool isHeavy)
        {
            int baseAC = ParseBaseAC(acString);
            if (isHeavy || string.IsNullOrEmpty(acString) || !acString.Contains("Dex", StringComparison.OrdinalIgnoreCase))
                return baseAC;

            int maxDex = 99;
            var maxMatch = System.Text.RegularExpressions.Regex.Match(acString, @"max (\d+)");
            if (maxMatch.Success && int.TryParse(maxMatch.Groups[1].Value, out int max))
                maxDex = max;

            return baseAC + Math.Min(dexMod, maxDex);
        }

        private void SetupBackgroundLanguageChoices(string detailsText)
        {
            pnlBackgroundLanguages.Visibility = Visibility.Collapsed;
            cmbBackgroundLanguage1.Visibility = Visibility.Collapsed;
            cmbBackgroundLanguage2.Visibility = Visibility.Collapsed;

            int numChoices = 0;
            if (detailsText.Contains("Two of your choice", StringComparison.OrdinalIgnoreCase)) numChoices = 2;
            else if (detailsText.Contains("One of your choice", StringComparison.OrdinalIgnoreCase)) numChoices = 1;

            if (numChoices == 0) return;

            // Get languages already known from race
            var knownLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (cmbRace.SelectedItem is string race && GameData.RaceData.TryGetValue(race, out var raceData))
            {
                knownLanguages.UnionWith(raceData.Languages);
            }

            var available = GameData.AllLanguages.Where(l => !knownLanguages.Contains(l)).ToList();

            // Populate dropdown(s)
            cmbBackgroundLanguage1.ItemsSource = available;
            cmbBackgroundLanguage1.SelectedIndex = 0;
            cmbBackgroundLanguage1.Visibility = Visibility.Visible;

            if (numChoices >= 2)
            {
                cmbBackgroundLanguage2.ItemsSource = available;
                cmbBackgroundLanguage2.SelectedIndex = Math.Min(1, available.Count - 1);
                cmbBackgroundLanguage2.Visibility = Visibility.Visible;
            }

            pnlBackgroundLanguages.Visibility = Visibility.Visible;

            // Live update when user selects
            cmbBackgroundLanguage1.SelectionChanged -= BackgroundLanguage_Changed;
            cmbBackgroundLanguage1.SelectionChanged += BackgroundLanguage_Changed;
            cmbBackgroundLanguage2.SelectionChanged -= BackgroundLanguage_Changed;
            cmbBackgroundLanguage2.SelectionChanged += BackgroundLanguage_Changed;
        }

        private void BackgroundLanguage_Changed(object sender, SelectionChangedEventArgs e)
        {
            backgroundLanguage1 = cmbBackgroundLanguage1.SelectedItem?.ToString() ?? "";
            backgroundLanguage2 = cmbBackgroundLanguage2.SelectedItem?.ToString() ?? "";
            // You can optionally update txtBackgroundDetails here to show the chosen languages
        }

        private void AddEquipmentChoiceGroup(string label, string[] options)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            var cmb = new ComboBox { ItemsSource = options, SelectedIndex = 0, Width = 400, Margin = new Thickness(0, 3, 0, 0) };
            panel.Children.Add(cmb);
            pnlEquipmentChoices.Children.Add(panel);
        }

        private void FeatStatChoice_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (dgFeats.SelectedItem is not Feat selectedFeat) return;

            string name = selectedFeat.Name.ToLowerInvariant();

            // === RESET PREVIOUS ABILITY BONUS ===
            if (!string.IsNullOrEmpty(currentFeatAbilityChoice))
            {
                featStatBonuses[currentFeatAbilityChoice] = Math.Max(0, featStatBonuses.GetValueOrDefault(currentFeatAbilityChoice, 0) - 1);
                currentFeatAbilityChoice = "";
            }

            // Clear Resilient save when changing feats / ability picks (re-applied below if still Resilient)
            if (!string.IsNullOrEmpty(resilientSaveAbility))
                resilientSaveAbility = "";

            // Only apply choices when the choices panel is actually shown for this feat
            if (pnlFeatStatChoices.Visibility != Visibility.Visible)
                return;

            // ===================== FIGHTING INITIATE =====================
            if (name == "fighting initiate")
            {
                ApplyFightingInitiateStyleFromUi();
                return;
            }

            // ===================== MAGIC INITIATE =====================
            if (name == "magic initiate")
            {
                currentFeatSpellSource = "Magic Initiate";
                currentFeatSpells.Clear();

                magicInitiateClass = cmbFeatStatChoice1.SelectedItem?.ToString() ?? "";

                string c1 = cmbFeatStatChoice2.SelectedItem as string;
                if (!string.IsNullOrEmpty(c1)) currentFeatSpells.Add(c1);

                string c2 = cmbFeatStatChoice3.SelectedItem as string;
                if (!string.IsNullOrEmpty(c2) && c2 != c1) currentFeatSpells.Add(c2);

                if (!string.IsNullOrEmpty(cmbFeatStatChoice4.SelectedItem as string))
                    currentFeatSpells.Add(cmbFeatStatChoice4.SelectedItem as string);

                UpdateFeatSpellsLabel();
                return;
            }

            // ===================== FEY TOUCHED =====================
            if (name == "fey touched")
            {
                currentFeatSpellSource = "Fey Touched";
                currentFeatSpells.Clear();

                // Automatic spell
                currentFeatSpells.Add("Misty Step");

                // Chosen spell
                if (cmbFeatStatChoice2.SelectedItem is string chosenSpell)
                    currentFeatSpells.Add(chosenSpell);

                // === APPLY ABILITY SCORE BONUS ===
                if (cmbFeatStatChoice1.SelectedItem is string ability)
                {
                    featStatBonuses[ability] = featStatBonuses.GetValueOrDefault(ability, 0) + 1;
                    currentFeatAbilityChoice = ability;
                }

                UpdateFeatSpellsLabel();
                UpdateStatDisplays();           // ← Important: refresh Ability Scores tab
                return;
            }

            // ===================== SHADOW TOUCHED =====================
            if (name == "shadow touched")
            {
                currentFeatSpellSource = "Shadow Touched";
                currentFeatSpells.Clear();

                // Automatic spell
                currentFeatSpells.Add("Invisibility");

                // Chosen spell
                if (cmbFeatStatChoice2.SelectedItem is string chosenSpell)
                    currentFeatSpells.Add(chosenSpell);

                // === APPLY ABILITY SCORE BONUS ===
                if (cmbFeatStatChoice1.SelectedItem is string ability)
                {
                    featStatBonuses[ability] = featStatBonuses.GetValueOrDefault(ability, 0) + 1;
                    currentFeatAbilityChoice = ability;
                }

                UpdateFeatSpellsLabel();
                UpdateStatDisplays();           // ← Important: refresh Ability Scores tab
                return;
            }

            // ===================== SPELL SNIPER =====================
            if (name == "spell sniper")
            {
                currentFeatSpellSource = "Spell Sniper";
                currentFeatSpells.Clear();

                if (cmbFeatStatChoice2.SelectedItem is string cantrip)
                    currentFeatSpells.Add(cantrip);

                UpdateFeatSpellsLabel();
                return;
            }

            // ===================== RESILIENT =====================
            // +1 to chosen ability and proficiency in that ability's saving throws
            if (name == "resilient")
            {
                if (cmbFeatStatChoice1.SelectedItem is string resilientAbility)
                {
                    featStatBonuses[resilientAbility] = featStatBonuses.GetValueOrDefault(resilientAbility, 0) + 1;
                    currentFeatAbilityChoice = resilientAbility;
                    resilientSaveAbility = resilientAbility;
                }

                UpdateStatDisplays();
                UpdateSavingThrows();
                UpdateInitiative();
                return;
            }

            // ===================== EXISTING ABILITY SCORE LOGIC =====================
            if (cmbFeatStatChoice1.Visibility == Visibility.Visible &&
                cmbFeatStatChoice1.SelectedItem is string ability1)
            {
                featStatBonuses[ability1] = featStatBonuses.GetValueOrDefault(ability1, 0) + 1;
                currentFeatAbilityChoice = ability1;
            }

            if (cmbFeatStatChoice2.Visibility == Visibility.Visible &&
                cmbFeatStatChoice2.SelectedItem is string choice2)
            {
                if (selectedFeat.Name == "Skill Expert" || selectedFeat.Name == "Prodigy")
                {
                    featStatBonuses[choice2] = featStatBonuses.GetValueOrDefault(choice2, 0) + 1;
                }
            }

            UpdateStatDisplays();
            UpdateSavingThrows();
            UpdateInitiative();
        }

        public bool MeetsPrerequisite(Feat feat)
        {
            if (string.IsNullOrWhiteSpace(feat.Prerequisites) ||
                feat.Prerequisites.Equals("None", StringComparison.OrdinalIgnoreCase))
                return true;

            string prereq = feat.Prerequisites;
            string prereqLower = prereq.ToLowerInvariant();

            // Compound requirements joined with " + " (e.g. Dex 13 + Stealth proficiency)
            if (prereq.Contains('+'))
            {
                var parts = prereq.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts.All(MeetsSinglePrerequisiteClause);
            }

            return MeetsSinglePrerequisiteClause(prereq);
        }

        /// <summary>Evaluate one prerequisite clause (ability score, proficiency, race, spellcasting, …).</summary>
        private bool MeetsSinglePrerequisiteClause(string clause)
        {
            if (string.IsNullOrWhiteSpace(clause))
                return true;

            string prereq = clause.Trim();
            string prereqLower = prereq.ToLowerInvariant();

            // === ABILITY SCORE: "Strength 13 or higher" ===
            if (prereqLower.Contains("or higher"))
            {
                // Match "Charisma 13 or higher" / "Intelligence or Wisdom 13 or higher"
                if (prereqLower.Contains("intelligence or wisdom"))
                {
                    int required = ExtractRequiredScore(prereqLower) ?? 13;
                    return GetFinalStat("Intelligence") >= required || GetFinalStat("Wisdom") >= required;
                }

                var scoreMatch = System.Text.RegularExpressions.Regex.Match(
                    prereq, @"(Strength|Dexterity|Constitution|Intelligence|Wisdom|Charisma)\s+(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[2].Value, out int requiredSingle))
                {
                    return GetFinalStat(scoreMatch.Groups[1].Value) >= requiredSingle;
                }
            }

            // === SPELLCASTING FEATURE ===
            if (prereqLower.Contains("spellcasting"))
                return CharacterHasSpellcastingFeature();

            // === RACE: half-elf / half-orc / human / custom lineage ===
            if (prereqLower.Contains("half-elf") || prereqLower.Contains("half-orc") ||
                prereqLower.Contains("human") || prereqLower.Contains("custom lineage"))
            {
                string currentRace = cmbRace.SelectedItem?.ToString() ?? "";
                return currentRace.Contains("Half-Elf", StringComparison.OrdinalIgnoreCase) ||
                       currentRace.Contains("Half-Orc", StringComparison.OrdinalIgnoreCase) ||
                       currentRace.Contains("Human", StringComparison.OrdinalIgnoreCase) ||
                       currentRace.Contains("Variant Human", StringComparison.OrdinalIgnoreCase) ||
                       currentRace.Contains("Custom Lineage", StringComparison.OrdinalIgnoreCase);
            }

            // === SKILL PROFICIENCY (e.g. "proficiency in Stealth") ===
            if (prereqLower.Contains("proficiency in "))
            {
                // "proficiency in Stealth"
                var skillMatch = System.Text.RegularExpressions.Regex.Match(
                    prereq, @"proficiency in\s+([A-Za-z ]+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (skillMatch.Success)
                {
                    string skillName = skillMatch.Groups[1].Value.Trim();
                    // strip trailing junk
                    int cut = skillName.IndexOf(" or ", StringComparison.OrdinalIgnoreCase);
                    if (cut > 0) skillName = skillName.Substring(0, cut).Trim();
                    return allSkills?.Any(s =>
                               s.IsProficient &&
                               s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase)) == true;
                }
            }

            // === HEALER'S KIT (equipment-gated; allow for now / common casters & medical classes) ===
            if (prereqLower.Contains("healer") && prereqLower.Contains("kit"))
                return true;

            // === ARMOR / SHIELD PROFICIENCY ===
            if (prereqLower.Contains("heavy armor"))
                return HasArmorProficiencyCategory("heavy");
            if (prereqLower.Contains("medium armor"))
                return HasArmorProficiencyCategory("medium") || HasArmorProficiencyCategory("heavy");
            if (prereqLower.Contains("light armor"))
                return HasArmorProficiencyCategory("light") ||
                       HasArmorProficiencyCategory("medium") ||
                       HasArmorProficiencyCategory("heavy");
            if (prereqLower.Contains("shield"))
                return HasShieldProficiency();

            // === WEAPON PROFICIENCIES (specific lists and categories) ===
            // Fighting Initiate: martial weapon OR unarmed strike (everyone is proficient with unarmed strikes)
            if (prereqLower.Contains("martial weapon") && prereqLower.Contains("unarmed"))
                return HasMartialWeaponProficiency() || HasUnarmedStrikeProficiency();

            if (prereqLower.Contains("martial weapon"))
                return HasMartialWeaponProficiency();

            if (prereqLower.Contains("finesse"))
                return HasWeaponProficiencyWithProperty("Finesse");

            // Polearm Master: glaive, haldberd, quarterstaff, or spear
            if (prereqLower.Contains("glaive") || prereqLower.Contains("halberd") ||
                prereqLower.Contains("quarterstaff") || prereqLower.Contains("spear"))
            {
                return HasProficiencyWithAnyNamedWeapon("Glaive", "Halberd", "Quarterstaff", "Spear");
            }

            // Piercer-style: thrown property OR ranged weapon
            if (prereqLower.Contains("thrown") ||
                (prereqLower.Contains("ranged") && prereqLower.Contains("weapon")))
            {
                return HasWeaponProficiencyWithProperty("Thrown") ||
                       HasWeaponProficiencyWithProperty("Ammunition") ||
                       HasProficientRangedWeapon();
            }

            // Slasher: weapon that deals slashing
            if (prereqLower.Contains("slashing"))
                return HasWeaponProficiencyWithDamageType("Slashing");

            // Generic "proficiency with a weapon..."
            if (prereqLower.Contains("proficiency with a weapon") ||
                prereqLower.Contains("proficiency with weapons"))
                return GetProficientWeapons().Count > 0 || HasUnarmedStrikeProficiency();

            // Unknown clause: do not block the player
            return true;
        }

        private static int? ExtractRequiredScore(string prereqLower)
        {
            var m = System.Text.RegularExpressions.Regex.Match(prereqLower, @"(\d+)\s+or higher");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int n))
                return n;
            return null;
        }

        // ───────────────────────── Proficiency inventory for feat prereqs ─────────────────────────

        /// <summary>
        /// Raw proficiency tags for feat prereqs, using PHB multiclass rules:
        /// primary class = full starting list; secondary classes = multiclass table only;
        /// plus subclass feature grants (domain/patron/college).
        /// </summary>
        private List<string> GetCharacterProficiencyTags()
        {
            var tags = new List<string>();

            void AddRange(IEnumerable<string>? items)
            {
                if (items == null) return;
                foreach (var t in items)
                {
                    if (string.IsNullOrWhiteSpace(t)) continue;
                    // Strip display suffixes like "(Twilight Domain)" / "(multiclass Cleric)"
                    string clean = t.Trim();
                    int paren = clean.IndexOf(" (", StringComparison.Ordinal);
                    if (paren > 0) clean = clean.Substring(0, paren).Trim();
                    if (!tags.Any(x => x.Equals(clean, StringComparison.OrdinalIgnoreCase)))
                        tags.Add(clean);
                }
            }

            // Reuse the same collection logic as the Skills tab summary
            var armor = new List<string>();
            var weapons = new List<string>();
            CollectClassAndSubclassProficiencies(armor, weapons, subclassGrantNotes: null);
            AddRange(armor);
            AddRange(weapons);

            return tags;
        }

        /// <summary>
        /// Expand proficiency tags into concrete <see cref="Weapon"/> entries.
        /// "Martial weapons" / "Simple weapons" expand to full lists; "Rapiers" matches Rapier, etc.
        /// </summary>
        private List<Weapon> GetProficientWeapons()
        {
            var result = new List<Weapon>();
            var tags = GetCharacterProficiencyTags();
            bool allSimple = tags.Any(t =>
                t.Equals("Simple weapons", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("Simple weapon", StringComparison.OrdinalIgnoreCase));
            bool allMartial = tags.Any(t =>
                t.Equals("Martial weapons", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("Martial weapon", StringComparison.OrdinalIgnoreCase));

            if (allSimple)
                result.AddRange(GameData.SimpleWeapons);
            if (allMartial)
                result.AddRange(GameData.MartialWeapons);

            foreach (var tag in tags)
            {
                // Skip category tags already expanded
                if (tag.Contains("Simple weapon", StringComparison.OrdinalIgnoreCase) ||
                    tag.Contains("Martial weapon", StringComparison.OrdinalIgnoreCase) ||
                    tag.Contains("armor", StringComparison.OrdinalIgnoreCase) ||
                    tag.Contains("Shield", StringComparison.OrdinalIgnoreCase) ||
                    tag.Equals("None", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Match specific weapons: "Rapiers" / "Longswords" / "Hand crossbows" / "Shortswords"
                string normalized = NormalizeWeaponProficiencyTag(tag);
                foreach (var w in GameData.SimpleWeapons.Concat(GameData.MartialWeapons))
                {
                    if (WeaponNameMatchesProficiency(w.Name, normalized) &&
                        !result.Any(r => r.Name.Equals(w.Name, StringComparison.OrdinalIgnoreCase)))
                        result.Add(w);
                }
            }

            return result;
        }

        private static string NormalizeWeaponProficiencyTag(string tag)
        {
            string t = tag.Trim();
            // Common plurals in class lists
            if (t.EndsWith("es", StringComparison.OrdinalIgnoreCase) && t.Length > 3)
            {
                // crosses → cross? prefer stripping trailing 's' for "swords"/"crossbows"
            }
            if (t.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
                !t.EndsWith("ss", StringComparison.OrdinalIgnoreCase) &&
                t.Length > 2)
            {
                // Rapiers → Rapier, Longswords → Longsword, Shortswords → Shortsword
                // Hand crossbows → Hand crossbow, Light crossbows → Light crossbow
                t = t.Substring(0, t.Length - 1);
            }
            return t;
        }

        private static bool WeaponNameMatchesProficiency(string weaponName, string proficiencyTag)
        {
            if (weaponName.Equals(proficiencyTag, StringComparison.OrdinalIgnoreCase))
                return true;
            // "Hand crossbow" vs "Hand Crossbows" already singularized
            if (weaponName.Equals(proficiencyTag + "s", StringComparison.OrdinalIgnoreCase))
                return true;
            // Loose contains for "crossbow" style tags
            string w = weaponName.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
            string p = proficiencyTag.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
            return w.Equals(p, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True if the character has proficiency with any martial weapon —
        /// full "Martial weapons" grant OR any specific martial (rapier, longsword, …).
        /// </summary>
        private bool HasMartialWeaponProficiency()
        {
            var tags = GetCharacterProficiencyTags();
            if (tags.Any(t => t.Contains("Martial weapon", StringComparison.OrdinalIgnoreCase)))
                return true;

            // Specific martial weapons by name (Rogue: rapiers, longswords, shortswords, hand crossbows)
            var proficient = GetProficientWeapons();
            var martialNames = new HashSet<string>(
                GameData.MartialWeapons.Select(w => w.Name),
                StringComparer.OrdinalIgnoreCase);
            return proficient.Any(w => martialNames.Contains(w.Name));
        }

        /// <summary>PHB: every character is proficient with unarmed strikes.</summary>
        private static bool HasUnarmedStrikeProficiency() => true;

        private bool HasWeaponProficiencyWithProperty(string property)
        {
            return GetProficientWeapons().Any(w =>
                !string.IsNullOrEmpty(w.Properties) &&
                w.Properties.Contains(property, StringComparison.OrdinalIgnoreCase));
        }

        private bool HasWeaponProficiencyWithDamageType(string damageType)
        {
            return GetProficientWeapons().Any(w =>
                !string.IsNullOrEmpty(w.Type) &&
                w.Type.Contains(damageType, StringComparison.OrdinalIgnoreCase));
        }

        private bool HasProficiencyWithAnyNamedWeapon(params string[] weaponNames)
        {
            var proficient = GetProficientWeapons();
            // Also treat full martial / simple grants
            var tags = GetCharacterProficiencyTags();
            bool allMartial = tags.Any(t => t.Contains("Martial weapon", StringComparison.OrdinalIgnoreCase));
            bool allSimple = tags.Any(t => t.Contains("Simple weapon", StringComparison.OrdinalIgnoreCase));

            foreach (var name in weaponNames)
            {
                if (proficient.Any(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    return true;

                bool isMartial = GameData.MartialWeapons.Any(w =>
                    w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                bool isSimple = GameData.SimpleWeapons.Any(w =>
                    w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (allMartial && isMartial) return true;
                if (allSimple && isSimple) return true;
            }
            return false;
        }

        private bool HasProficientRangedWeapon()
        {
            return GetProficientWeapons().Any(w =>
                (!string.IsNullOrEmpty(w.Range) && w.Range != "-") ||
                (!string.IsNullOrEmpty(w.Properties) &&
                 (w.Properties.Contains("Ammunition", StringComparison.OrdinalIgnoreCase) ||
                  w.Properties.Contains("Thrown", StringComparison.OrdinalIgnoreCase))));
        }

        private bool HasArmorProficiencyCategory(string category)
        {
            // category: light | medium | heavy
            var tags = GetCharacterProficiencyTags();
            if (tags.Any(t => t.Contains("All armor", StringComparison.OrdinalIgnoreCase)))
                return true;
            return tags.Any(t => t.Contains(category, StringComparison.OrdinalIgnoreCase) &&
                                 t.Contains("armor", StringComparison.OrdinalIgnoreCase));
        }

        private bool HasShieldProficiency()
        {
            var tags = GetCharacterProficiencyTags();
            return tags.Any(t => t.Contains("Shield", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// True when the character can cast spells from class levels, a spellcasting subclass
        /// (e.g. Arcane Trickster / Eldritch Knight), or racial innate spells.
        /// Syncs single-class UI subclass onto <see cref="Character.ClassLevels"/> first.
        /// </summary>
        private bool CharacterHasSpellcastingFeature()
        {
            if (raceHasInnateSpellcasting)
                return true;

            // Prefer live class levels (keeps Level-tab + Class-tab subclass picks in sync)
            List<ClassLevelEntry> entries;
            try
            {
                entries = GetActiveClassLevels();
            }
            catch
            {
                entries = CurrentCharacter?.ClassLevels?
                    .Where(e => e != null && e.Levels > 0 && !string.IsNullOrWhiteSpace(e.ClassName))
                    .ToList() ?? new List<ClassLevelEntry>();
            }

            foreach (var e in entries)
            {
                if (e == null || e.Levels <= 0 || string.IsNullOrWhiteSpace(e.ClassName))
                    continue;

                if (GameData.ClassData.TryGetValue(e.ClassName, out var data) && data.Spellcasting)
                    return true;

                // Eldritch Knight / Arcane Trickster (and any progression kind with slots)
                if (SpellProgressionCalculator.IsThirdCasterSubclass(e.ClassName, e.Subclass))
                    return true;

                if (SpellSlotCalculator.GetProgressionKind(e.ClassName, e.Subclass) !=
                    CasterProgressionKind.None)
                    return true;
            }

            // Fallback when ClassLevels is empty: Class-tab combos only
            if (entries.Count == 0 &&
                cmbClass?.SelectedItem is string className &&
                GameData.ClassData.TryGetValue(className, out var cd))
            {
                if (cd.Spellcasting) return true;
                string? sub = GetUiSelectedSubclassName();
                if (SpellProgressionCalculator.IsThirdCasterSubclass(className, sub))
                    return true;
                if (SpellSlotCalculator.GetProgressionKind(className, sub) != CasterProgressionKind.None)
                    return true;
            }

            return false;
        }

        private void ShowFeatSpellChoice(Feat feat)
        {
            // Hide extra combo boxes by default
            cmbFeatStatChoice2.Visibility = Visibility.Collapsed;
            cmbFeatStatChoice3.Visibility = Visibility.Collapsed;

            if (feat.Name == "Fey Touched" || feat.Name == "Shadow Touched")
            {
                // Show and repurpose cmbFeatStatChoice2 as the spell picker
                cmbFeatStatChoice2.Visibility = Visibility.Visible;

                List<string> allowedSchools = feat.Name == "Fey Touched"
                    ? new List<string> { "Enchantment", "Illusion" }
                    : new List<string> { "Illusion", "Necromancy" };

                var filteredSpells = GameData.All1stLevelSpells
                    .Where(s => allowedSchools.Contains(s.School, StringComparer.OrdinalIgnoreCase))
                    .Select(s => s.Name)
                    .OrderBy(n => n)
                    .ToList();

                cmbFeatStatChoice2.ItemsSource = filteredSpells;

                // Optional: Change header dynamically
                // You can add a small TextBlock above it if you want clearer labeling
            }
            else
            {
                cmbFeatStatChoice2.ItemsSource = null;
                featSelectedSpell = "";
            }
        }

        private void MagicInitiateClass_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFeatStatChoice1.SelectedItem is string selectedClass)
            {
                PopulateMagicInitiateChoices(selectedClass);
            }
        }

        /// <summary>True if Fighting Initiate is checked on the Feats tab.</summary>
        private bool IsFightingInitiateSelected()
        {
            return GameData.AllFeats?.Any(f =>
                       f != null &&
                       f.IsSelected &&
                       f.Name.Equals("Fighting Initiate", StringComparison.OrdinalIgnoreCase)) == true;
        }

        /// <summary>
        /// Class fighting styles plus Fighting Initiate style (when that feat is selected).
        /// </summary>
        private IEnumerable<string> GetEffectiveFightingStyles()
        {
            if (CurrentCharacter?.FightingStyles != null)
            {
                foreach (var s in CurrentCharacter.FightingStyles)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        yield return s.Trim();
                }
            }

            if (IsFightingInitiateSelected() &&
                !string.IsNullOrWhiteSpace(CurrentCharacter?.FightingInitiateStyle))
            {
                yield return CurrentCharacter.FightingInitiateStyle.Trim();
            }
        }

        private bool CharacterHasFightingStyle(string styleName)
        {
            if (string.IsNullOrWhiteSpace(styleName)) return false;
            return GetEffectiveFightingStyles()
                .Any(s => s.Equals(styleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Fighter fighting styles available for Fighting Initiate, excluding styles already
        /// taken from class (feat requires a different style if you already have one).
        /// </summary>
        private List<ClassFeatureOption> GetFightingInitiateStyleOptions()
        {
            var already = new HashSet<string>(
                CurrentCharacter?.FightingStyles?
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            return ClassFeatureOptionData.GetFightingStylesForClass("Fighter")
                .Where(o => !already.Contains(o.Name))
                .OrderBy(o => o.Name)
                .ToList();
        }

        private void SetupFightingInitiateChoices()
        {
            lblFeatStatChoiceHeader.Text = "CHOOSE FIGHTING STYLE (FIGHTER LIST)";

            var options = GetFightingInitiateStyleOptions();
            var names = options.Select(o => o.Name).ToList();

            // If the saved pick was filtered out (now owned by class), keep it visible so the user can re-pick
            string saved = CurrentCharacter?.FightingInitiateStyle?.Trim() ?? "";
            if (!string.IsNullOrEmpty(saved) &&
                !names.Contains(saved, StringComparer.OrdinalIgnoreCase) &&
                ClassFeatureOptionData.GetFightingStylesForClass("Fighter")
                    .Any(o => o.Name.Equals(saved, StringComparison.OrdinalIgnoreCase)))
            {
                names.Insert(0, saved);
            }

            cmbFeatStatChoice1.ItemsSource = names;
            cmbFeatStatChoice1.Visibility = Visibility.Visible;
            pnlFeatStatChoices.Visibility = Visibility.Visible;

            int idx = 0;
            if (!string.IsNullOrEmpty(saved))
            {
                int found = names.FindIndex(n => n.Equals(saved, StringComparison.OrdinalIgnoreCase));
                if (found >= 0) idx = found;
            }
            if (names.Count > 0)
                cmbFeatStatChoice1.SelectedIndex = idx;
            else
                ApplyFightingInitiateStyleFromUi(); // clears style text if list empty

            // If SelectedIndex was already this value, SelectionChanged may not fire — re-apply once
            if (names.Count > 0)
                ApplyFightingInitiateStyleFromUi();
        }

        private void EnsureFightingInitiateStyleDefault()
        {
            if (CurrentCharacter == null) return;
            if (!string.IsNullOrWhiteSpace(CurrentCharacter.FightingInitiateStyle)) return;

            var options = GetFightingInitiateStyleOptions();
            if (options.Count > 0)
                CurrentCharacter.FightingInitiateStyle = options[0].Name;
        }

        private void ApplyFightingInitiateStyleFromUi()
        {
            if (CurrentCharacter == null) return;

            string style = cmbFeatStatChoice1.SelectedItem as string ?? "";
            CurrentCharacter.FightingInitiateStyle = style;

            // Show style description under the feat text
            if (!string.IsNullOrEmpty(style))
            {
                var opt = ClassFeatureOptionData.GetFightingStylesForClass("Fighter")
                    .FirstOrDefault(o => o.Name.Equals(style, StringComparison.OrdinalIgnoreCase));
                string desc = opt?.Description ?? "";
                txtFeatDetails.Text = string.IsNullOrEmpty(desc)
                    ? baseFeatDescription
                    : $"{baseFeatDescription}\n\n———\nSelected style: {style}\n{desc}";
            }
            else
            {
                txtFeatDetails.Text = baseFeatDescription;
            }

            // Defense style (and any future style-based rules) need a live AC refresh
            UpdateEquippedAC();
        }

        private void PopulateMagicInitiateChoices(string className)
        {
            if (string.IsNullOrEmpty(className)) return;

            // Cantrips (Combo 2 + Combo 3)
            var cantrips = GameData.ClassCantrips.ContainsKey(className)
                ? GameData.ClassCantrips[className]
                : new List<string>();

            cmbFeatStatChoice2.ItemsSource = cantrips;
            cmbFeatStatChoice3.ItemsSource = cantrips;

            if (cantrips.Any())
            {
                cmbFeatStatChoice2.SelectedIndex = 0;
                cmbFeatStatChoice3.SelectedIndex = 1 < cantrips.Count ? 1 : 0;
            }

            cmbFeatStatChoice2.Visibility = Visibility.Visible;
            cmbFeatStatChoice3.Visibility = Visibility.Visible;

            // 1st Level Spells (Combo 4)
            var spells = GameData.All1stLevelSpells
                .Where(s => s.Classes.Contains(className, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.Name)
                .OrderBy(n => n)
                .ToList();

            cmbFeatStatChoice4.ItemsSource = spells;
            if (spells.Any())
                cmbFeatStatChoice4.SelectedIndex = 0;

            cmbFeatStatChoice4.Visibility = Visibility.Visible;
        }

        private void UpdateFeatSpellPreview(Spell spell)
        {
            if (spell == null)
            {
                brdFeatSpellPreview.Visibility = Visibility.Collapsed;
                return;
            }

            txtFeatSpellDetails.Text = spell.FormatDetails(includeFullText: true);
            brdFeatSpellPreview.Visibility = Visibility.Visible;
        }

        private void FeatCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // The Feat.IsSelected setter already handles Apply/Remove + UI updates.
            // This handler exists mainly as a backup refresh.
            Dispatcher.BeginInvoke(() =>
            {
                UpdateStatDisplays();
                UpdateInitiative();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        public void ApplyFeatBonus(Feat feat)
        {
            if (feat == null) return;

            string name = feat.Name.ToLowerInvariant();

            // Skip dynamic choice feats (they handle their own bonuses)
            if (feat.HasDynamicStatChoice)
                return;

            // === FIGHTING INITIATE ===
            // Style pick lives on Character.FightingInitiateStyle (feat details combo).
            // Ensure a default fighter style if the player selects the feat without opening details.
            if (name == "fighting initiate")
            {
                EnsureFightingInitiateStyleDefault();
                UpdateEquippedAC();
                return;
            }

            // === MOBILE FEAT ===
            if (name == "mobile")
            {
                featSpeedBonus = 10;
                return;
            }

            // === ALERT ===
            if (name == "alert")
            {
                featStatBonuses["Dexterity"] = featStatBonuses.GetValueOrDefault("Dexterity", 0) + 1;
                featInitiativeBonus = 5;
                return;
            }

            // === ACTOR (explicit to avoid double application) ===
            if (name == "actor")
            {
                featStatBonuses["Charisma"] = featStatBonuses.GetValueOrDefault("Charisma", 0) + 1;
                return;
            }

            // === Generic +1 ability score feats (fallback) ===
            string desc = feat.FullDescription?.ToLowerInvariant() ?? "";

            if (desc.Contains("increase your dexterity score by 1"))
                featStatBonuses["Dexterity"] = featStatBonuses.GetValueOrDefault("Dexterity", 0) + 1;
            else if (desc.Contains("increase your strength score by 1"))
                featStatBonuses["Strength"] = featStatBonuses.GetValueOrDefault("Strength", 0) + 1;
            else if (desc.Contains("increase your constitution score by 1"))
                featStatBonuses["Constitution"] = featStatBonuses.GetValueOrDefault("Constitution", 0) + 1;
            else if (desc.Contains("increase your intelligence score by 1"))
                featStatBonuses["Intelligence"] = featStatBonuses.GetValueOrDefault("Intelligence", 0) + 1;
            else if (desc.Contains("increase your wisdom score by 1"))
                featStatBonuses["Wisdom"] = featStatBonuses.GetValueOrDefault("Wisdom", 0) + 1;
            else if (desc.Contains("increase your charisma score by 1"))
                featStatBonuses["Charisma"] = featStatBonuses.GetValueOrDefault("Charisma", 0) + 1;
        }

        private void UpdateFeatSpellsLabel()
        {
            if (lblFeatSpells == null) return;

            if (!string.IsNullOrEmpty(currentFeatSpellSource) && currentFeatSpells.Count > 0)
            {
                string spellsText = string.Join(", ", currentFeatSpells);
                lblFeatSpells.Text = $"Feat spells ({currentFeatSpellSource}): {spellsText}";
                lblFeatSpells.Foreground = AccentGreen;
            }
            else
            {
                lblFeatSpells.Text = "Feat spells: (none yet)";
                lblFeatSpells.Foreground = (Brush)new BrushConverter().ConvertFromString("#AAA");
            }
        }

        public void RemoveFeatBonus(Feat feat)
        {
            if (feat == null) return;

            string name = feat.Name.ToLowerInvariant();

            if (feat.HasDynamicStatChoice)
            {
                if (!string.IsNullOrEmpty(currentFeatAbilityChoice))
                {
                    featStatBonuses[currentFeatAbilityChoice] = Math.Max(0, featStatBonuses.GetValueOrDefault(currentFeatAbilityChoice, 0) - 1);
                    currentFeatAbilityChoice = "";
                }

                string featName = feat.Name?.ToLowerInvariant() ?? "";
                if (featName.Contains("resilient") || !string.IsNullOrEmpty(resilientSaveAbility))
                {
                    resilientSaveAbility = "";
                    UpdateSavingThrows();
                }
                return;
            }

            // === FIGHTING INITIATE ===
            if (name == "fighting initiate")
            {
                if (CurrentCharacter != null)
                    CurrentCharacter.FightingInitiateStyle = "";
                UpdateEquippedAC();
                return;
            }

            // === MOBILE FEAT ===
            if (name == "mobile")
            {
                featSpeedBonus = 0;
                return;
            }

            // === ALERT ===
            if (name == "alert")
            {
                featStatBonuses["Dexterity"] = Math.Max(0, featStatBonuses.GetValueOrDefault("Dexterity", 0) - 1);
                featInitiativeBonus = 0;
                return;
            }

            // === ACTOR ===
            if (name == "actor")
            {
                featStatBonuses["Charisma"] = Math.Max(0, featStatBonuses.GetValueOrDefault("Charisma", 0) - 1);
                return;
            }

            string desc = feat.FullDescription?.ToLowerInvariant() ?? "";

            if (desc.Contains("increase your dexterity score by 1"))
                featStatBonuses["Dexterity"] = Math.Max(0, featStatBonuses.GetValueOrDefault("Dexterity", 0) - 1);
            else if (desc.Contains("increase your strength score by 1"))
                featStatBonuses["Strength"] = Math.Max(0, featStatBonuses.GetValueOrDefault("Strength", 0) - 1);
            else if (desc.Contains("increase your constitution score by 1"))
                featStatBonuses["Constitution"] = Math.Max(0, featStatBonuses.GetValueOrDefault("Constitution", 0) - 1);
            else if (desc.Contains("increase your intelligence score by 1"))
                featStatBonuses["Intelligence"] = Math.Max(0, featStatBonuses.GetValueOrDefault("Intelligence", 0) - 1);
            else if (desc.Contains("increase your wisdom score by 1"))
                featStatBonuses["Wisdom"] = Math.Max(0, featStatBonuses.GetValueOrDefault("Wisdom", 0) - 1);
            else if (desc.Contains("increase your charisma score by 1"))
                featStatBonuses["Charisma"] = Math.Max(0, featStatBonuses.GetValueOrDefault("Charisma", 0) - 1);
        }

        private int GetFinalSpeed()
        {
            int speed = 30;

            // === 1. Base race speed ===
            string selectedRace = cmbRace.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedRace) &&
                GameData.RaceData.TryGetValue(selectedRace, out var raceData))
            {
                speed = raceData.Speed;
            }

            // === 2. Subrace speed override (correct lookup) ===
            string selectedSubrace = cmbSubrace.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedRace) &&
                !string.IsNullOrEmpty(selectedSubrace) &&
                GameData.RaceSubraces.TryGetValue(selectedRace, out var subraceList))
            {
                var matchingSubrace = subraceList
                    .FirstOrDefault(s => s.Name.Equals(selectedSubrace, StringComparison.OrdinalIgnoreCase));

                if (matchingSubrace != null && matchingSubrace.Speed.HasValue)
                {
                    speed = matchingSubrace.Speed.Value;
                }
            }

            // === 3. Mobile feat bonus (+10 ft) ===
            speed += featSpeedBonus;

            return speed;
        }

        public void UpdateInitiative()
        {
            int dexMod = GetModifierFromText(txtDexMod?.Text);
            // Initiative is a Dexterity ability check — Jack of All Trades applies
            bool joat = LevelUpCalculator.HasJackOfAllTrades(GetActiveClassLevels());
            int joatBonus = joat ? proficiencyBonus / 2 : 0;
            int total = dexMod + featInitiativeBonus + joatBonus;
            txtInitiative.Text = total >= 0 ? $"+{total}" : total.ToString();
        }

        private void InitializeSkills()
        {
            allSkills = new ObservableCollection<SkillProficiency>(GameData.CreateAllSkills());
            dgSkills.ItemsSource = allSkills;
        }

        private void UpdateSpellTabVisibility()
        {
            if (tabSpells == null) return;

            // Include full/half casters, third-caster subclasses (Arcane Trickster, Eldritch Knight),
            // and racial innate spellcasting — not only ClassData.Spellcasting on the base class.
            bool hasSpellcasting = CharacterHasSpellcastingFeature();
            tabSpells.Visibility = hasSpellcasting ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateSubclassSpellsLabel()
        {
            if (lblSubclassSpells == null) return;

            var classLevels = GetActiveClassLevels();
            var parts = new List<string>();

            foreach (var entry in classLevels)
            {
                string? subName = GameData.GetEffectiveSubclass(entry);
                if (string.IsNullOrWhiteSpace(subName))
                    continue;

                var grants = SubclassSpellCalculator.GetGrantsUpToLevel(subName, entry.Levels);
                if (grants.Count == 0)
                {
                    // Fallback: legacy level-1 domain/patron tables
                    List<string> legacy = new();
                    if (entry.ClassName.Equals("Cleric", StringComparison.OrdinalIgnoreCase) &&
                        GameData.ClericSubclasses.TryGetValue(subName, out var clericSub))
                        legacy = clericSub.DomainSpells;
                    else if (entry.ClassName.Equals("Warlock", StringComparison.OrdinalIgnoreCase) &&
                             GameData.WarlockSubclasses.TryGetValue(subName, out var warlockSub))
                        legacy = warlockSub.DomainSpells;
                    else if (entry.ClassName.Equals("Sorcerer", StringComparison.OrdinalIgnoreCase) &&
                             GameData.SorcererSubclasses.TryGetValue(subName, out var sorcSub))
                        legacy = sorcSub.AdditionalSpells;

                    if (legacy.Count > 0)
                        parts.Add($"{subName} (class {entry.Levels}): {string.Join(", ", legacy)}");
                    continue;
                }

                var prepared = grants.Where(g => g.Kind == SubclassSpellGrantKind.AlwaysPrepared).ToList();
                var known = grants.Where(g => g.Kind == SubclassSpellGrantKind.AlwaysKnown).ToList();
                var expanded = grants.Where(g => g.Kind == SubclassSpellGrantKind.ExpandedList).ToList();

                var chunks = new List<string>();
                if (prepared.Count > 0)
                    chunks.Add("always prepared: " + string.Join(", ",
                        prepared.Select(g => $"{g.SpellName} (L{g.SpellLevel})")));
                if (known.Count > 0)
                    chunks.Add("always known: " + string.Join(", ",
                        known.Select(g => $"{g.SpellName} (L{g.SpellLevel})")));
                if (expanded.Count > 0)
                    chunks.Add("expanded list: " + string.Join(", ",
                        expanded.Select(g => $"{g.SpellName} (L{g.SpellLevel})")));

                if (chunks.Count > 0)
                    parts.Add($"{subName} @ {entry.ClassName} {entry.Levels}: {string.Join(" | ", chunks)}");
            }

            if (parts.Count > 0)
            {
                lblSubclassSpells.Text = "Subclass spells: " + string.Join("  ||  ", parts);
                lblSubclassSpells.Foreground = AccentGreen;
            }
            else
            {
                lblSubclassSpells.Text = "Subclass spells: (none yet)";
                lblSubclassSpells.Foreground = (Brush)new BrushConverter().ConvertFromString("#AAA");
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ComboBox/ListBox SelectionChanged bubbles to TabControl. Only react when the
            // tab itself changed — otherwise level-row rebuilds re-enter and cancel +/−/Remove.
            if (!ReferenceEquals(e.OriginalSource, MainTabControl))
                return;

            if (MainTabControl.SelectedItem is TabItem tab)
            {
                // === SKILLS TAB ===
                if (tab == tabSkills)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Ensure PB matches current total character level before painting bonuses
                        RefreshProficiencyBonus();
                        UpdateSkillBonuses();
                        UpdateSavingThrows();

                        if (cmbClass?.SelectedItem is string classNameSkills)
                        {
                            UpdateSkillChoices(classNameSkills);
                        }
                        else if (lblClassSkillCounter != null)
                        {
                            lblClassSkillCounter.Text = "Please select a Class first";
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }

                // === LEVEL & MULTICLASS TAB ===
                else if (tab == tabLevelMulticlass)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        RefreshLevelMulticlassTab();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }

                // === FEATS TAB ===
                else if (tab == tabFeats)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        EnsureFeatsLoaded();
                        UpdateFeatSelectionLimitLabel();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }

                // === SPELLS TAB ===
                else if (tab == tabSpells)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Sync levels first so slot-based highest spell level (e.g. Ranger 5 → 2nd)
                        // is correct before the level dropdown and grid filter refresh.
                        SyncCharacterClassFromUi();
                        UpdateFeatSpellsLabel();
                        UpdateRacialSpellsLabel();
                        UpdateSubclassSpellsLabel();

                        if (cmbClass.SelectedItem is string classNameSpells)
                        {
                            if (cantripOptions.Count == 0 || spell1Options.Count == 0)
                                PopulateSpells();
                            else
                            {
                                RebalanceCantripAssignments(BuildPreferredCantripAssignments());
                                RefreshSpellLevelDropdown();
                                ApplyCantripSelectableState();
                                ApplyLeveledSpellSelectableState();
                                UpdateSpellStats();
                                UpdateCantripCounter();
                                UpdateSpellCounter();
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }

                // === SUMMARY & EXPORT TAB ===
                else if (tab == tabSummaryExport)
                {
                    Dispatcher.BeginInvoke(new Action(RefreshExportPreview),
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void PopulateSpells()
        {
            if (cmbClass.SelectedItem is not string className || !GameData.ClassData.ContainsKey(className))
                return;

            SyncCharacterClassFromUi();
            UpdateCantripChoices(className);
            UpdateSpell1Choices(className);
            RefreshSpellLevelDropdown();
            UpdateSpellStats();
        }

        private HashSet<string> GetActiveSpellListClasses()
        {
            var fromLevels = SpellProgressionCalculator.GetSpellListClassNames(GetActiveClassLevels());
            if (fromLevels.Count > 0)
                return fromLevels;

            // Fallback: selected class only
            if (cmbClass.SelectedItem is string className)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { className };

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private bool SpellOnActiveClassList(Spell spell)
        {
            if (spell?.Classes == null) return false;
            var lists = GetActiveSpellListClasses();
            return spell.Classes.Any(c => lists.Contains(c));
        }

        public void UpdateCantripChoices(string className)
        {
            if (!GameData.ClassData.ContainsKey(className)) return;

            // Build master list only once
            if (cantripOptions.Count == 0)
            {
                foreach (var spell in GameData.AllCantrips)
                {
                    var selectable = new SelectableSpell
                    {
                        Name = spell.Name,
                        DamageDice = spell.DamageDice,
                        RollType = spell.RollType,
                        DamageType = spell.DamageType,
                        Description = spell.Description.Length > 80
                            ? spell.Description.Substring(0, 77) + "..."
                            : spell.Description,
                        FullSpell = spell,
                        IsChecked = false
                    };
                    cantripOptions.Add(selectable);
                }

                cantripViewSource.Source = cantripOptions;
                if (dgCantrips != null)
                    dgCantrips.ItemsSource = cantripViewSource.View;
            }

            // Uncheck everything when changing classes
            foreach (var item in cantripOptions)
            {
                item.IsChecked = false;
                item.AssignedClassKey = "";
                item.AssignedClassDisplay = "";
            }

            cantripViewSource.Filter -= CantripFilter;
            cantripViewSource.Filter += CantripFilter;
            cantripViewSource.View?.Refresh();

            ApplyCantripSelectableState();
            if (pnlCantripPreview != null)
                pnlCantripPreview.Visibility = Visibility.Collapsed;

            UpdateCantripHeader();
            UpdateCantripCounter();
        }

        private void CantripFilter(object sender, FilterEventArgs e)
        {
            if (e.Item is SelectableSpell spell)
                e.Accepted = SpellOnActiveClassList(spell.FullSpell);
            else
                e.Accepted = false;
        }

        private void dgCantrips_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCantrips.SelectedItem is SelectableSpell selectedSpell && selectedSpell.FullSpell != null)
            {
                txtCantripPreview.Text = selectedSpell.FullSpell.FormatDetails(includeFullText: true);
                pnlCantripPreview.Visibility = Visibility.Visible;
            }
            else
            {
                pnlCantripPreview.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Loads all leveled spells (1–9) once; filters by class list + selected spell level.
        /// Does not clear checkboxes when only the level filter changes.
        /// </summary>
        public void UpdateSpell1Choices(string className)
        {
            if (!GameData.ClassData.ContainsKey(className)) return;

            // Build master list once from full catalog (levels 1–9)
            if (spell1Options.Count == 0)
            {
                foreach (var spell in GameData.AllSpells.Where(s => s.Level >= 1 && s.Level <= 9)
                             .OrderBy(s => s.Level).ThenBy(s => s.Name))
                {
                    var selectable = new SelectableSpell
                    {
                        Name = spell.Name,
                        DamageDice = spell.DamageDice,
                        RollType = spell.RollType,
                        DamageType = spell.DamageType,
                        Description = spell.Description != null && spell.Description.Length > 80
                            ? spell.Description.Substring(0, 77) + "..."
                            : spell.Description ?? "",
                        FullSpell = spell,
                        IsChecked = false
                    };
                    spell1Options.Add(selectable);
                }

                spell1ViewSource.Source = spell1Options;
                if (dgSpells1 != null)
                    dgSpells1.ItemsSource = spell1ViewSource.View;
            }
            else
            {
                // Class changed: clear selections
                foreach (var item in spell1Options)
                    item.IsChecked = false;
            }

            spell1ViewSource.Filter -= Spell1Filter;
            spell1ViewSource.Filter += Spell1Filter;
            spell1ViewSource.View?.Refresh();

            ApplyLeveledSpellSelectableState();
            UpdateSpellCounter();
        }

        private void Spell1Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not SelectableSpell spell || spell.FullSpell == null)
            {
                e.Accepted = false;
                return;
            }

            e.Accepted = spell.FullSpell.Level == currentSpellLevelFilter &&
                         SpellOnActiveClassList(spell.FullSpell);
        }

        private void cmbSpellLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSpellLevelEvent) return;
            if (cmbSpellLevel?.SelectedItem is not string label) return;

            int level = ParseSpellLevelLabel(label);
            if (level < 1 || level > 9) return;

            currentSpellLevelFilter = level;
            spell1ViewSource.View?.Refresh();
            ApplyLeveledSpellSelectableState();
            UpdateSpellStats();
            UpdateSpellCounter();
        }

        private static int ParseSpellLevelLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return 1;
            // "1st-level", "2nd-level", … or "1", "Level 3"
            var digits = new string(label.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int n) && n >= 1 && n <= 9)
                return n;
            for (int i = 1; i <= 9; i++)
            {
                if (label.StartsWith(i.ToString(), StringComparison.Ordinal))
                    return i;
            }
            return 1;
        }

        private static string FormatSpellLevelLabel(int level) => level switch
        {
            1 => "1st-level",
            2 => "2nd-level",
            3 => "3rd-level",
            _ when level >= 4 && level <= 9 => $"{level}th-level",
            _ => $"Level {level}"
        };

        /// <summary>
        /// Highest spell slot level from current class levels (shared pool + pact magic).
        /// Uses ClassLevels directly so Ranger 5 → 2, etc.
        /// </summary>
        private int ComputeHighestSpellSlotLevel(out List<ClassLevelEntry> levels, out string slotSummary)
        {
            EnsureClassLevelsSeeded();
            SyncCharacterClassFromUi();
            levels = GetActiveClassLevels();

            var result = SpellSlotCalculator.Calculate(levels);
            int highest = 0;
            if (result.SharedSlots != null)
                highest = Math.Max(highest, result.SharedSlots.HighestSlotLevel);
            if (result.PactMagicSlots != null)
                highest = Math.Max(highest, result.PactMagicSlots.HighestSlotLevel);

            // Defensive: if multiclass merge produced empty shared pool but a single class
            // still has its own slots, take the max per-class table as well.
            foreach (var e in levels)
            {
                var single = SpellSlotCalculator.Calculate(e.ClassName, e.Levels, e.Subclass);
                if (single.SharedSlots != null)
                    highest = Math.Max(highest, single.SharedSlots.HighestSlotLevel);
                if (single.PactMagicSlots != null)
                    highest = Math.Max(highest, single.PactMagicSlots.HighestSlotLevel);
            }

            slotSummary = FormatSlotSummary(result);
            return highest;
        }

        /// <summary>
        /// Fills the spell-level dropdown with levels the character has slots for (at least 1st).
        /// Example: Ranger 5 → 1st-level and 2nd-level.
        /// </summary>
        private void RefreshSpellLevelDropdown()
        {
            if (cmbSpellLevel == null) return;

            int highestFromSlots = ComputeHighestSpellSlotLevel(out var levels, out string slotSummary);
            int highest = highestFromSlots > 0 ? highestFromSlots : 1;

            var items = new List<string>();
            for (int lvl = 1; lvl <= highest; lvl++)
                items.Add(FormatSpellLevelLabel(lvl));

            // Keep already-selected higher-level spells browsable if slots later shrink
            if (spell1Options != null)
            {
                foreach (var s in spell1Options.Where(x => x.IsChecked && x.FullSpell != null))
                {
                    int sl = s.FullSpell.Level;
                    if (sl >= 1 && sl <= 9)
                    {
                        string lab = FormatSpellLevelLabel(sl);
                        if (!items.Contains(lab))
                            items.Add(lab);
                    }
                }
            }

            items = items.OrderBy(ParseSpellLevelLabel).ToList();

            _suppressSpellLevelEvent = true;
            try
            {
                // Ensure ItemsSource is our ObservableCollection (never a one-shot List)
                if (!ReferenceEquals(cmbSpellLevel.ItemsSource, spellLevelComboItems))
                    cmbSpellLevel.ItemsSource = spellLevelComboItems;

                string prefer = FormatSpellLevelLabel(currentSpellLevelFilter);

                spellLevelComboItems.Clear();
                foreach (var label in items)
                    spellLevelComboItems.Add(label);

                if (spellLevelComboItems.Contains(prefer))
                {
                    cmbSpellLevel.SelectedItem = prefer;
                }
                else if (spellLevelComboItems.Contains(FormatSpellLevelLabel(1)))
                {
                    cmbSpellLevel.SelectedItem = FormatSpellLevelLabel(1);
                    currentSpellLevelFilter = 1;
                }
                else if (spellLevelComboItems.Count > 0)
                {
                    cmbSpellLevel.SelectedIndex = 0;
                    currentSpellLevelFilter = ParseSpellLevelLabel(spellLevelComboItems[0]);
                }
            }
            finally
            {
                _suppressSpellLevelEvent = false;
            }

            // Surface class levels + slots on the budget line so unlocks are obvious
            if (lblSpellBudget != null)
            {
                var budget = GetSpellBudget();
                var bits = new List<string>();
                string classInfo = levels.Count == 0
                    ? "no class levels"
                    : string.Join(", ", levels.Select(e =>
                        string.IsNullOrWhiteSpace(e.Subclass)
                            ? $"{e.ClassName} {e.Levels}"
                            : $"{e.ClassName} {e.Levels} ({e.Subclass})"));
                bits.Add($"Classes: {classInfo}");
                if (budget.HasPreparedCaster)
                    bits.Add($"Prepared {budget.PreparedMax}");
                if (budget.HasKnownCaster)
                    bits.Add($"Known {budget.KnownMax}");
                bits.Add($"Highest slot: {highestFromSlots}");
                if (!string.IsNullOrWhiteSpace(slotSummary))
                    bits.Add(slotSummary);
                bits.Add("Levels in combo: " + string.Join(", ", spellLevelComboItems));
                lblSpellBudget.Text = "Spell budget: " + string.Join("  |  ", bits);
            }

            spell1ViewSource.View?.Refresh();
        }

        /// <summary>
        /// Single-click toggle for cantrip/spell DataGrid checkboxes.
        /// First click on a DataGrid row normally only focuses the row; this applies the check immediately
        /// (same pattern as skill proficiency checkboxes).
        /// </summary>
        private void SpellGridCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CheckBox cb)
                return;
            if (!cb.IsEnabled)
                return;
            if (cb.DataContext is not SelectableSpell spell)
                return;

            // Toggle immediately; budget/limit enforcement runs in Checked/Unchecked handlers
            bool willCheck = cb.IsChecked != true;
            spell.IsChecked = willCheck;
            e.Handled = true;
        }

        private void Spell1CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var budget = GetSpellBudget();
            CountLeveledSelections(budget, out int preparedSel, out int knownSel);

            bool overPrepared = budget.HasPreparedCaster && preparedSel > budget.PreparedMax;
            bool overKnown = budget.HasKnownCaster && knownSel > budget.KnownMax;
            bool overNone = !budget.HasPreparedCaster && !budget.HasKnownCaster &&
                            (spell1Options?.Count(s => s.IsChecked) ?? 0) > 0;

            if ((overPrepared || overKnown || overNone) &&
                sender is CheckBox cb &&
                cb.DataContext is SelectableSpell spell &&
                spell.IsChecked)
            {
                spell.IsChecked = false;
                return;
            }

            ApplyLeveledSpellSelectableState();
            UpdateSpellStats();
            UpdateSpellCounter();
        }

        private void dgSpells1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSpells1.SelectedItem is SelectableSpell selectedSpell && selectedSpell.FullSpell != null)
            {
                txtCantripPreview.Text = selectedSpell.FullSpell.FormatDetails(includeFullText: true);
                pnlCantripPreview.Visibility = Visibility.Visible;
            }
            else
            {
                pnlCantripPreview.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Attribute a leveled spell to prepared vs known pools for multiclass budgets.
        /// Dual-list spells count as prepared first.
        /// </summary>
        private void ClassifyLeveledSpell(SelectableSpell spell, SpellBudgetSnapshot budget,
            out bool countsPrepared, out bool countsKnown)
        {
            countsPrepared = false;
            countsKnown = false;
            if (spell?.FullSpell?.Classes == null) return;

            bool onPrepared = budget.HasPreparedCaster &&
                budget.PreparedClassNames.Any(pc =>
                    spell.FullSpell.Classes.Contains(pc, StringComparer.OrdinalIgnoreCase));

            // Known-list class names may be "Bard" or "Fighter (Eldritch Knight)" — map to spell list classes
            bool onKnown = false;
            if (budget.HasKnownCaster)
            {
                var knownListClasses = GetKnownSpellListClassNames();
                onKnown = spell.FullSpell.Classes.Any(c => knownListClasses.Contains(c));
            }

            if (onPrepared)
                countsPrepared = true;
            else if (onKnown)
                countsKnown = true;
        }

        private HashSet<string> GetKnownSpellListClassNames()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in GetActiveClassLevels())
            {
                if (!SpellProgressionCalculator.IsKnownCaster(e.ClassName, e.Subclass))
                    continue;
                if (SpellProgressionCalculator.IsThirdCasterSubclass(e.ClassName, e.Subclass))
                    set.Add("Wizard");
                else
                    set.Add(e.ClassName);
            }
            return set;
        }

        private void CountLeveledSelections(SpellBudgetSnapshot budget, out int preparedSel, out int knownSel)
        {
            preparedSel = 0;
            knownSel = 0;
            if (spell1Options == null) return;

            foreach (var s in spell1Options.Where(x => x.IsChecked))
            {
                ClassifyLeveledSpell(s, budget, out bool prep, out bool known);
                if (prep) preparedSel++;
                if (known) knownSel++;
            }
        }

        /// <summary>
        /// Eligible per-class cantrip budgets for a spell (spell must be on that class's list).
        /// </summary>
        private static List<CantripClassBudget> GetEligibleCantripBudgets(
            Spell? spell,
            IReadOnlyList<CantripClassBudget> budgets)
        {
            var result = new List<CantripClassBudget>();
            if (spell?.Classes == null || budgets == null || budgets.Count == 0)
                return result;

            foreach (var b in budgets)
            {
                if (b.Max <= 0) continue;
                if (spell.Classes.Any(c =>
                        c.Equals(b.SpellListClass, StringComparison.OrdinalIgnoreCase)))
                    result.Add(b);
            }
            return result;
        }

        /// <summary>Count currently assigned cantrips per class key (checked items only).</summary>
        private Dictionary<string, int> CountCantripAssignmentsByClass()
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (cantripOptions == null) return counts;

            foreach (var item in cantripOptions.Where(c => c.IsChecked))
            {
                if (string.IsNullOrWhiteSpace(item.AssignedClassKey))
                    continue;
                if (!counts.ContainsKey(item.AssignedClassKey))
                    counts[item.AssignedClassKey] = 0;
                counts[item.AssignedClassKey]++;
            }
            return counts;
        }

        private static int GetAssignedCount(Dictionary<string, int> counts, string key) =>
            counts.TryGetValue(key, out int n) ? n : 0;

        /// <summary>
        /// Pick a class budget with remaining room for this cantrip.
        /// Prefers <paramref name="preferredKey"/> when still valid, else the pool with the most room.
        /// </summary>
        private static CantripClassBudget? PickCantripBudget(
            SelectableSpell spell,
            IReadOnlyList<CantripClassBudget> budgets,
            Dictionary<string, int> counts,
            string? preferredKey = null)
        {
            var eligible = GetEligibleCantripBudgets(spell.FullSpell, budgets);
            if (eligible.Count == 0) return null;

            bool HasRoom(CantripClassBudget b) => GetAssignedCount(counts, b.Key) < b.Max;

            if (!string.IsNullOrWhiteSpace(preferredKey))
            {
                var preferred = eligible.FirstOrDefault(b =>
                    b.Key.Equals(preferredKey, StringComparison.OrdinalIgnoreCase) && HasRoom(b));
                if (preferred != null)
                    return preferred;
            }

            return eligible
                .Where(HasRoom)
                .OrderByDescending(b => b.Max - GetAssignedCount(counts, b.Key))
                .ThenBy(b => b.DisplayName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private void ApplyCantripAssignment(SelectableSpell spell, CantripClassBudget budget)
        {
            spell.AssignedClassKey = budget.Key;
            spell.AssignedClassDisplay = budget.DisplayName;
        }

        private void ClearCantripAssignment(SelectableSpell spell)
        {
            spell.AssignedClassKey = "";
            spell.AssignedClassDisplay = "";
        }

        /// <summary>
        /// Re-assign checked cantrips to class budgets (load / multiclass level changes).
        /// Single-list cantrips are placed first so dual-list ones fill remaining slots.
        /// </summary>
        private void RebalanceCantripAssignments(
            IReadOnlyDictionary<string, string>? preferredAssignments = null)
        {
            if (cantripOptions == null) return;

            var budget = GetSpellBudget();
            var budgets = budget.CantripBudgets ?? Array.Empty<CantripClassBudget>();
            if (budgets.Count == 0)
            {
                foreach (var item in cantripOptions.Where(c => c.IsChecked))
                {
                    item.IsChecked = false;
                    ClearCantripAssignment(item);
                }
                return;
            }

            var checkedItems = cantripOptions.Where(c => c.IsChecked).ToList();
            // Clear counts via temporary wipe of assignments (keep IsChecked)
            foreach (var item in checkedItems)
                ClearCantripAssignment(item);

            var counts = budgets.ToDictionary(b => b.Key, _ => 0, StringComparer.OrdinalIgnoreCase);

            // Prefer fewer eligible lists first (must-place), then prefer saved assignment
            var ordered = checkedItems
                .OrderBy(c => GetEligibleCantripBudgets(c.FullSpell, budgets).Count)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var item in ordered)
            {
                string? preferred = null;
                if (preferredAssignments != null &&
                    preferredAssignments.TryGetValue(item.Name, out var saved) &&
                    !string.IsNullOrWhiteSpace(saved))
                {
                    preferred = saved;
                }

                var pick = PickCantripBudget(item, budgets, counts, preferred);
                if (pick == null)
                {
                    item.IsChecked = false;
                    ClearCantripAssignment(item);
                    continue;
                }

                ApplyCantripAssignment(item, pick);
                counts[pick.Key] = GetAssignedCount(counts, pick.Key) + 1;
            }
        }

        private void ApplyCantripSelectableState()
        {
            if (cantripOptions == null) return;

            var budget = GetSpellBudget();
            var budgets = budget.CantripBudgets ?? Array.Empty<CantripClassBudget>();
            var counts = CountCantripAssignmentsByClass();

            // Single-class / empty budget: fall back to total max
            bool usePerClass = budgets.Count > 0;

            foreach (var item in cantripOptions)
            {
                if (item.IsChecked)
                {
                    item.IsSelectable = true;
                    continue;
                }

                if (!SpellOnActiveClassList(item.FullSpell))
                {
                    item.IsSelectable = false;
                    continue;
                }

                if (!usePerClass)
                {
                    int selected = cantripOptions.Count(s => s.IsChecked);
                    item.IsSelectable = selected < budget.CantripsKnownMax;
                    continue;
                }

                // Selectable if any eligible class still has room
                item.IsSelectable = PickCantripBudget(item, budgets, counts) != null;
            }
        }

        private void ApplyLeveledSpellSelectableState()
        {
            var budget = GetSpellBudget();
            CountLeveledSelections(budget, out int preparedSel, out int knownSel);

            if (spell1Options == null) return;

            bool canAddPrepared = !budget.HasPreparedCaster || preparedSel < budget.PreparedMax;
            bool canAddKnown = !budget.HasKnownCaster || knownSel < budget.KnownMax;
            // If neither caster type, nothing selectable
            bool anyCaster = budget.HasPreparedCaster || budget.HasKnownCaster;

            // Prefer live slot math over budget snapshot so level-ups unlock immediately
            int highest = ComputeHighestSpellSlotLevel(out _, out _);
            if (highest <= 0) highest = 1;

            foreach (var item in spell1Options)
            {
                if (item.FullSpell == null || !SpellOnActiveClassList(item.FullSpell))
                {
                    item.IsSelectable = false;
                    continue;
                }

                if (item.IsChecked)
                {
                    item.IsSelectable = true;
                    continue;
                }

                ClassifyLeveledSpell(item, budget, out bool wouldPrep, out bool wouldKnown);
                bool allowed = false;
                if (wouldPrep && canAddPrepared) allowed = true;
                else if (wouldKnown && canAddKnown) allowed = true;

                // If spell is on active list but classification failed (e.g. only expanded), treat by caster type of primary class
                if (!wouldPrep && !wouldKnown && anyCaster)
                {
                    if (budget.HasKnownCaster && canAddKnown) allowed = true;
                    else if (budget.HasPreparedCaster && canAddPrepared) allowed = true;
                }

                // Only allow spell levels the character has slots for
                if (item.FullSpell.Level > highest)
                    allowed = false;

                item.IsSelectable = allowed;
            }
        }

        private void UpdateSpellStats()
        {
            if (lblSpellStats == null) return;

            var classLevels = GetActiveClassLevels();
            if (classLevels.Count == 0)
            {
                lblSpellStats.Text = "Spellcasting Ability: —";
                return;
            }

            // Build ability / DC / attack for each casting class
            var abilityParts = new List<string>();
            foreach (var e in classLevels)
            {
                if (!GameData.ClassData.TryGetValue(e.ClassName, out var data) || !data.Spellcasting)
                {
                    // Third casters still cast with Int
                    if (SpellProgressionCalculator.IsThirdCasterSubclass(e.ClassName, e.Subclass))
                    {
                        int modEk = CalculateModifier(GetFinalStat("Intelligence"));
                        int pb = CurrentCharacter?.ProficiencyBonus > 0
                            ? CurrentCharacter.ProficiencyBonus
                            : proficiencyBonus;
                        abilityParts.Add($"{e.ClassName}: Int (DC {8 + pb + modEk}, +{pb + modEk})");
                    }
                    continue;
                }

                string ability = data.SpellAbility;
                int mod = CalculateModifier(GetFinalStat(ability));
                int pb2 = CurrentCharacter?.ProficiencyBonus > 0
                    ? CurrentCharacter.ProficiencyBonus
                    : proficiencyBonus;
                int dc = 8 + pb2 + mod;
                int attack = pb2 + mod;
                abilityParts.Add($"{e.ClassName}: {ability} (DC {dc}, +{attack})");
            }

            var slots = SpellSlotCalculator.Calculate(classLevels);
            string slotText = FormatSlotSummary(slots);

            lblSpellStats.Text = abilityParts.Count > 0
                ? string.Join("  |  ", abilityParts) + (string.IsNullOrEmpty(slotText) ? "" : "  ||  " + slotText)
                : "Spellcasting Ability: —";

            UpdateFeatSpellsLabel();
            UpdateSubclassSpellsLabel();
            UpdateRacialSpellsLabel();

            UpdateCantripHeader();

            if (lblSpellHeader != null)
                lblSpellHeader.Text = $"{FormatSpellLevelLabel(currentSpellLevelFilter).ToUpperInvariant()} SPELLS";

            // Keep budget line aligned with class levels (Ranger 5 → highest slot 2, etc.)
            if (lblSpellBudget != null)
            {
                int highestFromSlots = ComputeHighestSpellSlotLevel(out var lvlEntries, out string slotSummary);
                var budget = GetSpellBudget();
                var bits = new List<string>();
                string classInfo = lvlEntries.Count == 0
                    ? "no class levels"
                    : string.Join(", ", lvlEntries.Select(e =>
                        string.IsNullOrWhiteSpace(e.Subclass)
                            ? $"{e.ClassName} {e.Levels}"
                            : $"{e.ClassName} {e.Levels} ({e.Subclass})"));
                bits.Add($"Classes: {classInfo}");
                if (budget.HasPreparedCaster)
                    bits.Add($"Prepared: {budget.PreparedMax} max");
                if (budget.HasKnownCaster)
                    bits.Add($"Known: {budget.KnownMax} max");
                if (!budget.HasPreparedCaster && !budget.HasKnownCaster)
                    bits.Add("No prepared/known spellcasting at current level");
                bits.Add($"Highest slot: {highestFromSlots}");
                if (!string.IsNullOrWhiteSpace(slotSummary))
                    bits.Add(slotSummary);
                lblSpellBudget.Text = "Spell budget: " + string.Join("  |  ", bits);
            }

            UpdateSpellCounter();
        }

        private void UpdateCantripHeader()
        {
            if (lblCantripHeader == null) return;

            var budget = GetSpellBudget();
            var budgets = budget.CantripBudgets ?? Array.Empty<CantripClassBudget>();
            if (budgets.Count == 0)
            {
                lblCantripHeader.Text = "CANTRIPS (0 known)";
                return;
            }

            if (budgets.Count == 1)
            {
                lblCantripHeader.Text = $"CANTRIPS ({budgets[0].Max} known)";
                return;
            }

            // Multiclass: show each class's max
            lblCantripHeader.Text = "CANTRIPS (" +
                string.Join(" · ", budgets.Select(b => $"{b.DisplayName} {b.Max}")) +
                $"; {budget.CantripsKnownMax} total)";
        }

        /// <summary>Per-class selected/max breakdown for the cantrip counter label.</summary>
        private string FormatCantripSelectionStatus()
        {
            var budget = GetSpellBudget();
            var budgets = budget.CantripBudgets ?? Array.Empty<CantripClassBudget>();
            var counts = CountCantripAssignmentsByClass();
            int selected = cantripOptions?.Count(s => s.IsChecked) ?? 0;

            if (budgets.Count == 0)
                return $"Selected: {selected} / {budget.CantripsKnownMax}";

            if (budgets.Count == 1)
            {
                int n = GetAssignedCount(counts, budgets[0].Key);
                // Include unassigned checked (legacy) in the count
                if (selected > n) n = selected;
                return $"Selected: {n} / {budgets[0].Max}";
            }

            var parts = budgets.Select(b =>
            {
                int n = GetAssignedCount(counts, b.Key);
                return $"{b.DisplayName} {n}/{b.Max}";
            });
            return $"Selected: {string.Join(" · ", parts)}  (total {selected}/{budget.CantripsKnownMax})";
        }

        private static string FormatSlotSummary(MulticlassSpellSlotResult slots)
        {
            if (slots == null) return "";
            var parts = new List<string>();
            if (slots.SharedSlots != null && slots.SharedSlots.HighestSlotLevel > 0)
            {
                var by = new List<string>();
                for (int i = 1; i <= 9; i++)
                {
                    int n = slots.SharedSlots.GetSlots(i);
                    if (n > 0) by.Add($"{i}:{n}");
                }
                if (by.Count > 0)
                    parts.Add("Slots " + string.Join(" ", by));
            }
            if (slots.PactMagicSlots != null && slots.PactMagicSlots.HighestSlotLevel > 0)
            {
                int lvl = slots.PactMagicSlots.PactSlotLevel ?? slots.PactMagicSlots.HighestSlotLevel;
                int n = slots.PactMagicSlots.GetSlots(lvl);
                parts.Add($"Pact {n}×L{lvl}");
            }
            return string.Join(" · ", parts);
        }

        public void UpdateCantripCounter()
        {
            if (lblCantripCount == null) return;
            lblCantripCount.Text = FormatCantripSelectionStatus();
        }

        private void CantripCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb || cb.DataContext is not SelectableSpell spell)
            {
                ApplyCantripSelectableState();
                UpdateCantripCounter();
                return;
            }

            var budget = GetSpellBudget();
            var budgets = budget.CantripBudgets ?? Array.Empty<CantripClassBudget>();

            if (!spell.IsChecked)
            {
                ClearCantripAssignment(spell);
                ApplyCantripSelectableState();
                UpdateCantripHeader();
                UpdateCantripCounter();
                return;
            }

            // Checking: must assign to a class that still has room
            var counts = CountCantripAssignmentsByClass();
            // Exclude this spell if it was already counted (re-check edge case)
            if (!string.IsNullOrWhiteSpace(spell.AssignedClassKey) &&
                counts.ContainsKey(spell.AssignedClassKey))
            {
                counts[spell.AssignedClassKey] = Math.Max(0, counts[spell.AssignedClassKey] - 1);
            }

            if (budgets.Count == 0)
            {
                // No cantrip-granting classes
                spell.IsChecked = false;
                ClearCantripAssignment(spell);
                ApplyCantripSelectableState();
                UpdateCantripCounter();
                return;
            }

            var pick = PickCantripBudget(spell, budgets, counts, spell.AssignedClassKey);
            if (pick == null)
            {
                spell.IsChecked = false;
                ClearCantripAssignment(spell);
                ApplyCantripSelectableState();
                UpdateCantripCounter();
                return;
            }

            ApplyCantripAssignment(spell, pick);
            ApplyCantripSelectableState();
            UpdateCantripHeader();
            UpdateCantripCounter();
        }

        public void UpdateSpellCounter()
        {
            if (lblSpellCount == null) return;

            var budget = GetSpellBudget();
            CountLeveledSelections(budget, out int preparedSel, out int knownSel);

            int thisLevel = spell1Options?.Count(s =>
                s.IsChecked && s.FullSpell != null && s.FullSpell.Level == currentSpellLevelFilter) ?? 0;

            lblSpellCount.Text = $"Selected this level: {thisLevel}";

            if (lblSpellTotalCount != null)
            {
                var parts = new List<string>();
                if (budget.HasPreparedCaster)
                    parts.Add($"Prepared {preparedSel} / {budget.PreparedMax}");
                if (budget.HasKnownCaster)
                    parts.Add($"Known {knownSel} / {budget.KnownMax}");
                if (parts.Count == 0)
                    parts.Add("No spell budget");
                lblSpellTotalCount.Text = "Total: " + string.Join("  |  ", parts);
            }
        }

        private int CalculateModifier(int score)
        {
            // Official 5e formula: floor( (score - 10) / 2 )
            return (int)Math.Floor((score - 10) / 2.0);
        }

        /// <summary>
        /// Max leveled spells the character may choose (prepared + known budgets combined for limits).
        /// Kept for any legacy call sites.
        /// </summary>
        private int GetMax1stLevelSpells(string className, ClassData classData)
        {
            var budget = GetSpellBudget();
            if (budget.HasPreparedCaster && !budget.HasKnownCaster)
                return budget.PreparedMax;
            if (budget.HasKnownCaster && !budget.HasPreparedCaster)
                return budget.KnownMax;
            return budget.PreparedMax + budget.KnownMax;
        }

        private int GetFinalStat(string ability)
        {
            // Build current final stats from UI once (null-safe during early load / rebuild)
            static int ParseOr(string? text, int fallback) =>
                !string.IsNullOrWhiteSpace(text) && int.TryParse(text, out int v) ? v : fallback;

            var finalStats = new Dictionary<string, int>
            {
                ["Strength"] = ParseOr(txtStrFinal?.Text, 10),
                ["Dexterity"] = ParseOr(txtDexFinal?.Text, 10),
                ["Constitution"] = ParseOr(txtConFinal?.Text, 10),
                ["Intelligence"] = ParseOr(txtIntFinal?.Text, 10),
                ["Wisdom"] = ParseOr(txtWisFinal?.Text, 10),
                ["Charisma"] = ParseOr(txtChaFinal?.Text, 10)
            };

            return GameData.GetAbilityScore(ability, finalStats);
        }

        private void PopulateAbilityScores()
        {
            CurrentCharacter.AbilityScores.Strength.Base = int.TryParse(txtStrBase.Text, out int sb) ? sb : 8;
            CurrentCharacter.AbilityScores.Strength.Racial = GetRacialBonus("Strength");
            CurrentCharacter.AbilityScores.Strength.Feat = featStatBonuses.GetValueOrDefault("Strength", 0);
            CurrentCharacter.AbilityScores.Strength.Final = int.TryParse(txtStrFinal.Text, out int sf) ? sf : 8;
            CurrentCharacter.AbilityScores.Strength.Modifier = GetModifierFromText(txtStrMod.Text);

            CurrentCharacter.AbilityScores.Dexterity.Base = int.TryParse(txtDexBase.Text, out int db) ? db : 8;
            CurrentCharacter.AbilityScores.Dexterity.Racial = GetRacialBonus("Dexterity");
            CurrentCharacter.AbilityScores.Dexterity.Feat = featStatBonuses.GetValueOrDefault("Dexterity", 0);
            CurrentCharacter.AbilityScores.Dexterity.Final = int.TryParse(txtDexFinal.Text, out int df) ? df : 8;
            CurrentCharacter.AbilityScores.Dexterity.Modifier = GetModifierFromText(txtDexMod.Text);

            CurrentCharacter.AbilityScores.Constitution.Base = int.TryParse(txtConBase.Text, out int cb) ? cb : 8;
            CurrentCharacter.AbilityScores.Constitution.Racial = GetRacialBonus("Constitution");
            CurrentCharacter.AbilityScores.Constitution.Feat = featStatBonuses.GetValueOrDefault("Constitution", 0);
            CurrentCharacter.AbilityScores.Constitution.Final = int.TryParse(txtConFinal.Text, out int cf) ? cf : 8;
            CurrentCharacter.AbilityScores.Constitution.Modifier = GetModifierFromText(txtConMod.Text);

            CurrentCharacter.AbilityScores.Intelligence.Base = int.TryParse(txtIntBase.Text, out int ib) ? ib : 8;
            CurrentCharacter.AbilityScores.Intelligence.Racial = GetRacialBonus("Intelligence");
            CurrentCharacter.AbilityScores.Intelligence.Feat = featStatBonuses.GetValueOrDefault("Intelligence", 0);
            CurrentCharacter.AbilityScores.Intelligence.Final = int.TryParse(txtIntFinal.Text, out int ifinal) ? ifinal : 8;
            CurrentCharacter.AbilityScores.Intelligence.Modifier = GetModifierFromText(txtIntMod.Text);

            CurrentCharacter.AbilityScores.Wisdom.Base = int.TryParse(txtWisBase.Text, out int wb) ? wb : 8;
            CurrentCharacter.AbilityScores.Wisdom.Racial = GetRacialBonus("Wisdom");
            CurrentCharacter.AbilityScores.Wisdom.Feat = featStatBonuses.GetValueOrDefault("Wisdom", 0);
            CurrentCharacter.AbilityScores.Wisdom.Final = int.TryParse(txtWisFinal.Text, out int wf) ? wf : 8;
            CurrentCharacter.AbilityScores.Wisdom.Modifier = GetModifierFromText(txtWisMod.Text);

            CurrentCharacter.AbilityScores.Charisma.Base = int.TryParse(txtChaBase.Text, out int chb) ? chb : 8;
            CurrentCharacter.AbilityScores.Charisma.Racial = GetRacialBonus("Charisma");
            CurrentCharacter.AbilityScores.Charisma.Feat = featStatBonuses.GetValueOrDefault("Charisma", 0);
            CurrentCharacter.AbilityScores.Charisma.Final = int.TryParse(txtChaFinal.Text, out int chf) ? chf : 8;
            CurrentCharacter.AbilityScores.Charisma.Modifier = GetModifierFromText(txtChaMod.Text);
        }

        private List<SavingThrow> GetSavingThrows()
        {
            var result = new List<SavingThrow>();

            var abilities = new[] { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };

            List<string> classProficientSaves = new();

            if (!string.IsNullOrEmpty(CurrentCharacter.Class) &&
                GameData.ClassData.TryGetValue(CurrentCharacter.Class, out var classData))
            {
                classProficientSaves = classData.SavingThrowProficiencies ?? new List<string>();
            }

            RefreshProficiencyBonus();
            int prof = proficiencyBonus > 0 ? proficiencyBonus : Math.Max(2, CurrentCharacter.ProficiencyBonus);

            foreach (var ability in abilities)
            {
                int modifier = ability switch
                {
                    "Strength" => CurrentCharacter.AbilityScores.Strength.Modifier,
                    "Dexterity" => CurrentCharacter.AbilityScores.Dexterity.Modifier,
                    "Constitution" => CurrentCharacter.AbilityScores.Constitution.Modifier,
                    "Intelligence" => CurrentCharacter.AbilityScores.Intelligence.Modifier,
                    "Wisdom" => CurrentCharacter.AbilityScores.Wisdom.Modifier,
                    "Charisma" => CurrentCharacter.AbilityScores.Charisma.Modifier,
                    _ => 0
                };

                bool isProficient = HasSaveProficiency(ability, classProficientSaves);

                result.Add(new SavingThrow
                {
                    Name = ability,
                    Bonus = modifier + (isProficient ? prof : 0),
                    IsProficient = isProficient
                });
            }

            return result;
        }

        private int GetRacialBonus(string ability)
        {
            return racialBonuses.TryGetValue(ability, out int bonus) ? bonus : 0;
        }

        private List<SkillEntry> BuildFullSkillListForPDF()
        {
            var skills = new List<SkillEntry>();

            var skillDefinitions = new[]
            {
        ("Acrobatics", "Dex"), ("Animal Handling", "Wis"), ("Arcana", "Int"),
        ("Athletics", "Str"), ("Deception", "Cha"), ("History", "Int"),
        ("Insight", "Wis"), ("Intimidation", "Cha"), ("Investigation", "Int"),
        ("Medicine", "Wis"), ("Nature", "Int"), ("Perception", "Wis"),
        ("Performance", "Cha"), ("Persuasion", "Cha"), ("Religion", "Int"),
        ("Sleight of Hand", "Dex"), ("Stealth", "Dex"), ("Survival", "Wis")
    };

            int prof = CurrentCharacter.ProficiencyBonus > 0
                ? CurrentCharacter.ProficiencyBonus
                : proficiencyBonus;
            bool joat = LevelUpCalculator.HasJackOfAllTrades(GetActiveClassLevels());
            var expert = new HashSet<string>(
                (CurrentCharacter.Skills ?? new List<SkillEntry>())
                    .Where(s => s.IsExpertise)
                    .Select(s => s.Name),
                StringComparer.OrdinalIgnoreCase);
            var proficient = new HashSet<string>(
                (CurrentCharacter.Skills ?? new List<SkillEntry>()).Select(s => s.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (var (name, ability) in skillDefinitions)
            {
                int mod = ability switch
                {
                    "Str" => CurrentCharacter.AbilityScores.Strength.Modifier,
                    "Dex" => CurrentCharacter.AbilityScores.Dexterity.Modifier,
                    "Con" => CurrentCharacter.AbilityScores.Constitution.Modifier,
                    "Int" => CurrentCharacter.AbilityScores.Intelligence.Modifier,
                    "Wis" => CurrentCharacter.AbilityScores.Wisdom.Modifier,
                    "Cha" => CurrentCharacter.AbilityScores.Charisma.Modifier,
                    _ => 0
                };

                bool isProficient = proficient.Contains(name);
                bool isExpertise = isProficient && expert.Contains(name);

                skills.Add(new SkillEntry
                {
                    Name = name,
                    Ability = ability,
                    IsProficient = isProficient,
                    IsExpertise = isExpertise,
                    Bonus = LevelUpCalculator.ComputeSkillBonus(mod, prof, isProficient, isExpertise, joat)
                });
            }

            return skills.OrderBy(s => s.Name).ToList();
        }

        private List<string> GetFormattedEquippedWeapons()
        {
            var result = new List<string>();
            if (CurrentCharacter?.Equipment == null) return result;

            int strMod = CurrentCharacter.AbilityScores.Strength.Modifier;
            int dexMod = CurrentCharacter.AbilityScores.Dexterity.Modifier;
            int prof = CurrentCharacter.ProficiencyBonus;

            var allWeapons = GameData.SimpleWeapons
                .Concat(GameData.MartialWeapons)
                .ToList();

            foreach (var item in CurrentCharacter.Equipment)
            {
                // === STRICTER MATCHING: whole word or exact match only ===
                var weapon = allWeapons.FirstOrDefault(w =>
                {
                    string name = w.Name;
                    string lowerItem = item.ToLowerInvariant();
                    string lowerName = name.ToLowerInvariant();

                    // Exact match or whole word match (avoids "signet" matching "Net")
                    return lowerItem == lowerName ||
                           lowerItem.StartsWith(lowerName + " ") ||
                           lowerItem.Contains(" " + lowerName + " ") ||
                           lowerItem.EndsWith(" " + lowerName);
                });

                if (weapon == null) continue;

                bool hasFinesse = weapon.Properties.Contains("Finesse", StringComparison.OrdinalIgnoreCase);
                bool isRanged = !string.IsNullOrWhiteSpace(weapon.Range) && weapon.Range != "-";

                int attackMod;
                int damageMod;

                if (isRanged)
                {
                    attackMod = prof + dexMod;
                    damageMod = dexMod;
                }
                else if (hasFinesse)
                {
                    int bestMod = Math.Max(strMod, dexMod);
                    attackMod = prof + bestMod;
                    damageMod = bestMod;
                }
                else
                {
                    attackMod = prof + strMod;
                    damageMod = strMod;
                }

                string attackStr = attackMod >= 0 ? $"+{attackMod}" : attackMod.ToString();
                string damageStr = $"{weapon.Damage} {(damageMod >= 0 ? "+" : "")}{damageMod}";

                string line = $"{weapon.Name} | Attack {attackStr} | Damage {damageStr} | {weapon.Type} | {weapon.Properties}";
                result.Add(line);
            }

            return result;
        }

        private string Slugify(string spellName)
        {
            return spellName
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace(",", "")
                .Replace("(", "")
                .Replace(")", "");
        }

        /// <summary>
        /// Shared extras passed into the official 5e sheet export (and the summary preview).
        /// </summary>
        private CharacterSheetExporter.ExportExtras BuildOfficialSheetExtras()
        {
            var extras = new CharacterSheetExporter.ExportExtras
            {
                Skills = BuildFullSkillListForPDF(),
                SavingThrows = GetSavingThrows(),
                Weapons = GetFormattedEquippedWeapons()
                    .Select(line =>
                    {
                        // "Name | Attack +X | Damage YdZ+N | ..."
                        var parts = line.Split('|').Select(p => p.Trim()).ToArray();
                        return new CharacterSheetExporter.WeaponAttackLine
                        {
                            Name = parts.Length > 0 ? parts[0] : "",
                            AttackBonus = parts.Length > 1
                                ? parts[1].Replace("Attack", "", StringComparison.OrdinalIgnoreCase).Trim()
                                : "",
                            Damage = parts.Length > 2
                                ? parts[2].Replace("Damage", "", StringComparison.OrdinalIgnoreCase).Trim()
                                : ""
                        };
                    }).ToList(),
                // Total hit dice from all class levels, e.g. 5d10 or 5d10/3d8
                HitDice = LevelUpCalculator.FormatHitDicePool(
                    LevelUpCalculator.GetHitDicePool(GetActiveClassLevels()))
                // Spell slots (1st–9th) and page-3 spell lists are derived inside
                // CharacterSheetExporter from ClassLevels + selected/subclass spells.
            };
            if (string.IsNullOrWhiteSpace(extras.HitDice) || extras.HitDice == "—")
                extras.HitDice = "1d8";
            return extras;
        }

        /// <summary>
        /// Rebuilds the Summary &amp; Export tab preview using the same values as the 5e fillable PDF.
        /// </summary>
        private static SolidColorBrush PreviewBrush(string hex) =>
            new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(hex));

        private void RefreshExportPreview()
        {
            if (pnlExportPreview == null)
                return;

            try
            {
                // Pull latest UI state into CurrentCharacter (same as export)
                AutoSaveCharacterToJson();

                if (CurrentCharacter == null)
                {
                    pnlExportPreview.Children.Clear();
                    pnlExportPreview.Children.Add(new TextBlock
                    {
                        Text = "No character loaded yet.",
                        Foreground = PreviewBrush("#AAA"),
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap
                    });
                    return;
                }

                var extras = BuildOfficialSheetExtras();
                var preview = CharacterSheetExporter.BuildSheetPreview(CurrentCharacter, extras);
                RenderExportPreview(preview);
            }
            catch (Exception ex)
            {
                pnlExportPreview.Children.Clear();
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = "Could not build preview:\n" + ex.Message,
                    Foreground = PreviewBrush("#E88"),
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        private void RenderExportPreview(CharacterSheetExporter.SheetPreview p)
        {
            pnlExportPreview.Children.Clear();

            var accent = ThemeBrush("Nemo.Brush.Accent", "#7CFC00");
            var muted = ThemeBrush("Nemo.Brush.TextMuted", "#AAAAAA");
            var bright = ThemeBrush("Nemo.Brush.TextPrimary", "#F0F0F0");
            var cyan = ThemeBrush("Nemo.Brush.Info", "#9CDCFE");
            var gold = ThemeBrush("Nemo.Brush.Warn", "#E8C36A");

            // ── Identity header ──
            string title = string.IsNullOrWhiteSpace(p.CharacterName) ? "(Unnamed Character)" : p.CharacterName;
            pnlExportPreview.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = accent,
                Margin = new Thickness(0, 0, 0, 2)
            });

            if (!string.IsNullOrWhiteSpace(p.PlayerName))
            {
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = "Player: " + p.PlayerName,
                    FontSize = 12,
                    Foreground = muted,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            var identityLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.ClassLevel))
                identityLines.Add(p.ClassLevel);
            if (!string.IsNullOrWhiteSpace(p.Race))
                identityLines.Add(p.Race);
            if (!string.IsNullOrWhiteSpace(p.Background))
                identityLines.Add(p.Background);
            if (identityLines.Count > 0)
            {
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = string.Join("  •  ", identityLines),
                    FontSize = 14,
                    Foreground = cyan,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }

            if (!string.IsNullOrWhiteSpace(p.Feat))
            {
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = "Feat: " + p.Feat,
                    FontSize = 13,
                    Foreground = gold,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }
            else
            {
                pnlExportPreview.Children.Add(new Border { Height = 6 });
            }

            // ── Combat strip ──
            AddPreviewSectionHeader(pnlExportPreview, "COMBAT STATS", accent);
            var combatGrid = new UniformGrid { Columns = 6, Margin = new Thickness(0, 0, 0, 12) };
            void AddCombatCell(string label, string value)
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, 8, 6) };
                sp.Children.Add(new TextBlock
                {
                    Text = label,
                    FontSize = 11,
                    Foreground = muted,
                    FontWeight = FontWeights.SemiBold
                });
                sp.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = bright
                });
                combatGrid.Children.Add(sp);
            }
            AddCombatCell("Armor Class", p.ArmorClass);
            AddCombatCell("Initiative", p.Initiative);
            AddCombatCell("Speed", p.Speed);
            AddCombatCell("Hit Points", p.HitPoints);
            AddCombatCell("Hit Dice", p.HitDice);
            AddCombatCell("Proficiency", p.ProficiencyBonus);
            pnlExportPreview.Children.Add(combatGrid);

            // Passive Perception under combat
            pnlExportPreview.Children.Add(new TextBlock
            {
                Text = $"Passive Perception: {p.PassivePerception}",
                FontSize = 13,
                Foreground = bright,
                Margin = new Thickness(0, -6, 0, 12)
            });

            // ── Ability scores ──
            AddPreviewSectionHeader(pnlExportPreview, "ABILITY SCORES", accent);
            var abilityGrid = new UniformGrid { Columns = 6, Margin = new Thickness(0, 0, 0, 14) };
            foreach (var ab in p.AbilityScores)
            {
                var box = new Border
                {
                    Background = PreviewBrush("#2A2A2A"),
                    BorderBrush = PreviewBrush("#444"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 6, 0)
                };
                var sp = new StackPanel { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
                sp.Children.Add(new TextBlock
                {
                    Text = ab.Abbreviation,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = cyan,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                });
                sp.Children.Add(new TextBlock
                {
                    Text = ab.Score.ToString(),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = bright,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                });
                sp.Children.Add(new TextBlock
                {
                    Text = ab.Modifier,
                    FontSize = 13,
                    Foreground = accent,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                });
                box.Child = sp;
                abilityGrid.Children.Add(box);
            }
            pnlExportPreview.Children.Add(abilityGrid);

            // ── Saving throws ──
            AddPreviewSectionHeader(pnlExportPreview, "SAVING THROWS", accent);
            var saveParts = p.SavingThrows.Select(s =>
            {
                string mark = s.IsProficient ? "●" : "○";
                return $"{mark} {s.Name} {s.Bonus}";
            });
            pnlExportPreview.Children.Add(new TextBlock
            {
                Text = string.Join("   ", saveParts),
                FontSize = 13,
                Foreground = bright,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            // ── Skills (proficient) ──
            AddPreviewSectionHeader(pnlExportPreview, "SKILL PROFICIENCIES", accent);
            if (p.ProficientSkills.Count == 0)
            {
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = "None selected",
                    FontSize = 13,
                    Foreground = muted,
                    Margin = new Thickness(0, 0, 0, 12)
                });
            }
            else
            {
                string skillsLine = string.Join(", ", p.ProficientSkills.Select(s =>
                    s.IsExpertise ? $"{s.Name} {s.Bonus} (expertise)" : $"{s.Name} {s.Bonus}"));
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = skillsLine,
                    FontSize = 13,
                    Foreground = bright,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
            }

            // ── Attacks ──
            AddPreviewSectionHeader(pnlExportPreview, "ATTACKS", accent);
            if (p.Weapons.Count == 0)
            {
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = "No weapons equipped",
                    FontSize = 13,
                    Foreground = muted,
                    Margin = new Thickness(0, 0, 0, 12)
                });
            }
            else
            {
                foreach (var w in p.Weapons)
                {
                    pnlExportPreview.Children.Add(new TextBlock
                    {
                        Text = $"{w.Name}   {w.AttackBonus}   {w.Damage}",
                        FontSize = 13,
                        Foreground = bright,
                        Margin = new Thickness(0, 0, 0, 2)
                    });
                }
                pnlExportPreview.Children.Add(new Border { Height = 10 });
            }

            // ── Spellcasting ──
            if (p.HasSpellcasting)
            {
                AddPreviewSectionHeader(pnlExportPreview, "SPELLCASTING", accent);
                var spellHeader = new List<string>();
                if (!string.IsNullOrWhiteSpace(p.SpellcastingClass))
                    spellHeader.Add(p.SpellcastingClass);
                if (!string.IsNullOrWhiteSpace(p.SpellcastingAbility))
                    spellHeader.Add(p.SpellcastingAbility);
                if (!string.IsNullOrWhiteSpace(p.SpellSaveDC))
                    spellHeader.Add("DC " + p.SpellSaveDC);
                if (!string.IsNullOrWhiteSpace(p.SpellAttackBonus))
                    spellHeader.Add("Atk " + p.SpellAttackBonus);
                if (spellHeader.Count > 0)
                {
                    pnlExportPreview.Children.Add(new TextBlock
                    {
                        Text = string.Join("  •  ", spellHeader),
                        FontSize = 13,
                        Foreground = cyan,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
                if (!string.IsNullOrWhiteSpace(p.SpellSlotsSummary) && p.SpellSlotsSummary != "—")
                {
                    pnlExportPreview.Children.Add(new TextBlock
                    {
                        Text = "Slots: " + p.SpellSlotsSummary,
                        FontSize = 13,
                        Foreground = bright,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
                if (p.Cantrips.Count > 0)
                {
                    pnlExportPreview.Children.Add(new TextBlock
                    {
                        Text = "Cantrips: " + string.Join(", ", p.Cantrips),
                        FontSize = 13,
                        Foreground = bright,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
                if (p.LeveledSpells.Count > 0)
                {
                    // Keep the preview compact — show first ~12, then a count of remaining
                    const int maxShow = 12;
                    var shown = p.LeveledSpells.Take(maxShow).ToList();
                    string spellsText = string.Join("  |  ", shown);
                    if (p.LeveledSpells.Count > maxShow)
                        spellsText += $"  … (+{p.LeveledSpells.Count - maxShow} more)";
                    pnlExportPreview.Children.Add(new TextBlock
                    {
                        Text = "Spells: " + spellsText,
                        FontSize = 12,
                        Foreground = bright,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
                pnlExportPreview.Children.Add(new Border { Height = 8 });
            }

            // ── Other selections ──
            if (p.ExtraSelections.Count > 0)
            {
                AddPreviewSectionHeader(pnlExportPreview, "FEATURE SELECTIONS", accent);
                foreach (var sel in p.ExtraSelections)
                {
                    pnlExportPreview.Children.Add(new TextBlock
                    {
                        Text = "• " + sel,
                        FontSize = 13,
                        Foreground = bright,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 2)
                    });
                }
                pnlExportPreview.Children.Add(new Border { Height = 8 });
            }

            // ── Proficiencies & languages ──
            if (!string.IsNullOrWhiteSpace(p.ProficienciesAndLanguages))
            {
                AddPreviewSectionHeader(pnlExportPreview, "PROFICIENCIES & LANGUAGES", accent);
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = p.ProficienciesAndLanguages.Replace("\r\n", "\n").Trim(),
                    FontSize = 13,
                    Foreground = bright,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
            }

            // ── Equipment (compact) ──
            if (p.Equipment.Count > 0)
            {
                AddPreviewSectionHeader(pnlExportPreview, "EQUIPMENT", accent);
                const int maxEq = 20;
                string eqText = string.Join(", ", p.Equipment.Take(maxEq));
                if (p.Equipment.Count > maxEq)
                    eqText += $" … (+{p.Equipment.Count - maxEq} more)";
                pnlExportPreview.Children.Add(new TextBlock
                {
                    Text = eqText,
                    FontSize = 12,
                    Foreground = bright,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }
        }

        private static void AddPreviewSectionHeader(Panel parent, string title, Brush accent)
        {
            parent.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = accent,
                Margin = new Thickness(0, 2, 0, 6)
            });
            parent.Children.Add(new Border
            {
                Height = 1,
                Background = PreviewBrush("#3A3A3A"),
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        /// <summary>
        /// Fills the official 5e multi-page fillable character sheet PDF with the current character
        /// (reverse of Load Character from PDF).
        /// </summary>
        private void ExportOfficialSheet_Click(object sender, RoutedEventArgs e)
        {
            AutoSaveCharacterToJson();

            if (CurrentCharacter == null)
            {
                MessageBox.Show("Please save the character first (click Save Character).");
                return;
            }

            if (CharacterSheetExporter.FindTemplatePath() == null)
            {
                MessageBox.Show(
                    "Could not find the official sheet template (5E_CharacterSheet_Fillable.pdf).\n" +
                    "Expected location: Templates\\ folder next to the application.",
                    "Template Missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string safeName = string.Join("_", (CurrentCharacter.Name ?? "Character")
                .Split(System.IO.Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "Character";

            var saveDlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{safeName}_5e_CharacterSheet.pdf",
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Export Official 5e Character Sheet",
                DefaultExt = ".pdf"
            };

            if (saveDlg.ShowDialog() != true) return;

            try
            {
                var extras = BuildOfficialSheetExtras();
                CharacterSheetExporter.ExportToFile(CurrentCharacter, saveDlg.FileName, extras);

                MessageBox.Show(
                    $"Official 5e character sheet exported!\n\n{saveDlg.FileName}\n\n" +
                    "The PDF remains fillable so you can edit fields in any PDF reader.",
                    "Export Complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting official sheet:\n{ex.Message}", "Export Error");
            }
        }

        // Legacy custom fillable builder kept for reference / future use (no longer wired to UI).
        private void ExportFillablePDF_Click(object sender, RoutedEventArgs e)
        {
            ExportOfficialSheet_Click(sender, e);
            return;

#pragma warning disable CS0162
            // === Ensure we have the latest character data (same as regular PDF export) ===
            AutoSaveCharacterToJson();

            if (CurrentCharacter == null)
            {
                MessageBox.Show("Please save the character first (click Save Character).");
                return;
            }

            var saveDlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{CurrentCharacter.Name}_Fillable.pdf",
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Export Fillable Character Sheet"
            };

            if (saveDlg.ShowDialog() != true) return;

            try
            {
                using (var writer = new iText.Kernel.Pdf.PdfWriter(saveDlg.FileName))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                {
                    var currentPage = pdf.AddNewPage(iText.Kernel.Geom.PageSize.LETTER);
                    var form = iText.Forms.PdfAcroForm.GetAcroForm(pdf, true);

                    pdf.GetDocumentInfo().SetTitle($"{CurrentCharacter.Name} - D&D 5e Character Sheet (Fillable)");
                    pdf.GetDocumentInfo().SetAuthor("Nemo Character Creator");

                    float y = 725;   // ~3 lines of top margin (~55 points) for better visual balance and page fit
                    float left = 50;
                    float fieldHeight = 15;
                    float smallFieldHeight = 14;

                    // ========== HEADER ==========
                    DrawLabel(currentPage, "Character Name:", left, y + 3);
                    CreateTextField(form, currentPage, "CharacterName", CurrentCharacter.Name ?? "", left + 100, y, 190, fieldHeight);

                    DrawLabel(currentPage, "Player Name:", left + 310, y + 3);
                    CreateTextField(form, currentPage, "PlayerName", CurrentCharacter.PlayerName ?? "", left + 395, y, 145, fieldHeight);
                    y -= 20;

                    string displayClass = CurrentCharacter.Class;
                    if (displayClass != null && displayClass.Contains("("))
                        displayClass = displayClass.Substring(0, displayClass.IndexOf("(")).Trim();

                    DrawLabel(currentPage, "Class:", left, y + 3);
                    CreateTextField(form, currentPage, "Class", displayClass ?? "", left + 38, y, 95, fieldHeight);

                    DrawLabel(currentPage, "Level:", left + 145, y + 3);
                    CreateTextField(form, currentPage, "Level", "1", left + 178, y, 30, fieldHeight);

                    DrawLabel(currentPage, "Race:", left + 220, y + 3);
                    CreateTextField(form, currentPage, "Race", CurrentCharacter.Race ?? "", left + 250, y, 100, fieldHeight);

                    DrawLabel(currentPage, "Background:", left + 365, y + 3);
                    CreateTextField(form, currentPage, "Background", CurrentCharacter.Background ?? "", left + 430, y, 110, fieldHeight);
                    y -= 22;

                    // ========== COMBAT ==========
                    DrawSectionHeader(currentPage, "COMBAT", left, ref y);

                    DrawLabel(currentPage, "HP:", left, y + 3);
                    CreateTextField(form, currentPage, "HitPoints", CurrentCharacter.HitPoints.ToString(), left + 22, y, 38, fieldHeight);

                    DrawLabel(currentPage, "AC:", left + 70, y + 3);
                    string acDisplay = !string.IsNullOrWhiteSpace(CurrentCharacter.EquippedACDisplay)
                        ? CurrentCharacter.EquippedACDisplay
                        : CurrentCharacter.ArmorClass.ToString();
                    CreateTextField(form, currentPage, "ArmorClass", acDisplay, left + 92, y, 200, fieldHeight);

                    DrawLabel(currentPage, "Initiative:", left + 140, y + 3);
                    CreateTextField(form, currentPage, "Initiative", CurrentCharacter.Initiative >= 0 ? $"+{CurrentCharacter.Initiative}" : CurrentCharacter.Initiative.ToString(), left + 195, y, 42, fieldHeight);

                    DrawLabel(currentPage, "Speed:", left + 248, y + 3);
                    CreateTextField(form, currentPage, "Speed", CurrentCharacter.Speed.ToString() + " ft", left + 285, y, 50, fieldHeight);

                    DrawLabel(currentPage, "Prof Bonus:", left + 348, y + 3);
                    CreateTextField(form, currentPage, "ProficiencyBonus", "+" + CurrentCharacter.ProficiencyBonus, left + 415, y, 40, fieldHeight);
                    y -= 18;

                    // === NEW: Weapon Attack bonuses and conditional Spell stats ===
                    int strMod = CurrentCharacter.AbilityScores?.Strength?.Modifier ?? 0;
                    int dexMod = CurrentCharacter.AbilityScores?.Dexterity?.Modifier ?? 0;
                    int prof = CurrentCharacter.ProficiencyBonus;

                    string meleeStr = (strMod + prof) >= 0 ? $"+{strMod + prof}" : (strMod + prof).ToString();
                    string rangedStr = (dexMod + prof) >= 0 ? $"+{dexMod + prof}" : (dexMod + prof).ToString();

                    DrawLabel(currentPage, "Weapon Attack (Str):", left, y + 2);
                    CreateTextField(form, currentPage, "WeaponAttackStr", meleeStr, left + 125, y, 35, smallFieldHeight);

                    DrawLabel(currentPage, "Ranged Attack (Dex):", left + 175, y + 2);
                    CreateTextField(form, currentPage, "WeaponAttackDex", rangedStr, left + 305, y, 35, smallFieldHeight);
                    y -= 17;

                    // Only show Spell DC / Attack if the character has spellcasting
                    if (!string.IsNullOrWhiteSpace(CurrentCharacter.SpellcastingAbility))
                    {
                        DrawLabel(currentPage, "Spell DC:", left, y + 2);
                        CreateTextField(form, currentPage, "SpellDC", CurrentCharacter.SpellSaveDC.ToString(), left + 60, y, 30, smallFieldHeight);

                        DrawLabel(currentPage, "Spell Attack:", left + 100, y + 2);
                        CreateTextField(form, currentPage, "SpellAttack", "+" + CurrentCharacter.SpellAttackBonus, left + 175, y, 35, smallFieldHeight);
                        y -= 17;
                    }

                    // ========== ABILITY SCORES ==========
                    DrawSectionHeader(currentPage, "ABILITY SCORES", left, ref y);

                    var abilities = new[]
                    {
                        ("Strength",     CurrentCharacter.AbilityScores.Strength.Final,     CurrentCharacter.AbilityScores.Strength.Modifier),
                        ("Dexterity",    CurrentCharacter.AbilityScores.Dexterity.Final,    CurrentCharacter.AbilityScores.Dexterity.Modifier),
                        ("Constitution", CurrentCharacter.AbilityScores.Constitution.Final, CurrentCharacter.AbilityScores.Constitution.Modifier),
                        ("Intelligence", CurrentCharacter.AbilityScores.Intelligence.Final, CurrentCharacter.AbilityScores.Intelligence.Modifier),
                        ("Wisdom",       CurrentCharacter.AbilityScores.Wisdom.Final,       CurrentCharacter.AbilityScores.Wisdom.Modifier),
                        ("Charisma",     CurrentCharacter.AbilityScores.Charisma.Final,     CurrentCharacter.AbilityScores.Charisma.Modifier)
                    };

                    foreach (var (name, final, mod) in abilities)
                    {
                        DrawLabel(currentPage, name + ":", left, y + 2);
                        CreateTextField(form, currentPage, $"{name}Score", final.ToString(), left + 72, y, 32, smallFieldHeight);

                        string modStr = mod >= 0 ? $"+{mod}" : mod.ToString();
                        CreateTextField(form, currentPage, $"{name}Modifier", modStr, left + 108, y, 32, smallFieldHeight);

                        y -= 17;
                    }

                    y -= 6;

                    // ========== SAVING THROWS ==========
                    DrawSectionHeader(currentPage, "SAVING THROWS", left, ref y);

                    var saves = GetSavingThrows();

                    float saveCol1 = left;
                    float saveCol2 = left + 255;
                    float saveY = y;

                    for (int i = 0; i < 3; i++)
                    {
                        var save = saves[i];
                        DrawLabel(currentPage, save.Name + " Save:", saveCol1, saveY + 2);
                        CreateCheckbox(form, currentPage, $"{save.Name}SaveProf", save.IsProficient, saveCol1 + 85, saveY, 14, 14);

                        string bonus = save.Bonus >= 0 ? $"+{save.Bonus}" : save.Bonus.ToString();
                        CreateTextField(form, currentPage, $"{save.Name}Save", bonus, saveCol1 + 102, saveY, 32, smallFieldHeight);
                        saveY -= 17;
                    }

                    saveY = y;
                    for (int i = 3; i < 6; i++)
                    {
                        var save = saves[i];
                        DrawLabel(currentPage, save.Name + " Save:", saveCol2, saveY + 2);
                        CreateCheckbox(form, currentPage, $"{save.Name}SaveProf", save.IsProficient, saveCol2 + 85, saveY, 14, 14);

                        string bonus = save.Bonus >= 0 ? $"+{save.Bonus}" : save.Bonus.ToString();
                        CreateTextField(form, currentPage, $"{save.Name}Save", bonus, saveCol2 + 102, saveY, 32, smallFieldHeight);
                        saveY -= 17;
                    }

                    y = Math.Min(y, saveY) - 6;

                    // ========== SKILLS ==========
                    DrawSectionHeader(currentPage, "SKILLS", left, ref y);

                    var allSkillsList = BuildFullSkillListForPDF();

                    float skillCol1 = left;
                    float skillCol2 = left + 255;
                    float skillY = y;

                    int half = (allSkillsList.Count + 1) / 2;

                    // Column 1
                    for (int i = 0; i < half; i++)
                    {
                        var skill = allSkillsList[i];
                        string bonusStr = skill.Bonus >= 0 ? $"+{skill.Bonus}" : skill.Bonus.ToString();
                        string label = $"{skill.Name} ({skill.Ability}):";

                        DrawLabel(currentPage, label, skillCol1, skillY + 2);
                        CreateCheckbox(form, currentPage, $"{skill.Name}Prof", skill.IsProficient, skillCol1 + 115, skillY, 14, 14);
                        CreateTextField(form, currentPage, $"{skill.Name}Bonus", bonusStr, skillCol1 + 132, skillY, 28, smallFieldHeight);
                        skillY -= 16;
                    }

                    // Column 2
                    skillY = y;
                    for (int i = half; i < allSkillsList.Count; i++)
                    {
                        var skill = allSkillsList[i];
                        string bonusStr = skill.Bonus >= 0 ? $"+{skill.Bonus}" : skill.Bonus.ToString();
                        string label = $"{skill.Name} ({skill.Ability}):";

                        DrawLabel(currentPage, label, skillCol2, skillY + 2);
                        CreateCheckbox(form, currentPage, $"{skill.Name}Prof", skill.IsProficient, skillCol2 + 115, skillY, 14, 14);
                        CreateTextField(form, currentPage, $"{skill.Name}Bonus", bonusStr, skillCol2 + 132, skillY, 28, smallFieldHeight);
                        skillY -= 16;
                    }

                    y = Math.Min(y, skillY) - 6;

                    // ========== FEATURES / FEAT (only show if a feat is selected) ==========
                    if (!string.IsNullOrWhiteSpace(CurrentCharacter.SelectedFeat))
                    {
                        DrawSectionHeader(currentPage, "FEATURES", left, ref y);

                        DrawLabel(currentPage, "Feat:", left, y + 3);
                        CreateTextField(form, currentPage, "SelectedFeat", CurrentCharacter.SelectedFeat, left + 32, y, 300, fieldHeight);
                        y -= 20;
                    }

                    // ========== EQUIPMENT ==========
                    CheckForNewPage(pdf, ref currentPage, ref y, 130);
                    DrawSectionHeader(currentPage, "EQUIPMENT", left, ref y);

                    string equipmentText = CurrentCharacter.Equipment != null && CurrentCharacter.Equipment.Count > 0
                        ? string.Join("\n", CurrentCharacter.Equipment)
                        : "No equipment recorded.";

                    var equipRect = new iText.Kernel.Geom.Rectangle(left, y - 80, 485, 85);
                    var equipField = new iText.Forms.Fields.TextFormFieldBuilder(form.GetPdfDocument(), "Equipment")
                        .SetWidgetRectangle(equipRect)
                        .CreateText();

                    equipField.SetMultiline(true);
                    equipField.SetValue(equipmentText);
                    equipField.SetFontSize(9f);
                    form.AddField(equipField, currentPage);

                    y -= 95;

                    // ========== EQUIPPED WEAPONS ==========
                    CheckForNewPage(pdf, ref currentPage, ref y, 140);
                    DrawSectionHeader(currentPage, "EQUIPPED WEAPONS", left, ref y);

                    var equippedWeapons = GetFormattedEquippedWeapons();
                    string weaponsText = equippedWeapons.Count > 0
                        ? string.Join("\n", equippedWeapons)
                        : "No weapons equipped.";

                    var weaponsRect = new iText.Kernel.Geom.Rectangle(left, y - 55, 485, 60);
                    var weaponsField = new iText.Forms.Fields.TextFormFieldBuilder(form.GetPdfDocument(), "EquippedWeapons")
                        .SetWidgetRectangle(weaponsRect)
                        .CreateText();

                    weaponsField.SetMultiline(true);
                    weaponsField.SetValue(weaponsText);
                    weaponsField.SetFontSize(8f);
                    form.AddField(weaponsField, currentPage);

                    y -= 70;

                    // ========== CLASS FEATURES (up to each class's actual levels only) ==========
                    CheckForNewPage(pdf, ref currentPage, ref y, 160);
                    DrawSectionHeader(currentPage, "CLASS FEATURES", left, ref y);

                    var classFeaturesText = new System.Text.StringBuilder();
                    var fillClassLevels = GetActiveClassLevels();
                    if (fillClassLevels.Count == 0 && !string.IsNullOrWhiteSpace(CurrentCharacter.Class))
                    {
                        fillClassLevels = new List<ClassLevelEntry>
                        {
                            new(CurrentCharacter.Class, Math.Max(1, CurrentCharacter.Level), CurrentCharacter.Subclass)
                        };
                    }

                    foreach (var classEntry in fillClassLevels)
                    {
                        string classKey = classEntry.ClassName;
                        int classLv = Math.Max(1, classEntry.Levels);
                        string displayClassName = classKey;
                        if (displayClassName.Contains("("))
                            displayClassName = displayClassName.Substring(0, displayClassName.IndexOf("(")).Trim();

                        string classSlug = Slugify(displayClassName);
                        string classUrl = $"https://dnd5e.wikidot.com/{classSlug}";
                        classFeaturesText.AppendLine($"{displayClassName} {classLv}");
                        classFeaturesText.AppendLine($"Source: {classUrl}");
                        classFeaturesText.AppendLine();

                        var classFeats = GameData.GetClassFeaturesUpToLevel(classKey, classLv, includeOptional: true);
                        if (classFeats.Count == 0 && classLv <= 1 &&
                            GameData.ClassLevel1Features.TryGetValue(classKey, out var legacyClassFeats))
                            classFeats = legacyClassFeats;

                        foreach (var f in classFeats)
                        {
                            string nameLabel = f.Level > 1 ? $"(Lv {f.Level}) {f.Name}" : f.Name;
                            classFeaturesText.AppendLine($"• {nameLabel}");

                            string shortDesc = (f.Description ?? "").Length > 280
                                ? f.Description.Substring(0, 277) + "..."
                                : (f.Description ?? "");
                            classFeaturesText.AppendLine($"   {shortDesc}");

                            if (!string.IsNullOrWhiteSpace(f.Uses))
                                classFeaturesText.AppendLine($"   Uses: {f.Uses}");

                            classFeaturesText.AppendLine();
                        }

                        string? subclassKey = GameData.GetEffectiveSubclass(classEntry);
                        if (string.IsNullOrWhiteSpace(subclassKey))
                            continue;

                        var subFeats = GameData.GetSubclassFeaturesUpToLevel(subclassKey, classLv);
                        if (subFeats.Count == 0 &&
                            GameData.SubclassLevel1Features.TryGetValue(subclassKey, out var legacySubFeats))
                            subFeats = legacySubFeats.Where(f => f.Level <= classLv || f.Level <= 0).ToList();

                        if (subFeats.Count == 0)
                            continue;

                        classFeaturesText.AppendLine($"--- {subclassKey} ---");

                        string subSlug = Slugify(subclassKey);
                        string subUrl = classKey.ToLowerInvariant() switch
                        {
                            "cleric" => $"https://dnd5e.wikidot.com/cleric:{subSlug}",
                            "sorcerer" => $"https://dnd5e.wikidot.com/sorcerer:{subSlug}",
                            "warlock" => $"https://dnd5e.wikidot.com/warlock:{subSlug}",
                            "barbarian" => $"https://dnd5e.wikidot.com/barbarian:{subSlug.Replace("path-of-the-", "").Replace("path-of-", "")}",
                            "fighter" => $"https://dnd5e.wikidot.com/fighter:{subSlug}",
                            _ => $"https://dnd5e.wikidot.com/{classKey.ToLowerInvariant()}"
                        };
                        classFeaturesText.AppendLine($"Source: {subUrl}");
                        classFeaturesText.AppendLine();

                        foreach (var f in subFeats)
                        {
                            string featLabel = f.Level > 0 ? $"(Lv {f.Level}) {f.Name}" : f.Name;
                            classFeaturesText.AppendLine($"• {featLabel}");

                            string shortDesc = (f.Description ?? "").Length > 280
                                ? f.Description.Substring(0, 277) + "..."
                                : (f.Description ?? "");
                            classFeaturesText.AppendLine($"   {shortDesc}");

                            if (!string.IsNullOrWhiteSpace(f.Uses))
                                classFeaturesText.AppendLine($"   Uses: {f.Uses}");

                            classFeaturesText.AppendLine();
                        }
                    }

                    string featuresText = classFeaturesText.Length > 0
                        ? classFeaturesText.ToString()
                        : "No class features recorded.";

                    var featuresRect = new iText.Kernel.Geom.Rectangle(left, y - 130, 485, 135);
                    var featuresField = new iText.Forms.Fields.TextFormFieldBuilder(form.GetPdfDocument(), "ClassFeatures")
                        .SetWidgetRectangle(featuresRect)
                        .CreateText();

                    featuresField.SetMultiline(true);
                    featuresField.SetValue(featuresText.Trim());
                    featuresField.SetFontSize(8f);
                    form.AddField(featuresField, currentPage);

                    y -= 145;

                    // ========== PROFICIENCIES ==========
                    CheckForNewPage(pdf, ref currentPage, ref y, 150);
                    DrawSectionHeader(currentPage, "PROFICIENCIES", left, ref y);

                    var profBuilder = new System.Text.StringBuilder();

                    // Armor Proficiencies
                    var armorProfs = new List<string>();
                    if (GameData.ClassData.TryGetValue(CurrentCharacter.Class, out var classData))
                        armorProfs.AddRange(classData.ArmorProficiencies);

                    if (!string.IsNullOrEmpty(CurrentCharacter.Subclass))
                    {
                        if (CurrentCharacter.Class == "Cleric" && GameData.ClericSubclasses.TryGetValue(CurrentCharacter.Subclass, out var cs))
                            armorProfs.AddRange(cs.ArmorProficiencies);
                        else if (CurrentCharacter.Class == "Warlock" && GameData.WarlockSubclasses.TryGetValue(CurrentCharacter.Subclass, out var ws))
                            armorProfs.AddRange(ws.ArmorProficiencies);
                    }

                    if (CurrentCharacter.Race != null && CurrentCharacter.Race.Contains("Dwarf"))
                        armorProfs.Add("Dwarven Armor Training");

                    if (armorProfs.Count > 0)
                        profBuilder.AppendLine("Armor: " + string.Join(", ", armorProfs.Distinct()));

                    // Weapon Proficiencies
                    var weaponProfs = new List<string>();
                    if (classData != null)
                        weaponProfs.AddRange(classData.WeaponProficiencies);

                    if (!string.IsNullOrEmpty(CurrentCharacter.Subclass))
                    {
                        if (CurrentCharacter.Class == "Cleric" && GameData.ClericSubclasses.TryGetValue(CurrentCharacter.Subclass, out var cs))
                            weaponProfs.AddRange(cs.WeaponProficiencies);
                        else if (CurrentCharacter.Class == "Warlock" && GameData.WarlockSubclasses.TryGetValue(CurrentCharacter.Subclass, out var ws))
                            weaponProfs.AddRange(ws.WeaponProficiencies);
                    }

                    if (CurrentCharacter.Race != null && CurrentCharacter.Race.Contains("Dwarf"))
                        weaponProfs.Add("Dwarven Combat Training");
                    if (CurrentCharacter.Race != null && CurrentCharacter.Race.Contains("Elf"))
                        weaponProfs.Add("Elf Weapon Training");

                    if (weaponProfs.Count > 0)
                        profBuilder.AppendLine("Weapons: " + string.Join(", ", weaponProfs.Distinct()));

                    // Languages
                    var languages = new List<string>();
                    if (GameData.RaceData.TryGetValue(CurrentCharacter.Race ?? "", out var raceData))
                        languages.AddRange(raceData.Languages);

                    if (languages.Count > 0)
                        profBuilder.AppendLine("Languages: " + string.Join(", ", languages));

                    string profText = profBuilder.Length > 0 ? profBuilder.ToString() : "None";

                    var profRect = new Rectangle(left, y - 55, 485, 60);
                    var profField = new TextFormFieldBuilder(form.GetPdfDocument(), "Proficiencies")
                        .SetWidgetRectangle(profRect)
                        .CreateText();
                    profField.SetMultiline(true);
                    profField.SetValue(profText.Trim());
                    profField.SetFontSize(9f);
                    form.AddField(profField, currentPage);

                    y -= 70;

                    // ========== RACIAL & SUBRACIAL TRAITS ==========
                    CheckForNewPage(pdf, ref currentPage, ref y, 120);
                    DrawSectionHeader(currentPage, "RACIAL & SUBRACIAL TRAITS", left, ref y);

                    var traitsBuilder = new System.Text.StringBuilder();

                    if (raceData != null && raceData.Traits.Any())
                    {
                        traitsBuilder.AppendLine("Racial Traits:");
                        foreach (var t in raceData.Traits)
                            traitsBuilder.AppendLine("• " + t);
                    }

                    if (!string.IsNullOrEmpty(CurrentCharacter.Subrace) &&
                        GameData.RaceSubraces.TryGetValue(CurrentCharacter.Race ?? "", out var subList))
                    {
                        var subrace = subList.FirstOrDefault(s => s.Name == CurrentCharacter.Subrace);
                        if (subrace != null && subrace.Traits.Any())
                        {
                            traitsBuilder.AppendLine($"\nSubracial Traits ({CurrentCharacter.Subrace}):");
                            foreach (var t in subrace.Traits)
                                traitsBuilder.AppendLine("• " + t);
                        }
                    }

                    string traitsText = traitsBuilder.Length > 0 ? traitsBuilder.ToString() : "None";

                    var traitsRect = new Rectangle(left, y - 95, 485, 100);
                    var traitsField = new TextFormFieldBuilder(form.GetPdfDocument(), "RacialTraits")
                        .SetWidgetRectangle(traitsRect)
                        .CreateText();
                    traitsField.SetMultiline(true);
                    traitsField.SetValue(traitsText.Trim());
                    traitsField.SetFontSize(9f);
                    form.AddField(traitsField, currentPage);

                    y -= 110;

                    // ========== SPELLS (Detailed with stats + description) ==========
                    if (CurrentCharacter.Cantrips.Any() || CurrentCharacter.Level1Spells.Any())
                    {
                        CheckForNewPage(pdf, ref currentPage, ref y, 220);
                        DrawSectionHeader(currentPage, "SPELLS", left, ref y);

                        // --- CANTRIPS ---
                        if (CurrentCharacter.Cantrips.Any())
                        {
                            DrawLabel(currentPage, "Cantrips:", left, y + 3);
                            y -= 14;

                            var cantripBuilder = new System.Text.StringBuilder();

                            foreach (var cantripName in CurrentCharacter.Cantrips)
                            {
                                var spell = GameData.AllCantrips.FirstOrDefault(s =>
                                    s.Name.Equals(cantripName, StringComparison.OrdinalIgnoreCase));

                                if (spell != null)
                                {
                                    cantripBuilder.AppendLine($"• {spell.Name}");
                                    cantripBuilder.AppendLine($"  Casting Time: {spell.CastingTime} | Range: {spell.Range} | Duration: {spell.Duration}" +
                                        (spell.IsConcentration ? " (Concentration)" : ""));

                                    if (!string.IsNullOrWhiteSpace(spell.DamageDice))
                                        cantripBuilder.AppendLine($"  Dice: {spell.DamageDice} {spell.DamageType}");

                                    if (!string.IsNullOrWhiteSpace(spell.RollType))
                                        cantripBuilder.AppendLine($"  Roll: {spell.RollType}");

                                    string shortDesc = spell.Description.Length > 200
                                        ? spell.Description.Substring(0, 197) + "..."
                                        : spell.Description;

                                    cantripBuilder.AppendLine($"  {shortDesc}");
                                    cantripBuilder.AppendLine($"  https://dnd5e.wikidot.com/spell:{Slugify(spell.Name)}");
                                    cantripBuilder.AppendLine();
                                }
                                else
                                {
                                    cantripBuilder.AppendLine($"• {cantripName}");
                                }
                            }

                            var cantripRect = new iText.Kernel.Geom.Rectangle(left, y - 115, 485, 120);
                            var cantripField = new iText.Forms.Fields.TextFormFieldBuilder(form.GetPdfDocument(), "CantripsDetails")
                                .SetWidgetRectangle(cantripRect)
                                .CreateText();

                            cantripField.SetMultiline(true);
                            cantripField.SetValue(cantripBuilder.ToString().Trim());
                            cantripField.SetFontSize(8f);
                            form.AddField(cantripField, currentPage);

                            y -= 130;
                        }

                        // --- 1ST LEVEL SPELLS (one field per spell) ---
                        if (CurrentCharacter.Level1Spells.Any())
                        {
                            CheckForNewPage(pdf, ref currentPage, ref y, 180);
                            DrawSectionHeader(currentPage, "1ST LEVEL SPELLS", left, ref y);

                            foreach (var spellName in CurrentCharacter.Level1Spells)
                            {
                                var spell = GameData.All1stLevelSpells.FirstOrDefault(s =>
                                    s.Name.Equals(spellName, StringComparison.OrdinalIgnoreCase));

                                CheckForNewPage(pdf, ref currentPage, ref y, 140);

                                string header = spell != null 
                                    ? $"{spell.Name} (Level {(spell as LeveledSpell)?.Level ?? 1})"
                                    : spellName;

                                DrawLabel(currentPage, header, left, y + 2);
                                y -= 16;

                                if (spell != null)
                                {
                                    var spellBuilder = new System.Text.StringBuilder();
                                    spellBuilder.AppendLine($"Casting Time: {spell.CastingTime} | Range: {spell.Range} | Duration: {spell.Duration}" + 
                                        (spell.IsConcentration ? " (Concentration)" : ""));
                                    
                                    if (!string.IsNullOrWhiteSpace(spell.DamageDice))
                                        spellBuilder.AppendLine($"Dice: {spell.DamageDice} {spell.DamageType}");
                                    
                                    if (!string.IsNullOrWhiteSpace(spell.RollType))
                                        spellBuilder.AppendLine($"Roll: {spell.RollType}");

                                    spellBuilder.AppendLine();
                                    spellBuilder.AppendLine(spell.Description);
                                    spellBuilder.AppendLine($"https://dnd5e.wikidot.com/spell:{Slugify(spell.Name)}");

                                    var spellRect = new Rectangle(left, y - 95, 485, 100);
                                    var spellField = new TextFormFieldBuilder(form.GetPdfDocument(), $"Spell_{Slugify(spellName)}")
                                        .SetWidgetRectangle(spellRect)
                                        .CreateText();
                                    spellField.SetMultiline(true);
                                    spellField.SetValue(spellBuilder.ToString().Trim());
                                    spellField.SetFontSize(8f);
                                    form.AddField(spellField, currentPage);

                                    y -= 110;
                                }
                                else
                                {
                                    y -= 20;
                                }
                            }
                        }
                    }

                    pdf.Close();
                }

                MessageBox.Show($"Fillable PDF exported successfully!\n{saveDlg.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating fillable PDF:\n{ex.Message}");
            }
        }

        // ==================== Helper Methods ====================

        private void CreateTextField(iText.Forms.PdfAcroForm form, iText.Kernel.Pdf.PdfPage page,
            string fieldName, string value, float x, float y, float width, float height)
        {
            var rect = new iText.Kernel.Geom.Rectangle(x, y, width, height);

            var textField = new iText.Forms.Fields.TextFormFieldBuilder(form.GetPdfDocument(), fieldName)
                .SetWidgetRectangle(rect)
                .CreateText();

            textField.SetValue(value ?? "");
            textField.SetFontSize(10f);
            form.AddField(textField, page);
        }

        private void CreateCheckbox(iText.Forms.PdfAcroForm form, iText.Kernel.Pdf.PdfPage page,
            string fieldName, bool isChecked, float x, float y, float width, float height)
        {
            var rect = new iText.Kernel.Geom.Rectangle(x, y, width, height);

            var checkField = new iText.Forms.Fields.CheckBoxFormFieldBuilder(form.GetPdfDocument(), fieldName)
                .SetWidgetRectangle(rect)
                .CreateCheckBox();

            if (isChecked)
                checkField.SetValue("Yes");   // Use SetValue("Yes") instead of SetChecked(true)

            form.AddField(checkField, page);
        }

        private void DrawSectionHeader(iText.Kernel.Pdf.PdfPage pdfPage, string title, float x, ref float y)
        {
            // Draw a visible section header using iText Layout
            try
            {
                var canvas = new iText.Layout.Canvas(pdfPage, pdfPage.GetPageSize());
                var para = new Paragraph(title)
                    .SetFontSize(10)
                    .SetFontColor(new iText.Kernel.Colors.DeviceRgb(60, 60, 110))
                    .SetBold();

                canvas.ShowTextAligned(para, x, y + 4, iText.Layout.Properties.TextAlignment.LEFT);
                canvas.Close();
            }
            catch { /* fallback - header text omitted */ }

            y -= 16;
        }

        /// <summary>
        /// Draws a static label text using iText Layout (for fillable PDF).
        /// </summary>
        private void DrawLabel(iText.Kernel.Pdf.PdfPage pdfPage, string text, float x, float y)
        {
            try
            {
                var canvas = new iText.Layout.Canvas(pdfPage, pdfPage.GetPageSize());
                var para = new Paragraph(text)
                    .SetFontSize(9)
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK);

                canvas.ShowTextAligned(para, x, y, iText.Layout.Properties.TextAlignment.LEFT);
                canvas.Close();
            }
            catch { /* silent fallback */ }
        }

        /// <summary>
        /// Checks if we have enough vertical space left on the current page for fillable PDF.
        /// If not, creates a new page and resets the y coordinate.
        /// </summary>
        private void CheckForNewPage(iText.Kernel.Pdf.PdfDocument pdf, ref iText.Kernel.Pdf.PdfPage currentPage, ref float y, float neededSpace = 130)
        {
            if (y < neededSpace)
            {
                currentPage = pdf.AddNewPage(iText.Kernel.Geom.PageSize.LETTER);
                y = 750; // reset near top of new page
            }
        }

        private int GetCharacterLevel() => GetEffectiveCharacterLevel();

        private void AutoSaveCharacterToJson()
        {
            // Reuse the same logic — no duplication
            SaveCharacterToFixedPath(showMessage: false);
        }

        /// <summary>
        /// Saves the current character to character.json next to the executable.
        /// This is the single source of truth for saving.
        /// </summary>
        private void SaveCharacterToFixedPath(bool showMessage = false)
        {
            // Update the existing character in place.
            // IMPORTANT: do NOT replace with `new Character()` — that wiped ClassLevels, Level,
            // fighting styles, ASI decisions, HP rolls, etc., which broke higher-level spell slots
            // (e.g. Ranger 5 losing 2nd-level spells after save/export).
            CurrentCharacter ??= new Character();

            // Keep level / multiclass / feature picks current from the Level tab model
            EnsureClassLevelsSeeded();
            SyncCharacterClassFromUi();

            CurrentCharacter.Name = txtCharacterName.Text.Trim();
            CurrentCharacter.PlayerName = txtPlayerName.Text.Trim();
            CurrentCharacter.AvatarBase64 = avatarBase64;

            CurrentCharacter.Race = cmbRace.SelectedItem?.ToString() ?? "";
            CurrentCharacter.Subrace = cmbSubrace.SelectedItem?.ToString() ?? "";
            CurrentCharacter.Background = cmbBackground.SelectedItem?.ToString() ?? "";
            // Clean class name (remove "Requires Level X" suffix)
            string rawClass = cmbClass.SelectedItem?.ToString() ?? "";
            CurrentCharacter.Class = rawClass.Contains("(")
                ? rawClass.Substring(0, rawClass.IndexOf("(")).Trim()
                : rawClass;
            // Subclass only when the primary class has unlocked it (level-gated)
            string rawSubclass = cmbSubclass.SelectedItem?.ToString() ?? "";
            if (rawSubclass.Contains("Requires Level", StringComparison.OrdinalIgnoreCase) ||
                rawSubclass.StartsWith("(No", StringComparison.OrdinalIgnoreCase) ||
                rawSubclass.StartsWith("(Unlocks", StringComparison.OrdinalIgnoreCase))
            {
                rawSubclass = "";
            }

            // Mirror primary class/subclass onto ClassLevels when single-class
            if (CurrentCharacter.ClassLevels != null && CurrentCharacter.ClassLevels.Count == 1 &&
                !string.IsNullOrWhiteSpace(CurrentCharacter.Class))
            {
                CurrentCharacter.ClassLevels[0].ClassName = CurrentCharacter.Class;
                if (!string.IsNullOrWhiteSpace(rawSubclass) &&
                    GameData.HasUnlockedSubclass(CurrentCharacter.Class, CurrentCharacter.ClassLevels[0].Levels))
                {
                    CurrentCharacter.ClassLevels[0].Subclass = rawSubclass;
                }
            }

            // Effective subclass for sheet / summary fields
            if (CurrentCharacter.ClassLevels != null && CurrentCharacter.ClassLevels.Count > 0)
            {
                var primary = CurrentCharacter.ClassLevels[0];
                CurrentCharacter.Subclass =
                    GameData.GetEffectiveSubclass(primary.ClassName, primary.Levels,
                        !string.IsNullOrWhiteSpace(rawSubclass) ? rawSubclass : primary.Subclass) ?? "";
            }
            else
            {
                int lvl = Math.Max(1, CurrentCharacter.Level);
                CurrentCharacter.Subclass =
                    GameData.GetEffectiveSubclass(CurrentCharacter.Class, lvl, rawSubclass) ?? "";
            }

            // Total character level from class levels (or keep at least 1)
            if (CurrentCharacter.ClassLevels != null && CurrentCharacter.ClassLevels.Count > 0)
                CurrentCharacter.Level = Math.Clamp(
                    CurrentCharacter.ClassLevels.Sum(e => e.Levels), 1, 20);
            else if (CurrentCharacter.Level <= 0)
                CurrentCharacter.Level = 1;

            if (dgFeats.SelectedItem is Feat feat)
                CurrentCharacter.SelectedFeat = feat.Name;

            PopulateAbilityScores();
            RefreshProficiencyBonus();
            CurrentCharacter.SavingThrows = GetSavingThrows();

            // Calculate and store final speed (including Mobile feat)
            CurrentCharacter.Speed = GetFinalSpeed();

            // Skills (rebuild from UI — include expertise)
            CurrentCharacter.Skills ??= new List<SkillEntry>();
            CurrentCharacter.Skills.Clear();
            foreach (var skill in allSkills.Where(s => s.IsProficient))
            {
                int bonusVal = 0;
                if (!string.IsNullOrEmpty(skill.Bonus))
                {
                    string raw = skill.Bonus.Replace("+", "").Trim();
                    int.TryParse(raw, out bonusVal);
                }
                CurrentCharacter.Skills.Add(new SkillEntry
                {
                    Name = skill.SkillName,
                    Ability = skill.Ability,
                    IsProficient = true,
                    IsExpertise = skill.IsExpertise,
                    Bonus = bonusVal
                });
            }

            // Equipment (rebuild from UI; gold is stored on dedicated fields, not as gear lines)
            CurrentCharacter.Equipment ??= new List<string>();
            CurrentCharacter.Equipment.Clear();
            foreach (var child in pnlTotalEquipmentSummary.Children.OfType<TextBlock>())
            {
                string text = child.Text.Replace("• ", "").Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (text.StartsWith("Starting gold", StringComparison.OrdinalIgnoreCase)) continue;
                if (text.StartsWith("Higher-level wealth", StringComparison.OrdinalIgnoreCase)) continue;
                if (text.StartsWith("Custom gold", StringComparison.OrdinalIgnoreCase)) continue;
                if (text.StartsWith("Custom / DM gold", StringComparison.OrdinalIgnoreCase)) continue;
                if (text.Contains("not rolled yet", StringComparison.OrdinalIgnoreCase)) continue;
                CurrentCharacter.Equipment.Add(text);
            }
            CurrentCharacter.BackgroundEquipment = txtBackgroundEquipment?.Text ?? "";

            // Starting wealth: at level 1, mode follows the radio; after level-up/multiclass,
            // keep UseRolledGoldInsteadOfEquipment if they already rolled (don't wipe on save).
            if (GetCharacterLevelForWealth() <= 1)
            {
                CurrentCharacter.UseRolledGoldInsteadOfEquipment = rbRollStartingGold?.IsChecked == true;
            }
            ApplyCustomGoldFromUi();
            SyncGoldPiecesTotal();

            // Spells (rebuild from UI; Level1Spells stores all selected leveled spells 1–9)
            CurrentCharacter.Cantrips = cantripOptions.Where(c => c.IsChecked).Select(c => c.Name).ToList();
            CurrentCharacter.CantripClassAssignments = cantripOptions
                .Where(c => c.IsChecked && !string.IsNullOrWhiteSpace(c.AssignedClassKey))
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().AssignedClassKey, StringComparer.OrdinalIgnoreCase);
            CurrentCharacter.Level1Spells = spell1Options.Where(s => s.IsChecked).Select(s => s.Name).ToList();

            // Feat-granted spells (Magic Initiate, Fey Touched, etc.) for PDF tags / reload
            CurrentCharacter.FeatSpellSource = currentFeatSpellSource ?? "";
            CurrentCharacter.FeatSpells = currentFeatSpells?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();

            // Derived values
            CurrentCharacter.Initiative = GetModifierFromText(txtInitiative.Text);
            CurrentCharacter.HitPoints = int.TryParse(txtHitPoints.Text, out int hp) ? hp : 0;

            // === Armor Class (Base + Equipped, including Defense style when armored) ===
            // Prefer the equipped total (includes armor, shield, Defense fighting style).
            // Refresh first so Defense / equipment changes are reflected before export/save.
            UpdateEquippedAC();
            if (txtEquippedAC != null &&
                txtEquippedAC.Visibility == Visibility.Visible &&
                !string.IsNullOrWhiteSpace(txtEquippedAC.Text))
            {
                CurrentCharacter.EquippedACDisplay = txtEquippedAC.Text;
                var acMatch = System.Text.RegularExpressions.Regex.Match(
                    txtEquippedAC.Text, @"^\((\d+)\)");
                if (acMatch.Success && int.TryParse(acMatch.Groups[1].Value, out int equippedTotal))
                    CurrentCharacter.ArmorClass = equippedTotal;
                else
                    CurrentCharacter.ArmorClass = int.TryParse(txtBaseAC?.Text, out int fallbackAc) ? fallbackAc : 10;
            }
            else
            {
                CurrentCharacter.ArmorClass = int.TryParse(txtBaseAC?.Text, out int ac) ? ac : 10;
                CurrentCharacter.EquippedACDisplay = "";
            }

            // Spellcasting ability / DC / attack — include third-caster subclasses (AT / EK use Int)
            {
                string spellAbility = "";
                if (cmbClass.SelectedItem is string className &&
                    GameData.ClassData.TryGetValue(className, out var classData) &&
                    classData.Spellcasting &&
                    !string.IsNullOrWhiteSpace(classData.SpellAbility))
                {
                    spellAbility = classData.SpellAbility;
                }
                else
                {
                    // Prefer any casting class level (multiclass or third-caster subclass)
                    foreach (var e in GetActiveClassLevels())
                    {
                        if (SpellProgressionCalculator.IsThirdCasterSubclass(e.ClassName, e.Subclass))
                        {
                            spellAbility = "Intelligence";
                            break;
                        }
                        if (GameData.ClassData.TryGetValue(e.ClassName, out var cd) &&
                            cd.Spellcasting &&
                            !string.IsNullOrWhiteSpace(cd.SpellAbility))
                        {
                            spellAbility = cd.SpellAbility;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(spellAbility))
                {
                    CurrentCharacter.SpellcastingAbility = spellAbility;
                    int mod = spellAbility switch
                    {
                        "Wisdom" => GetModifierFromText(txtWisMod.Text),
                        "Charisma" => GetModifierFromText(txtChaMod.Text),
                        "Intelligence" => GetModifierFromText(txtIntMod.Text),
                        _ => 0
                    };
                    CurrentCharacter.SpellSaveDC = 8 + CurrentCharacter.ProficiencyBonus + mod;
                    CurrentCharacter.SpellAttackBonus = CurrentCharacter.ProficiencyBonus + mod;
                }
                else
                {
                    CurrentCharacter.SpellcastingAbility = "";
                    CurrentCharacter.SpellSaveDC = 0;
                    CurrentCharacter.SpellAttackBonus = 0;
                }
            }

            CurrentCharacter.HighElfCantrip = highElfCantrip;
            CurrentCharacter.RaceGrantedSkill = raceGrantedSkill;

            // Ensure list fields used by level-up / class features are non-null for serialization
            CurrentCharacter.ClassLevels ??= new List<ClassLevelEntry>();
            CurrentCharacter.AsiOrFeatDecisions ??= new List<AsiOrFeatDecision>();
            CurrentCharacter.FightingStyles ??= new List<string>();
            CurrentCharacter.EldritchInvocations ??= new List<string>();
            CurrentCharacter.MetamagicOptions ??= new List<string>();
            CurrentCharacter.HitPointRolls ??= new List<int>();
            CurrentCharacter.BackgroundLanguages ??= new List<string>();

            // Drop Fighting Initiate style if the feat is not selected
            if (!IsFightingInitiateSelected())
                CurrentCharacter.FightingInitiateStyle = "";
            else if (string.IsNullOrWhiteSpace(CurrentCharacter.FightingInitiateStyle))
                EnsureFightingInitiateStyleDefault();

            // === Save Foundry VTT–compatible actor JSON next to the .exe ===
            // Full Nemo state is embedded under flags.nemo.character for round-trip load.
            try
            {
                string savePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "character.json");
                string json = FoundryCharacterExporter.ToJson(CurrentCharacter);
                File.WriteAllText(savePath, json);

                if (showMessage)
                {
                    MessageBox.Show(
                        "✅ Character saved as Foundry VTT JSON.\n\n" +
                        "In Foundry: create a Character actor → right-click → Import Data → pick this file.\n" +
                        "Nemo can also reload this file with full character data.",
                        "Save Complete");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save character:\n{ex.Message}", "Error");
            }
        }

        private void SaveCharacter_Click(object sender, RoutedEventArgs e)
        {
            SaveCharacterToFixedPath(showMessage: true);
        }

        private void RestoreAbilityScores()
        {
            if (CurrentCharacter.AbilityScores == null) return;

            txtStrBase.Text = CurrentCharacter.AbilityScores.Strength.Base.ToString();
            txtDexBase.Text = CurrentCharacter.AbilityScores.Dexterity.Base.ToString();
            txtConBase.Text = CurrentCharacter.AbilityScores.Constitution.Base.ToString();
            txtIntBase.Text = CurrentCharacter.AbilityScores.Intelligence.Base.ToString();
            txtWisBase.Text = CurrentCharacter.AbilityScores.Wisdom.Base.ToString();
            txtChaBase.Text = CurrentCharacter.AbilityScores.Charisma.Base.ToString();
        }

        private void RestoreSkills()
        {
            if (CurrentCharacter.Skills == null || allSkills == null) return;

            // First clear all proficiencies / expertise
            foreach (var skill in allSkills)
            {
                skill.IsProficient = false;
                skill.SetExpertiseQuiet(false);
                skill.IsBackgroundProficiency = false;
            }

            // Skills granted by race/background are locked and do NOT count toward class slots.
            // Class (and other) picks stay proficient but toggleable / countable.
            var granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string bg = CurrentCharacter.Background ?? cmbBackground?.SelectedItem?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(bg))
            {
                foreach (var s in GetBackgroundSkillList(bg))
                    granted.Add(s);
            }

            if (currentRaceAutomaticSkills != null)
            {
                foreach (var s in currentRaceAutomaticSkills)
                    if (!string.IsNullOrWhiteSpace(s))
                        granted.Add(s);
            }

            string race = CurrentCharacter.Race ?? cmbRace?.SelectedItem?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(race) &&
                GameData.RaceData.TryGetValue(race, out var raceData) &&
                raceData.SkillProficiencies != null)
            {
                foreach (var s in raceData.SkillProficiencies)
                    granted.Add(s);
            }

            if (!string.IsNullOrWhiteSpace(raceGrantedSkill))
                granted.Add(raceGrantedSkill);
            if (!string.IsNullOrWhiteSpace(CurrentCharacter.RaceGrantedSkill))
                granted.Add(CurrentCharacter.RaceGrantedSkill);

            // Apply saved proficiencies + expertise
            foreach (var savedSkill in CurrentCharacter.Skills)
            {
                if (string.IsNullOrWhiteSpace(savedSkill.Name)) continue;

                var skill = allSkills.FirstOrDefault(s =>
                    s.SkillName.Equals(savedSkill.Name, StringComparison.OrdinalIgnoreCase));

                if (skill != null)
                {
                    skill.IsProficient = true;
                    skill.SetExpertiseQuiet(savedSkill.IsExpertise);
                    // Only true race/background grants are locked — class picks must remain
                    // countable for "X / Y class skills selected".
                    skill.IsBackgroundProficiency = granted.Contains(skill.SkillName);
                }
            }

            dgSkills.Items.Refresh();
            UpdateSkillBonuses();
        }

        private void RestoreSelectedFeat()
        {
            // Always clear first so a prior feat does not stick when the new character has none
            EnsureFeatsLoaded();
            if (GameData.AllFeats != null)
            {
                foreach (var feat in GameData.AllFeats)
                {
                    if (feat != null)
                        feat.IsSelected = false;
                }
            }
            if (dgFeats != null)
                dgFeats.SelectedItem = null;

            if (string.IsNullOrEmpty(CurrentCharacter?.SelectedFeat))
            {
                currentFeatSpellSource = "";
                currentFeatSpells = new List<string>();
                UpdateFeatSpellsLabel();
                dgFeats?.Items.Refresh();
                return;
            }

            if (dgFeats?.ItemsSource == null)
                return;

            foreach (Feat feat in dgFeats.ItemsSource)
            {
                if (feat.Name.Equals(CurrentCharacter.SelectedFeat, StringComparison.OrdinalIgnoreCase))
                {
                    feat.IsSelected = true;
                    dgFeats.SelectedItem = feat;
                    break;
                }
            }

            // Restore feat-granted spells for labels / re-export after load
            if (CurrentCharacter.FeatSpells != null && CurrentCharacter.FeatSpells.Count > 0)
            {
                currentFeatSpellSource = CurrentCharacter.FeatSpellSource
                    ?? CurrentCharacter.SelectedFeat
                    ?? "";
                currentFeatSpells = CurrentCharacter.FeatSpells
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToList();
            }
            else
            {
                currentFeatSpellSource = "";
                currentFeatSpells = new List<string>();
            }

            UpdateFeatSpellsLabel();
            dgFeats.Items.Refresh();
        }

        private void RestoreCantrips()
        {
            if (cantripOptions == null) return;

            var selected = CurrentCharacter?.Cantrips ?? new List<string>();
            var preferred = CurrentCharacter?.CantripClassAssignments ??
                            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var option in cantripOptions)
            {
                bool isOn = selected.Contains(option.Name, StringComparer.OrdinalIgnoreCase);
                option.IsChecked = isOn;
                if (isOn && preferred.TryGetValue(option.Name, out var classKey) &&
                    !string.IsNullOrWhiteSpace(classKey))
                {
                    option.AssignedClassKey = classKey;
                    // Display filled during rebalance
                }
                else
                {
                    ClearCantripAssignment(option);
                }
            }

            // Enforce per-class budgets and fill missing / invalid assignments
            RebalanceCantripAssignments(preferred);
            ApplyCantripSelectableState();
            UpdateCantripHeader();

            dgCantrips.Items.Refresh();
            UpdateCantripCounter();
        }

        private void RestoreLevel1Spells()
        {
            if (spell1Options == null) return;

            var selected = CurrentCharacter?.Level1Spells ?? new List<string>();
            foreach (var option in spell1Options)
            {
                option.IsChecked = selected.Contains(option.Name, StringComparer.OrdinalIgnoreCase);
            }

            dgSpells1.Items.Refresh();
            UpdateSpellCounter();
        }

        private void RestoreEquipment()
        {
            // Restore starting wealth mode / prior rolls before rebuilding the summary
            _suppressWealthModeEvents = true;
            try
            {
                if (rbRollStartingGold != null && rbStartingEquipment != null)
                {
                    if (CurrentCharacter.UseRolledGoldInsteadOfEquipment && GetEffectiveCharacterLevel() <= 1)
                        rbRollStartingGold.IsChecked = true;
                    else
                        rbStartingEquipment.IsChecked = true;
                }
            }
            finally
            {
                _suppressWealthModeEvents = false;
            }

            // Align class tracker so regenerating equipment after load doesn't wipe rolls
            string loadedClass = cmbClass?.SelectedItem as string ?? CurrentCharacter.Class ?? "";
            if (!string.IsNullOrWhiteSpace(loadedClass))
                _lastStartingWealthClass = loadedClass;

            RefreshStartingWealthUi();

            if (txtLevel1GoldResult != null && CurrentCharacter.Level1RolledGoldGp > 0)
                txtLevel1GoldResult.Text = string.IsNullOrWhiteSpace(CurrentCharacter.Level1RolledGoldBreakdown)
                    ? $"{CurrentCharacter.Level1RolledGoldGp} gp"
                    : CurrentCharacter.Level1RolledGoldBreakdown;

            if (txtHigherLevelGoldResult != null && CurrentCharacter.HigherLevelWealthGp > 0)
                txtHigherLevelGoldResult.Text = string.IsNullOrWhiteSpace(CurrentCharacter.HigherLevelWealthBreakdown)
                    ? $"{CurrentCharacter.HigherLevelWealthGp:N0} gp"
                    : CurrentCharacter.HigherLevelWealthBreakdown;

            RestoreCustomGoldUi();

            // Rebuild live summary (class kit / gold / background / higher-level wealth)
            UpdateTotalEquipmentSummary();

            // === Try to restore weapon combo boxes intelligently ===
            RestoreWeaponSelectionsFromEquipment();

            // Restore Background Equipment label if it exists
            if (!string.IsNullOrEmpty(CurrentCharacter.BackgroundEquipment) && txtBackgroundEquipment != null)
            {
                txtBackgroundEquipment.Text = CurrentCharacter.BackgroundEquipment;
            }
        }

        private void RestoreWeaponSelectionsFromEquipment()
        {
            if (CurrentCharacter.Equipment == null || CurrentCharacter.Equipment.Count == 0)
                return;

            var equipment = CurrentCharacter.Equipment;

            // Check for martial weapons
            bool hasMartialWeapon = equipment.Any(e =>
                GameData.MartialWeapons.Any(mw =>
                    e.Contains(mw.Name, StringComparison.OrdinalIgnoreCase)));

            // Check for simple weapons
            bool hasSimpleWeapon = equipment.Any(e =>
                GameData.SimpleWeapons.Any(sw =>
                    e.Contains(sw.Name, StringComparison.OrdinalIgnoreCase)));

            // Try to restore simple weapon combo box
            var simpleWeapon = equipment.FirstOrDefault(e =>
                GameData.SimpleWeapons.Any(sw =>
                    e.Contains(sw.Name, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrEmpty(simpleWeapon) && cmbWeaponChoice1 != null)
            {
                cmbWeaponChoice1.SelectedItem = simpleWeapon;
                cmbWeaponChoice1.Visibility = Visibility.Visible;
            }

            // Try to restore martial weapon combo box
            var martialWeapon = equipment.FirstOrDefault(e =>
                GameData.MartialWeapons.Any(mw =>
                    e.Contains(mw.Name, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrEmpty(martialWeapon) && cmbWeaponChoice2 != null)
            {
                cmbWeaponChoice2.SelectedItem = martialWeapon;
                cmbWeaponChoice2.Visibility = Visibility.Visible;
            }

            // Optional: Handle "Martial weapon and shield" case
            if (hasMartialWeapon && equipment.Any(e => e.Contains("Shield", StringComparison.OrdinalIgnoreCase)))
            {
                // You can add logic here later to re-select the correct radio button if needed
            }
        }

        private void LoadCharacter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Load Character",
                    Filter = "Character Files (*.json;*.pdf)|*.json;*.pdf|Foundry / Nemo JSON (*.json)|*.json|Character Sheet PDF (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    DefaultExt = ".json",
                    CheckFileExists = true
                };

                if (dlg.ShowDialog() != true)
                    return;

                string path = dlg.FileName;
                string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                string? importNote = null;

                if (ext == ".pdf")
                {
                    var (character, note) = CharacterSheetImporter.ImportFromPdf(path);
                    CurrentCharacter = character;
                    importNote = note;
                }
                else
                {
                    string json = File.ReadAllText(path);
                    CurrentCharacter = FoundryCharacterExporter.TryParseCharacter(json, out importNote);
                }

                if (CurrentCharacter == null)
                {
                    MessageBox.Show("Failed to load character data.", "Error");
                    return;
                }

                ApplyCharacterToUI(fromPdf: ext == ".pdf");

                string message = "Character loaded successfully!";
                if (!string.IsNullOrWhiteSpace(importNote))
                    message += "\n\n" + importNote;

                MessageBox.Show(message, "Load Complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading character:\n{ex.Message}", "Load Error");
            }
        }

        /// <summary>
        /// Clears session/UI state that would otherwise leak from a previous character
        /// when generate/load applies a new one. Combo selections are blanked so
        /// SelectionChanged re-fires even if the next race/class matches the previous.
        /// </summary>
        private void ClearUiStateForCharacterReplace()
        {
            // Race / background session fields
            raceGrantedSkill = "";
            highElfCantrip = "";
            currentRaceAutomaticSkills = new List<string>();
            racialBonuses = new Dictionary<string, int>();
            backgroundLanguage1 = "";
            backgroundLanguage2 = "";
            currentBackgroundEquipmentAdded = "";

            // Feat session fields
            featStatBonuses = new Dictionary<string, int>();
            featInitiativeBonus = 0;
            currentFeatAbilityChoice = "";
            resilientSaveAbility = "";
            featSelectedSpell = "";
            baseFeatDescription = "";
            featSpeedBonus = 0;
            magicInitiateClass = "";
            magicInitiateCantrips = new List<string>();
            magicInitiateSpell = "";
            currentFeatSpellSource = "";
            currentFeatSpells = new List<string>();

            try { ResetFeatChoiceUi(); } catch { /* UI may not be ready */ }

            EnsureFeatsLoaded();
            if (GameData.AllFeats != null)
            {
                foreach (var feat in GameData.AllFeats)
                {
                    if (feat != null)
                        feat.IsSelected = false;
                }
            }
            if (dgFeats != null)
            {
                dgFeats.SelectedItem = null;
                dgFeats.Items.Refresh();
            }
            if (txtFeatDetails != null)
                txtFeatDetails.Text = "";
            try { UpdateFeatSpellsLabel(); } catch { /* ignore */ }

            // Skills / expertise
            if (allSkills != null)
            {
                foreach (var skill in allSkills)
                {
                    skill.IsProficient = false;
                    skill.SetExpertiseQuiet(false);
                    skill.IsBackgroundProficiency = false;
                }
                dgSkills?.Items.Refresh();
            }

            // Spells — uncheck everything; restore methods re-check from character
            if (cantripOptions != null)
            {
                foreach (var c in cantripOptions)
                {
                    c.IsChecked = false;
                    ClearCantripAssignment(c);
                }
                try
                {
                    dgCantrips?.Items.Refresh();
                    UpdateCantripCounter();
                }
                catch { /* ignore */ }
            }
            if (spell1Options != null)
            {
                foreach (var s in spell1Options)
                    s.IsChecked = false;
                try
                {
                    dgSpells1?.Items.Refresh();
                    UpdateSpellCounter();
                }
                catch { /* ignore */ }
            }

            // Equipment / wealth UI leftovers
            pickedWeapons.Clear();
            activeWeaponChoices.Clear();
            _lastStartingWealthClass = "";
            try
            {
                pnlEquipmentChoices?.Children.Clear();
                if (pnlWeaponChoices != null)
                    pnlWeaponChoices.Visibility = Visibility.Collapsed;
                if (txtBackgroundEquipment != null)
                    txtBackgroundEquipment.Text = "";
                if (txtLevel1GoldResult != null)
                    txtLevel1GoldResult.Text = "";
                if (txtHigherLevelGoldResult != null)
                    txtHigherLevelGoldResult.Text = "";
                if (txtCustomGoldGp != null)
                {
                    txtCustomGoldGp.TextChanged -= TxtCustomGoldGp_TextChanged;
                    txtCustomGoldGp.Text = "";
                    txtCustomGoldGp.TextChanged += TxtCustomGoldGp_TextChanged;
                }
            }
            catch { /* ignore */ }

            // Optional panels that may stick from the previous race
            if (pnlFlexibleBonuses != null)
                pnlFlexibleBonuses.Visibility = Visibility.Collapsed;
            if (pnlRaceSkillChoice != null)
                pnlRaceSkillChoice.Visibility = Visibility.Collapsed;
            if (pnlHighElfCantrip != null)
                pnlHighElfCantrip.Visibility = Visibility.Collapsed;
            if (pnlHighElfCantripPreview != null)
                pnlHighElfCantripPreview.Visibility = Visibility.Collapsed;
            if (pnlBackgroundLanguages != null)
                pnlBackgroundLanguages.Visibility = Visibility.Collapsed;

            // Force combos empty so re-select always raises SelectionChanged
            _suppressRaceCategoryEvents = true;
            _suppressLevelTabRebuild = true;
            try
            {
                if (cmbSubrace != null)
                {
                    cmbSubrace.SelectedIndex = -1;
                    cmbSubrace.ItemsSource = null;
                    cmbSubrace.IsEnabled = false;
                }
                if (cmbSubclass != null)
                {
                    cmbSubclass.SelectedIndex = -1;
                    cmbSubclass.ItemsSource = null;
                    cmbSubclass.IsEnabled = false;
                }
                if (cmbRace != null)
                    cmbRace.SelectedIndex = -1;
                if (cmbBackground != null)
                    cmbBackground.SelectedIndex = -1;
                if (cmbClass != null)
                    cmbClass.SelectedIndex = -1;
                if (cmbFlexibleBonus1 != null)
                    cmbFlexibleBonus1.SelectedIndex = -1;
                if (cmbFlexibleBonus2 != null)
                    cmbFlexibleBonus2.SelectedIndex = -1;
                if (cmbRaceSkillChoice != null)
                    cmbRaceSkillChoice.SelectedIndex = -1;
                if (cmbHighElfCantrip != null)
                    cmbHighElfCantrip.SelectedIndex = -1;
                if (cmbBackgroundLanguage1 != null)
                    cmbBackgroundLanguage1.SelectedIndex = -1;
                if (cmbBackgroundLanguage2 != null)
                    cmbBackgroundLanguage2.SelectedIndex = -1;
            }
            finally
            {
                _suppressRaceCategoryEvents = false;
                _suppressLevelTabRebuild = false;
            }

            // Avatar only when the incoming character has none
            if (string.IsNullOrEmpty(CurrentCharacter?.AvatarBase64))
            {
                avatarBase64 = "";
                if (imgAvatar != null)
                    imgAvatar.Source = null;
                if (lblAvatarStatus != null)
                    lblAvatarStatus.Text = "";
            }
        }

        /// <summary>
        /// Pushes <see cref="CurrentCharacter"/> into the UI controls.
        /// When <paramref name="fromPdf"/> is true, ability score bases are reverse-engineered
        /// so that base + racial ≈ the PDF's final scores.
        /// </summary>
        private void ApplyCharacterToUI(bool fromPdf = false)
        {
            // Drop previous character UI leftovers before applying the new one
            ClearUiStateForCharacterReplace();

            // Custom method allows free editing of base scores after import
            if (rbCustom != null)
                rbCustom.IsChecked = true;

            txtCharacterName.Text = CurrentCharacter.Name ?? "";
            txtPlayerName.Text = CurrentCharacter.PlayerName ?? "";

            // Avatar
            if (!string.IsNullOrEmpty(CurrentCharacter.AvatarBase64))
            {
                try
                {
                    avatarBase64 = CurrentCharacter.AvatarBase64;
                    byte[] bytes = Convert.FromBase64String(avatarBase64);
                    var img = new BitmapImage();
                    using (var ms = new MemoryStream(bytes))
                    {
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.StreamSource = ms;
                        img.EndInit();
                        img.Freeze();
                    }
                    imgAvatar.Source = img;
                    if (lblAvatarStatus != null)
                        lblAvatarStatus.Text = "Avatar loaded ✓";
                }
                catch
                {
                    // ignore bad avatar data
                }
            }
            else
            {
                avatarBase64 = "";
                if (imgAvatar != null)
                    imgAvatar.Source = null;
                if (lblAvatarStatus != null)
                    lblAvatarStatus.Text = "";
            }

            // Race category first so the race dropdown contains the saved race
            if (!string.IsNullOrEmpty(CurrentCharacter.Race))
            {
                string cat = GameData.GetRaceCategory(CurrentCharacter.Race);
                if (cmbRaceCategory != null)
                {
                    _suppressRaceCategoryEvents = true;
                    try
                    {
                        cmbRaceCategory.SelectedItem = cat;
                        RefreshRaceComboForCategory(cat);
                    }
                    finally
                    {
                        _suppressRaceCategoryEvents = false;
                    }
                }

                // Match race case-insensitively against combo items
                string? raceMatch = cmbRace?.Items.Cast<object>()
                    .Select(i => i?.ToString())
                    .FirstOrDefault(r => r != null &&
                        r.Equals(CurrentCharacter.Race, StringComparison.OrdinalIgnoreCase));

                if (raceMatch != null && cmbRace != null)
                    cmbRace.SelectedItem = raceMatch;
            }

            // Subrace after race populates the list (clear already done if empty)
            if (!string.IsNullOrEmpty(CurrentCharacter.Subrace) && cmbSubrace != null && cmbSubrace.Items.Count > 0)
            {
                string? subMatch = cmbSubrace.Items.Cast<object>()
                    .Select(i => i?.ToString())
                    .FirstOrDefault(s => s != null &&
                        s.Equals(CurrentCharacter.Subrace, StringComparison.OrdinalIgnoreCase));

                if (subMatch != null)
                    cmbSubrace.SelectedItem = subMatch;
            }

            if (!string.IsNullOrEmpty(CurrentCharacter.Background))
            {
                string? bgMatch = cmbBackground.Items.Cast<object>()
                    .Select(i => i?.ToString())
                    .FirstOrDefault(b => b != null &&
                        b.Equals(CurrentCharacter.Background, StringComparison.OrdinalIgnoreCase));

                if (bgMatch != null)
                    cmbBackground.SelectedItem = bgMatch;
            }

            if (!string.IsNullOrEmpty(CurrentCharacter.Class))
            {
                string? classMatch = cmbClass.Items.Cast<object>()
                    .Select(i => i?.ToString())
                    .FirstOrDefault(c => c != null &&
                        c.Equals(CurrentCharacter.Class, StringComparison.OrdinalIgnoreCase));

                if (classMatch != null)
                    cmbClass.SelectedItem = classMatch;
            }

            // Subclass if present and combo is ready
            if (!string.IsNullOrEmpty(CurrentCharacter.Subclass) && cmbSubclass != null && cmbSubclass.Items.Count > 0)
            {
                string? subcMatch = cmbSubclass.Items.Cast<object>()
                    .Select(i => i?.ToString())
                    .FirstOrDefault(s => s != null &&
                        !s.Contains("Requires Level", StringComparison.OrdinalIgnoreCase) &&
                        s.Equals(CurrentCharacter.Subclass, StringComparison.OrdinalIgnoreCase));

                if (subcMatch != null)
                    cmbSubclass.SelectedItem = subcMatch;
            }
            else if (cmbSubclass != null && string.IsNullOrEmpty(CurrentCharacter.Subclass))
            {
                // Leave locked/placeholder state from PopulateSubclassDropdown; ensure no stale pick
                string? first = cmbSubclass.Items.Cast<object>().Select(o => o?.ToString()).FirstOrDefault();
                if (first != null &&
                    (first.Contains("Requires Level", StringComparison.OrdinalIgnoreCase) ||
                     first.StartsWith("(No", StringComparison.OrdinalIgnoreCase) ||
                     first.StartsWith("(Unlocks", StringComparison.OrdinalIgnoreCase)))
                {
                    cmbSubclass.SelectedIndex = 0;
                }
            }

            // Level / multiclass tab from ClassLevels (PDF and JSON imports)
            if (CurrentCharacter.ClassLevels != null && CurrentCharacter.ClassLevels.Count > 0)
            {
                try { RefreshLevelMulticlassTab(); }
                catch { /* UI may not be fully ready */ }
            }

            // Ability scores — for PDF, reverse base so final ≈ PDF final after racial
            if (fromPdf && CurrentCharacter.AbilityScores != null)
            {
                RestoreAbilityScoresFromFinals();
            }
            else
            {
                RestoreAbilityScores();
            }

            // Race-granted skill from saved character (after race change handlers)
            if (!string.IsNullOrEmpty(CurrentCharacter.RaceGrantedSkill))
            {
                raceGrantedSkill = CurrentCharacter.RaceGrantedSkill;
                if (cmbRaceSkillChoice != null &&
                    cmbRaceSkillChoice.Items.Cast<object>().Any(i =>
                        string.Equals(i?.ToString(), raceGrantedSkill, StringComparison.OrdinalIgnoreCase)))
                {
                    cmbRaceSkillChoice.SelectedItem = raceGrantedSkill;
                }
            }

            if (!string.IsNullOrEmpty(CurrentCharacter.HighElfCantrip))
            {
                highElfCantrip = CurrentCharacter.HighElfCantrip;
                if (cmbHighElfCantrip != null &&
                    cmbHighElfCantrip.Items.Cast<object>().Any(i =>
                        string.Equals(i?.ToString(), highElfCantrip, StringComparison.OrdinalIgnoreCase)))
                {
                    cmbHighElfCantrip.SelectedItem = highElfCantrip;
                }
            }

            RestoreSkills();
            RestoreSelectedFeat();
            // Spells grids may have been rebuilt on class change — re-apply after that
            RestoreCantrips();
            RestoreLevel1Spells();
            RestoreEquipment();

            // HP / AC / Initiative from imported values (after UpdateStatDisplays may overwrite HP)
            UpdateStatDisplays();
            UpdateSkillTabLabels();
            UpdateFeatsTabVisibility();
            try { UpdateRacialSpellsLabel(); } catch { /* ignore */ }
            try { UpdateSubclassSpellsLabel(); } catch { /* ignore */ }

            // Re-apply imported HP/AC/initiative so PDF values win over recalculated ones
            if (CurrentCharacter.HitPoints > 0 && txtHitPoints != null)
                txtHitPoints.Text = CurrentCharacter.HitPoints.ToString();
            else
                UpdateHitPoints();

            if (CurrentCharacter.ArmorClass > 0)
            {
                if (!string.IsNullOrWhiteSpace(CurrentCharacter.EquippedACDisplay) && txtEquippedAC != null)
                {
                    txtEquippedAC.Text = CurrentCharacter.EquippedACDisplay;
                    txtEquippedAC.Visibility = Visibility.Visible;
                }
            }
            else if (txtEquippedAC != null)
            {
                txtEquippedAC.Text = "";
                // Recalc from current equipment / AC rules
                try { UpdateEquippedAC(); } catch { /* ignore */ }
            }

            if (txtInitiative != null)
            {
                int init = CurrentCharacter.Initiative;
                // Prefer live recalculation when imported initiative is default-ish empty
                if (fromPdf || CurrentCharacter.Initiative != 0)
                    txtInitiative.Text = init >= 0 ? $"+{init}" : init.ToString();
            }

            if (cmbClass.SelectedItem is string className)
                UpdateSkillChoices(className);
        }

        /// <summary>
        /// Sets base ability scores so that base + current racial (+ feat) ≈ the imported Final scores.
        /// Used when loading from PDF (which only stores final totals).
        /// </summary>
        private void RestoreAbilityScoresFromFinals()
        {
            if (CurrentCharacter.AbilityScores == null) return;

            void SetBase(string baseBoxName, string racialBoxName, int finalTarget)
            {
                var baseBox = this.FindName(baseBoxName) as TextBox;
                var racialBox = this.FindName(racialBoxName) as TextBlock;
                if (baseBox == null) return;

                int racial = 0;
                if (racialBox != null && int.TryParse(racialBox.Text.Replace("+", ""), out int r))
                {
                    if (racialBox.Text.TrimStart().StartsWith("-"))
                        racial = -Math.Abs(r);
                    else
                        racial = r;
                }

                string abilityKey = baseBoxName switch
                {
                    "txtStrBase" => "Strength",
                    "txtDexBase" => "Dexterity",
                    "txtConBase" => "Constitution",
                    "txtIntBase" => "Intelligence",
                    "txtWisBase" => "Wisdom",
                    "txtChaBase" => "Charisma",
                    _ => ""
                };
                int feat = featStatBonuses.GetValueOrDefault(abilityKey, 0);

                int baseVal = finalTarget - racial - feat;
                // Keep within a sensible range for the UI
                if (baseVal < 1) baseVal = 1;
                if (baseVal > 30) baseVal = 30;
                baseBox.Text = baseVal.ToString();
            }

            var scores = CurrentCharacter.AbilityScores;
            SetBase("txtStrBase", "txtStrRacial", scores.Strength?.Final > 0 ? scores.Strength.Final : 10);
            SetBase("txtDexBase", "txtDexRacial", scores.Dexterity?.Final > 0 ? scores.Dexterity.Final : 10);
            SetBase("txtConBase", "txtConRacial", scores.Constitution?.Final > 0 ? scores.Constitution.Final : 10);
            SetBase("txtIntBase", "txtIntRacial", scores.Intelligence?.Final > 0 ? scores.Intelligence.Final : 10);
            SetBase("txtWisBase", "txtWisRacial", scores.Wisdom?.Final > 0 ? scores.Wisdom.Final : 10);
            SetBase("txtChaBase", "txtChaRacial", scores.Charisma?.Final > 0 ? scores.Charisma.Final : 10);
        }

        private void UploadAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Image Files|*.jpg;*.png;*.jpeg;*.gif" };
            if (dlg.ShowDialog() == true)
            {
                var img = new BitmapImage(new Uri(dlg.FileName));
                imgAvatar.Source = img;
                byte[] bytes = File.ReadAllBytes(dlg.FileName);
                avatarBase64 = Convert.ToBase64String(bytes);
                lblAvatarStatus.Text = "Avatar loaded ✓";
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Wire up the events safely after everything is loaded
            txtStrBase.TextChanged += Stat_TextChanged;
            txtDexBase.TextChanged += Stat_TextChanged;
            txtConBase.TextChanged += Stat_TextChanged;
            txtIntBase.TextChanged += Stat_TextChanged;
            txtWisBase.TextChanged += Stat_TextChanged;
            txtChaBase.TextChanged += Stat_TextChanged;
            cmbSubclass.SelectionChanged += cmbSubclass_SelectionChanged;
            cmbWeaponType.SelectionChanged += cmbWeaponType_SelectionChanged;

            dgWeaponRef.ItemsSource = GameData.SimpleWeapons;     // Default to Simple Weapons
            dgArmorRef.ItemsSource = GameData.AllArmors;
            dgCantrips.SelectionChanged += dgCantrips_SelectionChanged;
            dgSpells1.SelectionChanged += dgSpells1_SelectionChanged;

            StatMethod_Changed(null, null);
            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
            if (cmbClass.SelectedItem is string className)
                UpdateSkillChoices(className);
        }

        // Additional helper methods for point buy validation, live bonus calculation, etc. are fully implemented in the complete version.
    }

    public class SkillProficiency : INotifyPropertyChanged
    {
        public string SkillName { get; set; }
        public string Ability { get; set; }
        public bool IsBackgroundProficiency { get; set; } = false;

        private bool _isSelectable = true;
        /// <summary>
        /// When false, the proficiency checkbox is disabled (not a class choice, or granted by race/background/import).
        /// </summary>
        public bool IsSelectable
        {
            get => _isSelectable;
            set
            {
                if (_isSelectable != value)
                {
                    _isSelectable = value;
                    OnPropertyChanged(nameof(IsSelectable));
                }
            }
        }

        private bool _isExpertiseSelectable;
        /// <summary>Expertise checkbox enabled only when proficient and Expertise slots remain (or already expert).</summary>
        public bool IsExpertiseSelectable
        {
            get => _isExpertiseSelectable;
            set
            {
                if (_isExpertiseSelectable != value)
                {
                    _isExpertiseSelectable = value;
                    OnPropertyChanged(nameof(IsExpertiseSelectable));
                }
            }
        }

        private bool _isProficient;
        public bool IsProficient
        {
            get => _isProficient;
            set
            {
                if (_isProficient != value)
                {
                    _isProficient = value;
                    OnPropertyChanged(nameof(IsProficient));

                    // Expertise requires proficiency
                    if (!_isProficient && _isExpertise)
                    {
                        _isExpertise = false;
                        OnPropertyChanged(nameof(IsExpertise));
                    }

                    if (Application.Current.MainWindow is MainWindow main)
                    {
                        main.UpdateSkillBonuses();
                        main.UpdateExpertiseSelectableState();
                    }
                }
            }
        }

        private bool _isExpertise;
        public bool IsExpertise
        {
            get => _isExpertise;
            set
            {
                bool next = value && _isProficient;
                if (_isExpertise != next)
                {
                    _isExpertise = next;
                    OnPropertyChanged(nameof(IsExpertise));

                    if (Application.Current.MainWindow is MainWindow main)
                    {
                        main.UpdateSkillBonuses();
                        main.UpdateExpertiseSelectableState();
                    }
                }
            }
        }

        /// <summary>Set expertise without re-entering bonus/slot refresh (for reconcile loops).</summary>
        public void SetExpertiseQuiet(bool value)
        {
            bool next = value && _isProficient;
            if (_isExpertise == next) return;
            _isExpertise = next;
            OnPropertyChanged(nameof(IsExpertise));
        }

        private string _bonus = "+0";
        public string Bonus
        {
            get => _bonus;
            set
            {
                if (_bonus != value)
                {
                    _bonus = value;
                    OnPropertyChanged(nameof(Bonus));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SkillProficiency(string name, string ability)
        {
            SkillName = name;
            Ability = ability;
        }
    }

    public class SelectableSpell : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public string DamageDice { get; set; } = "";
        public string RollType { get; set; } = "";
        public string DamageType { get; set; } = "";
        public string Description { get; set; } = "";
        public Spell FullSpell { get; set; }

        // === NEW: Use IsChecked for the checkbox ===
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));

                    if (!_isChecked)
                    {
                        AssignedClassKey = "";
                        AssignedClassDisplay = "";
                    }

                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.UpdateCantripCounter();
                        mainWindow.UpdateSpellCounter();
                    }
                }
            }
        }

        // Keep IsSelectable (controls whether checkbox can be clicked)
        private bool _isSelectable = true;
        public bool IsSelectable
        {
            get => _isSelectable;
            set
            {
                if (_isSelectable != value)
                {
                    _isSelectable = value;
                    OnPropertyChanged(nameof(IsSelectable));
                }
            }
        }

        /// <summary>Class key this cantrip counts against (multiclass budgets).</summary>
        private string _assignedClassKey = "";
        public string AssignedClassKey
        {
            get => _assignedClassKey;
            set
            {
                if (_assignedClassKey != value)
                {
                    _assignedClassKey = value ?? "";
                    OnPropertyChanged(nameof(AssignedClassKey));
                }
            }
        }

        /// <summary>UI label for the class that owns this selection.</summary>
        private string _assignedClassDisplay = "";
        public string AssignedClassDisplay
        {
            get => _assignedClassDisplay;
            set
            {
                if (_assignedClassDisplay != value)
                {
                    _assignedClassDisplay = value ?? "";
                    OnPropertyChanged(nameof(AssignedClassDisplay));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
