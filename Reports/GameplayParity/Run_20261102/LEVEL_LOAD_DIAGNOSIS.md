# Level load diagnosis

VERIFIED FROM SOURCE: LevelRepository.cs:22-27 uses Application.persistentDataPath/Levels; :214-238 seeds Resources L01-L03 only if file absent; :60-93 persistent filenames override external directories and merged list sorts by mtime. LevelSelectUI.cs:119,273 passes selected full disk path to GameManager.cs:201-278. Legacy LevelLoader.cs:58-111 has first-file/legacy/programmatic fallback; normal replay uses explicit filename.

Actual persisted directory C:/Users/thanh/AppData/LocalLow/ContrastTeam/Contrast/Levels exists. All3 files differ from canonical by hash. L01 persisted5392bytes hash prefix1A4E4DE2 versus canonicalECB8D5A6. Thus Resources-only repairs do not refresh existing menu data. Normal menu selection itself NOT_RUN. No persisted production copy overwritten.

VERIFIED BY RUNTIME TEST: Unity idle/horizontal/jump/combined/phase/x logs record [FLOW][START_GAME_FILE] and levelSource=[FilePath] Y:/Contrast/My project/Assets/Resources/Levels/L01.json. Canonical direct load succeeds; this deliberately tests the intended file and does not establish normal menu source acceptance.

Mismatch chain: accurate HTML conversion -> correct supported JSON -> possibly stale selected copy -> sanitizer -> builder ignores player size/collectibles -> runtime camera/death/movement/Newton differs. Actual observer reports size1x1 versus JSON .59375x1. PlayerController.cs:19 default size; LevelLoader.cs:175 uses size for spawn marker, :314 only respawns. Player root scale.3 is independent visual issue.

CameraFollow.cs:97-109 clamps; ComputeKillY:521-529 derives bottom-1. PlayerController:217-228 combines dynamic grace3.75units(240px) plus fixed kill plane versus HTML560px dynamic rule. Corrections touch protected runtime; do not compensate in JSON.

Cleanup, failed initialization, sanitizer mutation and ghost-free repeat loads require separate runtime tests; NOT_RUN.
