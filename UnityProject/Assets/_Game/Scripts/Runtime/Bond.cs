using System;
using UnityEngine;

namespace UpTogether
{
    /// 친밀도. 이 게임의 차별점이라 수치는 PROJECT.md 를 그대로 따른다.
    ///   높이 구간 통과 +5 / 스테이지 클리어 +10 / 크게 낙하 -0.8~-3.5 (거리 비례)
    /// 프로토타입은 localStorage 에 저장했다. 여기서는 PlayerPrefs.
    public class Bond : MonoBehaviour
    {
        const string SaveKey = "climb2_bond";

        /// 단계 문턱과 이름. 0 부터 시작해 넘을 때마다 올라간다.
        static readonly (float at, string name)[] Stages =
        {
            (0f,  "서먹서먹"),
            (20f, "조금 친해짐"),
            (40f, "꽤 가까움"),
            (62f, "단짝"),
            (82f, "한 몸"),
            (95f, "영혼의 단짝"),
        };

        public float Value { get; private set; }
        public string StageName
        {
            get
            {
                string n = Stages[0].name;
                foreach (var s in Stages) if (Value >= s.at) n = s.name;
                return n;
            }
        }

        /// 값이 바뀔 때마다 (값, 단계이름). UI 가 듣는다.
        public event Action<float, string> Changed;

        void Awake()
        {
            Value = PlayerPrefs.GetFloat(SaveKey, 0f);
        }

        void Start()
        {
            Changed?.Invoke(Value, StageName);
        }

        public void Add(float amount)
        {
            float next = Mathf.Clamp(Value + amount, 0f, 100f);
            if (Mathf.Approximately(next, Value)) return;
            Value = next;
            PlayerPrefs.SetFloat(SaveKey, Value);
            Changed?.Invoke(Value, StageName);
        }

        /// 크게 떨어졌을 때. 멀리 떨어질수록 많이 잃는다 (최대 3.5).
        public void LoseByFall(float meters)
        {
            Add(-Mathf.Min(3.5f, 0.8f + meters * 0.035f));
        }
    }
}
