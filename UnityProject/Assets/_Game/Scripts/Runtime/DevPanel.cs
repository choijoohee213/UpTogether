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
        public Bond bond;
        public Narration narration;
        public GameObject row;        // 스테이지 버튼 줄
        public GameObject bondRow;    // 친밀도 버튼 줄
        public bool shown = true;

        void Start()
        {
            if (row != null) row.SetActive(shown);
            if (bondRow != null) bondRow.SetActive(shown);
        }

        /// DevTap 이 부른다
        public void Go(int stageIndex)
        {
            if (flow == null) return;
            flow.Go(stageIndex);
            if (pause != null) pause.Resume();   // 고르면 바로 플레이로
        }

        /// BondTap 이 부른다. 친밀도를 바로 세팅한다 —
        /// 6단계 × 5스테이지를 플레이해서 올라갈 수는 없으므로 테스트에 필수다.
        ///
        /// 본 대사 기록도 같이 지운다. 사건 대사는 최초 1회만 뜨도록 저장되는데,
        /// 한 번 돌린 뒤로는 다시 확인할 방법이 없어진다. 구간을 골라 들어가는 것은
        /// 곧 그 구간 대사를 보려는 것이니 같이 초기화하는 쪽이 맞다.
        public void SetBond(float value)
        {
            if (bond != null) bond.Set(value);
            if (narration != null) narration.ForgetAll();
            if (pause != null) pause.Resume();
        }
    }
}
