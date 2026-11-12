using SombraDaSanidade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SombraDaSanidade.EditorTools
{
    // Gera Scenes/Main/Menu.unity: câmera preta, o MainMenuScreen (kit CRT de art-src/UI), a música de fundo,
    // a cutscene de abertura (StoryIntro) e os rótulos do rodapé, os dois em Canvas com TextMeshPro (VT323).
    // Os Canvas usam CanvasScaler por tamanho de tela e âncoras nos cantos/centro, então mantêm o aspecto
    // em qualquer resolução. O Menu é a cena 0 do Build Profile e a primeira da build WebGL (WebGLBuilder), #32.
    public static class MenuSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main/Menu.unity";
        public const string MusicPath = "Assets/_Project/Audio/Music/menu_drone.wav";

        // Mesma resolução de referência do HUD em pixel art (PixelHud).
        private static readonly Vector2 ReferenceResolution = new Vector2(640, 360);

        [MenuItem("Sombra da Sanidade/Criar cena de Menu", priority = 20)]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var font = FontAssetBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PixelHud.Vazio;
            camera.gameObject.AddComponent<AudioListener>();

            new GameObject("MainMenu").AddComponent<MainMenuScreen>();
            CreateMusic();
            CreateFooter(font);
            CreateStoryIntro(font);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Menu] Cena criada em {ScenePath}.");
        }

        private static void CreateMusic()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            if (clip == null) Debug.LogWarning($"[Menu] {MusicPath} não existe. Rode python3 tools/audio/gen_menu_music.py.");
            var source = new GameObject("MenuMusic").AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = 0.7f;
            source.spatialBlend = 0f;
        }

        // Créditos e versão ancorados nos cantos de baixo.
        private static void CreateFooter(TMP_FontAsset font)
        {
            var canvas = CreateCanvas("FooterCanvas", 5);
            var credits = CreateText(canvas.transform, "Credits", font, "JOGOS DIGITAIS · BSI/UFRPE 2026", 14, PixelHud.Fosforo3,
                TextAlignmentOptions.BottomLeft);
            Anchor(credits.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 8), new Vector2(260, 20));
            var version = CreateText(canvas.transform, "Version", font, "VERSÃO BETA", 14, PixelHud.Fosforo3,
                TextAlignmentOptions.BottomRight);
            Anchor(version.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 8), new Vector2(160, 20));
        }

        private static void CreateStoryIntro(TMP_FontAsset font)
        {
            var canvas = CreateCanvas("StoryCanvas", 20);
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvas.transform, false);
            background.GetComponent<Image>().color = PixelHud.Vazio;
            var bg = (RectTransform)background.transform;
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.offsetMin = bg.offsetMax = Vector2.zero;

            var title = CreateText(canvas.transform, "Title", font, "SOMBRA DA SANIDADE", 32, PixelHud.Ambar1, TextAlignmentOptions.Top);
            Anchor(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(560, 40));

            var body = CreateText(canvas.transform, "Body", font, "", 22, PixelHud.Fosforo5, TextAlignmentOptions.Center);
            Anchor(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 200));
            body.textWrappingMode = TextWrappingModes.Normal;

            var hint = CreateText(canvas.transform, "Hint", font, "", 14, PixelHud.Fosforo3, TextAlignmentOptions.BottomRight);
            Anchor(hint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 8), new Vector2(400, 20));

            var intro = canvas.gameObject.AddComponent<StoryIntro>();
            var so = new SerializedObject(intro);
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("hint").objectReferenceValue = hint;
            so.FindProperty("root").objectReferenceValue = canvas.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, string text, float size,
            Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // Batchmode: -executeMethod SombraDaSanidade.EditorTools.MenuSceneBuilder.BuildBatch
        public static void BuildBatch()
        {
            Build();
            EditorApplication.Exit(0);
        }
    }
}
