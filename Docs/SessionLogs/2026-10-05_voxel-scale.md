# 복셀 크기 조정 (RT 5px -> 4px)

## 2026-10-05 20:25 | feature/voxel-scale-4px
- 작업: `CameraDefines.VoxelPixels`를 5에서 4로 바꿨다. 복셀 importScale은 0.16이다.
- 작업: 메뉴 `LOTW/Voxel/Apply Voxel Scale To Prefabs`(`VoxelScaleApplier.cs`)를 추가했다. 이 메뉴로 복셀 프리팹 23개의 메시와 DSS `_ImportScale`을 바꿨다.
- 작업: `PF_FlowerShop`, `PF_SnowOffice`, 가로등의 자식 위치를 0.8배로 옮겼다. 데칼 이미지는 바꾸지 않았다.
- 작업: `TX_E_FountainWater`를 프레임당 240x152로 리샘플했다. `PF_FountainSet`의 물과 분무를 0.8배로 맞췄다.
- 작업: 빌더로 다시 만든 `Plaza.unity`와 `NavMesh-Ground.asset`을 HEAD로 되돌렸다. 재빌드가 수동 배치(레일 A3-A7, 분수 위치)를 지웠기 때문이다.
- 결과: `Validate Voxel Scale`이 OK를 냈다. EditMode 테스트 250개가 통과했다.
- 확인 안 함: 분무 파티클 움직임. 정지 화면만 캡처했다.
- 확인 안 함: 외곽선 깊이 임계값 0.8 유닛. 이제 5복셀 깊이에 해당한다. 얕은 홈의 외곽선이 사라지는지 화면으로 보지 않았다.
- 확인 안 함: Play 모드에서 `PlayerController.cs:73`, `PlayerInteractor.cs:45`에 NullReferenceException이 났다. 이번 변경은 런타임 코드를 바꾸지 않았다. HEAD에서 같은 오류가 나는지 확인하지 않았다.
- 다음: 에디터에서 건물 간격과 문 높이를 눈으로 확인한다. 필요하면 Plaza 배치를 손으로 조정한다.
