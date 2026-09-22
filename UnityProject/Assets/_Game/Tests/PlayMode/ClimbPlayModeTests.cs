using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 실제로 게임을 돌려서 확인한다.
    /// 에디터 검증(Verify Jump Heights)은 물리 수치만 보지만, 여기서는 씬을 띄우고
    /// FixedUpdate를 진짜로 돌린다 — 착지, 강아지, 카메라처럼 "굴러가야 아는" 것들.
    public class ClimbPlayModeTests
    {
        PlayerController player;
        PlayerVisual playerVisual;
        DogController dog;
        StageRunner stage;
        Camera cam;
        GameInput input;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;

            player = Object.FindFirstObjectByType<PlayerController>();
            dog    = Object.FindFirstObjectByType<DogController>();
            playerVisual = Object.FindFirstObjectByType<PlayerVisual>();
            stage  = Object.FindFirstObjectByType<StageRunner>();
            input  = Object.FindFirstObjectByType<GameInput>();
            cam    = Camera.main;

            Assert.IsNotNull(player, "씬에 Player가 없다");
            Assert.IsNotNull(dog, "씬에 Dog가 없다");
            Assert.IsNotNull(stage, "씬에 Stage가 없다");
            Assert.IsNotNull(input, "씬에 GameInput이 없다");
            Assert.IsNotNull(cam, "씬에 Main Camera가 없다");
        }

        [TearDown]
        public void ReleaseInput()
        {
            if (input == null) return;
            input.SetLeft(false); input.SetRight(false); input.SetJump(false);
        }

        static IEnumerator Steps(int n)
        {
            for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate();
        }

        [Test]
        public void 물리스텝이_60Hz다()
        {
            // GameBootstrap이 강제한다. 50Hz로 돌면 점프 높이가 조용히 어긋난다.
            Assert.AreEqual(1f / 60f, Time.fixedDeltaTime, 1e-6f);
        }

        [UnityTest]
        public IEnumerator 가만히_두면_바닥에_서_있는다()
        {
            yield return Steps(30);
            Assert.IsTrue(player.Body.grounded, "바닥에 서 있지 않다");
            Assert.AreEqual(stage.Data.groundY, player.Body.Y, 0.001f);
        }

        [UnityTest]
        public IEnumerator 오른쪽을_누르면_걷기_최고속도에_도달한다()
        {
            float startX = player.Body.X;
            input.SetRight(true);
            yield return Steps(60);

            Assert.Greater(player.Body.X, startX, "오른쪽으로 움직이지 않았다");
            // lerp 0.34면 1초 안에 최고속도에 충분히 수렴한다
            Assert.AreEqual(player.tuning.WalkSpeedV, player.Body.vx, 0.02f, "걷기 속도가 기준과 다르다");
            Assert.AreEqual(1, player.Body.face, "오른쪽을 보고 있어야 한다");
        }

        [UnityTest]
        public IEnumerator 점프해서_첫_발판에_올라선다()
        {
            // 첫 발판(바닥 제외) 한가운데 아래로 옮긴 뒤 최대 홀드로 뛴다
            var target = stage.Data.platforms[1];
            float centerX = target.x + target.width * 0.5f;
            player.Body.Teleport(centerX, stage.Data.groundY);
            yield return Steps(5);

            input.SetJump(true);
            yield return Steps(90);   // 올라갔다 내려올 때까지
            input.SetJump(false);
            yield return Steps(30);

            Assert.IsTrue(player.Body.grounded, "어딘가에 착지하지 못했다");
            Assert.AreEqual(target.y, player.Body.Y, 0.001f,
                $"첫 발판(y={target.y:F3})이 아니라 y={player.Body.Y:F3}에 있다");
        }

        [UnityTest]
        public IEnumerator 아래에서_발판을_통과한다()
        {
            // 원웨이: 올라갈 때는 뚫고 지나가야 한다.
            // 첫 발판 바로 아래에서 위로 쏘면 발판 위로 나와야 한다.
            var target = stage.Data.platforms[1];
            float centerX = target.x + target.width * 0.5f;
            player.Body.Teleport(centerX, target.y - 0.30f);
            player.Body.grounded = false;
            player.Body.vy = player.tuning.Jump1V;

            bool wentAbove = false;
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForFixedUpdate();
                if (player.Body.Y > target.y + 0.05f) { wentAbove = true; break; }
            }
            Assert.IsTrue(wentAbove, "아래에서 발판을 뚫고 올라가지 못했다 (원웨이가 깨졌다)");
        }

        [UnityTest]
        public IEnumerator 강아지가_따라온다()
        {
            float dogStartX = dog.transform.position.x;
            input.SetRight(true);
            yield return Steps(120);
            input.SetRight(false);
            yield return Steps(60);

            Assert.Greater(dog.transform.position.x, dogStartX + 0.5f, "강아지가 따라오지 않았다");

            float gap = Mathf.Abs(player.Body.X - dog.transform.position.x);
            Assert.Less(gap, player.tuning.DogTeleportXU, "강아지가 순간이동 거리보다 멀어졌다");
        }

        [UnityTest]
        public IEnumerator 크게_떨어지면_강아지가_안긴다()
        {
            yield return FallUntilHeld();
            Assert.IsTrue(dog.IsClinging);
        }

        [UnityTest]
        public IEnumerator 높이_뛰었다_내려오는_것만으로는_안기지_않는다()
        {
            // 예전엔 낙하 속도만 봐서, 높이 점프해 내려오는 중에도 안겨버렸다.
            yield return Steps(30);
            Assert.IsTrue(player.Body.grounded);

            input.SetJump(true);
            yield return Steps(20);      // 최대 홀드로 높이 뛴다
            input.SetJump(false);

            for (int i = 0; i < 60; i++) // 정점 찍고 내려와 착지할 때까지
            {
                yield return new WaitForFixedUpdate();
                Assert.IsFalse(dog.IsClinging,
                    $"제자리 점프인데 안겼다 (y={player.Body.Y:F2}, 마지막 지면={player.LastGroundedY:F2})");
                if (player.Body.grounded && i > 10) break;
            }
        }

        [UnityTest]
        public IEnumerator 카메라가_플레이어를_따라간다()
        {
            player.Body.Teleport(player.Body.X, stage.Data.groundY + 5f);
            player.Body.grounded = false;
            yield return Steps(3);

            float before = cam.transform.position.y;
            yield return Steps(60);
            Assert.AreNotEqual(before, cam.transform.position.y, "카메라가 전혀 움직이지 않았다");
        }

        [UnityTest]
        public IEnumerator 카메라가_플레이어를_화면_세로_58퍼센트에_둔다()
        {
            // 맵 위아래 끝에서는 화면 밖을 안 보여주려고 클램프가 걸려 58%가 깨진다.
            // 바닥에 서 있을 때가 그렇다 — 원본 프로토타입도 똑같다.
            // 그래서 클램프가 안 걸리는 중간 높이 발판에서 잰다.
            var mid = stage.Data.platforms[0];
            float targetY = stage.Data.height * 0.5f;
            foreach (var p in stage.Data.platforms)
                if (Mathf.Abs(p.y - targetY) < Mathf.Abs(mid.y - targetY)) mid = p;

            player.Body.Teleport(mid.x + mid.width * 0.5f, mid.y);
            yield return Steps(240);   // 착지 + 카메라 lerp 수렴

            Assert.IsTrue(player.Body.grounded, "중간 발판에 서 있지 않다");

            float viewportY = cam.WorldToViewportPoint(new Vector3(player.Body.X, player.Body.Y, 0f)).y;
            Assert.AreEqual(1f - player.tuning.camViewportY, viewportY, 0.03f,
                $"플레이어가 화면 아래에서 {viewportY * 100f:F0}% 지점에 있다 (기대 42%)");
        }

        // ── 안기 3겹 (주인공 본체 → 강아지 → 앞팔) ──────────────────

        /// 강아지가 안길 때까지 떨어뜨린다.
        /// 공중으로 순간이동만 시키면 안 된다 — 안기 판정이
        /// "마지막으로 서 있던 높이보다 한참 아래"를 보기 때문에,
        /// 먼저 높은 발판에 실제로 서 있어야 한다.
        IEnumerator FallUntilHeld()
        {
            var high = stage.Data.platforms[0];
            foreach (var pl in stage.Data.platforms)
                if (pl.y > high.y && pl.y < stage.Data.groundY + 6f) high = pl;

            player.Body.Teleport(high.x + high.width * 0.5f, high.y);
            yield return Steps(40);                       // 착지해서 LastGroundedY 가 올라가도록
            Assert.IsTrue(player.Body.grounded, "높은 발판에 못 섰다");

            // 그 아래 허공에서 떨어뜨린다
            player.Body.Teleport(high.x + high.width * 0.5f, high.y - 2f);
            player.Body.grounded = false;

            for (int i = 0; i < 180; i++)
            {
                yield return new WaitForFixedUpdate();
                if (dog.IsClinging) yield break;
            }
            Assert.Fail("강아지가 안기지 않았다");
        }

        [UnityTest]
        public IEnumerator 안으면_세_겹이_모두_켜진다()
        {
            if (playerVisual == null) Assert.Ignore("주인공 스프라이트가 아직 없다");

            yield return FallUntilHeld();
            yield return null;   // PlayerVisual 은 LateUpdate 에서 돈다

            Assert.AreEqual("hold_fall", playerVisual.CurrentClip, "떨어지며 안은 클립이 아니다");
            Assert.IsTrue(playerVisual.overlay.enabled, "앞팔 오버레이가 꺼져 있다");
            Assert.IsNotNull(playerVisual.overlay.sprite, "앞팔 스프라이트가 비었다");
            Assert.Greater(playerVisual.dogRenderer.sortingOrder,
                           playerVisual.target.sortingOrder, "강아지가 주인공 뒤에 있다");
            Assert.Greater(playerVisual.overlay.sortingOrder,
                           playerVisual.dogRenderer.sortingOrder, "앞팔이 강아지 뒤에 있다");
        }

        [UnityTest]
        public IEnumerator 안았을_때_강아지가_지정된_오프셋에_붙는다()
        {
            if (playerVisual == null) Assert.Ignore("주인공 스프라이트가 아직 없다");

            yield return FallUntilHeld();
            yield return null;

            var clip = playerVisual.spriteSet.Find(playerVisual.CurrentClip);
            float sign = player.Body.face < 0 ? -1f : 1f;
            float wantX = player.Body.X + Px.U(clip.dogPivotOffsetPx.x) * sign;
            float wantY = player.Body.Y + Px.U(clip.dogPivotOffsetPx.y);

            Assert.AreEqual(wantX, dog.transform.position.x, 0.001f, "강아지 가로 위치가 어긋났다");
            Assert.AreEqual(wantY, dog.transform.position.y, 0.001f, "강아지 세로 위치가 어긋났다");
        }

        [UnityTest]
        public IEnumerator 왼쪽을_보면_세_겹이_같이_뒤집힌다()
        {
            if (playerVisual == null) Assert.Ignore("주인공 스프라이트가 아직 없다");

            yield return FallUntilHeld();
            player.Body.face = -1;
            yield return null;

            Assert.IsTrue(playerVisual.target.flipX, "본체가 안 뒤집혔다");
            Assert.IsTrue(playerVisual.overlay.flipX, "앞팔이 안 뒤집혔다");
            Assert.IsTrue(playerVisual.dogRenderer.flipX, "강아지가 안 뒤집혔다");

            // x 오프셋도 부호가 바뀌어야 한다 — 안 그러면 팔이 엉뚱한 데 붙는다
            var clip = playerVisual.spriteSet.Find(playerVisual.CurrentClip);
            float wantX = player.Body.X - Px.U(clip.dogPivotOffsetPx.x);
            Assert.AreEqual(wantX, dog.transform.position.x, 0.001f, "왼쪽인데 강아지가 오른쪽에 붙었다");
        }

        [UnityTest]
        public IEnumerator 카메라가_맵_바깥을_보여주지_않는다()
        {
            // 세로 화면(폰 비율)에서 맵 왼쪽 끝에 섰을 때가 가장 위험하다.
            cam.aspect = 375f / 812f;
            player.Body.Teleport(0.5f, stage.Data.groundY);
            yield return Steps(180);

            float halfW = cam.orthographicSize * cam.aspect;
            float left = cam.transform.position.x - halfW;
            float right = cam.transform.position.x + halfW;

            Assert.GreaterOrEqual(left, -0.001f, $"맵 왼쪽 바깥이 보인다 (left={left:F3})");
            Assert.LessOrEqual(right, player.tuning.MapWidthU + 0.001f,
                               $"맵 오른쪽 바깥이 보인다 (right={right:F3})");
        }

        /// 계측용. 통과/실패를 가리지 않고 숫자만 찍는다.
        /// 플레이어를 발판 따라 위로 옮기면서 강아지가 어떻게 따라오는지 본다.
        [UnityTest]
        public IEnumerator 진단_오르는동안_강아지_추적()
        {
            var plats = new System.Collections.Generic.List<StageData.Platform>(stage.Data.platforms);
            plats.Sort((a, b) => a.y.CompareTo(b.y));

            float maxGapY = 0f, maxGapX = 0f, sumGapY = 0f; int samples = 0;
            int clingCount = 0;
            bool wasCling = false;

            foreach (var pl in plats)
            {
                if (pl.y <= stage.Data.groundY) continue;
                player.Body.Teleport(pl.x + pl.width * 0.5f, pl.y);

                for (int i = 0; i < 90; i++)   // 발판당 1.5초
                {
                    yield return new WaitForFixedUpdate();
                    float gy = player.Body.Y - dog.transform.position.y;
                    float gx = Mathf.Abs(player.Body.X - dog.transform.position.x);
                    sumGapY += Mathf.Max(0f, gy); samples++;
                    if (gy > maxGapY) maxGapY = gy;
                    if (gx > maxGapX) maxGapX = gx;
                    if (dog.IsClinging && !wasCling) clingCount++;
                    wasCling = dog.IsClinging;
                }
            }

            Debug.Log($"[진단] 발판 {plats.Count - 1}개를 오르는 동안\n" +
                      $"  순간이동 {dog.TeleportCount}회 (화면 안이라 참은 것 {dog.SuppressedTeleportCount}회)\n" +
                      $"  세로 간격 평균 {sumGapY / samples * Px.PPU:F0}px / 최대 {maxGapY * Px.PPU:F0}px (순간이동 기준 {player.tuning.dogTeleportY}px)\n" +
                      $"  최대 가로 간격 {maxGapX * Px.PPU:F0}px (순간이동 기준 {player.tuning.dogTeleportX}px)\n" +
                      $"  안기 발동 {clingCount}회");
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator 강아지가_발판을_타고_올라온다()
        {
            // 플레이어를 몇 칸 위로 올려두고 내버려 두면, 강아지가 스스로 발판을 밟고 올라와야 한다.
            // 예전에는 제자리에서 헛뛰기만 해서 바닥에 남았다.
            var plats = new System.Collections.Generic.List<StageData.Platform>(stage.Data.platforms);
            plats.Sort((a, b) => a.y.CompareTo(b.y));
            var target = plats[3];   // 바닥 제외 세 번째 발판

            player.Body.Teleport(target.x + target.width * 0.5f, target.y);
            float dogStartY = dog.transform.position.y;

            for (int i = 0; i < 600; i++)   // 10초
            {
                yield return new WaitForFixedUpdate();
                player.Body.Teleport(target.x + target.width * 0.5f, target.y);  // 플레이어는 가만히 둔다
            }

            float climbed = dog.transform.position.y - dogStartY;
            Assert.Greater(climbed, 0.5f,
                $"강아지가 {climbed * Px.PPU:F0}px 밖에 못 올라왔다 (목표 발판은 {(target.y - dogStartY) * Px.PPU:F0}px 위)");
            Assert.Less(Mathf.Abs(dog.transform.position.y - target.y), 1.2f,
                "강아지가 플레이어가 선 발판 근처에 못 왔다");
        }

        [UnityTest]
        public IEnumerator 플레이어가_뛰면_강아지도_같이_뛴다()
        {
            // 예전에는 플레이어가 34px 위로 올라간 뒤에야 반응해서 한 박자 늦었다.
            yield return Steps(30);
            Assert.IsTrue(dog.GetComponent<CharacterBody>().grounded, "강아지가 땅에 있지 않다");

            int before = dog.SyncJumpCount;
            input.SetJump(true);
            yield return Steps(6);
            input.SetJump(false);

            Assert.Greater(dog.SyncJumpCount, before, "플레이어가 뛰었는데 강아지가 안 뛰었다");
        }

        [UnityTest]
        public IEnumerator 멀리_있으면_같이_뛰지_않는다()
        {
            // 화면 반대편에서 덩달아 뛰면 이상하다. 사거리 밖이면 제 갈 길을 가야 한다.
            var body = dog.GetComponent<CharacterBody>();
            yield return Steps(20);
            body.Teleport(player.Body.X + player.tuning.DogSyncJumpRangeU + 1f, stage.Data.groundY);
            body.grounded = true;
            yield return Steps(2);

            // 강아지에겐 "많이 뒤처지면 확률로 점프"가 따로 있어서 vy 로 보면 흔들린다.
            // 같이 뛴 횟수만 본다.
            int before = dog.SyncJumpCount;
            input.SetJump(true);
            yield return Steps(3);
            input.SetJump(false);

            Assert.AreEqual(before, dog.SyncJumpCount, "사거리 밖인데 덩달아 뛰었다");
        }

        [UnityTest]
        public IEnumerator 진단_카메라_클램프()
        {
            cam.aspect = 375f / 812f;                       // 폰 세로
            player.Body.Teleport(0.90f, stage.Data.groundY); // 씬 시작 위치
            yield return Steps(240);

            float halfW = cam.orthographicSize * cam.aspect;
            float min = halfW, max = player.tuning.MapWidthU - halfW;
            Debug.Log($"[진단-카메라] ortho={cam.orthographicSize:F3} aspect={cam.aspect:F4} halfW={halfW:F3}\n" +
                      $"  클램프 범위 [{min:F3}, {max:F3}]  플레이어 x={player.Body.X:F3}\n" +
                      $"  카메라 x={cam.transform.position.x:F3}  좌측 끝={cam.transform.position.x - halfW:F3}");
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator 시작하자마자_맵_바깥이_보이지_않는다()
        {
            // 카메라가 (0,0) 에서 lerp 로 들어오면 첫 순간 맵 밖이 보인다.
            cam.aspect = 375f / 812f;
            yield return null;          // 첫 LateUpdate 직후

            float halfW = cam.orthographicSize * cam.aspect;
            Assert.GreaterOrEqual(cam.transform.position.x - halfW, -0.001f,
                $"시작 프레임에 맵 왼쪽 바깥이 보인다 (left={cam.transform.position.x - halfW:F3})");
        }

        [UnityTest]
        public IEnumerator 공중에서는_다시_뛸_수_없다()
        {
            yield return Steps(30);
            Assert.IsTrue(player.Body.grounded, "시작할 때 땅에 있지 않다");

            input.SetJump(true);
            yield return Steps(6);
            input.SetJump(false);
            yield return Steps(6);
            Assert.IsFalse(player.Body.grounded, "점프가 안 됐다");

            // 올라가는 중에 다시 눌러도 속도가 늘면 안 된다
            float vyBefore = player.Body.vy;
            input.SetJump(true);
            yield return new WaitForFixedUpdate();
            input.SetJump(false);

            Assert.LessOrEqual(player.Body.vy, vyBefore + 0.001f,
                $"공중에서 다시 뛰었다 (vy {vyBefore:F2} -> {player.Body.vy:F2})");
        }

        /// 발판을 하나씩 짚어가며 강아지가 따라 올라오는지 전수 확인한다.
        /// 한 곳이라도 막히면 실패다 — 예전엔 16개 중 8개에서 무한 점프에 갇혔다.
        [UnityTest]
        public IEnumerator 강아지가_모든_발판을_따라_올라온다()
        {
            var plats = new System.Collections.Generic.List<StageData.Platform>(stage.Data.platforms);
            plats.Sort((a, b) => a.y.CompareTo(b.y));

            var body = dog.GetComponent<CharacterBody>();
            var log = new System.Text.StringBuilder("[진단-도달] 발판별 (플레이어를 올려두고 4초 대기)\n");
            int stuck = 0;

            for (int n = 1; n < plats.Count; n++)
            {
                var target = plats[n];
                player.Body.Teleport(target.x + target.width * 0.5f, target.y);

                int jumps = 0; bool wasGrounded = body.grounded;
                for (int i = 0; i < 240; i++)
                {
                    yield return new WaitForFixedUpdate();
                    player.Body.Teleport(target.x + target.width * 0.5f, target.y);
                    if (wasGrounded && !body.grounded) jumps++;
                    wasGrounded = body.grounded;
                }

                float gap = (target.y - body.Y) * Px.PPU;
                bool ok = gap < 120f;              // 발판 한 칸 안쪽이면 따라온 것으로 본다
                if (!ok) stuck++;
                log.AppendLine($"  #{n,2} y={target.y:F2} → 남은 {gap,5:F0}px, 점프 {jumps,2}회" +
                               (ok ? "" : $"  <-- 막힘 | 개 x={body.X:F2} y={body.Y:F2} 접지={body.grounded}" +
                                          $" | 목표 {(dog.HasStep ? $"y={dog.StepY:F2} x={dog.StepAimX:F2} (올라야 할 높이 {(dog.StepY - body.Y) * Px.PPU:F0}px)" : "없음")}"));
            }

            log.AppendLine($"  못 올라온 발판: {stuck} / {plats.Count - 1}");
            Debug.Log(log.ToString());
            Assert.AreEqual(0, stuck, $"강아지가 못 올라온 발판이 {stuck}개 있다. 위 로그 참고.");
        }
    }
}
