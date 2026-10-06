using UnityEngine;

namespace UpTogether
{
    /// 숲 스프라이트 팩을 한곳에 모은 라이브러리. 베이커가 채우고, 씬 컴포넌트들이 참조한다.
    /// (컴포넌트마다 스프라이트 필드를 흩뿌리지 않으려는 것.)
    [CreateAssetMenu(menuName = "UpTogether/Sprite Lib", fileName = "SpriteLib")]
    public class SpriteLib : ScriptableObject
    {
        [Header("장애물")]
        public Sprite spikeFloor;
        public Sprite[] thornVine;       // 2
        public Sprite sawLog;
        public Sprite[] bounceMushroom;  // 2
        public Sprite ringFlower, ringThorn;
        public Sprite[] wind;            // 3

        [Header("장애물 v3")]
        public Sprite branchBar;         // 매달려 건너는 가지 (가로로 이어 붙임)
        public Sprite pendulumWeight;    // 흔들리는 추
        public Sprite pendulumChain;     // 추를 매단 사슬 (세로로 이어 붙임)
        public Sprite[] rockFall;        // 3 — 매달림/흔들림/낙하
        public Sprite[] steam;           // 4 — 간헐 증기
        public Sprite wallGrip;          // 벽 타기용 벽면
        public Sprite[] updraft;         // 3 — 상승 기류

        [Header("수집")]
        public Sprite[] treatBone, treatHeart, treatStar;  // 각 2

        [Header("효과")]
        public Sprite[] dustPuff;        // 5
        public Sprite[] warpSparkle;     // 4
        public Sprite[] leaf, firefly;   // 4, 3

        [Header("정체성")]
        public Sprite[] doghouse;        // 2
        public Sprite emoteHeart, emoteExclaim, emoteQuestion, emoteNote;

        [Header("UI")]
        public Sprite[] btnLeft, btnRight, btnUp, btnPause;  // 각 2
        public Sprite panelWood, gaugeFrame, gaugeFill;
        public Sprite[] heartIcon;       // 3
    }
}
