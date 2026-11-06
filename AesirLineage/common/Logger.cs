using BepInEx.Logging;
using System;
using AesirLineage.common;


namespace AesirLineage
{
    internal class Logger
    {
        public static LogLevel Level = LogLevel.Info;

        public static void enableDebugLogging(object sender, EventArgs e)
        {
            CheckEnableDebugLogging();
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
                AesirLineage.Log.LogInfo(message);
            }
        }
        public static void LogInfo(string message)
        {
            if (Level >= LogLevel.Info)
            {
                AesirLineage.Log.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (Level >= LogLevel.Warning)
            {
                AesirLineage.Log.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (Level >= LogLevel.Error)
            {
                AesirLineage.Log.LogError(message);
            }
        }
    }
}
