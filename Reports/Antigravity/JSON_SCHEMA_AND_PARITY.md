# JSON SCHEMA AND PARITY REPORT

**Target Files:**  
- `Assets/Resources/Levels/L01.json`  
- `Assets/Resources/Levels/L02.json`  
- `Assets/Resources/Levels/L03.json`  
**C# Schema Model:** `Assets/Script/Contrast/Data/LevelData.cs`  
**Report Date:** November 2026  
**Status:** Validated & Parity Confirmed  

---

## 1. Coordinate Conversion Mathematics

HTML level authoring specifies rectangles as $[x, y, w, h]$ on an inverted Y canvas. Unity 2D game objects utilize center-anchored bounding boxes in Cartesian coordinates $(+X \text{ right}, +Y \text{ up})$.

### Exact Mathematical Mapping
$$X_{\text{center}} = \frac{x_{\text{HTML}} + \frac{w_{\text{HTML}}}{2}}{64.0}$$
$$Y_{\text{center}} = -\frac{y_{\text{HTML}} + \frac{h_{\text{HTML}}}{2}}{64.0}$$
$$\text{Width} = \frac{w_{\text{HTML}}}{64.0}, \quad \text{Height} = \frac{h_{\text{HTML}}}{64.0}$$

### Inverse Mapping (Unity to HTML)
$$x_{\text{HTML}} = \left(X_{\text{center}} - \frac{\text{Width}}{2}\right) \times 64.0$$
$$y_{\text{HTML}} = \left(-Y_{\text{center}} - \frac{\text{Height}}{2}\right) \times 64.0$$

---

## 2. Unity C# Deserialization Schema

The runtime schema is defined in [LevelData.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs):

```csharp
[System.Serializable]
public class LevelData
{
    public string levelId;
    public string levelName;
    public SpawnPointData startPos;
    public SpawnPointData spawnPoint;
    public BoundaryData cameraBoundary;
    public BoundaryData end;
    public List<PlatformData> platforms;
    public List<CheckpointData> checkPoint;
    public List<CollectibleData> collectibles;
    public NewtonData newton;
}

[System.Serializable]
public class PlatformData
{
    public Vector2Data position;
    public Vector2Data size;
    public float rawGrayscaleColor;
    public TrajectoryData trajectory;
}

[System.Serializable]
public class CheckpointData
{
    public Vector2Data position;
    public Vector2Data size;
    public int index;
}

[System.Serializable]
public class CollectibleData
{
    public Vector2Data position;
    public Vector2Data size;
    public int id;
    public string type;
}
```

---

## 3. Runtime Schema Gaps & Architectural Documentation

### 3.1 Collectible Entities Runtime Gap `[CONFIRMED GAP]`
- **Observation:** `L01.json`, `L02.json`, and `L03.json` contain valid collectible metadata extracted from HTML reference (e.g. 2 collectibles in L01, 2 in L02, 3 in L03).
- **Runtime Investigation:** Inspection of [LevelLoader.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) reveals that while `LevelData.collectibles` is deserialized, `LevelLoader` does not instantiate collectible GameObjects or attach pickup trigger components.
- **Architectural Policy Compliance:**
  - *Contract Rule:* Do not silently delete valid reference data from JSON to conceal runtime deficiencies.
  - *Resolution:* Collectible definitions are preserved verbatim in JSON. The absence of runtime collectible GameObjects is formally recorded as an approved non-Core gameplay gap.

---

## 4. Verification Checksum & Integrity Matrix

| Level | JSON File Path | Platform Count | Checkpoint Count | Collectibles Count | Newton Block | Schema Validation Status |
| :---: | :--- | :---: | :---: | :---: | :---: | :---: |
| **L01** | `Assets/Resources/Levels/L01.json` | 16 | 5 | 2 | Present | **VALID / PASS** |
| **L02** | `Assets/Resources/Levels/L02.json` | 15 | 4 | 2 | Present | **VALID / PASS** |
| **L03** | `Assets/Resources/Levels/L03.json` | 27 | 8 | 3 | Present | **VALID / PASS** |

All JSON files deserialize cleanly with zero schema warnings and zero missing required fields.
