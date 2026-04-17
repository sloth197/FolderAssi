const fs = require("node:fs/promises");
const os = require("node:os");
const path = require("node:path");
const test = require("node:test");
const assert = require("node:assert/strict");

const {
  createZipFromDirectory,
  extractZipToDirectory,
  ZipArchiveCreationError,
} = require("../src");

test("createZipFromDirectory creates a ZIP while preserving folder structure", async () => {
  const tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), "folderassi-zip-"));
  const sourceDir = path.join(tempRoot, "demo-app");
  const zipPath = path.join(tempRoot, "artifacts", "demo-app.zip");
  const extractDir = path.join(tempRoot, "unzipped");

  try {
    await fs.mkdir(path.join(sourceDir, "src"), { recursive: true });
    await fs.writeFile(path.join(sourceDir, "README.md"), "# Demo App");
    await fs.writeFile(path.join(sourceDir, "src", "index.js"), "console.log('demo');");

    const result = await createZipFromDirectory({
      sourceDir,
      outputZipPath: zipPath,
    });

    assert.equal(result.outputZipPath, zipPath);

    await extractZipToDirectory({
      zipPath,
      destinationDir: extractDir,
    });

    const extractedReadme = await fs.readFile(
      path.join(extractDir, "demo-app", "README.md"),
      "utf8",
    );
    const extractedIndex = await fs.readFile(
      path.join(extractDir, "demo-app", "src", "index.js"),
      "utf8",
    );

    assert.equal(extractedReadme, "# Demo App");
    assert.equal(extractedIndex, "console.log('demo');");
  } finally {
    await fs.rm(tempRoot, { recursive: true, force: true });
  }
});

test("createZipFromDirectory throws when the source folder does not exist", async () => {
  const tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), "folderassi-zip-"));

  try {
    await assert.rejects(
      () =>
        createZipFromDirectory({
          sourceDir: path.join(tempRoot, "missing"),
          outputZipPath: path.join(tempRoot, "missing.zip"),
        }),
      (error) => error instanceof ZipArchiveCreationError,
    );
  } finally {
    await fs.rm(tempRoot, { recursive: true, force: true });
  }
});
