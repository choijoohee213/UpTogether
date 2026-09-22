using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 플레이어 설정 중 손으로 매번 맞추기 쉬운 것들을 코드로 박아둔다.
    /// 한 번 실행하면 ProjectSettings 에 저장되므로 평소엔 쓸 일이 없다.
    public static class ProjectSetup
    {
        [MenuItem("UpTogether/Apply Player Settings")]
        public static void Run()
        {
            // 세로 고정. 가로는 검토 끝에 접었다 —
            // 카메라가 담는 월드 높이가 고정이라 가로로 돌려도 위로 더 보이지 않고,
            // 폰 화면 높이가 절반 이하라 캐릭터만 작아진다. 버튼도 플레이 영역을 덮는다.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;

            AssetDatabase.SaveAssets();
            Debug.Log($"플레이어 설정 적용: 화면 방향 = {PlayerSettings.defaultInterfaceOrientation} (가로 회전 잠금)");
        }
    }
}
