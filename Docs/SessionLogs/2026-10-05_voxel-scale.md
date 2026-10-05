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

## 2026-10-05 21:00 | feature/voxel-scale-4px
- 작업: 복셀 밀도를 건물별로 정하도록 바꿨다. 문 높이는 캐릭터 키(120px)의 약 1.6배(192px)다.
- 작업: 프리팹, 머티리얼, `TX_E_FountainWater`, `Decors` 텍스처, `PlazaLayout.cs`, `CameraDefines.VoxelPixels`를 5px(main) 상태로 되돌렸다.
- 작업: 메뉴 `LOTW/Voxel/Apply Voxel Scale To Prefabs`를 `LOTW/Voxel/Set Voxel Pixels...`로 바꿨다. 이 메뉴는 선택한 프리팹에만 적용한다.
- 작업: `VoxelScaleValidator`는 importScale이 xyz 동일하고 정수 px / 25인지 검사한다.
- 작업: `PF_CommonBuilding_001`을 13px, `_004`와 `_005`를 6px, `_008`, `PF_CommonHouse_001`, `PF_MiddleHouse_1`을 4px로 적용했다.
- 결과: EditMode 테스트 250개가 통과했다. 프리팹은 `Validate Voxel Scale` 검사를 통과했다.
- 확인 안 함: 열린 `Character.unity`의 `PF_SnowOffice` 인스턴스(scale 1.1)가 검사에서 1건 걸렸다. 이번 변경과 무관하다.
- 확인 안 함: Plaza Play 모드 스크린샷. 저장하지 않은 `Character.unity`가 열려 있어 장면을 바꾸지 않았다.
- 다음: 에디터에서 건물 문 높이와 간격을 눈으로 확인한다.

## 2026-10-05 21:09 | feature/voxel-scale-4px
- 작업: Fable 리뷰에서 나온 코드 지적 4건을 고쳤다.
- 작업: `VoxelScaleValidator`는 머티리얼 `_ImportScale`이 importScale과 다르면 보고한다.
- 작업: `VoxelScaleApplier`는 메시 재생성이 실패하면 importScale을 되돌리고 저장하지 않는다. 같은 배율 판정은 xyz 전체를 비교한다. `materials`가 null이면 건너뛴다.
- 결과: Editor 어셈블리가 오류 없이 다시 컴파일되었다.
- 확인 안 함: `PF_Fountain` importScale 0.3과 `Plaza.unity` 수정. 이 변경은 다른 분수 작업에서 나왔다. 이 커밋에 넣지 않았다.
- 다음: 건물 .vox를 회전해서 문을 -Z 정면으로 맞춘다. `Plaza.unity` 분수 작업이 커밋된 뒤에 시작한다.

## 2026-10-05 21:57 | feature/voxel-door-front
- 작업: 건물 .vox 12개를 회전해서 문이 yaw 0에서 -Z(카메라 쪽)를 보게 했다. 180도: Bar_Outside, RabbitHouse, MiddleHouse_1, OldApartment. -90도: CandyShop, CommonBuilding_001/002/004/005/008, CommonHouse_001. +90도: SnowmanOffice. 원본은 `C:/tmp/vox/orig/`에 있다.
- 작업: .vox의 SIZE, XYZI만 바꿨다. RabbitHouse는 nTRN `_t`의 x, y 부호도 바꿨다(`17 5 67` -> `-17 -5 67`). 피벗이 회전한 옛 경계와 같아야 하기 때문이다.
- 작업: 프리팹 12개의 메시를 다시 만들고 DSS 점유 텍스처를 다시 구웠다. `PF_SnowOffice`의 직계 자식 12개는 root 기준으로 Y축 +90도 돌렸다.
- 작업: 수동 배율 프리팹 5개(`PF_CommonBuilding_001`, `PF_Bench_1`, `PF_DrinkMachine_3`, `PF_Fountain`, `PF_StreetLight_1`)의 머티리얼 `_ImportScale`을 importScale에 맞췄다. importScale은 바꾸지 않았다.
- 작업: `Plaza.unity`의 인스턴스 12개 yaw를 Y - R로 바꿨다. YAML을 직접 고친 뒤 장면을 다시 열었고, dirty가 아니었다.
- 결과: 12개 프리팹 모두 새 경계가 옛 경계를 R만큼 돌린 값과 같다. 노멀 개수는 면 방향을 따라 옮겨졌다.
- 결과: `Validate Voxel Scale`은 수동 배율 9건만 보고했다(알려진 항목). EditMode 테스트 249개가 통과했다.
- 결과: 같은 카메라 4곳에서 전후 화면을 비교했다. 달라진 곳은 `PF_CommonBuilding_001`과 수동 배율 프리팹 4개의 DSS 음영이다. 실루엣은 같다.
- 확인 안 함: `Test/Character.unity`의 인스턴스 6개(CommonBuilding_001/002/004, CommonHouse_001, Bar_Outside, SnowOffice). 이 장면은 고치지 않았다. 건물이 90~180도 돌아 보인다.
- 확인 안 함: `PlazaLayout.cs`. main에 이 파일이 없어서 고치지 않았다.
- 확인 안 함: Play 모드 화면. 에디터 장면에서 임시 카메라로만 확인했다.
- 다음: `Test/Character.unity`의 인스턴스 yaw를 Y - R로 바꾼다.
- 작업: Fable 리뷰에서 막히는 결함은 없었다. 규칙 문서에 "프리팹이 있는 모델만 회전했다"를 적었다.
- 확인 안 함: 프리팹이 없는 모델(OldApartment_2/_3/_Laundry, MiddleHouse_2/_3/_4)은 회전하지 않았다. 이 모델들은 아직 문이 +Z를 본다.
