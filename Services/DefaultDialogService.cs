using AudioRecorder.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Аудио Файлы (.wav;.mp3)|*.wav;*.mp3"
        };
        if (openFileDialog.ShowDialog() != DialogResult.OK) return false;
        FilePath = openFileDialog.FileName;
        return true;
    }
}
