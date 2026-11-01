using System;
using UnityEngine;

namespace UpTogether
{
    /// 서사 한 줄. STORY.md 의 유기견 아크를 담는다.
    ///
    /// 세 층으로 뜬다.
    ///   1. 높이 — 구간을 넘을 때 (대부분은 친밀도만 올리고 글은 안 띄운다)
    ///   2. 스테이지 — 오프닝·엔딩 한 줄씩
    ///   3. 사건 — 처음 혼자 뛴 날, 처음 안긴 날, 떨어졌는데 안 온 날
    ///
    /// 톤 규칙(STORY.md 1장): 과거를 묘사하지 않는다. 폭력은 한 번도 보여주지 않고
    /// 흔적만 남긴다. 한 줄은 두 문장 이내, 담백한 관찰체. 수식어를 붙이지 않는다.
    /// 화자는 강아지를 재촉하지 않는다 — 안 오면 "아직은 이게 맞다"고 넘긴다.
    public class Narration : MonoBehaviour
    {
        const string SeenPrefix = "climb2_story_";

        [Serializable]
        public struct Line
        {
            public float meters;
            [TextArea] public string text;
        }

        /// 높이 구간. 넘으면 친밀도 +5 — 글은 text 가 있는 줄만 띄운다.
        /// 빈 줄을 남겨 둔 이유: 문턱을 지우면 한 판에서 얻는 친밀도가 30 에서 10 으로
        /// 줄어 PROJECT.md "검증된 수치"가 깨진다. 문턱은 여섯 개 그대로 두고 글만 덜어냈다.
        public static readonly Line[] Default =
        {
            new Line { meters = 10f,  text = "" },   // 이 높이에서 강아지는 아직 경계한다. 글이 없는 게 맞다
            new Line { meters = 28f,  text = "뒤돌아보니 강아지가 발판 끝에서 망설이고 있다." },
            new Line { meters = 48f,  text = "" },   // → 친밀도 '한 몸' 단계 대사로 옮겼다
            new Line { meters = 72f,  text = "" },   // → 친밀도 '품에 안김' 단계 대사로 옮겼다
            new Line { meters = 100f, text = "" },   // → 친밀도 '영혼의 단짝' 단계 대사로 옮겼다
            new Line { meters = 130f, text = "둘 다 숨이 차지만 아무도 멈추자고 하지 않는다." },
        };

        /// 프롤로그 — 발견 장면. 전용 씬(Prologue)에서 한 줄씩 띄운다.
        ///
        /// 장소는 플레이 맵(언덕)이 아니라 비 내리는 거리다. 발견은 다른 곳에서
        /// 일어나고, 마지막 줄이 거리 → 언덕으로 넘겨 게임 맵을 가리킨다.
        ///
        /// 세 번째 줄이 이 기획의 핵심이다. 학대를 한 글자도 쓰지 않고 전달한다 —
        /// "아직 아무것도 하지 않았는데"가 나머지를 플레이어에게 채우게 한다.
        /// 그래서 이 줄만 1인칭이다: 편 손이 들어오고 강아지가 상자 밖으로 튀어나간다.
        /// (그림이 '움츠림'이 아니라 '뛰쳐나감'으로 와서 줄을 거기 맞췄다 —
        ///  글과 그림이 어긋나면 둘 다 안 읽힌다.)
        public static readonly string[] Prologue =
        {
            "비 내리는 골목 끝, 가로등 아래 젖은 상자 안에 작은 것이 웅크리고 있었다.",
            "목에 낡은 목줄이 남아 있었다. 이름 칸은 지워져 있었다.",
            "손을 내밀자 강아지가 상자 밖으로 튀어나갔다. 아직 아무것도 하지 않았는데.",
            "한참 떨어져서 이쪽을 보고 있었다. 걸음을 옮기자, 그만큼 떨어져서 따라왔다.",
            "길이 끝나는 곳에 비탈이 있었다. 그 꼭대기에 빈 집이 하나 있다.",
        };

        /// 비는 그림이 아니라 코드로 내린다 (Rain). 마지막 컷에서 그친다 —
        /// 그림의 트인 하늘과 맞아야 해서, 그 컷에서만 비를 끈다.

        /// 스테이지 오프닝. 경계 → 관찰 → 시도 → 신뢰 → 역전.
        public static readonly string[] StageOpen =
        {
            "강아지는 세 발판쯤 뒤에서 멈춰 섰다. 더 오지도, 돌아가지도 않았다.",
            "발판이 움직인다. 강아지가 발을 올렸다가 다시 뺐다.",
            "어두워졌다. 강아지가 처음으로 내 쪽에 붙어 섰다.",
            "미끄럽다. 강아지가 네 발을 다 쓰며 조심조심 디뎠다.",
            "돌이 떨어지는 곳이다. 강아지가 내 앞으로 나와 섰다.",
        };

        /// 스테이지 엔딩. 마지막 줄이 프롤로그의 "빈 집"을 닫는다.
        public static readonly string[] StageClose =
        {
            "꼭대기에서 기다렸다. 한참 뒤에 강아지가 올라왔다.",
            "오늘은 두 발판 뒤에서 따라왔다. 한 칸 줄었다.",
            "내려다보니 지나온 발판이 별처럼 흩어져 있었다. 강아지도 같이 내려다봤다.",
            "추운 데서 강아지가 내 옆에 붙어 앉았다. 비키지 않았다.",
            "빈 집 문 앞에서 강아지가 먼저 들어갔다. 그리고 돌아서서 짖었다.",
        };

        /// 친밀도 단계가 올라간 순간. Bond.Stages 와 같은 순서 (0번은 초기값이라 비어 있다).
        public static readonly string[] BondUp =
        {
            "",
            "눈이 마주쳤다. 바로 피하지는 않았다.",
            "부르지 않았는데 옆에 와 있었다.",
            "이제 떨어지면 같이 떨어진다.",
            "뛰기 전에 강아지가 먼저 자세를 잡는다.",
            "앞장서서 발판을 확인하기 시작했다.",
        };

        public const string OnFirstSelfJump = "처음으로 혼자 뛰었다. 착지하고 나서 나를 확인했다.";
        public const string OnFirstCling = "떨어지는데 무언가 품으로 뛰어들었다. 처음이었다.";
        public const string OnCling = "강아지가 달려와 품에 안겼다.";
        public const string OnBigFall = "많이 떨어졌지만, 강아지는 끝까지 안겨 있었다.";
        /// 안기기가 아직 안 열렸을 때. 플레이어가 강아지에게 실망하지 않게 하는 줄이다.
        public const string OnFallAlone = "올려다보니 강아지는 그 자리에 있었다. 아직은 이게 맞다.";
        public const string OnFlashback = "강아지가 다시 거리를 벌렸다. 서두르지 않기로 했다.";
        public const string OnFlashbackOver = "다시 돌아왔다. 아까보다 빠르게.";

        public Line[] lines = Default;

        /// 띄울 문장. UI 가 듣는다.
        public event Action<string> Say;

        int nextIndex;

        /// 스테이지를 새로 시작할 때 부른다. (높이 구간만 되돌린다 —
        /// 한 번 본 사건 대사는 PlayerPrefs 에 남아 다시 뜨지 않는다.)
        public void Reset() => nextIndex = 0;

        /// 지금 높이(m)를 넘겨준다. 새 구간에 들어가면 true 를 돌려준다.
        /// 글이 비어 있는 구간도 true 다 — 친밀도는 올라야 한다.
        public bool CheckHeight(float meters)
        {
            if (nextIndex >= lines.Length || meters < lines[nextIndex].meters) return false;
            string text = lines[nextIndex].text;
            if (!string.IsNullOrEmpty(text)) Say?.Invoke(text);
            nextIndex++;
            return true;
        }

        public void Speak(string text)
        {
            if (!string.IsNullOrEmpty(text)) Say?.Invoke(text);
        }

        /// 한 번 띄운 줄은 다시 띄우지 않는다. 스테이지를 다시 해도 안 뜬다.
        /// 관계의 사건은 "처음"이라서 뜻이 있다 — 두 번째는 뜻이 없다.
        /// 띄웠으면 true.
        public bool SpeakOnce(string key, string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string k = SeenPrefix + key;
            if (PlayerPrefs.GetInt(k, 0) != 0) return false;
            PlayerPrefs.SetInt(k, 1);
            Say?.Invoke(text);
            return true;
        }

        /// 이미 띄운 줄인가 (글은 안 띄우고 확인만)
        public bool Seen(string key) => PlayerPrefs.GetInt(SeenPrefix + key, 0) != 0;

        /// 글 없이 "봤다"고만 적는다. 컷신처럼 이 클래스가 띄우지 않는 것에 쓴다.
        public void MarkSeen(string key) => PlayerPrefs.SetInt(SeenPrefix + key, 1);

        /// 개발용. 본 줄 기록을 전부 지운다 — 안 그러면 한 번 돌린 뒤로는
        /// 대사를 다시 확인할 방법이 없다. DevPanel 이 부른다.
        public void ForgetAll()
        {
            for (int i = 0; i < StageOpen.Length; i++)
            {
                PlayerPrefs.DeleteKey(SeenPrefix + "open" + i);
                PlayerPrefs.DeleteKey(SeenPrefix + "close" + i);
            }
            for (int i = 0; i < BondUp.Length; i++) PlayerPrefs.DeleteKey(SeenPrefix + "bond" + i);
            foreach (string k in new[] { "prologue", "selfjump", "firstcling",
                                         "fallalone", "flashback", "flashbackover" })
                PlayerPrefs.DeleteKey(SeenPrefix + k);
        }
    }
}
