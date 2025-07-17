using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using VoiceInput.Services;

namespace VoiceInput.Views
{
    public partial class SpectrumWindow : Window
    {
        private readonly ConfigManager _configManager;
        private readonly DispatcherTimer _animationTimer;
        private readonly List<Rectangle> _spectrumBars = new List<Rectangle>();
        private readonly int _barCount = 20;
        private readonly double[] _fftData;
        private readonly double[] _smoothedData;
        private bool _isRecording = false;
        
        public SpectrumWindow(ConfigManager configManager)
        {
            InitializeComponent();
            _configManager = configManager;
            
            // 初始化数组
            _fftData = new double[_barCount];
            _smoothedData = new double[_barCount];
            
            // 创建频谱条
            CreateSpectrumBars();
            
            // 初始化动画计时器
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // 30 FPS
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            
            // 窗口初始化时设置为不可见
            Visibility = Visibility.Hidden;
            
            LoggerService.Log("SpectrumWindow initialized");
        }
        
        private void SetWindowPosition()
        {
            var workingArea = SystemParameters.WorkArea;
            var screenWidth = workingArea.Width;
            var screenHeight = workingArea.Height;
            
            // 设置窗口大小
            Width = 100;
            Height = 20;
            
            // 设置窗口位置在任务栏上方中间
            Left = (screenWidth - Width) / 2;
            Top = screenHeight - Height - 10; // 距离底部10像素（任务栏上方）
        }
        
        private void CreateSpectrumBars()
        {
            SpectrumGrid.Children.Clear();
            _spectrumBars.Clear();
            
            for (int i = 0; i < _barCount; i++)
            {
                var bar = new Rectangle
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0.5, 0, 0.5, 0), // 水平间距
                    RadiusX = 0,
                    RadiusY = 0,
                    Height = 0,
                    Fill = new SolidColorBrush(Color.FromRgb(220, 220, 220)), // 浅灰色
                    Width = 2 // 更窄的宽度
                };
                
                SpectrumGrid.Children.Add(bar);
                _spectrumBars.Add(bar);
            }
        }
        
        // 极简风格，不需要颜色配置
        
        public void StartRecording()
        {
            LoggerService.Log($"SpectrumWindow.StartRecording() called, ShowWaveform: {_configManager.ShowWaveform}");
            
            if (!_configManager.ShowWaveform)
            {
                LoggerService.Log("ShowWaveform is false, returning");
                return;
            }
                
            _isRecording = true;
            
            // 重置数据
            for (int i = 0; i < _barCount; i++)
            {
                _fftData[i] = 0;
                _smoothedData[i] = 0;
            }
            
            // 设置窗口位置
            SetWindowPosition();
            
            // 显示窗口
            LoggerService.Log($"Showing spectrum window, IsVisible: {IsVisible}");
            Visibility = Visibility.Visible;
            Show();
            Topmost = true;
            Activate();
            LoggerService.Log($"After Show(), IsVisible: {IsVisible}, Left: {Left}, Top: {Top}, Width: {Width}, Height: {Height}");
            
            // 开始动画
            _animationTimer.Start();
        }
        
        public void StopRecording()
        {
            LoggerService.Log("SpectrumWindow.StopRecording() called");
            _isRecording = false;
            _animationTimer.Stop();
            
            // 隐藏窗口
            Visibility = Visibility.Hidden;
            Hide();
        }
        
        public void UpdateAudioData(float[] audioSamples)
        {
            if (!_isRecording || audioSamples == null || audioSamples.Length == 0)
                return;
            
            // 简单的频率分析（将音频数据分组并计算每组的能量）
            int samplesPerBar = audioSamples.Length / _barCount;
            
            for (int i = 0; i < _barCount; i++)
            {
                double sum = 0;
                int startIndex = i * samplesPerBar;
                int endIndex = Math.Min(startIndex + samplesPerBar, audioSamples.Length);
                
                // 计算该频段的能量
                for (int j = startIndex; j < endIndex; j++)
                {
                    sum += Math.Abs(audioSamples[j]);
                }
                
                // 计算平均值并放大
                double average = (sum / samplesPerBar) * 50;
                
                // 应用对数缩放使显示更自然
                if (average > 0)
                {
                    average = Math.Log10(1 + average * 9) * 0.5;
                }
                
                _fftData[i] = Math.Min(1.0, average);
            }
        }
        
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isRecording)
                return;
                
            UpdateSpectrum();
        }
        
        private void UpdateSpectrum()
        {
            // 平滑处理
            for (int i = 0; i < _barCount; i++)
            {
                // 使用平滑因子让动画更流畅
                _smoothedData[i] = _smoothedData[i] * 0.7 + _fftData[i] * 0.3;
                
                // 添加下降效果
                if (_smoothedData[i] < _fftData[i])
                {
                    _smoothedData[i] = _fftData[i];
                }
                else
                {
                    _smoothedData[i] *= 0.95; // 缓慢下降
                }
            }
            
            // 更新频谱条高度
            var maxHeight = 14; // 最大高度（稍微增加以保持可见性）
            
            for (int i = 0; i < _barCount && i < _spectrumBars.Count; i++)
            {
                var targetHeight = _smoothedData[i] * maxHeight;
                var bar = _spectrumBars[i];
                
                // 直接设置高度，无动画（更极简）
                bar.Height = targetHeight;
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _animationTimer?.Stop();
            base.OnClosed(e);
        }
    }
}