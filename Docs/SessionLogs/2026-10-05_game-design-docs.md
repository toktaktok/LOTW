# 게임 기획 문서화와 리소스 규칙 정리

## 2026-10-05 00:50 | main
- 작업: Notion 기획과 저장소를 비교해서 `Docs/GameDesign.md`를 만들었다.
- 작업: 루트 `CLAUDE.md`에 `Docs/GameDesign.md`를 먼저 읽으라는 줄을 추가했다.
- 결과: 문서는 8개 장이다. 개요, 세계관, 캐릭터, 맵, 시스템, 구현 현황, 미정과 모순, Notion 링크다.
- 다음: 사용자가 내용을 검토한다.

## 2026-10-05 01:00 | main
- 작업: 사용자 수정 4개를 `Docs/GameDesign.md`에 반영했다.
- 작업: 정식 제목을 "창 밖을 보라" (Look out the Window)로 정했다.
- 작업: 마을 이름, 보물의 정체, Ch4를 미정으로 바꿨다.
- 작업: 대화 선택지는 Notion 기획 문구를 기준으로 정했다.
- 다음: 없음.

## 2026-10-05 01:20 | main
- 작업: `.claude/rules/asset-naming.md`에 `Texture size` 장과 환경 스프라이트 설정 줄을 추가했다.
- 작업: `Game/Assets/Project/Art`의 `.png.meta` 180개를 고쳤다. 스프라이트 압축을 끄고, 환경 스프라이트 12개를 Point 필터로 바꿨다.
- 작업: `TX_E_Wall.png.meta`, `TX_E_Wall_Rotate.png.meta`를 Sprite, PPU 25로 바꿨다.
- 작업: Notion 페이지 3개를 고쳤다. 리소스 제작 규칙, 파일 네이밍, 폴더 관리 Depth다.
- 확인 안 함: Unity 재임포트와 화면 확인. Point 필터로 바꾼 스프라이트 12개는 보이는 모양이 달라진다.
- 다음: Unity 에디터에서 스프라이트 12개를 확인한다.

## 2026-10-05 01:35 | main
- 작업: Fable 검토에서 나온 수정을 반영했다.
- 작업: `asset-naming.md`에서 틀린 내용 3개를 지웠다. PPU 범위 "11-78", 아틀라스 설명, Unity가 크기를 바꾼다는 설명이다.
- 작업: 4의 배수 규칙을 새 스프라이트에만 적용했다. 4의 배수가 아닌 기존 스프라이트는 20개다.
- 작업: `Docs/GameDesign.md`에서 `Game/CLAUDE.md`와 겹치는 구현 목록을 지웠다.
- 작업: Notion에서 깨진 링크 2개와 지워진 밑줄을 고쳤다. "Plugins 폴더는 쓰지 않습니다" 문장을 지웠다.
- 확인 안 함: Notion 페이지 화면. Notion 검색 결과로만 확인했다.
- 다음: 사용자가 정한다. Notion "제목" 페이지의 옛 제목 후보, 대화 테이블 선택지 문구, 환경 스프라이트 PPU 통일이 남아 있다.

## 2026-10-05 12:09 | main
- 작업: 진행 중이던 `t3code/core-gameplay-loop` 머지를 커밋했다(24f1864). 충돌 16개는 커밋 전에 이미 해결되어 있었다.
- 결과: 충돌 표시가 0개였다. 테이블 JSON 파일이 모두 파싱되었다. `Table/Json`과 `Resources/Table`의 파일이 같았다.
- 확인 안 함: 엑셀과 JSON의 일치. 이 PC에 Python이 없다.
- 확인 안 함: Unity 컴파일과 EditMode 테스트. Unity 에디터가 닫혀 있었다.
- 다음: 에디터에서 컴파일과 EditMode 테스트를 확인한다.

## 2026-10-05 12:18 | main
- 작업: 세션 로그 규칙 `.claude/rules/session-log.md`를 읽었다.
- 작업: 이 세션의 로그 파일을 만들었다.
- 다음: 없음.

## 2026-10-05 12:37 | main
- 작업: Notion "제목" 페이지 맨 위에 확정 제목 콜아웃을 추가했다.
- 작업: 페이지의 제목 후보 목록은 지우지 않았다. 이 목록은 팀원의 논의 기록이다.
- 확인 안 함: Notion 페이지 화면.
- 다음: 사용자가 정한다. 대화 테이블 선택지 문구와 환경 스프라이트 PPU 통일이 남아 있다.

## 2026-10-05 12:42 | main
- 작업: 이 세션의 이름을 "게임 기획 문서화와 리소스 규칙 정리"로 바꿨다.
- 작업: worktree 2개를 main으로 fast-forward했다. `t3code/voxel-shader-quality`, `t3code/minigame-framework-design`이다.
- 작업: `t3code/core-gameplay-loop`에 main을 머지했다. 이 브랜치에는 main에 없는 커밋 1개(1416aac)가 있었다.
- 결과: 3개 worktree에서 충돌이 0개였다.
- 확인 안 함: `t3code/codebase-extensibility-review` worktree. 커밋하지 않은 변경이 있고, 경로가 옛 구조(`Assets/`)여서 손대지 않았다.
- 다음: 사용자가 "그거"가 어떤 작업인지 정한다.
