using AudioRecorder.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecorder.Services;

/// <summary>
/// Сервис для работы с аудио-потоками
/// </summary>
public class AudioService
{
    /// <summary>
    /// Обработка любого файла
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="needToWriteRightChannel"></param>
    /// <param name="outputFile"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IEnumerable<string> ProcessFile(string filePath, bool needToWriteRightChannel, string outputFile)
    {
        using var reader = new MediaFoundationReader(filePath);
        var hdrInfo = reader.WaveFormat;
        if (hdrInfo.Channels > 2)
            throw new NotImplementedException("Пока не могу обрабатывать более двух каналов");

        var info = new FileInfo(outputFile);
        var now = info.Name.Replace(".txt", "");

        var _leftFileName = String.Format(@"{0}\{1}{2}.txt", info.Directory, now, hdrInfo.Channels > 1 && needToWriteRightChannel ? "-left" : string.Empty);
        var _rightFileName = hdrInfo.Channels > 1 && needToWriteRightChannel ? String.Format(@"{0}\{1}-right.txt", info.Directory, now) : string.Empty;

        var result = new List<string> { _leftFileName };
        if (!string.IsNullOrEmpty(_rightFileName)) result.Add(_rightFileName);

        byte[] bytes = new byte[reader.Length];
        reader.Position = 0;
        reader.Read(bytes, 0, (int)reader.Length);
        using var streamWriter = new StreamWriter(_leftFileName);
        StreamWriter? rightStreamWriter = null;
        if (hdrInfo.Channels > 1 && needToWriteRightChannel) rightStreamWriter = new StreamWriter(_rightFileName);

        var samplesPerTick = hdrInfo.Channels == 1 ? 2 : 4;
        for (int i = 0; i < reader.Length; i += samplesPerTick)
        {
            try
            {
                if (i >= bytes.Length-1) return result;

                var leftSample = (short)((bytes[i + 1] << 8) | bytes[i]);
                streamWriter!.WriteLineAsync(leftSample.ToString()).GetAwaiter().GetResult();
                if (hdrInfo.Channels > 1 && needToWriteRightChannel && bytes.Length > (i + 2) && bytes.Length >= (i + 3))
                {
                    var rightSample = (short)((bytes[i + 3] << 8) | bytes[i + 2]);
                    rightStreamWriter!.WriteLineAsync(rightSample.ToString()).GetAwaiter().GetResult();
                }
            }
            catch{}

            
        }

        return result;
    }

    /// <summary>
    /// Получаем амплитудый из Wav-файла, пишем каналы отдельно, если нужно
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <param name="needToWriteRightChannel">Нужно ли отдельно писать правый канал. Если не установлено, будет писать только левый, если тип записи стерео.</param>
    /// <returns>Коллекцию путей созданных файлов</returns>
    public static IEnumerable<string> ProcessWavFile(string filePath, bool needToWriteRightChannel, string outputFile)
    {
        //TODO: Вынести из метода функционал преобразования потока wav в поток уровней (сэмплов) так, чтобы не при переборе в цикле писать в файл, а после получения потока
        using var reader = new WaveFileReader(filePath);
        var hdrInfo = reader.WaveFormat;
        if (hdrInfo.Channels > 2)
            throw new NotImplementedException("Пока не могу обрабатывать более двух каналов");

        var info = new FileInfo(outputFile);
        var now = info.Name.Replace(".txt", "");

        var _leftFileName = String.Format(@"{0}\{1}{2}.txt", info.Directory, now, hdrInfo.Channels > 1 && needToWriteRightChannel ? "-left" : string.Empty);
        var _rightFileName = hdrInfo.Channels > 1 && needToWriteRightChannel ? String.Format(@"{0}\{1}-right.txt", info.Directory, now) : string.Empty;

        var result = new List<string> { _leftFileName };
        if (!string.IsNullOrEmpty(_rightFileName)) result.Add(_rightFileName);

        byte[] bytes = new byte[reader.Length];
        reader.Position = 0;
        reader.Read(bytes, 0, (int)reader.Length);
        using var streamWriter = new StreamWriter(_leftFileName);
        StreamWriter? rightStreamWriter = null;
        if(hdrInfo.Channels > 1 && needToWriteRightChannel) rightStreamWriter = new StreamWriter(_rightFileName);

        var samplesPerTick = hdrInfo.Channels == 1 ? 2 : 4;
        for (int i = 0; i < reader.Length; i += samplesPerTick)
        {
            var leftSample = (short)((bytes[i + 1] << 8) | bytes[i]);
            streamWriter!.WriteLineAsync(leftSample.ToString()).GetAwaiter().GetResult();
            if (hdrInfo.Channels > 1 && needToWriteRightChannel)
            {
                var rightSample = (short)((bytes[i + 3] << 8) | bytes[i + 2]);
                rightStreamWriter!.WriteLineAsync(rightSample.ToString()).GetAwaiter().GetResult();
            }
        }

        return result;
    }

    /// <summary>
    /// Получение строки параметров wav-файла
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetWaveFormatString(string filePath)
    {
        using var reader = new WaveFileReader(filePath);
        var hdrInfo = reader.WaveFormat;
        return String.Format("Частота дескритизации: {0}, разрядность: {1}, количество каналов: {2}", hdrInfo.SampleRate, hdrInfo.BitsPerSample, hdrInfo.Channels);
    }


    

    
}
