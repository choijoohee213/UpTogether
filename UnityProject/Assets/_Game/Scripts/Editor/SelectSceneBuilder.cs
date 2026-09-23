using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UpTogether.EditorTools
{
    /// 시작 화면(캐릭터·강아지 선택)을 조립한다. UpTogether ▸ Build Select Scene.
    /// 게임 씬과 같은 방식으로 코드로 만든다.
    public static class SelectSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Select.unity";
        const string FontPath = "Assets/_Game/Fonts/Jua-Regular.ttf";
        const string CharDir = "Assets/_Game/Art/Player/Generated";
        const string DogDir = "Assets/_Game/Art/Dog/Generated";
        const string HeroJson = "Assets/_Game/Art/Player/hero_sprites_v2/hero_sprites.json";

        [MenuItem("UpTogether/Build Select Scene")]
        public static void Build()
        {
            Time.fixedDeltaTime = 1f / Px.FPS;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var chars = LoadCharacters();
            var dogs = LoadDogs();
            if (chars.Count == 0 || dogs.Count == 0)
            {
                Debug.LogError("구워진 캐릭터/견종 에셋이 없습니다. Import Player/Dog Art 를 먼저 실행하세요.");
                return;
            }

            // ── 카메라 (배경) ──
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4f;
            cam.backgroundColor = new Color(0.68f, 0.85f, 0.95f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10f);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            // ── 미리보기 스프라이트 (월드) ──
            var charPrev = MakePreview("CharPreview", new Vector3(-1.6f, 0.6f, 0f), 2.2f);
            var dogPrev = MakePreview("DogPreview", new Vector3(1.6f, 0.3f, 0f), 2.0f);

            // ── 캔버스 ──
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var screen = canvasGo.AddComponent<SelectScreen>();
            screen.charPreview = charPrev;
            screen.dogPreview = dogPrev;
            screen.characters = chars.ToArray();
            screen.breeds = dogs.ToArray();

            Title(canvasGo.transform, font);

            // 이름: 캐릭터는 왼쪽 미리보기 아래, 강아지는 오른쪽 미리보기 아래
            screen.charName = Label(canvasGo.transform, font, 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(-500f, -820f), new Vector2(-40f, -760f));
            screen.dogName = Label(canvasGo.transform, font, 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(40f, -820f), new Vector2(500f, -760f));

            // 캐릭터 선택 줄
            screen.charHighlights = MakeRow(canvasGo.transform, font, chars, -1020f,
                i => screen.PickCharacter(i));
            // 강아지 선택 줄
            screen.dogHighlights = MakeRow(canvasGo.transform, font, dogs, -1360f,
                i => screen.PickBreed(i));

            // 시작 버튼
            StartButton(canvasGo.transform, font, screen);

            // 씬 참조 확인
            if (screen.characters == null || screen.breeds == null || screen.charPreview == null)
            { Debug.LogError("선택 화면 참조 연결 실패. 저장하지 않았습니다."); return; }

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScenes();
            Debug.Log($"선택 화면을 만들었습니다: {ScenePath} (캐릭터 {chars.Count} / 견종 {dogs.Count})");
        }

        static SpriteRenderer MakePreview(string name, Vector3 pos, float scale)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            return sr;
        }

        static List<SelectScreen.Option> LoadCharacters()
        {
            var names = LoadNameMap();
            var list = new List<SelectScreen.Option>();
            // JSON 순서를 따른다
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(HeroJson);
            string[] order = json != null ? ParseCharacters(json.text) : null;
            if (order == null) order = names.keys;
            foreach (var id in order)
            {
                var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>($"{CharDir}/{id}.asset");
                if (set == null) continue;
                list.Add(new SelectScreen.Option { id = id, displayName = names.Get(id, id), set = set });
            }
            return list;
        }

        static List<SelectScreen.Option> LoadDogs()
        {
            string[] breeds = { "shiba", "retriever", "maltese", "bordercollie", "samoyed", "bernese", "poodle" };
            string[] kr = { "시바", "리트리버", "말티즈", "보더콜리", "사모예드", "버니즈", "푸들" };
            var list = new List<SelectScreen.Option>();
            for (int i = 0; i < breeds.Length; i++)
            {
                var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>($"{DogDir}/{breeds[i]}.asset");
                if (set == null) continue;
                list.Add(new SelectScreen.Option { id = breeds[i], displayName = kr[i], set = set });
            }
            return list;
        }

        // ── 이름 맵 (hero_sprites.json 의 characterNames) ──
        class Names { public string[] keys; string[] vals;
            public Names(string[] k, string[] v){keys=k;vals=v;}
            public string Get(string id, string fb){for(int i=0;i<keys.Length;i++) if(keys[i]==id) return vals[i]; return fb;} }

        static Names LoadNameMap()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(HeroJson);
            var keys = new List<string>(); var vals = new List<string>();
            if (json != null)
            {
                // "characterNames": { "girl": "단발+후드티", ... }
                int b = json.text.IndexOf("\"characterNames\"");
                if (b >= 0)
                {
                    int open = json.text.IndexOf('{', b);
                    int close = json.text.IndexOf('}', open);
                    string body = json.text.Substring(open + 1, close - open - 1);
                    foreach (var pair in body.Split(','))
                    {
                        int c = pair.IndexOf(':');
                        if (c < 0) continue;
                        keys.Add(Unquote(pair.Substring(0, c)));
                        vals.Add(Unquote(pair.Substring(c + 1)));
                    }
                }
            }
            return new Names(keys.ToArray(), vals.ToArray());
        }

        static string[] ParseCharacters(string jsonText)
        {
            int b = jsonText.IndexOf("\"characters\"");
            if (b < 0) return null;
            int open = jsonText.IndexOf('[', b), close = jsonText.IndexOf(']', open);
            var list = new List<string>();
            foreach (var part in jsonText.Substring(open + 1, close - open - 1).Split(','))
            {
                var v = Unquote(part);
                if (!string.IsNullOrEmpty(v)) list.Add(v);
            }
            return list.ToArray();
        }

        static string Unquote(string s)
        {
            s = s.Trim();
            int a = s.IndexOf('"'), z = s.LastIndexOf('"');
            return a >= 0 && z > a ? s.Substring(a + 1, z - a - 1) : s;
        }

        // ── UI 만들기 ──
        static void Title(Transform parent, Font font)
        {
            var t = Label(parent, font, 68, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(-500f, -180f), new Vector2(500f, -60f));
            t.text = "같이 올라가자";
            t.color = new Color(0.3f, 0.32f, 0.4f);
        }

        static Image[] MakeRow(Transform parent, Font font, List<SelectScreen.Option> opts,
                               float y, System.Action<int> onPick)
        {
            const float cell = 130f, gap = 14f;
            float total = opts.Count * cell + (opts.Count - 1) * gap;
            float x0 = -total / 2f + cell / 2f;

            var highlights = new Image[opts.Count];
            for (int i = 0; i < opts.Count; i++)
            {
                float x = x0 + i * (cell + gap);
                var btnGo = new GameObject($"opt{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(parent, false);
                var rt = (RectTransform)btnGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(cell, cell);
                rt.anchoredPosition = new Vector2(x, y);
                btnGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);

                // 선택 하이라이트 테두리
                var hl = new GameObject("hl", typeof(RectTransform), typeof(Image));
                hl.transform.SetParent(btnGo.transform, false);
                var hrt = (RectTransform)hl.transform;
                hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
                hrt.offsetMin = new Vector2(-6f, -6f); hrt.offsetMax = new Vector2(6f, 6f);
                var himg = hl.GetComponent<Image>();
                himg.color = new Color(0.97f, 0.55f, 0.3f);
                himg.raycastTarget = false;
                hl.transform.SetAsFirstSibling();   // 뒤에 깔린다
                highlights[i] = himg;

                // 미리보기 아이콘 (idle 프레임)
                var icon = new GameObject("icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(btnGo.transform, false);
                var irt = (RectTransform)icon.transform;
                irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(12f, 12f); irt.offsetMax = new Vector2(-12f, -12f);
                var iimg = icon.GetComponent<Image>();
                iimg.sprite = opts[i].set.Frame(4);
                iimg.preserveAspect = true;
                iimg.raycastTarget = false;

                int idx = i;
                btnGo.GetComponent<Button>().onClick.AddListener(() => onPick(idx));
            }
            return highlights;
        }

        static void StartButton(Transform parent, Font font, SelectScreen screen)
        {
            var go = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(560f, 150f);
            rt.anchoredPosition = new Vector2(0f, 260f);
            go.GetComponent<Image>().color = new Color(0.97f, 0.45f, 0.56f);

            var t = Label(go.transform, font, 52, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(-280f, -75f), new Vector2(280f, 75f));
            t.text = "시작";

            go.GetComponent<Button>().onClick.AddListener(screen.StartGame);
        }

        static Text Label(Transform parent, Font font, int size, TextAnchor anchor,
                          Vector2 anchorPoint, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchorPoint;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// Select 를 첫 씬, Playground 를 두 번째로 등록한다.
        static void RegisterScenes()
        {
            var select = new EditorBuildSettingsScene(ScenePath, true);
            var play = new EditorBuildSettingsScene("Assets/_Game/Playground.unity", true);
            EditorBuildSettings.scenes = new[] { select, play };
        }
    }
}
