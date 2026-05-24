# План развития VidDownload

> Текущая версия: 0.6.5 → цель: 1.0.0
> Статус: 🔴 не начато | 🟡 в работе | 🟢 готово | ⚪ неприменимо

---

## Этап 1: Технический долг и стабильность кода

- [x] 1.1 — **Исправить `.gitignore`**: первая строка `` ```gitignore `` — невалидный синтаксис, убрать
- [x] 1.2 — **Обновить версию**: в `.csproj` `0.6.5`, в хедере кода `0.7.0` — привести к единой
- [x] 1.3 — **Удалить исключённую папку `Update\`**: либо удалить мусор, либо реализовать; убрать `Compile Remove` из `.csproj`
- [x] 1.4 — **Заменить `async void` на `async Task`**: `Download()`, `CheckUpdateAsync()` — сейчас исключения теряются
- [x] 1.5 — **Исправить смешивание sync/async**: `CheckForInternetConnection().Result` — блокирующий вызов; лишние `Task.Run` вокруг UI-работы
- [x] 1.6 — **Вычистить неиспользуемые `using`**: `System.Collections.Generic`, `System.Linq` и др. в нескольких файлах
- [x] 1.7 — **Убрать лишние `Dispatcher.Invoke`**: в `Download()` вызовы `Dispatcher.Invoke` внутри `Task.Run` нарушают потоковую модель WPF
- [x] 1.8 — **Включить `ImplicitUsings`**: `<ImplicitUsings>enable</ImplicitUsings>` в `.csproj`, убрать явные `using`
- [x] 1.9 — **Заменить `WebClient` (deprecated) на `HttpClient`**: `WebClient` устарел в .NET 6
- [x] 1.10 — **Добавить обработку ошибок**: нет try-catch в `Download()`, нет обработки ошибок запуска yt-dlp
- [x] 1.11 — **Исправить `ParseLog`**: замена `.` на `,` — хак под русскую локаль. Использовать `CultureInfo.InvariantCulture`

### Критичные баги (в рамках этапа 1)

- [x] 1.12 — **Утечка ресурсов**: `FileStream` и `StreamWriter` не обёрнуты в `using` — при исключении не освободятся
- [x] 1.13 — **Потенциальный deadlock**: `CheckForInternetConnection().Result` в контексте синхронизации WPF

---

## Этап 2: Архитектура и рефакторинг

- [ ] **2.1 — Внедрить MVVM: выделить `MainViewModel` и `ConvertViewModel`**
  - [x] 2.1.1 — Добавить NuGet `CommunityToolkit.Mvvm`, создать `ViewModels/` и `ViewModels/Base/`
  - [x] 2.1.2 — Создать `MainViewModel` со свойствами: `Url`, `SelectedResolution`, `SelectedCodec`, `SelectedAudioFormat`, `SelectedFormat`, `IsPlaylist`, `IsAudioOnly`, `IsReEncode`, `StatusMessage`, `ProgressPercent`, `IsDownloading`, `IsVideoOptionsVisible`, `IsAudioOptionsVisible`, `LinkLabelText` + `ObservableCollection` для списков выбора
  - [x] 2.1.3 — Создать `MainViewModel` команды: `DownloadCommand` (`AsyncRelayCommand`), `OpenFolderCommand`, `OpenConverterCommand`, `OpenHelpCommand`
  - [x] 2.1.4 — Создать `ConvertViewModel` со свойствами: `FilePath`, `SelectedFormat`, `StatusMessage`, `ProgressPercent`, `IsConverting` + команды: `ConvertCommand`, `BrowseFileCommand`
  - [x] 2.1.5 — Заменить `x:Name` в `MainWindow.xaml` на `{Binding ...}`: TextBoxURL, ComboBox'ы, CheckBox'ы, ProgressBar, Label, Button'ы (Command)
  - [x] 2.1.6 — Заменить `x:Name` в `ConvertWindow.xaml` на `{Binding ...}`: LabelFileName, ComboFormat, status-метки, прогресс, кнопки
  - [x] 2.1.7 — Заменить `CheckAudio_Checked/Unchecked` и `CheckBoxPlaylist_Checked/Unchecked` на привязку `Visibility` через конвертер или computed-свойства в MainViewModel
  - [x] 2.1.8 — Перенести `InitApp()` в MainViewModel (заполнение `ObservableCollection`), инициализацию папок — в `App.xaml.cs` или бутстраппер
  - [x] 2.1.9 — Очистить code-behind: `MainWindow.xaml.cs` и `ConvertWindow.xaml.cs` должны содержать только конструктор с `DataContext = new ...ViewModel(...)`
- [x] 2.2 — **Выделить сервис yt-dlp: создать `YtDlpService`**
  - [x] 2.2.1 — Создать интерфейс `IYtDlpService` в новой папке `Services/` с методами: `Task DownloadAsync(...)` для загрузки контента и `Task<string> GetLocalVersionAsync()` для получения текущей версии `yt-dlp.exe`.
  - [x] 2.2.2 — Создать модель `DownloadProgress`, передающую процент прогресса и текущее состояние.
  - [x] 2.2.3 — Перенести всю логику конфигурации и запуска процесса `yt-dlp.exe`, обработки stdout и записи логов в `YtDlpService.cs`.
  - [x] 2.2.4 — Внедрить `IYtDlpService` в `MainViewModel` через конструктор и переписать метод `DownloadAsync()` на вызовы сервиса.
- [x] 2.3 — **Выделить сервис обновлений**: GitHub API (Octokit) — в `UpdateService`
  - [x] 2.3.1 — Создать интерфейс `IUpdateService` в `Services/` с методами: `Task<UpdateInfo> CheckForUpdateAsync()`, `Task DownloadUpdateAsync(UpdateInfo info, IProgress<DownloadProgress> progress)`, `Task<string> GetCurrentVersionAsync()`
  - [x] 2.3.2 — Создать модель `UpdateInfo` с полями: `Version`, `DownloadUrl`, `ReleaseNotes`, `IsPreRelease`
  - [x] 2.3.3 — Перенести всю логику работы с Octokit (вызов GitHub Releases API, проверка версий, парсинг тегов) в `UpdateService.cs`
  - [x] 2.3.4 — Внедрить `IUpdateService` в `MainViewModel` через конструктор и заменить прямые вызовы Octokit на вызовы сервиса
- [x] 2.4 — **Добавить DI-контейнер**: `Microsoft.Extensions.DependencyInjection` для внедрения зависимостей
  - [x] 2.4.1 — Добавить NuGet-пакет `Microsoft.Extensions.DependencyInjection`
  - [x] 2.4.2 — Создать статический класс `AppServices` (или настроить в `App.xaml.cs`) с `IServiceProvider`, зарегистрировать все сервисы (`IYtDlpService`, `IUpdateService`) и ViewModels (`MainViewModel`, `ConvertViewModel`)
  - [x] 2.4.3 — Переделать `MainWindow` и `ConvertWindow` для разрешения через DI: получать `DataContext` из контейнера
  - [x] 2.4.4 — В конструкторах ViewModel убрать fallback `?? new ...()`, оставить только обязательное внедрение через DI
  - [x] 2.4.5 — Заменить ручное создание окон (`new ConvertWindow()`, `new HelpWindow()`) на фабрику из контейнера или через `IServiceProvider`
- [x] 2.5 — **Привязать прогресс через Binding**: вместо ручного `Dispatcher.Invoke` — `IProgress<double>` и binding
  - [x] 2.5.1 — Инвентаризация: найти все вызовы `Dispatcher.Invoke` в проекте (ConvertViewModel, MainViewModel и т.д.)
  - [x] 2.5.2 — Переделать `ConvertViewModel`: передавать в `FFmpegAction` не лямбды с `Dispatcher.Invoke`, а `IProgress<DownloadProgress>`
  - [x] 2.5.3 — Переделать `FFmpegAction.ConvertVideoAsync()`: принимать `IProgress<DownloadProgress>` вместо делегатов `Action<string, int>`
  - [x] 2.5.4 — Прогресс-бар в `ConvertWindow.xaml` привязать через `{Binding ProgressPercent}` (если ещё нет) и удалить ручное обновление через `Dispatcher.Invoke`
  - [x] 2.5.5 — Проверить отсутствие `Dispatcher.Invoke` в MainViewModel и ConvertViewModel — если остались, удалить
- [x] 2.6 — **Настройки сохранять между сессиями**: JSON-файл или `Properties.Settings` для выбранных форматов/кодеков
  - [x] 2.6.1 — Создать модель `UserSettings` с полями для сохранения: `Resolution`, `VideoCodec`, `AudioCodec`, `Format`
  - [x] 2.6.2 — Создать интерфейс `ISettingsService` с методами `Task<UserSettings> LoadAsync()`, `Task SaveAsync(UserSettings settings)`
  - [x] 2.6.3 — Реализовать `JsonSettingsService`: сериализация/десериализация `UserSettings` в JSON-файл (`%APPDATA%/VidDownload/settings.json` или рядом с .exe)
  - [x] 2.6.4 — Зарегистрировать `ISettingsService` в DI-контейнере (AppServices)
  - [x] 2.6.5 — В `MainViewModel`: загружать настройки при старте (`LoadAsync`), применять к выбранным полям; сохранять после каждой загрузки (`SaveAsync` с текущими значениями)
- [x] 2.7 — **Внедрить IMessageService / IDialogService** для абстракции всплывающих сообщений и диалоговых окон (избавление ViewModels от зависимости от HandyControl)
  - [x] 2.7.1 — Создать интерфейс `IMessageService` с методами: `Info(string)`, `Warning(string)`, `Error(string)`
  - [x] 2.7.2 — Создать интерфейс `IDialogService` с методами: `Task<bool> AskAsync(string, string)`, `Task<bool> ConfirmAsync(string, string)` и т.п.
  - [x] 2.7.3 — Реализовать `HandyControlMessageService` и `HandyControlDialogService`, оборачивающие `HandyControl.Controls.MessageBox.*`
  - [x] 2.7.4 — Зарегистрировать `IMessageService` и `IDialogService` в DI-контейнере
  - [x] 2.7.5 — Внедрить `IMessageService` и `IDialogService` во все ViewModel и заменить все прямые вызовы `HandyControl.Controls.MessageBox.*` на методы сервисов
  - [x] 2.7.6 — Удалить неиспользуемые `using HandyControl;` из ViewModel-файлов
- [x] 2.9 — **История загрузок**: сохранять список последних ссылок с возможностью повторного скачивания (вынести из формата настроек в отдельную историю)
  - [x] 2.9.1 — Создать модель `DownloadHistoryEntry` (Id, Url, Title, FilePath, Timestamp, Status)
  - [x] 2.9.2 — Создать интерфейс `IDownloadHistoryService` с методами: `AddEntryAsync`, `GetRecentEntriesAsync(int count)`, `ClearHistoryAsync`, `RemoveEntryAsync(Guid id)`
  - [x] 2.9.3 — Реализовать `JsonDownloadHistoryService` (хранение в отдельном JSON-файле `%APPDATA%/download-history.json`)
  - [x] 2.9.4 — Зарегистрировать `IDownloadHistoryService` в DI-контейнере
  - [x] 2.9.5 — Создать View/ViewModel для отображения истории загрузок (список с датой, названием, статусом)
  - [x] 2.9.6 — Реализовать повторное скачивание: кнопка "Download again" в списке истории
  - [x] 2.9.7 — Перенести старые «последние ссылки» из настроек в новую историю (миграция) — не требуется, старые ссылки в настройках отсутствуют
- [x] 2.8 — **Использовать ArgumentList вместо конкатенации строк** при запуске `yt-dlp` через ProcessStartInfo для безопасности путей и параметров
  - [x] 2.8.1 — Изменить сигнатуру `Command.LoadAudio()`: возвращать `List<string>` вместо `string`, каждый аргумент отдельным элементом
  - [x] 2.8.2 — Изменить сигнатуру `Command.LoadVideo()`: возвращать `List<string>` вместо `string`
  - [x] 2.8.3 — Обновить `YtDlpService.DownloadAsync()`: использовать `proc.StartInfo.ArgumentList` вместо `proc.StartInfo.Arguments`, убрать `StartsWith("yt-dlp ")` stripping
  - [x] 2.8.4 — Удалить неиспользуемый `using System.Text;` из `YtDlpService.cs` — не требуется, `Encoding.Default` используется в `StreamWriter`

---

## Этап 3: Пользовательский функционал

- [x] 3.1 — **Индикация оставшегося времени / скорости**: парсить дополнительную информацию из stdout yt-dlp
  - [x] 3.1.1 — Проанализировать формат строки прогресса yt-dlp в stdout (пример: `[download]  45.6% of ~326.89MiB at 12.5MiB/s ETA 00:14`)
  - [x] 3.1.2 — Расширить модель `DownloadProgress` полями: `Speed` (string), `Eta` (string), `TotalSize` (string)
  - [x] 3.1.3 — Дополнить парсер в `ParseLog.cs`: извлекать скорость, ETA, размер из stdout с помощью Regex
  - [x] 3.1.4 — Передавать новые поля через `IProgress<DownloadProgress>.Report()` при каждом обновлении прогресса
  - [x] 3.1.5 — В `MainViewModel`: добавить свойства `SpeedText`, `EtaText`, `TotalSizeText` с привязкой к UI
  - [x] 3.1.6 — Обновить `MainWindow.xaml`: добавить Label/TextBlock для отображения скорости и ETA рядом с ProgressBar
  - [x] 3.1.7 — Обработать краевые случаи: отсутствие информации о скорости/ETA — показывать прочерк или "--"
- [ ] 3.2 — **Отмена загрузки**: кнопка «Отмена» при активной загрузке (kill процесса yt-dlp)
- [ ] 3.3 — **История загрузок**: список последних ссылок с повторным скачиванием
- [ ] 3.4 — **Выбор папки сохранения**: `FolderBrowserDialog` вместо фиксированной `MyVideos/`
- [ ] 3.5 — **Включить FFmpeg auto-download**: раскомментировать `FFmpegDownloader.GetLatestVersion()`
- [ ] 3.6 — **NVENC-переключатель в UI**: добавить checkbox в ConvertWindow (сейчас `useNVENC = false` жёстко)
- [ ] 3.7 — **Drag-and-drop ссылок**: перетаскивание URL в TextBox
- [ ] 3.8 — **Скачивание субтитров**: опция `--write-subs --write-auto-subs` в yt-dlp
- [ ] 3.9 — **Выбор конкретных потоков**: ручной ввод формата (не только через `-S`)
- [ ] 3.10 — **Обновление ffmpeg через GUI**: проверка/обновление FFmpeg аналогично yt-dlp
- [ ] 3.11 — **Безопасные пути сохранения по умолчанию**: использовать стандартную системную папку «Видео» пользователя (`Environment.SpecialFolder.MyVideos`) вместо `./MyVideos/` для избежания `UnauthorizedAccessException`
- [ ] 3.12 — **Кнопка «Вставить из буфера» (Paste from Clipboard)** с автоматической валидацией URL-адреса
- [ ] 3.13 — **Поддержка тем оформления (Темная / Светлая)** с сохранением предпочтения в настройках

---

## Этап 4: Перевод на современный .NET

- [ ] 4.1 — **Миграция на .NET 10**: `net10.0-windows` вместо `net6.0-windows10.0.22621.0`
- [ ] 4.2 — **Обновить пакеты NuGet**: HandyControl, Octokit, Xabe.FFmpeg до последних версий
- [ ] 4.3 — **Понизить целевой SDK**: `Windows10.0.19041.0` вместо `22621.0` (для совместимости с Windows 10)

---

## Этап 5: Автоматическое тестирование и CI/CD

- [ ] 5.1 — **Создать тестовый проект на xUnit или NUnit** для покрытия бизнес-логики
- [ ] 5.2 — **Покрыть тестами парсер логов (ParseLog)** для валидации различных форматов вывода yt-dlp
- [ ] 5.3 — **Реализовать модульные тесты для YtDlpService и UpdateService** с использованием Mock-объектов
- [ ] 5.4 — **Настроить пайплайн GitHub Actions** для автоматической сборки проекта и запуска тестов при Push/PR

---

## Этап 6: Упаковка и распространение

- [ ] 6.1 — **Подписать сборку**: настроить SignAssembly или убрать `DelaySign=True` с пустым ключом
- [ ] 6.2 — **Создать MSI-инсталлятор**: WiX Toolset для удобной установки
- [ ] 6.3 — **Single-file publish**: `dotnet publish --self-contained -p:PublishSingleFile=true`
- [ ] 6.4 — **Автообновление приложения**: механизм обновления самого VidDownload (не только yt-dlp)

---

## Этап 7: Локализация и доступность

- [ ] 7.1 — **Перевод интерфейса на английский**: ресурсные файлы `.resx` для EN
- [ ] 7.2 — **Переключение языка в рантайме**: через `CultureInfo` или `ResourceDictionary`
