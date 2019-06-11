using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Coffee.PackageManager
{
	[CreateAssetMenu]
	public class UpmGitSettings : ScriptableObject
	{
		static readonly HostData s_EmptyHostData = new HostData ();
		static UpmGitSettings s_Instance = null;
		static UpmGitSettings Instance
		{
			get
			{
				if (s_Instance == null)
				{
					s_Instance = AssetDatabase.FindAssets ("t:" + typeof (UpmGitSettings).Name)
						.Select (x => AssetDatabase.GUIDToAssetPath (x))
						.OrderBy (x => x)
						.Select(x=>AssetDatabase.LoadAssetAtPath<UpmGitSettings> (x))
						.FirstOrDefault ();
				}
				return s_Instance;
			}
		}

		public HostData [] m_HostData;

		public static HostData GetHostData (string packageId)
		{
			return Instance.m_HostData.FirstOrDefault (x=> packageId.Contains(x.Domain)) ?? s_EmptyHostData;
		}
	}

	[System.Serializable]
	public class HostData
	{
		public string Name = "undefined";
		public string Domain = "undefined";
		public string Blob = "blob";
		public Texture2D LogoDark = null;
		public Texture2D LogoLight = null;
	}
}