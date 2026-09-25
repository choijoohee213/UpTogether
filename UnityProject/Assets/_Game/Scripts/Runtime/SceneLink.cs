using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace UpTogether
{
    /// 다른 씬으로 넘어가는 버튼.
    /// SelectTap 과 같은 이유로 Button.onClick 대신 직렬화되는 IPointerDown 을 쓴다 —
    /// 코드로 붙인 onClick 리스너는 씬 저장 때 사라져 빌드된 씬에서 안 먹는다.
    public class SceneLink : MonoBehaviour, IPointerDownHandler
    {
        public string scene;

        public void OnPointerDown(PointerEventData e)
        {
            Sfx.I?.UiClick();
            if (!string.IsNullOrEmpty(scene)) SceneManager.LoadScene(scene);
        }
    }
}
