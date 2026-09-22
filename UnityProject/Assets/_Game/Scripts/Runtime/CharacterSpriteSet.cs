using UnityEngine;

namespace UpTogether
{
    /// 견종 한 마리(또는 캐릭터 하나)의 스프라이트와 클립 표.
    /// 임포터가 시트 PNG + JSON 에서 만들어낸다. 손으로 고치지 말 것 — 다시 임포트하면 덮어쓴다.
    ///
    /// ★ 이 클래스는 반드시 CharacterSpriteSet.cs 에 있어야 한다 ★
    /// Unity는 .cs 파일 하나당 MonoScript 하나를 파일명으로 만든다.
    /// 클래스명과 파일명이 다르면 에셋의 m_Script 가 0 으로 저장되고,
    /// AssetDatabase.LoadAssetAtPath<CharacterSpriteSet> 이 null 을 돌려준다.
    [CreateAssetMenu(menuName = "UpTogether/Character Sprite Set", fileName = "SpriteSet")]
    public class CharacterSpriteSet : ScriptableObject
    {
        public string displayName;
        public Sprite[] frames;
        public SpriteClip[] clips;

        public SpriteClip Find(string clipName)
        {
            if (clips == null) return null;
            foreach (var c in clips) if (c.name == clipName) return c;
            return null;
        }

        public Sprite Frame(int i) => frames != null && i >= 0 && i < frames.Length ? frames[i] : null;
    }
}
