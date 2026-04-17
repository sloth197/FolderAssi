const path = require("node:path");
const test = require("node:test");
const assert = require("node:assert/strict");

const { TemplateLoader } = require("../../template-registry/src");
const { renderTemplate, InvalidConditionKeyError } = require("../src");

test("renderTemplate builds the final tree with substituted names and content", async () => {
  const loader = new TemplateLoader({
    templatesRoot: path.join(__dirname, "..", "..", "..", "templates"),
  });
  const template = await loader.getTemplateById("react-feature-based-starter");

  const rendered = renderTemplate({
    template,
    variables: {
      projectName: "admin-console",
      appTitle: "Admin Console",
    },
    options: {
      includeRouter: true,
      includeTesting: false,
      styling: "css-modules",
    },
  });

  assert.equal(rendered.root.name, "admin-console");
  assert.ok(rendered.manifest.directories.includes("admin-console/src"));
  assert.ok(rendered.manifest.files.includes("admin-console/package.json"));
  assert.ok(
    rendered.manifest.files.includes(
      "admin-console/src/app/providers/RouterProvider.tsx",
    ),
  );
  assert.ok(
    !rendered.manifest.directories.includes("admin-console/src/test"),
  );

  const readme = findNode(rendered.root, "admin-console/README.md");
  assert.match(readme.content, /Admin Console/);
});

test("renderTemplate excludes conditional nodes when the option is false", async () => {
  const loader = new TemplateLoader({
    templatesRoot: path.join(__dirname, "..", "..", "..", "templates"),
  });
  const template = await loader.getTemplateById("spring-boot-layered-api-starter");

  const rendered = renderTemplate({
    template,
    variables: {
      projectName: "order-service",
      groupId: "com.example",
      artifactId: "order-service",
      packageRoot: "com",
      packageDomain: "example",
      packageName: "orders",
    },
    options: {
      includeDocker: false,
      includeSwagger: false,
      buildTool: "maven",
    },
  });

  assert.ok(!rendered.manifest.files.includes("order-service/Dockerfile"));
  assert.ok(
    !rendered.manifest.files.includes(
      "order-service/src/main/java/com/example/orders/config/OpenApiConfig.java",
    ),
  );
});

test("renderTemplate throws for unresolved condition keys", () => {
  assert.throws(
    () =>
      renderTemplate({
        template: {
          id: "broken",
          templateVersion: "1.0.0",
          defaultVariables: {},
          requiredVariables: [],
          options: [],
          root: {
            name: "broken",
            type: "folder",
            overwritePolicy: "error",
            children: [
              {
                name: "debug.txt",
                type: "file",
                contentTemplate: "x",
                conditionKey: "missingFlag",
                overwritePolicy: "overwrite",
              },
            ],
          },
        },
      }),
    (error) => error instanceof InvalidConditionKeyError,
  );
});

function findNode(node, targetPath) {
  if (node.path === targetPath) {
    return node;
  }

  if (node.type === "folder") {
    for (const child of node.children || []) {
      const result = findNode(child, targetPath);
      if (result) {
        return result;
      }
    }
  }

  return null;
}
