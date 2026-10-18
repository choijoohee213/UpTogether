using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 일시정지: 시간이 멈추고 패널이 뜨는지, 버튼 배선이 직렬화됐는지.
    public class PauseTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void Restore() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator 일시정지하면_시간이_멈추고_재개하면_돌아온다()
        {
            var pause = Object.FindFirstObjectByType<Pause>();
            Assert.IsNotNull(pause, "Pause 컴포넌트가 없다");
            Assert.IsNotNull(pause.panel, "Pause.panel 이 비었다");
            Assert.IsFalse(pause.panel.activeSelf, "패널이 처음부터 켜져 있으면 안 된다");

            pause.Open();
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "일시정지인데 시간이 안 멈췄다");
            Assert.IsTrue(pause.panel.activeSelf, "패널이 안 떴다");

            pause.Resume();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "재개했는데 시간이 안 돌아왔다");
            Assert.IsFalse(pause.panel.activeSelf, "재개했는데 패널이 안 닫혔다");
        }

        [Test]
        public void 일시정지_버튼_셋이_pause_를_참조한다()
        {
            // 패널이 꺼져 있어도 참조는 살아 있어야 한다 → 비활성 포함으로 찾는다
            var taps = Object.FindObjectsByType<PauseTap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(3, taps.Length, "열기 / 계속하기 / 메인으로 3개여야 한다");
            foreach (var t in taps)
                Assert.IsNotNull(t.pause, $"{t.kind} 버튼이 pause 참조를 잃었다 (직렬화 실패)");
        }
    }
}
