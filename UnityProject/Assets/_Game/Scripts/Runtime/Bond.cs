using System;
using UnityEngine;

namespace UpTogether
{
    /// 친밀도. 이 게임의 차별점이라 수치는 PROJECT.md 를 그대로 따른다.
    ///   높이 구간 통과 +5 / 스테이지 클리어 +10 / 크게 낙하 -0.8~-3.5 (거리 비례)
    /// 프로토타입은 localStorage 에 저장했다. 여기서는 PlayerPrefs.
    ///
    /// 이 값은 숫자만이 아니다 — DogController 가 읽어서 따라오는 거리, 뛰기 전
    /// 멈칫, 안기기 해금을 바꾼다. 유기견이 상처를 회복하는 과정이 이 수치 하나다.
    public class Bond : MonoBehaviour
    {
        const string SaveKey = "climb2_bond";

        /// 단계 문턱과 이름. 0 부터 시작해 넘을 때마다 올라간다.
        /// 이름을 해금과 맞췄다 — 62 가 "품에 안김"이면 안기기가 열린 것을
        /// 플레이어가 설명 없이 이해한다 (Tuning.bondClingUnlock 과 같은 값).
        static readonly (float at, string name)[] Stages =
        {
            (0f,  "겁먹음"),
            (20f, "곁눈질"),
            (40f, "거리를 좁힘"),
            (62f, "품에 안김"),
            (82f, "한 몸"),
            (95f, "영혼의 단짝"),
        };

        public float Value { get; private set; }

        /// 0~1. 강아지 행동을 보간할 때 쓴다.
        public float Normalized => Value * 0.01f;

        /// 지금 몇 번째 단계인가 (0부터)
        public int StageIndex
        {
            get
            {
                int i = 0;
                for (int k = 0; k < Stages.Length; k++) if (Value >= Stages[k].at) i = k;
                return i;
            }
        }

        public string StageName => Stages[StageIndex].name;

        /// 값이 바뀔 때마다 (값, 단계이름). UI 가 듣는다.
        public event Action<float, string> Changed;

        /// 단계가 올라간 순간 (새 단계 번호). 서사 한 줄이 붙는다.
        /// 내려갈 때는 울리지 않는다 — 퇴행은 강아지 몸으로 보여주고 말로 하지 않는다.
        public event Action<int> StageUp;

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
            int before = StageIndex;
            Value = next;
            PlayerPrefs.SetFloat(SaveKey, Value);
            Changed?.Invoke(Value, StageName);
            if (StageIndex > before) StageUp?.Invoke(StageIndex);
        }

        /// 크게 떨어졌을 때. 멀리 떨어질수록 많이 잃는다 (최대 3.5).
        public void LoseByFall(float meters)
        {
            Add(-Mathf.Min(3.5f, 0.8f + meters * 0.035f));
        }

        /// 개발용. 6단계 × 5스테이지를 손으로 플레이해 검증할 수는 없으므로
        /// 아무 구간이나 바로 세팅할 수 있어야 한다. Add 를 거쳐 이벤트도 울린다.
        public void Set(float value) => Add(value - Value);
    }
}
