/**
 * server.js
 * Unity AI Feedback Laboratory — Render Backend
 *
 * Architecture:
 *   Unity → HTTPS JSON → This Server → OpenAI Responses API
 *         → Validate & Sanitise → Unity (structured feedback)
 *
 * Environment variables (NEVER hardcoded here):
 *   OPENAI_API_KEY          – OpenAI secret key (set in Render dashboard only)
 *   OPENAI_MODEL            – Model to use (default: gpt-4o-mini)
 *   UNITY_CLIENT_TOKEN      – Optional shared secret for Unity requests
 *   MAX_REQUESTS_PER_MINUTE – Rate limit ceiling (default: 30)
 *   MOCK_AI                 – "true" to skip OpenAI and return deterministic responses
 *   PORT                    – HTTP port (Render sets this automatically)
 */

'use strict';

// Load .env for local development only — never shipped with real secrets
require('dotenv').config();

const express = require('express');
const { OpenAI } = require('openai');

const app = express();

// ---------------------------------------------------------------------------
// Configuration — all values come from environment variables
// ---------------------------------------------------------------------------
const CONFIG = {
  port: process.env.PORT || 3000,
  host: '0.0.0.0',                         // Required for Render
  openaiModel: process.env.OPENAI_MODEL || 'gpt-4o-mini',
  mockMode: process.env.MOCK_AI === 'true',
  maxRequestsPerMinute: parseInt(process.env.MAX_REQUESTS_PER_MINUTE || '30', 10),
  clientToken: process.env.UNITY_CLIENT_TOKEN || null,
  maxBodyBytes: 64 * 1024,                  // 64 KB generous ceiling for gameplay JSON
};

// ---------------------------------------------------------------------------
// OpenAI client — lazy, only created when actually needed
// ---------------------------------------------------------------------------
let openaiClient = null;

function getOpenAIClient() {
  if (openaiClient) return openaiClient;
  if (!process.env.OPENAI_API_KEY) {
    throw new Error(
      'OPENAI_API_KEY environment variable is not set. Configure it in the Render dashboard.'
    );
  }
  openaiClient = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });
  return openaiClient;
}

// ---------------------------------------------------------------------------
// Allowed action types — the ONLY values Unity will ever execute
// ---------------------------------------------------------------------------
const ALLOWED_ACTIONS = new Set([
  'show_control_hint',
  'play_gesture_demo',
  'highlight_safe_route',
  'preview_jump_arc',
  'adjust_gesture_tolerance',
  'reduce_game_speed',
  'activate_practice_mode',
  'restore_normal_difficulty',
  'recommend_next_strategy',
  'show_run_summary',
  'none',
]);

// ---------------------------------------------------------------------------
// System prompt — defines AI role and hard constraints
// ---------------------------------------------------------------------------
const SYSTEM_PROMPT = `
You are an AI coaching assistant embedded in a Unity game feedback system.

ROLE:
- Interpret real-time gameplay events sent by the Unity game engine.
- Provide coaching feedback, hints, progress summaries, and strategy suggestions.
- Select a single safe predefined action for Unity to execute.

WHAT YOU MUST DO:
- Interpret the gameplay context supplied in the request.
- Explain repeated failures clearly and constructively.
- Provide actionable hints and encouragement.
- Summarise progress when relevant.
- Suggest strategy changes appropriate to the current difficulty.
- Choose ONE action from the allowed action list below.

WHAT YOU MUST NEVER DO:
- Control the player character directly.
- Perform physics or collision calculations.
- Modify scores, timers, or game state directly.
- Delete or spawn GameObjects.
- Bypass or skip levels.
- Execute, suggest, or reference arbitrary code (C#, shell, Python, etc.).
- Invent gameplay facts not explicitly supplied by Unity.
- Expose or request API keys or secrets.

ALLOWED ACTIONS (use ONLY these exact strings for action.type):
show_control_hint, play_gesture_demo, highlight_safe_route, preview_jump_arc,
adjust_gesture_tolerance, reduce_game_speed, activate_practice_mode,
restore_normal_difficulty, recommend_next_strategy, show_run_summary, none

OUTPUT FORMAT:
Respond with ONLY valid JSON — no markdown, no code fences, no extra keys:
{
  "message": "<short coaching feedback, 1-3 sentences>",
  "feedbackType": "<one of: hint | encouragement | summary | strategy | warning>",
  "action": {
    "type": "<one allowed action string>",
    "target": "<relevant target identifier or empty string>",
    "value": "<relevant value or empty string>"
  },
  "priority": "<one of: low | normal | high>",
  "cooldownSeconds": <integer 0-300>
}
`.trim();


// ---------------------------------------------------------------------------
// Simple in-memory rate limiter
// ---------------------------------------------------------------------------
const rateLimitStore = new Map(); // ip -> { count, windowStart }

function isRateLimited(ip) {
  const now = Date.now();
  const windowMs = 60000;
  const entry = rateLimitStore.get(ip) || { count: 0, windowStart: now };

  if (now - entry.windowStart > windowMs) {
    rateLimitStore.set(ip, { count: 1, windowStart: now });
    return false;
  }
  if (entry.count >= CONFIG.maxRequestsPerMinute) {
    return true;
  }
  entry.count += 1;
  rateLimitStore.set(ip, entry);
  return false;
}

// Purge stale entries every 5 minutes
setInterval(() => {
  const cutoff = Date.now() - 60000;
  for (const [ip, entry] of rateLimitStore.entries()) {
    if (entry.windowStart < cutoff) rateLimitStore.delete(ip);
  }
}, 5 * 60000);

// ---------------------------------------------------------------------------
// Request ID generator
// ---------------------------------------------------------------------------
let requestCounter = 0;
function nextRequestId() {
  return 'req-' + Date.now() + '-' + String(++requestCounter).padStart(4, '0');
}

// ---------------------------------------------------------------------------
// Structured logger — never logs the API key
// ---------------------------------------------------------------------------
function log(level, requestId, data) {
  const entry = { ts: new Date().toISOString(), level, requestId, ...data };
  // Scrub any accidental key leakage
  delete entry.apiKey;
  delete entry.OPENAI_API_KEY;
  console[level === 'error' ? 'error' : 'log'](JSON.stringify(entry));
}

// ---------------------------------------------------------------------------
// Action allowlist validator
// ---------------------------------------------------------------------------
function sanitizeAction(rawAction) {
  if (!rawAction || typeof rawAction !== 'object') {
    return { type: 'none', target: '', value: '' };
  }

  const actionType = typeof rawAction.type === 'string'
    ? rawAction.type.trim().toLowerCase()
    : 'none';

  if (!ALLOWED_ACTIONS.has(actionType)) {
    log('warn', 'system', {
      event: 'action_blocked',
      reason: 'not_in_allowlist',
      rejectedType: rawAction.type,
    });
    return { type: 'none', target: '', value: '' };
  }

  return {
    type: actionType,
    target: typeof rawAction.target === 'string' ? rawAction.target.slice(0, 128) : '',
    value: rawAction.value !== undefined ? String(rawAction.value).slice(0, 256) : '',
  };
}

// ---------------------------------------------------------------------------
// Mock response (no OpenAI call — deterministic for testing)
// ---------------------------------------------------------------------------
function getMockResponse() {
  return {
    message: 'Your jump timing appears late. Try starting the jump slightly earlier.',
    feedbackType: 'hint',
    action: {
      type: 'highlight_safe_route',
      target: 'next_platform',
      value: '1',
    },
    priority: 'normal',
    cooldownSeconds: 20,
  };
}

// ---------------------------------------------------------------------------
// Real OpenAI call via Responses API
// ---------------------------------------------------------------------------
async function getAIFeedback(eventPayload) {
  const client = getOpenAIClient();
  const userContent = JSON.stringify(eventPayload, null, 2);

  const response = await client.responses.create({
    model: CONFIG.openaiModel,
    instructions: SYSTEM_PROMPT,
    input: userContent,
  });

  const rawText = response.output_text || '';
  const parsed = JSON.parse(rawText.trim()); // may throw — caught by caller

  const VALID_FEEDBACK_TYPES = ['hint', 'encouragement', 'summary', 'strategy', 'warning'];
  const VALID_PRIORITIES = ['low', 'normal', 'high'];

  return {
    message: typeof parsed.message === 'string' ? parsed.message.slice(0, 512) : '',
    feedbackType: VALID_FEEDBACK_TYPES.includes(parsed.feedbackType) ? parsed.feedbackType : 'hint',
    action: sanitizeAction(parsed.action),
    priority: VALID_PRIORITIES.includes(parsed.priority) ? parsed.priority : 'normal',
    cooldownSeconds: Number.isInteger(parsed.cooldownSeconds)
      ? Math.max(0, Math.min(parsed.cooldownSeconds, 300))
      : 20,
  };
}

// ---------------------------------------------------------------------------
// Middleware
// ---------------------------------------------------------------------------
app.use(express.json({ limit: CONFIG.maxBodyBytes }));

// Attach request ID and start timer
app.use((req, _res, next) => {
  req.requestId = nextRequestId();
  req.startTime = Date.now();
  next();
});

// ---------------------------------------------------------------------------
// GET /api/health
// ---------------------------------------------------------------------------
app.get('/api/health', (req, res) => {
  log('info', req.requestId, { event: 'health_check', ip: req.ip });
  res.status(200).json({
    status: 'ok',
    service: 'unity-ai-feedback',
    mockMode: CONFIG.mockMode,
    model: CONFIG.mockMode ? 'mock' : CONFIG.openaiModel,
    timestamp: new Date().toISOString(),
  });
});

// ---------------------------------------------------------------------------
// POST /api/feedback
// ---------------------------------------------------------------------------
app.post('/api/feedback', async (req, res) => {
  const requestId = req.requestId;
  const startTime = req.startTime;
  const clientIp = req.ip || 'unknown';

  // Rate limiting
  if (isRateLimited(clientIp)) {
    log('warn', requestId, { event: 'rate_limited', ip: clientIp });
    return res.status(429).json({ error: 'Too many requests. Please slow down.' });
  }

  // Optional client token auth
  if (CONFIG.clientToken) {
    const provided = req.headers['x-unity-client-token'];
    if (provided !== CONFIG.clientToken) {
      log('warn', requestId, { event: 'auth_failed', ip: clientIp });
      return res.status(401).json({ error: 'Unauthorized.' });
    }
  }

  const body = req.body;

  // Validate required fields
  if (!body || typeof body !== 'object') {
    return res.status(400).json({ error: 'Invalid request: body must be JSON.' });
  }
  if (!body.projectType) {
    return res.status(400).json({ error: 'Invalid request: projectType is required.' });
  }
  if (!body.sessionId) {
    return res.status(400).json({ error: 'Invalid request: sessionId is required.' });
  }
  if (!body.eventType) {
    return res.status(400).json({ error: 'Invalid request: eventType is required.' });
  }
  if (!body.context || typeof body.context !== 'object') {
    return res.status(400).json({ error: 'Invalid request: context object is required.' });
  }

  const { sessionId, eventType, projectType } = body;

  log('info', requestId, {
    event: 'feedback_request',
    sessionId,
    eventType,
    projectType,
    mockMode: CONFIG.mockMode,
    ip: clientIp,
  });

  try {
    let feedback;

    if (CONFIG.mockMode) {
      feedback = getMockResponse();
      log('info', requestId, { event: 'mock_response_returned', sessionId });
    } else {
      feedback = await getAIFeedback(body);
      log('info', requestId, {
        event: 'ai_response_returned',
        sessionId,
        feedbackType: feedback.feedbackType,
        actionType: feedback.action.type,
      });
    }

    const latencyMs = Date.now() - startTime;
    log('info', requestId, { event: 'request_complete', sessionId, latencyMs, success: true });

    return res.status(200).json({
      requestId,
      sessionId,
      ...feedback,
    });

  } catch (err) {
    const latencyMs = Date.now() - startTime;
    log('error', requestId, {
      event: 'feedback_error',
      sessionId,
      latencyMs,
      success: false,
      errorMessage: err.message,
    });

    if (err.message && err.message.includes('OPENAI_API_KEY')) {
      return res.status(503).json({
        error: 'AI service is not configured. Contact the administrator.',
      });
    }
    if (err instanceof SyntaxError) {
      return res.status(502).json({
        error: 'AI returned an unexpected response format. Please try again.',
      });
    }
    return res.status(500).json({
      error: 'An internal error occurred. Please try again.',
    });
  }
});

// ---------------------------------------------------------------------------
// 404 catch-all
// ---------------------------------------------------------------------------
app.use((_req, res) => {
  res.status(404).json({ error: 'Endpoint not found.' });
});

// ---------------------------------------------------------------------------
// Start server
// ---------------------------------------------------------------------------
app.listen(CONFIG.port, CONFIG.host, () => {
  console.log(JSON.stringify({
    ts: new Date().toISOString(),
    level: 'info',
    event: 'server_start',
    host: CONFIG.host,
    port: CONFIG.port,
    mockMode: CONFIG.mockMode,
    model: CONFIG.mockMode ? 'mock' : CONFIG.openaiModel,
    rateLimit: CONFIG.maxRequestsPerMinute + '/min',
    clientTokenRequired: !!CONFIG.clientToken,
  }));
});

module.exports = app; // Export for future automated tests
