# VidDownload

**Графическая оболочка для yt-dlp, позволяющая скачивать видео и аудио с различных платформ по ссылке.**

---

## Основные функции

- Загрузка видео и аудио через WPF-интерфейс
- Скачивание плейлистов целиком
- Выбор разрешения (до 4K), видеокодека (AV1, H.264, H.265), формата (MP4, MKV, AVI) и аудиокодека
- Перекодировка видео через FFmpeg (отдельное окно конвертера)
- Автоматическое обновление yt-dlp и FFmpeg через GUI
- Автообновление самого приложения (Updater.exe)
- Скачивание и встраивание субтитров (`--write-subs --embed-subs`)
- Индикация скорости, ETA и размера во время загрузки
- Отмена загрузки
- История загрузок с возможностью повторного скачивания
- Сохранение настроек между сессиями (JSON)
- Переключение языка интерфейса на лету (RU / EN)
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
| `Updater/` | Консольный помощник автообновления (net10.0, single-file) |
| `Setup.wxs` | Описание MSI-инсталлятора (WiX v7) |
| `build-installer.ps1` | Скрипт сборки MSI |
| `.github/workflows/` | CI/CD: автосборка MSI при push тега `v*` |

### Архитектура (VidDownload.WPF)

- **MVVM** на базе `CommunityToolkit.Mvvm`
- **DI-контейнер**: `Microsoft.Extensions.DependencyInjection`
- **Сервисы** (`Services/`):
  - `YtDlpService` — запуск yt-dlp, парсинг прогресса
  - `UpdateService` — проверка обновлений VidDownload и yt-dlp через GitHub API (Octokit)
  - `FFmpegService` — проверка/загрузка обновлений FFmpeg
  - `SettingsService` — сохранение настроек в JSON
  - `DownloadHistoryService` — история загрузок (JSON)
  - `MessageService` / `DialogService` — абстракция над HandyControl
  - `LocalizationService` — переключение языка во время выполнения
- **ViewModels**: `MainViewModel`, `ConvertViewModel`, `HistoryViewModel`
- **UI-библиотека**: HandyControl 3.5

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

## CI/CD

GitHub Actions (`build-and-installer.yml`) запускается при push тега `v*`:

- Собирает MSI (`build-installer.ps1`)
- Публикует артефакт `VidDownload-Installer`
- Создаёт GitHub Release с прикреплённым `.msi`

---

## Интерфейс

![Скриншот интерфейса](https://github.com/mesheni/VidDownload/assets/26280352/b4f1df46-2cf3-4ee4-9ca4-9d95e091522c)

---

## Лицензия

MIT. Подробности в `license.txt`.

---

## Связь

- Telegram: [@mesheni](https://t.me/mesheni)
- GitHub: [https://github.com/mesheni/VidDownload](https://github.com/mesheni/VidDownload)
