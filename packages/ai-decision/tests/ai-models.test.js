const test = require("node:test");
const assert = require("node:assert/strict");

const { aiInputModel, aiOutputModel, createTemplateSummary } = require("../src");

test("aiInputModel exposes the expected fields", () => {
  assert.deepEqual(Object.keys(aiInputModel.fields), ["request", "templates"]);
});

test("aiOutputModel exposes the expected fields", () => {
  assert.deepEqual(Object.keys(aiOutputModel.fields), [
    "templateId",
    "variables",
    "options",
    "confidence",
    "notes",
  ]);
});

test("createTemplateSummary reduces a full template to AI-safe metadata", () => {
  const summary = createTemplateSummary({
    id: "react-feature-based-starter",
    name: "React Feature Based Starter",
    description: "React starter",
    language: "TypeScript",
    framework: "React",
    tags: ["react"],
    requiredVariables: ["projectName"],
    options: [
      { key: "includeRouter", label: "Include Router", type: "boolean", default: true },
    ],
    root: {
      name: "{{projectName}}",
      type: "folder",
      overwritePolicy: "error",
      children: [],
    },
  });

  assert.deepEqual(summary, {
    id: "react-feature-based-starter",
    name: "React Feature Based Starter",
    description: "React starter",
    language: "TypeScript",
    framework: "React",
    tags: ["react"],
    requiredVariables: ["projectName"],
    optionKeys: ["includeRouter"],
  });
});
