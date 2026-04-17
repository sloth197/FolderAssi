const fs = require("node:fs/promises");
const path = require("node:path");

const { OverwritePolicyError, TemplateGenerationError } = require("./errors");
const { renderTemplate } = require("./renderer");

async function generateProject({
  template,
  variables = {},
  options = {},
  destinationRoot,
} = {}) {
  if (!destinationRoot) {
    throw new TemplateGenerationError("destinationRoot is required.");
  }

  const rendered = renderTemplate({ template, variables, options });
  const createdDirectories = [];
  const createdFiles = [];
  const skippedPaths = [];

  await createNode(rendered.root, path.resolve(destinationRoot), {
    createdDirectories,
    createdFiles,
    skippedPaths,
  });

  return {
    ...rendered,
    outputPath: path.join(path.resolve(destinationRoot), rendered.root.name),
    createdDirectories,
    createdFiles,
    skippedPaths,
  };
}

async function createNode(node, parentPath, results) {
  const targetPath = path.join(parentPath, node.name);

  if (node.type === "folder") {
    const folderCreated = await ensureFolder(targetPath, node.overwritePolicy);
    if (!folderCreated.shouldContinue) {
      results.skippedPaths.push(node.path);
      return;
    }

    if (folderCreated.created) {
      results.createdDirectories.push(node.path);
    }

    for (const child of node.children || []) {
      await createNode(child, targetPath, results);
    }

    return;
  }

  await fs.mkdir(parentPath, { recursive: true });
  const fileCreated = await writeFileNode(targetPath, node);

  if (fileCreated.skipped) {
    results.skippedPaths.push(node.path);
    return;
  }

  results.createdFiles.push(node.path);
}

async function ensureFolder(targetPath, overwritePolicy) {
  const existing = await getExistingPathType(targetPath);

  if (!existing) {
    await fs.mkdir(targetPath, { recursive: false });
    return { created: true, shouldContinue: true };
  }

  if (existing !== "folder") {
    throw new OverwritePolicyError(`Expected folder at "${targetPath}" but found file.`, {
      targetPath,
      overwritePolicy,
    });
  }

  if (overwritePolicy === "skip") {
    return { created: false, shouldContinue: false };
  }

  if (overwritePolicy === "error") {
    throw new OverwritePolicyError(`Folder already exists at "${targetPath}".`, {
      targetPath,
      overwritePolicy,
    });
  }

  return { created: false, shouldContinue: true };
}

async function writeFileNode(targetPath, node) {
  const existing = await getExistingPathType(targetPath);

  if (existing === "folder") {
    throw new OverwritePolicyError(`Expected file at "${targetPath}" but found folder.`, {
      targetPath,
      overwritePolicy: node.overwritePolicy,
    });
  }

  if (existing === "file") {
    if (node.overwritePolicy === "skip") {
      return { skipped: true };
    }

    if (node.overwritePolicy === "error") {
      throw new OverwritePolicyError(`File already exists at "${targetPath}".`, {
        targetPath,
        overwritePolicy: node.overwritePolicy,
      });
    }
  }

  await fs.writeFile(targetPath, toWritableValue(node.content, node.encoding));
  return { skipped: false };
}

async function getExistingPathType(targetPath) {
  try {
    const stats = await fs.stat(targetPath);
    if (stats.isDirectory()) {
      return "folder";
    }

    if (stats.isFile()) {
      return "file";
    }

    return "other";
  } catch (error) {
    if (error && error.code === "ENOENT") {
      return null;
    }

    throw error;
  }
}

function toWritableValue(content, encoding) {
  const safeContent = content || "";

  if (encoding === "base64") {
    return Buffer.from(safeContent, "base64");
  }

  if (encoding === "binary") {
    return Buffer.from(safeContent, "binary");
  }

  return safeContent;
}

module.exports = {
  generateProject,
};
