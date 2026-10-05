using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 구간 추락: 미끄러지면 구간 처음으로 돌아가고, 강아지는 위에 남아 기다린다.
    public class ZoneFallTests
    {
        Zones zones;
        PlayerController player;
        DogController dog;
        Bond bond;
        StageRunner stage;

        [UnitySetUp]
        public IEnumerator Load()
        {
            DogPerks.Clear();
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            TestUi.SilencePerkCard();
            zones = Object.FindFirstObjectByType<Zones>();
            player = Object.FindFirstObjectByType<PlayerController>();
            dog = Object.FindFirstObjectByType<DogController>();
            bond = Object.FindFirstObjectByType<Bond>();
            stage = Object.FindFirstObjectByType<StageRunner>();
            bond.Add(50f - bond.Value);   // 깎이는 걸 볼 수 있게 가운데로
        }

        [TearDown]
        public void Restore() { Time.timeScale = 1f; DogPerks.Clear(); }

        [Test]
        public void 배선이_직렬화됐다()
        {
            Assert.IsNotNull(zones, "Zones 가 씬에 없다");
            Assert.IsNotNull(zones.player, "Zones.player 가 비었다");
            Assert.IsNotNull(zones.dog, "Zones.dog 가 비었다");
            Assert.IsNotNull(zones.stage, "Zones.stage 가 비었다");
        }

        /// 구간 1 의 ★위쪽★ 발판에 둘 다 올려둔다.
        /// 구간 시작 발판에서 떨어뜨리면 되돌아온 자리가 곧 강아지 옆이라
        /// 즉시 재회해 버려서 아무것도 시험하지 못한다.
        IEnumerator ClimbHighInZone1()
        {
            float step = Px.U(zones.zoneMeters * Px.PxPerMeter);
            float zone1 = stage.Data.groundY + step;
            float zone2 = stage.Data.groundY + step * 2f;

            // 구간 1 의 중턱. 꼭대기를 잡으면 그게 곧 구간 2 시작 발판이라 구간이 넘어간다.
            float mid = (zone1 + zone2) * 0.5f;
            float bestY = zone1, bestX = 0.9f;
            foreach (var p in stage.Data.platforms)
                if (p.y < mid && p.y > bestY) { bestY = p.y; bestX = p.x + p.width * 0.5f; }

            player.Body.Teleport(bestX, bestY + 0.05f);
            dog.Body.Teleport(bestX - 0.3f, bestY + 0.05f);
            yield return null;
            yield return null;
            Assert.AreEqual(1, zones.Zone, "구간 1 에 있어야 한다");
        }

        [UnityTest]
        public IEnumerator 구간_바닥_아래로_떨어지면_구간_처음으로_돌아온다()
        {
            yield return ClimbHighInZone1();
            float fellFrom = player.Body.Y;
            float before = bond.Value;

            // 구간 바닥선보다 확실히 아래로 떨어뜨린다
            player.Body.Teleport(player.Body.X, stage.Data.groundY + 0.2f);
            yield return null;
            yield return null;

            // 바닥까지 잃지는 않되, 떨어진 자리로 되돌려주지도 않는다 — 구간 처음이다
            Assert.Greater(player.Body.Y, stage.Data.groundY + 1f,
                           "바닥까지 떨어졌다 (구간 처음으로 돌아와야 한다)");
            Assert.Less(player.Body.Y, fellFrom - 1f,
                        "떨어진 자리로 되돌려줬다 (잃는 게 없으면 긴장도 없다)");
            Assert.Less(bond.Value, before, "구간 추락인데 친밀도가 안 깎였다");
        }

        [UnityTest]
        public IEnumerator 떨어지면_강아지도_같이_내려온다()
        {
            yield return ClimbHighInZone1();
            float fellFrom = player.Body.Y;

            player.Body.Teleport(player.Body.X, stage.Data.groundY + 0.2f);
            yield return null;
            yield return null;

            // 혼자 남겨두지 않는다 — 둘이 같은 자리로
            Assert.Less(dog.Body.Y, fellFrom - 1f, "강아지가 위에 남았다");
            Assert.Less(Mathf.Abs(dog.Body.Y - player.Body.Y), 1.2f,
                        "강아지가 플레이어와 다른 높이에 있다");
            Assert.Less(Mathf.Abs(dog.Body.X - player.Body.X), 1.5f,
                        "강아지가 플레이어와 멀리 떨어졌다");
        }

        [Test]
        public void 작은_실수는_구간을_잃지_않는다()
        {
            // 바닥 근처(구간 0)에서는 더 잃을 구간이 없다
            Assert.AreEqual(0, zones.Zone);
            Assert.Less(Mathf.Abs(dog.Body.Y - player.Body.Y), 2f, "시작부터 강아지와 떨어져 있다");
        }
    }
}
