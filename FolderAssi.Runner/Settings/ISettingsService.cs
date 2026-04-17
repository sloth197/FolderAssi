internal interface ISettingsService
{
    AppSettings Get();
    AppSettingsView GetView();
    AppSettings Save(AppSettingsUpdateRequest request);
    string SettingsFilePath { get; }
}
