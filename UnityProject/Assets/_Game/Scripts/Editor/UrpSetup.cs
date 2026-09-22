using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UpTogether.EditorTools
{
    /// Built-in 렌더 파이프라인이 Unity 6.5부터 deprecated라 URP 2D로 옮긴다.
    /// 커스텀 셰이더와 머티리얼이 없어서(SpriteRenderer + UGUI뿐) 변환할 게 없다.
    /// 한 번 실행하면 끝이고, 다시 실행하면 기존 에셋을 덮어쓴다.
    public static class UrpSetup
    {
        const string Dir = "Assets/_Game/Rendering";
        const string RendererPath = Dir + "/Renderer2D.asset";
        const string PipelinePath = Dir + "/URP-2D.asset";

        [MenuItem("UpTogether/Switch to URP 2D")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/_Game", "Rendering");

            // 2D 렌더러 — 스프라이트용. 3D용 UniversalRendererData가 아니다.
            var rendererData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            AssetDatabase.SaveAssets();

            // Graphics 와 Quality 양쪽에 물려야 한다.
            // Quality 쪽을 비워두면 품질 단계에 따라 Built-in으로 되돌아간다.
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var active = GraphicsSettings.currentRenderPipeline;
            Debug.Log($"렌더 파이프라인: {(active == null ? "Built-in (실패)" : active.name)}\n" +
                      $"  파이프라인 에셋: {PipelinePath}\n" +
                      $"  렌더러: {RendererPath} ({rendererData.GetType().Name})");

            if (active == null)
                Debug.LogError("URP가 활성화되지 않았습니다. Project Settings ▸ Graphics 를 확인하세요.");
        }
    }
}
