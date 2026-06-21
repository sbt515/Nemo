using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
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

// Alias to avoid conflicts with PdfSharp
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
        private string baseClassDescription = "";
        private string raceGrantedSkill = "";
        private List<string> currentRaceAutomaticSkills = new List<string>();
        private List<string> pickedWeapons = new List<string>();
        // Tracks active weapon radio button choices
        private Dictionary<string, (int Count, string WeaponType)> activeWeaponChoices = new();
        private string backgroundLanguage1 = "";
        private string backgroundLanguage2 = "";
        private string currentBackgroundEquipmentAdded = "";   // ← NEW
        private string highElfCantrip = "";
        private bool raceHasInnateSpellcasting = false;
        private List<Feat> allFeats = new();
        private Dictionary<string, int> featStatBonuses = new();
        private int featInitiativeBonus = 0;
        private string currentFeatAbilityChoice = "";
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
        public ObservableCollection<SelectableSpell> spell1Options = new();
        private CollectionViewSource spell1ViewSource = new CollectionViewSource();
        private readonly Brush AccentGreen = (Brush)new BrushConverter().ConvertFromString("#7CFC00");
        private readonly Brush AccentGray = (Brush)new BrushConverter().ConvertFromString("#2A2A2A");


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => StatMethod_Changed(null, null);
            this.Loaded += MainWindow_Loaded;
            LoadAllCombos();
            InitializeSkills();
            rbPointBuy.IsChecked = true;
            StatMethod_Changed(null, null);
        }

        private void LoadAllCombos()
        {
            cmbRace.ItemsSource = GameData.RaceData.Keys.OrderBy(r => r).ToList();
            cmbBackground.ItemsSource = GameData.AllBackgrounds;
            cmbClass.ItemsSource = GameData.ClassData.Keys.OrderBy(c => c).ToList();
        }

        private void cmbRace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRace.SelectedItem is not string race || !GameData.RaceData.ContainsKey(race)) return;

            var data = GameData.RaceData[race];
            string bonuses = string.Join(", ", data.AbilityBonuses.Select(kv => $"+{kv.Value} {kv.Key}"));

            // Filter out any trait that is just listing languages
            var filteredTraits = data.Traits
                .Where(t => !t.Contains("Common +", StringComparison.OrdinalIgnoreCase) &&
                            !t.Contains("Languages:", StringComparison.OrdinalIgnoreCase))
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

            if (race == "Custom Lineage" || race == "Human" || race == "Variant Human")
            {
                pnlRaceSkillChoice.Visibility = Visibility.Visible;

                // Set the correct header label based on race
                if (race == "Human")
                {
                    lblRaceSkillHeader.Text = "HUMAN SKILL VERSATILITY";
                    pnlFlexibleBonuses.Visibility = Visibility.Collapsed;
                    rbRaceDarkvision.Visibility = Visibility.Collapsed;     // Hide Darkvision
                    rbRaceSkill.Visibility = Visibility.Visible;
                    rbRaceSkill.IsChecked = true;                           // Auto-select skill
                    cmbRaceSkillChoice.Visibility = Visibility.Visible;
                }
                else if (race == "Variant Human")
                {
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

                    // === FIX: Apply the default bonuses immediately ===
                    FlexibleBonus_Changed(null, null);
                }
                else if (race == "Half-Elf")
                {
                    pnlFlexibleBonuses.Visibility = Visibility.Visible;
                    SetupFlexibleBonusPickers(race);
                    FlexibleBonus_Changed(null, null);   // Apply defaults immediately
                }
                else // Custom Lineage
                {
                    lblRaceSkillHeader.Text = "CUSTOM LINEAGE CHOICES";
                    pnlFlexibleBonuses.Visibility = Visibility.Visible;
                    rbRaceDarkvision.Visibility = Visibility.Visible;
                    rbRaceSkill.Visibility = Visibility.Visible;
                    rbRaceDarkvision.IsChecked = true;   // Default to Darkvision
                    cmbRaceSkillChoice.Visibility = Visibility.Collapsed;
                }

                // Populate skill dropdown
                cmbRaceSkillChoice.ItemsSource = allSkills.Select(s => s.SkillName).ToList();
                if (string.IsNullOrEmpty(raceGrantedSkill))
                    cmbRaceSkillChoice.SelectedIndex = 0;

                cmbRaceSkillChoice.SelectionChanged -= RaceGrantedSkill_Changed; // Prevent duplicates
                cmbRaceSkillChoice.SelectionChanged += RaceGrantedSkill_Changed;
            }
            else
            {
                pnlRaceSkillChoice.Visibility = Visibility.Collapsed;
                if (race != "Half-Elf")
                    pnlFlexibleBonuses.Visibility = Visibility.Collapsed;
            }

            // Show or hide Feats tab
            bool showFeatsTab = GameData.FeatGrantingRaces.Contains(race);
            tabFeats.Visibility = showFeatsTab ? Visibility.Visible : Visibility.Collapsed;

            if (showFeatsTab && dgFeats.ItemsSource == null)
            {
                GameData.InitializeFeats();
                dgFeats.ItemsSource = GameData.AllFeats;
                dgFeats.SelectionChanged += DgFeats_SelectionChanged; // We'll add this next
            }

            // Cleanup when switching away from races that grant a skill choice
            if ((race != "Custom Lineage" && race != "Human" && race != "Variant Human")
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
                abilities.Remove("Charisma"); // Half-Elf cannot choose Charisma again

            cmbFlexibleBonus1.ItemsSource = abilities;
            cmbFlexibleBonus2.ItemsSource = abilities;

            cmbFlexibleBonus1.SelectedIndex = 0;
            cmbFlexibleBonus2.SelectedIndex = 1;

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
                var details = new System.Text.StringBuilder();
                details.AppendLine($"**{spell.Name}** (Cantrip)");
                details.AppendLine($"School: {spell.School}");
                details.AppendLine($"Casting Time: {spell.CastingTime}");
                details.AppendLine($"Range: {spell.Range}");
                details.AppendLine($"Components: {spell.Components}");
                details.AppendLine($"Duration: {spell.Duration}" + (spell.IsConcentration ? " (Concentration)" : ""));

                if (!string.IsNullOrWhiteSpace(spell.DamageDice))
                    details.AppendLine($"Damage: {spell.DamageDice} {spell.DamageType}");

                if (!string.IsNullOrWhiteSpace(spell.RollType))
                    details.AppendLine($"Roll: {spell.RollType}");

                details.AppendLine();
                details.AppendLine(spell.Description);

                txtHighElfCantripPreview.Text = details.ToString();
                pnlHighElfCantripPreview.Visibility = Visibility.Visible;
            }

            UpdateRacialSpellsLabel();
        }

        private string GetRacialCantrip()
        {
            string race = cmbRace.SelectedItem?.ToString() ?? "";
            string subrace = cmbSubrace.SelectedItem?.ToString() ?? "";

            // Fixed racial cantrips
            if (race == "Aasimar")
                return "Light";

            if (race == "Elf" && subrace == "Drow (Dark Elf)")
                return "Dancing Lights";

            if (race == "Gnome" && subrace == "Forest Gnome")
                return "Minor Illusion";

            // High Elf = player-chosen Wizard cantrip
            if (race == "Elf" && subrace == "High Elf" && !string.IsNullOrEmpty(highElfCantrip))
                return highElfCantrip;

            return "";
        }

        private void UpdateRacialSpellsLabel()
        {
            if (lblRacialSpells == null) return;

            string racialCantrip = GetRacialCantrip();

            if (!string.IsNullOrEmpty(racialCantrip))
            {
                string race = cmbRace.SelectedItem?.ToString() ?? "";
                string subrace = cmbSubrace.SelectedItem?.ToString() ?? "";

                string displayName = !string.IsNullOrEmpty(subrace) ? subrace : race;

                lblRacialSpells.Text = $"Racial spells ({displayName}): {racialCantrip}";
                lblRacialSpells.Foreground = AccentGreen;
            }
            else
            {
                lblRacialSpells.Text = "Racial spells: (none yet)";
                lblRacialSpells.Foreground = (Brush)new BrushConverter().ConvertFromString("#AAA");
            }
        }

        private void cmbSubrace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRace.SelectedItem is not string race ||
                cmbSubrace.SelectedItem is not string subraceName ||
                !GameData.RaceSubraces.ContainsKey(race)) return;

            var subrace = GameData.RaceSubraces[race].FirstOrDefault(s => s.Name == subraceName);
            if (subrace == null) return;

            // IMPORTANT: Reset to ONLY the base race bonuses first
            var baseRace = GameData.RaceData[race];
            racialBonuses = new Dictionary<string, int>(baseRace.AbilityBonuses);

            // Now apply ONLY the selected subrace bonus
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
            string bonuses = string.Join(", ", racialBonuses.Select(kv => $"+{kv.Value} {kv.Key}"));

            var filteredBaseTraits = baseRace.Traits
                .Where(t => !t.Contains("Common +", StringComparison.OrdinalIgnoreCase) &&
                            !t.Contains("Languages:", StringComparison.OrdinalIgnoreCase))
                .ToList();

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

        /// <summary>
        /// Appends the new detailed ClassFeature entries (name + description) to the given StringBuilder.
        /// Skips any features whose name contains "Spellcasting" (we already show spellcasting stats elsewhere).
        /// Also skips any names passed in excludeNames (e.g. "Domain Spells" for Cleric, "Expanded Spell List" for Warlock,
        /// because those are already shown via the DOMAIN SPELLS / PATRON SPELLS lines above).
        /// If sectionTitle is provided, it is printed as a header first.
        /// </summary>
        private void AppendDetailedFeatures(System.Text.StringBuilder sb, List<ClassFeature> features, string sectionTitle = null, IEnumerable<string> excludeNames = null)
        {
            if (features == null || features.Count == 0) return;

            // Build a case-insensitive set of names to exclude (in addition to the Spellcasting rule)
            var excludeSet = (excludeNames ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filtered = features
                .Where(f => !string.IsNullOrWhiteSpace(f.Name) &&
                            !f.Name.Contains("Spellcasting", StringComparison.OrdinalIgnoreCase) &&
                            !excludeSet.Contains(f.Name.Trim()))
                .ToList();

            if (filtered.Count == 0) return;

            if (!string.IsNullOrEmpty(sectionTitle))
            {
                sb.AppendLine(sectionTitle);
            }

            foreach (var f in filtered)
            {
                sb.AppendLine($"• {f.Name}: {f.Description}");
            }

            sb.AppendLine();
        }

        private void cmbClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClass.SelectedItem is not string className || !GameData.ClassData.ContainsKey(className)) return;

            var data = GameData.ClassData[className];

            int hitDie = 8;
            if (!string.IsNullOrEmpty(data.HitDie))
            {
                var parts = data.HitDie.Split('d');
                if (parts.Length == 2 && int.TryParse(parts[1], out int die))
                    hitDie = die;
            }

            string subclassLevelText = className switch
            {
                "Cleric" or "Sorcerer" or "Warlock" => "SUBCLASSES (available at level 1)",
                "Druid" or "Wizard" => "SUBCLASSES (available at level 2)",
                _ => "SUBCLASSES (available at level 3)"
            };

            var desc = new System.Text.StringBuilder();

            desc.AppendLine($"HIT DICE: {data.HitDie}");
            desc.AppendLine($"HP AT 1ST LEVEL: {hitDie} + Con mod");
            desc.AppendLine();

            if (data.SavingThrowProficiencies.Count > 0)
            {
                desc.AppendLine("SAVING THROW PROFICIENCIES");
                desc.AppendLine("• " + string.Join(", ", data.SavingThrowProficiencies));
                desc.AppendLine();
            }

            if (data.ArmorProficiencies.Count > 0)
            {
                desc.AppendLine("ARMOR PROFICIENCIES");
                desc.AppendLine("• " + string.Join(", ", data.ArmorProficiencies));
                desc.AppendLine();
            }

            if (data.WeaponProficiencies.Count > 0)
            {
                desc.AppendLine("WEAPON PROFICIENCIES");
                desc.AppendLine("• " + string.Join(", ", data.WeaponProficiencies));
                desc.AppendLine();
            }

            desc.AppendLine($"SKILL PROFICIENCIES ({data.SkillChoiceCount} skills from {string.Join(", ", data.SkillChoices)})");
            desc.AppendLine();

            if (data.Spellcasting)
            {
                desc.AppendLine("SPELLCASTING");
                desc.AppendLine($"Ability: {data.SpellAbility}");
                desc.AppendLine($"Cantrips: {data.CantripsKnown}");

                int slotsAtLevel1 = className switch
                {
                    "Paladin" or "Ranger" => 0,
                    "Warlock" => 1,
                    _ => 2
                };
                desc.AppendLine($"1st-level Spell Slots: {slotsAtLevel1}");

                if (data.SpellsKnown > 0)
                    desc.AppendLine($"Spells Known: {data.SpellsKnown}");
                else if (!string.IsNullOrEmpty(data.SpellsPrepared))
                    desc.AppendLine($"Spells Prepared: {data.SpellsPrepared}");

                desc.AppendLine();
                PopulateSpells();
            }

            // Use the new detailed class features (much richer than any older terse lists)
            if (GameData.ClassLevel1Features.TryGetValue(className, out var classFeats))
                AppendDetailedFeatures(desc, classFeats, "CLASS FEATURES (Level 1)", excludeNames: null);

            desc.AppendLine(subclassLevelText);
            desc.AppendLine(string.Join(", ", data.Subclasses));

            // Save the base description so we can replace only the subclass part later
            baseClassDescription = desc.ToString();

            txtClassDetails.Text = baseClassDescription;

            // Subclass dropdown
            if (className == "Cleric")
            {
                cmbSubclass.IsEnabled = true;
                cmbSubclass.ItemsSource = GameData.ClericSubclasses.Keys.OrderBy(k => k).ToList();
                cmbSubclass.SelectedIndex = 0;
            }
            else if (className == "Warlock")
            {
                cmbSubclass.IsEnabled = true;
                cmbSubclass.ItemsSource = GameData.WarlockSubclasses.Keys.OrderBy(k => k).ToList();
                cmbSubclass.SelectedIndex = 0;
            }
            else if (className == "Sorcerer")
            {
                cmbSubclass.IsEnabled = true;
                cmbSubclass.ItemsSource = GameData.SorcererSubclasses.Keys.OrderBy(k => k).ToList();
                cmbSubclass.SelectedIndex = 0;
            }
            else if (className == "Druid" || className == "Wizard")
            {
                cmbSubclass.IsEnabled = false;
                cmbSubclass.ItemsSource = new List<string> { "Requires Level 2" };
                cmbSubclass.SelectedIndex = 0;
            }
            else
            {
                cmbSubclass.IsEnabled = false;
                cmbSubclass.ItemsSource = new List<string> { "Requires Level 3" };
                cmbSubclass.SelectedIndex = 0;
            }

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

            // Show the first subclass immediately
            if (className == "Cleric" || className == "Warlock" || className == "Sorcerer")
                cmbSubclass_SelectionChanged(null, null);
        }

        private void cmbSubclass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClass.SelectedItem is not string className) return;

            var extra = new System.Text.StringBuilder();

            if (className == "Cleric" && cmbSubclass.SelectedItem is string subName && GameData.ClericSubclasses.ContainsKey(subName))
            {
                var sub = GameData.ClericSubclasses[subName];
                extra.AppendLine($"\n\n=== {sub.Name.ToUpper()} FEATURES (Level 1) ===");
                if (sub.AdditionalCantrips.Count > 0) extra.AppendLine($"ADDITIONAL CANTRIPS: {string.Join(", ", sub.AdditionalCantrips)}");
                if (sub.DomainSpells.Count > 0) extra.AppendLine($"DOMAIN SPELLS: {string.Join(", ", sub.DomainSpells)}");
                if (sub.ArmorProficiencies.Count > 0) extra.AppendLine($"ARMOR: {string.Join(", ", sub.ArmorProficiencies)}");
                if (sub.WeaponProficiencies.Count > 0) extra.AppendLine($"WEAPONS: {string.Join(", ", sub.WeaponProficiencies)}");

                // Use the new detailed subclass features (name + full description) instead of the old terse UniqueAbilities list.
                // We also exclude "Domain Spells" here because we already printed the DOMAIN SPELLS line above.
                if (GameData.SubclassLevel1Features.TryGetValue(subName, out var subFeats))
                {
                    extra.AppendLine();
                    AppendDetailedFeatures(extra, subFeats, excludeNames: new[] { "Domain Spells" });
                }
            }
            else if (className == "Warlock" && cmbSubclass.SelectedItem is string patronName && GameData.WarlockSubclasses.ContainsKey(patronName))
            {
                var sub = GameData.WarlockSubclasses[patronName];
                extra.AppendLine($"\n\n=== {sub.Name.ToUpper()} FEATURES (Level 1) ===");
                if (sub.DomainSpells.Count > 0) extra.AppendLine($"PATRON SPELLS: {string.Join(", ", sub.DomainSpells)}");
                if (sub.ArmorProficiencies.Count > 0) extra.AppendLine($"ARMOR: {string.Join(", ", sub.ArmorProficiencies)}");
                if (sub.WeaponProficiencies.Count > 0) extra.AppendLine($"WEAPONS: {string.Join(", ", sub.WeaponProficiencies)}");

                // Use the new detailed subclass features instead of the old terse UniqueAbilities list.
                // We exclude "Expanded Spell List" because we already printed the PATRON SPELLS line above.
                if (GameData.SubclassLevel1Features.TryGetValue(patronName, out var subFeats))
                {
                    extra.AppendLine();
                    AppendDetailedFeatures(extra, subFeats, excludeNames: new[] { "Expanded Spell List" });
                }
            }
            else if (className == "Sorcerer" && cmbSubclass.SelectedItem is string sorcName && GameData.SorcererSubclasses.ContainsKey(sorcName))
            {
                var sub = GameData.SorcererSubclasses[sorcName];
                extra.AppendLine($"\n\n=== {sub.Name.ToUpper()} FEATURES (Level 1) ===");
                if (sub.AdditionalSpells.Count > 0) extra.AppendLine($"ORIGIN SPELLS: {string.Join(", ", sub.AdditionalSpells)}");

                // Use the new detailed subclass features instead of the old terse UniqueAbilities list.
                if (GameData.SubclassLevel1Features.TryGetValue(sorcName, out var subFeats))
                {
                    extra.AppendLine();
                    AppendDetailedFeatures(extra, subFeats);
                }
            }

            txtClassDetails.Text = baseClassDescription + extra.ToString();
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

        private void ValidatePointBuy()
        {
            if (lblPointBuy == null) return;

            int totalPoints = 27;
            int used = 0;
            bool hasInvalid = false;

            var costs = new Dictionary<int, int>
    {
        {8, 0}, {9, 1}, {10, 2}, {11, 3}, {12, 4}, {13, 5}, {14, 7}, {15, 9}
    };

            string[] statNames = { "Str", "Dex", "Con", "Int", "Wis", "Cha" };

            foreach (var name in statNames)
            {
                var txt = this.FindName($"txt{name}Base") as TextBox;
                if (txt == null) continue;

                if (!int.TryParse(txt.Text, out int val))
                {
                    val = 8;
                    txt.Text = "8";
                }

                if (costs.ContainsKey(val))
                    used += costs[val];
                else
                    used += 99; // safety
            }

            int remaining = totalPoints - used;

            lblPointBuy.Text = $"Points Remaining: {remaining}";
            lblPointBuy.Foreground = (remaining < 0 || hasInvalid) ? Brushes.Red :
                                     (remaining == 0 ? AccentGreen : Brushes.Yellow);
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

            if (val < 8) txt.Text = "8";
            if (val > 15) txt.Text = "15";
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
            else if (lblPointBuy != null)
                lblPointBuy.Text = "";

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

        public void UpdateSkillChoices(string className)
        {
            if (string.IsNullOrEmpty(className) ||
                !GameData.ClassData.ContainsKey(className) ||
                allSkills == null ||
                lblClassSkillCounter == null)
                return;

            var classData = GameData.ClassData[className];
            var allowedSkills = classData.SkillChoices;
            int maxAllowed = classData.SkillChoiceCount;

            int currentlySelected = 0;

            foreach (var skill in allSkills)
            {
                bool isClassSkill = allowedSkills.Contains(skill.SkillName);
                bool isRaceOrBackground = skill.IsBackgroundProficiency ||
                                          currentRaceAutomaticSkills.Contains(skill.SkillName, StringComparer.OrdinalIgnoreCase) ||
                                          skill.SkillName == raceGrantedSkill;

                // Only class skills that are not already granted by race/background can be selected
                if (isClassSkill && !isRaceOrBackground)
                {
                    skill.IsSelectable = true;
                }
                else
                {
                    skill.IsSelectable = false;
                }

                if (isClassSkill && skill.IsProficient && !isRaceOrBackground)
                    currentlySelected++;
            }

            lblClassSkillCounter.Text = $"{currentlySelected} / {maxAllowed} class skills selected";

            if (currentlySelected > maxAllowed)
                lblClassSkillCounter.Foreground = Brushes.Red;
            else
                lblClassSkillCounter.Foreground = Brushes.White;

            dgSkills.Items.Refresh();   // Needed so IsEnabled updates visually
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

            foreach (var skill in allSkills)
            {
                int mod = skill.Ability switch
                {
                    "Str" => GetModifierFromText(txtStrMod.Text),
                    "Dex" => GetModifierFromText(txtDexMod.Text),
                    "Con" => GetModifierFromText(txtConMod.Text),
                    "Int" => GetModifierFromText(txtIntMod.Text),
                    "Wis" => GetModifierFromText(txtWisMod.Text),
                    "Cha" => GetModifierFromText(txtChaMod.Text),
                    _ => 0
                };

                int totalBonus = mod + (skill.IsProficient ? proficiencyBonus : 0);
                skill.Bonus = totalBonus >= 0 ? $"+{totalBonus}" : totalBonus.ToString();
            }

            //dgSkills.Items.Refresh();
        }

        private void UpdateSavingThrows()
        {
            if (cmbClass.SelectedItem is not string className || !GameData.ClassData.ContainsKey(className)) return;

            var data = GameData.ClassData[className];
            var profSaves = data.SavingThrowProficiencies;

            int strMod = GetModifierFromText(txtStrMod.Text);
            int dexMod = GetModifierFromText(txtDexMod.Text);
            int conMod = GetModifierFromText(txtConMod.Text);
            int intMod = GetModifierFromText(txtIntMod.Text);
            int wisMod = GetModifierFromText(txtWisMod.Text);
            int chaMod = GetModifierFromText(txtChaMod.Text);

            txtStrSave.Text = FormatSave(strMod, profSaves.Contains("Strength"));
            txtDexSave.Text = FormatSave(dexMod, profSaves.Contains("Dexterity"));
            txtConSave.Text = FormatSave(conMod, profSaves.Contains("Constitution"));
            txtIntSave.Text = FormatSave(intMod, profSaves.Contains("Intelligence"));
            txtWisSave.Text = FormatSave(wisMod, profSaves.Contains("Wisdom"));
            txtChaSave.Text = FormatSave(chaMod, profSaves.Contains("Charisma"));

            txtInitiative.Text = txtDexMod.Text;   // Initiative = Dex modifier
        }

        private string FormatSave(int mod, bool proficient)
        {
            int total = mod + (proficient ? 2 : 0);
            return total >= 0 ? $"+{total}" : total.ToString();
        }

        // Helper method to parse modifier text
        private int GetModifierFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            // int.Parse handles "+3" and "-1" perfectly — no manual sign stripping needed
            string cleanText = text.Replace("+", "").Trim();
            return int.TryParse(cleanText, out int mod) ? mod : 0;
        }

        private void UpdateEquipmentProficiencySummary()
        {
            pnlProficiencySummary.Children.Clear();

            var armor = new List<string>();
            var weapons = new List<string>();

            // === CLASS PROFICIENCIES ===
            string className = cmbClass.SelectedItem as string;
            if (className != null && GameData.ClassData.TryGetValue(className, out var classData))
            {
                armor.AddRange(classData.ArmorProficiencies);
                weapons.AddRange(classData.WeaponProficiencies);
            }

            // === SUBCLASS PROFICIENCIES ===
            if (className == "Cleric" && cmbSubclass.SelectedItem is string subName && GameData.ClericSubclasses.TryGetValue(subName, out var sub))
            {
                armor.AddRange(sub.ArmorProficiencies);
                weapons.AddRange(sub.WeaponProficiencies);
            }
            else if (className == "Warlock" && cmbSubclass.SelectedItem is string patronName && GameData.WarlockSubclasses.TryGetValue(patronName, out var warlockSub))
            {
                armor.AddRange(warlockSub.ArmorProficiencies);
                weapons.AddRange(warlockSub.WeaponProficiencies);
            }

            // === RACE / SUBRACE PROFICIENCIES (correct categorization) ===
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
                    Text = "• " + string.Join(", ", armor.Distinct()),
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
                    Text = "• " + string.Join(", ", weapons.Distinct()),
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        private void DgFeats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFeats.SelectedItem is not Feat selectedFeat) return;

            baseFeatDescription = $"**Prerequisites:** {selectedFeat.Prerequisites}\n\n{selectedFeat.FullDescription}";
            txtFeatDetails.Text = baseFeatDescription;

            string name = selectedFeat.Name.ToLowerInvariant();

            // === FULL RESET ===
            pnlFeatStatChoices.Visibility = Visibility.Collapsed;
            cmbFeatStatChoice1.ItemsSource = null;
            cmbFeatStatChoice1.Visibility = Visibility.Visible;
            cmbFeatStatChoice2.ItemsSource = null;
            cmbFeatStatChoice2.Visibility = Visibility.Collapsed;
            cmbFeatStatChoice3.ItemsSource = null;
            cmbFeatStatChoice3.Visibility = Visibility.Collapsed;
            brdFeatSpellPreview.Visibility = Visibility.Collapsed;
            txtFeatSpellDetails.Text = "";
            featSelectedSpell = "";
            lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE(S) TO INCREASE";

            // === 1. RESILIENT → Only ability score (cmb1) ===
            if (name.Contains("resilient"))
            {
                var allAbilities = new List<string> { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = allAbilities;
                cmbFeatStatChoice1.SelectedIndex = 0;
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE TO INCREASE";
                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 2. SPELL SNIPER → Only cantrip with attack roll (cmb2) ===
            else if (name.Contains("spell sniper"))
            {
                var attackCantrips = GameData.AllCantrips
                    .Where(c => !string.IsNullOrWhiteSpace(c.RollType) &&
                                c.RollType.Contains("Spell Attack", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Name)
                    .OrderBy(n => n)
                    .ToList();

                cmbFeatStatChoice2.ItemsSource = attackCantrips;
                cmbFeatStatChoice2.Visibility = Visibility.Visible;
                if (attackCantrips.Any()) cmbFeatStatChoice2.SelectedIndex = 0;

                cmbFeatStatChoice1.Visibility = Visibility.Collapsed;
                lblFeatStatChoiceHeader.Text = "CHOOSE A CANTRIP THAT REQUIRES AN ATTACK ROLL";
                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 3. FEY TOUCHED / SHADOW TOUCHED → Mental stats (cmb1) + Spell (cmb2) ===
            else if (name.Contains("fey touched") || name.Contains("shadow touched"))
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE AND SPELL";
                // Ability score dropdown
                var mentalStats = new List<string> { "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = mentalStats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;

                // Spell dropdown
                List<string> allowedSchools = name.Contains("fey touched")
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
            else if (name.Contains("artificer initiate"))
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
            // === MENTAL STATS ONLY (Int / Wis / Cha) ===
            // Used by: Telekinetic, Telepathic, Gift of the Metallic/Chromatic/Gem Dragon
            else if (name.Contains("telekinetic") ||
                     name.Contains("telepathic") ||
                     name.Contains("gift of the"))
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE TO INCREASE";

                var mentalStats = new List<string> { "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = mentalStats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                cmbFeatStatChoice1.Visibility = Visibility.Visible;

                cmbFeatStatChoice2.Visibility = Visibility.Collapsed;
                cmbFeatStatChoice3.Visibility = Visibility.Collapsed;

                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === GIFT OF THE DRAGON FEATS (fixed spells) ===
            else if (name.Contains("gift of the chromatic dragon"))
            {
                currentFeatSpellSource = "Gift of the Chromatic Dragon";
                currentFeatSpells = new List<string> { "Chromatic Orb" };
                UpdateFeatSpellsLabel();
            }
            else if (name.Contains("gift of the gem dragon"))
            {
                currentFeatSpellSource = "Gift of the Gem Dragon";
                currentFeatSpells = new List<string> { "Detect Thoughts" };
                UpdateFeatSpellsLabel();
            }
            else if (name.Contains("gift of the metallic dragon"))
            {
                currentFeatSpellSource = "Gift of the Metallic Dragon";
                currentFeatSpells = new List<string> { "Cure Wounds", "Detect Magic" };
                UpdateFeatSpellsLabel();
            }
            // === MAGIC INITIATE ===
            else if (name.Contains("magic initiate"))
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

                // Wire up class change handler
                cmbFeatStatChoice1.SelectionChanged -= MagicInitiateClass_Changed;
                cmbFeatStatChoice1.SelectionChanged += MagicInitiateClass_Changed;

                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 5. Physical feats (Slasher, Piercer, etc.) → cmb1 only ===
            else if (name.Contains("slasher") || name.Contains("piercer") ||
                     name.Contains("dual wielder") || name.Contains("weapon master"))
            {
                var physicalStats = new List<string> { "Strength", "Dexterity" };
                cmbFeatStatChoice1.ItemsSource = physicalStats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 6. Athlete → cmb1 only ===
            else if (name.Contains("athlete"))
            {
                var stats = new List<string> { "Strength", "Dexterity", "Constitution" };
                cmbFeatStatChoice1.ItemsSource = stats;
                cmbFeatStatChoice1.SelectedIndex = 0;
                pnlFeatStatChoices.Visibility = Visibility.Visible;
            }
            // === 7. Skill Expert / Prodigy → cmb1 + cmb2 + cmb3 ===
            else if (name.Contains("skill expert") || name.Contains("prodigy"))
            {
                lblFeatStatChoiceHeader.Text = "CHOOSE ABILITY SCORE AND SKILL PROFICIENCY";
                var allAbilities = new List<string> { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
                cmbFeatStatChoice1.ItemsSource = allAbilities;
                cmbFeatStatChoice1.SelectedIndex = 0;

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

            int finalVal = baseVal + racialVal + featVal;

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

            ValidatePointBuy();
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

            string className = cmbClass.SelectedItem as string;

            int hitDie = className switch
            {
                "Wizard" or "Sorcerer" => 6,
                "Cleric" or "Druid" or "Rogue" or "Bard" or "Warlock" or "Monk" => 8,
                "Fighter" or "Paladin" or "Ranger" => 10,
                "Barbarian" => 12,
                _ => 8
            };

            int hp = hitDie + conMod;

            // Hill Dwarf (Dwarven Toughness): +1 HP per level (applied to starting HP)
            if (cmbSubrace?.SelectedItem is string subrace && subrace == "Hill Dwarf")
            {
                hp += 1;
            }

            txtHitPoints.Text = hp.ToString();
        }

        private void GenerateEquipmentChoices(string className)
        {
            pickedWeapons.Clear();
            pnlEquipmentChoices.Children.Clear();
            activeWeaponChoices.Clear();
            pnlWeaponChoices.Visibility = Visibility.Visible;   // Always keep middle column visible

            if (!GameData.StartingEquipment.ContainsKey(className))
                return;

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
        }

        private void UpdateTotalEquipmentSummary()
        {
            pnlTotalEquipmentSummary.Children.Clear();

            var totalItems = new List<string>();

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

            // === BACKGROUND EQUIPMENT (as ONE item) ===
            if (cmbBackground.SelectedItem is string bg)
            {
                string bgEquip = GameData.GetBackgroundEquipment(bg);
                if (!string.IsNullOrWhiteSpace(bgEquip) && !bgEquip.Contains("See Background tab"))
                {
                    totalItems.Add("• " + bgEquip);   // ← Add as single bullet
                }
            }

            // === FINAL CLEAN DISPLAY ===
            if (totalItems.Count > 0)
            {
                foreach (var item in totalItems.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    pnlTotalEquipmentSummary.Children.Add(new TextBlock
                    {
                        Text = item,
                        Foreground = Brushes.White,
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
                pbCarryingCapacity.Foreground = (Brush)new BrushConverter().ConvertFromString("#4A7C59");

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

            // === 5. Update UI ===
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

                string displayText = $"({finalAC}): {breakdown}";
                txtEquippedAC.Text = displayText;

                if (CurrentCharacter != null)
                    CurrentCharacter.EquippedACDisplay = displayText;
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

            // ===================== MAGIC INITIATE =====================
            if (name.Contains("magic initiate"))
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
            if (name.Contains("fey touched"))
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
            if (name.Contains("shadow touched"))
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
            if (name.Contains("spell sniper"))
            {
                currentFeatSpellSource = "Spell Sniper";
                currentFeatSpells.Clear();

                if (cmbFeatStatChoice2.SelectedItem is string cantrip)
                    currentFeatSpells.Add(cantrip);

                UpdateFeatSpellsLabel();
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
            UpdateInitiative();
        }

        public bool MeetsPrerequisite(Feat feat)
        {
            if (string.IsNullOrWhiteSpace(feat.Prerequisites) || feat.Prerequisites == "None")
                return true;

            string prereq = feat.Prerequisites.ToLowerInvariant();

            // === ABILITY SCORE REQUIREMENTS ===
            if (prereq.Contains(" or higher"))
            {
                var parts = prereq.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[1], out int required))
                {
                    string ability = parts[0];
                    int score = GetFinalStat(ability);
                    return score >= required;
                }
            }

            // === SPELLCASTING FEATURE ===
            if (prereq.Contains("spellcasting"))
            {
                if (cmbClass.SelectedItem is string className &&
                    GameData.ClassData.TryGetValue(className, out var data))
                {
                    return data.Spellcasting || raceHasInnateSpellcasting;
                }
                return raceHasInnateSpellcasting;
            }

            // === RACE REQUIREMENTS ===
            string currentRace = cmbRace.SelectedItem?.ToString() ?? "";
            if (prereq.Contains("half-elf") || prereq.Contains("half-orc") || prereq.Contains("human"))
            {
                return currentRace.Contains("Half-Elf", StringComparison.OrdinalIgnoreCase) ||
                       currentRace.Contains("Half-Orc", StringComparison.OrdinalIgnoreCase) ||
                       currentRace.Contains("Human", StringComparison.OrdinalIgnoreCase) ||
                       currentRace.Contains("Variant Human", StringComparison.OrdinalIgnoreCase) ||
                       currentRace.Contains("Custom Lineage", StringComparison.OrdinalIgnoreCase);
            }

            // === FINESSE WEAPON PROFICIENCY ===
            if (prereq.Contains("finesse"))
            {
                var className = cmbClass.SelectedItem?.ToString();
                return className is "Fighter" or "Rogue" or "Monk" or "Paladin" or "Ranger" or "Bard";
            }

            // === HEALER FEAT ===
            if (prereq.Contains("healer's kit"))
            {
                // For now we assume the user has it if they picked a class that gets it (Cleric, Druid, etc.)
                // You can expand this later with actual equipment tracking
                return true; // Placeholder — improve later
            }

            // === GREAT WEAPON MASTER / POLEARM MASTER ===
            if (prereq.Contains("martial weapon"))
            {
                var className = cmbClass.SelectedItem?.ToString();
                return className is "Fighter" or "Paladin" or "Ranger" or "Barbarian" or "Monk";
            }

            return true; // Default allow
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

            var sb = new System.Text.StringBuilder();

            // Determine if it's a cantrip or a leveled spell
            bool isCantrip = spell is not LeveledSpell;
            int level = (spell as LeveledSpell)?.Level ?? 0;

            if (isCantrip)
                sb.AppendLine($"**{spell.Name}** (Cantrip)");
            else
                sb.AppendLine($"**{spell.Name}** (Level {level})");

            sb.AppendLine($"School: {spell.School}");
            sb.AppendLine($"Casting Time: {spell.CastingTime}");
            sb.AppendLine($"Range: {spell.Range}");
            sb.AppendLine($"Components: {spell.Components}");
            sb.AppendLine($"Duration: {spell.Duration}" + (spell.IsConcentration ? " (Concentration)" : ""));

            if (!string.IsNullOrWhiteSpace(spell.DamageDice))
                sb.AppendLine($"Dice: {spell.DamageDice} {spell.DamageType}");

            if (!string.IsNullOrWhiteSpace(spell.RollType))
                sb.AppendLine($"Roll: {spell.RollType}");

            sb.AppendLine();
            sb.AppendLine(spell.Description);

            txtFeatSpellDetails.Text = sb.ToString();
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
            int dexMod = GetModifierFromText(txtDexMod.Text);
            int total = dexMod + featInitiativeBonus;
            txtInitiative.Text = total >= 0 ? $"+{total}" : total.ToString();
        }

        private void InitializeSkills()
        {
            allSkills = new ObservableCollection<SkillProficiency>(GameData.CreateAllSkills());
            dgSkills.ItemsSource = allSkills;
        }

        private void UpdateSpellTabVisibility()
        {
            if (cmbClass.SelectedItem is string className &&
                GameData.ClassData.TryGetValue(className, out var classData))
            {
                bool hasSpellcasting = classData.Spellcasting || raceHasInnateSpellcasting;
                tabSpells.Visibility = hasSpellcasting ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                tabSpells.Visibility = raceHasInnateSpellcasting ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateSubclassSpellsLabel()
        {
            if (lblSubclassSpells == null) return;

            if (cmbClass.SelectedItem is not string className ||
                cmbSubclass.SelectedItem is not string subName)
            {
                lblSubclassSpells.Text = "Subclass spells: (none yet)";
                lblSubclassSpells.Foreground = (Brush)new BrushConverter().ConvertFromString("#AAA");
                return;
            }

            List<string> spells = new();

            if (className == "Cleric" && GameData.ClericSubclasses.ContainsKey(subName))
            {
                spells = GameData.ClericSubclasses[subName].DomainSpells;
            }
            else if (className == "Warlock" && GameData.WarlockSubclasses.ContainsKey(subName))
            {
                spells = GameData.WarlockSubclasses[subName].DomainSpells;
            }
            else if (className == "Sorcerer" && GameData.SorcererSubclasses.ContainsKey(subName))
            {
                spells = GameData.SorcererSubclasses[subName].AdditionalSpells;
            }

            if (spells.Count > 0)
            {
                lblSubclassSpells.Text = $"Subclass spells ({subName}): {string.Join(", ", spells)}";
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
            if (MainTabControl.SelectedItem is TabItem tab)
            {
                // === SKILLS TAB ===
                if (tab == tabSkills)
                {
                    if (cmbClass?.SelectedItem is string classNameSkills)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            UpdateSkillChoices(classNameSkills);
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                    else
                    {
                        // No class selected yet — show helpful message
                        if (lblClassSkillCounter != null)
                            lblClassSkillCounter.Text = "Please select a Class first";
                    }
                }

                // === SPELLS TAB ===
                else if (tab == tabSpells && cmbClass.SelectedItem is string classNameSpells)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // NEW: Show any spells chosen via feats (Magic Initiate, Fey Touched, etc.)
                        UpdateFeatSpellsLabel();

                        // === CANTRIPS ===
                        if (cantripOptions.Count == 0)
                        {
                            UpdateCantripChoices(classNameSpells);
                        }
                        else
                        {
                            int max = GameData.ClassData[classNameSpells].CantripsKnown;
                            int selected = cantripOptions.Count(s => s.IsChecked);
                            foreach (var item in cantripOptions)
                            {
                                bool isForThisClass = item.FullSpell.Classes.Contains(classNameSpells, StringComparer.OrdinalIgnoreCase);
                                item.IsSelectable = isForThisClass && ((selected < max) || item.IsChecked);
                            }
                            UpdateCantripCounter();
                        }

                        // === 1ST LEVEL SPELLS ===
                        if (spell1Options.Count == 0)
                        {
                            UpdateSpell1Choices(classNameSpells);
                        }
                        else
                        {
                            var classDataRef = GameData.ClassData[classNameSpells];
                            int max = GetMax1stLevelSpells(classNameSpells, classDataRef);
                            int selected = spell1Options.Count(s => s.IsChecked);
                            foreach (var item in spell1Options)
                            {
                                bool isForThisClass = item.FullSpell.Classes.Contains(classNameSpells, StringComparer.OrdinalIgnoreCase);
                                item.IsSelectable = isForThisClass && ((selected < max) || item.IsChecked);
                            }
                            UpdateSpellCounter();
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void PopulateSpells()
        {
            if (cmbClass.SelectedItem is not string className || !GameData.ClassData.ContainsKey(className))
                return;

            UpdateCantripChoices(className);
            UpdateSpell1Choices(className);           // ← NEW
        }

        public void UpdateCantripChoices(string className)
        {
            if (!GameData.ClassData.ContainsKey(className)) return;

            var classData = GameData.ClassData[className];
            int maxCantrips = classData.CantripsKnown;

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
                dgCantrips.ItemsSource = cantripViewSource.View;
            }

            // === KEY PART: Uncheck everything when changing classes ===
            foreach (var item in cantripOptions)
            {
                item.IsChecked = false;
            }

            // Apply filter for the new class
            cantripViewSource.Filter -= CantripFilter;
            cantripViewSource.Filter += CantripFilter;

            // Update selectable state for the new class
            foreach (var item in cantripOptions)
            {
                bool isForThisClass = item.FullSpell.Classes.Contains(className, StringComparer.OrdinalIgnoreCase);
                item.IsSelectable = isForThisClass;
            }

            // Hide preview panel when switching classes
            pnlCantripPreview.Visibility = Visibility.Collapsed;

            UpdateCantripCounter();
        }

        // Filter method (add this new method)
        private void CantripFilter(object sender, FilterEventArgs e)
        {
            if (e.Item is SelectableSpell spell && cmbClass.SelectedItem is string className)
            {
                e.Accepted = spell.FullSpell.Classes.Contains(className, StringComparer.OrdinalIgnoreCase);
            }
        }

        private void dgCantrips_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCantrips.SelectedItem is SelectableSpell selectedSpell && selectedSpell.FullSpell != null)
            {
                var spell = selectedSpell.FullSpell;

                var details = new System.Text.StringBuilder();
                details.AppendLine($"**{spell.Name}** (Cantrip)");
                details.AppendLine($"School: {spell.School}");
                details.AppendLine($"Casting Time: {spell.CastingTime}");
                details.AppendLine($"Range: {spell.Range}");
                details.AppendLine($"Components: {spell.Components}");
                details.AppendLine($"Duration: {spell.Duration}" + (spell.IsConcentration ? " (Concentration)" : ""));

                if (!string.IsNullOrWhiteSpace(spell.DamageDice))
                    details.AppendLine($"Damage: {spell.DamageDice} {spell.DamageType}");

                if (!string.IsNullOrWhiteSpace(spell.RollType))
                    details.AppendLine($"Roll: {spell.RollType}");

                details.AppendLine();
                details.AppendLine(spell.Description);

                txtCantripPreview.Text = details.ToString();
                pnlCantripPreview.Visibility = Visibility.Visible;
            }
            else
            {
                pnlCantripPreview.Visibility = Visibility.Collapsed;
            }
        }

        public void UpdateSpell1Choices(string className)
        {
            if (!GameData.ClassData.ContainsKey(className)) return;

            var classData = GameData.ClassData[className];
            int maxSpells = GetMax1stLevelSpells(className, classData);

            // Build master list only once
            if (spell1Options.Count == 0)
            {
                foreach (var spell in GameData.All1stLevelSpells)
                {
                    // === SAFETY: Only include actual 1st-level spells ===
                    if (spell.Level != 1) continue;

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
                    spell1Options.Add(selectable);
                }

                spell1ViewSource.Source = spell1Options;
                dgSpells1.ItemsSource = spell1ViewSource.View;
            }

            // Uncheck everything when class changes
            foreach (var item in spell1Options)
            {
                item.IsChecked = false;
            }

            // Apply filter
            spell1ViewSource.Filter -= Spell1Filter;
            spell1ViewSource.Filter += Spell1Filter;

            // Update selectable state
            foreach (var item in spell1Options)
            {
                bool isForThisClass = item.FullSpell.Classes.Contains(className, StringComparer.OrdinalIgnoreCase);
                item.IsSelectable = isForThisClass;
            }

            UpdateSpellStats();
            UpdateSpellCounter();
        }

        private void Spell1Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is SelectableSpell spell && cmbClass.SelectedItem is string className)
            {
                e.Accepted = spell.FullSpell.Classes.Contains(className, StringComparer.OrdinalIgnoreCase);
            }
        }

        private void Spell1CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (cmbClass.SelectedItem is not string className) return;
            if (!GameData.ClassData.TryGetValue(className, out var classData)) return;

            int max = GetMax1stLevelSpells(className, classData);

            int currentSelected = spell1Options.Count(s => s.IsChecked);

            if (currentSelected > max)
            {
                if (sender is CheckBox cb && cb.DataContext is SelectableSpell spell)
                {
                    spell.IsChecked = false;
                    return;
                }
            }

            int nowSelected = spell1Options.Count(s => s.IsChecked);
            foreach (var item in spell1Options)
            {
                bool shouldBeSelectable = (nowSelected < max) || item.IsChecked;
                if (item.IsSelectable != shouldBeSelectable)
                {
                    item.IsSelectable = shouldBeSelectable;
                }
            }

            UpdateSpellStats();
            UpdateSpellCounter();
        }

        private void dgSpells1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSpells1.SelectedItem is SelectableSpell selectedSpell && selectedSpell.FullSpell != null)
            {
                var spell = selectedSpell.FullSpell;
                int level = (spell as LeveledSpell)?.Level ?? 0;

                var details = new System.Text.StringBuilder();
                details.AppendLine($"**{spell.Name}** (Level {level})");
                details.AppendLine($"School: {spell.School}");
                details.AppendLine($"Casting Time: {spell.CastingTime}");
                details.AppendLine($"Range: {spell.Range}");
                details.AppendLine($"Components: {spell.Components}");
                details.AppendLine($"Duration: {spell.Duration}" + (spell.IsConcentration ? " (Concentration)" : ""));

                if (!string.IsNullOrWhiteSpace(spell.DamageDice))
                    details.AppendLine($"Dice: {spell.DamageDice} {spell.DamageType}");

                details.AppendLine();
                details.AppendLine(spell.Description);

                txtCantripPreview.Text = details.ToString();
                pnlCantripPreview.Visibility = Visibility.Visible;
            }
            else
            {
                pnlCantripPreview.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSpellStats()
        {
            if (cmbClass.SelectedItem is not string className || !GameData.ClassData.TryGetValue(className, out var data))
                return;

            string ability = data.SpellAbility;
            int mod = 0;
            if (ability == "Wisdom") mod = CalculateModifier(GetFinalStat("Wisdom"));
            else if (ability == "Charisma") mod = CalculateModifier(GetFinalStat("Charisma"));
            else if (ability == "Intelligence") mod = CalculateModifier(GetFinalStat("Intelligence"));

            int dc = 8 + proficiencyBonus + mod;
            int attack = proficiencyBonus + mod;

            lblSpellStats.Text = $"Spellcasting Ability: {ability} | Spell Save DC: {dc} | Spell Attack: +{attack}";
            // TODO: Update these when racial/feat spell functionality is added
            if (lblRacialSpells != null)
                lblRacialSpells.Text = "Racial spells: (none yet)";

            if (lblFeatSpells != null)
                lblFeatSpells.Text = "Feat spells: (none yet)";

            if (lblSubclassSpells != null)
                lblSubclassSpells.Text = "Subclass spells: (none yet)";

            // === DYNAMIC HEADERS ===
            if (lblCantripHeader != null)
                lblCantripHeader.Text = $"CANTRIPS ({data.CantripsKnown} known)";

            int spellSlots = className == "Warlock" ? 1 : 2;
            string spellType = (className == "Wizard" || className == "Cleric" || className == "Druid" || className == "Artificer")
                ? "prepared" : "known";

            int spellCount = GetMax1stLevelSpells(className, data);

            if (lblSpellHeader != null)
            {
                if (spellCount > 0)
                    lblSpellHeader.Text = $"1ST LEVEL SPELLS ({spellCount} {spellType}, {spellSlots} slot{(spellSlots > 1 ? "s" : "")})";
                else
                    lblSpellHeader.Text = "1ST LEVEL SPELLS (none at this level)";
            }

            int selectedSpells = spell1Options.Count(s => s.IsChecked);
            lblSpellCount.Text = $"Selected: {selectedSpells} / {spellCount}";
            UpdateSubclassSpellsLabel();
            UpdateRacialSpellsLabel();
        }

        public void UpdateCantripCounter()
        {
            if (lblCantripCount == null) return;

            if (cmbClass.SelectedItem is not string className)
            {
                lblCantripCount.Text = "Selected: 0 / 0";
                return;
            }

            if (GameData.ClassData.TryGetValue(className, out var classData))
            {
                int selected = cantripOptions?.Count(s => s.IsChecked) ?? 0;
                lblCantripCount.Text = $"Selected: {selected} / {classData.CantripsKnown}";
            }
        }

        private void CantripCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (cmbClass.SelectedItem is not string className) return;
            if (!GameData.ClassData.TryGetValue(className, out var classData)) return;

            int max = classData.CantripsKnown;

            int currentSelected = cantripOptions.Count(s => s.IsChecked);

            if (currentSelected > max)
            {
                if (sender is CheckBox cb && cb.DataContext is SelectableSpell spell)
                {
                    spell.IsChecked = false;
                    return;
                }
            }

            // Update IsSelectable (binding will handle the UI)
            int nowSelected = cantripOptions.Count(s => s.IsChecked);
            foreach (var item in cantripOptions)
            {
                bool shouldBeSelectable = (nowSelected < max) || item.IsChecked;
                if (item.IsSelectable != shouldBeSelectable)
                {
                    item.IsSelectable = shouldBeSelectable;
                }
            }

            UpdateCantripCounter();
        }

        public void UpdateSpellCounter()
        {
            if (lblSpellCount == null) return;

            if (cmbClass.SelectedItem is not string className)
            {
                lblSpellCount.Text = "Selected: 0 / 0";
                return;
            }

            if (!GameData.ClassData.TryGetValue(className, out var classData))
                return;

            int selected = spell1Options?.Count(s => s.IsChecked) ?? 0;
            int max = GetMax1stLevelSpells(className, classData);

            lblSpellCount.Text = $"Selected: {selected} / {max}";
        }

        private int CalculateModifier(int score)
        {
            // Official 5e formula: floor( (score - 10) / 2 )
            // C# integer division truncates toward zero, so we fix negative numbers explicitly
            //return (score - 10) / 2 - (score < 10 && (score - 10) % 2 != 0 ? 1 : 0);
            return (int)Math.Floor((score - 10) / 2.0);
        }

        /// <summary>
        /// Returns the correct maximum number of 1st-level spells for the given class.
        /// - Known spells classes (Bard, Sorcerer, Warlock, Ranger): uses SpellsKnown from GameData
        /// - Prepared spells classes (Cleric, Druid, Wizard, Artificer): uses modifier + 1 (min 1)
        /// - Classes with no spells at current level (e.g. Ranger level 1): returns 0
        /// </summary>
        private int GetMax1stLevelSpells(string className, ClassData classData)
        {
            if (classData.SpellsKnown > 0)
            {
                // Known spells classes (Bard, Sorcerer, Warlock, Ranger)
                return classData.SpellsKnown;
            }
            else if (className == "Wizard" || className == "Cleric" || className == "Druid" || className == "Artificer")
            {
                // Prepared spells classes
                int mod = CalculateModifier(GetFinalStat(classData.SpellAbility));
                return Math.Max(1, mod + 1);
            }
            else
            {
                // No 1st-level spells at this level (e.g. Ranger level 1)
                return 0;
            }
        }

        private int GetFinalStat(string ability)
        {
            // Build current final stats from UI once
            var finalStats = new Dictionary<string, int>
            {
                ["Strength"] = int.TryParse(txtStrFinal.Text, out int s) ? s : 10,
                ["Dexterity"] = int.TryParse(txtDexFinal.Text, out int d) ? d : 10,
                ["Constitution"] = int.TryParse(txtConFinal.Text, out int c) ? c : 10,
                ["Intelligence"] = int.TryParse(txtIntFinal.Text, out int i) ? i : 10,
                ["Wisdom"] = int.TryParse(txtWisFinal.Text, out int w) ? w : 10,
                ["Charisma"] = int.TryParse(txtChaFinal.Text, out int h) ? h : 10
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

            int prof = CurrentCharacter.ProficiencyBonus;

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

                bool isProficient = classProficientSaves.Contains(ability);

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

        private string GetFinalACStringForPDF()
        {
            if (CurrentCharacter != null && !string.IsNullOrWhiteSpace(CurrentCharacter.EquippedACDisplay))
            {
                return CurrentCharacter.EquippedACDisplay;
            }

            // Fallback (should rarely happen)
            return CurrentCharacter?.ArmorClass.ToString() ?? "10";
        }

        private void DrawSectionHeader(XGraphics gfx, string title, double x, ref double y, double pageWidth)
        {
            var rect = new XRect(x, y - 4, pageWidth - 80, 18);
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(230, 230, 240)), rect);
            gfx.DrawString(title, new XFont("Arial", 11, XFontStyleEx.Bold), XBrushes.DarkSlateBlue, new XPoint(x + 5, y + 10));
            y += 28;
        }

        private void DrawCleanAbilityScore(XGraphics gfx, string label, AbilityScore score, double x, ref double y)
        {
            var bold = new XFont("Arial", 9, XFontStyleEx.Bold);
            var normal = new XFont("Arial", 9);

            gfx.DrawString(label, bold, XBrushes.DarkSlateBlue, new XPoint(x, y));
            y += 13;

            string mainLine = $"{score.Final} ({score.Modifier:+#;-#;0})";
            gfx.DrawString(mainLine, new XFont("Arial", 11, XFontStyleEx.Bold), XBrushes.Black, new XPoint(x, y));
            y += 12;

            string breakdown = $"Base {score.Base} + Racial {score.Racial} + Feat {score.Feat}";
            gfx.DrawString(breakdown, normal, XBrushes.Gray, new XPoint(x, y));
            y += 18;
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

            int prof = CurrentCharacter.ProficiencyBonus;

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

                bool isProficient = CurrentCharacter.Skills.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                skills.Add(new SkillEntry
                {
                    Name = name,
                    Ability = ability,
                    IsProficient = isProficient,
                    Bonus = mod + (isProficient ? prof : 0)
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
        /// Draws text that wraps within the given maxWidth and advances y accordingly.
        /// </summary>
        private void DrawWrappedText(XGraphics gfx, string text, XFont font, XBrush brush,
                                     double x, ref double y, double maxWidth, double lineHeight = 11)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var words = text.Split(' ');
            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                double width = gfx.MeasureString(testLine, font).Width;

                if (width > maxWidth && currentLine.Length > 0)
                {
                    gfx.DrawString(currentLine, font, brush, new XPoint(x, y));
                    y += lineHeight;
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                gfx.DrawString(currentLine, font, brush, new XPoint(x, y));
                y += lineHeight;
            }
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            // === STEP 1: Auto-save JSON silently ===
            AutoSaveCharacterToJson();

            if (CurrentCharacter == null)
            {
                MessageBox.Show("No character loaded. Please save the character first.", "Export Failed");
                return;
            }

            // === Build clean filename from character name ===
            string characterName = CurrentCharacter.Name?.Trim() ?? "";

            // Remove invalid filename characters and replace spaces with underscores
            string safeName = string.IsNullOrWhiteSpace(characterName)
                ? "Untitled"
                : string.Join("_", characterName.Split(System.IO.Path.GetInvalidFileNameChars()));

            string defaultFileName = $"{safeName}_CharacterSheet.pdf";

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = defaultFileName,
                DefaultExt = ".pdf",
                Filter = "PDF Files (*.pdf)|*.pdf|All files (*.*)|*.*",
                Title = "Export Character Sheet as PDF"
            };

            if (dlg.ShowDialog() != true)
                return; // User cancelled

            string filePath = dlg.FileName;

            try
            {
                var document = new PdfSharp.Pdf.PdfDocument();
                document.Info.Title = $"{CurrentCharacter.Name} - D&D 5e Character Sheet";
                document.Info.Author = "Nemo Character Creator";

                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                var font = new XFont("Arial", 10);
                var boldFont = new XFont("Arial", 11, XFontStyleEx.Bold);
                var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
                var sectionFont = new XFont("Arial", 11, XFontStyleEx.Bold);
                var normalFont = new XFont("Arial", 9);
                var grayBrush = XBrushes.DimGray;
                var smallGray = new XFont("Arial", 8);
                var linkFont = new XFont("Arial", 10, XFontStyleEx.Underline);
                var linkBrush = XBrushes.Blue;

                double y = 40;
                double left = 40;
                double pageWidth = page.Width;
                double maxTextWidth = pageWidth - 100;

                // ========== HEADER ==========
                gfx.DrawString("D&D 5e", titleFont, XBrushes.DarkSlateBlue, new XPoint(left, y));
                y += 26;

                gfx.DrawString(CurrentCharacter.Name, new XFont("Arial", 16, XFontStyleEx.Bold), XBrushes.Black, new XPoint(left, y));
                gfx.DrawString($"Player: {CurrentCharacter.PlayerName}", font, XBrushes.Black, new XPoint(320, y));
                y += 22;

                // Clean class name for display
                string displayClass = CurrentCharacter.Class;
                if (displayClass.Contains("("))
                    displayClass = displayClass.Substring(0, displayClass.IndexOf("(")).Trim();

                // Clean subclass (in case old data still has the placeholder)
                string displaySubclass = CurrentCharacter.Subclass;
                if (!string.IsNullOrEmpty(displaySubclass) && displaySubclass.Contains("Requires Level", StringComparison.OrdinalIgnoreCase))
                    displaySubclass = "";

                string raceLine = $"{CurrentCharacter.Race}";
                if (!string.IsNullOrEmpty(CurrentCharacter.Subrace)) raceLine += $" ({CurrentCharacter.Subrace})";
                raceLine += $"  •  {displayClass}";
                if (!string.IsNullOrEmpty(displaySubclass)) raceLine += $" ({displaySubclass})";
                raceLine += $"  •  {CurrentCharacter.Background}";

                gfx.DrawString(raceLine, font, XBrushes.Black, new XPoint(left, y));
                y += 18;

                if (!string.IsNullOrEmpty(CurrentCharacter.SelectedFeat))
                    gfx.DrawString($"Feat: {CurrentCharacter.SelectedFeat}", font, XBrushes.DarkGreen, new XPoint(left, y));

                y += 25;

                // ========== ABILITY SCORES ==========
                DrawSectionHeader(gfx, "ABILITY SCORES", left, ref y, pageWidth);

                double col1X = left;
                double col2X = 280;
                double startY = y;

                // Left column
                DrawCleanAbilityScore(gfx, "Strength", CurrentCharacter.AbilityScores.Strength, col1X, ref y);
                DrawCleanAbilityScore(gfx, "Constitution", CurrentCharacter.AbilityScores.Constitution, col1X, ref y);
                DrawCleanAbilityScore(gfx, "Wisdom", CurrentCharacter.AbilityScores.Wisdom, col1X, ref y);

                // Right column (reset y)
                y = startY;
                DrawCleanAbilityScore(gfx, "Dexterity", CurrentCharacter.AbilityScores.Dexterity, col2X, ref y);
                DrawCleanAbilityScore(gfx, "Intelligence", CurrentCharacter.AbilityScores.Intelligence, col2X, ref y);
                DrawCleanAbilityScore(gfx, "Charisma", CurrentCharacter.AbilityScores.Charisma, col2X, ref y);

                y = Math.Max(y, startY) + 15;

                // ========== COMBAT ==========
                DrawSectionHeader(gfx, "COMBAT", left, ref y, pageWidth);

                string finalAC = GetFinalACStringForPDF();

                int strMod = CurrentCharacter.AbilityScores.Strength.Modifier;
                int dexMod = CurrentCharacter.AbilityScores.Dexterity.Modifier;
                int prof = CurrentCharacter.ProficiencyBonus;

                string combatText = $"AC: {finalAC}     HP: {CurrentCharacter.HitPoints}     " +
                                    $"Initiative: +{CurrentCharacter.Initiative}     Proficiency: +{prof}";

                gfx.DrawString(combatText, boldFont, XBrushes.Black, new XPoint(left, y));
                y += 18;

                // === NEW: Weapon Attack Bonuses ===
                string meleeAttack = (strMod + prof) >= 0 ? $"+{strMod + prof}" : (strMod + prof).ToString();
                string rangedAttack = (dexMod + prof) >= 0 ? $"+{dexMod + prof}" : (dexMod + prof).ToString();

                string attackText = $"Weapon Attack (Str): {meleeAttack}     Ranged Weapon Attack (Dex): {rangedAttack}";
                gfx.DrawString(attackText, font, XBrushes.Black, new XPoint(left, y));
                y += 20;

                // Spellcasting line (only if the character has spellcasting)
                if (!string.IsNullOrEmpty(CurrentCharacter.SpellcastingAbility))
                {
                    gfx.DrawString($"Spell DC: {CurrentCharacter.SpellSaveDC}     Spell Attack: +{CurrentCharacter.SpellAttackBonus}",
                                   font, XBrushes.Black, new XPoint(left, y));
                    y += 18;
                }

                // ========== SPEED ==========
                int speed = GetFinalSpeed();

                gfx.DrawString($"Speed: {speed} ft", font, XBrushes.Black, new XPoint(left, y));
                y += 18;

                // ========== EQUIPPED WEAPONS ==========
                var equippedWeapons = GetFormattedEquippedWeapons();
                if (equippedWeapons.Count > 0)
                {
                    DrawSectionHeader(gfx, "EQUIPPED WEAPONS", left, ref y, pageWidth);

                    foreach (var weaponLine in equippedWeapons)
                    {
                        if (y > page.Height - 70)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            y = 40;
                            DrawSectionHeader(gfx, "EQUIPPED WEAPONS (continued)", left, ref y, pageWidth);
                        }

                        gfx.DrawString(weaponLine, font, XBrushes.Black, new XPoint(left, y));
                        y += 16;
                    }

                    y += 10;
                }

                // ========== CLASS FEATURES (NEW - between Weapons and Saving Throws) ==========
                string classKey = CurrentCharacter.Class;
                if (GameData.ClassLevel1Features.TryGetValue(classKey, out var classFeatures) && classFeatures.Count > 0)
                {
                    DrawSectionHeader(gfx, "CLASS FEATURES", left, ref y, pageWidth);

                    double pageHeight = page.Height;

                    // Clean display name (strip any parenthetical notes)
                    string displayClassName = classKey;
                    if (displayClassName.Contains("("))
                        displayClassName = displayClassName.Substring(0, displayClassName.IndexOf("(")).Trim();

                    // Hyperlink to class on wikidot (like spells)
                    string classSlug = Slugify(displayClassName);
                    string classUrl = $"https://dnd5e.wikidot.com/{classSlug}";

                    gfx.DrawString(displayClassName, linkFont, linkBrush, new XPoint(left + 10, y));
                    double classNameWidth = gfx.MeasureString(displayClassName, linkFont).Width;
                    var classLinkRect = new PdfRectangle(new XRect(left + 10, pageHeight - (y + 4), classNameWidth + 4, 16));
                    page.AddWebLink(classLinkRect, classUrl);

                    // === Subclass hyperlink (only for Cleric, Sorcerer, Warlock) ===
                    string subclassKey = CurrentCharacter.Subclass ?? "";
                    if (!string.IsNullOrWhiteSpace(subclassKey) &&
                        !subclassKey.Contains("Requires Level", StringComparison.OrdinalIgnoreCase) &&
                        (classKey == "Cleric" || classKey == "Sorcerer" || classKey == "Warlock"))
                    {
                        double xAfterClass = left + 10 + classNameWidth + 6;
                        gfx.DrawString(" / ", normalFont, XBrushes.Black, new XPoint(xAfterClass, y));

                        double slashWidth = gfx.MeasureString(" / ", normalFont).Width;
                        double subX = xAfterClass + slashWidth;

                        // Build the special subclass URL format
                        string subSlug = Slugify(subclassKey);
                        string subUrl = classKey.ToLowerInvariant() switch
                        {
                            "cleric" => $"https://dnd5e.wikidot.com/cleric:{subSlug}",
                            "sorcerer" => $"https://dnd5e.wikidot.com/sorcerer:{subSlug}",
                            "warlock" => $"https://dnd5e.wikidot.com/warlock:{subSlug}",
                            _ => $"https://dnd5e.wikidot.com/{subSlug}"
                        };

                        string displaySub = subclassKey;
                        gfx.DrawString(displaySub, linkFont, linkBrush, new XPoint(subX, y));
                        double subWidth = gfx.MeasureString(displaySub, linkFont).Width;
                        var subLinkRect = new PdfRectangle(new XRect(subX, pageHeight - (y + 4), subWidth + 4, 16));
                        page.AddWebLink(subLinkRect, subUrl);
                    }

                    y += 18;

                    foreach (var feature in classFeatures)
                    {
                        // New page safety
                        if (y > page.Height - 95)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            y = 40;
                            DrawSectionHeader(gfx, "CLASS FEATURES (continued)", left, ref y, pageWidth);
                            pageHeight = page.Height;
                        }

                        // Feature name (bold)
                        gfx.DrawString($"• {feature.Name}", boldFont, XBrushes.Black, new XPoint(left + 10, y));
                        y += 13;

                        // Brief description (wrapped, gray, small)
                        string desc = feature.Description;
                        if (desc.Length > 220)
                            desc = desc.Substring(0, 217) + "...";
                        DrawWrappedText(gfx, desc, smallGray, XBrushes.Gray, left + 18, ref y, maxTextWidth - 10, 10);
                        y += 3;

                        // Uses / recharge info
                        if (!string.IsNullOrWhiteSpace(feature.Uses))
                        {
                            gfx.DrawString($"   Uses: {feature.Uses}", normalFont, XBrushes.DarkGreen, new XPoint(left + 18, y));
                            y += 12;
                        }

                        y += 5;
                    }

                    y += 8;

                    // === Subclass Features (for Cleric / Sorcerer / Warlock only) ===
                    if (!string.IsNullOrWhiteSpace(subclassKey) &&
                        !subclassKey.Contains("Requires Level", StringComparison.OrdinalIgnoreCase) &&
                        GameData.SubclassLevel1Features.TryGetValue(subclassKey, out var subFeatures) &&
                        subFeatures.Count > 0)
                    {
                        // Subheader using the subclass name
                        if (y > page.Height - 70)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            y = 40;
                            DrawSectionHeader(gfx, "CLASS FEATURES (continued)", left, ref y, pageWidth);
                            pageHeight = page.Height;
                        }

                        string subHeader = $"{subclassKey} Features";
                        gfx.DrawString(subHeader, boldFont, XBrushes.DarkSlateBlue, new XPoint(left + 10, y));
                        y += 16;

                        foreach (var feature in subFeatures)
                        {
                            if (y > page.Height - 95)
                            {
                                page = document.AddPage();
                                gfx = XGraphics.FromPdfPage(page);
                                y = 40;
                                DrawSectionHeader(gfx, "CLASS FEATURES (continued)", left, ref y, pageWidth);
                                pageHeight = page.Height;
                            }

                            gfx.DrawString($"• {feature.Name}", boldFont, XBrushes.Black, new XPoint(left + 10, y));
                            y += 13;

                            string desc = feature.Description;
                            if (desc.Length > 220)
                                desc = desc.Substring(0, 217) + "...";
                            DrawWrappedText(gfx, desc, smallGray, XBrushes.Gray, left + 18, ref y, maxTextWidth - 10, 10);
                            y += 3;

                            if (!string.IsNullOrWhiteSpace(feature.Uses))
                            {
                                gfx.DrawString($"   Uses: {feature.Uses}", normalFont, XBrushes.DarkGreen, new XPoint(left + 18, y));
                                y += 12;
                            }

                            y += 5;
                        }

                        y += 6;
                    }
                }

                // ========== SAVING THROWS (NEW) ==========
                DrawSectionHeader(gfx, "SAVING THROWS", left, ref y, pageWidth);

                var savingThrows = GetSavingThrows();
                double saveY = y;

                // Left column
                for (int i = 0; i < 3; i++)
                {
                    var save = savingThrows[i];
                    string text = $"{save.Name}: {save.Bonus:+#;-#;0}";
                    if (save.IsProficient) text += "  (Proficient)";
                    gfx.DrawString(text, font, XBrushes.Black, new XPoint(left, saveY));
                    saveY += 15;
                }

                // Right column
                saveY = y;
                for (int i = 3; i < 6; i++)
                {
                    var save = savingThrows[i];
                    string text = $"{save.Name}: {save.Bonus:+#;-#;0}";
                    if (save.IsProficient) text += "  (Proficient)";
                    gfx.DrawString(text, font, XBrushes.Black, new XPoint(300, saveY));
                    saveY += 15;
                }

                y = Math.Max(y, saveY) + 15;

                // ========== SKILLS ==========
                DrawSectionHeader(gfx, "SKILLS", left, ref y, pageWidth);

                // Build full skill list with current bonuses
                var allSkillsList = BuildFullSkillListForPDF();

                int half = (allSkillsList.Count + 1) / 2;
                double skillY = y;

                for (int i = 0; i < half; i++)
                {
                    var skill = allSkillsList[i];
                    string sign = skill.Bonus >= 0 ? "+" : "";
                    string profMark = skill.IsProficient ? " ●" : "";
                    gfx.DrawString($"{skill.Name} ({skill.Ability}): {sign}{skill.Bonus}{profMark}", font, XBrushes.Black, new XPoint(left, skillY));
                    skillY += 14;
                }

                skillY = y;
                for (int i = half; i < allSkillsList.Count; i++)
                {
                    var skill = allSkillsList[i];
                    string sign = skill.Bonus >= 0 ? "+" : "";
                    string profMark = skill.IsProficient ? " ●" : "";
                    gfx.DrawString($"{skill.Name} ({skill.Ability}): {sign}{skill.Bonus}{profMark}", font, XBrushes.Black, new XPoint(300, skillY));
                    skillY += 14;
                }

                y = Math.Max(y + (half * 14), skillY) + 12;

                // ========== PROFICIENCIES AND ATTRIBUTES ==========
                DrawSectionHeader(gfx, "PROFICIENCIES AND ATTRIBUTES", left, ref y, pageWidth);

                // Helper to check for new page
                void CheckNewPage()
                {
                    if (y > page.Height - 120)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 40;
                    }
                }

                // === ARMOR PROFICIENCIES ===
                CheckNewPage();
                gfx.DrawString("Armor Proficiencies:", boldFont, XBrushes.Black, new XPoint(left, y));
                y += 16;

                var armorProfs = new List<string>();

                if (GameData.ClassData.TryGetValue(CurrentCharacter.Class, out var classData))
                {
                    armorProfs.AddRange(classData.ArmorProficiencies);
                }

                // Add subclass armor profs (example for Twilight Cleric, etc.)
                if (!string.IsNullOrEmpty(CurrentCharacter.Subclass))
                {
                    if (CurrentCharacter.Class == "Cleric" && GameData.ClericSubclasses.TryGetValue(CurrentCharacter.Subclass, out var clericSub))
                        armorProfs.AddRange(clericSub.ArmorProficiencies);
                    else if (CurrentCharacter.Class == "Warlock" && GameData.WarlockSubclasses.TryGetValue(CurrentCharacter.Subclass, out var warlockSub))
                        armorProfs.AddRange(warlockSub.ArmorProficiencies);
                }

                // Race-based armor proficiencies
                string race = CurrentCharacter.Race;
                if (race.Contains("Dwarf"))
                    armorProfs.Add("Dwarven Armor Training (light & medium armor)");

                string armorText = armorProfs.Count > 0
                    ? string.Join(", ", armorProfs.Distinct(StringComparer.OrdinalIgnoreCase))
                    : "None";

                CheckNewPage();
                DrawWrappedText(gfx, armorText, normalFont, XBrushes.Black, left + 10, ref y, maxTextWidth);
                y += 18;

                // === WEAPON PROFICIENCIES ===
                CheckNewPage();
                gfx.DrawString("Weapon Proficiencies:", boldFont, XBrushes.Black, new XPoint(left, y));
                y += 16;

                var weaponProfs = new List<string>();

                if (classData != null)
                    weaponProfs.AddRange(classData.WeaponProficiencies);

                // Subclass weapon profs
                if (!string.IsNullOrEmpty(CurrentCharacter.Subclass))
                {
                    if (CurrentCharacter.Class == "Cleric" && GameData.ClericSubclasses.TryGetValue(CurrentCharacter.Subclass, out var clericSub))
                        weaponProfs.AddRange(clericSub.WeaponProficiencies);
                    else if (CurrentCharacter.Class == "Warlock" && GameData.WarlockSubclasses.TryGetValue(CurrentCharacter.Subclass, out var warlockSub))
                        weaponProfs.AddRange(warlockSub.WeaponProficiencies);
                }

                // Race weapon training
                if (race.Contains("Dwarf"))
                    weaponProfs.Add("Dwarven Combat Training (battleaxe, handaxe, light hammer, warhammer)");
                if (race.Contains("Elf"))
                    weaponProfs.Add("Elf Weapon Training (longsword, shortsword, shortbow, longbow)");

                string weaponText = weaponProfs.Count > 0
                    ? string.Join(", ", weaponProfs.Distinct(StringComparer.OrdinalIgnoreCase))
                    : "None";

                CheckNewPage();
                DrawWrappedText(gfx, weaponText, normalFont, XBrushes.Black, left + 10, ref y, maxTextWidth);
                y += 18;

                // === LANGUAGES ===
                CheckNewPage();
                gfx.DrawString("Languages:", boldFont, XBrushes.Black, new XPoint(left, y));
                y += 16;

                var languages = new List<string>();
                if (GameData.RaceData.TryGetValue(race, out var raceData))
                    languages.AddRange(raceData.Languages);

                string langText = languages.Count > 0 ? string.Join(", ", languages) : "Common";
                CheckNewPage();
                DrawWrappedText(gfx, langText, normalFont, XBrushes.Black, left + 10, ref y, maxTextWidth);
                y += 18;

                // === RACIAL TRAITS ===
                CheckNewPage();
                gfx.DrawString("Racial Traits:", boldFont, XBrushes.Black, new XPoint(left, y));
                y += 16;

                if (raceData != null && raceData.Traits.Any())
                {
                    foreach (var trait in raceData.Traits)
                    {
                        CheckNewPage();
                        DrawWrappedText(gfx, "• " + trait, normalFont, XBrushes.Black, left + 10, ref y, maxTextWidth);
                        y += 4;
                    }
                }
                y += 10;

                // === SUBRACIAL TRAITS (if any) ===
                if (!string.IsNullOrEmpty(CurrentCharacter.Subrace) &&
                    GameData.RaceSubraces.TryGetValue(race, out var subList))
                {
                    var subrace = subList.FirstOrDefault(s => s.Name == CurrentCharacter.Subrace);
                    if (subrace != null && subrace.Traits.Any())
                    {
                        CheckNewPage();
                        gfx.DrawString($"Subracial Traits ({CurrentCharacter.Subrace}):", boldFont, XBrushes.Black, new XPoint(left, y));
                        y += 16;

                        foreach (var trait in subrace.Traits)
                        {
                            CheckNewPage();
                            DrawWrappedText(gfx, "• " + trait, normalFont, XBrushes.Black, left + 10, ref y, maxTextWidth);
                            y += 4;
                        }
                    }
                }

                y += 20;

                // ========== EQUIPMENT ==========
                DrawSectionHeader(gfx, "EQUIPMENT", left, ref y, pageWidth);

                if (CurrentCharacter.Equipment != null && CurrentCharacter.Equipment.Count > 0)
                {
                    foreach (var item in CurrentCharacter.Equipment)
                    {
                        // Check if we need to start a new page
                        if (y > page.Height - 70)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            y = 40;

                            DrawSectionHeader(gfx, "EQUIPMENT", left, ref y, pageWidth);
                        }

                        gfx.DrawString($"• {item}", font, XBrushes.Black, new XPoint(left, y));
                        y += 15;
                    }
                }
                else
                {
                    gfx.DrawString("No equipment recorded.", font, XBrushes.Gray, new XPoint(left, y));
                    y += 15;
                }

                y += 15;

                // ========== SPELLS ==========
                if (CurrentCharacter.Cantrips.Any() || CurrentCharacter.Level1Spells.Any())
                {
                    DrawSectionHeader(gfx, "SPELLS", left, ref y, pageWidth);

                    double pageHeight = page.Height;

                    // --- Cantrips ---
                    if (CurrentCharacter.Cantrips.Any())
                    {
                        gfx.DrawString("Cantrips:", boldFont, XBrushes.Black, new XPoint(left, y));
                        y += 16;

                        foreach (var cantripName in CurrentCharacter.Cantrips)
                        {
                            var spell = GameData.AllCantrips.FirstOrDefault(s =>
                                s.Name.Equals(cantripName, StringComparison.OrdinalIgnoreCase));

                            string url = $"https://dnd5e.wikidot.com/spell:{Slugify(cantripName)}";

                            // Clickable spell name
                            gfx.DrawString($"• {cantripName}", linkFont, linkBrush, new XPoint(left + 10, y));
                            double nameWidth = gfx.MeasureString($"• {cantripName}", linkFont).Width;
                            var linkRect = new PdfRectangle(new XRect(left + 10, pageHeight - (y + 4), nameWidth + 4, 16));
                            page.AddWebLink(linkRect, url);

                            if (spell != null)
                            {
                                y += 13;
                                string dicePart = !string.IsNullOrWhiteSpace(spell.DamageDice)
                                    ? $"{spell.DamageDice} {spell.DamageType}" : "—";
                                string rollPart = !string.IsNullOrWhiteSpace(spell.RollType) ? spell.RollType : "—";
                                string concPart = spell.IsConcentration ? " | Concentration: Yes" : "";

                                gfx.DrawString($"   Casting Time: {spell.CastingTime}  |  Range: {spell.Range}  |  Duration: {spell.Duration}{concPart}",
                                               normalFont, grayBrush, new XPoint(left + 14, y));

                                y += 13;
                                gfx.DrawString($"   Dice: {dicePart}  |  Roll: {rollPart}", normalFont, grayBrush, new XPoint(left + 14, y));

                                // === WRAPPED DESCRIPTION ===
                                y += 13;
                                string shortDesc = spell.Description.Length > 440
                                    ? spell.Description.Substring(0, 437) + "..."
                                    : spell.Description;

                                DrawWrappedText(gfx, shortDesc, smallGray, XBrushes.Gray, left + 14, ref y, maxTextWidth, 10);
                            }

                            y += 16;
                        }
                        y += 6;
                    }

                    // --- 1st Level Spells (same pattern) ---
                    if (CurrentCharacter.Level1Spells.Any())
                    {
                        gfx.DrawString("1st Level Spells:", boldFont, XBrushes.Black, new XPoint(left, y));
                        y += 16;

                        foreach (var spellName in CurrentCharacter.Level1Spells)
                        {
                            var spell = GameData.All1stLevelSpells.FirstOrDefault(s =>
                                s.Name.Equals(spellName, StringComparison.OrdinalIgnoreCase));

                            string url = $"https://dnd5e.wikidot.com/spell:{Slugify(spellName)}";

                            gfx.DrawString($"• {spellName}", linkFont, linkBrush, new XPoint(left + 10, y));
                            double nameWidth = gfx.MeasureString($"• {spellName}", linkFont).Width;
                            var linkRect = new PdfRectangle(new XRect(left + 10, pageHeight - (y + 4), nameWidth + 4, 16));
                            page.AddWebLink(linkRect, url);

                            if (spell != null)
                            {
                                y += 13;
                                string dicePart = !string.IsNullOrWhiteSpace(spell.DamageDice)
                                    ? $"{spell.DamageDice} {spell.DamageType}" : "—";
                                string rollPart = !string.IsNullOrWhiteSpace(spell.RollType) ? spell.RollType : "—";
                                string concPart = spell.IsConcentration ? " | Concentration: Yes" : "";

                                gfx.DrawString($"   Casting Time: {spell.CastingTime}  |  Range: {spell.Range}  |  Duration: {spell.Duration}{concPart}",
                                               normalFont, grayBrush, new XPoint(left + 14, y));

                                y += 13;
                                gfx.DrawString($"   Dice: {dicePart}  |  Roll: {rollPart}", normalFont, grayBrush, new XPoint(left + 14, y));

                                y += 13;
                                string shortDesc = spell.Description.Length > 440
                                    ? spell.Description.Substring(0, 437) + "..."
                                    : spell.Description;

                                DrawWrappedText(gfx, shortDesc, smallGray, XBrushes.Gray, left + 14, ref y, maxTextWidth, 10);
                            }

                            y += 16;
                        }
                    }
                }

                // Footer
                gfx.DrawString($"Generated by Nemo D&D 5e Character Creator  •  {DateTime.Now:yyyy-MM-dd HH:mm}",
                    new XFont("Arial", 8), XBrushes.Gray, new XPoint(left, page.Height - 25));

                document.Save(filePath);

                MessageBox.Show($"PDF exported successfully to:\n{filePath}", "Export Complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export PDF:\n{ex.Message}", "Error");
            }
        }

        private void ExportFillablePDF_Click(object sender, RoutedEventArgs e)
        {
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

                    // ========== CLASS FEATURES (with descriptions + wikidot link) ==========
                    CheckForNewPage(pdf, ref currentPage, ref y, 160);
                    DrawSectionHeader(currentPage, "CLASS FEATURES", left, ref y);

                    var classFeaturesText = new System.Text.StringBuilder();

                    string classKey = CurrentCharacter.Class;
                    string displayClassName = classKey;
                    if (displayClassName.Contains("("))
                        displayClassName = displayClassName.Substring(0, displayClassName.IndexOf("(")).Trim();

                    // Class wikidot link
                    string classSlug = Slugify(displayClassName);
                    string classUrl = $"https://dnd5e.wikidot.com/{classSlug}";
                    classFeaturesText.AppendLine($"{displayClassName}");
                    classFeaturesText.AppendLine($"Source: {classUrl}");
                    classFeaturesText.AppendLine();

                    // Class features with descriptions
                    if (GameData.ClassLevel1Features.TryGetValue(classKey, out var classFeats) && classFeats.Count > 0)
                    {
                        foreach (var f in classFeats)
                        {
                            classFeaturesText.AppendLine($"• {f.Name}");
                            
                            string shortDesc = f.Description.Length > 280 
                                ? f.Description.Substring(0, 277) + "..." 
                                : f.Description;
                            classFeaturesText.AppendLine($"   {shortDesc}");

                            if (!string.IsNullOrWhiteSpace(f.Uses))
                                classFeaturesText.AppendLine($"   Uses: {f.Uses}");

                            classFeaturesText.AppendLine();
                        }
                    }

                    // Subclass features (with descriptions)
                    string subclassKey = CurrentCharacter.Subclass ?? "";
                    if (!string.IsNullOrWhiteSpace(subclassKey) &&
                        !subclassKey.Contains("Requires Level", StringComparison.OrdinalIgnoreCase) &&
                        GameData.SubclassLevel1Features.TryGetValue(subclassKey, out var subFeats) &&
                        subFeats.Count > 0)
                    {
                        classFeaturesText.AppendLine($"--- {subclassKey} ---");

                        string subSlug = Slugify(subclassKey);
                        string subUrl = classKey.ToLowerInvariant() switch
                        {
                            "cleric" => $"https://dnd5e.wikidot.com/cleric:{subSlug}",
                            "sorcerer" => $"https://dnd5e.wikidot.com/sorcerer:{subSlug}",
                            "warlock" => $"https://dnd5e.wikidot.com/warlock:{subSlug}",
                            _ => $"https://dnd5e.wikidot.com/{subSlug}"
                        };
                        classFeaturesText.AppendLine($"Source: {subUrl}");
                        classFeaturesText.AppendLine();

                        foreach (var f in subFeats)
                        {
                            classFeaturesText.AppendLine($"• {f.Name}");

                            string shortDesc = f.Description.Length > 280
                                ? f.Description.Substring(0, 277) + "..."
                                : f.Description;
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

        // Helper to get total character level (you can improve this)
        private int GetCharacterLevel()
        {
            // Placeholder — improve with your actual level tracking later
            return 1;
        }

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
            // Populate CurrentCharacter from the current UI state
            CurrentCharacter = new Character();

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
            // Clean Subclass (remove placeholder text like "Requires Level X")
            string rawSubclass = cmbSubclass.SelectedItem?.ToString() ?? "";
            CurrentCharacter.Subclass = (rawSubclass.Contains("Requires Level", StringComparison.OrdinalIgnoreCase))
                ? ""
                : rawSubclass;

            if (dgFeats.SelectedItem is Feat feat)
                CurrentCharacter.SelectedFeat = feat.Name;

            PopulateAbilityScores();
            CurrentCharacter.ProficiencyBonus = 2;
            CurrentCharacter.SavingThrows = GetSavingThrows();

            // Calculate and store final speed (including Mobile feat)
            CurrentCharacter.Speed = GetFinalSpeed();

            // Skills
            CurrentCharacter.Skills.Clear();
            foreach (var skill in allSkills.Where(s => s.IsProficient))
            {
                CurrentCharacter.Skills.Add(new SkillEntry
                {
                    Name = skill.SkillName,
                    Ability = skill.Ability,
                    IsProficient = true,
                    Bonus = int.TryParse(skill.Bonus.Replace("+", ""), out int b) ? b : 0
                });
            }

            // Equipment
            CurrentCharacter.Equipment.Clear();
            foreach (var child in pnlTotalEquipmentSummary.Children.OfType<TextBlock>())
            {
                string text = child.Text.Replace("• ", "").Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    CurrentCharacter.Equipment.Add(text);
            }
            CurrentCharacter.BackgroundEquipment = txtBackgroundEquipment?.Text ?? "";

            // Spells
            CurrentCharacter.Cantrips = cantripOptions.Where(c => c.IsChecked).Select(c => c.Name).ToList();
            CurrentCharacter.Level1Spells = spell1Options.Where(s => s.IsChecked).Select(s => s.Name).ToList();

            // Derived values
            CurrentCharacter.Initiative = GetModifierFromText(txtInitiative.Text);
            CurrentCharacter.HitPoints = int.TryParse(txtHitPoints.Text, out int hp) ? hp : 0;

            // === Armor Class (Base + Equipped) ===
            CurrentCharacter.ArmorClass = int.TryParse(txtBaseAC.Text, out int ac) ? ac : 10;

            // Capture the nice formatted Equipped AC string shown in the UI
            if (txtEquippedAC != null &&
                txtEquippedAC.Visibility == Visibility.Visible &&
                !string.IsNullOrWhiteSpace(txtEquippedAC.Text))
            {
                CurrentCharacter.EquippedACDisplay = txtEquippedAC.Text;
            }

            if (cmbClass.SelectedItem is string className &&
                GameData.ClassData.TryGetValue(className, out var classData))
            {
                CurrentCharacter.SpellcastingAbility = classData.SpellAbility;
                int mod = classData.SpellAbility switch
                {
                    "Wisdom" => GetModifierFromText(txtWisMod.Text),
                    "Charisma" => GetModifierFromText(txtChaMod.Text),
                    "Intelligence" => GetModifierFromText(txtIntMod.Text),
                    _ => 0
                };
                CurrentCharacter.SpellSaveDC = 8 + CurrentCharacter.ProficiencyBonus + mod;
                CurrentCharacter.SpellAttackBonus = CurrentCharacter.ProficiencyBonus + mod;
            }

            CurrentCharacter.HighElfCantrip = highElfCantrip;
            CurrentCharacter.RaceGrantedSkill = raceGrantedSkill;

            // === Save to fixed path next to the .exe ===
            try
            {
                string savePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "character.json");

                string json = JsonSerializer.Serialize(CurrentCharacter, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(savePath, json);

                if (showMessage)
                {
                    MessageBox.Show($"✅ Character saved successfully!", "Save Complete");
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

            // First clear all proficiencies
            foreach (var skill in allSkills)
            {
                skill.IsProficient = false;
                skill.IsBackgroundProficiency = false;
            }

            // Apply saved proficiencies
            foreach (var savedSkill in CurrentCharacter.Skills)
            {
                var skill = allSkills.FirstOrDefault(s =>
                    s.SkillName.Equals(savedSkill.Name, StringComparison.OrdinalIgnoreCase));

                if (skill != null)
                {
                    skill.IsProficient = true;
                    skill.IsBackgroundProficiency = true; // Treat loaded skills as granted
                }
            }

            dgSkills.Items.Refresh();
        }

        private void RestoreSelectedFeat()
        {
            if (string.IsNullOrEmpty(CurrentCharacter.SelectedFeat) || dgFeats.ItemsSource == null)
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
        }

        private void RestoreCantrips()
        {
            if (CurrentCharacter.Cantrips == null || cantripOptions == null) return;

            foreach (var option in cantripOptions)
            {
                option.IsChecked = CurrentCharacter.Cantrips.Contains(option.Name, StringComparer.OrdinalIgnoreCase);
            }

            dgCantrips.Items.Refresh();
            UpdateCantripCounter();
        }

        private void RestoreLevel1Spells()
        {
            if (CurrentCharacter.Level1Spells == null || spell1Options == null) return;

            foreach (var option in spell1Options)
            {
                option.IsChecked = CurrentCharacter.Level1Spells.Contains(option.Name, StringComparer.OrdinalIgnoreCase);
            }

            dgSpells1.Items.Refresh();
            UpdateSpellCounter();
        }

        private void RestoreEquipment()
        {
            if (CurrentCharacter.Equipment == null || pnlTotalEquipmentSummary == null)
                return;

            // Clear current summary
            pnlTotalEquipmentSummary.Children.Clear();

            // Add header
            var header = new TextBlock
            {
                Text = "YOUR TOTAL STARTING EQUIPMENT",
                FontWeight = FontWeights.Bold,
                Foreground = AccentGreen,
                Margin = new Thickness(0, 25, 0, 8),
                FontSize = 15
            };
            pnlTotalEquipmentSummary.Children.Add(header);

            // Repopulate saved equipment
            foreach (string item in CurrentCharacter.Equipment.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var tb = new TextBlock
                {
                    Text = "• " + item,
                    Foreground = Brushes.White,
                    Margin = new Thickness(10, 2, 0, 2),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13
                };
                pnlTotalEquipmentSummary.Children.Add(tb);
            }

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
                string loadPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "character.json");

                if (!File.Exists(loadPath))
                {
                    MessageBox.Show("No saved character found (character.json).", "Load Failed");
                    return;
                }

                string json = File.ReadAllText(loadPath);
                CurrentCharacter = JsonSerializer.Deserialize<Character>(json);

                if (CurrentCharacter == null)
                {
                    MessageBox.Show("Failed to load character data.", "Error");
                    return;
                }

                // === Restore UI from CurrentCharacter ===
                txtCharacterName.Text = CurrentCharacter.Name;
                txtPlayerName.Text = CurrentCharacter.PlayerName;

                if (!string.IsNullOrEmpty(CurrentCharacter.Race))
                    cmbRace.SelectedItem = CurrentCharacter.Race;

                if (!string.IsNullOrEmpty(CurrentCharacter.Background))
                    cmbBackground.SelectedItem = CurrentCharacter.Background;

                if (!string.IsNullOrEmpty(CurrentCharacter.Class))
                    cmbClass.SelectedItem = CurrentCharacter.Class;

                // Restore ability scores, skills, etc.
                RestoreAbilityScores();
                RestoreSkills();
                RestoreSelectedFeat();
                RestoreCantrips();
                RestoreLevel1Spells();
                RestoreEquipment();

                UpdateStatDisplays();
                UpdateSkillTabLabels();

                if (cmbClass.SelectedItem is string className)
                    UpdateSkillChoices(className);

                MessageBox.Show("Character loaded successfully!", "Load Complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading character:\n{ex.Message}", "Load Error");
            }
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
        public bool IsSelectable { get; set; } = true;
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

                    // === KEY FIX: Recalculate bonus immediately ===
                    if (Application.Current.MainWindow is MainWindow main)
                    {
                        main.UpdateSkillBonuses();
                    }
                }
            }
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
