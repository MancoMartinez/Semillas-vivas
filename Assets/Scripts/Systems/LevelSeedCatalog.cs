using UnityEngine;
using SemillasVivas.Gameplay.Demo;

namespace SemillasVivas.Systems
{
    public readonly struct LevelSeedData
    {
        public LevelSeedData(
            int levelNumber,
            string sceneName,
            string seedName,
            string spriteResourcePath,
            DemoPowerUpType powerUpType,
            string introMessage,
            string winDescription)
        {
            LevelNumber = levelNumber;
            SceneName = sceneName;
            SeedName = seedName;
            SpriteResourcePath = spriteResourcePath;
            PowerUpType = powerUpType;
            IntroMessage = introMessage;
            WinDescription = winDescription;
        }

        public int LevelNumber { get; }
        public string SceneName { get; }
        public string SeedName { get; }
        public string SpriteResourcePath { get; }
        public DemoPowerUpType PowerUpType { get; }
        public string IntroMessage { get; }
        public string WinDescription { get; }
    }

    public static class LevelSeedCatalog
    {
        private static readonly LevelSeedData[] Entries =
        {
            new(
                1,
                "DemoGameplay",
                "Sacha Inchi",
                "lvl5/sanchaiche (1)",
                DemoPowerUpType.SachaInchiDoubleJump,
                "Inchi te ayuda a llegar más alto. Ahora puedes saltar mejor. Prepárate para lo que viene.",
                "Pasaste el nivel 1 y obtuviste la semilla Sacha Inchi; esta te da beneficios de salto más alto."),
            new(
                2,
                "lvl2",
                "Acaí",
                "lvl5/acai",
                DemoPowerUpType.AcaiSpeed,
                "Acaí te llena de energía. Ahora puedes moverte más rápido. Usa esa velocidad a tu favor.",
                "Pasaste el nivel 2 y obtuviste la semilla Acaí, esta te da beneficios de mayor velocidad."),
            new(
                3,
                "lvl3",
                "Chontaduro",
                "lvl5/chontaduro",
                DemoPowerUpType.ChontaduroStrength,
                "Chontaduro fortalece tu ataque. Ahora puedes golpear con más alcance.",
                "Pasaste el nivel 3 y obtuviste la semilla Chontaduro, esta te da beneficios de mayor fuerza."),
            new(
                4,
                "lvl4",
                "Copoazú",
                "lvl5/copohazu",
                DemoPowerUpType.CopoazuVitality,
                "Copoazú fortalece tu vitalidad. Ahora puedes resistir mejor los peligros.",
                "Pasaste el nivel 4 y obtuviste la semilla Copoazú, esta te da beneficios de más vitalidad."),
            new(
                5,
                "lvl5",
                "Uva Caimora",
                "lvl5/uva caimora",
                DemoPowerUpType.UvaShield,
                "Uva Caimora te protege. Ahora tienes un escudo para resistir un golpe.",
                "Pasaste el nivel 5 y obtuviste la semilla Uva Caimora; esta te da beneficios de protección."),
            new(
                6,
                "Boss",
                "Coca",
                "lvl5/Coca (1)",
                DemoPowerUpType.CocaSlowdown,
                "La semilla de coca altera el ritmo del combate. Por unos segundos, los ataques enemigos se vuelven más lentos.",
                "¡Lo lograste, Sacha! Soy Koka. He sido parte de la vida de muchas comunidades. Ahora sabes que el bosque guarda historias que debemos cuidar."),
        };

        public static LevelSeedData GetForScene(string sceneName)
        {
            for (int index = 0; index < Entries.Length; index++)
            {
                if (MatchesScene(Entries[index].SceneName, sceneName))
                {
                    return Entries[index];
                }
            }

            return Entries[0];
        }

        public static Sprite LoadSeedSprite(LevelSeedData data)
        {
            return string.IsNullOrWhiteSpace(data.SpriteResourcePath)
                ? null
                : Resources.Load<Sprite>(data.SpriteResourcePath);
        }

        private static bool MatchesScene(string catalogSceneName, string sceneName)
        {
            if (string.Equals(catalogSceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (catalogSceneName == "DemoGameplay" &&
                (string.Equals(sceneName, "lvl1", System.StringComparison.OrdinalIgnoreCase)
                 || string.Equals(sceneName, "Level_01", System.StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }
}
