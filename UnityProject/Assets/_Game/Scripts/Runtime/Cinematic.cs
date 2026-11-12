using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UpTogether
{
    /// 인게임 시네마틱. 영상 파일이 아니라 게임 씬이다 — 카메라가 움직이고
    /// 조작이 끊기고 글이 한 줄씩 뜬다. 플레이어 눈에는 영상으로 보인다.
    ///
    /// 그림은 정지화만 쓴다(STORY.md 7장). 움직임은 전부 여기서 카메라와
    /// 기존 스프라이트로 만든다 — 픽셀 애니메이션을 그리기 시작하면 컷당
    /// 프레임 수만큼 비용이 곱해져서 끝나지 않는다.
    ///
    /// timeScale 을 0 으로 멈추지 않는다. 강아지 떨림처럼 연출이 계속 돌아야
    /// 하기 때문이다. 대신 GameInput 을 끊어 플레이어만 멈춘다.
    [DefaultExecutionOrder(60)]
    public class Cinematic : MonoBehaviour
    {
        /// 보는 시점. 교차시키면 같은 장면이 훨씬 가깝게 느껴진다 —
        /// 멀리서 보다가(3인칭) 갑자기 내 눈이 되는(1인칭) 순간이 생긴다.
        public enum View { Third, First }

        /// 한 컷. 플레이트(배경 그림)를 갈고, 카메라가 at 으로 seconds 동안
        /// 옮겨가고, 글이 뜨고, 탭을 기다린다.
        [Serializable]
        public struct Shot
        {
            /// 이 컷에서 보여줄 플레이트 번호. -1 이면 앞 컷 그림을 그대로 쓴다
            /// (한 장 안에서 카메라만 옮겨 여러 컷을 뽑는 방식).
            public int plate;
            public View view;
            /// 컷이 시작될 때 카메라를 여기로 붙인다. fromSize 가 0 이면
            /// 앞 컷에서 이어서 움직인다 (같은 플레이트 안을 훑을 때).
            /// 컷마다 그림이 바뀌면 이어 움직일 자리가 없으므로 시작점을 준다.
            public Vector2 from;
            public float fromSize;
            /// 카메라가 가서 멈출 지점 (월드 유닛)
            public Vector2 at;
            /// 평소 시야의 몇 배로 볼지 (1 = 평소, 작을수록 클로즈업)
            public float size;
            /// 그 지점으로 옮겨가는 시간
            public float seconds;
            [TextArea] public string line;
            /// 글이 뜬 뒤 탭을 받기 전까지 두는 침묵
            public float holdAfter;
            /// 이 컷에서 강아지 낑낑 소리를 낼지
            public bool whine;
        }

        /// 컷이 시작될 때 (컷 번호, 그 컷). 플레이트를 갈아끼우는 쪽이 듣는다.
        public event Action<int, Shot> ShotStarted;

        /// 탭이 너무 빨리 먹히면 컷이 스쳐 지나간다. 이만큼은 못 넘긴다.
        const float MinShotSeconds = 0.45f;
        const float BarsInSeconds = 0.35f;
        const float BarsOutSeconds = 0.3f;
        /// 글이 떠오르는 시간. 툭 나타나면 자막이 아니라 알림처럼 보인다.
        const float LineFadeSeconds = 0.5f;
        /// 레터박스 띠 높이 (캔버스 1080x1920 기준)
        const float BarHeight = 190f;
        /// 시야를 정수배로 맞추느라 이만큼 넘게 바뀌면 맞추지 않는다
        const float SnapTolerance = 0.1f;

        public Camera cam;
        public FollowCamera follow;
        public GameInput input;
        public Tuning tuning;

        /// 플레이트 반크기(유닛). 0 이 아니면 카메라가 그림 밖을 비추지 않게 가둔다.
        /// SnapSize 가 시야를 정수배로 맞추느라 조금 넓힐 수 있고, 화면 비율도
        /// 기기마다 달라서, 손으로 잡은 값만으로는 가장자리가 새는 일이 생긴다.
        public Vector2 plateHalf;

        [Header("연결 (SceneBuilder 가 만든다)")]
        public CanvasGroup group;
        public RectTransform barTop;
        public RectTransform barBottom;
        public Text label;

        public bool Running { get; private set; }

        /// 컷신이 끝난 순간. 조작은 이때 돌아온다.
        public event Action Finished;

        CanvasGroup labelGroup;
        Coroutine lineFade;

        void Awake()
        {
            if (group != null) group.alpha = 0f;
            SetBars(0f);
            if (label == null) return;
            label.text = "";
            // 글자와 그림자를 같이 흐리려면 묶어서 다뤄야 한다.
            // ?? 를 쓰면 안 된다 — 없는 컴포넌트는 진짜 null 이 아니라서
            // 가짜 null 이 그대로 들어오고, 쓸 때 터진다.
            labelGroup = label.GetComponent<CanvasGroup>();
            if (labelGroup == null) labelGroup = label.gameObject.AddComponent<CanvasGroup>();
            labelGroup.alpha = 0f;
        }

        public void Play(Shot[] shots)
        {
            if (Running || shots == null || shots.Length == 0) return;
            StartCoroutine(Run(shots));
        }

        IEnumerator Run(Shot[] shots)
        {
            Running = true;
            if (follow != null) follow.enabled = false;
            if (input != null) input.Blocked = true;
            if (group != null) group.alpha = 1f;

            // 띠가 열리는 동안 빈 판이 보이면 안 된다. 첫 컷 그림을 먼저 깐다.
            int shown = -1;
            if (shots.Length > 0) { ShotStarted?.Invoke(0, shots[0]); shown = 0; }

            yield return Bars(0f, 1f, BarsInSeconds);

            for (int i = 0; i < shots.Length; i++)
            {
                var shot = shots[i];
                // 플레이트를 먼저 갈아끼운다 — 카메라가 움직이기 전에 그림이 바뀌어야 한다
                if (i != shown) ShotStarted?.Invoke(i, shot);

                // 글을 먼저 띄우고 카메라가 움직이는 동안 읽게 한다.
                // 이동이 끝난 뒤에 띄우면 첫 컷은 2.6초 동안 읽을 것이 없다.
                SetLine(shot.line);
                yield return MoveTo(shot);

                if (shot.whine) Sfx.I?.Whine();

                float wait = Mathf.Max(MinShotSeconds, shot.holdAfter);
                for (float t = 0f; t < wait; t += Time.unscaledDeltaTime) yield return null;
                while (!Advanced()) yield return null;
            }

            SetLine("");
            yield return Bars(1f, 0f, BarsOutSeconds);
            if (group != null) group.alpha = 0f;

            // 카메라를 플레이어에게 돌려주고 시야를 원래대로
            if (cam != null && tuning != null) cam.orthographicSize = tuning.CameraOrthoSize;
            if (follow != null) { follow.enabled = true; follow.Snap(); }
            if (input != null) input.Blocked = false;

            Running = false;
            Finished?.Invoke();
        }

        /// 한 줄을 띄운다 — 떠오르듯이.
        void SetLine(string text)
        {
            if (label == null) return;
            label.text = text;
            if (labelGroup == null) return;
            if (lineFade != null) StopCoroutine(lineFade);
            labelGroup.alpha = 0f;
            if (string.IsNullOrEmpty(text)) return;
            lineFade = StartCoroutine(FadeLine());
        }

        IEnumerator FadeLine()
        {
            for (float t = 0f; t < LineFadeSeconds; t += Time.unscaledDeltaTime)
            {
                labelGroup.alpha = t / LineFadeSeconds;
                yield return null;
            }
            labelGroup.alpha = 1f;
            lineFade = null;
        }

        /// 카메라를 그 컷의 지점·시야로 옮긴다.
        IEnumerator MoveTo(Shot shot)
        {
            if (cam == null || tuning == null) yield break;

            Vector3 from = cam.transform.position;
            float sizeFrom = cam.orthographicSize;

            // 시작점이 주어졌으면 거기에 붙이고 출발한다. 컷마다 그림이 바뀌므로
            // 앞 컷이 끝난 자리에서 이어 움직이면 엉뚱한 곳을 훑게 된다.
            if (shot.fromSize > 0f)
            {
                sizeFrom = SnapSize(tuning.CameraOrthoSize * shot.fromSize);
                from = Inside(new Vector3(shot.from.x, shot.from.y, from.z), sizeFrom);
                cam.transform.position = from;
                cam.orthographicSize = sizeFrom;
            }

            float sizeTo = SnapSize(tuning.CameraOrthoSize * Mathf.Max(0.05f, shot.size));
            var to = Inside(new Vector3(shot.at.x, shot.at.y, from.z), sizeTo);

            float dur = Mathf.Max(0.01f, shot.seconds);
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                // 양끝이 부드럽게 — 선형으로 가면 기계가 움직이는 것처럼 보인다
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                float size = Mathf.Lerp(sizeFrom, sizeTo, k);
                cam.orthographicSize = size;
                cam.transform.position = Inside(Vector3.Lerp(from, to, k), size);
                yield return null;
            }
            cam.orthographicSize = sizeTo;
            cam.transform.position = to;
        }

        /// 그림 밖이 보이지 않게 카메라를 플레이트 안으로 밀어 넣는다.
        /// 시야가 그림보다 크면 가운데로 둔다 (한쪽만 새는 것보다 낫다).
        Vector3 Inside(Vector3 pos, float orthoSize)
        {
            if (plateHalf == Vector2.zero || cam == null) return pos;
            float halfH = orthoSize;
            float halfW = orthoSize * cam.aspect;
            pos.x = Clamp(pos.x, plateHalf.x - halfW);
            pos.y = Clamp(pos.y, plateHalf.y - halfH);
            return pos;
        }

        static float Clamp(float v, float limit)
            => limit <= 0f ? 0f : Mathf.Clamp(v, -limit, limit);

        /// 화면 픽셀과 게임 픽셀의 비를 정수로 맞춘다.
        ///
        /// 컷신은 정지화를 크게·오래 보여주므로 비가 정수가 아니면 픽셀이 뭉개져
        /// 바로 보인다. 평소 플레이에서는 캐릭터가 작고 계속 움직여서 티가 안 난다.
        /// Pixel Perfect Camera 를 전역으로 켜면 FollowCamera 가 orthographicSize 를
        /// 직접 세팅하는 것과 충돌하므로, 범위를 컷신으로 한정한다.
        float SnapSize(float want)
        {
            float screenH = Screen.height;
            float gamePx = want * 2f * Px.PPU;
            if (screenH <= 0f || gamePx <= 0f) return want;
            int k = Mathf.Max(1, Mathf.RoundToInt(screenH / gamePx));
            float snapped = screenH / (2f * Px.PPU * k);

            // 조금 밀어 맞추는 것까지만 받는다. 넓은 컷에서는 k 가 1~2 라
            // 반올림 한 칸이 시야를 25~50% 씩 바꿔 버린다 — 기기 화면 높이에
            // 따라 구도가 통째로 달라지고 피사체가 잘린다. 선명함보다
            // 무엇이 화면에 잡히느냐가 먼저다.
            return Mathf.Abs(snapped - want) > want * SnapTolerance ? want : snapped;
        }

        IEnumerator Bars(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetBars(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            SetBars(to);
        }

        void SetBars(float k)
        {
            float h = BarHeight * Mathf.Clamp01(k);
            if (barTop != null) barTop.sizeDelta = new Vector2(0f, h);
            if (barBottom != null) barBottom.sizeDelta = new Vector2(0f, h);
        }

        /// 다음 컷으로. 탭·클릭·스페이스를 받는다.
        /// 전체 화면 버튼을 두지 않은 이유는 조작 버튼들이 아래에 깔려 있고
        /// 컷신 동안은 어차피 입력이 끊겨 있어서다.
        static bool Advanced()
        {
            if (Input.GetMouseButtonDown(0)) return true;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) return true;
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
            return false;
        }
    }
}
