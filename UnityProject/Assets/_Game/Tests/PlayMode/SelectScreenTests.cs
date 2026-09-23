using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UpTogether.Tests
{
    /// 선택 화면이 실제로 캐릭터·강아지를 고르고 그 선택을 저장하는지 본다.
    /// 브라우저 합성 클릭은 WebGL 에 전달되지 않을 수 있어, 여기서 배선을 확정한다.
    public class SelectScreenTests
    {
        SelectScreen screen;

        [UnitySetUp]
        public IEnumerator LoadSelect()
        {
            yield return SceneManager.LoadSceneAsync("Select", LoadSceneMode.Single);
            yield return null;
            screen = Object.FindFirstObjectByType<SelectScreen>();
            Assert.IsNotNull(screen, "Select 씬에 SelectScreen 이 없다");
        }

        [Test]
        public void 캐릭터와_견종이_모두_로드된다()
        {
            Assert.AreEqual(8, screen.characters.Length, "캐릭터 8종이어야 한다");
            Assert.AreEqual(7, screen.breeds.Length, "견종 7종이어야 한다");
            foreach (var c in screen.characters)
                Assert.IsNotNull(c.set, $"{c.id} 스프라이트 세트가 비었다");
        }

        [UnityTest]
        public IEnumerator 다른_칸을_고르면_미리보기가_바뀐다()
        {
            var before = screen.CharPreviewSprite;
            screen.PickCharacter(3);
            yield return null;
            Assert.AreNotSame(before, screen.CharPreviewSprite, "미리보기 스프라이트가 안 바뀌었다");
            Assert.AreEqual(screen.characters[3].id, screen.CurrentCharacterId);
        }

        [UnityTest]
        public IEnumerator 고른_뒤_저장하면_게임에_반영된다()
        {
            screen.PickCharacter(4);
            screen.PickBreed(2);
            yield return null;

            screen.Commit();

            Assert.AreEqual(screen.characters[4].id, Selection.Character, "캐릭터 선택이 저장되지 않았다");
            Assert.AreEqual(screen.breeds[2].id, Selection.Breed, "견종 선택이 저장되지 않았다");

            // 되돌려 다른 테스트에 영향 주지 않게
            Selection.Character = Selection.DefaultCharacter;
            Selection.Breed = Selection.DefaultBreed;
        }

        [UnityTest]
        public IEnumerator 저장한_선택이_게임씬에_실제로_적용된다()
        {
            // shiba 가 기본이니 다른 견종으로 골라 확인한다
            string wantChar = screen.characters[5].id;
            string wantBreed = screen.breeds[3].id;
            Selection.Character = wantChar;
            Selection.Breed = wantBreed;

            yield return SceneManager.LoadSceneAsync("Playground", LoadSceneMode.Single);
            yield return null;

            var pv = Object.FindFirstObjectByType<PlayerVisual>();
            var dv = Object.FindFirstObjectByType<DogVisual>();
            Assert.IsNotNull(pv, "게임씬에 PlayerVisual 이 없다");
            Assert.AreEqual(wantChar, pv.spriteSet.displayName, "고른 캐릭터가 게임에 반영되지 않았다");
            Assert.AreEqual(wantBreed, dv.spriteSet.displayName, "고른 견종이 게임에 반영되지 않았다");
            Assert.AreEqual(wantBreed, pv.dogSpriteSet.displayName, "안기 오버레이 견종이 안 맞는다");

            Selection.Character = Selection.DefaultCharacter;
            Selection.Breed = Selection.DefaultBreed;
        }

        [Test]
        public void 저장된_씬의_탭들이_화면을_참조한다()
        {
            // ★ 원래 버그를 잡는 검사 ★
            // 예전엔 Button.onClick 에 람다를 AddListener 했는데, 그건 씬 저장 때 사라져
            // 빌드된 씬의 버튼이 아무 반응도 안 했다. SelectTap 은 screen 참조를 직렬화한다.
            var taps = Object.FindObjectsByType<SelectTap>(FindObjectsSortMode.None);
            Assert.AreEqual(8 + 7 + 1, taps.Length, "칸 8+7 + 시작 1 = 16개여야 한다");

            int chars = 0, breeds = 0, starts = 0;
            foreach (var t in taps)
            {
                Assert.IsNotNull(t.screen, $"{t.name}({t.kind}) 이 screen 참조를 잃었다 (직렬화 실패)");
                if (t.kind == SelectTap.Kind.Character) chars++;
                else if (t.kind == SelectTap.Kind.Breed) breeds++;
                else starts++;
            }
            Assert.AreEqual(8, chars);
            Assert.AreEqual(7, breeds);
            Assert.AreEqual(1, starts);
        }

        [UnityTest]
        public IEnumerator 탭하면_실제로_선택이_바뀐다()
        {
            // 컴포넌트의 OnPointerDown 을 직접 호출 — 버튼 배선 전체를 탄다
            SelectTap target = null;
            foreach (var t in Object.FindObjectsByType<SelectTap>(FindObjectsSortMode.None))
                if (t.kind == SelectTap.Kind.Breed && t.index == 4) { target = t; break; }
            Assert.IsNotNull(target, "5번째 견종 탭을 못 찾았다");

            target.OnPointerDown(null);
            yield return null;
            Assert.AreEqual(screen.breeds[4].id, screen.CurrentBreedId, "탭했는데 선택이 안 바뀌었다");
        }
    }
}
