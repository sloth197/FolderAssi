# FolderAssi Data Models

This document defines the core template-driven data models used across the AI, Template, Engine, Archive, and UI layers.

## 1. ProjectTemplate

Top-level object representing a single approved project scaffold.

| Field | Type | Description |
| --- | --- | --- |
| `id` | `string` | Stable template identifier returned by the AI layer. |
| `name` | `string` | Human-readable template name. |
| `description` | `string` | Short explanation of the scaffold purpose. |
| `language` | `string` | Primary language used by the scaffold, such as TypeScript or Python. |
| `framework` | `string` | Primary framework represented by the scaffold, such as Next.js or Express. |
| `templateVersion` | `string` | Immutable version used for reproducible builds and audit logs. |
| `tags` | `string[]` | Search, recommendation, and categorization tags. |
| `defaultVariables` | `Record<string, string\|boolean\|number\|null>` | Default variable values applied before generation. |
| `requiredVariables` | `string[]` | Variable keys that must be resolved before generation starts. |
| `options` | `TemplateOption[]` | Supported configuration switches for this template. |
| `root` | `TemplateNode` | Root node containing the full folder and file tree. |

## 2. TemplateNode

Declarative node that represents a folder or file inside the project tree.

| Field | Type | Description |
| --- | --- | --- |
| `name` | `string` | Name of the folder or file at the current tree level. |
| `type` | `"folder" \| "file"` | Determines whether the engine creates a folder or a file. |
| `children` | `TemplateNode[]` | Nested child nodes used when the current node is a folder. |
| `contentTemplate` | `string \| null` | Template text or content reference used for file rendering. |
| `optional` | `boolean` | Marks the node as skippable. |
| `conditionKey` | `string \| null` | Variable or option key checked before rendering the node. |
| `encoding` | `"utf8" \| "base64" \| "binary" \| null` | Encoding strategy for file output. |
| `overwritePolicy` | `"skip" \| "overwrite" \| "error" \| "merge"` | Behavior when the destination path already exists. |

## 3. TemplateOption

Structured option that can be selected by AI or adjusted by the user.

| Field | Type | Description |
| --- | --- | --- |
| `key` | `string` | Stable option key used in the generation request. |
| `label` | `string` | User-facing label shown in the UI. |
| `type` | `"string" \| "boolean" \| "number" \| "enum"` | Primitive type used for validation and form rendering. |
| `default` | `string \| boolean \| number \| null` | Default value applied when the option is omitted. |

## 4. Notes

- `ProjectTemplate` is the top-level registry object.
- `TemplateNode` is recursive and models the actual file tree.
- `TemplateOption` drives both AI output constraints and UI configuration forms.
- These models are also exported from `packages/shared-types/src/template-models.js`.
