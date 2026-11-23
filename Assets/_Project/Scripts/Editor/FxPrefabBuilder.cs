using System.IO;
using SombraDaSanidade.Flashlight;
using UnityEditor;
using UnityEngine;

namespace SombraDaSanidade.EditorTools
{
    // Gera os efeitos de partícula do #81 (planejamento-rc 2): material, prefabs em Prefabs/FX/ e o GameFx em
    // Resources. Partículas quadradas de 1 a 2 px (16 PPU), só com cores de art-src/palette.json.
    // Mudar um efeito = editar aqui e gerar de novo; não editar os prefabs à mão.
    // Menu: Sombra da Sanidade > Arte > Gerar efeitos de partícula.
    // Batchmode: -executeMethod SombraDaSanidade.EditorTools.FxPrefabBuilder.BuildBatch -quit
    public static class FxPrefabBuilder
    {
        public const string PrefabDir = "Assets/_Project/Prefabs/FX";
        public const string MaterialPath = "Assets/_Project/Art/FX/fx_unlit.mat";
        public const string GameFxPath = "Assets/_Project/Resources/" + GameFx.ResourcePath + ".asset";
        // Sprite sem luz do URP 2D: usa a cor da partícula (cor do vértice) e não escurece no escuro.
        public const string ShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        private const float Pixel = 1f / 16f;
        private const int SortingOrder = 60;   // acima de props e personagens

        // Paleta (art-src/palette.json)
        private static readonly Color Eletrico3 = Hex(0x2d6ad0);
        private static readonly Color Eletrico4 = Hex(0x5bb2ff);
        private static readonly Color Eletrico5 = Hex(0xc6eeff);
        private static readonly Color Fosforo4 = Hex(0x55b870);
        private static readonly Color Fosforo5 = Hex(0xa9f294);
        private static readonly Color Ambar2 = Hex(0xf6d878);
        private static readonly Color Emergencia5 = Hex(0xff8f6e);
        private static readonly Color Concreto4 = Hex(0x444b59);
        private static readonly Color Concreto7 = Hex(0xa1a8b2);

        [MenuItem("Sombra da Sanidade/Arte/Gerar efeitos de partícula", priority = 120)]
        public static void Build()
        {
            var mat = BuildMaterial();
            Directory.CreateDirectory(PrefabDir);

            var fx = AssetDatabase.LoadAssetAtPath<GameFx>(GameFxPath);
            if (fx == null)
            {
                fx = ScriptableObject.CreateInstance<GameFx>();
                AssetDatabase.CreateAsset(fx, GameFxPath);
            }
            fx.empWave = Save(BuildEmpWave(mat));
            fx.skillGreat = Save(BuildSkillGreat(mat));
            fx.skillFail = Save(BuildSkillFail(mat));
            EditorUtility.SetDirty(fx);
            AssetDatabase.SaveAssets();
            Debug.Log("[FX] 3 efeitos gerados em " + PrefabDir);
        }

        public static void BuildBatch() => Build();

        private static Material BuildMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var shader = Shader.Find(ShaderName);
            if (shader == null) Debug.LogError("[FX] Shader não encontrado: " + ShaderName);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            else mat.shader = shader;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // EMP: anel azul que se abre do Thomas e some em 0,4 s, com um clarão curto no centro.
        private static GameObject BuildEmpWave(Material mat)
        {
            var root = NewSystem("FX_EmpWave", mat, out var ps);
            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(6.5f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(Pixel * 2f);
            Burst(ps, 72);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            shape.radiusThickness = 0f;   // só a borda: vira anel
            shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
            Fade(ps, Eletrico5, Eletrico3);

            var flash = NewSystem("Clarao", mat, out var fps, root.transform);
            var fm = fps.main;
            fm.duration = 0.15f;
            fm.startLifetime = 0.15f;
            fm.startSpeed = 0f;
            fm.startSize = 1.5f;
            Burst(fps, 1);
            Fade(fps, Eletrico4, Eletrico3);
            return root;
        }

        // Great: chuva de pontos verdes saindo do mostrador do multímetro.
        private static GameObject BuildSkillGreat(Material mat)
        {
            var root = NewSystem("FX_SkillGreat", mat, out var ps);
            var main = ps.main;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(Pixel, Pixel * 2f);
            main.gravityModifier = 0.25f;
            Burst(ps, 26);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;
            Drag(ps, 2f);
            Fade(ps, Fosforo5, Fosforo4);
            return root;
        }

        // Fail: faíscas âmbar que caem e fumaça cinza que sobe do servidor.
        private static GameObject BuildSkillFail(Material mat)
        {
            var root = NewSystem("FX_SkillFail", mat, out var ps);
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(Pixel, Pixel * 1.5f);
            main.gravityModifier = 0.8f;
            Burst(ps, 20);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;
            Drag(ps, 1.5f);
            Fade(ps, Ambar2, Emergencia5);

            var smoke = NewSystem("Fumaca", mat, out var sps, root.transform);
            var sm = sps.main;
            sm.duration = 0.6f;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            sm.startSize = new ParticleSystem.MinMaxCurve(Pixel * 2f, Pixel * 3f);
            var em = sps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 4, 3, 0.15f) });
            var ss = sps.shape;
            ss.enabled = true;
            ss.shapeType = ParticleSystemShapeType.Circle;
            ss.radius = 0.15f;
            var vel = sps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.7f);   // sobe na tela
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            var size = sps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 2.5f));
            Fade(sps, Concreto7, Concreto4, 0.7f);
            return root;
        }

        // ---------- utilidades ----------

        private static GameObject NewSystem(string name, Material mat, out ParticleSystem ps, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;
            // A raiz se destrói quando o próprio sistema e os filhos acabam.
            main.stopAction = parent == null ? ParticleSystemStopAction.Destroy : ParticleSystemStopAction.None;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = parent == null ? SortingOrder : SortingOrder - 1;
            return go;
        }

        private static void Burst(ParticleSystem ps, short count)
        {
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        }

        private static void Drag(ParticleSystem ps, float drag)
        {
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = drag;
        }

        // Cor ao longo da vida em degraus (GradientMode.Fixed): `from` na 1ª metade, `to` na 2ª, e o alfa caindo em
        // 3 passos. A cor inicial fica branca, então a partícula só mostra cores da paleta (sem mistura).
        private static void Fade(ParticleSystem ps, Color from, Color to, float startAlpha = 1f)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient { mode = GradientMode.Fixed };
            g.SetKeys(
                new[] { new GradientColorKey(from, 0.5f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(startAlpha, 0.45f), new GradientAlphaKey(startAlpha * 0.6f, 0.75f),
                        new GradientAlphaKey(startAlpha * 0.25f, 1f) });
            col.color = g;
        }

        private static GameObject Save(GameObject root)
        {
            string path = $"{PrefabDir}/{root.name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, 1f);
    }
}
