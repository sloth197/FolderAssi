const test = require("node:test");
const assert = require("node:assert/strict");

const {
  TemplateNodeType,
  TemplateOptionType,
  TemplateEncoding,
  OverwritePolicy,
  templateModels,
} = require("../src");

test("ProjectTemplate model exposes all required fields", () => {
  assert.deepEqual(Object.keys(templateModels.ProjectTemplate.fields), [
    "id",
    "name",
    "description",
    "language",
    "framework",
    "templateVersion",
    "tags",
    "defaultVariables",
    "requiredVariables",
    "options",
    "root",
  ]);
});

test("TemplateNode model exposes all required fields", () => {
  assert.deepEqual(Object.keys(templateModels.TemplateNode.fields), [
    "name",
    "type",
    "children",
    "contentTemplate",
    "optional",
    "conditionKey",
    "encoding",
    "overwritePolicy",
  ]);
});

test("TemplateOption model exposes all required fields", () => {
  assert.deepEqual(Object.keys(templateModels.TemplateOption.fields), [
    "key",
    "label",
    "type",
    "default",
  ]);
});

test("Template enums expose the supported values", () => {
  assert.deepEqual(Object.values(TemplateNodeType), ["folder", "file"]);
  assert.deepEqual(Object.values(TemplateOptionType), [
    "string",
    "boolean",
    "number",
    "enum",
  ]);
  assert.deepEqual(Object.values(TemplateEncoding), ["utf8", "base64", "binary"]);
  assert.deepEqual(Object.values(OverwritePolicy), [
    "skip",
    "overwrite",
    "error",
    "merge",
  ]);
});
