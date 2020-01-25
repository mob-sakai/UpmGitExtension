#if UNITY_EDITOR && !OPEN_SESAME
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Coffee.OpenSesamePortable
{
    internal static class ReflectionExtensions
    {
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static object Inst(this object self)
        {
            return (self is Type) ? null : self;
        }

        static Type Type(this object self)
        {
            return (self as Type) ?? self.GetType();
        }

        public static object New(this Type self, params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return self.Type().GetConstructor(types)
                .Invoke(args);
        }

        public static object Call(this object self, string methodName, params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return self.Type().GetMethod(methodName, types)
                .Invoke(self.Inst(), args);
        }

        public static object Call(this object self, Type[] genericTypes, string methodName, params object[] args)
        {
            return self.Type().GetMethod(methodName, FLAGS)
                .MakeGenericMethod(genericTypes)
                .Invoke(self.Inst(), args);
        }

        public static object Get(this object self, string memberName, MemberInfo mi = null)
        {
            mi = mi ?? self.Type().GetMember(memberName, FLAGS)[0];
            return mi is PropertyInfo
                ? (mi as PropertyInfo).GetValue(self.Inst(), new object[0])
                : (mi as FieldInfo).GetValue(self.Inst());
        }

        public static void Set(this object self, string memberName, object value, MemberInfo mi = null)
        {
            mi = mi ?? self.Type().GetMember(memberName, FLAGS)[0];
            if (mi is PropertyInfo)
                (mi as PropertyInfo).SetValue(self.Inst(), value, new object[0]);
            else
                (mi as FieldInfo).SetValue(self.Inst(), value);
        }
    }

    internal class Core
    {
        const string kCompilerVersion = "3.4.0";
        public static bool logEnabled;
        static string kLogHeader = "";

        static Core()
        {
            if (PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split(';', ',')
                .Any(x => x == "OPEN_SESAME_LOG"))
            {
                var assemblyName = typeof(Core).Assembly.GetName().Name;
                logEnabled = true;
                kLogHeader = string.Format("<b><color=#9a4089>[OpenSesamePortable.Core ({0})]</color></b> ", assemblyName);
            }
        }

        static void Log(string format, params object[] args)
        {
            if (logEnabled)
                UnityEngine.Debug.LogFormat(kLogHeader + format, args);
        }

        public static string GetInstalledCompiler()
        {
            return InstallCompiler(kCompilerVersion);
        }

        static string InstallCompiler(string version)
        {
            string packageId = "OpenSesameCompiler." + version;
            string url = "https://globalcdn.nuget.org/packages/" + packageId.ToLower() + ".nupkg";
            string dowloadPath = Path.GetTempFileName() + ".nuget";
            string installPath = ("Library/" + packageId).Replace('/', Path.DirectorySeparatorChar);
            string cscToolExe = (installPath + "/tools/csc.exe").Replace('/', Path.DirectorySeparatorChar);

            // OpenSesame compiler is already installed.
            if (File.Exists(cscToolExe))
            {
                Log("{0} is already installed at {1}", packageId, cscToolExe);
                return cscToolExe;
            }

            if (Directory.Exists(installPath))
                Directory.Delete(installPath, true);

            // Download csc from nuget.
            UnityEngine.Debug.LogFormat(kLogHeader + "Download {0} from nuget: {1}", packageId, url);
            try
            {
                using (var client = new WebClient())
                    client.DownloadFile(url, dowloadPath);
            }
            catch
            {
                using (var client = new WebClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback += OnServerCertificateValidation;
                    client.DownloadFile(url, dowloadPath);
                }
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback -= OnServerCertificateValidation;
            }

            // Extract zip.
            string args = string.Format("x {0} -o{1}", dowloadPath, installPath);
            string exe = Path.Combine(EditorApplication.applicationContentsPath,
                Application.platform == RuntimePlatform.WindowsEditor ? "Tools\\7z.exe" : "Tools/7za");
            UnityEngine.Debug.LogFormat(kLogHeader + "Extract {0} to {1} with 7z command: {2} {3}", dowloadPath, installPath, exe, args);
            Process.Start(exe, args).WaitForExit();

            if (File.Exists(cscToolExe))
                return cscToolExe;

            throw new Exception(kLogHeader + "Open Sesame compiler is not found at " + cscToolExe);
        }

        private static bool OnServerCertificateValidation(object _, X509Certificate __, X509Chain ___, SslPolicyErrors ____)
        {
            return true;
        }

        public static object GetScriptAssembly(string assemblyName)
        {
            Type tEditorCompilationInterface = Type.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface, UnityEditor");
            Type tCSharpLanguage = Type.GetType("UnityEditor.Scripting.Compilers.CSharpLanguage, UnityEditor");
            return tEditorCompilationInterface.Call(new[] { tCSharpLanguage }, "GetScriptAssemblyForLanguage", assemblyName);
        }

        public static void GetOpenSesameSettings(string asmdefPath, out bool openSesame, out string modifySymbols, out bool optimize)
        {
            openSesame = false;
            modifySymbols = "";
            optimize = false;
            if (string.IsNullOrEmpty(asmdefPath))
                return;

            if (Directory.Exists(asmdefPath))
                asmdefPath = Directory.GetFiles(asmdefPath, "*.asmdef")
                    .Select(x => x.Replace(Environment.CurrentDirectory + Path.DirectorySeparatorChar, ""))
                    .FirstOrDefault();

            if (string.IsNullOrEmpty(asmdefPath) || !File.Exists(asmdefPath) || !File.Exists(asmdefPath + ".meta"))
                return;

            try
            {
                var json = AssetImporter.GetAtPath(asmdefPath).userData;
                GetOpenSesameSettingsFromJson(json, out openSesame, out modifySymbols, out optimize);
            }
            catch { }
        }

        public static void GetOpenSesameSettingsFromJson(string json, out bool openSesame, out string modifySymbols, out bool optimize)
        {
            openSesame = false;
            modifySymbols = "";
            optimize = false;
            if (string.IsNullOrEmpty(json))
                return;

            openSesame = Regex.Match(json, "\"OpenSesame\":\\s*(true|false)").Groups[1].Value == "true";
            modifySymbols = Regex.Match(json, "\"ModifySymbols\":\\s*\"([^\"]*)\"").Groups[1].Value;
            optimize = Regex.Match(json, "\"Optimize\":\\s*(true|false)").Groups[1].Value == "true";
        }

        public static string[] ModifyDefines(IEnumerable<string> defines, bool openSesame, string modifySymbols, bool optimize)
        {
            var symbols = modifySymbols.Split(';', ',');
            var add = symbols.Where(x => 0 < x.Length && !x.StartsWith("!"));
            var remove = symbols.Where(x => 1 < x.Length && x.StartsWith("!")).Select(x => x.Substring(1));
            return defines
                .Union(add ?? Enumerable.Empty<string>())
                .Except(remove ?? Enumerable.Empty<string>())
                .Union(openSesame ? new[] { "OPEN_SESAME" } : Enumerable.Empty<string>())
                .Except(optimize ? new[] { "DEBUG", "TRACE" } : Enumerable.Empty<string>())
                .Distinct()
                .ToArray();
        }

        public static object BeginCompileAssembly(string assemblyName, string outFilename = null)
        {
            Type tMicrosoftCSharpCompiler = Type.GetType("UnityEditor.Scripting.Compilers.MicrosoftCSharpCompiler, UnityEditor");
            Type tEditorScriptCompilationOptions = Type.GetType("UnityEditor.Scripting.ScriptCompilation.EditorScriptCompilationOptions, UnityEditor");
            object options = Enum.Parse(tEditorScriptCompilationOptions, "BuildingForEditor");

            //  -> EditorCompilationInterface.GetScriptAssemblyForLanguage<CSharpLanguage>(assemblyName);
            var scriptAssembly = GetScriptAssembly(assemblyName);
            if (scriptAssembly == null)
                throw new Exception(string.Format(kLogHeader + "ScriptAssembly '{0}' is not exist.", assemblyName));

            // Get open sesame settings.
            bool openSesame;
            string modifySymbols;
            bool optimize;
            GetOpenSesameSettings(scriptAssembly.Get("OriginPath") as string, out openSesame, out modifySymbols, out optimize);

            // 
            if (outFilename != null)
            {
                scriptAssembly.Set("Filename", outFilename);
                openSesame = true;
            }

            // Modify script defines.
            var defines = ModifyDefines(scriptAssembly.Get("Defines") as string[], openSesame, modifySymbols, optimize);
            scriptAssembly.Set("Defines", defines);

            Log("Create compiler for {0}", assemblyName);
            object compiler;
            if (string.Compare("2020.1", Application.unityVersion) < 0)
                // << Unity 2020.1 or later >>
                compiler = tMicrosoftCSharpCompiler.New(scriptAssembly, "Temp");
            else if (string.Compare("2019.3", Application.unityVersion) < 0)
                // << Unity 2019.3 or later >>
                compiler = tMicrosoftCSharpCompiler.New(scriptAssembly, options, "Temp");
            else
                // << Unity 2019.2 or earlier >>
                compiler = tMicrosoftCSharpCompiler.New(scriptAssembly.Call("ToMonoIsland", options, "Temp", ""), true);

            Log("Compile assembly for {0}", Path.GetFileName(assemblyName));
            compiler.Call("BeginCompiling");

            if (openSesame || optimize)
                ChangeCompilerProcess(compiler, openSesame ? GetInstalledCompiler() : null, optimize);

            return compiler;
        }

        static void ChangeCompilerProcess(object compiler, string cscToolExe = null, bool optimize = false)
        {
            Type tProgram = Type.GetType("UnityEditor.Utils.Program, UnityEditor");
            Type tScriptCompilerBase = Type.GetType("UnityEditor.Scripting.Compilers.ScriptCompilerBase, UnityEditor");
            FieldInfo fiProcess = tScriptCompilerBase.GetField("process", BindingFlags.NonPublic | BindingFlags.Instance);

            Log("Kill previous compiler process");
            var psi = compiler.Get("process", fiProcess).Call("GetProcessStartInfo") as ProcessStartInfo;
            compiler.Call("Dispose");

            string responseFile = Regex.Replace(psi.Arguments, "^.*@(.+)$", "$1");
            if (optimize)
            {
                Log("With optimize option: {0}", responseFile);
                var text = File.ReadAllText(responseFile);
                text = Regex.Replace(text, "^/optimize-$", "/optimize+", RegexOptions.Multiline);
                text = Regex.Replace(text, "^/debug:\\w*$", "", RegexOptions.Multiline);
                File.WriteAllText(responseFile, text);
            }

            if (!string.IsNullOrEmpty(cscToolExe))
            {
                Log("Change csc tool exe to {0}", cscToolExe);
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    psi.FileName = Path.GetFullPath(cscToolExe);
                    psi.Arguments = "/shared /noconfig @" + responseFile;
                }
                else
                {
                    psi.FileName = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin/mono");
                    psi.Arguments = cscToolExe + " /noconfig @" + responseFile;
                }
            }


            Log("Restart compiler process: {0} {1}", psi.FileName, psi.Arguments);
            var program = tProgram.New(psi);
            program.Call("Start");
            compiler.Set("process", program, fiProcess);
        }
    }

    [InitializeOnLoad]
    class Bootstrap
    {
        static string kLogHeader = "";

        static void Log(string format, params object[] args)
        {
            if (Core.logEnabled)
                UnityEngine.Debug.LogFormat(kLogHeader + format, args);
        }

        static void Warning(string format, params object[] args)
        {
            if (Core.logEnabled)
                UnityEngine.Debug.LogWarningFormat(kLogHeader + format, args);
        }

        static Bootstrap()
        {
            var assemblyName = typeof(Bootstrap).Assembly.GetName().Name;
            kLogHeader = string.Format("<b><color=#c7634c>[OpenSesamePortable.Bootstrap ({0})]</color></b> ", assemblyName);

            if (assemblyName == "Coffee.OpenSesame")
            {
                Warning("This assembly is OpenSesame. Skip portable bootstrap task.");
                return;
            }

            try
            {
                // Compile this assembly with OpenSesame compiler.
                Log("<<<< <b>Recompile {0} with OpenSesame compiler.</b>", assemblyName);
                var outpath = Path.GetFullPath("Temp/" + assemblyName + ".OSC.dll");
                var compiler = Core.BeginCompileAssembly(assemblyName, Path.GetFileName(outpath));

                compiler.Call("WaitForCompilationToFinish");
                var messages = compiler.Call("GetCompilerMessages") as IEnumerable;
                compiler.Call("Dispose");
                compiler = null;

                foreach (var m in messages.Cast<object>())
                {
                    if ((int)m.Get("type") == 0)
                        UnityEngine.Debug.LogError(m.Get("message"));
                    else
                        UnityEngine.Debug.LogWarning(m.Get("message"));
                }

                // Load and Initialize the compiled assembly.
                var tmp = Path.GetTempFileName() + ".dll";
                File.Move(outpath, tmp);
                InitializeAssemblyOnLoad(Assembly.LoadFrom(tmp));

                //File.Copy(tmp, "Library/ScriptAssemblies/" + assemblyName + ".dll", true);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(new Exception(kLogHeader + e.Message, e.InnerException));
            }
        }

        public static void InitializeAssemblyOnLoad(Assembly assembly)
        {
            Log("Initialize assembly on load: {0}", assembly.FullName);
            var types = assembly.GetTypes();
            foreach (var type in types
                .Where(x => 0 < x.GetCustomAttributes(typeof(InitializeOnLoadAttribute), false).Length))
            {
                try
                {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(new Exception(kLogHeader + e.Message, e.InnerException));
                }
            }

            foreach (var method in types
                .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .Where(x => 0 < x.GetCustomAttributes(typeof(InitializeOnLoadMethodAttribute), false).Length))
            {
                try
                {
                    method.Invoke(null, null);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(new Exception(kLogHeader + e.Message, e.InnerException));
                }
            }
        }
    }
}
#endif