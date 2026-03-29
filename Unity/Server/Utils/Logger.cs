using System;
using System.IO;

namespace QBlockyFighter.Server.Utils
{
    /// <summary>
    /// 服务器日志系统 - 分级日志 + 文件输出
    /// </summary>
    public static class Logger
    {
        public enum LogLevel { Debug, Info, Warning, Error, Critical }

        private static LogLevel _minLevel = LogLevel.Info;
        private static string _logDir = "logs";
        private static StreamWriter _fileWriter;
        private static readonly object _lock = new();

        public static void Init(LogLevel minLevel = LogLevel.Info, string logDir = "logs")
        {
            _minLevel = minLevel;
            _logDir = logDir;

            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);

            string logFile = Path.Combine(_logDir, $"server_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            _fileWriter = new StreamWriter(logFile, append: true) { AutoFlush = true };

            Info("=== 日志系统初始化 ===");
        }

        public static void Debug(string msg) => Log(LogLevel.Debug, msg);
        public static void Info(string msg) => Log(LogLevel.Info, msg);
        public static void Warning(string msg) => Log(LogLevel.Warning, msg);
        public static void Error(string msg) => Log(LogLevel.Error, msg);
        public static void Critical(string msg) => Log(LogLevel.Critical, msg);

        private static void Log(LogLevel level, string msg)
        {
            if (level < _minLevel) return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string levelStr = level switch
            {
                LogLevel.Debug => "DBG",
                LogLevel.Info => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "???"
            };

            string line = $"[{timestamp}] [{levelStr}] {msg}";

            // 控制台输出（带颜色）
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
            Console.WriteLine(line);
            Console.ForegroundColor = oldColor;

            // 文件输出
            lock (_lock)
            {
                _fileWriter?.WriteLine(line);
            }
        }

        public static void Shutdown()
        {
            Info("=== 日志系统关闭 ===");
            _fileWriter?.Close();
            _fileWriter = null;
        }
    }
}
