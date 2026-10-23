using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 개발용 스테이지 선택: 일시정지에서 아무 스테이지로나 바로 간다.
    public class DevPanelTests
    {
        DevPanel dev;
        StageFlow flow;
        StageRunner stage;
        Pause pause;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            dev = Object.FindFirstObjectByType<DevPanel>();
            flow = Object.FindFirstObjectByType<StageFlow>();
            stage = Object.FindFirstObjectByType<StageRunner>();
            pause = Object.FindFirstObjectByType<Pause>();
        }

        [TearDown]
        public void Restore() => Time.timeScale = 1f;

        [Test]
        public void 배선이_직렬화됐다()
        {
            Assert.IsNotNull(dev, "DevPanel 이 씬에 없다");
            Assert.IsNotNull(dev.flow, "DevPanel.flow 가 비었다");
            Assert.IsNotNull(dev.row, "버튼 줄이 비었다");

            // 버튼은 일시정지 패널 안에 있어 평소엔 꺼져 있다 — 꺼진 것도 찾는다
            var taps = Object.FindObjectsByType<DevTap>(FindObjectsInactive.Include,
                                                        FindObjectsSortMode.None);
            Assert.AreEqual(flow.stages.Length, taps.Length, "버튼 수가 스테이지 수와 다르다");
            foreach (var t in taps)
                Assert.IsNotNull(t.panel, "버튼의 panel 참조가 비었다 (람다는 직렬화되지 않는다)");
        }

        [UnityTest]
        public IEnumerator 고른_스테이지로_바로_간다()
        {
            int last = flow.stages.Length - 1;
            Assert.AreNotSame(flow.stages[last], stage.Data, "이미 마지막 스테이지다");

            pause.Open();
            yield return null;
            dev.Go(last);
            yield return null;

            Assert.AreSame(flow.stages[last], stage.Data, "고른 스테이지로 안 갔다");
            Assert.AreEqual(last, flow.Index, "진행 번호가 안 맞는다");
            Assert.AreEqual(1f, Time.timeScale, "고른 뒤에도 멈춰 있다");
            Assert.IsFalse(pause.panel.activeSelf, "일시정지가 안 닫혔다");
        }

        [UnityTest]
        public IEnumerator 선택_뒤에도_이어서_진행된다()
        {
            dev.Go(1);
            yield return null;
            flow.Next();
            yield return null;
            Assert.AreSame(flow.stages[2], stage.Data, "선택 다음부터 이어지지 않는다");
        }
    }
}
