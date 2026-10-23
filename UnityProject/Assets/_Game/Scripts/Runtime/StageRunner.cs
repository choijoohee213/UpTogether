using UnityEngine;

namespace UpTogether
{
    /// 스테이지 데이터를 런타임 발판 배열로 펼치고, 움직이는/사라지는/튕기는 발판을 갱신한다.
    /// 발판은 콜라이더가 아니라 배열이다 — 충돌은 CharacterBody가 직접 푼다.
    /// 가시·바람은 발판이 아니라 Hazards가 따로 처리한다(여기서는 그림만 그린다).
    [DefaultExecutionOrder(-100)]
    public class StageRunner : MonoBehaviour
    {
        public enum Kind : byte { Normal, Bounce, Vanish }

        public struct RuntimePlatform
        {
            public float left, right, y;
            public float deltaX;   // 이번 스텝에 옆으로 움직인 양. 위에 탄 몸이 같이 실려 간다.
            public float deltaY;   // 위아래로 움직인 양.
            public Kind kind;
            public bool active;    // 사라지는 발판이 사라진 동안 false
            public bool icy;       // 얼음 — 마찰이 거의 없다
        }

        public StageData Data { get; private set; }
        public int PlatformCount => platforms.Length;
        public RuntimePlatform GetPlatform(int i) => platforms[i];
        /// 이 아래로 떨어지면 바닥으로 되돌린다 (원본: y > H + 300px)
        public float FallResetY => -Px.U(300f);

        public StageData startStage;
        public Transform platformRoot;
        /// 숲 타일셋 32칸(위-왼쪽부터 0번). SceneBuilder 가 채운다. 비면 절차적 발판으로 떨어진다.
        public Sprite[] tiles;
        /// 장애물 스프라이트. 비면 절차적 도형으로 떨어진다.
        public SpriteLib lib;
        /// 떨어지는 돌이 '밑을 지나갔나' 를 보려면 플레이어 위치가 필요하다.
        public CharacterBody player;

        float TileU => (tiles != null && tiles.Length > 0 && tiles[0] != null)
            ? tiles[0].rect.width / tiles[0].pixelsPerUnit : Px.U(16f);

        RuntimePlatform[] platforms;
        int moverStart, bouncerStart, vanishStart;   // 배열 구간 시작점
        Transform[] moverVisuals;
        Transform[] vanishBody;        // 사라지는 발판 GameObject (켜고 끄고 흔든다)
        SpriteRenderer[] vanishFrost;  // 그 위의 청록 표시줄 (밟기 전 경고)

        // 사라지는 발판 상태
        enum VanishState : byte { Solid, Shaking, Gone }
        VanishState[] vanishState;
        float[] vanishTimer;
        const float ShakeTime = 0.45f;
        const float GoneTime = 1.6f;

        // 튕김판 눌림 연출 (0=기본, 1=눌림)
        Transform[] bounceVisuals;
        SpriteRenderer[] bounceRenderers;
        float[] bounceSquashUntil;
        const float SquashTime = 0.16f;

        // 흔들리는 추
        Transform[] pendChain, pendWeight;
        Vector2[] pendPos;
        public int PendulumCount => pendPos?.Length ?? 0;
        public Vector2 PendulumPos(int i) => pendPos[i];
        public const float PendulumRadius = 0.26f;

        // 뿜는 증기
        SpriteRenderer[] steamVis;
        bool[] steamOn;
        public int SteamCount => steamOn?.Length ?? 0;
        public bool SteamOn(int i) => steamOn[i];

        // 떨어지는 돌
        enum RockState : byte { Hang, Shake, Fall, Gone }
        RockState[] rockState;
        float[] rockTimer;        // 흔들림/사라짐 남은 시간
        float[] rockY, rockVy;    // 현재 높이와 낙하 속도
        Transform[] rockVisuals;
        SpriteRenderer[] rockRenderers;
        const float RockShake = 0.45f;   // 떨어지기 전 경고
        const float RockBack = 2.5f;     // 되살아나기까지
        const float RockTriggerX = 1.0f; // 이 안으로 들어오면 흔들린다
        const float RockTriggerY = 3.2f; // 이만큼 아래까지 본다
        public int RockCount => rockState?.Length ?? 0;
        public bool RockFalling(int i) => rockState[i] == RockState.Fall;
        public Vector2 RockPos(int i) => new Vector2(OrEmpty(Data.rocks)[i].x, rockY[i]);

        // 구르는 통나무
        Vector2[] rollerPos;
        Transform[] rollerVisuals;
        public int RollerCount => rollerPos?.Length ?? 0;
        public Vector2 RollerPos(int i) => rollerPos[i];
        public float RollerRadius(int i) => Data.rollers[i].radius;

        // 돌아가는 톱니
        Vector2[] sawPos;
        Transform[] sawVisuals;
        public int SawCount => sawPos?.Length ?? 0;
        public Vector2 SawPos(int i) => sawPos[i];
        public float SawBlade(int i) => Data.saws[i].blade;

        float clock;

        void Awake()
        {
            if (startStage == null || platformRoot == null)
            {
                Debug.LogError($"{name}: startStage/platformRoot가 비어 있다. " +
                               "UpTogether ▸ Build Play Scene 을 다시 실행하거나 인스펙터에서 직접 지정할 것.", this);
                enabled = false;
                return;
            }
            Load(startStage);
        }

        static T[] OrEmpty<T>(T[] a) => a ?? System.Array.Empty<T>();

        public void Load(StageData data)
        {
            Data = data;
            clock = 0f;

            var movers = OrEmpty(data.movers);
            var bouncers = OrEmpty(data.bouncers);
            var vanishers = OrEmpty(data.vanishers);

            int total = data.platforms.Length + movers.Length + bouncers.Length + vanishers.Length;
            platforms = new RuntimePlatform[total];

            int at = 0;
            foreach (var p in data.platforms)
                platforms[at++] = new RuntimePlatform { left = p.x, right = p.x + p.width, y = p.y,
                                                        kind = Kind.Normal, active = true, icy = p.icy };

            moverStart = at;
            foreach (var m in movers)
                platforms[at++] = new RuntimePlatform { left = m.x, right = m.x + m.width, y = m.y, kind = Kind.Normal, active = true };

            bouncerStart = at;
            int bounceCount = OrEmpty(Data.bouncers).Length;
            bounceVisuals = new Transform[bounceCount];
            bounceRenderers = new SpriteRenderer[bounceCount];
            bounceSquashUntil = new float[bounceCount];
            foreach (var b in bouncers)
                platforms[at++] = new RuntimePlatform { left = b.x, right = b.x + b.width, y = b.y, kind = Kind.Bounce, active = true };

            vanishStart = at;
            foreach (var v in vanishers)
                platforms[at++] = new RuntimePlatform { left = v.x, right = v.x + v.width, y = v.y, kind = Kind.Vanish, active = true };

            vanishState = new VanishState[vanishers.Length];
            vanishTimer = new float[vanishers.Length];

            sawPos = new Vector2[OrEmpty(data.saws).Length];

            BuildVisuals();
            UpdateMovers(0f);
            UpdateSaws(0f);
        }

        /// 사라지는 발판을 밟았다고 알린다 (CharacterBody가 부른다).
        /// 튕김판을 밟았다 — 잠깐 눌린 프레임으로 바꾼다
        public void NotifyBounce(int platformIndex)
        {
            int b = platformIndex - bouncerStart;
            if (bounceSquashUntil == null || b < 0 || b >= bounceSquashUntil.Length) return;
            bounceSquashUntil[b] = Time.time + SquashTime;
            var frames = lib != null ? lib.bounceMushroom : null;
            if (frames != null && frames.Length > 1 && bounceRenderers[b] != null)
                bounceRenderers[b].sprite = frames[1];
        }

        void UpdateBouncers()
        {
            if (bounceSquashUntil == null) return;
            var frames = lib != null ? lib.bounceMushroom : null;
            if (frames == null || frames.Length < 2) return;
            for (int i = 0; i < bounceSquashUntil.Length; i++)
            {
                if (bounceSquashUntil[i] <= 0f || Time.time < bounceSquashUntil[i]) continue;
                bounceSquashUntil[i] = 0f;
                if (bounceRenderers[i] != null) bounceRenderers[i].sprite = frames[0];
            }
        }

        /// 발판의 얼음 여부를 런타임에 바꾼다 (시험·연출용).
        public void SetIcy(int platformIndex, bool value)
        {
            if (platforms == null || platformIndex < 0 || platformIndex >= platforms.Length) return;
            platforms[platformIndex].icy = value;
        }

        public void NotifyStand(int platformIndex)
        {
            int v = platformIndex - vanishStart;
            if (v < 0 || v >= vanishState.Length) return;
            if (vanishState[v] == VanishState.Solid)
            {
                vanishState[v] = VanishState.Shaking;
                vanishTimer[v] = ShakeTime;
            }
        }

        void BuildVisuals()
        {
            for (int i = platformRoot.childCount - 1; i >= 0; i--)
                Destroy(platformRoot.GetChild(i).gameObject);

            var movers = OrEmpty(Data.movers);
            moverVisuals = new Transform[movers.Length];
            int vanishCount = OrEmpty(Data.vanishers).Length;
            vanishBody = new Transform[vanishCount];
            vanishFrost = new SpriteRenderer[vanishCount];

            for (int i = 0; i < platforms.Length; i++)
            {
                var p = platforms[i];
                float width = p.right - p.left;
                bool isMover = i >= moverStart && i < bouncerStart;
                bool isBounce = p.kind == Kind.Bounce;
                bool isVanish = p.kind == Kind.Vanish;
                bool isGround = i == 0;

                var go = new GameObject(
                    isBounce ? $"Bouncer{i - bouncerStart}" :
                    isVanish ? $"Vanisher{i - vanishStart}" :
                    isMover ? $"Mover{i - moverStart}" : $"Platform{i}");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3((p.left + p.right) * 0.5f, p.y, 0f);

                if (!isGround)
                {
                    var sh = new GameObject("shadow");
                    sh.transform.SetParent(go.transform, false);
                    sh.transform.localPosition = new Vector3(0f, Px.U(-26f), 0f);
                    sh.transform.localScale = new Vector3(width * 0.84f / Px.U(120f), 0.75f, 1f);
                    var shr = sh.AddComponent<SpriteRenderer>();
                    shr.sprite = ProceduralArt.Shadow;
                    shr.sortingOrder = -12;
                }

                BuildLedge(go.transform, width, floating: !isGround);

                if (p.icy)
                {
                    // 얼음은 한눈에 알아야 한다 — 발판 전체를 푸르게 물들이고 윗면에 광택
                    foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                        if (sr.sortingOrder <= -10) sr.color = new Color(0.72f, 0.88f, 1f);
                    var shine = new GameObject("ice");
                    shine.transform.SetParent(go.transform, false);
                    shine.transform.localPosition = new Vector3(0f, Px.U(3f), 0f);
                    shine.transform.localScale = new Vector3(width * 0.9f, Px.U(3f), 1f);
                    var ir = shine.AddComponent<SpriteRenderer>();
                    ir.sprite = ProceduralArt.Square;
                    ir.color = new Color(0.9f, 0.97f, 1f, 0.85f);
                    ir.sortingOrder = -9;
                }

                if (isVanish)
                {
                    // 윗면에 청록 표시줄 — "이 발판은 밟으면 사라져요"
                    var frost = new GameObject("frost");
                    frost.transform.SetParent(go.transform, false);
                    frost.transform.localPosition = new Vector3(0f, Px.U(2f), 0f);
                    frost.transform.localScale = new Vector3(width * 0.92f, Px.U(7f), 1f);
                    var fr = frost.AddComponent<SpriteRenderer>();
                    fr.sprite = ProceduralArt.Square;
                    fr.color = new Color(0.35f, 0.85f, 0.95f, 0.85f);
                    fr.sortingOrder = -9;
                    vanishBody[i - vanishStart] = go.transform;
                    vanishFrost[i - vanishStart] = fr;
                }

                if (isBounce)
                {
                    var pad = new GameObject("pad");
                    pad.transform.SetParent(go.transform, false);
                    var pr = pad.AddComponent<SpriteRenderer>();
                    if (lib != null && lib.bounceMushroom != null && lib.bounceMushroom.Length > 0)
                    {
                        pr.sprite = lib.bounceMushroom[0];   // 버섯 트램폴린 (피벗 하단)
                        // 원본 크기(0.5u)로 그리면 판정 폭보다도 작아 눈에 안 띈다 — 폭에 맞춘다
                        float vis = pr.sprite.rect.width / pr.sprite.pixelsPerUnit;
                        if (vis > 0.01f) pad.transform.localScale = Vector3.one * (width / vis);
                    }
                    else pr.sprite = ProceduralArt.BouncePad(Mathf.RoundToInt(width * Px.PPU));
                    pr.sortingOrder = -9;
                    int bi = i - bouncerStart;
                    if (bi >= 0 && bi < bounceVisuals.Length)
                    { bounceVisuals[bi] = pad.transform; bounceRenderers[bi] = pr; }
                }

                if (isGround)
                {
                    var fill = new GameObject("fill");
                    fill.transform.SetParent(go.transform, false);
                    fill.transform.localPosition = new Vector3(0f, -6f, 0f);
                    fill.transform.localScale = new Vector3(width, 12f, 1f);
                    var fr = fill.AddComponent<SpriteRenderer>();
                    fr.sprite = ProceduralArt.Square;
                    fr.color = ProceduralArt.Dirt;
                    fr.sortingOrder = -11;
                }

                if (!isGround && !isVanish && !isBounce)
                    Decorate(go.transform, width, i);

                if (isMover) moverVisuals[i - moverStart] = go.transform;
            }

            BuildSpikes();
            BuildWinds();
            BuildSaws();
            BuildRings();
            BuildVines();
            BuildRocks();
            BuildPendulums();
            BuildSteams();
            BuildUpdrafts();
            BuildBars();
            BuildRollers();
        }

        /// 떨어지는 돌 — 천장에 매달렸다가 밑을 지나면 떨어진다
        void BuildRocks()
        {
            var rocks = OrEmpty(Data.rocks);
            rockState = new RockState[rocks.Length];
            rockTimer = new float[rocks.Length];
            rockY = new float[rocks.Length];
            rockVy = new float[rocks.Length];
            rockVisuals = new Transform[rocks.Length];
            rockRenderers = new SpriteRenderer[rocks.Length];

            for (int i = 0; i < rocks.Length; i++)
            {
                rockState[i] = RockState.Hang;
                rockY[i] = rocks[i].y;
                var go = new GameObject("Rock");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(rocks[i].x, rocks[i].y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                var frames = lib != null ? lib.rockFall : null;
                sr.sprite = frames != null && frames.Length > 0 ? frames[0] : ProceduralArt.Square;
                if (frames == null || frames.Length == 0)
                {
                    sr.color = new Color(0.67f, 0.65f, 0.75f);
                    go.transform.localScale = Vector3.one * 0.3f;
                }
                sr.sortingOrder = -6;
                rockVisuals[i] = go.transform;
                rockRenderers[i] = sr;
            }
        }

        /// 흔들리는 추 — 사슬은 늘여서 돌리고, 추는 끝에 매단다
        void BuildPendulums()
        {
            var ps = OrEmpty(Data.pendulums);
            pendChain = new Transform[ps.Length];
            pendWeight = new Transform[ps.Length];
            pendPos = new Vector2[ps.Length];

            for (int i = 0; i < ps.Length; i++)
            {
                var go = new GameObject("Pendulum");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(ps[i].x, ps[i].y, 0f);
                pendChain[i] = go.transform;

                var chain = new GameObject("chain");
                chain.transform.SetParent(go.transform, false);
                var cr = chain.AddComponent<SpriteRenderer>();
                if (lib != null && lib.pendulumChain != null)
                {
                    cr.sprite = lib.pendulumChain;       // 피벗 상단
                    cr.drawMode = SpriteDrawMode.Tiled;
                    float tw = lib.pendulumChain.rect.width / lib.pendulumChain.pixelsPerUnit;
                    cr.size = new Vector2(tw, ps[i].length);
                }
                else { cr.sprite = ProceduralArt.Square; cr.color = new Color(0.67f, 0.65f, 0.75f);
                       chain.transform.localScale = new Vector3(Px.U(4f), ps[i].length, 1f); }
                cr.sortingOrder = -7;

                var w = new GameObject("weight");
                w.transform.SetParent(go.transform, false);
                var wr = w.AddComponent<SpriteRenderer>();
                if (lib != null && lib.pendulumWeight != null)
                {
                    wr.sprite = lib.pendulumWeight;
                    float vis = lib.pendulumWeight.rect.height / lib.pendulumWeight.pixelsPerUnit;
                    w.transform.localScale = Vector3.one * (PendulumRadius * 2f / vis);
                }
                else { wr.sprite = ProceduralArt.Square; wr.color = new Color(0.67f, 0.65f, 0.75f);
                       w.transform.localScale = Vector3.one * PendulumRadius * 2f; }
                wr.sortingOrder = -6;
                pendWeight[i] = w.transform;
            }
        }

        void UpdatePendulums()
        {
            var ps = OrEmpty(Data.pendulums);
            for (int i = 0; i < ps.Length; i++)
            {
                float a = Mathf.Sin(clock * ps[i].speed + ps[i].phase) * ps[i].swing;
                // 사슬을 통째로 돌린다 (피벗이 매단 지점이다)
                if (pendChain[i] != null)
                    pendChain[i].localRotation = Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg);
                Vector2 at = new Vector2(ps[i].x + Mathf.Sin(a) * ps[i].length,
                                         ps[i].y - Mathf.Cos(a) * ps[i].length);
                pendPos[i] = at;
                if (pendWeight[i] != null)
                    pendWeight[i].localPosition = new Vector3(0f, -ps[i].length, 0f);
            }
        }

        /// 뿜는 증기 — 주기의 뒤쪽 40% 동안 뿜는다
        void BuildSteams()
        {
            var ss = OrEmpty(Data.steams);
            steamVis = new SpriteRenderer[ss.Length];
            steamOn = new bool[ss.Length];
            for (int i = 0; i < ss.Length; i++)
            {
                var go = new GameObject("Steam");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(ss[i].x + ss[i].width * 0.5f, ss[i].y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                var frames = lib != null ? lib.steam : null;
                if (frames != null && frames.Length > 0)
                {
                    sr.sprite = frames[0];
                    float vh = frames[0].rect.height / frames[0].pixelsPerUnit;
                    go.transform.localScale = Vector3.one * (ss[i].height / Mathf.Max(0.01f, vh));
                    var a = go.AddComponent<SpriteAnim>();
                    a.frames = frames; a.fps = 10f;
                }
                else { sr.sprite = ProceduralArt.Square; sr.color = new Color(0.9f, 0.95f, 1f, 0.5f);
                       go.transform.localScale = new Vector3(ss[i].width, ss[i].height, 1f); }
                sr.sortingOrder = -5;
                steamVis[i] = sr;
            }
        }

        void UpdateSteams()
        {
            var ss = OrEmpty(Data.steams);
            for (int i = 0; i < ss.Length; i++)
            {
                float t = Mathf.Repeat(clock + ss[i].phase, Mathf.Max(0.2f, ss[i].period));
                steamOn[i] = t > ss[i].period * 0.6f;
                if (steamVis[i] != null) steamVis[i].enabled = steamOn[i];
            }
        }

        void UpdateRocks(float dt, float px, float py)
        {
            var rocks = OrEmpty(Data.rocks);
            var frames = lib != null ? lib.rockFall : null;
            for (int i = 0; i < rocks.Length; i++)
            {
                switch (rockState[i])
                {
                    case RockState.Hang:
                        // 밑을 지나면 흔들리기 시작한다
                        if (Mathf.Abs(px - rocks[i].x) < RockTriggerX &&
                            py < rocks[i].y && py > rocks[i].y - RockTriggerY)
                        {
                            rockState[i] = RockState.Shake;
                            rockTimer[i] = RockShake;
                            if (frames != null && frames.Length > 1) rockRenderers[i].sprite = frames[1];
                        }
                        break;

                    case RockState.Shake:
                        rockTimer[i] -= dt;
                        if (rockVisuals[i] != null)
                            rockVisuals[i].localPosition = new Vector3(
                                rocks[i].x + Mathf.Sin(clock * 60f) * Px.U(1.5f), rocks[i].y, 0f);
                        if (rockTimer[i] <= 0f)
                        {
                            rockState[i] = RockState.Fall;
                            rockVy[i] = 0f;
                            if (frames != null && frames.Length > 2) rockRenderers[i].sprite = frames[2];
                        }
                        break;

                    case RockState.Fall:
                        rockVy[i] -= Px.A(0.55f) * dt;
                        rockY[i] += rockVy[i] * dt;
                        if (rockVisuals[i] != null)
                            rockVisuals[i].localPosition = new Vector3(rocks[i].x, rockY[i], 0f);
                        // 바닥이나 아래 발판에 닿으면 부서진다
                        if (rockY[i] <= Data.groundY || LandedOnSomething(rocks[i].x, rockY[i], rocks[i].y))
                        {
                            rockState[i] = RockState.Gone;
                            rockTimer[i] = RockBack;
                            if (rockVisuals[i] != null) rockVisuals[i].gameObject.SetActive(false);
                        }
                        break;

                    case RockState.Gone:
                        rockTimer[i] -= dt;
                        if (rockTimer[i] > 0f) break;
                        rockState[i] = RockState.Hang;
                        rockY[i] = rocks[i].y;
                        if (rockVisuals[i] != null)
                        {
                            rockVisuals[i].localPosition = new Vector3(rocks[i].x, rocks[i].y, 0f);
                            rockVisuals[i].gameObject.SetActive(true);
                        }
                        if (frames != null && frames.Length > 0) rockRenderers[i].sprite = frames[0];
                        break;
                }
            }
        }

        /// 떨어지던 돌이 무언가에 닿았나.
        /// hangY 는 매달려 있던 높이다 — 자기가 매달린 천장에 '착지' 하지 않도록 뺀다
        /// (돌이 천장 바로 아래에서 시작해서, 안 빼면 떨어지자마자 사라졌다).
        bool LandedOnSomething(float x, float y, float hangY)
        {
            for (int i = 0; i < platforms.Length; i++)
            {
                var p = platforms[i];
                if (!p.active) continue;
                if (p.y > hangY - 0.3f) continue;
                if (x > p.left && x < p.right && y <= p.y && y > p.y - 0.25f) return true;
            }
            return false;
        }

        /// 상승 기류 — 기둥 안을 세로로 채운다
        void BuildUpdrafts()
        {
            foreach (var u in OrEmpty(Data.updrafts))
            {
                var go = new GameObject("Updraft");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(u.x + u.width * 0.5f, u.y, 0f);
                var frames = lib != null ? lib.updraft : null;
                if (frames != null && frames.Length > 0 && frames[0] != null)
                {
                    // 기둥 높이만큼 세로로 쌓는다 (피벗 하단)
                    float cell = frames[0].rect.height / frames[0].pixelsPerUnit;
                    int rows = Mathf.Max(1, Mathf.RoundToInt(u.height / cell));
                    for (int k = 0; k < rows; k++)
                    {
                        var g = new GameObject("u");
                        g.transform.SetParent(go.transform, false);
                        g.transform.localPosition = new Vector3(0f, cell * k, 0f);
                        g.transform.localScale = new Vector3(u.width / cell, 1f, 1f);
                        var sr = g.AddComponent<SpriteRenderer>();
                        sr.sprite = frames[0];
                        sr.sortingOrder = -7;
                        if (frames.Length > 1)
                        {
                            // 칸마다 시작 프레임을 어긋나게 — 기둥 전체가 한 박자로
                            // 깜빡이면 기류가 아니라 전광판처럼 보인다
                            var shifted = new Sprite[frames.Length];
                            for (int f = 0; f < frames.Length; f++)
                                shifted[f] = frames[(f + k) % frames.Length];
                            var a = g.AddComponent<SpriteAnim>();
                            a.frames = shifted; a.fps = 8f;
                        }
                    }
                }
            }
        }

        /// 매달려 건너는 가지 — 가로로 이어 붙인다
        void BuildBars()
        {
            foreach (var b in OrEmpty(Data.bars))
            {
                var go = new GameObject("Bar");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(b.x + b.width * 0.5f, b.y, 0f);
                if (lib != null && lib.branchBar != null)
                    TileRow(go.transform, new[] { lib.branchBar }, b.width, 0f, 0f, -7);
                else
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = ProceduralArt.Square;
                    sr.color = new Color(0.73f, 0.50f, 0.35f);
                    go.transform.localScale = new Vector3(b.width, Px.U(6f), 1f);
                    sr.sortingOrder = -7;
                }
            }
        }

        /// 구르는 통나무
        void BuildRollers()
        {
            var rollers = OrEmpty(Data.rollers);
            rollerPos = new Vector2[rollers.Length];
            rollerVisuals = new Transform[rollers.Length];
            for (int i = 0; i < rollers.Length; i++)
            {
                var r = rollers[i];
                var go = new GameObject("Roller");
                go.transform.SetParent(platformRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (lib != null && lib.sawLog != null)
                {
                    sr.sprite = lib.sawLog;
                    float vis = lib.sawLog.rect.height / lib.sawLog.pixelsPerUnit;
                    go.transform.localScale = Vector3.one * (r.radius * 2f / vis);
                }
                else sr.sprite = ProceduralArt.Saw(Mathf.RoundToInt(r.radius * 2f * Px.PPU));
                sr.sortingOrder = -6;
                rollerVisuals[i] = go.transform;
                rollerPos[i] = new Vector2(r.x, r.y);
            }
        }

        void BuildVines()
        {
            var vines = OrEmpty(Data.vines);
            Sprite[] vf = lib != null ? lib.thornVine : null;
            Sprite s = vf != null && vf.Length > 0 ? vf[0] : null;
            foreach (var v in vines)
            {
                var go = new GameObject("Vine");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(v.x, v.y + v.height, 0f);  // 피벗 상단
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2;   // 발판 앞, 캐릭터(10) 뒤
                if (s != null)
                {
                    sr.sprite = s;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    float tileW = s.rect.width / s.pixelsPerUnit;
                    sr.size = new Vector2(tileW, v.height);
                }
                else
                {
                    sr.sprite = ProceduralArt.Square;
                    sr.color = new Color(0.42f, 0.52f, 0.30f);
                    go.transform.localScale = new Vector3(Px.U(6f), v.height, 1f);
                }
            }
        }

        /// (x,y)가 밧줄에 겹치면 true. vineX=밧줄 중심, vineTop=꼭대기 y.
        public bool TryGetVine(float x, float y, out float vineX, out float vineTop)
        {
            vineX = 0f; vineTop = 0f;
            foreach (var v in OrEmpty(Data.vines))
            {
                if (x > v.x - 0.2f && x < v.x + 0.2f && y > v.y - 0.1f && y < v.y + v.height)
                {
                    vineX = v.x; vineTop = v.y + v.height;
                    return true;
                }
            }
            return false;
        }

        /// 발판 윗줄을 타일로 깐다: 왼끝·오른끝은 낱장, 가운데는 Tiled 로 반복.
        /// floating=true 면 떠 있는 발판 타일(4·5·6), 아니면 지면 타일(0·1·3).
        void BuildLedge(Transform parent, float width, bool floating)
        {
            if (tiles == null || tiles.Length < 7 || tiles[0] == null)
            {
                var sr0 = parent.gameObject.AddComponent<SpriteRenderer>();
                sr0.sprite = ProceduralArt.Ledge(Mathf.RoundToInt(width * Px.PPU), false, withHighlight: true);
                sr0.sortingOrder = -10;
                return;
            }
            float T = TileU;
            int L = floating ? 4 : 0, M = floating ? 5 : 1, R = floating ? 6 : 3;
            float top = -T * 0.5f;   // 타일 윗면이 발판면(y=0)에 오게

            Cap(parent, tiles[L], -width * 0.5f + T * 0.5f, top);
            Cap(parent, tiles[R], width * 0.5f - T * 0.5f, top);

            float midW = width - 2f * T;
            if (midW > T * 0.5f)
            {
                var g = new GameObject("mid");
                g.transform.SetParent(parent, false);
                g.transform.localPosition = new Vector3(0f, top, 0f);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = tiles[M];
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(midW, T);
                sr.sortingOrder = -10;
            }
        }

        void Cap(Transform parent, Sprite s, float x, float y)
        {
            var g = new GameObject("cap");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3(x, y, 0f);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = -10;
        }

        // 발판 위에 이따금 꽃·풀만 얹는다 (덤불·버섯·바위는 '잘린' 느낌이라 뺐다).
        static readonly int[] Deco = { 21, 22, 23 };
        void Decorate(Transform parent, float width, int seed)
        {
            if (tiles == null || tiles.Length < 27) return;
            uint r = (uint)(seed * 1103515245 + 12345);
            if (((r >> 16) & 3u) == 0u) return;   // 약 1/4 은 장식 없음
            int idx = Deco[(int)((r >> 8) % (uint)Deco.Length)];
            if (tiles[idx] == null) return;
            float T = TileU;
            float x = Mathf.Lerp(-width * 0.5f + T, width * 0.5f - T, (r & 0xFFu) / 255f);
            var g = new GameObject("deco");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3(x, T * 0.5f, 0f);  // 풀 위에 앉힌다
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = tiles[idx];
            sr.sortingOrder = -9;
        }

        void BuildSpikes()
        {
            foreach (var s in OrEmpty(Data.spikes))
            {
                var go = new GameObject(s.down ? "HangThorns" : "Spikes");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(s.x + s.width * 0.5f, s.y, 0f);

                // 위아래 모두 같은 가시 그림을 쓴다. 매달린 쪽은 뒤집어 발판 밑으로 뻗는다.
                // 예전에는 매달린 가시가 덩굴(thornVine) 그림이라 타고 오르는 밧줄과
                // 구별이 안 됐다 — 하나는 지름길, 하나는 아픈 것인데 똑같이 보였다.
                Sprite[] frames = lib != null && lib.spikeFloor != null
                    ? new[] { lib.spikeFloor } : null;
                if (frames != null && frames[0] != null)
                    TileRow(go.transform, frames, s.width, s.down ? -TileU : 0f, 0f, -8,
                            yScale: s.down ? -1.7f : 1f);   // 매달린 가시는 뒤집고 더 길게
                else
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = ProceduralArt.Spikes(Mathf.RoundToInt(s.width * Px.PPU), s.down);
                    sr.sortingOrder = -8;
                }
            }
        }

        /// 스프라이트를 발판 너비에 맞춰 가로로 여러 장 깐다(가시·매달린 가시·바람).
        /// 피벗은 스프라이트 자체가 정한다(가시=하단, 매달린 가시=상단).
        void TileRow(Transform parent, Sprite[] frames, float width, float y, float fps, int order,
                     float yScale = 1f)
        {
            float T = TileU;
            int n = Mathf.Max(1, Mathf.RoundToInt(width / T));
            float cw = width / n;
            for (int k = 0; k < n; k++)
            {
                var g = new GameObject("t");
                g.transform.SetParent(parent, false);
                g.transform.localPosition = new Vector3(-width * 0.5f + cw * (k + 0.5f), y, 0f);
                g.transform.localScale = new Vector3(cw / T, yScale, 1f);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingOrder = order;
                if (frames.Length > 1 && fps > 0f)
                {
                    var a = g.AddComponent<SpriteAnim>();
                    a.frames = frames; a.fps = fps;
                }
            }
        }

        void BuildSaws()
        {
            var saws = OrEmpty(Data.saws);
            sawVisuals = new Transform[saws.Length];
            for (int i = 0; i < saws.Length; i++)
            {
                var s = saws[i];
                var go = new GameObject("Saw");
                go.transform.SetParent(platformRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (lib != null && lib.sawLog != null)
                {
                    sr.sprite = lib.sawLog;
                    float vis = lib.sawLog.rect.height / lib.sawLog.pixelsPerUnit;
                    go.transform.localScale = Vector3.one * (s.blade * 2f / vis);  // 판정 크기에 맞춤
                }
                else sr.sprite = ProceduralArt.Saw(Mathf.RoundToInt(s.blade * 2f * Px.PPU));
                sr.sortingOrder = -6;
                sawVisuals[i] = go.transform;
            }
        }

        void BuildRings()
        {
            foreach (var r in OrEmpty(Data.rings))
            {
                if (!r.thorny) continue;   // 평범한 링은 Collectibles 가 그린다
                var go = new GameObject("ThornRing");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(r.x, r.y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                if (lib != null && lib.ringThorn != null)
                {
                    sr.sprite = lib.ringThorn;
                    float vis = lib.ringThorn.rect.height / lib.ringThorn.pixelsPerUnit;
                    go.transform.localScale = Vector3.one * (r.radius * 2f / vis);
                }
                else sr.sprite = ProceduralArt.Ring(Mathf.RoundToInt(r.radius * 2f * Px.PPU), true);
                sr.sortingOrder = -6;
            }
        }

        void BuildWinds()
        {
            foreach (var w in OrEmpty(Data.winds))
            {
                var go = new GameObject("Wind");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(w.x + w.width * 0.5f, w.y + w.height * 0.5f, 0f);

                var box = new GameObject("zone");
                box.transform.SetParent(go.transform, false);
                box.transform.localScale = new Vector3(w.width, w.height, 1f);
                var br = box.AddComponent<SpriteRenderer>();
                br.sprite = ProceduralArt.Square;
                br.color = new Color(0.6f, 0.85f, 1f, 0.14f);
                br.sortingOrder = -13;

                // 방향 갈매기 몇 개
                int dir = w.force >= 0 ? 1 : -1;
                int count = Mathf.Clamp(Mathf.RoundToInt(w.height / Px.U(60f)), 1, 5);
                bool hasWind = lib != null && lib.wind != null && lib.wind.Length > 0;
                for (int k = 0; k < count; k++)
                {
                    var ch = new GameObject("gust");
                    ch.transform.SetParent(go.transform, false);
                    float ty = Mathf.Lerp(-w.height * 0.35f, w.height * 0.35f, count == 1 ? 0.5f : k / (count - 1f));
                    ch.transform.localPosition = new Vector3(0f, ty, 0f);
                    ch.transform.localScale = new Vector3(dir, 1f, 1f);
                    var cr = ch.AddComponent<SpriteRenderer>();
                    cr.sortingOrder = -7;
                    if (hasWind)
                    {
                        cr.sprite = lib.wind[0];
                        var a = ch.AddComponent<SpriteAnim>();
                        a.frames = lib.wind; a.fps = 6f;
                    }
                    else cr.sprite = ProceduralArt.WindChevron;
                }
            }
        }

        // 실행 순서가 플레이어/강아지보다 앞이다(-100).
        void FixedUpdate()
        {
            if (Data == null) return;
            clock += Time.fixedDeltaTime;
            UpdateMovers(Time.fixedDeltaTime);
            UpdateVanishers(Time.fixedDeltaTime);
            UpdateSaws(Time.fixedDeltaTime);
            UpdateRollers();
            UpdateBouncers();
            if (player != null) UpdateRocks(Time.fixedDeltaTime, player.X, player.Y);
            UpdatePendulums();
            UpdateSteams();
        }

        void UpdateRollers()
        {
            var rollers = OrEmpty(Data.rollers);
            for (int i = 0; i < rollers.Length; i++)
            {
                var r = rollers[i];
                float t = Mathf.Sin(clock * r.speed + r.phase);
                float x = r.x + t * r.range;
                rollerPos[i] = new Vector2(x, r.y + r.radius);
                if (rollerVisuals[i] != null)
                {
                    rollerVisuals[i].localPosition = new Vector3(x, r.y + r.radius, 0f);
                    // 굴러가는 방향으로 돈다 (반지름으로 나눠 미끄러지지 않게)
                    rollerVisuals[i].localRotation =
                        Quaternion.Euler(0f, 0f, -(x - r.x) / Mathf.Max(0.01f, r.radius) * Mathf.Rad2Deg);
                }
            }
        }

        /// (x,y)가 상승 기류 안인가. lift 는 초당 올라가는 속도.
        public bool InUpdraft(float x, float y, out float lift)
        {
            lift = 0f;
            foreach (var u in OrEmpty(Data.updrafts))
                if (x > u.x && x < u.x + u.width && y > u.y && y < u.y + u.height)
                { lift = u.lift; return true; }
            return false;
        }

        /// 머리가 가지에 닿았나. barY=가지 높이, barL/barR=좌우 끝.
        public bool TryGetBar(float x, float headY, out float barY, out float barL, out float barR)
        {
            barY = barL = barR = 0f;
            foreach (var b in OrEmpty(Data.bars))
            {
                if (x < b.x || x > b.Right) continue;
                if (headY < b.y - 0.22f || headY > b.y + 0.12f) continue;
                barY = b.y; barL = b.x; barR = b.Right;
                return true;
            }
            return false;
        }

        void UpdateSaws(float dt)
        {
            var saws = OrEmpty(Data.saws);
            for (int i = 0; i < saws.Length; i++)
            {
                var s = saws[i];
                float a = clock * s.speed;
                sawPos[i] = new Vector2(s.x + Mathf.Cos(a) * s.orbit, s.y + Mathf.Sin(a) * s.orbit);
                if (sawVisuals != null && sawVisuals[i] != null)
                {
                    sawVisuals[i].localPosition = new Vector3(sawPos[i].x, sawPos[i].y, 0f);
                    sawVisuals[i].localRotation = Quaternion.Euler(0f, 0f, -clock * 260f);  // 스핀
                }
            }
        }

        void UpdateMovers(float dt)
        {
            var movers = OrEmpty(Data.movers);
            for (int i = 0; i < movers.Length; i++)
            {
                var m = movers[i];
                int pi = moverStart + i;
                var p = platforms[pi];
                float width = p.right - p.left;
                float offset = Mathf.Sin(clock * m.speed + m.phase) * m.range;

                if (m.vertical)
                {
                    float y = m.y + offset;
                    p.deltaY = y - p.y;
                    p.deltaX = 0f;
                    p.y = y;
                }
                else
                {
                    float left = m.x + offset;
                    p.deltaX = left - p.left;
                    p.deltaY = 0f;
                    p.left = left;
                    p.right = left + width;
                }
                platforms[pi] = p;

                if (moverVisuals[i] != null)
                    moverVisuals[i].localPosition = new Vector3(p.left + width * 0.5f, p.y, 0f);
            }
        }

        void UpdateVanishers(float dt)
        {
            for (int v = 0; v < vanishState.Length; v++)
            {
                if (vanishState[v] == VanishState.Solid) continue;

                vanishTimer[v] -= dt;
                int pi = vanishStart + v;
                var p = platforms[pi];
                var bodyT = vanishBody[v];
                var frost = vanishFrost[v];

                if (vanishState[v] == VanishState.Shaking)
                {
                    // 흔들림 + 표시줄 깜빡임 (곧 사라진다는 신호)
                    if (bodyT != null)
                        bodyT.localPosition = new Vector3(
                            (p.left + p.right) * 0.5f + Mathf.Sin(clock * 70f) * Px.U(1.6f), p.y, 0f);
                    if (frost != null)
                    {
                        var c = frost.color; c.a = Mathf.Sin(clock * 40f) > 0f ? 1f : 0.3f; frost.color = c;
                    }
                    if (vanishTimer[v] <= 0f)
                    {
                        vanishState[v] = VanishState.Gone;
                        vanishTimer[v] = GoneTime;
                        p.active = false; platforms[pi] = p;
                        if (bodyT != null) bodyT.gameObject.SetActive(false);
                    }
                }
                else // Gone → 되살아남
                {
                    if (vanishTimer[v] <= 0f)
                    {
                        vanishState[v] = VanishState.Solid;
                        p.active = true; platforms[pi] = p;
                        if (bodyT != null)
                        {
                            bodyT.gameObject.SetActive(true);
                            bodyT.localPosition = new Vector3((p.left + p.right) * 0.5f, p.y, 0f);
                        }
                        if (frost != null) { var c = frost.color; c.a = 0.85f; frost.color = c; }
                    }
                }
            }
        }
    }
}
