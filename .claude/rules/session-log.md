# LOTW Session Log

Every session records its work in the repo, and summaries are shown in time order. All log text and summaries follow the writing rules below (ASD-STE100 Simplified Technical English, applied to Korean).

## Location
- One file per session: `Docs/SessionLogs/{YYYY-MM-DD}_{session-slug}.md` (`2026-10-05_repo-structure.md`).
- `session-slug`: lowercase, `-` between words, from the session topic. Keep the same file for the whole session; a session that continues on a later day keeps its first file.
- One file per session, so parallel sessions in different worktrees do not conflict on merge.

## When to write
- Add an entry at the end of each turn that changed files, made a commit, merged, or found a blocking problem.
- Add nothing for turns that only answer a question.
- Write the entry before the final report to the user. Commit it with the work it describes; do not make a separate commit just for the log.

## Entry format
```markdown
## 2026-10-05 12:30 | main
- 작업: 대화 편집기에 조건/실행 목록 UI를 추가했다.
- 결과: 커밋 bdd6472. Python 테스트 11개가 통과했다.
- 확인 안 함: Unity EditMode 테스트. 에디터가 닫혀 있었다.
- 다음: 에디터에서 EditMode 테스트를 실행한다.
```
- Heading: local date and time (24h), then the branch.
- `작업`: what changed. `결과`: commits, test counts, files. `확인 안 함`: what was not verified and why. `다음`: the next action, or `없음`.
- Omit `확인 안 함` when everything was verified. Use one line per fact; add lines instead of long sentences.
- File header: `# {session topic}` once at the top, then entries oldest first.

## Showing summaries
When the user asks for progress ("진행 상황", "로그 보여줘", "요약"):
1. Read every file in `Docs/SessionLogs/` (on main, plus the worktrees from `git worktree list` for unmerged sessions).
2. Merge all entries and sort them by date and time, oldest first.
3. Show one block per entry: time, session, branch, then `작업` and `결과` in one or two sentences. Show `확인 안 함` and `다음` items as a list at the end.
4. Do not invent entries. If a session has no log file, say so.

## Writing rules (ASD-STE100, applied to Korean)
- One sentence, one fact or one instruction.
- Sentence length: instructions 20 words (어절) or fewer, descriptions 25 or fewer.
- Use the active voice and name the subject when it is not clear (`테스트가 실패했다`, not `실패가 확인되었다`).
- Write instructions in the imperative, one action per step, in the order of work.
- One term for one thing. Do not change a term in the same text (`세션` is always `세션`, not `스레드` in the next line).
- Use exact values and names: file paths, commit hashes, counts. Do not use vague words (`적절히`, `등`, `여러`, `아마`, `거의`).
- Do not stack three or more nouns without a particle (`테이블 검증 규칙 오류 목록` -> `테이블 검증에서 나온 오류 목록`).
- Put a warning before the step it applies to, not after.
- Keep a paragraph to one topic and six sentences or fewer.
- Keep code, commands, and file names in their original form.
