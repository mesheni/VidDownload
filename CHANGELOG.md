# Changelog

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## [0.9.0] — 2026-08-29

### Добавлено
- Скачивание плейлистов: прогресс по каждому элементу (бейдж PLAYLIST, счётчик N/M, процент текущего видео и общий процент).
- Переключатель темы в заголовке главного окна: Авто (по системе) / Светлая / Тёмная, сохранение предпочтения в настройках.

### Изменено
- Миграция UI-библиотеки с HandyControl 3.5 на WPF-UI 4.3 (Fluent Design, Windows 11): `FluentWindow`, Mica-фон, `TitleBar` с системными кнопками, ContentDialog-диалоги и Snackbar-уведомления вместо Growl.
- Новая тема оформления: словари `Themes/Dark.Colors.xaml` / `Light.Colors.xaml` + общие `Shared.xaml` / `Styles.xaml`, шрифты Segoe UI Variable и Cascadia Code, скруглённые карточки и прогресс-бары.
- Список очереди виртуализирован (`VirtualizingStackPanel` + Recycling).
- Настройка `Appearance` (Auto/Light/Dark) в settings.json.

### Удалено
- HandyControl и связанные сервисы (`GrowlNotificationService`, `HandyControlDialogService`, `HandyControlMessageService`, `Themes/Colors.xaml`).

## [0.8.0]

### Добавлено
- Очередь загрузок: до 3 параллельных загрузок, пауза/продолжение с докачкой `.part`, отмена, повтор, очистка завершённых.
- Сворачивание в системный трей с balloon-уведомлениями о завершении загрузок.
- Мониторинг буфера обмена с предложением добавить ссылку в очередь.
- Горячие клавиши: `Enter` — добавить в очередь, `Esc` — отменить активную загрузку.
- Автообновление FFmpeg из GUI (BtbN/FFmpeg-Builds) и самообновление приложения через `Updater.exe` (GitHub Releases).
- История загрузок с повторным скачиванием.
- Расширены модульные тесты (парсер прогресса, версии, аргументы yt-dlp, очередь).
