# Этап 7. Симуляция

В «Макете» работают фоновый сеанс, ручное управление GPIO Uno/Nano/ESP32, шесть встроенных сценариев, визуальные реакции деталей, три прибора и Serial. Проблемы сеанса доступны в общем списке со временем, кодом и статьёй справки.

## Ручная проверка

1. Запустите `dotnet run --project CircuitEditor.App`. Откройте «Макет», добавьте Uno, резистор 330 Ω и LED. Соедините D13 → резистор → анод LED. Соедините катод с GND. Выйдите из режима перемычек клавишей Esc.
2. Нажмите «Сценарий и GPIO» и выберите «Мигающий светодиод». Назначьте `U1.D13` как `LED_PIN`, период 1000 мс, долю включения 0,5. Нажмите «Старт». LED загорается на полпериода. «Пауза» замораживает время, «Шаг» продвигает его на 1 мс. «Сброс» возвращает обычный вид и редактирование.
3. Для проверки ручного ШИМ подключите LED через резистор к D9. Выберите «Без сценария», запустите сеанс и откройте «Контроллер». Выберите D9, режим `Pwm`, скважность 25% и нажмите «Применить режим». Повторите с 75%. Яркость и среднее напряжение растут. Включите «Показать ток» для анимации перемычек.
4. В «Симуляции» включите осциллограф и логический анализатор. На панели прибора выберите канал, нажмите «Зонд» и щёлкните по D9 или подключённому отверстию. Настройте время и напряжение на деление. Осциллограф показывает мгновенное напряжение, логический анализатор показывает уровни. Заголовок прибора можно перетащить. В мультиметре установите красный и чёрный зонды. Для измерения тока используйте концы одной перемычки или выводы одной поддерживаемой ветви.
5. Для LCD I2C подайте питание на VCC/GND, выберите «Текст на LCD/OLED» и укажите дисплей. Для Serial выберите «Печать в Serial», задайте период и проверьте нижнюю вкладку «Вывод».
6. Для проверки диагностики соедините D9 и D10 и в ручном режиме задайте двум выходам разные уровни. В «Проблемах» выберите источник «Симуляция». Появятся конфликт уровней и предупреждения или ошибки по току. При «Сбросе» диагностика сеанса исчезает.
7. Сохраните .circ, затем откройте его. Сценарий и положения приборов возвращаются. Сеанс запускается отдельно. Файл v2 со скетчем открывается через миграцию, формат v3 записывается при сохранении.

## Точность моделей

| Детали | Реализация |
|---|---|
| Uno, Nano, ESP32 | GPIO и АЦП по профилям плат, выходы с последовательным сопротивлением, режимы `Input`, `Output`, `InputPullup`, `Pwm`. Профили токов учебные. |
| Резистор, потенциометр, LDR, NTC, батарея | DC-модели. Номинал, ручка, температура и освещённость влияют на результат. |
| LED, RGB, диод, семисегментный индикатор | Кусочно-линейные переходы. Яркость LED пропорциональна среднему току, сегменты отображаются по току ветвей. |
| Кнопка, переключатель, реле | Идеальные контакты. Катушка реле имеет сопротивление и разные пороги включения и выключения. Паразитные эффекты и дребезг отсутствуют. |
| Зуммер и серво | Зуммер отмечает активность визуально. Серво отображает положение по ширине ШИМ-импульса с ограниченной скоростью перемещения. |
| LCD 16×2, LCD I2C, OLED | Сценарий записывает строки в буфер выбранного запитанного дисплея. |
| DHT11/22, PIR, HC-SR04 | DHT отдаёт свойства температуры и влажности, PIR формирует цифровой выход, HC-SR04 формирует импульс ECHO по длительности триггера и расстоянию. |
| NPN | Учебная ключевая модель с порогом базы, ограничением коллекторного тока по коэффициенту усиления и насыщением. |
| Конденсатор и индуктивность | Временной модели нет. Сеанс выдаёт `SIM_UNSUPPORTED`. |

В сценариях доступны мигание, кнопка → LED, потенциометр → ШИМ, качание серво, строки на LCD/OLED, периодический Serial со значениями выбранного датчика. Вводы и выходы используют стабильные ID платы и пина. Удаление платы или вывода оставляет привязку видимой для исправления и блокирует запуск.

Arduino C++ из вкладки «Код» не исполняется. Возможное продолжение отдельного этапа — интерпретатор заранее определённого подмножества Arduino API, где каждая операция добавляет событие на `ISimulationEngine` через виртуальные часы. Побитовые I2C/DHT, аудио зуммера и аналоговая динамика C/L находятся за границами учебной модели.

UI использует последний неизменяемый снимок, при отставании кадры пропускаются. Событийная очередь сортируется по модельному времени и порядковому номеру. Периодические ШИМ-фазы читаются из кэшированных DC-решений. При слишком большом общем цикле частот выдаётся диагностика приближения. Для дозированного кадра макета максимум 120 частиц. Ток в замкнутом контуре идеальных перемычек может быть неоднозначен. Осциллограф и логический анализатор хранят до 4096 отсчётов с шагом 100 мкс, поэтому импульсы короче шага могут пройти между отсчётами. Для двух зондов напряжение определено в одном электрическом острове.

## Формат и архитектура

- `.circ` v3 сохраняет `simulationSetup.scenario` с ID шаблона, параметрами и привязками. `simulationSetup.instruments` хранит координаты и видимость панелей. Миграция 2→3 добавляет пустые настройки и сохраняет скетч. Цепочка 1→2→3 также поддерживается.
- Снимки сеанса, логические уровни, текущие положения элементов, зонды, осциллограммы, Serial и диагностика живут в памяти. Действия в запущенном сеансе не входят в историю схемы. Свойства компонентов вне сеанса идут через обычные команды редактирования.
- Core и Simulation без ссылок на WPF. Репозитории и миграции находятся в Infrastructure. Интерфейсы часов, движка, сценариев, приборов и поведения компонентов связаны через DI. ViewModel получает снимки через сервисы. Новые компоненты WPF используют разметку, адаптеры ввода и отрисовку, а их code-behind только вызывает InitializeComponent.
- Проверены порядок событий, независимость от частоты UI-снимков, отсутствие DC-решений на фронтах ШИМ, реле с обратной связью, отказ при недействительной привязке, диагностика тока, предел памяти приборов, WPF-отрисовка, сохранение v3 и миграции старых файлов.

## Проверка

В Debug выполнены `dotnet build CircuitEditor.sln` и `dotnet test CircuitEditor.sln`. Сборка завершилась с 0 ошибок и 0 предупреждений. Пройдено 432 теста, пропущенных нет. Проверка WPF запускает отрисовку панелей и приборов; производительность на конкретном ПК можно проверить переключением скорости и наблюдением за временем.

Исходные C# и XAML укладываются в лимит 300 строк. Максимум 285 строк у общего словаря стилей. В Core и Simulation отсутствуют ссылки на WPF; ViewModel обращается к хранилищам через интерфейсы. Законченные шаги сохранены отдельными локальными коммитами.

## Дерево изменённых файлов этапа

```text
.
├── AGENTS.md
├── CircuitEditor.App/
│   ├── Controls/
│   │   ├── BreadboardSurface.cs
│   │   ├── InstrumentTrace.cs
│   │   ├── MiscComponentDrawing.cs
│   │   ├── PotentiometerDrawing.cs
│   │   ├── RealComponentDrawing.cs
│   │   ├── ServoDrawing.cs
│   │   ├── SevenSegmentDrawing.cs
│   │   ├── SimulationDrawing.cs
│   │   └── WireDrawing.cs
│   ├── Interaction/
│   │   └── InstrumentDragBehavior.cs
│   ├── Program.cs
│   ├── Resources/
│   │   └── Styles.xaml
│   ├── Services/
│   │   ├── ISimulationPlayback.cs
│   │   ├── ISimulationUiPulse.cs
│   │   ├── SimulationPlayback.cs
│   │   ├── SimulationPlaybackRun.cs
│   │   ├── SimulationRegistration.cs
│   │   └── SimulationUiPulse.cs
│   ├── ViewModels/
│   │   ├── BreadboardViewModel.cs
│   │   ├── DiagnosticsViewModel.cs
│   │   ├── InstrumentMove.cs
│   │   ├── InstrumentViewModel.cs
│   │   ├── MainViewModel.cs
│   │   ├── MockupSimulationInput.cs
│   │   ├── OutputViewModel.cs
│   │   ├── ProjectViewModel.cs
│   │   ├── ScenarioPinViewModel.cs
│   │   ├── SchematicViewModel.cs
│   │   ├── SimulationControllerViewModel.cs
│   │   ├── SimulationInstrumentsViewModel.cs
│   │   ├── SimulationNumberViewModel.cs
│   │   ├── SimulationPinOption.cs
│   │   ├── SimulationScenarioViewModel.cs
│   │   ├── SimulationTargetOption.cs
│   │   ├── SimulationViewModel.cs
│   │   └── WireEditorViewModel.cs
│   └── Views/
│       ├── AnalysisView.xaml
│       ├── BreadboardView.xaml
│       ├── EditorLayoutView.xaml
│       ├── InstrumentDeckView.xaml
│       ├── InstrumentDeckView.xaml.cs
│       ├── InstrumentPanelView.xaml
│       ├── InstrumentPanelView.xaml.cs
│       ├── SimulationInspectorView.xaml
│       ├── SimulationInspectorView.xaml.cs
│       ├── SimulationToolbarView.xaml
│       └── SimulationToolbarView.xaml.cs
├── CircuitEditor.Core/
│   ├── Documents/
│   │   ├── ProjectDocument.cs
│   │   └── ProjectDocumentResolver.cs
│   ├── Electronics/
│   │   ├── DcBranch.cs
│   │   ├── DcBranchResult.cs
│   │   ├── DcCircuitAssembler.cs
│   │   ├── DcIslandSolver.cs
│   │   ├── DcMatrixCache.cs
│   │   ├── DcResult.cs
│   │   ├── DcSolver.cs
│   │   └── IDcSolver.cs
│   ├── History/
│   │   └── ProjectSnapshot.cs
│   ├── Models/
│   │   └── CircuitProject.cs
│   ├── Netlist/
│   │   └── Netlist.cs
│   ├── Repositories/
│   │   ├── IScenarioRepository.cs
│   │   └── ISimulationModelRepository.cs
│   ├── Simulation/
│   │   ├── ComponentSignal.cs
│   │   ├── InstrumentKind.cs
│   │   ├── InstrumentLayout.cs
│   │   ├── InstrumentSignal.cs
│   │   ├── PinSignal.cs
│   │   ├── ScenarioConfiguration.cs
│   │   ├── ScenarioTemplate.cs
│   │   ├── SerialLine.cs
│   │   ├── SimulationBoardProfile.cs
│   │   ├── SimulationDiagnostic.cs
│   │   ├── SimulationModelCatalog.cs
│   │   ├── SimulationSetup.cs
│   │   ├── SimulationState.cs
│   │   ├── TraceSample.cs
│   │   └── WireSignal.cs
│   └── Validation/
│       └── PinDirectionRule.cs
├── CircuitEditor.Infrastructure/
│   ├── Data/
│   │   ├── error-help.json
│   │   ├── scenarios.json
│   │   └── simulation-models.json
│   ├── Documents/
│   │   └── ProjectMigrationV2ToV3.cs
│   └── Repositories/
│       ├── JsonElectricalLibraryRepository.cs
│       ├── JsonScenarioRepository.cs
│       └── JsonSimulationModelRepository.cs
├── CircuitEditor.Simulation/
│   ├── Components/
│   │   ├── BehaviorContext.cs
│   │   ├── ButtonBehavior.cs
│   │   ├── BuzzerBehavior.cs
│   │   ├── ComponentMemory.cs
│   │   ├── Dht11Behavior.cs
│   │   ├── Dht22Behavior.cs
│   │   ├── DiodeBehavior.cs
│   │   ├── DisplayBehavior.cs
│   │   ├── I2cLcdBehavior.cs
│   │   ├── IComponentBehavior.cs
│   │   ├── LcdBehavior.cs
│   │   ├── LdrBehavior.cs
│   │   ├── LedBehavior.cs
│   │   ├── NpnBehavior.cs
│   │   ├── NtcBehavior.cs
│   │   ├── OledBehavior.cs
│   │   ├── PirBehavior.cs
│   │   ├── PotentiometerBehavior.cs
│   │   ├── RelayBehavior.cs
│   │   ├── RgbLedBehavior.cs
│   │   ├── SensorBehavior.cs
│   │   ├── ServoBehavior.cs
│   │   ├── SevenSegmentBehavior.cs
│   │   ├── SwitchBehavior.cs
│   │   └── UltrasonicBehavior.cs
│   ├── Engine/
│   │   ├── ISimulationDiagnostics.cs
│   │   ├── ISimulationEngine.cs
│   │   ├── ISimulationSessionFactory.cs
│   │   ├── PinMeasurements.cs
│   │   ├── PwmAveraging.cs
│   │   ├── PwmPhase.cs
│   │   ├── PwmPhasePlan.cs
│   │   ├── SimulationCircuit.cs
│   │   ├── SimulationDiagnostics.cs
│   │   ├── SimulationEngine.cs
│   │   ├── SimulationSession.cs
│   │   ├── SimulationSessionFactory.cs
│   │   └── WireMeasurements.cs
│   ├── Gpio/
│   │   ├── DigitalLevel.cs
│   │   ├── GpioController.cs
│   │   ├── GpioMode.cs
│   │   └── GpioState.cs
│   ├── Instruments/
│   │   ├── IInstrument.cs
│   │   ├── InstrumentChannels.cs
│   │   ├── InstrumentContext.cs
│   │   ├── LogicAnalyzer.cs
│   │   ├── Multimeter.cs
│   │   ├── Oscilloscope.cs
│   │   ├── SessionInstruments.cs
│   │   └── TraceHistory.cs
│   ├── Scenarios/
│   │   ├── IScenarioRunner.cs
│   │   └── ScenarioRunner.cs
│   └── Timing/
│       ├── ISimulationClock.cs
│       ├── SimulationClock.cs
│       └── SimulationEventQueue.cs
├── CircuitEditor.Tests/
│   ├── DiagnosticsPresentationChecks.cs
│   ├── ProjectFileTests.cs
│   ├── ProjectSerializationTests.cs
│   ├── SimulationClockTests.cs
│   ├── SimulationCompositionTests.cs
│   ├── SimulationDeviceTests.cs
│   ├── SimulationGpioTests.cs
│   ├── SimulationInstrumentTests.cs
│   ├── SimulationLifecycleTests.cs
│   ├── SimulationPresentationChecks.cs
│   ├── SimulationScenarioTests.cs
│   ├── SimulationStorageTests.cs
│   ├── SketchStorageTests.cs
│   ├── Stage4TestEnvironment.cs
│   ├── TestSimulationPulse.cs
│   └── ThemeResourceTests.cs
├── docs/
│   └── stage-7-simulation.md
└── README.md
```
