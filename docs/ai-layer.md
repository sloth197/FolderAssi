# FolderAssi AI Layer

## 1. Goal

Convert a user's natural-language requirement into structured JSON.

The AI may only choose:

- `templateId`
- `variables`
- `options`
- `confidence`
- `notes`

The AI may not generate file trees, file contents, or custom templates.

## 2. Input model

```json
{
  "request": "Create a React admin dashboard with routing and test setup.",
  "templates": [
    {
      "id": "react-feature-based-starter",
      "name": "React Feature-Based Starter",
      "description": "React application scaffold organized by feature.",
      "language": "TypeScript",
      "framework": "React",
      "tags": ["react", "frontend"],
      "requiredVariables": ["projectName", "appTitle"],
      "optionKeys": ["includeRouter", "includeTesting", "styling"]
    }
  ]
}
```

## 3. Output model

```json
{
  "templateId": "react-feature-based-starter",
  "variables": {
    "projectName": "admin-console",
    "appTitle": "Admin Console"
  },
  "options": {
    "includeRouter": true,
    "includeTesting": true,
    "styling": "css-modules"
  },
  "confidence": 0.93,
  "notes": [
    "Chose the React starter because the request described a frontend app."
  ]
}
```

## 4. Prompt design

### System prompt

```text
You are FolderAssi's template selection assistant.
Your job is to convert a user's natural-language requirement into structured JSON.
You must never generate folder structures, file trees, file paths, or file contents.
You may only choose a templateId from the allowed list and fill variables and options for that template.
templateId must be exactly one of the allowed template ids.
options must use only keys defined for the selected template.
variables must use only keys relevant to the selected template.
confidence must be a number between 0.0 and 1.0.
notes must be an array of short strings.
Output valid JSON only. Do not wrap it in markdown. Do not add explanations before or after the JSON.
```

### User prompt example

```text
Analyze the following user requirement and choose the best matching template.

Template catalog:
{
  "id": "react-feature-based-starter",
  "name": "React Feature-Based Starter",
  "description": "React application scaffold organized by feature.",
  "language": "TypeScript",
  "framework": "React",
  "tags": ["react", "frontend"],
  "requiredVariables": ["projectName", "appTitle"],
  "optionKeys": ["includeRouter", "includeTesting", "styling"]
}

User requirement:
Create a React admin dashboard with routing and test setup.

Return JSON only.
```
