const state = {
  templates: [],
  templatesById: new Map(),
  selectedTemplateId: "",
  recommendation: null,
  policy: "",
  candidates: [],
  isBusy: false,
  previewBusy: false,
  previewRequestId: 0,
  previewTimer: null,
  preferenceService: new UiPreferenceService(window.localStorage),
  favoriteTemplates: [],
  savedPrompts: [],
  settings: null,
  clearApiKeyRequested: false,
  historyEntries: [],
};

const els = {
  requestInput: document.querySelector("#requestInput"),
  recommendBtn: document.querySelector("#recommendBtn"),
  candidateList: document.querySelector("#candidateList"),
  recommendationBox: document.querySelector("#recommendationBox"),
  policyBadge: document.querySelector("#policyBadge"),
  openAiApiKeyInput: document.querySelector("#openAiApiKeyInput"),
  modelInput: document.querySelector("#modelInput"),
  templatesPathInput: document.querySelector("#templatesPathInput"),
  outputPathInput: document.querySelector("#outputPathInput"),
  zipPathInput: document.querySelector("#zipPathInput"),
  aiModeSelect: document.querySelector("#aiModeSelect"),
  saveSettingsBtn: document.querySelector("#saveSettingsBtn"),
  clearApiKeyBtn: document.querySelector("#clearApiKeyBtn"),
  settingsStatus: document.querySelector("#settingsStatus"),
  openAiEndpointInput: document.querySelector("#openAiEndpointInput"),
  savePromptBtn: document.querySelector("#savePromptBtn"),
  savedPromptList: document.querySelector("#savedPromptList"),
  templateSelect: document.querySelector("#templateSelect"),
  saveTemplateBtn: document.querySelector("#saveTemplateBtn"),
  favoriteTemplateList: document.querySelector("#favoriteTemplateList"),
  variablesForm: document.querySelector("#variablesForm"),
  optionsForm: document.querySelector("#optionsForm"),
  previewBtn: document.querySelector("#previewBtn"),
  generateBtn: document.querySelector("#generateBtn"),
  treePreview: document.querySelector("#treePreview"),
  resultPaths: document.querySelector("#resultPaths"),
  refreshHistoryBtn: document.querySelector("#refreshHistoryBtn"),
  historyList: document.querySelector("#historyList"),
  historyStatus: document.querySelector("#historyStatus"),
};

bootstrap().catch((error) => {
  renderInfo(els.recommendationBox, `초기화 실패: ${error.message}`);
});

els.recommendBtn.addEventListener("click", onRecommend);
els.saveSettingsBtn.addEventListener("click", onSaveSettings);
els.clearApiKeyBtn.addEventListener("click", onClearApiKey);
els.openAiApiKeyInput.addEventListener("input", () => {
  if (els.openAiApiKeyInput.value.trim()) {
    state.clearApiKeyRequested = false;
  }
});
els.savePromptBtn.addEventListener("click", onSavePrompt);
if (els.savedPromptList) {
  els.savedPromptList.addEventListener("click", onSavedPromptClick);
}
els.templateSelect.addEventListener("change", onTemplateChanged);
els.saveTemplateBtn.addEventListener("click", onSaveFavoriteTemplate);
els.favoriteTemplateList.addEventListener("click", onFavoriteTemplateClick);
els.candidateList.addEventListener("click", onCandidateTemplateClick);
els.variablesForm.addEventListener("input", onFormValueChanged);
els.optionsForm.addEventListener("input", onFormValueChanged);
els.optionsForm.addEventListener("change", onFormValueChanged);
els.previewBtn.addEventListener("click", () => void runPreview({ auto: false }));
els.generateBtn.addEventListener("click", onGenerate);
els.refreshHistoryBtn.addEventListener("click", () => void loadHistory());
els.historyList.addEventListener("click", onHistoryClick);

async function bootstrap() {
  await loadSettings();
  await reloadTemplates();
  await loadHistory();

  refreshSavedPrompts();
  refreshFavoriteTemplates();
  updateButtons();
}

async function onRecommend() {
  const userInput = els.requestInput.value.trim();
  if (!userInput) {
    renderInfo(els.recommendationBox, "요청 문장을 입력하세요.");
    return;
  }

  setBusy(true);
  try {
    const result = await apiPost("/api/recommend", { userInput });
    state.candidates = result.candidates ?? [];
    state.policy = result.policy ?? "-";
    state.recommendation = result.recommendation ?? null;

    renderCandidates(state.candidates);
    els.policyBadge.textContent = `Policy: ${state.policy}`;

    if (!state.recommendation) {
      renderInfo(els.recommendationBox, result.message ?? "추천 결과가 없습니다.");
      return;
    }

    selectTemplate(state.recommendation.templateId, {
      manual: false,
      reason: "recommendation",
    });
    renderRecommendation(result);
  } catch (error) {
    renderInfo(els.recommendationBox, `추천 실패: ${error.message}`);
  } finally {
    setBusy(false);
  }
}

function onTemplateChanged() {
  selectTemplate(els.templateSelect.value, {
    manual: true,
    reason: "template-select",
  });
}

async function loadSettings() {
  try {
    const settings = await apiGet("/api/settings");
    state.settings = settings;

    els.modelInput.value = settings.model ?? "";
    els.openAiEndpointInput.value = settings.openAiEndpoint ?? "";
    els.templatesPathInput.value = settings.templatesPath ?? "";
    els.outputPathInput.value = settings.outputPath ?? "";
    els.zipPathInput.value = settings.zipPath ?? "";
    els.aiModeSelect.value = settings.aiMode ?? "mock";
    els.openAiApiKeyInput.value = "";
    els.openAiApiKeyInput.placeholder = settings.hasApiKey
      ? `저장됨 (${settings.apiKeyMasked}) - 변경 시 새 값 입력`
      : "새 API Key 입력(비우면 기존 유지)";

    renderInfo(
      els.settingsStatus,
      [
        "설정 로드 완료",
        `AI 모드: ${settings.aiMode}`,
        `Endpoint: ${settings.openAiEndpoint ?? "-"}`,
        `Templates: ${settings.templatesPath}`,
        `Output: ${settings.outputPath}`,
        `ZIP: ${settings.zipPath}`,
      ].join("\n"),
    );
  } catch (error) {
    renderInfo(els.settingsStatus, `설정 로드 실패: ${error.message}`);
    throw error;
  }
}

async function onSaveSettings() {
  const payload = collectSettingsPayload();
  setBusy(true);

  try {
    const result = await apiPost("/api/settings", payload);
    state.clearApiKeyRequested = false;
    state.settings = result.settings ?? null;

    renderInfo(
      els.settingsStatus,
      [
        "설정 저장 완료",
        `AI 모드: ${result.settings?.aiMode ?? "-"}`,
        `API Key: ${result.settings?.hasApiKey ? result.settings.apiKeyMasked : "(none)"}`,
      ].join("\n"),
    );

    await loadSettings();
    await reloadTemplates();
  } catch (error) {
    renderInfo(
      els.settingsStatus,
      [
        `설정 저장 실패: ${error.message}`,
        ...formatErrorDetails(error),
      ].join("\n"),
    );
  } finally {
    setBusy(false);
  }
}

function onClearApiKey() {
  state.clearApiKeyRequested = true;
  els.openAiApiKeyInput.value = "";
  renderInfo(
    els.settingsStatus,
    "API Key 초기화가 예약되었습니다. '설정 저장'을 누르면 실제로 반영됩니다.",
  );
}

function onSavePrompt() {
  const text = els.requestInput.value.trim();
  if (!text) {
    renderInfo(els.recommendationBox, "저장할 자연어 입력을 먼저 작성하세요.");
    return;
  }

  try {
    state.preferenceService.savePrompt(text);
    refreshSavedPrompts();
    renderInfo(els.recommendationBox, "자주 쓰는 입력으로 저장했습니다.");
  } catch (error) {
    renderInfo(els.recommendationBox, `입력 저장 실패: ${error.message}`);
  }
}

function onSavedPromptClick(event) {
  const applyButton = event.target.closest("button[data-saved-prompt]");
  if (applyButton) {
    const prompt = applyButton.dataset.savedPrompt;
    if (prompt) {
      els.requestInput.value = prompt;
      renderInfo(els.recommendationBox, "저장된 입력을 적용했습니다. 추천 받기를 눌러 진행하세요.");
    }
    return;
  }

  const removeButton = event.target.closest("button[data-remove-saved-prompt]");
  if (!removeButton) {
    return;
  }

  const prompt = removeButton.dataset.removeSavedPrompt;
  if (!prompt) {
    return;
  }

  state.preferenceService.removePrompt(prompt);
  refreshSavedPrompts();
}

function onSaveFavoriteTemplate() {
  const template = state.templatesById.get(state.selectedTemplateId);
  if (!template) {
    renderInfo(els.recommendationBox, "저장할 템플릿을 선택하세요.");
    return;
  }

  try {
    state.preferenceService.saveFavoriteTemplate(template.id, template.name);
    refreshFavoriteTemplates();
    renderInfo(els.recommendationBox, `템플릿 즐겨찾기 저장: ${template.id}`);
  } catch (error) {
    renderInfo(els.recommendationBox, `즐겨찾기 저장 실패: ${error.message}`);
  }
}

function onFavoriteTemplateClick(event) {
  const applyButton = event.target.closest("button[data-favorite-template-id]");
  if (applyButton) {
    const templateId = applyButton.dataset.favoriteTemplateId;
    if (templateId) {
      selectTemplate(templateId, { manual: true, reason: "favorite-template" });
    }
    return;
  }

  const removeButton = event.target.closest("button[data-remove-favorite-template-id]");
  if (!removeButton) {
    return;
  }

  const templateId = removeButton.dataset.removeFavoriteTemplateId;
  if (!templateId) {
    return;
  }

  state.preferenceService.removeFavoriteTemplate(templateId);
  refreshFavoriteTemplates();
}

function onCandidateTemplateClick(event) {
  const button = event.target.closest("button[data-template-id]");
  if (!button) {
    return;
  }

  const templateId = button.dataset.templateId;
  if (!templateId) {
    return;
  }

  selectTemplate(templateId, {
    manual: true,
    reason: "candidate-list",
  });
}

function onFormValueChanged() {
  updateGenerateButtonState();
  scheduleAutoPreview();
}

async function onGenerate() {
  if (!state.selectedTemplateId) {
    renderInfo(els.resultPaths, "템플릿을 선택하세요.");
    return;
  }

  const payload = collectScaffoldPayload();
  const selectedTemplate = state.templatesById.get(state.selectedTemplateId);
  if (!selectedTemplate) {
    renderInfo(els.resultPaths, "선택한 템플릿 정보를 찾을 수 없습니다.");
    return;
  }

  const missingVariables = getMissingRequiredVariables(selectedTemplate, payload.variables);
  if (missingVariables.length > 0) {
    renderInfo(
      els.resultPaths,
      [
        "생성 불가: 필수 변수가 누락되었습니다.",
        ...missingVariables.map((key) => `- ${key}`),
      ].join("\n"),
    );
    return;
  }

  setBusy(true);
  renderInfo(els.resultPaths, "생성 중입니다. 잠시만 기다려주세요...");
  try {
    const result = await apiPost("/api/generate", payload);
    renderInfo(
      els.resultPaths,
      [
        `프로젝트 경로: ${result.generatedProjectPath}`,
        `ZIP 경로: ${result.generatedZipPath}`,
      ].join("\n"),
    );

    if (result.tree) {
      els.treePreview.textContent = toTreeText(result.tree);
    }
  } catch (error) {
    renderInfo(els.resultPaths, `생성 실패: ${error.message}`);
  } finally {
    await loadHistory();
    setBusy(false);
  }
}

async function runPreview({ auto }) {
  if (!state.selectedTemplateId) {
    if (!auto) {
      els.treePreview.textContent = "템플릿을 선택하세요.";
    }
    return;
  }

  const payload = collectScaffoldPayload();
  const selectedTemplate = state.templatesById.get(state.selectedTemplateId);
  if (!selectedTemplate) {
    if (!auto) {
      els.treePreview.textContent = "선택한 템플릿 정보를 찾을 수 없습니다.";
    }
    return;
  }

  const missingVariables = getMissingRequiredVariables(selectedTemplate, payload.variables);
  if (missingVariables.length > 0) {
    els.treePreview.textContent = [
      "필수 변수 입력이 필요합니다.",
      ...missingVariables.map((key) => `- ${key}`),
    ].join("\n");
    return;
  }

  const requestId = ++state.previewRequestId;
  setPreviewBusy(true);
  if (!auto) {
    els.treePreview.textContent = "구조 미리보기 생성 중...";
  }
  try {
    const result = await apiPost("/api/preview", payload);
    if (requestId !== state.previewRequestId) {
      return;
    }

    els.treePreview.textContent = toTreeText(result.tree);
  } catch (error) {
    if (requestId !== state.previewRequestId) {
      return;
    }

    els.treePreview.textContent = `미리보기 실패: ${error.message}`;
  } finally {
    if (requestId === state.previewRequestId) {
      setPreviewBusy(false);
    }
  }
}

function scheduleAutoPreview() {
  if (state.previewTimer) {
    clearTimeout(state.previewTimer);
  }

  state.previewTimer = setTimeout(() => {
    void runPreview({ auto: true });
  }, 250);
}

function selectTemplate(templateId, { manual, reason }) {
  if (!state.templatesById.has(templateId)) {
    return;
  }

  state.selectedTemplateId = templateId;
  els.templateSelect.value = templateId;

  renderForms();
  renderCandidates(state.candidates);
  updateGenerateButtonState();
  scheduleAutoPreview();

  if (manual) {
    renderInfo(
      els.recommendationBox,
      [
        `수동 선택: ${templateId}`,
        `선택 경로: ${reason}`,
        "변수/옵션을 수정한 뒤 미리보기와 생성을 진행하세요.",
      ].join("\n"),
    );
  }
}

function refreshSavedPrompts() {
  if (!els.savedPromptList) {
    return;
  }

  state.savedPrompts = state.preferenceService.getSavedPrompts();
  els.savedPromptList.innerHTML = "";

  if (state.savedPrompts.length === 0) {
    const empty = document.createElement("span");
    empty.className = "muted";
    empty.textContent = "저장된 입력이 없습니다.";
    els.savedPromptList.append(empty);
    return;
  }

  state.savedPrompts.forEach((entry) => {
    const apply = document.createElement("button");
    apply.type = "button";
    apply.className = "quick-chip";
    apply.dataset.savedPrompt = entry.text;
    apply.textContent = entry.text.length > 44
      ? `${entry.text.slice(0, 44)}...`
      : entry.text;

    const remove = document.createElement("button");
    remove.type = "button";
    remove.className = "quick-chip-remove";
    remove.dataset.removeSavedPrompt = entry.text;
    remove.textContent = "삭제";

    els.savedPromptList.append(apply, remove);
  });
}

function refreshFavoriteTemplates() {
  state.favoriteTemplates = state.preferenceService.getFavoriteTemplates();
  els.favoriteTemplateList.innerHTML = "";

  if (state.favoriteTemplates.length === 0) {
    const empty = document.createElement("span");
    empty.className = "muted";
    empty.textContent = "저장된 즐겨찾기가 없습니다.";
    els.favoriteTemplateList.append(empty);
    return;
  }

  state.favoriteTemplates.forEach((entry) => {
    const apply = document.createElement("button");
    apply.type = "button";
    apply.className = "quick-chip";
    apply.dataset.favoriteTemplateId = entry.templateId;
    apply.textContent = `${entry.templateName} (${entry.templateId})`;

    const remove = document.createElement("button");
    remove.type = "button";
    remove.className = "quick-chip-remove";
    remove.dataset.removeFavoriteTemplateId = entry.templateId;
    remove.textContent = "삭제";

    els.favoriteTemplateList.append(apply, remove);
  });
}

function fillTemplateSelect(templates) {
  els.templateSelect.innerHTML = "";
  templates.forEach((template) => {
    const option = document.createElement("option");
    option.value = template.id;
    option.textContent = `${template.name} (${template.id})`;
    els.templateSelect.append(option);
  });
}

async function reloadTemplates() {
  const templates = await apiGet("/api/templates");
  state.templates = templates;
  state.templatesById = new Map(templates.map((template) => [template.id, template]));
  fillTemplateSelect(templates);

  if (templates.length === 0) {
    state.selectedTemplateId = "";
    els.templateSelect.value = "";
    els.variablesForm.innerHTML = "";
    els.optionsForm.innerHTML = "";
    els.treePreview.textContent = "템플릿이 없습니다. 설정의 templates 경로를 확인하세요.";
    return;
  }

  if (!state.selectedTemplateId || !state.templatesById.has(state.selectedTemplateId)) {
    state.selectedTemplateId = templates[0].id;
  }

  selectTemplate(state.selectedTemplateId, { manual: false, reason: "reload-templates" });
}

async function loadHistory() {
  try {
    const result = await apiGet("/api/history?limit=5");
    state.historyEntries = Array.isArray(result.items) ? result.items : [];
    renderHistory(state.historyEntries);
    renderInfo(
      els.historyStatus,
      `히스토리 ${state.historyEntries.length}건 로드 완료`,
    );
  } catch (error) {
    state.historyEntries = [];
    renderHistory([]);
    renderInfo(els.historyStatus, `히스토리 로드 실패: ${error.message}`);
  }
}

function renderHistory(entries) {
  els.historyList.innerHTML = "";

  if (!entries || entries.length === 0) {
    const empty = document.createElement("li");
    empty.className = "muted";
    empty.textContent = "저장된 생성 히스토리가 없습니다.";
    els.historyList.append(empty);
    return;
  }

  entries.forEach((entry) => {
    const item = document.createElement("li");
    const button = document.createElement("button");
    button.type = "button";
    button.className = "history-entry-btn";
    button.dataset.historyId = entry.id;

    const title = document.createElement("div");
    title.className = "history-entry-title";
    title.textContent = `${entry.success ? "성공" : "실패"} · ${entry.selectedTemplateId || "(template 없음)"}`;

    const meta = document.createElement("div");
    meta.className = "history-entry-meta";
    meta.textContent = [
      toDisplayTime(entry.createdAtUtc),
      entry.userInput ? `요청: ${truncate(entry.userInput, 72)}` : "요청 없음",
    ].join(" · ");

    button.append(title, meta);
    item.append(button);
    els.historyList.append(item);
  });
}

function onHistoryClick(event) {
  const button = event.target.closest("button[data-history-id]");
  if (!button) {
    return;
  }

  const historyId = button.dataset.historyId;
  if (!historyId) {
    return;
  }

  const entry = state.historyEntries.find((item) => item.id === historyId);
  if (!entry) {
    renderInfo(els.historyStatus, "선택한 히스토리 항목을 찾을 수 없습니다.");
    return;
  }

  applyHistoryEntry(entry);
}

function applyHistoryEntry(entry) {
  if (entry.userInput) {
    els.requestInput.value = entry.userInput;
  }

  if (!entry.selectedTemplateId || !state.templatesById.has(entry.selectedTemplateId)) {
    renderInfo(
      els.historyStatus,
      `히스토리 복원 실패: 템플릿 '${entry.selectedTemplateId}'를 찾을 수 없습니다.`,
    );
    return;
  }

  selectTemplate(entry.selectedTemplateId, {
    manual: true,
    reason: "history-restore",
  });

  const variables = entry.variables ?? {};
  for (const input of els.variablesForm.querySelectorAll("input[data-kind='variable']")) {
    const key = input.dataset.key;
    if (Object.prototype.hasOwnProperty.call(variables, key)) {
      input.value = asString(variables[key], "");
    }
  }

  const options = entry.options ?? {};
  for (const input of els.optionsForm.querySelectorAll("[data-kind='option']")) {
    const key = input.dataset.key;
    if (!Object.prototype.hasOwnProperty.call(options, key)) {
      continue;
    }

    if (input.type === "checkbox") {
      input.checked = asBoolean(options[key], false);
      continue;
    }

    input.value = asString(options[key], "");
  }

  updateGenerateButtonState();
  scheduleAutoPreview();

  const statusLine = entry.success
    ? `이전 생성 성공 기록 복원됨: ${toDisplayTime(entry.createdAtUtc)}`
    : `이전 생성 실패 기록 복원됨: ${toDisplayTime(entry.createdAtUtc)}`;

  renderInfo(
    els.resultPaths,
    [
      statusLine,
      entry.generatedProjectPath ? `프로젝트 경로: ${entry.generatedProjectPath}` : "프로젝트 경로: (없음)",
      entry.generatedZipPath ? `ZIP 경로: ${entry.generatedZipPath}` : "ZIP 경로: (없음)",
      entry.failureReason ? `실패 사유: ${entry.failureReason}` : "",
    ].filter(Boolean).join("\n"),
  );

  renderInfo(els.historyStatus, "히스토리 항목을 불러와 폼에 적용했습니다.");
}

function renderForms() {
  const template = state.templatesById.get(state.selectedTemplateId);
  if (!template) {
    els.variablesForm.innerHTML = "";
    els.optionsForm.innerHTML = "";
    return;
  }

  const recommendation = getActiveRecommendation(template.id);
  const recommendationVariables = recommendation?.variables ?? {};
  const variableKeys = dedupe([
    ...(template.requiredVariables ?? []),
    ...Object.keys(template.defaultVariables ?? {}),
  ]);

  els.variablesForm.innerHTML = "";
  variableKeys.forEach((key) => {
    const wrapper = document.createElement("div");
    wrapper.className = "field";

    const label = document.createElement("label");
    const isRequired = (template.requiredVariables ?? []).includes(key);
    label.textContent = isRequired ? `${key} *` : key;

    const input = document.createElement("input");
    input.type = "text";
    input.dataset.kind = "variable";
    input.dataset.key = key;
    input.value =
      recommendationVariables[key] ??
      template.defaultVariables?.[key] ??
      "";

    wrapper.append(label, input);
    els.variablesForm.append(wrapper);
  });

  const recommendationOptions = recommendation?.options ?? {};
  els.optionsForm.innerHTML = "";
  (template.options ?? []).forEach((optionDef) => {
    const type = String(optionDef.type ?? "").toLowerCase();

    if (type === "boolean" || type === "bool") {
      const row = document.createElement("label");
      row.className = "check-field";

      const input = document.createElement("input");
      input.type = "checkbox";
      input.dataset.kind = "option";
      input.dataset.key = optionDef.key;
      input.dataset.type = type;
      input.checked = asBoolean(recommendationOptions[optionDef.key], optionDef.default);

      const text = document.createElement("span");
      text.textContent = `${optionDef.label} (${optionDef.key})`;

      row.append(input, text);
      els.optionsForm.append(row);
      return;
    }

    const wrapper = document.createElement("div");
    wrapper.className = "field";

    const label = document.createElement("label");
    label.textContent = `${optionDef.label} (${optionDef.key})`;

    const input = document.createElement("input");
    input.type = "text";
    input.dataset.kind = "option";
    input.dataset.key = optionDef.key;
    input.dataset.type = type;
    input.value = asString(recommendationOptions[optionDef.key], optionDef.default);

    wrapper.append(label, input);
    els.optionsForm.append(wrapper);
  });
}

function renderCandidates(candidates) {
  els.candidateList.innerHTML = "";
  if (!candidates || candidates.length === 0) {
    const item = document.createElement("li");
    item.textContent = "후보 없음";
    els.candidateList.append(item);
    return;
  }

  candidates.forEach((candidate) => {
    const item = document.createElement("li");
    const button = document.createElement("button");
    button.type = "button";
    button.dataset.templateId = candidate.id;
    button.className = `candidate-btn${candidate.id === state.selectedTemplateId ? " is-active" : ""}`;
    button.textContent = `${candidate.id} · ${candidate.framework} · ${candidate.language}`;
    item.append(button);
    els.candidateList.append(item);
  });
}

function renderRecommendation(result) {
  const recommendation = result.recommendation;
  if (!recommendation) {
    renderInfo(els.recommendationBox, result.message ?? "추천 결과가 없습니다.");
    return;
  }

  const lines = [
    `templateId: ${recommendation.templateId}`,
    `confidence: ${Number(recommendation.confidence).toFixed(2)}`,
    `policy: ${result.policy}`,
    "",
    "variables:",
    ...formatDictionary(recommendation.variables),
    "",
    "options:",
    ...formatDictionary(recommendation.options),
    "",
    "안내: 후보/템플릿/변수/옵션은 사용자가 직접 수정할 수 있습니다.",
  ];

  renderInfo(els.recommendationBox, lines.join("\n"));
}

function collectScaffoldPayload() {
  const variables = {};
  for (const input of els.variablesForm.querySelectorAll("input[data-kind='variable']")) {
    variables[input.dataset.key] = input.value.trim();
  }

  const options = {};
  for (const input of els.optionsForm.querySelectorAll("[data-kind='option']")) {
    const key = input.dataset.key;
    const type = String(input.dataset.type ?? "").toLowerCase();

    if (input.type === "checkbox") {
      options[key] = input.checked;
      continue;
    }

    if (type === "number") {
      const parsed = Number(input.value);
      options[key] = Number.isFinite(parsed) ? parsed : input.value;
      continue;
    }

    options[key] = input.value;
  }

  return {
    userInput: els.requestInput.value.trim(),
    templateId: state.selectedTemplateId,
    variables,
    options,
  };
}

function collectSettingsPayload() {
  const openAiApiKey = els.openAiApiKeyInput.value.trim();

  return {
    openAiApiKey: openAiApiKey || null,
    clearOpenAiApiKey: state.clearApiKeyRequested,
    model: els.modelInput.value.trim(),
    templatesPath: els.templatesPathInput.value.trim(),
    openAiEndpoint: els.openAiEndpointInput.value.trim(),
    outputPath: els.outputPathInput.value.trim(),
    zipPath: els.zipPathInput.value.trim(),
    aiMode: els.aiModeSelect.value,
  };
}

function updateGenerateButtonState() {
  const template = state.templatesById.get(state.selectedTemplateId);
  if (!template) {
    updateButtons({ canGenerate: false });
    return;
  }

  const payload = collectScaffoldPayload();
  const missing = getMissingRequiredVariables(template, payload.variables);
  const canGenerate = missing.length === 0;
  updateButtons({ canGenerate });

  if (!canGenerate) {
    renderInfo(
      els.resultPaths,
      [
        "생성 대기: 필수 변수를 입력하세요.",
        ...missing.map((key) => `- ${key}`),
      ].join("\n"),
    );
  }
}

function updateButtons({ canGenerate = true } = {}) {
  const disableAll = state.isBusy;
  els.recommendBtn.disabled = disableAll;
  els.saveSettingsBtn.disabled = disableAll;
  els.clearApiKeyBtn.disabled = disableAll;
  els.modelInput.disabled = disableAll;
  els.templatesPathInput.disabled = disableAll;
  els.openAiEndpointInput.disabled = disableAll;
  els.outputPathInput.disabled = disableAll;
  els.zipPathInput.disabled = disableAll;
  els.aiModeSelect.disabled = disableAll;
  els.openAiApiKeyInput.disabled = disableAll;
  els.templateSelect.disabled = disableAll;
  els.previewBtn.disabled = disableAll || state.previewBusy;
  els.generateBtn.disabled = disableAll || !canGenerate;
  els.refreshHistoryBtn.disabled = disableAll;
}

function setBusy(isBusy) {
  state.isBusy = isBusy;
  updateGenerateButtonState();
}

function setPreviewBusy(isBusy) {
  state.previewBusy = isBusy;
  updateGenerateButtonState();
}

function getMissingRequiredVariables(template, variables) {
  return (template.requiredVariables ?? []).filter((key) => {
    const value = variables[key];
    return value === null || value === undefined || String(value).trim() === "";
  });
}

function getActiveRecommendation(templateId) {
  if (!state.recommendation) {
    return null;
  }

  return state.recommendation.templateId === templateId
    ? state.recommendation
    : null;
}

function formatDictionary(dictionary) {
  if (!dictionary || Object.keys(dictionary).length === 0) {
    return ["- (none)"];
  }

  return Object.entries(dictionary).map(([key, value]) => `- ${key}: ${value}`);
}

function toTreeText(root) {
  if (!root) {
    return "(empty)";
  }

  const lines = [];
  walk(root, 0);
  return lines.join("\n");

  function walk(node, depth) {
    const marker = node.type?.toLowerCase() === "folder" ? "[D]" : "[F]";
    lines.push(`${"  ".repeat(depth)}${marker} ${node.name}`);
    (node.children ?? []).forEach((child) => walk(child, depth + 1));
  }
}

function renderInfo(element, text) {
  element.textContent = text;
}

function dedupe(items) {
  return [...new Set(items)];
}

function asBoolean(preferred, fallback) {
  if (typeof preferred === "boolean") {
    return preferred;
  }
  if (typeof fallback === "boolean") {
    return fallback;
  }
  return false;
}

function asString(preferred, fallback) {
  if (preferred === null || preferred === undefined) {
    return fallback === null || fallback === undefined ? "" : String(fallback);
  }
  return String(preferred);
}

function toDisplayTime(value) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return date.toLocaleString("ko-KR");
}

function truncate(value, length) {
  if (!value) {
    return "";
  }

  if (value.length <= length) {
    return value;
  }

  return `${value.slice(0, length)}...`;
}

async function apiGet(url) {
  const response = await fetch(url, { headers: { Accept: "application/json" } });
  return handleResponse(response);
}

async function apiPost(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify(payload),
  });

  return handleResponse(response);
}

async function handleResponse(response) {
  const text = await response.text();
  const data = text ? JSON.parse(text) : {};
  if (!response.ok) {
    const error = new Error(data.message ?? `HTTP ${response.status}`);
    error.details = data.errors;
    throw error;
  }

  return data;
}

function formatErrorDetails(error) {
  if (!error || !Array.isArray(error.details) || error.details.length === 0) {
    return [];
  }

  return error.details.map((entry) => {
    const field = entry?.field ?? "-";
    const message = entry?.message ?? "알 수 없는 검증 오류";
    return `- ${field}: ${message}`;
  });
}
