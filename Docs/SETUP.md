# Roll & Escape — Full Setup & How-To Guide

This document is the complete, step-by-step guide to getting this repository running in
Unity, verifying what's built so far, and understanding how the project is organized so you
can keep building milestone by milestone. Read this once end-to-end before opening Unity.

---

## 0. What already exists vs. what needs the Unity Editor

No Unity Editor is installed on the machine this project was scaffolded on, so work so far
splits into two categories:

| Category | Status | How it was verified |
|---|---|---|
| Maze generation algorithm (`Assets/Scripts/MazeGeneration/`) | **Done, milestone 1 complete** | 24 NUnit edit-mode tests, all passing — run both standalone via `dotnet test` (done already, see §5) and inside Unity's Test Runner (you should re-confirm, see §4) |
| 3D scene/prefab/lighting/camera work (milestones 2+) | **Not started** | Requires the Unity Editor to actually build scenes and prefabs — this guide's §6 covers how that will be delivered (editor generator scripts you run yourself) |

You will need Unity installed to do anything past milestone 1 — install it now (§1) even if
you just want to confirm milestone 1's result in-editor.

---

## 1. Install Unity

1. Install **Unity Hub**: https://unity.com/download
2. In Unity Hub → **Installs** → **Install Editor**, install **Unity 6000.0.32f1** (Unity 6 LTS).
   - If that exact patch isn't offered anymore, install the newest available **6000.0.x LTS**
     — when you open the project, Unity Hub will detect the version mismatch in
     `ProjectSettings/ProjectVersion.txt` and offer to open with your installed version instead.
     Accept that; do not hand-edit the version file.
3. During install, check these **modules** (you can add them later via Hub → Installs → the
   Editor's gear icon → Add Modules if you skip this now):
   - ✅ **Android Build Support** (+ Android SDK & NDK Tools, OpenJDK — sub-items under it)
   - ✅ **iOS Build Support** (building/archiving an .ipa still requires a Mac + Xcode later;
     this module lets you build the Xcode project from any OS but you'll need Xcode itself
     on a Mac to produce the final .ipa)

---

## 2. Open the project

1. Unity Hub → **Projects** → **Add** → **Add project from disk** → select the
   `RollAndEscape/` folder (the one containing `Assets/`, `Packages/`, `ProjectSettings/`).
2. Click the project to open it. **First open will take a few minutes** — Unity needs to:
   - Import every package listed in `Packages/manifest.json` (URP, Input System, Cinemachine,
     Newtonsoft Json, Unity IAP, TextMeshPro, Test Framework)
   - Compile the two script assemblies under `Assets/Scripts/MazeGeneration/` and
     `Assets/Tests/EditMode/MazeGeneration/`
   - Generate the rest of `ProjectSettings/` (only `ProjectVersion.txt` was hand-authored;
     Unity fills in everything else — Graphics, Physics, Tags & Layers, etc. — with defaults
     the first time it opens the project)
3. If a **TextMeshPro "Import TMP Essentials"** prompt appears, click **Import TMP Essentials**
   (needed before any UI milestone that uses TMP text).
4. If Package Manager reports it can't resolve an exact version pinned in `manifest.json`
   (version numbers can go stale as Unity ships updates), let it resolve the closest
   compatible version automatically — that's expected and fine.

---

## 3. Configure URP as the active render pipeline

Because this project folder was hand-scaffolded rather than created from Unity's "3D URP"
template, URP is installed as a package but not yet wired up as the active pipeline. Do this
once, in-editor:

1. **Create the pipeline asset**: in the Project window, right-click inside
   `Assets/Settings/` → **Create → Rendering → URP Asset (with Universal Renderer)**. Name it
   `RollAndEscape_URP`.
2. **Assign it globally**: **Edit → Project Settings → Graphics** → drag `RollAndEscape_URP`
   into the **Scriptable Render Pipeline Settings** slot.
3. **Assign it per quality level**: **Edit → Project Settings → Quality** → for each quality
   level your target devices use (at minimum the default/Medium tier), set **Render Pipeline
   Asset** to `RollAndEscape_URP` too — Graphics Settings alone doesn't cover every level.
4. Mobile-friendly defaults worth setting on the URP asset now (Inspector on
   `RollAndEscape_URP`): disable HDR, set MSAA to 2x or off, keep Shadow Distance modest
   (~30-50) — we'll tune this for real once milestone 2's lighting is in place.

---

## 4. Run the milestone 1 unit tests inside Unity

1. **Window → General → Test Runner**.
2. Click the **EditMode** tab.
3. You should see `RollAndEscape.MazeGeneration.Tests` → `RecursiveBacktrackerMazeGeneratorTests`
   with 24 test cases (dimension checks, connectivity, perfect-maze/single-path proof,
   solution-path BFS, determinism by seed, custom entrance/exit, wall-symmetry, and
   invalid-input guards).
4. Click **Run All**. Expect **24/24 green** — this mirrors the standalone run already done
   outside Unity (§5), so a failure here would point at an environment/package problem
   (e.g. Test Framework package not resolved) rather than the algorithm itself.

---

## 5. (Reference) How milestone 1 was verified without Unity installed

Because no Unity Editor was available when this code was written, correctness was proven
with a **throwaway, gitignored `dotnet test` project** that compiles the exact same source
files (`Assets/Scripts/MazeGeneration/*.cs` and
`Assets/Tests/EditMode/MazeGeneration/*.cs`) against real NUnit, outside of Unity entirely.
This is not part of the shipped project (it lives outside `RollAndEscape/` entirely, in a
scratch folder) — it's included here purely so you know *why* milestone 1 is trusted as done
before you've even opened Unity:

```
dotnet test
...
Passed!  - Failed: 0, Passed: 24, Skipped: 0, Total: 24
```

The Unity Test Framework's EditMode runner (§4) is built on the same NUnit engine, which is
exactly why the two are expected to agree.

---

## 6. What "done" looks like for milestones 2+, and how they'll be delivered

Milestones 2 onward (3D maze instantiation, ball physics, win detection, UI screens,
save/progress, settings, ads/IAP, polish) all require scenes, prefabs, materials, and
lighting — things that can't be hand-authored reliably as raw `.unity`/`.prefab` YAML without
a live Editor to validate them. Instead, each of those milestones will ship as:

- Plain C# gameplay/service scripts (as already done for `MazeGeneration`), plus
- **Editor-time generator scripts** — `[MenuItem]`-driven tools under `Assets/Editor/` that
  build the actual scene/prefab/material objects via the Editor's scripting API
  (`GameObject.CreatePrimitive`, `PrefabUtility`, `AssetDatabase`, etc.) when you run them
  from a Unity menu, plus
- Exact steps in this guide for the menu command to run and what you should see in the Game
  view afterward (this is the closest substitute for a screenshot review loop, since nothing
  here can open the Editor to capture one for you).

You'll need Unity open to run each generator and confirm the result before we move to the
next milestone — that hand-off point will be called out explicitly each time.

---

## 7. Milestone status

Moved to **Docs/STATUS.md** — the single up-to-date status doc (milestone table, locked
decisions, what's verified and how). This file stays focused on setup/how-to steps.

---

## 8. Project structure reference

```
RollAndEscape/
  Assets/
    Scripts/
      MazeGeneration/   pure C#, no UnityEngine dependency — RecursiveBacktrackerMazeGenerator,
                        MazeModel, MazeCell (milestone 1, done)
      Gameplay/         BallController, TiltInputHandler, JoystickInputHandler,
                        LevelExitTrigger, HazardController (milestones 3-4)
      Levels/           LevelRepository, LevelProgressService, StarCalculator (milestones 5-6)
      Monetization/     AdsService, IAPService (milestone 8)
      UI/               per-screen controllers: MainMenuUI, LevelSelectUI, PauseUI, etc.
      Persistence/      SaveSystem (Newtonsoft.Json + local file)
      Core/             GameServices static locator wiring the above together
      Editor/           in-editor scene/prefab generator tools (milestone 2+, see §6)
    Prefabs/            Ball, WallSegment, FloorTile, ExitTrigger, Hazards, UI
    Scenes/             Splash, MainMenu, LevelSelect, Game, Settings
    Materials/, Audio/, Art/
    Tests/
      EditMode/         fast, no-Editor-window-needed tests (maze generation lives here)
      PlayMode/         reserved for tests that need a running scene (ball physics, etc.)
  Packages/manifest.json   package dependencies (URP, Input System, Cinemachine, Newtonsoft
                           Json, Unity IAP, TextMeshPro, Test Framework)
  ProjectSettings/         Unity project configuration (mostly auto-generated on first open)
  Docs/                    this guide
```

## 9. Milestone 2: build & confirm the maze preview

Milestone 2's geometry, materials, lighting, and camera framing are all produced by one
Editor menu command rather than a hand-authored scene, so nothing is committed as a raw
`.unity`/`.prefab` file you'd need Unity just to inspect for typos - running it *is* how
the scene comes to exist.

1. With the project open, in the Unity menu bar click
   **Roll & Escape → Milestone 2 - Build Maze Preview Scene**.
2. This will, in order:
   - Create four materials under `Assets/Materials/` (`Floor`, `Wall`, `EntranceMarker`,
     `ExitMarker`) using the clean-minimalist palette (soft neutral floor, soft blue walls,
     pastel green entrance marker, pastel gold exit marker), on the URP Lit shader.
   - Create three prefabs under `Assets/Prefabs/Maze/` (`FloorTile`, `WallSegment` - both
     plain cubes with a collider kept, ready for milestone 3's ball physics; `EntranceMarker`
     and `ExitMarker` - thin, collider-less discs) plus reuse `WallSegment` for all four wall
     orientations by rotating/scaling it per spawn rather than needing four prefabs.
   - Create (or reopen) `Assets/Scenes/Game.unity`, add a soft directional light + flat
     ambient lighting (no skybox reflections), and add a `MazeRoot` object running the new
     `MazeView3D` component.
   - Generate an 8×8 test maze (seed 1) via `RecursiveBacktrackerMazeGenerator` **immediately,
     at edit time** - no Play button needed - so the finished maze is visible the moment the
     command finishes.
   - Frame a camera at a ~52° tilted 3/4 overview angle over the whole grid via the new
     `MazeCameraFramer` component, and save the scene.
3. **What you should see**: the Scene/Game view frames a complete 8×8 maze from a raised,
   tilted angle - a grid of light neutral floor tiles bounded by soft blue walls with no gaps
   or doubled-up wall segments, a green marker on the entrance cell (bottom-left, column 0/row
   0), and a gold marker on the exit cell (top-right corner). Flat, soft-shadowed lighting, no
   harsh reflections - that's the "clean minimalist" look this milestone is judging.
4. The command is safe to re-run: it reuses existing materials/prefabs (checks
   `AssetDatabase.LoadAssetAtPath` first) and only rebuilds the maze instance and camera
   framing each time.
5. **This is the hand-off point** - confirm the visual style/lighting/camera angle look
   right (or tell me what to adjust - wall height, palette, tilt angle, cell size are all
   single fields in `Milestone2_MazeSceneBuilder.cs` / `MazeCameraFramer.cs`) before milestone
   3 (ball physics) builds on top of this scene.

## 10. Milestone 3: build & confirm the ball test scene

1. Menu bar → **Roll & Escape → Milestone 3 - Build Ball Test Scene**. This rebuilds a fresh
   Milestone 2 maze first, then adds:
   - A coral-colored ball (Rigidbody + SphereCollider + a low-friction `BallPhysics`
     PhysicsMaterial) at the maze entrance, just above the floor.
   - `TiltInputHandler` (device accelerometer via the New Input System) and
     `JoystickInputHandler` (on-screen drag) both attached to the ball, switched between by
     `PlayerInputRouter` (defaults to Tilt - the primary control scheme per spec).
   - An on-screen joystick (bottom-left, semi-transparent circle) wired to the joystick
     handler, visible whichever scheme is active (Settings, milestone 7, will hide/show it).
   - The Main Camera's static Milestone-2 framing is replaced with `BallFollowCamera`, which
     smoothly tracks the ball from the same tilted angle.
2. **What you should see**: the ball resting on the floor at the maze entrance. Press **Play**:
   - On a real device (or via **Window → Analysis → Input Debugger** to simulate accelerometer
     values in-editor): tilting moves the ball, and the camera follows it smoothly.
   - In the Editor Game view with just a mouse: click-drag the on-screen joystick circle -
     the ball should roll in the dragged direction, same as tilt would.
   - The ball should roll and collide solidly against walls (no falling through floors, no
     clipping through walls) - the floor/wall colliders are the same ones from milestone 2.
3. **Already verified without Unity's UI**: a **Roll & Escape → Milestone 3 - Simulate
   Physics Sanity Check** menu command manually steps the physics simulation (no Play mode
   needed) and confirms the ball settles to rest at exactly its own radius above the floor
   (not clipped through it) and moves under an applied force the same way tilt/joystick input
   would drive it - so the physics fundamentals are already known-good going into your check;
   what's left to confirm live is mainly *feel* (does tilt/joystick control response feel
   right) and *visual* correctness (same headless-color-capture caveat as milestone 2, §9).
4. Safe to re-run any time - rebuilds the maze and ball fresh, reuses existing prefabs/materials.

## 11. Milestone 4: build & confirm win detection

1. Menu bar → **Roll & Escape → Milestone 4 - Build Win Condition Test Scene**. This rebuilds
   a fresh Milestone 3 scene, then adds an invisible trigger volume at the maze exit
   (`Assets/Prefabs/ExitTrigger.prefab`) and a Level Complete overlay (dark scrim, time text,
   Replay + Next Level buttons - `Assets/Prefabs/UI/LevelCompleteUI.prefab`), wired together by
   a `LevelFlowController`.
2. **What you should see on Play**: roll/tilt the ball to the exit cell (top-right corner of
   the 8×8 maze) - on entering it, the overlay should darken the screen, show the elapsed
   time and star count, and the ball should stop responding to input. **Replay** reloads the
   scene fresh; **Next Level** (as of milestone 6) actually advances to the next level's
   size/seed via `LevelRepository`, not just a restart.
3. **Already verified without you clicking through it**: a real **PlayMode test**
   (`Assets/Tests/PlayMode/LevelFlowPlayModeTests.cs`) drops a ball onto a trigger in an
   isolated test scene and confirms the overlay appears, ball control disables, and (a second
   test) that completing a selected level actually records stars/time/unlock to the save file
   - so the win-condition *and* progress-recording logic are known-good; what's left to
   confirm live is navigating the actual maze to the exit and the overlay's visual look (same
   headless-color-capture caveat as milestones 2-3).

## 12. Milestones 5-9: level select, settings, ads/IAP, and polish

These milestones' generator scripts all chain from one another (each rebuilds on top of the
previous), so a single menu command gets you the fully current state of everything:

1. Menu bar → **Roll & Escape → Milestone 9 - Build Polish**. This rebuilds Settings (which
   rebuilds Level Select, which rebuilds Game) and then layers on the milestone 9 polish -
   so one command produces all four scenes (`Splash`, `LevelSelect`, `Game`, `Settings`) fully
   wired and current. Use the more specific `Milestone 5/7` commands instead if you only want
   to rebuild up through that point.
2. **What you should see**: open `Splash.unity` and press Play - the logo fades/scales in,
   holds briefly, then loads `LevelSelect` automatically (this is also what happens on a real
   device boot, since Splash is first in Build Settings). Level Select shows a scrollable grid
   of 70 level tiles (locked ones dimmed), a Settings button, and (once ads are real) a
   banner. Picking an unlocked level loads `Game` with that level's actual size/seed - notice
   the maze gets visibly bigger every 10 levels. Completing it shows stars + a particle burst
   + a synthesized chime, and records progress you can see reflected back in Level Select
   (more stars, next tile unlocked) if you back out. Settings' Sound/Music/Control-scheme
   toggles and Remove Ads/Restore Purchases buttons are all live (Remove Ads purchases
   through Unity IAP's Fake Store in-editor - no real store account needed to test that flow).
3. **Already verified without you clicking through it**: all of the above *logic* - difficulty
   curve, save/load, star calculation, unlock progression, interstitial cadence, Remove-Ads
   gating - is covered by the 53 automated tests (`Assets/Tests/EditMode` +
   `Assets/Tests/PlayMode`) and by every generator rebuilding cleanly headlessly. What's left
   to confirm live is the same visual/feel check as every prior milestone, plus actually
   seeing the Cinemachine camera's smoothing and the particle/SFX polish in motion.

## 13. Note on the Google Mobile Ads plugin (still needed - the ad network itself is a placeholder)

Unity IAP (`com.unity.purchasing`) is a normal package and is already in `manifest.json` and
fully wired (Remove Ads/Restore Purchases both work for real, testable via Unity IAP's Fake
Store in-editor). The Google Mobile Ads Unity plugin is different: it's **not** distributed
through Unity's package registry, so it couldn't be added automatically here -
`NoOpAdsService` (`Assets/Scripts/Monetization/NoOpAdsService.cs`) stands in for it (banner/
interstitial/rewarded calls just log; rewarded ads auto-grant). To wire the real network,
download the latest `.unitypackage` from
https://github.com/googleads/googleads-mobile-unity/releases and import it via
**Assets → Import Package → Custom Package**, then write a `GoogleMobileAdsService`
implementing `IAdsService` (same interface `NoOpAdsService` implements) and register it in
`GameServices.Ads` instead - every call site (banner on Level Select, interstitial cadence,
rewarded-ad hook) already goes through that interface, so nothing else needs to change.
