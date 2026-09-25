using UnityEngine;

namespace UpTogether
{
    /// 게임 이벤트를 효과음으로 옮긴다. 게임플레이 클래스는 건드리지 않고
    /// 이벤트(점프·착지·친밀도·클리어)만 듣는다. 강아지 안김과 발소리는 상태를 본다.
    [DefaultExecutionOrder(40)]
    public class GameAudio : MonoBehaviour
    {
        public PlayerController player;
        public Bond bond;
        public StageSession session;
        public DogController dog;

        const float StepInterval = 0.28f;
        const float WalkThresholdPx = 0.4f;

        float prevBond;
        string prevStage;
        bool ready;
        bool wasClinging;
        float stepTimer;

        void OnEnable()
        {
            if (player != null) { player.Jumped += OnJump; player.Landed += OnLanded; }
            if (bond != null) bond.Changed += OnBond;
            if (session != null) session.OnCleared += OnClear;
        }

        void OnDisable()
        {
            if (player != null) { player.Jumped -= OnJump; player.Landed -= OnLanded; }
            if (bond != null) bond.Changed -= OnBond;
            if (session != null) session.OnCleared -= OnClear;
        }

        void Start()
        {
            if (bond != null) { prevBond = bond.Value; prevStage = bond.StageName; }
            ready = true;   // 이 시점 전의 Changed(초기값)는 소리 없이 흘려보낸다
        }

        void Update()
        {
            if (Sfx.I == null || player == null) return;

            // 강아지가 막 안긴 순간
            if (dog != null)
            {
                if (dog.IsClinging && !wasClinging) Sfx.I.Catch();
                wasClinging = dog.IsClinging;
            }

            // 발소리 — 땅에서 걷는 동안 일정 간격
            if (player.Body.grounded && Mathf.Abs(player.Body.vx) > Px.V(WalkThresholdPx))
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) { Sfx.I.Step(); stepTimer = StepInterval; }
            }
            else stepTimer = 0f;
        }

        void OnJump() => Sfx.I?.Jump();

        void OnLanded(float meters)
        {
            if (Sfx.I == null) return;
            if (meters > 8f) Sfx.I.Whine();   // 크게 떨어짐 — 강아지가 걱정한다
            else Sfx.I.Land();
        }

        void OnBond(float value, string stage)
        {
            if (!ready) { prevBond = value; prevStage = stage; return; }
            if (Sfx.I != null)
            {
                if (stage != prevStage && value > prevBond) Sfx.I.Reward();       // 단계 상승
                else if (value > prevBond + 0.01f) Sfx.I.BondUp();
                else if (value < prevBond - 0.01f) Sfx.I.BondDown();
            }
            prevBond = value; prevStage = stage;
        }

        void OnClear(string name) => Sfx.I?.Clear();
    }
}
