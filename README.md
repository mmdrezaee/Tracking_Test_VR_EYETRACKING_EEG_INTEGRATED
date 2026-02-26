## Tracking Test (VR + Eye Tracking + EEG/LSL) — Unity Project

Unity VR experiment project that runs a **VR target-tracking and sound-localization game**, plays a scheduled audio stimulus, collects VR controller/joystick responses, and logs:

- **Trials / responses** (correct/incorrect/missed, reaction times for sound direction)
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

## One build, many configurations (Experiment folder)

You can run **one build** and select **Subject (1–20)** and **Session (1–24)**. The app loads the matching CSV and Mixed WAV from an external **Experiment** folder.

1. **Experiment folder layout** (e.g. `C:\Users\...\Experiment`):
   - `Subject_01/` … `Subject_20/`
   - In each: `SubjXX_SessYY_alert_info.csv` and `SubjXX_SessYY_*_Mixed.wav` (e.g. `Subj01_Sess01_N0.846_mode1_mode2_rep2_Mixed.wav`).

2. **In the scene**:
   - Add (or use) a GameObject with **ExperimentSessionConfigurator**.
   - Assign **experiment** to your `SimpleExperimentVR` component.
   - Set **Experiment Root Path** to the Experiment folder (e.g. `C:\Users\rezaeia2\Desktop\Tracking Test Project\Experiment`).
   - Set **Subject** (1–20) and **Session** (1–24).
   - On the `SimpleExperimentVR` component, enable **Use External Session Config** (and leave **Alert Schedule Csv** / **Combined Stimulus Clip** unassigned for that mode).

3. Press Play: the configurator loads the CSV and the matching Mixed WAV, injects them into the experiment, and starts the run. Outputs go to `ExperimentOutputs/<timestamp>_PXX_SessYY/`.

### EEG baseline (Session 0)

At the start of each subject’s data collection, run a **5-minute EEG baseline** with no task:

- Set **Session = 0** (and **Subject** as the participant, e.g. 1–20).
- The app shows only a **fixation cross**; participants wear the VR headset, fixate on the cross, refrain from speaking, and minimize movement.
- **EEG** (and optionally **eye tracking**) are recorded for 5 minutes into a session folder named `..._PXX_Baseline/` (e.g. `ExperimentOutputs/<timestamp>_P01_Baseline/`). No audio or trials run.
- After 5 minutes the app closes (or shows a completion message then quits).

**Scene setup:** Add a GameObject with the **BaselineRunner** component. Assign it to **ExperimentSessionConfigurator → Baseline Runner**. You can leave **Fixation Cross Root** empty and a simple “+” will be created at runtime, or assign your own Canvas/UI. Baseline duration is 300 s by default (editable in BaselineRunner).

**Recommended order per subject:** Run Session 0 (baseline) once, then run Session 1 … 24 for the main task.

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

### VR target-tracking task (joystick / controller)

The main task is a **continuous target-tracking game** where the participant tracks a moving target using a **VR joystick / controller thumbstick**, while also responding to sound direction.

Relevant scripts (non-exhaustive):
- `Assets/TrackingCsvLogger2D.cs`: logs 2D tracking-board target and cursor positions and error to `Tracking2D.csv`
- `Assets/Scripts/Experiment/RayTrackingCsvLogger.cs`: logs VR ray / target positions and on-target state to `RayTracking.csv`
- `Assets/Scripts/Experiment/ViveThumbstickResponseInput.cs`: reads XR controller input (thumbstick/joystick) and forwards responses to `SimpleExperimentVR`

These use the same experiment timebase (`experiment.AudioTime`) so tracking, responses, and audio timing are aligned.

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

### VR tracking task screenshot

Once you have a build or are running in the Unity Editor, you can capture a screenshot of the tracking game and add it to the repo, for example at:

- `docs/vr-tracking-task.png`

Then reference it here:

```markdown
![VR tracking task](docs/vr-tracking-task.png)
```

This README cannot capture a screenshot automatically; you need to run the scene and save the image from your own machine.

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

**Local / file packages** (you must obtain the `.tgz` files):

- **com.varjo.xr** (Varjo XR Unity SDK) — required for Varjo eye tracking and the iMotions Varjo integration. The manifest expects `file:com.varjo.xr-3.5.0.tgz`.
  - Get the Varjo XR package (e.g. from [Varjo Developer](https://developer.varjo.com/) or from your lab’s copy).
  - Place `com.varjo.xr-3.5.0.tgz` in the project’s **Packages** folder (next to `manifest.json`), or put it elsewhere and set the path in `manifest.json` (e.g. `"com.varjo.xr": "file:C:/path/to/com.varjo.xr-3.5.0.tgz"`).
- **com.threespacelab.imotions.varjointegration** and **com.threespacelab.imotions.core** — iMotions Varjo integration; paths are set in `manifest.json` (local tarball or PackageCache path).

**AR Foundation** is included from the Unity registry (`com.unity.xr.arfoundation` 4.2.10) for the iMotions mixed-reality components.

If you clone this onto a new machine, resolve any missing `file:` packages by obtaining the `.tgz` files and placing them where `manifest.json` points, or by updating the paths in `manifest.json`.

---

## Repo structure (quick map)

- `Assets/`: Unity scenes, prefabs, scripts, plugins (Varjo, LSL, etc.)
- `Packages/`: Unity Package Manager manifests
- `ProjectSettings/`: Unity settings including build scenes and XR settings
- `ExperimentOutputs/`: generated session outputs (CSV logs)

