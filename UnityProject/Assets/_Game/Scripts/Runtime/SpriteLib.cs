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
