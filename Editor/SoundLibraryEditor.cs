using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace KidzDev.Unity.Audio.Editor
{
    [CustomEditor(typeof(SoundLibrary))]
    internal sealed class SoundLibraryEditor : UnityEditor.Editor
    {
        SerializedProperty _entriesProp;
        ReorderableList    _list;

        void OnEnable()
        {
            _entriesProp = serializedObject.FindProperty("_entries");
            BuildList();
        }

        void BuildList()
        {
            _list = new ReorderableList(
                serializedObject, _entriesProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _list.drawHeaderCallback    = rect => EditorGUI.LabelField(rect, "Sound Entries");
            _list.elementHeightCallback = index =>
                EditorGUI.GetPropertyHeight(_entriesProp.GetArrayElementAtIndex(index), true);
            _list.drawElementCallback   = DrawElement;
        }

        void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var elem = _entriesProp.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, elem, true);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var dupes = FindDuplicateKeys();
            if (dupes.Count > 0)
                EditorGUILayout.HelpBox(
                    $"Duplicate keys: {string.Join(", ", dupes)}\nKeys must be unique.",
                    MessageType.Error);

            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        HashSet<string> FindDuplicateKeys()
        {
            var seen  = new HashSet<string>();
            var dupes = new HashSet<string>();
            for (int i = 0; i < _entriesProp.arraySize; i++)
            {
                var keyProp = _entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Key");
                if (keyProp == null) continue;
                var key = keyProp.stringValue;
                if (string.IsNullOrEmpty(key)) continue;
                if (!seen.Add(key)) dupes.Add(key);
            }
            return dupes;
        }
    }
}
