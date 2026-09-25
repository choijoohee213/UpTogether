using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 창의 장애물 — 톱니/간식/링/매달린 가시가 실제로 친밀도를 올리고 내리는지.
    public class CreativeObstacleTests
    {
        StageRunner stage;
        CharacterBody body;
        Bond bond;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            stage = Object.FindFirstObjectByType<StageRunner>();
            body = Object.FindFirstObjectByType<PlayerController>().Body;
            bond = Object.FindFirstObjectByType<Bond>();
            // 높이 구간 보상이 장애물 효과를 가리지 않게 세션 판정을 끈다
            var session = Object.FindFirstObjectByType<StageSession>();
            if (session != null) session.enabled = false;
            SetBond(50f);   // 오르내림을 둘 다 볼 수 있게 가운데로
        }

        void SetBond(float v) => bond.Add(v - bond.Value);

        [Test]
        public void 창의_장애물이_모두_배치됐다()
        {
            var d = stage.Data;
            Assert.Greater(d.saws.Length, 0, "톱니");
            Assert.Greater(d.rings.Length, 0, "링");
            Assert.Greater(d.treats.Length, 0, "간식");
            bool hangThorn = false;
            foreach (var s in d.spikes) if (s.down) hangThorn = true;
            Assert.IsTrue(hangThorn, "매달린 가시");
        }

        [UnityTest]
        public IEnumerator 톱니에_닿으면_친밀도가_깎인다()
        {
            var p = stage.SawPos(0);
            body.Teleport(p.x, p.y - 0.28f);   // 몸통 중심(Y+0.28)이 톱니에 오게
            float before = bond.Value;
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.Less(bond.Value, before, "톱니에 닿았는데 친밀도가 그대로");
        }

        [UnityTest]
        public IEnumerator 간식을_주우면_친밀도가_오른다()
        {
            var t = stage.Data.treats[0];
            SetBond(50f);
            body.Teleport(t.x, t.y - 0.2f);
            float before = bond.Value;
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(bond.Value, before, "간식을 주웠는데 친밀도가 그대로");
        }

        [UnityTest]
        public IEnumerator 평범한_링을_통과하면_친밀도가_오른다()
        {
            int idx = -1;
            for (int i = 0; i < stage.Data.rings.Length; i++)
                if (!stage.Data.rings[i].thorny) { idx = i; break; }
            Assert.GreaterOrEqual(idx, 0, "평범한 링이 없다");
            var r = stage.Data.rings[idx];
            SetBond(50f);
            body.Teleport(r.x, r.y - 0.28f);   // 가운데 통과
            float before = bond.Value;
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(bond.Value, before, "링을 통과했는데 친밀도가 그대로");
        }

        [UnityTest]
        public IEnumerator 가시_링_테두리에_닿으면_친밀도가_깎인다()
        {
            int idx = -1;
            for (int i = 0; i < stage.Data.rings.Length; i++)
                if (stage.Data.rings[i].thorny) { idx = i; break; }
            Assert.GreaterOrEqual(idx, 0, "가시 링이 없다");
            var r = stage.Data.rings[idx];
            SetBond(50f);
            body.Teleport(r.x + r.radius, r.y - 0.28f);   // 테두리에 닿게
            float before = bond.Value;
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.Less(bond.Value, before, "가시 링 테두리에 닿았는데 친밀도가 그대로");
        }
    }
}
