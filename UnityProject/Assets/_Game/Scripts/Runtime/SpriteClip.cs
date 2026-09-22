using System;
using UnityEngine;

namespace UpTogether
{
    /// 프레임 인덱스 + 프레임별 지속시간. 지속시간이 프레임마다 다르므로
    /// (idle이 1.5s / 0.15s 처럼) 단일 fps로는 표현할 수 없다.
    [Serializable]
    public class SpriteClip
    {
        public string name;
        public int[] frames;
        public float[] durations;
        public bool loop;

        [Header("안기 전용 — 없으면 비워둔다")]
        [Tooltip("주인공 앞에 겹쳐 그릴 팔 프레임. frames 와 같은 인덱스로 재생한다.")]
        public int[] overlay;
        [Tooltip("주인공 피벗 기준 강아지 피벗의 오프셋(px). 왼쪽을 볼 때 x 는 자동으로 뒤집힌다.")]
        public Vector2 dogPivotOffsetPx;
        [Tooltip("이 클립에서 쓸 강아지 프레임. -1 이면 안 쓴다.")]
        public int dogFrame = -1;

        public bool HasOverlay => overlay != null && overlay.Length > 0;
        /// 오버레이는 본체와 같은 인덱스로 간다 (13→16, 14→17).
        public int OverlayFrame(int i) => HasOverlay ? overlay[Mathf.Min(i, overlay.Length - 1)] : -1;

        public int Length => frames == null ? 0 : frames.Length;
        public float Duration(int i) => durations != null && i < durations.Length ? durations[i] : 0.1f;

        /// 클립 전체 길이. 고정 프레임(지속시간 0)은 0을 돌려준다.
        public float TotalDuration
        {
            get
            {
                float t = 0f;
                for (int i = 0; i < Length; i++) t += Duration(i);
                return t;
            }
        }
    }
}
