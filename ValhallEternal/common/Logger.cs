using BepInEx.Logging;
using System;
using ValhallEternal.common;


namespace ValhallEternal
{
    internal class Logger
    {
        public static LogLevel Level = LogLevel.Info;

        public static void enableDebugLogging(object sender, EventArgs e)
        {
            if (ValConfig.EnableDebugMode.Value)
            {
                Level = LogLevel.Debug;
            }
            else
            {
                Level = LogLevel.Info;
            }
            // set log level
        }

        public static void CheckEnableDebugLogging()
        {
            if (ValConfig.EnableDebugMode.Value)
            {
                Level = LogLevel.Debug;
            }
            else
            {
                Level = LogLevel.Info;
            }
        }

        public static void LogDebug(string message)
        {
            if (Level >= LogLevel.Debug)
            {
                ValhallEternal.Log.LogInfo(message);
            }
        }
        public static void LogInfo(string message)
        {
            if (Level >= LogLevel.Info)
            {
                ValhallEternal.Log.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (Level >= LogLevel.Warning)
            {
                ValhallEternal.Log.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (Level >= LogLevel.Error)
            {
                ValhallEternal.Log.LogError(message);
            }
        }
    }
}
