using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Coffee.PackageManager
{
    public class DotNet
    {
        static ProcessStartInfo startInfo = System.Type.GetType("UnityEditor.Scripting.NetCoreProgram, UnityEditor")
                .GetMethod("CreateDotNetCoreStartInfoForArgs", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new[] { "" }) as ProcessStartInfo;

        public static void Restore(string proj, System.Action<bool> callback)
        {
            Execute(string.Format("restore {0}", proj), (success, stdout, stderr) =>
            {
                if (!success)
                    Debug.LogError(stderr);
                callback(success);
            });
        }

        public static void Run(string proj, string args, System.Action<bool, string> resultCallback)
        {
            var commandArgs = string.Format("run -p {0} -- {1}", proj, args);
            Execute(commandArgs, (success, stdout, stderr) =>
            {
                if (success)
                    resultCallback(success, stdout);
                else
                    RunWithRestore(proj, args, resultCallback);
            });
        }


        public static void RunWithRestore(string proj, string args, System.Action<bool, string> resultCallback)
        {
            Restore(proj, _ =>
            {
                var commandArgs = string.Format("run -p {0} -- {1}", proj, args);
                Execute(commandArgs, (success, stdout, stderr) =>
                {
                    if (!success)
                        Debug.LogError(stderr);
                    resultCallback(success, stdout);
                });
            });
        }

        public static string GetVersion()
        {
            string version = "";
            Execute("--version", (_, stdout, __) =>version = stdout, true);
            return version;
        }

        public static void Execute(string args, System.Action<bool, string, string> resultCallback = null, bool wait = false)
        {
            startInfo.Arguments = args;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var p = System.Diagnostics.Process.Start(startInfo);
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

            if(wait)
                p.WaitForExit(1000 * 10);
        }
    }

    public class BridgeCompiler : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            Regex regIntrenalBridge = new Regex("^Packages/com.coffee.upm-git-extension/Editor/InternalBridge/[^/]+.cs$", RegexOptions.Compiled);
            var needToCompile = importedAssets
                .Union(deletedAssets)
                .Union(movedAssets)
                .Union(movedFromAssetPaths)
                .Any(x=>regIntrenalBridge.IsMatch(x));
            
            if(needToCompile)
            {
                CompileBridge();
            }
        }

        // static ProcessStartInfo startInfo = System.Type.GetType("UnityEditor.Scripting.NetCoreProgram, UnityEditor")
        //         .GetMethod("CreateDotNetCoreStartInfoForArgs", BindingFlags.Static | BindingFlags.NonPublic)
        //         .Invoke(null, new[] { "" }) as ProcessStartInfo;
        static MethodInfo miSyncSolution = System.Type.GetType("UnityEditor.SyncVS, UnityEditor")
                .GetMethod("SyncSolution", BindingFlags.Static | BindingFlags.Public);

        static void CompileCsproj(string proj, string dll, bool withRestore = false)
        {
            var compiler = "Packages/com.coffee.upm-git-extension/Editor/InternalBridge/InternalAccessableCompiler~/InternalAccessableCompiler.csproj";
            var outputDll = Path.GetFileName(dll);
            var args = string.Format("{0} {1}", proj, dll);
            Debug.LogFormat("Start compile {0}", proj);

            DotNet.Run(compiler, args, (success, stdout) =>
            {
                Debug.Log("Compile Complete!");
            });
        }


        [MenuItem("Assets/Compile Bridge")]
        static void CompileBridge()
        {
            miSyncSolution.Invoke(null, new object[0]);

            CompileCsproj(
#if UNITY_2019_3_OR_NEWER
                "Unity.PackageManagerCaptain.Editor.csproj",
#else
                "Unity.PackageManagerCaptain.Editor.csproj",
#endif

#if UNITY_2019_3_OR_NEWER
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.Bridge.2019.3.dll"
#elif UNITY_2019_2_OR_NEWER
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.Bridge.2019.2.dll"
#elif UNITY_2019_1_OR_NEWER
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.Bridge.2019.1.dll"
#else
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.Bridge.2018.3.dll"
#endif
            );
        }

        [MenuItem("Assets/Switch Bridge Mode")]
        static void SwitchBridgeMode()
        {
            // SwitchBridgeMode(AssetDatabase.GUIDToAssetPath("6d31ece5aaa0f4bdfa2eb458573a31af"));
        }

        static void SwitchBridgeMode(string asmdefPath)
        {
            var disabledAsmdef = asmdefPath + "~";
            var swapedAsmdef = asmdefPath + "~~";

            FileUtil.MoveFileOrDirectory(asmdefPath, swapedAsmdef);
            FileUtil.MoveFileOrDirectory(disabledAsmdef, asmdefPath);
            FileUtil.MoveFileOrDirectory(swapedAsmdef, disabledAsmdef);

            AssetDatabase.ImportAsset(asmdefPath);
        }
    }
}