---
name: controller-status
description: VishasController 상태 보고 때 visha-common:status에 더하는 항목. VishaSynapse 통신 규약의 새 판 여부와 명세 "구현 현황"의 확인 대기를 본다. 읽기만 한다.
---

# VishasController 상태 보고 보충

`visha-common:status` 보고에 다음 줄을 더한다.

| 항목 | 확인 방법 |
|---|---|
| Synapse 규약 | `gh api "repos/Vishamos/VishaSynapse/commits?path=docs/protocol/README.md&per_page=5"` → 각 명세 머리말 "참조 규약"의 커밋 뒤에 새 커밋이 있으면 "규약 새 판 있음: <해시>, 영향 확인 필요" |
| 구현 현황 | `docs/tech_spec/*.md` 끝의 "구현 현황"에서 "확인 대기"인 줄 수와 항목 |
