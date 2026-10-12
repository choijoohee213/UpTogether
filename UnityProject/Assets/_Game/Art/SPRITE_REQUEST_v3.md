# 스프라이트 요청서 v3 — 새 장애물·오르는 방법

목적: "올라가는 방법이 점프 하나뿐"이라 단조롭던 것을 고친다. 동사를 늘린다.

이 문서는 **그림 만드는 쪽에 그대로 넘기는 용도**다. 규격·팔레트·프롬프트가 다 들어 있다.

---

## 0. 먼저 — 새로 안 만들어도 되는 것

이미 있는 아트로 되는 기능이다. 그림 요청에서 **빼도 된다**.

| 기능 | 쓸 기존 아트 |
|---|---|
| 상승 기류 (떠오르기) | `wind_3f.png` 를 세로로 돌려 씀 |
| 버섯 차지 점프 | `bounce_mushroom_2f.png` |
| 미끄러운 얼음 발판 | `Variants/tileset_snow_16px.png` (이미 있는데 안 쓰임) |
| 굴러 내려오는 통나무 | `saw_log.png` |
| 밧줄 스윙 | `thorn_vine_2f.png` |
| 돌 테마 스테이지 | `Variants/tileset_stone_16px.png` + `bg_snow` |

**캐릭터 시트는 건드리지 않는다.** 매달리기·스윙은 기존 `climb`(10–11번, 뒷모습) 프레임을
재활용한다. 캐릭터는 8명이라 프레임 하나 추가가 곧 8장 수정이다.

---

## 1. 공통 규격 (전부 동일)

| 항목 | 값 |
|---|---|
| 형식 | PNG, 32bit, **투명 배경** |
| 스타일 | 픽셀아트. **안티앨리어싱 금지**, 경계는 딱 떨어지게 |
| 외곽선 | 1px, 어두운 보라빛 회색 `#4A4458` |
| PPU | 16 (16px = 게임 1칸) |
| 시트 배열 | 애니메이션은 **가로 한 줄**, 왼쪽부터 0번. 칸 사이 여백 없음 |
| 파일명 | `이름_프레임수f.png` (예: `steam_4f.png`). 1프레임이면 접미사 없이 `이름.png` |
| Unity 설정 | Point filter / Compression None (임포터가 자동 처리) |

### 팔레트 (기존 아트에서 추출한 실제 색)

```
어두운 선 / 그림자   #4A4458
나무·흙 (밝은→어두운) #E6BE8C  #DEAA76  #CDA070  #BA8054  #80543C
잎·풀   (밝은→어두운) #C8F0A0  #9ED98A  #74C06A  #4F9A5A
돌·금속 (밝은→어두운) #D6D2E2  #AAA5BE
강조 (위험·하트)      #E87878
밝은 하이라이트       #FAE8BE
```

새 색은 되도록 쓰지 말고 위에서 고른다. 위험한 것(추·돌·증기)은 돌/금속 계열 + `#E87878` 포인트.

---

## 2. 요청 목록

### 필수 4종

| 파일 | 크기 | 프레임 | 피벗 | 쓰임 |
|---|---|---|---|---|
| `branch_bar.png` | 16x16 | 1 | 중앙 | 매달려 손 바꿔가며 건너는 가지. **가로로 이어 붙여** 길게 만든다 |
| `pendulum_weight.png` | 32x32 | 1 | 중앙 | 좌우로 크게 흔들려 플레이어를 쳐내는 추 |
| `pendulum_chain.png` | 8x16 | 1 | **상단 중앙** | 추를 매단 사슬. 세로로 이어 붙인다 |
| `rock_fall_3f.png` | 16x16 | 3 | 중앙 | 밑을 지나면 떨어지는 돌. 0=매달림 1=흔들림(경고) 2=떨어지는 중 |

### 있으면 좋은 3종

| 파일 | 크기 | 프레임 | 피벗 | 쓰임 |
|---|---|---|---|---|
| `steam_4f.png` | 24x32 | 4 | 하단 중앙 | 주기적으로 뿜는 증기. 반투명 픽셀 포함, 루프 |
| `wall_grip.png` | 16x16 | 1 | 중앙 | 벽 타기용 벽면. 세로로 이어 붙인다 |
| `updraft_3f.png` | 24x24 | 3 | 하단 중앙 | 위로 떠오르게 하는 기류. `wind_3f` 의 세로 버전, 루프 |

**이어 붙인다(tileable)** 는 건 좌우(또는 상하) 끝이 자연스럽게 연결돼야 한다는 뜻이다.
`spike_floor.png` 가 같은 방식이니 참고용으로 같이 넘기면 좋다.

---

## 3. AI 프롬프트 (영어, 파일마다 하나씩)

공통으로 앞에 붙일 말:

```
16-bit style pixel art, transparent background, no anti-aliasing, crisp 1px dark
outline in color #4A4458, limited palette using only these colors:
#E6BE8C #DEAA76 #CDA070 #BA8054 #80543C (wood/earth),
#C8F0A0 #9ED98A #74C06A #4F9A5A (foliage),
#D6D2E2 #AAA5BE (stone/metal), #E87878 (danger accent), #FAE8BE (highlight).
Cute storybook forest game. Side view, flat lighting, no gradients, no text.
```

| 파일 | 프롬프트 |
|---|---|
| `branch_bar.png` | `A short horizontal tree branch segment, 16x16, seamlessly tileable left and right so copies form one long branch. Bark texture in wood colors, a few small leaves on top. Sturdy enough to hang from.` |
| `pendulum_weight.png` | `A heavy round stone weight hanging as a pendulum, 32x32, centered. Stone grays with a dark outline, a small iron ring on top where a chain attaches, subtle #E87878 warning marking.` |
| `pendulum_chain.png` | `A vertical chain segment, 8x16, seamlessly tileable top to bottom. Two interlocking iron links in stone/metal grays.` |
| `rock_fall_3f.png` | `A rough rock hanging from a ceiling, 3 frames in one horizontal row, each 16x16. Frame 0: resting, attached above. Frame 1: cracked and wobbling with small dust specks, warning. Frame 2: detached and falling, slight motion streaks. Stone grays.` |
| `steam_4f.png` | `A puff of steam venting upward, 4 frames in one horizontal row, each 24x32, looping. Grows from a small burst at the bottom to a tall wide plume, then thins out. Soft white and pale gray with semi-transparent pixels.` |
| `wall_grip.png` | `A rocky cliff wall surface, 16x16, seamlessly tileable vertically. Grippy ledges and cracks, stone grays with mossy #4F9A5A touches on a few edges.` |
| `updraft_3f.png` | `An upward air current, 3 frames in one horizontal row, each 24x24, looping. Curved streaks and small leaves rising, semi-transparent pale pixels, suggesting wind blowing upward.` |

---

## 4. AI 로 만들 때 꼭 알아야 할 것

AI 이미지 생성기는 **16x16 격자에 딱 맞춰 그려주지 않는다.** 보통 1024px 같은 큰 그림을
내놓고, 픽셀처럼 보이지만 실제로는 격자가 어긋나 있고 색도 수백 가지가 섞인다.

그래서 이렇게 받으면 된다.

1. 위 프롬프트로 **큰 이미지(512~1024px)** 를 받는다. 배경은 투명 또는 단색(#FF00FF 같은 것)으로.
2. 그 파일을 그대로 주면 **내가 처리한다** — 격자 정렬, 최근접 축소, 팔레트 정리(위 색으로 강제),
   프레임 잘라 가로 한 줄로 배치, 투명 배경 처리.
3. 애니메이션 프레임은 **한 장에 다 그려 달라고 하지 말고** 프레임마다 따로 받는 게 낫다.
   AI 가 한 장 안에서 일관성을 잘 못 지킨다.

즉 **최종 규격은 내가 맞춘다.** 그림 쪽에는 "모양과 색만" 맞춰 달라고 하면 된다.

---

## 5. 그림이 오면 하는 일 (내 몫)

1. `Assets/_Game/Art/Obstacles/` 에 넣는다
2. `SpritePackImporter` 에 새 파일의 피벗 규칙을 추가한다 (위 표의 피벗)
3. `UpTogether ▸ Import Sprite Pack` → `UpTogether ▸ Bake Sprite Lib`
4. `SpriteLib` 에 필드를 늘리고 `StageData` 에 새 장애물 종류를 추가
5. `StageObstacles` 배치 규칙 + 검증(`Verify`) 추가
6. PlayMode 테스트

---

## 6. 우선순위

그림이 하나도 없어도 **상승 기류 · 버섯 차지 점프 · 얼음 발판 · 굴러오는 통나무**는
지금 바로 만들 수 있다. 이것부터 넣고, 그림이 오는 대로 매달리기(가지) → 추 → 돌 순으로 붙이면
기다리지 않고 계속 진행된다.
