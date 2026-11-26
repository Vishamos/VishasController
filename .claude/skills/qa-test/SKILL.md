---
name: qa-test
description: VishasController 검증용 테스트 규칙. qa가 명세 기준 자동 테스트를 쓰고 실행할 때, 사용자 시나리오(TestServer + 트레이 앱)를 준비·판정할 때 사용한다 (테스트 프로젝트 위치·만드는 조건·실행 명령, 경계값, 시나리오 로그 위치와 비우는 방법).
---

# 검증용 테스트 (VishasController)

## 1. 자동 테스트
| 항목 | 값 |
|---|---|
| 위치 | `tests/VishasController.Tests/` (커밋 대상) |
| 대상 프레임워크 | `net10.0-windows` (WinForms 프로젝트를 참조하므로) |
| 실행 | `dotnet test VishasController.sln` |
| 시나리오 문서 | `docs/qa/<묶음 이름>.md` |

- **테스트 프로젝트는 아직 없다.** 만들려면 테스트 프레임워크(xUnit 또는 MSTest) NuGet 패키지가 필요하므로, qa는 직접 만들지 않고 "테스트 프로젝트 필요"와 이유를 보고한다. 메인 스레드가 사용자 승인을 받아 worklist 작업으로 만든다(worklist Test-1).
- 테스트 프로젝트가 없는 동안에는 구조 검토와 사용자 시나리오로 판정한다.
- 테스트 대상은 화면과 분리된 로직이다: `MessageParser`(JSON 해석), `AppSettings.TryParseServerUrl`, 재접속 간격 계산, 알림 묶음 규칙. 화면이 필요한 로직을 테스트하려면 먼저 로직 분리를 제안한다.
- 경계값(이 프로젝트에서 꼭 넣을 것):
  - 메시지: 깨진 JSON, `data`가 객체가 아님, 키 없음, 값 형식이 다름(`time`이 문자열 등), 모르는 `type`, 모르는 `state`, `emojis`가 빈 객체·객체 아님
  - 등록: `client.registered` 전 메시지, 등록 응답 404, 등록 응답 없음(시간 초과)
  - 크기: 64KB 정확히, 64KB + 1바이트, 바이너리 프레임
- 쓰지 않는 것: 실제 VishaSynapse 서버, 치지직, 실제 설정 파일(`%LOCALAPPDATA%\VishaSTD\controller\settings.json`, 옛 위치 `%AppData%\VishasController\settings.json`).
- 서버가 필요한 테스트는 테스트 안에서 `127.0.0.1`의 임의 포트(`HttpListener` 등)에 가짜 WebSocket 서버를 띄우고 끝나면 닫는다.

## 2. 사용자 시나리오
- 도구: `tools/TestServer`(VishaSynapse 대역)와 트레이 앱. 둘 다 사용자가 실행한다.
- 시나리오 문서에는 TestServer 실행 명령, 콘솔 명령 순서(`m`, `b 20`, `d`, `x`, `kick`, `big`, `u` 등), 단계마다 기대하는 아이콘 색·알림·채팅 창 동작, 기대 로그를 적는다.

## 3. 시나리오 로그
- 파일: `%LOCALAPPDATA%\VishaSTD\controller\logs\qa_scenario.log` (저장소 밖이라 git과 무관).
- **아직 이 파일에 쓰는 코드가 없다.** 로그로 판정해야 하는 시나리오가 필요하면 qa는 로그 지점 `[Proposal]` 이슈부터 올린다. 형식은 qa 에이전트의 "로그 규칙"을 따른다.
- 비우기: 실행 전에 사용자가 트레이 앱을 끈 상태에서 파일을 지운다(앱이 시작할 때 새로 만든다).
- 로그에 서버 주소, 닉네임, 채팅 내용을 남기지 않는다. 이벤트 이름과 개수·상태 값만 남긴다.
- 같은 폴더의 `settings.json`은 읽지 않는다(서버 주소가 있음).
