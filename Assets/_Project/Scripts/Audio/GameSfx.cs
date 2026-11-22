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

        [Tooltip("Respiração ofegante em loop (SanityHeartbeat). Sem ela, o SanityHeartbeat gera a sintética.")]
        public SoundEvent playerPanting;
        [Tooltip("Sussurro da alucinação sonora (SanityAudioHallucination). Sem ele, o sintético do script.")]
        public SoundEvent hallucinationWhisper;

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

        [Header("Mundo e itens (#106)")]
        public SoundEvent breakerOn;
        public SoundEvent breakerOff;
        public SoundEvent breakerBurnt;
        public SoundEvent driveEject;
        public SoundEvent driveCollect;
        public SoundEvent lockerOpen;
        public SoundEvent itemPickup;
        public SoundEvent injection;
        public SoundEvent flareIgnite;
        public SoundEvent flareBurn;
        public SoundEvent keypadOk;
        public SoundEvent keypadWrong;
        public SoundEvent substationOn;
        [Tooltip("Loops enquanto o jogador segura E: consertando o disjuntor, forçando o armário, ejetando o HD.")]
        public SoundEvent breakerRepair;
        public SoundEvent lockerForce;
        public SoundEvent driveEjecting;
        public SoundEvent toolboxOpen;
        public SoundEvent substationRepair;
        public SoundEvent neuralModule;
        public SoundEvent keypadKey;
        public SoundEvent eclusaInsert;
        public SoundEvent eclusaOn;
        [Tooltip("Vitória final (VOCÊ ESCAPOU): eclusa despressurizando e o sinal de homologação da Aethelgard.")]
        public SoundEvent victory;

        [Header("Interface (#106)")]
        public SoundEvent uiMove;
        public SoundEvent uiConfirm;
        public SoundEvent uiBack;
        public SoundEvent uiUpgrade;
        public SoundEvent uiSaved;

        private static GameSfx cached;

        public static GameSfx Load()
        {
            if (cached == null) cached = Resources.Load<GameSfx>(ResourcePath);
            return cached;
        }
    }
}
