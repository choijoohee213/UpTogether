using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 친밀도가 강아지 행동을 바꾸는 부분과 서사 연동.
    /// 친밀도는 PlayerPrefs 에 저장되므로 들어올 때 값을 적어 두고 나갈 때 되돌린다
    /// — 안 그러면 테스트가 실제 플레이 기록을 덮어쓴다.
    public class BondStoryTests
    {
        const string TestKey = "__test_line";

        DogController dog;
        StageSession session;
        Bond bond;
        Narration narration;
        DevPanel dev;
        Tuning tuning;
        float savedBond;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            dog = Object.FindFirstObjectByType<DogController>();
            session = Object.FindFirstObjectByType<StageSession>();
            bond = Object.FindFirstObjectByType<Bond>();
            narration = Object.FindFirstObjectByType<Narration>();
            dev = Object.FindFirstObjectByType<DevPanel>();
            tuning = dog != null ? dog.tuning : null;
            savedBond = bond != null ? bond.Value : 0f;
        }

        [TearDown]
        public void Restore()
        {
            if (bond != null) bond.Set(savedBond);
            PlayerPrefs.DeleteKey("climb2_story_" + TestKey);
            Time.timeScale = 1f;
        }

        [Test]
        public void 배선이_직렬화됐다()
        {
            Assert.IsNotNull(dog, "DogController 가 씬에 없다");
            Assert.IsNotNull(dog.bond, "DogController.bond 가 비었다 — 친밀도가 행동에 안 닿는다");
            Assert.IsNotNull(session.flow, "StageSession.flow 가 비었다 — 오프닝·엔딩 줄을 못 고른다");
            Assert.IsNotNull(session.hazards, "StageSession.hazards 가 비었다 — 플래시백 줄이 안 뜬다");
            Assert.IsNotNull(dev.bond, "DevPanel.bond 가 비었다");
            Assert.IsNotNull(dev.bondRow, "친밀도 버튼 줄이 비었다");

            // 일시정지 패널 안이라 평소엔 꺼져 있다 — 꺼진 것도 찾는다
            var taps = Object.FindObjectsByType<BondTap>(FindObjectsInactive.Include,
                                                         FindObjectsSortMode.None);
            Assert.AreEqual(6, taps.Length, "친밀도 버튼이 6단계만큼 없다");
            foreach (var t in taps)
                Assert.IsNotNull(t.panel, "버튼의 panel 참조가 비었다 (람다는 직렬화되지 않는다)");
        }

        [Test]
        public void 안기기는_문턱을_넘어야_열린다()
        {
            float unlock = tuning.bondClingUnlock;

            bond.Set(0f);
            Assert.IsFalse(dog.ClingUnlocked, "친밀도 0 인데 안기기가 열려 있다");

            bond.Set(unlock - 1f);
            Assert.IsFalse(dog.ClingUnlocked, "문턱 바로 아래인데 열려 있다");

            bond.Set(unlock);
            Assert.IsTrue(dog.ClingUnlocked, "문턱에 닿았는데 안 열렸다");
        }

        [Test]
        public void 단계_이름이_문턱과_맞는다()
        {
            bond.Set(0f);
            Assert.AreEqual("겁먹음", bond.StageName);
            // 안기기 해금과 단계 이름이 같은 지점이어야 플레이어가 설명 없이 이해한다
            bond.Set(tuning.bondClingUnlock);
            Assert.AreEqual("품에 안김", bond.StageName, "해금 문턱과 '품에 안김' 단계가 어긋났다");
            bond.Set(100f);
            Assert.AreEqual("영혼의 단짝", bond.StageName);
        }

        [Test]
        public void 단계가_올라갈_때만_울린다()
        {
            int fired = -1;
            bond.Set(0f);
            bond.StageUp += i => fired = i;

            bond.Set(20f);
            Assert.AreEqual(1, fired, "단계가 올라갔는데 StageUp 이 안 울렸다");

            fired = -1;
            bond.Set(0f);
            Assert.AreEqual(-1, fired, "내려갈 때도 울렸다 — 퇴행은 말로 하지 않는다");
        }

        [Test]
        public void 높이_문턱은_여섯_개_그대로다()
        {
            // 글을 덜어내면서 문턱까지 지우면 한 판에서 얻는 친밀도가 30 에서 10 으로
            // 줄어 PROJECT.md "검증된 수치"가 깨진다. 글과 문턱은 따로 가야 한다.
            Assert.AreEqual(6, Narration.Default.Length, "높이 문턱 수가 바뀌었다");
            Assert.AreEqual(10f, Narration.Default[0].meters);
            Assert.AreEqual(130f, Narration.Default[5].meters);

            int withText = 0;
            foreach (var l in Narration.Default) if (!string.IsNullOrEmpty(l.text)) withText++;
            Assert.AreEqual(2, withText, "글이 붙은 높이 줄은 28m·130m 둘뿐이어야 한다");
        }

        [Test]
        public void 빈_높이_구간도_친밀도는_올린다()
        {
            narration.Reset();
            // 10m 구간은 글이 없다. 그래도 구간을 넘었다고 알려야 친밀도가 오른다.
            Assert.IsTrue(narration.CheckHeight(10f), "글 없는 구간이 false 를 돌려줬다");
        }

        [Test]
        public void 스테이지_수만큼_오프닝_엔딩이_있다()
        {
            var flow = Object.FindFirstObjectByType<StageFlow>();
            Assert.AreEqual(flow.stages.Length, Narration.StageOpen.Length, "오프닝 줄 수가 스테이지 수와 다르다");
            Assert.AreEqual(flow.stages.Length, Narration.StageClose.Length, "엔딩 줄 수가 스테이지 수와 다르다");
        }

        [Test]
        public void 사건_대사는_한_번만_뜬다()
        {
            int said = 0;
            narration.Say += _ => said++;
            PlayerPrefs.DeleteKey("climb2_story_" + TestKey);

            Assert.IsTrue(narration.SpeakOnce(TestKey, "처음"), "첫 번째가 안 떴다");
            Assert.IsFalse(narration.SpeakOnce(TestKey, "처음"), "두 번째도 떴다 — '처음'의 뜻이 없어진다");
            Assert.AreEqual(1, said, "한 줄만 떠야 한다");
        }

        [Test]
        public void 안_친할수록_멀리_떨어져_따라온다()
        {
            Assert.Greater(tuning.dogTrailDistanceShy, tuning.dogTrailDistance,
                           "경계할 때 거리가 친할 때보다 멀지 않다");
            Assert.Greater(tuning.dogHesitateSeconds, 0f, "멈칫이 0 이면 망설이지 않는다");
            Assert.Greater(tuning.dogFlashbackTrailScale, 1f, "플래시백이 거리를 벌리지 않는다");
        }

        [UnityTest]
        public IEnumerator 플래시백은_시간이_지나면_풀린다()
        {
            dog.Flashback();
            Assert.IsTrue(dog.InFlashback, "움츠러들지 않았다");

            float wait = tuning.dogFlashbackSeconds + 0.2f;
            for (float t = 0f; t < wait; t += Time.deltaTime) yield return null;

            Assert.IsFalse(dog.InFlashback, "시간이 지났는데 안 돌아왔다");
        }
    }
}
