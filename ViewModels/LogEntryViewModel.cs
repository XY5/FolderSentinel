using System;

namespace FolderSentinel.ViewModels
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public class LogEntryViewModel
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public LogLevel Level { get; }
        public string Message { get; }

        public LogEntryViewModel(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss}] {Level}: {Message}";
    }
}
