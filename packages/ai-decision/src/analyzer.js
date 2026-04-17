function analyzeProjectRequest({ request, templates } = {}) {
  if (typeof request !== "string" || request.trim() === "") {
    throw new TypeError("request must be a non-empty string.");
  }

  if (!Array.isArray(templates) || templates.length === 0) {
    throw new TypeError("templates must be a non-empty array.");
  }

  const normalizedRequest = request.toLowerCase();
  const scoredTemplates = templates
    .map((template) => ({
      template,
      score: scoreTemplate(normalizedRequest, template),
    }))
    .sort((left, right) => right.score - left.score);

  const selected = scoredTemplates[0].template;
  const selectedScore = scoredTemplates[0].score;
  const confidence = Math.max(0.35, Math.min(0.98, selectedScore / 10));
  const variables = inferVariables(selected, request);
  const options = inferOptions(selected, normalizedRequest);
  const notes = buildNotes(selected, confidence, variables);

  return {
    templateId: selected.id,
    variables,
    options,
    confidence: Number(confidence.toFixed(2)),
    notes,
  };
}

function scoreTemplate(request, template) {
  let score = 1;
  const haystacks = [
    template.id,
    template.name,
    template.description,
    template.language,
    template.framework,
    ...(template.tags || []),
  ]
    .filter(Boolean)
    .map((value) => String(value).toLowerCase());

  for (const haystack of haystacks) {
    if (request.includes(haystack)) {
      score += 3;
    }
  }

  if (template.id === "spring-boot-layered-api-starter") {
    if (request.includes("spring")) score += 4;
    if (request.includes("java")) score += 3;
    if (request.includes("layered")) score += 2;
    if (request.includes("api")) score += 1;
  }

  if (template.id === "aspnetcore-webapi-starter") {
    if (request.includes("asp.net")) score += 4;
    if (request.includes("aspnet")) score += 4;
    if (request.includes(".net")) score += 3;
    if (request.includes("web api")) score += 2;
    if (request.includes("c#")) score += 3;
  }

  if (template.id === "react-feature-based-starter") {
    if (request.includes("react")) score += 4;
    if (request.includes("frontend")) score += 2;
    if (request.includes("feature")) score += 2;
    if (request.includes("dashboard")) score += 1;
  }

  return score;
}

function inferVariables(template, request) {
  const baseVariables = {
    ...(template.defaultVariables || {}),
  };
  const extractedName = extractProjectName(request);

  if (template.id === "react-feature-based-starter") {
    const projectName = extractedName ? toKebabCase(extractedName) : baseVariables.projectName;
    return {
      ...baseVariables,
      projectName,
      appTitle: extractedName ? toTitleCase(extractedName) : baseVariables.appTitle,
    };
  }

  if (template.id === "spring-boot-layered-api-starter") {
    const projectName = extractedName ? toKebabCase(extractedName) : baseVariables.projectName;
    return {
      ...baseVariables,
      projectName,
      artifactId: projectName,
      packageName: toCamelCase(projectName),
    };
  }

  if (template.id === "aspnetcore-webapi-starter") {
    const projectName = extractedName ? toPascalCase(extractedName) : baseVariables.projectName;
    return {
      ...baseVariables,
      projectName,
      solutionName: projectName,
      rootNamespace: projectName,
    };
  }

  return baseVariables;
}

function inferOptions(template, normalizedRequest) {
  const defaultOptions = {};
  for (const option of template.options || []) {
    defaultOptions[option.key] = option.default;
  }

  if (template.id === "react-feature-based-starter") {
    defaultOptions.includeRouter = containsAny(normalizedRequest, ["router", "routing"]);
    defaultOptions.includeTesting = containsAny(normalizedRequest, ["test", "testing", "jest"]);
    defaultOptions.styling = normalizedRequest.includes("tailwind")
      ? "tailwind"
      : defaultOptions.styling;
  }

  if (template.id === "spring-boot-layered-api-starter") {
    defaultOptions.includeDocker = normalizedRequest.includes("docker");
    defaultOptions.includeSwagger = containsAny(normalizedRequest, [
      "swagger",
      "openapi",
      "api docs",
    ]);
  }

  if (template.id === "aspnetcore-webapi-starter") {
    defaultOptions.includeDocker = normalizedRequest.includes("docker");
    defaultOptions.includeSwagger = containsAny(normalizedRequest, [
      "swagger",
      "openapi",
      "api docs",
    ]);
    defaultOptions.useControllers = containsAny(normalizedRequest, [
      "controller",
      "controllers",
      "web api",
    ]);
  }

  return defaultOptions;
}

function buildNotes(template, confidence, variables) {
  const notes = [`Selected ${template.name} as the closest approved template.`];

  if (confidence < 0.6) {
    notes.push("Confidence is moderate; review the suggested template before generation.");
  }

  if (variables.projectName) {
    notes.push(`Using "${variables.projectName}" as the project name.`);
  }

  return notes;
}

function extractProjectName(request) {
  const quotedMatch = request.match(/["']([^"']+)["']/);
  if (quotedMatch) {
    return quotedMatch[1];
  }

  const namedMatch = request.match(
    /\b(?:named|called|for)\s+([a-z0-9][a-z0-9\s-_]+)$/i,
  );
  if (namedMatch) {
    return namedMatch[1].trim();
  }

  return null;
}

function containsAny(value, searchTerms) {
  return searchTerms.some((term) => value.includes(term));
}

function toKebabCase(value) {
  return value
    .trim()
    .replace(/([a-z0-9])([A-Z])/g, "$1-$2")
    .replace(/[^a-zA-Z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .toLowerCase();
}

function toCamelCase(value) {
  const kebab = toKebabCase(value);
  return kebab.replace(/-([a-z0-9])/g, (_, letter) => letter.toUpperCase());
}

function toPascalCase(value) {
  return toKebabCase(value)
    .split("-")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join("");
}

function toTitleCase(value) {
  return toKebabCase(value)
    .split("-")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

module.exports = {
  analyzeProjectRequest,
};
