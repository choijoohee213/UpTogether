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

        public bool Cleared { get; private set; }
        /// 지금 높이(m). UI 가 읽는다.
        public float Meters { get; private set; }
        /// 이번 판에서 올라간 최고 높이(m)
        public float PeakMeters { get; private set; }

        /// 클리어한 순간. (스테이지 이름)
        public event Action<string> OnCleared;

        bool wasClinging;

        void OnEnable()
        {
            if (player != null) player.Landed += HandleLanded;
        }

        void OnDisable()
        {
            if (player != null) player.Landed -= HandleLanded;
        }

        void Start()
        {
            narration?.Reset();
        }

        void FixedUpdate()
        {
            if (stage == null || stage.Data == null || player == null) return;

            Meters = stage.Data.Meters(player.Body.Y);
            if (Meters > PeakMeters) PeakMeters = Meters;

            // 새 높이 구간에 들어가면 한 줄 뜨고 친밀도가 오른다
            if (narration != null && narration.CheckHeight(Meters))
                bond?.Add(5f);

            // 강아지가 막 안긴 순간
            if (dog != null)
            {
                if (dog.IsClinging && !wasClinging && bond != null && bond.Value > 12f)
                    narration?.Speak(Narration.OnCling);
                wasClinging = dog.IsClinging;
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
            OnCleared?.Invoke(stage.Data.displayName);
        }

        /// 착지할 때 낙하 거리(m)를 받는다.
        void HandleLanded(float meters)
        {
            if (meters <= BigFallMeters) return;
            bond?.LoseByFall(meters);
            if (meters > FallLineMeters) narration?.Speak(Narration.OnBigFall);
        }
    }
}
