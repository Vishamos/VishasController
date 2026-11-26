---
name: coding-rule
description: VishasController C# 코딩 표준. 코드를 작성·수정·리뷰할 때 visha-common:coding-common-rule과 함께 적용한다 (.NET 10 WinForms, UI 스레드, async·취소, WebSocket 수신, JSON 해석, 서버 주소 취급, 스타일, 의존성 점검 명령, 보고에 더할 것).
---

# C# 코딩 표준 (VishasController)

공통 원칙은 `visha-common:coding-common-rule`, 확인 절차는 `simple-test`, 테스트 작성은 `qa-test`를 따른다. 이 스킬은 이 프로젝트에만 해당하는 것을 담는다.

## 0. 기준
- 구현 기준은 기술 명세(`docs/tech_spec/`) 하나다.
- 서버 규약(시그니처, 등록, 메시지, 에러 코드)은 명세 머리말의 "참조 규약" 경로를 GitHub에서 읽는다(`gh api repos/Vishamos/VishaSynapse/contents/docs/protocol/README.md`). 스킬·기억·시작 자료 요약에 의존하지 않는다.
- 이 스킬과 어긋나는 작업(예: 기존 파일을 통째로 다시 쓰기)이 필요하면 하기 전에 "규칙 예외 제안"으로 알린다.

## 1. 분석
- 대상 파일을 끝까지 읽고 Grep으로 사용처(이벤트 구독, 호출부)를 찾아 영향 범위를 확인한다.

## 2. 런타임과 의존성
- .NET 10(`net10.0-windows`, WinForms), `Nullable`·`ImplicitUsings` 켬. 표준 라이브러리를 우선한다(`System.Net.WebSockets.ClientWebSocket`, `System.Text.Json`).
- NuGet 패키지 추가는 사용자 승인이 필요하다.
- **의존성 점검 명령** (security가 사용): `dotnet list VishasController.sln package --vulnerable` (조회만). 지금은 패키지 참조가 없다.

## 3. 스레드
- 화면(컨트롤, `NotifyIcon`)은 UI 스레드에서만 만진다. 백그라운드 → UI는 `SynchronizationContext.Post`(기존 `SynapseClient.Post`)로 넘긴다. `Control.Invoke`로 UI 스레드를 막지 않는다.
- 수신 루프는 `Task.Run` 안에서 돈다. 라이브러리 쪽 `await`에는 `ConfigureAwait(false)`를 붙인다.
- 스레드 사이에 공유하는 단순 플래그는 `Volatile`/`Interlocked`, 그 밖에는 `lock`으로 지킨다.
- 송출 PC 부담: 타이머 폴링·상시 다시 그리기를 넣지 않는다. 이벤트가 올 때만 화면을 고친다.

## 4. async와 취소
- 긴 작업은 `CancellationToken`을 받는다. 종료는 `Cancel` → 루프 `await` → `Dispose` 순서(기존 `StopAsync`).
- `async void`는 WinForms 이벤트 핸들러에만 쓰고, 안에서 예외를 잡는다(잡지 않으면 앱이 죽는다).
- 시간 제한은 `CancellationTokenSource.CancelAfter`나 `CreateLinkedTokenSource`로 건다.

## 5. WebSocket 수신
- 접속 직후 `client.register`를 보내고, `client.registered`를 받은 뒤에만 정상 상태로 본다.
- 메시지는 `EndOfMessage`까지 모아 한 번에 해석한다. 64KB를 넘거나 바이너리면 버리고 연결은 유지한다.
- 끊김·예외는 재접속 루프로 넘긴다(간격 1초부터 2배, 최대 30초). 재접속 중에도 UI는 멈추지 않는다.
- 프로토콜 값(시그니처 41, type 문자열, 에러 코드, 한도)은 상수로 정의해 쓰고 직접 적지 않는다.

## 6. JSON 해석
- `JsonDocument.Parse`는 `try` 안에서 하고, 깨진 JSON은 버린다.
- 키가 없거나 형식이 다르면 기본값(`""`, `Unknown`)으로 받는다(`MessageParser.GetString` 방식). 모르는 `type`·키는 무시한다.
- 받은 문자열(닉네임, 채팅 내용, 이모지 URL)은 사용자 입력으로 취급한다. 실행·경로·URL 열기에 그대로 쓰지 않는다.

## 7. 서버 주소와 로그
- 서버 주소는 `AppSettings`(`%LOCALAPPDATA%\VishaSTD\controller\settings.json`, 명세 02 §5)에서만 읽는다. 코드 기본값은 `ws://localhost:4000`.
- 서버 주소, 받은 메시지 전체, 설정 객체를 로그·알림 풍선에 쓰지 않는다. 에러는 예외 종류·종료 코드·에러 코드만 남긴다.
- 상태 표시줄에 주소를 보여 줄 때는 방송 화면에 잡힐 수 있으므로 호스트를 가린다(새로 넣을 때 제안 먼저).

## 8. 스타일
- 기존 파일의 관례를 따른다: 파일 범위 `namespace`, `_camelCase` 비공개 필드, PascalCase 상수·메서드, 4칸 들여쓰기, 줄바꿈 LF, `.cs`는 BOM 없음. Edit로 수정한다.
- 한 줄 `if`·`foreach`에 중괄호를 생략하는 기존 방식은 그대로 둔다.
- 주석은 한국어로, "왜 그렇게 했는지"가 코드만으로 드러나지 않을 때만 단다. 공개 형식·멤버에는 짧은 `/// <summary>`를 단다.
- 빈 `catch { }`는 이유를 주석으로 적은 곳에만 쓴다(예: `/* 이미 끊긴 경우 */`).

## 9. 보고에 더할 것
developer 보고 형식에 다음을 더한다.
- **흐름 대응표**: `메서드(파일:줄) → 명세 § → 확인 방법과 결과`.
- **사용자가 확인할 것**: 트레이 아이콘 색, 알림 풍선, 채팅 창처럼 화면으로만 확인할 수 있는 동작과 TestServer 명령 순서.
- **문서 반영 내용**: 명세 "구현 현황"에 넣을 줄(메인 스레드가 반영).
