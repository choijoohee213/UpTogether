using UnityEngine;

namespace UpTogether.Tests
{
    /// 테스트용 도우미.
    static class TestUi
    {
        /// 특기 선택 카드는 timeScale 을 0 으로 만든다. WaitForFixedUpdate 로
        /// 진행하는 테스트는 그 순간 영원히 멈추므로, 게임플레이를 흉내내는
        /// 스위트에서는 카드를 끈다. (실제 플레이에서는 당연히 떠야 한다.)
        public static void SilencePerkCard()
        {
            var picker = Object.FindFirstObjectByType<PerkPicker>();
            if (picker == null) return;
            picker.enabled = false;
            if (picker.panel != null) picker.panel.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}
