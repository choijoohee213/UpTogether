using UnityEngine;

namespace UpTogether
{
    /// 타이틀 화면에서 마지막에 고른 캐릭터·강아지를 포옹 자세로 보여준다.
    /// ★ 씬은 빌드 시점 스프라이트로 굳는다 ★ — 그래서 모든 세트를 들고 있다가
    /// 런타임에 Selection 으로 골라 갈아끼운다 (SelectionApplier 와 같은 이유).
    ///
    /// 포옹은 3겹이다: 캐릭터 본체 → 강아지 → 앞팔. 위치·좌우반전은 씬에서 잡아두고,
    /// 여기서는 프레임만 두 장씩(본체 18↔19 / 팔 20↔21 / 강아지 11↔12) 번갈아 재생한다.
    [DefaultExecutionOrder(-50)]
    public class TitleHug : MonoBehaviour
    {
        public SelectionApplier.Entry[] characters;
        public SelectionApplier.Entry[] breeds;

        public SpriteRenderer body;
        public SpriteRenderer dog;
        public SpriteRenderer arms;

        static readonly int[] BodyFrames = { 18, 19 };
        static readonly int[] ArmsFrames = { 20, 21 };
        static readonly int[] DogFrames = { 11, 12 };
        const float Interval = 0.42f;

        CharacterSpriteSet cset, dset;
        int i;
        float t;

        void Start()
        {
            cset = Find(characters, Selection.Character);
            dset = Find(breeds, Selection.Breed);
            if (cset == null || dset == null || body == null) { enabled = false; return; }
            Apply(0);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t < Interval) return;
            t -= Interval;
            i ^= 1;
            Apply(i);
        }

        void Apply(int k)
        {
            if (body != null) body.sprite = cset.Frame(BodyFrames[k]);
            if (arms != null) arms.sprite = cset.Frame(ArmsFrames[k]);
            if (dog != null) dog.sprite = dset.Frame(DogFrames[k]);
        }

        static CharacterSpriteSet Find(SelectionApplier.Entry[] list, string id)
        {
            if (list == null || list.Length == 0) return null;
            foreach (var e in list) if (e.id == id) return e.set;
            return list[0].set;
        }
    }
}
