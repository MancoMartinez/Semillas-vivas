using UnityEngine;

namespace SemillasVivas.Systems
{
    
    public static class LevelProgressService
    {
        private const string UnlockedLevelsKey = "SV_UnlockedLevelCount";

        private static readonly string[] LevelOrder =
        {
            "DemoGameplay",
            "lvl2",
            "lvl3",
            "lvl4",
            "lvl5",
        };

        public static void MarkLevelCompleted(string sceneName)
        {
            int levelIndex = FindLevelIndex(sceneName);

            if (levelIndex < 0)
            {
                
                Debug.LogWarning($"[LevelProgressService] Escena '{sceneName}' no está en el orden de niveles.");
                return;
            }

            int newUnlocked = levelIndex + 2; 
            int currentUnlocked = GetUnlockedLevelCount();

            if (newUnlocked > currentUnlocked)
            {
                PlayerPrefs.SetInt(UnlockedLevelsKey, newUnlocked);
                PlayerPrefs.Save();
                Debug.Log($"[LevelProgressService] Nivel '{sceneName}' completado. " +
                          $"Niveles desbloqueados: {newUnlocked}");
            }
        }

        public static int GetUnlockedLevelCount()
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(UnlockedLevelsKey, 1));
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(UnlockedLevelsKey);
            PlayerPrefs.Save();
            Debug.Log("[LevelProgressService] Progreso reiniciado.");
        }

        private static int FindLevelIndex(string sceneName)
        {
            for (int i = 0; i < LevelOrder.Length; i++)
            {
                if (string.Equals(LevelOrder[i], sceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }

                if (i == 0 &&
                    (string.Equals(sceneName, "lvl1", System.StringComparison.OrdinalIgnoreCase)
                     || string.Equals(sceneName, "Level_01", System.StringComparison.OrdinalIgnoreCase)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
