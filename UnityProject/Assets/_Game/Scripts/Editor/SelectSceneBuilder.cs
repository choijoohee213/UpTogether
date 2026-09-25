using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UpTogether.EditorTools
{
    /// 캐릭터·강아지 선택 화면을 조립한다. UpTogether ▸ Build Select Scene.
    /// 흐름: Title → Select → Playground. 게임 씬과 같은 방식으로 코드로 만든다.
    public static class SelectSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Select.unity";
        const string FontPath = "Assets/_Game/Fonts/Jua-Regular.ttf";
        const string CharDir = "Assets/_Game/Art/Player/Generated";
        const string DogDir = "Assets/_Game/Art/Dog/Generated";
        const string HeroJson = "Assets/_Game/Art/Player/hero_sprites_v3/hero_sprites.json";

        static Sprite box;   // 둥근 사각 (빌트인 UISprite)

        [MenuItem("UpTogether/Build Select Scene")]
        public static void Build()
        {
            Time.fixedDeltaTime = 1f / Px.FPS;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            box = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var chars = LoadCharacters();
            var dogs = LoadDogs();
            if (chars.Count == 0 || dogs.Count == 0)
            {
                Debug.LogError("구워진 캐릭터/견종 에셋이 없습니다. Import Player/Dog Art 를 먼저 실행하세요.");
                return;
            }

            // ── 카메라 (하늘, 타이틀과 같은 톤) ──
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

            AudioSetup.Attach();

            // ── 캔버스 ──
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            var root = canvasGo.transform;

            var screen = canvasGo.AddComponent<SelectScreen>();
            screen.characters = chars.ToArray();
            screen.breeds = dogs.ToArray();

            // 제목
            var head = Label(root, font, 66, new Vector2(0.5f, 1f),
                new Vector2(-520f, -190f), new Vector2(520f, -60f));
            head.text = "누구랑 함께 오를까?";
            head.color = new Color(0.24f, 0.27f, 0.34f);

            // ── 미리보기 카드 ──
            var card = Panel(root, new Color(1f, 1f, 1f, 0.55f),
                new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(880f, 560f), false);
            screen.charPreview = PreviewImage(card.transform, new Vector2(-200f, 60f), new Vector2(320f, 380f));
            screen.dogPreview = PreviewImage(card.transform, new Vector2(200f, 40f), new Vector2(320f, 320f));
            screen.charName = CardLabel(card.transform, font, 40, new Vector2(-200f, -210f), new Vector2(360f, 80f));
            screen.dogName = CardLabel(card.transform, font, 40, new Vector2(200f, -210f), new Vector2(360f, 80f));

            // ── 선택 줄 ──
            SectionLabel(root, font, "친구", -960f);
            screen.charHighlights = MakeRow(root, chars, -1120f, screen, SelectTap.Kind.Character);
            SectionLabel(root, font, "강아지", -1300f);
            screen.dogHighlights = MakeRow(root, dogs, -1460f, screen, SelectTap.Kind.Breed);

            // ── 버튼 ──
            StartButton(root, font, screen);
            BackButton(root, font);

            if (screen.characters == null || screen.breeds == null || screen.charPreview == null)
            { Debug.LogError("선택 화면 참조 연결 실패. 저장하지 않았습니다."); return; }

            EditorSceneManager.SaveScene(scene, ScenePath);
            TitleSceneBuilder.RegisterScenes();
            Debug.Log($"선택 화면을 만들었습니다: {ScenePath} (캐릭터 {chars.Count} / 견종 {dogs.Count})");
        }

        // ── 로더 ──
        static List<SelectScreen.Option> LoadCharacters()
        {
            var names = LoadNameMap();
            var list = new List<SelectScreen.Option>();
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

        class Names { public string[] keys; string[] vals;
            public Names(string[] k, string[] v){keys=k;vals=v;}
            public string Get(string id, string fb){for(int i=0;i<keys.Length;i++) if(keys[i]==id) return vals[i]; return fb;} }

        static Names LoadNameMap()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(HeroJson);
            var keys = new List<string>(); var vals = new List<string>();
            if (json != null)
            {
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

        // ── UI 조각 ──
        static Image[] MakeRow(Transform parent, List<SelectScreen.Option> opts,
                               float y, SelectScreen screen, SelectTap.Kind kind)
        {
            const float cell = 116f, gap = 12f;
            float total = opts.Count * cell + (opts.Count - 1) * gap;
            float x0 = -total / 2f + cell / 2f;

            var highlights = new Image[opts.Count];
            for (int i = 0; i < opts.Count; i++)
            {
                float x = x0 + i * (cell + gap);
                var btnGo = new GameObject($"opt{i}", typeof(RectTransform), typeof(Image), typeof(SelectTap));
                btnGo.transform.SetParent(parent, false);
                var rt = (RectTransform)btnGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(cell, cell);
                rt.anchoredPosition = new Vector2(x, y);
                var bg = btnGo.GetComponent<Image>();
                bg.sprite = box; bg.type = Image.Type.Sliced;
                bg.color = new Color(1f, 1f, 1f, 0.75f);

                // 선택 테두리 (뒤에 깔린 둥근 사각)
                var hl = new GameObject("hl", typeof(RectTransform), typeof(Image));
                hl.transform.SetParent(btnGo.transform, false);
                var hrt = (RectTransform)hl.transform;
                hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
                hrt.offsetMin = new Vector2(-8f, -8f); hrt.offsetMax = new Vector2(8f, 8f);
                var himg = hl.GetComponent<Image>();
                himg.sprite = box; himg.type = Image.Type.Sliced;
                himg.color = new Color(0.97f, 0.55f, 0.3f);
                himg.raycastTarget = false;
                hl.transform.SetAsFirstSibling();
                highlights[i] = himg;

                // 아이콘 (idle 프레임)
                var icon = new GameObject("icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(btnGo.transform, false);
                var irt = (RectTransform)icon.transform;
                irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(14f, 14f); irt.offsetMax = new Vector2(-14f, -14f);
                var iimg = icon.GetComponent<Image>();
                iimg.sprite = opts[i].set.Frame(4);
                iimg.preserveAspect = true;
                iimg.raycastTarget = false;

                var tap = btnGo.GetComponent<SelectTap>();
                tap.screen = screen;
                tap.kind = kind;
                tap.index = i;
            }
            return highlights;
        }

        static void StartButton(Transform parent, Font font, SelectScreen screen)
        {
            var go = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(SelectTap));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(620f, 168f);
            rt.anchoredPosition = new Vector2(0f, 250f);
            var img = go.GetComponent<Image>();
            img.sprite = box; img.type = Image.Type.Sliced;
            img.color = new Color(0.97f, 0.45f, 0.56f);

            var t = Label(go.transform, font, 56, new Vector2(0.5f, 0.5f),
                new Vector2(-300f, -84f), new Vector2(300f, 84f));
            t.text = "이 친구로 시작";
            t.raycastTarget = false;

            var tap = go.GetComponent<SelectTap>();
            tap.screen = screen;
            tap.kind = SelectTap.Kind.Start;
        }

        static void BackButton(Transform parent, Font font)
        {
            var go = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(SceneLink));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(170f, 92f);
            rt.anchoredPosition = new Vector2(120f, -110f);
            var img = go.GetComponent<Image>();
            img.sprite = box; img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.55f);

            var t = Label(go.transform, font, 40, new Vector2(0.5f, 0.5f),
                new Vector2(-85f, -46f), new Vector2(85f, 46f));
            t.text = "‹ 뒤로";
            t.color = new Color(0.3f, 0.34f, 0.4f);
            t.raycastTarget = false;

            go.GetComponent<SceneLink>().scene = "Title";
        }

        static void SectionLabel(Transform parent, Font font, string text, float y)
        {
            var t = Label(parent, font, 40, new Vector2(0.5f, 1f),
                new Vector2(-500f, y - 30f), new Vector2(500f, y + 30f));
            t.text = text;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = new Color(0.28f, 0.32f, 0.4f, 0.9f);
        }

        static Image Panel(Transform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool ray)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.sprite = box; img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = ray;
            return img;
        }

        static Image PreviewImage(Transform card, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(card, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static Text CardLabel(Transform card, Font font, int size, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(card, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dim;
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.28f, 0.31f, 0.38f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static Text Label(Transform parent, Font font, int size, Vector2 anchorPoint,
                          Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchorPoint;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
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
    }
}
