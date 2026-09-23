using UnityEngine;
using UnityEngine.UI;

namespace UpTogether
{
    /// 화면 위에 얹는 것들 — 친밀도, 높이, 서사 한 줄, 클리어 화면.
    /// 만드는 건 SceneBuilder 가 하고, 여기서는 값만 받아 갱신한다.
    public class GameHud : MonoBehaviour
    {
        const float SayDuration = 3.2f;
        const float SayFade = 0.6f;

        public StageSession session;
        public Bond bond;
        public Narration narration;

        [Header("연결")]
        public Image bondFill;
        public Text bondLabel;
        public Text heightLabel;
        public CanvasGroup sayGroup;
        public Text sayText;
        public CanvasGroup clearGroup;
        public Text clearText;

        float sayUntil = -1f;

        void OnEnable()
        {
            if (bond != null) bond.Changed += OnBond;
            if (narration != null) narration.Say += OnSay;
            if (session != null) session.OnCleared += OnClear;
        }

        void OnDisable()
        {
            if (bond != null) bond.Changed -= OnBond;
            if (narration != null) narration.Say -= OnSay;
            if (session != null) session.OnCleared -= OnClear;
        }

        void Start()
        {
            if (sayGroup != null) sayGroup.alpha = 0f;
            if (clearGroup != null) { clearGroup.alpha = 0f; clearGroup.blocksRaycasts = false; }
            if (bond != null) OnBond(bond.Value, bond.StageName);
        }

        void Update()
        {
            if (session != null && heightLabel != null)
                heightLabel.text = $"{Mathf.FloorToInt(Mathf.Max(0f, session.Meters))}m";

            if (sayGroup == null) return;
            float left = sayUntil - Time.time;
            sayGroup.alpha = left <= 0f ? 0f : Mathf.Clamp01(left / SayFade);
        }

        void OnBond(float value, string stageName)
        {
            if (bondFill != null) bondFill.fillAmount = value / 100f;
            if (bondLabel != null) bondLabel.text = stageName;
        }

        void OnSay(string text)
        {
            if (sayText == null) return;
            sayText.text = text;
            sayUntil = Time.time + SayDuration;
        }

        void OnClear(string stageName)
        {
            if (clearGroup == null) return;
            if (clearText != null)
                clearText.text = $"{stageName} 완주!\n강아지가 꼬리를 흔들며 옆에 앉았어요.\n\n마음의 거리 — {bond?.StageName}";
            clearGroup.alpha = 1f;
            clearGroup.blocksRaycasts = true;
        }
    }
}
