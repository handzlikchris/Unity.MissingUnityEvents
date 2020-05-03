using UnityEditor;
using UnityEngine;

namespace Assets.MissingUnityEvents.Editor
{
    public abstract class EditorWindowPersistableConfiguration<T> where T: EditorWindowPersistableConfiguration<T>, new()
    {
        private static string CurrentlyPersistedValue;
        private static readonly string EditorPrefsKey = $"EditorWindowPersistableConfiguration_{typeof(T).Name}";

        protected abstract void InitEmpty();

        public void SaveChanges()
        {
            var json = JsonUtility.ToJson(this);
            if (json != CurrentlyPersistedValue)
            {
                EditorPrefs.SetString(EditorPrefsKey, json);
            }
        }

        public T LoadConfig()
        {
            CurrentlyPersistedValue = EditorPrefs.GetString(EditorPrefsKey);
            if (!string.IsNullOrEmpty(CurrentlyPersistedValue))
            {
                return JsonUtility.FromJson<T>(CurrentlyPersistedValue);
            }
            else
            {
                return InitializeEmptyConfig();
            }
        }

        public T InitializeEmptyConfig()
        {
            var settings = new T();
            settings.InitEmpty();
            settings.SaveChanges();
            return settings;
        }

        public void RemoveFromPersistentStore()
        {
            EditorPrefs.DeleteKey(EditorPrefsKey);
        }
    }
}