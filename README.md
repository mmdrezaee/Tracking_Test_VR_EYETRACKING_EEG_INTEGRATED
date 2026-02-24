## Tracking Test (VR + Eye Tracking + EEG/LSL) — Unity Project

Unity VR experiment project that plays a scheduled audio stimulus, collects VR controller responses, and logs:

- **Trials / responses** (correct/incorrect/missed, reaction times)
- **Varjo eye tracking** (gaze ray + pupil/openness; optional gaze raycast hit)
- **EEG via Lab Streaming Layer (LSL)** (LSL timestamps + channel samples)
- (Optional) additional tracking logs (e.g., ray / 2D tracking board)

Outputs are written to per-session folders under `ExperimentOutputs/` as CSV.

---

## Requirements

- **Unity Editor**: `2021.3.27f1` (see `ProjectSettings/ProjectVersion.txt`)
- **Git**: required by Unity Package Manager to fetch LSL packages
- **Hardware / runtimes (depending on what you use)**
  - Varjo headset + Varjo XR runtime (for `Varjo.XR` eye tracking)
  - An EEG app/device that publishes an **LSL stream** (this project reads by **stream name**)

---

## Open / run the project

1. Install Unity `2021.3.27f1` in Unity Hub.
2. Open this folder as a Unity project.
3. Open the main scene (the enabled build scene is):
   - `Assets/Samples/3SL_ iMotions Varjo Integration/2.2.0/HDRP Samples/HDRPExample-iMotions-Varjo (controllers).unity`
   - (Build scene list is in `ProjectSettings/EditorBuildSettings.asset`)
4. Press **Play** (or build via **File → Build Settings…**).

---

## Configure an experiment run (typical)

### `SimpleExperimentVR` (experiment driver)

Core logic lives in `Assets/Scripts/Experiment/SimpleExperimentVR.cs`.

In the scene, find the GameObject with `SimpleExperimentVR` and set:

- **Session**
  - `participantId` (e.g., `P01`)
  - `conditionName` (e.g., `ConditionA`)
- **Audio**
  - `stimulusSource` (AudioSource)
  - `combinedStimulusClip` (AudioClip)
- **CSV Input (trials)**
  - `alertScheduleCsv` (TextAsset)
- **Trial timing**
  - `responseWindowSeconds`, etc.

The project uses a stable shared timebase: `experiment.AudioTime` (DSP-clock aligned to the scheduled audio start) so logs can be aligned.

### Eye tracking logger (Varjo)

`Assets/VarjoEyeTrackingCsvLogger.cs` writes `EyeTracking.csv`.

Typical settings:
- Assign the `experiment` field (optional; enables using the experiment timebase).
- Enable `doWorldHitTest` if you want gaze ray → world hit logging.

### EEG logger (LSL inlet)

`Assets/LslCsvLogger.cs` writes `LSL_EEG.csv`.

Important setting:
- `StreamName`: must match the **LSL stream name** being broadcast by your EEG system.

---

## Data output

### Where files go

Session folders are created by `Assets/Scripts/Experiment/ExperimentPaths.cs`:

- **In Editor**: `<repo>/ExperimentOutputs/<UTCtimestamp>_<participant>_<condition>/`
- **In a build**: `<buildFolder>/ExperimentOutputs/<session>/` (with fallback to `Application.persistentDataPath`)

### Common CSVs

- `Trials.csv`: per-trial results (see `Assets/Scripts/Experiment/TrialCsvLogger.cs`)
- `EyeTracking.csv`: Varjo gaze samples (see `Assets/VarjoEyeTrackingCsvLogger.cs`)
- `LSL_EEG.csv`: EEG samples pulled from LSL (see `Assets/LslCsvLogger.cs`)

---

## Packages / dependencies (important note for GitHub clones)

Unity packages are declared in `Packages/manifest.json`.

This repo includes git-based dependencies (portable):

- `com.labstreaminglayer.lsl4unity`: `https://github.com/labstreaminglayer/LSL4Unity.git`
- `com.bci4kids.bciessentials`: `https://github.com/kirtonBCIlab/bci-essentials-unity.git`

However, it also currently references **machine-specific local `.tgz` files** (not included in this repo), e.g.:

- `com.threespacelab.imotions.varjointegration`: `file:C:/Users/labadmin/Desktop/...`
- `com.threespacelab.imotions.core` and `com.varjo.xr`: `file:../Library/PackageCache/...`

If you clone this from GitHub onto a new machine, Unity may fail to resolve these `file:` dependencies until you:

- obtain those `.tgz` packages and update `Packages/manifest.json` to point to valid paths, **or**
- replace them with an alternative distribution method (registry/git) that your team uses for Varjo/iMotions packages.

---

## Repo structure (quick map)

- `Assets/`: Unity scenes, prefabs, scripts, plugins (Varjo, LSL, etc.)
- `Packages/`: Unity Package Manager manifests
- `ProjectSettings/`: Unity settings including build scenes and XR settings
- `ExperimentOutputs/`: generated session outputs (CSV logs)

