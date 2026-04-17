class TemplateLoaderError extends Error {
  constructor(message, details = {}) {
    super(message);
    this.name = new.target.name;
    this.details = details;
  }
}

class TemplateDirectoryNotFoundError extends TemplateLoaderError {}

class TemplateFileNotFoundError extends TemplateLoaderError {}

class TemplateParseError extends TemplateLoaderError {}

class TemplateValidationError extends TemplateLoaderError {}

module.exports = {
  TemplateLoaderError,
  TemplateDirectoryNotFoundError,
  TemplateFileNotFoundError,
  TemplateParseError,
  TemplateValidationError,
};
