using UnityEngine;

namespace UpTogether
{
    /// 꼭대기 깃발. 장대 + 살랑이는 깃발.
    /// 원본 프로토타입의 flag() 를 옮긴 것이다.
    public class GoalFlag : MonoBehaviour
    {
        public StageRunner stage;
        public Transform cloth;

        float phase;

        void Start()
        {
            if (stage == null || stage.Data == null) { enabled = false; return; }
            transform.position = new Vector3(stage.Data.goal.x, stage.Data.goal.y, 0f);
        }

        void Update()
        {
            if (cloth == null) return;
            // 원본: Math.sin(now/260)*4 px 만큼 흔들린다
            phase += Time.deltaTime * (1000f / 260f);
            var p = cloth.localPosition;
            p.x = Px.U(11f) + Mathf.Sin(phase) * Px.U(2f);
            cloth.localPosition = p;
        }
    }
}
