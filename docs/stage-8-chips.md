# Этап 8. Конструктор микросхем

Готовы шаги 1–7. Режимы таблицы истинности и логической схемы используют общую распиновку DIP-8, DIP-14, DIP-16 и DIP-20. В «Макете» рисуется процедурный корпус с выемкой и меткой первого вывода. В «Схеме» подписи и контакты раскладывает общий калькулятор символов.

## Шаблоны

| Шаблоны | Что моделируется |
|---|---|
| 7400, 7402, 7404, 7408, 7432, 7486 | Функции вентилей и распиновка DIP. Электрические параметры приближены общим учебным профилем TTL. |
| MUX-4, DECODER-2, FULL-ADDER | Таблицы мультиплексора 4:1, дешифратора 2→4 и полного сумматора. Используются учебные распиновки. |
| 7474 | Два D-триггера с фронтом CLK и асинхронными PRE/CLR. Одновременная активация PRE и CLR отображается как X. |
| COUNTER-4 | Учебный 4-битный синхронный счётчик с асинхронным сбросом и собственной распиновкой. |
| 74HC595 | Восьмибитный регистр сдвига, отдельная защёлка, асинхронный SRCLR, трёхсостоянийные QA–QH и независимый QH′. При одновременных фронтах RCLK считывает прежнее значение регистра. |

Пороги и уровни хранятся в JSON-профилях TTL, LS и HC. Для HC пороги выражены в долях VCC. Пределы и выходное сопротивление заданы раздельно для втекающего и вытекающего тока. Цифровая модель использует заданную инерционную задержку 0–5000 мкс. Переходные аналоговые процессы, времена установления и удержания остаются за пределами цифровых шаблонов. Проверочные стенды используют отдельные часы и память.

Пользовательские файлы `.chip` имеют версию контейнера 1 и отдельную ревизию определения. Миграционный механизм проверен на искусственной старой версии. Проекты `.circ` v4 содержат одну встроенную копию каждой размещённой пары ID и ревизии. Миграция 3→4 сохраняет схему, скетч и настройки симуляции. При конфликте можно выбрать локальную или встроенную копию. При несовместимом обновлении открывается встроенная ревизия, а в диалоге видна причина.

## Практикум 7400 с двумя кнопками

Подключите вывод 14 к 5 В Arduino Uno, вывод 7 к GND. Для каждого из входов 1 и 2 сделайте отдельную ветвь. Кнопка соединяет вход с 5 В при нажатии, резистор 10 кΩ удерживает вход на GND при отпускании. Подключите нагрузку в порядке 5 В → 270 Ω → анод красного LED → катод → выход 3.

| Кнопка 1 | Кнопка 2 | Выход 3 | LED |
|---|---|---|---|
| Отпущена | Отпущена | 1 | Погашен |
| Нажата | Отпущена | 1 | Погашен |
| Отпущена | Нажата | 1 | Погашен |
| Нажата | Нажата | 0 | Горит |

В проверенном макете при двух замкнутых кнопках ток составил 10,34 мА, предупреждение о перегрузке отсутствовало. Для проверки этого состояния перед стартом выберите «Кнопка замкнута → Да» у обеих кнопок. При отключении VCC появилось `CHIP_UNPOWERED`, выход стал `Z`. При нагрузке между выходом и GND сверяйтесь с пределом вытекающего тока (source). В приведённой цепи к 5 В действует предел втекающего тока (sink).

## Экспорт и диагностика

- Для экспорта выберите «Распиновка PNG» или «Распиновка Markdown». В режиме таблицы доступны «Таблица Markdown» и «Таблица CSV».
- В Excel при открытии через мастер импорта используйте UTF-8 (65001) и разделитель «точка с запятой». В заголовке CSV, например `IN:9:3A`, указаны направление, номер и имя вывода. Табличные секции разделены пустой строкой.
- Если редактор показывает «Найдена комбинационная петля без блока памяти», проверьте источники вида `B1` у вентилей. Выход NAND, подключённый к собственному входу, вызывает эту проверку. Ошибка находится в списке под стендом.
- `CHIP_POWER` относится к фактическому напряжению между VCC и GND при симуляции. `CHIP_UNPOWERED` указывает на отсутствующее внешнее подключение питания. Для `CHIP_OUTPUT_CURRENT` проверьте направление тока и последовательный резистор.

## Ручная проверка

1. Запустите `dotnet run --project CircuitEditor.App`. Откройте «Микросхема», выберите **7400** и нажмите «Открыть шаблон». Проверьте DIP-14 и четыре секции NAND. В третьей секции входы идут 9, 10, в четвёртой 12, 13.
2. В секции измените ячейку, нажмите «Применить ячейки», затем «Отменить правку». Убедитесь, что вернулась исходная ячейка. Запустите стенд и включите оба входа первой секции. На выходе 3 появится 0. «Проверить таблицу» покажет результат полного прогона.
3. Нажмите «Сохранить в библиотеку». В «Макете» перетащите 7400 из категории «Микросхемы» на breadboard, разместив вывод 1 в E2, а противоположный вывод 14 в F2. Попробуйте разместить обе стороны в одной группе отверстий. Вставка должна отклониться. Откройте «Схема» и проверьте 14 контактов.
4. Без питания выполните ERC. В списке появится `CHIP_UNPOWERED`. Подключите 14 к 5 В, 7 к GND, а 1 и 2 к 5 В. Соберите цепь 5 В → 270 Ω → анод LED → катод LED → выход 3. Запустите симуляцию. В этой цепи в выход втекает около 10 мА. При 120 Ω появится `CHIP_OUTPUT_CURRENT`. Для направления source нужны HIGH на выходе и нагрузка между этим выходом и GND.
5. На стенде **74HC595** переключите SRCLR# и SER в 1. Выберите SRCLK и дайте импульс. Затем выберите RCLK и дайте импульс. На QA появится 1. Переведите OE# в 1 и проверьте Z на QA–QH. QH′ продолжит показывать состояние регистра сдвига.
6. Сохраните тестовый проект в `.circ`. В пользовательской библиотеке сохраните новую ревизию тестовой микросхемы. Повторно откройте проект и проверьте оба варианта диалога. Для проверки переносимости экспортируйте тестовую микросхему, уберите её из пользовательской библиотеки и откройте тот же `.circ` со встроенным определением.
7. Экспортируйте распиновку в PNG и Markdown, таблицу 7400 в CSV и Markdown. Проверьте нумерацию, подписи, секции и чтение CSV через «Импорт CSV». Для логической схемы проверьте провод через поле источника, например P2 или B1, и его отображение на предпросмотре.

## Архитектура и тесты

- Core на `net8.0` без ссылок на WPF и ViewModels. Таблицы, графы, валидация, DIP, подготовка CSV/Markdown и разрешение встроенных определений работают в Core.
- Модель событийной симуляции и изолированный стенд находятся в Simulation. App отвечает за ViewModels, диалоги, визуализацию и PNG. Code-behind новых представлений содержит только `InitializeComponent`.
- JSON и файлы проходят через репозитории Core/Infrastructure. Сервисы приложения регистрируются через интерфейсы. В изменённых исходных файлах максимум 300 строк.
- `dotnet build CircuitEditor.sln` прошёл с 0 ошибок и предупреждений. `dotnet test CircuitEditor.sln` — 488 из 488. В составе тестов прошли отрисовка WPF и декодирование экспортированного PNG. Ручные шаги выше предназначены для проверки в запущенном окне.
- Шаг C#-скриптов ожидает отдельного подтверждения.

## Дерево изменённых файлов

```text
.
├─ AGENTS.md
├─ CircuitEditor.App/
│  ├─ Controls/
│  │  ├─ ChipFootprintPreview.cs
│  │  ├─ ChipGraphPreview.cs
│  │  ├─ ChipPackageDrawing.cs
│  │  └─ RealComponentDrawing.cs
│  ├─ Program.cs
│  ├─ Resources/
│  │  └─ Navigation.xaml
│  ├─ Services/
│  │  ├─ ChipPinoutRenderer.cs
│  │  ├─ DialogService.cs
│  │  ├─ IChipPinoutRenderer.cs
│  │  ├─ IDialogService.cs
│  │  ├─ IProjectChipVersionSelector.cs
│  │  ├─ IUserChipLibraryService.cs
│  │  ├─ ProjectChipVersionSelector.cs
│  │  ├─ SimulationRegistration.cs
│  │  └─ UserChipLibraryService.cs
│  ├─ ViewModels/
│  │  ├─ BreadboardViewModel.cs
│  │  ├─ ChipBenchPinViewModel.cs
│  │  ├─ ChipBenchViewModel.cs
│  │  ├─ ChipDocumentationViewModel.cs
│  │  ├─ ChipDraftHistory.cs
│  │  ├─ ChipEditorDefaults.cs
│  │  ├─ ChipEditorText.cs
│  │  ├─ ChipGraphEditorViewModel.cs
│  │  ├─ ChipLibraryViewModel.cs
│  │  ├─ ChipPinEdit.cs
│  │  ├─ ChipTruthEditorViewModel.cs
│  │  ├─ ChipTruthRowEdit.cs
│  │  ├─ ChipViewModel.cs
│  │  ├─ MainViewModel.cs
│  │  ├─ ProjectSessionViewModel.cs
│  │  ├─ ProjectViewModel.cs
│  │  └─ SchematicViewModel.cs
│  └─ Views/
│     ├─ ChipBenchView.xaml
│     ├─ ChipBenchView.xaml.cs
│     ├─ ChipGraphView.xaml
│     ├─ ChipGraphView.xaml.cs
│     ├─ ChipLibraryView.xaml
│     ├─ ChipLibraryView.xaml.cs
│     ├─ ChipTruthView.xaml
│     ├─ ChipTruthView.xaml.cs
│     ├─ ChipView.xaml
│     └─ ChipView.xaml.cs
├─ CircuitEditor.Core/
│  ├─ Chips/
│  │  ├─ ChipBehaviorDefinition.cs
│  │  ├─ ChipContent.cs
│  │  ├─ ChipDefinition.cs
│  │  ├─ ChipEvaluation.cs
│  │  ├─ ChipEvaluator.cs
│  │  ├─ ChipFileDocument.cs
│  │  ├─ ChipGate.cs
│  │  ├─ ChipGateKind.cs
│  │  ├─ ChipGateLogic.cs
│  │  ├─ ChipGraph.cs
│  │  ├─ ChipGraphValidation.cs
│  │  ├─ ChipLevel.cs
│  │  ├─ ChipMarkdown.cs
│  │  ├─ ChipOutputLink.cs
│  │  ├─ ChipPackage.cs
│  │  ├─ ChipPin.cs
│  │  ├─ ChipPinType.cs
│  │  ├─ ChipSignal.cs
│  │  ├─ ChipState.cs
│  │  ├─ ChipTruthCsv.cs
│  │  ├─ ChipTruthLogic.cs
│  │  ├─ ChipTruthSection.cs
│  │  ├─ ChipTruthTable.cs
│  │  ├─ ChipValidator.cs
│  │  ├─ IChipEvaluator.cs
│  │  ├─ LogicFamilyProfile.cs
│  │  └─ LogicVoltage.cs
│  ├─ Documents/
│  │  ├─ ComponentData.cs
│  │  ├─ ProjectChipDefinitions.cs
│  │  ├─ ProjectDocument.cs
│  │  └─ ProjectDocumentResolver.cs
│  ├─ History/
│  │  ├─ ProjectSnapshot.cs
│  │  └─ SelectionClipboard.cs
│  ├─ Models/
│  │  └─ CircuitProject.cs
│  ├─ Repositories/
│  │  ├─ IChipCsvRepository.cs
│  │  ├─ IChipMigration.cs
│  │  ├─ IChipRepository.cs
│  │  ├─ IChipTemplateRepository.cs
│  │  └─ ILogicFamilyRepository.cs
│  ├─ Services/
│  │  └─ ComponentInsertionService.cs
│  └─ Validation/
│     └─ ChipSupplyRule.cs
├─ CircuitEditor.Infrastructure/
│  ├─ CircuitEditor.Infrastructure.csproj
│  ├─ Data/
│  │  ├─ Chips/
│  │  │  ├─ 7400.json
│  │  │  ├─ 7402.json
│  │  │  ├─ 7404.json
│  │  │  ├─ 7408.json
│  │  │  ├─ 7432.json
│  │  │  ├─ 7474.json
│  │  │  ├─ 7486.json
│  │  │  ├─ 74hc595.json
│  │  │  ├─ counter-4.json
│  │  │  ├─ decoder-2.json
│  │  │  ├─ full-adder.json
│  │  │  └─ mux-4.json
│  │  ├─ error-help.json
│  │  └─ logic-families.json
│  ├─ Documents/
│  │  └─ ProjectMigrationV3ToV4.cs
│  └─ Repositories/
│     ├─ FileChipCsvRepository.cs
│     ├─ JsonChipRepository.cs
│     ├─ JsonChipTemplateRepository.cs
│     └─ JsonLogicFamilyRepository.cs
├─ CircuitEditor.Simulation/
│  ├─ Components/
│  │  ├─ BehaviorContext.cs
│  │  ├─ ChipBehavior.cs
│  │  ├─ ChipRuntime.cs
│  │  └─ IComponentBehavior.cs
│  ├─ Engine/
│  │  ├─ SimulationCircuit.cs
│  │  └─ SimulationSession.cs
│  └─ Instruments/
│     ├─ ChipBenchVerification.cs
│     ├─ ChipTestBench.cs
│     ├─ ChipTestBenchFactory.cs
│     └─ IChipTestBenchFactory.cs
├─ CircuitEditor.Tests/
│  ├─ ChipDocumentationTests.cs
│  ├─ ChipEditorTests.cs
│  ├─ ChipGraphTests.cs
│  ├─ ChipLibraryTests.cs
│  ├─ ChipPresentationChecks.cs
│  ├─ ChipSimulationTests.cs
│  ├─ ChipTemplateTests.cs
│  ├─ ChipTruthTableTests.cs
│  ├─ ProjectChipStorageTests.cs
│  ├─ ProjectFileTests.cs
│  ├─ SimulationStorageTests.cs
│  ├─ SketchStorageTests.cs
│  ├─ Stage4TestEnvironment.cs
│  ├─ TestChipMigration.cs
│  ├─ TestProjectAutosave.cs
│  └─ ThemeResourceTests.cs
├─ docs/
│  └─ stage-8-chips.md
└─ README.md
```
