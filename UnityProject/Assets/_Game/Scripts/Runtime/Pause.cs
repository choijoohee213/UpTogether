using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpTogether
{
    /// 일시정지. 버튼을 누르면 시간을 멈추고 패널을 띄운다.
    /// ★ timeScale 은 씬을 넘어가도 유지된다 ★ — 메인으로 갈 때 반드시 1 로 돌려놓는다.
    public class Pause : MonoBehaviour
    {
        public GameObject panel;            // 일시정지 오버레이 (처음엔 꺼둠)
        public string homeScene = "Title";

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
            Time.timeScale = 0f;
            // 누르고 있던 조작을 비워, 재개했을 때 저절로 걷지 않게 한다.
            if (GameInput.I != null)
            {
                GameInput.I.SetLeft(false);
                GameInput.I.SetRight(false);
                GameInput.I.SetJump(false);
            }
        }

        public void Resume()
        {
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
        }

        public void Home()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(homeScene);
        }

        void OnDisable() => Time.timeScale = 1f;   // 안전장치
    }
}
