﻿using AudioRecorder.Models;

namespace AudioRecorder.Extensions;

/// <summary>
/// Методы расширения для работы с файлом амплитуд
/// </summary>
public static class FileStringExtension
{
    /// <summary>
    /// Готовим строку для записи в файл
    /// </summary>
    /// <param name="timeSpan"></param>
    /// <param name="sample"></param>
    /// <returns></returns>
    public static string GetFileSting(this TimeSpan timeSpan, short sample)
        => string.Format("{0}|{1}", timeSpan.Ticks.ToString(), sample.ToString());
    /// <summary>
    /// Получения сэмпла аудио из строки файла
    /// </summary>
    /// <param name="fileString"></param>
    /// <returns></returns>
    public static FileRecordModel ToFileRecordModel(this string fileString)
    {
        var sample = fileString.Split('|');
        return new FileRecordModel
        {
            Time = new TimeSpan(long.Parse(sample[0])),
            Sample = short.Parse(sample[1])
        };
    }
}
