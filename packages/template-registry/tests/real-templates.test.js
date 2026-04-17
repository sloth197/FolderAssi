const path = require("node:path");
const test = require("node:test");
const assert = require("node:assert/strict");

const { TemplateLoader, assertValidTemplate } = require("../src");

test("real templates load and validate successfully", async () => {
  const templatesRoot = path.join(__dirname, "..", "..", "..", "templates");
  const loader = new TemplateLoader({ templatesRoot });

  const templates = await loader.listTemplates();

  assert.deepEqual(
    templates.map((template) => template.id),
    [
      "aspnetcore-webapi-starter",
      "react-feature-based-starter",
      "spring-boot-layered-api-starter",
    ],
  );

  templates.forEach((template) => {
    assert.doesNotThrow(() => assertValidTemplate(template));
  });
});
