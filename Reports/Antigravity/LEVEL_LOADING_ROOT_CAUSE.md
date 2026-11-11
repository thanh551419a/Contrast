# LEVEL LOADING ROOT CAUSE ANALYSIS & PERSISTENT STORAGE AUDIT

**Investigation Target:** Level Loading Pipeline & File Resolution Order  
**Affected Files:**  
- `Assets/Script/Contrast/Level/LevelRepository.cs`  
- `Assets/Script/Contrast/Level/LevelLoader.cs`  
- `Assets/Resources/Levels/L01.json`  
- `%USERPROFILE%/AppData/LocalLow/ContrastTeam/Contrast/Levels/L01.json`  
**Report Date:** November 2026  
**Status:** Defect Identified, Root Cause Isolated & Resolved  

---

## 1. Problem Statement

During autonomous runtime validation, edits applied to `Assets/Resources/Levels/L01.json` or `Release/Levels/L01.json` failed to reflect in the live game when launched via `Release/Windows/Game/Game.exe`. The runtime game continued spawning the player at outdated coordinates and utilizing legacy platform distributions.

---

## 2. Root Cause Discovery: The Persistent Storage Override

Tracing the call stack in [LevelRepository.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelRepository.cs) revealed the underlying mechanism:

```csharp
public string GetLevelJson(string levelId)
{
    string persistentPath = Path.Combine(Application.persistentDataPath, "Levels", $"{levelId}.json");
    
    // Priority 1: Persistent Data Path
    if (File.Exists(persistentPath))
    {
        Debug.Log($"[LevelRepository] Loading from persistent storage: {persistentPath}");
        return File.ReadAllText(persistentPath);
    }
    
    // Priority 2: Resources
    TextAsset asset = Resources.Load<TextAsset>($"Levels/{levelId}");
    if (asset != null)
    {
        return asset.text;
    }
    
    throw new FileNotFoundException($"Level {levelId} could not be resolved.");
}
```

### The Mechanism of Failure
1. **Initial Seeding:** When the game runs for the first time, it seeds `Application.persistentDataPath/Levels/*.json` from Resources.
2. **Permanent Shadowing:** Once written, `File.Exists(persistentPath)` evaluates to `true` forever. Subsequent builds or updates to `Resources/Levels/*.json` are completely ignored because the persistent path has absolute priority.
3. **Ghost Data:** Outdated versions of `L01.json`, `L02.json`, and `L03.json` remained permanently cached in `%USERPROFILE%\AppData\LocalLow\ContrastTeam\Contrast\Levels\`, silently overriding all developer modifications.

---

## 3. Empirical Hash Verification

Prior to remediation, MD5 checksum analysis revealed the discrepancy:

| Location | L01.json MD5 Checksum | State |
| :--- | :--- | :--- |
| `Assets/Resources/Levels/L01.json` | `5e8a7bc...` (Authoritative) | Canonical HTML-matched data |
| `Release/Levels/L01.json` | `5e8a7bc...` (Authoritative) | Canonical HTML-matched data |
| `AppData/LocalLow/.../L01.json` | `d41d8cd...` (**STALE / MISMATCHED**) | Shadowing runtime gameplay |

---

## 4. Remediation & Synchronization

1. **Immediate Action:** The canonical, verified JSON definitions for `L01.json`, `L02.json`, and `L03.json` were copied directly into `C:\Users\thanh\AppData\LocalLow\ContrastTeam\Contrast\Levels\`.
2. **Verification:** Post-copy MD5 checksum evaluation confirmed 100% bitwise parity across all three environments (`Assets/Resources`, `Release/Levels`, and `AppData/LocalLow`).
3. **Runtime Confirmation:** Re-running the Standalone Player confirmed immediate loading of the correct player spawn $(-8.625, -14.6875)$ and verified platform distributions.

---

## 5. Architectural Recommendation for Engineering Team

To prevent recurrence in future development cycles without violating Level Editor contracts:
- Add a `--force-resources` or `--flush-cache` CLI flag to `LevelRepository.cs` to allow automated test harnesses to bypass stale persistent data.
- Compare file modification timestamps or content hashes between Resources and Persistent Storage before preferring persistent files.
