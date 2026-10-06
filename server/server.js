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

Unity's currentTheme, element types, counts, normalized positions,
sizes, 3 x 3 grid, and advisor recommendation are authoritative.
Do not infer or invent objects, counts, positions, crowded areas,
or empty areas. Use the advisor action, element, zone, and reason
as the factual basis for a suggestion. Recommend only an element
allowed by currentTheme.

Taiwan allows only: light_trail, lantern, petals, fireworks.
Underwater allows only: water_current, coral, fish_school, bubble_burst.
Never describe or recommend an element from the other theme.

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
- light_trail
- lantern
- petals
- fireworks
- water_current
- coral
- fish_school
- bubble_burst

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

1. What Unity measured in the artwork.
2. The measured location or distribution.
3. Why the CompositionAdvisor recommendation fits.
4. One useful suggestion.

Do not name an element unless it appears in the current theme's
measured elements or is the advisor's recommended element.
Do not describe a location unless the grid, element positions, or
advisor zone support it.

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

When eventType is artwork_review and automatic is false,
give a broader text review of the current artwork. Mention its
actual theme elements, their measured distribution, one strong
area, one underused or crowded area when present, and up to two
specific recommendations. Do not speak or address an audio output.

For automatic requests, keep the feedback to one or two short sentences and
paraphrase only the advisor's single factual recommendation. Unity's advisor is
authoritative for the action, element, target zone, and reason.

For automatic=true, copy context.advisor.action into action.type exactly,
context.advisor.element into action.target exactly, and context.advisor.zone into
action.value exactly. Never substitute another action, element, or zone. If the
advisor action is LEAVE_OPEN, do not recommend adding, placing, moving, or
spreading any element. Recommend only the advisor element. You may mention another element only as a factual observation
when Unity measured it; never recommend a different element.

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

For manual requests (automatic=false), normally use:

action.type = "none"

For automatic requests, the action fields are the validated composition
recommendation, not a Unity control: echo the advisor action, element, and zone
exactly as specified above. This rule overrides the manual action list.

The AI should provide advice first.

Do NOT directly control Unity.

============================================================
RESPONSE FORMAT
============================================================

Return ONLY valid JSON.

Use this exact structure. For automatic=true, keep these fields but set action.type,
action.target, and action.value to the exact advisor action, element, and zone.
The example below shows the manual-request defaults.

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

For automatic requests, the message must be one or two concise sentences.
For manual review feedback, it may be slightly longer.

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


const THEME_ELEMENTS = {
  Taiwan: [
    "light_trail",
    "lantern",
    "petals",
    "fireworks"
  ],
  Underwater: [
    "water_current",
    "coral",
    "fish_school",
    "bubble_burst"
  ]
};

const ELEMENT_MENTION_PATTERNS = {
  light_trail: /\b(?:light[\s_-]+)?trails?\b/i,
  lantern: /\blanterns?\b/i,
  petals: /\bpetals?(?:\s+groups?)?\b/i,
  fireworks: /\bfireworks?\b/i,
  water_current: /\bwater(?:[\s_-]+currents?)\b/i,
  coral: /\bcorals?\b/i,
  fish_school: /\bfish(?:[\s_-]+schools?)?\b/i,
  bubble_burst: /\bbubbles?(?:[\s_-]+bursts?)?\b/i
};

function allowedElementsForTheme(theme) {
  if (theme === "Taiwan") return THEME_ELEMENTS.Taiwan;
  if (theme === "Underwater") return THEME_ELEMENTS.Underwater;
  return [];
}

const AUTOMATIC_ADVISOR_ACTIONS = new Set([
  "NONE",
  "LEAVE_OPEN",
  "BALANCE",
  "SPREAD",
  "ADD",
  "VARY_SIZE"
]);

const PLACEMENT_ADVISOR_ACTIONS = new Set([
  "BALANCE",
  "SPREAD",
  "ADD"
]);

const COMPOSITION_ZONES = new Set([
  "TOP_LEFT", "TOP_CENTER", "TOP_RIGHT",
  "MIDDLE_LEFT", "MIDDLE_CENTER", "MIDDLE_RIGHT",
  "BOTTOM_LEFT", "BOTTOM_CENTER", "BOTTOM_RIGHT"
]);

function isAutomaticCompositionRequest(body) {
  return body?.automatic === true && body?.eventType !== "artwork_review";
}

function canonicalZoneName(value) {
  return typeof value === "string"
    ? value.trim().toUpperCase().replace(/[\s-]+/g, "_")
    : "";
}

function getAutomaticAdvisor(context) {
  return context?.advisor && typeof context.advisor === "object"
    ? context.advisor
    : {};
}

function deterministicAdvisorAction(body) {
  const advisor = getAutomaticAdvisor(body?.context);
  return {
    type: typeof advisor.action === "string" &&
      AUTOMATIC_ADVISOR_ACTIONS.has(advisor.action)
      ? advisor.action
      : "NONE",
    target: typeof advisor.element === "string" ? advisor.element : "",
    value: typeof advisor.zone === "string" ? advisor.zone : ""
  };
}

function getMentionedZones(text) {
  const normalized = String(text || "")
    .toLowerCase()
    .replace(/[_-]+/g, " ")
    .replace(/\s+/g, " ");
  const zones = new Set();
  const pattern = /\b(?:(top|upper|middle|bottom|lower)\s+(left|center|right)|(left|right)\s+center|center)\b/g;
  let match;

  while ((match = pattern.exec(normalized)) !== null) {
    if (match[1]) {
      const row = match[1] === "top" || match[1] === "upper"
        ? "TOP"
        : match[1] === "bottom" || match[1] === "lower"
          ? "BOTTOM"
          : "MIDDLE";
      const column = match[2].toUpperCase();
      zones.add(`${row}_${column}`);
    } else if (match[3]) {
      zones.add(`MIDDLE_${match[3].toUpperCase()}`);
    } else {
      zones.add("MIDDLE_CENTER");
    }
  }

  const compass = [
    ["TOP_LEFT", /\bnorth\s*west\b/],
    ["TOP_RIGHT", /\bnorth\s*east\b/],
    ["BOTTOM_LEFT", /\bsouth\s*west\b/],
    ["BOTTOM_RIGHT", /\bsouth\s*east\b/]
  ];
  for (const [zone, pattern] of compass) {
    if (pattern.test(normalized)) zones.add(zone);
  }

  return zones;
}

function countSentences(message) {
  const trimmed = String(message || "").trim();
  if (!trimmed) return 0;
  const marks = trimmed.match(/[.!?]+(?=\s|$)/g);
  return marks ? marks.length : 1;
}

function hasAlternativeElementRecommendation(message, recommendedElement, measuredElements) {
  const placementCue = /\b(?:add(?:ing)?|place(?:d|s|ing)?|put(?:ting)?|introduc(?:e|ing)|fill(?:ing)?|mov(?:e|ing)|spread(?:ing)?)\b/i;
  const sentences = String(message || "").split(/[.!?]+/);
  for (const [element, pattern] of Object.entries(ELEMENT_MENTION_PATTERNS)) {
    if (element === recommendedElement || !measuredElements.has(element)) continue;
    for (const sentence of sentences) {
      if (pattern.test(sentence) && placementCue.test(sentence)) return true;
    }
  }
  return false;
}

function validateAutomaticAdvisor(context) {
  const theme = context?.currentTheme;
  const allowed = allowedElementsForTheme(theme);
  if (!allowed.length) {
    return { valid: false, reason: "The current theme is missing or unsupported." };
  }

  const advisor = getAutomaticAdvisor(context);
  if (!AUTOMATIC_ADVISOR_ACTIONS.has(advisor.action)) {
    return { valid: false, reason: "Unity advisor action is missing or unsupported." };
  }

  const element = typeof advisor.element === "string" ? advisor.element : "";
  const zone = canonicalZoneName(advisor.zone);
  const measured = new Set(
    Array.isArray(context?.elements)
      ? context.elements
        .filter(item => item && typeof item.type === "string")
        .map(item => item.type)
      : []
  );

  if (advisor.action !== "NONE") {
    if (!allowed.includes(element)) {
      return { valid: false, reason: "Unity advisor element conflicts with the current theme." };
    }
    if (advisor.action !== "ADD" && !measured.has(element)) {
      return { valid: false, reason: "Unity advisor element is not supported by measured data." };
    }
    if (!COMPOSITION_ZONES.has(zone)) {
      return { valid: false, reason: "Unity advisor zone is missing or invalid." };
    }
  } else if (element && (!allowed.includes(element) || !measured.has(element))) {
    return { valid: false, reason: "Unity advisor element is not supported by measured data." };
  }

  const grid = Array.isArray(context?.grid) ? context.grid : [];
  let sourceZone = "";
  let sourceCount = -1;
  for (const item of grid) {
    if (!item || typeof item.name !== "string") continue;
    const count = Number(item.elementCount) || 0;
    if (count > sourceCount) {
      sourceZone = canonicalZoneName(item.name);
      sourceCount = count;
    }
  }

  if (PLACEMENT_ADVISOR_ACTIONS.has(advisor.action)) {
    const target = grid.find(item => canonicalZoneName(item?.name) === zone);
    if (!target) {
      return { valid: false, reason: "Unity advisor placement zone is absent from the measured grid." };
    }
    if (target.isCrowded !== false) {
      return { valid: false, reason: "Unity advisor placement zone is crowded or its crowd state is missing." };
    }
  }

  return { valid: true, reason: "", advisor, allowed, measured, zone, sourceZone };
}

function buildAdvisorFallback(body) {
  const context = body?.context || {};
  const advisor = context.advisor || {};
  const isReview = body?.eventType === "artwork_review";
  const automatic = isAutomaticCompositionRequest(body);
  const message =
    typeof advisor.fallbackMessage === "string" &&
    advisor.fallbackMessage.trim()
      ? advisor.fallbackMessage.trim()
      : "The active theme data is unavailable, so a theme-specific review is not ready.";

  return {
    message,
    feedbackType: isReview ? "review" : "hint",
    action: automatic
      ? deterministicAdvisorAction(body)
      : { type: "none", target: "", value: "" },
    priority: "normal",
    cooldownSeconds: 20
  };
}

function validateAIResponse(feedback, body) {
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
  const automatic = isAutomaticCompositionRequest(body);
  const advisor = getAutomaticAdvisor(body?.context);
  const actionTypeValid = automatic
    ? typeof feedback?.action?.type === "string"
    : validActions.includes(feedback?.action?.type);

  if (!feedback || typeof feedback !== "object" ||
      typeof feedback.message !== "string" ||
      !feedback.message.trim() ||
      !validFeedbackTypes.includes(feedback.feedbackType) ||
      !feedback.action || typeof feedback.action !== "object" ||
      !actionTypeValid ||
      typeof feedback.action.target !== "string" ||
      typeof feedback.action.value !== "string" ||
      !Number.isFinite(Number(feedback.cooldownSeconds)) ||
      Number(feedback.cooldownSeconds) <= 0) {
    return { valid: false, reason: "Malformed response structure." };
  }

  if (!(["normal", "high", "low"].includes(feedback.priority))) {
    return { valid: false, reason: "Invalid response priority." };
  }

  if (body?.eventType === "artwork_review" &&
      feedback.feedbackType !== "review") {
    return { valid: false, reason: "A manual review must use feedbackType review." };
  }

  const context = body?.context || {};
  const theme = context.currentTheme;
  const allowed = allowedElementsForTheme(theme);
  if (!allowed.length) {
    return { valid: false, reason: "The current theme is missing or unsupported." };
  }

  if (automatic) {
    const advisorValidation = validateAutomaticAdvisor(context);
    if (!advisorValidation.valid) return advisorValidation;
    if (feedback.action.type !== advisor.action ||
        feedback.action.target !== (advisor.element || "") ||
        feedback.action.value !== (advisor.zone || "")) {
      return {
        valid: false,
        reason: "Automatic response action, element, or zone differs from Unity's deterministic advisor."
      };
    }
    if (countSentences(feedback.message) > 2) {
      return { valid: false, reason: "Automatic response exceeds two sentences." };
    }

    const messageZones = getMentionedZones(feedback.message);
    for (const zone of messageZones) {
      if (zone !== advisorValidation.zone && zone !== advisorValidation.sourceZone) {
        return { valid: false, reason: "Automatic response mentions a different composition zone." };
      }
    }

    if (PLACEMENT_ADVISOR_ACTIONS.has(advisor.action) &&
        !messageZones.has(advisorValidation.zone)) {
      return { valid: false, reason: "Automatic response does not preserve Unity's target zone in its message." };
    }

    const placementLanguage = /\b(?:add(?:ing)?|place(?:d|s|ing)?|put(?:ting)?|introduc(?:e|ing)|fill(?:ing)?|mov(?:e|ing)|spread(?:ing)?)\b/i;
    if (advisor.action === "LEAVE_OPEN" && placementLanguage.test(feedback.message)) {
      return { valid: false, reason: "LEAVE_OPEN response contains a placement recommendation." };
    }
    if ((advisor.action === "NONE" || advisor.action === "VARY_SIZE") &&
        placementLanguage.test(feedback.message)) {
      return { valid: false, reason: "Automatic response contains a different composition action." };
    }
    if (hasAlternativeElementRecommendation(feedback.message, advisor.element, advisorValidation.measured)) {
      return { valid: false, reason: "Automatic response recommends a measured element other than Unity's advisor element." };
    }

    const actionLanguage = {
      ADD: /\b(?:add|adding|place|placing|put|try|consider|toward|near(?:by)?)\b/i,
      SPREAD: /\b(?:spread|spreading|place|placing|move|moving|try|consider|near|toward)\b/i,
      BALANCE: /\b(?:balance|balancing|place|placing|add|adding|try|consider|near|toward)\b/i,
      VARY_SIZE: /\b(?:size|scale|larger|smaller|vary|varying)\b/i,
      LEAVE_OPEN: /\b(?:leave|leaving|open|space|breathing room|stand out)\b/i
    };
    const requiredLanguage = actionLanguage[advisor.action];
    if (requiredLanguage && !requiredLanguage.test(feedback.message)) {
      return { valid: false, reason: "Automatic message does not express Unity's recommended action." };
    }
  }

  const present = new Set();
  if (Array.isArray(context.elements)) {
    for (const element of context.elements) {
      if (element && typeof element.type === "string") {
        present.add(element.type);
      }
    }
  }

  if (advisor.element && advisor.action !== "NONE") {
    if (!allowed.includes(advisor.element)) {
      return { valid: false, reason: "Advisor element conflicts with current theme." };
    }
    present.add(advisor.element);
  }

  const combinedText =
    feedback.message + " " +
    feedback.action.target + " " +
    feedback.action.value;

  for (const [element, pattern] of Object.entries(ELEMENT_MENTION_PATTERNS)) {
    if (!pattern.test(combinedText)) continue;
    if (!allowed.includes(element)) {
      return {
        valid: false,
        reason: "Response mentions an element forbidden by the current theme."
      };
    }
    if (!present.has(element)) {
      return {
        valid: false,
        reason: "Response references an element absent from Unity data and advisor."
      };
    }
  }

  if (!automatic && feedback.action.type === "show_composition_hint" &&
      feedback.action.value) {
    const requestedZone = feedback.action.value.toUpperCase().replace(/-/g, "_");
    const advisorZone = typeof advisor.zone === "string"
      ? advisor.zone.toUpperCase().replace(/-/g, "_")
      : "";
    if (requestedZone !== advisorZone) {
      return {
        valid: false,
        reason: "Response action location differs from the deterministic advisor."
      };
    }
  }

  return { valid: true, reason: "" };
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
  return buildAdvisorFallback(body);
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

        automatic:
          body.automatic === true,

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
    return buildAdvisorFallback(body);
  }

  const validation =
    validateAIResponse(parsed, body);

  if (!validation.valid) {
    console.warn(
      "[OPENAI RESPONSE REJECTED]",
      validation.reason
    );
    return buildAdvisorFallback(body);
  }

  const normalized = normalizeFeedback(parsed);
  if (isAutomaticCompositionRequest(body)) {
    // The model's fields were checked above; keep Unity's exact action contract.
    normalized.action = deterministicAdvisorAction(body);
  }
  return normalized;
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