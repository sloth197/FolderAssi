const { MissingTemplateValueError } = require("./errors");

const PLACEHOLDER_PATTERN = /\{\{\s*([^{}]+?)\s*\}\}/g;

function interpolateValue(value, context, details = {}) {
  if (typeof value !== "string") {
    return value;
  }

  return value.replace(PLACEHOLDER_PATTERN, (_, token) => {
    const resolved = resolveContextValue(token, context);

    if (resolved === undefined) {
      throw new MissingTemplateValueError(`Missing value for placeholder "${token}".`, {
        placeholder: token,
        ...details,
      });
    }

    if (resolved === null) {
      return "";
    }

    return String(resolved);
  });
}

function resolveContextValue(token, context) {
  const normalizedToken = token.trim();

  if (normalizedToken.startsWith("variables.")) {
    return getPathValue(context.variables, normalizedToken.slice("variables.".length));
  }

  if (normalizedToken.startsWith("options.")) {
    return getPathValue(context.options, normalizedToken.slice("options.".length));
  }

  const variableValue = getPathValue(context.variables, normalizedToken);
  if (variableValue !== undefined) {
    return variableValue;
  }

  return getPathValue(context.options, normalizedToken);
}

function getPathValue(target, keyPath) {
  if (!target || typeof target !== "object") {
    return undefined;
  }

  return keyPath.split(".").reduce((current, segment) => {
    if (current === null || current === undefined) {
      return undefined;
    }

    return current[segment];
  }, target);
}

module.exports = {
  interpolateValue,
  resolveContextValue,
};
