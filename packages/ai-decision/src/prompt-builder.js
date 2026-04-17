const { createTemplateSummary } = require("./ai-models");

function buildSystemPrompt({ templates }) {
  const summaries = templates.map(createTemplateSummary);
  const templateLines = summaries
    .map(
      (template) =>
        `- ${template.id}: variables=[${template.requiredVariables.join(", ")}], options=[${template.optionKeys.join(", ")}]`,
    )
    .join("\n");

  return [
    "You are FolderAssi's template selection assistant.",
    "Your job is to convert a user's natural-language requirement into structured JSON.",
    "You must never generate folder structures, file trees, file paths, or file contents.",
    "You may only choose a templateId from the allowed list and fill variables and options for that template.",
    "Allowed templateIds:",
    templateLines,
    "Rules:",
    "1. templateId must be exactly one of the allowed template ids.",
    "2. options must use only keys defined for the selected template.",
    "3. variables must use only keys relevant to the selected template.",
    "4. If the request does not specify a value, infer conservatively or use an empty note.",
    "5. confidence must be a number between 0.0 and 1.0.",
    "6. notes must be an array of short strings.",
    "7. Output valid JSON only. Do not wrap it in markdown. Do not add explanations before or after the JSON.",
    "Required JSON shape:",
    "{",
    '  "templateId": "",',
    '  "variables": {},',
    '  "options": {},',
    '  "confidence": 0.0,',
    '  "notes": []',
    "}",
  ].join("\n");
}

function buildUserPrompt({ request, templates }) {
  const summaries = templates.map(createTemplateSummary);
  const catalog = summaries
    .map(
      (template) =>
        JSON.stringify(
          {
            id: template.id,
            name: template.name,
            description: template.description,
            language: template.language,
            framework: template.framework,
            tags: template.tags,
            requiredVariables: template.requiredVariables,
            optionKeys: template.optionKeys,
          },
          null,
          2,
        ),
    )
    .join("\n");

  return [
    "Analyze the following user requirement and choose the best matching template.",
    "Use only the templates listed below.",
    "Template catalog:",
    catalog,
    "User requirement:",
    request,
    "Return JSON only.",
  ].join("\n\n");
}

module.exports = {
  buildSystemPrompt,
  buildUserPrompt,
};
