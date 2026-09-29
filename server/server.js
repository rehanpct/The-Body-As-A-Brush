const express = require("express");
const dotenv = require("dotenv");
const OpenAI = require("openai");
const crypto = require("crypto");

dotenv.config();

const app = express();

// ============================================================
// LIVE DEBUG DATA
// Development-only: stores the latest Unity request and
// latest OpenAI-generated response in memory.
// ============================================================

let latestUnityPayload = null;
let latestAIResponse = null;

// ============================================================
// CONFIGURATION
// ============================================================

const CONFIG = {
  port: process.env.PORT || 3000,

  openaiApiKey:
    process.env.OPENAI_API_KEY || "",

  openaiModel:
    process.env.OPENAI_MODEL || "gpt-4o-mini",

  mockAI:
    String(process.env.MOCK_AI || "false").toLowerCase() === "true",

  rateLimitPerMinute:
    Number(process.env.RATE_LIMIT_PER_MINUTE || 30)
};

// ============================================================
// OPENAI CLIENT
// ============================================================

const client = CONFIG.openaiApiKey
  ? new OpenAI({
      apiKey: CONFIG.openaiApiKey
    })
  : null;

// ============================================================
// SYSTEM PROMPT
// ============================================================

const SYSTEM_PROMPT = `
You are the AI creative assistant for "The Body as a Brush",
a gesture-based interactive artwork experience.

IMPORTANT:
This is NOT a platform game.
This is NOT a combat game.
This is NOT a jumping game.

The user creates artwork using body gestures.

The artwork can contain:
- fish schools
- coral
- bubbles
- water currents
- underwater elements
- Taiwan lanterns
- other visual composition elements

Your job is to observe the artwork and provide useful,
positive and concise creative feedback.

Focus on:
- composition
- balance
- visual density
- empty space
- clustering
- scale
- distribution
- color suggestions
- creative progress
- encouragement

Do not criticize the user harshly.

Do not control Unity directly.

Only suggest safe, predefined actions.

Valid feedback types:
- hint
- encouragement
- summary
- strategy
- warning

Valid actions:
- none
- highlight_tool
- change_brush
- suggest_color
- spawn_preview
- adjust_brush_size
- play_gesture_demo
- undo_last_action
- show_composition_hint
- add_animation
- show_summary

For most feedback, use:
action.type = "none"

Only suggest another action when it is genuinely useful.

Return JSON in this structure:

{
  "message": "short helpful feedback",
  "feedbackType": "hint",
  "action": {
    "type": "none",
    "target": "",
    "value": ""
  },
  "priority": "normal",
  "cooldownSeconds": 20
}

Keep the message concise.
`;

// ============================================================
// MIDDLEWARE
// ============================================================

app.use(
  express.json({
    limit: "1mb"
  })
);

// ============================================================
// RATE LIMITING
// ============================================================

const requestHistory = new Map();

function checkRateLimit(sessionId) {
  const now = Date.now();

  const existing =
    requestHistory.get(sessionId) || [];

  const recent =
    existing.filter(
      timestamp =>
        now - timestamp < 60 * 1000
    );

  if (
    recent.length >=
    CONFIG.rateLimitPerMinute
  ) {
    requestHistory.set(
      sessionId,
      recent
    );

    return false;
  }

  recent.push(now);

  requestHistory.set(
    sessionId,
    recent
  );

  return true;
}

// ============================================================
// HELPERS
// ============================================================

function generateRequestId() {
  return `req-${Date.now()}-${crypto
    .randomBytes(3)
    .toString("hex")}`;
}

function normalizeFeedback(feedback) {
  const validFeedbackTypes = [
    "hint",
    "encouragement",
    "summary",
    "strategy",
    "warning"
  ];

  const validActions = [
    "none",
    "highlight_tool",
    "change_brush",
    "suggest_color",
    "spawn_preview",
    "adjust_brush_size",
    "play_gesture_demo",
    "undo_last_action",
    "show_composition_hint",
    "add_animation",
    "show_summary"
  ];

  const result = {
    message:
      typeof feedback?.message === "string"
        ? feedback.message
        : "Keep exploring your composition.",

    feedbackType:
      validFeedbackTypes.includes(
        feedback?.feedbackType
      )
        ? feedback.feedbackType
        : "encouragement",

    action: {
      type:
        validActions.includes(
          feedback?.action?.type
        )
          ? feedback.action.type
          : "none",

      target:
        typeof feedback?.action?.target === "string"
          ? feedback.action.target
          : "",

      value:
        typeof feedback?.action?.value === "string"
          ? feedback.action.value
          : ""
    },

    priority:
      feedback?.priority === "high" ||
      feedback?.priority === "low"
        ? feedback.priority
        : "normal",

    cooldownSeconds:
      Number.isFinite(
        Number(feedback?.cooldownSeconds)
      )
        ? Number(feedback.cooldownSeconds)
        : 20
  };

  return result;
}

// ============================================================
// MOCK AI
// ============================================================

function generateMockFeedback(body) {
  const eventType =
    body?.eventType || "";

  if (eventType === "object_added") {
    return {
      message:
        "Nice addition! Your new element is bringing more life and movement into the artwork.",

      feedbackType:
        "encouragement",

      action: {
        type: "none",
        target: "",
        value: ""
      },

      priority: "normal",

      cooldownSeconds: 20
    };
  }

  if (eventType === "composition_update") {
    return {
      message:
        "Your composition is developing nicely. Consider balancing the open space with another visual element.",

      feedbackType:
        "hint",

      action: {
        type: "show_composition_hint",
        target: "",
        value: ""
      },

      priority: "normal",

      cooldownSeconds: 20
    };
  }

  return {
    message:
      "Your artwork is developing nicely. Keep experimenting with your composition.",

    feedbackType:
      "encouragement",

    action: {
      type: "none",
      target: "",
      value: ""
    },

    priority: "normal",

    cooldownSeconds: 20
  };
}

// ============================================================
// OPENAI FEEDBACK
// ============================================================

async function generateAIFeedback(body) {
  if (CONFIG.mockAI) {
    return generateMockFeedback(body);
  }

  if (!client) {
    throw new Error(
      "OPENAI_API_KEY is not configured."
    );
  }

  const userContent = JSON.stringify(
    {
      eventType:
        body.eventType,

      sessionId:
        body.sessionId,

      occurredAtUtc:
        body.occurredAtUtc,

      context:
        body.context || {}
    },
    null,
    2
  );

  const response =
    await client.responses.create({
      model:
        CONFIG.openaiModel,

      instructions:
        SYSTEM_PROMPT,

      input:
        userContent
    });

  const outputText =
    response.output_text ||
    "";

  let parsed;

  try {
    parsed =
      JSON.parse(outputText);
  } catch (error) {
    console.error(
      "OpenAI returned non-JSON output:",
      outputText
    );

    parsed = {
      message:
        outputText ||
        "Keep exploring your artwork.",

      feedbackType:
        "encouragement",

      action: {
        type: "none",
        target: "",
        value: ""
      },

      priority:
        "normal",

      cooldownSeconds:
        20
    };
  }

  return normalizeFeedback(parsed);
}

// ============================================================
// HEALTH ENDPOINT
// ============================================================

app.get(
  "/api/health",
  (req, res) => {
    return res.status(200).json({
      status: "ok",

      service:
        "the-body-as-a-brush",

      timestamp:
        new Date().toISOString(),

      mockMode:
        CONFIG.mockAI,

      openaiConfigured:
        Boolean(CONFIG.openaiApiKey),

      model:
        CONFIG.openaiModel
    });
  }
);

// ============================================================
// DEBUG — LATEST UNITY + OPENAI DATA
// ============================================================
//
// Open in Chrome:
//
// https://the-body-as-a-brush.onrender.com/api/debug/latest
//
// This shows:
//
// Unity
//   ↓
// Render
//   ↓
// OpenAI
//   ↓
// Render
//
// The data is stored only in server memory.
// It disappears when Render restarts/redeploys.
//
// ============================================================

app.get(
  "/api/debug/latest",
  (req, res) => {

    if (
      !latestUnityPayload &&
      !latestAIResponse
    ) {
      return res
        .status(404)
        .json({
          message:
            "No Unity/OpenAI transaction received yet."
        });
    }

    return res
      .status(200)
      .json({
        unityToRender:
          latestUnityPayload,

        renderToUnity:
          latestAIResponse
      });
  }
);

// ============================================================
// FEEDBACK ENDPOINT
// ============================================================

app.post(
  "/api/feedback",
  async (req, res) => {

    const requestStartedAt =
      Date.now();

    const requestId =
      generateRequestId();

    try {

      // --------------------------------------------------------
      // BODY
      // --------------------------------------------------------

      const body =
        req.body;

      // --------------------------------------------------------
      // LIVE DEBUG — STORE LATEST UNITY PAYLOAD
      // --------------------------------------------------------

      latestUnityPayload = {
        receivedAt:
          new Date().toISOString(),

        ...body
      };

      // --------------------------------------------------------
      // BASIC VALIDATION
      // --------------------------------------------------------

      if (
        !body ||
        typeof body !== "object"
      ) {
        return res
          .status(400)
          .json({
            error:
              "Request body must be a JSON object."
          });
      }

      if (
        body.projectType !==
        "gesture_painting"
      ) {
        return res
          .status(400)
          .json({
            error:
              "Invalid projectType."
          });
      }

      if (
        !body.sessionId ||
        typeof body.sessionId !==
          "string"
      ) {
        return res
          .status(400)
          .json({
            error:
              "sessionId is required."
          });
      }

      if (
        !body.eventType ||
        typeof body.eventType !==
          "string"
      ) {
        return res
          .status(400)
          .json({
            error:
              "eventType is required."
          });
      }

      // --------------------------------------------------------
      // RATE LIMIT
      // --------------------------------------------------------

      if (
        !checkRateLimit(
          body.sessionId
        )
      ) {
        return res
          .status(429)
          .json({
            error:
              "Rate limit exceeded. Please wait before sending another request.",

            requestId,

            sessionId:
              body.sessionId
          });
      }

      // --------------------------------------------------------
      // LOG UNITY REQUEST
      // --------------------------------------------------------

      console.log(
        "\n============================================================"
      );

      console.log(
        "[FEEDBACK] Unity request received"
      );

      console.log(
        "Request ID:",
        requestId
      );

      console.log(
        "Session ID:",
        body.sessionId
      );

      console.log(
        "Event Type:",
        body.eventType
      );

      console.log(
        "Unity Payload:"
      );

      console.log(
        JSON.stringify(
          body,
          null,
          2
        )
      );

      // --------------------------------------------------------
      // GENERATE AI FEEDBACK
      // --------------------------------------------------------

      const feedback =
        await generateAIFeedback(
          body
        );

      // --------------------------------------------------------
      // LIVE DEBUG — STORE LATEST OPENAI RESPONSE
      // --------------------------------------------------------

      latestAIResponse = {
        receivedAt:
          new Date().toISOString(),

        requestId,

        sessionId:
          body.sessionId,

        eventType:
          body.eventType,

        feedback
      };

      // --------------------------------------------------------
      // LATENCY
      // --------------------------------------------------------

      const latencyMs =
        Date.now() -
        requestStartedAt;

      // --------------------------------------------------------
      // LOG AI RESPONSE
      // --------------------------------------------------------

      console.log(
        "\n[FEEDBACK] AI response generated"
      );

      console.log(
        "Request ID:",
        requestId
      );

      console.log(
        "Latency:",
        `${latencyMs} ms`
      );

      console.log(
        "AI Response:"
      );

      console.log(
        JSON.stringify(
          feedback,
          null,
          2
        )
      );

      console.log(
        "============================================================\n"
      );

      // --------------------------------------------------------
      // RESPONSE TO UNITY
      // --------------------------------------------------------

      return res
        .status(200)
        .json({
          requestId,

          sessionId:
            body.sessionId,

          message:
            feedback.message,

          feedbackType:
            feedback.feedbackType,

          action:
            feedback.action,

          priority:
            feedback.priority,

          cooldownSeconds:
            feedback.cooldownSeconds
        });

    } catch (error) {

      console.error(
        "\n[FEEDBACK] ERROR"
      );

      console.error(
        error
      );

      return res
        .status(500)
        .json({
          error:
            "Failed to generate AI feedback.",

          requestId,

          details:
            error?.message ||
            "Unknown error."
        });
    }
  }
);

// ============================================================
// ROOT
// ============================================================

app.get(
  "/",
  (req, res) => {
    res.status(200).send(
      `
      <!DOCTYPE html>
      <html>
      <head>
        <title>The Body as a Brush</title>
        <style>
          body {
            font-family: Arial, sans-serif;
            background: #0d1726;
            color: white;
            padding: 40px;
          }

          h1 {
            margin-bottom: 10px;
          }

          .card {
            background: #162235;
            padding: 20px;
            border-radius: 12px;
            max-width: 700px;
          }

          code {
            background: #0a101a;
            padding: 4px 8px;
            border-radius: 5px;
          }

          a {
            color: #7dd3fc;
          }
        </style>
      </head>

      <body>

        <div class="card">

          <h1>
            The Body as a Brush
          </h1>

          <p>
            AI feedback backend is running.
          </p>

          <p>
            Health:
            <a href="/api/health">
              /api/health
            </a>
          </p>

          <p>
            Latest Unity + OpenAI JSON:
            <a href="/api/debug/latest">
              /api/debug/latest
            </a>
          </p>

        </div>

      </body>
      </html>
      `
    );
  }
);

// ============================================================
// ERROR HANDLER
// ============================================================

app.use(
  (
    err,
    req,
    res,
    next
  ) => {

    console.error(
      "[SERVER ERROR]",
      err
    );

    if (
      res.headersSent
    ) {
      return next(err);
    }

    return res
      .status(500)
      .json({
        error:
          "Internal server error."
      });
  }
);

// ============================================================
// SERVER START
// ============================================================

app.listen(
  CONFIG.port,
  () => {

    console.log(
      "\n============================================================"
    );

    console.log(
      "The Body as a Brush AI Backend"
    );

    console.log(
      "============================================================"
    );

    console.log(
      "Port:",
      CONFIG.port
    );

    console.log(
      "Model:",
      CONFIG.openaiModel
    );

    console.log(
      "Mock AI:",
      CONFIG.mockAI
    );

    console.log(
      "OpenAI configured:",
      Boolean(
        CONFIG.openaiApiKey
      )
    );

    console.log(
      "Health endpoint:",
      `/api/health`
    );

    console.log(
      "Feedback endpoint:",
      `/api/feedback`
    );

    console.log(
      "Debug endpoint:",
      `/api/debug/latest`
    );

    console.log(
      "============================================================\n"
    );
  }
);