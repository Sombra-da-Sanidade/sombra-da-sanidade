using System.Collections;
using SombraDaSanidade.Core;
using SombraDaSanidade.Player;
using SombraDaSanidade.Sanity;
using UnityEngine;

namespace SombraDaSanidade.Monster
{
    // Quem pega o jogador e toca uma cena de morte antes do Game Over (Espécime e Sonâmbulo).
    public interface IMonsterKiller
    {
        // Encostou no jogador fora do modo seguro. Sem ninguém ouvindo, o Game Over é imediato.
        event System.Action<Transform> Caught;
        bool IsKilling { get; }
        float KillFps { get; }
        float KillHold { get; }
    }

    // Frente 2. Cena de morte quando um monstro pega o jogador: no lugar do monstro e do Thomas entra
    // um quadro de 80x80 com os dois juntos. A morte é sorteada entre as duas da folha:
    // Espécime (EspecimeB_kills): 0 = mordida, 1 = partir ao meio.
    // Sonâmbulo (Sonambulo_kills): 0 = engolir a cabeça, 1 = torcer o pescoço.
    // Toca em tempo real e só depois chama o Game Over. No modo seguro os monstros não matam, então isto nunca toca.
    // O animador de cada monstro cria este componente quando o prefab tem a folha das mortes.
    public class MonsterKillScene : MonoBehaviour
    {
        public const int FramesPerKill = 32;
        private static readonly Vector2Int Cell = new Vector2Int(80, 80);
        // Os pés do monstro ficam 5 px acima da borda de baixo do quadro: alinha com o pivô do sprite normal.
        private static readonly Vector2 Pivot = new Vector2(0.5f, 0.066f);

        public enum Kill { Bite = 0, Tear = 1 }      // no Sonâmbulo: 0 = engolir, 1 = pescoço

        public bool IsPlaying { get; private set; }
        public Kill LastKill { get; private set; }

        private IMonsterKiller monster;
        private SpriteRenderer body;
        private SpriteRenderer glow;
        private SpriteRenderer sceneBody;
        private SpriteRenderer sceneGlow;
        // Quadros em ordem de leitura: mordida 0-31, partir ao meio 32-63 (folha de 16 colunas,
        // para caber no limite de 2048 px da textura).
        private Sprite[] bodyFrames;
        private Sprite[] glowFrames;

        public void Setup(Texture2D sheet, Texture2D glowSheet, SpriteRenderer monsterBody, SpriteRenderer monsterGlow, float pixelsPerUnit)
        {
            monster = GetComponent<IMonsterKiller>();
            body = monsterBody;
            glow = monsterGlow;
            bodyFrames = Slice(sheet, pixelsPerUnit);
            if (glowSheet != null) glowFrames = Slice(glowSheet, pixelsPerUnit);

            sceneBody = CreateRenderer("KillScene", monsterBody);
            if (monsterGlow != null && glowFrames != null) sceneGlow = CreateRenderer("KillSceneGlow", monsterGlow);

            monster.Caught -= OnCaught;
            monster.Caught += OnCaught;
        }

        private void OnDestroy()
        {
            if (monster != null) monster.Caught -= OnCaught;
        }

        private void OnCaught(Transform player)
        {
            if (IsPlaying) return;
            StartCoroutine(Play(player, Random.value < 0.5f ? Kill.Bite : Kill.Tear));
        }

        private IEnumerator Play(Transform player, Kill kill)
        {
            IsPlaying = true;
            LastKill = kill;
            HidePlayer(player);

            // Do lado de onde ele pegou: a cena é desenhada com o arremesso para a direita.
            bool flip = player != null && player.position.x < transform.position.x;
            body.enabled = false;
            if (glow != null) glow.enabled = false;
            sceneBody.enabled = true;
            sceneBody.flipX = flip;
            if (sceneGlow != null)
            {
                sceneGlow.enabled = true;
                sceneGlow.flipX = flip;
            }

            float fps = monster.KillFps > 0f ? monster.KillFps : 12f;
            float hold = monster.KillHold;
            int first = (int)kill * FramesPerKill;
            int count = Mathf.Min(FramesPerKill, bodyFrames.Length - first);
            float t = 0f;
            while (true)
            {
                int frame = Mathf.Min(Mathf.FloorToInt(t * fps), count - 1);
                sceneBody.sprite = bodyFrames[first + frame];
                if (sceneGlow != null && first + frame < glowFrames.Length) sceneGlow.sprite = glowFrames[first + frame];
                if (t >= count / fps + hold) break;
                yield return null;
                t += Time.deltaTime;
            }

            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver(DeathCause.Monster);
        }

        // O Thomas passa a ser desenhado dentro da cena: some o sprite dele e trava controle e sanidade
        // (a sanidade zerando no meio da cena daria a causa de morte errada).
        private static void HidePlayer(Transform player)
        {
            if (player == null) return;
            var controller = player.GetComponentInParent<PlayerController>();
            var root = controller != null ? controller.transform : player;
            if (controller != null) controller.enabled = false;
            var rb = root.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            foreach (var anim in root.GetComponentsInChildren<PlayerSpriteAnimator>()) anim.enabled = false;
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>()) sr.enabled = false;
            var sanity = root.GetComponentInChildren<SanityController>();
            if (sanity == null) sanity = FindAnyObjectByType<SanityController>();
            if (sanity != null) sanity.enabled = false;
        }

        private SpriteRenderer CreateRenderer(string goName, SpriteRenderer template)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(template.transform.parent, false);
            go.transform.localPosition = template.transform.localPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = template.sharedMaterial;
            sr.sortingLayerID = template.sortingLayerID;
            sr.sortingOrder = template.sortingOrder;
            sr.enabled = false;
            return sr;
        }

        private static Sprite[] Slice(Texture2D tex, float ppu)
        {
            int rows = tex.height / Cell.y;
            int cols = tex.width / Cell.x;
            var result = new Sprite[rows * cols];
            for (int r = 0; r < rows; r++)
            {
                float y = tex.height - (r + 1) * Cell.y;
                for (int c = 0; c < cols; c++)
                {
                    var sprite = Sprite.Create(tex, new Rect(c * Cell.x, y, Cell.x, Cell.y), Pivot, ppu, 0, SpriteMeshType.FullRect);
                    sprite.name = $"{tex.name}_{r}_{c}";
                    result[r * cols + c] = sprite;
                }
            }
            return result;
        }
    }
}
