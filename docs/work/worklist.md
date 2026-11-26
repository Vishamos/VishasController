# 작업 목록

## 현재 진행 (갱신: 2026-11-26 10:30)
- 진행 중: 없음 (visha-common 플러그인 작업 흐름 도입 — Main)
- 마지막 완료: 1단계 구현 b2943ea (치지직 채팅 수신 트레이 앱과 테스트 서버)
- 대기:
  - (사용자) 1단계 동작 확인: TestServer로 아이콘 색·알림·채팅 창·재접속 확인 (시작자료 §6)
  - (VishaSynapse) 시그니처 41·치지직 메시지 서버 구현 (VishaSynapse #9) → 끝나면 실제 서버로 확인
  - (사용자) push
- 다음: Spec-1 (시작자료 §4∼6 → 명세 01) → Test-1 여부 결정

## 운영 방식
- 형식과 규칙은 `visha-common:design-work` 5절. 끝난 항목은 명세 "구현 현황"에 반영하고 지운다. 명세 밖 항목은 커밋 메시지(작업 번호 포함)로만 남는다.
- 근거 기획이 없으면 `기획: 없음(AI 발견)` 또는 `기획: 없음(사용자 요청)`.
- 영역: Doc(문서), Spec(명세), Net(서버 접속), UI(트레이·창·알림), Test(테스트). 다음 번호: Doc-1, Spec-2, Net-1, UI-1, Test-2.

## Doc. 문서

## Spec. 명세
- [ ] Spec-1 [P1] `VishasController_시작자료.md` §4∼6을 `docs/tech_spec/01_chat_receive.md`로 옮김(참조 규약 `Vishamos/VishaSynapse:docs/protocol/README.md @59d27fe` §5a, 1단계 구현 동작을 항목 표로, 구현 현황은 b2943ea). 시작 자료의 §1∼3은 배경으로 남기거나 plan_review로 — 기획: 없음(AI 발견) / 명세: 01

## Net. 서버 접속

## UI. 트레이·창·알림

## Test. 테스트
- [ ] Test-1 [P2] 테스트 프로젝트 `tests/VishasController.Tests` 추가(xUnit 또는 MSTest, NuGet 승인 필요)와 `MessageParser` 경계값 테스트. 로직이 늘어나는 2단계 시작 때 — 기획: 없음(AI 발견) / 명세: 01 (보류 대기: 테스트 프로젝트 도입 시점)

## 올린 이슈
