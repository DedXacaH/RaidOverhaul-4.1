using Microsoft.Extensions.Logging;
using Spectre.Console;
using SPTarkov.Common.Models.Logging;

namespace RaidOverhaulMain.Helpers;

public static class ROLogger
{
    private const string LogPrefix = "[Raid Overhaul] ";

    public static void Log<T>(ISptLogger<T> logger, string message, Color textColor = default)
    {
        logger.LogWithColor(LogPrefix + message, textColor);
    }

    public static void LogDebug<T>(ISptLogger<T> logger, string message)
    {
        if (logger.IsLogEnabled(LogLevel.Debug))
        {
            logger.Debug(LogPrefix + message);
        }
    }

    public static void LogInfo<T>(ISptLogger<T> logger, string message)
    {
        if (logger.IsLogEnabled(LogLevel.Information))
        {
            logger.Info(LogPrefix + message);
        }
    }

    public static void LogWarning<T>(ISptLogger<T> logger, string message)
    {
        if (logger.IsLogEnabled(LogLevel.Warning))
        {
            logger.Warning(LogPrefix + message);
        }
    }

    public static void LogError<T>(ISptLogger<T> logger, string message)
    {
        if (logger.IsLogEnabled(LogLevel.Error))
        {
            logger.Error(LogPrefix + message);
        }
    }

    public static void LogToServer<T>(ISptLogger<T> logger, string message, Color textColor = default)
    {
        logger.LogWithColor(LogPrefix + message, textColor);
    }
}
