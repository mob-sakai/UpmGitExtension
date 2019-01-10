using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Utils = Coffee.PackageManager.UpmGitExtensionUtils;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.PackageManager;
using System.IO;
using System;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif


namespace Coffee.PackageManager.Tests
{
	public class UtilsTests
	{
		const string packageName = "com.coffee.upm-git-extension";
		const string userRepo = "mob-sakai/GitPackageTest";
		const string repoURL = "https://github.com/" + userRepo;
		const string revisionHash = "4386b3c3c27b1daaeed0292fc8dfcf62c7b1b427";
		const string fileName = "README.md";
		const string fileURL = repoURL + "/blob/" + revisionHash + "/" + fileName;

		[TestCase ("", ExpectedResult = "")]
		[TestCase (packageName + "@https://github.com/" + userRepo + ".git", ExpectedResult = repoURL)]
		[TestCase (packageName + "@https://github.com/" + userRepo + ".git#0.3.0", ExpectedResult = repoURL)]
		[TestCase (packageName + "@ssh://git@github.com/" + userRepo + ".git", ExpectedResult = repoURL)]
		[TestCase (packageName + "@ssh://git@github.com/" + userRepo + ".git#0.3.0", ExpectedResult = repoURL)]
		[TestCase (packageName + "@git@github.com:" + userRepo + ".git", ExpectedResult = repoURL)]
		[TestCase (packageName + "@git@github.com:" + userRepo + ".git#0.3.0", ExpectedResult = repoURL)]
		[TestCase (packageName + "@git:git@github.com:" + userRepo + ".git", ExpectedResult = repoURL)]
		[TestCase (packageName + "@git:git@github.com:" + userRepo + ".git#0.3.0", ExpectedResult = repoURL)]
		public string GetRepoURLTest (string packageId)
		{
			return Utils.GetRepoURL (packageId);
		}

		[TestCase ("", ExpectedResult = "")]
		[TestCase (packageName + "@https://github.com/" + userRepo + ".git", ExpectedResult = userRepo)]
		[TestCase (packageName + "@https://github.com/" + userRepo + ".git#0.3.0", ExpectedResult = userRepo)]
		[TestCase (packageName + "@ssh://git@github.com/" + userRepo + ".git", ExpectedResult = userRepo)]
		[TestCase (packageName + "@ssh://git@github.com/" + userRepo + ".git#0.3.0", ExpectedResult = userRepo)]
		[TestCase (packageName + "@git@github.com:" + userRepo + ".git", ExpectedResult = userRepo)]
		[TestCase (packageName + "@git@github.com:" + userRepo + ".git#0.3.0", ExpectedResult = userRepo)]
		[TestCase (packageName + "@git:git@github.com:" + userRepo + ".git", ExpectedResult = userRepo)]
		[TestCase (packageName + "@git:git@github.com:" + userRepo + ".git#0.3.0", ExpectedResult = userRepo)]
		public string GetRepoIdTest (string packageId)
		{
			return Utils.GetRepoId (packageId);
		}

		[TestCase ("", ExpectedResult = true)]
		[TestCase ("true", ExpectedResult = true)]
		[TestCase ("false", ExpectedResult = false)]
		[TestCase ("false,true", ExpectedResult = true)]
		[TestCase ("true,false", ExpectedResult = false)]
		public bool ElementVisibleTest (string operations)
		{
			var _element = new VisualElement ();
			if (0 < operations.Length)
			{
				foreach (bool flag in operations.Split (',').Select (System.Convert.ToBoolean))
					Utils.SetElementDisplay (_element, flag);
			}

			return Utils.IsElementDisplay (_element);
		}

		[TestCase ("", ExpectedResult = false)]
		[TestCase ("true", ExpectedResult = true)]
		[TestCase ("false", ExpectedResult = false)]
		[TestCase ("false,true", ExpectedResult = true)]
		[TestCase ("true,false", ExpectedResult = false)]
		public bool ElementClassTest (string operations)
		{
			var _element = new VisualElement ();
			if (0 < operations.Length)
			{
				foreach (bool flag in operations.Split (',').Select (System.Convert.ToBoolean))
					Utils.SetElementClass (_element, "test", flag);
			}

			return Utils.HasElementClass (_element, "test");
		}


		[TestCase (LogType.Log, "Success", "https://api.github.com/repos/"+ userRepo + "/tags", ExpectedResult = null)]
		[TestCase (LogType.Error, "HTTP/1.1 404 Not Found", "https://api.github.com/repos/"+ userRepo + "/tags2", ExpectedResult = null)]
		[TestCase (LogType.Error, "Cannot resolve destination host", "https://api.githuberror.com/repos/"+ userRepo + "/tags", ExpectedResult =null)]
		[UnityTest]
		[Order(0)]
		public IEnumerator RequestTest(LogType logType, string message, string url)
		{
			var path = Utils.GetRequestCachePath (url);
			Debug.Log (path);
			if(File.Exists(path))
				File.Delete (path);

			LogAssert.Expect (logType, message);
			yield return Utils.Request(url, x=>Debug.Log("Success"));
		}

		[TestCase (LogType.Log, "Success", "https://api.github.com/repos/"+ userRepo + "/tags", ExpectedResult = null)]
		[UnityTest]
		[Order (1)]
		public IEnumerator RequestCacheTest (LogType logType, string message, string url)
		{
			Assert.IsNotNull (Utils.GetRequestCache (url));

			LogAssert.Expect (logType, message);
			Utils.Request (url, x => Debug.Log ("Success"));
			yield break;
		}
	}



#if UPM_GIT_EXT_PROJECT
	public class PackageInfoUtilsTests
	{
		const string repoURL = "https://github.com/mob-sakai/GitPackageTest";
		const string revisionHash = "4386b3c3c27b1daaeed0292fc8dfcf62c7b1b427";
		const string fileName = "README.md";
		const string fileURL = repoURL + "/blob/" + revisionHash + "/" + fileName;

		static PackageInfo pi;
		[TestCase ("com.coffee.upm-git-extension", ExpectedResult = null)]
		[UnityTest ()]
		[Order (-1)]
		public IEnumerator GetPackageInfo (string packageName)
		{
			pi = null;
			var op = Client.List ();
			while (!op.IsCompleted)
				yield return null;

			if (op.Status == StatusCode.Success)
				pi = op.Result.FirstOrDefault (x => x.name == packageName);

			Assert.IsNotNull (pi, string.Format ("{0} is not installed.", packageName));
		}

		[TestCase (false, ExpectedResult = "")]
		[TestCase (true, ExpectedResult = repoURL)]
		public string GetRepoURLTest (bool isPackageInfoExist)
		{
			return Utils.GetRepoURL (isPackageInfoExist ? pi : null);
		}

		[TestCase (false, ExpectedResult = "")]
		[TestCase (true, ExpectedResult = revisionHash)]
		public string GetRevisionHashTest (bool isPackageInfoExist)
		{
			return Utils.GetRevisionHash (isPackageInfoExist ? pi : null);
		}

		[TestCase (false, "README.md", ExpectedResult = "")]
		[TestCase (true, "README.md", ExpectedResult = fileURL)]
		public string GetFileURLTest (bool isPackageInfoExist, string fileName)
		{
			return Utils.GetFileURL (isPackageInfoExist ? pi : null, fileName);
		}
	}
#endif
}
