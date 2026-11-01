using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UpTogether
{
    /// 프롤로그 — 발견 장면. **플레이 맵과 완전히 무관한 전용 씬이다.**
    ///
    /// 처음에는 Playground 안에서 카메라만 돌렸는데, 플레이하던 그 언덕에서
    /// 이야기가 진행되니 "다른 곳에서 만났다"가 전혀 안 느껴졌다. 그래서
    /// 비 내리는 거리를 따로 두고, 마지막 줄이 거리 → 언덕으로 넘긴다.
    ///
    /// 시점을 3인칭과 1인칭으로 교차한다. 멀리서 보다가 갑자기 내 눈이 되는
    /// 순간이 생겨야 "손을 내밀자 움츠렸다"가 남의 일이 아니게 된다.
    ///
    /// 그림(플레이트)은 아직 없다. 없으면 자리표시 판을 깔고 무엇이 들어갈
    /// 자리인지 화면에 적어 둔다 — 구성과 타이밍을 먼저 확인할 수 있다.
    [DefaultExecutionOrder(30)]
    public class PrologueScene : MonoBehaviour
    {
        /// 이 씬을 봤다고 적는 키. Narration 과 같은 저장소를 쓴다.
        public const string SeenKey = "prologue";

        /// 한 품종의 플레이트 한 벌. 고른 강아지로 이야기가 돌아가도록
        /// 1~4번 컷은 품종마다 따로 그렸다 (5번은 강아지가 없어 공용).
        [System.Serializable]
        public struct BreedPlates
        {
            public string breed;
            public Sprite[] plates;
        }

        public Cinematic cinematic;
        public Narration narration;
        public string nextScene = "Playground";

        [Header("플레이트 (그림이 들어오면 채운다. 비면 자리표시)")]
        public SpriteRenderer plate;
        public BreedPlates[] breedPlates;
        /// 비. 마지막 컷(비탈·집)은 비가 그친 하늘이라 그때 끈다.
        public Rain rain;
        /// 비를 멈출 컷 번호 (0부터)
        public int rainStopsAt = 4;
        /// 자리표시일 때 무엇이 들어갈 자리인지 적는다
        public Text slate;
        /// 자리표시 판에 씌우는 색 — 3인칭/1인칭을 구분해 보여준다
        public Color thirdTint = new Color(0.21f, 0.25f, 0.33f);
        public Color firstTint = new Color(0.33f, 0.26f, 0.24f);

        /// 각 컷에 무엇이 들어갈 자리인지. 그림이 없을 때만 화면에 적는다.
        static readonly string[] PlateNotes =
        {
            "골목 — 가로등 아래 젖은 상자, 그 안에 웅크린 것",
            "상자 앞 — 올려다보는 강아지, 목줄과 지워진 이름표",
            "손을 내밀자 — 강아지가 상자 밖으로 튀어나간다",
            "한참 떨어져서 — 이쪽을 보고 있다",
            "비탈 꼭대기 — 빈 집",
        };

        /// 지금 쓸 플레이트 한 벌. 고른 견종이 없으면 첫 벌로 떨어진다.
        Sprite[] Plates
        {
            get
            {
                if (breedPlates == null || breedPlates.Length == 0) return null;
                string want = Selection.Breed;
                foreach (var b in breedPlates)
                    if (b.breed == want && b.plates != null) return b.plates;
                return breedPlates[0].plates;
            }
        }

        void Start()
        {
            if (cinematic == null) { Done(); return; }

            // 이미 본 사람은 곧바로 넘긴다. 이 판단을 한 곳에만 두기 위해
            // Select 화면은 늘 이 씬으로 보내고, 건너뛸지는 여기서 정한다.
            if (narration != null && narration.Seen(SeenKey)) { Done(); return; }
            narration?.MarkSeen(SeenKey);

            cinematic.ShotStarted += ShowPlate;
            cinematic.Finished += Done;
            cinematic.Play(Shots());
        }

        void OnDestroy()
        {
            if (cinematic == null) return;
            cinematic.ShotStarted -= ShowPlate;
            cinematic.Finished -= Done;
        }

        /// 다섯 컷. 3인칭 → 1인칭 → 1인칭 → 3인칭 → 3인칭.
        ///
        /// 세 번째가 핵심이라 뒤에 2초 침묵과 낑낑 소리를 둔다. 1인칭으로
        /// 손이 들어오는 컷이어서, 거절당하는 것이 플레이어의 손이 된다.
        ///
        /// 구도는 **그림을 직접 보고** 잡았다.
        /// 받은 표의 y 값이 컷마다 제각각 어긋나 있었다 — 2번 컷은 이름표를
        /// 노린 값이 320px 아래를 가리켜서 목줄 대신 상자 바닥이 클로즈업됐다.
        ///
        /// 품종 플레이트를 서로 차분해 재려고 했다가 틀렸다. 품종마다 배경
        /// 팔레트도 달라서 차이나는 영역이 강아지만이 아니다. 눈으로 쟀다.
        /// (플레이트 픽셀 기준 피사체 범위, 테스트가 같은 값을 들고 있다)
        ///   컷1 강아지 x248~390 y835~920
        ///   컷2 이름표 x335~412 y588~645
        ///   컷3 강아지 x148~455 y495~760 · 내민 손 x495~700 y845~1070
        ///   컷4 강아지 x365~425 y752~835 · 사람 x230~360 y940~
        ///   컷5 집     x295~470 y415~650
        ///
        /// 플레이트는 750x1300, PPU 100 → 7.5x13 유닛, 중심 (0,0).
        ///   유닛x = (px − 375)/100,  유닛y = (650 − px)/100
        /// 시야 배수 s 에서 보이는 범위는 366s x 650s px 다.
        /// 가장자리가 새지 않게 Cinematic.plateHalf 가 한 번 더 가둔다.
        public Cinematic.Shot[] Shots()
        {
            var l = Narration.Prologue;
            return new[]
            {
                // 골목 거의 전체 → 상자 속 강아지로 내려온다
                new Cinematic.Shot { plate = 0, view = Cinematic.View.Third,
                                     from = new Vector2(0.00f, 0.00f), fromSize = 1.9f,
                                     at = new Vector2(-0.54f, -2.30f), size = 0.85f,
                                     seconds = 2.6f, line = l[0], holdAfter = 0.5f },
                // 상자 전체 → 목줄·이름표로 푸시인
                new Cinematic.Shot { plate = 1, view = Cinematic.View.First,
                                     from = new Vector2(0.25f, 0.10f), fromSize = 1.45f,
                                     at = new Vector2(0.00f, 0.30f), size = 0.70f,
                                     seconds = 1.8f, line = l[1], holdAfter = 0.4f },
                // 핵심. 느린 푸시인 — 손과 강아지 사이 틈을 깨지 않는다. 2초 침묵.
                // 강아지 주둥이(x148)부터 손끝(x700)까지 가로 552px 을 한 화면에
                // 담아야 해서 시야를 넓게 둔다. 더 당기면 주둥이가 잘린다.
                new Cinematic.Shot { plate = 2, view = Cinematic.View.First,
                                     from = new Vector2(0.46f, -0.65f), fromSize = 1.80f,
                                     at = new Vector2(0.49f, -1.30f), size = 1.55f,
                                     seconds = 1.6f, line = l[2], holdAfter = 2.0f, whine = true },
                // 고정. 멀리 앉은 강아지와 앞의 뒷모습이 둘 다 들어와야 한다
                new Cinematic.Shot { plate = 3, view = Cinematic.View.Third,
                                     from = new Vector2(0.11f, -3.41f), fromSize = 0.95f,
                                     at = new Vector2(0.11f, -3.41f), size = 0.95f,
                                     seconds = 0.8f, line = l[3], holdAfter = 0.5f },
                // 길 아래에서 집까지 올려다보고 게임으로 넘긴다
                new Cinematic.Shot { plate = 4, view = Cinematic.View.Third,
                                     from = new Vector2(-1.75f, -3.25f), fromSize = 1.0f,
                                     at = new Vector2(0.05f, 1.10f), size = 1.1f,
                                     seconds = 2.6f, line = l[4], holdAfter = 0.6f },
            };
        }

        void ShowPlate(int index, Cinematic.Shot shot)
        {
            // 마지막 컷에서 비가 그친다 — 하늘이 트이는 것이 회복의 첫 신호다
            if (rain != null) rain.SetFalling(index < rainStopsAt);

            if (plate == null) return;

            var set = Plates;
            bool hasArt = set != null && shot.plate >= 0
                       && shot.plate < set.Length && set[shot.plate] != null;

            if (hasArt)
            {
                plate.sprite = set[shot.plate];
                plate.color = Color.white;
                if (slate != null) slate.text = "";
                return;
            }

            // 자리표시: 시점에 따라 색을 달리하고, 무엇이 들어갈 자리인지 적는다
            plate.color = shot.view == Cinematic.View.First ? firstTint : thirdTint;
            if (slate == null) return;
            string note = shot.plate >= 0 && shot.plate < PlateNotes.Length
                        ? PlateNotes[shot.plate] : "";
            string who = shot.view == Cinematic.View.First ? "1인칭" : "3인칭";
            slate.text = $"[{who}]  컷 {index + 1}/5  ·  그림 자리\n{note}";
        }

        void Done()
        {
            if (string.IsNullOrEmpty(nextScene)) return;
            SceneManager.LoadScene(nextScene);
        }
    }
}
