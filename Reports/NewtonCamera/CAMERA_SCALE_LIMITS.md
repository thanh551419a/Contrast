# NEWTON CAMERA SCALE LIMITS & FRAMING REPORT (`minScale` / `maxScale`)

## 1. Explicit Scale Bounds Configuration

In `CameraFollow.cs`, `minScale` and `maxScale` bound the orthographic camera's `orthographicSize` (and **never** modify `Camera.transform.localScale`):

```csharp
minScale = Mathf.Min(normalFollowOrthographicSize, newtonWideOrthographicSize);
maxScale = Mathf.Max(normalFollowOrthographicSize, newtonWideOrthographicSize);
```

### Per-Level Endpoint Values
| Level | Normal Follow `minScale` | Newton Wide `maxScale` | Newton Camera Center `(cx, cy, z)` | Outer Ring Radius `r_K` |
| :--- | :---: | :---: | :---: | :---: |
| **`L01`** (`k = 6, i = 3`) | `5.00000` | `10.25351` | `(80.23438, -10.29688, -10.0)` | `8.42012 u` |
| **`L02`** (`k = 4, i = 2`) | `5.00000` | `8.60000` | `(79.84375, -8.25000, -10.0)` | `6.87500 u` |
| **`L03`** (`k = 7, i = 2`) | `5.00000` | `11.00536` | `(81.26563, -32.81250, -10.0)` | `9.09477 u` |

In all levels:
- `minScale = 5.0f > 0f`
- `maxScale >= minScale` (`8.60f >= 5.0f`, `10.25351f >= 5.0f`, `11.00536f >= 5.0f`)

---

## 2. Per-Frame Clamping & Velocity Correction

On every update inside `CameraFollow.UpdateBidirectionalNewtonCamera`:
1. **Target Clamping**:
   ```csharp
   desiredOrthographicSize = Mathf.Clamp(desiredOrthographicSize, minScale, maxScale);
   ```
2. **Input State Clamping**:
   ```csharp
   currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
   ```
3. **Output Clamping & Outward Velocity Correction**:
   ```csharp
   if (newScale <= minScale)
   {
       newScale = minScale;
       if (scaleVelocity < 0f)
           scaleVelocity = 0f;
   }
   else if (newScale >= maxScale)
   {
       newScale = maxScale;
       if (scaleVelocity > 0f)
           scaleVelocity = 0f;
   }
   cam.orthographicSize = Mathf.Clamp(newScale, minScale, maxScale);
   ```
This guarantees `minScale <= Camera.orthographicSize <= maxScale` on every single frame under all frame rates, rapid threshold crossings, and lifecycle transitions.

