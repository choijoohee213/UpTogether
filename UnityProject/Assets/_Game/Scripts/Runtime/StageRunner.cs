using UnityEngine;

namespace UpTogether
{
    /// 스테이지 데이터를 런타임 발판 배열로 펼치고, 움직이는 발판을 갱신한다.
    /// 발판은 콜라이더가 아니라 배열이다 — 충돌은 CharacterBody가 직접 푼다.
    [DefaultExecutionOrder(-100)]
    public class StageRunner : MonoBehaviour
    {
        public struct RuntimePlatform
        {
            public float left, right, y;
            public float deltaX; // 이번 스텝에 옆으로 움직인 양. 위에 탄 몸이 같이 실려 간다.
        }

        public StageData Data { get; private set; }
        public int PlatformCount => platforms.Length;
        public RuntimePlatform GetPlatform(int i) => platforms[i];
        /// 이 아래로 떨어지면 바닥으로 되돌린다 (원본: y > H + 300px)
        public float FallResetY => -Px.U(300f);

        public StageData startStage;
        public Transform platformRoot;
        RuntimePlatform[] platforms;
        int moverStart;              // platforms[moverStart..] 가 움직이는 발판
        Transform[] moverVisuals;
        float clock;

        void Awake()
        {
            if (startStage == null || platformRoot == null)
            {
                // 조용히 넘어가면 이 뒤로 NullReference가 매 프레임 쏟아진다. 여기서 끊는다.
                Debug.LogError($"{name}: startStage/platformRoot가 비어 있다. " +
                               "UpTogether ▸ Build Play Scene 을 다시 실행하거나 인스펙터에서 직접 지정할 것.", this);
                enabled = false;
                return;
            }
            Load(startStage);
        }

        public void Load(StageData data)
        {
            Data = data;
            clock = 0f;

            int total = data.platforms.Length + data.movers.Length;
            platforms = new RuntimePlatform[total];
            moverStart = data.platforms.Length;

            for (int i = 0; i < data.platforms.Length; i++)
            {
                var p = data.platforms[i];
                platforms[i] = new RuntimePlatform { left = p.x, right = p.x + p.width, y = p.y };
            }
            for (int i = 0; i < data.movers.Length; i++)
            {
                var m = data.movers[i];
                platforms[moverStart + i] = new RuntimePlatform { left = m.x, right = m.x + m.width, y = m.y };
            }

            BuildVisuals();
            UpdateMovers(0f);
        }

        /// 발판을 그린다. 흙+잔디는 폭에 맞춰 한 장씩 텍스처를 굽는다 (ProceduralArt.Ledge).
        /// 충돌면은 잔디 윗면이고, 스프라이트 피벗이 거기에 맞춰져 있다.
        void BuildVisuals()
        {
            for (int i = platformRoot.childCount - 1; i >= 0; i--)
                Destroy(platformRoot.GetChild(i).gameObject);

            moverVisuals = new Transform[Data.movers.Length];

            for (int i = 0; i < platforms.Length; i++)
            {
                var p = platforms[i];
                bool isMover = i >= moverStart;
                float width = p.right - p.left;
                // 바닥은 베이커가 가장 먼저 넣는 발판이다.
                // 예전엔 '폭 1000px 초과'로 봤는데, 맵을 좁히자(1250->900) 조건에 안 걸려
                // 아래를 메우는 흙이 통째로 사라졌다. 폭으로 판단하면 안 된다.
                bool isGround = i == 0;

                var go = new GameObject(isMover ? $"Mover{i - moverStart}" : $"Platform{i}");
                go.transform.SetParent(platformRoot, false);
                go.transform.localPosition = new Vector3((p.left + p.right) * 0.5f, p.y, 0f);

                // 공중 발판 아래 옅은 그림자 — 높이감이 확 산다
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

                // 바닥은 화면 아래까지 흙으로 메운다 (원본: fillRect(x, y, w, VH+200))
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
        }

        // 실행 순서가 플레이어/강아지보다 앞이다(-100).
        // 발판이 이동한 뒤의 위치로 충돌을 풀어야 하기 때문.
        void FixedUpdate()
        {
            if (Data == null) return;
            clock += Time.fixedDeltaTime;
            UpdateMovers(Time.fixedDeltaTime);
        }

        void UpdateMovers(float dt)
        {
            for (int i = 0; i < Data.movers.Length; i++)
            {
                var m = Data.movers[i];
                int pi = moverStart + i;
                var p = platforms[pi];

                float left = m.x + Mathf.Sin(clock * m.speed + m.phase) * m.range;
                p.deltaX = left - p.left;
                float width = p.right - p.left;
                p.left = left;
                p.right = left + width;
                platforms[pi] = p;

                if (moverVisuals[i] != null)
                {
                    var v = moverVisuals[i].localPosition;
                    v.x = left + width * 0.5f;
                    moverVisuals[i].localPosition = v;
                }
            }
        }
    }
}
