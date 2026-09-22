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

            if (s.result != BuildResult.Succeeded)
            {
                Debug.LogError($"웹 빌드 실패: {s.result} / 오류 {s.totalErrors}건");
                return;
            }

            PostProcessIndex(Path.Combine(path, "index.html"));
            Debug.Log($"웹 빌드 성공: {path} ({s.totalSize / 1024 / 1024} MB, {s.totalTime.TotalSeconds:F0}초)");
        }

        /// index.html 후처리 두 가지.
        ///
        /// 1) 항상 창을 채우기 — 기본 템플릿은 모바일에서만 캔버스로 창을 채우고
        ///    데스크톱에서는 defaultWebScreen 크기(450x800)로 고정한다.
        ///
        /// 2) 캐시 무력화 — 파일명이 매 빌드 같아서 브라우저가 옛 loader 와 새 wasm 을
        ///    섞어 들면 "call_indirect to a signature that does not match" 로 죽는다.
        ///    빌드마다 다른 쿼리를 붙여 그런 조합이 생길 수 없게 한다.
        static void PostProcessIndex(string indexPath)
        {
            if (!File.Exists(indexPath)) { Debug.LogWarning($"{indexPath} 가 없어 후처리를 건너뜁니다."); return; }
            string html = File.ReadAllText(indexPath);

            const string cond = "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {";
            if (html.Contains(cond)) html = html.Replace(cond, "if (true) {   // 항상 창을 채운다");
            else Debug.LogWarning("index.html 의 모바일 분기를 못 찾았습니다. 템플릿이 바뀌었는지 확인하세요.");

            string stamp = System.DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            int stamped = 0;
            foreach (var name in new[] { "Web.loader.js", "Web.data.unityweb",
                                         "Web.framework.js.unityweb", "Web.wasm.unityweb" })
            {
                string from = $"\"/{name}\"";
                string to = $"\"/{name}?v={stamp}\"";
                if (html.Contains(from)) { html = html.Replace(from, to); stamped++; }
            }
            if (stamped != 4)
                Debug.LogWarning($"캐시 무력화를 {stamped}/4 개에만 적용했습니다. 템플릿 확인 필요.");

            File.WriteAllText(indexPath, html);
            Debug.Log($"index.html 후처리: 창 채우기 + 캐시 무력화 v={stamp}");
        }
    }
}
