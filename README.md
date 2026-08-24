# VidDownload

<img width="1774" height="887" alt="ChatGPT Image 24 авг  2026 г , 15_27_22" src="https://github.com/user-attachments/assets/46d80811-285d-4991-87c0-623e3b84641a" />

**Графическая оболочка для yt-dlp, позволяющая скачивать видео и аудио с различных платформ по ссылке.**

---

## Основные функции

- Загрузка видео и аудио через WPF-интерфейс
- **Очередь загрузок**: несколько ссылок подряд, пауза/продолжение (yt-dlp продолжает докачку), отмена отдельных элементов, до 3 параллельных загрузок (настраивается в settings.json)
- Скачивание плейлистов целиком
- Выбор разрешения (до 4K), видеокодека (AV1, H.264, H.265), формата (MP4, MKV, AVI) и аудиокодека
- **Выбор папки сохранения** и **лимит скорости** (`--limit-rate`) прямо из интерфейса
- **Свертывание в системный трей** с balloon-уведомлениями о завершении загрузок
- **Мониторинг буфера обмена**: скопировали ссылку — приложение предложит добавить её в очередь
- Горячие клавиши: `Enter` — добавить в очередь, `Esc` — отменить активную загрузку
- Перекодировка видео через FFmpeg (отдельное окно конвертера)
- Автоматическое обновление yt-dlp и FFmpeg через GUI
- Автообновление самого приложения (Updater.exe; в релизах публикуется portable `.exe`)
- Скачивание и встраивание субтитров (`--write-subs --embed-subs`)
- Индикация скорости, ETA и размера во время загрузки (прогресс больше не сбрасывается на строках без процентов)
- История загрузок с реальными названиями видео и повторным скачиванием
- Сохранение настроек между сессиями (JSON, атомарная запись)
- Переключение языка интерфейса на лету (RU / EN / ZH)
- Логирование операций

---

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (или Runtime для запуска)
- `yt-dlp.exe` — скачивается автоматически при первом запуске
- `ffmpeg.exe` и `ffprobe.exe` — скачиваются автоматически из GUI (или вручную в `PATH`)
- [WiX Toolset v7](https://wixtoolset.org/) — только для сборки MSI-инсталлятора

---

## Структура проекта

| Компонент | Описание |
|---|---|
| `VidDownload.WPF/` | Основное WPF-приложение (net10.0-windows) |
| `Updater/` | Консольный помощник автообновления (net10.0, single-file, с проверкой PE-подписности) |
| `VidDownload.Tests/` | Модульные тесты xUnit (парсер прогресса, версии, аргументы yt-dlp, очередь загрузок) |
| `Setup.wxs` | Описание MSI-инсталлятора (WiX v7) |
| `build-installer.ps1` | Скрипт сборки MSI |
| `.github/workflows/` | CI/CD: MSI + portable `.exe` при push тега `v*` |

### Архитектура (VidDownload.WPF)

- **MVVM** на базе `CommunityToolkit.Mvvm`
- **DI-контейнер**: `Microsoft.Extensions.DependencyInjection`
- **Сервисы** (`Services/`):
  - `YtDlpService` — запуск yt-dlp, парсинг прогресса и путей итоговых файлов
  - `DownloadQueueService` — очередь загрузок: параллельность, пауза/резюме, отмена
  - `UpdateService` — проверка обновлений VidDownload и yt-dlp через GitHub API (Octokit)
  - `FFmpegService` — проверка/загрузка обновлений FFmpeg
  - `SettingsService` — сохранение настроек в JSON (атомарно)
  - `DownloadHistoryService` — история загрузок (JSON, атомарно)
  - `TrayService` / `ClipboardMonitorService` / `GrowlNotificationService` — трей, буфер обмена, уведомления
  - `NetworkHelper` — общий HttpClient, проверка интернета, безопасное скачивание файлов (проверка HTTP-статуса, temp+move)
  - `MessageService` / `DialogService` — абстракция над HandyControl
  - `LocalizationService` — переключение языка во время выполнения
- **ViewModels**: `MainViewModel`, `ConvertViewModel`, `HistoryViewModel`
- **UI-библиотека**: HandyControl 3.5, трей — H.NotifyIcon.Wpf

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
