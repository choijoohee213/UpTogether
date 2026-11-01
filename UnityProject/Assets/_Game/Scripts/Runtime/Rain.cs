using UnityEngine;

namespace UpTogether
{
    /// 프롤로그에 내리는 비. 그림에는 비가 없다 — 그리면 컷마다 다시 그려야 하고
    /// 멈춰 있는 비는 비처럼 안 보인다. 그래서 코드로 뿌린다.
    ///
    /// 카메라가 컷마다 줌을 크게 바꾸므로(0.7 ~ 1.9배) 뿜는 자리를 고정하면
    /// 넓은 컷에서는 화면을 다 못 덮고 좁은 컷에서는 한참 위에서 떨어진다.
    /// 매 프레임 지금 시야에 맞춰 띠를 다시 재운다.
    [RequireComponent(typeof(ParticleSystem))]
    public class Rain : MonoBehaviour
    {
        public Camera cam;
        /// 시야 위로 이만큼 띄워서 뿜는다
        public float above = 0.6f;

        /// 화면에 보이는 빗줄기 굵기·길이 (게임 px). 줌과 무관하게 이만큼 유지한다.
        const float WidthPx = 2f;
        const float LengthPx = 34f;
        /// 한 화면에 보일 빗줄기 수
        const float DropsOnScreen = 160f;
        /// 옆으로 밀리는 속도 (velocityOverLifetime.x 의 크기)
        const float SideSpeed = 2.1f;
        /// 시야 650px 일 때 1 유닛 = 100px 이므로 반높이 1 유닛당 325px 이다
        const float PxPerHalfUnit = 325f;

        ParticleSystem ps;
        ParticleSystem.ShapeModule shape;
        ParticleSystemRenderer rend;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            shape = ps.shape;
            rend = GetComponent<ParticleSystemRenderer>();
            if (cam == null) cam = Camera.main;
        }

        void LateUpdate()
        {
            if (cam == null) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // 카메라 바로 위에 가로로 긴 띠를 두고 거기서 떨어뜨린다
            transform.position = new Vector3(cam.transform.position.x,
                                             cam.transform.position.y + halfH + above,
                                             0f);

            // 굵기·길이는 월드 크기라서 그냥 두면 줌에 따라 2.7배씩 달라진다.
            // 당겨 본 컷에서 빗줄기가 손가락만 해지므로 시야에 맞춰 줄인다.
            var main = ps.main;
            float unitsPerPx = halfH / PxPerHalfUnit;
            float speed = Mathf.Max(1f, main.startSpeed.constant);
            main.startSize = WidthPx * unitsPerPx;
            if (rend != null) rend.velocityScale = LengthPx * unitsPerPx / speed;

            // 화면을 가로지를 만큼만 살려 둔다. 수명을 길게 두면 대부분이
            // 화면 한참 아래에서 떨어지고 있어서, 양을 올려도 화면은 안 찬다.
            float life = (halfH * 2f + above + 1f) / speed;
            main.startLifetime = life;

            // 비스듬히 내리므로 수명 동안 옆으로 밀린 만큼 넓게 뿜어야
            // 바람이 불어오는 쪽 가장자리가 비지 않는다
            float margin = SideSpeed * life + 0.3f;
            shape.scale = new Vector3((halfW + margin) * 2f, 0.1f, 1f);

            // 보이는 빗줄기 수를 일정하게 — 시야가 바뀌어도 같은 빗속이어야 한다.
            // 뿜는 띠가 화면보다 넓으니 그 비율만큼 더 뿜어야 화면이 찬다.
            var emission = ps.emission;
            emission.rateOverTime = DropsOnScreen / life * (halfW + margin) / Mathf.Max(0.1f, halfW);
        }

        /// 컷마다 켜고 끈다 (마지막 컷은 비가 그친 하늘이라 끈다).
        public void SetFalling(bool on)
        {
            if (ps == null) ps = GetComponent<ParticleSystem>();
            var emission = ps.emission;
            emission.enabled = on;
        }
    }
}
