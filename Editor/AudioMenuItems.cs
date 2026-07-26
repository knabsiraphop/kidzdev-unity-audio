using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KidzDev.Unity.Audio.Editor
{
    internal static class AudioMenuItems
    {
        [MenuItem("Tools/Audio/Create Settings")]
        static void CreateSettings()
        {
            const string dir  = "Assets/Resources";
            const string path = dir + "/AudioServiceSettings.asset";
            EnsureDir(dir);

            var existing = AssetDatabase.LoadAssetAtPath<AudioServiceSettings>(path);
            if (existing != null) { PingAndSelect(existing); return; }

            var asset = ScriptableObject.CreateInstance<AudioServiceSettings>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            PingAndSelect(asset);
        }

        [MenuItem("Tools/Audio/Create Sound Library")]
        static void CreateSoundLibrary()
        {
            const string dir  = "Assets/Resources";
            const string path = dir + "/SoundLibrary.asset";
            EnsureDir(dir);

            var existing = AssetDatabase.LoadAssetAtPath<SoundLibrary>(path);
            if (existing != null) { PingAndSelect(existing); return; }

            var asset = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            PingAndSelect(asset);
        }

        [MenuItem("Tools/Audio/Validate Library")]
        static void ValidateLibrary()
        {
            var guids     = AssetDatabase.FindAssets("t:SoundLibrary");
            var errors    = new List<string>();
            var warnings  = new List<string>();
            int total     = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lib  = AssetDatabase.LoadAssetAtPath<SoundLibrary>(path);
                if (lib == null) continue;

                lib.BuildMap();

                var seen = new HashSet<string>();
                foreach (var entry in lib.EditorEntries)
                {
                    total++;
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        errors.Add($"[{path}] Entry with empty key.");
                        continue;
                    }

                    if (!seen.Add(entry.Key))
                        errors.Add($"[{path}] Duplicate key: '{entry.Key}'");

                    var clip = Resources.Load<AudioClip>(entry.Key);
                    if (clip == null)
                        warnings.Add($"[{path}] Key '{entry.Key}' not found in Resources. (OK if using Addressables.)");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Validated {total} entries across {guids.Length} library asset(s).");

            if (errors.Count == 0 && warnings.Count == 0)
            {
                sb.AppendLine("✓ No issues found.");
                Debug.Log(sb.ToString());
                EditorUtility.DisplayDialog("Audio Library — Valid", sb.ToString(), "OK");
                return;
            }

            foreach (var e in errors)   { sb.AppendLine("ERROR: " + e);   Debug.LogError(e); }
            foreach (var w in warnings) { sb.AppendLine("WARN:  " + w);   Debug.LogWarning(w); }

            var title = errors.Count > 0 ? "Audio Library — Errors Found" : "Audio Library — Warnings";
            EditorUtility.DisplayDialog(title, sb.ToString(), "OK");
        }

        static void EnsureDir(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }

        static void PingAndSelect(Object obj)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }
}
