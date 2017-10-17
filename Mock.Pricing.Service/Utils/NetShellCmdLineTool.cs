using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;

namespace Mock.Pricing.Service.Utils
{
    /// <summary>
    ///     Encapsulates functionality of the command line tool 'netsh.exe' used for managing
    ///     URL access of applications, users and groups and TLS/SSL certificate bindings of applications.
    /// </summary>
    public static class NetShellCmdLineTool
    {
        #region ----------------------- Properties ------------------------------------------------

        /// <summary>
        ///     The <see cref="ILogger" /> used to log messages.
        /// </summary>
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        // the command line tool name and arguments
        private const string ArgHttp = "http";
        private const string ArgHttps = "https";
        private const string ArgUrlAcl = "urlacl";
        private const string ArgUrlEquals = "url=";

        #endregion

        #region ----------------------- Public Methods --------------------------------------------

        /// <summary>
        ///     Invokes NETSH to check and add the URL registration for this application.
        /// </summary>
        public static void RegisterHttpNamespace(Configuration configuration)
        {
            foreach (var portExtensionKvp in new[]
            {
                new Tuple<string, int>(ArgHttp, configuration.ServiceHttpPort),
                new Tuple<string, int>(ArgHttps, configuration.ServiceHttpsPort)
            })
            {
                var url = $"{portExtensionKvp.Item1}://*:{portExtensionKvp.Item2}/{configuration.ServiceBaseUrl}/";
                var userOrGroupName = configuration.ServiceGroupName;
                if (IsUrlAclRegistered(url, userOrGroupName))
                {
                    Logger.Info("URL '{0}' namespace access for user or group '{1}' is already registered.", url,
                        userOrGroupName);
                    continue;
                }
                Logger.Info("Registering URL '{0}' namespace access for user or group '{1}'.", url, userOrGroupName);
                // register '*' weak prefix wild card instead of strong '+' - see https://msdn.microsoft.com/en-us/library/aa364698(v=vs.85).aspx
                var args = $"{ArgHttp} add {ArgUrlAcl} {ArgUrlEquals}{url} user={userOrGroupName}";
                var res = ExecuteNetsh(args);
                if (res.Item1 != 0)
                    Logger.Error(
                        "Error while trying to register URL '{0}' namespace access for user or group '{1}' with arguments '{2}':{3}",
                        url, userOrGroupName, args, res.Item2);
                else
                    Logger.Info("Successfully registered URL '{0}' namespace access for user or group '{1}'.", url,
                        userOrGroupName);
            }
        }


        /// <summary>
        ///     Invokes NETSH to remove the URL registration for this application.
        /// </summary>
        public static void UnregisterHttpNamespace(Configuration configuration)
        {
            foreach (var portExtensionKvp in new[]
            {
                new Tuple<string, int>(ArgHttp, configuration.ServiceHttpPort),
                new Tuple<string, int>(ArgHttps, configuration.ServiceHttpsPort)
            })
            {
                var url = $"{portExtensionKvp.Item1}://*:{portExtensionKvp.Item2}/{configuration.ServiceBaseUrl}/";
                var userOrGroupName = configuration.ServiceGroupName;
                if (!IsUrlAclRegistered(url, userOrGroupName))
                {
                    Logger.Info("URL '{0}' namespace access for user or group '{1}' is already unregistered.", url,
                        userOrGroupName);
                    continue;
                }
                Logger.Info("Unregistering URL '{0}' namespace access for user or group '{1}'.", url, userOrGroupName);
                // register '*' weak prefix wild card instead of strong '+' - see https://msdn.microsoft.com/en-us/library/aa364698(v=vs.85).aspx
                var args = $"{ArgHttp} delete {ArgUrlAcl} {ArgUrlEquals}{url} user={userOrGroupName}";
                var res = ExecuteNetsh(args);
                if (res.Item1 != 0)
                    Logger.Error(
                        "Error while trying to unregister URL '{0}' namespace access for user or group '{1}' with arguments '{2}':{3}",
                        url, userOrGroupName, args, res.Item2);
                else
                    Logger.Info("Successfully unregistered URL '{0}' namespace access for user or group '{1}'.", url,
                        userOrGroupName);
            }
        }

        #endregion

        #region ----------------------- Private Methods -------------------------------------------

        private static bool IsUrlAclRegistered(string url, string userOrGroupName)
        {
            var res = ExecuteNetsh($"{ArgHttp} show {ArgUrlAcl} {ArgUrlEquals}{url}");
            if (res.Item1 != 0)
            {
                Logger.Error("Error while trying to determine registration status of URL '{0}':{1}", url, res.Item2);
                return false;
            }
            var outputLines = res.Item2.Split(new[] {Environment.NewLine, "\r\n", "\n"},
                StringSplitOptions.RemoveEmptyEntries);
            // an existing URL registration is recognized by having
            // - one line with content: *Reserved URL*: <URL>
            // - one line with content: *User: <USER or GROUP>
            // - one line with content: *Listen: Yes
            var requiredLineMatches = new[]
            {
                $".*Reserved URL.*:.*{Regex.Escape(url)}.*",
                $".*User.*:.*{Regex.Escape(userOrGroupName)}.*",
                ".*Listen:.*Yes.*"
            };
            return
                requiredLineMatches.All(
                    searchStr => outputLines.Any(line => Regex.IsMatch(line, searchStr, RegexOptions.IgnoreCase)));
        }


        private static Tuple<int, string> ExecuteNetsh(string args)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "netsh.exe";
                proc.StartInfo.Arguments = args;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                proc.WaitForExit();
                return new Tuple<int, string>(proc.ExitCode, proc.StandardOutput.ReadToEnd());
            }
        }

        #endregion
    }
}