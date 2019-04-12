using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.PackageManager;
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
			return PackageUtils.GetRepoHttpUrl (packageId);
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
			return PackageUtils.GetRepoId (packageId);
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
					UIUtils.SetElementDisplay (_element, flag);
			}

			return UIUtils.IsElementDisplay (_element);
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
					UIUtils.SetElementClass (_element, "test", flag);
			}

			return UIUtils.HasElementClass (_element, "test");
		}
	}

	public class PackageInfoUtilsTests
	{
		static PackageInfo pi;
		[TestCase ("com.coffee.upm-git-extension-test-github", ExpectedResult = null)]
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
		[TestCase (true, ExpectedResult = "https://github.com/mob-sakai/GitPackageTest")]
		public string GetRepoURLTest (bool isPackageInfoExist)
		{
			return PackageUtils.GetRepoHttpUrl (isPackageInfoExist ? pi : null);
		}

		[TestCase (false, ExpectedResult = "")]
		[TestCase (true, ExpectedResult = "fdcd7a6d0ff19c4a717cfb3153ba1f4bf1004d47")]
		public string GetRevisionHashTest (bool isPackageInfoExist)
		{
			return PackageUtils.GetRevisionHash (isPackageInfoExist ? pi : null);
		}

		[TestCase (false, "README.*", ExpectedResult = "")]
		[TestCase (true, "README.*", ExpectedResult = "Library/PackageCache/com.coffee.upm-git-extension-test-github@fdcd7a6d0ff19c4a717cfb3153ba1f4bf1004d47/README.md")]
		public string GetFileURLTest (bool isPackageInfoExist, string pattern)
		{
			var path = PackageUtils.GetFilePath (isPackageInfoExist ? pi : null, pattern);
			if (path.Length == 0)
				return "";

			return new Uri (Environment.CurrentDirectory + "/").MakeRelativeUri (new Uri (path)).ToString ();
		}
	}
}
