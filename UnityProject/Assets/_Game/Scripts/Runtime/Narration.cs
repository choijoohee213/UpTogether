using System;
using UnityEngine;

namespace UpTogether
{
    /// 높이에 따라 한 줄씩 띄운다. PROJECT.md 의 서사 트리거 그대로.
    /// "떨어질 때 잃는 것이 높이가 아니라 강아지를 두고 내려가는 것"이라는
    /// 설계가 실제로 작동하기 시작하는 지점이다.
    public class Narration : MonoBehaviour
    {
        [Serializable]
        public struct Line
        {
            public float meters;
            [TextArea] public string text;
        }

        public static readonly Line[] Default =
        {
            new Line { meters = 10f,  text = "강아지가 종종걸음으로 따라온다." },
            new Line { meters = 28f,  text = "뒤돌아보니 강아지가 발판 끝에서 망설이고 있다." },
            new Line { meters = 48f,  text = "이제는 네가 뛰기 전에 강아지가 먼저 자세를 잡는다." },
            new Line { meters = 72f,  text = "떨어질 때마다 같이 떨어져 줬다는 걸 알고 있다." },
            new Line { meters = 100f, text = "강아지가 앞장서서 발판을 확인하기 시작했다." },
            new Line { meters = 130f, text = "둘 다 숨이 차지만 아무도 멈추자고 하지 않는다." },
        };

        public const string OnBigFall = "많이 떨어졌지만, 강아지는 끝까지 안겨 있었다.";
        public const string OnCling = "강아지가 달려와 품에 안겼다.";

        public Line[] lines = Default;

        /// 띄울 문장. UI 가 듣는다.
        public event Action<string> Say;

        int nextIndex;

        /// 스테이지를 새로 시작할 때 부른다.
        public void Reset() => nextIndex = 0;

        /// 지금 높이(m)를 넘겨준다. 새 구간에 들어가면 true 를 돌려준다.
        public bool CheckHeight(float meters)
        {
            if (nextIndex >= lines.Length || meters < lines[nextIndex].meters) return false;
            Say?.Invoke(lines[nextIndex].text);
            nextIndex++;
            return true;
        }

        public void Speak(string text) => Say?.Invoke(text);
    }
}
