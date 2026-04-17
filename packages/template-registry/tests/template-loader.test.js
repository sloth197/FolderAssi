const path = require("node:path");
const test = require("node:test");
const assert = require("node:assert/strict");

const {
  TemplateLoader,
  TemplateDirectoryNotFoundError,
  TemplateFileNotFoundError,
  TemplateParseError,
} = require("../src");

const fixturesRoot = path.join(__dirname, "fixtures");

test("loadFromFile reads and parses a template JSON file", async () => {
  const loader = new TemplateLoader({
    templatesRoot: path.join(fixturesRoot, "flat-valid-templates"),
  });

  const template = await loader.loadFromFile(
    path.join(fixturesRoot, "flat-valid-templates", "nextjs-saas-basic.json"),
  );

  assert.equal(template.id, "nextjs-saas-basic");
  assert.equal(template.name, "Next.js SaaS Basic");
});

test("listTemplates returns every template file in sorted order", async () => {
  const loader = new TemplateLoader({
    templatesRoot: path.join(fixturesRoot, "flat-valid-templates"),
  });

  const templates = await loader.listTemplates();

  assert.deepEqual(
    templates.map((template) => template.id),
    ["express-api-basic", "nextjs-saas-basic"],
  );
});

test("getTemplateById returns a single template by templateId", async () => {
  const loader = new TemplateLoader({
    templatesRoot: path.join(fixturesRoot, "flat-valid-templates"),
  });

  const template = await loader.getTemplateById("express-api-basic");

  assert.equal(template.id, "express-api-basic");
  assert.equal(template.templateVersion, "1.0.0");
});

test("getTemplateById throws TemplateParseError for invalid JSON", async () => {
  const loader = new TemplateLoader({
    templatesRoot: path.join(fixturesRoot, "flat-invalid-json"),
  });

  await assert.rejects(
    () => loader.getTemplateById("broken-template"),
    (error) => error instanceof TemplateParseError,
  );
});

test("getTemplateById throws TemplateFileNotFoundError when a template file is missing", async () => {
  const loader = new TemplateLoader({
    templatesRoot: path.join(fixturesRoot, "flat-valid-templates"),
  });

  await assert.rejects(
    () => loader.getTemplateById("missing-template"),
    (error) => error instanceof TemplateFileNotFoundError,
  );
});

test("listTemplates throws TemplateDirectoryNotFoundError when root folder does not exist", async () => {
  const loader = new TemplateLoader({
    templatesRoot: path.join(fixturesRoot, "does-not-exist"),
  });

  await assert.rejects(
    () => loader.listTemplates(),
    (error) => error instanceof TemplateDirectoryNotFoundError,
  );
});
