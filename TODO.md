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
- [ ] 2.6 — **Настройки сохранять между сессиями**: JSON-файл или `Properties.Settings` для выбранных форматов/кодеков
- [ ] 2.7 — **Внедрить IMessageService / IDialogService** для абстракции всплывающих сообщений и диалоговых окон (избавление ViewModels от зависимости от HandyControl)
- [ ] 2.8 — **Использовать ArgumentList вместо конкатенации строк** при запуске `yt-dlp` через ProcessStartInfo для безопасности путей и параметров

---

## Этап 3: Пользовательский функционал

- [ ] 3.1 — **Индикация оставшегося времени / скорости**: парсить дополнительную информацию из stdout yt-dlp
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

- [ ] 4.1 — **Миграция на .NET 8**: `net8.0-windows` вместо `net6.0-windows10.0.22621.0`
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
