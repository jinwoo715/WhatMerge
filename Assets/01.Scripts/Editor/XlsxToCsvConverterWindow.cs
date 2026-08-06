using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WhatMerge.Enemies;
#endif

namespace WhatMerge.EditorTools
{
#if UNITY_EDITOR
    public sealed class XlsxToCsvConverterWindow : EditorWindow
    {
        private const string DefaultSourcePath = "Assets/06.Data/Origin/EnemyData.xlsx";
        private const string DefaultOutputPath = "Assets/06.Data/CSV/EnemyData.csv";

        private DefaultAsset _sourceAsset;
        private string _outputAssetPath = DefaultOutputPath;
        private readonly List<string> _worksheetNames = new List<string>();
        private int _selectedWorksheet;
        private string _statusMessage;
        private MessageType _statusType = MessageType.None;

        [MenuItem("Tools/Data/XLSX To CSV Converter")]
        public static void Open()
        {
            XlsxToCsvConverterWindow window = GetWindow<XlsxToCsvConverterWindow>("XLSX To CSV");
            window.minSize = new Vector2(520f, 250f);
        }

        private void OnEnable()
        {
            _sourceAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultSourcePath);
            ReloadWorksheetNames();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("XLSX To UTF-8 CSV", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            DefaultAsset sourceAsset = (DefaultAsset)EditorGUILayout.ObjectField(
                "XLSX Source",
                _sourceAsset,
                typeof(DefaultAsset),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                _sourceAsset = sourceAsset;
                ReloadWorksheetNames();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("CSV Output");
                _outputAssetPath = EditorGUILayout.TextField(_outputAssetPath);
                if (GUILayout.Button("...", GUILayout.Width(32f)))
                    SelectOutputPath();
            }

            using (new EditorGUI.DisabledScope(_worksheetNames.Count == 0))
            {
                _selectedWorksheet = EditorGUILayout.Popup(
                    "Worksheet",
                    Mathf.Clamp(_selectedWorksheet, 0, Mathf.Max(0, _worksheetNames.Count - 1)),
                    _worksheetNames.ToArray());
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "The CSV is written with a UTF-8 BOM. Save the XLSX in Excel before converting.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_sourceAsset == null))
                {
                    if (GUILayout.Button("Open XLSX", GUILayout.Height(28f)))
                        AssetDatabase.OpenAsset(_sourceAsset);
                }

                using (new EditorGUI.DisabledScope(!CanConvert()))
                {
                    if (GUILayout.Button("Convert To CSV", GUILayout.Height(28f)))
                        ConvertSelectedWorksheet();
                }
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private bool CanConvert()
        {
            return _sourceAsset != null
                && _worksheetNames.Count > 0
                && !string.IsNullOrWhiteSpace(_outputAssetPath);
        }

        private void ReloadWorksheetNames()
        {
            _worksheetNames.Clear();
            _selectedWorksheet = 0;
            _statusMessage = null;
            _statusType = MessageType.None;

            if (_sourceAsset == null)
                return;

            string sourceAssetPath = AssetDatabase.GetAssetPath(_sourceAsset);
            if (!string.Equals(Path.GetExtension(sourceAssetPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("The selected source is not an .xlsx file.", MessageType.Error);
                return;
            }

            try
            {
                string sourceFullPath = GetFullProjectPath(sourceAssetPath);
                _worksheetNames.AddRange(XlsxToCsvConverter.GetWorksheetNames(sourceFullPath));
                if (_worksheetNames.Count == 0)
                    SetStatus("The workbook has no worksheets.", MessageType.Error);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void SelectOutputPath()
        {
            string currentFullPath = GetFullProjectPath(_outputAssetPath);
            string selectedPath = EditorUtility.SaveFilePanel(
                "Select CSV Output",
                Path.GetDirectoryName(currentFullPath),
                Path.GetFileNameWithoutExtension(currentFullPath),
                "csv");

            if (string.IsNullOrEmpty(selectedPath))
                return;

            string projectRelativePath = FileUtil.GetProjectRelativePath(selectedPath);
            if (string.IsNullOrEmpty(projectRelativePath)
                || !projectRelativePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                SetStatus("The CSV output must be inside the project's Assets directory.", MessageType.Error);
                return;
            }

            _outputAssetPath = projectRelativePath.Replace('\\', '/');
            Repaint();
        }

        private void ConvertSelectedWorksheet()
        {
            try
            {
                ValidateOutputPath(_outputAssetPath);

                string sourceAssetPath = AssetDatabase.GetAssetPath(_sourceAsset);
                string sourceFullPath = GetFullProjectPath(sourceAssetPath);
                string outputFullPath = GetFullProjectPath(_outputAssetPath);
                string worksheetName = _worksheetNames[_selectedWorksheet];
                Type schemaType = ResolveKnownSchema(_outputAssetPath);

                XlsxConversionResult result = XlsxToCsvConverter.Convert(
                    sourceFullPath,
                    worksheetName,
                    outputFullPath,
                    schemaType);

                AssetDatabase.ImportAsset(_outputAssetPath, ImportAssetOptions.ForceUpdate);
                SetStatus(
                    $"Converted '{worksheetName}': {result.DataRowCount} row(s), {result.ColumnCount} column(s).",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
        }

        private void SetStatus(string message, MessageType messageType)
        {
            _statusMessage = message;
            _statusType = messageType;
            Repaint();
        }

        private static void ValidateOutputPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)
                || !assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !string.Equals(Path.GetExtension(assetPath), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The output must be a .csv file inside the project's Assets directory.");
            }
        }

        private static string GetFullProjectPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static Type ResolveKnownSchema(string outputAssetPath)
        {
            string filename = Path.GetFileNameWithoutExtension(outputAssetPath);
            if (string.Equals(filename, "EnemyData", StringComparison.OrdinalIgnoreCase))
                return typeof(EnemyData);

            if (string.Equals(filename, "EnemyRewardData", StringComparison.OrdinalIgnoreCase))
                return typeof(global::EnemyRewardData);

            return null;
        }
    }
#endif

    internal readonly struct XlsxConversionResult
    {
        public int DataRowCount { get; }
        public int ColumnCount { get; }

        public XlsxConversionResult(int dataRowCount, int columnCount)
        {
            DataRowCount = dataRowCount;
            ColumnCount = columnCount;
        }
    }

    internal static class XlsxToCsvConverter
    {
        private const int MaxWorksheetRows = 100000;
        private const int MaxWorksheetColumns = 16384;

        private static readonly XNamespace SpreadsheetNamespace =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        private static readonly XNamespace OfficeRelationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        private static readonly XNamespace PackageRelationshipNamespace =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        public static IReadOnlyList<string> GetWorksheetNames(string xlsxPath)
        {
            using (ZipArchive archive = OpenWorkbook(xlsxPath))
            {
                List<WorksheetInfo> worksheets = ReadWorksheetInfos(archive);
                List<string> names = new List<string>(worksheets.Count);
                for (int i = 0; i < worksheets.Count; i++)
                    names.Add(worksheets[i].Name);

                return names;
            }
        }

        public static XlsxConversionResult Convert(
            string xlsxPath,
            string worksheetName,
            string csvPath,
            Type schemaType = null)
        {
            List<List<string>> rows;
            using (ZipArchive archive = OpenWorkbook(xlsxPath))
            {
                WorksheetInfo worksheet = FindWorksheet(ReadWorksheetInfos(archive), worksheetName);
                List<string> sharedStrings = ReadSharedStrings(archive);
                rows = ReadWorksheetRows(archive, worksheet.EntryPath, sharedStrings);
            }

            int columnCount = NormalizeAndValidateRows(rows);
            if (schemaType != null)
                ValidateSchema(rows, schemaType);

            string csv = BuildCsv(rows);
            string directory = Path.GetDirectoryName(csvPath);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("The CSV output directory is invalid.");

            Directory.CreateDirectory(directory);
            File.WriteAllText(csvPath, csv, new UTF8Encoding(true));
            return new XlsxConversionResult(rows.Count - 1, columnCount);
        }

        private static ZipArchive OpenWorkbook(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath) || !File.Exists(xlsxPath))
                throw new FileNotFoundException("The XLSX source file was not found.", xlsxPath);

            FileStream stream = new FileStream(
                xlsxPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            try
            {
                return new ZipArchive(stream, ZipArchiveMode.Read, false);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        private static List<WorksheetInfo> ReadWorksheetInfos(ZipArchive archive)
        {
            XDocument workbook = LoadXml(archive, "xl/workbook.xml");
            XDocument relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");
            Dictionary<string, string> relationshipTargets = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (XElement relationship in relationships.Root.Elements(PackageRelationshipNamespace + "Relationship"))
            {
                string id = (string)relationship.Attribute("Id");
                string target = (string)relationship.Attribute("Target");
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                    relationshipTargets[id] = ResolveWorkbookTarget(target);
            }

            XElement sheets = workbook.Root.Element(SpreadsheetNamespace + "sheets");
            if (sheets == null)
                throw new InvalidDataException("The XLSX workbook has no sheets definition.");

            List<WorksheetInfo> results = new List<WorksheetInfo>();
            foreach (XElement sheet in sheets.Elements(SpreadsheetNamespace + "sheet"))
            {
                string name = (string)sheet.Attribute("name");
                string relationshipId = (string)sheet.Attribute(OfficeRelationshipNamespace + "id");
                if (string.IsNullOrEmpty(name)
                    || string.IsNullOrEmpty(relationshipId)
                    || !relationshipTargets.TryGetValue(relationshipId, out string entryPath))
                {
                    throw new InvalidDataException("The XLSX workbook contains an invalid worksheet relationship.");
                }

                results.Add(new WorksheetInfo(name, entryPath));
            }

            return results;
        }

        private static WorksheetInfo FindWorksheet(IReadOnlyList<WorksheetInfo> worksheets, string worksheetName)
        {
            for (int i = 0; i < worksheets.Count; i++)
            {
                if (string.Equals(worksheets[i].Name, worksheetName, StringComparison.Ordinal))
                    return worksheets[i];
            }

            throw new InvalidDataException($"Worksheet '{worksheetName}' was not found.");
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return new List<string>();

            XDocument document;
            using (Stream stream = entry.Open())
                document = XDocument.Load(stream, LoadOptions.None);

            List<string> sharedStrings = new List<string>();
            foreach (XElement item in document.Root.Elements(SpreadsheetNamespace + "si"))
            {
                StringBuilder value = new StringBuilder();
                foreach (XElement text in item.Descendants(SpreadsheetNamespace + "t"))
                    value.Append(text.Value);

                sharedStrings.Add(value.ToString());
            }

            return sharedStrings;
        }

        private static List<List<string>> ReadWorksheetRows(
            ZipArchive archive,
            string entryPath,
            IReadOnlyList<string> sharedStrings)
        {
            XDocument document = LoadXml(archive, entryPath);
            XElement sheetData = document.Root.Element(SpreadsheetNamespace + "sheetData");
            if (sheetData == null)
                throw new InvalidDataException("The worksheet has no sheet data.");

            List<List<string>> rows = new List<List<string>>();
            int inferredRowNumber = 1;

            foreach (XElement rowElement in sheetData.Elements(SpreadsheetNamespace + "row"))
            {
                int rowNumber = ParsePositiveInt((string)rowElement.Attribute("r"), inferredRowNumber, "row number");
                if (rowNumber > MaxWorksheetRows)
                    throw new InvalidDataException($"Worksheet row {rowNumber} exceeds the supported limit.");

                while (rows.Count < rowNumber - 1)
                    rows.Add(new List<string>());

                List<string> row = new List<string>();
                int inferredColumn = 0;

                foreach (XElement cell in rowElement.Elements(SpreadsheetNamespace + "c"))
                {
                    string reference = (string)cell.Attribute("r");
                    int columnIndex = string.IsNullOrEmpty(reference)
                        ? inferredColumn
                        : GetColumnIndex(reference);

                    if (columnIndex < 0 || columnIndex >= MaxWorksheetColumns)
                        throw new InvalidDataException($"Cell reference '{reference}' is invalid or unsupported.");

                    while (row.Count <= columnIndex)
                        row.Add(string.Empty);

                    row[columnIndex] = ReadCellValue(cell, sharedStrings, reference);
                    inferredColumn = columnIndex + 1;
                }

                rows.Add(row);
                inferredRowNumber = rowNumber + 1;
            }

            return rows;
        }

        private static string ReadCellValue(
            XElement cell,
            IReadOnlyList<string> sharedStrings,
            string reference)
        {
            if (cell.Element(SpreadsheetNamespace + "f") != null)
                throw new InvalidDataException($"Formula cells are not supported: {reference}.");

            string cellType = (string)cell.Attribute("t");
            if (string.Equals(cellType, "inlineStr", StringComparison.Ordinal))
            {
                XElement inlineString = cell.Element(SpreadsheetNamespace + "is");
                if (inlineString == null)
                    return string.Empty;

                StringBuilder value = new StringBuilder();
                foreach (XElement text in inlineString.Descendants(SpreadsheetNamespace + "t"))
                    value.Append(text.Value);

                return value.ToString();
            }

            string rawValue = cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty;
            switch (cellType)
            {
                case "s":
                    if (!int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out int sharedStringIndex)
                        || sharedStringIndex < 0
                        || sharedStringIndex >= sharedStrings.Count)
                    {
                        throw new InvalidDataException($"Cell {reference} has an invalid shared string index.");
                    }

                    return sharedStrings[sharedStringIndex];
                case "b":
                    return rawValue == "1" ? "TRUE" : "FALSE";
                case "e":
                    throw new InvalidDataException($"Cell {reference} contains an Excel error: {rawValue}.");
                default:
                    return rawValue;
            }
        }

        private static int NormalizeAndValidateRows(List<List<string>> rows)
        {
            while (rows.Count > 0 && IsEmptyRow(rows[rows.Count - 1]))
                rows.RemoveAt(rows.Count - 1);

            if (rows.Count == 0 || IsEmptyRow(rows[0]))
                throw new InvalidDataException("The worksheet has no header row.");

            int columnCount = GetLastNonEmptyColumn(rows[0]) + 1;
            if (columnCount <= 0)
                throw new InvalidDataException("The worksheet header is empty.");

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                List<string> row = rows[rowIndex];
                int lastColumn = GetLastNonEmptyColumn(row);
                if (lastColumn >= columnCount)
                {
                    throw new InvalidDataException(
                        $"Worksheet row {rowIndex + 1} has data beyond the {columnCount}-column header.");
                }

                while (row.Count < columnCount)
                    row.Add(string.Empty);

                if (row.Count > columnCount)
                    row.RemoveRange(columnCount, row.Count - columnCount);
            }

            ValidateHeaders(rows[0]);
            return columnCount;
        }

        private static void ValidateHeaders(List<string> headers)
        {
            HashSet<string> uniqueHeaders = new HashSet<string>(StringComparer.Ordinal);
            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                string header = headers[columnIndex]?.Trim().TrimStart('\uFEFF');
                if (string.IsNullOrEmpty(header))
                    throw new InvalidDataException($"Header column {columnIndex + 1} is empty.");

                if (!uniqueHeaders.Add(header))
                    throw new InvalidDataException($"Header '{header}' is duplicated.");

                headers[columnIndex] = header;
            }
        }

        private static void ValidateSchema(IReadOnlyList<List<string>> rows, Type schemaType)
        {
            Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            FieldInfo[] publicFields = schemaType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < publicFields.Length; i++)
                fields.Add(publicFields[i].Name, publicFields[i]);

            IReadOnlyList<string> headers = rows[0];
            Dictionary<string, int> headerIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                string header = headers[columnIndex];
                headerIndices.Add(header, columnIndex);
                if (!fields.ContainsKey(header))
                    throw new InvalidDataException($"Header '{header}' does not exist on {schemaType.Name}.");
            }

            foreach (string fieldName in fields.Keys)
            {
                if (!headerIndices.ContainsKey(fieldName))
                    throw new InvalidDataException($"The worksheet is missing field '{fieldName}' for {schemaType.Name}.");
            }

            HashSet<int> uids = new HashSet<int>();
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<string> row = rows[rowIndex];
                if (IsEmptyRow(row))
                    continue;

                foreach (KeyValuePair<string, FieldInfo> field in fields)
                {
                    int columnIndex = headerIndices[field.Key];
                    try
                    {
                        ValidateCellValue(row[columnIndex], field.Value.FieldType);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidDataException(
                            $"Invalid value at row {rowIndex + 1}, column '{field.Key}': {exception.Message}",
                            exception);
                    }
                }

                if (headerIndices.TryGetValue("UID", out int uidColumn))
                {
                    int uid = (int)TypeDescriptor.GetConverter(typeof(int))
                        .ConvertFromInvariantString(row[uidColumn].Trim());
                    if (uid <= 0)
                        throw new InvalidDataException($"UID must be greater than zero at row {rowIndex + 1}.");

                    if (!uids.Add(uid))
                        throw new InvalidDataException($"UID {uid} is duplicated at row {rowIndex + 1}.");
                }
            }
        }

        private static void ValidateCellValue(string value, Type type)
        {
            if (type == typeof(string))
                return;

            if (string.IsNullOrWhiteSpace(value))
                return;

            string trimmedValue = value.Trim();
            if (type.IsEnum)
            {
                Enum.Parse(type, trimmedValue, true);
                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                string[] elements = trimmedValue.Split('&');
                for (int i = 0; i < elements.Length; i++)
                    ValidateCellValue(elements[i], elementType);

                return;
            }

            TypeDescriptor.GetConverter(type).ConvertFromInvariantString(trimmedValue);
        }

        private static string BuildCsv(IReadOnlyList<List<string>> rows)
        {
            StringBuilder csv = new StringBuilder();
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<string> row = rows[rowIndex];
                for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
                {
                    if (columnIndex > 0)
                        csv.Append(',');

                    AppendCsvField(csv, row[columnIndex] ?? string.Empty);
                }

                csv.Append("\r\n");
            }

            return csv.ToString();
        }

        private static void AppendCsvField(StringBuilder csv, string value)
        {
            bool requiresQuotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!requiresQuotes)
            {
                csv.Append(value);
                return;
            }

            csv.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '"')
                    csv.Append("\"\"");
                else
                    csv.Append(value[i]);
            }

            csv.Append('"');
        }

        private static XDocument LoadXml(ZipArchive archive, string entryPath)
        {
            ZipArchiveEntry entry = archive.GetEntry(entryPath);
            if (entry == null)
                throw new InvalidDataException($"The XLSX entry '{entryPath}' was not found.");

            using (Stream stream = entry.Open())
                return XDocument.Load(stream, LoadOptions.None);
        }

        private static string ResolveWorkbookTarget(string target)
        {
            string path = target.Replace('\\', '/');
            if (path.StartsWith("/", StringComparison.Ordinal))
                path = path.TrimStart('/');
            else
                path = "xl/" + path;

            string[] parts = path.Split('/');
            List<string> normalized = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "." || parts[i].Length == 0)
                    continue;

                if (parts[i] == "..")
                {
                    if (normalized.Count == 0)
                        throw new InvalidDataException($"Invalid XLSX relationship target: {target}.");

                    normalized.RemoveAt(normalized.Count - 1);
                    continue;
                }

                normalized.Add(parts[i]);
            }

            return string.Join("/", normalized);
        }

        private static int GetColumnIndex(string cellReference)
        {
            int columnNumber = 0;
            int letterCount = 0;

            for (int i = 0; i < cellReference.Length; i++)
            {
                char character = cellReference[i];
                if (character >= 'a' && character <= 'z')
                    character = (char)(character - 'a' + 'A');

                if (character < 'A' || character > 'Z')
                    break;

                columnNumber = columnNumber * 26 + character - 'A' + 1;
                letterCount++;
            }

            return letterCount == 0 ? -1 : columnNumber - 1;
        }

        private static int ParsePositiveInt(string value, int fallback, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return fallback;

            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result)
                || result <= 0)
            {
                throw new InvalidDataException($"Invalid {fieldName}: {value}.");
            }

            return result;
        }

        private static bool IsEmptyRow(IReadOnlyList<string> row)
        {
            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                    return false;
            }

            return true;
        }

        private static int GetLastNonEmptyColumn(IReadOnlyList<string> row)
        {
            for (int i = row.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(row[i]))
                    return i;
            }

            return -1;
        }

        private readonly struct WorksheetInfo
        {
            public string Name { get; }
            public string EntryPath { get; }

            public WorksheetInfo(string name, string entryPath)
            {
                Name = name;
                EntryPath = entryPath;
            }
        }
    }
}
