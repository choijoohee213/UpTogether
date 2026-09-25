using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 씬에 오디오 재생기(Sfx)를 붙이고 클립을 채운다. 세 씬 빌더가 공용으로 쓴다.
    public static class AudioSetup
    {
        const string SfxDir = "Assets/_Game/Audio/SFX";
        const string BgmDir = "Assets/_Game/Audio/BGM";

        public static Sfx Attach()
        {
            var go = new GameObject("Audio");
            var s = go.AddComponent<Sfx>();
            s.bgm = Clip($"{BgmDir}/bgm_musicbox_loop.wav");
            s.jump = Clip($"{SfxDir}/player_jump.wav");
            s.land = Clip($"{SfxDir}/player_land.wav");
            s.whine = Clip($"{SfxDir}/dog_whine.wav");
            s.catchDog = Clip($"{SfxDir}/dog_hug_catch.wav");
            s.bondUp = Clip($"{SfxDir}/affection_up.wav");
            s.bondDown = Clip($"{SfxDir}/affection_down.wav");
            s.reward = Clip($"{SfxDir}/reward_get.wav");
            s.clear = Clip($"{SfxDir}/stage_clear.wav");
            s.uiClick = Clip($"{SfxDir}/ui_click.wav");
            s.uiPopup = Clip($"{SfxDir}/ui_popup_open.wav");
            s.step1 = Clip($"{SfxDir}/player_step_1.wav");
            s.step2 = Clip($"{SfxDir}/player_step_2.wav");
            s.step3 = Clip($"{SfxDir}/player_step_3.wav");
            return s;
        }

        static AudioClip Clip(string path)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (c == null) Debug.LogWarning($"오디오 클립을 못 찾음: {path}");
            return c;
        }
    }
}
