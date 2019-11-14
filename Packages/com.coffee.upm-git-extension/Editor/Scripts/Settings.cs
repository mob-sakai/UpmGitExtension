using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Coffee.PackageManager
{
	public class Settings : ScriptableObject
	{
		static HostData s_EmptyHostData;
		static Settings s_Instance;
		static Settings Instance
		{
			get
			{
				if (s_Instance == null)
				{
					s_Instance = AssetDatabase.FindAssets ("t:" + typeof (Settings).Name)
						.Select (x => AssetDatabase.GUIDToAssetPath (x))
						.OrderBy (x => x)
						.Select(x=>AssetDatabase.LoadAssetAtPath<Settings> (x))
						.FirstOrDefault ();
				}
				if (s_EmptyHostData == null)
				{
					s_EmptyHostData = new HostData
					{
						LogoDark = EditorGUIUtility.FindTexture ("buildsettings.web.small"),
						LogoLight = EditorGUIUtility.FindTexture ("d_buildsettings.web.small")
					};
				}
				return s_Instance;
			}
		}

		public HostData [] m_HostData;

		public static HostData GetHostData (string packageId)
		{
			return Instance
				? Instance.m_HostData.FirstOrDefault (x => packageId.Contains (x.Domain)) ?? s_EmptyHostData
				: s_EmptyHostData;
		}
	}

	[System.Serializable]
	public class HostData
	{
		public string Name = "web";
		public string Domain = "undefined";
		public string Blob = "blob";
		public string Raw = "https://rawcdn.githack.com/{0}/{1}/{2}";
		public Texture2D LogoDark;
		public Texture2D LogoLight;
		public Texture2D Logo { get { return EditorGUIUtility.isProSkin ? LogoLight : LogoDark; } }
	}
}