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
        private string _fileName;
        private WaveIn _waveIn;
        private int samples;
        private StreamWriter _streamWriter;

        public MainWindow()
        {
            InitializeComponent();
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
            StatusTextBlock.Text = String.Format("Начата запись в файл: {0}", _fileName);
            StatusTextBlock.ToolTip = StatusTextBlock.Text;
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
                samples = 0;
                _streamWriter = new StreamWriter(_fileName);
                _waveIn.StartRecording();

                
            }catch (Exception ex)
            {
                StopButton.IsEnabled = false;
                StatusTextBlock.Text = String.Format("Ошибка: {0}, выполнение программы не возможно.", ex.Message);
                StatusTextBlock.ToolTip = StatusTextBlock.Text;
            }
            
        }

        /// <summary>
        /// Остановка записи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            _waveIn.StopRecording();
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            StatusTextBlock.Text = String.Format("Запись в файл: {0} остановлена. Ожидание запуска", _fileName);
            StatusTextBlock.ToolTip = StatusTextBlock.Text;
        }

        private void WaveInDataAvailable(object sender, WaveInEventArgs e)
        {
            
            for (int i = 0; i < e.BytesRecorded; i += 2) //loop for bytes
            {
                samples++;
                short sample = (short)((e.Buffer[i + 1] << 8) |
                                e.Buffer[i + 0]);
                var level = sample + 32768;
                LevelProgressBar.Value = level;
                _streamWriter.WriteLineAsync(sample.ToString()).GetAwaiter().GetResult();
            }
            
        }

        private void WaveInRecordingStopped(object sender, EventArgs e)
        {
            _waveIn.Dispose();
            _streamWriter.Dispose();
            _waveIn = null;

        }
    }
}