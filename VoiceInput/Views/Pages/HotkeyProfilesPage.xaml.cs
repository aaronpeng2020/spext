using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using VoiceInput.Models;
using VoiceInput.Services;
using VoiceInput.Views.Dialogs;

namespace VoiceInput.Views.Pages
{
    public partial class HotkeyProfilesPage : Page
    {
        private readonly IHotkeyProfileService _profileService;
        private readonly ILoggerService _logger;
        private ObservableCollection<HotkeyProfile> _profiles;

        public HotkeyProfilesPage()
        {
            InitializeComponent();
            
            var app = Application.Current as App;
            var serviceProvider = app?.GetServiceProvider();
            
            _profileService = serviceProvider?.GetService<IHotkeyProfileService>();
            _logger = serviceProvider?.GetService<ILoggerService>();
            
            _profiles = new ObservableCollection<HotkeyProfile>();
            ProfilesDataGrid.ItemsSource = _profiles;
            
            // 延迟加载，等待页面加载完成
            Loaded += (s, e) => LoadProfiles();
        }

        private async void LoadProfiles()
        {
            try
            {
                if (_profileService == null)
                {
                    _logger?.Warn("配置服务未初始化");
                    return;
                }
                
                var profiles = await _profileService.GetProfilesAsync();
                _profiles.Clear();
                
                foreach (var profile in profiles.OrderBy(p => !p.IsDefault).ThenBy(p => p.Name))
                {
                    _profiles.Add(profile);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"加载配置失败: {ex.Message}", ex);
                MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new HotkeyProfileEditDialog(_profileService, _logger);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    var newProfile = dialog.Profile;
                    if (await _profileService.AddProfileAsync(newProfile))
                    {
                        _profiles.Add(newProfile);
                        _logger?.Info($"添加配置成功: {newProfile.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"添加配置失败: {ex.Message}", ex);
                MessageBox.Show($"添加配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var profile = button?.Tag as HotkeyProfile;
                if (profile == null) return;

                var dialog = new HotkeyProfileEditDialog(_profileService, _logger, profile.Clone());
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    var updatedProfile = dialog.Profile;
                    
                    if (await _profileService.UpdateProfileAsync(updatedProfile))
                    {
                        // 更新列表中的项
                        var index = _profiles.IndexOf(profile);
                        if (index >= 0)
                        {
                            _profiles[index] = updatedProfile;
                        }
                        _logger?.Info($"更新配置成功: {updatedProfile.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"编辑配置失败: {ex.Message}", ex);
                MessageBox.Show($"编辑配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var profile = button?.Tag as HotkeyProfile;
                if (profile == null) return;

                var result = MessageBox.Show(
                    $"确定要删除配置 \"{profile.Name}\" 吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (await _profileService.RemoveProfileAsync(profile.Id))
                    {
                        _profiles.Remove(profile);
                        _logger?.Info($"删除配置成功: {profile.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"删除配置失败: {ex.Message}", ex);
                MessageBox.Show($"删除配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "导入配置",
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    DefaultExt = ".json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (await _profileService.ImportConfigurationAsync(dialog.FileName))
                    {
                        LoadProfiles();
                        MessageBox.Show("配置导入成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"导入配置失败: {ex.Message}", ex);
                MessageBox.Show($"导入配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "导出配置",
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = $"spext_profiles_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (await _profileService.ExportConfigurationAsync(dialog.FileName))
                    {
                        MessageBox.Show("配置导出成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"导出配置失败: {ex.Message}", ex);
                MessageBox.Show($"导出配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "确定要恢复默认配置吗？这将删除所有自定义配置。",
                    "确认恢复",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (await _profileService.RestoreDefaultsAsync())
                    {
                        LoadProfiles();
                        MessageBox.Show("已恢复默认配置！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"恢复默认配置失败: {ex.Message}", ex);
                MessageBox.Show($"恢复默认配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 处理启用/禁用状态变化
        private async void ProfilesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "启用" && e.EditAction == DataGridEditAction.Commit)
            {
                var profile = e.Row.Item as HotkeyProfile;
                if (profile != null)
                {
                    // 获取 CheckBox 的新值
                    var checkBox = e.EditingElement as System.Windows.Controls.CheckBox;
                    if (checkBox != null)
                    {
                        var newValue = checkBox.IsChecked ?? false;
                        profile.IsEnabled = newValue;
                        await _profileService.SetProfileEnabledAsync(profile.Id, newValue);
                        _logger?.Info($"更新配置 '{profile.Name}' 的启用状态为: {newValue}");
                    }
                }
            }
        }
    }
}