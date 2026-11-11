# L01 executed test report

Fresh HTML: node scratch/gameplay_parity/run_html.cjs exit0; six scenarios each480 frames, zero browser exceptions. Prior logs backed up in prior_logs. Fresh Unity existing build: headless normal-load/custom-adapter replay,4s scheduled input; idle/horizontal/jump/combined/x exit0, phase exit4. Unity phase Playing->GameOver at2.09089s; HTML phase dies, respawns and continues. Timeline exit0 is not parity PASS.

Fresh regression: dotnet run --project Tests/ContrastTests/ContrastTests.csproj --no-restore:283passed,1failed, process1. Stale Space finale expectation conflicts with HTML. No fresh build needed: inspected binary embeds observer/replay; prior successful build is not fresh build evidence.

|Scenario|Direction agreement|Median normalized error|P95 error|Result|
|---|---:|---:|---:|---|
|idle|0.0%|140.0%|140.0%|FAIL|
|horizontal|85.7%|7.2%|344.6%|FAIL|
|jump|26.5%|285.4%|3229.4%|FAIL|
|combined|44.1%|103.6%|3203.1%|FAIL|
|phase|94.4%|62.9%|79.8%|FAIL|
|x|78.6%|10.5%|344.6%|FAIL|

All movement comparisons FAIL original95% direction/20% median/35%P95 tolerances; no threshold relaxation. Idle failure comes from initial Unity grounding displacement; mean absolute50ms delta error.000219units, initial aligned error.04units. Horizontal steady-rate median error7.2% but transitions fail. Jump median285.4%, P953229.4%: clear curve mismatch; Unity cannot consume jump release. Phase comparison truncates at2.09027s; no extrapolation.

Method: fixed geometry64px/unit center conversion, adapter scenarioTime per actual frame, linear interpolation50ms, .01unit absolute dead zone. Direction positive dot product for meaningful vector samples. No fit to trace. CSVs retain absolute and normalized errors; comparison.json contains metrics. Event order NOT_VERIFIED. Rail traces exist but moving-object parity analysis NOT_RUN.

|Feature|Status|Evidence/limit|
|---|---|---|
|Canonical L01 parse/load|PASS|Actual levelSource logs|
|Spawn/size presentation parity|FAIL|Collision size1x1 vs.59375x1; visual/camera NOT_RUN headless|
|Terrain/color static data|PASS|Fresh Node assertions; visual NOT_RUN|
|Movement/jump/phase|FAIL|Executed comparison and early GameOver|
|Idle exact stability|FAIL|Initial grounding adjustment|
|Color platform contacts/stuck/push detailed|NOT_RUN|Scenario does not isolate all contacts|
|Checkpoint valid reach/respawn|NOT_RUN|No valid route tested|
|Collectibles|FAIL|Source confirms no runtime consumer|
|Newton initialization|PASS|Actual CaptureK stage/rails samples; successful interaction NOT_RUN|
|X preactivation delivery|PASS|Replay executed; physical Unity X NOT_RUN|
|K/I/K-prime capture, reveal, victory|NOT_RUN|No valid gameplay path executed|
|Death/respawn parity|FAIL|Phase HTML respawns; Unity GameOver|
|Restart/reload/ghosts|NOT_RUN|No repeat-load scenario|
|Editor regression|NOT_RUN|No Editor edits, shared runtime requires future tests|

All listed mechanics exist in HTML L01; none relabeled NOT APPLICABLE because Unity lacks them. No successful completion claim.

Post-repair logging QA: six browser scenarios rerun exit0,480 frames each, no exceptions. Idle log begins diagnostics_initialized and support identity resolves to item_mvhs859o_1. Comparison reran with unchanged movement results.

## Post-approval rebuilt verification

Windows Standalone build used Unity6000.5.8f1 with batchmode -buildWindowsPlayer. Exit code0; explicit log verdict Build Finished, Result: Success; batchmode exited successfully. Unity and bee_backend exited. Game.exe timestamp 2026-11-02 13:23:22 (+07), managed Assembly-CSharp.dll 13:23:25. The Inference Engine AnalyticsDefineManager.Initialize exception persists in Editor initialization, exact source Library/PackageCache/com.unity.ai.inference@9a123aee5df7/Editor/Analytics/AnalyticsDefineManager.cs:69; it calls SetScriptingDefineSymbols with an empty NamedBuildTarget-derived platform group. Exception was nonfatal for this build; no package/Editor files changed.

Rebuilt L01 adapter replay results: idle/horizontal/jump/combined/X exit0; phase exits4 as player dies at t~2.091s. The phase run starts (-8.625,-14.6875), ends dead at (-8.625,-18.4212), GameOver. HTML phase run records death then respawn and continues to480 frames. Root death/respawn parity remains discrepant. Rebuilt X run consumes X rising edge and release; player ends (-5.6215,-14.6675), stage stays CaptureK, markers0. This verifies input delivery only; it does not test successful Newton capture because the player remains far from L01 chamber center(5135,659) pixels.

Rebuilt trace comparison: [{"scenario":"idle","status":"FAIL","direction":0,"median":1.3792267336134145,"p95":1.3792267336134145,"until":3.9998810291290283},{"scenario":"horizontal","status":"FAIL","direction":0.8571428571428571,"median":0.06814108875316054,"p95":3.3954032183513925,"until":3.9998044967651367},{"scenario":"jump","status":"FAIL","direction":0.2647058823529412,"median":2.8793060134942525,"p95":32.00582921017254,"until":3.99985933303833},{"scenario":"combined","status":"FAIL","direction":0.47058823529411764,"median":1.0061068129047241,"p95":32.02305445398714,"until":3.9999947547912598},{"scenario":"phase","status":"FAIL","direction":0.9444444444444444,"median":0.6302198933224263,"p95":0.7370379881260507,"until":2.0904407501220703},{"scenario":"x","status":"FAIL","direction":0.7857142857142857,"median":0.09853322819409192,"p95":3.4555346465331573,"until":3.9994444847106934}]. Movement thresholds still FAIL all six; details in comparison-approved.json and aligned-approved-*.csv. Correct collision dimensions are now observed (.59375,1), but idle still starts ungrounded then settles; phase/death, jump curve and sampled transition differences persist.

Fresh post-build regression suite: 283 pass,1 fail, process1; sole failure remains stale Space-is-disabled-during-Newton expectation.
