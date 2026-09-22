using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 아이폰 사파리에서 조작감을 확인하려고 만든 웹 빌드.
    /// Xcode도 서명도 없이 같은 Wi-Fi에서 바로 열어볼 수 있다.
    public static class WebBuilder
    {
        const string OutputDir = "Build/Web";

        [MenuItem("UpTogether/Build for Web")]
        public static void Build()
        {
            // Brotli 로 줄이되, 디컴프레션 폴백을 켠다.
            // 폴백이 있으면 서버가 Content-Encoding 헤더를 안 맞춰줘도 열린다 —
            // GitHub Pages 처럼 헤더를 못 건드리는 정적 호스팅과 로컬 http.server 양쪽에서 통한다.
            // 대가는 시작이 조금 느려지는 것인데, 42MB 를 모바일 데이터로 받는 것보다 낫다.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.runInBackground = true;

            // 세로 화면 기준으로 만들었다
            PlayerSettings.defaultWebScreenWidth = 450;
            PlayerSettings.defaultWebScreenHeight = 800;

            string path = Path.GetFullPath(OutputDir);
            Directory.CreateDirectory(path);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Game/Playground.unity" },
                locationPathName = path,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;

            if (s.result == BuildResult.Succeeded)
                Debug.Log($"웹 빌드 성공: {path} ({s.totalSize / 1024 / 1024} MB, {s.totalTime.TotalSeconds:F0}초)");
            else
                Debug.LogError($"웹 빌드 실패: {s.result} / 오류 {s.totalErrors}건");
        }
    }
}
