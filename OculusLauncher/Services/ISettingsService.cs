using OculusLauncher.Models;

namespace OculusLauncher.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
