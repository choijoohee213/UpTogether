using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 화면 위 '길': 체크포인트가 제자리에 찍히고, 오를수록 표시가 오른쪽으로 간다.
    public class RouteBarTests
    {
        RouteBar bar;
        Zones zones;
        StageRunner stage;
        PlayerController player;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            // 구간을 켜 둔다 — 이 시험이 보려는 게 '구간 진행' 이다.
            // (높은 곳에 붙잡아 두므로 구간 추락은 걸리지 않는다)
            bar = Object.FindFirstObjectByType<RouteBar>();
            zones = Object.FindFirstObjectByType<Zones>();
            stage = Object.FindFirstObjectByType<StageRunner>();
            player = Object.FindFirstObjectByType<PlayerController>();
        }

        [TearDown]
        public void Restore() => Time.timeScale = 1f;

        [Test]
        public void 배선이_직렬화됐다()
        {
            Assert.IsNotNull(bar, "RouteBar 가 씬에 없다");
            Assert.IsNotNull(bar.zones, "RouteBar.zones 가 비었다");
            Assert.IsNotNull(bar.rail, "길이 비었다");
            Assert.IsNotNull(bar.walker, "현위치 표시가 비었다");
            Assert.Greater(bar.dots.Length, 0, "체크포인트 점이 없다");
        }

        [UnityTest]
        public IEnumerator 체크포인트_개수만큼만_켜진다()
        {
            yield return null;
            int on = 0;
            foreach (var d in bar.dots) if (d.gameObject.activeSelf) on++;
            Assert.AreEqual(zones.ZoneCount, on, "켜진 점 수가 체크포인트 수와 다르다");
            Assert.Greater(zones.ZoneCount, 1, "체크포인트가 하나뿐이다");
        }

        [UnityTest]
        public IEnumerator 올라가면_표시가_오른쪽으로_간다()
        {
            yield return null;
            float before = bar.walker.anchoredPosition.x;

            // 스테이지 절반 높이에 '붙잡아' 둔다 — 그냥 올려두면 바로 떨어진다.
            // 표시는 부드럽게 따라가므로 프레임 수가 아니라 흐른 시간으로 기다린다.
            var d = stage.Data;
            float want = (d.groundY + d.height) * 0.5f;
            float t = 0f;
            while (t < 1.2f)
            {
                player.Body.Teleport(player.Body.X, want);
                player.Body.vy = 0f;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Greater(bar.walker.anchoredPosition.x, before + 20f,
                           "올라갔는데 표시가 안 움직였다");
        }

        [UnityTest]
        public IEnumerator 지나온_체크포인트와_남은_것이_구분된다()
        {
            yield return null;
            // 두 번째 체크포인트까지 올라간다
            Assert.Greater(zones.ZoneCount, 2, "체크포인트가 너무 적어 시험할 수 없다");
            var d = stage.Data;
            float want = d.groundY + (d.height - d.groundY) * zones.ZoneAt(2);
            for (int i = 0; i < 5; i++)
            {
                player.Body.Teleport(player.Body.X, want + 0.05f);
                player.Body.vy = 0f;
                yield return null;
            }

            Assert.GreaterOrEqual(zones.Zone, 1, "구간이 안 올라갔다");
            // 지나온 것은 또렷하게, 남은 것은 흐리게
            Assert.Greater(bar.dots[0].color.a, bar.dots[zones.ZoneCount - 1].color.a,
                           "지나온 체크포인트와 남은 것이 같아 보인다");
        }
    }
}
