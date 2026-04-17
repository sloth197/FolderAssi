const path = require("node:path");

const {
  InvalidConditionKeyError,
  MissingTemplateValueError,
  TemplateRenderError,
  UnsafeTemplatePathError,
} = require("./errors");
const { interpolateValue, resolveContextValue } = require("./interpolator");

function renderTemplate({ template, variables = {}, options = {} } = {}) {
  if (!template || typeof template !== "object") {
    throw new TemplateRenderError("template is required.");
  }

  const resolvedVariables = resolveVariables(template, variables);
  const resolvedOptions = resolveOptions(template, options);
  const context = {
    variables: resolvedVariables,
    options: resolvedOptions,
  };

  const root = renderNode(template.root, context, "");

  if (!root) {
    throw new TemplateRenderError("Template root was excluded during rendering.");
  }

  const manifest = buildManifest(root);

  return {
    templateId: template.id,
    templateVersion: template.templateVersion,
    variables: resolvedVariables,
    options: resolvedOptions,
    root,
    manifest,
  };
}

function renderNode(node, context, parentPath) {
  if (!node || typeof node !== "object") {
    throw new TemplateRenderError("Template node must be an object.");
  }

  if (!shouldIncludeNode(node, context)) {
    return null;
  }

  const name = interpolateValue(node.name, context, {
    nodeType: node.type,
    field: "name",
  });

  assertSafeSegment(name);

  const nodePath = parentPath ? path.posix.join(parentPath, name) : name;

  if (node.type === "file") {
    const content = interpolateValue(node.contentTemplate, context, {
      nodePath,
      field: "contentTemplate",
    });

    return {
      name,
      path: nodePath,
      type: "file",
      content,
      encoding: node.encoding || "utf8",
      overwritePolicy: node.overwritePolicy || "error",
    };
  }

  if (node.type !== "folder") {
    throw new TemplateRenderError(`Unsupported node type "${node.type}".`, {
      nodePath,
    });
  }

  const children = Array.isArray(node.children)
    ? node.children
        .map((childNode) => renderNode(childNode, context, nodePath))
        .filter(Boolean)
    : [];

  return {
    name,
    path: nodePath,
    type: "folder",
    overwritePolicy: node.overwritePolicy || "error",
    children,
  };
}

function resolveVariables(template, inputVariables) {
  const resolvedVariables = {
    ...(template.defaultVariables || {}),
    ...(inputVariables || {}),
  };

  const requiredVariables = Array.isArray(template.requiredVariables)
    ? template.requiredVariables
    : [];

  for (const key of requiredVariables) {
    if (!hasUsableValue(resolvedVariables[key])) {
      throw new MissingTemplateValueError(`Missing required variable "${key}".`, {
        key,
      });
    }
  }

  return resolvedVariables;
}

function resolveOptions(template, inputOptions) {
  const resolvedOptions = {};
  const optionDefinitions = Array.isArray(template.options) ? template.options : [];
  const allowedKeys = new Set();

  for (const option of optionDefinitions) {
    allowedKeys.add(option.key);
    if (Object.prototype.hasOwnProperty.call(inputOptions || {}, option.key)) {
      resolvedOptions[option.key] = inputOptions[option.key];
    } else {
      resolvedOptions[option.key] = option.default;
    }
  }

  for (const key of Object.keys(inputOptions || {})) {
    if (!allowedKeys.has(key)) {
      throw new TemplateRenderError(`Unknown option key "${key}".`, { key });
    }
  }

  return resolvedOptions;
}

function shouldIncludeNode(node, context) {
  if (!node.conditionKey) {
    return true;
  }

  const conditionValue = resolveContextValue(node.conditionKey, context);

  if (conditionValue === undefined) {
    throw new InvalidConditionKeyError(
      `Condition key "${node.conditionKey}" could not be resolved.`,
      { conditionKey: node.conditionKey },
    );
  }

  return Boolean(conditionValue);
}

function buildManifest(root) {
  const directories = [];
  const files = [];

  visitNode(root, (node) => {
    if (node.type === "folder") {
      directories.push(node.path);
      return;
    }

    files.push(node.path);
  });

  return {
    directories,
    files,
  };
}

function visitNode(node, visitor) {
  visitor(node);

  if (node.type === "folder") {
    for (const child of node.children || []) {
      visitNode(child, visitor);
    }
  }
}

function assertSafeSegment(name) {
  if (typeof name !== "string" || name.trim() === "") {
    throw new UnsafeTemplatePathError("Rendered node name must be a non-empty string.");
  }

  if (name === "." || name === ".." || name.includes("/") || name.includes("\\")) {
    throw new UnsafeTemplatePathError(`Unsafe rendered node name "${name}".`, { name });
  }
}

function hasUsableValue(value) {
  if (value === null || value === undefined) {
    return false;
  }

  if (typeof value === "string") {
    return value.trim() !== "";
  }

  return true;
}

module.exports = {
  renderTemplate,
  renderNode,
  buildManifest,
};
