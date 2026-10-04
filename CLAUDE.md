# CLAUDE.md

## Behavior

Think before acting. Read files before editing. Be concise in output, thorough in reasoning. Edit over rewrite. No redundant file reads. No sycophantic openers/closers. No question restating. No unsolicited suggestions or scope creep. No over-engineering. ASCII only in output. If unsure, say so. Never guess file paths. User instructions override this file.

## Layout

- `Game/` -- Unity project (open this folder in Unity Hub). See `Game/CLAUDE.md`.
- `Docs/` -- design docs and analysis notes.
- `Table/` -- Excel (`Excel/*.xml`) to JSON converter. `ConvertTable.bat` writes `Table/Json/` and `Game/Assets/Project/Resources/Table/`.
