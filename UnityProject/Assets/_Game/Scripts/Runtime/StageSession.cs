using System;
using UnityEngine;

namespace UpTogether
{
    /// 한 판의 흐름. 높이를 재고, 서사와 친밀도를 굴리고, 깃발에 닿으면 클리어.
    /// 지금까지 물리와 연출만 있고 "게임"이 없었다. 그 자리를 채우는 것이다.
    [DefaultExecutionOrder(20)]   // 플레이어(0)가 움직인 뒤에 판정한다
    public class StageSession : MonoBehaviour
    {
        /// 깃발 판정 범위 (원본 프로토타입과 같은 값)
        const float GoalRangeXPx = 40f;
        const float GoalRangeYPx = 52f;
        /// 이보다 크게 떨어지면 친밀도가 깎인다
        const float BigFallMeters = 8f;
        /// 이보다 크게 떨어지면 한 줄 뜬다
        const float FallLineMeters = 26f;

        public PlayerController player;
        public DogController dog;
        public StageRunner stage;
        public Bond bond;
        public Narration narration;
        /// 몇 번째 스테이지인지 알아야 오프닝·엔딩 줄을 고를 수 있다
        public StageFlow flow;
        /// 찔린 순간에 플래시백 한 줄이 붙는다
        public Hazards hazards;
        /// 컷신. 프롤로그는 전용 씬(Prologue)으로 옮겼고, 이 자리는
        /// 마지막 판 엔딩("강아지가 먼저 들어갔다")에 쓸 것이다.
        public Cinematic cinematic;

        public bool Cleared { get; private set; }
        /// 지금 높이(m). UI 가 읽는다.
        public float Meters { get; private set; }
        /// 이번 판에서 올라간 최고 높이(m)
        public float PeakMeters { get; private set; }

        /// 클리어한 순간. (스테이지 이름)
        public event Action<string> OnCleared;

        bool wasClinging;
        bool wasFlashback;

        void OnEnable()
        {
            if (player != null) player.Landed += HandleLanded;
            if (bond != null) bond.StageUp += HandleBondStageUp;
            if (dog != null) dog.SelfLanded += HandleDogSelfLanded;
            if (hazards != null) hazards.Hurt += HandleHurt;
        }

        void OnDisable()
        {
            if (player != null) player.Landed -= HandleLanded;
            if (bond != null) bond.StageUp -= HandleBondStageUp;
            if (dog != null) dog.SelfLanded -= HandleDogSelfLanded;
            if (hazards != null) hazards.Hurt -= HandleHurt;
        }

        void Start()
        {
            narration?.Reset();
            SpeakStageOpen();
        }

        /// 다음 스테이지로 넘어갈 때 판정을 초기화한다 (친밀도는 이어진다).
        public void ResetForNewStage()
        {
            Cleared = false;
            Meters = 0f;
            PeakMeters = 0f;
            wasClinging = false;
            wasFlashback = false;
            narration?.Reset();
            SpeakStageOpen();
        }

        /// 몇 번째 스테이지인가 (0부터). StageFlow 가 없으면 첫 판으로 본다.
        int StageIndex => flow != null ? flow.Index : 0;

        /// 스테이지 오프닝 한 줄. 최초 1회만 — 친밀도는 누적되는데 스테이지는
        /// 다시 고를 수 있어서, 두 번째 방문에 "세 발판쯤 뒤에서 멈춰 섰다"가
        /// 뜨면 딱 붙어 따라오는 강아지와 어긋난다.
        void SpeakStageOpen()
        {
            int i = StageIndex;
            if (narration == null || i < 0 || i >= Narration.StageOpen.Length) return;
            narration.SpeakOnce("open" + i, Narration.StageOpen[i]);
        }

        void FixedUpdate()
        {
            if (stage == null || stage.Data == null || player == null) return;

            Meters = stage.Data.Meters(player.Body.Y);
            if (Meters > PeakMeters) PeakMeters = Meters;

            // 새 높이 구간에 들어가면 한 줄 뜨고 친밀도가 오른다
            if (narration != null && narration.CheckHeight(Meters))
                bond?.Add(5f);

            // 강아지가 막 안긴 순간. 처음 안긴 날은 따로 기념한다.
            // 다만 밧줄·가지에서 안기는 것은 친밀도와 무관하게 일어나므로
            // (제 발로 갈 수 없는 길이라) 그건 "처음 안겼다"로 치지 않는다.
            if (dog != null)
            {
                if (dog.IsClinging && !wasClinging && dog.ClingUnlocked && narration != null)
                {
                    if (!narration.SpeakOnce("firstcling", Narration.OnFirstCling))
                        narration.Speak(Narration.OnCling);
                }
                wasClinging = dog.IsClinging;

                // 움츠렸다가 돌아온 순간
                if (!dog.InFlashback && wasFlashback)
                    narration?.SpeakOnce("flashbackover", Narration.OnFlashbackOver);
                wasFlashback = dog.InFlashback;
            }

            CheckGoal();
        }

        void CheckGoal()
        {
            if (Cleared) return;
            var goal = stage.Data.goal;
            if (Mathf.Abs(player.Body.X - goal.x) > Px.U(GoalRangeXPx)) return;
            if (Mathf.Abs(player.Body.Y - goal.y) > Px.U(GoalRangeYPx)) return;

            Cleared = true;
            bond?.Add(10f);
            int i = StageIndex;
            if (narration != null && i >= 0 && i < Narration.StageClose.Length)
                narration.SpeakOnce("close" + i, Narration.StageClose[i]);
            OnCleared?.Invoke(stage.Data.displayName);
        }

        /// 착지할 때 낙하 거리(m)를 받는다.
        void HandleLanded(float meters)
        {
            if (meters <= BigFallMeters) return;
            bond?.LoseByFall(meters);
            dog?.Flashback();                       // 크게 떨어지면 한동안 움츠러든다
            if (meters <= FallLineMeters) return;

            // 안기기가 아직 안 열렸으면 강아지는 오지 않았다. 그걸 그대로 말한다.
            // 화자가 먼저 괜찮다고 해줘야 플레이어가 강아지에게 실망하지 않는다.
            if (dog != null && !dog.ClingUnlocked)
                narration?.SpeakOnce("fallalone", Narration.OnFallAlone);
            else
                narration?.Speak(Narration.OnBigFall);
        }

        /// 친밀도 단계가 올라간 순간
        void HandleBondStageUp(int stageIndex)
        {
            if (narration == null || stageIndex < 0 || stageIndex >= Narration.BondUp.Length) return;
            narration.SpeakOnce("bond" + stageIndex, Narration.BondUp[stageIndex]);
        }

        /// 강아지가 제 발로 뛰어 올라가 착지했다
        void HandleDogSelfLanded()
        {
            narration?.SpeakOnce("selfjump", Narration.OnFirstSelfJump);
        }

        /// 찔렸다. 강아지가 움츠러든다.
        /// 낙하 쪽은 자기 대사가 따로 있어서 여기서만 플래시백 줄을 띄운다 —
        /// 둘 다 띄우면 한 줄이 다른 줄을 덮어쓴다 (GameHud 는 한 줄만 보여준다).
        void HandleHurt()
        {
            dog?.Flashback();
            narration?.SpeakOnce("flashback", Narration.OnFlashback);
        }
    }
}
