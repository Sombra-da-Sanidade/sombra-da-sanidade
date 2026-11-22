using UnityEngine;

namespace SombraDaSanidade.Audio
{
    // Sons de gameplay (#105, docs/audio/sound-design.md seção 5). Fica em Resources/GameSfx; os campos são
    // ligados por Sombra da Sanidade > Áudio > Gerar SoundEvents (tabela SoundEventBuilder.GameSfxTable).
    // Quem toca é o GameSfxHooks, ouvindo os eventos dos sistemas. Nenhum destes sons emite ruído no NoiseBus:
    // o que o Espécime ouve continua sendo só o que já estava no código (GDD 4 e 6).
    [CreateAssetMenu(fileName = "GameSfx", menuName = "Sombra da Sanidade/Áudio/Sons de gameplay")]
    public class GameSfx : ScriptableObject
    {
        public const string ResourcePath = "GameSfx";

        [Header("Thomas")]
        public SoundEvent footstepWalk;
        [Tooltip("Passos com o Adrena-Syn ativo.")]
        public SoundEvent footstepRun;

        [Header("Lux-90")]
        public SoundEvent flashlightOn;
        public SoundEvent flashlightOff;
        [Tooltip("Loop contínuo enquanto a lanterna está acesa.")]
        public SoundEvent flashlightHum;
        public SoundEvent batteryLow;
        public SoundEvent batteryDead;
        public SoundEvent empFire;
        public SoundEvent empFail;

        [Header("Espécime")]
        [Tooltip("Loop no monstro, com oclusão.")]
        public SoundEvent specimenBreath;
        public SoundEvent specimenHear;
        public SoundEvent specimenStep;
        public SoundEvent specimenScream;
        public SoundEvent specimenStunned;
        public SoundEvent specimenAttack;

        [Header("Sonâmbulo")]
        [Tooltip("Loop só enquanto ele se move; corta em seco quando a luz o congela.")]
        public SoundEvent sleepwalkerCreak;
        public SoundEvent sleepwalkerBreath;
        public SoundEvent sleepwalkerWake;
        public SoundEvent sleepwalkerAttack;

        [Header("Multímetro (skill check)")]
        public SoundEvent skillWarn;
        public SoundEvent skillTick;
        public SoundEvent skillGood;
        public SoundEvent skillGreat;
        public SoundEvent skillFail;

        [Header("Câmera de segurança (#79)")]
        public SoundEvent cameraServo;
        public SoundEvent cameraDetect;
        public SoundEvent cameraAlarm;
        public SoundEvent cameraBlackout;
        public SoundEvent cameraShort;

        private static GameSfx cached;

        public static GameSfx Load()
        {
            if (cached == null) cached = Resources.Load<GameSfx>(ResourcePath);
            return cached;
        }
    }
}
