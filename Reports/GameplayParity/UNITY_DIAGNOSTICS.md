# Unity diagnostics task scope

- Objective: optional read-only JSONL observation of actual Unity gameplay and existing phase logs.
- Author/owner: unity_engineer.
- Files: new `Assets/Diagnostics/GameplayParity/GameplayParityObserver.cs` and this report only.
- Classification: diagnostics; no existing Core method/span or Editor file edits; no platform behavior changes.
- Sampling: LateUpdate observes final frame state without changing execution order. Existing log messages retain phase provenance; missing phase detail remains unknown.
- Input: observe physical keyboard and the existing Player Newton request event, never consume or synthesize input.
- Test plan: Unity compilation; QA opt-in runtime launch; inspect JSONL and unchanged protected source diff.
- Separate replay harness is only proposed pending Guardian boundary assessment.

## Implementation

`GameplayParityObserver.cs` uses `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` only when a new output filename is supplied through `-contrastParityLog <absolute-file.jsonl>` or `CONTRAST_PARITY_LOG`. It creates its diagnostic host, subscribes to existing log and Newton-request observation events, and samples public getters in LateUpdate. It does not call gameplay processing or alter script execution order. The observer removes subscriptions on destruction.

The output has runId, environment, levelId, eventName, frameIndex, simulationTime, deltaTime, observationPhase, object identity, position/size, physical keyboard snapshot, Player state/color/phase/grounded, inter-frame observed displacement, Newton stage and marker count. Platform IDs use existing runtime names `ColorPlatform_<canonical index>`; Newton K/I have explicit IDs. Marker IDs retain runtime names. Existing phase messages are copied verbatim into `phase_log` records with their true observation phase, without pretending an observed final state was an internal commit callback.

## Limitations

- Displacement is between observed LateUpdate positions, and can include respawn/other existing writers; it is not falsely labelled as the movement resolver's proposed result.
- Exact input intent/impact/collision values remain in captured existing runtime text logs. No protected source method was instrumented.
- Keyboard samples are physical device observations. A future scripted adapter test must retain its own injected input log rather than treating keyboard fields as injected input.
- No collectible runtime component exists under the inspected Contrast source; missing collectibles cannot produce observed collection events.
- No gameplay replay is implemented in this observer. The proposed separate adapter harness requires coordination with Guardian and QA.
- Compile and runtime execution are NOT_RUN until the corresponding retained command evidence exists.

## Opt-in input replay harness

Guardian boundary assessment permits a new harness using existing public loading and custom-adapter test seams. `GameplayParityReplay.cs` calls `GameManager.PlayLevelFromFile` then installs an `IPlatformInputAdapter` through existing `PlayerInput.SetCustomAdapter`. It never calls ProcessUpdate, movement, collision, teleport, grounded setters, Newton capture or win methods. The adapter's scenario clock advances only in the existing ReadInput -> ResetFrameState call. Its coroutine handles normal load setup, completion observation and test-process exit only.

CLI: `-contrastParityReplay <canonical JSON> -contrastParityScenario idle -contrastParityDuration 5 -contrastParityReplayLog <new input.jsonl> -contrastParityLog <new state.jsonl>`.

Scenarios use seconds since the first actual input read: idle; horizontal Right [1,2), Left [3,4); jump press at 1; combined Right [1,2) and jump at 1; phase RightArrow [1,2); x press/hold [1,1.1), [2,2.1), [3,4). X one-shot occurs on held-state rising edge only. The replay log records each actual adapter consumption and its scenario time. This tests the adapter seam, not the physical keyboard path. Physical X must be tested separately.

No test scenario forces later checkpoints or Newton activation. If these are unreachable through the provided valid input sequence, their acceptance remains NOT_RUN.

Parent-aligned final schedules (supersede the initial schedule above): all run 4 seconds. Horizontal D [1,2), A [2.5,3.5); jump edge1 with logical hold [1,1.2); combined D [1,3) plus jump; phase right [1,2), left [2.5,3.5); X held [1,2.5) then released, D+X [2.75,3.25). A repeated physical keydown while already held is represented by no second logical rising edge, as required. Timing uses actual Unity deltaTime, logged per consumption; the harness does not change Time settings.
