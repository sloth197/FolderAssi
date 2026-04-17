class ZipServiceError extends Error {
  constructor(message, details = {}) {
    super(message);
    this.name = new.target.name;
    this.details = details;
  }
}

class ZipArchiveCreationError extends ZipServiceError {}

class ZipArchiveExtractionError extends ZipServiceError {}

module.exports = {
  ZipServiceError,
  ZipArchiveCreationError,
  ZipArchiveExtractionError,
};
