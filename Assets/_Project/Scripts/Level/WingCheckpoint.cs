using System;
using SombraDaSanidade.Core;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Fim de uma ala: ao vencer (porta aberta com os HDs da ala), salva o checkpoint (#45) e a tela de
    // vitória mostra "Progresso salvo" como recompensa (Proposta 2 do v1.1). Um por cena de ala.
    public class WingCheckpoint : MonoBehaviour
    {
        public static WingCheckpoint Instance { get; private set; }

        [SerializeField] private string wing = "Medica";
        [SerializeField] private string nextWing = "Energia";

        public string Wing => wing;
        public string NextWing => nextWing;
        public bool Saved { get; private set; }
        public bool SaveFailed { get; private set; }

        private GameManager gm;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            gm = GameManager.Instance;
            if (gm != null) gm.VictoryTriggered += OnVictory;
        }

        private void OnDestroy()
        {
            if (gm != null) gm.VictoryTriggered -= OnVictory;
            if (Instance == this) Instance = null;
        }

        private void OnVictory()
        {
            try
            {
                Checkpoint.Save(new CheckpointData
                {
                    lastWing = wing,
                    nextWing = nextWing,
                    drives = gm.DrivesCollected,
                    elapsedSeconds = gm.ElapsedTime
                });
                Saved = true;
            }
            catch (Exception e)
            {
                SaveFailed = true;
                Debug.LogError($"Não foi possível salvar o checkpoint da ala {wing}: {e.Message}", this);
            }
        }

        public static string DisplayName(string wing) => wing switch
        {
            "Medica" => "ALA MÉDICA",
            "Energia" => "ALA DE ENERGIA",
            "Executiva" => "ALA EXECUTIVA",
            _ => "SAGUÃO"
        };

        // Texto da recompensa na tela de vitória (pixel e fallback).
        public string StatusLine => Saved ? "PROGRESSO SALVO" : SaveFailed ? "FALHA AO SALVAR O PROGRESSO" : "";
    }
}
