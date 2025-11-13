# BIRDS: Bi-directional Interaction for Referential Dialogue in Spatial Contexts for Virtual Reality

<img src="img/BIRDS_Teaser_Wide.png" alt="Teaser" style="width:100%; height:auto;">


## Abstract
Embodied conversational agents in Virtual Reality (VR) are increasingly powered by large language models (LLMs) to enable natural language interaction, with most prior work remaining unidirectional by using gaze, gesture, and speech as inputs from the user to the agent. However, bi-directional multimodal communication where both users and agents use these modalities remains largely unexplored. To address this gap, we present BIRDS, a system for bi-directional referential dialogue with an LLM-driven agent in VR. In a controlled study (N = 24), we found that agent gestures yielded the greatest benefits by reducing conversational turns and time while improving accuracy, trust, and user experience. Interestingly, gaze input alone had limited impact, but when combined with gestures, it produced the highest accuracy.  This suggests that the value of multimodality lies not only in efficiency gains but also in how non-verbal reciprocity shapes trust, social presence, and the perceived competence of embodied agents.

## System Overview

<img src="img/design_system_overview.png" alt="Teaser" style="width:70%; height:auto;">

### System Prompts

Each task defines a set of system prompts that specify the avatar’s functions and the meaning of the input/output JSON schema.

Prompt definitions can be found in:

* **Task 1:**
  `Assets/Resources/LLMPrompt/1_GatherItemGamePrompt/`
* **Task 2:**
  `Assets/Resources/LLMPrompt/2_BlockPuzzleGamePrompt/`

### JSON Examples

Example JSON files from LLM communication:

* `data_examples/`

These include representative input/output formats for both tasks.


## Install Unity3D Engine

1. Visit **Unity Hub** → *Installs*.
2. Click **Add** → search for version `6000.0.39f1`.  
   If it does not appear:
   - Open **Unity Download Archive**:  
     https://unity.com/releases/editor/archive
   - Navigate to **6000.x** versions.
   - Select **6000.0.39f1** and install it via Hub.
   Please ensure that your local Unity installation matches this version to avoid package or API incompatibilities.
3. After installation, open the BIRDS project from Unity Hub.

## OpenAI Authentication

BIRDS relies on an LLM backend implemented through
**[com.openai.unity](https://github.com/RageAgainstThePixel/com.openai.unity)**.
To access OpenAI services, you must create a valid authentication file.

### 1. Prepare `auth.json`

Create a file named **`auth.json`** that contains your OpenAI credentials:

```json
{
    "apiKey": "sk-proj-aaaaaaaaaaaaaaaaaaaaaaa",
    "organizationId": "org-aaaaaaaaaaaaaaaaaaaaaa",
    "projectId": "proj_aaaaaaaaaaaaaaaaaaaa"
}
```

⚠️ **Keep this file private. Never commit it to GitHub or share it publicly.**
We strongly recommend adding `.openai/` and `auth.json` to `.gitignore`.

---

### 2. Saving Location

The correct save path depends on the runtime platform:

| Platform             | Path                                        |
| -------------------- | ------------------------------------------- |
| **Unity Editor**     | `C:\Users\<YourUsername>\.openai\auth.json` |
| **Standalone Build** | `Application.persistentDataPath/auth.json`  |

This separation ensures that your API key remains local during development while allowing secure credential loading in release builds.

---

### 3. Loading Authentication in Code

Example usage from
**[`LLMAPI.cs`](Assets/LLM/Scripts/Common/OpenAI/LLMAPI.cs)**:

```csharp
// Determine the auth file path based on platform
string authPath;

#if UNITY_ANDROID && !UNITY_EDITOR
    // On Android builds, use persistentDataPath (copy file there before build)
    authPath = Path.Combine(Application.persistentDataPath, "auth.json");
#else
    // On Editor / Standalone (Windows/Mac), use the user's home directory
    var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    authPath = Path.Combine(userPath, ".openai", "auth.json");
#endif

// Create OpenAI client
openAI = new OpenAIClient(new OpenAIAuthentication().LoadFromPath(authPath))
{
    EnableDebug = enableDebug  // Optional debugging logs
};
```

### Notes

* If your standalone app cannot find `auth.json`, ensure the file has been copied to `Application.persistentDataPath` before the first run.


## Connect to Headset and Run Application

To run BIRDS in VR, connect your **Meta Quest Pro** to your PC and run the Unity scenes inside the headset.

### 1. Install Meta Quest Link Software

Download and install **Meta Quest Link** (formerly Oculus PC app):

👉 [https://www.meta.com/quest/setup/](https://www.meta.com/quest/setup/)

This software enables USB Link / Air Link streaming between your PC and the Quest Pro.

### 2. Connect Quest Pro to the PC

You can connect using either:

#### **Option A — Meta Link (USB-C, recommended)**

1. Use a high-quality USB-C cable.
2. Plug into your PC and grant permissions inside the headset.
3. In the Meta PC app, select **Devices → Quest Pro → Enable Link**.

This provides the most stable performance and lowest latency.

#### **Option B — Air Link (Wireless)**

1. Ensure both PC and Quest Pro are on the same **5GHz Wi-Fi** network.
2. On the headset:
   **Quick Settings → Meta Quest Link → Enable Air Link**
3. Pair and connect to your PC.

This enables wireless testing but may introduce latency.

---

### 3. Run the Unity Application in VR

Once the Quest Pro is connected via Link:

1. Open the BIRDS project in **Unity 6000.0.39f1**.
2. Ensure **OpenXR** and **Quest Link** are correctly enabled (Unity usually detects this automatically).
3. Click **Play** in Unity.
   The VR view will stream directly into your Quest Pro.

---

### 4. Available Demo Scenes

You may test the system using the following scenes:

* **Tutorial - How to interact with the avatar**
  `Assets/LLM/Scenes/L_LLMScene_0_Tutorial.unity`

* **Task 1 – User instructs avatar to collect items**
  `Assets/LLM/Scenes/L_LLMScene_1_GatherItem.unity`

* **Task 2 – Avatar instructs user to place items**
  `Assets/LLM/Scenes/L_LLMScene_2_BlockPuzzle.unity`

Simply open a scene and press **Play** to begin testing.

---

## Additional Notes

This repository is provided **only for the anonymous review process**.
A cleaned, public, and fully documented version of the BIRDS codebase will be released after the review period concludes.
