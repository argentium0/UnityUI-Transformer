using System;
using UnityEngine;

namespace UnityUITransformer.App.ViewModels
{
    public class LogEntryModel
    {
        public DateTime Timestamp { get; }
        public ShimLogLevel Level { get; }
        public string Message { get; }

        public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");

        public LogEntryModel(ShimLogLevel level, string message)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{FormattedTimestamp}] [{Level.ToString().ToUpper()}] {Message}";
        }
    }
}
