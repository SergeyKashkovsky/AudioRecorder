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
    private readonly MainWindowViewModel _viewModel;
    private string? _fileName = string.Empty;
    private WaveIn? _waveIn = null; 
    private StreamWriter? _streamWriter = null;
    private WaveFileWriter? _writer = null;
    private TimeSpan _graphX;
    private readonly TimeSpan _period = new(AudioDefaults.MicrophoneSamplePeriod);

    public AudioService(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
    }
    #region Функционал записи из микрофона
    /// <summary>
    /// Начинаем писать микрофон
    /// </summary>
    /// <param name="filename"></param>
    public void BeginMicrophoneRecord(string filename)
    {
        _fileName = filename;
        _viewModel.IsRecording = true;
        _viewModel.StatusText = String.Format("Начата запись в файл: {0}", _fileName);
        try
        {
            var waveFormat = new WaveFormat(16000, 16, 1);
            _waveIn = new WaveIn
            {
                BufferMilliseconds = 100,
                DeviceNumber = 0,
                WaveFormat = waveFormat,
            };

            _writer = new WaveFileWriter(_fileName.Replace(".txt", ".wav"), waveFormat);
            _waveIn.DeviceNumber = _viewModel.AudioDevice.Id;
            //Прикрепляем к событию DataAvailable обработчик, возникающий при наличии записываемых данных
            _waveIn.DataAvailable += WaveInDataAvailable!;
            //Прикрепляем обработчик завершения записи
            _waveIn.RecordingStopped += WaveInRecordingStopped!;
            _streamWriter = new StreamWriter(_fileName);
            _waveIn.StartRecording();
            _graphX = new TimeSpan();
            _viewModel.InitControllerForMic();
        }
        catch (Exception ex)
        {
            _viewModel.IsRecording = false;
            _viewModel.StatusText = String.Format("Ошибка: {0}, выполнение программы не возможно.", ex.Message);
        }
    }
    /// <summary>
    /// Завершение записи микрофона
    /// </summary>
    public void EndMicrophoneRecord()
    {
        _viewModel.IsRecording = false;
        _waveIn?.StopRecording();
        _viewModel.StatusText = String.Format("Запись в файл: {0} остановлена. Ожидание запуска", _fileName);
    }

    /// <summary>
    /// Обработка события поступления данных от микрофона
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void WaveInDataAvailable(object sender, WaveInEventArgs e)
    {
        try
        {
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                var sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i + 0]);
                _graphX = _graphX.Add(_period);
                _viewModel.PushGraphData(_graphX, sample);

                _streamWriter!.WriteLineAsync(_graphX.GetFileSting(sample)).GetAwaiter().GetResult();

            }
            _writer!.WriteAsync(e.Buffer, 0, e.BytesRecorded);
        }
        catch (Exception ex)
        {
            _viewModel.IsRecording = false;
            _viewModel.StatusText = String.Format("Ошибка {0}, останавливаю запись в файл {1}", ex.Message, _fileName);
            _waveIn?.StopRecording();
        }
    }

    /// <summary>
    /// Обработка события остановки слушателя событий микрофона
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void WaveInRecordingStopped(object sender, EventArgs e)
    {
        _writer!.Close();
        _writer.Dispose();
        _waveIn?.Dispose();
        _streamWriter?.Dispose();
        _waveIn = null;
        _viewModel.DrawFileGraph(_fileName!);
    }
    #endregion
    #region Функционал записи из аудио-файла
    /// <summary>
    /// Получаем параметры аудио файла, готовим сервис к работе с ним
    /// </summary>
    /// <param name="fileName">Путь к аудиофайлу</param>
    public void PrepareToReadAudioFile(string fileName)
    {
        try
        {
            _fileName = fileName;
            var ft = GetHeader(_fileName);
            if (ft.Channels > 1) _viewModel.NeedForRightChannel = true;
            _viewModel.StatusText = String.Format("Выбран файл {0}, параметры: {1}", _fileName, ft);
            _viewModel.IsFileOpened = true;
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = String.Format("Ошибка при попытке открытия файла: {0}", ex.Message);
        }
    }
    /// <summary>
    /// Читаем аудиофайл, создаем файл амплитуд
    /// </summary>
    /// <param name="fileName">Путь к файлу амплитуд</param>
    public void ReadAudiofile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return;
        if (string.IsNullOrEmpty(_fileName))
        {
            _viewModel.StatusText = "Файл не выбран, как у вас получилось нажать эту кнопку?";
            return;
        }
        _viewModel.StatusText = String.Format("Начата обработка файла {0}", _fileName);
        try
        {
            var files = AudioService.ProcessFile(_fileName, _viewModel.WriteRightChannel, fileName);
            _viewModel.DrawFileGraph(files.First());
            _viewModel.StatusText = String.Format("Закончена обработка файла {0}. Результат: {1}", _fileName, String.Join(", ", files));
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = String.Format("Ошибка при обработке файла {0}: {1}", _fileName, ex.Message);
        }
    }
    /// <summary>
    /// Обработка аудио файла
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
                if (i >= bytes.Length - 1) return result;

                var leftSample = (short)((bytes[i + 1] << 8) | bytes[i]);
                streamWriter!.WriteLineAsync(timePosition.GetFileSting(leftSample)).GetAwaiter().GetResult();
                if (hdrInfo.Channels > 1 && needToWriteRightChannel && bytes.Length > (i + 2) && bytes.Length >= (i + 3))
                {
                    var rightSample = (short)((bytes[i + 3] << 8) | bytes[i + 2]);

                    rightStreamWriter!.WriteLineAsync(timePosition.GetFileSting(rightSample)).GetAwaiter().GetResult();
                }
            }
            catch { }
        }
        return result;
    }
    #endregion
    
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
