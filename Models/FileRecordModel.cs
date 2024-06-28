
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecorder.Models;
/// <summary>
/// Модель для записи в файл
/// </summary>
public class FileRecordModel
{
    /// <summary>
    /// Время сэмпла
    /// </summary>
    public TimeSpan Time { get; set; }
    /// <summary>
    /// Значение сэмпла
    /// </summary>
    public short Sample { get; set; }
}
