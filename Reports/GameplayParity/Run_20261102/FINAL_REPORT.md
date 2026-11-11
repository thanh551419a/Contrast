# Final report — acceptance not achieved

All seven specialists used installed configurations. Fresh six HTML and six Unity replay scenarios, regression suite, static geometry validation and movement comparison executed. No existing Unity runtime/Core/Editor source or JSON was changed by this run. Existing repairs/diagnostics were preserved; two HTML observer-only lines were repaired. New run comparison tooling/reports/logs generated. Existing HTML observer/replay and Unity observer/replay reused.

Confirmed defects: stale persisted data priority; ignored player size; absent collectible construction; checkpoint zone schema mismatch; camera/death differences; jump release absent and divergent curves; Newton activation/gating/order risks; L02 disabled trajectory config; L03 easing. Protected fixes were not applied. HTML observer numeric support identity and initialization-after-clear defects were repaired; all six browser scenarios reran exit0 with480 frames and no exceptions. Actual initial LEVEL_START still precedes observer install; initialization snapshot is recorded, not mislabeled LEVEL_START.

|Scenario|Direction agreement|Median normalized error|P95 error|Result|
|---|---:|---:|---:|---|
|idle|0.0%|140.0%|140.0%|FAIL|
|horizontal|85.7%|7.2%|344.6%|FAIL|
|jump|26.5%|285.4%|3229.4%|FAIL|
|combined|44.1%|103.6%|3203.1%|FAIL|
|phase|94.4%|62.9%|79.8%|FAIL|
|x|78.6%|10.5%|344.6%|FAIL|

|Acceptance|Status|
|---|---|
|1 Data|FAIL: supported geometry accurate, unsupported schema semantics remain|
|2 Loading|FAIL overall: explicit canonical replay PASS, menu stale precedence unresolved|
|3 Presentation|FAIL dimensions; rendered inspection NOT_RUN|
|4 Input|FAIL parity; full physical X NOT_RUN|
|5 Tracing|PASS for actual sessions, incomplete phase/collision/support schema|
|6 Motion|FAIL all six movement metrics|
|7 Mechanics|FAIL missing collectibles; full Newton/checkpoint/completion NOT_RUN|
|8 Architecture|FAIL existing discrepancies; no new Core/Editor edits|
|9 Evidence|PASS explicit statuses and real retained logs|
|10 Completion|FAIL full game acceptance; specialist results integrated|

Unity phase exits4 at GameOver; other replay timelines exit0. Fresh regression283passed/1failed stale Space expectation. X exists in source and preactivation adapter input executes; full physical/capture correctness unproven. L01 does not match sufficiently.

Next owner decision: exact collision-size and Newton Playing-gate unapplied diffs in ARCHITECTURE_GUARDIAN_REVIEW.md. Their approval alone does not authorize broader checkpoint/collectible/camera/physics/platform/Core redesign. Further protected corrections require separately scoped exact proposals. Read-only and executable diagnostic work completed here; full valid-path Newton/completion and Editor regression remain NOT_RUN. No overall PASS claim.

## Post-approval rebuilt verification

Windows Standalone build used Unity6000.5.8f1 with batchmode -buildWindowsPlayer. Exit code0; explicit log verdict Build Finished, Result: Success; batchmode exited successfully. Unity and bee_backend exited. Game.exe timestamp 2026-11-02 13:23:22 (+07), managed Assembly-CSharp.dll 13:23:25. The Inference Engine AnalyticsDefineManager.Initialize exception persists in Editor initialization, exact source Library/PackageCache/com.unity.ai.inference@9a123aee5df7/Editor/Analytics/AnalyticsDefineManager.cs:69; it calls SetScriptingDefineSymbols with an empty NamedBuildTarget-derived platform group. Exception was nonfatal for this build; no package/Editor files changed.

Rebuilt L01 adapter replay results: idle/horizontal/jump/combined/X exit0; phase exits4 as player dies at t~2.091s. The phase run starts (-8.625,-14.6875), ends dead at (-8.625,-18.4212), GameOver. HTML phase run records death then respawn and continues to480 frames. Root death/respawn parity remains discrepant. Rebuilt X run consumes X rising edge and release; player ends (-5.6215,-14.6675), stage stays CaptureK, markers0. This verifies input delivery only; it does not test successful Newton capture because the player remains far from L01 chamber center(5135,659) pixels.

Rebuilt trace comparison: [{"scenario":"idle","status":"FAIL","direction":0,"median":1.3792267336134145,"p95":1.3792267336134145,"until":3.9998810291290283},{"scenario":"horizontal","status":"FAIL","direction":0.8571428571428571,"median":0.06814108875316054,"p95":3.3954032183513925,"until":3.9998044967651367},{"scenario":"jump","status":"FAIL","direction":0.2647058823529412,"median":2.8793060134942525,"p95":32.00582921017254,"until":3.99985933303833},{"scenario":"combined","status":"FAIL","direction":0.47058823529411764,"median":1.0061068129047241,"p95":32.02305445398714,"until":3.9999947547912598},{"scenario":"phase","status":"FAIL","direction":0.9444444444444444,"median":0.6302198933224263,"p95":0.7370379881260507,"until":2.0904407501220703},{"scenario":"x","status":"FAIL","direction":0.7857142857142857,"median":0.09853322819409192,"p95":3.4555346465331573,"until":3.9994444847106934}]. Movement thresholds still FAIL all six; details in comparison-approved.json and aligned-approved-*.csv. Correct collision dimensions are now observed (.59375,1), but idle still starts ungrounded then settles; phase/death, jump curve and sampled transition differences persist.

Fresh post-build regression suite: 283 pass,1 fail, process1; sole failure remains stale Space-is-disabled-during-Newton expectation.
