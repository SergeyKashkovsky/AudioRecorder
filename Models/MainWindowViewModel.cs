using NAudio.Wave;
using RealTimeGraphX.DataPoints;
using RealTimeGraphX.WPF;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace AudioRecorder.Models;

/// <summary>
/// Модель представления главного окна
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly static string _firstTabDefaultStatus = $"Ожидание запуска. Параметры записи: {new WaveFormat(16000, 16, 1)}";
    private const string _secondTabDefaultStatus = "Откройте файл в формате wav или mp3";
    private const string _thirdTabDefaultStatus = "Откройте файл амплитуд в формате .txt";

    /// <summary>
    /// Коллекция аудио-устройств
    /// </summary>
    public List<AudioDevice> AudioDevices { get; set; } = new List<AudioDevice>();

    /// <summary>
    /// Выбранное аудио-устройство
    /// </summary>
    public AudioDevice AudioDevice { get; set; } = new AudioDevice();

    #region Свойства и поля для привязки

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
    /// <summary>
    /// Открыт ли файл
    /// </summary>
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

    private bool _needForRightChannel = false;
    /// <summary>
    /// Есть ли возможность писать правый канал аудиофайла в файл амплитуд
    /// </summary>
    public bool NeedForRightChannel
    {
        get => _needForRightChannel;
        set
        {
            _needForRightChannel = value;
            OnPropertyChanged("NeedForRightChannel");
        }
    }

    private bool _writeRightChannell = false;
    /// <summary>
    /// Нужно ли писать в файл правый аудиоканал при чтении аудио-файла
    /// </summary>
    public bool WriteRightChannel
    {
        get => _writeRightChannell;
        set
        {
            _writeRightChannell = value;
            OnPropertyChanged("NeedForRightChannel");
        }
    }

    #endregion

    /// <summary>
    /// Конструктор модели представления
    /// </summary>
    public MainWindowViewModel()
    {
        StatusText = _firstTabDefaultStatus;

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
