# Checklist

|Task|Responsible|Dependency|Evidence|Status|Approval|
|---|---|---|---|---|---|
|Architecture audit|architecture_auditor|Contract|PROJECT_AUDIT.md|Done source|Read-only|
|HTML extraction|html_analyst|Actual HTML|HTML_LEVEL_EXTRACTION.md|Done source|Read-only|
|JSON validation|json_engineer|Schema/HTML|JSON_PARITY_REPORT.md|Supported geometry PASS|Existing data repairs preserved|
|Runtime/load audit|unity_engineer|Source/binary|LEVEL_LOAD_DIAGNOSIS.md|Done source/runtime canonical|Protected fixes pending|
|Newton/X audit|newton_specialist|HTML/runtime|INPUT_X_AUDIT.md|Source done; full gameplay NOT_RUN|Protected fixes pending|
|QA|qa_release/root execution|Existing harness|Logs/CSV/test report|Executed six scenarios; parity FAIL|No production edits|
|Guardian|architecture_guardian|Exact spans|ARCHITECTURE_GUARDIAN_REVIEW.md|Review done|Owner approval outstanding|
|Integration/comparison|root|Actual logs|compare.cjs/comparison.json|Done|Non-Core tooling|

Exclusive production file ownership was unnecessary: no production edits in this run. Root owns run artifacts. Existing pre-run changes and .meta identities preserved.

Root implemented and Guardian reviewed HTML snapshot/clear repairs only. Backup html.before-observer-repair.html; six browser scenarios rerun exit0; idle log first event diagnostics_initialized and actual support item_mvhs859o_1. No protected implementation applied.
