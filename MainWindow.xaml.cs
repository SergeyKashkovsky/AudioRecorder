using AudioRecorder.Models;
using NAudio.Wave;
using System.IO;
using System.Reflection;
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
using static System.Windows.Forms.DataFormats;

namespace AudioRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? _fileName;
        private WaveIn? _waveIn;
        private StreamWriter? _streamWriter;
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
        }

        /// <summary>
        /// Запуск записи в файл
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            var info = new FileInfo(Assembly.GetExecutingAssembly().Location);

            _fileName = String.Format(@"{0}\{1}.txt", info.Directory, DateTime.Now.ToString("yyyyMMddHHmmss"));
            StopButton.IsEnabled = true;
            StartButton.IsEnabled = false;
            _viewModel.StatusText = String.Format("Начата запись в файл: {0}", _fileName);
            try
            {
                _waveIn = new WaveIn
                {
                    BufferMilliseconds = 50,
                    DeviceNumber = 0,
                    WaveFormat = new WaveFormat(16000, 16, 1)
                };
                //Дефолтное устройство для записи (если оно имеется)
                //встроенный микрофон ноутбука имеет номер 0
                _waveIn.DeviceNumber = 0;
                //Прикрепляем к событию DataAvailable обработчик, возникающий при наличии записываемых данных
                _waveIn.DataAvailable += WaveInDataAvailable!;
                //Прикрепляем обработчик завершения записи
                _waveIn.RecordingStopped += WaveInRecordingStopped!;
                _streamWriter = new StreamWriter(_fileName);
                _waveIn.StartRecording();

                
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
            _waveIn?.StopRecording();
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
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
            }catch(Exception ex)
            {
                _viewModel.StatusText = String.Format("Ошибка {0}, останавливаю запись в файл {1}", ex.Message, _fileName);
                _waveIn?.StopRecording();
                StopButton.IsEnabled = false;
                StartButton.IsEnabled = true;
            }
        }
        /// <summary>
        /// Обработка события остановки слушателя событий микрофона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveInRecordingStopped(object sender, EventArgs e)
        {
            _waveIn?.Dispose();
            _streamWriter?.Dispose();
            _waveIn = null;

        }
    }
}