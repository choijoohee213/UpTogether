using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UpTogether.EditorTools
{
    /// 프롤로그 전용 씬을 조립한다. UpTogether ▸ Build Prologue Scene.
    /// 흐름: Title → Select → **Prologue** → Playground.
    ///
    /// 플레이 맵과 완전히 따로 둔다. Playground 안에서 카메라만 돌렸을 때는
    /// 플레이하던 그 언덕에서 이야기가 진행돼 "다른 곳에서 만났다"가 안 느껴졌다.
    ///
    /// 그림이 아직 없으므로 플레이트 자리에 판 하나를 깔고, 무엇이 들어갈
    /// 자리인지 화면에 적는다. 모서리 눈금을 둬서 카메라가 움직이는 것이 보인다.
    public static class PrologueSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Prologue.unity";
        const string FontPath = "Assets/_Game/Fonts/Jua-Regular.ttf";

        /// 플레이트 한 장의 크기(유닛). 세로 시야 650px = 6.5유닛 기준으로
        /// 팬·줌 여유를 두고 1.5~2배 크게 잡는다 (STORY.md 7장).
        /// 플레이트 한 장의 크기(유닛). 그림이 750x1300, PPU 100 이라 7.5 x 13 이다.
        /// 게임이 세로 9:16 이라 화면에 보이는 것은 그 중 366x650px 뿐이고,
        /// 나머지가 카메라 팬·줌 여유다.
        const float PlateW = 7.5f;
        const float PlateH = 13f;

        [MenuItem("UpTogether/Build Prologue Scene")]
        public static void Build()
        {
            Time.fixedDeltaTime = 1f / Px.FPS;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var box = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var tuning = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Game/Tuning.asset");

            // ── 카메라 ── 비 내리는 거리라 어둡게 둔다
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = tuning != null ? tuning.CameraOrthoSize : 3.25f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            AudioSetup.Attach();

            // ── 플레이트 ── 그림이 들어가는 자리.
            // PPU 100 이라 1400x1000 그림이 정확히 PlateW x PlateH 유닛이 된다.
            PrologueArtImporter.Import();
            var byBreed = PrologueArtImporter.LoadAll();
            bool hasArt = byBreed.Count > 0;

            var plateGo = new GameObject("Plate");
            var plate = plateGo.AddComponent<SpriteRenderer>();
            plate.sortingOrder = 0;
            if (hasArt)
            {
                // 첫 컷은 런타임에 고른 견종으로 갈아끼운다. 여기서는 보기용 한 장만.
                plate.sprite = byBreed[0].plates[0];
                plate.drawMode = SpriteDrawMode.Simple;
                plate.color = Color.white;
            }
            else
            {
                plate.sprite = box;
                plate.drawMode = SpriteDrawMode.Sliced;
                plate.size = new Vector2(PlateW, PlateH);
                plate.color = new Color(0.21f, 0.25f, 0.33f);

                // 모서리 눈금 — 단색 판이라 카메라가 움직여도 티가 안 나서 넣는다.
                // 그림이 들어오면 안 만든다.
                MakeTick(box, "TickTL", new Vector2(-PlateW * 0.42f,  PlateH * 0.36f));
                MakeTick(box, "TickTR", new Vector2( PlateW * 0.42f,  PlateH * 0.36f));
                MakeTick(box, "TickBL", new Vector2(-PlateW * 0.42f, -PlateH * 0.36f));
                MakeTick(box, "TickBR", new Vector2( PlateW * 0.42f, -PlateH * 0.36f));
                MakeTick(box, "TickC",  Vector2.zero);
            }

            var rain = BuildRain(cam);

            // ── 컷신 오버레이 ── 레터박스·글. Playground 와 같은 구성.
            var canvasGo = new GameObject("Cinematic", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var cine = canvasGo.AddComponent<Cinematic>();
            cine.cam = cam;
            cine.tuning = tuning;
            cine.group = group;
            // 그림 밖이 보이지 않게 가둔다 (750x1300 / PPU 100 의 절반)
            cine.plateHalf = new Vector2(PlateW * 0.5f, PlateH * 0.5f);
            // follow·input 은 이 씬에 없다. Cinematic 이 null 을 허용한다.

            cine.barTop = Bar(canvasGo.transform, "BarTop", true);
            cine.barBottom = Bar(canvasGo.transform, "BarBottom", false);

            var line = Text(canvasGo.transform, font, "Line", 42, TextAnchor.LowerCenter,
                            new Vector2(70f, 215f), new Vector2(-70f, 455f));
            line.color = new Color(0.97f, 0.97f, 0.95f);
            cine.label = line;

            // 자리표시 설명 — 그림이 들어오면 비워진다
            var slate = Text(canvasGo.transform, font, "Slate", 30, TextAnchor.UpperCenter,
                             new Vector2(70f, -560f), new Vector2(-70f, -300f));
            slate.color = new Color(1f, 1f, 1f, 0.5f);

            // ── 진행 ──
            var sysGo = new GameObject("Systems");
            var narration = sysGo.AddComponent<Narration>();
            var pro = sysGo.AddComponent<PrologueScene>();
            pro.cinematic = cine;
            pro.narration = narration;
            pro.plate = plate;
            pro.breedPlates = hasArt ? byBreed.ToArray() : null;
            pro.slate = slate;
            pro.rain = rain;
            pro.nextScene = "Playground";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            TitleSceneBuilder.RegisterScenes();
            Debug.Log($"프롤로그 씬을 만들었습니다: {ScenePath}");
        }

        /// 비. 그림에는 비가 없다 — 그리면 컷마다 다시 그려야 하고,
        /// 멈춰 있는 비는 비처럼 안 보인다. 파티클로 뿌린다.
        /// 띠를 카메라 위에 두고 Rain 이 매 프레임 시야에 맞춰 늘린다.
        static Rain BuildRain(Camera cam)
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(cam.transform, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 2.4f;
            main.startSpeed = 11f;
            main.startSize = 0.16f;                 // 늘여 그리므로 굵기만 정한다
            main.startColor = new Color(0.82f, 0.86f, 0.95f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 900;
            main.gravityModifier = 0.6f;
            main.startRotation = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 0.1f, 1f);
            shape.rotation = new Vector3(90f, 0f, 0f);   // 아래로 뿜는다

            // 비스듬히 내려야 비처럼 보인다.
            // 세 축을 모두 같은 모드(2-상수)로 둬야 한다 — x 만 바꾸면
            // "Particle Velocity curves must all be in the same mode" 로 터진다.
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-2.6f, -1.6f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var emission = ps.emission;
            emission.rateOverTime = 180f;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.14f;                // 속도만큼 길게 늘인다 = 빗줄기
            r.lengthScale = 1f;
            r.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            r.sortingOrder = 50;                    // 플레이트(0) 위, 레터박스(UI) 아래

            var rain = go.AddComponent<Rain>();
            rain.cam = cam;
            return rain;
        }

        /// 카메라 이동이 보이도록 두는 작은 표식 (자리표시용)
        static void MakeTick(Sprite box, string name, Vector2 at)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = box;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.5f, 0.5f);
            sr.color = new Color(1f, 1f, 1f, 0.16f);
            sr.sortingOrder = 1;
            go.transform.position = new Vector3(at.x, at.y, 0f);
        }

        /// 레터박스 띠. 위는 아래로, 아래는 위로 자란다 (Cinematic 이 높이를 키운다).
        ///
        /// 스프라이트를 주지 않는다. 빌트인 UISprite 는 모서리가 둥근 그림이라
        /// Simple 로 늘이면 화면 폭만 한 둥근 검은 덩어리가 떠 있는 꼴이 된다.
        /// 스프라이트가 없는 Image 는 그냥 꽉 찬 사각형으로 그려진다.
        static RectTransform Bar(Transform parent, string name, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            float y = top ? 1f : 0f;
            rt.anchorMin = new Vector2(0f, y);
            rt.anchorMax = new Vector2(1f, y);
            rt.pivot = new Vector2(0.5f, y);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return rt;
        }

        static Text Text(Transform parent, Font font, string name, int size,
                         TextAnchor anchor, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            // 아래쪽 글은 아래에, 위쪽 설명은 위에 붙인다 (offset 의 부호로 가린다)
            bool fromTop = offsetMin.y < 0f;
            rt.anchorMin = new Vector2(0f, fromTop ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, fromTop ? 1f : 0f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
