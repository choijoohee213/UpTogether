using UnityEngine;

namespace UpTogether
{
    /// 원본의 수동 lerp 카메라. Cinemachine 대신 직접 짠 이유는
    /// 프로토타입의 추적 감각(lerp 0.12, 세로 58%)을 그대로 재현하기 위해서다.
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        public Tuning tuning;
        public StageRunner stage;
        public PlayerController player;

        Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (stage == null || stage.Data == null || player == null) enabled = false;
        }

        void LateUpdate()
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // 원본은 화면 위에서 58% 지점에 플레이어를 둔다 → 아래에서 42%
            float fromBottom = 1f - tuning.camViewportY;
            float targetY = player.Body.Y + (0.5f - fromBottom) * (halfH * 2f);
            float targetX = player.Body.X;

            // 맵 밖이 보이지 않게 가둔다 (원본의 clamp와 같은 범위)
            targetX = ClampRange(targetX, halfW, tuning.MapWidthU - halfW);
            targetY = ClampRange(targetY, -Px.U(120f) + halfH, stage.Data.height + Px.U(40f) - halfH);

            var pos = transform.position;
            float k = Px.Smoothing(tuning.camFollow, Time.deltaTime);
            pos.x += (targetX - pos.x) * k;
            pos.y += (targetY - pos.y) * k;

            // 크게 떨어졌을 때만 흔든다
            float s = player.ShakeAmount;
            if (s > 0.0001f)
            {
                pos.x += (Random.value - 0.5f) * s;
                pos.y += (Random.value - 0.5f) * s;
            }

            transform.position = pos;
        }

        /// 맵이 화면보다 좁으면 min>max가 된다. 그 땐 가운데로.
        static float ClampRange(float v, float min, float max)
            => min > max ? (min + max) * 0.5f : Mathf.Clamp(v, min, max);
    }
}
