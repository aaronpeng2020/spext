using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VoiceInput.Models;

namespace VoiceInput.Services
{
    public interface IProfileConfigurationService
    {
        Task<ProfileConfiguration> LoadConfigurationAsync();
        Task SaveConfigurationAsync(ProfileConfiguration configuration);
        Task<bool> ExportConfigurationAsync(string filePath, ProfileConfiguration configuration);
        Task<ProfileConfiguration> ImportConfigurationAsync(string filePath);
        void BackupConfiguration();
        string GetConfigurationPath();
    }

    public class ProfileConfigurationService : IProfileConfigurationService
    {
        private readonly ILoggerService _logger;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly string _backupDirectory;
        private readonly JsonSerializerSettings _jsonSettings;

        public ProfileConfigurationService(ILoggerService logger)
        {
            _logger = logger;
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SPEXT"
            );
            _configFilePath = Path.Combine(_configDirectory, "profiles.json");
            _backupDirectory = Path.Combine(_configDirectory, "Backups");

            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore
            };

            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                    _logger.Info($"Created configuration directory: {_configDirectory}");
                }

                if (!Directory.Exists(_backupDirectory))
                {
                    Directory.CreateDirectory(_backupDirectory);
                    _logger.Info($"Created backup directory: {_backupDirectory}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create directories: {ex.Message}", ex);
            }
        }

        public async Task<ProfileConfiguration> LoadConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.Info("Configuration file not found, creating default configuration");
                    var defaultConfig = ProfileConfiguration.CreateDefault();
                    await SaveConfigurationAsync(defaultConfig);
                    return defaultConfig;
                }

                var json = await File.ReadAllTextAsync(_configFilePath);
                var configuration = JsonConvert.DeserializeObject<ProfileConfiguration>(json, _jsonSettings);

                if (configuration == null || configuration.Version == 0)
                {
                    _logger.Warn("Invalid configuration file, creating default configuration");
                    var defaultConfig = ProfileConfiguration.CreateDefault();
                    await SaveConfigurationAsync(defaultConfig);
                    return defaultConfig;
                }

                _logger.Info($"Loaded configuration with {configuration.Profiles.Count} profiles");
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load configuration: {ex.Message}", ex);
                BackupCorruptedFile();
                var defaultConfig = ProfileConfiguration.CreateDefault();
                await SaveConfigurationAsync(defaultConfig);
                return defaultConfig;
            }
        }

        public async Task SaveConfigurationAsync(ProfileConfiguration configuration)
        {
            try
            {
                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                configuration.LastModified = DateTime.Now;
                var json = JsonConvert.SerializeObject(configuration, _jsonSettings);
                
                var tempFile = _configFilePath + ".tmp";
                await File.WriteAllTextAsync(tempFile, json);

                if (File.Exists(_configFilePath))
                {
                    BackupConfiguration();
                }

                File.Move(tempFile, _configFilePath, true);
                _logger.Info($"Saved configuration with {configuration.Profiles.Count} profiles");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save configuration: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> ExportConfigurationAsync(string filePath, ProfileConfiguration configuration)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("File path cannot be empty", nameof(filePath));

                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                var json = JsonConvert.SerializeObject(configuration, _jsonSettings);
                await File.WriteAllTextAsync(filePath, json);
                
                _logger.Info($"Exported configuration to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to export configuration: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<ProfileConfiguration> ImportConfigurationAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Configuration file not found", filePath);

                var json = await File.ReadAllTextAsync(filePath);
                var configuration = JsonConvert.DeserializeObject<ProfileConfiguration>(json, _jsonSettings);

                if (configuration == null || configuration.Version == 0)
                    throw new InvalidOperationException("Invalid configuration file format");

                foreach (var profile in configuration.Profiles)
                {
                    if (!profile.IsValid())
                        throw new InvalidOperationException($"Invalid profile: {profile.Name}");
                }

                _logger.Info($"Imported configuration with {configuration.Profiles.Count} profiles from: {filePath}");
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to import configuration: {ex.Message}", ex);
                throw;
            }
        }

        public void BackupConfiguration()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return;

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"profiles_backup_{timestamp}.json";
                var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

                File.Copy(_configFilePath, backupFilePath, true);
                _logger.Info($"Created configuration backup: {backupFileName}");

                CleanupOldBackups();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to backup configuration: {ex.Message}", ex);
            }
        }

        private void BackupCorruptedFile()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return;

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var corruptedFileName = $"profiles_corrupted_{timestamp}.json";
                var corruptedFilePath = Path.Combine(_backupDirectory, corruptedFileName);

                File.Move(_configFilePath, corruptedFilePath);
                _logger.Warn($"Moved corrupted configuration file to: {corruptedFileName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to backup corrupted file: {ex.Message}", ex);
            }
        }

        private void CleanupOldBackups()
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupDirectory, "profiles_backup_*.json");
                if (backupFiles.Length <= 10)
                    return;

                Array.Sort(backupFiles);
                var filesToDelete = backupFiles.Length - 10;

                for (int i = 0; i < filesToDelete; i++)
                {
                    File.Delete(backupFiles[i]);
                    _logger.Info($"Deleted old backup: {Path.GetFileName(backupFiles[i])}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to cleanup old backups: {ex.Message}", ex);
            }
        }

        public string GetConfigurationPath()
        {
            return _configFilePath;
        }
    }
}