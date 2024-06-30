using AudioRecorder.Constants;
using AudioRecorder.Extensions;
using AudioRecorder.Models;
using AudioRecorder.Services;
using NAudio.Wave;
using RealTimeGraphX.DataPoints;
using RealTimeGraphX.WPF;
using System.IO;
using System.Windows;

namespace AudioRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? _fileName; //TODO: перенести в модель представления
        private readonly MainWindowViewModel _viewModel;
        private readonly AudioService _audioService;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            _audioService = new AudioService(_viewModel);
            DataContext = _viewModel;
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

            _audioService.BeginMicrophoneRecord(_fileName);
        }

        /// <summary>
        /// Остановка записи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            _audioService.EndMicrophoneRecord();
        }

        /// <summary>
        /// Обработчик события нажатия кнопки открытия файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButtonClick(object sender, RoutedEventArgs e)
        {
            var dialogService = new DefaultDialogService();
            if (!dialogService.OpenFileDialog()) return;
            _audioService.PrepareToReadAudioFile(dialogService.FilePath);
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
            _audioService.ReadAudiofile(fileName);
        }

        /// <summary>
        /// Обработка нажатия на кнопку открытия файла амплитуд
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenSamplesFileButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogService = new DefaultDialogService();
                if (!dialogService.OpenFileDialog(false)) return;
                _viewModel.StatusText = string.Format("Читаю файл {0}", dialogService.FilePath);
                _viewModel.SampleFileParams = string.Format("{0} ({1})", dialogService.FilePath, AudioService.GetSampleFileAudioParams(dialogService.FilePath));
                _viewModel.DrawFileGraph(dialogService.FilePath);
                _viewModel.StatusText = string.Format("Файл {0} прочитан", dialogService.FilePath);

            }
            catch (Exception ex)
            {
                _viewModel.StatusText = String.Format("Ошибка при попытке открытия файла: {0}", ex.Message);
            }
        }
    }
}