class TemplateEngineError extends Error {
  constructor(message, details = {}) {
    super(message);
    this.name = new.target.name;
    this.details = details;
  }
}

class TemplateRenderError extends TemplateEngineError {}

class MissingTemplateValueError extends TemplateRenderError {}

class InvalidConditionKeyError extends TemplateRenderError {}

class UnsafeTemplatePathError extends TemplateRenderError {}

class TemplateGenerationError extends TemplateEngineError {}

class OverwritePolicyError extends TemplateGenerationError {}

module.exports = {
  TemplateEngineError,
  TemplateRenderError,
  MissingTemplateValueError,
  InvalidConditionKeyError,
  UnsafeTemplatePathError,
  TemplateGenerationError,
  OverwritePolicyError,
};
