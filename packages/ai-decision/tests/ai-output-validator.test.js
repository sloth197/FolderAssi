const test = require("node:test");
const assert = require("node:assert/strict");

const {
  validateAiOutput,
  assertValidAiOutput,
  AiOutputValidationError,
} = require("../src");

const templates = [
  {
    id: "react-feature-based-starter",
    name: "React Feature Based Starter",
    description: "React starter",
    language: "TypeScript",
    framework: "React",
    templateVersion: "1.0.0",
    tags: ["react"],
    defaultVariables: {
      projectName: "feature-react-app",
      appTitle: "Feature React App",
    },
    requiredVariables: ["projectName", "appTitle"],
    options: [
      { key: "includeRouter", label: "Include Router", type: "boolean", default: true },
      { key: "styling", label: "Styling", type: "string", default: "css-modules" },
    ],
    root: {
      name: "{{projectName}}",
      type: "folder",
      overwritePolicy: "error",
      children: [],
    },
  },
];

test("validateAiOutput accepts valid AI output", () => {
  const result = validateAiOutput(
    {
      templateId: "react-feature-based-starter",
      variables: {
        projectName: "admin-console",
        appTitle: "Admin Console",
      },
      options: {
        includeRouter: true,
        styling: "css-modules",
      },
      confidence: 0.91,
      notes: ["Inferred a feature-based React app from the request."],
    },
    templates,
  );

  assert.equal(result.valid, true);
  assert.equal(result.errors.length, 0);
});

test("validateAiOutput reports missing templateId", () => {
  const result = validateAiOutput(
    {
      variables: {},
      options: {},
      confidence: 0.5,
      notes: [],
    },
    templates,
  );

  assert.ok(result.errors.some((error) => error.code === "missing_template_id"));
});

test("validateAiOutput reports unknown variable keys", () => {
  const result = validateAiOutput(
    {
      templateId: "react-feature-based-starter",
      variables: {
        projectName: "admin-console",
        unknownKey: "x",
      },
      options: {},
      confidence: 0.5,
      notes: [],
    },
    templates,
  );

  assert.ok(result.errors.some((error) => error.code === "unknown_variable_key"));
});

test("validateAiOutput reports missing required variables without defaults", () => {
  const templateWithoutDefaults = [
    {
      ...templates[0],
      defaultVariables: {
        projectName: "feature-react-app",
      },
      requiredVariables: ["projectName", "appTitle"],
    },
  ];

  const result = validateAiOutput(
    {
      templateId: "react-feature-based-starter",
      variables: {
        projectName: "admin-console",
      },
      options: {},
      confidence: 0.5,
      notes: [],
    },
    templateWithoutDefaults,
  );

  assert.ok(result.errors.some((error) => error.code === "missing_required_variable"));
});

test("validateAiOutput reports unknown option keys", () => {
  const result = validateAiOutput(
    {
      templateId: "react-feature-based-starter",
      variables: {
        projectName: "admin-console",
        appTitle: "Admin Console",
      },
      options: {
        includeRouter: true,
        unknownOption: true,
      },
      confidence: 0.5,
      notes: [],
    },
    templates,
  );

  assert.ok(result.errors.some((error) => error.code === "unknown_option_key"));
});

test("validateAiOutput reports invalid option types", () => {
  const result = validateAiOutput(
    {
      templateId: "react-feature-based-starter",
      variables: {
        projectName: "admin-console",
        appTitle: "Admin Console",
      },
      options: {
        includeRouter: "yes",
        styling: "css-modules",
      },
      confidence: 0.5,
      notes: [],
    },
    templates,
  );

  assert.ok(result.errors.some((error) => error.code === "invalid_option_type"));
});

test("assertValidAiOutput throws AiOutputValidationError on failure", () => {
  assert.throws(
    () => assertValidAiOutput({ templateId: "" }, templates),
    (error) => error instanceof AiOutputValidationError,
  );
});
