# Электрическая библиотека компонентов

Определения читаются из ресурсов CircuitEditor.Infrastructure через IComponentLibraryRepository (интерфейс в Core). Категории разделены на файлы для редактирования без изменения C#-кода.

- voltage — номинальная рабочая точка выбранной детали/модуля, не абсолютный максимум и не результат электрической проверки.
- defaultValue — начальный номинал; prefix — общий счётчик обозначений экземпляров, например R1 или LED1.
- pins: type — основная функция, capabilities — альтернативные функции. isInputOnly ограничивает направление; isExposed=false запрещает использование контакта на разъёме выбранной платы.
- Электрические определения не содержат координат выводов или размеров корпуса. Физическая геометрия находится в Data/footprints.json, символы рассчитываются в Core/Layout, положения экземпляров — в независимых ViewLayout проекта.
- ESP32 DevKit выбран в варианте классического ESP32-WROOM. GPIO34–39 input-only; GPIO37/38 не выведены на WROOM, GPIO6–11 заняты flash. Эти контакты сохранены в данных, но не рисуются как доступные выводы.
- У Uno A4/SDA и A5/SCL обозначают общие аппаратные сигналы; эта особенность описана в карточке. Объединение электрических узлов относится к будущему этапу соединений.
- I2C-адреса LCD/OLED — примеры конфигурации модулей, могут отличаться в зависимости от перемычек.

Первичные источники для контроллеров:

- ESP32 GPIO: https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/gpio.html
- ESP32 datasheet: https://documentation.espressif.com/esp32_datasheet_en.html
- Arduino Uno R3: https://docs.arduino.cc/resources/datasheets/A000066-datasheet.pdf
- PWM Uno/Nano: https://support.arduino.cc/hc/en-us/articles/9350537961500-Use-PWM-output-with-Arduino
