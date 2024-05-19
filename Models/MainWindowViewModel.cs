using AudioRecorder.Interfaces;
using AudioRecorder.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows;

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

    public MainWindowViewModel()
    {
        StatusText = "Ожидание запуска. Частота дискретизации: 16кГц, 16 бит, моно";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Уведомляем об изменении свойства
    /// </summary>
    /// <param name="prop"></param>
    public void OnPropertyChanged([CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
