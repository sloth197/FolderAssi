class AiDecisionError extends Error {
  constructor(message, details = {}) {
    super(message);
    this.name = new.target.name;
    this.details = details;
  }
}

class AiOutputValidationError extends AiDecisionError {}

module.exports = {
  AiDecisionError,
  AiOutputValidationError,
};
