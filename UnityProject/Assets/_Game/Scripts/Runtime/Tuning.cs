using UnityEngine;

namespace UpTogether
{
    /// PROJECT.md "2. 검증된 수치"를 그대로 담는다.
    /// 인스펙터에는 문서와 같은 px/frame 값을 노출하고, 코드는 변환된 프로퍼티만 쓴다.
    /// 실기기에서 다시 만져야 하므로 값은 전부 여기 모아둔다.
    [CreateAssetMenu(menuName = "UpTogether/Tuning", fileName = "Tuning")]
    public class Tuning : ScriptableObject
    {
        [Header("플레이어 물리 (px/frame @60fps)")]
        public float gravity = 0.62f;
        public float holdGravity = 0.23f;
        public float holdMaxSeconds = 0.30f;
        public float jump1 = 9.4f;
        public float jump2 = 8.4f;
        public float maxFall = 17f;
        public float walkSpeed = 3.6f;
        [Tooltip("목표 속도로 가는 프레임당 lerp 계수")]
        public float walkAccel = 0.34f;

        [Header("맵 (px)")]
        public float mapWidth = 1250f;
        [Tooltip("좌우 벽에서 몸이 멈추는 여유")]
        public float wallMargin = 16f;
        [Tooltip("발판 좌우로 발이 걸리는 여유")]
        public float platformGrabMargin = 6f;

        [Header("카메라")]
        [Tooltip("화면 세로에 담을 월드 높이(px). 프로토타입은 800이었는데 캐릭터가 화면의 6%라 작아 보인다. " +
                 "줄이면 캐릭터가 커지고 위를 덜 보게 된다. 발판 세로 간격이 68~102px 이라 " +
                 "너무 줄이면 다음 발판이 안 보인다.")]
        public float cameraViewHeightPx = 500f;
        public float camFollow = 0.12f;
        [Tooltip("플레이어를 화면 세로 몇 % 지점에 둘지")]
        public float camViewportY = 0.58f;

        [Header("강아지 AI")]
        public float dogSpeed = 3.3f;
        public float dogTrailDistance = 26f;
        public float dogJumpTrigger = 34f;
        public float dogJumpSpeed = 12.2f;
        public float dogTeleportX = 640f;
        public float dogTeleportY = 460f;
        [Tooltip("플레이어가 점프할 때 강아지도 같이 뛰는 가로 거리(px). 이보다 멀면 무시하고 제 갈 길 간다.")]
        public float dogSyncJumpRange = 170f;
        public float dogClingFallSpeed = 7f;
        public float dogClingLerp = 0.35f;
        public float dogClingOffsetX = 10f;
        public float dogClingOffsetY = 24f;

        // ── 변환값 (읽기 전용) ──────────────────────────────
        public float GravityA        => Px.A(gravity);
        public float HoldGravityA    => Px.A(holdGravity);
        public float Jump1V          => Px.V(jump1);
        public float Jump2V          => Px.V(jump2);
        public float MaxFallV        => Px.V(maxFall);
        public float WalkSpeedV      => Px.V(walkSpeed);
        public float CameraOrthoSize => Px.U(cameraViewHeightPx) * 0.5f;
        public float MapWidthU       => Px.U(mapWidth);
        public float WallMarginU     => Px.U(wallMargin);
        public float GrabMarginU     => Px.U(platformGrabMargin);
        public float DogSpeedV       => Px.V(dogSpeed);
        public float DogTrailU       => Px.U(dogTrailDistance);
        public float DogJumpTrigU    => Px.U(dogJumpTrigger);
        public float DogJumpV        => Px.V(dogJumpSpeed);
        public float DogTeleportXU   => Px.U(dogTeleportX);
        public float DogTeleportYU   => Px.U(dogTeleportY);
        public float DogSyncJumpRangeU => Px.U(dogSyncJumpRange);
        public float DogClingFallV   => Px.V(dogClingFallSpeed);
        public float DogClingOffXU   => Px.U(dogClingOffsetX);
        public float DogClingOffYU   => Px.U(dogClingOffsetY);
    }
}
