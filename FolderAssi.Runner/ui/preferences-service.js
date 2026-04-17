const PreferenceStorageKeys = Object.freeze({
  favoriteTemplates: "folderassi.favoriteTemplates.v1",
  savedPrompts: "folderassi.savedPrompts.v1",
});

class FavoriteTemplateEntry {
  constructor(templateId, templateName, savedAtUtc) {
    this.templateId = templateId;
    this.templateName = templateName;
    this.savedAtUtc = savedAtUtc;
  }
}

class SavedPromptEntry {
  constructor(text, savedAtUtc) {
    this.text = text;
    this.savedAtUtc = savedAtUtc;
  }
}

class UiPreferenceService {
  constructor(storage) {
    this.storage = storage ?? window.localStorage;
  }

  getFavoriteTemplates() {
    return this.readArray(PreferenceStorageKeys.favoriteTemplates)
      .filter((item) => item && typeof item.templateId === "string")
      .map((item) => new FavoriteTemplateEntry(
        item.templateId,
        typeof item.templateName === "string" ? item.templateName : item.templateId,
        typeof item.savedAtUtc === "string" ? item.savedAtUtc : new Date().toISOString(),
      ));
  }

  saveFavoriteTemplate(templateId, templateName) {
    if (!templateId || !String(templateId).trim()) {
      throw new Error("templateId is required.");
    }

    const favorites = this.getFavoriteTemplates()
      .filter((item) => item.templateId !== templateId);

    favorites.unshift(
      new FavoriteTemplateEntry(
        String(templateId).trim(),
        templateName && String(templateName).trim() ? String(templateName).trim() : String(templateId).trim(),
        new Date().toISOString(),
      ),
    );

    this.writeArray(PreferenceStorageKeys.favoriteTemplates, favorites.slice(0, 20));
  }

  removeFavoriteTemplate(templateId) {
    const favorites = this.getFavoriteTemplates()
      .filter((item) => item.templateId !== templateId);
    this.writeArray(PreferenceStorageKeys.favoriteTemplates, favorites);
  }

  getSavedPrompts() {
    return this.readArray(PreferenceStorageKeys.savedPrompts)
      .filter((item) => item && typeof item.text === "string" && item.text.trim().length > 0)
      .map((item) => new SavedPromptEntry(
        item.text.trim(),
        typeof item.savedAtUtc === "string" ? item.savedAtUtc : new Date().toISOString(),
      ));
  }

  savePrompt(text) {
    const trimmed = text ? String(text).trim() : "";
    if (!trimmed) {
      throw new Error("Prompt text is required.");
    }

    const prompts = this.getSavedPrompts()
      .filter((item) => item.text !== trimmed);

    prompts.unshift(new SavedPromptEntry(trimmed, new Date().toISOString()));
    this.writeArray(PreferenceStorageKeys.savedPrompts, prompts.slice(0, 30));
  }

  removePrompt(text) {
    const prompts = this.getSavedPrompts()
      .filter((item) => item.text !== text);
    this.writeArray(PreferenceStorageKeys.savedPrompts, prompts);
  }

  readArray(storageKey) {
    try {
      const raw = this.storage.getItem(storageKey);
      if (!raw) {
        return [];
      }

      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  writeArray(storageKey, items) {
    this.storage.setItem(storageKey, JSON.stringify(items));
  }
}
