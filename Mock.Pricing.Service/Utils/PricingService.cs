using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using NLog;

namespace Mock.Pricing.Service.Utils
{
    /// <summary>
    ///     Implements the Windows service hosting the WebAPI application.
    /// </summary>
    public class PricingService
    {
        #region ----------------------- Constructors and Destructor -------------------------------

        public PricingService(Configuration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _config = config;
            _syncServiceThreadAccess = new object();
            _syncServiceHostAccess = new object();
        }

        #endregion

        #region ----------------------- Properties ------------------------------------------------

        /// <summary>
        ///     Used to log messages.
        /// </summary>
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     The <see cref="Configuration" /> containing the application's settings.
        /// </summary>
        private readonly Configuration _config;

        /// <summary>
        ///     Used to synchronize access to the service thread (e.g. stopping and starting).
        /// </summary>
        private readonly object _syncServiceThreadAccess;

        /// <summary>
        ///     Used to synchronize access to the service host (e.g. when starting and shutting down).
        /// </summary>
        private readonly object _syncServiceHostAccess;

        /// <summary>
        ///     The service host hosting the WebAPI container.
        /// </summary>
        private IDisposable _serviceHost;

        /// <summary>
        ///     The <see cref="CancellationTokenSource" /> used to signal the service loop to cease service execution.
        /// </summary>
        private CancellationTokenSource _cancelTokenSrc;

        /// <summary>
        ///     The background <see cref="Thread" /> executing health checks to determine unhealthy component state and tries to
        ///     recover if unhealthy state detected.
        /// </summary>
        private Thread _serviceCheckThread;

        /// <summary>
        ///     The <see cref="Task" /> used to reload the application's <see cref="Configuration" /> from an external source.
        /// </summary>
        private Task _reloadConfigurationTask;

        /// <summary>
        ///     The <see cref="Task" /> used to reload the application's <see cref="Configuration" /> from an external source.
        /// </summary>
        private Task _reloadCalendarTask;

        #endregion

        #region ----------------------- Public Methods --------------------------------------------

        /// <summary>
        ///     Called when the service should start.
        /// </summary>
        public void OnStart()
        {
            var hasLock = false;
            try
            {
                Monitor.Enter(_syncServiceThreadAccess, ref hasLock);
                if (_serviceCheckThread != null)
                {
                    Logger.Info("Service thread already started - skipping further action.");
                    return;
                }
                Logger.Info("Starting service thread.");
                _cancelTokenSrc = new CancellationTokenSource();
                Initialize(_cancelTokenSrc.Token);
                _serviceCheckThread = new Thread(o => ServiceCheck(_cancelTokenSrc.Token))
                {
                    IsBackground = false
                    // make sure to be background thread in order to prevent this thread from running after other threads have been terminated.
                };
                _serviceCheckThread.Start();
                Logger.Info("Starting service thread finished.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while starting service thread: {ex}.");
                Uninitialize();
                throw;
            }
            finally
            {
                if (hasLock)
                    Monitor.Exit(_syncServiceThreadAccess);
            }
        }


        /// <summary>
        ///     Called when the service should stop.
        /// </summary>
        public void OnStop()
        {
            var hasLock = false;
            try
            {
                Monitor.Enter(_syncServiceThreadAccess, ref hasLock);
                if (_serviceCheckThread == null)
                    return;
                Logger.Info("Stopping service thread.");
                _cancelTokenSrc.Cancel(false);
                _serviceCheckThread.Interrupt(); // wake up thread if sleeping
                var state = _serviceCheckThread.ThreadState;
                if ((!state.HasFlag(ThreadState.Running) &&
                     !state.HasFlag(ThreadState.SuspendRequested) &&
                     !state.HasFlag(ThreadState.Suspended) &&
                     !state.HasFlag(ThreadState.WaitSleepJoin))
                    || _serviceCheckThread.Join(_config.ServiceTimeOutBeforeForcefulServiceStop))
                    Logger.Info("Service thread stopped successfully.");
                else
                {
                    Logger.Info("Sending abort signal to service thread.");
                    _serviceCheckThread.Interrupt(); // wake up thread if sleeping
                    _serviceCheckThread.Abort();
                    if (_serviceCheckThread.Join(_config.ServiceAbortWaitTime))
                        Logger.Info("Service thread aborted successfully.");
                    else
                        Logger.Warn(
                            $"Service thread did not respond to forceful abort request in time span '{_config.ServiceAbortWaitTime}'.");
                }
                Logger.Info("Stopping service thread finished successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while trying to abort service thread: {ex}.");
            }
            finally
            {
                _serviceCheckThread = null;
                _cancelTokenSrc = null;
                Uninitialize();
                if (hasLock)
                    Monitor.Exit(_syncServiceThreadAccess);
            }
        }

        #endregion

        #region ----------------------- Private Methods -------------------------------------------

        private void ServiceCheck(CancellationToken cancelToken)
        {
            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    try
                    {
                        // check for new holiday calendar update
                        EnsureServiceHostIsRunning();
                        try
                        {
                            // wait before next run
                            Thread.Sleep(TimeSpan.FromSeconds(5));
                        }
                        catch (ThreadInterruptedException)
                        {
                            // do nothing here and continue
                        }
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Unexpected error while executing service loop: {ex}.";
                        Logger.Error(msg);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Logger.Info(@"Service loop abort requested.");
            }
            catch (Exception ex)
            {
                var msg = $"Error while executing service loop: {ex}.";
                Logger.Error(msg);
                throw new Exception($"Unexpected error while trying to execute service '{_config.ServiceName}'.", ex);
            }
        }


        private void Initialize(CancellationToken cancelToken)
        {
            Logger.Info("Initializing service environment.");
            if (cancelToken.IsCancellationRequested)
                return;
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            EnsureServiceHostIsRunning();
        }


        private void Uninitialize()
        {
            Logger.Info("Uninitializing service environment.");
            ShutDownServiceHost();
        }


        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain,
            SslPolicyErrors error)
        {
            // return true to indicate that the certificate is always trusted
            return true;
        }


        private void EnsureServiceHostIsRunning()
        {
            var hasLock = false;
            try
            {
                Monitor.Enter(_syncServiceHostAccess, ref hasLock);
                if (_serviceHost != null)
                    return;
                var options = new StartOptions();
                // register '*' weak prefix wild card instead of strong '+' - see https://msdn.microsoft.com/en-us/library/aa364698(v=vs.85).aspx
                options.Urls.Add($"http://*:{_config.ServiceHttpPort}/{_config.ServiceBaseUrl}");
                var urls = string.Join(", ", options.Urls.Select(url => $"'{url}'"));
                Logger.Info($"Starting WebAPI service host listening on URLS: {urls}.");
                _serviceHost = WebApp.Start<Startup>(options);
                Logger.Info("WebAPI service host started successfully.");
            }
            catch (Exception ex)
            {
                var msg = $"Error while starting WebAPI service host: {ex}.";
                Console.WriteLine(msg);
                Logger.Error(msg);
                try
                {
                    _serviceHost?.Dispose();
                }
                catch (Exception dispEx)
                {
                    Logger.Error($"Error while disposing WebAPI service host: '{dispEx}'.");
                }
                _serviceHost = null;
            }
            finally
            {
                if (hasLock)
                    Monitor.Exit(_syncServiceHostAccess);
            }
        }


        private void ShutDownServiceHost()
        {
            var hasLock = false;
            try
            {
                Monitor.Enter(_syncServiceHostAccess, ref hasLock);
                if (_serviceHost == null)
                    return;
                Logger.Info("Shutting down WebAPI service host.");
                _serviceHost.Dispose();
                Logger.Info("WebAPI service host shut down successfully.");
            }
            catch (Exception ex)
            {
                var msg = $"Error while shutting down WebAPI service host: {ex}.";
                Logger.Error(msg);
            }
            finally
            {
                _serviceHost = null;
                if (hasLock)
                    Monitor.Exit(_syncServiceHostAccess);
            }
        }


        private void HandleError(string errorMsg, params object[] formatItems)
        {
            Logger.Error(errorMsg, formatItems);
        }

        #endregion
    }
}