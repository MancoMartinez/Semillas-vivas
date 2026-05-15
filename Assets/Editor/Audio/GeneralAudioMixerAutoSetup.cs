#if UNITY_EDITOR
using System;
using System.Reflection;
using SemillasVivas.Systems.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace SemillasVivas.Editor.Audio
{
    [InitializeOnLoad]
    public static class GeneralAudioMixerAutoSetup
    {
        private const string AudioFolderPath = "Assets/Audio";
        private const string ResourcesFolderPath = "Assets/Audio/Resources";
        private const string MixerAssetPath = "Assets/Audio/GeneralAudioMixer.mixer";
        private const string CatalogAssetPath = "Assets/Audio/Resources/GameAudioCatalog.asset";

        static GeneralAudioMixerAutoSetup()
        {
            EditorApplication.delayCall += EnsureAudioAssets;
        }

        private static void EnsureAudioAssets()
        {
            AudioMixer mixer = EnsureMixerAsset();
            EnsureCatalogAsset(mixer);
        }

        private static AudioMixer EnsureMixerAsset()
        {
            AudioMixer existingMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerAssetPath);

            if (existingMixer != null)
            {
                return existingMixer;
            }

            if (!AssetDatabase.IsValidFolder(AudioFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
            }

            Type mixerControllerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");

            if (mixerControllerType == null)
            {
                Debug.LogWarning("UnityEditor.Audio.AudioMixerController was not found. The audio mixer asset could not be auto-created.");
                return null;
            }

            MethodInfo createMethod = mixerControllerType.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (createMethod != null)
            {
                createMethod.Invoke(null, new object[] { MixerAssetPath });
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerAssetPath);
            }

            ScriptableObject mixerAsset = ScriptableObject.CreateInstance(mixerControllerType) as ScriptableObject;

            if (mixerAsset == null)
            {
                Debug.LogWarning("The general audio mixer asset could not be instantiated.");
                return null;
            }

            AssetDatabase.CreateAsset(mixerAsset, MixerAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerAssetPath);
        }

        private static void EnsureCatalogAsset(AudioMixer mixer)
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolderPath))
            {
                AssetDatabase.CreateFolder(AudioFolderPath, "Resources");
            }

            GameAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(CatalogAssetPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameAudioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            SerializedObject serializedCatalog = new(catalog);
            serializedCatalog.FindProperty("mixer").objectReferenceValue = mixer;

            AudioMixerGroup masterGroup = null;

            if (mixer != null)
            {
                AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master");
                masterGroup = groups != null && groups.Length > 0 ? groups[0] : null;
            }

            serializedCatalog.FindProperty("masterGroup").objectReferenceValue = masterGroup;

            AssignCue(serializedCatalog, GameAudioCue.UiClick, "Item collect");
            AssignCue(serializedCatalog, GameAudioCue.PlayerAttack, "deadly-strike");
            AssignCue(serializedCatalog, GameAudioCue.PlayerJump, "jump");
            AssignCue(serializedCatalog, GameAudioCue.PlayerHurt, "ouch");
            AssignCue(serializedCatalog, GameAudioCue.PlayerDeath, "Game over");
            AssignCue(serializedCatalog, GameAudioCue.PlayerFootstep, "running-on-concrete");
            AssignCue(serializedCatalog, GameAudioCue.EnemyFootstep, "footsteps-on-gravel");
            AssignCue(serializedCatalog, GameAudioCue.EnemyHurt, "ENEMY hurt-pain");
            AssignCue(serializedCatalog, GameAudioCue.EnemyDeath, "ENEMY DEATH");
            AssignCue(serializedCatalog, GameAudioCue.PowerUp, "power-up");
            AssignCue(serializedCatalog, GameAudioCue.ItemCollect, "Item collect");
            AssignCue(serializedCatalog, GameAudioCue.Heal, "Heal");

            AssignMusic(serializedCatalog, "MainMenu", "Home SV");
            AssignMusic(serializedCatalog, "DemoGameplay", "Acaí SV");

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void AssignCue(SerializedObject serializedCatalog, GameAudioCue cue, string clipName)
        {
            SerializedProperty cuesProperty = serializedCatalog.FindProperty("cues");
            int index = FindOrAddArrayElement(cuesProperty, "cue", (int)cue);
            cuesProperty.GetArrayElementAtIndex(index).FindPropertyRelative("cue").enumValueIndex = (int)cue;
            cuesProperty.GetArrayElementAtIndex(index).FindPropertyRelative("clip").objectReferenceValue = FindAudioClip(clipName);
        }

        private static void AssignMusic(SerializedObject serializedCatalog, string sceneName, string clipName)
        {
            SerializedProperty musicProperty = serializedCatalog.FindProperty("musicTracks");
            int index = FindOrAddArrayElement(musicProperty, "sceneName", sceneName);
            musicProperty.GetArrayElementAtIndex(index).FindPropertyRelative("sceneName").stringValue = sceneName;
            musicProperty.GetArrayElementAtIndex(index).FindPropertyRelative("clip").objectReferenceValue = FindAudioClip(clipName);
        }

        private static int FindOrAddArrayElement(SerializedProperty arrayProperty, string childName, int enumValue)
        {
            for (int index = 0; index < arrayProperty.arraySize; index++)
            {
                if (arrayProperty.GetArrayElementAtIndex(index).FindPropertyRelative(childName).enumValueIndex == enumValue)
                {
                    return index;
                }
            }

            int newIndex = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(newIndex);
            return newIndex;
        }

        private static int FindOrAddArrayElement(SerializedProperty arrayProperty, string childName, string stringValue)
        {
            for (int index = 0; index < arrayProperty.arraySize; index++)
            {
                if (arrayProperty.GetArrayElementAtIndex(index).FindPropertyRelative(childName).stringValue == stringValue)
                {
                    return index;
                }
            }

            int newIndex = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(newIndex);
            return newIndex;
        }

        private static AudioClip FindAudioClip(string clipName)
        {
            string[] guids = AssetDatabase.FindAssets($"{clipName} t:AudioClip");

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

                if (clip != null && string.Equals(clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
#endif
