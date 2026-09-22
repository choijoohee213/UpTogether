# 캐릭터 PNG 놓는 곳

받아온 원본을 여기 그대로 둔다. 배경 제거·피벗·PPU 설정은 임포터가 한다
(UpTogether ▸ Import Character Art).

## 파일 이름

PROJECT.md 5절의 필요 프레임과 같다.

```
Art/Dog/                     Art/Player/
  dog_idle.png                 player_idle.png
  dog_run_0.png                player_run_0.png
  dog_run_1.png                player_run_1.png
  dog_run_2.png                player_run_2.png
  dog_run_3.png                player_run_3.png
  dog_jump.png                 player_jump.png
  dog_fall.png                 player_fall.png
  dog_cling.png                player_cling.png
```

- 오른쪽을 보는 그림 하나만 있으면 된다. 왼쪽은 코드가 좌우 반전한다
- 일부만 있어도 된다. 없는 상태는 idle로 대체된다
- 배경은 마젠타(#FF00FF)든 투명이든 상관없다. 임포터가 알아서 처리한다
