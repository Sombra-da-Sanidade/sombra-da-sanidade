using System;
using UnityEngine;

namespace SombraDaSanidade.Core
{
    public enum VanceDecision { None, Leave, Save }

    public enum Ending { None, Sample, Witness }

    // Escolha da Vance na Eclusa (planejamento-rc 3, #82 e #95). O terminal registra a decisão, a cela registra
    // a libertação e o laudo lê o final. Zera quando a Eclusa recomeça (o terminal chama Reset no Awake), então
    // morrer e reiniciar volta a pedir a escolha.
    public static class VanceChoice
    {
        public const string AchievementKey = "Sombra.Conquista.VerdadeCorporativa";

        public static VanceDecision Decision { get; private set; }
        public static bool Released { get; private set; }

        // Testemunha só se a Vance foi solta de verdade: escolher salvar e fugir sem abrir a cela é Amostra.
        public static Ending Ending => Released ? Ending.Witness : Decision == VanceDecision.None ? Ending.None : Ending.Sample;

        public static event Action<VanceDecision> Decided;
        public static event Action ReleasedEvent;

        public static void Decide(VanceDecision decision)
        {
            if (Decision != VanceDecision.None || decision == VanceDecision.None) return;
            Decision = decision;
            Decided?.Invoke(decision);
        }

        public static void Release()
        {
            if (Released) return;
            Released = true;
            ReleasedEvent?.Invoke();
        }

        public static void Reset()
        {
            Decision = VanceDecision.None;
            Released = false;
        }

        // "A Verdade Corporativa" (roteiro-rc 6): sair com a Vance. Fica salvo entre partidas.
        public static bool HasAchievement => PlayerPrefs.GetInt(AchievementKey, 0) == 1;

        public static bool UnlockAchievement()
        {
            if (HasAchievement) return false;
            PlayerPrefs.SetInt(AchievementKey, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static string Title(Ending e) => e == Ending.Witness ? "TESTEMUNHA" : e == Ending.Sample ? "AMOSTRA ENTREGUE" : "";

        public static string Line(Ending e) => e switch
        {
            Ending.Witness => "AS PROVAS SAÍRAM DO SUB-NÍVEL 4. A DRA. VANCE TAMBÉM.",
            Ending.Sample => "A AETHELGARD RECEBEU OS DADOS DO VOLUNTÁRIO 08.",
            _ => ""
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Reset();
            Decided = null;
            ReleasedEvent = null;
        }
    }
}
