const fs = require("node:fs/promises");
const os = require("node:os");
const path = require("node:path");
const test = require("node:test");
const assert = require("node:assert/strict");

const { generateProject, OverwritePolicyError } = require("../src");

test("generateProject creates folders and files recursively", async () => {
  const tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), "folderassi-generate-"));

  try {
    const result = await generateProject({
      template: createTemplate(),
      variables: {
        projectName: "demo-app",
      },
      options: {
        includeDocs: true,
      },
      destinationRoot: tempRoot,
    });

    const readmePath = path.join(tempRoot, "demo-app", "README.md");
    const docsPath = path.join(tempRoot, "demo-app", "docs", "guide.txt");

    assert.equal(result.outputPath, path.join(tempRoot, "demo-app"));
    assert.equal(await fs.readFile(readmePath, "utf8"), "# demo-app");
    assert.equal(await fs.readFile(docsPath, "utf8"), "Guide for demo-app");
    assert.ok(result.createdDirectories.includes("demo-app"));
    assert.ok(result.createdFiles.includes("demo-app/README.md"));
  } finally {
    await fs.rm(tempRoot, { recursive: true, force: true });
  }
});

test("generateProject applies encoding when writing files", async () => {
  const tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), "folderassi-generate-"));

  try {
    await generateProject({
      template: {
        id: "binary-template",
        templateVersion: "1.0.0",
        defaultVariables: {
          projectName: "binary-app",
        },
        requiredVariables: ["projectName"],
        options: [],
        root: {
          name: "{{projectName}}",
          type: "folder",
          overwritePolicy: "error",
          children: [
            {
              name: "hello.txt",
              type: "file",
              contentTemplate: "SGVsbG8=",
              encoding: "base64",
              overwritePolicy: "overwrite",
            },
          ],
        },
      },
      destinationRoot: tempRoot,
    });

    const content = await fs.readFile(
      path.join(tempRoot, "binary-app", "hello.txt"),
      "utf8",
    );

    assert.equal(content, "Hello");
  } finally {
    await fs.rm(tempRoot, { recursive: true, force: true });
  }
});

test("generateProject applies overwrite policies for files", async () => {
  const tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), "folderassi-generate-"));

  try {
    const projectRoot = path.join(tempRoot, "demo-app");
    await fs.mkdir(projectRoot, { recursive: true });
    await fs.writeFile(path.join(projectRoot, "README.md"), "existing");

    await assert.rejects(
      () =>
        generateProject({
          template: createTemplate(),
          variables: {
            projectName: "demo-app",
          },
          options: {
            includeDocs: false,
          },
          destinationRoot: tempRoot,
        }),
      (error) => error instanceof OverwritePolicyError,
    );

    await generateProject({
      template: createTemplate({
        rootOverwritePolicy: "merge",
        readmeOverwritePolicy: "skip",
      }),
      variables: {
        projectName: "demo-app",
      },
      options: {
        includeDocs: false,
      },
      destinationRoot: tempRoot,
    });

    const skippedContent = await fs.readFile(path.join(projectRoot, "README.md"), "utf8");
    assert.equal(skippedContent, "existing");

    await generateProject({
      template: createTemplate({
        rootOverwritePolicy: "merge",
        readmeOverwritePolicy: "overwrite",
      }),
      variables: {
        projectName: "demo-app",
      },
      options: {
        includeDocs: false,
      },
      destinationRoot: tempRoot,
    });

    const overwrittenContent = await fs.readFile(
      path.join(projectRoot, "README.md"),
      "utf8",
    );
    assert.equal(overwrittenContent, "# demo-app");
  } finally {
    await fs.rm(tempRoot, { recursive: true, force: true });
  }
});

function createTemplate(overrides = {}) {
  const rootOverwritePolicy = overrides.rootOverwritePolicy || "error";
  const readmeOverwritePolicy = overrides.readmeOverwritePolicy || "error";

  return {
    id: "demo-template",
    templateVersion: "1.0.0",
    defaultVariables: {
      projectName: "demo-app",
    },
    requiredVariables: ["projectName"],
    options: [
      {
        key: "includeDocs",
        label: "Include Docs",
        type: "boolean",
        default: false,
      },
    ],
    root: {
      name: "{{projectName}}",
      type: "folder",
      overwritePolicy: rootOverwritePolicy,
      children: [
        {
          name: "README.md",
          type: "file",
          contentTemplate: "# {{projectName}}",
          overwritePolicy: readmeOverwritePolicy,
        },
        {
          name: "docs",
          type: "folder",
          conditionKey: "includeDocs",
          overwritePolicy: "error",
          children: [
            {
              name: "guide.txt",
              type: "file",
              contentTemplate: "Guide for {{projectName}}",
              overwritePolicy: "overwrite",
            },
          ],
        },
      ],
    },
  };
}
