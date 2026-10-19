using UnityEngine;
using UnityEngine.UI;

namespace UpTogether
{
    /// 화면 위쪽의 '길' — 시작점, 체크포인트들, 지금 어디쯤인지, 도착점(강아지집).
    /// 세로로 오르는 게임이라 높이가 감이 안 오는데, 한 줄로 펴서 보여준다.
    [DefaultExecutionOrder(60)]
    public class RouteBar : MonoBehaviour
    {
        public Zones zones;
        public RectTransform rail;      // 길 (가로 선)
        public RectTransform walker;    // 지금 위치 표시
        public Image walkerImage;       // 거기 그려지는 그림
        /// 실제 강아지의 스프라이트를 그대로 가져다 쓴다 — 고른 견종이 그대로 나온다
        public Image[] dots;            // 체크포인트 (왼쪽부터)
        public SpriteRenderer dogSource;
        public Sprite dotPassed, dotAhead;

        [Tooltip("표시가 길 끝에 딱 붙지 않게 두는 여백(px)")]
        public float inset = 18f;

        float shown;                    // 부드럽게 따라가는 현재 위치

        void Update()
        {
            if (zones == null || rail == null) return;

            float w = rail.rect.width - inset * 2f;
            int n = zones.ZoneCount;

            // 체크포인트 점 — 지나온 것은 꽉 찬 하트, 아직인 것은 빈 하트
            for (int i = 0; i < dots.Length; i++)
            {
                bool use = i < n;
                dots[i].gameObject.SetActive(use);
                if (!use) continue;
                var rt = (RectTransform)dots[i].transform;
                rt.anchoredPosition = new Vector2(inset + w * zones.ZoneAt(i), 0f);
                bool passed = i <= zones.Zone;
                if (dotPassed != null && dotAhead != null)
                    dots[i].sprite = passed ? dotPassed : dotAhead;
                dots[i].color = passed ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }

            // 지금 위치 — 부드럽게 따라간다 (위아래로 까딱이지는 않는다)
            shown = Mathf.Lerp(shown, zones.Progress01, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            if (walker != null)
                walker.anchoredPosition = new Vector2(inset + w * shown, 0f);

            // 실제 강아지 그림을 그대로 비춘다 (견종도, 걷는 동작도 따라온다)
            if (walkerImage != null && dogSource != null && dogSource.sprite != null)
                walkerImage.sprite = dogSource.sprite;
        }
    }
}
