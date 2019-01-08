using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Utils = Coffee.PackageManager.UpmGitExtensionUtils;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.PackageManager;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif


namespace Coffee.PackageManager.Tests
{
	public class UtilsTests
	{
		const string packageName = "coffee.upm-git-extension";
		const string userRepo = "mob-sakai/GitPackageTest";
		const string repoURL = "https://github.com/" + userRepo;
		const string revisionHash = "64d5c678fee9a68ffaaa3298f318d67e238131b4";
		const string fileName = "README.md";
		const string fileURL = repoURL + "/blob/" + revisionHash + "/" + fileName;

		[TestCase ("", ExpectedResult = "")]
		[TestCase (packageName + "@https://github.com/" + userRepo + ".git", ExpectedResult = repoURL)]
		[TestCase (packageName + "@https://github.com/" + userRepo + ".git#0.1.0", ExpectedResult = repoURL)]
		[TestCase (packageName + "@ssh://git@github.com/" + userRepo + ".git", ExpectedResult = repoURL)]
		[TestCase (packageName + "@ssh://git@github.com/" + userRepo + ".git#0.1.0", ExpectedResult = repoURL)]
		[TestCase (packageName + "@git@github.com:" + userRepo + ".git", ExpectedResult = repoURL)]
		[TestCase (packageName + "@git@github.com:" + userRepo + ".git#0.1.0", ExpectedResult = repoURL)]
		[TestCase (packageName + "@git:git@github.com:" + userRepo + ".git", ExpectedResult = repoURL)]
		[TestCase (packageName + "@git:git@github.com:" + userRepo + ".git#0.1.0", ExpectedResult = repoURL)]
		public string GetRepoURLTest (string packageId)
		{
			return Utils.GetRepoURL (packageId);
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

	}



#if UPM_GIT_EXT_PROJECT
	public class PackageInfoUtilsTests
	{
		const string repoURL = "https://github.com/mob-sakai/GitPackageTest";
		const string revisionHash = "64d5c678fee9a68ffaaa3298f318d67e238131b4";
		const string fileName = "README.md";
		const string fileURL = repoURL + "/blob/" + revisionHash + "/" + fileName;

		static PackageInfo pi;
		[TestCase ("coffee.git-package-test", ExpectedResult = null)]
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
