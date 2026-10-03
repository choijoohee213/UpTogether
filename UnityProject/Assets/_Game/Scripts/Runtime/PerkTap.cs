using UnityEngine;
using UnityEngine.EventSystems;

namespace UpTogether
{
    /// 특기 선택 칸. SelectTap 과 같은 이유로 onClick 람다를 쓰지 않는다 —
    /// 빌드 시점에 코드로 붙인 리스너는 씬 저장 때 사라져 버튼이 먹지 않는다.
    public class PerkTap : MonoBehaviour, IPointerDownHandler
    {
        public PerkPicker picker;
        public DogPerks.Kind kind;

        public void OnPointerDown(PointerEventData e)
        {
            Sfx.I?.UiClick();
            if (picker != null) picker.Pick(kind);
        }
    }
}
