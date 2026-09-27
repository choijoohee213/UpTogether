using System;
using UnityEngine;

namespace UpTogether
{
    /// 스테이지 진행: 강아지집(골)에 닿아 클리어하면 톡 눌러 다음 언덕으로.
    /// 스테이지마다 배경·타일 팔레트(테마)를 바꿔 분위기를 전환한다.
    /// 친밀도는 이어진다 — 여정 내내 관계가 쌓인다.
    public class StageFlow : MonoBehaviour
    {
        [Serializable]
        public class Theme
        {
            public Sprite sky, far, mid;
            public Sprite[] tiles;
            public Color bgColor = new Color(0.698f, 0.871f, 0.937f);
        }

        public StageData[] stages;
        public Theme[] themes;

        public StageRunner runner;
        public ForestBackdrop backdrop;
        public Collectibles collectibles;
        public CharacterBody player, dog;
        public StageSession session;
        public FollowCamera follow;
        public GameHud hud;
        public GoalFlag flag;
        public Camera cam;

        int index;

        /// 다음 스테이지로 (마지막 다음은 처음으로 돈다).
        public void Next()
        {
            if (stages == null || stages.Length == 0) return;
            index = (index + 1) % stages.Length;
            LoadStage(index);
        }

        void LoadStage(int i)
        {
            var st = stages[i];
            var th = themes != null && themes.Length > 0 ? themes[Mathf.Min(i, themes.Length - 1)] : null;

            if (th != null && runner != null) runner.tiles = th.tiles;
            if (runner != null) runner.Load(st);              // 발판·장애물·타일 새로 그림
            if (collectibles != null) collectibles.Rebuild(); // 간식·링 새로
            if (th != null && backdrop != null)
            {
                backdrop.sky = th.sky; backdrop.far = th.far; backdrop.mid = th.mid;
                backdrop.Rebuild();
            }
            if (th != null && cam != null) cam.backgroundColor = th.bgColor;

            if (player != null) player.Teleport(0.90f, st.groundY);
            if (dog != null) dog.Teleport(0.50f, st.groundY);
            if (flag != null) flag.Reposition();
            if (session != null) session.ResetForNewStage();
            if (hud != null) hud.HideClear();
            if (follow != null) follow.Snap();
        }
    }
}
