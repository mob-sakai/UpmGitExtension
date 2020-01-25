using System.IO;
using System.Text.RegularExpressions;
using Coffee.OpenSesamePortable;
using UnityEditor;
using UnityEditor.Compilation;

namespace Coffee.OpenSesame
{
    internal class CSProjectGenerator : AssetPostprocessor
    {
        static void Log(string format, params object[] args)
        {
            if (Core.logEnabled)
                UnityEngine.Debug.LogFormat("<color=#0063b1><b>[CSProjectGenerator]</b></color> " + format, args);
        }

        static string OnGeneratedCSProject(string path, string content)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(path);
            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
            var setting = OpenSesameSetting.GetAtPathOrDefault(asmdefPath);
            if (string.IsNullOrEmpty(setting.ModifySymbols) && !setting.OpenSesame)
                return content;

            var defines = Regex.Match(content, "<DefineConstants>(.*)</DefineConstants>").Groups[1].Value.Split(';', ',');
            defines = Core.ModifyDefines(defines, setting.OpenSesame, setting.ModifySymbols, setting.Optimize);
            var defineText = string.Join(";", defines);

            Log("Script defines in {0}.csproj are modified:\n{1}", assemblyName, defineText);
            return Regex.Replace(content, "<DefineConstants>(.*)</DefineConstants>", string.Format("<DefineConstants>{0}</DefineConstants>", defineText));
        }
    }
}
