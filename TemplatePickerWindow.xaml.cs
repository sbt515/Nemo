using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Nemo
{
    /// <summary>
    /// Lists Optimized / General builds for a role so the user can pick one.
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
            txtSubheader.Text =
                CharacterTemplateGenerator.DescribeCategory(category) + "\n" +
                CharacterTemplateGenerator.DescribeRole(role) +
                "\n\nSelect a build to generate a complete level-1 character.";

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
            }
        }

        /// <summary>
        /// Shows the picker. Returns the chosen template, or null if cancelled / empty.
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
            bool has = lstBuilds.SelectedItem is BuildListItem;
            btnSelect.IsEnabled = has;
            txtSelectionHint.Text = has
                ? "Double-click a build or press Use This Build."
                : "Select a build to continue.";
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

            SelectedTemplate = item.Template;
            DialogResult = true;
            Close();
        }

        private sealed class BuildListItem
        {
            public CharacterBuildTemplate Template { get; }
            public string Name => Template.Name;
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
