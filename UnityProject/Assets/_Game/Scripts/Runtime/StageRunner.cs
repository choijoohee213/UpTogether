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
        /// 숲 타일셋 32칸(위-왼쪽부터 0번). SceneBuilder 가 채운다. 비면 절차적 발판으로 떨어진다.
        public Sprite[] tiles;
        /// 장애물 스프라이트. 비면 절차적 도형으로 떨어진다.
        public SpriteLib lib;

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

            sawPos = new Vector2[OrEmpty(data.saws).Length];

            BuildVisuals();
            UpdateMovers(0f);
            UpdateSaws(0f);
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

                BuildLedge(go.transform, width, floating: !isGround);

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
                        pr.sprite = lib.bounceMushroom[0];   // 버섯 트램폴린 (피벗 하단)
                    else pr.sprite = ProceduralArt.BouncePad(Mathf.RoundToInt(width * Px.PPU));
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

                if (!isGround && !isVanish && !isBounce)
                    Decorate(go.transform, width, i);

                if (isMover) moverVisuals[i - moverStart] = go.transform;
            }

            BuildSpikes();
            BuildWinds();
            BuildSaws();
            BuildRings();
            BuildVines();
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
