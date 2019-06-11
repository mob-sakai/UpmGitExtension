using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.IO;
using System.Text;
using UnityEditor.PackageManager.Requests;

namespace Coffee.PackageManager
{
	public class UpmGitAddWindow : EditorWindow
	{
		string _url = "";
		string _repoUrl = "";
		string _version = "(default)";
		string _packageId = "";
		List<string> _refs = new List<string> ();

		GUIContent _errorUrl;
		GUIContent _errorBranch;

		private void OnEnable ()
		{
			_errorUrl = new GUIContent (EditorGUIUtility.FindTexture ("console.erroricon.sml"), "Make sure you have the correct access rights and the repository exists.");
			_errorBranch = new GUIContent (EditorGUIUtility.FindTexture ("console.erroricon.sml"), "package.json and package.json.meta do not exist in this branch/tag.");
			minSize = new Vector2 (300, 40);
			maxSize = new Vector2 (600, 40);
			titleContent = new GUIContent ("Add package from URL");
		}

		void PopupVersions (Action<string> onVersionChanged)
		{
			var menu = new GenericMenu ();
			var currentRefName = _version;

			void callback (object x) => onVersionChanged (x as string);

			// x.y(.z-sufix) only 
			foreach (var t in _refs.Where (x => Regex.IsMatch (x, "^\\d+\\.\\d+.*$")).OrderByDescending (x => x))
			{
				string target = t;
				bool isCurrent = currentRefName == target;
				GUIContent text = new GUIContent (target);
				menu.AddItem (text, isCurrent, callback, target);
			}

			// other 
			menu.AddItem (new GUIContent ("Other/(default)"), currentRefName == "", callback, "(default)");
			foreach (var t in _refs.Where (x => !Regex.IsMatch (x, "^\\d+\\.\\d+.*$")).OrderByDescending (x => x))
			{
				string target = t;
				bool isCurrent = currentRefName == target;
				GUIContent text = new GUIContent ("Other/" + target);
				menu.AddItem (text, isCurrent, callback, target);
			}
			menu.ShowAsContext ();
		}

		void OnGUI ()
		{
			EditorGUIUtility.labelWidth = 100;
			using (var ds = new EditorGUI.DisabledScope (PackageUtils.isBusy))
			{
				using (var ccs = new EditorGUI.ChangeCheckScope ())
				using (new EditorGUILayout.HorizontalScope ())
				{
					_url = EditorGUILayout.TextField ("Repogitory URL", _url);
					if (ccs.changed)
					{
						_repoUrl = PackageUtils.GetRepoUrl (_url);
						_version = "-- Select Version --";
						_packageId = "";
						GitUtils.GetRefs (_url, _refs, () => { EditorApplication.delayCall += Repaint; });
					}

					if (!PackageUtils.isBusy && !string.IsNullOrEmpty (_url) && _refs.Count == 0)
						GUILayout.Label (_errorUrl, GUILayout.Width (20));
				}

				using (new EditorGUILayout.HorizontalScope ())
				{
					EditorGUILayout.PrefixLabel ("Version");
					using (new EditorGUI.DisabledScope (_refs.Count == 0))
					{
						if (GUILayout.Button (_version, EditorStyles.popup))
						{
							PopupVersions (ver =>
							{
								_version = _refs.Contains (ver) ? ver : "HEAD";
								_packageId = "";
								GitUtils.GetPackageJson (_url, _version, name =>
								{
									_packageId = string.IsNullOrEmpty (name)
										? null
										: name + "@" + _repoUrl + "#" + _version;
									EditorApplication.delayCall += Repaint;
								});
							});
						}
					}
					using (new EditorGUI.DisabledScope (string.IsNullOrEmpty (_packageId)))
					{
						if (GUILayout.Button (new GUIContent ("Add", "Add a package '" + _packageId + "' to the project."), EditorStyles.miniButton, GUILayout.Width (60)))
						{
							PackageUtils.AddPackage (_packageId, req =>
							{
								if (req.Status == StatusCode.Success)
									Close ();
							});
						}
					}
					if (_packageId == null)
						GUILayout.Label (_errorBranch, GUILayout.Width (20));
				}
			}
		}
	}
}