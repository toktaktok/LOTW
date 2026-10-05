# 배경 구성과 Plaza 위층 정리

## 2026-10-05 22:20 | refactor/remove-plaza-builder
- 작업: 사용자가 광장 배경을 2층(광장과 외곽 가게, 중간 지역)까지만 만들기로 정했다.
- 작업: Plaza 빌더 삭제는 main 커밋 35331b1이 먼저 했다. 이 브랜치의 빌더 삭제 커밋은 버렸다.
- 작업: `Plaza.unity`에서 위층 요소 7개를 삭제했다: `Far_MiddleHouse`, `Far_SnowOffice`, `Far_Apartment`, `Chimney`, `FX_ChimneySmoke`, `UpperStop_Base`, `UpperStop_Dome`.
- 작업: 비게 된 그룹 `Background`, `VFX`를 삭제했다.
- 결과: 씬 문서가 387개에서 361개로 줄었다. 끊어진 로컬 참조는 0개다. 루트는 4개로 같다.
- 확인 안 함: Unity 에디터에서 씬 열기와 플레이. 에디터가 이 워크트리를 열고 있지 않았다.
- 다음: 머지한 뒤 에디터에서 Plaza를 다시 열어 레인 F 뒤쪽 화면을 확인한다.

## 2026-10-05 22:35 | feature/voxel-door-front
- 작업: main(a2b945c)을 `feature/voxel-door-front`에 머지했다. 에디터가 이 브랜치를 열고 있다.
- 작업: `Plaza.unity` 충돌은 이 브랜치의 씬을 기준으로 위층 요소 9개를 다시 삭제해서 해결했다.
- 결과: 씬 문서는 361개다. 끊어진 로컬 참조는 0개다. 충돌 표시는 0개다.
- 확인 안 함: 에디터에서 씬 다시 불러오기와 화면 확인.
- 다음: 에디터에서 Plaza를 다시 열어 레인 F 뒤쪽 화면을 확인한다.
