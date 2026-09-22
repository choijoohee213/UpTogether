# Unity 이식 — 1차 (손맛 파이프라인)

`climb-dog2.html` 프로토타입의 **물리·발판·강아지·카메라·터치 입력**을 Unity로 옮긴 것.
친밀도, 서사 트리거, 스테이지 클리어, UI, 사운드는 아직 없다 (2차 범위).

> 아래에서 참조하는 `PROJECT.md` / `SUMMARY.md` 는 기획 문서로, 저장소에 넣지 않고
> 로컬에만 둔다. 수치와 방침의 출처를 표시하기 위해 이름만 남겨둔 것이다.

Unity 6000.6.2f1, **URP 2D** (Built-in은 Unity 6.5부터 deprecated라 옮겼다).

---

## 여는 법

Unity Hub ▸ Add ▸ 이 폴더를 열고 `Assets/_Game/Playground.unity` 재생.
`← →` 이동, `Space` 점프(길게 누르면 높이 올라감), 공중에서 한 번 더 누르면 이단 점프.

에디터 메뉴에 세 개가 있다.

| 메뉴 | 하는 일 |
|---|---|
| **UpTogether ▸ Bake Stages** | 스테이지 3개를 다시 굽는다 (+ 발판 제약 검사) |
| **UpTogether ▸ Build Play Scene** | `Playground.unity` 를 다시 만든다 (기존 씬을 덮어쓴다) |
| **UpTogether ▸ Verify Jump Heights** | 점프 높이가 프로토타입과 같은지 잰다 |
| **UpTogether ▸ Build for Web** | 웹 빌드 (아이폰 사파리 확인용) |
| **UpTogether ▸ Switch to URP 2D** | 렌더 파이프라인 설정. 이미 적용돼 있어서 다시 쓸 일은 없다 |

### 설치된 플랫폼 모듈
Android Build Support (+ SDK & NDK Tools, OpenJDK), Web. iOS는 아직 없다.

---

## 테스트

에디트 모드 검증(메뉴)은 물리 수치만 본다. 실제로 씬을 띄워 굴려 보는 건 PlayMode 테스트다.

```bash
/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . \
  -runTests -testPlatform PlayMode -testResults /tmp/results.xml -logFile /tmp/pm.log
```

`Assets/_Game/Tests/PlayMode/ClimbPlayModeTests.cs` — 9개, 전부 통과:

| 테스트 | 확인하는 것 |
|---|---|
| 물리스텝이_60Hz다 | `GameBootstrap`이 실제로 먹는지 |
| 가만히_두면_바닥에_서_있는다 | 착지·정지 |
| 오른쪽을_누르면_걷기_최고속도에_도달한다 | 입력 → 이동, 최고속도 수렴 |
| 점프해서_첫_발판에_올라선다 | 실제 발판에 올라타는지 |
| 아래에서_발판을_통과한다 | 원웨이 발판 |
| 강아지가_따라온다 | 추적 + 순간이동 거리 안 |
| 크게_떨어지면_강아지가_안긴다 | 이 게임의 핵심 정서 |
| 카메라가_플레이어를_따라간다 | 추적 |
| 카메라가_..._58퍼센트에_둔다 | 세로 위치 (맵 끝에서는 클램프가 이긴다) |

> Unity CLI(`~/.unity/bin/unity`, 1.0.0-beta.8)의 `unity test` 는 이 환경에서
> Hub만 띄우고 멈춘다. 위의 `-runTests` 방식을 쓸 것.
> `install-modules`, `editors`, `license` 같은 명령은 잘 동작한다.

---

## 검증한 것

| 항목 | 방법 | 결과 |
|---|---|---|
| 스테이지 생성기 이식 | 원본 JS와 C# 출력을 소수점 9자리까지 diff (3스테이지 66줄) | **완전 일치** |
| 발판 배치 제약 | 상승폭 68~102px, 가로 간격 ≤68px 전 구간 검사 | **위반 0건** (최대 간격 66.6 / 60.3 / 63.2px) |
| 점프 높이 변환 | 프레임 단위 원본 vs 초 단위 이식본 시뮬레이션 | **오차 1e-13** (사실상 동일) |
| 문법·타입 | 실제 Unity 6000.6.2f1 에서 컴파일 | **오류 0** |
| 실제 플레이 동작 | PlayMode 테스트 9개 (씬 띄워서 FixedUpdate 구동) | **9/9 통과** |
| 점프 높이 (실제 Unity 코드) | `PlayerController`+`CharacterBody`를 60Hz로 직접 돌려 측정 | **전 구간 일치** |

### 점프 높이 — 원본 JS vs Unity 이식본
| 입력 | 원본 | Unity |
|---|---|---|
| 누르자마자 뗌 | 66.60px | **66.60px** |
| 1프레임 홀드 | 72.45px | **72.45px** |
| 5프레임 홀드 | 94.38px | **94.38px** |
| 10프레임 홀드 | 118.53px | **118.53px** |
| 0.30초(최대) 홀드 | 146.21px | **146.21px** |

언제든 **UpTogether ▸ Verify Jump Heights** 로 다시 잴 수 있다.
수치를 만진 뒤에는 이걸 돌려서 얼마나 달라졌는지 확인할 것.

> PROJECT.md에는 최대 홀드가 **149px**로 적혀 있는데 실제 코드는 146.2px다.
> 이식 오류가 아니라 문서 쪽 수치가 어림수다. 탭 66px은 정확히 일치한다.

---

## 구조

```
Assets/_Game/
  Tuning.asset            수치 전부. 재생 중에도 만질 수 있다
  Stages/Stage1~3.asset   구워진 발판 데이터. 손으로 고쳐도 된다
  Scripts/Runtime/
    Px.cs                 단위 변환의 유일한 지점. 다른 데서 60/100을 곱하지 말 것
    Tuning.cs             PROJECT.md 수치를 px/frame 그대로 인스펙터에 노출
    StageGenerator.cs     makeStage() 이식본. Unity 타입 없음 = 원본과 diff 가능
    StageData.cs          구워진 스테이지
    StageRunner.cs        발판 배열 + 움직이는 발판 갱신
    CharacterBody.cs      stepBody() 이식본. 플레이어·강아지 공용
    PlayerController.cs   점프/홀드/이단/착지
    DogController.cs      따라오기·점프·순간이동·안기기
    FollowCamera.cs       수동 lerp 카메라
    GameInput.cs          키보드 + 화면 버튼
    TouchButton.cs        화면 버튼 한 개
    GameBootstrap.cs      물리 스텝 60Hz 강제. 지우면 안 된다
    ProceduralArt.cs      배경·지형·버튼 스프라이트를 코드로 생성 (캐릭터는 여기서 만들지 않는다)
    SkyBackground.cs      높이에 따라 변하는 하늘 그라데이션
    ParallaxLayer.cs      카메라보다 느리게 흐르는 배경 조각
    Backdrop.cs           구름·언덕 배치
    Puffs.cs              점프·착지 먼지
    SpriteClip.cs         프레임별 지속시간을 가진 클립
    CharacterSpriteSet.cs 캐릭터 한 마리의 프레임 + 클립 표 (파일명 = 클래스명 필수)
    DogVisual.cs          강아지 상태 → 스프라이트 교체
  Scripts/Editor/
    StageBaker.cs         UpTogether ▸ Bake Stages
    SceneBuilder.cs       UpTogether ▸ Build Play Scene
    JumpVerifier.cs       UpTogether ▸ Verify Jump Heights
    WebBuilder.cs         UpTogether ▸ Build for Web
    UrpSetup.cs           UpTogether ▸ Switch to URP 2D
    DogArtImporter.cs     UpTogether ▸ Import Dog Art
```

---

## 옮기면서 주의한 것 (건드리기 전에 읽을 것)

**y축 부호가 뒤집혔다.** 프로토타입은 y가 아래쪽 양수, `vy>0`이 낙하였다.
Unity는 반대다. `CharacterBody`는 전부 Unity 기준으로 뒤집어 놨다.
원본과 대조할 때 이걸 먼저 떠올릴 것.

**낙하 시작 높이의 센티넬이 0이 아니라 NaN이다.** 원본은 `fallFrom===0`으로 "아직 안 잡힘"을
표현했는데, Unity에서는 y=0이 실제로 존재하는 위치라 그대로 쓰면 오작동한다.

**물리 스텝은 60Hz여야 한다.** Project Settings의 Fixed Timestep은 기본값 0.02(50Hz)이고,
그대로 두면 점프 높이가 미묘하게 달라진다. 설정에 맡기지 않고 `GameBootstrap.cs` 가
실행 시점에 `Time.fixedDeltaTime = 1/60` 을 강제한다 — 에디터 재생과 실기기 빌드 양쪽에 적용된다.
이 파일을 지우면 손맛이 조용히 어긋난다.

**Rigidbody2D를 쓰지 않는다.** 검증된 수치를 그대로 살리려고 직접 적분한다.
발판도 콜라이더가 아니라 배열이고, 충돌은 `CharacterBody`가 직접 푼다.
아래에서 통과 가능한 원웨이는 "내려갈 때만 발판을 잡는다"로 구현돼 있다.

**생성기의 난수 소비 순서를 건드리지 말 것.** 원본 JS의 `&&` 단축 평가 때문에
움직이는 발판이 없는 스테이지에서는 해당 `R()`이 호출되지 않는다.
이걸 무심코 "정리"하면 맵이 통째로 달라진다.

**렌더 파이프라인은 URP 2D다.** 3D용 `UniversalRendererData`가 아니라 `Renderer2DData`를 쓴다.
설정 에셋은 `Assets/_Game/Rendering/` 에 있고 Graphics 설정에 물려 있다.
Quality 단계별 `renderPipeline` 은 비워둔 게 정상이다 — 비면 Graphics의 기본값(= URP)을 따른다.
나중에 2D Light를 쓸 수 있다 (스테이지 3 '별빛 언덕'에 쓸만하다).

**ScriptableObject 클래스는 파일명과 이름이 같아야 한다.** Unity는 `.cs` 파일 하나당
MonoScript 하나를 파일명으로 만든다. `CharacterSpriteSet` 을 `SpriteClip.cs` 안에 뒀더니
에셋의 `m_Script` 가 0 으로 저장되고 `LoadAssetAtPath<CharacterSpriteSet>` 이 null 을 돌려줬다.
경고도 오류도 안 난다. 실제로 이걸로 한 번 당했다.

**주인공 피벗 y 는 JSON 의 3/64 가 아니라 2/64 다.** 8캐릭터 전부 접지 프레임의
바닥 여백이 2px 이라(강아지는 3px 이라 JSON 과 맞았다), 3 을 그대로 쓰면 발이 1px 박힌다.
픽셀아트라 1px 이 화면 1px 이라 눈에 띈다. 임포터가 이 차이를 콘솔에 찍는다.

**스프라이트 PPU는 135다. JSON 의 48이 아니다.** 48이면 48px 셀이 1유닛이라
강아지 키가 0.83유닛이 되는데, 발판 세로 간격이 0.68~1.02유닛이라 말이 안 된다.
135면 40px 몸통이 0.30유닛으로 프로토타입 강아지와 맞고, 세로 8유닛 화면이 1080px일 때
유닛당 135px이라 픽셀아트가 1:1로 찍힌다. 피벗(0.47, 0.0625)은 비율이라 그대로 쓴다 —
전 견종의 접지 프레임 바닥여백이 정확히 3px 이라 발이 몸 위치에 정확히 맞는다.

**실행 순서가 고정돼 있다.** 입력(-200) → 발판 이동(-100) → 플레이어(0) → 강아지(10).
강아지가 플레이어보다 먼저 돌면 한 프레임 낡은 상태를 읽는다.

**씬을 만들 때는 씬을 먼저 만들고 에셋을 나중에 로드한다.** `EditorSceneManager.NewScene()` 이
참조 없는 에셋을 언로드해서, 지역 변수만 붙들고 있던 ScriptableObject가 파괴된다.
그 "fake null" 을 대입하면 **아무 경고 없이** null이 들어가고 재생하자마자 터진다.
실제로 이걸로 한 번 당했다. `SceneBuilder`는 저장 직전에 모든 참조를 검사하고,
하나라도 비면 어느 필드인지 찍고 저장하지 않는다.

---

## 아트 현황

**배경·지형·연출은 들어갔다.** 하늘 그라데이션(높이에 따라 옅어짐), 구름·언덕 시차,
잔디+흙 발판, 공중 발판 그림자, 점프·착지 먼지, 원형 화살표 조작 버튼.
전부 `ProceduralArt.cs` 가 코드로 굽는다.

**강아지 아트는 들어왔다.** 픽셀아트 시트, 7견종 × 13프레임.
`Art/Dog/dog_sprites_v2/{breed}48_sheet.png` (624×48, 셀 48×48) + `dog_sprites.json`.

`UpTogether ▸ Import Dog Art` 가 텍스처 설정·슬라이스·에셋 생성을 한 번에 한다.
결과는 `Art/Dog/Generated/{breed}.asset` (CharacterSpriteSet). 기본 견종은 `shiba`,
`SceneBuilder.DefaultBreed` 에서 바꾼다.

| 상태 | 프레임 | 루프 | 비고 |
|---|---|---|---|
| walk | 0–3 | O | 각 0.13s. 1·3번은 의도적으로 1px 통통 튄다 |
| idle | 4–5 | O | 1.5s / 0.15s — 숨쉬기 + 눈 깜빡임 |
| jump | 6 | X | vy > 0 일 때 |
| fall | 7–8 | O | 각 0.1s. vy ≤ 0 으로 바뀌는 순간 전환 |
| land | 9 | X | 0.12s 재생 후 idle/walk 자동 복귀 |
| held | 10 | X | 안김. **주인공 팔 스프라이트를 앞에 겹쳐야 완성된다** |
| happy | 11–12 | O | 각 0.12s. 아직 트리거 없음 (친밀도·클리어에 붙일 자리) |

**주인공 아트도 들어왔다.** 픽셀아트 시트, 8캐릭터 × 18프레임.
`Art/Player/hero_sprites_v2/{character}64_sheet.png` (1152×64, 셀 64×64) + `hero_sprites.json`.
`UpTogether ▸ Import Player Art` 로 굽는다. 기본은 `girl`, `SceneBuilder.DefaultCharacter` 에서 바꾼다.

| 상태 | 프레임 | 루프 | 비고 |
|---|---|---|---|
| walk | 0–3 | O | 각 0.13s |
| idle | 4–5 | O | 1.5s / 0.15s |
| jump / fall / land | 6 / 7–8 / 9 | | 강아지와 같은 규칙 |
| climb | 10–11 | O | 뒷모습. **사다리 기능이 아직 없어 트리거하지 않는다** |
| hold_idle | 12 (+오버레이 15) | X | 강아지 안고 서기 |
| hold_fall | 13–14 (+오버레이 16–17) | O | 강아지 안고 떨어지기 |

**안기는 3겹이다**: 주인공 본체(10) → 강아지(11) → 앞팔 오버레이(12).
평소 강아지는 주인공 뒤(9)에 있다가 안는 순간 앞(11)으로 온다.
왼쪽을 볼 때는 **셋 다 flipX 하고 강아지 x 오프셋의 부호도 뒤집어야** 한다 —
하나라도 빠뜨리면 팔이 엉뚱한 데 붙는다. PlayMode 테스트가 이 셋을 다 본다.

`ProceduralArt` 에 캐릭터를 추가하지 말 것 — 진짜 아트로 갈아끼울 때 버려지는 작업이 된다.

---

## 웹 빌드

`UpTogether ▸ Build for Web` → `Build/Web/`. 압축을 꺼서 평범한 정적 서버로 바로 열린다.

```bash
cd Build/Web && python3 -m http.server 8899 --bind 0.0.0.0
```

같은 Wi-Fi의 폰에서 `http://<맥 IP>:8899` 로 접속하면 터치 조작을 실기기에서 확인할 수 있다.
Xcode도 서명도 필요 없다.

---

## 다음

- [ ] 실기기에서 터치 조작 검증 — 홀드 점프가 손가락으로 조절되는지
      (안 되면 `Tuning`의 `holdMaxSeconds` 부터 만져볼 것)
- [ ] 화면 버튼 크기/위치 조정. 지금 값은 엄지 위치 어림값이다
- [ ] 친밀도 + 서사 트리거 (`PlayerController.Landed` 이벤트가 붙을 자리다)
- [ ] 스테이지 클리어 판정 / 깃발
- [ ] 캐릭터 아트 — 지금은 전부 임시 네모다
