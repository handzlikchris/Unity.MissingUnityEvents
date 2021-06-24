using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using File = UnityEngine.Windows.File;

namespace Assets.MissingUnityEvents.Editor
{
    public class MissingUnityEventsManagerEditorWindow : ConfigPersistableEditorWindow<MissingUnityEventsManagerConfiguration>
    {
        private const string PluginExecutablePathLabel = "Plugin Executable Path";

        private int _windowWidthPx;

        [MenuItem("Tools/Missing Unity Events/Run")]
        public static void ExecuteShowWindowMenuAction()
        {
            GetWindow<MissingUnityEventsManagerEditorWindow>(false, "Missing Unity Events");
        }

        [MenuItem("Tools/Missing Unity Events/Recreate Config")]
        public static void ExecuteRecreateConfigMenuAction()
        {
            var editor = GetWindow<MissingUnityEventsManagerEditorWindow>(false, "Missing Unity Events");
            editor.RecreateConfig();
        }

        //[MenuItem("Missing Unity Events/Remove Config")]
        //public static void ExecuteRemoveConfigMenuAction()
        //{
        //    var editor = GetWindow<MissingUnityEventsManagerEditorWindow>(false, "Missing Unity Events");
        //    editor.RemoveConfig();
        //}

        private object _typeToPropertiesMapGeneratingLock = new object();
        private Dictionary<Type, List<PropertyInfo>> _typeToPropertiesMap;
        public Dictionary<Type, List<PropertyInfo>> TypeToPropertiesMap
        {
            get
            {
                if (_typeToPropertiesMap == null)
                {
                    _typeToPropertiesMap = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("Unity")).ToList()
                        .SelectMany(a => a.GetTypes()).ToDictionary(t => t,
                            t => t.GetProperties(BindingFlags.Public
                                                 | BindingFlags.Instance
                                                 | BindingFlags.DeclaredOnly)
                                .Where(p => p.CanRead && p.CanWrite
                                                      && p.GetMethod?.MethodImplementationFlags == MethodImplAttributes.Managed &&
                                                      p.SetMethod?.MethodImplementationFlags == MethodImplAttributes.Managed).ToList()
                        );
                }
                return _typeToPropertiesMap;
            }
        }

        void Awake()
        {
        }

        private string GetAssemblyName(Assembly assy)
        {
            return assy.FullName.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)[0];
        }

        protected override void OnGUIInternal()
        {
            _windowWidthPx = Screen.width;

            GUILayout.Label("Plugin Configuration", EditorStyles.boldLabel);

            LayoutHelper.TextBoxWithButton(() => _config.IlWeaverPluginExecutablePath, (v) => _config.IlWeaverPluginExecutablePath = v, SelectPluginViaPicker, PluginExecutablePathLabel);
            GUILayout.Space(10);

            GUILayout.Label("Event Configuration", EditorStyles.boldLabel);

            EditorGUILayout.SelectableLabel("Event addition is quite flexible, you can add more targets based on type, property name and " +
                                            "property type, plugin will try to weave those.", EditorStyles.textArea);
            GUILayout.Space(10);

            CreateTargetDefinitionEditor();

            if (GUILayout.Button("Add New Event", GUILayout.Width(100))) _config.EventConfigurationEntries.Add(EventConfiguration.New());
            
            if (GUILayout.Button("Weave Events to DLL", GUILayout.Height(40)))
            {
                ExecuteWeaveEventsToDll();
            }
            if (GUILayout.Button("Revert DLL to original", GUILayout.Height(40)))
            {
                ExecuteRevertDllToOriginal();
            }
            GUILayout.Space(10);


            GUILayout.Label("Helper classes", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel("It's best to abstract usage of weaved events. This way if you're using clean dll " +
                                            "you won't have to change your existing code, instead it'll just log warning if specified.", EditorStyles.textArea);

            LayoutHelper.TextBoxWithButton(() => _config.HelperClassFilePath, (v) => _config.HelperClassFilePath = v, SelectHelperFileViaPicker, "Helper Class File Path");
            _config.HelperClassNamespace = EditorGUILayout.TextField("Namespace", _config.HelperClassNamespace);
            _config.HelperClassIncludeCustomCodeWhenNoBuildSymbol = EditorGUILayout.TextField("Custom Code When no Build Symbol", _config.HelperClassIncludeCustomCodeWhenNoBuildSymbol);

            using (LayoutHelper.LabelWidth(200)) _config.HelperClassForceUseNoBuildSymbolInEditor = EditorGUILayout.Toggle("Force no build symbol (in editor)", _config.HelperClassForceUseNoBuildSymbolInEditor);

            BuildDefineSymbolManager.SetBuildDefineSymbolState(MissingUnityEventsManagerConfiguration.CustomAutoGeneratedEventsEnabledBuildSymbol, !_config.HelperClassForceUseNoBuildSymbolInEditor);
            
            if (GUILayout.Button("Generate Helper Classes", GUILayout.Height(40)))
            {
                ExecuteGenerateHelperClasses();
            }
            GUILayout.Space(10);
        }

        private void EnsureEventConfigEntriesHaveCorrectDllModuleHydrated()
        {
            foreach (var eventConfigEventConfigurationEntry in _config.EventConfigurationEntries)
            {
                eventConfigEventConfigurationEntry.DllName = GetAssemblyName(
                    TypeToPropertiesMap.First(t => t.Key.Name == eventConfigEventConfigurationEntry.ObjectType).Key.Assembly
                );
            }
        }

        private void CreateTargetDefinitionEditor()
        {
            using (LayoutHelper.Horizontal())
            {
                GUILayout.Label("Type", PercentageWidth(40));
                GUILayout.Label("Property Name", PercentageWidth(40));
            }

            for (var i = _config.EventConfigurationEntries.Count - 1; i >= 0; i--)
            {
                var eventConfigurationEntry = _config.EventConfigurationEntries[i];
                using (LayoutHelper.Horizontal())
                {
                    eventConfigurationEntry.ObjectType =
                        EditorGUILayout.TextField("", eventConfigurationEntry.ObjectType, PercentageWidth(40));

                    var typeProperties =
                        TypeToPropertiesMap.FirstOrDefault(kv => kv.Key.Name == eventConfigurationEntry.ObjectType);
                    if (typeProperties.Key != null)
                    {
                        var selectedTypePropertyIndex = -1;
                        var allTypeProperties = typeProperties.Value.Select(p => p.Name).ToArray();
                        for (var j = 0; j < allTypeProperties.Length; j++)
                        {
                            if (allTypeProperties[j] == eventConfigurationEntry.PropertyName)
                            {
                                selectedTypePropertyIndex = j;
                                break;
                            }
                        }

                        selectedTypePropertyIndex =
                            EditorGUILayout.Popup(selectedTypePropertyIndex, allTypeProperties, PercentageWidth(40));
                        if (selectedTypePropertyIndex != -1)
                        {
                            eventConfigurationEntry.PropertyName = allTypeProperties[selectedTypePropertyIndex];
                        }
                    }
                    else
                    {
                        EditorGUILayout.TextField("", "<Type Not Found>", PercentageWidth(40));
                    }

                    if (GUILayout.Button("-", PercentageWidth(8)))
                    {
                        _config.EventConfigurationEntries.RemoveAt(i);
                    }
                }
            }
        }

        public GUILayoutOption PercentageWidth(int percentageWidth)
        {
            var pxWidth = percentageWidth / 100f * _windowWidthPx;
            return GUILayout.Width(pxWidth);
        }

        private void ExecuteGenerateHelperClasses()
        {
            if (string.IsNullOrWhiteSpace(_config.HelperClassNamespace))
            {
                EditorUtility.DisplayDialog("Error", $"Namespace is required", "Ok");
            }
            else
            {
                EnsureEventConfigEntriesHaveCorrectDllModuleHydrated();
                RunWeavingExecutable(EventILWeaverCommandLineArgsGenerator.GenerateCreateHelperClassesArgs(_config), false);
            }
        }

        private void ExecuteRevertDllToOriginal()
        {
            EnsurePluginSelected();
            EnsureEventConfigEntriesHaveCorrectDllModuleHydrated();
            RunWeavingExecutable(EventILWeaverCommandLineArgsGenerator.GenerateRevertToOriginalCommandLineArgs(_config), true);
            _config.HelperClassForceUseNoBuildSymbolInEditor = true;
        }

        private void ExecuteWeaveEventsToDll()
        {
            EnsurePluginSelected();

            EnsureEventConfigEntriesHaveCorrectDllModuleHydrated();
            RunWeavingExecutable(EventILWeaverCommandLineArgsGenerator.GenerateAddEventsCommandLineArgs(_config), true);

            _config.HelperClassForceUseNoBuildSymbolInEditor = false;

            var result = EditorUtility.DisplayDialog("Warning",
                $"Continue in console window, you'll likely need to close Editor for weaving to complete.\r\n\r\n" +
                $"Weaving events for some types/properties (eg. Object.name) will crash Editor, it's possible more type/property combinations will cause similar issues.\r\n\r\n " +
                $"A rollback script will be created and kept in same folder as plugin.\r\n" +
                $" '{_config.IlWeaverPluginExecutablePath}'\r\n\r\n" +
                $"If any issues occur run it to revert to backups.", "Close editor", "Keep open");

            if (result)
            {
                EditorApplication.Exit(0);
            }
        }

        private void EnsurePluginSelected()
        {
            if (!File.Exists(_config.IlWeaverPluginExecutablePath))
            {
                EditorUtility.DisplayDialog("Error",
                    $"Unable to find plugin, please make sure that '{PluginExecutablePathLabel}' is correct.", "Pick file");
                SelectPluginViaPicker();
            }
        }

        private void RunWeavingExecutable(string commandLineArgs, bool runAsAdministrator)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = _config.IlWeaverPluginExecutablePath;
            proc.StartInfo.UseShellExecute = true;
            if (runAsAdministrator) proc.StartInfo.Verb = "runas";
            Debug.Log($"Running weaving DLL with arguments: '{commandLineArgs}'");
            proc.StartInfo.Arguments = commandLineArgs;
            proc.Start();
        }

        private void SelectPluginViaPicker()
        {
            _config.IlWeaverPluginExecutablePath = EditorUtility.OpenFilePanel("Select Plugin exe",  _config.GetIlWeaverPluginFolder(), "exe");
        }

        private void SelectHelperFileViaPicker()
        {
            _config.HelperClassFilePath = EditorUtility.OpenFilePanel("Select Helper class file (create empty if needed)", Application.dataPath, "cs");
        }


    }
}
