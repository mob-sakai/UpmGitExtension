using UnityEngine;
using UnityEditor;

namespace Coffee.UpmGitExtension
{
    public class LegacyWarning
    {
        [InitializeOnLoadMethod]
        private static void Log()
        {
#if !UNITY_2020_1_OR_NEWER
            Debug.LogError("'UpmGitExtensions package (v2)' does not support Unity 2018 or 2019. Please install 'UpmGitExtensions package (v1)' instead.\n" +
                "For details, see https://github.com/mob-sakai/ParticleEffectForUGUI#installation-for-unity-2018-or-2019");
#endif
        }
    }
}
