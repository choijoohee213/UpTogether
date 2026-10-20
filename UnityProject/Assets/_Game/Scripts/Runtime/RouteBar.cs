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
        public Image[] dots;            // 체크포인트 (왼쪽부터)
        /// 고른 견종의 '걷기' 동작만 돌린다. 실제 강아지를 그대로 비추면
        /// 점프·낙하·안긴 자세까지 따라와 길 위에서 어수선하다.
        public DogVisual dogVisual;
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

            // 걷기 동작만 돌린다 (견종은 고른 것을 그대로 따라간다)
            if (walkerImage == null || dogVisual == null || dogVisual.spriteSet == null) return;
            var clip = dogVisual.spriteSet.Find("walk");
            if (clip == null || clip.Length == 0) return;
            int f = Mathf.FloorToInt(Time.unscaledTime / 0.13f) % clip.Length;
            var sp = dogVisual.spriteSet.Frame(clip.frames[f]);
            if (sp != null) walkerImage.sprite = sp;
        }
    }
}
