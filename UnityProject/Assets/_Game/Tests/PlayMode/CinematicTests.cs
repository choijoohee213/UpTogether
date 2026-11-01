using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 컷신 틀의 범용 동작(조작 차단·레터박스·정수배 스냅)과
    /// 친밀도에 따른 강아지 몸짓. 프롤로그 구성 자체는 PrologueSceneTests 가 본다.
    /// 친밀도는 PlayerPrefs 라 들어올 때 적어 두고 나갈 때 되돌린다.
    public class CinematicTests
    {
        Cinematic cine;
        DogController dog;
        DogVisual visual;
        FollowCamera follow;
        GameInput input;
        Bond bond;
        Camera cam;
        float savedBond;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;
            cine = Object.FindFirstObjectByType<Cinematic>();
            dog = Object.FindFirstObjectByType<DogController>();
            visual = Object.FindFirstObjectByType<DogVisual>();
            follow = Object.FindFirstObjectByType<FollowCamera>();
            input = Object.FindFirstObjectByType<GameInput>();
            bond = Object.FindFirstObjectByType<Bond>();
            cam = Camera.main;
            savedBond = bond != null ? bond.Value : 0f;
        }

        [TearDown]
        public void Restore()
        {
            if (bond != null) bond.Set(savedBond);
            if (input != null) input.Blocked = false;
            if (follow != null) follow.enabled = true;
            if (dog != null) dog.enabled = true;
            Time.timeScale = 1f;
        }

        /// 틀만 확인하는 짧은 컷 두 개. 프롤로그와 무관하게 둔다 —
        /// 프롤로그는 전용 씬에 있고 여기서는 Cinematic 자체를 본다.
        static Cinematic.Shot[] TwoShots() => new[]
        {
            new Cinematic.Shot { plate = -1, at = new Vector2(2f, 2f), size = 1f,
                                 seconds = 0.6f, line = "첫 컷", holdAfter = 0.1f },
            new Cinematic.Shot { plate = -1, at = new Vector2(3f, 3f), size = 0.8f,
                                 seconds = 0.6f, line = "둘째 컷", holdAfter = 0.1f },
        };

        [Test]
        public void 배선이_직렬화됐다()
        {
            Assert.IsNotNull(cine, "Cinematic 이 씬에 없다");
            Assert.IsNotNull(cine.cam, "Cinematic.cam 이 비었다");
            Assert.IsNotNull(cine.follow, "Cinematic.follow 가 비었다 — 카메라를 못 빼앗는다");
            Assert.IsNotNull(cine.input, "Cinematic.input 이 비었다 — 조작을 못 끊는다");
            Assert.IsNotNull(cine.barTop, "위 레터박스가 비었다");
            Assert.IsNotNull(cine.barBottom, "아래 레터박스가 비었다");
            Assert.IsNotNull(cine.label, "글 넣을 Text 가 비었다");
        }

        [UnityTest]
        public IEnumerator 컷신이_돌면_조작이_끊기고_카메라를_빼앗는다()
        {
            Assert.IsFalse(cine.Running, "시작부터 돌고 있다");

            cine.Play(TwoShots());
            yield return null;

            Assert.IsTrue(cine.Running, "컷신이 안 돌고 있다");
            Assert.IsTrue(input.Blocked, "조작이 안 끊겼다");
            Assert.IsFalse(follow.enabled, "FollowCamera 를 안 빼앗았다");

            // 끊긴 동안은 키를 눌러도 먹지 않는다
            input.SetRight(true);
            yield return null;
            Assert.IsFalse(input.Right, "조작이 끊겼는데 입력이 먹었다");
            input.SetRight(false);
        }

        [UnityTest]
        public IEnumerator 레터박스가_띠를_키운다()
        {
            float before = cine.barTop.sizeDelta.y;
            cine.Play(TwoShots());

            for (int i = 0; i < 40; i++) yield return null;   // 띠가 자랄 시간

            Assert.Greater(cine.barTop.sizeDelta.y, before, "위 띠가 안 자랐다");
            Assert.Greater(cine.barBottom.sizeDelta.y, before, "아래 띠가 안 자랐다");
        }

        [UnityTest]
        public IEnumerator 컷이_시작될_때_알려준다()
        {
            // 플레이트를 갈아끼우는 쪽(PrologueScene)이 이 신호를 듣는다
            int fired = -1;
            cine.ShotStarted += (i, s) => fired = i;

            cine.Play(TwoShots());
            yield return null;

            Assert.AreEqual(0, fired, "첫 컷 시작을 안 알렸다");
        }

        [UnityTest]
        public IEnumerator 컷신_중에는_화면_픽셀이_정수배다()
        {
            cine.Play(TwoShots());
            // 첫 컷 이동이 끝날 때까지 (0.6초 + 여유)
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) yield return null;

            // 비를 정수로 맞추되, 맞추려다 시야가 10% 넘게 바뀌면 구도를 지킨다.
            // 넓은 컷에서는 반올림 한 칸이 시야를 통째로 바꿔 피사체를 잘라낸다.
            float want = cine.tuning.CameraOrthoSize * TwoShots()[0].size;
            float ratio = Screen.height / (cam.orthographicSize * 2f * Px.PPU);
            bool snapped = Mathf.Abs(Mathf.Round(ratio) - ratio) <= 0.02f;
            bool kept = Mathf.Abs(cam.orthographicSize - want) <= want * 0.001f;

            Assert.IsTrue(snapped || kept,
                          $"화면/게임 픽셀 비가 {ratio:F3} 인데 시야도 요청한 {want:F3} 가 아니다");
            if (!snapped)
                Assert.Greater(Mathf.Abs(Screen.height / (2f * Px.PPU * Mathf.Max(1f, Mathf.Round(
                                   Screen.height / (want * 2f * Px.PPU)))) - want), want * 0.1f,
                               "10% 안쪽으로 맞출 수 있었는데 안 맞췄다");
        }

        [UnityTest]
        public IEnumerator 겁먹으면_웅크리고_친해지면_펴진다()
        {
            // 몸짓은 스프라이트 자식에만 건다 — 몸통을 건드리면 안긴 위치가 어긋난다
            bond.Set(0f);
            yield return null;
            yield return null;
            float scared = visual.target.transform.localScale.y;

            bond.Set(100f);
            yield return null;
            yield return null;
            float trusting = visual.target.transform.localScale.y;

            Assert.Less(scared, trusting, "친밀도가 낮을 때 더 웅크려야 한다");
            Assert.AreEqual(1f, trusting, 0.01f, "다 친해졌는데도 눌려 있다");
        }

        [UnityTest]
        public IEnumerator 몸짓은_몸통이_아니라_그림에만_건다()
        {
            bond.Set(0f);
            var body = dog.GetComponent<CharacterBody>();
            float x = body.X;
            for (int i = 0; i < 10; i++) yield return null;

            // 떨림이 루트를 흔들면 발판 판정과 안긴 좌표가 같이 흔들린다
            Assert.AreEqual(x, body.X, 0.0001f, "떨림이 몸통 좌표를 움직였다");
        }

        [UnityTest]
        public IEnumerator 많이_친해지면_꼬리를_흔든다()
        {
            // happy(11-12번)는 시트에 있는데 지금까지 아무 데서도 안 쓰이던 프레임이다
            var happy = visual.spriteSet.Find("happy");
            Assert.IsNotNull(happy, "happy 클립이 없다");

            // 강아지 AI 를 멈춰 세운다. CharacterBody 는 Step() 안에서만 물리를 돌기
            // 때문에 컨트롤러를 끄면 완전히 멈춘다 (따라 걷는 중이면 walk 가 뜬다).
            dog.enabled = false;
            var body = dog.GetComponent<CharacterBody>();
            body.vx = 0f;
            body.grounded = true;
            bond.Set(100f);

            // 공중에서 시작했으면 land 클립이 0.12초 붙잡힌다. 그게 풀릴 때까지 기다린다.
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                body.vx = 0f;
                body.grounded = true;
                yield return null;
            }

            Assert.AreEqual("happy", visual.CurrentClip, "다 친해져 서 있는데 꼬리를 안 흔든다");

            var want = new System.Collections.Generic.HashSet<Sprite>();
            foreach (int f in happy.frames) want.Add(visual.spriteSet.Frame(f));
            Assert.IsTrue(want.Contains(visual.target.sprite), "happy 프레임이 안 그려졌다");

            dog.enabled = true;
        }
    }
}
