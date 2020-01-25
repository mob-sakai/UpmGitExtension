using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Coffee.OpenSesamePortable;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Coffee.OpenSesame
{
    internal class EditorSettings : ScriptableSingleton<EditorSettings>
    {
        public static bool publishRequested;
        public string PublishAssemblyName;
        public bool IsSettingsOpend;
    }

    [System.Serializable]
    internal class OpenSesameSetting
    {
        public bool OpenSesame;
        public string ModifySymbols = "";
        public bool Optimize;

        public static OpenSesameSetting GetAtPathOrDefault(string path)
        {
            bool openSesame;
            string modifySymbols;
            bool optimize;
            Core.GetOpenSesameSettings(path, out openSesame, out modifySymbols, out optimize);
            return new OpenSesameSetting()
            {
                OpenSesame = openSesame,
                ModifySymbols = modifySymbols,
                Optimize = optimize,
            };
        }

        public static OpenSesameSetting CreateFromJson(string json)
        {
            bool openSesame;
            string modifySymbols;
            bool optimize;
            Core.GetOpenSesameSettingsFromJson(json ?? "", out openSesame, out modifySymbols, out optimize);
            return new OpenSesameSetting()
            {
                OpenSesame = openSesame,
                ModifySymbols = modifySymbols,
                Optimize = optimize,
            };
        }
    }

    [InitializeOnLoad]
    internal static class OpenSesameInspectorGUI
    {
        const string kIfText = "#if OPEN_SESAME // This line is added by Open Sesame Portable. DO NOT remov manually.";
        const string kEndIfText = "#endif // This line is added by Open Sesame Portable. DO NOT remov manually.";

        static GUIContent s_OpenSesameText;
        static GUIContent s_PortableModeText;
        static GUIContent s_ModifySymbolsText;
        static GUIContent s_SettingsText;
        static GUIContent s_PublishText;
        static GUIContent s_OptimizeText;
        static GUIContent s_HelpText;
        static Dictionary<string, bool> s_PortableModes = new Dictionary<string, bool>();

        static void Log(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat("<b>[OpenSesame]</b> " + format, args);
        }

        static OpenSesameInspectorGUI()
        {
            s_OpenSesameText = new GUIContent("Open Sesame", "Use OpenSesameCompiler instead of default csc. In other words, allow access to internals and privates in other assemblies.");
            s_ModifySymbolsText = new GUIContent("Modify Symbols", "When compiling this assembly, add or remove specific symbols separated with semicolons (;) or commas (,).\nSymbols starting with '!' will be removed.\n\ne.g. 'SYMBOL_TO_ADD;!SYMBOL_TO_REMOVE;...'");
            s_PortableModeText = new GUIContent("Portable Mode", "Make this assembly available to other projects that do not have the OpenSesame package installed.");
            s_SettingsText = new GUIContent("Settings", "Show other settings for this assembly.");
            s_PublishText = new GUIContent("Publish", "Publish this assembly as dll to the parent directory.");
            s_OptimizeText = new GUIContent("Optimize", "Compile without debug option. (/optimize+, /debug:none, /d:DEBUG=false, /d:TRACE=false)");
            s_HelpText = new GUIContent("Help", "Open help page on browser.");

            Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        static void OnPostHeaderGUI(Editor editor)
        {
            var importer = editor.target as AssemblyDefinitionImporter;
            if (!importer)
                return;

            bool settingChanged = false;
            OpenSesameSetting setting = OpenSesameSetting.CreateFromJson(importer.userData);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    setting.OpenSesame = EditorGUILayout.ToggleLeft(s_OpenSesameText, setting.OpenSesame, GUILayout.MaxWidth(100));
                    settingChanged |= ccs.changed;
                }

                GUILayout.FlexibleSpace();

                EditorSettings.instance.IsSettingsOpend = GUILayout.Toggle(EditorSettings.instance.IsSettingsOpend, s_SettingsText, EditorStyles.miniButtonLeft);

                if (GUILayout.Button(s_PublishText, EditorStyles.miniButtonMid))
                {
                    EditorSettings.publishRequested = true;
                    EditorSettings.instance.PublishAssemblyName = GetAssemblyName(importer.assetPath);
                    Log("Request to publish the assembly as dll: {0}", EditorSettings.instance.PublishAssemblyName);
                    settingChanged = true;
                }

                if (GUILayout.Button(s_HelpText, EditorStyles.miniButtonRight))
                {
                    Application.OpenURL("https://github.com/mob-sakai/OpenSesameCompilerForUnity");
                }
            }

            if (EditorSettings.instance.IsSettingsOpend)
            {
                EditorGUI.indentLevel++;
                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    setting.ModifySymbols = EditorGUILayout.DelayedTextField(s_ModifySymbolsText, setting.ModifySymbols);
                    settingChanged |= ccs.changed;
                }

                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    setting.Optimize = EditorGUILayout.ToggleLeft(s_OptimizeText, setting.Optimize);
                    settingChanged |= ccs.changed;
                }

                using (new EditorGUI.DisabledScope(!setting.OpenSesame))
                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    bool portableMode = false;
                    if (!s_PortableModes.TryGetValue(importer.assetPath, out portableMode))
                    {
                        portableMode = GetFilesInAsmdef(importer.assetPath).Any(x => Path.GetFileName(x) == "OpenSesamePortable.cs");
                        s_PortableModes.Add(importer.assetPath, portableMode);
                    }

                    portableMode = EditorGUILayout.ToggleLeft(s_PortableModeText, portableMode);
                    if (ccs.changed)
                    {
                        s_PortableModes[importer.assetPath] = portableMode;
                        EditorApplication.delayCall += () =>
                        {
                            if (portableMode)
                                EnablePortableMode(importer.assetPath);
                            else
                                DisablePortableMode(importer.assetPath);
                        };
                    }
                }

                EditorGUI.indentLevel--;
            }

            if (settingChanged)
            {
                Log("OpenSesame setting for {0}: {1}", GetAssemblyName(importer.assetPath), JsonUtility.ToJson(setting));
                importer.userData = JsonUtility.ToJson(setting);
                importer.SaveAndReimport();
            }
        }

        static void EnablePortableMode(string asmdefPath)
        {
            AssetDatabase.StartAssetEditing();
            Log("Enable OpenSesame Portable Mode for {0}", GetAssemblyName(asmdefPath));

            foreach (var file in GetFilesInAsmdef(asmdefPath))
            {
                // Add '#if OPEN_SESAME' and '#endif' to the file
                Log("Add '#if OPEN_SESAME' and '#endif' to {0}", file);
                File.WriteAllText(file, kIfText + "\n" + File.ReadAllText(file) + "\n" + kEndIfText);
                AssetDatabase.ImportAsset(file);
            }

            // Download OpenSesamePortable.cs
            var url = "https://raw.githubusercontent.com/mob-sakai/OpenSesameCompilerForUnity/develop/Packages/com.coffee.open-sesame-compiler/Editor/Coffee.OpenSesame/OpenSesamePortable.cs";
            var downloadPath = Path.Combine(Path.GetDirectoryName(asmdefPath), "OpenSesamePortable.cs");
            Log("Download {0} from the repository", Path.GetDirectoryName(downloadPath));
            using (var client = new WebClient())
                client.DownloadFile(url, downloadPath);

            AssetDatabase.ImportAsset(downloadPath);

            AssetDatabase.StopAssetEditing();
        }

        static void DisablePortableMode(string asmdefPath)
        {
            AssetDatabase.StartAssetEditing();
            Log("Disable OpenSesame Portable Mode for {0}", GetAssemblyName(asmdefPath));

            foreach (var file in GetFilesInAsmdef(asmdefPath))
            {
                if (Path.GetFileName(file) == "OpenSesamePortable.cs")
                {
                    // Delete OpenSesamePortable.cs.
                    Log("Delete {0}", file);
                    AssetDatabase.DeleteAsset(file);
                }
                else
                {
                    // Remove '#if OPEN_SESAME' and '#endif' from the file.
                    Log("Remove '#if OPEN_SESAME' and '#endif' from {0}", file);
                    var text = File.ReadAllText(file);
                    text = Regex.Replace(text, kIfText + "[\r\n]+", "");
                    text = Regex.Replace(text, "[\r\n]+" + kEndIfText, "");
                    File.WriteAllText(file, text);
                    AssetDatabase.ImportAsset(file);
                }
            }

            AssetDatabase.StopAssetEditing();
        }

        static string GetAssemblyName(string asmdefPath)
        {
            try
            {
                return Regex.Match(File.ReadAllText(asmdefPath), "\"name\":\\s*\"([^\"]*)\"").Groups[1].Value;
            }
            catch
            {
                Debug.LogError(asmdefPath);
                return null;
            }
        }

        static string[] GetFilesInAsmdef(string asmdefPath)
        {
            try
            {
                return Core.GetScriptAssembly(GetAssemblyName(asmdefPath)).Get("Files") as string[];
            }
            catch
            {
                return new string[0];
            }
        }
    }
}
