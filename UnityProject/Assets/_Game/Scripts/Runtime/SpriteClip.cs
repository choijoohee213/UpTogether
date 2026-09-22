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
