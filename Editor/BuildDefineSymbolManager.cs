using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Assets.MissingUnityEvents.Editor
{
    public static class BuildDefineSymbolManager
    {
        public static void SetBuildDefineSymbolState(string buildSymbol, bool isEnabled)
        {
            var allBuildSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split(';').ToList();
            var isBuildSymbolDefined = allBuildSymbols.Any(s => s == buildSymbol);

            if (isEnabled && !isBuildSymbolDefined)
            {
                allBuildSymbols.Add(buildSymbol);
                SetBuildSymbols(allBuildSymbols);
            }

            if (!isEnabled && isBuildSymbolDefined)
            {
                allBuildSymbols.Remove(buildSymbol);
                SetBuildSymbols(allBuildSymbols);
            }
        }

        private static void SetBuildSymbols(List<string> allBuildSymbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", allBuildSymbols.ToArray())
            );
        }
    }
}