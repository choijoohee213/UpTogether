using System;
using UnityEngine;

namespace UpTogether
{
    /// 강아지. 메이플스토리 펫 방식이다 (레퍼런스 영상 분석).
    ///
    /// 걷고 뛴다. 다만 ★경로를 계산하지 않는다★.
    /// 어느 발판을 밟고 어떻게 올라갈지 따지지 않고, 그냥 플레이어 쪽으로 걷다가
    /// 위에 있으면 폴짝 뛴다. 성공하든 못 하든 상관없다 —
    /// 못 따라가면 워프가 덮어준다. 그래서 갇히거나 앞서 나갈 일이 없다.
    ///
    /// 예전에 발판을 골라 경로를 짜게 만들었더니 헛뛰고 떨어지거나,
    /// 한 칸씩 기계적으로 기어오르거나, 플레이어보다 먼저 도착하는 문제가 끝없이 나왔다.
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(CharacterBody))]
    public class DogController : MonoBehaviour
    {
        /// 이 안에 들어오면 멈춘다. 없으면 제자리에서 덜덜 떤다.
        const float FollowDeadzonePx = 10f;
        /// 발판 가장자리에서 이만큼 안쪽까지만 걸어간다
        const float EdgeInsetPx = 6f;
        /// 연달아 뛰지 않도록 두는 간격
        const float HopInterval = 0.45f;
        /// 워프 직후 잠깐은 다시 워프하지 않는다
        const float WarpCooldown = 0.3f;
        /// 멈칫한 뒤 실제로 뛸 틈. 이만큼은 워프를 미뤄 준다.
        const float HesitateGrace = 0.2f;

        public Tuning tuning;
        public StageRunner stage;
        public PlayerController player;
        /// 친밀도가 거리·멈칫·안기기를 바꾼다. 비어 있으면 다 친한 것으로 둔다.
        public Bond bond;

        [Header("워프 연출")]
        public Puffs puffs;
        public DogVisual visual;
        public SpriteLib lib;

        public bool IsClinging { get; private set; }
        public CharacterBody Body => body;
        /// 계측용
        public int TeleportCount { get; private set; }

        /// 낙하할 때 달려와 안길 만큼 친해졌는가. 서사가 이 선을 기준으로 갈린다.
        public bool ClingUnlocked => bond == null || bond.Value >= tuning.bondClingUnlock;
        /// 찔리거나 크게 떨어진 뒤 움츠러든 상태
        public bool InFlashback => Time.time < flashbackUntil;

        /// 스스로 뛰어 올라가 착지한 순간 (워프로 옮겨온 것은 빼야 "혼자 해냈다"가 된다)
        public event Action SelfLanded;

        CharacterBody body;
        float farSince = -1f;
        float lastWarp = -99f;
        float lastHop = -99f;
        float aboveSince = -1f;      // 플레이어가 위에 있다고 본 시점 (멈칫 재는 용)
        float flashbackUntil = -99f;
        bool hopping, leftGround;    // 제 발로 뛰어 올라갔다 착지했는지

        /// 친밀도 0~1. 강아지 행동을 보간하는 유일한 입력이다.
        /// DogVisual 도 읽는다 — 거리·타이밍만으로는 눈에 안 띄어서 몸짓으로도 보여준다.
        public float Trust => bond != null ? bond.Normalized : 1f;

        /// 뛰기 전에 멈칫하는 시간. 친해질수록 0 에 가까워진다.
        float HesitateSeconds => Mathf.Lerp(tuning.dogHesitateSeconds, 0f, Trust);

        /// 지금 두고 싶은 거리. 친밀도가 낮으면 멀고, 움츠러든 동안은 더 멀다.
        float TrailU
        {
            get
            {
                float u = Mathf.Lerp(tuning.DogTrailShyU, tuning.DogTrailU, Trust);
                return InFlashback ? u * tuning.dogFlashbackTrailScale : u;
            }
        }

        /// 찔렸거나 크게 떨어졌을 때. 한동안 거리를 벌린다.
        /// 트라우마는 한 번 낫고 끝이 아니라는 것을 몸으로 보여주는 장치다.
        public void Flashback() => flashbackUntil = Time.time + tuning.dogFlashbackSeconds;

        void Awake()
        {
            if (stage == null || stage.Data == null) { enabled = false; return; }
            body = GetComponent<CharacterBody>();
            body.tuning = tuning;
            body.Bind(stage);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var p = player.Body;

            if (UpdateCling(dt, p)) return;

            Follow(dt, p);
            WarpIfLeftBehind(p);
        }


        /// 크게 떨어질 때 달려와 품에 안긴다. 이 게임만의 동작이라 그대로 둔다.
        bool UpdateCling(float dt, CharacterBody p)
        {
            // 빠르게 내려오는 것만으로는 부족하다 — 높이 뛰었다 내려오는 것도 그 속도를 넘긴다.
            // 마지막으로 발을 붙였던 높이보다 확실히 아래여야 진짜 낙하다.
            bool reallyFalling = !p.grounded && p.vy < -tuning.DogClingFallV
                                 && p.Y < player.LastGroundedY - tuning.DogClingMinDropU;

            // 아직 안 친하면 달려오지 않는다. 발판 위에서 멀어지는 것을 보고만 있는다.
            // 서사가 실제로 작동하는 지점이다 — 안기기는 주어지는 게 아니라 얻어내는 것.
            if (!ClingUnlocked) reallyFalling = false;

            // 밧줄·가지·상승 기류는 강아지가 제 발로 따라갈 수 없는 길이다.
            // 뒤에서 낑낑대다 워프하는 것보다, 달려와 품에 안겨 같이 가는 쪽이
            // 이 게임답다 (안긴 자세·하트·소리는 이미 다 있다).
            // 여기는 친밀도로 막지 않는다 — 막으면 뒤에 남아 워프만 반복한다.
            bool cannotFollow = player.Climbing || player.Hanging || player.Lifted;

            if (reallyFalling || cannotFollow) IsClinging = true;
            if (!IsClinging) return false;

            // 안기는 동안의 점프는 '혼자 해낸 것'이 아니다
            hopping = false; leftGround = false;

            float k = Px.Smoothing(tuning.dogClingLerp, dt);
            // 밧줄은 주인공이 뒷모습이라 옆구리에 안겨 있으면 어색하다.
            // 가운데로 붙여 등에 업힌 것처럼 보이게 한다 (그림은 주인공 뒤에 깔린다).
            float offX = player.Climbing ? 0f : tuning.DogClingOffXU * p.face;
            body.X += ((p.X - offX) - body.X) * k;
            body.Y += ((p.Y + tuning.DogClingOffYU) - body.Y) * k;
            body.face = p.face;
            body.vx = 0f; body.vy = 0f;
            // 땅을 딛는 순간 내려온다. 다만 타는 중에는 발이 닿아도 계속 안겨 있는다.
            if (p.grounded && !cannotFollow) IsClinging = false;
            return true;
        }

        /// 플레이어 뒤를 따라 걷고, 위에 있으면 폴짝 뛴다. 어디에 착지할지는 따지지 않는다.
        /// 두는 거리와 뛰기 전 멈칫은 친밀도가 정한다.
        void Follow(float dt, CharacterBody p)
        {
            // 목표보다 가까우면 아래 Step 이 반대로 걸어 나간다.
            // 그래서 거리만 멀게 두면 "다가가면 물러선다"가 따로 코드 없이 나온다.
            float want = p.X - TrailU * p.face;

            // 딛고 선 발판 밖으로는 걸어 나가지 않는다.
            // 목표가 발판 끝을 넘어가면 그대로 걸어 나가 떨어진다.
            if (body.groundIndex >= 0)
            {
                var cur = stage.GetPlatform(body.groundIndex);
                float inset = Px.U(EdgeInsetPx);
                float lo = cur.left + inset, hi = cur.right - inset;
                if (lo <= hi) want = Mathf.Clamp(want, lo, hi);
            }

            float dead = Px.U(FollowDeadzonePx);
            int dir = body.X > want + dead ? -1 : (body.X < want - dead ? 1 : 0);
            body.Step(dt, dir, tuning.DogSpeedV);

            // 플레이어가 위에 있으면 뛴다. 착지 지점은 따지지 않는다 —
            // 못 올라가면 워프가 덮어준다.
            bool playerAbove = player.LastGroundedY - body.Y > tuning.DogJumpTrigU;

            // 아직 안 친하면 뛰기 전에 멈칫한다. 28m 서사 "발판 끝에서 망설이고 있다"가
            // 글만 뜨고 실제로는 망설이지 않던 것을 맞춘다.
            //
            // 땅에 붙어 있는 동안만 잰다. 공중에서도 흐르게 두면 한 번 뛴 뒤로는
            // 시간이 이미 차 있어서 연달아 뛰는 동안 다시 망설이지 않는다.
            if (!playerAbove || !body.grounded) aboveSince = -1f;
            else if (aboveSince < 0f) aboveSince = Time.time;
            bool waitedEnough = aboveSince >= 0f && Time.time - aboveSince >= HesitateSeconds;

            if (body.grounded && playerAbove && waitedEnough && Time.time - lastHop > HopInterval)
            {
                body.vy = tuning.DogJumpV;
                lastHop = Time.time;
                hopping = true; leftGround = false;
            }

            // 뛰어올랐다 제 발로 착지한 순간을 집어낸다 (서사 한 줄이 붙는다).
            if (hopping)
            {
                if (!body.grounded) leftGround = true;
                else if (leftGround)
                {
                    hopping = false; leftGround = false;
                    SelfLanded?.Invoke();
                }
            }
        }

        /// 같은 높이로 못 오는 상태가 잠깐 이어지면 플레이어 옆으로 옮겨간다.
        void WarpIfLeftBehind(CharacterBody p)
        {
            // 가지에 매달려 있는 동안은 쫓아가지 않는다. 그 높이엔 디딜 자리가 없어
            // 워프해도 곧바로 떨어진다 — 반복해서 워프하는 것만 보기 싫게 된다.
            if (player.Hanging) { farSince = -1f; return; }

            // 플레이어 점프 정점으로 판단하면 안 된다. 마지막으로 디딘 발판을 기준으로 본다.
            float dx = Mathf.Abs(p.X - body.X);
            float dy = Mathf.Abs(player.LastGroundedY - body.Y);
            bool far = dy > tuning.DogWarpHeightU || dx > tuning.DogWarpDistanceU;

            if (!far) { farSince = -1f; return; }
            if (farSince < 0f) farSince = Time.time;

            // 멈칫하는 시간이 워프 지연(0.35초)보다 길면 망설임이 화면에 안 보인다 —
            // 망설이는 동안 워프가 먼저 끼어들어 그냥 옮겨가 버린다.
            // 아직 안 친할 때는 제 발로 뛸 틈을 주고 나서 워프한다.
            float delay = Mathf.Max(tuning.dogWarpDelay, HesitateSeconds + HesitateGrace);
            if (Time.time - farSince < delay) return;
            if (Time.time - lastWarp < WarpCooldown) return;

            // 사라진 자리와 나타난 자리 양쪽에 먼지를 남긴다.
            // 아무 연출 없이 옮기면 툭 하고 생겨난 것처럼 보인다.
            var from = new Vector2(body.X, body.Y);
            float tx = p.X - Px.U(24f) * p.face;
            // 플레이어가 딛고 선 발판 안으로 들여놓는다.
            // 발판이 좁아지면서 옆자리가 허공인 경우가 생겼고, 그러면 워프하자마자
            // 떨어져서 다시 워프하기를 반복한다.
            if (p.grounded && p.groundIndex >= 0)
            {
                var plat = stage.GetPlatform(p.groundIndex);
                float inset = Px.U(EdgeInsetPx);
                float lo = plat.left + inset, hi = plat.right - inset;
                tx = lo <= hi ? Mathf.Clamp(tx, lo, hi) : (plat.left + plat.right) * 0.5f;
            }
            var to = new Vector2(tx, p.Y);
            // 점프 먼지보다는 크되 과하지 않게.
            puffs?.Burst(from, 9, 24f, 4.5f, 2f, sizePx: 7f, sizeVarPx: 5f, life: 0.5f);

            body.Teleport(to.x, to.y);
            body.face = p.face;

            puffs?.Burst(to, 11, 26f, 5f, 2.4f, sizePx: 8f, sizeVarPx: 6f, life: 0.5f);
            visual?.PopIn();
            if (lib != null)
                SpriteAnim.Spawn(new Vector3(to.x, to.y + 0.25f, 0f), lib.warpSparkle, 12f, 2.5f, 101);
            TeleportCount++;
            farSince = -1f;
            lastWarp = Time.time;
            // 워프로 옮겨온 것은 혼자 올라간 게 아니다
            hopping = false; leftGround = false;
        }
    }
}
