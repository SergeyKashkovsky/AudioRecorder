using AudioRecorder.Models;
using AudioRecorder.Services;
using System.Windows;

namespace AudioRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
        /// Запуск записи в файл из микрофона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            
            var dialogService = new DefaultDialogService();
            var fileName = dialogService.SaveFileDialog();
            _audioService.BeginMicrophoneRecord(fileName);
        }

        /// <summary>
        /// Остановка записи микрофона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            _audioService.EndMicrophoneRecord();
        }

        /// <summary>
        /// Обработчик события нажатия кнопки открытия аудио файла
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
            var dialogService = new DefaultDialogService();
            if (!dialogService.OpenFileDialog(false)) return;
            _audioService.OpenSampleFile(dialogService.FilePath);
        }
    }
}