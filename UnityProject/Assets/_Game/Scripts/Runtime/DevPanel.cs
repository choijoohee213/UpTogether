using UnityEngine;

namespace UpTogether
{
    /// 개발용 스테이지 선택. 일시정지 화면에서 아무 스테이지로나 바로 간다.
    /// 테스트할 때 세 판을 깨고 올라갈 수는 없으니 필요하다.
    /// shown 을 끄면 패널째로 사라진다 (출시 때).
    public class DevPanel : MonoBehaviour
    {
        public StageFlow flow;
        public Pause pause;
        public GameObject row;        // 버튼들이 담긴 줄
        public bool shown = true;

        void Start() { if (row != null) row.SetActive(shown); }

        /// DevTap 이 부른다
        public void Go(int stageIndex)
        {
            if (flow == null) return;
            flow.Go(stageIndex);
            if (pause != null) pause.Resume();   // 고르면 바로 플레이로
        }
    }
}
