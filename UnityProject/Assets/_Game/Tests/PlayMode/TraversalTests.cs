using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 새 이동 수단: 상승 기류로 떠오르고, 가지에 매달려 건너고, 통나무에 맞는다.
    public class TraversalTests
    {
        StageRunner stage;
        PlayerController player;
        Bond bond;

        [UnitySetUp]
        public IEnumerator Load()
        {
            DogPerks.Clear();
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            TestUi.SilencePerkCard();
            TestUi.DisableZoneFall();     // 띄우고 떨어뜨리는 시험이라 구간 되돌림을 끈다
            stage = Object.FindFirstObjectByType<StageRunner>();
            player = Object.FindFirstObjectByType<PlayerController>();
            bond = Object.FindFirstObjectByType<Bond>();
            bond.Add(50f - bond.Value);
        }

        [TearDown]
        public void Restore() { Time.timeScale = 1f; DogPerks.Clear(); }

        [Test]
        public void 세_종류가_모두_배치됐다()
        {
            var d = stage.Data;
            Assert.Greater(d.updrafts.Length, 0, "상승 기류");
            Assert.Greater(d.bars.Length, 0, "매달리는 가지");
            Assert.Greater(d.rollers.Length, 0, "구르는 통나무");
        }

        [Test]
        public void 상승_기류는_발판_위에_겹쳐_있지_않다()
        {
            // 발판 위에 세우면 걸어다니다 저절로 떠올라 조작을 뺏는다
            foreach (var u in stage.Data.updrafts)
                foreach (var p in stage.Data.platforms)
                {
                    bool overlapX = u.x + u.width > p.x && u.x < p.Right;
                    bool insideY = p.y > u.y - 0.1f && p.y < u.y + u.height;
                    Assert.IsFalse(overlapX && insideY,
                        $"기류({u.x:F2},{u.y:F2})가 발판({p.x:F2},{p.y:F2}) 위에 걸쳤다");
                }
        }

        [UnityTest]
        public IEnumerator 상승_기류_안에서는_떠오른다()
        {
            var u = stage.Data.updrafts[0];
            float x = u.x + u.width * 0.5f, y = u.y + 0.2f;
            player.Body.Teleport(x, y);
            player.Body.vy = 0f;

            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            Assert.Greater(player.Body.Y, y + 0.2f, "기류 안인데 떠오르지 않았다");
        }

        [UnityTest]
        public IEnumerator 기류_밖에서는_떠오르지_않는다()
        {
            var u = stage.Data.updrafts[0];
            // 기류 옆, 같은 높이
            float x = u.x - 1.5f, y = u.y + 0.2f;
            player.Body.Teleport(x, y);
            player.Body.vy = 0f;

            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            Assert.LessOrEqual(player.Body.Y, y + 0.05f, "기류 밖인데 떠올랐다");
        }

        [UnityTest]
        public IEnumerator 가지에_닿으면_매달리고_점프로_놓는다()
        {
            var b = stage.Data.bars[0];
            // 머리가 가지에 닿는 높이로 떨어뜨린다
            player.Body.Teleport(b.x + b.width * 0.5f, b.y - 0.5f);
            player.Body.vy = -0.1f;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(player.Hanging, "가지에 닿았는데 매달리지 않았다");

            float heldY = player.Body.Y;
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(heldY, player.Body.Y, 0.05f, "매달린 채로 흘러내렸다");

            // 점프로 놓는다
            player.Tick(Time.fixedDeltaTime, 0, true, true);
            Assert.IsFalse(player.Hanging, "점프했는데 안 놓았다");
            Assert.Greater(player.Body.vy, 0f, "놓으면서 위로 튀지 않았다");
        }

        [UnityTest]
        public IEnumerator 매달린_채로_좌우로_건넌다()
        {
            var b = stage.Data.bars[0];
            player.Body.Teleport(b.x + 0.1f, b.y - 0.5f);
            player.Body.vy = -0.1f;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(player.Hanging, "먼저 매달려야 한다");

            float x0 = player.Body.X;
            for (int i = 0; i < 30; i++) player.Tick(Time.fixedDeltaTime, 1, false, false);

            Assert.Greater(player.Body.X, x0 + 0.05f, "매달린 채 오른쪽으로 안 갔다");
            Assert.LessOrEqual(player.Body.X, b.Right + 0.01f, "가지 끝을 넘어갔다");
        }

        [UnityTest]
        public IEnumerator 구르는_통나무는_좌우로_움직이고_닿으면_아프다()
        {
            Assert.Greater(stage.RollerCount, 0);
            var p0 = stage.RollerPos(0);
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            var p1 = stage.RollerPos(0);
            Assert.AreNotEqual(p0.x, p1.x, "통나무가 제자리다");

            // 주변 간식·링이 친밀도를 올려 덮으므로 찔림 이벤트를 직접 듣는다
            var hazards = Object.FindFirstObjectByType<Hazards>();
            bool hurt = false;
            System.Action onHurt = () => hurt = true;
            hazards.Hurt += onHurt;

            player.Body.Teleport(p1.x, p1.y - 0.28f);   // 통나무 한가운데로
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            hazards.Hurt -= onHurt;

            Assert.IsTrue(hurt, "통나무에 닿았는데 아무 일도 없다");
        }
    }
}
