using System;

namespace SombraDaSanidade.Core
{
    // Ordem das fases do RC (docs/planejamento-rc.md, seção 1) e a cena de cada ala. Fonte única para o Menu
    // (JOGAR e CONTINUAR), a tela "Ala concluída" (PRÓXIMA ALA) e o Build Profile. Issue #93.
    public static class WingFlow
    {
        public const string MenuScene = "Menu";

        // Ids de ala (DifficultyProfile.wing, WingCheckpoint.Wing) e cenas, na ordem da partida.
        private static readonly string[] Wings = { "Medica", "Energia", "Executiva", "Eclusa" };
        private static readonly string[] Scenes = { "AlaMedica", "AlaEnergia", "AlaExecutiva", "Eclusa" };

        public static string FirstWing => Wings[0];
        public static string FirstScene => Scenes[0];
        public static int Count => Wings.Length;

        public static string WingAt(int index) => Wings[index];
        public static string SceneAt(int index) => Scenes[index];

        public static bool IsKnown(string wing) => IndexOf(wing) >= 0;

        // Cena da ala, ou null para uma ala desconhecida (checkpoint antigo ou corrompido).
        public static string SceneFor(string wing)
        {
            int i = IndexOf(wing);
            return i >= 0 ? Scenes[i] : null;
        }

        // Ala de uma cena, ou null se a cena não é uma fase (Menu, Alfa, Beta, sandboxes).
        public static string WingForScene(string scene)
        {
            int i = string.IsNullOrEmpty(scene) ? -1 : Array.IndexOf(Scenes, scene);
            return i >= 0 ? Wings[i] : null;
        }

        // Ala seguinte, ou "" depois da Eclusa (fim da partida).
        public static string NextOf(string wing)
        {
            int i = IndexOf(wing);
            return i >= 0 && i + 1 < Wings.Length ? Wings[i + 1] : "";
        }

        public static bool IsFinal(string wing) => IsKnown(wing) && NextOf(wing).Length == 0;

        private static int IndexOf(string wing) =>
            string.IsNullOrEmpty(wing) ? -1 : Array.IndexOf(Wings, wing);
    }
}
