using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera a planta da cena Beta a partir de uma seed, no mesmo formato de caracteres da art-src/Maps/alfa.txt
    // (legenda lá). Saguão embaixo no centro com a eclusa, Ala Médica a oeste, Ala de Energia a leste e
    // Ala Executiva ao norte. Cada ala tem 3 ou 4 salas ligadas por corredores em loop (GDD 4.1, sem becos),
    // um servidor de HD, disjuntores, armários, itens, terminal de log e pontos de patrulha.
    // A Energia ganha duas salas lado a lado, separadas por uma parede fina: o atalho só do monstro (#37).
    // Determinístico: a mesma seed gera o mesmo mapa.
    internal static class BetaMapGenerator
    {
        public const int Width = 78;
        public const int Height = 60;
        private const int Gap = 3;          // paredes entre salas: vista 3/4 + servidor de 2 tiles
        private const int MaxRetries = 20;

        public const string Medica = "Medica";
        public const string Energia = "Energia";
        public const string Executiva = "Executiva";
        public const string Saguao = "Saguao";

        // Regiões das alas (x0, y0, x1, y1 inclusivos). O saguão fica entre Médica e Energia, embaixo.
        private static readonly RectInt HubRect = new RectInt(31, 3, 16, 9);
        private static readonly RectInt MedicaRegion = new RectInt(2, 3, 25, 54);
        private static readonly RectInt EnergiaRegion = new RectInt(51, 3, 25, 54);
        private static readonly RectInt ExecutivaRegion = new RectInt(29, 17, 20, 40);

        public class Info
        {
            public int Seed;
            public readonly Dictionary<string, List<RectInt>> Rooms = new Dictionary<string, List<RectInt>>();
            // Pares de células de chão (centro) ligadas só pelo monstro.
            public readonly List<(Vector2 from, Vector2 to)> Shortcuts = new List<(Vector2, Vector2)>();

            public string WingAt(int x, int y)
            {
                if (x < 29) return Medica;
                if (x >= 50) return Energia;
                return y >= 15 ? Executiva : Saguao;
            }

            public static RectInt RegionOf(string wing) => wing switch
            {
                Medica => MedicaRegion,
                Energia => EnergiaRegion,
                Executiva => ExecutivaRegion,
                _ => HubRect
            };
        }

        // Gera a planta; se a seed der um mapa inválido, tenta outras derivadas dela. info.Seed é a usada.
        public static AlfaSceneBuilder.Layout Generate(int seed, out Info info)
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                int s = seed + attempt * 1000;
                var cells = TryGenerate(s, out info, out string problem);
                if (problem == null) return ToLayout(cells);
                Debug.Log($"[Beta] seed {s} descartada: {problem}");
            }
            info = null;
            Debug.LogError($"[Beta] nenhuma planta válida a partir da seed {seed}.");
            return null;
        }

        // Planta em texto (mesmo formato da alfa.txt), para conferir ou versionar.
        public static string ToText(AlfaSceneBuilder.Layout map, int seed)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Planta gerada pelo BetaMapGenerator (seed {seed}). Não editar: gere de novo pelo menu Sombra da Sanidade > Beta.");
            sb.AppendLine("# Mesma legenda de alfa.txt. A Medica fica a oeste, a Energia a leste, a Executiva ao norte e o saguao embaixo.");
            for (int y = map.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < map.Width; x++) sb.Append(map.Cells[x, y]);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static AlfaSceneBuilder.Layout ToLayout(char[,] cells) =>
            new AlfaSceneBuilder.Layout { Width = Width, Height = Height, Cells = cells };

        // ---------- Geração ----------

        private static char[,] TryGenerate(int seed, out Info info, out string problem)
        {
            var rng = new System.Random(seed);
            info = new Info { Seed = seed };
            var c = new char[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    c[x, y] = '#';

            // Salas.
            var all = new List<RectInt> { HubRect };
            info.Rooms[Saguao] = new List<RectInt> { HubRect };
            info.Rooms[Medica] = PlaceRooms(rng, MedicaRegion, rng.Next(3, 5), all);
            var energia = PlaceEnergia(rng, all, out var pair);
            // A parede entre as duas salas do atalho não pode ser cavada por corredor, senão o atalho perde o sentido.
            Protected.Clear();
            if (energia.Count >= 2)
                for (int x = energia[pair.a].xMax; x < energia[pair.b].x; x++)
                    for (int y = energia[pair.a].y - 1; y <= energia[pair.a].yMax; y++)
                        Protected.Add(new Vector2Int(x, y));
            info.Rooms[Energia] = energia;
            info.Rooms[Executiva] = PlaceRooms(rng, ExecutivaRegion, rng.Next(3, 5), all);
            if (info.Rooms[Medica].Count < 3 || energia.Count < 3 || info.Rooms[Executiva].Count < 3)
            {
                problem = "salas insuficientes";
                return null;
            }
            foreach (var r in all) Fill(c, r, '.');

            // Corredores: árvore mínima em cada ala + um laço extra, cada ala ligada ao saguão
            // e as alas laterais ligadas à Executiva (laços grandes, sem becos).
            foreach (var wing in new[] { Medica, Energia, Executiva })
            {
                var rooms = info.Rooms[wing];
                var skip = wing == Energia ? pair : (-1, -1);
                var edges = Mst(rooms, skip.Item1, skip.Item2);
                foreach (var (i, j) in edges) Corridor(c, rng, rooms[i], rooms[j]);
                var extra = ExtraEdge(rng, rooms, edges, skip.Item1, skip.Item2);
                if (extra.HasValue) Corridor(c, rng, rooms[extra.Value.i], rooms[extra.Value.j]);
                Corridor(c, rng, Nearest(rooms, HubRect), HubRect);
            }
            Corridor(c, rng, Nearest(info.Rooms[Medica], info.Rooms[Executiva]), Nearest(info.Rooms[Executiva], info.Rooms[Medica]));
            Corridor(c, rng, Nearest(info.Rooms[Energia], info.Rooms[Executiva]), Nearest(info.Rooms[Executiva], info.Rooms[Energia]));

            // Eclusa na parede norte do saguão (porta dupla de 4 tiles), num trecho que nenhum corredor cortou.
            int ey = HubRect.yMax;
            int ex = -1;
            for (int x = HubRect.x + 1; x <= HubRect.xMax - 5 && ex < 0; x++)
            {
                bool solid = true;
                for (int dx = -1; dx <= 4 && solid; dx++)
                    for (int dy = 0; dy <= 2 && solid; dy++)
                        solid = c[x + dx, ey + dy] == '#';
                if (solid) ex = x;
            }
            if (ex < 0)
            {
                problem = "sem parede livre para a eclusa";
                return null;
            }
            for (int x = ex; x < ex + 4; x++)
            {
                c[x, ey] = 'E';
                c[x, ey + 1] = '#';
                c[x, ey + 2] = '#';
            }
            c[HubRect.x + HubRect.width / 2, HubRect.y + 1] = '@';
            c[ex + 1, ey - 3] = 'L';

            // Objetos por ala.
            var used = new HashSet<Vector2Int>();
            for (int x = ex - 1; x < ex + 5; x++) used.Add(new Vector2Int(x, ey));
            PlaceWing(c, rng, info.Rooms[Medica], used, items: "i");
            PlaceWing(c, rng, info.Rooms[Energia], used, items: "a");
            PlaceWing(c, rng, info.Rooms[Executiva], used, items: "ia");

            // Atalho da Energia: meio das paredes laterais que se encaram.
            var ra = energia[pair.a];
            var rb = energia[pair.b];
            int sy = Mathf.Max(ra.y, rb.y) + (Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.y, rb.y)) / 2;
            var from = new Vector2Int(ra.xMax - 1, sy);
            var to = new Vector2Int(rb.x, sy);
            c[from.x, from.y] = ',';
            c[to.x, to.y] = ',';
            info.Shortcuts.Add((new Vector2(from.x + 0.5f, from.y + 0.5f), new Vector2(to.x + 0.5f, to.y + 0.5f)));

            // Monstro na sala da Executiva mais longe do saguão; patrulha nos centros e nos cruzamentos.
            var far = info.Rooms[Executiva].OrderByDescending(r => Vector2.Distance(r.center, HubRect.center)).First();
            SetFloor(c, Center(far) + new Vector2Int(1, 0), 'M');
            foreach (var r in all.Where(r => r != HubRect)) SetFloor(c, Center(r), 'm');
            SetFloor(c, Center(HubRect) + new Vector2Int(-4, 0), 'm');
            SetFloor(c, Center(HubRect) + new Vector2Int(4, 0), 'm');
            AddCorridorPatrol(c, rng);

            Decorate(c, rng);
            problem = Validate(c, info);
            return c;
        }

        private static List<RectInt> PlaceRooms(System.Random rng, RectInt region, int count, List<RectInt> all)
        {
            var rooms = new List<RectInt>();
            for (int tries = 0; tries < 400 && rooms.Count < count; tries++)
            {
                int w = rng.Next(7, 12), h = rng.Next(6, 9);
                if (w > region.width || h > region.height) continue;
                var r = new RectInt(rng.Next(region.x, region.xMax - w + 1), rng.Next(region.y, region.yMax - h + 1), w, h);
                if (Overlaps(r, all)) continue;
                rooms.Add(r);
                all.Add(r);
            }
            return rooms;
        }

        // Energia: duas salas lado a lado com parede de 3 tiles entre elas (o atalho), mais 1 ou 2 salas soltas.
        private static List<RectInt> PlaceEnergia(System.Random rng, List<RectInt> all, out (int a, int b) pair)
        {
            var region = EnergiaRegion;
            var rooms = new List<RectInt>();
            pair = (0, 1);
            for (int tries = 0; tries < 400 && rooms.Count == 0; tries++)
            {
                int w1 = rng.Next(7, 10), w2 = rng.Next(7, 10), h = rng.Next(6, 9);
                if (w1 + Gap + w2 > region.width) continue;
                int x = rng.Next(region.x, region.xMax - (w1 + Gap + w2) + 1);
                int y = rng.Next(region.y + 8, region.yMax - h + 1);
                var r1 = new RectInt(x, y, w1, h);
                var r2 = new RectInt(x + w1 + Gap, y, w2, h);
                if (Overlaps(r1, all) || Overlaps(r2, all)) continue;
                rooms.Add(r1);
                rooms.Add(r2);
                all.Add(r1);
                all.Add(r2);
            }
            rooms.AddRange(PlaceRooms(rng, region, rng.Next(1, 3), all));
            return rooms;
        }

        private static bool Overlaps(RectInt r, List<RectInt> others)
        {
            var grown = new RectInt(r.x - Gap, r.y - Gap, r.width + Gap * 2, r.height + Gap * 2);
            return others.Any(o => grown.Overlaps(o));
        }

        // Árvore mínima (Prim) pelos centros; o par do atalho nunca é ligado direto.
        private static List<(int i, int j)> Mst(List<RectInt> rooms, int skipA, int skipB)
        {
            var edges = new List<(int, int)>();
            var inTree = new HashSet<int> { 0 };
            while (inTree.Count < rooms.Count)
            {
                float best = float.MaxValue;
                (int, int) pick = (-1, -1);
                foreach (int i in inTree)
                    for (int j = 0; j < rooms.Count; j++)
                    {
                        if (inTree.Contains(j) || IsPair(i, j, skipA, skipB)) continue;
                        float d = Vector2.Distance(rooms[i].center, rooms[j].center);
                        if (d < best) { best = d; pick = (i, j); }
                    }
                if (pick.Item1 < 0) break;
                edges.Add(pick);
                inTree.Add(pick.Item2);
            }
            return edges;
        }

        private static (int i, int j)? ExtraEdge(System.Random rng, List<RectInt> rooms, List<(int i, int j)> edges, int skipA, int skipB)
        {
            var options = new List<(int, int)>();
            for (int i = 0; i < rooms.Count; i++)
                for (int j = i + 1; j < rooms.Count; j++)
                    if (!IsPair(i, j, skipA, skipB) && !edges.Contains((i, j)) && !edges.Contains((j, i)))
                        options.Add((i, j));
            if (options.Count == 0) return null;
            return options[rng.Next(options.Count)];
        }

        // Só a Energia tem par de atalho; nas outras alas os índices 0 e 1 podem se ligar normalmente.
        private static bool IsPair(int i, int j, int a, int b) => a >= 0 && (i == a && j == b || i == b && j == a);

        private static RectInt Nearest(List<RectInt> rooms, RectInt target) =>
            rooms.OrderBy(r => Vector2.Distance(r.center, target.center)).First();

        private static RectInt Nearest(List<RectInt> rooms, List<RectInt> targets) =>
            rooms.OrderBy(r => targets.Min(t => Vector2.Distance(r.center, t.center))).First();

        // Corredor em L de 2 tiles. Onde cava parede vira grade (","); chão de sala fica como está.
        private static void Corridor(char[,] c, System.Random rng, RectInt a, RectInt b)
        {
            var p = Center(a);
            var q = Center(b);
            bool horizontalFirst = rng.Next(2) == 0;
            var corner = horizontalFirst ? new Vector2Int(q.x, p.y) : new Vector2Int(p.x, q.y);
            Dig(c, p, corner);
            Dig(c, corner, q);
        }

        private static readonly HashSet<Vector2Int> Protected = new HashSet<Vector2Int>();

        private static void Dig(char[,] c, Vector2Int from, Vector2Int to)
        {
            int x0 = Mathf.Min(from.x, to.x), x1 = Mathf.Max(from.x, to.x);
            int y0 = Mathf.Min(from.y, to.y), y1 = Mathf.Max(from.y, to.y);
            for (int x = x0; x <= x1 + 1; x++)
                for (int y = y0; y <= y1 + 1; y++)
                    if (x > 0 && y > 0 && x < Width - 1 && y < Height - 1 && c[x, y] == '#' && !Protected.Contains(new Vector2Int(x, y)))
                        c[x, y] = ',';
        }

        private static void PlaceWing(char[,] c, System.Random rng, List<RectInt> rooms, HashSet<Vector2Int> used, string items)
        {
            // Servidor na sala mais longe do saguão; disjuntores e terminal nas outras.
            var ordered = rooms.OrderByDescending(r => Vector2.Distance(r.center, HubRect.center)).ToList();
            PlaceOnNorthWall(c, rng, ordered[0], 'S', used, needsTwoRows: true);
            PlaceOnNorthWall(c, rng, ordered[1], 'K', used, needsTwoRows: false, lamp: true);
            if (ordered.Count > 2) PlaceOnNorthWall(c, rng, ordered[2], 'K', used, needsTwoRows: false, lamp: true);
            PlaceOnNorthWall(c, rng, ordered[ordered.Count - 1], 'T', used, needsTwoRows: false);
            foreach (var r in rooms)
                if (rng.Next(3) == 0) PlaceOnNorthWall(c, rng, r, 'P', used, needsTwoRows: false);

            // Armários encostados na parede norte (2 ou 3 por ala) e os itens no meio das salas.
            int lockers = rng.Next(2, 4);
            for (int i = 0; i < lockers; i++)
            {
                var r = rooms[rng.Next(rooms.Count)];
                for (int t = 0; t < 20; t++)
                {
                    var cell = new Vector2Int(rng.Next(r.x + 1, r.xMax - 1), r.yMax - 1);
                    if (c[cell.x, cell.y] != '.' || used.Contains(cell + Vector2Int.up)) continue;
                    c[cell.x, cell.y] = 'A';
                    used.Add(cell + Vector2Int.up);
                    break;
                }
            }
            foreach (char item in items)
            {
                var r = rooms[rng.Next(rooms.Count)];
                for (int t = 0; t < 20; t++)
                {
                    var cell = new Vector2Int(rng.Next(r.x + 1, r.xMax - 1), rng.Next(r.y + 1, r.yMax - 2));
                    if (c[cell.x, cell.y] != '.') continue;
                    c[cell.x, cell.y] = item;
                    break;
                }
            }
        }

        // Objeto de parede (servidor, disjuntor, terminal, canos) na face norte da sala: chão logo abaixo,
        // parede acima (duas fileiras para o servidor, que desenha a parte de cima no tile de cima).
        private static void PlaceOnNorthWall(char[,] c, System.Random rng, RectInt r, char what, HashSet<Vector2Int> used,
            bool needsTwoRows, bool lamp = false)
        {
            int y = r.yMax;
            for (int t = 0; t < 30; t++)
            {
                int x = rng.Next(r.x + 1, r.xMax - 1);
                var cell = new Vector2Int(x, y);
                if (used.Contains(cell) || used.Contains(cell + Vector2Int.left) || used.Contains(cell + Vector2Int.right)) continue;
                if (c[x, y] != '#' || c[x, y - 1] != '.') continue;
                if (c[x, y + 1] != '#' || needsTwoRows && c[x, y + 2] != '#') continue;
                c[x, y] = what;
                used.Add(cell);
                if (lamp && c[x, y - 3] == '.') c[x, y - 3] = 'L';
                return;
            }
        }

        // Pontos de patrulha em cruzamentos de corredor (grade com 3+ vizinhos de chão em cruz).
        private static void AddCorridorPatrol(char[,] c, System.Random rng)
        {
            int added = 0;
            for (int y = 2; y < Height - 2 && added < 10; y += 2)
                for (int x = 2; x < Width - 2 && added < 10; x += 2)
                {
                    if (c[x, y] != ',' || rng.Next(4) != 0) continue;
                    int n = (IsFloor(c[x + 2, y]) ? 1 : 0) + (IsFloor(c[x - 1, y]) ? 1 : 0) + (IsFloor(c[x, y + 2]) ? 1 : 0) + (IsFloor(c[x, y - 1]) ? 1 : 0);
                    if (n < 3) continue;
                    c[x, y] = 'm';
                    added++;
                }
        }

        // Poças, papéis, sangue e faixa de perigo espalhados no chão liso (guia de estilo, regra 7).
        private static void Decorate(char[,] c, System.Random rng)
        {
            for (int x = 1; x < Width - 1; x++)
                for (int y = 1; y < Height - 1; y++)
                {
                    if (c[x, y] != '.') continue;
                    int roll = rng.Next(100);
                    if (roll < 2) c[x, y] = 'w';
                    else if (roll < 4) c[x, y] = 'p';
                    else if (roll < 5) c[x, y] = 'b';
                    else if (roll < 6 && (c[x, y + 1] == 'K' || c[x, y + 2] == 'K')) c[x, y] = 'h';
                }
        }

        // ---------- Validação ----------

        private static string Validate(char[,] c, Info info)
        {
            var spawn = Find(c, '@').FirstOrDefault();
            var reach = new bool[Width, Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(spawn);
            reach[spawn.x, spawn.y] = true;
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var n = p + d;
                    if (n.x < 0 || n.y < 0 || n.x >= Width || n.y >= Height || reach[n.x, n.y] || !IsFloor(c[n.x, n.y])) continue;
                    reach[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            if (Find(c, 'S').Count != 3) return "precisa de 3 servidores";
            if (Find(c, 'K').Count < 3) return "menos de 3 disjuntores";
            if (Find(c, 'A').Count < 6) return "menos de 6 armários";
            if (Find(c, 'T').Count < 3) return "menos de 3 terminais";
            if (Find(c, 'M').Count != 1) return "sem monstro";
            if (!Find(c, 'i').Any() || !Find(c, 'a').Any()) return "faltam itens";

            // Objetos de parede: o chão em frente precisa ser alcançável. Objetos de chão: a própria célula.
            foreach (char w in "SKTE")
                foreach (var p in Find(c, w))
                    if (!reach[p.x, p.y - 1]) return $"{w} em ({p.x},{p.y}) inalcançável";
            foreach (char f in "AiaMm")
                foreach (var p in Find(c, f))
                    if (!reach[p.x, p.y]) return $"{f} em ({p.x},{p.y}) inalcançável";
            foreach (var (from, to) in info.Shortcuts)
                if (!reach[(int)from.x, (int)from.y] || !reach[(int)to.x, (int)to.y]) return "atalho inalcançável";
            return null;
        }

        // Mesmos caracteres de chão do AlfaSceneBuilder.
        private static bool IsFloor(char ch) => ".,hwpbL@MmiAa".IndexOf(ch) >= 0;

        private static List<Vector2Int> Find(char[,] c, char what)
        {
            var list = new List<Vector2Int>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (c[x, y] == what) list.Add(new Vector2Int(x, y));
            return list;
        }

        public static int Count(AlfaSceneBuilder.Layout map, char what) => Find(map.Cells, what).Count;

        private static Vector2Int Center(RectInt r) => new Vector2Int(r.x + r.width / 2, r.y + r.height / 2);

        private static void SetFloor(char[,] c, Vector2Int p, char what)
        {
            if (c[p.x, p.y] == '.' || c[p.x, p.y] == ',') c[p.x, p.y] = what;
        }

        private static void Fill(char[,] c, RectInt r, char what)
        {
            for (int x = r.x; x < r.xMax; x++)
                for (int y = r.y; y < r.yMax; y++)
                    c[x, y] = what;
        }
    }
}
