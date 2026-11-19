using System.Collections.Generic;
using SombraDaSanidade.Core;
using SombraDaSanidade.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SombraDaSanidade.EditorTools
{
    // Mobília das salas: armários redistribuídos, móveis na parede norte (de frente) e nas laterais (de perfil;
    // a direita é a esquerda espelhada), entulho no chão, bancadas com caixa de ferramentas (sinalizador) e as
    // sirenes de parede que acendem na purga. Tudo decoração, sem colisão (o NavMesh só conhece as paredes).
    // Sorteio fixo pelo tamanho do mapa: gerar de novo dá o mesmo resultado. docs/props-e-sinalizador.md.
    public static partial class AlfaSceneBuilder
    {
        private enum Side { North, West, East }

        private const string WallObjects = "STKXGUEQP";

        // Células de chão já ocupadas por um móvel (com margem), para nada ficar empilhado.
        private static readonly HashSet<Vector2Int> busy = new HashSet<Vector2Int>();

        private static void Occupy(Vector2Int c, int dx0, int dx1, int dy0, int dy1)
        {
            for (int dy = dy0; dy <= dy1; dy++)
                for (int dx = dx0; dx <= dx1; dx++)
                    busy.Add(new Vector2Int(c.x + dx, c.y + dy));
        }

        private static bool Free(Vector2Int c, int dx0, int dx1, int dy0, int dy1)
        {
            for (int dy = dy0; dy <= dy1; dy++)
                for (int dx = dx0; dx <= dx1; dx++)
                    if (busy.Contains(new Vector2Int(c.x + dx, c.y + dy))) return false;
            return true;
        }

        // Chão liso logo abaixo de uma parede norte reta, longe de servidor, disjuntor, porta, terminal e canos.
        private static bool NorthSpot(Layout map, int x, int y)
        {
            if (map[x, y] != '.' || map[x, y + 1] != '#' || map[x - 1, y + 1] != '#' || map[x + 1, y + 1] != '#') return false;
            if (!map.IsFloor(x - 1, y) || !map.IsFloor(x + 1, y) || !map.IsFloor(x, y - 1)) return false;
            for (int dx = -1; dx <= 1; dx++)
                if (WallObjects.IndexOf(map[x + dx, y + 1]) >= 0) return false;
            return true;
        }

        // Chão liso encostado numa parede lateral reta (west: parede à esquerda), no meio do trecho, sala larga.
        private static bool SideSpot(Layout map, int x, int y, Side side)
        {
            int w = side == Side.West ? -1 : 1;
            if (map[x, y] != '.' || map[x + w, y] != '#' || map[x + w, y + 1] != '#' || map[x + w, y - 1] != '#') return false;
            if (!map.IsFloor(x, y + 1) || !map.IsFloor(x, y - 1) || !map.IsFloor(x - w, y) || !map.IsFloor(x - 2 * w, y)) return false;
            // Não encosta na parede norte (o canto fica para os móveis de frente).
            return map.IsFloor(x, y + 2);
        }

        // ---------- Armários ----------

        // Antes de montar: armário colado em outro (até 2 tiles) ou fora de uma parede norte reta muda de lugar para o
        // ponto livre mais perto (busca pelo chão, até 14 passos). Os que viram de perfil ficam em sideLockers.
        private static readonly List<(Vector2Int cell, Side side)> sideLockers = new List<(Vector2Int, Side)>();

        private static void SpreadLockers(Layout map)
        {
            busy.Clear();
            sideLockers.Clear();
            var cells = new List<Vector2Int>();
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map[x, y] == 'A') cells.Add(new Vector2Int(x, y));
            var kept = new List<Vector2Int>();
            bool Clear(Vector2Int c) => !kept.Exists(k => Mathf.Abs(k.x - c.x) <= 2 && Mathf.Abs(k.y - c.y) <= 2);
            foreach (var c in cells)
            {
                map.Cells[c.x, c.y] = '.';
                if (NorthSpot(map, c.x, c.y) && Clear(c)) { Keep(c, Side.North); continue; }
                var spot = Nearest(map, c, 14, p => (NorthSpot(map, p.x, p.y) || SideSpot(map, p.x, p.y, Side.West) || SideSpot(map, p.x, p.y, Side.East)) && Clear(p));
                Keep(spot ?? c, spot == null || NorthSpot(map, spot.Value.x, spot.Value.y) ? Side.North
                    : SideSpot(map, spot.Value.x, spot.Value.y, Side.West) ? Side.West : Side.East);
            }

            void Keep(Vector2Int c, Side side)
            {
                kept.Add(c);
                if (side == Side.North) { map.Cells[c.x, c.y] = 'A'; Occupy(c, -1, 1, 0, 0); }
                else { sideLockers.Add((c, side)); Occupy(c, 0, 0, -1, 2); }
            }
        }

        private static Vector2Int? Nearest(Layout map, Vector2Int from, int maxSteps, System.Predicate<Vector2Int> ok)
        {
            var seen = new HashSet<Vector2Int> { from };
            var queue = new Queue<(Vector2Int, int)>();
            queue.Enqueue((from, 0));
            while (queue.Count > 0)
            {
                var (p, d) = queue.Dequeue();
                if (p != from && ok(p)) return p;
                if (d >= maxSteps) continue;
                foreach (var n in new[] { p + Vector2Int.left, p + Vector2Int.right, p + Vector2Int.up, p + Vector2Int.down })
                    if (map.IsFloor(n.x, n.y) && seen.Add(n)) queue.Enqueue((n, d + 1));
            }
            return null;
        }

        private static void CreateSideLockers(Transform parent, Sprite square, System.Func<int, int, string> wingAt)
        {
            foreach (var (c, side) in sideLockers)
            {
                var go = PrototypeSceneBuilder.CreateLocker(parent, square, $"Locker_{wingAt(c.x, c.y)}_{c.x}_{c.y}",
                    new Vector2(c.x + 0.5f, c.y + 0.5f), PrototypeSceneBuilder.LoadLootTable(wingAt(c.x, c.y)));
                var locker = go.GetComponent<Locker>();
                var body = (SpriteRenderer)new SerializedObject(locker).FindProperty("body").objectReferenceValue;
                var closed = PrototypeSceneBuilder.LoadPropSprite("locker_side_closed");
                if (body == null || closed == null) continue;
                body.sprite = closed;
                body.flipX = side == Side.East;
                body.transform.position = SidePosition(c, side, closed);
                PrototypeSceneBuilder.SetRef(locker, "openedSprite", PrototypeSceneBuilder.LoadPropSprite("locker_side_open"));
            }
        }

        // Pivô no pé, no centro da célula da folha; o desenho de perfil começa na borda esquerda da célula.
        private static Vector2 SidePosition(Vector2Int c, Side side, Sprite sprite)
        {
            float half = sprite.rect.width / sprite.pixelsPerUnit / 2f;
            float x = side == Side.West ? c.x + half : c.x + 1f - half;
            return new Vector2(x, c.y + 0.15f);
        }

        // ---------- Móveis ----------

        private class Piece
        {
            public string North, Side, Light;   // Side null: só de frente. Light: cor do brilho (null = sem luz)
            public string[] NorthFrames, SideFrames;
            public float Depth = 0.3f;
        }

        private static readonly Piece Bed = new Piece { North = "bed_restraints", Side = "bed_restraints_side", Depth = 1.4f };
        private static readonly Piece Iv = new Piece { North = "iv_stand", Side = "iv_stand" };
        private static readonly Piece Shelf = new Piece { North = "shelf_jars", Side = "shelf_jars_side" };
        private static readonly Piece Cabinet = new Piece { North = "file_cabinet", Side = "file_cabinet_side" };
        private static readonly Piece Crates = new Piece { North = "crates", Side = "crates" };
        private static readonly Piece Tank = new Piece { North = "tank_0", Side = "tank_0", NorthFrames = new[] { "tank_0", "tank_1" }, SideFrames = new[] { "tank_0", "tank_1" }, Light = "verde" };
        private static readonly Piece Eeg = new Piece { North = "eeg_cart_0", Side = "eeg_cart_side_0", NorthFrames = new[] { "eeg_cart_0", "eeg_cart_1" }, SideFrames = new[] { "eeg_cart_side_0", "eeg_cart_side_1" }, Light = "roxo" };

        private static Piece[] PiecesFor(string wing)
        {
            if (wing.Contains("Medica")) return new[] { Bed, Iv, Shelf, Cabinet, Bed, Iv };
            if (wing.Contains("Energia")) return new[] { Crates, Cabinet, Shelf, Crates };
            if (wing.Contains("Executiva")) return new[] { Tank, Eeg, Cabinet, Shelf, Tank };
            return new[] { Shelf, Cabinet, Crates, Iv, Tank, Eeg, Bed };
        }

        private static readonly string[] ClutterMedica = { "wheelchair", "tray_spilled", "broken_glass", "chair_fallen" };
        private static readonly string[] ClutterOther = { "chair_fallen", "broken_glass", "tray_spilled" };

        // Pedido de uma bancada perto de um ponto (a Beta usa para a porta da sala do Sonâmbulo). Atendido antes da mobília.
        internal static Vector2Int? ToolboxNear;

        // Área de início que fica mais limpa (a Beta passa o saguão): sem entulho, mortos nem bancadas e no máximo
        // 2 móveis. Sem área, vale um raio de 6 tiles em volta do início. Vale para uma geração.
        internal static RectInt? QuietArea;
        private const int QuietFurniture = 2;

        private static bool InQuiet(Vector2Int c, Vector2 spawn) =>
            QuietArea.HasValue ? QuietArea.Value.Contains(c) : Vector2.Distance(new Vector2(c.x + 0.5f, c.y + 0.5f), spawn) < 6f;

        // Depois de montar: móveis nas paredes, bancadas com caixa de ferramentas, entulho e sirenes.
        private static void FurnishRooms(Layout map, Transform items, Sprite square, System.Func<int, int, string> wingAt, Vector2 spawn)
        {
            if (PrototypeSceneBuilder.LoadPropSprite("shelf_jars") == null) return;
            var rng = new System.Random(map.Width * 17 + map.Height * 3);
            var parent = new GameObject("Furniture").transform;
            parent.SetParent(items, false);
            if (ToolboxNear.HasValue && !PlaceToolboxNear(map, items, square, ToolboxNear.Value))
                Debug.LogWarning($"[Props] sem parede livre para a caixa de ferramentas perto de {ToolboxNear.Value}");
            ToolboxNear = null;

            var spots = new List<(Vector2Int c, Side side)>();
            for (int y = 1; y < map.Height - 1; y++)
                for (int x = 1; x < map.Width - 1; x++)
                {
                    if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), spawn) < 3f) continue;
                    if (NorthSpot(map, x, y)) spots.Add((new Vector2Int(x, y), Side.North));
                    else if (SideSpot(map, x, y, Side.West)) spots.Add((new Vector2Int(x, y), Side.West));
                    else if (SideSpot(map, x, y, Side.East)) spots.Add((new Vector2Int(x, y), Side.East));
                }
            Shuffle(spots, rng);

            // Bancadas com caixa de ferramentas (sinalizador), só de frente, espalhadas.
            int benches = Mathf.Clamp(spots.Count / 40, 1, 3), made = 0, quiet = 0;
            var benchCells = new List<Vector2Int>();
            foreach (var (c, side) in spots)
            {
                if (made >= benches) break;
                if (side != Side.North || InQuiet(c, spawn) || !Free(c, -1, 1, 0, 0) || benchCells.Exists(b => Vector2Int.Distance(b, c) < 14f)) continue;
                CreateToolboxBench(items, square, c);
                benchCells.Add(c);
                made++;
            }

            int pieces = Mathf.Clamp(spots.Count / 3, 6, 32);
            made = 0;
            foreach (var (c, side) in spots)
            {
                if (made >= pieces) break;
                bool inQuiet = InQuiet(c, spawn);
                if (inQuiet && quiet >= QuietFurniture) continue;
                var options = PiecesFor(wingAt(c.x, c.y));
                var piece = options[rng.Next(options.Length)];
                if (side == Side.North)
                {
                    int depthCells = piece.Depth > 1f ? 2 : 0;
                    if (!Free(c, -1, 1, -depthCells - 1, 0)) continue;
                    PlaceNorth(parent, piece, c);
                    Occupy(c, -1, 1, -depthCells, 0);
                    if (inQuiet) quiet++;
                }
                else
                {
                    if (piece.Side == null || !Free(c, -1, 1, -2, 3)) continue;
                    PlaceSide(parent, piece, c, side);
                    Occupy(c, side == Side.West ? 0 : -2, side == Side.West ? 2 : 0, -1, 2);
                    if (inQuiet) quiet++;
                }
                made++;
            }

            // Entulho no chão aberto.
            var clutter = new GameObject("Clutter").transform;
            clutter.SetParent(items, false);
            int litter = Mathf.Clamp(spots.Count / 6, 3, 14);
            made = 0;
            for (int t = 0; t < 400 && made < litter; t++)
            {
                var c = new Vector2Int(rng.Next(2, map.Width - 2), rng.Next(2, map.Height - 2));
                bool open = true;
                for (int dy = -1; dy <= 1 && open; dy++)
                    for (int dx = -1; dx <= 1 && open; dx++)
                        open = map[c.x + dx, c.y + dy] == '.';
                if (!open || !Free(c, -2, 2, -2, 2) || InQuiet(c, spawn)) continue;
                var kinds = wingAt(c.x, c.y).Contains("Medica") ? ClutterMedica : ClutterOther;
                var go = PrototypeSceneBuilder.CreateCorpse(clutter, $"Clutter_{c.x}_{c.y}", new Vector2(c.x + 0.5f, c.y + 0.2f),
                    kinds[rng.Next(kinds.Length)], rng.Next(2) == 0);
                go.GetComponent<SpriteRenderer>().sortingOrder = -6;
                Occupy(c, -1, 1, -1, 1);
                made++;
            }

            PlaceSirens(map, items, rng);
            QuietArea = null;
        }

        private static void PlaceNorth(Transform parent, Piece p, Vector2Int c)
        {
            var sr = Prop(parent, $"{p.North}_{c.x}_{c.y}", new Vector2(c.x + 0.5f, c.y + 1f - p.Depth), p.North, p.NorthFrames, false);
            if (p.Light != null) Glow(sr.transform, p.Light, new Vector2(0f, 1.3f));
        }

        private static void PlaceSide(Transform parent, Piece p, Vector2Int c, Side side)
        {
            var sprite = PrototypeSceneBuilder.LoadPropSprite(p.Side);
            if (sprite == null) return;
            // Peças simétricas (tanque, soro, caixas) usam o desenho de frente: centralizado, encostado na lateral.
            bool profile = p.Side != p.North;
            Vector2 pos = profile ? SidePosition(c, side, sprite)
                : new Vector2(side == Side.West ? c.x + sprite.bounds.size.x * 0.5f - 0.15f : c.x + 1.15f - sprite.bounds.size.x * 0.5f, c.y + 0.15f);
            var sr = Prop(parent, $"{p.Side}_{c.x}_{c.y}", pos, p.Side, p.SideFrames, side == Side.East && profile);
            if (p.Light != null) Glow(sr.transform, p.Light, new Vector2(0f, 1.3f));
        }

        private static SpriteRenderer Prop(Transform parent, string name, Vector2 pos, string sprite, string[] frames, bool flip)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PrototypeSceneBuilder.LoadPropSprite(sprite);
            sr.flipX = flip;
            sr.sortingOrder = -4;   // móvel encostado, como o armário
            if (frames != null)
            {
                var loop = go.AddComponent<SpriteLoop>();
                var list = new List<Sprite>();
                foreach (var f in frames) { var s = PrototypeSceneBuilder.LoadPropSprite(f); if (s != null) list.Add(s); }
                PrototypeSceneBuilder.SetSprites(loop, "frames", list.ToArray());
            }
            return sr;
        }

        private static void Glow(Transform parent, string kind, Vector2 offset)
        {
            var light = new GameObject("Glow").AddComponent<Light2D>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = offset;
            light.lightType = Light2D.LightType.Point;
            light.color = kind == "verde" ? new Color(0.33f, 0.72f, 0.44f) : new Color(0.49f, 0.29f, 0.54f);
            light.intensity = 0.5f;
            light.pointLightOuterRadius = 1.8f;
        }

        // Bancada com a caixa de ferramentas (FlarePickup): segurar E abre e pega o sinalizador.
        private static void CreateToolboxBench(Transform parent, Sprite square, Vector2Int c)
        {
            var go = new GameObject($"Toolbox_{c.x}_{c.y}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector2(c.x + 0.5f, c.y + 0.5f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.2f, 1.6f);
            var art = new GameObject("toolbox_bench");
            art.transform.SetParent(go.transform, false);
            art.transform.position = new Vector2(c.x + 0.5f, c.y + 1f - 0.45f);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = PrototypeSceneBuilder.LoadPropSprite("toolbox_bench_closed");
            sr.sortingOrder = -4;
            // Brilho vermelho fraco da caixa, para ela chamar a atenção no escuro.
            var light = new GameObject("Glow").AddComponent<Light2D>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.35f, 0.3f);
            light.intensity = 0.3f;
            light.pointLightOuterRadius = 1.2f;
            var pickup = go.AddComponent<FlarePickup>();
            PrototypeSceneBuilder.SetRef(pickup, "art", sr);
            PrototypeSceneBuilder.SetRef(pickup, "openedSprite", PrototypeSceneBuilder.LoadPropSprite("toolbox_bench_open"));
            Occupy(c, -2, 2, 0, 0);
        }

        // Para a Beta: bancada no ponto de parede norte livre mais perto (ex.: a porta da sala do Sonâmbulo).
        internal static bool PlaceToolboxNear(Layout map, Transform parent, Sprite square, Vector2Int near)
        {
            var spot = Nearest(map, near, 40, p => NorthSpot(map, p.x, p.y) && Free(p, -1, 1, 0, 0));
            if (spot == null) return false;
            CreateToolboxBench(parent, square, spot.Value);
            return true;
        }

        // ---------- Sirenes da purga ----------

        // Sirenes de parede (RoomLamp só com alarme) na faixa de cima das paredes norte, a cada ~9 tiles,
        // fora das colunas com móvel alto na frente. Apagadas até a purga; aí giram e pulsam em vermelho.
        private static void PlaceSirens(Layout map, Transform items, System.Random rng)
        {
            var off = PrototypeSceneBuilder.LoadPropSprite("siren_off");
            if (off == null) return;
            var on = PrototypeSceneBuilder.LoadPropFrames("siren_on");
            var parent = new GameObject("Sirens").transform;
            parent.SetParent(items, false);
            var placed = new List<Vector2Int>();
            for (int y = 1; y < map.Height - 2; y++)
                for (int x = 1; x < map.Width - 1; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!map.IsFloor(x, y) || map[x, y + 1] != '#' || map[x, y + 2] != '#' || WallObjects.IndexOf(map[x, y + 1]) >= 0) continue;
                    if (busy.Contains(c) || placed.Exists(p => Vector2Int.Distance(p, c) < 9f)) continue;
                    placed.Add(c);
                    var root = new GameObject($"Siren_{x}_{y}");
                    root.transform.SetParent(parent, false);
                    root.transform.position = new Vector2(x + 0.5f, y + 1.35f);
                    var sr = root.AddComponent<SpriteRenderer>();
                    sr.sprite = off;
                    sr.sortingOrder = -3;
                    var light = new GameObject("Light").AddComponent<Light2D>();
                    light.transform.SetParent(root.transform, false);
                    light.transform.position = new Vector2(x + 0.5f, y - 0.4f);
                    light.lightType = Light2D.LightType.Point;
                    light.color = new Color(1f, 0.15f, 0.12f);
                    light.pointLightOuterRadius = 4.5f;
                    light.falloffIntensity = 0.5f;
                    light.shadowsEnabled = true;
                    light.enabled = false;
                    var lamp = root.AddComponent<RoomLamp>();
                    PrototypeSceneBuilder.SetRef(lamp, "art", sr);
                    PrototypeSceneBuilder.SetRef(lamp, "glow", light);
                    PrototypeSceneBuilder.SetRef(lamp, "offSprite", off);
                    PrototypeSceneBuilder.SetSprites(lamp, "alarmFrames", on);
                }
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
