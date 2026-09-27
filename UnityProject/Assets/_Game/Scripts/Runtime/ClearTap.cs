using UnityEngine;
using UnityEngine.EventSystems;

namespace UpTogether
{
    /// 클리어 화면을 톡 누르면 다음 스테이지로. (SelectTap 과 같은 직렬화 IPointerDown)
    public class ClearTap : MonoBehaviour, IPointerDownHandler
    {
        public StageFlow flow;
        public void OnPointerDown(PointerEventData e) { if (flow != null) flow.Next(); }
    }
}
