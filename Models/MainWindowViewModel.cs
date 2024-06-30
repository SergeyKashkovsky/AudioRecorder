using AudioRecorder.Extensions;
using AudioRecorder.Interfaces;
using AudioRecorder.Services;
using NAudio.Wave;
using RealTimeGraphX;
using RealTimeGraphX.DataPoints;
using RealTimeGraphX.Renderers;
using RealTimeGraphX.WPF;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;

namespace AudioRecorder.Models;

/// <summary>
/// Модель представления главного окна
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private const string _firstTabDefaultStatus = "Ожидание запуска. Частота дискретизации: 16кГц, 16 бит, моно";
    private const string _secondTabDefaultStatus = "Откройте файл в формате wav или mp3";
    private const string _thirdTabDefaultStatus = "Откройте файл амплитуд в формате .txt";
    private readonly IDialogService _dialogService = new DefaultDialogService();

    /// <summary>
    /// Коллекция аудио-устройств
    /// </summary>
    public List<AudioDevice> AudioDevices { get; set; } = new List<AudioDevice>();

    /// <summary>
    /// Выбранное аудио-устройство
    /// </summary>
    public AudioDevice AudioDevice { get; set; } = new AudioDevice();

    private string? _statusText;
    /// <summary>
    /// Текст статуса работы приложения
    /// </summary>
    public string StatusText {
        get => _statusText!;
        set
        {
            _statusText = value;
            OnPropertyChanged("StatusText");
        } 
    }

    
    private int _tabSelectedIndex = 0;
    /// <summary>
    /// Индекс открытой табы
    /// </summary>
    public int TabSelectedIndex
    {
        get => _tabSelectedIndex;
        set
        {
            _tabSelectedIndex = value;
            IsFileOpened = false;
            StatusText = _tabSelectedIndex switch
            {
                0 => _firstTabDefaultStatus,
                1 => _secondTabDefaultStatus,
                2 => _thirdTabDefaultStatus,
                _ => string.Empty
            };
            OnPropertyChanged("TabSelectedIndex");
        }
    }

    private bool _isFileOpened;
    public bool IsFileOpened
    {
        get => _isFileOpened;
        set
        {
            _isFileOpened = value;
            OnPropertyChanged("IsFileOpened");
        }
    }

    private bool _isRecording = false;
    /// <summary>
    /// Флаг для элементов управления, которые должны быть активны, если запись микрофона начата
    /// </summary>
    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            _isRecording = value;
            OnPropertyChanged("IsRecording");
            OnPropertyChanged("IsNotRecording");
        }
    }

    /// <summary>
    /// Флаг для элементов управления, которые должны быть активны, если запись микрофона не начата
    /// </summary>
    public bool IsNotRecording => !IsRecording;
    
    private int _signalLevel;
    /// <summary>
    /// Уровень сигнала
    /// </summary>
    public int SignalLevel 
    { 
        get => _signalLevel;
        set
        {
            _signalLevel = value;
            OnPropertyChanged("SignalLevel");
            OnPropertyChanged("Amplitude");
        } 
    }

    private string _sampleFileParams = string.Empty;
    /// <summary>
    /// Параметры файла амплитуд
    /// </summary>
    public string SampleFileParams
    {
        get => _sampleFileParams;
        set
        {
            _sampleFileParams = value;
            OnPropertyChanged("SampleFileParams");
        }
    }

    /// <summary>
    /// Конструктор модели представления
    /// </summary>
    public MainWindowViewModel()
    {
        StatusText = "Ожидание запуска. Частота дискретизации: 16кГц, 16 бит, моно";

        Controller = new WpfGraphController<TimeSpanDataPoint, DoubleDataPoint>();
        Controller.DataSeriesCollection.Add(new WpfGraphDataSeries()
        {
            Name = "Series",
            Stroke = Colors.DodgerBlue,
        });
        GetAudioDevices();
    }
    /// <summary>
    /// Инициализация контроллера для записи микрофона
    /// </summary>
    public void InitControllerForMic()
    {
        Controller.Clear();
        Controller.Range.MinimumY = -33000;
        Controller.Range.MaximumY = 33000;
        Controller.Range.MaximumX = TimeSpan.FromSeconds(2);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Уведомляем об изменении свойства
    /// </summary>
    /// <param name="prop"></param>
    public void OnPropertyChanged([CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));


    //Graph controller with timespan as X axis and double as Y.
    public WpfGraphController<TimeSpanDataPoint, DoubleDataPoint> Controller { get; set; }

    /// <summary>
    /// Вставляем точку
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void PushGraphData(TimeSpan x, double y)
    {
        Controller.PushData(x, y);
    }

    /// <summary>
    /// Рисуем график из файла
    /// </summary>
    /// <param name="filename"></param>
    public void DrawFileGraph(string filename)
    {
        var data = AudioService.ReadFileData(filename);
        DrawFileGraph(data);
    }

    /// <summary>
    /// Рисуем график по массивам точек
    /// </summary>
    /// <param name="xArray"></param>
    /// <param name="yArray"></param>
    /// <param name="maxX">Максимальное время для отображения графика</param>
    public void DrawFileGraph(SampleFileModel fileData)
    {
        Controller.Clear();
        Controller.Range.MaximumX = fileData.MaxX;
        Controller.Range.MinimumY = -33000;
        Controller.Range.MaximumY = 33000;
        Controller.PushData(fileData.XArray, fileData.YArray);
    }

    /// <summary>
    /// Получение списка аудиоустройств
    /// </summary>
    public void GetAudioDevices()
    {
        AudioDevices = new List<AudioDevice>();
        for (int n = -1; n < WaveIn.DeviceCount; n++)
        {
            var caps = WaveIn.GetCapabilities(n);
            AudioDevices.Add(new AudioDevice(caps, n));
        }
        AudioDevice = AudioDevices.FirstOrDefault(x => x.Id == 0)!;
    }
}
