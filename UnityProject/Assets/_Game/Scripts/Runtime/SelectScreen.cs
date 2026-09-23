using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UpTogether
{
    /// 시작 화면. 캐릭터와 강아지를 골라서 게임 씬으로 넘어간다.
    /// 격자·버튼은 SceneBuilder 가 만들고, 여기서는 선택 상태와 미리보기를 다룬다.
    public class SelectScreen : MonoBehaviour
    {
        [Serializable]
        public struct Option
        {
            public string id;           // 에셋 이름 (girl, shiba ...)
            public string displayName;   // 한글 이름
            public CharacterSpriteSet set;
        }

        public Option[] characters;
        public Option[] breeds;
        public string gameScene = "Playground";

        [Header("연결")]
        public SpriteRenderer charPreview;
        public SpriteRenderer dogPreview;
        public Text charName;
        public Text dogName;
        /// 선택된 칸에 테두리를 씌우기 위해, 버튼 인덱스별 하이라이트
        public Image[] charHighlights;
        public Image[] dogHighlights;

        int charIndex;
        int dogIndex;

        void Start()
        {
            charIndex = Mathf.Max(0, IndexOf(characters, Selection.Character));
            dogIndex = Mathf.Max(0, IndexOf(breeds, Selection.Breed));
            Refresh();
        }

        static int IndexOf(Option[] opts, string id)
        {
            for (int i = 0; i < opts.Length; i++) if (opts[i].id == id) return i;
            return -1;
        }

        public void PickCharacter(int i) { charIndex = i; Refresh(); }
        public void PickBreed(int i) { dogIndex = i; Refresh(); }

        /// 선택을 저장한다 (씬 전환 없이). 테스트가 여기까지 검증한다.
        public void Commit()
        {
            Selection.Character = characters[charIndex].id;
            Selection.Breed = breeds[dogIndex].id;
        }

        public void StartGame()
        {
            Commit();
            SceneManager.LoadScene(gameScene);
        }

        // 테스트용 읽기
        public string CurrentCharacterId => characters[charIndex].id;
        public string CurrentBreedId => breeds[dogIndex].id;
        public Sprite CharPreviewSprite => charPreview != null ? charPreview.sprite : null;

        void Refresh()
        {
            var c = characters[charIndex];
            var d = breeds[dogIndex];

            if (charPreview != null && c.set != null) charPreview.sprite = c.set.Frame(4); // idle
            if (dogPreview != null && d.set != null) dogPreview.sprite = d.set.Frame(4);
            if (charName != null) charName.text = c.displayName;
            if (dogName != null) dogName.text = d.displayName;

            Highlight(charHighlights, charIndex);
            Highlight(dogHighlights, dogIndex);
        }

        static void Highlight(Image[] items, int selected)
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null) items[i].enabled = i == selected;
        }
    }
}
