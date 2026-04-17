const test = require("node:test");
const assert = require("node:assert/strict");

const { buildSystemPrompt, buildUserPrompt } = require("../src");

const templates = [
  {
    id: "react-feature-based-starter",
    name: "React Feature Based Starter",
    description: "React starter",
    language: "TypeScript",
    framework: "React",
    tags: ["react"],
    requiredVariables: ["projectName", "appTitle"],
    options: [
      { key: "includeRouter", label: "Include Router", type: "boolean", default: true },
      { key: "includeTesting", label: "Include Testing", type: "boolean", default: true }
    ]
  }
];

test("buildSystemPrompt includes template restrictions and JSON-only instructions", () => {
  const prompt = buildSystemPrompt({ templates });

  assert.match(prompt, /Allowed templateIds:/);
  assert.match(prompt, /react-feature-based-starter/);
  assert.match(prompt, /options=\[includeRouter, includeTesting\]/);
  assert.match(prompt, /Output valid JSON only/);
});

test("buildUserPrompt includes the request and template catalog", () => {
  const prompt = buildUserPrompt({
    request: "Create a React admin dashboard with routing.",
    templates,
  });

  assert.match(prompt, /Create a React admin dashboard with routing/);
  assert.match(prompt, /Template catalog:/);
  assert.match(prompt, /react-feature-based-starter/);
});
