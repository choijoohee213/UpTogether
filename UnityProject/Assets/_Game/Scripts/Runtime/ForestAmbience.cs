using UnityEngine;

namespace UpTogether
{
    /// 화면 위쪽에서 낙엽을 이따금 떨어뜨려 숲 분위기를 더한다.
    public class ForestAmbience : MonoBehaviour
    {
        public SpriteLib lib;
        public float interval = 1.4f;

        Camera cam;
        float t;

        void Awake() { cam = Camera.main; }

        void Update()
        {
            if (lib == null || lib.leaf == null || lib.leaf.Length == 0 || cam == null) return;
            t -= Time.deltaTime;
            if (t > 0f) return;
            t = interval * (0.6f + Random.value * 0.9f);

            float halfW = cam.orthographicSize * cam.aspect;
            float x = cam.transform.position.x + (Random.value - 0.5f) * 2.2f * halfW;
            float y = cam.transform.position.y + cam.orthographicSize + 0.5f;

            var go = new GameObject("leaf");
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = Vector3.one * 1.4f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = lib.leaf[0];
            sr.sortingOrder = 90;
            var a = go.AddComponent<SpriteAnim>();
            a.frames = lib.leaf; a.fps = 6f;
            go.AddComponent<LeafDrift>();
        }
    }

    /// 낙엽 한 장: 흔들리며 떨어지다 화면 밖으로 나가면 사라진다. (런타임 생성 전용)
    public class LeafDrift : MonoBehaviour
    {
        float fall, sway, phase, spin;
        Camera cam;

        void Start()
        {
            cam = Camera.main;
            fall = 0.7f + Random.value * 0.5f;
            sway = 0.3f + Random.value * 0.4f;
            phase = Random.value * 6.28f;
            spin = (Random.value < 0.5f ? -1f : 1f) * (20f + Random.value * 30f);
        }

        void Update()
        {
            var p = transform.position;
            phase += Time.deltaTime * 2f;
            p.y -= fall * Time.deltaTime;
            p.x += Mathf.Cos(phase) * sway * Time.deltaTime;
            transform.position = p;
            transform.Rotate(0f, 0f, spin * Time.deltaTime);

            if (cam != null && p.y < cam.transform.position.y - cam.orthographicSize - 1f)
                Destroy(gameObject);
        }
    }
}
