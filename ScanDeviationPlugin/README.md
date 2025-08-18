# Scan Deviation Revit Plugin

Этот пример плагина для Autodesk Revit показывает, как измерять отклонения
между элементами модели и облаком точек и раскрашивать точки в зависимости от
расстояния до ближайшего элемента.

## Сборка
1. Создайте проект `Class Library (.NET Framework)` с таргетом `.NET Framework 4.8`.
2. Добавьте ссылки на `RevitAPI.dll` и `RevitAPIUI.dll`.
3. Скомпилируйте DLL.

## Подключение плагина
Создайте манифест в `%AppData%/Autodesk/Revit/Addins/<версия>`:

```xml
<RevitAddIns>
  <AddIn Type="Command">
    <Name>ScanDeviation</Name>
    <Assembly>C:\Path\ScanDeviation.dll</Assembly>
    <FullClassName>CmdScanDeviation</FullClassName>
    <AddInId>8D83C886-B739-4ACD-A9DB-1BC78C64DF5F</AddInId>
    <VendorId>COMP</VendorId>
    <VendorDescription>Deviation colorizer</VendorDescription>
  </AddIn>
</RevitAddIns>
```

## Использование
1. Запустите команду и выберите облако точек.
2. Укажите элементы модели, либо оставьте выбор пустым (будет выбран весь проект).
3. Введите пороговое расстояние в миллиметрах.
4. Точки облака будут раскрашены: красный — выше порога, зелёный — ниже.
