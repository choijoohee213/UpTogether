using UnityEngine;

namespace UpTogether
{
    /// 구름과 언덕. 시차 계수는 원본 그대로(구름 x0.4/y0.6, 언덕 x0.68/y0.9)지만
    /// 배치는 스테이지 높이에 고루 퍼지게 단순화했다.
    /// 원본의 배치 수식은 맵 높이·화면 크기에 얽혀 있어서 그대로 옮기면
    /// 카메라 설정이 바뀔 때마다 구름이 화면 밖으로 사라진다.
    public class Backdrop : MonoBehaviour
    {
        const int CloudCount = 12;
        const int HillCount = 6;

        public StageRunner stage;

        void Start()
        {
            if (stage == null || stage.Data == null) return;

            float mapW = (float)StageGenerator.MapWidth / Px.PPU;
            float top = stage.Data.height;

            // 구름 — 맵 전체 높이에 퍼뜨린다
            for (int i = 0; i < CloudCount; i++)
            {
                float x = (i * 317f % (float)StageGenerator.MapWidth) / Px.PPU;
                float y = Mathf.Lerp(2f, top * 0.95f, i / (CloudCount - 1f));
                float scale = 0.8f + (i % 3) * 0.35f;      // 크기를 섞어 깊이를 준다
                Spawn($"Cloud{i}", ProceduralArt.Cloud, new Vector2(x, y),
                      new Vector2(0.4f, 0.6f), -900,
                      new Color(1f, 1f, 1f, 0.72f), scale);
            }

            // 언덕 — 바닥 근처 지평선. 올라가면 자연스럽게 뒤로 사라진다.
            for (int i = 0; i < HillCount; i++)
            {
                float x = i * (mapW / (HillCount - 1f));
                Spawn($"Hill{i}", ProceduralArt.Hill,
                      new Vector2(x, stage.Data.groundY - Px.U(60f)),
                      new Vector2(0.68f, 0.9f), -800, Color.white,
                      1f + (i % 2) * 0.25f);
            }
        }

        void Spawn(string name, Sprite sprite, Vector2 basePos, Vector2 factor,
                   int order, Color color, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;

            var p = go.AddComponent<ParallaxLayer>();
            p.basePosition = basePos;
            p.factor = factor;
        }
    }
}
