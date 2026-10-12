using UnityEngine;
using UnityEngine.UI;

namespace UpTogether
{
    /// 친밀도가 문턱을 넘으면 플레이를 멈추고 특기 선택 카드를 띄운다.
    /// 선택 화면에서 미리 고르지 않는다 — 함께 오르다 친해진 만큼 열리는 쪽이
    /// 이 게임의 친밀도와 맞는다.
    [DefaultExecutionOrder(45)]
    public class PerkPicker : MonoBehaviour
    {
        public Bond bond;
        /// 일시정지와 동시에 뜨면 시간 복원이 꼬인다 — 열려 있으면 기다린다
        public Pause pause;
        public GameObject panel;        // 처음엔 꺼둔다
        public Text title;
        public PerkTap[] choices;       // 칸 세 개 (남은 특기 수에 맞춰 켠다)
        public Text[] choiceNames;
        public Text[] choiceDescs;

        bool open;

        void OnDisable()
        {
            if (open) { open = false; Time.timeScale = 1f; }   // 안전장치 (Pause 와 같은 이유)
        }

        void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        /// 이벤트 대신 매 프레임 본다 — 이어하기(이미 문턱을 넘은 상태)와
        /// 일시정지 중(열면 시간 복원이 꼬인다)을 한곳에서 처리할 수 있다.
        void Update()
        {
            if (open || bond == null) return;
            if (pause != null && pause.panel != null && pause.panel.activeSelf) return;
            if (!DogPerks.PickDue(bond.Value)) return;
            Open();
        }

        void Open()
        {
            if (panel == null || choices == null) return;
            var left = DogPerks.Remaining;
            if (left.Count == 0) return;

            open = true;
            if (title != null)
                title.text = DogPerks.Claimed == 0 ? "강아지가 재주를 하나 익혔다" : "강아지가 또 하나 익혔다";

            for (int i = 0; i < choices.Length; i++)
            {
                bool use = i < left.Count;
                if (choices[i] != null) choices[i].gameObject.SetActive(use);
                if (!use) continue;
                var info = DogPerks.Of(left[i]);
                choices[i].picker = this;
                choices[i].kind = left[i];
                if (i < choiceNames.Length && choiceNames[i] != null) choiceNames[i].text = info.name;
                if (i < choiceDescs.Length && choiceDescs[i] != null) choiceDescs[i].text = info.desc;
            }

            panel.SetActive(true);
            Time.timeScale = 0f;
            // 누르고 있던 조작을 비워, 재개했을 때 저절로 걷지 않게 한다 (Pause 와 같은 이유)
            if (GameInput.I != null)
            {
                GameInput.I.SetLeft(false);
                GameInput.I.SetRight(false);
                GameInput.I.SetJump(false);
            }
        }

        /// PerkTap 이 부른다
        public void Pick(DogPerks.Kind k)
        {
            DogPerks.Take(k);
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
            open = false;
            Sfx.I?.BarkBig();
        }
    }
}
