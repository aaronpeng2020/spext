using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoiceInput.Models;

namespace VoiceInput.Services
{
    public interface IHotkeyProfileService
    {
        event EventHandler<HotkeyProfile> ProfileAdded;
        event EventHandler<HotkeyProfile> ProfileUpdated;
        event EventHandler<string> ProfileRemoved;
        event EventHandler<ProfileConfiguration> ConfigurationChanged;

        Task InitializeAsync();
        Task<ProfileConfiguration> GetConfigurationAsync();
        Task<List<HotkeyProfile>> GetProfilesAsync();
        Task<List<HotkeyProfile>> GetEnabledProfilesAsync();
        Task<HotkeyProfile> GetProfileByIdAsync(string profileId);
        Task<HotkeyProfile> GetProfileByHotkeyAsync(string hotkey);
        Task<bool> AddProfileAsync(HotkeyProfile profile);
        Task<bool> UpdateProfileAsync(HotkeyProfile profile);
        Task<bool> RemoveProfileAsync(string profileId);
        Task<bool> SetProfileEnabledAsync(string profileId, bool enabled);
        Task<bool> ValidateHotkeyAsync(string hotkey, string excludeProfileId = null);
        Task<bool> RestoreDefaultsAsync();
        Task<bool> ExportConfigurationAsync(string filePath);
        Task<bool> ImportConfigurationAsync(string filePath);
    }

    public class HotkeyProfileService : IHotkeyProfileService
    {
        private readonly IProfileConfigurationService _configurationService;
        private readonly ILoggerService _logger;
        private ProfileConfiguration _configuration;
        private readonly object _lockObject = new object();

        public event EventHandler<HotkeyProfile> ProfileAdded;
        public event EventHandler<HotkeyProfile> ProfileUpdated;
        public event EventHandler<string> ProfileRemoved;
        public event EventHandler<ProfileConfiguration> ConfigurationChanged;

        public HotkeyProfileService(
            IProfileConfigurationService configurationService,
            ILoggerService logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _configuration = await _configurationService.LoadConfigurationAsync();
                _logger.Info($"HotkeyProfileService initialized with {_configuration.Profiles.Count} profiles");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to initialize HotkeyProfileService: {ex.Message}", ex);
                _configuration = ProfileConfiguration.CreateDefault();
            }
        }

        public Task<ProfileConfiguration> GetConfigurationAsync()
        {
            lock (_lockObject)
            {
                return Task.FromResult(_configuration);
            }
        }

        public Task<List<HotkeyProfile>> GetProfilesAsync()
        {
            lock (_lockObject)
            {
                if (_configuration == null)
                {
                    return Task.FromResult(new List<HotkeyProfile>());
                }
                return Task.FromResult(_configuration.Profiles.ToList());
            }
        }

        public Task<List<HotkeyProfile>> GetEnabledProfilesAsync()
        {
            lock (_lockObject)
            {
                if (_configuration == null)
                {
                    return Task.FromResult(new List<HotkeyProfile>());
                }
                return Task.FromResult(_configuration.GetEnabledProfiles());
            }
        }

        public Task<HotkeyProfile> GetProfileByIdAsync(string profileId)
        {
            lock (_lockObject)
            {
                return Task.FromResult(_configuration.GetProfileById(profileId));
            }
        }

        public Task<HotkeyProfile> GetProfileByHotkeyAsync(string hotkey)
        {
            lock (_lockObject)
            {
                return Task.FromResult(_configuration.GetProfileByHotkey(hotkey));
            }
        }

        public async Task<bool> AddProfileAsync(HotkeyProfile profile)
        {
            try
            {
                if (profile == null)
                    throw new ArgumentNullException(nameof(profile));

                lock (_lockObject)
                {
                    _configuration.AddProfile(profile);
                }

                await SaveConfigurationAsync();
                
                ProfileAdded?.Invoke(this, profile);
                _logger.Info($"Added profile: {profile.Name} with hotkey: {profile.Hotkey}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to add profile: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(HotkeyProfile profile)
        {
            try
            {
                if (profile == null)
                    throw new ArgumentNullException(nameof(profile));

                lock (_lockObject)
                {
                    _configuration.UpdateProfile(profile);
                }

                await SaveConfigurationAsync();
                
                ProfileUpdated?.Invoke(this, profile);
                _logger.Info($"Updated profile: {profile.Name}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to update profile: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> RemoveProfileAsync(string profileId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(profileId))
                    throw new ArgumentException("Profile ID cannot be empty", nameof(profileId));

                lock (_lockObject)
                {
                    _configuration.RemoveProfile(profileId);
                }

                await SaveConfigurationAsync();
                
                ProfileRemoved?.Invoke(this, profileId);
                _logger.Info($"Removed profile with ID: {profileId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to remove profile: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> SetProfileEnabledAsync(string profileId, bool enabled)
        {
            try
            {
                HotkeyProfile profile;
                lock (_lockObject)
                {
                    profile = _configuration.GetProfileById(profileId);
                    if (profile == null)
                        throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

                    profile.IsEnabled = enabled;
                    profile.UpdatedAt = DateTime.Now;
                }

                await SaveConfigurationAsync();
                
                ProfileUpdated?.Invoke(this, profile);
                _logger.Info($"Set profile '{profile.Name}' enabled state to: {enabled}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to set profile enabled state: {ex.Message}", ex);
                return false;
            }
        }

        public Task<bool> ValidateHotkeyAsync(string hotkey, string excludeProfileId = null)
        {
            try
            {
                lock (_lockObject)
                {
                    var hasConflict = _configuration.HasHotkeyConflict(hotkey, excludeProfileId);
                    return Task.FromResult(!hasConflict);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to validate hotkey: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }

        public async Task<bool> RestoreDefaultsAsync()
        {
            try
            {
                _logger.Info("Restoring default configuration");
                
                lock (_lockObject)
                {
                    _configuration = ProfileConfiguration.CreateDefault();
                }

                await SaveConfigurationAsync();
                
                ConfigurationChanged?.Invoke(this, _configuration);
                _logger.Info("Default configuration restored");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to restore defaults: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> ExportConfigurationAsync(string filePath)
        {
            try
            {
                ProfileConfiguration config;
                lock (_lockObject)
                {
                    config = _configuration;
                }

                return await _configurationService.ExportConfigurationAsync(filePath, config);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to export configuration: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> ImportConfigurationAsync(string filePath)
        {
            try
            {
                var importedConfig = await _configurationService.ImportConfigurationAsync(filePath);
                
                lock (_lockObject)
                {
                    _configuration = importedConfig;
                }

                await SaveConfigurationAsync();
                
                ConfigurationChanged?.Invoke(this, _configuration);
                _logger.Info($"Imported configuration with {_configuration.Profiles.Count} profiles");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to import configuration: {ex.Message}", ex);
                return false;
            }
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                ProfileConfiguration configToSave;
                lock (_lockObject)
                {
                    configToSave = _configuration;
                }

                await _configurationService.SaveConfigurationAsync(configToSave);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save configuration: {ex.Message}", ex);
                throw;
            }
        }
    }
}