namespace OculusLauncher.Services;

public interface IOculusProcessService
{
    string? FindOculusClientPath(string? manualOverride = null);
    bool IsOculusRunning();
    void KillOculusClient();
    void StartOculusClient(string exePath);
}
