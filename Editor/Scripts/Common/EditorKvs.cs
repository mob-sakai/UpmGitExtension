using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Coffee.PackageManager
{
	internal class EditorKvs : ScriptableSingleton<EditorKvs>, ISerializationCallbackReceiver
	{
		const long kLifeTime = 60 * 5 * TimeSpan.TicksPerSecond;

		Dictionary<string, int> m_IndexMap = new Dictionary<string, int> ();

		[SerializeField]
		List<string> m_Keys = new List<string> ();

		[SerializeField]
		List<string> m_Values = new List<string> ();

		[SerializeField]
		List<long> m_Expires = new List<long> ();

		int GetIndex (string key)
		{
			int index;
			if (m_IndexMap.TryGetValue (key, out index))
				return index;

			index = m_Keys.Count;
			m_Keys.Add (key);
			m_Values.Add ("");
			m_Expires.Add (0);
			m_IndexMap.Add (key, index);
			return index;
		}

		internal static string Get (string key)
		{
			string result;
			TryGet (key, out result);
			return result;
		}

		internal static T Get<T> (string key, Func<string, T> converter)
		{
			T result;
			TryGet (key, out result, converter);
			return result;
		}

		internal static bool TryGet (string key, out string result)
		{
			var inst = instance;
			int index = inst.GetIndex (key);
			bool isAvalable = DateTime.UtcNow.Ticks < inst.m_Expires [index];
			result = isAvalable ? inst.m_Values [index] : "";
			Debug.LogFormat (">>>> Cache hit? {0}, key = {1}, result = {2}", isAvalable, key, result);
			return isAvalable;
		}

		internal static bool TryGet<T> (string key, out T result, Func<string, T> converter)
		{
			string value;
			bool isAvalable = TryGet (key, out value);
			try
			{
				result = isAvalable ? converter (value) : default (T);
			}
			catch (Exception e)
			{
				Debug.LogException (e);
				isAvalable = false;
				result = default (T);
			}
			return isAvalable;
		}

		internal static void Set (string key, string value)
		{
			var inst = instance;
			int index = inst.GetIndex (key);
			inst.m_Values [index] = value;
			inst.m_Expires [index] = DateTime.UtcNow.Ticks + kLifeTime;
			Debug.LogFormat (">>>> Cache key = {0}, result = {1}", key, value);
		}

		internal static void Set<T> (string key, T value, Func<T, string> converter)
		{
			Set (key, converter (value));
		}

#if UPM_GIT_EXT_DEBUG
		[MenuItem("EditorKvs/Clear")]
#endif
		internal static void Clear ()
		{
			var inst = instance;
			inst.m_Keys.Clear ();
			inst.m_Values.Clear ();
			inst.m_Expires.Clear ();
			inst.OnAfterDeserialize ();
		}

		public void OnBeforeSerialize ()
		{
		}

		public void OnAfterDeserialize ()
		{
			m_IndexMap = m_Keys
				.Select ((k, i) => new { k, i })
				.ToDictionary (x => x.k, x => x.i);
		}
	}
}
