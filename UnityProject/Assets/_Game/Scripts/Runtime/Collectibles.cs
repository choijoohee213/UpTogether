using System;
using UnityEngine;

namespace UpTogether
{
    /// 주우면 친밀도가 오르는 것들 — 강아지 간식과 평범한 통과 링.
    /// (가시 링·톱니 같은 '아픈' 것은 Hazards 가 맡는다.)
    [DefaultExecutionOrder(16)]
    public class Collectibles : MonoBehaviour
    {
        public CharacterBody player;
        public StageRunner stage;
        public Bond bond;
        public SpriteLib lib;

        /// 뭔가 주운 순간. 소리가 붙는다.
        public event Action Collected;

        const float RingBond = 4f;
        const float TreatBond = 6f;
        const float TreatReach = 0.34f;

        bool[] ringDone, treatDone;
        Transform[] ringVis, treatVis;

        void Start()
        {
            if (stage == null || stage.Data == null || player == null) { enabled = false; return; }
            BuildRings();
            BuildTreats();
        }

        /// 스테이지가 바뀌면 옛 수집물을 지우고 새 스테이지 것으로 다시 만든다.
        public void Rebuild()
        {
            if (ringVis != null) foreach (var t in ringVis) if (t != null) Destroy(t.gameObject);
            if (treatVis != null) foreach (var t in treatVis) if (t != null) Destroy(t.gameObject);
            BuildRings();
            BuildTreats();
        }

        void BuildRings()
        {
            var rings = stage.Data.rings ?? Array.Empty<StageData.Ring>();
            ringDone = new bool[rings.Length];
            ringVis = new Transform[rings.Length];
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i].thorny) { ringDone[i] = true; continue; }  // 가시 링은 StageRunner 담당
                var go = new GameObject("Ring");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(rings[i].x, rings[i].y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                if (lib != null && lib.ringFlower != null)
                {
                    sr.sprite = lib.ringFlower;
                    float vis = lib.ringFlower.rect.height / lib.ringFlower.pixelsPerUnit;
                    go.transform.localScale = Vector3.one * (rings[i].radius * 2f / vis);
                }
                else sr.sprite = ProceduralArt.Ring(Mathf.RoundToInt(rings[i].radius * 2f * Px.PPU), false);
                sr.sortingOrder = -6;
                ringVis[i] = go.transform;
            }
        }

        void BuildTreats()
        {
            var treats = stage.Data.treats ?? Array.Empty<StageData.Treat>();
            treatDone = new bool[treats.Length];
            treatVis = new Transform[treats.Length];
            for (int i = 0; i < treats.Length; i++)
            {
                var go = new GameObject("Treat");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(treats[i].x, treats[i].y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -5;
                // 뼈다귀·하트·별을 번갈아 (반짝임 2프레임)
                Sprite[] kinds = lib == null ? null :
                    (i % 3 == 0 ? lib.treatBone : i % 3 == 1 ? lib.treatHeart : lib.treatStar);
                if (kinds != null && kinds.Length > 0 && kinds[0] != null)
                {
                    sr.sprite = kinds[0];
                    if (kinds.Length > 1) { var a = go.AddComponent<SpriteAnim>(); a.frames = kinds; a.fps = 3f; }
                }
                else sr.sprite = ProceduralArt.Treat(Mathf.RoundToInt(0.32f * Px.PPU));
                treatVis[i] = go.transform;
            }
        }

        void FixedUpdate()
        {
            var rings = stage.Data.rings;
            var treats = stage.Data.treats;
            float cx = player.X, cy = player.Y + 0.28f;

            if (rings != null)
                for (int i = 0; i < rings.Length; i++)
                {
                    if (ringDone[i]) continue;
                    float d = Vector2.Distance(new Vector2(cx, cy), new Vector2(rings[i].x, rings[i].y));
                    if (d < rings[i].radius * 0.55f) Take(ref ringDone[i], ringVis[i], RingBond);
                }

            if (treats != null)
                for (int i = 0; i < treats.Length; i++)
                {
                    if (treatDone[i]) continue;
                    float d = Vector2.Distance(new Vector2(player.X, player.Y + 0.2f),
                                               new Vector2(treats[i].x, treats[i].y));
                    if (d < TreatReach) Take(ref treatDone[i], treatVis[i], TreatBond);
                }
        }

        void Take(ref bool done, Transform vis, float amount)
        {
            done = true;
            if (vis != null) vis.gameObject.SetActive(false);
            bond?.Add(amount);
            Collected?.Invoke();
        }
    }
}
