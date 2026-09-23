using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UpTogether.Tests
{
    /// 메인 화면이 선택 화면으로 넘어가는 배선이 살아 있는지 본다.
    /// (SelectTap 과 같은 직렬화 함정을 SceneLink 도 피하는지 확인.)
    public class TitleSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadTitle()
        {
            yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
            yield return null;
        }

        [Test]
        public void 시작_버튼이_선택_화면을_가리킨다()
        {
            SceneLink start = null;
            foreach (var l in Object.FindObjectsByType<SceneLink>(FindObjectsSortMode.None))
                if (l.scene == "Select") { start = l; break; }
            Assert.IsNotNull(start, "메인 화면에 Select 로 가는 SceneLink 가 없다 (직렬화 실패?)");
        }

        [Test]
        public void 캐릭터와_강아지_미리보기가_있다()
        {
            var sprites = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            bool hasChar = false, hasDog = false;
            foreach (var s in sprites)
            {
                if (s.name == "Character") hasChar = true;
                if (s.name == "Dog") hasDog = true;
            }
            Assert.IsTrue(hasChar, "캐릭터 미리보기가 없다");
            Assert.IsTrue(hasDog, "강아지 미리보기가 없다");
        }
    }
}
