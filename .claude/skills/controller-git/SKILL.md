---
name: controller-git
description: VishasController에서 커밋할 때 visha-common:git-commit에 더하는 항목과 security 실행 조건. 메인 스레드·developer가 커밋할 때, 메인 스레드가 security를 부를지 판단할 때 사용한다.
---

# VishasController 커밋 보충

커밋 절차·메시지 형식·금지 명령은 `visha-common:git-commit`을 따른다. 이 스킬은 이 프로젝트에만 해당하는 것을 더한다.

## 커밋
- 작성 에이전트: `[VishasController-<에이전트>]` (예: `[VishasController-Main]`, `[VishasController-Developer]`)
- 커밋하지 않는 것: `bin/`, `obj/`, `.vs/`(이미 git 제외), `%LOCALAPPDATA%\VishaSTD\`·`%AppData%\VishasController\`의 파일(저장소 밖)

## security 실행 조건
변경이 다음 중 하나에 해당하면 메인 스레드가 `visha-common:security` 에이전트를 부른다.
- 메인 스레드가 커밋할 때: 커밋 전에 부른다.
- developer가 커밋했을 때: developer는 security를 부를 수 없으므로, 메인 스레드가 완료 보고를 받은 뒤 push 전에 부른다.

| 종류 | 대상 |
|---|---|
| 파일 | `AppSettings.cs`, `CLAUDE.md`(안전 규칙 절), 이 스킬 파일(security 실행 조건 절), `.claude/settings.json`, `.gitignore`, `*.csproj`(패키지 참조) |
| 내용 | 새 외부 통신(새 소켓·HTTP 요청, 이모지 이미지 내려받기 등), 외부 프로그램·파일 실행(`Process.Start`), 받은 메시지나 설정 객체를 통째로 로그·화면에 쓰는 코드 |
