﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.MissingUnityEvents.Editor
{
    [Serializable]
    public class EventConfiguration
    {
        public string ObjectType;
        public string PropertyName;
        public string DllName;

        public EventConfiguration(string objectType, string propertyName, string dllName)
        {
            ObjectType = objectType;
            PropertyName = propertyName;
            DllName = dllName;
        }

        public static EventConfiguration New()
        {
            return new EventConfiguration("<Type>", "<Property Name>", string.Empty);
        }
    }

    public class MissingUnityEventsManagerConfiguration : EditorWindowPersistableConfiguration<MissingUnityEventsManagerConfiguration>
    {
        public const string UnityCoreModuleDllName = "UnityEngine.CoreModule";
        public const string UnityPhysicsModuleDllName = "UnityEngine.PhysicsModule";

        public const string CustomAutoGeneratedEventsEnabledBuildSymbol = "ILWeavedEventsOn";
        private const string IlWeaverPluginExeName = "EventILWeaver.Console";

        public string IlWeaverPluginExecutablePath;
        public string HelperClassFilePath;
        public string HelperClassNamespace;
        public bool HelperClassForceUseNoBuildSymbolInEditor;
        public string HelperClassIncludeCustomCodeWhenNoBuildSymbol = $"Debug.LogWarning(\"Build symbol {CustomAutoGeneratedEventsEnabledBuildSymbol} not specified.\");";

        public List<EventConfiguration> EventConfigurationEntries = new List<EventConfiguration>();

        protected override void InitEmpty()
        {
            EventConfigurationEntries = new List<EventConfiguration>
            {
                new EventConfiguration(typeof(Transform).Name, nameof(Transform.position), UnityCoreModuleDllName),
                new EventConfiguration(typeof(Transform).Name, nameof(Transform.localScale), UnityCoreModuleDllName),
                new EventConfiguration(typeof(Transform).Name, nameof(Transform.rotation), UnityCoreModuleDllName),
                new EventConfiguration(typeof(BoxCollider).Name, nameof(BoxCollider.name), UnityPhysicsModuleDllName),
            };
        }

        public string GetIlWeaverPluginFolder()
        {
            var thisFilePath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(EditorWindow.GetWindow<MissingUnityEventsManagerEditorWindow>()));
            var ilWeaverPluginFolder = thisFilePath.Substring(0, thisFilePath.LastIndexOf("/")) + "/Plugins~";
            return ilWeaverPluginFolder;
        }
    }
}