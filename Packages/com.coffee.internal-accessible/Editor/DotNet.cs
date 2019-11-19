using System.Reflection;
using System.Diagnostics;
using System.IO;

namespace Coffee.InternalAccessible
{
    public class DotNet
    {
        static ProcessStartInfo startInfo = System.Type
                .GetType("UnityEditor.Scripting.NetCoreProgram, UnityEditor")
                .GetMethod("CreateDotNetCoreStartInfoForArgs", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new[] { "" }) as ProcessStartInfo;

        public static void Restore(string proj, System.Action<bool> callback)
        {
            proj = proj.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
            Execute(string.Format("restore {0}", proj), (success, stdout, stderr) =>
            {
                if (!success)
                    UnityEngine.Debug.LogError(stderr);
                callback(success);
            });
        }

        public static void Run(string proj, string args, System.Action<bool, string> resultCallback)
        {
            proj = proj.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
            var commandArgs = string.Format("run -p {0} -- {1}", proj, args);
            Execute(commandArgs, (success, stdout, stderr) =>
            {
                if (success)
                    resultCallback(success, stdout);
                else
                    RunWithRestore(proj, commandArgs, resultCallback);
            });
        }

        static void RunWithRestore(string proj, string commandArgs, System.Action<bool, string> resultCallback)
        {
            Restore(proj, restoreSuccess =>
            {
                if (!restoreSuccess)
                    return;

                Execute(commandArgs, (success, stdout, stderr) =>
                {
                    if (!success)
                        UnityEngine.Debug.LogError(stderr);
                    resultCallback(success, stdout);
                });
            });
        }

        public static string GetVersion()
        {
            string version = "";
            Execute("--version", (_, stdout, __) => version = stdout, true);
            return version;
        }

        public static void Execute(string args, System.Action<bool, string, string> resultCallback = null, bool wait = false)
        {
            startInfo.Arguments = args;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var p = Process.Start(startInfo);
            if (p == null || p.Id == 0 || p.HasExited)
            {
                resultCallback(false, "", "");
                return;
            }

            p.Exited += (_, __) =>
            {
                resultCallback(p.ExitCode == 0, p.StandardOutput.ReadToEnd(), p.StandardError.ReadToEnd());
                p.Dispose();
            };
            p.EnableRaisingEvents = true;

            if (wait)
                p.WaitForExit(1000 * 10);
        }
    }
}