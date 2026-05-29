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
- [x] 3.2 — **Отмена загрузки**: кнопка «Отмена» при активной загрузке (kill процесса yt-dlp)
  - [x] 3.2.1 — Добавить свойство `CancellationTokenSource` в `MainViewModel` для сигнализации отмены
  - [x] 3.2.2 — Создать команду `CancelCommand` (`AsyncRelayCommand`) в `MainViewModel`: отменяет токен и убивает процесс yt-dlp
  - [x] 3.2.3 — Дополнить интерфейс `IYtDlpService`: добавить метод `KillAsync()` или свойство `Process` для внешней остановки (уже был `CancellationToken` в `DownloadAsync`)
  - [x] 3.2.4 — В `YtDlpService.DownloadAsync()`: принимать `CancellationToken`, передавать в `WaitForExitAsync()` и стрим-ридеры; при отмене — `proc.Kill()` + throw `OperationCanceledException`
  - [x] 3.2.5 — Добавить кнопку «Отмена» в `MainWindow.xaml` рядом с кнопкой «Скачать»; видимость привязана к `IsDownloading`
  - [x] 3.2.6 — В `MainViewModel`: блокировать кнопку «Скачать» пока активна загрузка (`IsDownloading = true` → CanExecute=false)
  - [x] 3.2.7 — Добавить диалог подтверждения: «Вы уверены, что хотите отменить загрузку?» через `IDialogService.ConfirmAsync()`
  - [x] 3.2.8 — Восстанавливать состояние после отмены: очистить прогресс, снять флаг `IsDownloading`, вывести сообщение «Загрузка отменена»
  - [x] 3.2.9 — Обработать краевые случаи: попытка отмены уже завершённой загрузки; повторный вызов `Cancel()` (проверка `IsCancellationRequested`)
- [ ] 3.3 — **История загрузок**: список последних ссылок с повторным скачиванием
- [ ] 3.4 — **Выбор папки сохранения**: `FolderBrowserDialog` вместо фиксированной `MyVideos/`
- [ ] 3.5 — **Включить FFmpeg auto-download**: раскомментировать `FFmpegDownloader.GetLatestVersion()`
- [ ] 3.6 — **NVENC-переключатель в UI**: добавить checkbox в ConvertWindow (сейчас `useNVENC = false` жёстко)
- [ ] 3.7 — **Drag-and-drop ссылок**: перетаскивание URL в TextBox
- [x] 3.8 — **Скачивание субтитров**: опция `--write-subs --write-auto-subs` в yt-dlp
  - [x] 3.8.1 — Добавить свойство `IsDownloadSubtitles` (bool) в `MainViewModel`
  - [x] 3.8.2 — Добавить `SubtitleLanguage` (string) в `MainViewModel` для выбора языка субтитров (по умолчанию `"all"` или пусто)
  - [x] 3.8.3 — Добавить `IsEmbedSubtitles` (bool) в `MainViewModel`: флаг `--embed-subs` для встраивания субтитров в файл
  - [x] 3.8.4 — Добавить `ObservableCollection<string> SubtitleLanguages` в `MainViewModel` со списком: `""`, `"all"`, `"en"`, `"ru"`, `"de"`, `"fr"`, `"es"`, `"ja"`, `"zh-Hans"`, `"ar"`, `"pt"`
  - [x] 3.8.5 — Добавить чекбокс «Скачать субтитры» в `MainWindow.xaml` (группа опций справа)
  - [x] 3.8.6 — Добавить ComboBox выбора языка субтитров рядом с чекбоксом (видимость привязана к `IsDownloadSubtitles`)
  - [x] 3.8.7 — Добавить чекбокс «Встроить субтитры» (`--embed-subs`) под выбором языка (видимость привязана к `IsDownloadSubtitles`)
  - [x] 3.8.8 — Дополнить `Command.LoadAudio()` и `Command.LoadVideo()`: добавить `bool downloadSubtitles, string subLang, bool embedSubs` параметры; генерировать `--write-subs`, `--write-auto-subs`, `--sub-langs`, `--embed-subs`
  - [x] 3.8.9 — Обновить вызов `Command.LoadAudio()` / `Command.LoadVideo()` в `YtDlpService.DownloadAsync()` с передачей новых флагов (читает из `settings`, сигнатура не изменилась)
  - [x] 3.8.10 — Добавить поля `DownloadSubtitles`, `SubtitleLanguage`, `EmbedSubtitles` в модель `Settings` для проброса параметров через сервис
  - [x] 3.8.11 — Добавить поля `DownloadSubtitles`, `SubtitleLanguage`, `EmbedSubtitles` в `UserSettings` для сохранения между сессиями
  - [x] 3.8.12 — Обновить `MainViewModel.DownloadAsync()`: заполнять новые поля `_settings` перед вызовом
  - [x] 3.8.13 — Проверить совместимость `--embed-subs` с `--remux-video` / `--recode-video`; если конфликт — дать предупреждение
- [ ] 3.9 — **Выбор конкретных потоков**: ручной ввод формата (не только через `-S`)
- [x] 3.10 — **Обновление ffmpeg через GUI**: проверка/обновление FFmpeg аналогично yt-dlp
  - [x] 3.10.1 — Создать интерфейс `IFFmpegService` в `Services/` с методами: `GetLocalVersionAsync()`, `CheckForUpdateAsync()`, `DownloadUpdateAsync(IProgress<DownloadProgress>)`, `GetFFmpegPathAsync()`
  - [x] 3.10.2 — Создать модель `FFmpegInfo` с полями: `LocalVersion`, `LatestVersion`, `DownloadUrl`, `IsUpdateAvailable`
  - [x] 3.10.3 — Реализовать `FFmpegService.GetLocalVersionAsync()`: получать версию через `FileVersionInfo.GetVersionInfo("ffmpeg.exe")` или вывод `ffmpeg -version`
  - [x] 3.10.4 — Реализовать `FFmpegService.CheckForUpdateAsync()`: проверять релизы FFmpeg через GitHub API (Octokit, `BtbN/FFmpeg-Builds`); сравнивать сохранённый тег с последним релизом
  - [x] 3.10.5 — Реализовать `FFmpegService.DownloadUpdateAsync()`: скачивать архив с актуальной сборкой FFmpeg, распаковывать в папку приложения, заменять `ffmpeg.exe`
  - [x] 3.10.6 — Добавить свойства в `MainViewModel`: `FfmpegVersion` (string), `IsFfmpegChecking` (bool), `IsFfmpegUpdateAvailable` (bool), `FfmpegStatusMessage` (string)
  - [x] 3.10.7 — Добавить команду `CheckFFmpegUpdateCommand` (AsyncRelayCommand) в `MainViewModel`: проверяет обновление, при наличии — предлагает скачать через `IDialogService`, скачивает с прогрессом
  - [x] 3.10.8 — Добавить UI-элементы в `MainWindow.xaml`: отображение версии FFmpeg и кнопка «Проверить обновление FFmpeg» рядом с блоком yt-dlp (аналогично)
  - [x] 3.10.9 — Зарегистрировать `IFFmpegService` в DI-контейнере (`AppServices.cs`)
  - [x] 3.10.10 — Проверить совместимость: после замены `ffmpeg.exe` Xabe.FFmpeg должен корректно подхватывать новую версию (бинарный в той же папке)
  - [x] 3.10.11 — Обработать краевые случаи: отсутствие `ffmpeg.exe` (первичная загрузка вместо обновления), ошибки сети, отмена загрузки, повреждённый архив
- [x] **3.11 — Безопасные пути сохранения по умолчанию**: использовать стандартную системную папку «Видео» пользователя (`Environment.SpecialFolder.MyVideos`) вместо `./MyVideos/` для избежания `UnauthorizedAccessException`
  - [x] 3.11.1 — Инвентаризация: найти все места в коде, где используется `./MyVideos/`, `MyVideos`, `Environment.GetFolderPath(...)` и переменная пути сохранения
  - [x] 3.11.2 — Создать / обновить конфигурацию пути по умолчанию в `UserSettings` или отдельной константе: `DefaultDownloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)`
  - [x] 3.11.3 — Заменить жёстко заданный `./MyVideos/` в `Command.cs` (и где ещё) на путь из настроек/константы
  - [x] 3.11.4 — Заменить `./MyVideos/` в `YtDlpService.cs` (если используется напрямую) на вычисляемый путь
  - [x] 3.11.5 — Заменить `./MyVideos/` в `MainViewModel.cs` (инициализация, `OpenFolderCommand`, `DownloadAsync`) на путь по умолчанию
  - [x] 3.11.6 — В `MainViewModel.LoadSettingsAsync()`: если `UserSettings.SavePath` пуст — устанавливать `DefaultDownloadPath`; иначе — использовать сохранённый путь
  - [x] 3.11.7 — В `MainViewModel.DownloadAsync()` / `YtDlpService.DownloadAsync()`: перед созданием файла проверить, что папка существует (`Directory.CreateDirectory`)
  - [x] 3.11.8 — Обработать краевые случаи: папка `MyVideos` не существует или нет доступа — показать предупреждение и предложить выбрать другую папку
  - [x] 3.11.9 — Удалить `./MyVideos/` из `.gitignore` (если папка в репозитории) — проверить, не нужна ли она для тестовой структуры
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

- [x] 6.1 — **Подписать сборку**: настроить SignAssembly или убрать `DelaySign=True` с пустым ключом
  - [x] 6.1.1 — Определиться с подходом: подпись не нужна (`SignAssembly=False`, ключ не существует) → удаляем всё
  - [x] 6.1.2 — ~~Если нужна подпись…~~ не требуется
  - [x] 6.1.3 — ~~Если нужна подпись…~~ не требуется
  - [x] 6.1.4 — Удалены `<DelaySign>True</DelaySign>`, `<SignAssembly>False</SignAssembly>`, `<AssemblyOriginatorKeyFile>` из `.csproj`
  - [x] 6.1.5 — ~~Создание ключа / gitignore~~ не требуется (подпись удалена)
  - [x] 6.1.6 — `dotnet build` (Debug) — **0 errors**, ошибка `DelaySign` / подписи исчезла
  - [x] 6.1.7 — `dotnet build -c Release` и `dotnet publish` — ошибок подписи нет (RG1000 — pre-existing WPF BAML, не связан)
- [ ] 6.2 — **Создать MSI-инсталлятор**: WiX Toolset для удобной установки
  - [ ] 6.2.1 — Установить/проверить WiX Toolset (wix.exe или `dotnet tool install --global wix`)
  - [ ] 6.2.2 — Создать файл `Setup.wxs` с базовой структурой: `Product`, `Package`, `Directory`, `Component`
  - [ ] 6.2.3 — Определить `DirectoryRef`: папка `ProgramFiles64Folder\VidDownload`, подпапки для бинарников
  - [ ] 6.2.4 — Добавить `ComponentGroup` со всеми файлами сборки (`VidDownload.WPF.dll`, `*.exe`, `*.config`, yt-dlp.exe, ffmpeg.exe)
  - [ ] 6.2.5 — Создать `Feature` и `FeatureGroup` для главного компонента (связать с `ComponentGroupRef`)
  - [ ] 6.2.6 — Добавить ассоциацию `.url` / протокол (если нужно) или ярлык в меню «Пуск» — `Shortcut` + `RegistryValue`
  - [ ] 6.2.7 — Настроить `HeatDirectory` или `Harvest` для автоматического включения содержимого папки публикации
  - [ ] 6.2.8 — Настроить `UpgradeCode` и `<MajorUpgrade>` для поддержки обновления через установщик
  - [ ] 6.2.9 — Установить `WixToolset.Bal.wixext` (если нужен UI) или использовать встроенный `WixUI_InstallDir`
  - [ ] 6.2.10 — Добавить переменную `$(var.PublishDir)` через `-d` и привязать к Heat сборке
  - [ ] 6.2.11 — Создать `.wixproj` или Make-скрипт: `wix build Setup.wxs -ext WixToolset.UI.wixext -o VidDownload.msi`
  - [ ] 6.2.12 — Протестировать установку: `msiexec /i VidDownload.msi` — проверить пути, ярлыки, права
  - [ ] 6.2.13 — Протестировать удаление: `msiexec /x VidDownload.msi` — проверить полное удаление файлов
  - [ ] 6.2.14 — Добавить MSI-сборку в GitHub Actions: шаг `wix build` после `dotnet publish`
- [ ] 6.3 — **Single-file publish**: `dotnet publish --self-contained -p:PublishSingleFile=true`
  - [ ] 6.3.1 — Проверить, не используется ли `Assembly.GetEntryAssembly().Location` (возвращает пустую строку для single-file)
  - [ ] 6.3.2 — Заменить `Assembly.GetEntryAssembly().Location` на `AppContext.BaseDirectory` или `Environment.ProcessPath` при необходимости
  - [ ] 6.3.3 — Проверить `App.config` / `appsettings.json`: single-file не поддерживает `ConfigurationManager` — перенести настройки в код
  - [ ] 6.3.4 — Убедиться, что `yt-dlp.exe` и `ffmpeg.exe` не включены в single-file (пометить как `<Content CopyToPublishDirectory="PreserveNewest" />`)
  - [ ] 6.3.5 — Запустить тестовую публикацию: `dotnet publish -c Release --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/single`
  - [ ] 6.3.6 — Проверить размер: single-file >~60 МБ (self-contained) — оценить, не нужен ли `--trim-mode=partial`
  - [ ] 6.3.7 — Проверить запуск: `./publish/single/VidDownload.WPF.exe` — загрузка, yt-dlp, ffmpeg, конвертер
  - [ ] 6.3.8 — Решить, нужен ли `PublishReadyToRun=true` для ускорения старта (больше размер, быстрее JIT)
  - [ ] 6.3.9 — Если приложение подписывается (6.1), проверить совместимость single-file с подписью
- [ ] 6.4 — **Автообновление приложения**: механизм обновления самого VidDownload (не только yt-dlp)
  - [ ] 6.4.1 — Выбрать стратегию обновления: замена .exe / MSI upgrade / загрузчик-аттач
  - [ ] 6.4.2 — Расширить `IUpdateService`: метод `CheckAppUpdateAsync()` для проверки новой версии VidDownload по GitHub Releases
  - [ ] 6.4.3 — Создать модель `AppUpdateInfo` с полями: `Version`, `DownloadUrl`, `ReleaseNotes`, `IsPreRelease`, `AssetName`
  - [ ] 6.4.4 — В `UpdateService.CheckAppUpdateAsync()`: через Octokit получить теги релизов VidDownload, сравнить с текущей версией из сборки
  - [ ] 6.4.5 — Реализовать `DownloadAppUpdateAsync()`: скачать выбранный asset (.exe или .msi) во временную папку
  - [ ] 6.4.6 — Создать отдельный `Updater.exe` (простая консоль): дождаться завершения основного процесса, заменить файл, перезапустить
  - [ ] 6.4.7 — Запускать `Updater.exe` с аргументами: `--src <temp>\new.exe --dst <app>\VidDownload.WPF.exe --pid <current-pid>`
  - [ ] 6.4.8 — Добавить свойства `IsAppUpdateAvailable`, `AppUpdateVersion` в `MainViewModel`
  - [ ] 6.4.9 — Создать команду `UpdateAppCommand` в `MainViewModel`: проверить обновление, предложить скачать, скачать, запустить updater
  - [ ] 6.4.10 — В `MainWindow.xaml`: добавить кнопку/индикатор «Доступно обновление VidDownload» рядом с проверкой yt-dlp
  - [ ] 6.4.11 — Проверить цифровую подпись (если есть): верифицировать скачанный файл перед заменой
  - [ ] 6.4.12 — Обработать краевые случаи: нет прав на запись в `Program Files`, антивирус блокирует замену, ошибка скачивания

---

## Этап 7: Локализация и доступность

- [x] 7.1 — **Перевод интерфейса на английский**: ресурсные файлы `.resx` для EN
  - [x] 7.1.1 — Создать структуру `.resx`: `Resources.resx` (RU по умолчанию) и `Resources.en.resx` (EN)
  - [x] 7.1.2 — Извлечь строки из XAML: `MainWindow.xaml` (22 строки: заголовки, лейблы, кнопки, чекбоксы)
    - [x] 7.1.2.1 — Label «Разрешение:», «Кодек:», «Аудио формат:», «Расширение файла:»
    - [x] 7.1.2.2 — CheckBox «Скачать весь плейлист», «Только аудио», «Перекодировать видео»
    - [x] 7.1.2.3 — CheckBox «Скачать субтитры», «Встроить субтитры» + Label «Язык:»
    - [x] 7.1.2.4 — Label «Скорость:», «ETA:», «Размер:», «FFmpeg:»
    - [x] 7.1.2.5 — Button «Скачать», «Отмена», «Проверить обновление», «Открыть папку с видео»
    - [x] 7.1.2.6 — Button «История», «Конвертер», «?»
  - [x] 7.1.3 — Извлечь строки из XAML: `ConvertWindow.xaml` (5 строк: заголовок, лейблы, кнопки)
  - [x] 7.1.4 — Извлечь строки из XAML: `HelpWindow.xaml` (5 строк: заголовок, лейблы, гиперссылки)
  - [x] 7.1.5 — Извлечь строки из XAML: `HistoryWindow.xaml` (8 строк: заголовок, колонки, кнопки)
  - [x] 7.1.6 — Извлечь строки из `MainViewModel.cs` (~45 строк: статусы, сообщения, диалоги, заголовки окон)
    - [x] 7.1.6.1 — Статусные сообщения: «Пустое поле ссылки!», «Загрузка отменена», «проверка...», «не установлен», «не найдена»
    - [x] 7.1.6.2 — Предупреждения и ошибки FFmpeg: «Поиск обновлений FFmpeg...», «FFmpeg актуален», «FFmpeg не найден», «обновление...», «установлен», «FFmpeg успешно обновлён»
    - [x] 7.1.6.3 — Диалоги FFmpeg: «Текущая версия: …\nПоследняя сборка: …\nЗагрузить и установить?», «Не удалось найти ссылку…»
    - [x] 7.1.6.4 — Диалоги yt-dlp: «Текущая версия: …\nПоследняя версия: …\nПодтвердите начало обновления.», «Доступна новая версия yt-dlp!»
    - [x] 7.1.6.5 — Статусы yt-dlp: «Идет загрузка обновления yt-dlp!», «Версия yt-dlp обновлена до …»
    - [x] 7.1.6.6 — Сообщения субтитров: «Встраивание субтитров недоступно для аудио.», «Формат AVI может не поддерживать встроенные субтитры…»
    - [x] 7.1.6.7 — Пути сохранения: «Нет доступа к папке сохранения…», «Нет доступа к стандартной папке Видео…»
    - [x] 7.1.6.8 — Отмена загрузки: «Вы уверены, что хотите отменить загрузку?»
    - [x] 7.1.6.9 — Шаблон ссылки: «Поле для ссылки на видео:» / «Поле для ссылки на плейлист:»
    - [x] 7.1.6.10 — Общие заголовки: «Ошибка», «Предупреждение», «Ошибка обновления», «Подтверждение», «Подтверждение отмены»
  - [x] 7.1.7 — Извлечь строки из `ConvertViewModel.cs` (~9 строк: диалоги, фильтры, заголовки)
  - [x] 7.1.8 — Извлечь строки из `YtDlpService.cs` (1 строка: сообщение об ошибке)
  - [x] 7.1.9 — Извлечь строки из `FFmpegService.cs` (~5 строк: прогресс, ошибки)
  - [x] 7.1.10 — Извлечь строки из `FFmpegAction.cs` (1 строка: ошибка файла не найден)
  - [x] 7.1.11 — Извлечь строки из `UpdateService.cs` (1 строка: прогресс загрузки)
  - [x] 7.1.12 — Подключить ресурсы в XAML: `xmlns:res="clr-namespace:VidDownload.WPF.Resources"` + привязать через `{x:Static res:Res.Key}`
  - [x] 7.1.13 — Создать статический класс `Res` для доступа к ресурсам из C#-кода: `Res.ResourceManager.GetString("key")`
  - [x] 7.1.14 — Заменить все `_messageService.Warning("...", "Предупреждение")` и аналоги на вызовы из ресурсов
  - [x] 7.1.15 — Заменить форматированные строки с плейсхолдерами (например, `"Ошибка: {ex.Message}"`) на `string.Format(Res.ErrorWithMessage, ex.Message)`
  - [x] 7.1.16 — Заполнить `Resources.en.resx`: перевести все ~100 строк на английский
  - [x] 7.1.17 — Проверить сборку: `dotnet build` без ошибок после замены всех строк
  - [ ] 7.1.18 — Прогнать приложение: визуально проверить, что все строки отображаются корректно (EN-сборка не ломает RU-рендеринг)
- [x] 7.2 — **Переключение языка в рантайме**: через `CultureInfo` или `ResourceDictionary`
  - [x] 7.2.1 — Выбрать архитектуру: `CultureInfo` + `ResourceManager` с ручным обновлением binding'ов, либо WPF-словари `ResourceDictionary` с `DynamicResource`
  - [x] 7.2.2 — Создать `ILocalizationService` / `LanguageService` (синглтон):
    - [x] 7.2.2.1 — Свойство `CurrentCulture` / `CurrentLanguage`
    - [x] 7.2.2.2 — Событие `LanguageChanged` / реализовать `INotifyPropertyChanged` для привязок
    - [x] 7.2.2.3 — Метод `SetLanguage(string cultureCode)`: меняет `Thread.CurrentThread.CurrentUICulture` и оповещает подписчиков
  - [x] 7.2.3 — Создать класс-обёртку `LocalizedStrings` с Observable-свойствами под каждую ресурсную строку:
    - [x] 7.2.3.1 — Каждое свойство читает `Resources.ResourceManager.GetString(name, CultureInfo)`
    - [x] 7.2.3.2 — При смене языка — поднять `PropertyChanged` для всех свойств (через цикл по ключам или ручную рассылку)
  - [x] 7.2.4 — Зарегистрировать `LocalizedStrings` как синглтон в DI (`AppServices.cs`) и передать во все VM
  - [x] 7.2.5 — В XAML: привязать элементы к `{Binding LocalizedStrings.Key}` вместо `{x:Static res:Resources.Key}`
    - [x] 7.2.5.1 — Привязать `MainWindow.xaml`: Title, Label, CheckBox, Button, все строки
    - [x] 7.2.5.2 — Привязать `ConvertWindow.xaml`, `HelpWindow.xaml`, `HistoryWindow.xaml`
  - [x] 7.2.6 — В C#-коде: заменить прямые вызовы ресурсов на `LocalizedStrings.Instance.KeyName` или через инъекцию
    - [x] 7.2.6.1 — MainViewModel.cs: статусы, диалоги, ошибки
    - [x] 7.2.6.2 — ConvertViewModel.cs, YtDlpService.cs, FFmpegService.cs, FFmpegAction.cs, UpdateService.cs
  - [x] 7.2.7 — Создать UI для выбора языка:
    - [x] 7.2.7.1 — Добавить `ComboBox` (или `RadioButton`) в `MainWindow.xaml`: `RU` / `EN` / другие
    - [x] 7.2.7.2 — Добавить свойство `SelectedLanguage` в `MainViewModel`
    - [x] 7.2.7.3 — Привязать `SelectedLanguage` к `LanguageService.SetLanguage()` через команду или `PropertyChanged`
  - [x] 7.2.8 — Сохранять выбор языка между сессиями:
    - [x] 7.2.8.1 — Добавить поле `Language` в `UserSettings.cs`
    - [x] 7.2.8.2 — Загружать язык в `MainViewModel.LoadSettingsAsync()` и применять через `LanguageService.SetLanguage()`
    - [x] 7.2.8.3 — Сохранять язык в `MainViewModel.SaveSettingsAsync()`
  - [x] 7.2.9 — Обработать трюковой случай: окно `ConvertWindow`, `HelpWindow`, `HistoryWindow` открыто в момент смены языка — обновить его строки (общий синглтон `LocalizedStrings` сработает, если окно использует Binding)
  - [x] 7.2.10 — Обработать трюковой случай: если ресурсного файла для выбранного языка нет — падать на `RU` (culture fallback)
  - [x] 7.2.11 — Проверить сборку: `dotnet build` без ошибок после внедрения runtime-переключения
  - [x] 7.2.12 — Функциональный тест: переключить язык во время загрузки/простоя — все строки в главном окне и диалогах обновляются без перезапуска
