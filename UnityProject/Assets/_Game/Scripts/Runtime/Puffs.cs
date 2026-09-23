using UnityEngine;

namespace UpTogether
{
    /// 점프·착지 때 튀는 흰 먼지. 원본 파티클 값을 그대로 옮겼다.
    /// 배열 하나 돌려쓴다 — 한 번에 수십 개뿐이라 ParticleSystem까지 갈 일이 아니다.
    public class Puffs : MonoBehaviour
    {
        const int Max = 64;
        /// 원본: p.vy += 18*dt (px/frame). 60fps에서 프레임당 0.3px/frame² 이다.
        const float GravityPxPerFrame2 = 0.3f;
        const float Life = 0.55f;
        const float FadeRate = 1.6f;
        /// ProceduralArt.Circle 은 32px 짜리라 스케일 1에서 0.32유닛이다.
        /// 원하는 지름을 이 값으로 나눠야 실제 크기가 맞는다.
        /// 예전엔 이걸 빼먹어서 파티클이 화면에서 2px 남짓으로 보였다.
        static readonly float SpriteUnits = 32f / Px.PPU;

        struct Puff
        {
            public Vector2 pos, vel;   // vel은 units/s
            public float life, size;
            public Transform t;
            public SpriteRenderer sr;
        }

        Puff[] puffs = new Puff[Max];
        int next;

        void Awake()
        {
            for (int i = 0; i < Max; i++)
            {
                var go = new GameObject("puff");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralArt.Circle;
                sr.sortingOrder = 100;
                go.SetActive(false);
                puffs[i] = new Puff { t = go.transform, sr = sr };
            }
        }

        /// 인자는 전부 원본과 같은 px/frame 단위로 받는다.
        /// vyUp이 양수면 위로 튄다 (원본은 y가 아래쪽 양수라 부호가 반대였다).
        /// sizePx: 파티클 지름(px). 기본값은 원본 프로토타입과 같은 3~6px.
        public void Burst(Vector2 at, int count, float spreadPx, float vxRangePx, float vyUpPx,
                          float sizePx = 3f, float sizeVarPx = 3f, float life = Life)
        {
            for (int i = 0; i < count; i++)
            {
                var p = puffs[next];
                p.pos = at + new Vector2((Random.value - 0.5f) * Px.U(spreadPx), 0f);
                p.vel = new Vector2(Px.V((Random.value - 0.5f) * vxRangePx),
                                    Px.V(Random.value * vyUpPx));
                p.life = life;
                p.size = Px.U(sizePx + Random.value * sizeVarPx);
                p.t.gameObject.SetActive(true);
                p.t.position = p.pos;
                puffs[next] = p;
                next = (next + 1) % Max;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float g = Px.A(GravityPxPerFrame2);

            for (int i = 0; i < Max; i++)
            {
                var p = puffs[i];
                if (p.life <= 0f) continue;

                p.vel.y -= g * dt;
                p.pos += p.vel * dt;
                p.life -= dt * FadeRate;

                if (p.life <= 0f)
                {
                    p.t.gameObject.SetActive(false);
                    puffs[i] = p;
                    continue;
                }

                p.t.position = p.pos;
                p.t.localScale = Vector3.one * (p.size / SpriteUnits);
                p.sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(p.life));
                puffs[i] = p;
            }
        }
    }
}
