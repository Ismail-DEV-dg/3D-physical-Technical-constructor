# Фирменный стиль Circuit Editor

Выбран **Chip Trace**. Ломаная дорожка образует букву C, а отдельная зелёная площадка обозначает сигнал.

На 16 и 32 px буква и площадка остаются различимы. Тёмная подложка с контуром отделяет знак от светлого и тёмного фона.

| Вариант | Исходник | Превью |
| --- | --- | --- |
| **Chip Trace** · выбран | [SVG](../assets/branding/candidates/chip-trace.svg) | [PNG](../assets/branding/candidates/chip-trace.png) |
| Board Gate · логический вентиль | [SVG](../assets/branding/candidates/board-gate.svg) | [PNG](../assets/branding/candidates/board-gate.png) |
| Pulse Loop · импульс внутри контура чипа | [SVG](../assets/branding/candidates/pulse-loop.svg) | [PNG](../assets/branding/candidates/pulse-loop.png) |

## Палитра

| Цвет | HEX | Назначение |
| --- | --- | --- |
| Тёмный | `#0A0E17` | Подложка |
| Циан | `#00E5FF` | Основная дорожка |
| Фиолетовый | `#8B5CFF` | Вторая дорожка и выводы |
| Зелёный | `#39FF88` | Сигнальная площадка |

## Файлы

- [Основной SVG](../assets/branding/logo.svg), [PNG 256](../assets/branding/logo-256.png) и [PNG 512](../assets/branding/logo-512.png).
- [16 px](../assets/branding/logo-16.svg), [24 px](../assets/branding/logo-24.svg) и [32 px](../assets/branding/logo-32.svg) — отдельно нарисованные версии с собственными отступами и толщинами.
- [Windows ICO](../assets/branding/logo.ico) — 16, 24, 32, 48, 64, 128 и 256 px. Первые три кадра используют малые версии.
- [Горизонтальный SVG](../assets/branding/logo-horizontal.svg) и [PNG](../assets/branding/logo-horizontal.png) для шапки документов.
- [GitHub Social Preview](../assets/branding/social-preview.png) — 1280×640 PNG для настроек репозитория.
- [Левая панель Inno Setup](../assets/branding/wizard-panel.bmp) — 164×314 BMP и [малое изображение](../assets/branding/wizard-icon.bmp) — 55×55 BMP.

## Регенерация и замена

На Windows из корня проекта:

```powershell
dotnet run --project tools/BrandingGenerator -- assets/branding chip-trace
```

Замените последний аргумент на `board-gate` или `pulse-loop`. Перегенерируются основные файлы, все кадры ICO, установочные изображения и превью. Исходная геометрия и малые размеры находятся в [MarkCatalog.cs](../tools/BrandingGenerator/MarkCatalog.cs). После выбора другого варианта обновите описание в этом документе и пересоберите приложение и установщик. Генератор использует .NET 8/WPF и не входит в основное решение.

Логотип оригинальный. Генератор и изображения распространяются по [MIT](../LICENSE).
