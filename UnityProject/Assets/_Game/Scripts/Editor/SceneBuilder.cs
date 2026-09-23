using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UpTogether.EditorTools
{
    /// 손맛 확인용 씬을 한 번에 조립한다. 아트가 없으므로 전부 임시 네모다.
    /// 씬 구조를 손으로 고쳐도 되고, 다시 실행하면 새 씬으로 덮어쓴다.
    public static class SceneBuilder
    {
        const string TuningPath = "Assets/_Game/Tuning.asset";
        const string ScenePath = "Assets/_Game/Playground.unity";
        /// 기본 견종. Art/Dog/Generated 에 구워진 것 중에서 고른다.
        const string DefaultBreed = "shiba";
        /// 기본 주인공. Art/Player/Generated 에 구워진 것 중에서 고른다.
        const string DefaultCharacter = "girl";

        [MenuItem("UpTogether/Build Play Scene")]
        public static void Build()
        {
            // ★ 순서 주의 ★ 씬을 먼저 만들고 나서 에셋을 로드한다.
            // NewScene()은 참조 없는 에셋을 언로드하는데, 지역 변수만 붙들고 있던
            // ScriptableObject는 그 때 파괴되어 "fake null"이 된다.
            // 그 상태로 대입하면 아무 경고 없이 null이 들어가고, 재생하자마자 터진다.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var tuning = EnsureTuning();
            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/Stages/Stage1.asset");
            if (stage == null)
            {
                Debug.LogError("Assets/_Game/Stages/Stage1.asset 이 없습니다. " +
                               "UpTogether ▸ Bake Stages 를 먼저 실행하세요.");
                return;
            }

            // ── 카메라 ──
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = tuning.CameraOrthoSize;   // Tuning.cameraViewHeightPx 에서 조절
            cam.backgroundColor = new Color(0.68f, 0.85f, 0.95f);   // 하늘이 못 덮는 틈의 보험
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10f);

            // ── 입력 ──
            var inputGo = new GameObject("GameInput");
            var input = inputGo.AddComponent<GameInput>();

            // ── 스테이지 ──
            var stageGo = new GameObject("Stage");
            var runner = stageGo.AddComponent<StageRunner>();
            var platformRoot = new GameObject("Platforms").transform;
            platformRoot.SetParent(stageGo.transform, false);
            runner.startStage = stage;
            runner.platformRoot = platformRoot;

            // ── 플레이어 / 강아지 ──
            // ── 배경 ──
            var skyGo = new GameObject("Sky");
            skyGo.transform.SetParent(camGo.transform, false);
            skyGo.AddComponent<SpriteRenderer>();
            var sky = skyGo.AddComponent<SkyBackground>();
            sky.stage = runner;

            var backdropGo = new GameObject("Backdrop");
            var backdrop = backdropGo.AddComponent<Backdrop>();
            backdrop.stage = runner;

            var puffsGo = new GameObject("Puffs");
            var puffs = puffsGo.AddComponent<Puffs>();

            // ── 플레이어 / 강아지 ──
            var playerGo = MakePlayer(tuning, out var playerVisual);
            var player = playerGo.AddComponent<PlayerController>();
            if (playerVisual != null) playerVisual.player = player;
            player.tuning = tuning;
            player.stage = runner;
            player.puffs = puffs;
            playerGo.transform.position = new Vector3(0.90f, stage.groundY, 0f);

            var dogGo = MakeDog(tuning, out var dogVisual);
            var dog = dogGo.AddComponent<DogController>();
            if (dogVisual != null) dogVisual.dog = dog;
            dog.tuning = tuning;
            dog.stage = runner;
            dog.player = player;
            dog.puffs = puffs;
            dog.visual = dogVisual;
            dogGo.transform.position = new Vector3(0.50f, stage.groundY, 0f);

            // 안기 3겹: 주인공 본체 → 강아지 → 앞팔. PlayerVisual 이 켜고 끈다.
            if (playerVisual != null && dogVisual != null)
            {
                playerVisual.dog = dog;
                playerVisual.dogVisual = dogVisual;
                playerVisual.dogRenderer = dogVisual.target;
                playerVisual.dogSpriteSet = dogVisual.spriteSet;
            }

            var follow = camGo.AddComponent<FollowCamera>();
            follow.tuning = tuning;
            follow.stage = runner;
            follow.player = player;

            BuildTouchUi(input);

            // 참조가 하나라도 비면 재생하자마자 터진다. 저장 전에 확인한다.
            var missing = new System.Collections.Generic.List<string>();
            if (runner.startStage == null)   missing.Add("StageRunner.startStage");
            if (runner.platformRoot == null) missing.Add("StageRunner.platformRoot");
            if (player.tuning == null)       missing.Add("Player.tuning");
            if (player.stage == null)        missing.Add("Player.stage");
            if (dog.tuning == null)          missing.Add("Dog.tuning");
            if (dog.stage == null)           missing.Add("Dog.stage");
            if (dog.player == null)          missing.Add("Dog.player");
            if (follow.tuning == null)       missing.Add("Camera.tuning");
            if (follow.stage == null)        missing.Add("Camera.stage");
            if (follow.player == null)       missing.Add("Camera.player");
            if (sky.stage == null)           missing.Add("Sky.stage");
            if (backdrop.stage == null)      missing.Add("Backdrop.stage");
            if (player.puffs == null)        missing.Add("Player.puffs");
            if (dogVisual != null && dogVisual.spriteSet == null) missing.Add("DogVisual.spriteSet");
            if (playerVisual != null && playerVisual.spriteSet == null) missing.Add("PlayerVisual.spriteSet");
            if (playerVisual != null && playerVisual.overlay == null)   missing.Add("PlayerVisual.overlay");
            if (playerVisual != null && playerVisual.dogSpriteSet == null) missing.Add("PlayerVisual.dogSpriteSet");
            if (dog.puffs == null)           missing.Add("Dog.puffs");
            if (missing.Count > 0)
            {
                Debug.LogError("씬 참조 연결 실패 — 저장하지 않았습니다: " + string.Join(", ", missing));
                return;
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"씬을 만들었습니다: {ScenePath}\n재생 버튼을 누르고 ←/→ + Space 로 확인하세요.\n" +
                      "수치는 Assets/_Game/Tuning.asset 인스펙터에서 재생 중에도 바꿀 수 있습니다.");
        }

        /// 주인공. 구워진 스프라이트가 있으면 쓰고, 없으면 임시 네모로 떨어진다.
        static GameObject MakePlayer(Tuning tuning, out PlayerVisual visual)
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(
                $"Assets/_Game/Art/Player/Generated/{DefaultCharacter}.asset");

            if (set == null)
            {
                Debug.LogWarning($"{DefaultCharacter} 스프라이트 세트가 없어 임시 네모를 씁니다. " +
                                 "UpTogether ▸ Import Player Art 를 먼저 실행하세요.");
                visual = null;
                return MakeBody("Player", new Color(0.99f, 0.78f, 0.45f), 0.26f, 0.56f, tuning);
            }

            var go = new GameObject("Player");
            var body = go.AddComponent<CharacterBody>();
            body.tuning = tuning;

            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);   // 피벗이 발이라 오프셋 없음
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = set.Frame(4);                       // idle 첫 장
            sr.sortingOrder = 10;

            // 앞팔 — 강아지(11)보다 앞
            var armGo = new GameObject("HoldArm");
            armGo.transform.SetParent(go.transform, false);
            var arm = armGo.AddComponent<SpriteRenderer>();
            arm.sortingOrder = 12;
            arm.enabled = false;

            visual = go.AddComponent<PlayerVisual>();
            visual.spriteSet = set;
            visual.target = sr;
            visual.overlay = arm;
            visual.body = body;
            return go;
        }

        /// 강아지. 구워진 스프라이트가 있으면 쓰고, 없으면 임시 네모로 떨어진다.
        /// 스프라이트 피벗이 발바닥에 있어서 Art 를 (0,0) 에 두면 발이 몸 위치에 맞는다.
        static GameObject MakeDog(Tuning tuning, out DogVisual visual)
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(
                $"Assets/_Game/Art/Dog/Generated/{DefaultBreed}.asset");

            if (set == null)
            {
                Debug.LogWarning($"{DefaultBreed} 스프라이트 세트가 없어 임시 네모를 씁니다. " +
                                 "UpTogether ▸ Import Dog Art 를 먼저 실행하세요.");
                visual = null;
                return MakeBody("Dog", new Color(0.85f, 0.62f, 0.40f), 0.30f, 0.30f, tuning);
            }

            var go = new GameObject("Dog");
            var body = go.AddComponent<CharacterBody>();
            body.tuning = tuning;

            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);   // 피벗이 발이라 오프셋 없음

            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = set.Frame(4);                       // idle 첫 장
            sr.sortingOrder = 10;

            visual = go.AddComponent<DogVisual>();
            visual.spriteSet = set;
            visual.target = sr;
            visual.body = body;
            return go;
        }

        static GameObject MakeBody(string name, Color color, float w, float h, Tuning tuning)
        {
            var go = new GameObject(name);
            var body = go.AddComponent<CharacterBody>();
            body.tuning = tuning;

            // 위치의 y는 '발'이다. 그림은 그 위로 올라간다.
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            art.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            art.transform.localScale = new Vector3(w, h, 1f);

            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = UnitSquare();
            sr.color = color;
            sr.sortingOrder = 10;
            return go;
        }

        static void BuildTouchUi(GameInput input)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("TouchUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            // 기준 해상도 1080x1920. 버튼 160px 이면 화면 폭의 15% 로,
            // 엄지로 누르기엔 충분하면서 플레이 화면을 덜 가린다.
            // (230px 일 때는 세 개가 폭의 64% 를 먹었다)
            // 실기기에서 반드시 다시 만질 것 (PROJECT.md 열린 과제)
            MakeButton(canvasGo.transform, "Left",  TouchButton.Kind.Left,  -1, new Vector2(0f, 0f), new Vector2( 140f, 165f));
            MakeButton(canvasGo.transform, "Right", TouchButton.Kind.Right,  1, new Vector2(0f, 0f), new Vector2( 330f, 165f));
            MakeButton(canvasGo.transform, "Jump",  TouchButton.Kind.Jump,   0, new Vector2(1f, 0f), new Vector2(-175f, 180f));
        }

        static void MakeButton(Transform parent, string label, TouchButton.Kind kind, int dir, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(TouchButton));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(160f, 160f);
            rt.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.sprite = ProceduralArt.Button(dir);
            img.color = Color.white;
            go.GetComponent<TouchButton>().kind = kind;
        }

        static Sprite cached;
        static Sprite UnitSquare()
        {
            if (cached == null)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                cached = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return cached;
        }

        static Tuning EnsureTuning()
        {
            var t = AssetDatabase.LoadAssetAtPath<Tuning>(TuningPath);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tuning>();
                AssetDatabase.CreateAsset(t, TuningPath);
                AssetDatabase.SaveAssets();
            }
            return t;
        }
    }
}
