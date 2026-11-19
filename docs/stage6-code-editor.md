# Этап 6 — встроенный редактор кода

Реализован по шести согласованным шагам. Коммиты не создавались по последнему указанию пользователя.

## Редактор и команды

- AvalonEdit 6.3.1.120 только в UI-слое; собственная Arduino.xshd с адаптацией Neon Dark / Light Tech, JetBrains Mono, номера строк, текущая строка, оформление выделения, курсора, маркеров folding и дополнения.
- Сворачивание по скобкам с исключением комментариев/литералов; пересчёт с задержкой в фоне. Автоотступы CSharpIndentationStrategy, парные (), [], {}, кавычки, оборачивание выделенного текста и удаление пары Backspace.
- Ctrl+F — буквальный поиск, Ctrl+H — замена, Ctrl+G — строка, Ctrl+Space — дополнение, Ctrl+колесо — шрифт 10–26 px. Enter в полях поиска/замены/строки выполняет соответствующую команду. Esc закрывает поиск/дополнение.
- Поиск учитывает регистр по переключателю; поддержаны следующая позиция, циклический переход и замена всех совпадений одним действием Undo. Регулярные выражения не заявлены.
- Undo/Redo AvalonEdit независимы от схемы, включая команды меню. TextDocument и позиция курсора сохраняются между переключениями вкладок; новый/открытый проект получает свежий документ и пустую текстовую историю. В строке состояния показаны строка и столбец.
- Дополнение: API из arduino-api.json, имена компонентов/пинов текущего проекта и #define скетча. Предложение имени не создаёт объявление переменной: заготовки/ручной код должны его объявлять.

## Генерация

Выбираются Uno / Nano / ESP32 и при необходимости конкретный экземпляр платы. «Авто» работает, только если экземпляр выбранного семейства единственный. Подключения берутся из Netlist, включая внутренние группы breadboard. Для LED разрешён путь через последовательные резисторы. Неоднозначное назначение GPIO не угадывается.

| Компонент/случай | Что создаётся |
|---|---|
| LED | #define, OUTPUT; для единственного LED с резисторами — демонстрационный Blink на millis без delay |
| Кнопка | #define, INPUT_PULLUP при известном подключении к земле, иначе INPUT с предупреждением о подтяжке |
| LED + кнопка | Определения и режимы обоих пинов; связь поведения между ними остаётся ручной логикой |
| LCD 1602 I2C | Wire, LiquidCrystal_I2C, адрес из модели, объект, init/backlight |
| OLED SSD1306 | Wire, Adafruit_GFX / Adafruit_SSD1306, адрес, объект 128×64, проверка begin |
| DHT11 / DHT22 | DHT.h, правильный тип датчика, begin и TODO чтения; DATA требует двунаправленного GPIO |
| HC-SR04 | TRIG/ECHO, OUTPUT/INPUT, комментарий о pulseIn с тайм-аутом |
| PIR | INPUT и TODO digitalRead |
| Серво Uno/Nano | Servo.h, объект Servo, attach только на ШИМ-пине по ограничению задания |
| Серво ESP32 | ESP32Servo.h, объект Servo, attach только на доступном ШИМ-выходе |
| Активный зуммер | OUTPUT и TODO управления; предупреждение о допустимом токе/драйвере |
| Реле | Ограниченно распознаётся NPN-драйвер: COIL− → C, B через резистор к GPIO. Создаётся OUTPUT управления; требуется проверка диода/питания |
| Потенциометр / LDR / NTC | Аналоговый GPIO и TODO analogRead; физический расчёт значений в скетч не выдумывается |

I2C Uno/Nano ограничен штатными SDA=A4, SCL=A5. ESP32 получает Wire.begin с фактическими GPIO. В шаблонах используется одна шина Wire; другую распиновку второго модуля генератор предупреждением отклоняет. WiFi.h добавляется на ESP32 при реальном упоминании WiFi в ручном коде, не из комментария/строки.

Для неподдерживаемых компонентов — комментарий-заглушка. В частности, не генерируются параллельный LCD, RGB-эффекты, 7-сегментная мультиплексация, произвольные транзисторные схемы, SPI/UART-драйверы и пользовательские микросхемы. Пассивные детали не превращаются в фиктивные Arduino-объекты. Полная логика программы и установка внешних Arduino-библиотек не входят в этап.

Генерация обновляет только три секции:

```cpp
// <auto>
// section: globals
// определения и объекты
// </auto>

void setup() {
  // <auto>
  // section: setup
  // режимы и инициализация
  // </auto>
}

void loop() {
  // <auto>
  // section: loop
  // заготовки
  // </auto>
  // Ручной код вне маркеров сохраняется буквально.
}
```

Маркеры должны быть целыми, уникальными и невложенными. Непустой ручной скетч без полного набора маркеров не перестраивается: выводится предупреждение, текст остаётся прежним. Правки внутри маркеров заменяются при следующем обновлении.

Подпись актуальности учитывает электрические узлы, экземпляры/обозначения, номиналы, I2C-адреса, роли/параметры и выбранную плату. Координаты, цвет/изгиб провода и масштаб не делают код устаревшим. При изменении схемы текст не переписывается автоматически.

## Анализатор и ограничения

- Токенизатор сохраняет исходные строки/столбцы, пропуская //, /* */, обычные/символьные литералы, raw-строки и их u8/u/U/L-префиксы. Продолжения // и многострочных директив не превращаются в вызовы API.
- Распознаются известные вызовы Arduino API, простые #define / const / constexpr, цепочки псевдонимов и обёртки скобками вокруг одиночной константы. Пины нормализуются по JSON-таблице выбранной платы: числовые, A/D/GPIO-имена; LED_BUILTIN — только при известном значении профиля.
- Используемый пин без подключения — Warning. Подключённый, но не найденный в поддерживаемых вызовах — Info, поскольку он может использоваться через неподдерживаемую пользовательскую функцию.
- Запись при явно заданной роли Input в модели — Warning. pinMode OUTPUT и функции записи на input-only GPIO34–39 ESP32 — Warning по тексту и таблице, даже без экземпляра платы/симуляции.
- Wire.begin учитывает стандартные SDA/SCL либо фактические аргументы ESP32. attach рассматривается как GPIO-вызов при известном объекте Servo; произвольные одноимённые методы неизвестных объектов не считаются Arduino API.
- Не выполняются препроцессор, C++-компиляция, включение заголовков, вычисление произвольных выражений, полноценные области видимости и анализ потока управления. Условные ветви не вычисляются. Это подсказки для ограниченного набора конструкций, а не доказательство корректности программы.
- Debounce 350 мс. Анализ получает независимый снимок и работает вне UI-потока; старые результаты отменяются/отбрасываются. Ввод текста не перестраивает электрическую связность и не инвалидирует DC.

## Файлы и вывод

.circ теперь schemaVersion 2. Добавлено поле sketch с text, board, name, controllerComponentId и generatedCircuitSignature. libraryVersion и ссылки по ID сохраняются; библиотечные определения не встраиваются.

Миграция 1→2 добавляет пустой скетч Uno/Sketch, сохраняя старую схему. Тест реально записывает файл версии 1 без sketch и открывает через производственную миграцию. Неизвестные будущие версии отклоняются с указанием версии файла и поддерживаемой версии.

Скетч участвует в сохранении, автосохранении и восстановлении. Изменения текста/метаданных учитываются в звёздочке проекта отдельно от истории схемы. Undo схемы не откатывает текст; возврат текста к сохранённому состоянию снимает его dirty-флаг. Текст ограничен 2 000 000 символов.

Экспорт через ISketchRepository создаёт Name/Name.ino (либо использует уже одноимённую папку). Имена до 63 символов по Arduino-правилам, без зарезервированных имён файловой системы; UTF-8. Выбор пути/ошибки через IDialogService.

Нижние вкладки «Проблемы» и «Вывод» используют существующую изменяемую панель. Щелчок по проблеме открывает «Код» на нужной строке. Вывод принимает системные строки, ограничен последними 500 строками, имеет очистку и автопрокрутку. Просмотр старых строк колесом/полосой отключает автопрокрутку до её повторного включения. Реальных Serial-данных пока нет.

## Ручная проверка

1. Запустить `dotnet run --project CircuitEditor.App -c Release`. Добавить Uno, LED, резистор и кнопку: D9 → R → LED.A, LED.K → GND; D2 → кнопка.1, кнопка.2 → GND.
2. Открыть «Код», выбрать Uno/Авто, нажать «Обновить». Проверить LED1_PIN / SW1_PIN, OUTPUT и INPUT_PULLUP. Для одного LED без кнопки проверить Blink на millis.
3. Вписать ручной код после маркера loop, снова обновить. Ручной текст сохраняется. Переместить компонент — код актуален; переподключить GPIO — появляется «Код устарел», текст остаётся прежним до обновления.
4. Проверить (), {}, кавычки, Enter между {}, отступы, стрелку сворачивания у setup/loop, Ctrl+Space и подсказки. Переключить вкладку и вернуться: Ctrl+Z/Ctrl+Y/Ctrl+Shift+Z продолжают текстовую историю.
5. Ctrl+F / Ctrl+H — найти и заменить текст, включая «Все» одним Undo. Ctrl+G — перейти к строке, проверить неверный номер. Ctrl+колесо — масштаб; строка/столбец видны внизу. Повторить в Light Tech.
6. Дописать `digitalWrite(8,HIGH);`: после паузы появляется Warning, щелчок переводит курсор. Удалить вызов — устаревшая проблема исчезает. Такой же текст в комментарии/строке не считается вызовом.
7. Выбрать ESP32 и добавить `pinMode(GPIO34, OUTPUT);`, затем заменить OUTPUT на INPUT: предупреждение input-only должно исчезнуть. Повторить digitalWrite на GPIO39. Для серво проверить отказ D2 на Uno / GPIO34 на ESP32 и успешные D9 / GPIO18 соответственно.
8. Сохранить .circ, изменить текст и открыть сохранённый проект, учитывая диалог несохранённых изменений. Проверить текст, плату и очистку текстовой истории. Открыть старый .circ v1 — пустой скетч, исходная схема сохранена.
9. Экспортировать .ino: проверить одноимённые папку и файл. Проверить ошибочное имя и сообщение в диалоге.
10. Открыть «Вывод», проверить сообщения генерации/анализа, очистку и автопрокрутку. Проверить скрытие/возврат нижней панели через меню «Вид».

## Итоговая проверка

- Debug: dotnet build / dotnet test — 0 ошибок и предупреждений, 397/397 тестов.
- Release: dotnet build / dotnet test — 0 ошибок и предупреждений, 397/397 тестов.
- git diff --check без ошибок. Коммиты и staging не выполнялись.

## Проверки архитектуры

Core остаётся net8.0 без WPF, System.Windows, ViewModel и AvalonEdit. Генератор/анализатор имеют интерфейсы; API/шаблоны читаются репозиториями Infrastructure. Новые сервисы зарегистрированы в DI. Файлы проекта и .ino проходят через репозитории, диалоги — через IDialogService. Code-behind новых Views содержит только InitializeComponent. Один класс на файл; новые/изменённые исходники до 300 строк. Этапы 7–10 не реализовывались.

## Первичные источники

- [NuGet AvalonEdit 6.3.1.120](https://www.nuget.org/packages/AvalonEdit/6.3.1.120).
- [Исходники AvalonEdit](https://github.com/icsharpcode/AvalonEdit/tree/v6.3.1/ICSharpCode.AvalonEdit).
- [ESP32Servo](https://github.com/madhephaestus/ESP32Servo).
- [Arduino: спецификация папок и файлов скетча](https://docs.arduino.cc/arduino-cli/sketch-specification).
## Дерево исходников этапа 6 (новые и изменённые)

```text
CircuitEditor.App/
  CircuitEditor.App.csproj
  Interaction/
    CodeEditorBehavior.cs
    CodeEditorSession.cs
    EditorCompletionController.cs
    EditorCompletionData.cs
    EditorDocumentState.cs
    EditorFoldingController.cs
    EditorHighlighting.cs
    EditorSearchController.cs
    EditorTypingController.cs
    OutputScrollBehavior.cs
    ProjectShortcutBehavior.cs
    ShortcutCommands.cs
  MainWindow.xaml
  Program.cs
  Resources/
    Code/
      Arduino.xshd
    CodeStyles.xaml
    Navigation.xaml
    Styles.xaml
  Services/
    CodeAnalysisService.cs
    ICodeAnalysisService.cs
  ViewModels/
    CodeProblemsViewModel.cs
    CodeProblemViewModel.cs
    CodeViewModel.cs
    MainViewModel.cs
    OutputViewModel.cs
    PanelLayoutViewModel.cs
    ProjectSessionViewModel.cs
    ProjectViewModel.cs
    ShortcutGuideViewModel.cs
  Views/
    AnalysisView.xaml
    CodeProblemsView.xaml
    CodeProblemsView.xaml.cs
    CodeView.xaml
    CodeView.xaml.cs
    OutputView.xaml
    OutputView.xaml.cs
CircuitEditor.Core/
  Code/
    ArduinoCodeAnalyzer.cs
    ArduinoCodeGenerator.cs
    AutoRegionUpdater.cs
    BoardKind.cs
    BoardPinDefinition.cs
    CodeBoardProfile.cs
    CodeCatalog.cs
    CodeCircuitContext.cs
    CodeCircuitSignature.cs
    CodeCompletionItem.cs
    CodeConnectionResolver.cs
    CodeGenerationResult.cs
    CodePinResolver.cs
    CodePinUse.cs
    CodeProblem.cs
    CodeTemplate.cs
    ComponentSignal.cs
    CppToken.cs
    CppTokenScanner.cs
    ICodeAnalyzer.cs
    ICodeGenerator.cs
    PinUsageCollector.cs
    SketchDocument.cs
    SketchName.cs
  Documents/
    IProjectMigration.cs
    ProjectDocument.cs
    ProjectDocumentResolver.cs
  Models/
    CircuitProject.cs
  Repositories/
    ICodeCompletionRepository.cs
    ICodeTemplateRepository.cs
    ISketchRepository.cs
CircuitEditor.Infrastructure/
  Data/
    arduino-api.json
    code-templates.json
  Documents/
    ProjectMigrationV1ToV2.cs
  Repositories/
    CodeJsonReader.cs
    FileSketchRepository.cs
    JsonCodeCompletionRepository.cs
    JsonCodeTemplateRepository.cs
CircuitEditor.Tests/
  CodeAnalyzerTests.cs
  CodeEditorPresentationChecks.cs
  CodeGeneratorTests.cs
  CodeWorkspacePresentationChecks.cs
  CodeWorkspaceTests.cs
  FileNotificationTests.cs
  ProjectFileTests.cs
  SketchStorageTests.cs
  TestCodeAnalysisService.cs
  ThemeResourceTests.cs
```
