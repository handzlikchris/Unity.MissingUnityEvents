using System;
using UnityEditor;
using UnityEngine;

namespace Assets.MissingUnityEvents.Editor
{
    public static class LayoutHelper
    {
        private class HorizontalDisposable: IDisposable
        {
            public void Dispose()
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        public static IDisposable Horizontal()
        {
            EditorGUILayout.BeginHorizontal();
            return new HorizontalDisposable();
        }


        private class LabelWidthDisposable : IDisposable
        {
            private int _widthPxBeforeChange;

            public LabelWidthDisposable(int widthPxBeforeChange)
            {
                _widthPxBeforeChange = widthPxBeforeChange;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = _widthPxBeforeChange;
            }
        }

        public static IDisposable LabelWidth(int widthPx)
        {
            var labelWidthDisposable = new LabelWidthDisposable((int)EditorGUIUtility.labelWidth);
            EditorGUIUtility.labelWidth = widthPx;

            return labelWidthDisposable;
        }

        public static void TextBoxWithButton(Func<string> getValue, Action<string> setValue, Action onButtonClick, string labelText, string buttonText = "Pick")
        {
            using (Horizontal())
            {
                var current = EditorGUILayout.TextField(labelText, getValue());
                setValue(current);
                if (GUILayout.Button(buttonText, GUILayout.Width(40)))
                {
                    onButtonClick();
                }
            }
        }
    }
}