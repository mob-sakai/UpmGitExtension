using System;
using System.Linq;
using UnityEditor;

namespace Coffee.UpmGitExtension.Dev
{
    class UpmGitExtensionMenu
    {
        const string kDevelopModeText = "UGE/Develop Mode";
        const string kDevelopModeSymbol = "UGE_DEV";

        const string kEnableLoggingText = "UGE/Enable Logging";
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

        static Type GetType(string fullname)
        {
            return Type.GetType(fullname + ", Coffee.UpmGitExtension") ?? Type.GetType(fullname + ", Coffee.UpmGitExtension.OSC");
        }

        [MenuItem("UGE/Update Display Versions")]
        static void UpdateDisplayVersions()
        {
            GetType("Coffee.UpmGitExtension.Bridge")
                .GetMethod("UpdateGitPackageVersions")
                .Invoke(null, new object[0]);

            GetType("Coffee.UpmGitExtension.Bridge")
                .GetMethod("UpdatePackageCollection")
                .Invoke(null, new object[0]);
        }

        [MenuItem("UGE/Update Cached Versions")]
        static void UpdateCachedVersions()
        {
            GetType("Coffee.UpmGitExtension.Bridge")
                .GetMethod("UpdateAvailableVersionsForGitPackages")
                .Invoke(null, new object[0]);
        }

        [MenuItem("UGE/Dump Cached Versions")]
        static void DumpCachedVersions()
        {
            GetType("Coffee.UpmGitExtension.AvailableVersions")
                .GetMethod("Dump")
                .Invoke(null, new object[0]);
        }

        [MenuItem("UGE/Clear Cached Versions")]
        static void ClearCachedVersions()
        {
            GetType("Coffee.UpmGitExtension.AvailableVersions")
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
