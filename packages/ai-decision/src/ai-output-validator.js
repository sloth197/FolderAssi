const { AiOutputValidationError } = require("./errors");

function validateAiOutput(output, templates) {
  const errors = [];

  if (!isPlainObject(output)) {
    errors.push({
      path: "output",
      code: "invalid_output",
      message: "AI output must be a JSON object.",
    });

    return { valid: false, errors };
  }

  if (!hasNonEmptyString(output.templateId)) {
    errors.push({
      path: "templateId",
      code: "missing_template_id",
      message: 'Field "templateId" must be a non-empty string.',
    });
  }

  const selectedTemplate = Array.isArray(templates)
    ? templates.find((template) => template.id === output.templateId)
    : undefined;

  if (hasNonEmptyString(output.templateId) && !selectedTemplate) {
    errors.push({
      path: "templateId",
      code: "unknown_template_id",
      message: `Unknown templateId "${output.templateId}".`,
    });
  }

  if (!isPlainObject(output.variables)) {
    errors.push({
      path: "variables",
      code: "invalid_variables",
      message: 'Field "variables" must be an object.',
    });
  }

  if (!isPlainObject(output.options)) {
    errors.push({
      path: "options",
      code: "invalid_options",
      message: 'Field "options" must be an object.',
    });
  }

  if (typeof output.confidence !== "number" || Number.isNaN(output.confidence)) {
    errors.push({
      path: "confidence",
      code: "invalid_confidence",
      message: 'Field "confidence" must be a number.',
    });
  } else if (output.confidence < 0 || output.confidence > 1) {
    errors.push({
      path: "confidence",
      code: "confidence_out_of_range",
      message: 'Field "confidence" must be between 0 and 1.',
    });
  }

  if (!Array.isArray(output.notes) || output.notes.some((note) => typeof note !== "string")) {
    errors.push({
      path: "notes",
      code: "invalid_notes",
      message: 'Field "notes" must be an array of strings.',
    });
  }

  if (selectedTemplate && isPlainObject(output.variables)) {
    validateVariableKeys(output.variables, selectedTemplate, errors);
    validateRequiredVariables(output.variables, selectedTemplate, errors);
    validateVariableTypes(output.variables, selectedTemplate, errors);
  }

  if (selectedTemplate && isPlainObject(output.options)) {
    validateOptionKeys(output.options, selectedTemplate, errors);
    validateOptionTypes(output.options, selectedTemplate, errors);
  }

  return {
    valid: errors.length === 0,
    errors,
    template: selectedTemplate || null,
  };
}

function assertValidAiOutput(output, templates) {
  const result = validateAiOutput(output, templates);

  if (!result.valid) {
    throw new AiOutputValidationError("AI output validation failed.", {
      errors: result.errors,
    });
  }

  return result;
}

function validateVariableKeys(variables, template, errors) {
  const allowedKeys = new Set([
    ...Object.keys(template.defaultVariables || {}),
    ...(template.requiredVariables || []),
  ]);

  for (const key of Object.keys(variables)) {
    if (!allowedKeys.has(key)) {
      errors.push({
        path: `variables.${key}`,
        code: "unknown_variable_key",
        message: `Unknown variable key "${key}" for template "${template.id}".`,
      });
    }
  }
}

function validateRequiredVariables(variables, template, errors) {
  for (const key of template.requiredVariables || []) {
    const hasValue = key in variables || key in (template.defaultVariables || {});

    if (!hasValue) {
      errors.push({
        path: `variables.${key}`,
        code: "missing_required_variable",
        message: `Missing required variable "${key}".`,
      });
    }
  }
}

function validateVariableTypes(variables, template, errors) {
  for (const [key, value] of Object.entries(variables)) {
    if (!(key in (template.defaultVariables || {}))) {
      continue;
    }

    const defaultValue = template.defaultVariables[key];
    if (defaultValue === null || defaultValue === undefined) {
      if (!isScalar(value)) {
        errors.push({
          path: `variables.${key}`,
          code: "invalid_variable_type",
          message: `Variable "${key}" must be a scalar value.`,
        });
      }
      continue;
    }

    if (typeof value !== typeof defaultValue) {
      errors.push({
        path: `variables.${key}`,
        code: "invalid_variable_type",
        message: `Variable "${key}" must be of type "${typeof defaultValue}".`,
      });
    }
  }
}

function validateOptionKeys(options, template, errors) {
  const allowedKeys = new Set((template.options || []).map((option) => option.key));

  for (const key of Object.keys(options)) {
    if (!allowedKeys.has(key)) {
      errors.push({
        path: `options.${key}`,
        code: "unknown_option_key",
        message: `Unknown option key "${key}" for template "${template.id}".`,
      });
    }
  }
}

function validateOptionTypes(options, template, errors) {
  const optionDefinitions = new Map(
    (template.options || []).map((option) => [option.key, option]),
  );

  for (const [key, value] of Object.entries(options)) {
    const option = optionDefinitions.get(key);
    if (!option) {
      continue;
    }

    if (!matchesOptionType(value, option.type)) {
      errors.push({
        path: `options.${key}`,
        code: "invalid_option_type",
        message: `Option "${key}" must be of type "${option.type}".`,
      });
    }
  }
}

function matchesOptionType(value, type) {
  if (type === "string" || type === "enum") {
    return typeof value === "string";
  }

  if (type === "boolean") {
    return typeof value === "boolean";
  }

  if (type === "number") {
    return typeof value === "number" && !Number.isNaN(value);
  }

  return false;
}

function isPlainObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

function hasNonEmptyString(value) {
  return typeof value === "string" && value.trim() !== "";
}

function isScalar(value) {
  return value === null || ["string", "boolean", "number"].includes(typeof value);
}

module.exports = {
  validateAiOutput,
  assertValidAiOutput,
};
