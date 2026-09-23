using UnityEngine;

namespace UpTogether
{
    /// 게임 씬이 시작될 때, 선택 화면에서 고른 캐릭터·강아지를 실제로 적용한다.
    /// ★ 씬은 빌드 시점의 스프라이트로 굳어 있다 ★ — 런타임에 바꾸지 않으면
    /// 선택 화면에서 뭘 고르든 늘 같은 캐릭터가 나온다.
    /// 그래서 모든 세트를 미리 들고 있다가(빌드에 포함) id 로 골라 갈아끼운다.
    [DefaultExecutionOrder(-100)]   // Visual 들의 Awake(50/60)보다 먼저
    public class SelectionApplier : MonoBehaviour
    {
        [System.Serializable]
        public struct Entry { public string id; public CharacterSpriteSet set; }

        public Entry[] characters;
        public Entry[] breeds;

        public PlayerVisual playerVisual;
        public DogVisual dogVisual;

        void Awake()
        {
            var charSet = Find(characters, Selection.Character);
            var dogSet = Find(breeds, Selection.Breed);

            if (charSet != null && playerVisual != null)
            {
                playerVisual.spriteSet = charSet;
                if (playerVisual.target != null) playerVisual.target.sprite = charSet.Frame(4);
            }
            if (dogSet != null)
            {
                if (dogVisual != null)
                {
                    dogVisual.spriteSet = dogSet;
                    if (dogVisual.target != null) dogVisual.target.sprite = dogSet.Frame(4);
                }
                // 안기 오버레이도 같은 견종을 써야 한다
                if (playerVisual != null) playerVisual.dogSpriteSet = dogSet;
            }
        }

        static CharacterSpriteSet Find(Entry[] list, string id)
        {
            if (list == null) return null;
            foreach (var e in list) if (e.id == id) return e.set;
            return null;
        }
    }
}
