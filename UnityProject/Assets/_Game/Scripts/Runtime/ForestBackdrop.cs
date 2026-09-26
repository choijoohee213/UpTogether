using UnityEngine;

namespace UpTogether
{
    /// 숲 테마 배경 3겹.
    /// 하늘은 화면에 고정, 먼 숲·가까운 숲은 지평선에 두고 시차로 느리게 흐른다.
    /// 세로 등반이라 올라가면 숲 위 하늘로 자연스럽게 올라간다.
    public class ForestBackdrop : MonoBehaviour
    {
        public StageRunner stage;
        public Sprite sky, far, mid;

        void Start()
        {
            if (stage == null || stage.Data == null) return;
            float g = stage.Data.groundY;

            MakeSky(sky, -1000);
            MakeBand("Far", far, g + 1.4f, new Vector2(0.50f, 0.90f), -850);
            MakeBand("Mid", mid, g + 1.0f, new Vector2(0.72f, 0.85f), -820);
        }

        void MakeSky(Sprite s, int order)
        {
            if (s == null) return;
            var go = new GameObject("Sky");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.drawMode = SpriteDrawMode.Tiled;           // 가로로 넓게 덮는다
            float h = s.rect.height / s.pixelsPerUnit;    // 하늘 한 장 높이
            sr.size = new Vector2(40f, h);
            sr.sortingOrder = order;
            // 크림색 지평선(스프라이트 아래)이 땅 높이에 오게 앵커. 위로 파란 하늘.
            // 거의 고정(먼 배경)이라 올라가면 그 위 카메라 배경색으로 자연히 이어진다.
            var p = go.AddComponent<ParallaxLayer>();
            p.basePosition = new Vector2((float)StageGenerator.MapWidth * 0.5f / Px.PPU,
                                         stage.Data.groundY + h * 0.5f - 1.5f);
            p.factor = new Vector2(0.9f, 0.97f);
        }

        void MakeBand(string name, Sprite s, float y, Vector2 factor, int order)
        {
            if (s == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.drawMode = SpriteDrawMode.Tiled;          // 가로로 이음매 없이 반복
            sr.size = new Vector2(30f, s.rect.height / s.pixelsPerUnit);
            sr.sortingOrder = order;
            var p = go.AddComponent<ParallaxLayer>();
            p.basePosition = new Vector2((float)StageGenerator.MapWidth * 0.5f / Px.PPU, y);
            p.factor = factor;
        }
    }
}
