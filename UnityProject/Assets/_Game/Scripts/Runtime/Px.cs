namespace UpTogether
{
    /// 프로토타입(climb-dog2.html) 수치는 px/frame @60fps 기준이다.
    /// 이 클래스가 유일한 변환 지점이다. 다른 곳에서 60이나 100을 직접 곱하지 말 것.
    public static class Px
    {
        public const float PPU = 100f;   // Pixels Per Unit
        public const float FPS = 60f;
        public const float PxPerMeter = 26f; // 프로토타입: h=(H-40-y)/26

        /// px → units
        public static float U(float px) => px / PPU;
        /// px/frame → units/s
        public static float V(float pxPerFrame) => pxPerFrame * FPS / PPU;
        /// px/frame² → units/s²
        public static float A(float pxPerFrame2) => pxPerFrame2 * FPS * FPS / PPU;

        /// 프레임 단위 lerp 계수(0.34 등)를 dt에 맞게 보정.
        /// dt=1/60에서 원본과 정확히 같은 값이 된다.
        public static float Smoothing(float perFrame, float dt)
            => 1f - UnityEngine.Mathf.Pow(1f - perFrame, dt * FPS);
    }
}
