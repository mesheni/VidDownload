# VidDownload

<img width="1774" height="887" alt="ChatGPT Image 24 авг  2026 г , 15_27_22" src="https://github.com/user-attachments/assets/46d80811-285d-4991-87c0-623e3b84641a" />

**Графическая оболочка для yt-dlp: скачивание видео и аудио с различных платформ по ссылке, плюс встроенный конвертер на FFmpeg.**

---

## Основные функции

- Загрузка видео и аудио через WPF-интерфейс
- **Предпросмотр перед загрузкой**: обложка, название, длительность, выбор конкретного формата или элементов плейлиста
- **Очередь загрузок**: несколько ссылок подряд, пауза/продолжение (yt-dlp продолжает докачку), отмена отдельных элементов, до 3 параллельных загрузок (настраивается в окне настроек)
- **Очередь переживает перезапуск**: незавершённые загрузки восстанавливаются в состоянии паузы
- Скачивание плейлистов с выбором нужных элементов
- Выбор разрешения (до 4K), видеокодека (AV1, VP9, H.264, H.265 и др.), формата ремукса (MP4, MKV, AVI, WebM) и аудиоформата при извлечении (MP3, FLAC, WAV, AAC, M4A), качество аудио
- **Выбор папки сохранения** и **лимит скорости** (`--limit-rate`) прямо из интерфейса
- **Куки** (из Chrome/Edge/Firefox/Opera или cookies.txt) и **прокси** — для приватных видео, в окне настроек
- Встраивание обложки и метаданных, субтитры с конвертацией в SRT, фрагмент по таймкодам, архив загрузок (пропуск скачанного), ретраи
- Пакетный импорт списка ссылок (.txt) и drag&drop ссылок на окно
- **Свертывание в системный трей** с balloon-уведомлениями о завершении загрузок
- **Мониторинг буфера обмена**: скопировали ссылку — приложение предложит добавить её в очередь
- Горячие клавиши: `Enter` — добавить в очередь, `Esc` — отменить активную загрузку
- Действие после очереди: выключение / сон / гибернация
- **Конвертер FFmpeg** в отдельном окне: форматы MP4 / MKV / MOV / AVI / WebM / FLV / WMV / TS, режим «только аудио» (MP3, AAC, FLAC, OPUS, WAV, M4A), пакетная конвертация, предпросмотр итоговой ffmpeg-команды
- Аппаратные энкодеры NVENC / AMF / QSV с **реальной проверкой работоспособности** (короткое тестовое кодирование — присутствие в `-encoders` не гарантирует рабочего GPU), кодек-зависимые опции качества и пресетов (CRF, `-cq`, `-global_quality`, `-qp`, `-cpu-used` для VP9/AV1) и выбор только контейнер-совместимых кодеков
- Автоматическое обновление yt-dlp, FFmpeg и самого приложения (включая Updater.exe)
- История загрузок с поиском, повторным скачиванием, открытием файла и экспортом в CSV
- Сохранение настроек между сессиями (JSON, атомарная запись), отдельное окно настроек
- Переключение языка интерфейса на лету (RU / EN / ZH)
- Темы оформления: Авто (по системе) / Светлая / Тёмная — переключатель в заголовке окна
- Single-instance: повторный запуск разворачивает уже открытое окно
- Логирование операций

---

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (или Runtime для запуска)
- `yt-dlp.exe` — скачивается автоматически при первом запуске
- `ffmpeg.exe` и `ffprobe.exe` — скачиваются автоматически из GUI (берётся стабильная ветка сборки, а не nightly — nightly может требовать более нового драйвера GPU и ломать NVENC; либо вручную в `PATH`)
- [WiX Toolset v7](https://wixtoolset.org/) — только для сборки MSI-инсталлятора

---

## Структура проекта

| Компонент | Описание |
|---|---|
| `VidDownload.WPF/` | Основное WPF-приложение (net10.0-windows) |
| `Updater/` | Консольный помощник автообновления (net10.0, single-file, с проверкой PE-подписности) |
| `VidDownload.Tests/` | Модульные тесты xUnit (аргументы yt-dlp и FFmpeg-конвертера, парсер прогресса и метаданных, версии, таймкоды, URL-хелпер, очередь и её персистентность) |
| `Setup.wxs` | Описание MSI-инсталлятора (WiX v7) |
| `CHANGELOG.md` | История версий |
| `build-installer.ps1` | Скрипт сборки MSI |
| `.github/workflows/` | CI: тесты на каждый push в master и PR; при теге `v*` — сборка MSI и portable `.exe` (вместе с `Updater.exe`) в GitHub Releases |

### Архитектура (VidDownload.WPF)

- **MVVM** на базе `CommunityToolkit.Mvvm`
- **DI-контейнер**: `Microsoft.Extensions.DependencyInjection`
- **Сервисы** (`Services/`):
  - `YtDlpService` — запуск yt-dlp, парсинг прогресса и путей итоговых файлов
  - `DownloadQueueService` — очередь загрузок: параллельность, пауза/резюме, отмена
  - `QueuePersistenceService` — сохранение незавершённой очереди в `queue.json` и восстановление после перезапуска
  - `UpdateService` — проверка обновлений VidDownload и yt-dlp через GitHub API (Octokit)
  - `FFmpegService` — проверка/загрузка обновлений FFmpeg
  - `SettingsService` — сохранение настроек в JSON (атомарно)
  - `DownloadHistoryService` — история загрузок (JSON, атомарно)
  - `TrayService` / `ClipboardMonitorService` / `SnackbarNotificationService` — трей, буфер обмена, уведомления (Snackbar + balloon в трей)
  - `NetworkHelper` — общий HttpClient, проверка интернета, безопасное скачивание файлов (проверка HTTP-статуса, temp+move)
  - `FluentMessageService` / `FluentDialogService` — неблокирующие сообщения и диалоги поверх ContentDialog (WPF-UI)
  - `UiThemeService` / `UiDialogHost` — тема Авто/Светлая/Тёмная и хостинг ContentDialog в дочерних окнах
  - `LocalizationService` — переключение языка во время выполнения
- **ViewModels**: `MainViewModel`, `ConvertViewModel`, `HistoryViewModel`, `SettingsViewModel`, `VideoInfoViewModel`
- **Логика аргументов и парсинга** (`Control/`): `Command` — аргументы yt-dlp; `ConversionOptions` / `FFmpegAction` — аргументы FFmpeg под семейство кодека и проба аппаратных кодеров; `MetadataParser`, `Timecodes`
- **UI-библиотека**: WPF-UI 4.3 (Fluent Design, Mica), трей — H.NotifyIcon.Wpf

---

## Запуск и сборка

```powershell
# Клонирование
git clone https://github.com/mesheni/VidDownload.git
cd VidDownload

# Восстановление зависимостей
dotnet restore

# Запуск (Debug)
dotnet run --project VidDownload.WPF

# Публикация в папку publish\
dotnet publish VidDownload.WPF -c Release -o publish

# Сборка решения
dotnet build -c Release

# Запуск тестов
dotnet test VidDownload.Tests
```

### Сборка MSI-инсталлятора

Требуется WiX Toolset v7:

```powershell
# Установка WiX (однократно)
dotnet tool install --global wix

# Принятие EULA (однократно)
wix eula accept wix7

# Сборка MSI
powershell -ExecutionPolicy Bypass -File build-installer.ps1
```

Скрипт выполняет:
1. `dotnet publish` основного приложения
2. Сборку `Updater.exe` (single-file)
3. Генерацию `Files.wxs` из опубликованных файлов
4. Сборку MSI через `wix build`
5. Очистку промежуточных файлов

Результат: `VidDownload.msi` в корне проекта. Установщик — per-machine, требует права администратора.

---

## Интерфейс

<img width="701" height="733" alt="image" src="https://github.com/user-attachments/assets/64ee2db9-a0eb-4ce5-9455-e4a43e04cb36" />

---

## Лицензия

MIT. Подробности в `license.txt`.

---

## Связь

- Telegram: [@mesheni](https://t.me/mesheni)
- GitHub: [https://github.com/mesheni/VidDownload](https://github.com/mesheni/VidDownload)
