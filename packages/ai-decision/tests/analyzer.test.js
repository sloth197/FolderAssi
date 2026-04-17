const path = require("node:path");
const test = require("node:test");
const assert = require("node:assert/strict");

const { TemplateLoader } = require("../../template-registry/src");
const { analyzeProjectRequest, assertValidAiOutput } = require("../src");

test("analyzeProjectRequest picks the React template for frontend requests", async () => {
  const templates = await loadTemplates();

  const result = analyzeProjectRequest({
    request: 'Create a React dashboard with routing and tests called "Admin Console"',
    templates,
  });

  assert.equal(result.templateId, "react-feature-based-starter");
  assert.equal(result.options.includeRouter, true);
  assert.equal(result.options.includeTesting, true);
  assert.equal(result.variables.projectName, "admin-console");
  assertValidAiOutput(result, templates);
});

test("analyzeProjectRequest picks the Spring Boot template for Java API requests", async () => {
  const templates = await loadTemplates();

  const result = analyzeProjectRequest({
    request: 'Build a layered Spring Boot API with Docker for "Order Service"',
    templates,
  });

  assert.equal(result.templateId, "spring-boot-layered-api-starter");
  assert.equal(result.options.includeDocker, true);
  assert.equal(result.variables.projectName, "order-service");
  assertValidAiOutput(result, templates);
});

async function loadTemplates() {
  const loader = new TemplateLoader({
    templatesRoot: path.join(__dirname, "..", "..", "..", "templates"),
  });

  return loader.listTemplates();
}
