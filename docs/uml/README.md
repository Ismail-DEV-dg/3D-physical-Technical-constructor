# UML и диаграммы работы

Классы читаются из C# через Roslyn, зависимости — из ProjectReference в .csproj. Отдельный генератор не входит в CircuitEditor.sln. Частичные классы объединяются, в одной диаграмме не более 20 типов. Отображаются имена основных публичных членов и связи, обнаруженные по декларациям. Генерация не разворачивает тело методов, XAML-привязки, код генераторов и связи с типами на соседних страницах. Сценарии и переходы состояний сверены по реализации и поддерживаются отдельно.

| Диаграмма | Содержание | Изображения |
|---|---|---|
| [activity-chip](activity-chip.mmd) | Выбор корпуса, поведение, проверка и установка микросхемы. | [SVG](images/activity-chip.svg) · [PNG](images/activity-chip.png) |
| [sequence-erc](sequence-erc.mmd) | От изменения проекта и netlist до списка диагностик. | [SVG](images/sequence-erc.svg) · [PNG](images/sequence-erc.png) |
| [sequence-open](sequence-open.mmd) | Чтение .circ, миграции и проверка перед заменой проекта. | [SVG](images/sequence-open.svg) · [PNG](images/sequence-open.png) |
| [sequence-simulation](sequence-simulation.mmd) | Виртуальные часы, шаг и публикация снимка. | [SVG](images/sequence-simulation.svg) · [PNG](images/sequence-simulation.png) |
| [sequence-sketch](sequence-sketch.mmd) | Обновление секций auto по netlist и шаблонам. | [SVG](images/sequence-sketch.svg) · [PNG](images/sequence-sketch.png) |
| [state-simulation](state-simulation.mmd) | Запуск, пауза, шаг, сброс и ошибка сеанса. | [SVG](images/state-simulation.svg) · [PNG](images/state-simulation.png) |
| [use-cases](use-cases.mmd) | Действия пользователя от монтажа до экспорта. | [SVG](images/use-cases.svg) · [PNG](images/use-cases.png) |
| [00-projects](generated/00-projects.mmd) | Проекты и реальные ссылки ProjectReference. | [SVG](images/00-projects.svg) · [PNG](images/00-projects.png) |
| [01-project-model](generated/01-project-model.mmd) | CircuitProject, компоненты, размещения, breadboard и электрические узлы. | [SVG](images/01-project-model.svg) · [PNG](images/01-project-model.png) |
| [3d-app-01](generated/3d-app-01.mmd) | 20 типов: BoardMeshes, ControllerMeshes, DeviceMeshes, DisplayMeshes и другие. | [SVG](images/3d-app-01.svg) · [PNG](images/3d-app-01.png) |
| [3d-app-02](generated/3d-app-02.mmd) | 13 типов: SceneMeshPart, SceneMeshes, SceneObj, ScenePng и другие. | [SVG](images/3d-app-02.svg) · [PNG](images/3d-app-02.png) |
| [3d-core-01](generated/3d-core-01.mmd) | 18 типов: ISceneBuilder, SceneBounds, SceneBuilder, SceneCatalog и другие. | [SVG](images/3d-core-01.svg) · [PNG](images/3d-core-01.png) |
| [calculations-core-01](generated/calculations-core-01.mmd) | 20 типов: BandResistance, CalculationResult, ComponentElectricalState, DcAssembly и другие. | [SVG](images/calculations-core-01.svg) · [PNG](images/calculations-core-01.png) |
| [calculations-core-02](generated/calculations-core-02.mmd) | 6 типов: OhmCalculator, OhmValues, PowerSource, PowerSourceDefinition и другие. | [SVG](images/calculations-core-02.svg) · [PNG](images/calculations-core-02.png) |
| [chips-core-01](generated/chips-core-01.mmd) | 20 типов: ChipBehaviorDefinition, ChipContent, ChipDefinition, ChipEvaluation и другие. | [SVG](images/chips-core-01.svg) · [PNG](images/chips-core-01.png) |
| [chips-core-02](generated/chips-core-02.mmd) | 7 типов: ChipTruthLogic, ChipTruthSection, ChipTruthTable, ChipValidator и другие. | [SVG](images/chips-core-02.svg) · [PNG](images/chips-core-02.png) |
| [code-core-01](generated/code-core-01.mmd) | 20 типов: ArduinoCodeAnalyzer, ArduinoCodeGenerator, AutoRegionInitializer, AutoRegionUpdater и другие. | [SVG](images/code-core-01.svg) · [PNG](images/code-core-01.png) |
| [code-core-02](generated/code-core-02.mmd) | 6 типов: CppTokenScanner, ICodeAnalyzer, ICodeGenerator, PinUsageCollector и другие. | [SVG](images/code-core-02.svg) · [PNG](images/code-core-02.png) |
| [model-core-01](generated/model-core-01.mmd) | 20 типов: BreadboardDefinition, BreadboardGeometry, BreadboardHole, BreadboardInstance и другие. | [SVG](images/model-core-01.svg) · [PNG](images/model-core-01.png) |
| [model-core-02](generated/model-core-02.mmd) | 20 типов: PinLayout, SchematicPinGeometry, TextExtent, CapacitorKind и другие. | [SVG](images/model-core-02.svg) · [PNG](images/model-core-02.png) |
| [model-core-03](generated/model-core-03.mmd) | 9 типов: RotaryControlDefinition, ComponentPlacement, RepresentationKind, ViewLayout и другие. | [SVG](images/model-core-03.svg) · [PNG](images/model-core-03.png) |
| [netlist-erc-core-01](generated/netlist-erc-core-01.mmd) | 20 типов: INetlistBuilder, Net, NetTerminal, Netlist и другие. | [SVG](images/netlist-erc-core-01.svg) · [PNG](images/netlist-erc-core-01.png) |
| [netlist-erc-core-02](generated/netlist-erc-core-02.mmd) | 3 типов: RequiredPinRule, SourceConflictRule, SupplyShortRule | [SVG](images/netlist-erc-core-02.svg) · [PNG](images/netlist-erc-core-02.png) |
| [repositories-formats-core-01](generated/repositories-formats-core-01.mmd) | 20 типов: BreadboardData, ComponentData, IProjectMigration, ProjectChipDefinitions и другие. | [SVG](images/repositories-formats-core-01.svg) · [PNG](images/repositories-formats-core-01.png) |
| [repositories-formats-core-02](generated/repositories-formats-core-02.mmd) | 13 типов: IFootprintRepository, IGuideRepository, IInstallationLicenseRepository, ILogicFamilyRepository и другие. | [SVG](images/repositories-formats-core-02.svg) · [PNG](images/repositories-formats-core-02.png) |
| [repositories-formats-infrastructure-01](generated/repositories-formats-infrastructure-01.mmd) | 20 типов: BreadboardData, ColumnData, FootprintCatalogData, RailData и другие. | [SVG](images/repositories-formats-infrastructure-01.svg) · [PNG](images/repositories-formats-infrastructure-01.png) |
| [repositories-formats-infrastructure-02](generated/repositories-formats-infrastructure-02.mmd) | 16 типов: JsonChipRepository, JsonChipTemplateRepository, JsonCodeCompletionRepository, JsonCodeTemplateRepository и другие. | [SVG](images/repositories-formats-infrastructure-02.svg) · [PNG](images/repositories-formats-infrastructure-02.png) |
| [simulation-core-01](generated/simulation-core-01.mmd) | 15 типов: ComponentSignal, InstrumentKind, InstrumentLayout, InstrumentSignal и другие. | [SVG](images/simulation-core-01.svg) · [PNG](images/simulation-core-01.png) |
| [simulation-simulation-01](generated/simulation-simulation-01.mmd) | 20 типов: BehaviorContext, ButtonBehavior, BuzzerBehavior, ChipBehavior и другие. | [SVG](images/simulation-simulation-01.svg) · [PNG](images/simulation-simulation-01.png) |
| [simulation-simulation-02](generated/simulation-simulation-02.mmd) | 20 типов: RelayBehavior, RgbLedBehavior, SensorBehavior, ServoBehavior и другие. | [SVG](images/simulation-simulation-02.svg) · [PNG](images/simulation-simulation-02.png) |
| [simulation-simulation-03](generated/simulation-simulation-03.mmd) | 20 типов: DigitalLevel, GpioController, GpioMode, GpioState и другие. | [SVG](images/simulation-simulation-03.svg) · [PNG](images/simulation-simulation-03.png) |
| [simulation-simulation-04](generated/simulation-simulation-04.mmd) | 13 типов: SketchExpression, SketchExpressionKind, SketchExpressionParser, SketchInstruction и другие. | [SVG](images/simulation-simulation-04.svg) · [PNG](images/simulation-simulation-04.png) |
| [support-app-01](generated/support-app-01.mmd) | 20 типов: App, PaneLimitConverter, PaneSizeConverter, MainWindow и другие. | [SVG](images/support-app-01.svg) · [PNG](images/support-app-01.png) |
| [support-app-02](generated/support-app-02.mmd) | 20 типов: IClipboardService, ICodeAnalysisService, IDebounceService, IDialogService и другие. | [SVG](images/support-app-02.svg) · [PNG](images/support-app-02.png) |
| [support-app-03](generated/support-app-03.mmd) | 16 типов: ImageExportService, NavigationService, ProjectChipVersionSelector, SaveDecision и другие. | [SVG](images/support-app-03.svg) · [PNG](images/support-app-03.png) |
| [support-core-01](generated/support-core-01.mmd) | 19 типов: DiagnosticArticle, GuideBoard, GuideChapter, GuideLibrary и другие. | [SVG](images/support-core-01.svg) · [PNG](images/support-core-01.png) |
| [tests-tests-01](generated/tests-tests-01.mmd) | 20 типов: AnalysisViewModelTests, BatchAFootprintTests, BatchAPresentationChecks, BatchBFootprintTests и другие. | [SVG](images/tests-tests-01.svg) · [PNG](images/tests-tests-01.png) |
| [tests-tests-02](generated/tests-tests-02.mmd) | 20 типов: CircuitModelTests, CodeAnalyzerTests, CodeEditorPresentationChecks, CodeGeneratorTests и другие. | [SVG](images/tests-tests-02.svg) · [PNG](images/tests-tests-02.png) |
| [tests-tests-03](generated/tests-tests-03.mmd) | 20 типов: GuidePresentationChecks, GuideTests, LayoutAssertions, MainViewModelTests и другие. | [SVG](images/tests-tests-03.svg) · [PNG](images/tests-tests-03.png) |
| [tests-tests-04](generated/tests-tests-04.mmd) | 20 типов: SceneBuilderTests, SceneCacheTests, SceneExportTests, SceneInteractionTests и другие. | [SVG](images/tests-tests-04.svg) · [PNG](images/tests-tests-04.png) |
| [tests-tests-05](generated/tests-tests-05.mmd) | 20 типов: SketchStorageTests, SmoothScrollPresentationChecks, StaRunner, Stage4HistoryTests и другие. | [SVG](images/tests-tests-05.svg) · [PNG](images/tests-tests-05.png) |
| [tests-tests-06](generated/tests-tests-06.mmd) | 7 типов: ThemeResourceTests, ThemeServiceTests, UnoAndHoverPresentationChecks, WireBendInsertionTests и другие. | [SVG](images/tests-tests-06.svg) · [PNG](images/tests-tests-06.png) |
| [views-viewmodels-app-01](generated/views-viewmodels-app-01.mmd) | 20 типов: ActuatorBodyDrawing, AnalysisDrawing, ArduinoNanoDrawing, ArduinoUnoDrawing и другие. | [SVG](images/views-viewmodels-app-01.svg) · [PNG](images/views-viewmodels-app-01.png) |
| [views-viewmodels-app-02](generated/views-viewmodels-app-02.mmd) | 20 типов: MiscComponentDrawing, NpnDrawing, OledDrawing, PassiveBodyDrawing и другие. | [SVG](images/views-viewmodels-app-02.svg) · [PNG](images/views-viewmodels-app-02.png) |
| [views-viewmodels-app-03](generated/views-viewmodels-app-03.mmd) | 20 типов: VoltageDrawing, WireDrawing, WireDrawingCache, CanvasAction и другие. | [SVG](images/views-viewmodels-app-03.svg) · [PNG](images/views-viewmodels-app-03.png) |
| [views-viewmodels-app-04](generated/views-viewmodels-app-04.mmd) | 20 типов: ScrollMotionState, SelectionRefreshBehavior, ShortcutBinding, ShortcutCatalog и другие. | [SVG](images/views-viewmodels-app-04.svg) · [PNG](images/views-viewmodels-app-04.png) |
| [views-viewmodels-app-05](generated/views-viewmodels-app-05.mmd) | 20 типов: ChipPinEdit, ChipTruthEditorViewModel, ChipTruthRowEdit, ChipViewModel и другие. | [SVG](images/views-viewmodels-app-05.svg) · [PNG](images/views-viewmodels-app-05.png) |
| [views-viewmodels-app-06](generated/views-viewmodels-app-06.mmd) | 20 типов: InputChoiceViewModel, InstrumentMove, InstrumentViewModel, MainViewModel и другие. | [SVG](images/views-viewmodels-app-06.svg) · [PNG](images/views-viewmodels-app-06.png) |
| [views-viewmodels-app-07](generated/views-viewmodels-app-07.mmd) | 20 типов: SceneCameraView, SchematicViewModel, ShortcutGuideViewModel, SimulationControllerViewModel и другие. | [SVG](images/views-viewmodels-app-07.svg) · [PNG](images/views-viewmodels-app-07.png) |
| [views-viewmodels-app-08](generated/views-viewmodels-app-08.mmd) | 20 типов: ChipBenchView, ChipGraphView, ChipLibraryView, ChipTruthView и другие. | [SVG](images/views-viewmodels-app-08.svg) · [PNG](images/views-viewmodels-app-08.png) |
| [views-viewmodels-app-09](generated/views-viewmodels-app-09.mmd) | 6 типов: SchematicView, ShortcutGuideWindow, SimulationInspectorView, SimulationToolbarView и другие. | [SVG](images/views-viewmodels-app-09.svg) · [PNG](images/views-viewmodels-app-09.png) |

## Проверка и регенерация

Из корня репозитория:

```powershell
dotnet run --project tools/UmlGenerator -- .
dotnet run --project tools/UmlGenerator -- . --check
```

Режим --check сообщает о несовпадении и возвращает 1, если исходники и автогенерированные .mmd расходятся.

Для экспорта нужен Node.js и браузер, который устанавливает Mermaid CLI. Следующий проход проверяет синтаксис и создаёт оба формата:

```powershell
$inputFiles = Get-ChildItem docs/uml -Recurse -Filter *.mmd
foreach ($file in $inputFiles) {
    foreach ($ext in 'svg', 'png') {
        npx --yes @mermaid-js/mermaid-cli@12.2.0 -q -i $file.FullName -o "docs/uml/images/$($file.BaseName).$ext"
        if ($LASTEXITCODE -ne 0) { throw "Ошибка Mermaid в $($file.Name)" }
    }
}
```
