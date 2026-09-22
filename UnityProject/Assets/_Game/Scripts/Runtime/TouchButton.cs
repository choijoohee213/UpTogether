using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UpTogether
{
    /// 화면 조작 버튼. 손가락이 버튼 밖으로 미끄러져도 떼진 것으로 친다(원본 pointerleave와 동일).
    [RequireComponent(typeof(Image))]
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum Kind { Left, Right, Jump }
        public Kind kind;
        [Tooltip("눌렸을 때 살짝 진해지는 정도")]
        public float pressedAlpha = 0.6f;

        Image img;
        float idleAlpha;

        void Awake()
        {
            img = GetComponent<Image>();
            idleAlpha = img.color.a;
        }

        public void OnPointerDown(PointerEventData e) => Set(true);
        public void OnPointerUp(PointerEventData e) => Set(false);
        public void OnPointerExit(PointerEventData e) => Set(false);

        void OnDisable() => Set(false);

        void Set(bool on)
        {
            if (GameInput.I == null) return;
            switch (kind)
            {
                case Kind.Left:  GameInput.I.SetLeft(on);  break;
                case Kind.Right: GameInput.I.SetRight(on); break;
                case Kind.Jump:  GameInput.I.SetJump(on);  break;
            }
            var c = img.color;
            c.a = on ? pressedAlpha : idleAlpha;
            img.color = c;
        }
    }
}
