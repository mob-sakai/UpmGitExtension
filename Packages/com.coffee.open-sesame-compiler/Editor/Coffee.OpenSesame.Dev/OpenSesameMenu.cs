using System.Linq;
using UnityEditor;

namespace Coffee.OpenSesame.Dev
{
    class OpenSesameMenu
    {
        const string kDevelopModeText = "OpenSesame/Develop Mode";
        const string kDevelopModeSymbol = "OPEN_SESAME_DEV";

        const string kEnableLoggingText = "OpenSesame/Enable Logging";
        const string kEnableLoggingSymbol = "OPEN_SESAME_LOG";

        const string kInstallDefaultCompilerText = "OpenSesame/Install Default Compiler";

        [MenuItem(kDevelopModeText, false)]
        static void DevelopMode()
        {
            SwitchSymbol(kDevelopModeSymbol);
        }

        [MenuItem(kDevelopModeText, true)]
        static bool DevelopMode_Valid()
        {
            Menu.SetChecked(kDevelopModeText, HasSymbol(kDevelopModeSymbol));
            return true;
        }

        [MenuItem(kEnableLoggingText, false)]
        static void EnableLogging()
        {
            SwitchSymbol(kEnableLoggingSymbol);
        }

        [MenuItem(kEnableLoggingText, true)]
        static bool EnableLogging_Valid()
        {
            Menu.SetChecked(kEnableLoggingText, HasSymbol(kEnableLoggingSymbol));
            return true;
        }

        static string[] GetSymbols()
        {
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';', ',');
        }

        static void SetSymbols(string[] symbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", symbols));
        }

        static bool HasSymbol(string symbol)
        {
            return GetSymbols().Any(x => x == symbol);
        }

        static void SwitchSymbol(string symbol)
        {
            var symbols = GetSymbols();
            SetSymbols(symbols.Any(x => x == symbol)
                ? symbols.Where(x => x != symbol).ToArray()
                : symbols.Concat(new[] { symbol }).ToArray()
            );
        }
    }
}
