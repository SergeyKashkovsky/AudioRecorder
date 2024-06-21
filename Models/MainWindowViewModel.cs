using AudioRecorder.Interfaces;
using AudioRecorder.Services;
using RealTimeGraphX;
using RealTimeGraphX.DataPoints;
using RealTimeGraphX.Renderers;
using RealTimeGraphX.WPF;
using System.ComponentModel;
using System.Diagnostics;
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
        Controller.Range.MinimumY = 0;
        Controller.Range.MaximumY = 1080;
        Controller.Range.MaximumX = TimeSpan.FromSeconds(10);
        Controller.Range.AutoY = true;
        Controller.Range.AutoYFallbackMode = GraphRangeAutoYFallBackMode.MinMax;


        Controller.DataSeriesCollection.Add(new WpfGraphDataSeries()
        {
            Name = "Series",
            Stroke = Colors.DodgerBlue,
        });

        Application.Current.MainWindow.ContentRendered += (_, __) => Start();
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

    private void Start()
    {
        Task.Factory.StartNew(() =>
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            while (true)
            {
                var y = System.Windows.Forms.Cursor.Position.Y;

                List<DoubleDataPoint> yy = new List<DoubleDataPoint>()
                    {
                        y,
                        y + 20,
                        y + 40,
                        y + 60,
                        y + 80,
                    };

                var x = watch.Elapsed;

                List<TimeSpanDataPoint> xx = new List<TimeSpanDataPoint>()
                    {
                        x,
                        x,
                        x,
                        x,
                        x
                    };
                Controller.Range.MaximumX = x;
                Controller.PushData(x, y);
                Thread.Sleep(30);
            }
        }, TaskCreationOptions.LongRunning);
    }
}
