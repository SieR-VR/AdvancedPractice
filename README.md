# AdvancedPractice

Beat Saber **1.40.8**용 연습 시작 화면 확장 플러그인입니다. 원본 참고: [denpadokei/PracticePlugin](https://github.com/denpadokei/PracticePlugin). 원본 코드를 포함하지 않는 독립 구현입니다.

## 기능

- **Song Start:** 매핑 툴의 beat(0부터 시작)를 1 단위로 지정합니다. BPM 변경 이벤트를 반영한 시작 시각을 소수점 세 자리 seconds로 표시하고 해당 위치 BPM을 표시합니다.
- **First notes:** Play 버튼 왼쪽의 `Next L / R`에 각 손의 다음 노트를 따로 표시합니다. 두 노트의 등장 시각이 달라도 왼손·오른손 슬롯을 유지하며, 해당 손에 남은 노트가 없으면 `None`으로 표시합니다. 폭탄·체인 링크는 제외하고 무방향 노트는 점으로 표시합니다. 기본 빨강/파랑 색과 맵 원본 방향을 사용하므로 커스텀 색상·왼손 모드·게임플레이 미러 변환은 반영하지 않습니다.
- **Speed:** 시작 위치의 원본 BPM을 기준으로 목표 BPM과 배속 %를 표시합니다. `-10`, `-5`, `-1` 및 `+1`, `+5`, `+10` 버튼을 제공합니다. 지원 배속은 10–500%입니다. 시작 beat를 바꾸면 목표 BPM을 유지하면서 %를 다시 계산하고 범위에 맞게 제한합니다. 재생 중에는 선택한 배속이 고정되므로 이후 BPM 변화에도 배속은 같습니다.
- **NJS:** 기본 NJS를 1–50 범위로 변경합니다. Mod Settings → AdvancedPractice에서 단위를 바꿀 수 있으며 다음 연습 화면 진입 시 적용됩니다. 기본값은 1입니다.
- **VNJS:** 게임 표준 NJS 이벤트와 easing을 사용해 선택 위치의 **원본 맵 NJS**를 표시합니다. NJS 조절은 기본 NJS를 바꾸며 맵의 상대 VNJS 이벤트를 유지합니다. Noodle Extensions의 커스텀 트랙 애니메이션은 이 표시의 대상이 아닙니다.
- JD 및 spawn offset을 수정하지 않습니다. JDFixer / BSPlus가 관리할 수 있습니다.

처음 선택한 맵은 이전 곡의 배속을 상속하지 않고 시작 위치 원본 BPM의 100%로 열립니다. 선택한 beat·목표 BPM·NJS는 게임 실행 중 곡·특성·난이도별로 유지됩니다. 연습 종료 후 같은 맵으로 돌아오면 마지막 선택값을 복원하며, 게임을 다시 시작하면 초기화됩니다.

## 설치

1. Beat Saber 1.40.8에 BSIPA와 BeatSaberMarkupLanguage를 설치합니다.
2. **PracticePlugin을 제거합니다.** 함께 로드되면 AdvancedPractice는 오류를 기록하고 기능을 활성화하지 않습니다.
3. `bin/Release/AdvancedPractice.dll`을 게임의 `Plugins` 폴더에 복사합니다.
4. 게임을 다시 시작하고 곡 선택 화면에서 Practice Mode에 진입합니다.

이 버전은 요청한 **곡 시작 전 연습 설정 화면**을 구현합니다. 원본 PracticePlugin의 일시정지 seek/loop, 실패 시간 표시, 에너지 보정 기능은 포함하지 않습니다. 게임 폴더에는 자동 배포하지 않습니다.

## 빌드 및 검증

.NET SDK와 .NET Framework 4.8 참조 어셈블리가 필요합니다. `AdvancedPractice.csproj.user`에 로컬 게임 경로를 지정합니다(이 파일은 Git에서 제외됩니다).

```xml
<Project><PropertyGroup><BeatSaberDir>D:\Games\Beat Saber</BeatSaberDir></PropertyGroup></Project>
```

```powershell
dotnet build AdvancedPractice.csproj -c Release
dotnet run --project Tests/AdvancedPractice.Tests.csproj -c Release
```

게임 DLL을 직접 참조하므로 1.44.1 등 다른 버전의 호환성을 보장하지 않습니다. 테스트는 BPM 변환 및 배속 계산을 검증하며 Unity/BSML 통합 테스트를 대체하지 않습니다.

## 게임 내 확인 항목

VR 실행 검증은 아직 수행되지 않았습니다. 실제 사용 전 다음을 확인합니다.

- 일반 맵과 BPM 변경 맵에서 beat, seconds, BPM이 매핑 툴과 일치하는지 확인합니다.
- Speed 버튼 및 슬라이더 변경 후 표시 %와 실제 재생 속도를 확인합니다.
- VNJS 변화 구간과 easing 구간에서 표시 NJS 및 변경된 기본 NJS를 확인합니다.
- Mod Settings 단위 변경 후 연습 화면을 다시 열어 NJS 간격을 확인합니다.
- 다른 난이도 선택, 재시작, 일반 플레이 복귀 시 설정 누수와 중복 UI가 없는지 확인합니다.
- JDFixer / BSPlus 사용 시 JD 설정이 유지되는지 확인합니다.
- VR에서 텍스트 겹침, Play 버튼 접근성, 컨트롤러 조작을 확인합니다.
