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
                if (i >= frames.Length) { if (loop) i = 0; else { i = frames.Length - 1; enabled = false; } }
                sr.sprite = frames[i];
            }
        }
    }
}
