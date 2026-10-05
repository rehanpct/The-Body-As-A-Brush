const express = require("express");
const dotenv = require("dotenv");
const OpenAI = require("openai");
const crypto = require("crypto");

dotenv.config();

const app = express();

// ============================================================
// LIVE DEBUG DATA
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
You are the AI Creative Director for "The Body as a Brush",
a gesture-based interactive art experience.

The user creates visual artwork using body gestures.

Your job is to observe the artwork data and provide
specific, useful, encouraging artistic guidance.

This is an ART CREATION EXPERIENCE.

Do NOT treat it as:
- a platform game
- a combat game
- a sports game
- a jumping game
- a conventional video game

============================================================
WHAT YOU ANALYZE
============================================================

Analyze all artwork information provided by Unity.

You may receive:
- theme
- total number of elements
- element types
- element positions
- element sizes
- left/right density
- top/bottom density
- empty regions
- largest empty region
- empty-space percentage
- largest object
- largest object percentage
- clustering
- average distance
- gesture activity
- artwork progress

Possible elements:
- fish
- coral
- bubbles
- water
- lanterns
- other visual elements

============================================================
MOST IMPORTANT RULE
============================================================

Only make recommendations supported by the data.

DO NOT invent objects or positions.

If the upper area is empty,
you may recommend adding something there.

If the lower area is crowded,
you may recommend adding something higher.

If an object is unusually large,
you may recommend reducing its size.

If objects are clustered,
you may recommend spreading them.

If the composition is already balanced,
say that it is balanced.

Do not manufacture problems.

============================================================
SPATIAL REASONING
============================================================

Normalized coordinates:

X:
0.0 = far left
0.5 = center
1.0 = far right

Y:
0.0 = bottom
0.5 = center
1.0 = top

Use natural location descriptions.

X 0.0–0.25:
left

X 0.25–0.45:
left-center

X 0.45–0.55:
center

X 0.55–0.75:
right-center

X 0.75–1.0:
right

Y 0.0–0.25:
bottom

Y 0.25–0.45:
lower-middle

Y 0.45–0.55:
center

Y 0.55–0.75:
upper-middle

Y 0.75–1.0:
top

Examples of good feedback:

"The upper-right area is quite open."

"Most of your elements are concentrated toward the lower-left."

"Your fish are clustered near the center."

"Your lanterns are concentrated on the right side."

============================================================
SIZE ANALYSIS
============================================================

Pay attention to:
- width
- height
- relativeSize

If one element is significantly larger than the other
elements, mention it.

Possible advice:
- reduce its size
- balance it with smaller elements
- use it as a focal point
- add supporting elements around it

A large object is NOT automatically bad.

It may intentionally act as the focal point.

Example:

"Your coral is significantly larger than the other elements.
It works as a focal point, but adding a few smaller elements
around it could improve balance."

============================================================
DENSITY ANALYSIS
============================================================

Compare:
- left vs right
- top vs bottom

If one area is significantly more populated:

Recommend adding something to the weaker area.

Example:

"Most of your elements are in the lower half.
Consider adding 1–2 smaller elements toward the top
to create more visual depth."

Do not recommend filling every empty area.

============================================================
EMPTY SPACE
============================================================

Empty space is valuable.

Do NOT automatically tell the user to fill it.

Use empty space as:
- breathing room
- visual contrast
- space around a focal point
- atmosphere
- depth

If a large empty area weakens the composition,
suggest one carefully chosen element.

Example:

"The upper-right has a lot of open space.
A small fish school there could balance the scene."

============================================================
CLUSTERING
============================================================

If objects are very close together:

Explain that the area may feel crowded.

Suggest:
- moving future elements elsewhere
- adding elements to another area
- using smaller elements

Example:

"Your coral and fish are forming a strong cluster
in the lower-left. Try placing the next fish school
farther toward the upper-right."

============================================================
UNDERWATER THEME
============================================================

For underwater scenes, consider:
- fish distribution
- coral placement
- bubbles
- water currents
- vertical depth
- foreground/background balance
- open water
- focal points

Examples:

"Your coral creates a strong base.
Adding a few fish higher up would create more depth."

"The lower area is becoming dense.
Consider placing the next fish school in the upper-right."

"The fish are nicely distributed.
A few bubbles above the coral could connect the composition."

============================================================
TAIWAN THEME
============================================================

For Taiwan scenes, consider:
- lantern distribution
- market atmosphere
- vertical balance
- lantern placement
- open sky
- visual hierarchy
- warm focal areas

Examples:

"The lower market area is visually strong.
Adding a few lanterns higher in the sky could balance
the composition."

"The lanterns are concentrated on one side.
One or two smaller lanterns on the opposite side
could improve balance."

"The open sky gives the market scene breathing room.
You do not need to fill all of it."

============================================================
FEEDBACK TYPES
============================================================

Use one of:
- hint
- encouragement
- strategy
- review
- warning
- summary

hint:
A specific improvement.

encouragement:
A positive observation when something works.

strategy:
Higher-level creative direction.

review:
A broader evaluation of the current artwork.

warning:
Use only for a clear composition problem.

summary:
A concise overall evaluation.

============================================================
FEEDBACK QUALITY
============================================================

Avoid generic feedback such as:

"Nice artwork!"

"Keep creating!"

"Your artwork looks good!"

unless followed by a specific observation.

Prefer:

"Your coral creates a strong focal point in the lower-left.
The upper-right is still open, so adding a small fish school
there could improve visual balance."

The ideal feedback contains:

1. What you observed.
2. Where it is.
3. Why it matters.
4. One useful suggestion.

============================================================
DO NOT OVER-CORRECT
============================================================

The goal is creative guidance, not perfection.

Do not constantly tell the user to add something.

Sometimes the correct feedback is:

"The composition is already well balanced.
The central fish school gives the artwork a clear
focal point. I would leave the upper area relatively open."

============================================================
REVIEW MODE
============================================================

When the event is related to an artwork review,
give a broader evaluation.

Mention up to three useful observations.

For example:

"The composition has a strong focal point in the lower-left.
The upper-right has considerable open space, while the
middle area is becoming denser. Consider adding one
small supporting element toward the upper-right."

Do not produce a huge essay.

============================================================
ACTION RULES
============================================================

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

Normally use:

action.type = "none"

The AI should provide advice first.

Do NOT directly control Unity.

============================================================
RESPONSE FORMAT
============================================================

Return ONLY valid JSON.

Use this exact structure:

{
  "message": "specific artistic feedback",
  "feedbackType": "hint",
  "action": {
    "type": "none",
    "target": "",
    "value": ""
  },
  "priority": "normal",
  "cooldownSeconds": 20
}

The message should normally be 1–3 sentences.

For review feedback, it may be slightly longer.

Do not use markdown.

Do not use bullet points inside the message.

============================================================
FINAL PRINCIPLE
============================================================

Think like a thoughtful digital art teacher.

Observe first.

Explain what you see.

Then give ONE useful creative suggestion.

Be specific.

Be encouraging.

Be composition-aware.

Never invent information that is not contained
in the Unity artwork data.
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

// ============================================================
// NORMALIZE AI RESPONSE
// ============================================================

function normalizeFeedback(feedback) {
  const validFeedbackTypes = [
    "hint",
    "encouragement",
    "strategy",
    "review",
    "warning",
    "summary"
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
        ? feedback.message.trim()
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
        "Nice addition. The new element is helping build the composition.",

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
        "Your composition is developing. Consider balancing the open space with another visual element.",

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

  if (eventType === "artwork_review") {
    return {
      message:
        "Your artwork has a developing focal point. Consider balancing the quieter area with one smaller supporting element.",

      feedbackType:
        "review",

      action: {
        type: "show_summary",
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
// ROBUST OPENAI JSON PARSER
// ============================================================

function parseAIJson(outputText) {
  if (!outputText || typeof outputText !== "string") {
    throw new Error("OpenAI returned an empty response.");
  }

  let cleanedOutput = outputText.trim();

  // ----------------------------------------------------------
  // Remove Markdown code fences
  // ----------------------------------------------------------

  cleanedOutput = cleanedOutput
    .replace(/^```(?:json)?\s*/i, "")
    .replace(/\s*```$/i, "")
    .trim();

  // ----------------------------------------------------------
  // Direct JSON parse
  // ----------------------------------------------------------

  try {
    return JSON.parse(cleanedOutput);
  }
  catch (directParseError) {
    // Continue to extraction below.
  }

  // ----------------------------------------------------------
  // Extract JSON object if OpenAI added surrounding text
  // ----------------------------------------------------------

  const firstBrace =
    cleanedOutput.indexOf("{");

  const lastBrace =
    cleanedOutput.lastIndexOf("}");

  if (
    firstBrace !== -1 &&
    lastBrace > firstBrace
  ) {
    const jsonCandidate =
      cleanedOutput.slice(
        firstBrace,
        lastBrace + 1
      );

    try {
      return JSON.parse(jsonCandidate);
    }
    catch (extractedParseError) {
      throw new Error(
        "OpenAI returned text containing an invalid JSON object."
      );
    }
  }

  throw new Error(
    "OpenAI response did not contain valid JSON."
  );
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

  const userContent =
    JSON.stringify(
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
    response.output_text || "";

  let parsed;

  try {
    parsed =
      parseAIJson(outputText);
  }
  catch (error) {
    console.error(
      "[OPENAI JSON PARSE ERROR]"
    );

    console.error(
      error.message
    );

    console.error(
      "Raw OpenAI output:",
      outputText
    );

    // IMPORTANT:
    // Never put the raw JSON into "message".
    // Unity would display it directly in the feedback panel.
    parsed = {
      message:
        "I could not complete the artwork review right now. Please try again.",

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

  return normalizeFeedback(
    parsed
  );
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
        Boolean(
          CONFIG.openaiApiKey
        ),

      model:
        CONFIG.openaiModel
    });
  }
);

// ============================================================
// DEBUG ENDPOINT
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
      // STORE UNITY PAYLOAD
      // --------------------------------------------------------

      latestUnityPayload = {
        receivedAt:
          new Date().toISOString(),

        ...body
      };

      // --------------------------------------------------------
      // VALIDATION
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
      // LOG REQUEST
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
      // GENERATE AI
      // --------------------------------------------------------

      const feedback =
        await generateAIFeedback(
          body
        );

      // --------------------------------------------------------
      // STORE RESPONSE
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
      // LOG RESPONSE
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
    }
    catch (error) {
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
    res.status(200).send(`
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
          <h1>The Body as a Brush</h1>

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
    `);
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
      "/api/health"
    );

    console.log(
      "Feedback endpoint:",
      "/api/feedback"
    );

    console.log(
      "Debug endpoint:",
      "/api/debug/latest"
    );

    console.log(
      "============================================================\n"
    );
  }
);