using System;

namespace UnityEngine
{
    public enum ShimLogLevel
    {
        Info,
        Warning,
        Error
    }

    public class ShimLogEventArgs : EventArgs
    {
        public ShimLogLevel Level { get; }
        public string Message { get; }

        public ShimLogEventArgs(ShimLogLevel level, string message)
        {
            Level = level;
            Message = message ?? string.Empty;
        }
    }

    public static class ShimLogSink
    {
        public static event EventHandler<ShimLogEventArgs>? OnLog;

        public static void RaiseLog(ShimLogLevel level, string message)
        {
            OnLog?.Invoke(null, new ShimLogEventArgs(level, message));
        }
    }
}
