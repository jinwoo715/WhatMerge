using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace WhatMerge.EditorTools.DataConversion.Readers
{
    internal sealed class XlsxDataReader : ITabularDataReader
    {
        private const int MaxWorksheetRows = 1048576;
        private const int MaxWorksheetColumns = 16384;

        private static readonly XNamespace SpreadsheetNamespace =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        private static readonly XNamespace OfficeRelationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        private static readonly XNamespace PackageRelationshipNamespace =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        public TabularData Read(string path, string worksheetName = null)
        {
            using (ZipArchive archive = OpenWorkbook(path))
            {
                List<WorksheetInfo> worksheets = ReadWorksheetInfos(archive);
                if (worksheets.Count == 0)
                    throw new InvalidDataException("The XLSX workbook has no worksheets.");

                WorksheetInfo worksheet = string.IsNullOrWhiteSpace(worksheetName)
                    ? worksheets[0]
                    : FindWorksheet(worksheets, worksheetName);

                List<string> sharedStrings = ReadSharedStrings(archive);
                List<List<string>> rows = ReadWorksheetRows(archive, worksheet.EntryPath, sharedStrings);
                return TabularData.FromStringRows(rows, true, true);
            }
        }

        public static IReadOnlyList<string> GetWorksheetNames(string path)
        {
            using (ZipArchive archive = OpenWorkbook(path))
            {
                List<WorksheetInfo> worksheets = ReadWorksheetInfos(archive);
                List<string> names = new List<string>(worksheets.Count);
                for (int i = 0; i < worksheets.Count; i++)
                    names.Add(worksheets[i].Name);

                return names;
            }
        }

        private static ZipArchive OpenWorkbook(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("The XLSX source file was not found.", path);

            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
                    if (!int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                        || index < 0
                        || index >= sharedStrings.Count)
                    {
                        throw new InvalidDataException($"Cell {reference} has an invalid shared string index.");
                    }

                    return sharedStrings[index];
                case "b":
                    return rawValue == "1" ? "TRUE" : "FALSE";
                case "e":
                    throw new InvalidDataException($"Cell {reference} contains an Excel error: {rawValue}.");
                default:
                    return rawValue;
            }
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
