using UnityEngine;

namespace UpTogether.Tests
{
    /// 테스트용 도우미.
    static class TestUi
    {
        /// 구간 추락은 큰 낙하를 가로채 구간 처음으로 되돌린다.
        /// 낙하 자체를 보는 테스트들은 그 전에 쓰였으므로 끈다.
        public static void DisableZoneFall()
        {
            var zones = Object.FindFirstObjectByType<Zones>();
            if (zones != null) zones.enabled = false;
        }
    }
}
