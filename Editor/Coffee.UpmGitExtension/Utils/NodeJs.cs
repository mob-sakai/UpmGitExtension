using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Utils;
using Debug = UnityEngine.Debug;

namespace Coffee.UpmGitExtension
{
    internal static class NodeJs
    {
        public static void Run(string workingDir, string js, string argument = null, Action<int> callback = null)
        {
#if UNITY_EDITOR_WIN
            var node = Path.Combine(EditorApplication.applicationContentsPath, "Tools/nodejs/node.exe").Replace('/', '\\');
#else
            var node = Path.Combine(EditorApplication.applicationContentsPath, "Tools/nodejs/bin/node");
#endif

            var program = new Program(new ProcessStartInfo()
            {
                Arguments = $"\"{Path.GetFullPath(js)}\" {argument}",
                CreateNoWindow = true,
                FileName = node,
                WorkingDirectory = workingDir,
            });

            program.Start((_, __) =>
            {
                var exitCode = program._process.ExitCode;
                if (exitCode != 0)
                {
                    Debug.LogError(program.GetAllOutput());
                }

                EditorApplication.delayCall += () => callback?.Invoke(exitCode);
            });
        }
    }
}