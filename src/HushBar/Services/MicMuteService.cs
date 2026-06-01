using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace HushBar.Services;

/// <summary>
/// The heart of HushBar on Windows. Toggles the system default capture
/// endpoint's hardware mute flag (WASAPI / MMDevice) — system-wide, no capture
/// stream opened, so nothing is recorded and no microphone permission is needed.
///
/// Mirrors the macOS MicMuteController discipline:
///   - write the mute flag, then READ IT BACK rather than trusting the call,
///   - listen for external mute changes (OnVolumeNotification),
///   - listen for default-device changes (IMMNotificationClient) and re-apply intent.
/// </summary>
public sealed class MicMuteService : IDisposable, IMMNotificationClient
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _device;

    /// <summary>The mute state we intend — re-asserted when the device changes.</summary>
    private bool _muteIntent;

    /// <summary>Raised on the UI thread whenever the effective mute state changes.</summary>
    public event Action<bool>? MuteChanged;

    public bool IsMuted { get; private set; }

    public MicMuteService()
    {
        _enumerator.RegisterEndpointNotificationCallback(this);
        AttachToDefaultDevice();
        _muteIntent = IsMuted;
    }

    // MARK: - Public API

    public void Toggle() => SetMuted(!IsMuted);

    public void SetMuted(bool muted)
    {
        _muteIntent = muted;
        Apply();
    }

    private void Apply()
    {
        if (_device is null) return;
        try
        {
            _device.AudioEndpointVolume.Mute = _muteIntent;
            // Read back the real state instead of trusting the write.
            UpdateMuted(_device.AudioEndpointVolume.Mute);
        }
        catch (Exception ex)
        {
            HushLog.Write($"Apply failed: {ex.Message}");
        }
    }

    private void UpdateMuted(bool muted)
    {
        if (muted == IsMuted) return;
        IsMuted = muted;
        MuteChanged?.Invoke(muted);
    }

    // MARK: - Device attachment + external-change listener

    private void AttachToDefaultDevice()
    {
        DetachDevice();
        try
        {
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            _device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
            IsMuted = _device.AudioEndpointVolume.Mute;
            HushLog.Write($"attached to capture device: {_device.FriendlyName} muted={IsMuted}");
        }
        catch (Exception ex)
        {
            HushLog.Write($"no default capture device: {ex.Message}");
            _device = null;
        }
    }

    private void DetachDevice()
    {
        if (_device is null) return;
        try { _device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification; }
        catch { /* device may already be gone */ }
        _device.Dispose();
        _device = null;
    }

    /// <summary>External mute change (Control Panel, another app) — reflect it.</summary>
    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        // NAudio raises this on a COM thread; marshal to the UI dispatcher.
        System.Windows.Application.Current?.Dispatcher.Invoke(() => UpdateMuted(data.Muted));
    }

    // MARK: - IMMNotificationClient (default-device change)

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow != DataFlow.Capture) return;
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            HushLog.Write("default capture device changed; re-applying intent");
            AttachToDefaultDevice();
            Apply(); // re-assert our intent on the new device
        });
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
    public void OnDeviceAdded(string deviceId) { }
    public void OnDeviceRemoved(string deviceId) { }
    public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }

    // MARK: - Cleanup

    public void Dispose()
    {
        try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch { }
        DetachDevice();
        _enumerator.Dispose();
    }
}
