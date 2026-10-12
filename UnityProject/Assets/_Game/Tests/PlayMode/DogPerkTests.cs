using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 강아지 특기: 문턱에서 선택 카드가 뜨는지, 고른 능력이 실제로 작동하는지.
    public class DogPerkTests
    {
        PerkPicker picker;
        Bond bond;
        DogAbilities abilities;
        StageSession session;
        Hazards hazards;

        [UnitySetUp]
        public IEnumerator Load()
        {
            DogPerks.Clear();                 // 저장된 선택을 비우고 시작
            PlayerPrefs.SetFloat("climb2_bond", 0f);
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            picker = Object.FindFirstObjectByType<PerkPicker>();
            bond = Object.FindFirstObjectByType<Bond>();
            abilities = Object.FindFirstObjectByType<DogAbilities>();
            session = Object.FindFirstObjectByType<StageSession>();
            hazards = Object.FindFirstObjectByType<Hazards>();
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            DogPerks.Clear();
        }

        [Test]
        public void 배선이_직렬화됐다()
        {
            Assert.IsNotNull(picker, "PerkPicker 가 씬에 없다");
            Assert.IsNotNull(picker.panel, "선택 패널이 비었다");
            Assert.IsNotNull(abilities, "DogAbilities 가 씬에 없다");
            Assert.AreEqual(3, picker.choices.Length, "선택 칸이 3개가 아니다");
            foreach (var c in picker.choices)
                Assert.IsNotNull(c.picker, "칸의 picker 참조가 비었다 (람다는 직렬화되지 않는다)");
            Assert.AreSame(abilities, hazards.abilities, "Hazards 가 능력을 모른다");
            Assert.AreSame(abilities, session.abilities, "StageSession 이 능력을 모른다");
        }

        [UnityTest]
        public IEnumerator 친밀도_문턱을_넘으면_선택_카드가_뜬다()
        {
            Assert.IsFalse(picker.panel.activeSelf, "처음부터 떠 있으면 안 된다");

            bond.Add(DogPerks.Thresholds[0] - bond.Value + 1f);   // 첫 문턱 넘기
            yield return null;

            Assert.IsTrue(picker.panel.activeSelf, "문턱을 넘었는데 카드가 안 떴다");
            Assert.AreEqual(0f, Time.timeScale, "카드가 떴는데 시간이 안 멈췄다");
        }

        [UnityTest]
        public IEnumerator 고르면_닫히고_능력이_남는다()
        {
            bond.Add(DogPerks.Thresholds[0] - bond.Value + 1f);
            yield return null;

            picker.Pick(DogPerks.Kind.Cozy);
            yield return null;

            Assert.IsFalse(picker.panel.activeSelf, "고른 뒤에도 카드가 떠 있다");
            Assert.AreEqual(1f, Time.timeScale, "고른 뒤 시간이 안 돌아왔다");
            Assert.IsTrue(DogPerks.Has(DogPerks.Kind.Cozy), "고른 특기가 저장되지 않았다");
            Assert.AreEqual(1, DogPerks.Claimed, "문턱 소비가 기록되지 않았다");
        }

        [UnityTest]
        public IEnumerator 문턱은_세_번이고_같은_문턱에_두_번_주지_않는다()
        {
            Assert.AreEqual(3, DogPerks.Thresholds.Length);

            bond.Add(DogPerks.Thresholds[0] + 1f - bond.Value);
            yield return null;
            picker.Pick(DogPerks.Kind.Super);
            yield return null;

            // 같은 구간에서 더 올라도 또 뜨지 않는다
            bond.Add(5f);
            yield return null;
            Assert.IsFalse(picker.panel.activeSelf, "같은 문턱에서 또 떴다");

            // 다음 문턱을 넘으면 다시 뜬다
            bond.Add(DogPerks.Thresholds[1] + 1f - bond.Value);
            yield return null;
            Assert.IsTrue(picker.panel.activeSelf, "두 번째 문턱에서 안 떴다");
        }

        [Test]
        public void 포근한_강아지는_찔림을_한_번_막는다()
        {
            DogPerks.Take(DogPerks.Kind.Cozy);
            Assert.IsTrue(abilities.TryShield(), "막아주지 않았다");
            Assert.IsFalse(abilities.TryShield(), "쿨타임 없이 연달아 막았다");
        }

        [Test]
        public void 특기가_없으면_아무것도_막지_않는다()
        {
            Assert.IsFalse(abilities.TryShield(), "특기가 없는데 막았다");
            Assert.IsFalse(abilities.TryRescue(20f), "특기가 없는데 구해줬다");
        }

        [Test]
        public void 슈퍼맨_강아지는_떨어진_만큼_끌어올린다()
        {
            DogPerks.Take(DogPerks.Kind.Super);
            var player = Object.FindFirstObjectByType<PlayerController>();
            float before = player.Body.Y;

            Assert.IsTrue(abilities.TryRescue(20f), "구해주지 않았다");
            Assert.Greater(player.Body.Y, before, "끌어올리지 않았다");
            Assert.IsFalse(abilities.TryRescue(20f), "쿨타임 없이 연달아 구해줬다");
        }

        [Test]
        public void 먹보_강아지는_손이_닿지_않는_간식만_가져온다()
        {
            var collectibles = Object.FindFirstObjectByType<Collectibles>();
            var stage = Object.FindFirstObjectByType<StageRunner>();
            var treats = stage.Data.treats;
            Assert.Greater(treats.Length, 0, "간식이 없는 스테이지다");

            // 바로 위에 선 간식은 본인이 줍는 거리라 대신 가져오지 않는다.
            // (반경을 좁혀 다른 간식이 끼어들지 않게 한다)
            Assert.IsFalse(collectibles.FetchNear(treats[0].x, treats[0].y - 0.2f, 0.3f, out _, out _),
                           "직접 닿는 간식을 대신 가져갔다");

            // 조금 떨어지면 가져온다
            Assert.IsTrue(collectibles.FetchNear(treats[0].x + 1.2f, treats[0].y - 0.2f, 2.4f,
                                                 out float tx, out float ty),
                          "닿지 않는 간식을 안 가져왔다");
            float d = Mathf.Sqrt((tx - treats[0].x) * (tx - treats[0].x) +
                                 (ty - treats[0].y) * (ty - treats[0].y));
            Assert.Less(d, 2.4f, "반경 밖의 간식을 가져왔다");
        }
    }
}
