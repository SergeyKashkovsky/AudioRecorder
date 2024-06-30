using NAudio.Wave;

namespace AudioRecorder.Models;

/// <summary>
/// Аудио устройство
/// </summary>
public class AudioDevice
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public int Id { get; set; }
    

    /// <summary>
    /// Отображаемое название
    /// </summary>
    public string? Name { get; set; }


    public AudioDevice(WaveInCapabilities caps, int index)
    {
        Id = index;
        Name = caps.ProductName;
    }

    public AudioDevice()
    {
    }
}
