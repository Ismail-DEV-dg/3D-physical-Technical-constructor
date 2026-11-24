# Архитектура

Решение разделено на пять проектов. Core и Simulation собираются для net8.0, приложение и тесты — для net8.0-windows.

| Проект | Назначение |
|---|---|
| Core | Модели и форматы, физическая геометрия, netlist, ERC, узловой DC-расчёт, история, генерация кода и контракты репозиториев. |
| Infrastructure | File/JSON-реализации, каталоги компонентов и учебных проектов, атомарная запись, миграции, журнал. |
| Simulation | Виртуальные часы, очередь событий, GPIO, модели компонентов, приборы и учебное выполнение скетча. Зависит от Core. |
| App | WPF, AvalonEdit, HelixToolkit.Wpf, ViewModel и регистрация сервисов. Зависит от Core, Infrastructure, Simulation. |
| Tests | xUnit, тесты моделей и репозиториев, полный прогон примеров, проверки WPF в STA. |

## Потоки данных

- Редактирование: ввод в представлении преобразуется в команды ViewModel. Жест записывается одной командой в историю из 200 операций. «Код» использует свою историю AvalonEdit.
- Проект: один CircuitProject содержит электрическую схему, общие ID, отдельные координаты «Макета» и «Схемы», привязки к breadboard и скетч. ViewModel рабочих вкладок читают этот объект через ProjectViewModel.
- Проводимость: INetlistBuilder объединяет группы отверстий, привязки и концы перемычек. IErcService выполняет зарегистрированные правила, IDcSolver решает поддерживаемые DC-ветви. Цвет и изгиб перемычки не участвуют в электрической связности.
- Сеанс: ISimulationPlayback передаёт снимок модели в ISimulationSessionFactory. Симуляция обновляет собственное состояние. UI получает последний снимок и может пропускать промежуточные кадры.
- Хранилище: IProjectRepository читает и мигрирует JSON. ProjectDocumentResolver проверяет ссылки, и ProjectSessionViewModel заменяет открытый проект только после успешной валидации.
- 3D: ISceneBuilder преобразует физические координаты в описания сцены. WPF получает замороженные меши и меняет отдельные узлы по сигналам сеанса.

## Основные интерфейсы

- IComponentLibraryRepository, IFootprintRepository, IBreadboardRepository — справочные данные.
- ITextMeasurer, ComponentLayoutCalculator — измерение текста и раскладка условных символов.
- ICodeGenerator, ICodeAnalyzer — шаблоны скетчей по netlist и ограниченный анализ GPIO.
- ISimulationClock, ISimulationEngine, IComponentBehavior, IInstrument — время, события, поведения и измерения.
- IChipEvaluator, IChipRepository, IChipTemplateRepository — таблицы/графы, библиотека и шаблоны.
- IProjectSampleRepository, IErrorLogRepository, IExportRepository — каталог примеров, обезличенный журнал и запись экспорта.

Диаграммы зависимостей, классов и взаимодействий собраны в [разделе UML](uml/README.md).
