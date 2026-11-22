using System.Threading;
using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public interface IDialogService
    {
        Task<bool> AskAsync(string message, string title);
        Task<bool> ConfirmAsync(string message, string title);

        /// <summary>
        /// Вопрос «Да/Нет», который можно закрыть программно: отмена токена убирает диалог
        /// с экрана и завершает задачу <see cref="System.OperationCanceledException"/>.
        /// </summary>
        Task<bool> AskAsync(string message, string title, CancellationToken cancellationToken);
    }
}
