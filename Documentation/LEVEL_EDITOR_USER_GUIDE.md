# Contrast Level Editor: User & Level Designer Guide

> **Target Audience:** Level Designers, Content Creators, and Players.  
> **Purpose:** A complete procedural, step-by-step manual on how to design, test, and save levels in Contrast without needing any programming knowledge.

---

## 1. Introduction

Welcome to the **Contrast Level Editor**! This built-in tool gives you the power to craft custom puzzle-platforming stages complete with color-shifting platforms, moving obstacle trajectories, checkpoint systems, and camera boundaries.

### What You Can Do in the Editor:
- **Build World Geometry:** Place, resize, and position static and dynamic platforms.
- **Tune Puzzle Logic:** Assign colors (White, Gray, Black, or Universal Gold) to dictate where the player can walk or fall through.
- **Animate Platforms:** Create smooth moving platforms along linear or circular trajectories.
- **Structure the Challenge:** Place player spawn points, sequential checkpoints, and the victory goal.
- **Camera Boundary Framing:** Define the visible boundaries of the level to frame the player's view.
- **Instant Playtesting:** Test your level immediately with a single click and return to the editor just as quickly.
- **File Management:** Save and load custom JSON level files.

---

## 2. Editor Interface Overview

When you launch the Level Editor from the Main Menu, the screen is organized into several key areas:

```
+-------------------------------------------------------------------------+
| [Level Name Input]  [Save] [Load] [Play Test] [Clear]     [Main Menu]   | <-- Top Bar
+---------------+-----------------------------------------+---------------+
| TOOL PALETTE  |                                         | INSPECTOR     |
|               |                                         |               |
| [+ Platform]  |             VIEWPORT AREA               | Position:     |
|               |                                         |   X [   ]     |
| [+ Checkpoint]|       (Click, Drag, Pan, Zoom)          |   Y [   ]     |
|               |                                         | Size:         |
| [Cam Bounds]  |                                         |   W [   ]     |
|               |                                         |   H [   ]     |
|               |                                         | Color:        |
|               |                                         |   [Slider]    |
|               |                                         | Trajectory:   |
|               |                                         |   [ ] Enabled |
+---------------+-----------------------------------------+---------------+
```

1. **Top Bar:** Quick controls for saving, loading, playtesting, clearing, and exiting.
2. **Left Palette:** Creation buttons to spawn new platforms, checkpoints, or configure camera bounds.
3. **Viewport Area:** The main workspace where you view and interact directly with your level.
4. **Right Inspector:** Contextual settings panel that displays properties for whichever object you currently have selected.

---

## 3. Viewport Navigation & Camera Controls

Before placing objects, learn how to move around your level:

| Action | Control |
| :--- | :--- |
| **Pan Camera** | Hold **Middle Mouse Button** (Scroll Wheel Click) and drag the mouse, OR hold **Alt + Right Mouse Button** and drag. |
| **Zoom In / Out** | Scroll the **Mouse Wheel** Up (zoom in) or Down (zoom out). |
| **Select Object** | **Left Click** directly on any platform, marker, or handle. |
| **Deselect** | **Left Click** on empty space in the viewport. |

---

## 4. Working with Objects

### 4.1 Creating Platforms
1. Click the **[+ Platform]** button in the Left Palette.
2. A new 1x1 platform will appear in the center of your screen.
3. The platform will automatically be selected, and its properties will appear in the Inspector on the right.

### 4.2 Selecting and Moving Objects
- **Click & Drag:** Left-click and hold the body of any platform to drag it freely across the screen.
- **Precise Coordinate Entry:** With the object selected, look at the **Position** inputs in the Inspector. You can type exact numerical values into the **X** and **Y** fields.

### 4.3 Resizing Objects
- **Corner Handles:** When a platform is selected, small square handles appear at its corners. Left-click and drag any corner handle to stretch or shrink the platform.
- **Precise Dimensions:** In the Inspector, adjust the **Size Width (W)** and **Size Height (H)** numerical fields.

### 4.4 Deleting Objects
1. Left-click the platform or checkpoint you want to delete.
2. Press the **Delete** or **Backspace** key on your keyboard.
3. *Note: The Level Start marker and Victory End marker cannot be deleted.*

---

## 5. Configuring Platform Colors

The core mechanic of Contrast depends on platform color:
- **White Platform ($0 - 84$):** Solid only when the player is White.
- **Gray Platform ($85 - 169$):** Solid only when the player is Gray.
- **Black Platform ($170 - 254$):** Solid only when the player is Black.
- **Universal Platform ($255$, Gold):** Always solid, regardless of the player's color.

### How to Change Color:
1. Select a platform.
2. In the Inspector, locate the **Color** section.
3. You have two ways to set color:
   - **Preset Buttons:** Click **[White]**, **[Gray]**, **[Black]**, or **[Universal]** for instant standard values.
   - **Continuous Slider:** Drag the grayscale slider ($0$ to $255$) to fine-tune the shade. The platform's color will update live in the viewport.

---

## 6. Setting Up Moving Platforms (Trajectories)

You can turn any static platform into a moving platform.

### Step-by-Step Trajectory Setup:
1. Select the platform you want to animate.
2. In the Inspector, find the **Trajectory** section and check the **[x] Enabled** box.
3. Choose the **Curve Type**:
   - **Line:** The platform moves back and forth or loops along a straight path.
   - **Circle:** The platform rotates around a circular orbit.
4. Choose the **Playback Mode**:
   - **Ping-Pong:** The platform moves to the end, then reverses back to the start smoothly.
   - **Loop:** The platform reaches the end and wraps immediately back to the beginning.
   - **Once:** The platform travels its path once and stops at the destination.
5. Set the **Duration**:
   - Enter the time in seconds (e.g., `4.0` seconds) that it takes to complete a movement cycle. Higher numbers mean slower movement; lower numbers mean faster movement.
6. Optional Settings:
   - **Reverse Direction:** Check this box if you want the platform to start moving in the opposite direction.
   - **Rotation:** For linear platforms, set an angle in degrees to tilt the movement path (e.g., $45^\circ$ for diagonal motion, $90^\circ$ for purely vertical motion).

### Using Trajectory Handles in the Viewport:
When an animated platform is selected, visual handles appear:
- **Origin Handle:** Drag this handle to reposition where the path is anchored in the world.
- **End / Radius Handle:** Drag this handle to extend the length of a line path or expand the radius of a circular orbit.

### Previewing Movement:
- Click the **[Preview Trajectories]** toggle button in the Inspector or Top Bar.
- All moving platforms will begin animating in real time inside the editor!
- Check that platform timings align and jumps are physically possible.
- Click **[Stop Preview]** to halt the animation and return all platforms to their starting positions.

---

## 7. Level Flow: Spawns, Checkpoints, and Goals

Every playable level requires a starting point, checkpoints, and a goal.

### 7.1 Start Spawn Point
- The green **Start Marker** indicates where the player will spawn.
- Left-click and drag the Start Marker to your desired beginning position.
- In the Inspector, you can configure the player's **Starting Color** (White, Gray, Black, or Universal).

### 7.2 Checkpoints
- Click **[+ Checkpoint]** on the Left Palette to add a checkpoint flag.
- Position the checkpoint along your level's path.
- In the Inspector, choose what color the player will switch to when touching this checkpoint.
- When the player touches a checkpoint during gameplay, their respawn location and color update immediately.

### 7.3 Victory Goal Area
- The yellow/gold **End Marker** represents the exit portal of the stage.
- Move and resize the End Marker to cover the final door or landing pad.
- When the player touches this zone, the level is completed and the Victory screen appears!

---

## 8. Setting Camera Boundaries

To keep the game camera focused on your play area and avoid showing empty voids:
1. Click **[Camera Boundary]** in the Left Palette.
2. A large rectangular boundary box will appear in the viewport.
3. Drag the boundary edges or corners to enclose your entire level.
4. During gameplay, the camera will smoothly track the player but will never scroll past the edges of this box.

---

## 9. Testing, Saving, and Loading

### 9.1 Instant Playtesting
1. Click the **[Play Test]** button on the Top Bar.
2. The editor instantly launches the game! You can run, jump, land on platforms, trigger checkpoints, and test your puzzle mechanics.
3. When you are done testing, press **Escape** or click **[Return to Editor]** to immediately jump back into editing mode. All your objects and settings will remain exactly as you left them.

### 9.2 Saving Your Level
1. In the Top Bar, type a name for your level into the **Level Name** text field (e.g., `PuzzleStage_01`).
2. Click the **[Save]** button.
3. Your level will be saved as a `.json` file in the game's `Levels/` folder.

### 9.3 Loading an Existing Level
1. Type the name of the level you wish to open into the **Level Name** field.
2. Click the **[Load]** button.
3. The editor will clear the current workspace and load all platforms, trajectories, markers, and boundaries from the saved file.

### 9.4 Clearing the Workspace
- Click the **[Clear]** button to reset the level. This removes all custom platforms and checkpoints and restores the default Start, End, and Camera Boundary markers.

---

## 10. Pro Tips for Level Designers

1. **Test Rider Platforms Early:** Players can stand on moving platforms as long as the platform's color matches the player's color or is Universal Gold. Use Trajectory Preview to make sure moving platforms don't push the player into hazards or off-screen.
2. **Color Puzzles:** Use checkpoints strategically to change the player's color midway through a section, forcing them to rethink which platforms they can stand on.
3. **Safe Landings:** Always make sure the initial platform below the Start Spawn point is solid for the player's starting color, or they will fall straight to their death on frame 1!
4. **Grid Alignment:** Use the Inspector's numerical position fields to align platform edges perfectly for clean, seamless jumps.
