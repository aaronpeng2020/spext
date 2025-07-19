using System;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf;
using VoiceInput.Core;
using VoiceInput.Services;
using VoiceInput.Models;

namespace VoiceInput
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex? _mutex;
        private IServiceProvider? _serviceProvider;
        private TrayIcon? _trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 单实例检查
            _mutex = new Mutex(true, "Spext_SingleInstance", out bool createdNew);
            
            if (!createdNew)
            {
                MessageBox.Show("Spext 已经在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            // 使用日志服务
            LoggerService.Log("===============================================");
            LoggerService.Log("Spext 正在启动...");
            LoggerService.Log($"日志文件位置: {LoggerService.GetLogFilePath()}");
            LoggerService.Log("请查看系统托盘图标");
            LoggerService.Log("按住 F3 键开始录音");
            LoggerService.Log("===============================================");
            
            // 设置默认主题
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

            // 配置服务
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // 隐藏主窗口
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 初始化系统托盘
            _trayIcon = _serviceProvider.GetRequiredService<TrayIcon>();
            _trayIcon.Initialize();

            // 初始化控制器（使用增强版）
            var controller = _serviceProvider.GetRequiredService<EnhancedVoiceInputController>();
            controller.InitializeAsync().GetAwaiter().GetResult();

            LoggerService.Log("Spext 启动成功！");

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            
            services.AddSingleton<IConfiguration>(configuration);
            
            // 注册日志服务
            services.AddSingleton<ILoggerService, LoggerServiceWrapper>();
            
            // 注册基础服务
            services.AddSingleton<SecureStorageService>();
            services.AddSingleton<AutoStartService>();
            services.AddSingleton<TrayIcon>();
            services.AddSingleton<AudioRecorderService>();
            services.AddSingleton<AudioMuteService>();
            services.AddSingleton<TextInputService>();
            services.AddSingleton<ConfigManager>();
            
            // 注册新的增强服务
            services.AddSingleton<IProfileConfigurationService, ProfileConfigurationService>();
            services.AddSingleton<IHotkeyProfileService, HotkeyProfileService>();
            services.AddSingleton<ICustomPromptService, CustomPromptService>();
            services.AddSingleton<IEnhancedSpeechRecognitionService, EnhancedSpeechRecognitionService>();
            services.AddSingleton<IEnhancedGlobalHotkeyService, EnhancedGlobalHotkeyService>();
            
            // 注册控制器（使用增强版）
            services.AddSingleton<EnhancedVoiceInputController>();
            
            // 保留旧服务以便兼容
            services.AddSingleton<GlobalHotkeyService>();
            services.AddSingleton<SpeechRecognitionService>();
            services.AddSingleton<VoiceInputController>();
        }

        public IServiceProvider? GetServiceProvider()
        {
            return _serviceProvider;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            
            base.OnExit(e);
        }
    }
}
