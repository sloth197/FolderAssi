const fs = require("node:fs/promises");
const path = require("node:path");

const {
  TemplateDirectoryNotFoundError,
  TemplateFileNotFoundError,
  TemplateParseError,
} = require("./errors");

class TemplateLoader {
  constructor({ templatesRoot, templateExtension = ".json" } = {}) {
    if (!templatesRoot) {
      throw new TypeError("templatesRoot is required.");
    }

    this.templatesRoot = path.resolve(templatesRoot);
    this.templateExtension = templateExtension;
  }

  async loadFromFile(templateFilePath) {
    const absoluteFilePath = path.resolve(templateFilePath);
    const rawTemplate = await this.#readTemplateFile(absoluteFilePath);

    return this.#validateTemplateShape(rawTemplate, absoluteFilePath);
  }

  async listTemplates() {
    const templateFiles = await this.#readTemplateFiles();
    const templates = await Promise.all(
      templateFiles.map((fileName) => this.loadFromFile(path.join(this.templatesRoot, fileName))),
    );

    return templates.sort((left, right) => left.id.localeCompare(right.id));
  }

  async getTemplateById(templateId) {
    if (!templateId || typeof templateId !== "string") {
      throw new TypeError("templateId must be a non-empty string.");
    }

    return this.loadFromFile(path.join(this.templatesRoot, `${templateId}${this.templateExtension}`));
  }

  async #readTemplateFiles() {
    let entries;

    try {
      entries = await fs.readdir(this.templatesRoot, { withFileTypes: true });
    } catch (error) {
      if (error && error.code === "ENOENT") {
        throw new TemplateDirectoryNotFoundError(
          `Template root directory not found: ${this.templatesRoot}`,
          { templatesRoot: this.templatesRoot },
        );
      }

      throw error;
    }

    return entries
      .filter(
        (entry) =>
          entry.isFile() &&
          path.extname(entry.name).toLowerCase() === this.templateExtension.toLowerCase(),
      )
      .map((entry) => entry.name);
  }

  async #readTemplateFile(templateFilePath) {
    let sourceText;

    try {
      sourceText = await fs.readFile(templateFilePath, "utf8");
    } catch (error) {
      if (error && error.code === "ENOENT") {
        throw new TemplateFileNotFoundError(
          `Template file not found: ${templateFilePath}`,
          { templateFilePath },
        );
      }

      throw error;
    }

    try {
      return JSON.parse(sourceText);
    } catch (error) {
      throw new TemplateParseError(
        `Invalid JSON in template file: ${templateFilePath}`,
        {
          templateFilePath,
          cause: error.message,
        },
      );
    }
  }

  #validateTemplateShape(template, templateFilePath) {
    if (!template || typeof template !== "object" || Array.isArray(template)) {
      throw new TemplateParseError(
        `Template JSON must be an object: ${templateFilePath}`,
        { templateFilePath },
      );
    }

    if (typeof template.id !== "string" || template.id.trim() === "") {
      throw new TemplateParseError(
        `Template is missing a valid "id": ${templateFilePath}`,
        { templateFilePath },
      );
    }

    return template;
  }
}

module.exports = {
  TemplateLoader,
};
