using AudioRecorder.Constants;
using AudioRecorder.Extensions;
using AudioRecorder.Interfaces;
using AudioRecorder.Models;
using NAudio.MediaFoundation;
using NAudio.Wave;
using RealTimeGraphX.DataPoints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.DataFormats;

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
        var sampleRate = AudioDefaults._ticksPerSecond / hdrInfo.SampleRate;
        var period = new TimeSpan(sampleRate);
        var timePosition = new TimeSpan();

        if (hdrInfo.Channels > 2)
            throw new NotImplementedException("Пока не могу обрабатывать более двух каналов");

        var info = new FileInfo(outputFile);
        var now = info.Name.Replace(".txt", "");
        if (!outputFile.EndsWith(".wav"))
        {
            var outfile = String.Format(@"{0}\{1}.wav", info.Directory, now);
            WaveFileWriter.CreateWaveFile(outfile, reader);
        }
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
            timePosition = timePosition.Add(period);
            try
            {
                if (i >= bytes.Length-1) return result;

                var leftSample = (short)((bytes[i + 1] << 8) | bytes[i]);
                streamWriter!.WriteLineAsync(timePosition.GetFileSting(leftSample)).GetAwaiter().GetResult();
                if (hdrInfo.Channels > 1 && needToWriteRightChannel && bytes.Length > (i + 2) && bytes.Length >= (i + 3))
                {
                    var rightSample = (short)((bytes[i + 3] << 8) | bytes[i + 2]);
                    
                    rightStreamWriter!.WriteLineAsync(timePosition.GetFileSting(rightSample)).GetAwaiter().GetResult();
                }
            }
            catch{}

            
        }

        return result;
    }
    /// <summary>
    /// Получение параметров файла
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static WaveFormat GetHeader(string filename)
    {
        using var reader = new MediaFoundationReader(filename);
        return reader.WaveFormat;

    }
    /// <summary>
    /// Получение продолжительности сэмпла
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static long GetSamplePeriod(string filename)
    {
        var format = GetHeader(filename);
        return AudioDefaults._ticksPerSecond / format.SampleRate;
    }
    /// <summary>
    /// Получение параметров файла амплитуд
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static WaveFormat GetSampleFileAudioParams(string filename) 
    {
        using var reader = new StreamReader(filename);
        var firstString = reader.ReadLine()!.ToFileRecordModel().Time.Ticks;
        var sampleRate = AudioDefaults._ticksPerSecond / firstString;
        return new WaveFormat((int)sampleRate, 1);
    }
    /// <summary>
    /// Читаем данные из файла
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static SampleFileModel ReadFileData(string filename)
    {
        var strArray = File.ReadAllLines(filename);
        var samples = strArray.Select(x => x.ToFileRecordModel());
        var maxX = samples.Last().Time;
        var arrays = samples.Select(x => new
        {
            X = new TimeSpanDataPoint(x.Time),
            Y = new DoubleDataPoint(x.Sample)
        }).OrderBy(x => x.X);

        return new SampleFileModel
        {
            XArray = arrays.Select(x => x.X).ToArray(),
            YArray = arrays.Select(x => x.Y).ToArray(),
            MaxX = maxX
        };
        
    }
}
