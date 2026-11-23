using SombraDaSanidade.UI;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Cutscene de abertura da ala (roteiro-rc 5): C0 antes da fase 1, I1 entre a fase 1 e a 2, I2 entre a 2 e a 3.
    // Os intertítulos são o fim da ala anterior, mas tocam no começo da seguinte para não mexer na tela "Ala
    // concluída". Lidas de Resources/Cutscenes/<id> (CutsceneAssetBuilder). Uma vez por sessão: ao reiniciar a
    // ala depois de morrer, o CutscenePlayer já marcou como tocada.
    public class WingOpening : MonoBehaviour
    {
        public const string ResourceFolder = "Cutscenes/";

        [Tooltip("Id da cutscene em Resources/Cutscenes (C0, I1, I2).")]
        [SerializeField] private string cutsceneId = "C0";

        public string CutsceneId => cutsceneId;

        public void Setup(string id) => cutsceneId = id;

        public static Cutscene Load(string id) => Resources.Load<Cutscene>(ResourceFolder + id);

        private void Start()
        {
            if (string.IsNullOrEmpty(cutsceneId) || CutscenePlayer.HasPlayed(cutsceneId)) return;
            var scene = Load(cutsceneId);
            if (scene != null) CutscenePlayer.Play(scene);
        }
    }
}
