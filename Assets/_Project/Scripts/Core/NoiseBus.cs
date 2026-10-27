using System;
using UnityEngine;

namespace SombraDaSanidade.Core
{
    // Canal global de ruído. Quem faz barulho chama Emit; o monstro escuta NoiseEmitted (GDD 4.3).
    // Exemplos: reparo de disjuntor, pulso EMP, ejetar HD.
    public static class NoiseBus
    {
        public static event Action<Vector2, float> NoiseEmitted;

        public static void Emit(Vector2 position, float radius)
        {
            NoiseEmitted?.Invoke(position, radius);
        }
    }
}
