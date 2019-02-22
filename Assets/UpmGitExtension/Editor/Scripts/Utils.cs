#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using System.Text.RegularExpressions;
using UnityEngine;
using System;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using Markdig;

namespace Coffee.PackageManager
{
	internal class PackageJsonHelper
	{
		[SerializeField]
		private string name = string.Empty;

		public static string GetPackageName (string path)
		{
			var jsonPath = Directory.Exists (path) ? Path.Combine (path, "package.json") : path;
			return File.Exists (jsonPath) && File.Exists (jsonPath + ".meta")
				? JsonUtility.FromJson<PackageJsonHelper> (File.ReadAllText (jsonPath)).name
				: "";
		}
	}

	internal static class GitUtils
	{
		static readonly StringBuilder s_sbError = new StringBuilder ();
		static readonly StringBuilder s_sbOutput = new StringBuilder ();

		public static bool IsGitRunning { get; private set; }

		public delegate void GitCommandCallback (bool success, string output);

		public static WaitWhile GetRefs (string repoUrl, List<string> result, Action callback)
		{
			result.Clear ();
			string args = string.Format ("ls-remote --refs -q {0}", repoUrl);
			return ExecuteGitCommand (args, (success, output) =>
			{
				if (success)
				{
					foreach (Match m in Regex.Matches (output, "refs/(tags|heads)/(.*)$", RegexOptions.Multiline))
						result.Add (m.Groups [2].Value.Trim());
				}
				callback ();
			});
		}

		public static WaitWhile GetPackageJson (string repoUrl, string branch, Action<string> callback)
		{
			const string kPath = "Temp/UpmGit";
			FileUtil.DeleteFileOrDirectory (kPath);

			string args = string.Format ("clone --depth=1 --branch {0} --single-branch {1} {2}", branch, repoUrl, kPath);
			return ExecuteGitCommand (args, (_, __) => callback (PackageJsonHelper.GetPackageName (kPath)));
		}

		static WaitWhile ExecuteGitCommand (string args, GitCommandCallback callback)
		{
			var startInfo = new System.Diagnostics.ProcessStartInfo
			{
				Arguments = args,
				CreateNoWindow = true,
				FileName = "git",
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};

			var launchProcess = System.Diagnostics.Process.Start (startInfo);
			if (launchProcess == null || launchProcess.HasExited == true || launchProcess.Id == 0)
			{
				Debug.LogError ("No 'git' executable was found. Please install Git on your system and restart Unity");
				callback (false, "");
			}
			else
			{
				//Add process callback.
				IsGitRunning = true;
				s_sbError.Length = 0;
				s_sbOutput.Length = 0;
				launchProcess.OutputDataReceived += (sender, e) => s_sbOutput.AppendLine (e.Data ?? "");
				launchProcess.ErrorDataReceived += (sender, e) => s_sbError.AppendLine (e.Data ?? "");
				launchProcess.Exited += (sender, e) =>
				{
					IsGitRunning = false;
					bool success = 0 == launchProcess.ExitCode;
					if (!success)
					{
						Debug.LogErrorFormat ("Error: git {0}\n\n{1}", args, s_sbError);
					}
					callback (success, s_sbOutput.ToString ());
				};

				launchProcess.BeginOutputReadLine ();
				launchProcess.BeginErrorReadLine ();
				launchProcess.EnableRaisingEvents = true;
			}
			return new WaitWhile (() => IsGitRunning);
		}
	}

	internal static class UIUtils
	{
		const string kDisplayNone = "display-none";
		public static void SetElementDisplay (VisualElement element, bool value)
		{
			if (element == null)
				return;

			SetElementClass (element, kDisplayNone, !value);
			element.visible = value;
		}

		public static bool IsElementDisplay (VisualElement element)
		{
			return !HasElementClass (element, kDisplayNone);
		}

		public static void SetElementClass (VisualElement element, string className, bool value)
		{
			if (element == null)
				return;

			if (value)
				element.AddToClassList (className);
			else
				element.RemoveFromClassList (className);
		}

		public static bool HasElementClass (VisualElement element, string className)
		{
			if (element == null)
				return false;

			return element.ClassListContains (className);
		}

		public static VisualElement GetRoot (VisualElement element)
		{
			while (element != null && element.parent != null)
			{
				element = element.parent;
			}
			return element;
		}
	}

	internal static class PackageUtils
	{
		public static string GetRepoUrl (string url)
		{
			Match m = Regex.Match (url, "(git@[^:]+):(.*)");
			string ret = m.Success ? string.Format ("ssh://{0}/{1}", m.Groups [1].Value, m.Groups [2].Value) : url;
			return ret.EndsWith (".git") ? ret : ret + ".git";
		}

		public static string GetRepoHttpUrl (PackageInfo packageInfo)
		{
			return GetRepoHttpUrl (packageInfo != null ? packageInfo.packageId : "");
		}

		public static string GetRepoHttpUrl (string packageId)
		{
			Match m = Regex.Match (packageId, "^[^@]+@([^#]+)(#.+)?$");
			if (m.Success)
			{
				var repoUrl = m.Groups [1].Value;
				repoUrl = Regex.Replace (repoUrl, "(git:)?git@([^:]+):", "https://$2/");
				repoUrl = repoUrl.Replace ("ssh://", "https://");
				repoUrl = repoUrl.Replace ("git@", "");
				repoUrl = Regex.Replace (repoUrl, "\\.git$", "");

				return repoUrl;
			}
			return "";
		}

		public static string GetRefName (string packageId)
		{
			Match m = Regex.Match (packageId, "^[^@]+@[^#]+#(.+)$");
			if (m.Success)
			{
				return m.Groups [1].Value;
			}
			return "";
		}

		public static string GetRepoId (PackageInfo packageInfo)
		{
			return GetRepoId (packageInfo != null ? packageInfo.packageId : "");
		}

		public static string GetRepoId (string packageId)
		{
			Match m = Regex.Match (GetRepoHttpUrl (packageId), "/([^/]+/[^/]+)$");
			if (m.Success)
			{
				return m.Groups [1].Value;
			}
			return "";
		}

		public static string GetRevisionHash (PackageInfo packageInfo)
		{
			return GetRevisionHash (packageInfo != null ? packageInfo.resolvedPath : "");
		}

		public static string GetRevisionHash (string resolvedPath)
		{
			Match m = Regex.Match (resolvedPath, "@([^@]+)$");
			if (m.Success)
			{
				return m.Groups [1].Value;
			}
			return "";
		}

		public static string GetFileURL (PackageInfo packageInfo, string filePath)
		{
			return packageInfo != null
				? GetFileURL (packageInfo.packageId, packageInfo.resolvedPath, filePath)
				: "";
		}

		public static string GetFileURL (string packageId, string resolvedPath, string filePath)
		{
			if (string.IsNullOrEmpty (packageId) || string.IsNullOrEmpty (resolvedPath) || string.IsNullOrEmpty (filePath))
				return "";

			string repoURL = GetRepoHttpUrl (packageId);
			string hash = GetRevisionHash (resolvedPath);
			string blob = Settings.GetHostData (packageId).Blob;

			return string.Format ("{0}/{1}/{2}/{3}", repoURL, blob, hash, filePath);
		}

		public static string GetFilePath (PackageInfo packageInfo, string filePattern)
		{
			return packageInfo != null
				? GetFilePath (packageInfo.resolvedPath, filePattern)
				: "";
		}

		public static string GetFilePath (string resolvedPath, string filePattern)
		{
			if (string.IsNullOrEmpty (resolvedPath) || string.IsNullOrEmpty (filePattern))
				return "";

			foreach(var path in Directory.EnumerateFiles (resolvedPath, filePattern))
			{
				if(!path.EndsWith(".meta"))
				{
					return path;
				}
			}
			return "";
		}

		public static string GetSpecificPackageId (string packageId, string tag)
		{
			if (string.IsNullOrEmpty (packageId))
				return "";

			Match m = Regex.Match (packageId, "^([^#]+)(#.+)?$");
			if (m.Success)
			{
				var id = m.Groups [1].Value;
				return string.IsNullOrEmpty (tag) ? id : id + "#" + tag;
			}
			return "";
		}

		public static void AddPackage (string packageId, Action<Request> callback = null)
		{
			_request = Client.Add (packageId);
			_callback = callback;
			EditorUtility.DisplayProgressBar ("Add Package", "Cloning " + packageId, 0.5f);
			EditorApplication.update += UpdatePackageRequest;
		}

		public static bool isBusy { get { return GitUtils.IsGitRunning || (_request != null && _request.Status == StatusCode.InProgress); } }

		static Request _request;
		static Action<Request> _callback;

		static void UpdatePackageRequest ()
		{
			if (_request.Status != StatusCode.InProgress)
			{
				if (_request.Status == StatusCode.Failure)
				{
					Debug.LogErrorFormat ("Error: {0} ({1})", _request.Error.message, _request.Error.errorCode);
				}
				
				EditorApplication.update -= UpdatePackageRequest;
				EditorUtility.ClearProgressBar ();
				if(_callback != null)
				{
					_callback (_request);
				}
				_request = null;
				return;
			}
		}
	}

	internal static class MarkdownUtils
	{
		const string k_CssFileName = "github-markdown";

		static readonly MarkdownPipeline s_Pipeline = new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ();
		static readonly string s_TempDir = Path.Combine (Directory.GetCurrentDirectory (), "Temp");

		public static void OpenInBrowser (string path)
		{
			string cssPath = Path.Combine (s_TempDir, k_CssFileName + ".css");
			if (!File.Exists (cssPath))
			{
				File.Copy (AssetDatabase.GUIDToAssetPath (AssetDatabase.FindAssets (k_CssFileName) [0]), cssPath);
			}

			var htmlPath = Path.Combine (s_TempDir, Path.GetFileNameWithoutExtension (path) + ".html");
			using (StreamReader sr = new StreamReader (path))
			using (StreamWriter sw = new StreamWriter (htmlPath))
			{
				sw.WriteLine ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + k_CssFileName + ".css\">");
				sw.Write (Markdown.ToHtml (sr.ReadToEnd (), s_Pipeline));
			}

			Application.OpenURL ("file://" + htmlPath);
		}
	}
}
