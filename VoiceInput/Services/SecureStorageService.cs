using System;
using CredentialManagement;
using VoiceInput.Services;

namespace VoiceInput.Services
{
    public class SecureStorageService
    {
        private const string CREDENTIAL_TARGET = "VoiceInput_APIKey";
        private const string PROXY_CREDENTIAL_TARGET = "VoiceInput_Proxy";

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
    }
}