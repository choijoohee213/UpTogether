using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 떨어지는 돌: 돌 언덕에서만 나오고, 밑을 지나면 흔들리다 떨어진다.
    public class RockTests
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
        public void 돌은_돌_언덕에만_있다()
        {
            int stone = -1;
            for (int i = 0; i < flow.stages.Length; i++)
                if (flow.stages[i].displayName.Contains("돌")) stone = i;
            Assert.GreaterOrEqual(stone, 0, "돌 언덕을 못 찾았다");

            for (int i = 0; i < flow.stages.Length; i++)
            {
                int n = flow.stages[i].rocks != null ? flow.stages[i].rocks.Length : 0;
                if (i == stone) Assert.Greater(n, 0, "돌 언덕에 돌이 없다");
                else Assert.AreEqual(0, n, $"{flow.stages[i].displayName} 에 돌이 있다");
            }
        }

        [Test]
        public void 돌은_천장에_매달려_있고_아래가_비었다()
        {
            foreach (var st in flow.stages)
            {
                if (st.rocks == null) continue;
                foreach (var rk in st.rocks)
                {
                    bool ceiling = false, blocked = false;
                    foreach (var p in st.platforms)
                    {
                        if (rk.x <= p.x || rk.x >= p.Right) continue;
                        if (Mathf.Abs(p.y - (rk.y + 0.18f)) < 0.05f) ceiling = true;
                        if (p.y < rk.y && p.y > rk.y - 1.0f) blocked = true;
                    }
                    Assert.IsTrue(ceiling, $"돌({rk.x:F2},{rk.y:F2}) 위에 천장이 없다");
                    Assert.IsFalse(blocked, $"돌({rk.x:F2},{rk.y:F2}) 아래가 막혔다");
                }
            }
        }

        /// 돌 언덕을 띄운다
        IEnumerator LoadStoneStage()
        {
            for (int i = 0; i < flow.stages.Length; i++)
            {
                if (stage.Data != null && stage.Data.displayName.Contains("돌")) yield break;
                flow.Next();
                yield return null;
            }
            Assert.IsTrue(stage.Data.displayName.Contains("돌"), "돌 언덕을 못 띄웠다");
        }

        [UnityTest]
        public IEnumerator 밑을_지나면_흔들리다_떨어진다()
        {
            yield return LoadStoneStage();
            Assert.Greater(stage.RockCount, 0, "돌이 없다");

            var rk = stage.Data.rocks[0];
            float startY = stage.RockPos(0).y;
            Assert.IsFalse(stage.RockFalling(0), "처음부터 떨어지고 있다");

            // 돌 바로 밑으로 간다
            player.Body.Teleport(rk.x, rk.y - 1.0f);
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(stage.RockFalling(0), "경고 없이 바로 떨어졌다");

            // 흔들림이 끝나면 떨어진다
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(stage.RockFalling(0), "흔들린 뒤에도 안 떨어진다");

            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.Less(stage.RockPos(0).y, startY - 0.1f, "떨어지는데 높이가 그대로다");
        }

        [UnityTest]
        public IEnumerator 떨어지는_돌에_맞으면_아프다()
        {
            yield return LoadStoneStage();
            var hazards = Object.FindFirstObjectByType<Hazards>();
            var rk = stage.Data.rocks[0];

            bool hurt = false;
            System.Action onHurt = () => hurt = true;
            hazards.Hurt += onHurt;

            // 밑에서 기다렸다가 떨어지는 돌을 맞는다
            player.Body.Teleport(rk.x, rk.y - 1.0f);
            for (int i = 0; i < 120 && !hurt; i++)
            {
                player.Body.Teleport(rk.x, stage.RockPos(0).y - 0.3f);   // 돌 바로 아래 붙어 있기
                yield return new WaitForFixedUpdate();
            }
            hazards.Hurt -= onHurt;

            Assert.IsTrue(hurt, "떨어지는 돌에 맞았는데 아무 일도 없다");
        }
    }
}
