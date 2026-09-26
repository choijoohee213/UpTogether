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
        const string FontPath = "Assets/_Game/Fonts/Jua-Regular.ttf";
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
            cam.backgroundColor = new Color(0.698f, 0.871f, 0.937f);   // 하늘 스프라이트 위쪽과 맞춤(이음매 없이)
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
            runner.tiles = LoadTiles("Assets/_Game/Art/Map/forest_tileset_16px.png");

            // ── 플레이어 / 강아지 ──
            // ── 배경 (숲 테마 3겹) ──
            var backdropGo = new GameObject("Backdrop");
            var forest = backdropGo.AddComponent<ForestBackdrop>();
            forest.stage = runner;
            forest.sky = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Map/bg0_sky.png");
            forest.far = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Map/bg1_far.png");
            forest.mid = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Map/bg2_mid.png");

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

            // ── 깃발 ──
            var flag = MakeFlag(runner);

            // ── 게임 흐름 ──
            var sysGo = new GameObject("Systems");
            var bond = sysGo.AddComponent<Bond>();
            var narration = sysGo.AddComponent<Narration>();
            var session = sysGo.AddComponent<StageSession>();
            session.player = player;
            session.dog = dog;
            session.stage = runner;
            session.bond = bond;
            session.narration = narration;

            // 장애물 — 가시·바람·톱니·가시 링 (발판 계열은 StageRunner가 처리)
            var body = playerVisual != null ? playerVisual.body : playerGo.GetComponent<CharacterBody>();
            var hazards = sysGo.AddComponent<Hazards>();
            hazards.player = body;
            hazards.stage = runner;
            hazards.bond = bond;

            // 수집 — 간식·통과 링 (친밀도 상승)
            var collectibles = sysGo.AddComponent<Collectibles>();
            collectibles.player = body;
            collectibles.stage = runner;
            collectibles.bond = bond;

            // 오디오 — 재생기(BGM+효과음) + 게임 이벤트를 소리로 옮기는 다리
            AudioSetup.Attach();
            var gameAudio = sysGo.AddComponent<GameAudio>();
            gameAudio.player = player;
            gameAudio.bond = bond;
            gameAudio.session = session;
            gameAudio.dog = dog;
            gameAudio.hazards = hazards;
            gameAudio.collectibles = collectibles;

            // 런타임에 선택된 캐릭터·강아지를 적용한다 (씬은 빌드 시점 스프라이트로 굳어 있으므로)
            if (playerVisual != null)
            {
                var applier = sysGo.AddComponent<SelectionApplier>();
                applier.playerVisual = playerVisual;
                applier.dogVisual = dogVisual;
                applier.characters = LoadEntries("Assets/_Game/Art/Player/Generated");
                applier.breeds = LoadEntries("Assets/_Game/Art/Dog/Generated");
            }

            var follow = camGo.AddComponent<FollowCamera>();
            follow.tuning = tuning;
            follow.stage = runner;
            follow.player = player;

            BuildTouchUi(input);
            BuildHud(session, bond, narration);

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
            if (forest.stage == null)        missing.Add("Backdrop.stage");
            if (forest.sky == null)          missing.Add("Backdrop.sky (Import Map Art 실행?)");
            if (player.puffs == null)        missing.Add("Player.puffs");
            if (dogVisual != null && dogVisual.spriteSet == null) missing.Add("DogVisual.spriteSet");
            if (playerVisual != null && playerVisual.spriteSet == null) missing.Add("PlayerVisual.spriteSet");
            if (playerVisual != null && playerVisual.overlay == null)   missing.Add("PlayerVisual.overlay");
            if (playerVisual != null && playerVisual.dogSpriteSet == null) missing.Add("PlayerVisual.dogSpriteSet");
            if (dog.puffs == null)           missing.Add("Dog.puffs");
            if (session.player == null)      missing.Add("Session.player");
            if (session.bond == null)        missing.Add("Session.bond");
            if (flag.stage == null)          missing.Add("GoalFlag.stage");
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
            string id = Selection.Character;
            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(
                $"Assets/_Game/Art/Player/Generated/{id}.asset")
                ?? AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(
                $"Assets/_Game/Art/Player/Generated/{DefaultCharacter}.asset");

            if (set == null)
            {
                Debug.LogWarning($"주인공 스프라이트 세트가 없어 임시 네모를 씁니다. " +
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
            string breed = Selection.Breed;
            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(
                $"Assets/_Game/Art/Dog/Generated/{breed}.asset")
                ?? AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(
                $"Assets/_Game/Art/Dog/Generated/{DefaultBreed}.asset");

            if (set == null)
            {
                Debug.LogWarning($"강아지 스프라이트 세트가 없어 임시 네모를 씁니다. " +
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

        /// 꼭대기 깃발. 장대와 천을 네모로 짜 맞춘다 — 원본도 도형으로 그렸다.
        static GoalFlag MakeFlag(StageRunner runner)
        {
            var go = new GameObject("GoalFlag");
            var flag = go.AddComponent<GoalFlag>();
            flag.stage = runner;

            var pole = new GameObject("Pole");
            pole.transform.SetParent(go.transform, false);
            pole.transform.localPosition = new Vector3(0f, Px.U(2f), 0f);
            pole.transform.localScale = new Vector3(Px.U(5f), Px.U(56f), 1f);
            var ps = pole.AddComponent<SpriteRenderer>();
            ps.sprite = ProceduralArt.Square;
            ps.color = new Color(0.54f, 0.35f, 0.22f);
            ps.sortingOrder = 4;

            var cloth = new GameObject("Cloth");
            cloth.transform.SetParent(go.transform, false);
            cloth.transform.localPosition = new Vector3(Px.U(11f), Px.U(22f), 0f);
            cloth.transform.localScale = new Vector3(Px.U(22f), Px.U(14f), 1f);
            var cs = cloth.AddComponent<SpriteRenderer>();
            cs.sprite = ProceduralArt.Square;
            cs.color = new Color(0.95f, 0.38f, 0.49f);
            cs.sortingOrder = 5;

            flag.cloth = cloth.transform;
            return flag;
        }

        /// 폴더의 모든 CharacterSpriteSet 을 id(파일명)와 함께 모은다. 빌드에 포함된다.
        static SelectionApplier.Entry[] LoadEntries(string dir)
        {
            var list = new System.Collections.Generic.List<SelectionApplier.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:CharacterSpriteSet", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(path);
                if (set == null) continue;
                string id = System.IO.Path.GetFileNameWithoutExtension(path);
                list.Add(new SelectionApplier.Entry { id = id, set = set });
            }
            return list.ToArray();
        }

        /// 슬라이스된 숲 타일 32칸을 forest_0..31 순서로 모은다.
        static Sprite[] LoadTiles(string path)
        {
            var byName = new System.Collections.Generic.Dictionary<string, Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) byName[s.name] = s;
            var arr = new Sprite[32];
            for (int i = 0; i < 32; i++) byName.TryGetValue($"forest_{i}", out arr[i]);
            return arr;
        }

        static Font LoadFont()
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (f == null) Debug.LogWarning($"{FontPath} 를 못 찾았습니다. 한글이 깨집니다.");
            return f;
        }

        static Text MakeText(Transform parent, string name, Font font, int size,
                             TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax,
                             Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;

            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static Image MakeImage(Transform parent, string name, Color color,
                               Vector2 anchorMin, Vector2 anchorMax,
                               Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            // 스프라이트가 없으면 Filled 타입의 fillAmount 가 무시된다 (바가 늘 꽉 차 보인다).
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            return img;
        }

        /// 친밀도 바, 높이, 서사 한 줄, 클리어 화면.
        static void BuildHud(StageSession session, Bond bond, Narration narration)
        {
            var font = LoadFont();

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;          // 조작 버튼보다 위
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var hud = canvasGo.AddComponent<GameHud>();
            hud.session = session; hud.bond = bond; hud.narration = narration;

            // 친밀도 바 (좌상단)
            MakeImage(canvasGo.transform, "BondBack", new Color(0f, 0f, 0f, 0.25f),
                      new Vector2(0f, 1f), new Vector2(0f, 1f),
                      new Vector2(40f, -96f), new Vector2(460f, -56f));
            var fill = MakeImage(canvasGo.transform, "BondFill", new Color(0.97f, 0.45f, 0.56f),
                      new Vector2(0f, 1f), new Vector2(0f, 1f),
                      new Vector2(40f, -96f), new Vector2(460f, -56f));
            fill.type = Image.Type.Filled;   // Sliced 기본값을 덮어쓴다
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            hud.bondFill = fill;

            hud.bondLabel = MakeText(canvasGo.transform, "BondLabel", font, 30, TextAnchor.MiddleLeft,
                      new Vector2(0f, 1f), new Vector2(0f, 1f),
                      new Vector2(40f, -146f), new Vector2(560f, -100f));

            // 높이 (우상단 — 일시정지 버튼과 겹치지 않게 왼쪽으로 비켜둔다)
            hud.heightLabel = MakeText(canvasGo.transform, "Height", font, 44, TextAnchor.MiddleRight,
                      new Vector2(1f, 1f), new Vector2(1f, 1f),
                      new Vector2(-460f, -110f), new Vector2(-170f, -50f));

            // 서사 한 줄 (가운데 아래쪽)
            var sayGo = new GameObject("Say", typeof(RectTransform), typeof(CanvasGroup));
            sayGo.transform.SetParent(canvasGo.transform, false);
            var sayRt = (RectTransform)sayGo.transform;
            sayRt.anchorMin = new Vector2(0.5f, 0f); sayRt.anchorMax = new Vector2(0.5f, 0f);
            sayRt.sizeDelta = new Vector2(900f, 150f);
            sayRt.anchoredPosition = new Vector2(0f, 620f);
            hud.sayGroup = sayGo.GetComponent<CanvasGroup>();
            hud.sayGroup.blocksRaycasts = false;

            MakeImage(sayGo.transform, "SayBack", new Color(0f, 0f, 0f, 0.45f),
                      Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hud.sayText = MakeText(sayGo.transform, "SayText", font, 34, TextAnchor.MiddleCenter,
                      Vector2.zero, Vector2.one, new Vector2(24f, 12f), new Vector2(-24f, -12f));

            // 클리어 화면
            var clearGo = new GameObject("Clear", typeof(RectTransform), typeof(CanvasGroup));
            clearGo.transform.SetParent(canvasGo.transform, false);
            var clearRt = (RectTransform)clearGo.transform;
            clearRt.anchorMin = Vector2.zero; clearRt.anchorMax = Vector2.one;
            clearRt.offsetMin = Vector2.zero; clearRt.offsetMax = Vector2.zero;
            hud.clearGroup = clearGo.GetComponent<CanvasGroup>();

            MakeImage(clearGo.transform, "Dim", new Color(0f, 0f, 0f, 0.55f),
                      Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hud.clearText = MakeText(clearGo.transform, "ClearText", font, 46, TextAnchor.MiddleCenter,
                      new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      new Vector2(-460f, -260f), new Vector2(460f, 260f));

            // 일시정지 버튼 + 오버레이 (맨 위 형제라 다른 HUD 위에 그려진다)
            BuildPause(canvasGo, font);
        }

        /// 우상단 일시정지 버튼과, 누르면 뜨는 오버레이(계속하기 / 메인으로).
        static void BuildPause(GameObject canvasGo, Font font)
        {
            var box = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var pause = canvasGo.AddComponent<Pause>();

            // 버튼 (우상단 모서리)
            var btn = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(PauseTap));
            btn.transform.SetParent(canvasGo.transform, false);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(1f, 1f);
            brt.sizeDelta = new Vector2(96f, 96f);
            brt.anchoredPosition = new Vector2(-36f, -36f);
            var bimg = btn.GetComponent<Image>();
            bimg.sprite = box; bimg.type = Image.Type.Sliced;
            bimg.color = new Color(1f, 1f, 1f, 0.5f);
            var ptap = btn.GetComponent<PauseTap>();
            ptap.pause = pause; ptap.kind = PauseTap.Kind.Open;
            PauseBar(btn.transform, -13f);   // ‖ 아이콘 (글리프 없이 막대 두 개)
            PauseBar(btn.transform, 13f);

            // 오버레이 패널 (처음엔 꺼둔다)
            var panel = new GameObject("PausePanel", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            pause.panel = panel;

            // 딤 — 뒤의 조작 버튼 탭을 막는다
            MakeImage(panel.transform, "Dim", new Color(0f, 0f, 0f, 0.55f),
                      Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            MakeImage(panel.transform, "Card", new Color(1f, 1f, 1f, 0.96f),
                      new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      new Vector2(-360f, -320f), new Vector2(360f, 320f));

            var title = MakeText(panel.transform, "PauseTitle", font, 54, TextAnchor.MiddleCenter,
                      new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      new Vector2(-320f, 150f), new Vector2(320f, 300f));
            title.text = "잠깐 쉬어가기";
            title.color = new Color(0.24f, 0.27f, 0.34f);

            PauseMenuButton(panel.transform, font, box, "계속하기",
                            new Color(0.97f, 0.45f, 0.56f), Color.white, 30f, pause, PauseTap.Kind.Resume);
            PauseMenuButton(panel.transform, font, box, "메인으로",
                            new Color(0.90f, 0.92f, 0.95f, 1f), new Color(0.3f, 0.34f, 0.4f),
                            -130f, pause, PauseTap.Kind.Home);

            panel.SetActive(false);
        }

        static void PauseBar(Transform parent, float x)
        {
            var go = new GameObject("bar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(14f, 44f);
            rt.anchoredPosition = new Vector2(x, 0f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.24f, 0.27f, 0.34f);
            img.raycastTarget = false;
        }

        static void PauseMenuButton(Transform parent, Font font, Sprite box, string label,
                                    Color bg, Color fg, float y, Pause pause, PauseTap.Kind kind)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(PauseTap));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 128f);
            rt.anchoredPosition = new Vector2(0f, y);
            var img = go.GetComponent<Image>();
            img.sprite = box; img.type = Image.Type.Sliced; img.color = bg;

            var t = MakeText(go.transform, "T", font, 44, TextAnchor.MiddleCenter,
                             Vector2.zero, Vector2.one, new Vector2(20f, 8f), new Vector2(-20f, -8f));
            t.text = label; t.color = fg; t.raycastTarget = false;

            var tap = go.GetComponent<PauseTap>();
            tap.pause = pause; tap.kind = kind;
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
