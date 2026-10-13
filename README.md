# Monster Strike Runner

몬스터를 공격해 처치하고 방어선을 지키는 모바일 세로형 2D 러너입니다. 시작 → 전투 → 결과 → 재시작으로 한 판을 반복합니다.

## 전투 규칙

- 60초 생존, HP 3으로 시작합니다.
- 일반 몬스터(보라색)는 HP 1, 강화 몬스터(주황색)는 HP 2입니다. 머리 위 표시로 남은 체력을 확인합니다.
- 현재 플레이어의 가로 위치에서 앞으로 발사합니다. 스와이프로 몬스터의 차선에 맞춰 공격하세요.
- 일반 공격은 피해 1이고 첫 몬스터에 맞으면 사라집니다. 재사용 대기 시간은 0.35초입니다.
- 충전 버튼을 0.65초 이상 누른 뒤 놓으면 피해 2의 관통 공격을 발사합니다. 짧게 누르고 놓으면 일반 공격과 같은 피해 1입니다.
- 몬스터가 플레이어 뒤의 방어선을 넘으면 어느 차선이든 한 마리당 HP 1과 점수 최대 30이 감소합니다. 피해서 방치해도 피해를 받습니다.
- 일반 몬스터 처치 +50점, 강화 몬스터 처치 +90점, 시간 경과나 이동으로 점수가 오르지 않습니다. 점수는 음수가 되지 않습니다.
- HP 0 또는 60초 경과 시 결과 화면에 최종 점수, 처치 수, 놓친 수를 표시합니다.
- 재시작하면 HP, 점수, 처치·누락 수, 몬스터, 충전 상태를 초기화합니다.

## 플레이 조작 3종

| 입력 방식 | 모바일 조작 | 역할 |
| --- | --- | --- |
| 스와이프 | 화면에서 좌우로 끌기 | 차선 변경과 공격 방향 조준 |
| 클릭 버튼 | 하단 ATTACK 클릭 | 일반 공격 |
| 홀드 버튼 | 하단 HOLD CHARGE를 누른 뒤 놓기 | 시간에 따라 위력이 달라지는 충전 공격 |

세 방식이 한 플레이 화면에서 함께 사용됩니다. 시작·재시작 버튼은 조작법 3종에 포함하지 않습니다. PC에서는 좌우 방향키, Space(일반 공격), 아래 방향키 유지 후 해제(충전 공격)를 사용합니다.

## 실행과 웹 빌드

- Unity 6000.3.23f1 / Input System / WebGL. 캔버스와 UI 기준 480 × 854.
- Assets/Scenes/SampleScene을 열고 Play → START로 실행합니다. Inspector 연결 작업은 추가로 필요하지 않습니다.
- Runner → Build Web for GitHub Pages 메뉴는 프로젝트 루트 docs에 웹 빌드를 생성합니다.
- 커스텀 RunnerPortrait 템플릿, Brotli 압축, Decompression Fallback 활성화, 코드 용량 최적화(LTO), WebGL 관리 코드 High stripping, SampleScene 포함으로 빌드합니다.
- Build And Run 또는 HTTP 서버로 실행해야 합니다. index.html을 더블클릭하는 file:// 실행은 지원하지 않습니다.

## GitHub Pages 제출

배포 저장소: https://github.com/LeeGeonMyeong/MonsterEscapeRunner

공개 게임: https://leegeonmyeong.github.io/MonsterEscapeRunner/

로컬 작업 폴더 D:/Git/Runner도 이 저장소와 연결되어 있습니다. 실제 스마트폰 검증은 위 URL로 진행하세요.

1. 본인의 Public GitHub 저장소에 Assets, Packages, ProjectSettings, README.md, .gitignore, docs를 넣습니다.
2. Library, Logs, obj 등은 .gitignore로 제외합니다. docs/Build는 제외하지 않습니다.
3. GitHub Settings → Pages → Deploy from a branch → main /docs → Save.
4. 위 공개 URL로 스마트폰에서 스와이프, 일반 공격, 충전 공격과 결과·재시작을 확인합니다.
5. 저장소 주소와 Pages 주소를 제출합니다.

## 검증과 백업

Unity 전투 검사: Assets/Editor/RunnerSmokeCheck.cs. 기존 RunnerMobileCheck.Start도 같은 통합 검사를 실행합니다. 테스트 코드는 빌드에 포함되지 않습니다.
전투 변경 전 백업: DevelopmentBackup/BeforeCombat. 이전 안내 문서들은 이전 회피 버전의 기록입니다.

## 휴대폰 테스트와 캡처

[테스트 안내](output/mobile/휴대폰_테스트_안내.md) · [QR](output/mobile/MonsterStrikeRunner-QR.png) · [공개 전투 화면](output/mobile/Public-Mobile-Combat.jpg)

기존 로컬 웹 파일 66.36 MiB에서 10.15 MiB로 약 84.7% 감소했습니다. 실제 스마트폰 테스트는 공개 게임 주소로 진행하세요.
