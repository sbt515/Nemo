using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Nemo
{
    /// <summary>
    /// Lists Optimized / General builds for a role so the user can pick one.
    /// Multi-tier kits (e.g. General L1/3/5/8) show a Level dropdown instead of cluttering the list.
    /// </summary>
    public partial class TemplatePickerWindow : Window
    {
        public CharacterBuildTemplate? SelectedTemplate { get; private set; }

        public TemplatePickerWindow(
            TemplateCategory category,
            TemplateRole role,
            IReadOnlyList<CharacterBuildTemplate> templates)
        {
            InitializeComponent();

            string catLabel = category.ToString();
            string roleLabel = role == TemplateRole.None ? "True Random" : role.ToString();

            Title = $"Choose {catLabel} {roleLabel} Build";
            txtHeader.Text = $"{catLabel} · {roleLabel}";
            string levelNote = category is TemplateCategory.General or TemplateCategory.Optimized
                ? "\n\nBuilds support levels 1, 3, 5, and 8 — choose the tier in the Level dropdown after selecting a build. Multiclass kits scale their class split to the chosen level."
                : "\n\nSelect a build to generate a complete character (level and multiclass as listed).";

            txtSubheader.Text =
                CharacterTemplateGenerator.DescribeCategory(category) + "\n" +
                CharacterTemplateGenerator.DescribeRole(role) +
                levelNote;

            var items = (templates ?? Array.Empty<CharacterBuildTemplate>())
                .Select(t => new BuildListItem(t))
                .ToList();

            lstBuilds.ItemsSource = items;
            if (items.Count > 0)
                lstBuilds.SelectedIndex = 0;
            else
            {
                txtSelectionHint.Text = "No builds found for this category and role.";
                btnSelect.IsEnabled = false;
                pnlLevelPick.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Shows the picker. Returns the chosen template (with level applied), or null if cancelled / empty.
        /// </summary>
        public static CharacterBuildTemplate? Pick(
            Window owner,
            TemplateCategory category,
            TemplateRole role)
        {
            var templates = CharacterTemplateGenerator.GetTemplates(category, role);
            if (templates.Count == 0)
                return null;

            var dlg = new TemplatePickerWindow(category, role, templates)
            {
                Owner = owner
            };

            return dlg.ShowDialog() == true ? dlg.SelectedTemplate : null;
        }

        private void lstBuilds_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RefreshLevelPicker();
            bool has = lstBuilds.SelectedItem is BuildListItem;
            btnSelect.IsEnabled = has;
            txtSelectionHint.Text = has
                ? "Double-click a build or press Use This Build."
                : "Select a build to continue.";
        }

        private void RefreshLevelPicker()
        {
            if (pnlLevelPick == null || cmbTemplateLevel == null)
                return;

            if (lstBuilds.SelectedItem is not BuildListItem item)
            {
                pnlLevelPick.Visibility = Visibility.Collapsed;
                cmbTemplateLevel.ItemsSource = null;
                return;
            }

            var tiers = CharacterTemplateGenerator.GetSupportedLevels(item.Template);
            if (tiers.Count <= 1)
            {
                pnlLevelPick.Visibility = Visibility.Collapsed;
                cmbTemplateLevel.ItemsSource = null;
                return;
            }

            // Prefer keeping prior selection when switching between multi-tier kits
            int? previous = cmbTemplateLevel.SelectedItem as int?;
            cmbTemplateLevel.ItemsSource = tiers.ToList();
            if (previous.HasValue && tiers.Contains(previous.Value))
                cmbTemplateLevel.SelectedItem = previous.Value;
            else
            {
                // Default: L5 if available (subclass + Extra Attack tier), else middle, else first
                if (tiers.Contains(5))
                    cmbTemplateLevel.SelectedItem = 5;
                else
                    cmbTemplateLevel.SelectedIndex = Math.Min(1, tiers.Count - 1);
            }

            txtLevelHint.Text =
                $"Available tiers: {string.Join(", ", tiers.Select(l => "L" + l))}. " +
                "Pick the campaign level for this character.";
            pnlLevelPick.Visibility = Visibility.Visible;
        }

        private void lstBuilds_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstBuilds.SelectedItem is BuildListItem)
                AcceptSelection();
        }

        private void Select_Click(object sender, RoutedEventArgs e) => AcceptSelection();

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AcceptSelection()
        {
            if (lstBuilds.SelectedItem is not BuildListItem item)
                return;

            var baseTemplate = item.Template;
            var tiers = CharacterTemplateGenerator.GetSupportedLevels(baseTemplate);
            int level;
            if (tiers.Count > 1 && cmbTemplateLevel?.SelectedItem is int picked && tiers.Contains(picked))
                level = picked;
            else if (tiers.Count > 0)
                level = tiers[0];
            else
                level = 1;

            SelectedTemplate = CharacterTemplateGenerator.ApplyTemplateLevel(baseTemplate, level);
            DialogResult = true;
            Close();
        }

        private sealed class BuildListItem
        {
            public CharacterBuildTemplate Template { get; }
            public string Name => CharacterTemplateGenerator.StripLevelSuffix(Template.Name);
            public string KitLine => CharacterTemplateGenerator.FormatKitLine(Template);
            public string Playstyle =>
                string.IsNullOrWhiteSpace(Template.Description)
                    ? CharacterTemplateGenerator.DescribeRole(Template.Role)
                    : Template.Description;

            public BuildListItem(CharacterBuildTemplate template)
            {
                Template = template ?? throw new ArgumentNullException(nameof(template));
            }
        }
    }
}
