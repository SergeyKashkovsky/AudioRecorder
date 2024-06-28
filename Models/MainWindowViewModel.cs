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
    private readonly IDialogService _dialogService = new DefaultDialogService();

    /// <summary>
    /// Коллекция аудио-устройств
    /// </summary>
    public List<AudioDevice> AudioDevices { get; set; }
    
    /// <summary>
    /// Выбранное аудио-устройство
    /// </summary>
    public AudioDevice AudioDevice { get; set; }

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
            StatusText = _tabSelectedIndex <=0 ? _firstTabDefaultStatus : _secondTabDefaultStatus;
            IsFileOpened = false;
            OnPropertyChanged("TabSelectedIndex");
            OnPropertyChanged("MicrophoneUICollapsed");
            OnPropertyChanged("FileUICollapsed");
        }
    }

    /// <summary>
    /// Отображаем или скрываем запись потока с микрофона
    /// </summary>
    public Visibility MicrophoneUICollapsed => TabSelectedIndex > 0 ? Visibility.Collapsed : Visibility.Visible;
    /// <summary>
    /// Отображаем или скрываем запись потока из файла
    /// </summary>
    public Visibility FileUICollapsed => TabSelectedIndex != 1 ? Visibility.Collapsed : Visibility.Visible;

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

    private string _sampleFileParams;
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
    /// Амплитуда сигнала: модуль уровня
    /// </summary>
    public int Amplitude => Math.Abs(SignalLevel);

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
        var strArray = File.ReadAllLines(filename);
        var samples = strArray.Select(x => x.ToFileRecordModel());
        var maxX = samples.Last().Time;    
        var arrays = samples.Select(x => new
        {
            X = new TimeSpanDataPoint(x.Time),
            Y = new DoubleDataPoint(x.Sample)
        }).OrderBy(x=>x.X).ToArray();
        var xArray = arrays.Select(x => x.X).ToArray();
        var yArray = arrays.Select(x => x.Y).ToArray();
        Controller.Clear();
        Controller.Range.MaximumX = maxX;
        Controller.Range.MinimumY = -33000;
        Controller.Range.MaximumY = 33000;
        Controller.PushData(xArray, yArray);
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
