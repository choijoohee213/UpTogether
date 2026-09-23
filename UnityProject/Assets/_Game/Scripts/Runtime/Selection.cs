using UnityEngine;

namespace UpTogether
{
    /// 선택 화면에서 고른 캐릭터·견종을 저장한다. 게임 씬이 이걸 읽어 스프라이트를 정한다.
    /// PlayerPrefs 라 다시 켜도 유지된다.
    public static class Selection
    {
        const string CharKey = "sel_character";
        const string BreedKey = "sel_breed";

        public const string DefaultCharacter = "girl";
        public const string DefaultBreed = "shiba";

        public static string Character
        {
            get => PlayerPrefs.GetString(CharKey, DefaultCharacter);
            set { PlayerPrefs.SetString(CharKey, value); PlayerPrefs.Save(); }
        }

        public static string Breed
        {
            get => PlayerPrefs.GetString(BreedKey, DefaultBreed);
            set { PlayerPrefs.SetString(BreedKey, value); PlayerPrefs.Save(); }
        }
    }
}
