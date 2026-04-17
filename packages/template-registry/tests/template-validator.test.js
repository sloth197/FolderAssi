const test = require("node:test");
const assert = require("node:assert/strict");

const {
  validateTemplate,
  assertValidTemplate,
  TemplateValidationError,
} = require("../src");

test("validateTemplate accepts a valid template", () => {
  const template = createValidTemplate();

  const result = validateTemplate(template);

  assert.equal(result.valid, true);
  assert.equal(result.errors.length, 0);
});

test("validateTemplate reports missing required fields", () => {
  const result = validateTemplate({});

  assert.equal(result.valid, false);
  assert.ok(result.errors.some((error) => error.code === "missing_required_field"));
});

test("validateTemplate reports missing node names", () => {
  const template = createValidTemplate();
  template.root.children[0].name = "";

  const result = validateTemplate(template);

  assert.ok(result.errors.some((error) => error.code === "missing_name"));
});

test("validateTemplate reports file nodes without contentTemplate", () => {
  const template = createValidTemplate();
  delete template.root.children[0].contentTemplate;

  const result = validateTemplate(template);

  assert.ok(result.errors.some((error) => error.code === "missing_content_template"));
});

test("validateTemplate reports duplicate child names", () => {
  const template = createValidTemplate();
  template.root.children.push({
    name: "README.md",
    type: "file",
    contentTemplate: "# duplicate",
    overwritePolicy: "overwrite",
  });

  const result = validateTemplate(template);

  assert.ok(result.errors.some((error) => error.code === "duplicate_name"));
});

test("validateTemplate reports invalid node types", () => {
  const template = createValidTemplate();
  template.root.children[0].type = "document";

  const result = validateTemplate(template);

  assert.ok(result.errors.some((error) => error.code === "invalid_type"));
});

test("assertValidTemplate throws TemplateValidationError on failure", () => {
  assert.throws(
    () => assertValidTemplate({}),
    (error) => error instanceof TemplateValidationError,
  );
});

function createValidTemplate() {
  return {
    id: "sample-template",
    name: "Sample Template",
    description: "Valid template for testing.",
    language: "TypeScript",
    framework: "React",
    templateVersion: "1.0.0",
    tags: ["sample"],
    defaultVariables: {
      projectName: "sample-app",
    },
    requiredVariables: ["projectName"],
    options: [
      {
        key: "includeRouter",
        label: "Include Router",
        type: "boolean",
        default: true,
      },
    ],
    root: {
      name: "{{projectName}}",
      type: "folder",
      overwritePolicy: "error",
      children: [
        {
          name: "README.md",
          type: "file",
          contentTemplate: "# {{projectName}}",
          overwritePolicy: "overwrite",
        },
      ],
    },
  };
}
