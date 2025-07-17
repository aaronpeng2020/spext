using System;
using System.IO;
using System.Text;

namespace VoiceInput.Services
{
    public static class LoggerService
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VoiceInput",
            "Logs"
        );
        
        private static readonly string LogFilePath = Path.Combine(
            LogDirectory,
            $"VoiceInput_{DateTime.Now:yyyyMMdd}.log"
        );
        
        private static readonly object _lockObject = new object();

        static LoggerService()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch { }
        }

        public static void Log(string message)
        {
            var logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            
            // 输出到控制台
            Console.WriteLine(logMessage);
            
            // 同时写入日志文件
            try
            {
                lock (_lockObject)
                {
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            var errorMessage = ex != null ? $"{message}: {ex.Message}" : message;
            Log($"错误 - {errorMessage}");
            
            if (ex != null)
            {
                Log($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        public static string GetLogFilePath()
        {
            return LogFilePath;
        }
    }
}