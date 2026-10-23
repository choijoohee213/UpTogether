using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 얼음 발판: 눈 언덕에서만 나오고, 멈추기도 출발하기도 더디다.
    public class IceTests
    {
        StageRunner stage;
        PlayerController player;
        StageFlow flow;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            TestUi.DisableZoneFall();
            stage = Object.FindFirstObjectByType<StageRunner>();
            player = Object.FindFirstObjectByType<PlayerController>();
            flow = Object.FindFirstObjectByType<StageFlow>();
        }

        [TearDown]
        public void Restore() => Time.timeScale = 1f;

        [Test]
        public void 얼음은_눈_언덕에만_있다()
        {
            int snow = -1;
            for (int i = 0; i < flow.stages.Length; i++)
                if (flow.stages[i].displayName.Contains("눈")) snow = i;
            Assert.GreaterOrEqual(snow, 0, "눈 언덕을 못 찾았다");

            for (int i = 0; i < flow.stages.Length; i++)
            {
                int icy = 0;
                foreach (var p in flow.stages[i].platforms) if (p.icy) icy++;
                if (i == snow) Assert.Greater(icy, 0, "눈 언덕에 얼음이 없다");
                else Assert.AreEqual(0, icy, $"{flow.stages[i].displayName} 에 얼음이 있다");
            }
        }

        [Test]
        public void 바닥은_얼음이_아니다()
        {
            // 시작하자마자 미끄러지면 조작을 배울 데가 없다
            foreach (var st in flow.stages)
                if (st.platforms.Length > 0)
                    Assert.IsFalse(st.platforms[0].icy, $"{st.displayName} 의 바닥이 얼음이다");
        }

        /// 발판 하나를 얼음으로 바꿔 세우고, 입력을 끊은 뒤 얼마나 미끄러지는지 잰다.
        IEnumerator SlideDistance(bool icy, System.Action<float> result)
        {
            // 바닥 위에서 잰다 (평평하고 넓다)
            var ground = stage.GetPlatform(0);
            float startX = (ground.left + ground.right) * 0.5f - 1.5f;
            player.Body.Teleport(startX, ground.y);
            yield return new WaitForFixedUpdate();

            stage.SetIcy(0, icy);

            // 오른쪽으로 충분히 달린다
            for (int i = 0; i < 70; i++) player.Tick(Time.fixedDeltaTime, 1, false, false);
            float released = player.Body.X;
            float speed = player.Body.vx;
            Assert.Greater(speed, 0.1f, "달리지 못했다");

            // 손을 떼고 멈출 때까지
            for (int i = 0; i < 120; i++) player.Tick(Time.fixedDeltaTime, 0, false, false);
            result(player.Body.X - released);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 얼음에서는_손을_떼도_한참_미끄러진다()
        {
            float normal = 0f, ice = 0f;
            yield return SlideDistance(false, v => normal = v);
            yield return SlideDistance(true, v => ice = v);

            Assert.Greater(ice, normal * 2f,
                $"얼음이 안 미끄럽다 (평지 {normal:F2}u, 얼음 {ice:F2}u)");
        }

        [UnityTest]
        public IEnumerator 얼음에서는_출발도_더디다()
        {
            var ground = stage.GetPlatform(0);
            float cx = (ground.left + ground.right) * 0.5f;

            player.Body.Teleport(cx, ground.y);
            yield return new WaitForFixedUpdate();
            stage.SetIcy(0, false);
            for (int i = 0; i < 12; i++) player.Tick(Time.fixedDeltaTime, 1, false, false);
            float normal = player.Body.vx;

            player.Body.Teleport(cx, ground.y);
            player.Body.vx = 0f;
            yield return new WaitForFixedUpdate();
            stage.SetIcy(0, true);
            for (int i = 0; i < 12; i++) player.Tick(Time.fixedDeltaTime, 1, false, false);
            float ice = player.Body.vx;

            Assert.Less(ice, normal * 0.6f, $"얼음에서 너무 잘 출발한다 (평지 {normal:F2}, 얼음 {ice:F2})");
        }
    }
}
