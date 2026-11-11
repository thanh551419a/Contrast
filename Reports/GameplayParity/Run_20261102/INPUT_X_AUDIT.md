# X audit

Source path exists: physical X -> StandaloneKeyboardInputAdapter.GetNewtonMarkDown:92-103 -> PlayerInput.ReadInput:94,121,164 -> PlayerInputIntent.NewtonMarkRequested:177 -> PlayerController.ProcessUpdate:109-122 -> Newton.IsNewtonMarkPressedThisFrame:852-863 -> AttemptCapture:919-943 -> CaptureStepK/I/Kp -> StartRevealSequence:1045 -> Complete:1145-1150. Paths under Assets/Script/Contrast. Player event exists but Newton polls property and also physical-X fallback; do not duplicate handlers.

HTML only captures in PLAY after radial activation1012px, stage<3; repeated held keydown has no new pressed edge (:589,629,1296-1298,1593-1600). K grounding sets axis; I48px/K-prime54px alignment. Capture precedes current player move after rails. Unity Newton separate Update has unverified relative order, no global Playing gate, x-threshold activation instead of radial persistent activation. Stale request/duplicate risk is source hypothesis, not reproduced capture defect.

Fresh preactivation tests: HTML physical CDP X press/hold/repeat/release and movement sequence executed; Unity custom-adapter equivalent executed exit0. No successful capture was exercised. Unity physical keyboard path NOT_RUN; allowed, invalid active, capture/reveal/completion cases NOT_RUN. Input logs retain actual consumed rising edges; keyboard observer fields do not represent adapter input.

L02 dock bug from source: Newton SpawnOrbitPlatforms:232,264 sets TrajectoryData.enabled=false; Update:810-812 enables component only, while Trajectory.ProcessUpdate:110 still returns on disabled config. Protected platform correction required; L01 auto unaffected.

Space/W jump is intentional HTML. Fresh .NET suite283PASS/1FAIL has stale finale Space expectation at Tests/ContrastTests/Program.cs:1448. No runtime behavior changed to satisfy it.

Conclusion: X implementation exists; full end-to-end acceptance NOT_RUN, not proven working.

Post-build test: rebuilt X replay input log contains X press/held/release; Newton state stays CaptureK and marker count0 throughout. The user-facing key pathway delivery was observed; valid capture/result NOT_RUN because normal scripted route does not reach vestibule.
