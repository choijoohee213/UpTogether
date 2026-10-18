using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 장애물이 실제로 동작하는지 — 튕김판/사라지는 발판/바람.
    public class ObstacleTests
    {
        StageRunner stage;
        PlayerController player;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            stage = Object.FindFirstObjectByType<StageRunner>();
            player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(stage); Assert.IsNotNull(player);
        }

        int FindKind(StageRunner.Kind k)
        {
            for (int i = 0; i < stage.PlatformCount; i++)
                if (stage.GetPlatform(i).kind == k) return i;
            return -1;
        }

        [Test]
        public void 스테이지에_네_장애물이_모두_있다()
        {
            var d = stage.Data;
            Assert.Greater(d.vanishers.Length, 0, "사라지는 발판");
            Assert.Greater(d.spikes.Length, 0, "가시");
            Assert.Greater(d.winds.Length, 0, "바람");
            Assert.Greater(d.bouncers.Length, 0, "튕김판");
            Assert.IsNotNull(Object.FindFirstObjectByType<Hazards>(), "Hazards 컴포넌트가 없다");
        }

        [Test]
        public void 튕김판을_밟으면_점프보다_높이_튕긴다()
        {
            int bi = FindKind(StageRunner.Kind.Bounce);
            Assert.GreaterOrEqual(bi, 0, "튕김판이 없다");
            var p = stage.GetPlatform(bi);
            var b = player.Body;
            b.X = (p.left + p.right) * 0.5f;
            b.Y = p.y + 0.05f;
            b.vy = -4f;                       // 발판을 향해 내려오는 중
            player.Tick(1f / 60f, 0, false, false);
            Assert.Greater(b.vy, player.tuning.Jump1V * 1.4f, "튕겨 오르지 않았다");
        }

        [UnityTest]
        public IEnumerator 사라지는_발판은_밟으면_잠시_뒤_사라진다()
        {
            int vi = FindKind(StageRunner.Kind.Vanish);
            Assert.GreaterOrEqual(vi, 0, "사라지는 발판이 없다");
            Assert.IsTrue(stage.GetPlatform(vi).active, "처음엔 단단해야 한다");

            stage.NotifyStand(vi);
            float t = 0f;
            while (t < 0.7f) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsFalse(stage.GetPlatform(vi).active, "밟았는데 사라지지 않았다");
        }

        [UnityTest]
        public IEnumerator 바람_지대에서는_바람_방향으로_밀린다()
        {
            var w = stage.Data.winds[0];
            var b = player.Body;
            b.Teleport(w.x + w.width * 0.5f, w.y + w.height * 0.5f);
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.AreNotEqual(0f, b.vx, "바람인데 안 밀렸다");
            Assert.AreEqual(Mathf.Sign(w.force), Mathf.Sign(b.vx), "밀리는 방향이 바람과 반대다");
        }
    }
}
