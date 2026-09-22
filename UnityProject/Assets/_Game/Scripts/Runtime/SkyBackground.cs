using UnityEngine;

namespace UpTogether
{
    /// 화면을 채우는 하늘 그라데이션. 올라갈수록 옅고 밝아진다.
    /// 원본: hsl(206-p*16, 64-p*16%, 70+p*12%) → hsl(199, 62%, 84+p*6%)
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkyBackground : MonoBehaviour
    {
        const int Steps = 64;   // 세로 그라데이션 해상도. 1px 폭이라 비용은 없다시피 하다

        public StageRunner stage;

        Camera cam;
        SpriteRenderer sr;
        Texture2D tex;
        Color[] pixels;
        float lastProgress = -1f;

        void Awake()
        {
            cam = Camera.main;
            sr = GetComponent<SpriteRenderer>();
            sr.sortingOrder = -1000;

            tex = new Texture2D(1, Steps, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            pixels = new Color[Steps];
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, Steps), new Vector2(0.5f, 0.5f), 1f);
        }

        void LateUpdate()
        {
            if (cam == null || stage == null || stage.Data == null) return;

            // 카메라를 따라다니며 화면을 덮는다. 세로로 조금 여유를 준다.
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 10f);
            transform.localScale = new Vector3(w * 1.05f, h * 1.05f, 1f);

            // 0 = 바닥, 1 = 꼭대기
            float top = Mathf.Max(1f, stage.Data.height - h);
            float progress = Mathf.Clamp01((cam.transform.position.y - h * 0.5f) / top);
            if (Mathf.Abs(progress - lastProgress) < 0.004f) return;
            lastProgress = progress;

            Color hi = Hsl(206f - progress * 16f, 0.64f - progress * 0.16f, 0.70f + progress * 0.12f);
            Color lo = Hsl(199f, 0.62f, 0.84f + progress * 0.06f);

            for (int i = 0; i < Steps; i++)
                pixels[i] = Color.Lerp(lo, hi, i / (Steps - 1f));   // 0번이 아래쪽
            tex.SetPixels(pixels);
            tex.Apply();
        }

        /// Unity에는 HSV만 있어서 HSL은 직접 변환한다. h는 도(0~360), s·l은 0~1.
        static Color Hsl(float h, float s, float l)
        {
            float c = (1f - Mathf.Abs(2f * l - 1f)) * s;
            float hp = Mathf.Repeat(h, 360f) / 60f;
            float x = c * (1f - Mathf.Abs(hp % 2f - 1f));
            float r = 0f, g = 0f, b = 0f;
            switch (Mathf.FloorToInt(hp))
            {
                case 0: r = c; g = x; break;
                case 1: r = x; g = c; break;
                case 2: g = c; b = x; break;
                case 3: g = x; b = c; break;
                case 4: r = x; b = c; break;
                default: r = c; b = x; break;
            }
            float m = l - c * 0.5f;
            return new Color(r + m, g + m, b + m);
        }
    }
}
