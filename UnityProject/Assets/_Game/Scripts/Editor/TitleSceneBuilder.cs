using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UpTogether.EditorTools
{
    /// 메인(타이틀) 화면을 조립한다. UpTogether ▸ Build Title Scene.
    /// 흐름: Title → Select → Playground. 코드로 만들어 씬으로 저장한다.
    public static class TitleSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Title.unity";
        const string SelectPath = "Assets/_Game/Select.unity";
        const string PlayPath = "Assets/_Game/Playground.unity";
        const string FontPath = "Assets/_Game/Fonts/Jua-Regular.ttf";
        const string CharDir = "Assets/_Game/Art/Player/Generated";
        const string DogDir = "Assets/_Game/Art/Dog/Generated";

        [MenuItem("UpTogether/Build Title Scene")]
        public static void Build()
        {
            Time.fixedDeltaTime = 1f / Px.FPS;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

            // ── 카메라 (하늘) ──
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4f;
            cam.backgroundColor = Hsl(203f, 0.66f, 0.80f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10f);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var box = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // 언덕 두 겹 (뒤가 옅다)
            WorldSprite("HillBack", box, new Vector3(0f, -4.4f, 0f), 1f, Hsl(120f, 0.32f, 0.72f), -20, new Vector2(26f, 6f));
            WorldSprite("HillFront", box, new Vector3(0f, -3.7f, 0f), 1f, Hsl(112f, 0.40f, 0.62f), -10, new Vector2(28f, 5f));

            // ── 캐릭터 + 강아지 (마지막에 고른 것) ──
            var charSet = LoadSet(CharDir, Selection.Character, Selection.DefaultCharacter);
            var dogSet = LoadSet(DogDir, Selection.Breed, Selection.DefaultBreed);
            WorldSprite("Character", charSet != null ? charSet.Frame(4) : null,
                new Vector3(-0.85f, -0.15f, 0f), 2.5f, Color.white, 5);
            WorldSprite("Dog", dogSet != null ? dogSet.Frame(4) : null,
                new Vector3(0.9f, -0.7f, 0f), 2.0f, Color.white, 6);

            // ── UI ──
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            // 제목 (그림자 → 본문)
            var shadow = Label(canvasGo.transform, font, 132, new Vector2(0.5f, 1f),
                new Vector2(-520f, -430f), new Vector2(520f, -230f), TextAnchor.MiddleCenter);
            shadow.text = "같이 올라가자";
            shadow.color = new Color(0.20f, 0.34f, 0.30f, 0.28f);
            var title = Label(canvasGo.transform, font, 132, new Vector2(0.5f, 1f),
                new Vector2(-520f, -420f), new Vector2(520f, -220f), TextAnchor.MiddleCenter);
            title.text = "같이 올라가자";
            title.color = new Color(0.24f, 0.27f, 0.34f);

            var sub = Label(canvasGo.transform, font, 46, new Vector2(0.5f, 1f),
                new Vector2(-520f, -520f), new Vector2(520f, -440f), TextAnchor.MiddleCenter);
            sub.text = "작은 친구와 나란히, 한 걸음씩";
            sub.color = new Color(0.30f, 0.36f, 0.42f, 0.85f);

            // 시작 버튼
            StartButton(canvasGo.transform, font, box);

            var hint = Label(canvasGo.transform, font, 34, new Vector2(0.5f, 0f),
                new Vector2(-400f, 150f), new Vector2(400f, 210f), TextAnchor.MiddleCenter);
            hint.text = "톡 누르면 시작해요";
            hint.color = new Color(0.32f, 0.38f, 0.44f, 0.7f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScenes();
            Debug.Log($"메인 화면을 만들었습니다: {ScenePath}");
        }

        static void StartButton(Transform parent, Font font, Sprite box)
        {
            var go = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(SceneLink));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(640f, 180f);
            rt.anchoredPosition = new Vector2(0f, 340f);
            var img = go.GetComponent<Image>();
            img.sprite = box; img.type = Image.Type.Sliced;
            img.color = new Color(0.97f, 0.45f, 0.56f);

            var t = Label(go.transform, font, 62, new Vector2(0.5f, 0.5f),
                new Vector2(-300f, -90f), new Vector2(300f, 90f), TextAnchor.MiddleCenter);
            t.text = "게임 시작";
            t.raycastTarget = false;

            go.GetComponent<SceneLink>().scene = "Select";
        }

        static SpriteRenderer WorldSprite(string name, Sprite sprite, Vector3 pos, float scale,
                                          Color color, int order, Vector2? size = null)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = size.HasValue
                ? new Vector3(size.Value.x, size.Value.y, 1f)
                : Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (size.HasValue) { sr.drawMode = SpriteDrawMode.Sliced; sr.size = Vector2.one; }
            return sr;
        }

        static CharacterSpriteSet LoadSet(string dir, string id, string fallback)
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>($"{dir}/{id}.asset");
            if (set == null) set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>($"{dir}/{fallback}.asset");
            return set;
        }

        static Text Label(Transform parent, Font font, int size, Vector2 anchor,
                          Vector2 offMin, Vector2 offMax, TextAnchor align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = align;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static Color Hsl(float h, float s, float l)
        {
            float c = (1f - Mathf.Abs(2f * l - 1f)) * s;
            float hp = Mathf.Repeat(h, 360f) / 60f;
            float x = c * (1f - Mathf.Abs(hp % 2f - 1f));
            float r = 0, g = 0, b = 0;
            switch (Mathf.FloorToInt(hp))
            {
                case 0: r = c; g = x; break;
                case 1: r = x; g = c; break;
                case 2: g = c; b = x; break;
                case 3: g = x; b = c; break;
                case 4: r = x; b = c; break;
                default: r = c; b = x; break;
            }
            float m = l - c * 0.5f;
            return new Color(r + m, g + m, b + m);
        }

        /// Title 첫 씬, 그다음 Select, Playground.
        internal static void RegisterScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(SelectPath, true),
                new EditorBuildSettingsScene(PlayPath, true),
            };
        }

        /// 세 씬을 한 번에 다시 만든다.
        [MenuItem("UpTogether/Build All Scenes")]
        public static void BuildAll()
        {
            SceneBuilder.Build();
            SelectSceneBuilder.Build();
            Build();
        }
    }
}
