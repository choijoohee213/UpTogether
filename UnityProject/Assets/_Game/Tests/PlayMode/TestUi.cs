using UnityEngine;

namespace UpTogether.Tests
{
    /// 테스트용 도우미.
    static class TestUi
    {
        /// 특기 선택 카드는 timeScale 을 0 으로 만든다. WaitForFixedUpdate 로
        /// 진행하는 테스트는 그 순간 영원히 멈추므로, 게임플레이를 흉내내는
        /// 스위트에서는 카드를 끈다. (실제 플레이에서는 당연히 떠야 한다.)
        /// 구간 추락은 큰 낙하를 가로채 구간 처음으로 되돌린다.
        /// 낙하 자체를 보는 옛 테스트들은 그 전에 쓰였으므로 끈다.
        public static void DisableZoneFall()
        {
            var zones = Object.FindFirstObjectByType<Zones>();
            if (zones != null) zones.enabled = false;
        }

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
