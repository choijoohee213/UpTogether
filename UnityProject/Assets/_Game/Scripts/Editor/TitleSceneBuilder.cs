using System.Collections.Generic;
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

            // ── 포옹 그룹 (마지막에 고른 캐릭터·강아지) ──
            // 3겹: 본체 → 강아지 → 앞팔. 강아지는 본체의 자식이라 위치·크기가 같이 따라간다.
            // 스프라이트는 TitleHug 가 런타임에 Selection 으로 채운다. 여기 건 에디터 미리보기용.
            var bodyGo = new GameObject("Character");
            bodyGo.transform.position = new Vector3(-0.5f, -1.2f, 0f);
            bodyGo.transform.localScale = Vector3.one * 2.5f;
            var body = bodyGo.AddComponent<SpriteRenderer>();
            body.sortingOrder = 5;

            var dogGo = new GameObject("Dog");
            dogGo.transform.SetParent(bodyGo.transform, false);
            // 캐릭터 피벗 기준 (28.5, -7)px. Px.PPU 로 유닛 변환 (게임의 안기 오프셋과 같은 규칙).
            dogGo.transform.localPosition = new Vector3(28.5f / Px.PPU, -7f / Px.PPU, 0f);
            dogGo.transform.localScale = new Vector3(-1f, 1f, 1f);   // 마주보게 뒤집기
            var dog = dogGo.AddComponent<SpriteRenderer>();
            dog.sortingOrder = 6;

            var armsGo = new GameObject("Arms");
            armsGo.transform.SetParent(bodyGo.transform, false);
            var arms = armsGo.AddComponent<SpriteRenderer>();
            arms.sortingOrder = 7;

            var cset0 = LoadSet(CharDir, Selection.Character, Selection.DefaultCharacter);
            var dset0 = LoadSet(DogDir, Selection.Breed, Selection.DefaultBreed);
            if (cset0 != null) { body.sprite = cset0.Frame(18); arms.sprite = cset0.Frame(20); }
            if (dset0 != null) dog.sprite = dset0.Frame(11);

            var hug = bodyGo.AddComponent<TitleHug>();
            hug.body = body; hug.dog = dog; hug.arms = arms;
            hug.characters = LoadEntries(CharDir);
            hug.breeds = LoadEntries(DogDir);

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

        /// 폴더의 모든 세트를 id 와 함께 모은다(빌드에 포함). TitleHug 가 런타임에 고른다.
        static SelectionApplier.Entry[] LoadEntries(string dir)
        {
            var list = new List<SelectionApplier.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:CharacterSpriteSet", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(path);
                if (set == null) continue;
                list.Add(new SelectionApplier.Entry
                { id = System.IO.Path.GetFileNameWithoutExtension(path), set = set });
            }
            return list.ToArray();
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
