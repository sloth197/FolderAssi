const test = require("node:test");
const assert = require("node:assert/strict");

const { interpolateValue } = require("../src");

test("interpolateValue replaces placeholders in file names", () => {
  const value = interpolateValue("{{projectName}}.csproj", {
    variables: { projectName: "CatalogService" },
    options: {},
  });

  assert.equal(value, "CatalogService.csproj");
});

test("interpolateValue replaces placeholders in folder names", () => {
  const value = interpolateValue("{{packageRoot}}", {
    variables: { packageRoot: "com" },
    options: {},
  });

  assert.equal(value, "com");
});

test("interpolateValue replaces placeholders in file content", () => {
  const value = interpolateValue("# {{appTitle}}\nrouter={{includeRouter}}", {
    variables: { appTitle: "Admin Console" },
    options: { includeRouter: true },
  });

  assert.equal(value, "# Admin Console\nrouter=true");
});

test("interpolateValue supports explicit variables and options paths", () => {
  const value = interpolateValue("{{variables.projectName}}::{{options.styling}}", {
    variables: { projectName: "admin-console" },
    options: { styling: "css-modules" },
  });

  assert.equal(value, "admin-console::css-modules");
});
