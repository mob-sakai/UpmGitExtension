using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.IO;
using System.Reflection;
using UnityEditor.Compilation;

namespace Coffee.InternalAccessible
{
    [CreateAssetMenu()]
    public class InternalAccessibleAsset : ScriptableObject
    {
        [System.Serializable]
        public class Condition
        {
            public string m_Version;
            public string m_AccessibleAssemblyName;
            public string m_AssemblyName;
            public Condition(string version, string accessible, string assembly)
            {
                m_Version = version;
                m_AccessibleAssemblyName = accessible;
                m_AssemblyName = assembly;
            }

            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}", m_Version, m_AccessibleAssemblyName, m_AssemblyName);
            }
        }

        static Regex s_RegName = new Regex("\"name\":\\s*\"(.+)\"", RegexOptions.Compiled);
        static Regex s_RegUnityVersion = new Regex("^(\\d+\\.\\d+)", RegexOptions.Compiled);

        public AssemblyDefinitionAsset m_Asmdef;
        public Condition[] m_Conditions = {
                new Condition("2019.2", "Unity.InternalAPIEditorBridgeDev.001", "UnityEditor"),
                new Condition("2019.1", "Unity.PackageManagerCaptain.Editor", "Unity.PackageManagerUI.Editor"),
                new Condition("2018.3", "Unity.PackageManagerCaptain.Editor", "Unity.PackageManagerUI.Editor"),
            };

        public string CsProjPath { get { return m_Asmdef ? s_RegName.Match(m_Asmdef.text).Groups[1].Value + ".csproj" : null; } }
        public string AsmdefPath { get { return m_Asmdef ? AssetDatabase.GetAssetPath(m_Asmdef) : null; } }
        public string DllName { get { return string.Format("{0}.{1}.dll", m_Asmdef.name, s_RegUnityVersion.Match(Application.unityVersion).Groups[1].Value); } }
        public string DllPath { get { return Path.Combine(Path.GetDirectoryName(AsmdefPath), DllName); } }

        public Condition CurrentCondition { get { return m_Conditions.OrderBy(x=>x.m_Version).FirstOrDefault(x=>x.m_Version.CompareTo(Application.unityVersion) < 0); } }

        public static IEnumerable<InternalAccessibleAsset> GetAll()
        {
            return AssetDatabase.FindAssets(string.Format("t:{0}", typeof(InternalAccessibleAsset)))
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<InternalAccessibleAsset>(path));
        }

        public static IEnumerable<InternalAccessibleAsset> GetAllFromScriptPath(IEnumerable<string> paths)
        {
            var affectedAsmdefPaths = paths
                .Select(CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath)
                .Distinct()
                .ToArray();

            return 0 == affectedAsmdefPaths.Length
                ? Enumerable.Empty<InternalAccessibleAsset>()
                : GetAll()
                    .Where(x => affectedAsmdefPaths.Contains(x.AsmdefPath));
        }

        //[InitializeOnLoadMethod]
        //static void ApplyAll ()
        //{
        //    foreach(var asset in GetAll())
        //    {
        //        var currentAssemblyName = s_RegName.Match(asset.m_Asmdef.text).Groups[1].Value;
        //        var currentCondition = asset.CurrentCondition;

        //        Debug.Log(asset.name);
        //        Debug.Log(currentCondition);

        //        if(currentCondition.m_AccessibleAssemblyName != currentAssemblyName)
        //        {
        //            Debug.Log("Change!!!!");

        //            var path = AssetDatabase.GetAssetPath(asset);
        //            var replace = string.Format("\"name\": \"{0}\"", currentCondition.m_AccessibleAssemblyName);
        //            File.WriteAllText(path, s_RegName.Replace(asset.m_Asmdef.text, replace));
        //        }
        //    }
        //}

        
    }

    internal class Settings : ScriptableSingleton<Settings>
    {
        public bool IsDevelopMode = false;

    }

    internal class Postprocessor : AssetPostprocessor
    {
        static Regex s_RegIntrenalBridge = new Regex("^Packages/com.coffee.upm-git-extension/Editor/InternalBridge/[^/]+.cs$", RegexOptions.Compiled);

        static MethodInfo s_MiSyncSolution = System.Type.GetType("UnityEditor.SyncVS, UnityEditor")
                .GetMethod("SyncSolution", BindingFlags.Static | BindingFlags.Public);


        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!Settings.instance.IsDevelopMode)
                return;

            var scriptPaths = importedAssets
                .Union(deletedAssets)
                .Union(movedAssets)
                .Union(movedFromAssetPaths)
                .Where(x => x.EndsWith(".cs", System.StringComparison.Ordinal))
                ;

            if (scriptPaths.Any())
            {
                if (scriptPaths.Any(s_RegIntrenalBridge.IsMatch))
                {
                    CompileBridge();
                }
            }
        }

        static void CompileCsproj(string proj, string dll)
        {
#if UNITY_2019_2_OR_NEWER
            var compiler = "Packages/com.coffee.internal-accessible/Compiler~/Compiler1.4.csproj";
#else
            var compiler = "Packages/com.coffee.internal-accessible/Compiler~/Compiler1.1.csproj";
#endif
            var outputDll = Path.GetFileName(dll);
            var args = string.Format("\"{0}\" \"{1}\"", Path.GetFullPath(proj), Path.GetFullPath(dll));
            UnityEngine.Debug.LogFormat("Start compile {0}", proj);

            DotNet.Run(compiler, args, (success, stdout) =>
            {
                if (!success)
                    return;

                UnityEngine.Debug.Log("Compile Complete! ");
                EditorApplication.delayCall += () =>
                {
                    AssetDatabase.ImportAsset(dll, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                };
            });
        }

        [MenuItem("Assets/Develop Mode")]
        static void DevelopBridge()
        {
            var targets = new[] {
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.asmdef",
                "Packages/com.coffee.upm-git-extension/Editor/InternalBridge/Coffee.UpmGitExtension.Editor.Bridge.asmdef"
            };

#if UNITY_2019_2_OR_NEWER
            const string suffix = ".2019.2~";
#else
            const string suffix = ".2018.3~";
#endif
            foreach(var t in targets)
            {
                File.Copy(t + suffix, t, true);
            }

            Settings.instance.IsDevelopMode = true;
        }

        [MenuItem("Assets/Compile Bridge")]
        static void CompileBridge()
        {
            s_MiSyncSolution.Invoke(null, new object[0]);

            CompileCsproj(
#if UNITY_2019_2_OR_NEWER
                "Unity.InternalAPIEditorBridgeDev.001.csproj",
#else
                "Unity.PackageManagerCaptain.Editor.csproj",
#endif

#if UNITY_2019_3_OR_NEWER
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.Bridge.2019.3.dll"
#elif UNITY_2019_2_OR_NEWER
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.Bridge.2019.2.dll"
#elif UNITY_2019_1_OR_NEWER
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.Bridge.2019.1.dll"
#else
                "Packages/com.coffee.upm-git-extension/Editor/Coffee.UpmGitExtension.Editor.Bridge.2018.3.dll"
#endif
            );
        }
    }
}