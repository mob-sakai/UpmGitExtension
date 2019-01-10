#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;
using UnityEngine.Networking;
using UnityEngine;
using System;
using System.IO;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Collections.Generic;
using System.Linq;

namespace Coffee.PackageManager
{
	internal static class UpmGitExtensionUtils
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

		public static string GetRepoURL (PackageInfo packageInfo)
		{
			return GetRepoURL (packageInfo != null ? packageInfo.packageId : "");
		}

		public static string GetRepoURL (string packageId)
		{
			Match m = Regex.Match (packageId, "^[^@]+@([^#]+)(#.+)?$");
			if (m.Success)
			{
				var repoUrl = m.Groups [1].Value;
				repoUrl = Regex.Replace (repoUrl, "(git:)?git@([^:]+):", "https://$2/");
				//repoUrl = repoUrl.Replace ("github.com:", "https://github.com/");
				repoUrl = repoUrl.Replace ("ssh://", "https://");
				repoUrl = repoUrl.Replace ("git@", "");
				repoUrl = Regex.Replace (repoUrl, "\\.git$", "");

				return repoUrl;
			}
			return "";
		}

		public static string GetRepoId (PackageInfo packageInfo)
		{
			return GetRepoId (packageInfo != null ? packageInfo.packageId : "");
		}

		public static string GetRepoId (string packageId)
		{
			Match m = Regex.Match (GetRepoURL(packageId), "/([^/]+/[^/]+)$");
			if (m.Success)
			{
				return m.Groups [1].Value;
			}
			return "";
		}

		public static string GetApiRequestUrl (string packageId, string methodPath)
		{
			var repoId = GetRepoId (packageId);
			if (packageId.Contains("github.com"))
			{
				return "https://api.github.com/repos/" + repoId + "/" + methodPath;
			}
			return "";
		}

		public static AsyncOperation RequestTags (string packageId, List<string> result)
		{
			return Request (GetApiRequestUrl (packageId, "tags"), x => FillRefNamesFromResponse (x, result));
		}

		public static AsyncOperation RequestBranches (string packageId, List<string> result)
		{
			return Request (GetApiRequestUrl (packageId, "branches"), x => FillRefNamesFromResponse (x, result));
		}

		public static void FillRefNamesFromResponse(string res, List<string> result)
		{
			result.Clear ();
			result.AddRange (
				Regex.Matches (res, "\\s*\"name\": \"(.+)\",")
					.Cast<Match> ()
					.Select (x => x.Groups [1].Value)
			);
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

			string repoURL = GetRepoURL (packageId);
			string hash = GetRevisionHash (resolvedPath);
			string blob = "blob";

			return string.Format ("{0}/{1}/{2}/{3}", repoURL, blob, hash, filePath);
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

		public static string GetRequestCache (string url)
		{
			var path = GetRequestCachePath (url);
			return File.Exists (path) && ((DateTime.UtcNow - File.GetLastWriteTimeUtc (path)).TotalSeconds < 300)
				? File.ReadAllText (path)
				: null;
		}

		public static string GetRequestCachePath (string url)
		{
			return "Temp/RequestCache_" + url.GetHashCode ();
		}

		public static AsyncOperation Request (string url, Action<string> onSuccess)
		{
			if (string.IsNullOrEmpty (url))
				return null;

			var cache = GetRequestCache (url);
			if (!string.IsNullOrEmpty (cache))
			{
				onSuccess (cache);
				return null;
			}

			var www = UnityWebRequest.Get (url);
			var op = www.SendWebRequest ();
			op.completed += _ =>
			{
				if (www.isHttpError || www.isHttpError || !string.IsNullOrEmpty(www.error))
				{
					Debug.LogError (www.error);
					www.Dispose ();
					return;
				}
				var path = GetRequestCachePath (url);
				File.WriteAllText (path, www.downloadHandler.text);
				onSuccess (www.downloadHandler.text);
				www.Dispose ();
			};
			return op;
		}
	}
}
