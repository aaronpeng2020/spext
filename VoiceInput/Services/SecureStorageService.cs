using System;
using CredentialManagement;
using VoiceInput.Services;

namespace VoiceInput.Services
{
    public class SecureStorageService
    {
        private const string CREDENTIAL_TARGET = "VoiceInput_APIKey";
        private const string PROXY_CREDENTIAL_TARGET = "VoiceInput_Proxy";
        private const string WHISPER_CREDENTIAL_TARGET = "VoiceInput_WhisperAPIKey";
        private const string GPT_CREDENTIAL_TARGET = "VoiceInput_GPTAPIKey";
        private const string TTS_CREDENTIAL_TARGET = "VoiceInput_TTSAPIKey";

        public void SaveApiKey(string apiKey)
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = CREDENTIAL_TARGET;
                    credential.Username = "VoiceInput";
                    credential.Password = apiKey;
                    credential.Type = CredentialType.Generic;
                    credential.PersistanceType = PersistanceType.LocalComputer;
                    credential.Save();
                }
                
                LoggerService.Log("API密钥已安全保存到Windows凭据管理器");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"保存API密钥失败: {ex.Message}");
                throw new Exception("无法保存API密钥到凭据管理器", ex);
            }
        }

        public string? LoadApiKey()
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = CREDENTIAL_TARGET;
                    credential.Load();
                    
                    if (!string.IsNullOrEmpty(credential.Password))
                    {
                        LoggerService.Log("从Windows凭据管理器加载API密钥成功");
                        return credential.Password;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"加载API密钥失败: {ex.Message}");
            }

            return null;
        }

        public void DeleteApiKey()
        {
            try
            {
                using (var credential = new Credential { Target = CREDENTIAL_TARGET })
                {
                    credential.Delete();
                    LoggerService.Log("API密钥已从Windows凭据管理器删除");
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"删除API密钥失败: {ex.Message}");
            }
        }

        public bool HasApiKey()
        {
            try
            {
                using (var credential = new Credential { Target = CREDENTIAL_TARGET })
                {
                    return credential.Exists();
                }
            }
            catch
            {
                return false;
            }
        }

        // 代理凭据管理
        public void SaveProxyCredentials(string username, string password)
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = PROXY_CREDENTIAL_TARGET;
                    credential.Username = username;
                    credential.Password = password;
                    credential.Type = CredentialType.Generic;
                    credential.PersistanceType = PersistanceType.LocalComputer;
                    credential.Save();
                }
                
                LoggerService.Log("代理凭据已安全保存到Windows凭据管理器");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"保存代理凭据失败: {ex.Message}");
                throw new Exception("无法保存代理凭据到凭据管理器", ex);
            }
        }

        public (string? username, string? password) LoadProxyCredentials()
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = PROXY_CREDENTIAL_TARGET;
                    credential.Load();
                    
                    if (!string.IsNullOrEmpty(credential.Username))
                    {
                        LoggerService.Log("从Windows凭据管理器加载代理凭据成功");
                        return (credential.Username, credential.Password);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"加载代理凭据失败: {ex.Message}");
            }

            return (null, null);
        }

        public void DeleteProxyCredentials()
        {
            try
            {
                using (var credential = new Credential { Target = PROXY_CREDENTIAL_TARGET })
                {
                    credential.Delete();
                    LoggerService.Log("代理凭据已从Windows凭据管理器删除");
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"删除代理凭据失败: {ex.Message}");
            }
        }

        // Whisper API 密钥管理
        public void SaveWhisperApiKey(string apiKey)
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = WHISPER_CREDENTIAL_TARGET;
                    credential.Username = "VoiceInput_Whisper";
                    credential.Password = apiKey;
                    credential.Type = CredentialType.Generic;
                    credential.PersistanceType = PersistanceType.LocalComputer;
                    credential.Save();
                }
                
                LoggerService.Log("Whisper API密钥已安全保存到Windows凭据管理器");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"保存Whisper API密钥失败: {ex.Message}");
                throw new Exception("无法保存Whisper API密钥到凭据管理器", ex);
            }
        }

        public string? LoadWhisperApiKey()
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = WHISPER_CREDENTIAL_TARGET;
                    credential.Load();
                    
                    if (!string.IsNullOrEmpty(credential.Password))
                    {
                        LoggerService.Log("从Windows凭据管理器加载Whisper API密钥成功");
                        return credential.Password;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"加载Whisper API密钥失败: {ex.Message}");
            }

            return null;
        }

        public void DeleteWhisperApiKey()
        {
            try
            {
                using (var credential = new Credential { Target = WHISPER_CREDENTIAL_TARGET })
                {
                    credential.Delete();
                    LoggerService.Log("Whisper API密钥已从Windows凭据管理器删除");
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"删除Whisper API密钥失败: {ex.Message}");
            }
        }

        public bool HasWhisperApiKey()
        {
            try
            {
                using (var credential = new Credential { Target = WHISPER_CREDENTIAL_TARGET })
                {
                    return credential.Exists();
                }
            }
            catch
            {
                return false;
            }
        }

        // GPT API 密钥管理
        public void SaveGPTApiKey(string apiKey)
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = GPT_CREDENTIAL_TARGET;
                    credential.Username = "VoiceInput_GPT";
                    credential.Password = apiKey;
                    credential.Type = CredentialType.Generic;
                    credential.PersistanceType = PersistanceType.LocalComputer;
                    credential.Save();
                }
                
                LoggerService.Log("GPT API密钥已安全保存到Windows凭据管理器");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"保存GPT API密钥失败: {ex.Message}");
                throw new Exception("无法保存GPT API密钥到凭据管理器", ex);
            }
        }

        public string? LoadGPTApiKey()
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = GPT_CREDENTIAL_TARGET;
                    credential.Load();
                    
                    if (!string.IsNullOrEmpty(credential.Password))
                    {
                        LoggerService.Log("从Windows凭据管理器加载GPT API密钥成功");
                        return credential.Password;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"加载GPT API密钥失败: {ex.Message}");
            }

            return null;
        }

        public void DeleteGPTApiKey()
        {
            try
            {
                using (var credential = new Credential { Target = GPT_CREDENTIAL_TARGET })
                {
                    credential.Delete();
                    LoggerService.Log("GPT API密钥已从Windows凭据管理器删除");
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"删除GPT API密钥失败: {ex.Message}");
            }
        }

        public bool HasGPTApiKey()
        {
            try
            {
                using (var credential = new Credential { Target = GPT_CREDENTIAL_TARGET })
                {
                    return credential.Exists();
                }
            }
            catch
            {
                return false;
            }
        }
        
        // TTS API 密钥管理
        public void SaveTtsApiKey(string apiKey)
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = TTS_CREDENTIAL_TARGET;
                    credential.Username = "VoiceInput_TTS";
                    credential.Password = apiKey;
                    credential.Type = CredentialType.Generic;
                    credential.PersistanceType = PersistanceType.LocalComputer;
                    credential.Save();
                }
                
                LoggerService.Log("TTS API密钥已安全保存到Windows凭据管理器");
            }
            catch (Exception ex)
            {
                LoggerService.Log($"保存TTS API密钥失败: {ex.Message}");
                throw new Exception("无法保存TTS API密钥到凭据管理器", ex);
            }
        }

        public string? LoadTtsApiKey()
        {
            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = TTS_CREDENTIAL_TARGET;
                    credential.Load();
                    
                    if (!string.IsNullOrEmpty(credential.Password))
                    {
                        LoggerService.Log("从Windows凭据管理器加载TTS API密钥成功");
                        return credential.Password;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"加载TTS API密钥失败: {ex.Message}");
            }

            return null;
        }

        public void DeleteTtsApiKey()
        {
            try
            {
                using (var credential = new Credential { Target = TTS_CREDENTIAL_TARGET })
                {
                    credential.Delete();
                    LoggerService.Log("TTS API密钥已从Windows凭据管理器删除");
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"删除TTS API密钥失败: {ex.Message}");
            }
        }

        public bool HasTtsApiKey()
        {
            try
            {
                using (var credential = new Credential { Target = TTS_CREDENTIAL_TARGET })
                {
                    return credential.Exists();
                }
            }
            catch
            {
                return false;
            }
        }
    }
}