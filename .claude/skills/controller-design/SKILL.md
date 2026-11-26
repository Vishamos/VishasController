---
name: controller-design
description: VishasController 기획·명세 작업 때 visha-common:design-work에 더하는 항목. 읽을 노션 페이지 범위와 검토 때 함께 볼 것(송출 PC 부담, 방송 화면 노출, 서버 쪽 결정 필요 여부)을 다룬다.
---

# VishasController 기획 보충

절차는 `visha-common:design-work`를 따른다. 이 스킬은 이 프로젝트에만 해당하는 것을 더한다.

## 노션
- 기획 원본: `VishaSTD / VishasController`. 사용자가 지정한 페이지·블록만 읽는다.
- 비교용으로만 읽는 곳: `VishaSTD / VishaSynapse`의 컨트롤러 관련 항목(시그니처 41, 치지직 메시지, 관리 기능, 클라이언트 상태 전달), `VishaSTD / Vishamos`의 컨트롤러 관련 항목(장면 목록 `[Vishamos-Space]` 등).
- 2026-11-26 기준 기획 태그가 없다. 태그가 생기면 명세 `근거`와 worklist `기획:`을 태그로 바꾼다.

## 검토 때 함께 볼 것
- **송출 PC 부담:** 상시 다시 그리기·짧은 주기 폴링이 생기는지. 상태는 이벤트로 받는다.
- **방송 화면 노출:** 창·알림이 방송에 잡힐 수 있다. 서버 주소·개인 정보가 화면에 나오지 않는지, 알림을 끄는 방법이 있는지.
- **서버 쪽 결정 필요 여부:** 새 메시지(보내기·받기)가 필요하면 VishaSynapse 규약이 먼저다. 컨트롤러 명세에 형식을 정하지 않고, `visha-common:git-issue`로 VishaSynapse에 제안 이슈를 올린 뒤 worklist 항목에 `(보류 대기: …)`와 이슈 번호를 단다.
- **실행 위치:** 프로그램을 실행·종료하는 기능은 컨트롤러와 같은 PC에서만 된다. 다른 PC·iPhone에서 도는 프로그램은 상태 표시만 한다.
