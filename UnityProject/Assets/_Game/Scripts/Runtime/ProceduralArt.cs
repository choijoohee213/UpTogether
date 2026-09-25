using UnityEngine;

namespace UpTogether
{
    /// 배경·발판·파티클용 스프라이트를 코드로 만든다.
    /// 텍스처 1px = 프로토타입 1px (PPU 100)이라 원본 좌표를 그대로 쓸 수 있다.
    ///
    /// ★ 캐릭터는 여기서 만들지 않는다 ★
    /// PROJECT.md 5절: 캐릭터는 이미지 에셋으로 간다. 코드로 그린 벡터는 티가 난다.
    /// 여기 있는 건 전부 배경과 지형이다.
    public static class ProceduralArt
    {
        // 프로토타입 색
        public static readonly Color Dirt  = Rgb(0xC8, 0x9B, 0x63);
        public static readonly Color Grass = Rgb(0x8F, 0xCB, 0x6B);
        public static readonly Color HillGreen = Rgb(0xA8, 0xD8, 0xBC);

        static Sprite square, circle, cloud, hill, shadow;

        /// 단색 사각형. PPU 1이라 localScale이 곧 월드 크기가 된다.
        public static Sprite Square => square ??= Make(1, 1, (x, y) => 1f, Color.white, 0.5f, 1f);
        public static Sprite Circle => circle ??= MakeCircle(32);
        public static Sprite Cloud  => cloud  ??= MakeCloud();
        public static Sprite Hill   => hill   ??= MakeHill();
        public static Sprite Shadow => shadow ??= MakeShadow(120, 16);

        static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        /// coverage(x,y) → 0~1 알파. 경계에서 부드럽게 깎아 계단을 없앤다.
        static Sprite Make(int w, int h, System.Func<float, float, float> coverage, Color color,
                           float pivotY = 0.5f, float ppu = Px.PPU)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = Mathf.Clamp01(coverage(x + 0.5f, y + 0.5f));
                    px[y * w + x] = new Color(color.r, color.g, color.b, color.a * a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, pivotY), ppu);
        }

        static Sprite MakeCircle(int size)
        {
            float r = size * 0.5f;
            return Make(size, size, (x, y) =>
            {
                float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                return r - d + 0.5f;   // 경계 1px 안티에일리어싱
            }, Color.white);
        }

        /// 원본: arc(0,0,21) + arc(24,6,15) + arc(-24,7,13) 세 개를 겹친 구름
        static Sprite MakeCloud()
        {
            const int W = 88, H = 52;
            float cx = W * 0.5f, cy = H * 0.5f - 4f;
            (float x, float y, float r)[] blobs = { (0, 0, 21), (24, -6, 15), (-24, -7, 13) };
            return Make(W, H, (x, y) =>
            {
                float best = 0f;
                foreach (var b in blobs)
                {
                    float dx = x - (cx + b.x), dy = y - (cy + b.y);
                    best = Mathf.Max(best, b.r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                }
                return best;
            }, Color.white);
        }

        /// 원본: moveTo(-150,80) → quadraticCurveTo(0,-110) → (150,80) 을 채운 언덕.
        /// 꼭대기는 실제로 y=-15 지점이다 (이차 베지어라 제어점까지 올라가지 않는다).
        static Sprite MakeHill()
        {
            const int W = 300, H = 96;   // y: -16 ~ 80
            return Make(W, H, (x, y) =>
            {
                float t = x / W;
                // y(t) = 80(1-t)² - 220(1-t)t + 80t²   (화면 좌표, 아래가 양수)
                float u = 1f - t;
                float curve = 80f * u * u - 220f * u * t + 80f * t * t;
                float py = 80f - y;          // 텍스처는 위가 양수라 뒤집는다
                return py - curve + 0.5f;    // 곡선 아래를 채운다
            }, HillGreen, pivotY: 0f);            // 피벗을 바닥에 둬서 배치가 쉽다
        }

        static Sprite MakeShadow(int w, int h)
        {
            float rx = w * 0.5f, ry = h * 0.5f;
            return Make(w, h, (x, y) =>
            {
                float dx = (x - rx) / rx, dy = (y - ry) / ry;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                return (1f - d) * 2f;   // 가장자리로 갈수록 흐려진다
            }, new Color(0.35f, 0.31f, 0.27f, 0.12f));
        }


        // 장애물 색
        public static readonly Color Spring = Rgb(0xFF, 0xB0, 0x3A);   // 튕김판
        public static readonly Color SpikeGray = Rgb(0x9A, 0xA3, 0xAD); // 가시

        static Sprite windChevron;
        /// 바람 방향 표시용 갈매기(>) 하나. flipX 로 왼쪽도 만든다.
        public static Sprite WindChevron => windChevron ??= MakeChevron();

        static Sprite MakeChevron()
        {
            const int W = 18, H = 22;
            return Make(W, H, (x, y) =>
            {
                // 두 획으로 '>' 모양. 중심에서 벌어지는 대각선 두 줄.
                float cy = H * 0.5f;
                float arm = Mathf.Abs(y - cy);            // 위/아래로 갈수록
                float edge = 3f + arm * 0.7f;             // 오른쪽으로 뻗는 획 위치
                return 2.2f - Mathf.Abs(x - edge);        // 두께 ~2px
            }, new Color(1f, 1f, 1f, 0.75f));
        }

        /// 튕김판. 발판 윗면(피벗)에 얹히는 밝은 스프링 패드.
        public static Sprite BouncePad(int widthPx)
        {
            int w = Mathf.Max(10, widthPx), h = 14;
            return Make(w, h, (x, y) =>
            {
                // 위 3px 는 더 밝은 띠, 나머지는 패드. 좌우 2px 는 둥글게 깎는다.
                float edge = Mathf.Min(x, w - x);
                float round = Mathf.Clamp01(edge - 1f);
                float body = (y < h - 3) ? 1f : 0.0f;
                return round * (body > 0 ? 1f : 0f);
            }, Spring, pivotY: (h - 2) / (float)h);
        }

        /// 가시. 밑변이 바닥면(피벗)에 닿고 위로 뾰족하게 솟는 톱니.
        public static Sprite Spikes(int widthPx)
        {
            int tooth = 12;                       // 톱니 하나 폭
            int n = Mathf.Max(1, widthPx / tooth);
            int w = n * tooth, h = 14;
            return Make(w, h, (x, y) =>
            {
                float lx = x % tooth;             // 톱니 안에서의 x
                float half = tooth * 0.5f;
                float slope = 1f - Mathf.Abs(lx - half) / half;  // 가운데가 가장 높다
                float top = slope * (h - 1);      // 이 x 에서 가시 높이
                return top - y + 0.5f;            // 삼각형 아래를 채운다
            }, SpikeGray, pivotY: 0f);
        }

        static readonly Sprite[] buttons = new Sprite[3];

        /// 화면 조작 버튼. dir: -1 왼쪽, +1 오른쪽, 0 위(점프).
        /// 원형 + 화살표라 흙 위에서도 하늘 위에서도 읽힌다.
        public static Sprite Button(int dir)
        {
            int idx = dir + 1;
            if (buttons[idx] != null) return buttons[idx];

            const int S = 128;
            float r = S * 0.5f;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float fill = Mathf.Clamp01(r - 3f - d + 0.5f);        // 안쪽 원
                    float ring = Mathf.Clamp01(2.5f - Mathf.Abs(d - (r - 3f))); // 테두리

                    // 화살표: dir 방향으로 뾰족한 삼각형
                    float a = dir == 0 ? dy : dx * dir;   // 화살표가 향하는 축
                    float b = dir == 0 ? dx : dy;         // 그 수직축
                    float apex = r * 0.42f, back = -r * 0.26f, half = r * 0.44f;
                    float t = Mathf.InverseLerp(apex, back, a);
                    float arrow = (a <= apex && a >= back && Mathf.Abs(b) <= half * t) ? 1f : 0f;

                    var c = new Color(1f, 1f, 1f, 0f);
                    if (fill > 0f) c = new Color(1f, 1f, 1f, 0.30f * fill);
                    if (ring > 0f) c = new Color(1f, 1f, 1f, Mathf.Max(c.a, 0.75f * ring));
                    if (arrow > 0f) c = new Color(1f, 1f, 1f, 0.92f);

                    px[y * S + x] = c;
                }

            tex.SetPixels(px);
            tex.Apply();
            buttons[idx] = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 1f);
            return buttons[idx];
        }

        /// 발판 한 장을 통째로 그린다 (흙 + 잔디 + 윗면 하이라이트).
        /// 폭이 발판마다 달라서 한 장씩 만든다. 170px짜리가 20KB 정도라 60개여도 부담 없다.
        /// withHighlight: 윗면의 밝은 선. 작은 발판에서는 입체감을 주지만
        /// 맵 전체 폭인 바닥에 넣으면 화면을 가로지르는 흰 줄로 보인다.
        /// 원본도 바닥은 하이라이트 없이 그렸다.
        public static Sprite Ledge(int widthPx, bool isMover, bool withHighlight = true)
        {
            const int Top = 9;      // 잔디가 발판 위로 솟는 높이
            const int Body = 20;    // 흙 두께
            int w = Mathf.Max(8, widthPx), h = Top + Body;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];

            for (int x = 0; x < w; x++)
            {
                // 잔디 윗선: 16px마다 9px 솟는 혹 (원본과 같은 주기)
                float bump = Mathf.Sin((x % 16) / 16f * Mathf.PI) * 9f;
                float grassTop = Top - bump;        // 위에서부터의 깊이
                float grassBottom = Top + 6f;

                for (int y = 0; y < h; y++)
                {
                    int ty = h - 1 - y;             // 위에서부터의 y
                    Color c = new Color(0, 0, 0, 0);

                    if (ty >= grassTop && ty < grassBottom)
                        c = Grass;
                    else if (ty >= grassBottom)
                    {
                        float taper = 5f * (ty - grassBottom) / Body;
                        c = (x >= taper && x < w - taper) ? Dirt : new Color(0, 0, 0, 0);
                    }

                    // 윗면 하이라이트
                    if (withHighlight && ty >= Top && ty < Top + 3 && x > 3 && x < w - 4)
                        c = Color.Lerp(c, Color.white, 0.22f);

                    // 움직이는 발판 표시 — 가운데 흰 줄
                    if (isMover && ty >= Top + 10 && ty < Top + 13 &&
                        x > w / 2 - 9 && x < w / 2 + 9)
                        c = Color.Lerp(c, Color.white, 0.5f);

                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            // 피벗을 잔디 윗면(= 충돌면)에 맞춘다
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, (h - Top) / (float)h), Px.PPU);
        }
    }
}
