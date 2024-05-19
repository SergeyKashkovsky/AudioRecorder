using AudioRecorder.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioRecorder.Services;

public class DefaultDialogService: IDialogService
{
    /// <inheritdoc/>
    public string FilePath { get; set; } = string.Empty;

    /// <inheritdoc/>
    public bool OpenFileDialog()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Аудио Файлы (.wav;.mp3)|*.wav;*.mp3"
        };
        if (openFileDialog.ShowDialog() != DialogResult.OK) return false;
        FilePath = openFileDialog.FileName;
        return true;
    }

    public string SaveFileDialog()
    {
        var info = new FileInfo(Assembly.GetExecutingAssembly().Location);
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Файл амплитуд (.txt)|*.txt",
            InitialDirectory = info.DirectoryName
        };
        if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
            return string.Empty;
        // получаем выбранный файл
        return saveFileDialog.FileName;
    }
}
