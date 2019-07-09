using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Coffee.PackageManager
{
	[InitializeOnLoad]
	internal class UpmGitExtension : VisualElement, IPackageManagerExtension
	{
		//################################
		// Constant or Static Members.
		//################################
		static UpmGitExtension ()
		{
			PackageManagerExtensions.RegisterExtension (new UpmGitExtension ());
		}

		//################################
		// Public Members.
		//################################
		/// <summary>
		/// Creates the extension UI visual element.
		/// </summary>
		/// <returns>A visual element that represents the UI or null if none</returns>
		public VisualElement CreateExtensionUI ()
		{
			phase = Phase.Initialize;
			return this;
		}

		/// <summary>
		/// Called by the Package Manager UI when a package is added or updated.
		/// </summary>
		/// <param name="packageInfo">The package information</param>
		public void OnPackageAddedOrUpdated (PackageInfo packageInfo)
		{
		}

		/// <summary>
		/// Called by the Package Manager UI when a package is removed.
		/// </summary>
		/// <param name="packageInfo">The package information</param>
		public void OnPackageRemoved (PackageInfo packageInfo)
		{
		}

		/// <summary>
		/// Called by the Package Manager UI when the package selection changed.
		/// </summary>
		/// <param name="packageInfo">The newly selected package information (can be null)</param>
		public void OnPackageSelectionChange (PackageInfo packageInfo)
		{
			InitializeUI ();
			if (phase == Phase.Initialize || packageInfo == null)
				return;

			// Update document actions.
			documentActions.SetPackageInfo(packageInfo);

			if (packageInfo.source == PackageSource.Git)
			{
				// Show remove button for git package.
				var removeButton = root.Q<Button>("remove");
				UIUtils.SetElementDisplay(removeButton, true);
				removeButton.SetEnabled(true);

				// Show git tag.
				var tagGit = root.Q("tag-git");
				UIUtils.SetElementDisplay(tagGit, true);
			}
		}

		//################################
		// Private Static Members.
		//################################
		enum Phase
		{
			Initialize,
			Idle,
			UpdatePackages,
			ReloadPackageCollection,
		}
		
		static readonly string assemblyName = ", " + typeof(IPackageManagerExtension).Assembly.GetName().Name;
		static readonly string nameSpace = typeof(IPackageManagerExtension).Namespace + ".";
		static readonly Type tPackageWindow = Type.GetType(nameSpace + "PackageManagerWindow" + assemblyName);
		static readonly Type tPackageCollection = Type.GetType(nameSpace + "PackageCollection" + assemblyName);
		static readonly Type tUpmBaseOperation = Type.GetType(nameSpace + "UpmBaseOperation" + assemblyName);
		static readonly Regex regVersionValid = new Regex(@"^\d+", RegexOptions.Compiled);
		static readonly Regex regInstallVersion = new Regex(@"#.*$", RegexOptions.Compiled);
		static Expose _exPackageWindow;
		static EditorWindow _packageWindow;

		static Expose GetExposedPackages()
		{
#if UNITY_2019_1_OR_NEWER
			return Expose.FromType(tPackageCollection)["packages"];
#else
			if (_exPackageWindow == null || !_exPackageWindow.As<EditorWindow>())
				_exPackageWindow = GetExposedPackageWindow();
			return _exPackageWindow["Collection"] ["packages"];
#endif
		}

		static Expose GetExposedPackageWindow()
		{
			if(!_packageWindow)
				_packageWindow = Resources.FindObjectsOfTypeAll(tPackageWindow).FirstOrDefault() as EditorWindow;
			return Expose.FromObject(_packageWindow);
		}
		

		//################################
		// Private Members.
		//################################
		Phase phase;
		VisualElement root;
		DocumentActions documentActions;
		readonly Queue<Expose> gitPackages = new Queue<Expose>();

		/// <summary>
		/// Initializes UI.
		/// </summary>
		void InitializeUI ()
		{
			if (phase != Phase.Initialize)
				return;

			if (!DocumentActions.IsResourceReady() || !InstallPackageWindow.IsResourceReady() || !GitButton.IsResourceReady())
				return;

			root = UIUtils.GetRoot(this).Q("container");

			// Document actions.
			documentActions = new DocumentActions(root.Q("detailActions"));

			// Install package window.
			var installPackageWindow = new InstallPackageWindow();
			root.Add(installPackageWindow);

			// Add button to open InstallPackageWindow
			var addButton = root.Q("toolbarAddButton") ?? root.Q("moreAddOptionsButton");
			var gitButton = new GitButton(installPackageWindow.Open);
			addButton.parent.Insert(addButton.parent.IndexOf(addButton) + 1, gitButton);

#if UNITY_2018
			var space = new VisualElement();
			space.style.flexGrow = 1;
			addButton.parent.Insert(addButton.parent.IndexOf(addButton), space);
#endif

			// Update git packages on load packages
			var packageList = Expose.FromObject(root.Q("packageList"));
			Action onLoad = packageList["OnLoaded"].As<Action>();
			onLoad += OnPackageListLoaded;
			packageList["OnLoaded"] = Expose.FromObject(onLoad);

#if UNITY_2019_1_OR_NEWER
			var updateButton = root.Q("packageToolBar").Q<Button>("update");
#else
			OnPackageListLoaded();
			var updateButton = root.Q("updateCombo").Q<Button>("update");
#endif

			var detailView = Expose.FromObject(root.Q("detailsGroup"));

			// Override click action.
			Action actionUpdate = () =>
			{
			
#if UNITY_2019_1_OR_NEWER
				var exTargetPackage = detailView["TargetVersion"];
#else
				var exTargetPackage = detailView["SelectedPackage"];
#endif
				if (exTargetPackage["Origin"].As<int>() == 99)
				{
					var packageId = exTargetPackage["Info"]["m_PackageId"].As<string>();
					string packageIdPrefix = regInstallVersion.Replace(packageId, "");
					string refName = exTargetPackage["Version"].ToString().Replace("0.0.0-", "");
					exTargetPackage["_PackageId"] = Expose.FromObject(packageIdPrefix + "#" + refName);
				}

				detailView.Call("UpdateClick");
			};
			Expose.FromObject(updateButton.clickable)["clicked"] = Expose.FromObject(actionUpdate);

			phase = Phase.Idle;
		}

		
		void UpdateGitPackages(Queue<Expose> packagesToUpdate, Dictionary<string, IEnumerable<string>> results = null)
		{
			Debug.LogFormat ("[UpdateGitPackages] {0} package(s) left", packagesToUpdate.Count);
			phase = Phase.UpdatePackages;
			bool isRunning = 0 < packagesToUpdate.Count;
			PlaySpinner(isRunning);

			// Update task is finished.
			if (!isRunning)
			{
				Debug.LogFormat ("[UpdateGitPackages] Completed");
				// Nothing to do.
				if (results == null)
				{
					phase = Phase.Idle;
					return;
				}
					
				// Update package infomation's version.
				Expose exPackages = GetExposedPackages();
				foreach (var pair in results)
				{
					try
					{
						Debug.LogFormat ("[UpdateGitPackages] Overwrite {0}", pair.Key);
						if (exPackages.Call("ContainsKey", pair.Key).As<bool>())
							UpdatePackageInfoVersions(exPackages[pair.Key], pair.Value);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}

				// Reload package collection on next frame
				Debug.LogFormat ("[UpdateGitPackages] Reload on next frame");
				phase = Phase.ReloadPackageCollection;
				EditorApplication.delayCall += ReloadPackageCollection;
				return;
			}
			
			if (results == null)
			{
				results = new Dictionary<string, IEnumerable<string>>();
			}

			// 
			var package = packagesToUpdate.Dequeue();
			var displayPackage = package["VersionToDisplay"];
			var packageId = displayPackage["_PackageId"].As<string>();
			var packageName = package["packageName"].As<string>();
			
			// Already get versions.
			if(packageId == null || results.ContainsKey(packageName))
			{
				Debug.LogFormat ("[UpdateGitPackages] Skip: {0}", packageName);
				UpdateGitPackages(packagesToUpdate, results);
				return;
			}
			
			// Get all branch/tag names in repo.
			var url = PackageUtils.GetRepoUrlForCommand(packageId);
			Debug.LogFormat ("[UpdateGitPackages] GetRefs: {0}", packageName);
			GitUtils.GetRefs(url, refNames =>
			{
				results[packageName] = refNames;
				UpdateGitPackages(packagesToUpdate, results);
			});
		}

		void UpdatePackageInfoVersions(Expose exPackage, IEnumerable<string> versions)
		{
			Expose vers = Expose.FromObject(versions
				.Select(x => regVersionValid.IsMatch(x) ? x : "0.0.0-" + x)
				.Distinct()
				.OrderBy(x => x)
				.ToArray());

			var exInfo = exPackage["VersionToDisplay"]["Info"];
			var exVersions = exInfo["m_Versions"];
			exVersions["m_All"] = vers;
			exVersions["m_Compatible"] = vers;

			try
			{
				Expose exPackageInfoList = Expose.FromType(tUpmBaseOperation).Call("FromUpmPackageInfo", exInfo, true); // Convert to PackageInfos
				foreach (Expose x in exPackageInfoList)
				{
					x["Origin"] = Expose.FromObject(99);
#if UNITY_2019_2_OR_NEWER
					x["IsDiscoverable"] = Expose.FromObject(true);
#endif
				}
				exPackage["source"] = exPackageInfoList;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		void PlaySpinner(bool playing)
		{
			var statusBar = root.Q("packageStatusBar");
			if (statusBar == null)
				return;

			var spinner = statusBar.Q("packageSpinner");
			if (spinner == null)
				return;

			Expose.FromObject(spinner).Call(playing ? "Start" : "Stop");
		}

		void ReloadPackageCollection()
		{
			var exPackageWindow = GetExposedPackageWindow();
			if (exPackageWindow == null)
				return;

			Debug.LogFormat ("[UpdateGitPackages] Reloading package collection...");
			exPackageWindow["Collection"].Call("UpdatePackageCollection", false);
		}

		//bool updateRequest = false;

		void OnPackageListLoaded()
		{
			Debug.LogFormat ("[UpdateGitPackages] OnPackageListLoaded {0}, {1}", phase, Time.frameCount);
			if (phase == Phase.ReloadPackageCollection)
			{
				phase = Phase.Idle;
				return;
			}
				
			try
			{
				// Get repository refs as versions and update package infos.
				foreach (Expose p in GetExposedPackages()["Values"])
				{
					// Is it git package?
					var origin = p["VersionToDisplay"]["Origin"].As<int>();
					if (origin == 5 || origin == 99)
					{
						gitPackages.Enqueue(p);
					}
				}

				if (phase == Phase.Idle || phase == Phase.Initialize)
				{
					Debug.LogFormat ("[UpdateGitPackages] Start task to update git package.");
					UpdateGitPackages(gitPackages, null);
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}
