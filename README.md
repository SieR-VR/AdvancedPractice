# AdvancedPractice

Beat Saber **1.44.1**을 우선 지원하는 연습 시작 화면 확장 플러그인입니다. [LegatoBS 0.1.1](https://github.com/ScoreSaber/legato)을 소스 포함 방식으로 사용합니다. 원본 참고: [denpadokei/PracticePlugin](https://github.com/denpadokei/PracticePlugin). 원본 코드를 포함하지 않는 독립 구현입니다.

## 기능

- **Song Start:** 매핑 툴의 beat(0부터 시작)를 1 단위로 지정합니다. BPM 변경 이벤트를 반영한 시작 시각을 소수점 세 자리 seconds로 표시하고 해당 위치 BPM을 표시합니다.
- **First notes:** Play 버튼 왼쪽의 `Next L / R`에 각 손의 다음 노트를 따로 표시합니다. 두 노트의 등장 시각이 달라도 왼손·오른손 슬롯을 유지하며, 해당 손에 남은 노트가 없으면 `None`으로 표시합니다. 폭탄·체인 링크는 제외하고 무방향 노트는 점으로 표시합니다. 기본 빨강/파랑 색과 맵 원본 방향을 사용하므로 커스텀 색상·왼손 모드·게임플레이 미러 변환은 반영하지 않습니다.
- **Speed:** 시작 위치의 원본 BPM을 기준으로 목표 BPM과 배속 %를 표시합니다. `-10`, `-5`, `-1` 및 `+1`, `+5`, `+10` 버튼을 제공합니다. 지원 배속은 10–500%입니다. 시작 beat를 바꾸면 목표 BPM을 유지하면서 %를 다시 계산하고 범위에 맞게 제한합니다. 재생 중에는 선택한 배속이 고정되므로 이후 BPM 변화에도 배속은 같습니다.
- **NJS:** 기본 NJS를 1–50 범위로 변경합니다. Mod Settings → AdvancedPractice에서 단위를 바꿀 수 있으며 다음 연습 화면 진입 시 적용됩니다. 기본값은 1입니다.
- **VNJS (1.40.8 / 1.44.1):** 게임 표준 NJS 이벤트와 easing을 사용해 선택 위치의 **원본 맵 NJS**를 표시합니다. NJS 조절은 기본 NJS를 바꾸며 맵의 상대 VNJS 이벤트를 유지합니다. Noodle Extensions의 커스텀 트랙 애니메이션은 이 표시의 대상이 아닙니다.
- JD 및 spawn offset을 수정하지 않습니다. JDFixer / BSPlus가 관리할 수 있습니다.

처음 선택한 맵은 이전 곡의 배속을 상속하지 않고 시작 위치 원본 BPM의 100%로 열립니다. 선택한 beat·목표 BPM·NJS는 게임 실행 중 곡·특성·난이도별로 유지됩니다. 연습 종료 후 같은 맵으로 돌아오면 마지막 선택값을 복원하며, 게임을 다시 시작하면 초기화됩니다.

## 버전별 지원 상태

| 게임 버전 | Legato 프로필 | 상태 |
| --- | --- | --- |
| 1.44.1 | 1.42.0 | 우선 지원: 실제 게임 DLL 및 BSML 1.14.1 빌드·API·manifest 검증 완료 |
| 1.40.8 | 1.40.0 | 기존 지원: 실제 게임 DLL 및 BSML 1.12.5 재검증 완료 |
| 1.29.0 | 1.29.0 | 예비 지원: 호환 분기·빌드 설정만 검증, 실제 참조 빌드 미검증 |
| 1.37.1 | 1.37.1 | 예비 지원: 호환 분기·빌드 설정만 검증, 실제 참조 빌드 미검증 |
| 1.38.0 | 1.38.0 | 예비 지원: 호환 분기·빌드 설정만 검증, 실제 참조 빌드 미검증 |

모든 버전의 VR 실행 검증은 별도로 필요합니다. **버전별 DLL을 따로 빌드**하며 하나의 DLL을 모든 버전에 공통 설치하는 방식이 아닙니다. 구버전에서는 기본 NJS를 조정하고 표준 VNJS 이벤트 표시는 제공하지 않습니다. 1.29.0의 기존 difficulty beatmap 로더, 1.37.1의 로더 인수, 구 BSML의 소문자 필드/싱글턴 API를 `Compatibility/`에서 분리합니다.

## 설치

1. 대상 게임 버전에 맞는 BSIPA와 BeatSaberMarkupLanguage를 설치합니다. 1.44.1은 BSIPA 4.3.7+, BSML 1.14.1+가 필요합니다.
2. **PracticePlugin을 제거합니다.** 함께 로드되면 AdvancedPractice는 오류를 기록하고 기능을 활성화하지 않습니다.
3. 게임 버전과 일치하는 `artifacts/AdvancedPractice-0.2.0-bs<버전>.zip`의 `Plugins` 폴더를 게임 폴더에 복사합니다. 직접 빌드한 DLL은 `bin/Release/<버전>/AdvancedPractice.dll`에 있습니다.
4. 게임을 다시 시작하고 곡 선택 화면에서 Practice Mode에 진입합니다.

이 버전은 요청한 **곡 시작 전 연습 설정 화면**을 구현합니다. 원본 PracticePlugin의 일시정지 seek/loop, 실패 시간 표시, 에너지 보정 기능은 포함하지 않습니다. 게임 폴더에는 자동 배포하지 않습니다.

## 빌드 및 검증

.NET 10 SDK, .NET Framework 4.8 참조 어셈블리 및 PowerShell 7이 필요합니다. 각 버전의 게임 설치/참조 폴더에는 BSIPA가 필요하며, 해당 버전의 BSML DLL을 게임의 `Plugins` 또는 별도 모드 참조 폴더에 준비합니다.

```powershell
# 기본 대상은 1.44.1입니다. 빌드, API 검사, manifest 검사, ZIP 생성을 수행합니다.
pwsh ./Build/Build.ps1 -GameDirectory 'D:\Games\Beat Saber 1.44.1'
# BSML 참조를 게임에 설치하지 않고 별도로 준비한 경우:
pwsh ./Build/Build.ps1 -GameVersion 1.44.1 -GameDirectory 'D:\Games\Beat Saber 1.44.1' -ModReferencesDirectory 'D:\Refs\1.44.1\Plugins'
# 기존 버전 / 예비 버전도 각각 그 버전의 참조 폴더로 빌드합니다.
pwsh ./Build/Build.ps1 -GameVersion 1.40.8 -GameDirectory 'D:\Games\Beat Saber 1.40.8'
pwsh ./Build/Build.ps1 -GameVersion 1.29.0 -GameDirectory 'D:\Games\Beat Saber 1.29.0'
pwsh ./Build/Build.ps1 -GameVersion 1.37.1 -GameDirectory 'D:\Games\Beat Saber 1.37.1'
pwsh ./Build/Build.ps1 -GameVersion 1.38.0 -GameDirectory 'D:\Games\Beat Saber 1.38.0'

dotnet run --project Tests/AdvancedPractice.Tests.csproj -c Release
pwsh ./Tests/BuildProfiles.Tests.ps1
```

`Build/Versions.props`가 Legato 대상, 의존성 범위, 지원 상태, 출력 경로를 정의합니다. 빌드 시 원본 `manifest.json`을 바탕으로 버전별 manifest를 생성해 DLL에 포함합니다. 지원하지 않는 버전, 없는 참조, 설치 버전 파일과 다른 대상은 오류로 처리합니다. 예비 버전 패키지는 `-preview.zip`으로 표시합니다. 테스트는 게임 내 UI/플레이 검증을 대체하지 않습니다.

직접 MSBuild를 사용할 수도 있습니다. `AdvancedPractice.csproj.user`는 Git에서 제외되며 개인 경로를 저장할 수 있습니다. 기존 1.40.8 경로가 저장되어 있다면 **기본 대상 1.44.1과 맞지 않으므로** 명시적으로 대상과 참조를 지정해야 합니다.

```powershell
dotnet build AdvancedPractice.csproj -c Release /p:LegatoTargetVersion=1.44.1 '/p:BeatSaberDir=D:\Games\Beat Saber 1.44.1'
```

Legato CLI를 다시 설정할 때는 설치된 CLI를 사용합니다. .NET 8 런타임 없이 .NET 10만 있는 환경에서는 해당 터미널에 `$env:DOTNET_ROLL_FORWARD='Major'`를 설정해 실행할 수 있습니다. 플러그인 실행 시 Legato DLL이나 CLI는 필요하지 않습니다.

## 라이선스

Legato 소스가 DLL에 포함되므로 배포 라이선스는 GPLv3입니다. `LICENSE`와 `THIRD_PARTY_NOTICES.md`를 확인하세요. 생성되는 배포 ZIP은 정확한 빌드 소스와 Legato 0.1.1의 소스를 `corresponding-source.zip`에 포함합니다. 게임/BSIPA/BSML 참조 DLL과 개인 설정 파일은 배포하지 않습니다.

## 게임 내 확인 항목

VR 실행 검증은 아직 수행되지 않았습니다. 실제 사용 전 다음을 확인합니다.

- 일반 맵과 BPM 변경 맵에서 beat, seconds, BPM이 매핑 툴과 일치하는지 확인합니다.
- Speed 버튼 및 슬라이더 변경 후 표시 %와 실제 재생 속도를 확인합니다.
- VNJS 변화 구간과 easing 구간에서 표시 NJS 및 변경된 기본 NJS를 확인합니다.
- Mod Settings 단위 변경 후 연습 화면을 다시 열어 NJS 간격을 확인합니다.
- 다른 난이도 선택, 재시작, 일반 플레이 복귀 시 설정 누수와 중복 UI가 없는지 확인합니다.
- JDFixer / BSPlus 사용 시 JD 설정이 유지되는지 확인합니다.
- VR에서 텍스트 겹침, Play 버튼 접근성, 컨트롤러 조작을 확인합니다.
