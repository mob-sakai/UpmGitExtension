using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Coffee.UpmGitExtension
{
    internal static class NodeJs
    {
        public static void Run(string workingDir, string js, string argument = null, Action<int> callback = null)
        {
            ExecuteCommand(workingDir, GetExe(), $"\"{Path.GetFullPath(js)}\" {argument}", callback);
        }

        public static string GetExe()
        {
            var node = GetBuiltInExePath();
            if (File.Exists(node)) return node;

            node = InstallNodeJs("6.10.0");
            if (File.Exists(node)) return node;

            throw new FileNotFoundException($"nodejs is not found");
        }

        private static Program ExecuteCommand(string cwd, string filename, string args, Action<int> callback = null)
        {
            var program = new Program(new ProcessStartInfo
            {
                Arguments = args,
                CreateNoWindow = true,
                FileName = filename,
                WorkingDirectory = cwd
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

            return program;
        }

        private static string InstallNodeJs(string version)
        {
            var installPath = InternalEditorUtility.unityPreferencesFolder + "/GitPackageDatabase/nodejs";
            var url = GetDownloadUrl(version);
            var exePath = $"{installPath}/{GetExePath(version)}"
                .Replace("/", Path.DirectorySeparatorChar.ToString());
            if (File.Exists(exePath)) return exePath;

            try
            {
                Debug.Log($"Install nodejs {version} from {url}");
                EditorUtility.DisplayProgressBar("Package Installer", $"Download nodejs {version} from {url}", 0.5f);
                var downloadPath = DownloadFile(url);
                EditorUtility.DisplayProgressBar("Package Installer", $"Extract to {installPath}", 0.7f);
                ExtractArchive(downloadPath, installPath);

                Debug.LogFormat($"nodejs {version} has been installed at {installPath}.");
            }
            catch
            {
                throw new Exception($"nodejs {version} installation failed.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (File.Exists(exePath)) return exePath;

            throw new FileNotFoundException($"nodejs {version} is not found at {exePath}");
        }

        private static string GetDownloadUrl(string version)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return $"https://nodejs.org/download/release/v{version}/node-v{version}-win-x64.7z";
                case RuntimePlatform.OSXEditor:
                    return $"https://nodejs.org/download/release/v{version}/node-v{version}-darwin-x64.tar.gz";
                case RuntimePlatform.LinuxEditor:
                    return $"https://nodejs.org/download/release/v{version}/node-v{version}-linux-x64.tar.gz";

                default:
                    throw new NotSupportedException($"{Application.platform} is not supported");
            }
        }

        private static string GetBuiltInExePath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return $"{EditorApplication.applicationContentsPath}/Tools/nodejs/node.exe"
                        .Replace("/", Path.DirectorySeparatorChar.ToString());
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return $"{EditorApplication.applicationContentsPath}/Tools/nodejs/bin/node";
                default:
                    throw new NotSupportedException($"{Application.platform} is not supported");
            }
        }

        private static string GetExePath(string version)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return $"node-v{version}-win-x64/node.exe";
                case RuntimePlatform.OSXEditor:
                    return $"node-v{version}-darwin-x64/bin/node";
                case RuntimePlatform.LinuxEditor:
                    return $"node-v{version}-linux-x64/bin/node";

                default:
                    throw new NotSupportedException($"{Application.platform} is not supported");
            }
        }

        private static string DownloadFile(string url)
        {
            var dir = "Temp/DownloadedFiles";
            var downloadPath = $"{dir}/{Path.GetFileName(url)}";

            // Clear cache.
            if (File.Exists(downloadPath))
            {
                File.Delete(downloadPath);
            }

            var cb = ServicePointManager.ServerCertificateValidationCallback;
            try
            {
                Directory.CreateDirectory(dir);

                Debug.Log($"Download {url} with WebClient");
                ServicePointManager.ServerCertificateValidationCallback = (_, __, ___, ____) => true;
                using (var client = new WebClient())
                {
                    client.DownloadFile(url, downloadPath);
                }

                ServicePointManager.ServerCertificateValidationCallback = cb;
            }
            catch
            {
                ServicePointManager.ServerCertificateValidationCallback = cb;

                // NOTE: In .Net Framework 3.5, TSL1.2 is not supported.
                // So, download the file on command line instead.
                Debug.Log($"Download {url} (alternative)");
                var args = GetDownloadCommand(url, downloadPath, Application.platform);
                ExecuteCommand(Directory.GetCurrentDirectory(), args[0], args[1]).WaitForExit();
            }

            return downloadPath;
        }

        private static string[] GetDownloadCommand(string url, string downloadPath, RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return new[] { "PowerShell.exe", $"curl -O {downloadPath} {url}" };
                case RuntimePlatform.OSXEditor:
                    return new[] { "curl", $"-o {downloadPath} -L {url}" };
                case RuntimePlatform.LinuxEditor:
                    return new[] { "wget", $"-O {downloadPath} {url}" };

                default:
                    throw new NotSupportedException($"{Application.platform} is not supported");
            }
        }

        private static void ExtractArchive(string archivePath, string extractTo)
        {
            Debug.Log($"Extract archive {archivePath} to {extractTo}");
            var args = GetExtractArchiveCommand(archivePath, extractTo, Application.platform);
            ExecuteCommand(Directory.GetCurrentDirectory(), args[0], args[1]).WaitForExit();
        }

        private static string[] GetExtractArchiveCommand(string archivePath, string extractTo, RuntimePlatform platform)
        {
            var contentsPath = EditorApplication.applicationContentsPath;
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                    Directory.CreateDirectory(Path.GetDirectoryName(extractTo));
                    return new[] { Path.Combine(contentsPath, "Tools", "7z.exe"), $"x {archivePath} -o{extractTo}" };
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    if (archivePath.EndsWith("tar.gz"))
                    {
                        Directory.CreateDirectory(extractTo);
                        return new[] { "tar", $"-pzxf {archivePath} -C {extractTo}" };
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(extractTo));
                    return new[] { Path.Combine(contentsPath, "Tools", "7za"), $"x {archivePath} -o{extractTo}" };
                default:
                    throw new NotSupportedException($"{Application.platform} is not supported");
            }
        }
    }
}
