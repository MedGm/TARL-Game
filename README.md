# 🎓 TARL Number Game

**An Educational Math Adventure in Unity (with Firebase Integration)**  
Empowering primary school students through fun, engaging number challenges, while giving teachers real-time assessment tools.

![Gameplay Screenshot](https://github.com/user-attachments/assets/5b558ed4-6ca3-43b9-8ea4-568ee7ee936d)

---

## 🧩 Game Overview

**TARL Number Game** (Teach, Assess, Reinforce, Learn) is a Unity-powered educational game that teaches core math concepts through interactive gameplay. Students explore a 2D overworld, complete math puzzles in themed dungeons, and progress through levels of increasing difficulty. Teachers monitor progress and assign tests via Firebase integration.

---

## 🚀 Key Features

### 🧠 Gameplay Mechanics
- 🎮 **2D Dungeon Exploration** — Students move through 3 unique dungeons.
- 📈 **Progressive Difficulty** — Easy, Medium, and Hard levels with increasing complexity.
- 🧮 **Math Challenges**:
  - **Write Numbers in Words**
  - **Identify Place Values** (units, tens, hundreds, thousands)
  - **Decompose Numbers**

### 🎓 Learning Modes
- 🧪 **Test Mode** — QR-authenticated assessments, scores auto-saved.
- 🌍 **Free Roam Mode** — Sandbox learning with no pressure.

### 🔥 Firebase Integration
- 🔐 **QR Code Login** — Students log in via teacher-generated QR codes.
- 📊 **Progress Tracking** — Realtime scoring & analytics.
- 👨‍🏫 **Teacher Dashboard** — Monitor tests, results, and student behavior.
- 🧑‍🏫 **Multi-Class Support** — Handle different classrooms and teachers easily.

---

## 🎯 Learning Objectives

| Skill                        | Description                                          |
|-----------------------------|------------------------------------------------------|
| Number Recognition          | Identify and read numbers correctly                  |
| Place Value Understanding   | Understand digit positions and their meaning         |
| Number Decomposition        | Break numbers into simpler components                |
| Writing Numbers in Words    | Convert numbers to text (e.g., 125 → "one hundred twenty-five") |

### 📊 Difficulty Levels
- **Easy**: 3-digit numbers (100–999)
- **Medium**: 4-digit numbers (1000–4999)
- **Hard**: 5-digit numbers (5000–99999)

---

## 🏗️ Technical Architecture

### 🗂️ Unity Project Structure
```
Assets/
├── Scripts/
│   ├── GameManager.cs
│   ├── GameSessionManager.cs
│   ├── FirebaseDatabaseManager.cs
│   ├── QRCodeScanner.cs
│   ├── TitleScreenUI.cs
│   ├── SceneTransition.cs
│   └── Tasks/
│       ├── TerminalPuzzle.cs
│       ├── TerminalPuzzle2.cs
│       ├── TerminalPuzzle3.cs
│       ├── SoapBubbleTaskManager.cs
│       ├── SoapBubbleTaskManager2.cs
│       ├── SoapBubbleTaskManager3.cs
│       └── FinalSpellDecomposeTask.cs
├── Scenes/
│   ├── TitleScene
│   ├── overworldScene
│   ├── DungeonScene /2 /3
│   ├── SoapScene /2 /3
│   └── FinalSpellScene
└── Prefabs/
    ├── SceneTransition
    ├── QRCodeScanner
    └── UI Elements/
```

### 🔗 Firebase Database Structure
```json
{
  "users": {
    "student_id": {
      "firstName": "string",
      "lastName": "string",
      "role": "Student",
      "schoolGrade": "class_id",
      "linkedTeacherId": "teacher_id",
      "linkedSchoolId": "school_name",
      "password": "pin"
    }
  },
  "tests": {
    "test_id": {
      "idTeacher": "teacher_id",
      "title": {"fr": "Test Name"},
      "isActive": true,
      "isSent": true,
      "endDate": "date"
    }
  },
  "Answers": {
    "timestamp": {
      "studentId": "student_id",
      "gameId": "test_id",
      "idTeacher": "teacher_id",
      "totalScore": 0,
      "answers": {
        "findcomposition": {...},
        "WritetheFollowingNumberinLetters": {...},
        "IdentifthUnitsTensHundredsandThousands": {...}
      },
      "statistics": {
        "totalTimeSpent": 0,
        "totalAttemptsUsed": 0,
        "correctAnswersCount": 0,
        "incorrectAnswersCount": 0
      }
    }
  }
}
```

---

## ⚙️ Setup Instructions

### ✅ Prerequisites
- Unity **6** or newer
- Firebase Unity SDK
- **ZXing** library (QR scanning)
- TextMeshPro

### 📥 Installation
```bash
git clone [repository-url]
cd "TARL Game"
```

1. Open in Unity via Unity Hub
2. Import dependencies (Firebase, ZXing, TextMeshPro)
3. Configure Firebase project:
   - Download `google-services.json` (Android) or `GoogleService-Info.plist` (iOS)
   - Place it in `Assets/`
   - Enable Realtime Database with the rules below

### 🔧 Build Settings
Make sure the following scenes are added in this order:
```
TitleScene → overworldScene → DungeonScene(s) → SoapScene(s) → FinalSpellScene
```

---

## 🔐 Firebase Rules (Realtime Database)
```json
{
  "rules": {
    "users": { ".read": "auth != null", ".write": "auth != null" },
    "tests": { ".read": "auth != null", ".write": "auth != null" },
    "Answers": { ".read": "auth != null", ".write": "auth != null" },
    "classes": { ".read": "auth != null", ".write": "auth != null" }
  }
}
```

---

## 🎮 How to Play

### 👧 For Students
1. Scan QR code provided by teacher.
2. Enter overworld and choose dungeon (Easy → Medium → Hard).
3. Solve math puzzles and unlock next levels.
4. Complete place value tasks after each dungeon.
5. Reach and complete final number decomposition challenge.

### 👨‍🏫 For Teachers
1. Create and manage tests in Firebase.
2. Distribute QR codes for student access.
3. Track progress in real-time.
4. Review detailed results and statistics.

---

## 📱 Platform Support

- **Target**: Android (with camera support)
- **Development**: Ubuntu/Linux + Unity Editor

---

## 🔐 Security Features

- QR Code-based secure logins
- PIN verification for extra protection
- Firebase-backed user authentication
- Auto logout/session cleanup

---

## 📊 Assessment & Analytics

### Real-Time Scoring
- Scores update instantly
- Tracks time, attempts, and accuracy
- Max 3 tries per task

### Analytics Dashboard
- Task completion stats
- Average time per level
- Mistake patterns
- Overall performance breakdown

---

## 🎨 UI/UX Highlights

- ⚡ Smooth scene transitions
- 📱 Mobile-friendly UI
- 🇫🇷 French localization
- ✨ Visual feedback and animations
- 🚥 Progress bars and checkpoints

---

## 🧪 Dev Notes & Debugging

### Debug Tips
```csharp
// Log messages
Debug.Log("[Component] Debug message");

// Free roam test
GameSessionManager.Instance.StartFreeRoamMode();

// Firebase test
FirebaseDatabaseManager.Instance.IsFirebaseReady();
```

### Common Issues
| Problem                     | Solution                                                      |
|----------------------------|---------------------------------------------------------------|
| QR scanner not working     | Check camera permissions & lighting, validate QR format       |
| Firebase connection error  | Ensure network access, check credentials & rules              |
| Scene not loading          | Confirm scene is in Build Settings and named correctly        |

---

## 📄 License

This game is built for educational use.

---

## 🤝 Contributions

Want to help or report a bug?  
Feel free to open an issue or contact me directly!

---

**Version**: `1.0.0`  
**Last Updated**: *June 2025*  
**Unity Version**: `6 or Later`  
**Firebase SDK**: Latest Stable
