using UnityEngine;

namespace UpTogether
{
    /// 강아지 머리 위에 잠깐 뜨는 말풍선 이모트.
    /// 안기면(따라잡음) 하트, 크게 떨어지면 걱정. 이 게임의 정서를 살짝 거든다.
    [DefaultExecutionOrder(55)]
    public class DogEmote : MonoBehaviour
    {
        public DogController dog;
        public PlayerController player;
        public SpriteLib lib;
        public float showTime = 1.3f;

        SpriteRenderer sr;
        float hideAt;
        bool wasClinging;

        void Awake()
        {
            var go = new GameObject("emote");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.6f, 0f);   // 머리 위
            go.transform.localScale = Vector3.one * 1.5f;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 120;
            sr.enabled = false;
        }

        void OnEnable() { if (player != null) player.Landed += OnLanded; }
        void OnDisable() { if (player != null) player.Landed -= OnLanded; }

        void OnLanded(float meters) { if (meters > 12f) Show(lib != null ? lib.emoteQuestion : null); }

        void Update()
        {
            if (dog != null)
            {
                if (dog.IsClinging && !wasClinging) Show(lib != null ? lib.emoteHeart : null);
                wasClinging = dog.IsClinging;
            }
            if (sr.enabled && Time.time > hideAt) sr.enabled = false;
        }

        void Show(Sprite s)
        {
            if (s == null) return;
            sr.sprite = s;
            sr.enabled = true;
            hideAt = Time.time + showTime;
        }
    }
}
