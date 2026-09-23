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
        public float maxFall = 17f;
        public float walkSpeed = 3.6f;
        [Tooltip("목표 속도로 가는 프레임당 lerp 계수")]
        public float walkAccel = 0.34f;

        [Header("맵 (px)")]
        [Tooltip("StageGenerator.MapWidth 와 같아야 한다. 어긋나면 벽과 카메라 한계가 발판과 안 맞는다.")]
        public float mapWidth = 900f;
        [Tooltip("좌우 벽에서 몸이 멈추는 여유")]
        public float wallMargin = 16f;
        [Tooltip("발판 좌우로 발이 걸리는 여유")]
        public float platformGrabMargin = 6f;

        [Header("카메라")]
        [Tooltip("화면 세로에 담을 월드 높이(px). 프로토타입은 800이었는데 캐릭터가 화면의 6%라 작아 보인다. " +
                 "줄이면 캐릭터가 커지고 위를 덜 보게 된다. 발판 세로 간격이 68~102px 이라 " +
                 "너무 줄이면 다음 발판이 안 보인다.")]
        public float cameraViewHeightPx = 650f;
        public float camFollow = 0.12f;
        [Tooltip("플레이어를 화면 세로 몇 % 지점에 둘지")]
        public float camViewportY = 0.58f;

        [Header("강아지 AI")]
        [Tooltip("걷기 속도(px/frame). 플레이어(3.6)보다 느리면 계속 뒤처져서 " +
                 "발판 끝이 아니라 가운데에서 뛰게 되고, 가로 거리가 모자라 못 닿는다.")]
        public float dogSpeed = 4.2f;
        [Tooltip("공중에서의 가로 속도(px/frame). 강아지는 플레이어처럼 발판 끝에서 뛰지 못하고 " +
                 "뒤(가운데)에서 뛰게 된다. 가운데에서 다음 발판까지는 최대 153px 이라 " +
                 "이 속도가 낮으면 늘 못 닿고 떨어진다. 9.0 이면 약 173px 을 간다.")]
        public float dogAirSpeed = 9.0f;
        public float dogTrailDistance = 26f;
        public float dogJumpTrigger = 34f;
        public float dogJumpSpeed = 12.2f;
        public float dogTeleportX = 640f;
        [Tooltip("한 번 뛰어 닿는 높이보다 이만큼(px) 더 벌어지면 바로 따라붙는다. " +
                 "강아지는 '지금 칸에서 다음 칸'만 스스로 오르고, 그보다 멀어지면 " +
                 "한 칸씩 기어오르지 않고 그냥 쫓아온다.")]
        public float dogCatchUpBuffer = 12f;
        [Tooltip("플레이어가 점프할 때 강아지도 같이 뛰는 가로 거리(px). 이보다 멀면 무시하고 제 갈 길 간다.")]
        public float dogSyncJumpRange = 170f;
        public float dogClingFallSpeed = 7f;
        [Tooltip("마지막으로 서 있던 높이보다 이만큼(px) 아래로 내려가야 안는다. " +
                 "없으면 높이 뛰었다 내려오는 것만으로도 안겨버린다. " +
                 "발판 한 칸(68~102px) 정도 미끄러지면 안기는 값이다. 올리면 더 크게 떨어져야 안는다.")]
        public float dogClingMinDrop = 70f;
        public float dogClingLerp = 0.35f;
        public float dogClingOffsetX = 10f;
        public float dogClingOffsetY = 24f;

        // ── 변환값 (읽기 전용) ──────────────────────────────
        public float GravityA        => Px.A(gravity);
        public float HoldGravityA    => Px.A(holdGravity);
        public float Jump1V          => Px.V(jump1);
        public float MaxFallV        => Px.V(maxFall);
        public float WalkSpeedV      => Px.V(walkSpeed);
        public float CameraOrthoSize => Px.U(cameraViewHeightPx) * 0.5f;
        public float MapWidthU       => Px.U(mapWidth);
        public float WallMarginU     => Px.U(wallMargin);
        public float GrabMarginU     => Px.U(platformGrabMargin);
        public float DogSpeedV       => Px.V(dogSpeed);
        public float DogAirSpeedV    => Px.V(dogAirSpeed);
        public float DogTrailU       => Px.U(dogTrailDistance);
        public float DogJumpTrigU    => Px.U(dogJumpTrigger);
        public float DogJumpV        => Px.V(dogJumpSpeed);
        public float DogTeleportXU   => Px.U(dogTeleportX);
        public float DogCatchUpBufferU => Px.U(dogCatchUpBuffer);
        public float DogSyncJumpRangeU => Px.U(dogSyncJumpRange);
        public float DogClingFallV   => Px.V(dogClingFallSpeed);
        public float DogClingMinDropU => Px.U(dogClingMinDrop);
        public float DogClingOffXU   => Px.U(dogClingOffsetX);
        public float DogClingOffYU   => Px.U(dogClingOffsetY);
    }
}
