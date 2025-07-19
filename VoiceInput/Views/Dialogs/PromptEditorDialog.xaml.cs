using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using VoiceInput.Models;
using VoiceInput.Services;

namespace VoiceInput.Views.Dialogs
{
    public partial class PromptEditorDialog : Window
    {
        private readonly HotkeyProfile _profile;
        private readonly ICustomPromptService _promptService;
        private string _originalPrompt;

        public string ProfileName => _profile.Name;

        public PromptEditorDialog(HotkeyProfile profile)
        {
            _profile = profile;
            _originalPrompt = profile.CustomPrompt;
            
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            _promptService = serviceProvider?.GetService<ICustomPromptService>();

            InitializeComponent();
            DataContext = this;
            
            LoadTemplates();
            PromptTextBox.Text = profile.CustomPrompt;
            UpdatePreview();
        }

        private void LoadTemplates()
        {
            if (_promptService == null) return;

            var templates = _promptService.GetPromptTemplates();
            
            // 添加一个空项作为默认选项
            TemplateComboBox.Items.Add(new PromptTemplate 
            { 
                Name = "-- 选择模板 --", 
                Description = "选择一个模板来快速开始",
                Template = ""
            });
            
            foreach (var template in templates)
            {
                // 根据语言过滤模板
                if (template.SupportedLanguages.Contains("all") || 
                    template.SupportedLanguages.Contains(_profile.InputLanguage) ||
                    template.SupportedLanguages.Contains(_profile.OutputLanguage))
                {
                    TemplateComboBox.Items.Add(template);
                }
            }
            
            TemplateComboBox.SelectedIndex = 0;
        }

        private void TemplateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateComboBox.SelectedItem is PromptTemplate template && !string.IsNullOrEmpty(template.Template))
            {
                PromptTextBox.Text = template.Template;
            }
        }

        private void PromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharCount();
            UpdatePreview();
            ValidatePrompt();
        }

        private void UpdateCharCount()
        {
            var length = PromptTextBox.Text.Length;
            CharCountTextBlock.Text = $"{length}/500";
            
            if (length > 500)
            {
                CharCountTextBlock.Foreground = Brushes.Red;
            }
            else if (length > 400)
            {
                CharCountTextBlock.Foreground = Brushes.Orange;
            }
            else
            {
                CharCountTextBlock.Foreground = Brushes.Black;
            }
        }

        private void UpdatePreview()
        {
            if (_promptService == null) return;
            
            var preview = _promptService.ProcessPrompt(PromptTextBox.Text, _profile);
            PreviewTextBox.Text = preview;
        }

        private bool ValidatePrompt()
        {
            if (_promptService == null) return true;
            
            string errorMessage;
            if (!_promptService.ValidatePrompt(PromptTextBox.Text, out errorMessage))
            {
                ValidationTextBlock.Text = errorMessage;
                ValidationTextBlock.Visibility = Visibility.Visible;
                SaveButton.IsEnabled = false;
                return false;
            }
            else
            {
                ValidationTextBlock.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = true;
                return true;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_promptService == null) return;
            
            var defaultPrompt = _promptService.GetDefaultPrompt(_profile.InputLanguage, _profile.OutputLanguage);
            PromptTextBox.Text = defaultPrompt;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePrompt()) return;
            
            _profile.CustomPrompt = _promptService?.SanitizePrompt(PromptTextBox.Text) ?? PromptTextBox.Text;
            _profile.UpdatedAt = DateTime.Now;
            
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 恢复原始值
            _profile.CustomPrompt = _originalPrompt;
            
            DialogResult = false;
            Close();
        }
    }
}