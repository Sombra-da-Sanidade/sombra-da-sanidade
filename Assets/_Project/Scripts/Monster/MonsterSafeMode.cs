using System;
using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Modo seguro opcional (#48, acessibilidade, como o Safe Mode do SOMA): o monstro fica mais lento e
    // encostar no jogador não mata. Ele empurra o jogador e drena sanidade (MonsterConfig.safeMode*).
    // Fica salvo em PlayerPrefs. Frente 5: a opção do menu (#32) chama Enabled = true/false,
    // e o rank lê Enabled para marcar "modo seguro".
    public static class MonsterSafeMode
    {
        public const string PrefsKey = "SombraDaSanidade.SafeMode";

        private static bool? cached;

        public static event Action<bool> Changed;

        // O projeto entra no Play sem recarregar o domínio: sem isto o valor lido numa sessão de Play anterior
        // ficava preso e apagar o PlayerPrefs (Edit > Clear All PlayerPrefs) não desligava o modo seguro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cached = null;

        public static bool Enabled
        {
            get
            {
                if (!cached.HasValue) cached = PlayerPrefs.GetInt(PrefsKey, 0) == 1;
                return cached.Value;
            }
            set
            {
                if (cached.HasValue && cached.Value == value) return;
                cached = value;
                PlayerPrefs.SetInt(PrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke(value);
            }
        }
    }
}
