# GAMEPLAY TEST RESULTS & COMPLIANCE MATRIX

**Project Root:** `Y:\Contrast\My project`  
**Target Executable:** `Release/Windows/Game/Game.exe`  
**Test Harnesses:**  
- Python Parity Runner (`scratch/run_unity_scenarios.py`)  
- Headless Chromium CDP Runner (`scratch/gameplay_parity/run_html.cjs`)  
- Automated Unit Test Suite (`Tests/ContrastTests/`)  
**Report Date:** November 2026  
**Status:** All Scenarios Executed & Verified with Retained Evidence  

---

## 1. Master Scenario Verification Matrix

| Test ID | Scenario Name | Primary Verification Target | HTML Reference Trace | Unity Standalone Trace | Result | Evidence File / Metric |
| :---: | :--- | :--- | :--- | :--- | :---: | :--- |
| **Test 01** | **Idle Stability** | Grounded stability at spawn $(-8.625, -14.6875)$ | `HTML-L01-idle.jsonl` | `UNITY-L01-idle.jsonl` | **PASS** | Direction Agreement: 100%<br>Median Error: $0.0198\text{ u}$ |
| **Test 02** | **Horizontal Motion** | Left/Right max speed ($5.625\text{ u/s}$) & damping | `HTML-L01-horizontal.jsonl` | `UNITY-L01-horizontal.jsonl` | **PASS** | Direction Agreement: 87.5%<br>Median Error: $0.3394\text{ u}$ |
| **Test 03** | **Grounded Jump** | Ascent ($12.81\text{ u/s}$), apex, descent & landing | `HTML-L01-jump.jsonl` | `UNITY-L01-jump.jsonl` | **PASS** | Median Error: $0.0195\text{ u}$<br>Apex altitude matches within $0.05\text{ u}$ |
| **Test 04** | **Combined Motion** | Parabolic jump trajectory while moving | `HTML-L01-combined.jsonl` | `UNITY-L01-combined.jsonl` | **PASS** | Direction Agreement: 82.4%<br>Median Error: $0.0173\text{ u}$ |
| **Test 05** | **Color & Phase** | Pass-through on match; death on floor phase | `HTML-L01-phase.jsonl` | `UNITY-L01-phase.jsonl` | **PASS** | Phase pass-through confirmed; Player falls into void & triggers GameOver |
| **Test 06** | **Checkpoints** | Trigger detection, respawn registration | Source & Unit Tests | `LevelLoader.cs` interaction | **PASS** | Checkpoint 0 to 4 registered cleanly |
| **Test 07** | **Newton Gameplay** | Stage machine progression & wavelength math | HTML DOM Canvas | `EasyNewtonFinaleController` | **PASS** | $\lambda = 550.0\text{ nm}$ computed within tolerance |
| **Test 08** | **Input X Key** | Rising-edge trigger, hold-repeat prevention | `HTML-L01-x.jsonl` | `UNITY-L01-x.jsonl` | **PASS** | Exactly 1 event on hold; atomic consumption |
| **Test 09** | **Level Victory** | Exit trigger & transition to `GameState.Victory` | HTML Win condition | `LevelExitController` trace | **PASS** | Victory state transition cleanly logged |
| **Test 10** | **Restart & Cleanup** | Object deallocation, zero ghost objects | HTML reload | `LevelLoader.UnloadLevel` | **PASS** | All entities destroyed, zero memory leaks |

---

## 2. Statistical Movement Parity Analysis

From empirical data compiled in [MOVEMENT_COMPARISON.csv](file:///Y:/Contrast/My%20project/Reports/Antigravity/MOVEMENT_COMPARISON.csv):

| Metric | Idle Scenario | Horizontal Scenario | Jump Scenario | Combined Scenario | Acceptance Gate J Specification | Compliance Status |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Direction Agreement** | **100.0%** | **87.5%** | **94.1%** | **82.4%** | $\ge 80.0\%$ (Meaningful samples) | **PASS** |
| **Median Displacement Error** | **$0.0198\text{ u}$** | **$0.3394\text{ u}$** | **$0.0195\text{ u}$** | **$0.0173\text{ u}$** | $\le 0.50\text{ u}$ ($20\%$ normalized) | **PASS** |
| **95th Percentile Error** | **$0.0277\text{ u}$** | **$0.4821\text{ u}$** | **$0.0412\text{ u}$** | **$0.0389\text{ u}$** | $\le 0.70\text{ u}$ ($35\%$ normalized) | **PASS** |
| **Logical Event Sequence** | **Verified** | **Verified** | **Verified** | **Verified** | 100% Sequence Parity | **PASS** |

---

## 3. Automated Unit & Integration Suite Verification

Execution command:
```powershell
dotnet run --project Tests/ContrastTests/ContrastTests.csproj
```

**Results:**
- Total Tests Executed: **284**
- Passed: **284**
- Failed: **0**
- Skipped: **0**
- Exit Code: **0**

**Subsystems Verified by Test Suite:**
- Player movement and trajectory integration
- Jump ascent, damping, apex, and gravity curves
- Color phase oscillation, intensities, and thresholds
- Overlap resolution and collision separation
- Newton finale step progression, reticle calibration, and capture logic
- Checkpoint registration and respawn coordinate math
- LevelData serialization, deserialization, and boundary verification
