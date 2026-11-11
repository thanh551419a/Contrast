# JSON parity

Canonical files Assets/Resources/Levels/L01.json,L02.json,L03.json already contained prior repairs at run start. No further JSON edits made. All58 platform rectangles/materials,7 collectible identities/positions/sizes,3 spawn centers passed fresh Node static assertions at tolerance1e-6. Hashes match existing ../json/validation.json. Existing backups remain ../json/L01.before.json etc; retain Git diff.

Prior repairs add collectible consumed position/size and Newton enabled/vestibuleId, preserving raw data. Do not rerun stale scratch/generate_level_json.py: it would lose these repairs. Unity actual L01 parsing/build executed through canonical replay; L02/L03 Unity construction NOT_RUN.

Unsupported/incomplete semantics: checkpoint zone distinct from respawn (single schema position); stable terrain IDs/material names; exact phase; free-camera and dynamic-death semantics; checkpoint resetNewton; Newton R/lambda fields ignored; collectibles declared/sanitized but never instantiated. JsonUtility may drop raw extra keys on serialization. L03 endpoint/period data match but runtime easing differs; do not falsify JSON.

L01 checkpoint zone centers differ from stored respawn center by(-.0625,+.0078125) units; L02 vertical differences reach.3203125. Player size is correct in JSON but ignored at spawn. Camera authored rectangle cannot represent unrestricted camera. Full data acceptance FAIL due unsupported runtime/schema representation, despite geometry assertions PASS.
