// -----------------------------------------------------------------------
// <copyright file="Logger.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2014 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.Openshift.Runtime
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using NLog;
    using NLog.Targets;
    using NLog.Targets.Wrappers;
    using Uhuru.Openshift.Runtime.Config;
    using System.IO;
    using NLog.Config;
    using System;

    /// <summary>
    /// This is a helper logger class that is used throughout the code.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// The NLog.Logger object used for logging.
        /// </summary>
        private static readonly NLog.Logger log = LogManager.GetLogger("openshift.winnode");
        private static bool configured = false;
        private static readonly object configureLock = new object();
        private static string logFile = null;
        private static LogLevel nlogLevel = LogLevel.Off;

        public static string LogFile
        {
            get
            {
                if (logFile != null)
                {
                    return logFile;
                }
                else if (Environment.GetEnvironmentVariable("OPENSHIFT_HOMEDIR") != null)
                {
                    return Path.Combine(Environment.GetEnvironmentVariable("OPENSHIFT_HOMEDIR"), @"log\platform.log");
                }
                else
                {
                    return NodeConfig.Values["PLATFORM_LOG_FILE"];
                }
            }
            set
            {
                logFile = value;
            }
        }

        public static LogLevel NLogLevel
        {
            get
            {
                if (nlogLevel == LogLevel.Off)
                {
                    string logLevel = NodeConfig.Values["PLATFORM_LOG_LEVEL"];

                    switch (logLevel)
                    {
                        case "TRACE": return LogLevel.Trace;
                        case "WARN": return LogLevel.Warn;
                        case "ERROR": return LogLevel.Error;
                        case "FATAL": return LogLevel.Fatal;
                        case "INFO": return LogLevel.Info;
                        default: return LogLevel.Debug;
                    }
                }
                else
                {
                    return nlogLevel;
                }
            }
            set
            {
                nlogLevel = value;
            }
        }

        private static void Configure()
        {
            if (configured)
            {
                return;
            }

            lock (configureLock)
            {
                if (configured)
                {
                    return;
                }

                string directory = Path.GetDirectoryName(LogFile);
                Directory.CreateDirectory(directory);

                FileTarget target = new FileTarget();
                target.FileName = LogFile;
                target.ArchiveNumbering = ArchiveNumberingMode.Rolling;
                target.ArchiveEvery = FileArchivePeriod.None;
                target.ArchiveAboveSize = 10485760;

                AsyncTargetWrapper wrapper = new AsyncTargetWrapper(target, 5000, AsyncTargetWrapperOverflowAction.Discard);

                LogManager.Configuration = new NLog.Config.LoggingConfiguration();
                LogManager.Configuration.AddTarget("file", wrapper);

                LoggingRule fileRule = new LoggingRule("*", NLogLevel, wrapper);
                LogManager.Configuration.LoggingRules.Add(fileRule);

                LogManager.ReconfigExistingLoggers();

                configured = true;
            }
        }

        /// <summary>
        /// Logs a fatal message.
        /// This indicates a really severe error, that will probably make the application crash.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Fatal(string message)
        {
            Configure();
            log.Fatal(message);
        }

        /// <summary>
        /// Logs an error message.
        /// This indicates an error, but the application may be able to continue.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Error(string message)
        {
            Configure();
            log.Error(message);
        }

        /// <summary>
        /// Logs a warning message.
        /// This indicates a situation that could lead to some bad things.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Warning(string message)
        {
            Configure();
            log.Warn(message);
        }

        /// <summary>
        /// Logs an information message.
        /// The message is used to indicate some progress.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Info(string message)
        {
            Configure();
            log.Info(message);
        }

        /// <summary>
        /// Logs a debug message.
        /// This is an informational message, that is useful when debugging.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Debug(string message)
        {
            Configure();
            log.Debug(message);
        }

        /// <summary>
        /// Logs a fatal message and formats it.
        /// This indicates a really severe error, that will probably make the application crash.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Fatal(string message, params object[] args)
        {
            Configure();
            log.Fatal(CultureInfo.InvariantCulture, message, args);
        }

        /// <summary>
        /// Logs an error message and formats it.
        /// This indicates an error, but the application may be able to continue.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Error(string message, params object[] args)
        {
            Configure();
            log.Error(CultureInfo.InvariantCulture, message, args);
        }

        /// <summary>
        /// Logs a warning message and formats it.
        /// This indicates a situation that could lead to some bad things.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Warning(string message, params object[] args)
        {
            Configure();
            log.Warn(CultureInfo.InvariantCulture, message, args);
        }

        /// <summary>
        /// Logs an information message and formats it.
        /// The message is used to indicate some progress.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Info(string message, params object[] args)
        {
            Configure();
            log.Info(CultureInfo.InvariantCulture, message, args);
        }

        /// <summary>
        /// Logs a debug message and formats it.
        /// This is an informational message, that is useful when debugging.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Debug(string message, params object[] args)
        {
            Configure();
            log.Debug(CultureInfo.InvariantCulture, message, args);
        }
    }
}
