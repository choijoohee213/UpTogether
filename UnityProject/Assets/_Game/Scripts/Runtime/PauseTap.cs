using UnityEngine;
using UnityEngine.EventSystems;

namespace UpTogether
{
    /// 일시정지 관련 버튼. SelectTap 과 같은 이유로 직렬화되는 IPointerDown 을 쓴다
    /// (코드로 붙인 onClick 리스너는 씬 저장 때 사라진다).
    public class PauseTap : MonoBehaviour, IPointerDownHandler
    {
        public enum Kind { Open, Resume, Home }
        public Pause pause;
        public Kind kind;

        public void OnPointerDown(PointerEventData e)
        {
            if (pause == null) return;
            switch (kind)
            {
                case Kind.Open: pause.Open(); break;
                case Kind.Resume: pause.Resume(); break;
                case Kind.Home: pause.Home(); break;
            }
        }
    }
}
