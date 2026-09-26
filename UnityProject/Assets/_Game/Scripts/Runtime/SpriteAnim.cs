using UnityEngine;

namespace UpTogether
{
    /// 스프라이트 프레임을 일정 속도로 넘기는 가벼운 애니메이터.
    /// 장식·이펙트용(먼지·바람·간식 반짝임·이모트 등). 캐릭터는 기존 방식을 쓴다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnim : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 8f;
        public bool loop = true;
        public bool destroyOnEnd;   // 루프 아닌 재생이 끝나면 오브젝트를 없앤다(1회 이펙트)

        SpriteRenderer sr;
        float t;
        int i;

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        void OnEnable() { t = 0f; i = 0; if (Has) sr.sprite = frames[0]; }

        bool Has => frames != null && frames.Length > 0 && sr != null;

        void Update()
        {
            if (!Has || frames.Length < 2 || fps <= 0f) return;
            t += Time.deltaTime;
            float step = 1f / fps;
            while (t >= step)
            {
                t -= step;
                i++;
                if (i >= frames.Length)
                {
                    if (loop) i = 0;
                    else
                    {
                        sr.sprite = frames[frames.Length - 1];
                        enabled = false;
                        if (destroyOnEnd) Destroy(gameObject);
                        return;
                    }
                }
                sr.sprite = frames[i];
            }
        }

        /// 1회 재생 이펙트를 그 자리에 띄운다(끝나면 사라짐).
        public static void Spawn(Vector3 worldPos, Sprite[] frames, float fps, float scale, int order)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            var go = new GameObject("fx");
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * scale;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = frames[0];
            r.sortingOrder = order;
            var a = go.AddComponent<SpriteAnim>();
            a.frames = frames; a.fps = fps; a.loop = false; a.destroyOnEnd = true;
        }
    }
}
