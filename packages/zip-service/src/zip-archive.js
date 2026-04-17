const fs = require("node:fs/promises");
const path = require("node:path");
const { spawn } = require("node:child_process");

const {
  ZipArchiveCreationError,
  ZipArchiveExtractionError,
} = require("./errors");

async function createZipFromDirectory({ sourceDir, outputZipPath } = {}) {
  if (!sourceDir) {
    throw new TypeError("sourceDir is required.");
  }

  if (!outputZipPath) {
    throw new TypeError("outputZipPath is required.");
  }

  const absoluteSourceDir = path.resolve(sourceDir);
  const absoluteOutputZipPath = path.resolve(outputZipPath);

  await ensureDirectoryExists(absoluteSourceDir);
  await fs.mkdir(path.dirname(absoluteOutputZipPath), { recursive: true });
  await fs.rm(absoluteOutputZipPath, { force: true });

  const script = [
    `$source = '${escapePowerShellLiteral(absoluteSourceDir)}'`,
    `$destination = '${escapePowerShellLiteral(absoluteOutputZipPath)}'`,
    "Compress-Archive -LiteralPath $source -DestinationPath $destination -Force",
  ].join("; ");

  await runPowerShell(script, ZipArchiveCreationError, {
    sourceDir: absoluteSourceDir,
    outputZipPath: absoluteOutputZipPath,
  });

  return {
    sourceDir: absoluteSourceDir,
    outputZipPath: absoluteOutputZipPath,
  };
}

async function extractZipToDirectory({ zipPath, destinationDir } = {}) {
  if (!zipPath) {
    throw new TypeError("zipPath is required.");
  }

  if (!destinationDir) {
    throw new TypeError("destinationDir is required.");
  }

  const absoluteZipPath = path.resolve(zipPath);
  const absoluteDestinationDir = path.resolve(destinationDir);

  await ensureFileExists(absoluteZipPath);
  await fs.mkdir(absoluteDestinationDir, { recursive: true });

  const script = [
    `$archive = '${escapePowerShellLiteral(absoluteZipPath)}'`,
    `$destination = '${escapePowerShellLiteral(absoluteDestinationDir)}'`,
    "Expand-Archive -LiteralPath $archive -DestinationPath $destination -Force",
  ].join("; ");

  await runPowerShell(script, ZipArchiveExtractionError, {
    zipPath: absoluteZipPath,
    destinationDir: absoluteDestinationDir,
  });

  return {
    zipPath: absoluteZipPath,
    destinationDir: absoluteDestinationDir,
  };
}

async function ensureDirectoryExists(targetPath) {
  let stats;

  try {
    stats = await fs.stat(targetPath);
  } catch (error) {
    if (error && error.code === "ENOENT") {
      throw new ZipArchiveCreationError(`Source directory not found: ${targetPath}`, {
        sourceDir: targetPath,
      });
    }

    throw error;
  }

  if (!stats.isDirectory()) {
    throw new ZipArchiveCreationError(`Source path is not a directory: ${targetPath}`, {
      sourceDir: targetPath,
    });
  }
}

async function ensureFileExists(targetPath) {
  let stats;

  try {
    stats = await fs.stat(targetPath);
  } catch (error) {
    if (error && error.code === "ENOENT") {
      throw new ZipArchiveExtractionError(`ZIP file not found: ${targetPath}`, {
        zipPath: targetPath,
      });
    }

    throw error;
  }

  if (!stats.isFile()) {
    throw new ZipArchiveExtractionError(`ZIP path is not a file: ${targetPath}`, {
      zipPath: targetPath,
    });
  }
}

function runPowerShell(script, ErrorType, details) {
  return new Promise((resolve, reject) => {
    const child = spawn(
      "powershell",
      ["-NoProfile", "-NonInteractive", "-Command", script],
      {
        stdio: ["ignore", "pipe", "pipe"],
      },
    );

    let stderr = "";

    child.stderr.on("data", (chunk) => {
      stderr += chunk.toString();
    });

    child.on("error", (error) => {
      reject(
        new ErrorType(`PowerShell execution failed: ${error.message}`, {
          ...details,
          cause: error.message,
        }),
      );
    });

    child.on("close", (code) => {
      if (code === 0) {
        resolve();
        return;
      }

      reject(
        new ErrorType("ZIP command failed.", {
          ...details,
          code,
          stderr: stderr.trim(),
        }),
      );
    });
  });
}

function escapePowerShellLiteral(value) {
  return String(value).replace(/'/g, "''");
}

module.exports = {
  createZipFromDirectory,
  extractZipToDirectory,
};
