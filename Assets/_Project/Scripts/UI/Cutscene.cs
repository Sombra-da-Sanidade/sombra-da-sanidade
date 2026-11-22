using System;
using SombraDaSanidade.Audio;
using UnityEngine;

namespace SombraDaSanidade.UI
{
    public enum CutsceneShotKind
    {
        Panel,   // imagem 320x180 (painel do Aseprite). Sem imagem: tela preta com a legenda (intertítulo).
        World,   // câmera do jogo focando um alvo da cena, com faixas pretas.
    }

    public enum CutsceneTransition { Cut, Fade, Glitch }

    [Serializable]
    public class CutsceneShot
    {
        public CutsceneShotKind kind = CutsceneShotKind.Panel;

        [Tooltip("Painel (Panel). Vazio: fundo preto.")]
        public Sprite image;

        [Tooltip("Alvo da câmera (World): 'player', 'especime', 'sonambulo' ou o id de um CutsceneMarker da cena.")]
        public string focus = "player";

        [Tooltip("Zoom no começo e no fim do plano. Panel: 1 = painel inteiro, 1.5 = recorte de 2/3. " +
                 "World: multiplica o tamanho da câmera (0.7 = mais perto).")]
        public Vector2 zoom = Vector2.one;

        [Tooltip("Deslocamento no começo do plano. Panel: fração do painel (-0.5 a 0.5). World: unidades a partir do alvo.")]
        public Vector2 panFrom;
        [Tooltip("Deslocamento no fim do plano.")]
        public Vector2 panTo;

        [Min(0.1f)] public float duration = 3f;

        [Tooltip("Quem fala (VANCE, THOMAS...). Vazio: legenda de narração.")]
        public string speaker = "";
        [TextArea(1, 3)] public string caption = "";

        [Tooltip("Som tocado no começo do plano.")]
        public SoundEvent sound;

        [Tooltip("Como o plano entra.")]
        public CutsceneTransition transition = CutsceneTransition.Cut;

        [Tooltip("Plano de memória: liga o MemoryEffect (paleta verde, linhas de TV, quadros repetidos).")]
        public bool memory;

        [Tooltip("Evento disparado no começo do plano. Um CutsceneMarker com o mesmo id reage (ex.: 'lampada_pisca').")]
        public string cue = "";
    }

    // Lista de planos de uma cutscene (#88). Os textos vêm de docs/narrativa/roteiro-rc.md (#90).
    // Toca com CutscenePlayer.Play(cutscene). Montar as 11 cutscenes é a #91.
    [CreateAssetMenu(fileName = "Cutscene", menuName = "Sombra da Sanidade/Narrativa/Cutscene")]
    public class Cutscene : ScriptableObject
    {
        [Tooltip("Identificador estável (C0, C1, M1...). Cutscenes tocadas não repetem no mesmo checkpoint.")]
        public string id = "C0";

        [Tooltip("Faixa fixa durante a cutscene (Music/memoria_memory.ogg nas memórias, revelacao.ogg na C3). " +
                 "Vazio: a música da fase abaixa até só o ambiente.")]
        public AudioClip music;

        [Tooltip("Congela o jogo (Time.timeScale = 0). Desligue em cutscenes no motor que precisam do mundo andando.")]
        public bool freezeWorld = true;

        public CutsceneShot[] shots = new CutsceneShot[0];

        public float TotalDuration
        {
            get
            {
                float total = 0f;
                foreach (var s in shots) if (s != null) total += s.duration;
                return total;
            }
        }
    }
}
