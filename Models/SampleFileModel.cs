using RealTimeGraphX.DataPoints;

namespace AudioRecorder.Models;

/// <summary>
/// Модель данных файла амплитуд для отображения на графике
/// </summary>
public class SampleFileModel
{
    /// <summary>
    /// Данные оси абсцисс - метки времени
    /// </summary>
    public TimeSpanDataPoint[]? XArray { get; set; }
    /// <summary>
    /// Данные оси ординат - значение амплитуды сигнала
    /// </summary>
    public DoubleDataPoint[]? YArray { get; set; }
    /// <summary>
    /// Максимальная метка времени - диапазон отображения графика
    /// </summary>
    public TimeSpan MaxX { get; set; }
}
