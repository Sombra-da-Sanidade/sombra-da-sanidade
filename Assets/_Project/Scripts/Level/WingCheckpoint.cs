using System;
using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Flashlight;
using SombraDaSanidade.Interaction;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Checkpoint de uma ala (#45). Um por cena de ala.
    // - Ao vencer (porta aberta com os HDs da ala): salva HDs, bateria, sanidade, upgrades e as estatísticas do rank
    //   (#32), e a tela de vitória mostra "Progresso salvo" como recompensa (Proposta 2 do v1.1).
    // - Ao entrar na ala para a qual o checkpoint aponta (CONTINUAR do Menu ou reinício depois de um Game Over):
    //   o Thomas volta ao estado salvo. É o "reinício do checkpoint da ala" do GDD 8.
    // - Ala final (Eclusa, nextWing vazio): restaura o estado, mas ao vencer apaga o checkpoint (partida encerrada).
    public class WingCheckpoint : MonoBehaviour
    {
        public static WingCheckpoint Instance { get; private set; }

        [SerializeField] private string wing = "Medica";
        [Tooltip("Ala seguinte (WingFlow). Vazio: última ala, a vitória encerra a partida.")]
        [SerializeField] private string nextWing = "Energia";

        public string Wing => wing;
        public string NextWing => nextWing;
        public bool IsFinal => string.IsNullOrEmpty(nextWing);
        // Cena da próxima ala, ou null se for a última ou se a cena não estiver no Build Profile.
        public string NextScene
        {
            get
            {
                string scene = IsFinal ? null : WingFlow.SceneFor(nextWing);
                return scene != null && Application.CanStreamedLevelBeLoaded(scene) ? scene : null;
            }
        }
        public bool Saved { get; private set; }
        public bool SaveFailed { get; private set; }
        // A ala começou do checkpoint salvo (estado do Thomas restaurado).
        public bool Restored { get; private set; }
        // Upgrade escolhido nesta ala (tela "Ala concluída"), ou null.
        public LuxUpgrade UpgradeThisWing { get; private set; }

        private int upgradesAtStart;

        // A Eclusa não tem WingCheckpoint na cena: sem ele, entrar nela pelo encadeamento perderia upgrades,
        // bateria, sanidade e as estatísticas do rank. Cria um em runtime em qualquer ala conhecida sem um.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoads()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= EnsureForWing;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += EnsureForWing;
        }

        private static void EnsureForWing(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Pelo nome da cena: a Eclusa não tem DifficultyProfile no GameManager.
            string id = WingFlow.WingForScene(scene.name);
            if (id == null || GameManager.Instance == null || FindAnyObjectByType<WingCheckpoint>() != null) return;
            Create(id, WingFlow.NextOf(id));
        }

        public static WingCheckpoint Create(string wing, string nextWing)
        {
            var go = new GameObject("WingFlow (runtime)");
            go.SetActive(false);
            var checkpoint = go.AddComponent<WingCheckpoint>();
            checkpoint.wing = wing;
            checkpoint.nextWing = nextWing;
            go.SetActive(true);
            return checkpoint;
        }

        private GameManager gm;
        private FlashlightController flashlight;
        private SanityController sanity;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            gm = GameManager.Instance;
            if (gm != null) gm.VictoryTriggered += OnVictory;
            flashlight = FindAnyObjectByType<FlashlightController>();
            sanity = FindAnyObjectByType<SanityController>();
            Restore();
            upgradesAtStart = flashlight != null ? flashlight.Installed.Count : 0;
        }

        private void OnDestroy()
        {
            if (gm != null) gm.VictoryTriggered -= OnVictory;
            if (Instance == this) Instance = null;
        }

        private void Restore()
        {
            if (!Checkpoint.TryLoad(out var data) || data.nextWing != wing) return;
            RunStats.LoadFrom(data);

            // Upgrades antes da bateria: a Célula Extra muda a carga máxima.
            var tree = FindUpgradeTree();
            if (data.upgrades != null && tree != null)
            {
                foreach (var id in data.upgrades)
                {
                    var upgrade = tree.Find(id);
                    if (upgrade != null) LuxUpgrades.Apply(upgrade, flashlight, sanity);
                    else Debug.LogWarning($"Checkpoint: upgrade '{id}' não está na árvore de upgrades.", this);
                }
            }
            if (flashlight != null)
            {
                float delta = Mathf.Clamp(data.battery, 0f, flashlight.MaxBattery) - flashlight.Battery;
                if (delta > 0f) flashlight.Recharge(delta);
                else if (delta < 0f) flashlight.TrySpend(-delta);
            }
            // Sanidade 0 seria Game Over na entrada: o mínimo é 1.
            if (sanity != null) sanity.Add(Mathf.Max(1f, data.sanity) - sanity.Value);
            Restored = true;
        }

        private void OnVictory()
        {
            if (flashlight != null && flashlight.Installed.Count > upgradesAtStart)
                UpgradeThisWing = flashlight.Installed[flashlight.Installed.Count - 1];

            try
            {
                // Fim da partida: o CONTINUAR do Menu não pode voltar para uma Eclusa já vencida.
                if (IsFinal)
                {
                    Checkpoint.Clear();
                    return;
                }
                Checkpoint.Save(new CheckpointData
                {
                    lastWing = wing,
                    nextWing = nextWing,
                    drives = gm.TotalDrives,
                    elapsedSeconds = gm.ElapsedTime,
                    battery = flashlight != null ? flashlight.Battery : 0f,
                    sanity = sanity != null ? sanity.Value : 0f,
                    upgrades = InstalledUpgrades(),
                    totalSeconds = gm.RunTime,
                    deaths = gm.Deaths,
                    emps = gm.RunEmps,
                    sanitySum = gm.SanitySum,
                    sanitySeconds = gm.SanitySeconds,
                    skillCheckFailures = gm.CountSkillCheckFailures()
                });
                Saved = true;
            }
            catch (Exception e)
            {
                SaveFailed = true;
                Debug.LogError($"Não foi possível salvar o checkpoint da ala {wing}: {e.Message}", this);
            }
        }

        // Nomes dos assets instalados. Todos ficam na lista da lanterna, inclusive o Ressonador (LuxUpgrades).
        private string[] InstalledUpgrades()
        {
            var ids = new List<string>();
            if (flashlight != null)
                foreach (var u in flashlight.Installed) if (u != null) ids.Add(u.name);
            return ids.ToArray();
        }

        // A árvore fica no InteractionConfig dos servidores de HD. A Eclusa não tem servidor: carrega o mesmo asset, que fica em Resources.
        private static LuxUpgradeTree FindUpgradeTree()
        {
            var server = FindAnyObjectByType<DriveServer>();
            if (server != null && server.SkillCheckConfig != null && server.SkillCheckConfig.upgradeTree != null)
                return server.SkillCheckConfig.upgradeTree;
            return Resources.Load<LuxUpgradeTree>(UpgradeTreeResource);
        }

        private const string UpgradeTreeResource = "LuxUpgradeTree";

        public static string DisplayName(string wing) => wing switch
        {
            "Medica" => "ALA MÉDICA",
            "Energia" => "ALA DE ENERGIA",
            "Executiva" => "ALA EXECUTIVA",
            "Eclusa" => "ESCONDERIJO",
            _ => "SAGUÃO"
        };

        // Texto da recompensa na tela de vitória (pixel e fallback).
        public string StatusLine => Saved ? "PROGRESSO SALVO" : SaveFailed ? "FALHA AO SALVAR O PROGRESSO" : "";
    }
}
