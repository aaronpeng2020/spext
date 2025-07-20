using System;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceInput.Services
{
    public interface ITextToSpeechService
    {
        /// <summary>
        /// 朗读文本
        /// </summary>
        /// <param name="text">要朗读的文本</param>
        /// <param name="language">语言代码（如 zh, en, ja）</param>
        /// <param name="voice">指定语音，如果为 null 或 "auto" 则自动选择</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功开始朗读</returns>
        Task<bool> SpeakAsync(string text, string language, string voice = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止当前朗读
        /// </summary>
        void StopSpeaking();

        /// <summary>
        /// 是否正在朗读
        /// </summary>
        bool IsSpeaking { get; }

        /// <summary>
        /// 朗读完成事件
        /// </summary>
        event EventHandler<string> SpeakingCompleted;

        /// <summary>
        /// 朗读失败事件
        /// </summary>
        event EventHandler<Exception> SpeakingFailed;
    }
}