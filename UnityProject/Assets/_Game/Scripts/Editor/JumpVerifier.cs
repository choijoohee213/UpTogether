using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 이식된 물리가 프로토타입과 같은 점프 높이를 내는지 확인한다.
    /// 기준값은 원본 climb-dog2.html 을 60fps로 돌려 측정한 것이다.
    public static class JumpVerifier
    {
        const float ExpectedTapPx = 66.60f;   // 누르자마자 뗌
        const float ExpectedHoldPx = 146.21f; // 0.30초 이상 홀드
        const float TolerancePx = 0.05f;

        [MenuItem("UpTogether/Verify Jump Heights")]
        public static void Verify()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Game/Tuning.asset");
            if (tuning == null)
            {
                Debug.LogError("Assets/_Game/Tuning.asset 이 없습니다. UpTogether ▸ Build Play Scene 을 먼저 실행하세요.");
                return;
            }

            // 바닥만 있는 스테이지로 잰다. 실제 스테이지를 쓰면 올라가다 위 발판에 걸려
            // 측정이 잘린다 — 재는 건 점프 높이지 지형이 아니다.
            var stageData = ScriptableObject.CreateInstance<StageData>();
            stageData.groundY = 0.40f;
            stageData.height = 20f;
            stageData.platforms = new[] { new StageData.Platform { x = 0f, y = 0.40f, width = tuning.MapWidthU } };
            stageData.movers = new StageData.Mover[0];

            var log = new StringBuilder("점프 높이 검증 (기준: 원본 프로토타입 60fps 실측)\n");
            bool ok = true;

            ok &= Check(tuning, stageData, holdFrames: 0,   ExpectedTapPx,  "누르자마자 뗌", log);
            ok &= Check(tuning, stageData, holdFrames: 999, ExpectedHoldPx, "최대 홀드",     log);

            // 참고용 — 중간 구간도 같이 찍어둔다
            foreach (int hf in new[] { 1, 5, 10, 18 })
                log.AppendLine($"  (참고) {hf}프레임 홀드 → {Measure(tuning, stageData, hf):F2}px");

            if (ok) Debug.Log(log.ToString());
            else Debug.LogError(log.ToString());
        }

        static bool Check(Tuning t, StageData s, int holdFrames, float expected, string label, StringBuilder log)
        {
            float got = Measure(t, s, holdFrames);
            float diff = Mathf.Abs(got - expected);
            bool pass = diff <= TolerancePx;
            log.AppendLine($"  [{(pass ? "통과" : "실패")}] {label}: {got:F2}px (기준 {expected:F2}px, 차이 {diff:F3})");
            return pass;
        }

        /// 상승 높이(px). 점프해서 다시 착지할 때까지 돌린다.
        static float Measure(Tuning tuning, StageData stageData, int holdFrames)
        {
            var stageGo = new GameObject("~verify_stage");
            var playerGo = new GameObject("~verify_player");
            try
            {
                var runner = stageGo.AddComponent<StageRunner>();
                var root = new GameObject("Platforms").transform;
                root.SetParent(stageGo.transform, false);
                runner.platformRoot = root;
                runner.Load(stageData);

                var bodyC = playerGo.AddComponent<CharacterBody>();
                var player = playerGo.AddComponent<PlayerController>();
                player.tuning = tuning;
                player.stage = runner;
                bodyC.tuning = tuning;
                bodyC.Bind(runner);

                // 바닥 한가운데에서 시작. 벽이나 발판 모서리에 걸리지 않는 자리다.
                playerGo.transform.position = new Vector3(tuning.MapWidthU * 0.5f, stageData.groundY, 0f);
                bodyC.grounded = true;

                float dt = 1f / Px.FPS;
                float startY = bodyC.Y;
                float peak = startY;

                for (int f = 0; f < 400; f++)
                {
                    player.Tick(dt, 0, jumpPressed: f == 0, jumpHeld: f < holdFrames);
                    if (bodyC.Y > peak) peak = bodyC.Y;
                    if (f > 0 && bodyC.grounded) break;
                }
                return (peak - startY) * Px.PPU;
            }
            finally
            {
                Object.DestroyImmediate(playerGo);
                Object.DestroyImmediate(stageGo);
            }
        }
    }
}
