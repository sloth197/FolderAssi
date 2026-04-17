/**
 * @typedef {Object} AiAnalysisInput
 * @property {string} request Natural-language project requirement from the user.
 * @property {Array<{
 *   id: string,
 *   name: string,
 *   description: string,
 *   language: string,
 *   framework: string,
 *   tags: string[],
 *   requiredVariables: string[],
 *   optionKeys: string[]
 * }>} templates Template summaries that the AI may choose from.
 */

/**
 * @typedef {Object} AiAnalysisOutput
 * @property {string} templateId Selected template identifier.
 * @property {Record<string, string|boolean|number|null>} variables Structured variables extracted from the request.
 * @property {Record<string, string|boolean|number|null>} options Structured options inferred from the request.
 * @property {number} confidence Confidence score between 0 and 1.
 * @property {string[]} notes Short notes about assumptions or missing context.
 */

const aiInputModel = Object.freeze({
  name: "AiAnalysisInput",
  fields: Object.freeze({
    request: Object.freeze({
      type: "string",
      required: true,
      description: "Natural-language project requirement provided by the user.",
    }),
    templates: Object.freeze({
      type: "TemplateSummary[]",
      required: true,
      description: "List of allowed templates the AI can choose from.",
    }),
  }),
});

const aiOutputModel = Object.freeze({
  name: "AiAnalysisOutput",
  fields: Object.freeze({
    templateId: Object.freeze({
      type: "string",
      required: true,
      description: "Chosen template id. Must match one of the allowed template ids.",
    }),
    variables: Object.freeze({
      type: "Record<string, string|boolean|number|null>",
      required: true,
      description: "Structured variable values extracted from the user request.",
    }),
    options: Object.freeze({
      type: "Record<string, string|boolean|number|null>",
      required: true,
      description: "Structured option values limited to the selected template options.",
    }),
    confidence: Object.freeze({
      type: "number",
      required: true,
      description: "Confidence score from 0.0 to 1.0.",
    }),
    notes: Object.freeze({
      type: "string[]",
      required: true,
      description: "Short notes describing assumptions or ambiguities.",
    }),
  }),
});

function createTemplateSummary(template) {
  return {
    id: template.id,
    name: template.name,
    description: template.description,
    language: template.language,
    framework: template.framework,
    tags: Array.isArray(template.tags) ? [...template.tags] : [],
    requiredVariables: Array.isArray(template.requiredVariables)
      ? [...template.requiredVariables]
      : [],
    optionKeys: Array.isArray(template.options)
      ? template.options.map((option) => option.key)
      : [],
  };
}

module.exports = {
  aiInputModel,
  aiOutputModel,
  createTemplateSummary,
};
