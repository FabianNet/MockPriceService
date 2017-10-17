using System;
using NLog;

namespace Mock.Pricing.Service.Utils
{
    /// <summary>
    ///     Contains thew application's configuration settings.
    /// </summary>
    public sealed class Configuration
    {
        #region ----------------------- Constructors / Destructor ---------------------------------

        /// <summary>
        ///     Initializes a new instance of the <see cref="Configuration" /> class.
        /// </summary>
        /// <param name="executionEnvironment">
        ///     The <see cref="ExecutionEnvironment" /> this application is
        ///     started with.
        /// </param>
        /// <exception cref="ArgumentException">Thrown, if <paramref name="executionEnvironment" /> is not supported.</exception>
        public Configuration(ExecutionEnvironment executionEnvironment)
        {
            ExecutionEnvironment = executionEnvironment;

            ServiceLoopWaitTime = TimeSpan.FromSeconds(5);
            ServiceTimeOutBeforeForcefulServiceStop = TimeSpan.FromSeconds(20);
            ServiceAbortWaitTime = TimeSpan.FromSeconds(10);
            ServiceName = "MockPricingService";
            ServiceDisplayName = "Mock Pricing Service";
            ServiceUserName = @"x\xxx";
            ServiceUserPasswordEncrypted = "xxx";
            ServiceHttpPort = 51010;
            ServiceHttpsPort = 51011;
            ServiceBaseUrl = "";
            ServiceGroupName = @"x\xxx2";

            LoggingLevel = LogLevel.Debug;
        }

        #endregion

        #region ----------------------- Properties ------------------------------------------------

        /// <summary>
        ///     The <see cref="Components.Base.Utils.ExecutionEnvironment" /> in which this service will be executed.
        ///     Depending on the value of this property some values will vary (e.g. <see cref="MadBaseDbConnectionString" />).
        /// </summary>
        public readonly ExecutionEnvironment ExecutionEnvironment;

        /// <summary>
        ///     The time span to wait before the next service loop call will be executed.
        /// </summary>
        public TimeSpan ServiceLoopWaitTime { get; }

        /// <summary>
        ///     The time span to wait before the service is forcefully shut down (so called grace period).
        /// </summary>
        public TimeSpan ServiceTimeOutBeforeForcefulServiceStop { get; }

        /// <summary>
        ///     The time span to wait before the forceful service thread abortion is considered to be timed out..
        /// </summary>
        public TimeSpan ServiceAbortWaitTime { get; }

        /// <summary>
        ///     The unique name used to identify the service in commands like sc.exe or in Service Control Manager.
        ///     This name must not contain any white spaces.
        /// </summary>
        public string ServiceName { get; }

        /// <summary>
        ///     A unique and human readable name being displayed in the Service Control Manager to identify this service.
        ///     This name may contain white spaces.
        /// </summary>
        public string ServiceDisplayName { get; }

        /// <summary>
        ///     The encrypted password of the service user account under which the service will be running.
        /// </summary>
        public string ServiceUserPasswordEncrypted { get; }

        /// <summary>
        ///     The fully qualified user name (e.g. 'GROUP\R555555') under which this service will be executed.
        /// </summary>
        public string ServiceUserName { get; }

        /// <summary>
        ///     The port used by this service to listen for incoming HTTP connections.
        /// </summary>
        public int ServiceHttpPort { get; }

        /// <summary>
        ///     The port used by this service to listen for incoming HTTPS connections.
        /// </summary>
        public int ServiceHttpsPort { get; }

        /// <summary>
        ///     The base URL of this service.
        /// </summary>
        public string ServiceBaseUrl { get; }

        /// <summary>
        ///     The <see cref="LogLevel" /> used to log messages.
        /// </summary>
        public LogLevel LoggingLevel { get; }

        public string ServiceGroupName { get; private set; }

        #endregion
    }
}