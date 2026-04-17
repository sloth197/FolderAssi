const TemplateNodeType = Object.freeze({
  FOLDER: "folder",
  FILE: "file",
});

const TemplateOptionType = Object.freeze({
  STRING: "string",
  BOOLEAN: "boolean",
  NUMBER: "number",
  ENUM: "enum",
});

const TemplateEncoding = Object.freeze({
  UTF8: "utf8",
  BASE64: "base64",
  BINARY: "binary",
});

const OverwritePolicy = Object.freeze({
  SKIP: "skip",
  OVERWRITE: "overwrite",
  ERROR: "error",
  MERGE: "merge",
});

/**
 * @typedef {Object} TemplateOption
 * @property {string} key Stable option key used by AI output and engine input.
 * @property {string} label User-facing label shown in forms and previews.
 * @property {"string"|"boolean"|"number"|"enum"} type Primitive option type.
 * @property {string|boolean|number|null} default Default option value when omitted.
 */

/**
 * @typedef {Object} TemplateNode
 * @property {string} name Folder or file name at the current tree level.
 * @property {"folder"|"file"} type Node kind that decides render behavior.
 * @property {TemplateNode[]=} children Child nodes for folder nodes.
 * @property {string|null=} contentTemplate Inline or referenced file template content.
 * @property {boolean=} optional Marks a node as conditional or skippable.
 * @property {string|null=} conditionKey Option or variable key that controls inclusion.
 * @property {"utf8"|"base64"|"binary"|null=} encoding Output encoding for file nodes.
 * @property {"skip"|"overwrite"|"error"|"merge"} overwritePolicy Collision strategy during generation.
 */

/**
 * @typedef {Object} ProjectTemplate
 * @property {string} id Stable template identifier returned by the AI layer.
 * @property {string} name Human-readable template name.
 * @property {string} description Short explanation of the scaffold purpose.
 * @property {string} language Primary implementation language such as TypeScript or Python.
 * @property {string} framework Primary framework such as Next.js or Express.
 * @property {string} templateVersion Immutable template version for reproducibility.
 * @property {string[]} tags Search and classification tags.
 * @property {Record<string, string|boolean|number|null>} defaultVariables Default variable map used when the user omits values.
 * @property {string[]} requiredVariables Variable keys that must be resolved before generation.
 * @property {TemplateOption[]} options Supported template options.
 * @property {TemplateNode} root Root node of the template tree.
 */

const templateOptionModel = Object.freeze({
  name: "TemplateOption",
  description: "Configurable option exposed to AI selection and user confirmation flows.",
  fields: Object.freeze({
    key: Object.freeze({
      type: "string",
      required: true,
      description: "Stable option key used in the options payload.",
    }),
    label: Object.freeze({
      type: "string",
      required: true,
      description: "Human-readable label shown in the UI.",
    }),
    type: Object.freeze({
      type: "enum",
      required: true,
      values: Object.values(TemplateOptionType),
      description: "Primitive option type used for validation and form rendering.",
    }),
    default: Object.freeze({
      type: "string|boolean|number|null",
      required: true,
      description: "Default value applied when the option is not explicitly set.",
    }),
  }),
});

const templateNodeModel = Object.freeze({
  name: "TemplateNode",
  description: "Single folder or file node within the declarative project tree.",
  fields: Object.freeze({
    name: Object.freeze({
      type: "string",
      required: true,
      description: "Folder or file name at the current level.",
    }),
    type: Object.freeze({
      type: "enum",
      required: true,
      values: Object.values(TemplateNodeType),
      description: "Determines whether the engine creates a folder or a file.",
    }),
    children: Object.freeze({
      type: "TemplateNode[]",
      required: false,
      description: "Nested child nodes used when the current node is a folder.",
    }),
    contentTemplate: Object.freeze({
      type: "string|null",
      required: false,
      description: "Template text or asset reference used to build file contents.",
    }),
    optional: Object.freeze({
      type: "boolean",
      required: false,
      description: "Marks the node as skippable when its condition is not satisfied.",
    }),
    conditionKey: Object.freeze({
      type: "string|null",
      required: false,
      description: "Variable or option key checked before rendering the node.",
    }),
    encoding: Object.freeze({
      type: "enum|null",
      required: false,
      values: Object.values(TemplateEncoding),
      description: "Encoding strategy used when writing file content.",
    }),
    overwritePolicy: Object.freeze({
      type: "enum",
      required: true,
      values: Object.values(OverwritePolicy),
      description: "Behavior when the target path already exists.",
    }),
  }),
});

const projectTemplateModel = Object.freeze({
  name: "ProjectTemplate",
  description: "Top-level template contract consumed by the AI, engine, and UI layers.",
  fields: Object.freeze({
    id: Object.freeze({
      type: "string",
      required: true,
      description: "Stable template identifier used as templateId.",
    }),
    name: Object.freeze({
      type: "string",
      required: true,
      description: "Display name of the template.",
    }),
    description: Object.freeze({
      type: "string",
      required: true,
      description: "Short explanation of the generated project structure.",
    }),
    language: Object.freeze({
      type: "string",
      required: true,
      description: "Primary programming language of the scaffold.",
    }),
    framework: Object.freeze({
      type: "string",
      required: true,
      description: "Primary framework or platform represented by the scaffold.",
    }),
    templateVersion: Object.freeze({
      type: "string",
      required: true,
      description: "Immutable version used for reproducibility and audit logs.",
    }),
    tags: Object.freeze({
      type: "string[]",
      required: true,
      description: "Search, classification, and recommendation tags.",
    }),
    defaultVariables: Object.freeze({
      type: "Record<string, string|boolean|number|null>",
      required: true,
      description: "Default variable values applied before rendering.",
    }),
    requiredVariables: Object.freeze({
      type: "string[]",
      required: true,
      description: "Variable keys that must be resolved before generation.",
    }),
    options: Object.freeze({
      type: "TemplateOption[]",
      required: true,
      description: "Supported configuration switches for the template.",
    }),
    root: Object.freeze({
      type: "TemplateNode",
      required: true,
      description: "Root node containing the full folder and file tree.",
    }),
  }),
});

const templateModels = Object.freeze({
  ProjectTemplate: projectTemplateModel,
  TemplateNode: templateNodeModel,
  TemplateOption: templateOptionModel,
});

module.exports = {
  TemplateNodeType,
  TemplateOptionType,
  TemplateEncoding,
  OverwritePolicy,
  templateModels,
  projectTemplateModel,
  templateNodeModel,
  templateOptionModel,
};
