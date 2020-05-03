using System;
using UnityEditor;
using UnityEngine;

namespace Assets.MissingUnityEvents.Editor
{
    public abstract class ConfigPersistableEditorWindow<T> : EditorWindow where T: EditorWindowPersistableConfiguration<T>, new()
    {
        protected T _config;
        private bool _stopShowingErrorMessageOnGuiError;

        protected abstract void OnGUIInternal();

        void OnGUI()
        {
            if (_config == null)
            {
                _config = new T();
                _config = _config.LoadConfig();
            }

            try
            {
                OnGUIInternal();
            }
            catch (Exception e)
            {
                if (!_stopShowingErrorMessageOnGuiError)
                {
                    var result = EditorUtility.DisplayDialog("Error",
                        $"There were error executing OnGUI, this could be caused by editor configuration being incorrectly persisted, do you want to recreate?",
                        "Yes, recreate", "No and stop showing that message");

                    if (result)
                    {
                        RecreateConfig();
                    }
                    else
                    {
                        _stopShowingErrorMessageOnGuiError = true;
                    }
                }


                throw e;
            }

            if (GUI.changed)
            {
                _config.SaveChanges();
            }
        }

        public void RemoveConfig()
        {
            _config = new T();
            _config.RemoveFromPersistentStore();
        }

        public void RecreateConfig()
        {
            _config = new T();
            _config = _config.InitializeEmptyConfig();
        }
    }
}