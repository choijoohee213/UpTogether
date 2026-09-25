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
                sr.sprite = ProceduralArt.Ring(Mathf.RoundToInt(rings[i].radius * 2f * Px.PPU), false);
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
                sr.sprite = ProceduralArt.Treat(Mathf.RoundToInt(0.32f * Px.PPU));
                sr.sortingOrder = -5;
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
