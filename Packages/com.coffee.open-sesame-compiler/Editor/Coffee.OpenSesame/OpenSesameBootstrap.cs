using System;
using System.Collections;
using System.IO;
using System.Linq;
using Coffee.OpenSesamePortable;
using UnityEditor;
using UnityEditor.Compilation;

namespace Coffee.OpenSesame
{
    [InitializeOnLoad]
    internal class Bootstrap
    {
        const string kLogHeader = "<color=#c34062><b>[OpenSesameBootstrap]</b></color> ";
        static readonly string[] kIgnoredAssemblyNames = {
            "Coffee.OpenSesame",
            "OpenSesamePortableTests",
        };

        static void Log(string format, params object[] args)
        {
            if (Core.logEnabled)
                UnityEngine.Debug.LogFormat(kLogHeader + format, args);
        }

        static bool ShouldChangeCompilerProcess(string assemblyName)
        {
            if (kIgnoredAssemblyNames.Contains(assemblyName))
                return false;

            var scriptAssembly = Core.GetScriptAssembly(assemblyName);
            if (scriptAssembly == null)
                return false;

            var originPath = scriptAssembly.Get("OriginPath") as string;
            var setting = OpenSesameSetting.GetAtPathOrDefault(originPath);
            return setting.OpenSesame || !string.IsNullOrEmpty(setting.ModifySymbols) || setting.Optimize;
        }

        static void OnAssemblyCompilationStarted(string name)
        {
            try
            {
                string assemblyName = Path.GetFileNameWithoutExtension(name);
                string assemblyFilename = assemblyName + ".dll";

                // Should change compiler process for the assembly?
                if (!ShouldChangeCompilerProcess(assemblyName))
                {
                    Log("<<<< Assembly compilation started: <i>{0} should not be recompiled.</i>", assemblyName);
                    return;
                }

                Log("<<<< Assembly compilation started: <b>{0} should be recompiled.</b>", assemblyName);
                Type tEditorCompilationInterface = Type.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface, UnityEditor");
                var compilerTasks = tEditorCompilationInterface.Get("Instance").Get("compilationTask").Get("compilerTasks") as IDictionary;
                var scriptAssembly = compilerTasks.Keys.Cast<object>().FirstOrDefault(x => (x.Get("Filename") as string) == assemblyFilename);
                var compiler = compilerTasks[scriptAssembly];

                // Create new compiler to recompile.
                compiler.Call("Dispose");
                compilerTasks[scriptAssembly] = Core.BeginCompileAssembly(assemblyName);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(new Exception(kLogHeader + e.Message, e.InnerException));
            }
        }

        static void OnAssemblyCompilationFinished(string name, CompilerMessage[] messages)
        {
            try
            {
                // This assembly is requested to publish?
                string assemblyName = Path.GetFileNameWithoutExtension(name);
                if (!EditorSettings.publishRequested || EditorSettings.instance.PublishAssemblyName != assemblyName)
                    return;

                EditorSettings.instance.PublishAssemblyName = null;
                Log(">>>> Assembly compilation finished: <b>{0} is requested to publish.</b>", assemblyName);

                // No compilation error?
                if (messages.Any(x => x.type == CompilerMessageType.Error))
                    return;

                var scriptAssembly = Core.GetScriptAssembly(assemblyName);
                var originPath = scriptAssembly.Get("OriginPath") as string;

                // Publish a dll to parent directory.
                var dst = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(originPath)), assemblyName + ".dll");
                var src = "Library/ScriptAssemblies/" + Path.GetFileName(dst);
                UnityEngine.Debug.LogFormat("<b>[OpenSesame]</b> Publish assembly as dll: {0} -> {1}", src, dst);
                File.Copy(src, dst, true);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(new Exception(kLogHeader + e.Message, e.InnerException));
            }
        }

        static Bootstrap()
        {
            Log("Watch assembly compilation...");
            CompilationPipeline.assemblyCompilationStarted += OnAssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }
    }
}
