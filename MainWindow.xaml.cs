using AudioRecorder.Interfaces;
using AudioRecorder.Models;
using AudioRecorder.Services;
using NAudio.Wave;
using RealTimeGraphX.DataPoints;
using RealTimeGraphX.WPF;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.DataFormats;

namespace AudioRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? _fileName; //TODO: перенести в модель представления
        private WaveIn? _waveIn; // TODO: перенести в AudioService
        private StreamWriter? _streamWriter; // TODO: перенести в AudioService
        private readonly MainWindowViewModel _viewModel;
        private WaveFileWriter _writer;
        private int cnt = 0;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
            var Controller = new WpfGraphController<TimeSpanDataPoint, DoubleDataPoint>();
        }

        /// <summary>
        /// Запуск записи в файл
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            
            var dialogService = new DefaultDialogService();
            _fileName = dialogService.SaveFileDialog();
            if (string.IsNullOrEmpty(_fileName))
            {
                _viewModel.StatusText = "Не выбран файл, куда писать";
                return;
            }
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
                //Дефолтное устройство для записи (если оно имеется)
                //встроенный микрофон ноутбука имеет номер 0
                _waveIn.DeviceNumber = 0;
                //Прикрепляем к событию DataAvailable обработчик, возникающий при наличии записываемых данных
                _waveIn.DataAvailable += WaveInDataAvailable!;
                //Прикрепляем обработчик завершения записи
                _waveIn.RecordingStopped += WaveInRecordingStopped!;
                _streamWriter = new StreamWriter(_fileName);
                _waveIn.StartRecording();
                cnt = 0;

                
            }catch (Exception ex)
            {
                StopButton.IsEnabled = false;
                _viewModel.StatusText = String.Format("Ошибка: {0}, выполнение программы не возможно.", ex.Message);
            }
            
        }

        /// <summary>
        /// Остановка записи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopButtonClick(object sender, RoutedEventArgs e)
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
                    _viewModel.SignalLevel = sample;
                    _streamWriter!.WriteLineAsync(sample.ToString()).GetAwaiter().GetResult();
                }
                _writer!.WriteAsync(e.Buffer, 0, e.BytesRecorded);
            }
            catch(Exception ex)
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

        }

        /// <summary>
        /// Обработчик события нажатия кнопки открытия файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButtonClick(object sender, RoutedEventArgs e)
        {
            needForRightChannelCheckBox.IsEnabled = false;
            try
            {
                var dialogService = new DefaultDialogService();
                if (!dialogService.OpenFileDialog()) return;
                
                using var stream = new MediaFoundationReader(dialogService.FilePath);
                var ft = stream.WaveFormat.ToString();
                if (stream.WaveFormat.Channels > 1) needForRightChannelCheckBox.IsEnabled = true;
                _fileName = dialogService.FilePath;
                _viewModel.StatusText = String.Format("Выбран файл {0}, параметры: {1}", _fileName, ft);
                _viewModel.IsFileOpened = true;
            }
            catch (Exception ex)
            {
                _viewModel.StatusText = String.Format("Ошибка при попытке открытия файла: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Обработка нажатия кнопки "Записать"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordFileButtonClick(object sender, RoutedEventArgs e)
        {
            var dialogService = new DefaultDialogService();
            var fileName = dialogService.SaveFileDialog();
            if (string.IsNullOrEmpty(_fileName)) 
            {
                _viewModel.StatusText = "Файл не выбран, как у вас получилось нажать эту кнопку?";
                return;
            }
            _viewModel.StatusText = String.Format("Начата обработка файла {0}", _fileName);
            try
            {
                var files = AudioService.ProcessFile(_fileName, needForRightChannelCheckBox.IsChecked == true, fileName);
                _viewModel.StatusText = String.Format("Закончена обработка файла {0}. Результат: {1}", _fileName, String.Join(", ", files));
            }
            catch(Exception ex)
            {
                _viewModel.StatusText = String.Format("Ошибка при обработке файла {0}: {1}", _fileName, ex.Message);
            }
            
        }
    }
}