using System;

namespace VoiceInput.Services
{
    public interface ILoggerService
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception exception = null);
        void Debug(string message);
        string GetLogFilePath();
    }

    public class LoggerServiceWrapper : ILoggerService
    {
        public void Info(string message)
        {
            LoggerService.Log($"[INFO] {message}");
        }

        public void Warn(string message)
        {
            LoggerService.Log($"[WARN] {message}");
        }

        public void Error(string message, Exception exception = null)
        {
            if (exception != null)
            {
                LoggerService.Log($"[ERROR] {message}\n{exception}");
            }
            else
            {
                LoggerService.Log($"[ERROR] {message}");
            }
        }

        public void Debug(string message)
        {
            LoggerService.Log($"[DEBUG] {message}");
        }

        public string GetLogFilePath()
        {
            return LoggerService.GetLogFilePath();
        }
    }
}