using System;
using System.IO;
using UnityEngine;

namespace SombraDaSanidade.Core
{
    // Checkpoint entre alas (GDD 10, issue #45): um JSON local salvo ao concluir cada ala. No WebGL o
    // persistentDataPath fica no IndexedDB do navegador. Continuar na próxima ala (TryLoad) é das outras alas.
    [Serializable]
    public class CheckpointData
    {
        public string lastWing;       // ala concluída ("Medica", "Energia", "Executiva")
        public string nextWing;       // onde o jogador recomeça
        public int drives;            // HDs entregues até aqui
        public float elapsedSeconds;  // tempo da ala (para o rank da #49)
        public string savedAtUtc;     // ISO 8601
    }

    public static class Checkpoint
    {
        private const string FileName = "checkpoint.json";

        // Testes apontam para uma pasta temporária para não apagar o save de quem está jogando.
        public static string PathOverride;

        public static string FilePath => PathOverride ?? Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists => File.Exists(FilePath);

        // Grava em arquivo temporário e troca no fim: um crash no meio não deixa um JSON pela metade.
        public static void Save(CheckpointData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(data.savedAtUtc)) data.savedAtUtc = DateTime.UtcNow.ToString("o");
            string path = FilePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static bool TryLoad(out CheckpointData data)
        {
            data = null;
            try
            {
                if (!Exists) return false;
                data = JsonUtility.FromJson<CheckpointData>(File.ReadAllText(FilePath));
                return data != null && !string.IsNullOrEmpty(data.lastWing);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Checkpoint ilegível em {FilePath}: {e.Message}");
                data = null;
                return false;
            }
        }

        public static void Clear()
        {
            if (Exists) File.Delete(FilePath);
        }
    }
}
