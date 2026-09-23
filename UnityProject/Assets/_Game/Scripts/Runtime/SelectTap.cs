using UnityEngine;
using UnityEngine.EventSystems;

namespace UpTogether
{
    /// 선택 칸/시작 버튼. Button.onClick 대신 이걸 쓴다.
    ///
    /// ★ 코드로 AddListener 한 람다는 씬 저장 때 사라진다 ★
    /// SceneBuilder 가 빌드 시점에 붙인 onClick 리스너는 직렬화되지 않아,
    /// 빌드된 씬에서는 버튼이 아무 반응도 하지 않았다.
    /// 여기서는 screen 참조와 index 만 들고 있어(둘 다 직렬화됨) 그 문제가 없다.
    /// 게임 조작 버튼과 같은 IPointerDown 방식이라 모바일에서도 확실히 먹는다.
    public class SelectTap : MonoBehaviour, IPointerDownHandler
    {
        public enum Kind { Character, Breed, Start }

        public SelectScreen screen;
        public Kind kind;
        public int index;

        public void OnPointerDown(PointerEventData e)
        {
            if (screen == null) return;
            switch (kind)
            {
                case Kind.Character: screen.PickCharacter(index); break;
                case Kind.Breed:     screen.PickBreed(index);     break;
                case Kind.Start:     screen.StartGame();          break;
            }
        }
    }
}
