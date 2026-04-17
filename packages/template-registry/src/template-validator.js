const { TemplateValidationError } = require("./errors");

const REQUIRED_TEMPLATE_FIELDS = [
  "id",
  "name",
  "description",
  "language",
  "framework",
  "templateVersion",
  "tags",
  "defaultVariables",
  "requiredVariables",
  "options",
  "root",
];

const VALID_NODE_TYPES = new Set(["folder", "file"]);

function validateTemplate(template) {
  const errors = [];

  if (!isPlainObject(template)) {
    errors.push({
      path: "template",
      code: "invalid_template",
      message: "Template must be a JSON object.",
    });

    return { valid: false, errors };
  }

  for (const fieldName of REQUIRED_TEMPLATE_FIELDS) {
    if (!(fieldName in template)) {
      errors.push({
        path: fieldName,
        code: "missing_required_field",
        message: `Missing required field "${fieldName}".`,
      });
    }
  }

  if (!hasNonEmptyString(template.name)) {
    errors.push({
      path: "name",
      code: "missing_name",
      message: 'Template "name" must be a non-empty string.',
    });
  }

  if (template.root !== undefined) {
    validateNode(template.root, "root", errors);
  }

  return {
    valid: errors.length === 0,
    errors,
  };
}

function assertValidTemplate(template) {
  const result = validateTemplate(template);

  if (!result.valid) {
    throw new TemplateValidationError("Template validation failed.", {
      errors: result.errors,
    });
  }

  return template;
}

function validateNode(node, path, errors) {
  if (!isPlainObject(node)) {
    errors.push({
      path,
      code: "invalid_node",
      message: "Template node must be an object.",
    });
    return;
  }

  if (!hasNonEmptyString(node.name)) {
    errors.push({
      path: `${path}.name`,
      code: "missing_name",
      message: 'Node "name" must be a non-empty string.',
    });
  }

  if (!VALID_NODE_TYPES.has(node.type)) {
    errors.push({
      path: `${path}.type`,
      code: "invalid_type",
      message: 'Node "type" must be either "folder" or "file".',
    });
  }

  if (node.type === "file" && !hasNonEmptyString(node.contentTemplate)) {
    errors.push({
      path: `${path}.contentTemplate`,
      code: "missing_content_template",
      message: 'File nodes must include a non-empty "contentTemplate".',
    });
  }

  if (node.children !== undefined) {
    if (!Array.isArray(node.children)) {
      errors.push({
        path: `${path}.children`,
        code: "invalid_children",
        message: 'Node "children" must be an array when provided.',
      });
      return;
    }

    validateDuplicateNames(node.children, `${path}.children`, errors);

    node.children.forEach((childNode, index) => {
      validateNode(childNode, `${path}.children[${index}]`, errors);
    });
  }
}

function validateDuplicateNames(children, path, errors) {
  const nameCounts = new Map();

  for (const child of children) {
    if (!isPlainObject(child) || !hasNonEmptyString(child.name)) {
      continue;
    }

    nameCounts.set(child.name, (nameCounts.get(child.name) || 0) + 1);
  }

  for (const [name, count] of nameCounts.entries()) {
    if (count > 1) {
      errors.push({
        path,
        code: "duplicate_name",
        message: `Duplicate child name "${name}" found in the same folder.`,
      });
    }
  }
}

function isPlainObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

function hasNonEmptyString(value) {
  return typeof value === "string" && value.trim() !== "";
}

module.exports = {
  validateTemplate,
  assertValidTemplate,
};
