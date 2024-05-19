using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRecorder.Interfaces;
/// <summary>
/// Функционал для работы с диалоговыми окнами 
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Путь к выбранному пользователем файлу
    /// </summary>
    string FilePath { get; set; }
    /// <summary>
    /// Вызов окна открытия файла
    /// </summary>
    bool OpenFileDialog();
}
