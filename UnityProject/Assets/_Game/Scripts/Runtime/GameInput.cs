using UnityEngine;

namespace UpTogether
{
    /// 키보드(에디터 확인용) + 화면 버튼(실기기)을 한 곳으로 모은다.
    /// 점프는 "눌린 순간"과 "누르고 있음"이 둘 다 필요하다 — 홀드로 높이를 조절하기 때문.
    [DefaultExecutionOrder(-200)]
    public class GameInput : MonoBehaviour
    {
        public static GameInput I { get; private set; }

        public bool Left { get; private set; }
        public bool Right { get; private set; }
        public bool JumpHeld { get; private set; }
        /// FixedUpdate에서 소비된다. 한 번 눌림에 한 번만 true.
        public bool JumpPressed { get; private set; }

        bool touchLeft, touchRight, touchJump;
        bool jumpQueued, prevJump;

        void Awake() => I = this;

        void Update()
        {
            Left  = touchLeft  || Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A);
            Right = touchRight || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);

            bool jump = touchJump || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
            if (jump && !prevJump) jumpQueued = true;
            prevJump = jump;
            JumpHeld = jump;
        }

        void FixedUpdate()
        {
            JumpPressed = jumpQueued;
            jumpQueued = false;
        }

        // 화면 버튼이 호출한다
        public void SetLeft(bool v)  => touchLeft = v;
        public void SetRight(bool v) => touchRight = v;
        public void SetJump(bool v)  => touchJump = v;
    }
}
