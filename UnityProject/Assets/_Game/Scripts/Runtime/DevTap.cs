using UnityEngine;
using UnityEngine.EventSystems;

namespace UpTogether
{
    /// 개발용 스테이지 버튼. SelectTap·PauseTap 과 같은 이유로 onClick 람다를 쓰지 않는다
    /// (코드로 붙인 리스너는 씬 저장 때 사라진다).
    public class DevTap : MonoBehaviour, IPointerDownHandler
    {
        public DevPanel panel;
        public int stageIndex;

        public void OnPointerDown(PointerEventData e)
        {
            Sfx.I?.UiClick();
            if (panel != null) panel.Go(stageIndex);
        }
    }
}
