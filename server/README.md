# Unity AI Feedback Laboratory — Render Backend

## 1. What This Backend Does

This Node.js/Express server acts as the secure bridge between your Unity game and the OpenAI API.

```
Unity (game events)
  ↓ HTTPS JSON POST
Render Backend  (this server)
  ↓
OpenAI Responses API
  ↓
Backend validates & sanitises the AI response
  ↓
Unity receives structured, safe feedback JSON
  ↓
Unity Action Router executes only approved actions
```

Key responsibilities:
- Accepts gameplay event JSON from Unity
- Forwards context to OpenAI with a constrained coaching system prompt
- Validates that the AI only returns actions from a predefined allowlist
- Returns structured feedback Unity can safely act on
- Supports **mock mode** for testing without an OpenAI key

---

## 2. Folder Structure

```
server/
├── server.js       # Main Express server
├── package.json    # Dependencies and start script
├── .gitignore      # Excludes node_modules/ and .env
└── README.md       # This file
```

---

## 3. Local Installation

Requirements: **Node.js 18+**

```bash
cd server
npm install
```

---

## 4. How to Run the Server

### Mock mode (no OpenAI key needed)

```bash
# Windows PowerShell
$env:MOCK_AI="true"; node server.js

# macOS / Linux
MOCK_AI=true node server.js
```

### Real mode (requires OpenAI key in environment)

```bash
# Windows PowerShell
$env:OPENAI_API_KEY="sk-..."; node server.js

# macOS / Linux
OPENAI_API_KEY=sk-... node server.js
```

> **Never hardcode the API key in server.js or any source file.**

---

## 5. How MOCK_AI Works

When `MOCK_AI=true`, the `/api/feedback` endpoint skips the OpenAI call entirely and returns a fixed deterministic response:

```json
{
  "message": "Your jump timing appears late. Try starting the jump slightly earlier.",
  "feedbackType": "hint",
  "action": {
    "type": "highlight_safe_route",
    "target": "next_platform",
    "value": "1"
  },
  "priority": "normal",
  "cooldownSeconds": 20
}
```

**Testing sequence (recommended):**
1. Test with `MOCK_AI=true` — verifies Unity ↔ Render communication
2. Verify Unity sends JSON correctly
3. Verify Unity displays the response
4. Add `OPENAI_API_KEY` to Render environment variables
5. Set `MOCK_AI=false` (or remove it) — enables real AI feedback
6. Test real coaching responses
7. Verify action allowlist is enforced

---

## 6. Test GET /api/health

**curl:**
```bash
curl http://localhost:3000/api/health
```

**PowerShell:**
```powershell
Invoke-RestMethod -Uri "http://localhost:3000/api/health" -Method Get
```

**Expected response:**
```json
{
  "status": "ok",
  "service": "unity-ai-feedback",
  "mockMode": true,
  "model": "mock",
  "timestamp": "2026-09-22T06:00:00.000Z"
}
```

---

## 7. Test POST /api/feedback

**curl:**
```bash
curl -X POST http://localhost:3000/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
    "projectType": "2d_side_scroller_game",
    "sessionId": "student-demo-001",
    "eventType": "repeated_failure",
    "occurredAtUtc": "2026-09-22T00:00:00Z",
    "context": {
      "levelId": "forest_run_01",
      "currentSection": "moving_platforms",
      "gestureAttempts": 5,
      "consecutiveFailures": 3,
      "lastFailureReason": "late_jump",
      "userQuestion": "Why do I keep missing this jump?"
    }
  }'
```

**PowerShell:**
```powershell
$body = @{
    projectType = "2d_side_scroller_game"
    sessionId   = "student-demo-001"
    eventType   = "repeated_failure"
    occurredAtUtc = "2026-09-22T00:00:00Z"
    context = @{
        levelId            = "forest_run_01"
        currentSection     = "moving_platforms"
        gestureAttempts    = 5
        consecutiveFailures = 3
        lastFailureReason  = "late_jump"
        userQuestion       = "Why do I keep missing this jump?"
    }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "http://localhost:3000/api/feedback" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

**Expected response (mock mode):**
```json
{
  "requestId": "req-1234567890-0001",
  "sessionId": "student-demo-001",
  "message": "Your jump timing appears late. Try starting the jump slightly earlier.",
  "feedbackType": "hint",
  "action": {
    "type": "highlight_safe_route",
    "target": "next_platform",
    "value": "1"
  },
  "priority": "normal",
  "cooldownSeconds": 20
}
```

---

## 8. How to Configure Render

1. Go to [https://render.com](https://render.com) and sign in.
2. Click **New → Web Service**.
3. Connect your GitHub repository.
4. Set **Root Directory** to `server`.
5. Set **Build Command** to `npm install`.
6. Set **Start Command** to `npm start`.
7. Set **Environment** to `Node`.

---

## 9. How to Add OPENAI_API_KEY to Render

1. In your Render service, go to **Environment** tab.
2. Click **Add Environment Variable**.
3. Add the following variables:

| Key | Value |
|-----|-------|
| `OPENAI_API_KEY` | Your OpenAI secret key (starts with `sk-`) |
| `OPENAI_MODEL` | `gpt-4o-mini` (or your preferred model) |
| `MOCK_AI` | `false` (after testing; set `true` for mock mode) |
| `MAX_REQUESTS_PER_MINUTE` | `30` |
| `UNITY_CLIENT_TOKEN` | Optional shared secret for Unity requests |

> **Never paste the API key into any source file or commit it to GitHub.**
> The Render dashboard is the only place it should live.

---

## 10. How to Deploy on Render

1. Push the `server/` directory to your GitHub repository.
2. Render auto-deploys on every push to your main branch.
3. Your service URL will be: `https://your-service-name.onrender.com`
4. Test health: `GET https://your-service-name.onrender.com/api/health`
5. Set `MOCK_AI=false` and add your `OPENAI_API_KEY` when ready for real AI.

---

## 11. How Unity Communicates With This Backend

Unity will send HTTP POST requests to `/api/feedback` with gameplay event JSON.

**Required fields:**
- `projectType` — identifies your game type
- `sessionId` — unique session/student identifier
- `eventType` — what happened (e.g. `repeated_failure`, `level_complete`)
- `context` — object containing gameplay details

Unity receives structured JSON back and routes only the `action.type` through a local allowlist before executing anything.

**The Unity Action Router will only execute these action types:**
`show_control_hint`, `play_gesture_demo`, `highlight_safe_route`, `preview_jump_arc`,
`adjust_gesture_tolerance`, `reduce_game_speed`, `activate_practice_mode`,
`restore_normal_difficulty`, `recommend_next_strategy`, `show_run_summary`, `none`

---

## Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `OPENAI_API_KEY` | Yes (real mode) | — | OpenAI secret key. **Render only.** |
| `OPENAI_MODEL` | No | `gpt-4o-mini` | OpenAI model name |
| `MOCK_AI` | No | `false` | Set `true` to skip OpenAI |
| `MAX_REQUESTS_PER_MINUTE` | No | `30` | Per-IP rate limit |
| `UNITY_CLIENT_TOKEN` | No | — | Shared secret for Unity auth |
| `PORT` | No | `3000` | HTTP port (Render sets automatically) |
