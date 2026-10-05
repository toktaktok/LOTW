# 리소스 임포트와 분수 배치

## 2026-10-05 17:55 | feature/resource-import
- 작업: `Downloads/Resource`의 새 파일 67개(이미지 62개, `.vox` 4개, `.wav` 1개)를 규칙에 맞게 `Game/Assets/Project/`로 가져왔다.
- 작업: 가져오는 과정에서 `ANIM_FountainWater`, `AC_FountainWater`, `PF_Fountain`, `PF_Fountain_mat0`, `TEX3D_PF_Fountain_Occupancy`가 생겼다.
- 작업: 분수 물 스프라이트 시트 `TX_E_FountainWater`(8프레임), `ANIM_FountainWater`, `AC_FountainWater`를 만들었다.
- 작업: `PlazaLayout.cs`의 분수 임시 박스와 물줄기를 지우고 `PF_Fountain` 복셀 수반과 물 스프라이트로 바꿨다.
- 작업: Plaza 씬은 전체 빌드 없이 분수 그룹만 고쳤다. 두꺼비와 `FX_FountainSpray`는 물 꼭대기(Y 7.8)로 옮겼다.
- 결과: EditMode 테스트 250개가 통과했다. `Validate Voxel Scale`이 OK를 냈다.
- 결과: Play 모드에서 물 스프라이트 이름을 여러 시점에 읽었다.
- 확인 안 함: 8프레임이 모두 화면에 나오는지. 스크린샷 한 장으로는 증명되지 않는다.
- 확인 안 함: 영상 12-21초 장면과 나란히 놓은 픽셀 단위 비교. 눈으로만 비교했다.
- 다음: 사용자가 분수 크기와 물 높이를 영상과 비교해 확인한다.

## 2026-10-05 19:36 | feature/resource-import
- 작업: 두꺼비를 사용자가 둔 자리(월드 19.94, -0.01, 6.63)로 되돌렸다.
- 작업: `TX_C_Zig_Small`을 `TX_C_Zig_Shrink`로 바꾸고 64x64 8칸으로 잘랐다. `TX_C_Cham1_Idle`을 9칸으로 잘랐다.
- 작업: `TX_E_Window_005`, `TX_E_Window_010`을 108x108로 맞췄다. `.vox` 4개의 meta 설정을 고쳤다.
- 작업: `MAT_FountainStone`, `MAT_FountainWater`와 이 재질을 만드는 `PlazaRenderSetup.cs` 코드를 지웠다.
- 작업: `FountainWater`와 `FX_FountainSpray`를 분수 그룹 로컬 Z 6.2(그릇 앞 2.8)로 옮겼다. `PlazaLayout.cs`도 같은 값을 쓴다.
- 결과: Plaza 씬을 저장했다. 저장 전에 씬은 dirty가 아니었다.
- 확인 안 함: 물 위치 변경 후 화면. 스크린샷을 찍지 않았다.
- 다음: 분수를 `PF_FountainSet` 프리팹으로 바꿀지 사용자가 정한다.

## 2026-10-05 19:40 | feature/fountain-set
- 작업: main(`b6998df`)을 origin에 푸시했다.
- 작업: Plaza 씬의 `Fountain_Basin`, `FountainWater`, `FX_FountainSpray`를 `PF_FountainSet` 프리팹 하나로 묶었다.
- 작업: `PlazaLayout.cs`는 분수를 `PF_FountainSet` 배치 하나로 만든다. 물 스프라이트와 분무 항목을 지웠다.
- 작업: `PlazaSceneBuilder.cs`에서 `CreateSpray`와 Spray 상수 10개를 지웠다. 분무 설정은 프리팹에 저장된다.
- 결과: 컴파일 오류가 없었다. 씬 인스턴스의 월드 위치(0, 0, -4.78)가 그대로이고 오버라이드가 0개다.
- 확인 안 함: `LOTW/Plaza/5 Build Scene` 전체 재빌드. 재빌드하면 직접 고친 배치가 지워진다.
- 다음: 없음.
