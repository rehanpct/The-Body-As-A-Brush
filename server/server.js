/**
 * server.js
 * The Body as a Brush — Render Backend
 *
 * Unity
 *   ↓
 * Gesture / Artwork State
 *   ↓
 * AIFeedbackClient
 *   ↓
 * Render Backend
 *   ↓
 * OpenAI Responses API
 *   ↓
 * Structured Creative Feedback
 *   ↓
 * Unity AI Feedback Panel
 */

'use strict';

require('dotenv').config();

const express = require('express');
const { OpenAI } = require('openai');

const app = express();

// ============================================================
// CONFIGURATION
// ============================================================

const CONFIG = {
  port: process.env.PORT || 3000,

  host: '0.0.0.0',

  openaiModel:
    process.env.OPENAI_MODEL || 'gpt-4o-mini',

  mockMode:
    process.env.MOCK_AI === 'true',

  maxRequestsPerMinute:
    parseInt(
      process.env.MAX_REQUESTS_PER_MINUTE || '30',
      10
    ),

  clientToken:
    process.env.UNITY_CLIENT_TOKEN || null,

  maxBodyBytes:
    64 * 1024
};

// ============================================================
// OPENAI CLIENT
// ============================================================

let openaiClient = null;

function getOpenAIClient() {

  if (openaiClient) {
    return openaiClient;
  }

  if (!process.env.OPENAI_API_KEY) {
    throw new Error(
      'OPENAI_API_KEY environment variable is not set.'
    );
  }

  openaiClient =
    new OpenAI({
      apiKey:
        process.env.OPENAI_API_KEY
    });

  return openaiClient;
}

// ============================================================
// ALLOWED UNITY ACTIONS
// ============================================================

const ALLOWED_ACTIONS = new Set([
  'none',
  'highlight_tool',
  'change_brush',
  'suggest_color',
  'spawn_preview',
  'adjust_brush_size',
  'play_gesture_demo',
  'undo_last_action',
  'show_composition_hint',
  'add_animation',
  'show_summary'
]);

// ============================================================
// VALID VALUES
// ============================================================

const VALID_FEEDBACK_TYPES = [
  'hint',
  'encouragement',
  'summary',
  'strategy',
  'warning'
];

const VALID_PRIORITIES = [
  'low',
  'normal',
  'high'
];

// ============================================================
// AI SYSTEM PROMPT
// ============================================================

const SYSTEM_PROMPT = `
You are the AI creative observer for a Unity interactive artwork
called "The Body as a Brush".

This is NOT a platform game.
This is NOT a jumping game.
This is NOT a combat game.
This is NOT a conventional score-based game.

The user creates artwork using body gestures and hand tracking.

The artwork can contain elements such as:

- fish schools
- coral
- bubbles
- water currents
- lanterns
- other theme-specific elements

Your job is to observe the artwork state supplied by Unity and provide
short, useful, natural creative feedback while the user is drawing.

============================================================
IMPORTANT RULE
============================================================

ONLY use information explicitly supplied by Unity.

Never invent:

- object positions
- object counts
- colors
- sizes
- gestures
- movement
- user intentions
- artwork elements
- camera information

You are an observer and creative assistant.

You do NOT control the Unity game.

============================================================
REALTIME DRAWING OBSERVATIONS
============================================================

When eventType is "composition_update", analyze the supplied
composition information.

You may comment on:

1. EMPTY SPACE

If one region contains noticeably more empty space, mention it.

Examples:

"There is quite a bit of open space in the lower-left area."

"The left side of the composition is still relatively open."

Do NOT claim an exact visual position unless the supplied data supports it.

2. SCALE

If hasLargeObject is true, use largeObjectType and
largestObjectPercentage.

Examples:

"The fish school is becoming a strong visual element."

"The fish school is relatively large compared with the other elements."

Do not say an object is large if hasLargeObject is false.

3. BALANCE

Use leftDensity, rightDensity, topDensity and bottomDensity.

If one side is noticeably denser:

"Most of the elements are concentrated toward the right side."

"The composition is currently weighted toward the upper area."

4. CLUSTERING

If hasCluster is true:

"Several elements are forming a cluster."

"Your elements are beginning to group together."

Do not invent the exact location of a cluster unless the supplied
data supports it.

5. NEGATIVE SPACE

Use emptySpacePercentage carefully.

Do not treat the value as an exact artistic measurement.

Use approximate language such as:

"There is still a fair amount of open space."

6. PROGRESS

Comment on how the artwork is developing.

Examples:

"Your composition is starting to come together."

"You're building a nice variety of elements."

"Your scene is becoming more populated."

7. POSITIVE CREATIVE COMMENTS

The AI should not constantly criticize the user.

Mix observations with encouragement.

Examples:

"Nice addition — the new element gives the composition more character."

"Your underwater scene is starting to feel lively."

"Nice variety between the fish and coral."

============================================================
OBJECT ADDED EVENTS
============================================================

When eventType is "object_added":

Comment specifically about the newly added element.

For example:

"Nice fish addition. Think about how it balances the surrounding
coral."

"That coral adds some visual variety to the scene."

"Nice addition — your underwater world is becoming more lively."

Do NOT mention an element that was not supplied.

============================================================
OTHER EVENTS
============================================================

For "object_deleted":

Comment constructively about the change.

For "improvement_detected":

Encourage the user.

For "idle":

Give a gentle creative suggestion.

For "stage_completed":

Summarize the progress.

For "repeated_action":

Suggest variety.

For "user_question":

Answer using only the supplied context.

============================================================
GESTURE INFORMATION
============================================================

The project uses gestures such as:

- V sign
- Open Palm
- Thumb Up
- Index gesture
- Pinch

Gesture counts may be supplied by Unity.

Do not pretend to see a gesture unless Unity supplied it.

============================================================
TONE
============================================================

Be:

- creative
- friendly
- encouraging
- concise
- natural
- useful

Avoid sounding robotic.

Do not constantly say:

"Great job!"

Instead make observations about the actual artwork.

============================================================
MESSAGE LENGTH
============================================================

Keep the message short.

Normally use 1 sentence.

Maximum 2 short sentences.

============================================================
ACTIONS
============================================================

Choose exactly ONE safe action.

Allowed actions:

none
highlight_tool
change_brush
suggest_color
spawn_preview
adjust_brush_size
play_gesture_demo
undo_last_action
show_composition_hint
add_animation
show_summary

For normal observations, prefer:

none

or:

show_composition_hint

The AI must NEVER directly manipulate Unity objects.

============================================================
SECURITY
============================================================

Never request or expose:

- API keys
- secrets
- passwords
- camera frames
- raw body coordinates
- private information

============================================================
OUTPUT
============================================================

Return ONLY valid JSON.

No markdown.
No code fences.
No explanations outside JSON.

Use exactly this structure:

{
  "message": "short creative feedback",
  "feedbackType": "hint",
  "action": {
    "type": "none",
    "target": "",
    "value": ""
  },
  "priority": "normal",
  "cooldownSeconds": 20
}

feedbackType must be one of:

hint
encouragement
summary
strategy
warning

priority must be one of:

low
normal
high

cooldownSeconds must be between 0 and 300.
`.trim();

// ============================================================
// RATE LIMITER
// ============================================================

const rateLimitStore = new Map();

function isRateLimited(ip) {

  const now = Date.now();

  const windowMs = 60000;

  let entry =
    rateLimitStore.get(ip);

  if (!entry) {

    entry = {
      count: 0,
      windowStart: now
    };

    rateLimitStore.set(
      ip,
      entry
    );

    return false;
  }

  if (
    now - entry.windowStart >
    windowMs
  ) {

    entry.count = 1;

    entry.windowStart = now;

    return false;
  }

  if (
    entry.count >=
    CONFIG.maxRequestsPerMinute
  ) {
    return true;
  }

  entry.count += 1;

  return false;
}

// Clean old rate-limit entries.

setInterval(
  () => {

    const cutoff =
      Date.now() - 60000;

    for (
      const [
        ip,
        entry
      ] of rateLimitStore.entries()
    ) {

      if (
        entry.windowStart <
        cutoff
      ) {
        rateLimitStore.delete(ip);
      }
    }
  },
  5 * 60000
);

// ============================================================
// REQUEST IDS
// ============================================================

let requestCounter = 0;

function nextRequestId() {

  requestCounter += 1;

  return (
    'req-' +
    Date.now() +
    '-' +
    String(
      requestCounter
    ).padStart(4, '0')
  );
}

// ============================================================
// LOGGER
// ============================================================

function log(
  level,
  requestId,
  data
) {

  const entry = {
    ts:
      new Date().toISOString(),

    level,

    requestId,

    ...data
  };

  // Never log secrets.

  delete entry.apiKey;
  delete entry.OPENAI_API_KEY;

  if (
    level === 'error'
  ) {
    console.error(
      JSON.stringify(entry)
    );
  } else {
    console.log(
      JSON.stringify(entry)
    );
  }
}

// ============================================================
// ACTION SANITIZATION
// ============================================================

function sanitizeAction(
  rawAction
) {

  if (
    !rawAction ||
    typeof rawAction !== 'object'
  ) {

    return {
      type: 'none',
      target: '',
      value: ''
    };
  }

  const actionType =
    typeof rawAction.type === 'string'
      ? rawAction.type
          .trim()
          .toLowerCase()
      : 'none';

  if (
    !ALLOWED_ACTIONS.has(
      actionType
    )
  ) {

    log(
      'warn',
      'system',
      {
        event:
          'action_blocked',

        reason:
          'not_in_allowlist',

        rejectedType:
          rawAction.type
      }
    );

    return {
      type: 'none',
      target: '',
      value: ''
    };
  }

  return {

    type:
      actionType,

    target:
      typeof rawAction.target === 'string'
        ? rawAction.target.slice(
            0,
            128
          )
        : '',

    value:
      rawAction.value !== undefined
        ? String(
            rawAction.value
          ).slice(
            0,
            256
          )
        : ''
  };
}

// ============================================================
// DEFAULT RESPONSE
// ============================================================

function defaultResponse() {

  return {

    message:
      'Keep exploring your composition and see how the next element changes the balance.',

    feedbackType:
      'encouragement',

    action: {
      type:
        'none',

      target:
        '',

      value:
        ''
    },

    priority:
      'low',

    cooldownSeconds:
      20
  };
}

// ============================================================
// MOCK AI
// ============================================================

function getMockResponse(
  eventPayload
) {

  const eventType =
    eventPayload.eventType;

  const context =
    eventPayload.context || {};

  // ----------------------------------------------------------
  // COMPOSITION UPDATE
  // ----------------------------------------------------------

  if (
    eventType ===
    'composition_update'
  ) {

    if (
      context.hasLargeObject &&
      context.largeObjectType
    ) {

      return {

        message:
          `The ${context.largeObjectType.replace('_', ' ')} is becoming a strong visual element in your composition.`,

        feedbackType:
          'encouragement',

        action: {
          type:
            'none',

          target:
            '',

          value:
            ''
        },

        priority:
          'low',

        cooldownSeconds:
          20
      };
    }

    if (
      context.largestEmptyRegion
    ) {

      return {

        message:
          `There is still some open space around the ${context.largestEmptyRegion.replace('_', ' ')} of the composition.`,

        feedbackType:
          'hint',

        action: {
          type:
            'show_composition_hint',

          target:
            context.largestEmptyRegion,

          value:
            ''
        },

        priority:
          'normal',

        cooldownSeconds:
          20
      };
    }

    if (
      context.hasCluster
    ) {

      return {

        message:
          'Several elements are starting to form a cluster. Consider how another element could balance the scene.',

        feedbackType:
          'strategy',

        action: {
          type:
            'show_composition_hint',

          target:
            'balance',

          value:
            ''
        },

        priority:
          'normal',

        cooldownSeconds:
          20
      };
    }

    return {

      message:
        'Your composition is developing nicely. Keep experimenting with the placement of your elements.',

      feedbackType:
        'encouragement',

      action: {
        type:
          'none',

        target:
          '',

        value:
          ''
      },

      priority:
        'low',

      cooldownSeconds:
        20
    };
  }

  // ----------------------------------------------------------
  // OBJECT ADDED
  // ----------------------------------------------------------

  if (
    eventType ===
    'object_added'
  ) {

    const objectType =
      context.objectType ||
      'new element';

    return {

      message:
        `Nice addition. Your ${objectType.replace('_', ' ')} is adding more character to the scene.`,

      feedbackType:
        'encouragement',

      action: {
        type:
          'none',

        target:
          '',

        value:
          ''
      },

      priority:
        'low',

      cooldownSeconds:
        20
    };
  }

  // ----------------------------------------------------------
  // IDLE
  // ----------------------------------------------------------

  if (
    eventType ===
    'idle'
  ) {

    return {

      message:
        'Try adding another element or gesture to continue developing your world.',

      feedbackType:
        'hint',

      action: {
        type:
          'none',

        target:
          '',

        value:
          ''
      },

      priority:
        'low',

      cooldownSeconds:
        30
    };
  }

  // ----------------------------------------------------------
  // STAGE COMPLETED
  // ----------------------------------------------------------

  if (
    eventType ===
    'stage_completed'
  ) {

    return {

      message:
        'Your artwork has developed into a complete scene. Take a moment to look at how the elements work together.',

      feedbackType:
        'summary',

      action: {
        type:
          'show_summary',

        target:
          'artwork',

        value:
          ''
      },

      priority:
        'high',

      cooldownSeconds:
        30
    };
  }

  // ----------------------------------------------------------
  // IMPROVEMENT
  // ----------------------------------------------------------

  if (
    eventType ===
    'improvement_detected'
  ) {

    return {

      message:
        'Your gestures are becoming more confident. Keep experimenting with your composition.',

      feedbackType:
        'encouragement',

      action: {
        type:
          'none',

        target:
          '',

        value:
          ''
      },

      priority:
        'low',

      cooldownSeconds:
        20
    };
  }

  // ----------------------------------------------------------
  // REPEATED ACTION
  // ----------------------------------------------------------

  if (
    eventType ===
    'repeated_action'
  ) {

    return {

      message:
        'You have been using the same type of element repeatedly. Try introducing some variety into the scene.',

      feedbackType:
        'strategy',

      action: {
        type:
          'show_composition_hint',

        target:
          'variety',

        value:
          ''
      },

      priority:
        'normal',

      cooldownSeconds:
        25
    };
  }

  // ----------------------------------------------------------
  // OBJECT DELETED
  // ----------------------------------------------------------

  if (
    eventType ===
    'object_deleted'
  ) {

    return {

      message:
        'Removing an element can change the balance of the composition. See how the scene feels now.',

      feedbackType:
        'encouragement',

      action: {
        type:
          'none',

        target:
          '',

        value:
          ''
      },

      priority:
        'low',

      cooldownSeconds:
        20
    };
  }

  return defaultResponse();
}

// ============================================================
// REAL OPENAI REQUEST
// ============================================================

async function getAIFeedback(
  eventPayload
) {

  const client =
    getOpenAIClient();

  const userContent =
    JSON.stringify(
      eventPayload,
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

  const rawText =
    response.output_text || '';

  if (!rawText.trim()) {

    throw new Error(
      'OpenAI returned an empty response.'
    );
  }

  let parsed;

  try {

    parsed =
      JSON.parse(
        rawText.trim()
      );

  } catch (error) {

    log(
      'error',
      'system',
      {
        event:
          'invalid_ai_json',

        rawResponse:
          rawText.slice(
            0,
            1000
          )
      }
    );

    throw new SyntaxError(
      'OpenAI response was not valid JSON.'
    );
  }

  return {

    message:
      typeof parsed.message ===
      'string'

        ? parsed.message
            .trim()
            .slice(
              0,
              512
            )

        : '',

    feedbackType:
      VALID_FEEDBACK_TYPES.includes(
        parsed.feedbackType
      )

        ? parsed.feedbackType

        : 'hint',

    action:
      sanitizeAction(
        parsed.action
      ),

    priority:
      VALID_PRIORITIES.includes(
        parsed.priority
      )

        ? parsed.priority

        : 'normal',

    cooldownSeconds:
      Number.isInteger(
        parsed.cooldownSeconds
      )

        ? Math.max(
            0,
            Math.min(
              parsed.cooldownSeconds,
              300
            )
          )

        : 20
  };
}

// ============================================================
// EXPRESS MIDDLEWARE
// ============================================================

app.use(
  express.json({
    limit:
      CONFIG.maxBodyBytes
  })
);

// ============================================================
// REQUEST ID
// ============================================================

app.use(
  (req, _res, next) => {

    req.requestId =
      nextRequestId();

    req.startTime =
      Date.now();

    next();
  }
);

// ============================================================
// HEALTH CHECK
// ============================================================

app.get(
  '/api/health',
  (req, res) => {

    log(
      'info',
      req.requestId,
      {
        event:
          'health_check',

        ip:
          req.ip
      }
    );

    res.status(200).json({

      status:
        'ok',

      service:
        'body-as-a-brush-feedback',

      mockMode:
        CONFIG.mockMode,

      model:
        CONFIG.mockMode
          ? 'mock'
          : CONFIG.openaiModel,

      timestamp:
        new Date().toISOString()
    });
  }
);

// ============================================================
// FEEDBACK ENDPOINT
// ============================================================

app.post(
  '/api/feedback',
  async (req, res) => {

    const requestId =
      req.requestId;

    const startTime =
      req.startTime;

    const clientIp =
      req.ip ||
      'unknown';

    // --------------------------------------------------------
    // RATE LIMIT
    // --------------------------------------------------------

    if (
      isRateLimited(
        clientIp
      )
    ) {

      log(
        'warn',
        requestId,
        {
          event:
            'rate_limited',

          ip:
            clientIp
        }
      );

      return res
        .status(429)
        .json({
          error:
            'Too many requests. Please slow down.'
        });
    }

    // --------------------------------------------------------
    // OPTIONAL TOKEN AUTH
    // --------------------------------------------------------

    if (
      CONFIG.clientToken
    ) {

      const provided =
        req.headers[
          'x-unity-client-token'
        ];

      if (
        provided !==
        CONFIG.clientToken
      ) {

        log(
          'warn',
          requestId,
          {
            event:
              'auth_failed',

            ip:
              clientIp
          }
        );

        return res
          .status(401)
          .json({
            error:
              'Unauthorized.'
          });
      }
    }

    // --------------------------------------------------------
    // VALIDATE BODY
    // --------------------------------------------------------

    const body =
      req.body;

    if (
      !body ||
      typeof body !== 'object'
    ) {

      return res
        .status(400)
        .json({
          error:
            'Invalid request: body must be JSON.'
        });
    }

    if (
      !body.projectType
    ) {

      return res
        .status(400)
        .json({
          error:
            'Invalid request: projectType is required.'
        });
    }

    if (
      !body.sessionId
    ) {

      return res
        .status(400)
        .json({
          error:
            'Invalid request: sessionId is required.'
        });
    }

    if (
      !body.eventType
    ) {

      return res
        .status(400)
        .json({
          error:
            'Invalid request: eventType is required.'
        });
    }

    if (
      !body.context ||
      typeof body.context !== 'object'
    ) {

      return res
        .status(400)
        .json({
          error:
            'Invalid request: context object is required.'
        });
    }

    // --------------------------------------------------------
    // PROJECT VALIDATION
    // --------------------------------------------------------

    if (
      body.projectType !==
      'gesture_painting'
    ) {

      return res
        .status(400)
        .json({
          error:
            'Unsupported projectType.'
        });
    }

    // --------------------------------------------------------
    // EXTRACT
    // --------------------------------------------------------

    const {
      sessionId,
      eventType,
      projectType
    } = body;

    // --------------------------------------------------------
    // LOG REQUEST
    // --------------------------------------------------------

    log(
      'info',
      requestId,
      {

        event:
          'feedback_request',

        sessionId,

        eventType,

        projectType,

        mockMode:
          CONFIG.mockMode,

        ip:
          clientIp
      }
    );

    // --------------------------------------------------------
    // PROCESS
    // --------------------------------------------------------

    try {

      let feedback;

      if (
        CONFIG.mockMode
      ) {

        feedback =
          getMockResponse(
            body
          );

        log(
          'info',
          requestId,
          {

            event:
              'mock_response_returned',

            sessionId,

            eventType
          }
        );

      } else {

        feedback =
          await getAIFeedback(
            body
          );

        log(
          'info',
          requestId,
          {

            event:
              'ai_response_returned',

            sessionId,

            eventType,

            feedbackType:
              feedback.feedbackType,

            actionType:
              feedback.action.type
          }
        );
      }

      // ------------------------------------------------------
      // LATENCY
      // ------------------------------------------------------

      const latencyMs =
        Date.now() -
        startTime;

      log(
        'info',
        requestId,
        {

          event:
            'request_complete',

          sessionId,

          eventType,

          latencyMs,

          success:
            true
        }
      );

      // ------------------------------------------------------
      // RESPONSE
      // ------------------------------------------------------

      return res
        .status(200)
        .json({

          requestId,

          sessionId,

          ...feedback
        });

    } catch (err) {

      const latencyMs =
        Date.now() -
        startTime;

      log(
        'error',
        requestId,
        {

          event:
            'feedback_error',

          sessionId,

          eventType,

          latencyMs,

          success:
            false,

          errorMessage:
            err.message
        }
      );

      // ------------------------------------------------------
      // API KEY ERROR
      // ------------------------------------------------------

      if (
        err.message &&
        err.message.includes(
          'OPENAI_API_KEY'
        )
      ) {

        return res
          .status(503)
          .json({

            error:
              'AI service is not configured. Contact the administrator.'
          });
      }

      // ------------------------------------------------------
      // BAD AI JSON
      // ------------------------------------------------------

      if (
        err instanceof
        SyntaxError
      ) {

        return res
          .status(502)
          .json({

            error:
              'AI returned an unexpected response format. Please try again.'
          });
      }

      // ------------------------------------------------------
      // GENERIC ERROR
      // ------------------------------------------------------

      return res
        .status(500)
        .json({

          error:
            'An internal error occurred. Please try again.'
        });
    }
  }
);

// ============================================================
// 404
// ============================================================

app.use(
  (_req, res) => {

    res
      .status(404)
      .json({
        error:
          'Endpoint not found.'
      });
  }
);

// ============================================================
// SERVER START
// ============================================================

app.listen(
  CONFIG.port,
  CONFIG.host,
  () => {

    console.log(
      JSON.stringify({

        ts:
          new Date().toISOString(),

        level:
          'info',

        event:
          'server_start',

        host:
          CONFIG.host,

        port:
          CONFIG.port,

        mockMode:
          CONFIG.mockMode,

        model:
          CONFIG.mockMode
            ? 'mock'
            : CONFIG.openaiModel,

        rateLimit:
          CONFIG.maxRequestsPerMinute +
          '/min',

        clientTokenRequired:
          !!CONFIG.clientToken
      })
    );
  }
);

module.exports = app;