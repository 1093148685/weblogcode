using System.Collections.Concurrent;
using System.Text;

namespace Weblog.Core.Api.Logging;

public sealed class DailyFileLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DailyFileLogger> _loggers = new();
    private readonly DailyFileLoggerOptions _options;
    private readonly object _writeLock = new();

    public DailyFileLoggerProvider(IConfiguration configuration)
    {
        _options = configuration.GetSection("Logging:File").Get<DailyFileLoggerOptions>() ?? new DailyFileLoggerOptions();
        if (string.IsNullOrWhiteSpace(_options.Path))
        {
            _options.Path = "logs";
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DailyFileLogger(name, _options, _writeLock));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

public sealed class DailyFileLoggerOptions
{
    public string Path { get; set; } = "logs";

    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public int RetainedDays { get; set; } = 14;
}

internal sealed class DailyFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly DailyFileLoggerOptions _options;
    private readonly object _writeLock;
    private DateOnly _lastCleanupDate = DateOnly.MinValue;

    public DailyFileLogger(string categoryName, DailyFileLoggerOptions options, object writeLock)
    {
        _categoryName = categoryName;
        _options = options;
        _writeLock = writeLock;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= _options.MinimumLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            var now = DateTimeOffset.Now;
            var logDir = Path.IsPathRooted(_options.Path)
                ? _options.Path
                : Path.Combine(AppContext.BaseDirectory, _options.Path);

            Directory.CreateDirectory(logDir);
            CleanupOldFiles(logDir, now);

            var filePath = Path.Combine(logDir, $"weblog-{now:yyyyMMdd}.log");
            var message = formatter(state, exception);

            var builder = new StringBuilder()
                .Append('[').Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")).Append("] ")
                .Append(logLevel).Append(' ')
                .Append(_categoryName);

            if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
            {
                builder.Append(" [").Append(eventId.Id);
                if (!string.IsNullOrWhiteSpace(eventId.Name))
                {
                    builder.Append(':').Append(eventId.Name);
                }
                builder.Append(']');
            }

            builder.AppendLine()
                .AppendLine(message);

            if (exception != null)
            {
                builder.AppendLine(exception.ToString());
            }

            builder.AppendLine();

            lock (_writeLock)
            {
                File.AppendAllText(filePath, builder.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never interrupt the application.
        }
    }

    private void CleanupOldFiles(string logDir, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.DateTime);
        if (_lastCleanupDate == today || _options.RetainedDays <= 0)
        {
            return;
        }

        _lastCleanupDate = today;
        var cutoff = now.AddDays(-_options.RetainedDays);
        foreach (var file in Directory.EnumerateFiles(logDir, "weblog-*.log"))
        {
            try
            {
                if (File.GetLastWriteTime(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }
    }
}
