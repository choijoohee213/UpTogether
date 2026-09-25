using System;
using UnityEngine;

namespace UpTogether
{
    /// 베이크된 스테이지. 좌표는 전부 Unity 단위(y 위쪽 양수, PPU 100).
    /// 베이커가 한 번 굽고 나면 인스펙터에서 손으로 고쳐도 된다.
    /// (PROJECT.md 열린 과제 1번: 자동 생성 → 수작업 배치)
    [CreateAssetMenu(menuName = "UpTogether/Stage", fileName = "Stage")]
    public class StageData : ScriptableObject
    {
        [Serializable]
        public struct Platform
        {
            public float x;      // 왼쪽 끝
            public float y;
            public float width;
            public float Right => x + width;
        }

        [Serializable]
        public struct Mover
        {
            public float x;      // 왕복의 중심
            public float y;
            public float width;
            public float range;  // 중심에서 이 만큼
            public float speed;  // rad/s
            public float phase;
            public bool vertical; // true 면 위아래로 왕복한다
            public float Right => x + width;
        }

        /// 튕김판. 밟으면 위로 크게 튄다.
        [Serializable]
        public struct Bouncer { public float x, y, width; public float Right => x + width; }

        /// 사라지는 발판. 밟으면 잠깐 흔들리다 사라지고, 잠시 뒤 되살아난다.
        [Serializable]
        public struct Vanisher { public float x, y, width; public float Right => x + width; }

        /// 가시. y 는 가시가 솟은 바닥면. 닿으면 튕겨나가고 친밀도가 깎인다.
        [Serializable]
        public struct Spike { public float x, y, width; public float Right => x + width; }

        /// 바람 지대. 안에 있으면 옆으로 밀린다. force 부호가 방향(+오른쪽).
        [Serializable]
        public struct Wind { public float x, y, width, height, force; }

        public string displayName;
        [Tooltip("원본 생성에 쓴 시드. 다시 구우려면 필요하다.")]
        public int seed;
        [Tooltip("바닥 발판의 y. 높이(m) 계산의 0점.")]
        public float groundY = 0.40f;
        [Tooltip("스테이지 전체 높이(units). 카메라 상한에 쓴다.")]
        public float height;

        public Platform[] platforms;
        public Mover[] movers;
        public Bouncer[] bouncers;
        public Vanisher[] vanishers;
        public Spike[] spikes;
        public Wind[] winds;
        public Vector2 goal;

        /// 월드 y → 게임에 표시되는 높이(m). 프로토타입: 26px = 1m
        public float Meters(float worldY) => (worldY - groundY) * Px.PPU / Px.PxPerMeter;
    }
}
