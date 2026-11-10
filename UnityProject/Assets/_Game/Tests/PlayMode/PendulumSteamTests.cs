using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 흔들리는 추와 뿜는 증기: 스테이지별로 데뷔하고, 제대로 움직이고 아프다.
    public class PendulumSteamTests
    {
        StageRunner stage;
        PlayerController player;
        StageFlow flow;
        Hazards hazards;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            TestUi.DisableZoneFall();
            stage = Object.FindFirstObjectByType<StageRunner>();
            player = Object.FindFirstObjectByType<PlayerController>();
            flow = Object.FindFirstObjectByType<StageFlow>();
            hazards = Object.FindFirstObjectByType<Hazards>();
        }

        [TearDown]
        public void Restore() => Time.timeScale = 1f;

        [Test]
        public void 첫_스테이지에는_아직_안_나온다()
        {
            // 기본기를 배우는 곳이다 — 새 장애물은 뒤에서 하나씩 나온다
            var first = flow.stages[0];
            Assert.AreEqual(0, first.pendulums.Length, "첫 언덕에 추가 있다");
            Assert.AreEqual(0, first.steams.Length, "첫 언덕에 증기가 있다");
        }

        [Test]
        public void 추와_증기가_데뷔하는_스테이지가_있다()
        {
            int pend = 0, steam = 0;
            foreach (var st in flow.stages)
            {
                if (st.pendulums.Length > 0) pend++;
                if (st.steams.Length > 0) steam++;
            }
            Assert.Greater(pend, 0, "추가 어느 스테이지에도 없다");
            Assert.Greater(steam, 0, "증기가 어느 스테이지에도 없다");
        }

        [Test]
        public void 추가_흔들리는_길에는_발판이_없다()
        {
            foreach (var st in flow.stages)
                foreach (var pd in st.pendulums)
                {
                    float reach = Mathf.Sin(pd.swing) * pd.length + StageRunner.PendulumRadius;
                    float low = pd.y - pd.length - StageRunner.PendulumRadius;
                    foreach (var p in st.platforms)
                    {
                        bool overlapX = pd.x + reach > p.x && pd.x - reach < p.Right;
                        bool inArc = p.y < pd.y - 0.15f && p.y > low + 0.05f;
                        Assert.IsFalse(overlapX && inArc,
                            $"{st.displayName}: 추({pd.x:F2},{pd.y:F2}) 가 발판을 쓸고 지나간다");
                    }
                }
        }

        /// 그 장애물이 있는 스테이지를 띄운다
        IEnumerator LoadStageWith(System.Func<StageData, bool> has)
        {
            for (int i = 0; i < flow.stages.Length; i++)
            {
                if (stage.Data != null && has(stage.Data)) yield break;
                flow.Next();
                yield return null;
            }
            Assert.IsTrue(has(stage.Data), "그 장애물이 있는 스테이지를 못 띄웠다");
        }

        [UnityTest]
        public IEnumerator 추는_좌우로_흔들린다()
        {
            yield return LoadStageWith(d => d.pendulums.Length > 0);
            Assert.Greater(stage.PendulumCount, 0);

            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < 180; i++)
            {
                var p = stage.PendulumPos(0);
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(maxX - minX, 0.4f, $"추가 거의 안 움직인다 ({maxX - minX:F2}u)");
        }

        [UnityTest]
        public IEnumerator 추에_닿으면_아프다()
        {
            yield return LoadStageWith(d => d.pendulums.Length > 0);
            bool hurt = false;
            System.Action onHurt = () => hurt = true;
            hazards.Hurt += onHurt;

            for (int i = 0; i < 90 && !hurt; i++)
            {
                var p = stage.PendulumPos(0);
                player.Body.Teleport(p.x, p.y - 0.28f);   // 추에 몸을 붙인다
                yield return new WaitForFixedUpdate();
            }
            hazards.Hurt -= onHurt;
            Assert.IsTrue(hurt, "추에 닿았는데 아무 일도 없다");
        }

        [UnityTest]
        public IEnumerator 증기는_껐다_켰다_하고_뿜을_때만_아프다()
        {
            yield return LoadStageWith(d => d.steams.Length > 0);
            Assert.Greater(stage.SteamCount, 0);

            bool sawOn = false, sawOff = false;
            for (int i = 0; i < 300 && !(sawOn && sawOff); i++)
            {
                if (stage.SteamOn(0)) sawOn = true; else sawOff = true;
                yield return new WaitForFixedUpdate();
            }
            Assert.IsTrue(sawOn && sawOff, "증기가 계속 켜져 있거나 꺼져 있다");

            // 꺼져 있는 동안 그 자리에 서 있어도 안전하다
            var st = stage.Data.steams[0];
            bool hurt = false;
            System.Action onHurt = () => hurt = true;
            hazards.Hurt += onHurt;
            for (int i = 0; i < 200 && !hurt; i++)
            {
                player.Body.Teleport(st.x + st.width * 0.5f, st.y + 0.2f);
                yield return new WaitForFixedUpdate();
            }
            hazards.Hurt -= onHurt;
            Assert.IsTrue(hurt, "뿜는 증기 위에 서 있었는데 아무 일도 없다");
        }
    }
}
