using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecorder.Constants;

/// <summary>
/// Различные аудиоконстанты
/// </summary>
public class AudioDefaults
{
    /// <summary>
    /// Количество тактов в секунду
    /// </summary>
    public const long _ticksPerSecond = 1_0_000_000L;
    /// <summary>
    /// Продолжительность сэмпла микрофона по умолчанию
    /// </summary>
    public static long _microphoneSamplePeriod => _ticksPerSecond / 16000;
}
