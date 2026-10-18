using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 스테이지 진행: 클리어 후 다음 스테이지로 바뀌는지.
    public class StageFlowTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 다음을_부르면_다음_스테이지가_로드된다()
        {
            var flow = Object.FindFirstObjectByType<StageFlow>();
            var runner = Object.FindFirstObjectByType<StageRunner>();
            Assert.IsNotNull(flow, "StageFlow 가 없다");
            Assert.GreaterOrEqual(flow.stages.Length, 2, "스테이지가 2개 이상이어야");
            Assert.AreEqual(3, flow.themes.Length, "테마 3종");

            var first = runner.Data;
            flow.Next();
            yield return null;

            Assert.AreNotSame(first, runner.Data, "스테이지가 안 바뀌었다");
            Assert.AreSame(flow.stages[1], runner.Data, "두 번째 스테이지여야 한다");
        }
    }
}
