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
	internal class UpmGitExtensionUI : VisualElement, IPackageManagerExtension
	{
		//################################
		// Constant or Static Members.
		//################################
		static UpmGitExtensionUI ()
		{
			PackageManagerExtensions.RegisterExtension (new UpmGitExtensionUI ());
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
			if (packageInfo == null)
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
		

		//################################
		// Private Members.
		//################################
		Phase phase;
		VisualElement root;
		DocumentActions documentActions;

		/// <summary>
		/// Initializes UI.
		/// </summary>
		void InitializeUI ()
		{
			if (phase != Phase.Initialize)
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
			phase = Phase.Idle;
		}
	}
}
