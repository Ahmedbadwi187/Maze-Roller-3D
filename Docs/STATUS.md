# Maze Roller 3D — Build Status

Single source of truth for what's done, what's verified, and what's next. Updated as each
milestone progresses — read this first when resuming work on the project.

## Milestone table

| # | Milestone | Status |
|---|---|---|
| 1 | Maze generation algorithm + edit-mode unit tests | ✅ Done — 24/24 tests pass in Unity's real Test Runner (verified via headless `-runTests`) |
| 2 | Procedural 3D maze instantiation (geometry/camera/lighting) | ⏳ Geometry, camera framing, and materials independently verified correct; one headless-capture-only color artifact needs a live in-editor look (Docs/SETUP.md §9) |
| 3 | Ball prefab with tilt-based physics through a hard-coded test maze | ⏳ Built and physics-verified headlessly (ball settles on the floor at the correct radius, moves under force) — needs a live in-editor look to confirm tilt/joystick feel |
| 4 | Win condition detection + Level Complete overlay | ✅ Done — real PlayMode test (ball drops, physically enters exit trigger, overlay shows, ball control disables), passing |
| 5 | Level Select screen + persisted LevelProgressService | ✅ Done — 41/41 EditMode tests passing (repository difficulty curve + progress persistence), scene builds clean |
| 6 | Difficulty scaling + star/time tracking + save/load | ✅ Done — StarCalculator (size-scaled thresholds) + LevelFlowController now records real stars/time/unlock on completion; PlayMode test proves it end to end (drop ball → completes → progress persisted → next level unlocked) |
| 7 | Settings screen (control scheme, sound/music) | ✅ Done — Settings.unity with working sound/music/control-scheme toggles (persisted via new GameServices locator), Restore Purchases/Remove Ads stubbed pending milestone 8; 46 EditMode + 2 PlayMode tests all still passing |
| 8 | Ads (banner + interstitial every 3 levels + rewarded hints) + Remove Ads IAP | ⏳ Cadence/gating logic (AdsGate) done & unit-tested (5 tests); real Unity IAP wired for Remove Ads/Restore Purchases (testable via its Fake Store); ad *network* itself is a documented placeholder (NoOpAdsService) since Google Mobile Ads isn't a UPM package - see its doc comment; rewarded-ad hint system not yet built (spec marks it optional) |
| 9 | Polish (SFX, particles, app icon, splash, Cinemachine smoothing) | ✅ Done — Cinemachine follow camera (replaces milestone 3's plain script), particle burst + procedurally-synthesized chime on level complete, generated placeholder app icon (set in PlayerSettings), Splash scene (fade-in logo → Level Select); 51 EditMode + 2 PlayMode tests all still passing |

## Locked decisions (asked once, don't re-ask)

- **Art style**: clean minimalist — soft pastel palette, flat lighting, simple shapes.
- **Monetization**: banner on menu/level-select only (not in-game), interstitial every 3
  levels, rewarded video for hints, one-time Remove Ads IAP disables banner + interstitial.
- **Device support**: phone-only (no tablet layout guarantee).
- **Engine**: Unity 6000.0.32f1 (Unity 6 LTS) + URP.

## How verification works on this machine

Unity Editor (6000.0.32f1) is installed locally, so milestones are verified by driving it
**headlessly via command-line batch mode** — running real unit tests
(`-runTests -testPlatform EditMode`) and executing editor generator scripts
(`-executeMethod ...`) that build scenes/prefabs and, where useful, manually step the physics
simulation (`Physics.Simulate`) to prove things actually work, not just compile. This is why
milestones can be marked verified without you needing to click through the Editor yourself for
most of it.

**Important distinction**: `Physics.Simulate` from edit-time editor scripting steps real
physics (good enough for e.g. "does the ball settle on the floor"), but MonoBehaviour
lifecycle messages (`Awake`, `OnTriggerEnter`, etc.) only run in actual Play mode - so anything
depending on those (win-condition detection, UI reacting to events) is covered by real
**PlayMode tests** (`-runTests -testPlatform PlayMode`, see `Assets/Tests/PlayMode/`) instead,
which genuinely enter Play mode headlessly rather than faking it.

**Known quirk**: the very first headless run after adding a brand-new `.cs` file or a new/changed
`.asmdef` reference sometimes reports a bogus "type not found" compile error even though the
code is correct - Unity's incremental compile cache hasn't caught up with the new file/asmdef
yet. Immediately re-running the exact same command resolves it. If the same error survives a
second run, it's real.

**The one thing headless verification can't fully cover**: visual color fidelity. A
specific-to-headless-capture rendering artifact (documented in Docs/SETUP.md §9) means
screenshots taken this way render desaturated even when the underlying materials are
independently confirmed correct on disk. So the *only* thing still needing your eyes on a live
Editor session is: does it look right when you actually look at it. Structural correctness
(does the maze generate right, does the ball roll and collide correctly, does a scene wire up
without errors) is already verified without you.

## Project structure, setup steps, and full milestone 2/3 build details

See **Docs/SETUP.md** for: installing Unity, opening the project, running the tests yourself,
and step-by-step "what you should see" for each milestone's generator script.
