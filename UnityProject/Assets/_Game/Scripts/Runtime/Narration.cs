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
    /// (프롤로그만 세 문장까지 둔다 — 컷마다 탭을 기다려 머무는 시간이 길다.)
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
        /// **슬픔은 전부 강아지 쪽에 있다.** 화자에게 사연을 주는 안을 써 봤다가 뺐다.
        /// 화자가 자기 이야기를 시작하면 시선이 그쪽으로 옮겨 가고, 강아지는 그
        /// 이야기의 소품이 된다. 화자는 끝까지 보는 사람으로만 둔다.
        /// 보면서 묻는 것까지는 화자의 몫이다 — 자기 과거를 꺼내지 않는 선에서.
        ///
        /// 짜임은 **사실 → 화자의 물음**이다. 먼저 본 것을 적고, 그 다음에 묻는다.
        /// 묻기만 하고 답하지 않는다 — 과거를 설명해 버리면 강아지는 '사연'이 되고
        /// 플레이어가 상상할 몫이 없어진다. 흔적만 남기는 1장 톤 규칙이 여기 걸린다.
        ///   1줄 "상자는 열려 있었다"         — 나갈 수 있었는데 안 나갔다. 아직 기다린다.
        ///   1줄 "슬픈 눈이 보였다"           — 화자가 본 것. 여기서 처음 마음이 움직인다.
        ///   2줄 "이름 칸만 깨끗하게 지워져"   — 다 낡았는데 거기만 깨끗하다. 지우고 버렸다.
        ///   2줄 "주인에게 버려진 걸까."       — 그 사실 앞에서 나오는 물음. 답은 없다.
        ///   3줄 "아직 아무것도 하지 않았는데" — 편 손에서 도망친다. 손이 가르친 것이다.
        ///   3줄 "어떤 상처가 남아 있는 걸까." — 상처를 보여주지 않고 묻기만 한다.
        ///   4줄 "짖지도 울지도 않았다"       — 울어도 소용없다는 걸 이미 배웠다.
        ///   4줄 "딱 그만큼 떨어져서"         — 못 오지만 가고는 싶다. 이 간격이 곧 게임이다.
        ///
        /// 묻는 줄은 2·3번에만 둔다. 매 컷 물으면 화자가 혼자 떠드는 것처럼 되고,
        /// 4·5번은 묻는 자리가 아니라 지켜보고 결심하는 자리다.
        ///
        /// 세 번째 줄만 1인칭이다 (편 손이 들어오는 컷). 거절당하는 것이 플레이어의
        /// 손이어야 한다. 그림이 '움츠림'이 아니라 '뛰쳐나감'으로 와서 줄을 거기 맞췄다 —
        /// 글과 그림이 어긋나면 둘 다 안 읽힌다.
        ///
        /// 네 번째 줄이 묻지 않고도 세 문장인 것은 그림 때문이다: 멈춰 선 뒷모습과 멀리 앉은
        /// 강아지가 한 장에 있어서, 소리 없는 것과 그만큼 떨어져 따라오는 것이
        /// 같은 컷에 붙어야 한다.
        ///
        /// 다섯 번째 줄이 게임을 연다. "빈 집이 하나 있다"는 지도였고, 지도는
        /// 아무도 울리지 않는다. 결심으로 바꿨다 — 다만 "거기까지만"이다.
        /// 크게 약속하지 않는 편이 더 믿긴다. 나머지는 친밀도가 벌어야 한다.
        /// 그 집은 5번 플레이트의 개집이고 스테이지 목표물과 같은 것이다.
        public static readonly string[] Prologue =
        {
            "비 내리는 골목 끝, 젖은 상자 안에 작은 것이 웅크리고 있었다. 상자는 열려 있었다. 슬픈 눈이 보였다.",
            "낡은 목줄이 그대로 있었다. 이름 칸만 깨끗하게 지워져 있었다. 주인에게 버려진 걸까.",
            "손을 내밀자 강아지가 상자 밖으로 튀어나갔다. 아직 아무것도 하지 않았는데. 어떤 상처가 남아 있는 걸까.",
            "한참 떨어져서 이쪽을 보고 있었다. 짖지도 울지도 않았다. 걸음을 옮기자, 딱 그만큼 떨어져서 따라왔다.",
            "비탈 꼭대기에 아직 비어 있는 집이 하나 있다. 거기까지만 같이 가보기로 했다.",
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
