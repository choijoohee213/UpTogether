using System;
using System.Collections.Generic;

namespace UpTogether
{
    /// climb-dog2.html 의 makeStage() 이식본. Unity 타입을 쓰지 않는다 —
    /// 원본 JS와 결과를 그대로 비교(diff)할 수 있게 하기 위해서다.
    /// 좌표는 아직 프로토타입 px 공간(y 아래쪽 양수)이다. 변환은 베이커가 한다.
    public static class StageGenerator
    {
        public const double MapWidth = 1250.0;

        public struct Box
        {
            public double X, Y, W;
            public bool IsMover;
            public double Range, Speed, Phase;
        }

        public struct Config
        {
            public string Name;
            public int Seed;
            public int Rows;
            public double Width;
            public double MoverChance; // 0이면 움직이는 발판 없음
        }

        public static readonly Config[] Stages =
        {
            new Config { Name = "첫 번째 언덕", Seed = 1001, Rows = 16, Width = 170, MoverChance = 0.0  },
            new Config { Name = "구름 언덕",   Seed = 2002, Rows = 20, Width = 148, MoverChance = 0.28 },
            new Config { Name = "별빛 언덕",   Seed = 3003, Rows = 24, Width = 124, MoverChance = 0.40 },
        };

        sealed class Rng
        {
            ulong s;
            public Rng(int seed) { s = (ulong)seed; }
            // JS: s=(s*1664525+1013904223)%4294967296 — double로도 정확히 표현되는 범위다
            public double Next()
            {
                s = (s * 1664525UL + 1013904223UL) % 4294967296UL;
                return s / 4294967296.0;
            }
        }

        public static double StageHeight(int rows) => 180 + rows * 110;

        public static List<Box> Generate(Config c)
        {
            var R = new Rng(c.Seed);
            var boxes = new List<Box>();
            double H = StageHeight(c.Rows);

            boxes.Add(new Box { X = 0, Y = H - 40, W = MapWidth }); // 바닥

            double y = H - 40, lastX = 80, lastW = 200;
            int dir = 1;

            for (int i = 0; i < c.Rows; i++)
            {
                double w = Math.Max(74, c.Width - R.Next() * 44);
                double rise = 68 + R.Next() * 34;

                // JS의 && 단축 평가를 그대로 재현해야 한다.
                // MoverChance가 0이거나 i<=2면 R()이 호출되지 않고, 난수 흐름이 달라진다.
                bool isMover = c.MoverChance > 0 && i > 2 && R.Next() < c.MoverChance;
                double range = isMover ? 24 + R.Next() * 20 : 0;

                double maxGap = 68 - range;
                double gap = 10 + R.Next() * Math.Max(8, maxGap - 10);

                double pL = lastX, pR = lastX + lastW;
                double x = dir > 0 ? pR + gap : pL - gap - w;
                if (x > MapWidth - w - 46) { dir = -1; x = pL - gap - w; }
                if (x < 46) { dir = 1; x = pR + gap; }
                x = Math.Max(46, Math.Min(MapWidth - w - 46, x));
                if (x > pR + maxGap) x = Math.Max(46, pR + maxGap);
                if (x + w < pL - maxGap) x = Math.Min(MapWidth - w - 46, pL - maxGap - w);

                y -= rise;

                if (isMover)
                    boxes.Add(new Box { X = x, Y = y, W = w, IsMover = true, Range = range,
                                        Speed = 0.5 + R.Next() * 0.6, Phase = R.Next() * 6 });
                else
                    boxes.Add(new Box { X = x, Y = y, W = w });

                lastX = x; lastW = w;
                if (R.Next() < 0.22) dir *= -1;
            }
            return boxes;
        }

        public struct Violation { public int Index; public string Message; }

        /// PROJECT.md "발판 배치 제약". 어기면 도달 불가 구간이 생긴다.
        /// SUMMARY.md: 예전에 이 검사로 도달 불가 7구간을 잡아냈다.
        /// 움직이는 발판은 가장 멀어진 순간을 기준으로 본다.
        public static List<Violation> Validate(List<Box> boxes, out double tightestGap)
        {
            var bad = new List<Violation>();
            tightestGap = 0;
            for (int i = 2; i < boxes.Count; i++) // 0=바닥, 1=첫 발판
            {
                var a = boxes[i - 1];
                var b = boxes[i];

                double rise = a.Y - b.Y;
                if (rise < 68 - 1e-9 || rise > 102 + 1e-9)
                    bad.Add(new Violation { Index = i, Message = $"상승폭 {rise:F1}px (68~102 벗어남)" });

                double aL = a.X - a.Range, aR = a.X + a.W + a.Range;
                double bL = b.X - b.Range, bR = b.X + b.W + b.Range;
                double gap = bL > aR ? bL - aR : (aL > bR ? aL - bR : 0);
                if (gap > 68 + 1e-9)
                    bad.Add(new Violation { Index = i, Message = $"가로 간격 {gap:F1}px (최대 68)" });
                if (gap > tightestGap) tightestGap = gap;
            }
            return bad;
        }

        /// JS: plats.concat(movers).reduce((a,b)=>a.y<b.y?a:b) — 고정 발판이 먼저 온다
        public static Box Highest(List<Box> boxes)
        {
            Box top = default; bool set = false;
            foreach (var b in boxes) if (!b.IsMover && (!set || b.Y < top.Y)) { top = b; set = true; }
            foreach (var b in boxes) if (b.IsMover && b.Y < top.Y) top = b;
            return top;
        }
    }
}
