using UnityEngine;

namespace UpTogether
{
    /// 검증된 수치가 60fps 프레임 단위라, 물리 스텝이 60Hz가 아니면 점프 높이가 달라진다.
    /// Project Settings의 Fixed Timestep에 맡기지 않는다 — 누가 바꾸면 조용히 손맛이 어긋난다.
    /// 여기서 강제하면 에디터 재생과 실기기 빌드 양쪽에 똑같이 적용된다.
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ForceFixedTimestep()
        {
            Time.fixedDeltaTime = 1f / Px.FPS;
        }
    }
}
