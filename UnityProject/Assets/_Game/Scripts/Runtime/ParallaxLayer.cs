using UnityEngine;

namespace UpTogether
{
    /// 카메라보다 느리게 흐르는 배경 조각.
    /// 원본은 그릴 때 camX*0.4 처럼 깎아서 썼다. 여기서는 월드 위치를 카메라에 끌려가게 해서
    /// 같은 결과를 낸다 — 카메라 기준 상대 위치가 base - cam*factor 가 된다.
    public class ParallaxLayer : MonoBehaviour
    {
        public Vector2 basePosition;
        [Tooltip("0 = 카메라와 같이 움직임(멀리), 1 = 고정(가까이). 원본의 camX 계수와 같다.")]
        public Vector2 factor = new Vector2(0.4f, 0.6f);

        Camera cam;

        void Awake() => cam = Camera.main;

        void LateUpdate()
        {
            if (cam == null) return;
            var c = cam.transform.position;
            transform.position = new Vector3(
                basePosition.x + c.x * (1f - factor.x),
                basePosition.y + c.y * (1f - factor.y),
                transform.position.z);
        }
    }
}
