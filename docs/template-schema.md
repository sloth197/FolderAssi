# FolderAssi Template Schema

## 1. Storage rule

FolderAssi stores templates as flat JSON files.

```text
templates/
  spring-boot-layered-api-starter.json
  aspnetcore-webapi-starter.json
  react-feature-based-starter.json
```

One file equals one template.

## 2. Document shape

Each template file follows the `ProjectTemplate` contract.

```json
{
  "id": "react-feature-based-starter",
  "name": "React Feature-Based Starter",
  "description": "React application scaffold organized by feature.",
  "language": "TypeScript",
  "framework": "React",
  "templateVersion": "1.0.0",
  "tags": ["react", "frontend"],
  "defaultVariables": {
    "projectName": "feature-react-app",
    "appTitle": "Feature React App"
  },
  "requiredVariables": ["projectName", "appTitle"],
  "options": [
    {
      "key": "includeRouter",
      "label": "Include Router",
      "type": "boolean",
      "default": true
    }
  ],
  "root": {
    "name": "{{projectName}}",
    "type": "folder",
    "overwritePolicy": "error",
    "children": [
      {
        "name": "README.md",
        "type": "file",
        "contentTemplate": "# {{appTitle}}",
        "encoding": "utf8",
        "overwritePolicy": "overwrite"
      }
    ]
  }
}
```

## 3. Core models

### `ProjectTemplate`

- metadata for AI selection
- variable defaults and required fields
- supported options
- root template tree

### `TemplateNode`

- `folder` or `file`
- recursive children for nested structure
- `contentTemplate` for file content rendering
- optional `conditionKey` for conditional inclusion

### `TemplateOption`

- constrained user-selectable or AI-selectable option
- typed with `string`, `boolean`, `number`, or `enum`

## 4. Validation rules

- Required top-level fields must exist
- Every node must have a non-empty `name`
- `type` must be `folder` or `file`
- `file` nodes must define `contentTemplate`
- Sibling nodes must not reuse the same `name`

## 5. Engine contract

The engine only accepts:

```json
{
  "templateId": "react-feature-based-starter",
  "variables": {},
  "options": {}
}
```

The engine never accepts AI-generated folder trees or file contents.
