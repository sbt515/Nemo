using System;
using System.Linq;
using System.Windows;

namespace Nemo
{
    /// <summary>
    /// Dialog to save the current character as a reusable Custom build template.
    /// </summary>
    public partial class CreateTemplateWindow : Window
    {
        private readonly Character _source;

        public CharacterBuildTemplate? CreatedTemplate { get; private set; }

        public CreateTemplateWindow(Character sourceCharacter)
        {
            InitializeComponent();
            _source = sourceCharacter ?? throw new ArgumentNullException(nameof(sourceCharacter));

            cmbRole.ItemsSource = CharacterTemplateGenerator.RoleNames.ToList();
            cmbRole.SelectedIndex = 0;

            string race = (_source.Race ?? "").Trim();
            string className = (_source.Class ?? "").Trim();
            string subclass = (_source.Subclass ?? "").Trim();

            // Suggested name: subclass class, else race class
            if (!string.IsNullOrEmpty(subclass) && !string.IsNullOrEmpty(className))
                txtName.Text = $"{subclass} {className}";
            else if (!string.IsNullOrEmpty(race) && !string.IsNullOrEmpty(className))
                txtName.Text = $"{race} {className}";
            else if (!string.IsNullOrEmpty(className))
                txtName.Text = className;
            else
                txtName.Text = "My Custom Build";

            // Guess role from class if possible
            string guessed = GuessRole(className, subclass);
            if (cmbRole.Items.Cast<object>().Any(i =>
                    string.Equals(i?.ToString(), guessed, StringComparison.OrdinalIgnoreCase)))
                cmbRole.SelectedItem = guessed;

            UpdatePreview();
            txtName.SelectAll();
            txtName.Focus();
        }

        /// <summary>
        /// Shows the create dialog. Returns the saved template, or null if cancelled.
        /// </summary>
        public static CharacterBuildTemplate? Create(Window owner, Character sourceCharacter)
        {
            var dlg = new CreateTemplateWindow(sourceCharacter) { Owner = owner };
            return dlg.ShowDialog() == true ? dlg.CreatedTemplate : null;
        }

        private void UpdatePreview()
        {
            var draft = CharacterTemplateGenerator.CreateCustomTemplateFromCharacter(
                _source,
                string.IsNullOrWhiteSpace(txtName.Text) ? "Preview" : txtName.Text.Trim(),
                CharacterTemplateGenerator.ParseRole(cmbRole.SelectedItem as string),
                txtDescription.Text);

            txtKitPreview.Text = CharacterTemplateGenerator.FormatKitLine(draft);

            int level = CharacterTemplateGenerator.GetTemplateTotalLevel(draft);
            string classLine = CharacterTemplateGenerator.FormatClassLevelsLine(
                draft.ClassLevels, draft.Class, draft.Subclass, level);

            var skillLine = draft.PreferredSkills is { Length: > 0 }
                ? string.Join(", ", draft.PreferredSkills)
                : "(none marked)";
            var spellBits = new System.Collections.Generic.List<string>();
            if (draft.PreferredCantrips is { Length: > 0 })
                spellBits.Add("Cantrips: " + string.Join(", ", draft.PreferredCantrips));
            if (draft.PreferredSpells is { Length: > 0 })
                spellBits.Add("Spells: " + string.Join(", ", draft.PreferredSpells));

            int asiCount = draft.AsiOrFeatDecisions?.Length ?? 0;
            string asiLine = asiCount > 0
                ? $"ASI/feat slots: {asiCount}"
                : (level >= 4 ? "ASI/feat slots: (auto on generate)" : "");

            var featureBits = new System.Collections.Generic.List<string>();
            if (draft.PreferredFightingStyles is { Length: > 0 })
                featureBits.Add("Fighting styles: " + string.Join(", ", draft.PreferredFightingStyles));
            if (!string.IsNullOrWhiteSpace(draft.PreferredFightingInitiateStyle))
                featureBits.Add("Fighting Initiate: " + draft.PreferredFightingInitiateStyle);
            if (!string.IsNullOrWhiteSpace(draft.PreferredPactBoon))
                featureBits.Add("Pact Boon: " + draft.PreferredPactBoon);
            if (draft.PreferredEldritchInvocations is { Length: > 0 })
                featureBits.Add("Invocations: " + string.Join(", ", draft.PreferredEldritchInvocations));
            if (draft.PreferredMetamagic is { Length: > 0 })
                featureBits.Add("Metamagic: " + string.Join(", ", draft.PreferredMetamagic));

            txtDetailsPreview.Text =
                $"Level {level}" + (string.IsNullOrEmpty(classLine) ? "" : $" · {classLine}") + "\n" +
                "Skills: " + skillLine +
                (spellBits.Count > 0 ? "\n" + string.Join("\n", spellBits) : "") +
                (featureBits.Count > 0 ? "\n" + string.Join("\n", featureBits) : "") +
                "\nAbility priority: " + string.Join(" → ", draft.AbilityPriority ?? Array.Empty<string>()) +
                (string.IsNullOrEmpty(asiLine) ? "" : "\n" + asiLine);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "";
            string name = (txtName.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                txtStatus.Text = "Enter a template name.";
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_source.Race) || string.IsNullOrWhiteSpace(_source.Class))
            {
                txtStatus.Text = "Current character needs a race and class before it can be saved as a template.";
                return;
            }

            bool exists = CustomTemplateStore.GetAll()
                .Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                var overwrite = MessageBox.Show(
                    $"A custom template named \"{name}\" already exists. Replace it?",
                    "Replace Template",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (overwrite != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                var template = CharacterTemplateGenerator.CreateCustomTemplateFromCharacter(
                    _source,
                    name,
                    CharacterTemplateGenerator.ParseRole(cmbRole.SelectedItem as string),
                    txtDescription.Text);

                CustomTemplateStore.Save(template);
                CreatedTemplate = template;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Could not save template: " + ex.Message;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string GuessRole(string className, string subclass)
        {
            string c = (className ?? "").Trim();
            string s = (subclass ?? "").Trim();

            // Quick heuristics from common 5e roles
            if (c.Equals("Cleric", StringComparison.OrdinalIgnoreCase) &&
                (s.Contains("Life", StringComparison.OrdinalIgnoreCase) ||
                 s.Contains("Peace", StringComparison.OrdinalIgnoreCase) ||
                 s.Contains("Twilight", StringComparison.OrdinalIgnoreCase)))
                return "Support";
            if (c.Equals("Bard", StringComparison.OrdinalIgnoreCase) &&
                !s.Contains("Swords", StringComparison.OrdinalIgnoreCase) &&
                !s.Contains("Valor", StringComparison.OrdinalIgnoreCase))
                return "Support";
            if (c.Equals("Barbarian", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Fighter", StringComparison.OrdinalIgnoreCase) &&
                (s.Contains("Cavalier", StringComparison.OrdinalIgnoreCase) ||
                 s.Contains("Rune", StringComparison.OrdinalIgnoreCase)))
                return "Tank";
            if (c.Equals("Paladin", StringComparison.OrdinalIgnoreCase) &&
                (s.Contains("Ancients", StringComparison.OrdinalIgnoreCase) ||
                 s.Contains("Crown", StringComparison.OrdinalIgnoreCase) ||
                 s.Contains("Devotion", StringComparison.OrdinalIgnoreCase)))
                return "Tank";
            if (c.Equals("Warlock", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Sorcerer", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Wizard", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Ranger", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Rogue", StringComparison.OrdinalIgnoreCase))
                return "Damage";

            return "Support";
        }
    }
}
