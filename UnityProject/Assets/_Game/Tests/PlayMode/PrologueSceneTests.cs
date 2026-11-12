using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 프롤로그 전용 씬. 플레이 맵과 무관해야 하고, 시점이 교차해야 한다.
    public class PrologueSceneTests
    {
        PrologueScene pro;
        Cinematic cine;
        Narration narration;
        bool savedSeen;

        [UnitySetUp]
        public IEnumerator Load()
        {
            // 매번 처음 보는 것처럼 — 안 그러면 Start 가 곧바로 Playground 로 넘긴다
            savedSeen = PlayerPrefs.GetInt("climb2_story_" + PrologueScene.SeenKey, 0) != 0;
            PlayerPrefs.DeleteKey("climb2_story_" + PrologueScene.SeenKey);

            yield return SceneManager.LoadSceneAsync("Prologue", LoadSceneMode.Single);
            yield return null;
            pro = Object.FindFirstObjectByType<PrologueScene>();
            cine = Object.FindFirstObjectByType<Cinematic>();
            narration = Object.FindFirstObjectByType<Narration>();
        }

        [TearDown]
        public void Restore()
        {
            if (savedSeen) PlayerPrefs.SetInt("climb2_story_" + PrologueScene.SeenKey, 1);
            else PlayerPrefs.DeleteKey("climb2_story_" + PrologueScene.SeenKey);
            Time.timeScale = 1f;
        }

        [Test]
        public void 배선이_직렬화됐다()
        {
            Assert.IsNotNull(pro, "PrologueScene 이 씬에 없다");
            Assert.IsNotNull(pro.cinematic, "PrologueScene.cinematic 이 비었다");
            Assert.IsNotNull(pro.narration, "PrologueScene.narration 이 비었다");
            Assert.IsNotNull(pro.plate, "플레이트(그림 자리)가 비었다");
            Assert.IsNotNull(pro.slate, "자리표시 설명 Text 가 비었다");
            Assert.AreEqual("Playground", pro.nextScene, "끝나고 갈 씬이 Playground 가 아니다");

            Assert.IsNotNull(cine.barTop, "위 레터박스가 비었다");
            Assert.IsNotNull(cine.barBottom, "아래 레터박스가 비었다");
            Assert.IsNotNull(cine.label, "글 넣을 Text 가 비었다");
        }

        [Test]
        public void 플레이_맵과_무관하다()
        {
            // 프롤로그가 Playground 안에서 돌면 "다른 곳에서 만났다"가 안 느껴진다.
            // 이 씬에는 발판·플레이어·강아지 AI 가 아예 없어야 한다.
            Assert.IsNull(Object.FindFirstObjectByType<StageRunner>(), "프롤로그 씬에 발판이 있다");
            Assert.IsNull(Object.FindFirstObjectByType<PlayerController>(), "프롤로그 씬에 플레이어가 있다");
            Assert.IsNull(Object.FindFirstObjectByType<DogController>(), "프롤로그 씬에 강아지 AI 가 있다");
            Assert.IsNull(Object.FindFirstObjectByType<StageSession>(), "프롤로그 씬에 판 진행이 있다");
        }

        [Test]
        public void 컷이_프롤로그_줄과_맞는다()
        {
            var shots = pro.Shots();
            Assert.AreEqual(Narration.Prologue.Length, shots.Length, "컷 수가 줄 수와 다르다");
            for (int i = 0; i < shots.Length; i++)
            {
                Assert.AreEqual(Narration.Prologue[i], shots[i].line, $"{i}번 컷의 글이 어긋났다");
                Assert.AreEqual(i, shots[i].plate, $"{i}번 컷의 플레이트 번호가 어긋났다");
                Assert.Greater(shots[i].seconds, 0f, $"{i}번 컷의 이동 시간이 0 이다");
                Assert.Greater(shots[i].size, 0f, $"{i}번 컷의 시야 배수가 0 이다");
                // 컷마다 그림이 바뀌므로 앞 컷에서 이어 움직이면 엉뚱한 곳을 훑는다
                Assert.Greater(shots[i].fromSize, 0f, $"{i}번 컷에 시작 구도가 없다");
            }
        }

        [Test]
        public void 품종마다_플레이트가_한_벌씩_있다()
        {
            // 고른 강아지로 이야기가 돌아가야 한다. 1~4번 컷은 품종별로 다시 그렸고
            // 5번(언덕과 집)은 강아지가 없어 공용 한 장을 쓴다.
            Assume.That(pro.breedPlates != null && pro.breedPlates.Length > 0,
                        "아직 그림이 안 들어왔다 — 자리표시 상태");

            Assert.AreEqual(7, pro.breedPlates.Length, "견종 수만큼 플레이트 벌이 없다");
            foreach (var b in pro.breedPlates)
            {
                Assert.IsNotEmpty(b.breed, "견종 id 가 비었다");
                Assert.AreEqual(Narration.Prologue.Length, b.plates.Length,
                                $"{b.breed} 의 플레이트 수가 컷 수와 다르다");
                for (int i = 0; i < b.plates.Length; i++)
                    Assert.IsNotNull(b.plates[i], $"{b.breed} 의 {i + 1}번 플레이트가 비었다");
            }
        }

        [UnityTest]
        public IEnumerator 고른_견종의_플레이트를_쓴다()
        {
            Assume.That(pro.breedPlates != null && pro.breedPlates.Length > 1,
                        "아직 그림이 안 들어왔다");

            string saved = Selection.Breed;
            try
            {
                // 서로 다른 두 견종이 서로 다른 그림을 쓰는지 본다
                var a = pro.breedPlates[0];
                var b = pro.breedPlates[1];
                Assert.AreNotSame(a.plates[0], b.plates[0], "두 견종이 같은 그림을 쓴다");

                Selection.Breed = b.breed;
                // 다시 처음 보는 것으로 돌려놔야 한다 — 안 그러면 Start 가
                // 곧바로 Playground 로 넘겨버려서 씬에 PrologueScene 이 없다.
                PlayerPrefs.DeleteKey("climb2_story_" + PrologueScene.SeenKey);

                yield return SceneManager.LoadSceneAsync("Prologue", LoadSceneMode.Single);
                yield return null;
                var again = Object.FindFirstObjectByType<PrologueScene>();
                Assert.IsNotNull(again, "다시 불러온 씬에 PrologueScene 이 없다");
                for (int i = 0; i < 6; i++) yield return null;

                Assert.AreEqual(b.plates[0], again.plate.sprite,
                                $"{b.breed} 을 골랐는데 그 견종 그림이 안 떴다");
            }
            finally { Selection.Breed = saved; }
        }

        [Test]
        public void 시점이_교차한다()
        {
            var shots = pro.Shots();
            // 3인칭 → 1인칭 → 1인칭 → 3인칭 → 3인칭.
            // 멀리서 보다가 내 눈이 되는 순간이 있어야 세 번째 줄이 남의 일이 아니게 된다.
            Assert.AreEqual(Cinematic.View.Third, shots[0].view);
            Assert.AreEqual(Cinematic.View.First, shots[1].view);
            Assert.AreEqual(Cinematic.View.First, shots[2].view, "핵심 컷이 1인칭이 아니다");
            Assert.AreEqual(Cinematic.View.Third, shots[3].view);
            Assert.AreEqual(Cinematic.View.Third, shots[4].view);

            int first = 0, third = 0;
            foreach (var s in shots)
                if (s.view == Cinematic.View.First) first++; else third++;
            Assert.Greater(first, 0, "1인칭 컷이 없다");
            Assert.Greater(third, 0, "3인칭 컷이 없다");
        }

        /// 플레이트는 750x1300, PPU 100, 9:16 세로. 시야 배수 s 에서 366s x 650s px 가 보인다.
        /// 유닛 → 플레이트 픽셀: px = x*100+375, py = 650-y*100
        static (float x0, float x1, float y0, float y1) ViewPx(Vector2 at, float size)
        {
            float w = 366f * size, h = 650f * size;
            float px = at.x * 100f + 375f, py = 650f - at.y * 100f;
            return (px - w / 2f, px + w / 2f, py - h / 2f, py + h / 2f);
        }

        [Test]
        public void 컷마다_피사체가_화면에_들어온다()
        {
            // 한때 받은 구도표의 y 가 컷마다 어긋나서, 목줄을 노린 컷이
            // 상자 바닥을 클로즈업했다. 아래 값은 그림을 눈으로 재서 적은
            // 피사체 범위다 (품종 차분은 배경 팔레트까지 달라서 못 쓴다).
            // 구도를 건드려서 피사체가 잘리면 여기서 걸린다.
            var subjects = new[]
            {
                new object[] { 0, "강아지",   248f, 390f,  835f,  920f },
                new object[] { 1, "이름표",   335f, 412f,  588f,  645f },
                new object[] { 2, "강아지",   148f, 455f,  495f,  760f },
                new object[] { 2, "내민 손",  495f, 700f,  845f, 1070f },
                new object[] { 3, "강아지",   365f, 425f,  752f,  835f },
                new object[] { 3, "사람",     230f, 360f,  940f, 1300f },
                new object[] { 4, "집",       295f, 470f,  415f,  650f },
            };

            var shots = pro.Shots();
            const float tol = 1.5f;   // 가장자리에 닿는 것(화면 밖에서 들어오는 손 등)은 허용

            foreach (var s in subjects)
            {
                int cut = (int)s[0]; string what = (string)s[1];
                float a = (float)s[2], b = (float)s[3], c = (float)s[4], d = (float)s[5];
                var v = ViewPx(shots[cut].at, shots[cut].size);

                Assert.LessOrEqual(v.x0, a + tol, $"컷{cut + 1} {what} 왼쪽이 잘린다");
                Assert.GreaterOrEqual(v.x1, b - tol, $"컷{cut + 1} {what} 오른쪽이 잘린다");
                Assert.LessOrEqual(v.y0, c + tol, $"컷{cut + 1} {what} 위가 잘린다");
                Assert.GreaterOrEqual(v.y1, d - tol, $"컷{cut + 1} {what} 아래가 잘린다");
            }
        }

        [Test]
        public void 어느_컷도_그림_밖을_비추지_않는다()
        {
            var shots = pro.Shots();
            const float tol = 1.5f;
            foreach (var shot in shots)
                foreach (var (at, size) in new[] { (shot.from, shot.fromSize), (shot.at, shot.size) })
                {
                    var v = ViewPx(at, size);
                    Assert.GreaterOrEqual(v.x0, -tol, "왼쪽으로 그림 밖이 보인다");
                    Assert.LessOrEqual(v.x1, 750f + tol, "오른쪽으로 그림 밖이 보인다");
                    Assert.GreaterOrEqual(v.y0, -tol, "위로 그림 밖이 보인다");
                    Assert.LessOrEqual(v.y1, 1300f + tol, "아래로 그림 밖이 보인다");
                }
        }

        [Test]
        public void 레터박스가_꽉_찬_사각형이다()
        {
            // 빌트인 UISprite 를 Simple 로 늘였더니 화면 위에 모서리가 둥근
            // 검은 덩어리가 떠 있었다. 스프라이트 없는 Image 라야 꽉 찬다.
            foreach (var bar in new[] { cine.barTop, cine.barBottom })
            {
                Assert.IsNotNull(bar, "레터박스 띠가 없다");
                var img = bar.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(img, "레터박스에 Image 가 없다");
                Assert.IsNull(img.sprite, $"{bar.name} 에 스프라이트가 붙어 모서리가 둥글어진다");
            }
        }

        [Test]
        public void 글_뒤에_어둠이_깔린다()
        {
            // 흰 글씨만 올려 두면 상자 앞면처럼 밝은 그림 위에서 글이 사라진다
            var scrim = GameObject.Find("LineScrim");
            Assert.IsNotNull(scrim, "글 뒤 어둠(LineScrim)이 없다");
            var img = scrim.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(img?.sprite, "어둠이 그라데이션이 아니다 — 단색 띠는 경계선이 보인다");
            Assert.IsNotNull(cine.label.GetComponent<UnityEngine.UI.Shadow>(),
                             "글자에 그림자가 없다");
            Assert.Less(scrim.transform.GetSiblingIndex(), cine.label.transform.GetSiblingIndex(),
                        "어둠이 글보다 위에 그려진다");
        }

        [Test]
        public void 프롤로그_동안_오르골을_끈다()
        {
            // 버려진 강아지를 발견하는 비 오는 골목에 밝은 음악이 깔리면
            // 글이 아무리 좋아도 장면이 서로 싸운다
            Assert.IsNotNull(Sfx.I, "Sfx 가 없다");
            Assert.IsFalse(Sfx.I.BgmPlaying, "프롤로그인데 BGM 이 돌고 있다");
        }

        [Test]
        public void 마지막_컷에서_비가_그친다()
        {
            // 그림의 트인 하늘과 맞아야 한다 — 거기만 비가 없다
            Assert.IsNotNull(pro.rain, "비가 씬에 없다");
            Assert.AreEqual(pro.Shots().Length - 1, pro.rainStopsAt,
                            "비가 마지막 컷에서 그치지 않는다");
        }

        [Test]
        public void 핵심_컷에_침묵과_소리가_있다()
        {
            var shots = pro.Shots();
            Assert.IsTrue(shots[2].whine, "핵심 줄에 낑낑 소리가 없다");
            Assert.GreaterOrEqual(shots[2].holdAfter, 1.5f, "핵심 줄 뒤 침묵이 너무 짧다");

            // 느리게 밀고 들어간다. 다만 **가장 가까운 컷은 아니다** —
            // 손과 강아지 사이의 틈이 이 컷의 주제라서, 더 당기면 그 틈이 잘린다.
            // (2번 컷이 이름표까지 더 깊게 들어간다.)
            Assert.Less(shots[2].size, shots[2].fromSize, "핵심 컷이 밀고 들어가지 않는다");
        }

        [UnityTest]
        public IEnumerator 씬에_들어오면_저절로_돌고_봤다고_적힌다()
        {
            yield return null;
            Assert.IsTrue(cine.Running, "프롤로그가 저절로 시작되지 않았다");
            Assert.IsTrue(narration.Seen(PrologueScene.SeenKey), "봤다고 적히지 않았다");
        }

        [UnityTest]
        public IEnumerator 그림이_있으면_자리표시_대신_그림이_뜬다()
        {
            for (int i = 0; i < 20; i++) yield return null;

            bool hasArt = pro.breedPlates != null && pro.breedPlates.Length > 0;
            if (hasArt)
            {
                Assert.IsNotNull(pro.plate.sprite, "그림이 있는데 플레이트가 비었다");
                Assert.AreEqual(Color.white, pro.plate.color, "자리표시 색이 남아 있다");
                Assert.IsEmpty(pro.slate.text, "그림이 있는데 자리표시 설명이 떠 있다");
            }
            else
            {
                // 그림이 빠진 상태에서도 구성과 타이밍은 확인할 수 있어야 한다
                Assert.IsNotEmpty(pro.slate.text, "자리표시 설명이 비어 있다");
                StringAssert.Contains("컷 1/5", pro.slate.text, "몇 번째 컷인지 안 적혀 있다");
                StringAssert.Contains("3인칭", pro.slate.text, "시점이 안 적혀 있다");
            }
        }
    }
}
