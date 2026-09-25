# 강아지 플랫포머 스프라이트 사양서

마지막 갱신: 2026-09-24

이 문서 하나면 임포터와 애니메이션 세팅을 끝낼 수 있게 정리했습니다.
강아지 7종과 캐릭터 8명은 **각각 시트 한 장씩(총 15장)**이고,
"안기"와 "포옹"은 이미지를 미리 합성하지 않고 **런타임에 3겹으로 겹쳐서** 표현합니다.
그래서 7 x 8 = 56가지 조합이 모두 이 15장으로 나옵니다.

---

## 1. 파일 목록

| 파일 | 내용 |
|---|---|
| `{견종}48_sheet.png` | 강아지 7종 (shiba, retriever, maltese, bordercollie, samoyed, bernese, poodle) |
| `{캐릭터}64_sheet.png` | 캐릭터 8명 (girl, pigtail, ponytail, bun, boy, beanie, glasses, strawhat) |
| `dog_sprites.json` | 강아지 시트 메타데이터 |
| `hero_sprites.json` | 캐릭터 시트 메타데이터 + 강아지 붙이는 위치 |
| `preview_*.png` | 확인용 미리보기 (게임에 넣지 않음) |

---

## 2. 공통 규격

| 항목 | 강아지 | 캐릭터 |
|---|---|---|
| 셀 크기 | 48 x 48 | 64 x 64 |
| 시트 크기 | 624 x 48 (13칸) | 1408 x 64 (22칸) |
| 줄 수 | 가로 1줄, 여백·간격 없음 | 가로 1줄, 여백·간격 없음 |
| 피벗(픽셀) | (22.5, 3) | (32, 3) |
| 피벗(정규화) | (0.47, 0.0625) | (0.5, 0.047) |
| **PPU** | **48** | **48** |
| 방향 | 오른쪽을 봄 | 오른쪽을 봄 |

셀 크기는 다르지만 PPU가 같아서 화면상 픽셀 크기가 서로 맞습니다.
피벗은 서 있을 때 발바닥 바로 아래 지점이며, 발밑에 3px 여백이 있어 y가 0이 아닙니다.

### Unity 임포트 설정 (15장 모두 동일)

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple` → Sprite Editor → Grid by Cell Size (48x48 또는 64x64)
- Pixels Per Unit: `48`
- Filter Mode: `Point (no filter)`
- Compression: `None`
- Pivot: Custom (위 표의 정규화 값)

---

## 3. 강아지 상태 (13프레임)

| 상태 | 프레임 | 루프 | 권장 시간 | 내용 |
|---|---|---|---|---|
| `walk` | 0–3 | O | 각 0.13s | 걷기. 1·3번은 의도적으로 1px 튐 |
| `idle` | 4–5 | O | 1.5s / 0.15s | 숨쉬기 + 눈 깜빡임 |
| `jump` | 6 | X | 고정 | 점프 |
| `fall` | 7–8 | O | 각 0.1s | 낙하. 귀가 날리고 다리 허우적 |
| `land` | 9 | X | 0.12s | 착지 |
| `held` | 10 | X | 고정 | 안겨 있는 자세 |
| `happy` | 11–12 | O | 각 0.12s | 눈웃음 + 꼬리 흔들기 |

---

## 4. 캐릭터 상태 (22프레임)

| 상태 | 본체 프레임 | 앞팔 오버레이 | 루프 | 권장 시간 | 내용 |
|---|---|---|---|---|---|
| `walk` | 0–3 | – | O | 각 0.13s | 걷기 |
| `idle` | 4–5 | – | O | 1.5s / 0.15s | 숨쉬기 + 깜빡임 |
| `jump` | 6 | – | X | 고정 | 점프 |
| `fall` | 7–8 | – | O | 각 0.1s | 놀란 눈, 머리카락 날림 |
| `land` | 9 | – | X | 0.12s | 착지 |
| `climb` | 10–11 | – | O | 각 0.18s | **뒷모습**, 사다리·로프 |
| `hold_idle` | 12 | 15 | X | 고정 | 강아지 안고 서 있기 |
| `hold_fall` | 13–14 | 16–17 | O | 각 0.1s | 강아지 안고 떨어지기 |
| `hug` | 18–19 | 20–21 | O | 각 0.42s | 무릎 굽혀 마주보고 포옹 |

오버레이는 **강아지 앞을 덮는 팔**입니다. 본체와 같은 순서로 재생하세요
(본체 13 ↔ 오버레이 16, 본체 14 ↔ 오버레이 17).

---

## 5. 겹치기 구조

```
Player (root)              ← 이동·물리, 좌우 반전은 여기서
├─ Body   (order 0)        ← 캐릭터 본체 프레임
├─ DogAnchor               ← 위치만 잡는 빈 오브젝트
│   └─ Dog (order 1)       ← 강아지 프레임
└─ Arms   (order 2)        ← 캐릭터 앞팔 오버레이
```

평소(walk/idle)에는 강아지를 DogAnchor에서 떼어내 독립 오브젝트로 따라다니게 하고,
안기거나 포옹할 때만 DogAnchor의 자식으로 붙인 뒤 `localPosition`을 0으로 두면 됩니다.

### 강아지 위치 (캐릭터 피벗 기준, 픽셀)

| 상태 | 강아지 프레임 | x | y | 좌우 반전 |
|---|---|---|---|---|
| `hold_idle` | 10 (`held`) | +10.5 | -3 (아래) | 없음 |
| `hold_fall` | 7, 8 (`fall`) | +10.5 | +1 (위) | 없음 |
| `hug` | 11, 12 (`happy`) | +28.5 | -7 (아래) | **있음** |

Unity 단위로 바꿀 때는 PPU로 나눕니다. 예: `hug` → `(28.5/48, -7/48)` = `(0.594, -0.146)`

`hug`는 강아지가 캐릭터를 마주봐야 하므로 **Dog의 localScale.x를 -1**로 둡니다.
피벗을 기준으로 뒤집히기 때문에 위치는 그대로 두면 됩니다.

---

## 6. 좌우 반전

세 장을 각각 `flipX` 하지 말고 **루트의 스케일**로 뒤집으세요.
자식의 위치까지 통째로 미러링되어 오프셋 부호를 따로 손볼 필요가 없습니다.

```csharp
transform.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);
```

`climb`는 뒷모습이라 반전이 필요 없습니다. 사다리에 붙을 때는 스케일을 1로 고정하세요.

---

## 7. 예제 코드

```csharp
// 상태 하나를 세 장에 동시에 적용
public void SetFrame(DogHoldState st, int i) {
    body.sprite = charSprites[st.frames[i]];
    arms.sprite = st.overlay != null ? charSprites[st.overlay[i]] : null;
    arms.enabled = arms.sprite != null;

    if (st.dogFrames != null) {
        dog.enabled = true;
        dog.sprite = dogSprites[st.dogFrames[i]];
        dogAnchor.localPosition = st.dogOffset;                 // 픽셀값 / 48
        dog.transform.localScale = new Vector3(st.dogFlip ? -1f : 1f, 1f, 1f);
    } else {
        dog.enabled = false;
    }
}
```

강아지 견종과 캐릭터는 `dogSprites`, `charSprites` 배열만 갈아 끼우면 바뀝니다.
상태 정보(프레임 번호, 오프셋)는 견종·캐릭터와 무관하게 동일합니다.

---

## 8. 상태 전환 참고

- `jump` → 속도 y가 음수로 바뀌면 `fall`
- 강아지를 안은 상태면 `fall` 대신 `hold_fall`
- 착지하면 `land`를 한 번 재생하고 `idle` 또는 `walk`로
- `hug`는 친밀도 연출용. 연출이 끝나면 `idle`로 복귀

---

## 9. 알아두면 좋은 점

- `hug`는 마주보는 구도라 가로로 넓습니다. 캐릭터 피벗 기준 오른쪽으로 약 52px까지 그림이 나가므로, 벽에 붙어 재생하면 강아지가 벽을 뚫고 보일 수 있습니다. 연출 전에 좌우 여유 공간을 확인하세요.
- 말티즈·사모예드·푸들은 꼬리가 등 위로 올라와서, `hold_idle`에서 캐릭터 턱을 살짝 가립니다. 거슬리면 꼬리를 내린 전용 프레임을 추가하면 됩니다.
- 흰 강아지(말티즈·사모예드)와 검은 강아지(보더콜리·버니즈)는 배경 밝기에 따라 윤곽이 묻힐 수 있습니다. 스테이지 배경을 정할 때 함께 확인하세요.
- `preview_*.png`는 확대·배경이 들어간 확인용입니다. 게임에는 넣지 마세요.
