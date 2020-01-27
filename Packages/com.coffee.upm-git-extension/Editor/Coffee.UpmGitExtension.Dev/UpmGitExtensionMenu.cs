using System;
using System.Linq;
using UnityEditor;

namespace Coffee.UpmGitExtension.Dev
{
    class UpmGitExtensionMenu
    {
        const string kDevelopModeText = "UpmGitExtension/Develop Mode";
        const string kDevelopModeSymbol = "UGE_DEV";

        const string kEnableLoggingText = "UpmGitExtension/Enable Logging";
        const string kEnableLoggingSymbol = "UGE_LOG";

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

        [MenuItem("UpmGitExtension/Update Git Package Versions")]
        static void UpdateGitPackageVersions()
        {
            Type.GetType("Coffee.UpmGitExtension.Bridge, Coffee.UpmGitExtension")
                .GetMethod("UpdateGitPackageVersions")
                .Invoke(null, new object[0]);
        }

        [MenuItem("UpmGitExtension/Clear Cached Versions")]
        static void ClearCachedVersions()
        {
            Type.GetType("Coffee.UpmGitExtension.AvailableVersions, Coffee.UpmGitExtension")
                .GetMethod("ClearAll")
                .Invoke(null, new object[0]);
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
