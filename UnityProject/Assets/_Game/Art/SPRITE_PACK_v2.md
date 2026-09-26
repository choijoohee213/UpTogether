# 강아지 플랫포머 – 숲 스프라이트 팩 v2

공통: PNG 투명 배경 / 픽셀아트 / Unity 임포트 시 Filter Mode = Point, Compression = None, PPU = 16
애니메이션 시트는 전부 가로 한 줄, 프레임은 왼쪽부터 순서대로.

## A. 장애물·아이템 (A_obstacles)
| 파일 | 프레임 크기 | 프레임 | 비고 |
|---|---|---|---|
| spike_floor.png | 16x16 | 1 | 가로로 이어 붙이면 연속 가시 |
| thorn_vine_2f.png | 16x24 | 2 | 흔들림 (위쪽이 천장/발판에 붙는 부분) |
| saw_log.png | 32x32 | 1 | 회전은 코드로, 피벗 중앙 |
| bounce_mushroom_2f.png | 24x16 | 2 | 0=기본, 1=눌림 |
| ring_flower.png / ring_thorn.png | 48x48 | 1 | 꽃고리 / 가시고리 |
| wind_3f.png | 24x24 | 3 | 루프, 반투명 픽셀 포함 |
| treat_bone/heart/star_2f.png | 16x16 | 2 | 1프레임째에 반짝임 |
| dust_puff_5f.png | 16x16 | 5 | 점프·착지·워프 공용, 피벗 하단 중앙 |
| warp_sparkle_4f.png | 16x16 | 4 | 커졌다가 흩어짐 |

## B. UI (B_ui) — 32px로 그려서 2배 확대한 픽셀 밀도
| 파일 | 프레임 크기 | 프레임 | 비고 |
|---|---|---|---|
| btn_left/right/up/pause_2f.png | 64x64 | 2 | 0=기본, 1=눌림 |
| panel_wood.png | 160x120 | 1 | 9-slice 테두리 12px 권장 |
| heart_icon_3f.png | 16x16 | 3 | 가득 / 반 / 빈 |
| gauge_frame.png | 96x16 | 1 | 안쪽 빈 칸 영역: x5~90, y6~9 |
| gauge_fill.png | 86x4 | 1 | Image Type = Filled(Horizontal) 로 쓰기 좋음 |

## C. 정체성 요소 (C_identity)
| 파일 | 프레임 크기 | 프레임 | 비고 |
|---|---|---|---|
| goal_doghouse_2f.png | 48x64 | 2 | 하트가 둥실 + 반짝 |
| emote_heart/exclaim/question/note.png | 16x16 | 1 | 머리 위 말풍선 |
| particle_leaf_4f.png | 8x8 | 4 | 회전하며 떨어지는 낙엽 |
| particle_firefly_3f.png | 8x8 | 3 | 빛 번짐, 밤 테마용 (Additive 추천) |

## D. 변주 (D_variants)
- bg_sunset / bg_night / bg_snow: 각각 bg0_sky, bg1_far, bg2_mid (320x180, 가로 무한 반복 가능)
- tileset_snow_16px.png / tileset_stone_16px.png: 기존 숲 타일셋과 **타일 순서 동일** → 팔레트만 교체해서 같은 맵 재사용 가능
- deco_big_tree.png 48x64 / deco_sign.png 16x16 / deco_mushroom_cluster.png 32x16
