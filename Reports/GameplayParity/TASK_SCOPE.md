# Task scope and checklist

All work follows ARCHITECTURE_CONTRACT v2.8. EasyNewtonFinaleController.Update remains unapproved.

| Task | Owner | Dependency | Evidence | Status | Approval |
|---|---|---|---|---|---|
| Prior-report/source/load-path verification | architecture_auditor | Contract | Existing reports and current source | Done | Read-only |
| HTML extraction/static parity | html_analyst | Actual HTML | HTML_LEVEL_EXTRACTION.md | Running | Read-only |
| Canonical JSON schema repairs | json_engineer | LevelData/HTML | json/validation.json and backups | Implemented | Authorized data only |
| Optional HTML JSONL observer | root | Final update/consoleEvent hooks | Embedded diagnostics and browser run | In progress | No gameplay writes |
| Optional Unity JSONL observer | unity_engineer | Public getters/existing logs | New opt-in observer | In progress | No Core changes |
| Input replay harness | unity_engineer | Guardian review | New test-only bootstrap/input adapter | Pending | Guardian boundary review |
| Newton/X verification | newton_specialist | Source/logs | INPUT_X_AUDIT.md | Pending | No protected edits |
| Independent executed QA | qa_release | Instrumentation/data | Logs and scenario report | Pending | No protected edits |
| Guardian review | architecture_guardian | Proposed/applied scopes | ARCHITECTURE_GUARDIAN_REVIEW.md | Pending | Owner alone approves Core |

Root owns HTML optional diagnostic insertion, comparison tooling, and consolidated reports. No concurrent file ownership overlap. JSON metadata and platform behavior preserved. Test sequences must enter through keyboard/input adapter, never teleport or force success.
