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
        }

        public StageData Data { get; private set; }
        public int PlatformCount => platforms.Length;
        public RuntimePlatform GetPlatform(int i) => platforms[i];
        /// 이 아래로 떨어지면 바닥으로 되돌린다 (원본: y > H + 300px)
        public float FallResetY => -Px.U(300f);

        public StageData startStage;
        public Transform platformRoot;

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
                platforms[at++] = new RuntimePlatform { left = p.x, right = p.x + p.width, y = p.y, kind = Kind.Normal, active = true };

            moverStart = at;
            foreach (var m in movers)
                platforms[at++] = new RuntimePlatform { left = m.x, right = m.x + m.width, y = m.y, kind = Kind.Normal, active = true };

            bouncerStart = at;
            foreach (var b in bouncers)
                platforms[at++] = new RuntimePlatform { left = b.x, right = b.x + b.width, y = b.y, kind = Kind.Bounce, active = true };

            vanishStart = at;
            foreach (var v in vanishers)
                platforms[at++] = new RuntimePlatform { left = v.x, right = v.x + v.width, y = v.y, kind = Kind.Vanish, active = true };

            vanishState = new VanishState[vanishers.Length];
            vanishTimer = new float[vanishers.Length];

            BuildVisuals();
            UpdateMovers(0f);
        }

        /// 사라지는 발판을 밟았다고 알린다 (CharacterBody가 부른다).
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

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralArt.Ledge(Mathf.RoundToInt(width * Px.PPU), isMover, withHighlight: !isGround);
                sr.sortingOrder = -10;

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
                    pr.sprite = ProceduralArt.BouncePad(Mathf.RoundToInt(width * Px.PPU));
                    pr.sortingOrder = -9;
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

                if (isMover) moverVisuals[i - moverStart] = go.transform;
            }

            BuildSpikes();
            BuildWinds();
        }

        void BuildSpikes()
        {
            foreach (var s in OrEmpty(Data.spikes))
            {
                var go = new GameObject("Spikes");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3(s.x + s.width * 0.5f, s.y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralArt.Spikes(Mathf.RoundToInt(s.width * Px.PPU));
                sr.sortingOrder = -8;
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
                for (int k = 0; k < count; k++)
                {
                    var ch = new GameObject("arrow");
                    ch.transform.SetParent(go.transform, false);
                    float ty = Mathf.Lerp(-w.height * 0.35f, w.height * 0.35f, count == 1 ? 0.5f : k / (count - 1f));
                    ch.transform.localPosition = new Vector3(0f, ty, 0f);
                    ch.transform.localScale = new Vector3(dir, 1f, 1f);
                    var cr = ch.AddComponent<SpriteRenderer>();
                    cr.sprite = ProceduralArt.WindChevron;
                    cr.sortingOrder = -7;
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
