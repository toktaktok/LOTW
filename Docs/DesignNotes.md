# LOTW Design Research Notes (Notion "로비" workspace)

Read-only research, 2026-10-05. Source: Notion workspace "로비" (https://app.notion.com/p/2a9f5794bb8240d7aecdaf11ffd00136).
Google Drive: checked, no LOTW content found.
URL form: `https://app.notion.com/p/<id>`. Each fact is tagged with a source key `[KEY]`; the keys are listed in Sources at the end.

Databases:
- Design DB "데이터베이스" collection://249f5813-8036-8035-986e-000b61dd7c18 (columns 대분류 = 기획 / 규칙 / 플밍, plus 소분류)
- Residents "주민 목록" collection://4566115c-9735-49ba-8c11-f6546c96acf6
- Quest list collection://fd98495b-3dfe-4fca-a31a-922fb84300a4
- Meeting notes "회의록" collection://aec9fb7f-2535-4d75-9e4c-6d75f20e51c3

---

## Genre and design concept

- Title: 창밖을 보라 (also written 창 밖을 보라). Meaning: look out the window / look at Bora (the village name). [TITLE]
- Genre (exact): "창밖을 보라 는 추리게임의 형식을 차용한 어드벤처 게임입니다." [FAQ]
- Premise: the player is a detective investigating the village to find the truth about its missing treasure. Along the way he meets 지그 (Zig), who believes he lives inside a game. [FAQ]
- The title is "게임의 메타적요소를 암시하는 유희적인 제목". [FAQ]
- Key differentiator (exact): "게임 안에서 이루어지는 여러 사소한 행동들이 개별 '창' 안에서 은유적인 미니게임으로 나타난다". [FAQ]
- Minigame pillar (exact): "플레이어(탐정과 지그)가 돌아다니며 대화하고 단서를 얻는 행동은 미니게임으로 비유되어 나타난다." [MINI]
- Keywords: 세상을 의심하는, 미스터리한, 추리적인, 세상밖을 의식하는. [TITLE]
- Meta stance (exact): "지구를 알아보는것이 목적이 아니라 게임속 세계(마을)에 녹아드는것이 목적". [TITLE]
  - Earth and the village are separate worlds. Even if esc is pressed at the end, Earth only stops observing and nothing in the village changes.
  - This is a deliberate inversion of how meta-fiction games usually treat "Earth".
- Whole-game arc: the snowman travels in search of winter and solves problems. He looks cold but is warm, and like a season he eventually leaves. Zig senses the player's presence. [ARC]
- Graphics (exact): "2D와 3D를 융합한 2.5D 스타일의 그래픽으로, 3D 오브젝트에는 윤곽선을 렌더링합니다." [GFX]
- Scope target: a demo covering one chapter (Ch1). [FAQ]
- Setting: winter (confirmed). [MTG1028]

## Story, prologue and chapter flow (in order)

Background [STORY]:
- The snowman detective is already somewhat known. He came to 보라 마을 to rest.
- He received a secret letter from the mayor, who also helped him get an office on the main street.
- The office is on a back alley near the plaza shops, on the 2F of a multi-storey building above a laundry.

Case timeline [ALIBI]:
- Mon Dec 10, night: the treasure is stolen. The same night the 우주 꼬마들 break the cable car.
- Wed Dec 12: the theft is discovered.
- Sun Dec 16: Chapter 1 begins.

Ch1 (demo), from [STORY] unless tagged:
- Prologue (#1, Sun Dec 16, night)
  - A clean red bus arrives at night.
  - A figure in coat and hat carries bags up a long slope and reaches the building near dawn.
  - The mayor rushes into the 2F office and tells him: the theft was discovered Wed Dec 12; finding the culprit comes first; the culprit is definitely a resident.
  - The mayor leaves in a hurry.
- #2, 7am, office opens
  - He turns the 1F sign: "탐정 사무소 오픈. 도움이 필요하다면 언제든지 방문 환영." The mood is cold.
  - 검맨 suggests getting to know the residents' faces, as quest 101 "얼굴 트기" (tutorial). [QUEST]
  - Optional sub-quests: jump rope with 하늘이; collect flower-shop customer reviews.
  - Optional minigame: tidy the trash can by the vending machine.
  - Zig meeting 1: Zig finds the detective interesting and starts following him.
  - The detective learns that the museum is at the summit, the cable car is broken, and the bar is busy in the evening.
  - Fade to evening.
- #3, 8pm, bar
  - Zig meeting 2: Zig does odd jobs at the bar and they meet in front of it. Zig likes being spoken to without prejudice.
  - The bar is cheerful. Clue: the residents are not very close to each other.
  - The detective resolves resident requests until night.
  - Meeting notes add that the snowman meets Zig by chance at the bar at midnight, and that Zig is not pinned as the culprit before the bar. [MTG0531]
- #4, next day, culprit identification
  - Zig meeting 3 happens at a shop in the middle of the plaza. Zig says he will visit the office that night.
  - At the office Zig confesses: "저는… 게임 안에 갇혀있다구요!!" [ZIG]
  - At this point the detective is morally certain Zig is the culprit. Zig asks for help escaping.
  - After meeting 3 reveals the secret, the game goes straight to the ending branch with no notebook organizing. [MTG0531]
- Ch1 mini-ending
  - The detective holds off on naming the culprit and sets two questions: (1) is the confession true? (2) where is the treasure?
  - Alternative "허무맹랑" ending: a block reads "지그가 범인이라는 증거는 무엇일까?"; the residents believe the detective and Zig panics. [MTG0524]

Later chapters [STORY]:
- Ch2, 녹지 (greenery): a dance contest is being prepared; Zig gets a chance to shine.
- Ch3, 꼭대기 (summit): Zig becomes psychologically independent of the snowman. The dance-contest finale puts the detective and Zig on stage, while conflict grows between the upper class and immigrants.
- Ch4: 쓰레기장 (junkyard).
- Ch5: the treasure's secret is revealed, it is recovered, its correct use is learned, and the game ends.
- Ending meta point: Zig was just a game character. [MTG1028]

Lore [STORY]:
- The mayor's legend says the treasure fell from the sky (true) and that mishandling it ends the world (the mayor's embellishment).
- The museum is at the summit, reached by cable car or a long staircase. It cannot be entered in Ch1.
- Candidates for what the treasure is: a real esc key, an object from another dimension, or an observation device.
- What really happened [ALIBI]:
  - Zig dropped the treasure while carrying it down the stairs. It looks like a stone, so it was lost.
  - The museum is free to enter with no visitor log, and the guard was lax.
  - 거위 put his own artwork in the treasure's place on Tuesday.
  - Twist: the cable car broke before the theft, not after.
  - The page has a per-resident table of real Wednesday activity versus claimed alibi (some residents lie).

Demo scenario drafts [DEMO]:
- 지수 version: Ch0 intro (night bus), then Ch1, where the frilled-lizard mayor brings the first mission.
- 효선 version
  - At the desk the snowman reads his info and a tourist pamphlet (intro, map, shops).
  - Some entries show a "recorded in detective notebook" marker (마을 소개, 지도).
  - Visiting a nearby shop is a quest. Then the mayor gives the theft quest.
  - Collect alibis, get followed by Zig, go to the bar.
  - Next day the decisive evidence points to Zig, followed by his confession.
- 인수 version
  - Arrival by taxi.
  - Desk documents (ID "직업:사설탐정", rent date, recommended restaurant) are stored in the notebook with an alert. The notebook gains the quest "추천맛집 가보기".
  - The main quest comes from the mayor.
  - Chained sub-quests guide the player around the map, e.g. deliver 고슴이's note to 캔디맨.
  - Follow-up dialogue options: 대화한다 / 알리바이에 관해묻는다 / 보물에 관해묻는다.

## Characters

Notion has no IDs. The IDs below come from repo folders `Game/Assets/Project/Art/Characters/{NNN}.{Name}` (0xx player, 1xx town NPCs, 2xx other NPCs). Name matching is approximate.

| ID | Repo name | Notion name | Notes |
|---|---|---|---|
| 001 | Snowman | 눈사람 탐정 | Traits 메모 중독, 하드-보일드, 마이페이스. Writes everything down because melting makes him forget. Tools: notebook (classic cover, insert pages), magnifier, gun?, radio (temporary). Home near the North Pole. [SNOW] |
| 002 | Zig | 지그 | 25, bar cleaner, lives alone in a shabby house at the plaza edge. Traits 소외감, 시한폭탄, 모순. He is the treasure thief. [ZIG] |
| 101 | Gumman | 검맨 | Made of gum, owns land, friendly, friend of 미러거북. Gives the tutorial quest 101. [GUM] |
| 102 | BarPider | 마라카스 거미 (bar spider) | Alibi: worked at the bar all day, then went home. A cocktail minigame is planned for his alibi. [NOTE][MINI] |
| 106 | JumpRopeKid | 줄넘기하는 아이 / 하늘이 | Jump-rope sub-quest. [STORY] |
| 108 | Mayor | 목도리 도마뱀 촌장님 | "이 일은 비밀스럽게 이루어져야만 하네. 해낼 수 있겠나?" Greedy and vain; shady ties with 두꺼비. [MAYOR] |
| 112 | Ele | 꽃집 코끼리 | Flower shop; customer-review sub-quest. |
| 113 | FarmDog | 목장 달마시안 | |
| 116 | Goose | 거위 아저씨 | Swapped his artwork into the treasure's spot. [ALIBI] |
| 117 | GSDC | 고슴도치 (고슴이) | Note-delivery chain. [DEMO] |
| 118 | CandyMan | 캔디맨 | |
| 119 | Mirrortle | 미러거북 | |
| 124 | PlanetKids | 우주/행성 꼬마들 | Broke the cable car. Golden-bug device minigame. [ALIBI][MINI] |
| 125 | RabbitCouple | 토끼부부 | Sub-quest: talk to lonely elders. [QUEST] |
| 126 | Saus | 소시지 경비 | Lax museum guard. |
| 127 | Toad | 두꺼비 | |
| 104 | FruBat | 애기과일박쥐 | |
| 202 | Mouse | 어린쥐 | Trades info for candy. [MTG1028] |

- The full resident DB has 49 entries, grouped by area: 광장 / 중간 거주지 / 꼭대기 지역 / 녹지 / 쓰레기장.
- Other names include 케이블카 직원, 키위새 (likely 123 Wikiwiki), 꼭끼리, 짭지그 and 기린. [RES]

## Core play loop

Synthesized from [STORY] [QUEST] [NOTE] [DIALOG] [MINI] [MTG0617]:
1. Story beat or cutscene, then time advances (time is tied to story progress, not a clock). [DAY]
2. Explore the area (side-scroll). NPCs with a request show a gesture such as sweating. The cursor is a carrot that becomes a magnifier over interactables. [NOTE][MTG1028]
3. Talk to an NPC. The first meeting writes the name into the notebook. Choose chat / investigate (alibi) / ask about requests / leave. [DIALOG]
4. Investigating starts an alibi minigame (persuade/press as the snowman, natural conversation as Zig). Success yields an alibi or evidence entry in the notebook. [MINI]
5. Requests become sub-quests. Rewards include golden-bug device parts that unlock information. [MTG1028]
6. Open the notebook and organize: alibis per case, open questions, linked evidence, and fitting blocks into a chain of events. [NOTE][MTG0524]
7. Area gate: only key residents must be talked to. Once an area's notebook evidence is complete, the player can leave. [MTG0617]
8. The main beat advances (next time slot or next Zig meeting), then repeat.

Quest rules [QUEST]:
- Up to 4 active at once: 1 main plus up to 3 subs. The notebook page instead caps the quest list at 3.
- Each resident has only one quest at a time.
- One main quest per chapter. Subs are optional for the main story but required for the true ending. Ch1 subs double as wayfinding and tutorial.
- Moving to the next chapter locks the previous chapter's quests. Two options were considered: cancel in-progress quests, or a daily cap that advances time.
- Quest priority can change (sub to main or main to sub). [MTG0524]
- Quests come from the office or directly from residents, shown with a speech-bubble UI. [STORY]
- Completion conditions (one quest can need several): collect a target item, collect a target keyword, or satisfy a conversation. [STORY]
- Quest DB rows:
  - id 1, main, "사라진 마을의 보물"
  - id 101, sub, "얼굴 트기" (검맨, tutorial)
  - unnamed sub from 토끼부부
- Demo quest plan [MTG0524]: talk to every resident (the last one points to the bar), plus one costume sub-mission and one route sub-mission.
- Alibi questions lead into a chain of route sub-quests (a->b->c->a->d). [MTG0617]

## Detective notebook (수첩), clues and deduction

Source [NOTE] unless tagged.

Access and navigation:
- Open with the on-screen notebook button or the Q key.
- Mouse hover or arrow keys highlight an entry (colored or white box). Clicking jumps to that quest's page.
- If opened during dialogue, it should pop up already open with no animation. Behavior while roaming is still being discussed. [MTG1028]
- At night the chat window and notebook UI switch to dark with white text. [MTG0902]

Pages / tabs, in book order:
- 표지 (cover)
  - Shown on first open.
  - Holds the current quest title and upcoming goals.
  - Each quest's tasks appear as a post-it in its own color.
- 의뢰 목록 (quest list)
  - Current chapter only, with the main quest pinned at the top.
- 의뢰 상세 정보 (quest detail)
  - One-line title, progress %, completed objectives, evidence (alibis).
  - Has a UI for when new story progress is appended.
- 주민 프로필 (resident profiles)
  - Faces drawn by the snowman. [MTG0822]
  - On a first meeting, highlighted keywords combine into the name, with a written-into-notebook animation. [DIALOG]
- 알리바이 (alibi)
  - Separated by an index per case and written in order of last whereabouts.
  - Each alibi links to evidence in the quest list.
  - Example (orange text): "거미가 바에서 하루종일 일한 뒤 바로 집으로 퇴근했다. 기린이 바에 잠깐 들러 이야기를 했다. 낮에는 꽃집에 있다가 밤에 나와서 코끼리가 마을을 산책했다."
- 의문점 (open questions)
  - The village story tied to the treasure, and the theft case.
  - Questions to solve include what the treasure is and whether Zig is really trapped. [MTG1028]
- 맨 뒤 페이지 (last page)
  - Completed-quest post-its and a record of rewards.
- To-do checklist entries such as "바에 가 보기". [MTG0617]

How entries are acquired:
- Desk documents and the pamphlet at the start: 마을 소개, 지도, ID, recommended restaurant. They arrive with a "recorded in notebook" alert. [DEMO]
- First-meeting dialogue gives a resident's profile and name. [DIALOG]
- Asking about an alibi starts the alibi minigame; winning gives the alibi or evidence. [NOTE][MINI]
- Natural-conversation success as Zig gives a clue and raises a hidden relationship value. [MINI]
- Sub-quest rewards: golden-bug device parts, then a parts-assembly game for information. [MTG1028]
- The chat button gives one of three topics (treasure / village / immigrants). [MTG1028]
- Info trades, e.g. 어린쥐 gives info for candy. [MTG1028]

How entries are used (deduction):
- Alibi UI beside the NPC: a colored index marker at the start of each line. Alibis proven irrelevant get a strikethrough and sort to the bottom.
- Fit blocks together to complete a chain of events (reference images in the notes). [MTG0524]
- Confirmed goal (exact): "수첩이 추리 퍼즐로써의 역할을 할 수 있었으면 좋겠다." [MTG0531]
- Before the bar, surface evidence can be combined into "Zig is the culprit" once the notebook is organized. [MTG0531]
- Intentional flaw (exact): "보물 도난 사건에 대한 추리를 수첩에서 완성시킬 수 없게끔 만들어 둔다. 결함이 존재하도록" [MTG0524]
- The ending-branch block "지그가 범인이라는 증거는 무엇일까?" is the hook for the absurd ending. [MTG0524]
- Notebook art style: craft-paper texture, stable mid-tone colors, a paper-puzzle feel, shared with the snowman's minigames. [MINI]

## Dialogue [DIALOG]

- Default one-liners: 5 per chapter, played in order by how many times the player has talked to that NPC.
- When the NPC has a request, the expression and line change (2-1).
- Quest dialogue is stored per quest id. Story and cutscene lines are stored in order.
- Choices:
  - 사담을 나눈다
  - 조사할 것이... (only once a quest is active)
  - 부탁할 것이 있는지?
  - 대화를 끝낸다
- Variant from [NOTE]: ask about alibi (alibi minigame) / ask about worries (sub-mission) / chat.

## Minigames [MINI]

- Shown in popups separate from the main screen. They also visualize Zig's problems.
- Planned layout: the in-game view shifts left; the snowman's face, the target and the minigame window are on screen; dialogue has a time limit and choices. [MTG1028]
- Snowman style: static, refined, craft paper. The player chooses statements to persuade or press and wins evidence or alibis.
- Zig style: unstable and distorted.
- Types:
  - persuade/press: main for the snowman, special case for Zig
  - natural-conversation clue finding: main for Zig, special case for the snowman
  - simple reward: both
- Ideas:
  - pipe rotation (golden-bug device)
  - slide puzzle
  - cocktail guessing (spider's alibi)
  - maze, minesweeper, 1-minute pingpong, tetris
  - fishing, stepping stones, dominoes
  - watering a flower, trash can, vending roulette, jump rope
- Main minigame: the space kids' golden-bug game. [MINI]
- Demo set: golden-bug device (starts the main mission), a bar game where you infer customer traits, slide puzzle or pingpong. [MTG0902]

## HUD

No dedicated HUD page exists. Gathered elements:
- Notebook button (graphic needed). [NOTE][MTG0822]
- Quest list, max 3 entries, marked "?". [NOTE]
- Speech-bubble quest indicator over NPCs; sweating gesture for NPCs who have a quest. [STORY][NOTE]
- Chat window: dark at night. [MTG0902]
- Cursor: carrot, magnifier when hovering an interactable. [MTG1028]
- "Recorded in notebook" alert toast. [DEMO]

## Title / main menu [TITLESCR]

- Initial screen, or after saving at the office: a briefcase in the office.
- Opening it shows 이어하기 / 새로하기 / 설정, each a sheet of paper.
- With keyboard navigation, the selected sheet rises slightly.
- The menu scene follows the last save location: cafe = drinking coffee; outside the village = on a train.

## Save

- No dedicated page. The save location drives the title scene (see above). [TITLESCR]
- Meta use: Zig pretends to recognize save/load, e.g. "이 카페에 온 게 처음이 아니지?", and pretends to know what you did before loading. [MTG0129][STORY]
- Implies: the save stores location/scene plus a load counter or flag that Zig's lines can react to.

## Flags / progress

- No dedicated page. Implied state:
  - chapter and time slot (time advances with story; chapter length not fixed) [DAY]
  - quest states per quest id, objective progress %, and the per-chapter lockout [QUEST][NOTE]
  - per-NPC talk count (drives the 5 one-liners) and first-meeting flag [DIALOG]
  - notebook entries: profiles, alibis per case, open questions, struck-through alibis [NOTE]
  - Zig meeting index (1-3) and hidden Zig relationship value [STORY][MINI]
  - golden-bug parts [MTG1028]
  - area-complete gates [MTG0617]
  - sub-quest completion count toward the true ending [QUEST]

## UI style constraints

From design:
- Notebook and snowman minigames: craft-paper texture, stable mid-tone colors, a paper-puzzle feel. [MINI]
- Zig UI: unstable and distorted. [MINI]
- Palette notes: objects dark warm brown, light yellow-to-yellow-green, air dark blue. [MTG1028]
- Night mode: dark panels with white text. [MTG0902]
- Repo: low-res RT integer upscale, PPU 25, Galmuri11 font (project memory).

User's global frontend rules (these override Notion):
- no purple hues
- no cream or off-white backgrounds
- no italic accent words in headlines
- no numbered "01/02/03" section labels
- no monospace labels
- no pill-shaped buttons

Note: craft paper must be a mid-tone kraft brown, not cream. The Notion icon and some color notes use purple; do not carry it over.

References [BOOK]:
- Unity pixel render; 3D modeling + cartoon + pixel render; 3D depth pixel games
- Lumino City miniature look
- lospec palettes, QuickSprites 3D
- Title-mood references: Undertale, Over the Garden Wall, Night in the Woods, Rain World

## Open questions and contradictions

- Theft date:
  - Mon Dec 10 night [ALIBI]
  - "4 days before arrival" [STORY]
  - "about 10 days ago" [MTG0129]
  - "discovered 3 days ago" (사흘 전) [DEMO 효선]
  - Recommend the [ALIBI] timeline: stolen Dec 10, discovered Dec 12, Ch1 on Dec 16.
- First mission giver: the mayor (prologue) [STORY] vs 검맨 (quest 101 at 7am) vs the mayor after a shop visit [DEMO]. Likely resolution: the mayor gives the main quest and 검맨 gives the tutorial sub.
- Arrival: bus [STORY][DEMO 지수] vs taxi [DEMO 인수].
- Zig meeting 3 location: plaza shop [STORY] vs "second morning, outside, then office" [MTG0524] vs the midnight bar meeting [MTG0531] (probably meeting 2).
- The notebook is meant to be a deduction puzzle [MTG0531], yet the theft deduction is intentionally impossible to complete [MTG0524]. Both can hold: Zig's guilt is completable, the full case is not.
- Active quest cap: 4 (main + 3) [QUEST] vs 3 [NOTE].
- The chapter-transition handling of unfinished quests is undecided [QUEST].
- Day/night is vague: tied to story, but a "daily quest cap" option also exists. [DAY][QUEST]
- Dialogue choice sets differ between [DIALOG] and [NOTE].
- Notebook open animation while roaming is undecided [MTG1028].
- What the treasure is remains undecided (esc key / other-dimension object / observation device). [STORY]
- HUD, save and flags have no spec pages; everything above is inferred.

## Sources

| Key | Page | URL |
|---|---|---|
| FAQ | 면접 예상 질문 | https://app.notion.com/p/00ca1f81bc5f452c95e373e61c667a27 |
| TITLE | 제목 | https://app.notion.com/p/1e06194045be4bf0b0a57f4bc8a8391f |
| ARC | 게임 전체 서사 구조 | https://app.notion.com/p/60e738a775c6417498c07d2d0a8ea645 |
| GFX | 그래픽 | https://app.notion.com/p/dcd1e41f458c4085b9e3318e543424e8 |
| STORY | 전체 스토리 | https://app.notion.com/p/c4bcd97f635544719ebb36e39da79835 |
| DEMO | 데모 시나리오(초기) | https://app.notion.com/p/6b42698f1ca5436a8c74e9d1a2e60c6f |
| ALIBI | 보물 도난 사건(알리바이) | https://app.notion.com/p/fe0b722e287e46aabf3d089fc978e8b8 |
| NOTE | 수첩 | https://app.notion.com/p/f4997fdd0e4646b3855fa3c13049377b |
| DIALOG | 대화 | https://app.notion.com/p/93fc1895dd824f75926718794a6c82fe |
| QUEST | 의뢰 | https://app.notion.com/p/c3e01e367c154f44b7afe1548e4011c6 |
| DAY | 낮/밤 시간 | https://app.notion.com/p/ed749aa39cd34508bc27a59b045e0435 |
| TITLESCR | 타이틀 화면 | https://app.notion.com/p/c79c40ae8e6b45aeb2c16412625548ec |
| MINI | 미니 게임 | https://app.notion.com/p/69d82a26335a42128f7746026b57b782 |
| SNOW | 눈사람 탐정 | https://app.notion.com/p/9e4806c1ce424c8a83ee1b77e50713cf |
| ZIG | 지그 | https://app.notion.com/p/53c98df7cf764f99924ffa0bd85f5993 |
| MAYOR | 목도리 도마뱀 촌장님 | https://app.notion.com/p/851f27ddf8c44a84b795950d9151426a |
| GUM | 검맨 | https://app.notion.com/p/f3f1947c44ad43278bb5664ee696dc7f |
| RES | 주민 목록 (DB) | collection://4566115c-9735-49ba-8c11-f6546c96acf6 |
| BOOK | 북마크 | https://app.notion.com/p/269774b779b240ebbbbe2629722115f6 |
| MTG0129 | 회의록 22/01/29 | https://app.notion.com/p/a9e19a2ff4854726b368ecf407531dfa |
| MTG0524 | 회의록 22/05/24 | https://app.notion.com/p/6ce7d58b8db5417fa36a578d702846c6 |
| MTG0531 | 회의록 22/05/31 | https://app.notion.com/p/632345f5c3254e028c722ac8d062574c |
| MTG0617 | 회의록 22/06/17 톡 | https://app.notion.com/p/f8706b3f8a064959802aa1cd58e0cf79 |
| MTG0822 | 회의록 22/08/22 | https://app.notion.com/p/9151e8b9ed9641478642bbb498c72468 |
| MTG0902 | 회의록 22/09/02 | https://app.notion.com/p/c34aff6e314a48689c0334249cba31e2 |
| MTG1028 | 회의록 22/10/28 톡 | https://app.notion.com/p/13bdcb7d1948439d9ff6f99b17848f48 |
