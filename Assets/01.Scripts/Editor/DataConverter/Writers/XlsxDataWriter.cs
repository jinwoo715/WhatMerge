using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace WhatMerge.EditorTools.DataConversion.Writers
{
    internal sealed class XlsxDataWriter : ITabularDataWriter
    {
        private const int MaxWorksheetRows = 1048576;
        private const int MaxWorksheetColumns = 16384;
        private const int MaxCellTextLength = 32767;

        private static readonly XNamespace SpreadsheetNamespace =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        private static readonly XNamespace OfficeRelationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        private static readonly XNamespace PackageRelationshipNamespace =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        private static readonly XNamespace ContentTypeNamespace =
            "http://schemas.openxmlformats.org/package/2006/content-types";

        public void Write(string path, TabularData data, string worksheetName = null)
        {
            ValidateDimensions(data);
            string sheetName = ValidateWorksheetName(worksheetName);
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, false))
                {
                    WriteXml(archive, "[Content_Types].xml", BuildContentTypes());
                    WriteXml(archive, "_rels/.rels", BuildRootRelationships());
                    WriteXml(archive, "xl/workbook.xml", BuildWorkbook(sheetName));
                    WriteXml(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships());
                    WriteXml(archive, "xl/styles.xml", BuildStyles());
                    WriteXml(archive, "xl/worksheets/sheet1.xml", BuildWorksheet(data));
                }

                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static XDocument BuildContentTypes()
        {
            return CreateDocument(
                new XElement(
                    ContentTypeNamespace + "Types",
                    new XElement(
                        ContentTypeNamespace + "Default",
                        new XAttribute("Extension", "rels"),
                        new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                    new XElement(
                        ContentTypeNamespace + "Default",
                        new XAttribute("Extension", "xml"),
                        new XAttribute("ContentType", "application/xml")),
                    new XElement(
                        ContentTypeNamespace + "Override",
                        new XAttribute("PartName", "/xl/workbook.xml"),
                        new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                    new XElement(
                        ContentTypeNamespace + "Override",
                        new XAttribute("PartName", "/xl/worksheets/sheet1.xml"),
                        new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")),
                    new XElement(
                        ContentTypeNamespace + "Override",
                        new XAttribute("PartName", "/xl/styles.xml"),
                        new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"))));
        }

        private static XDocument BuildRootRelationships()
        {
            return CreateDocument(
                new XElement(
                    PackageRelationshipNamespace + "Relationships",
                    new XElement(
                        PackageRelationshipNamespace + "Relationship",
                        new XAttribute("Id", "rId1"),
                        new XAttribute(
                            "Type",
                            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                        new XAttribute("Target", "xl/workbook.xml"))));
        }

        private static XDocument BuildWorkbook(string worksheetName)
        {
            return CreateDocument(
                new XElement(
                    SpreadsheetNamespace + "workbook",
                    new XAttribute(XNamespace.Xmlns + "r", OfficeRelationshipNamespace),
                    new XElement(
                        SpreadsheetNamespace + "sheets",
                        new XElement(
                            SpreadsheetNamespace + "sheet",
                            new XAttribute("name", worksheetName),
                            new XAttribute("sheetId", "1"),
                            new XAttribute(OfficeRelationshipNamespace + "id", "rId1")))));
        }

        private static XDocument BuildWorkbookRelationships()
        {
            return CreateDocument(
                new XElement(
                    PackageRelationshipNamespace + "Relationships",
                    new XElement(
                        PackageRelationshipNamespace + "Relationship",
                        new XAttribute("Id", "rId1"),
                        new XAttribute(
                            "Type",
                            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                        new XAttribute("Target", "worksheets/sheet1.xml")),
                    new XElement(
                        PackageRelationshipNamespace + "Relationship",
                        new XAttribute("Id", "rId2"),
                        new XAttribute(
                            "Type",
                            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
                        new XAttribute("Target", "styles.xml"))));
        }

        private static XDocument BuildStyles()
        {
            return CreateDocument(
                new XElement(
                    SpreadsheetNamespace + "styleSheet",
                    new XElement(
                        SpreadsheetNamespace + "fonts",
                        new XAttribute("count", "1"),
                        new XElement(
                            SpreadsheetNamespace + "font",
                            new XElement(SpreadsheetNamespace + "sz", new XAttribute("val", "11")),
                            new XElement(SpreadsheetNamespace + "name", new XAttribute("val", "Calibri")),
                            new XElement(SpreadsheetNamespace + "family", new XAttribute("val", "2")),
                            new XElement(SpreadsheetNamespace + "scheme", new XAttribute("val", "minor")))),
                    new XElement(
                        SpreadsheetNamespace + "fills",
                        new XAttribute("count", "2"),
                        new XElement(
                            SpreadsheetNamespace + "fill",
                            new XElement(SpreadsheetNamespace + "patternFill", new XAttribute("patternType", "none"))),
                        new XElement(
                            SpreadsheetNamespace + "fill",
                            new XElement(SpreadsheetNamespace + "patternFill", new XAttribute("patternType", "gray125")))),
                    new XElement(
                        SpreadsheetNamespace + "borders",
                        new XAttribute("count", "1"),
                        new XElement(
                            SpreadsheetNamespace + "border",
                            new XElement(SpreadsheetNamespace + "left"),
                            new XElement(SpreadsheetNamespace + "right"),
                            new XElement(SpreadsheetNamespace + "top"),
                            new XElement(SpreadsheetNamespace + "bottom"),
                            new XElement(SpreadsheetNamespace + "diagonal"))),
                    new XElement(
                        SpreadsheetNamespace + "cellStyleXfs",
                        new XAttribute("count", "1"),
                        CreateXf()),
                    new XElement(
                        SpreadsheetNamespace + "cellXfs",
                        new XAttribute("count", "1"),
                        CreateXf(new XAttribute("xfId", "0"))),
                    new XElement(
                        SpreadsheetNamespace + "cellStyles",
                        new XAttribute("count", "1"),
                        new XElement(
                            SpreadsheetNamespace + "cellStyle",
                            new XAttribute("name", "Normal"),
                            new XAttribute("xfId", "0"),
                            new XAttribute("builtinId", "0")))));
        }

        private static XElement CreateXf(params XAttribute[] additionalAttributes)
        {
            XElement element = new XElement(
                SpreadsheetNamespace + "xf",
                new XAttribute("numFmtId", "0"),
                new XAttribute("fontId", "0"),
                new XAttribute("fillId", "0"),
                new XAttribute("borderId", "0"));

            for (int i = 0; i < additionalAttributes.Length; i++)
                element.Add(additionalAttributes[i]);

            return element;
        }

        private static XDocument BuildWorksheet(TabularData data)
        {
            XElement sheetData = new XElement(SpreadsheetNamespace + "sheetData");
            sheetData.Add(BuildHeaderRow(data.Headers));

            for (int rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
            {
                int excelRowNumber = rowIndex + 2;
                XElement row = new XElement(
                    SpreadsheetNamespace + "row",
                    new XAttribute("r", excelRowNumber));

                for (int columnIndex = 0; columnIndex < data.Headers.Count; columnIndex++)
                {
                    row.Add(BuildCell(
                        data.Rows[rowIndex][columnIndex],
                        columnIndex,
                        excelRowNumber));
                }

                sheetData.Add(row);
            }

            string dimension = $"A1:{GetColumnName(data.Headers.Count - 1)}{data.Rows.Count + 1}";
            return CreateDocument(
                new XElement(
                    SpreadsheetNamespace + "worksheet",
                    new XElement(SpreadsheetNamespace + "dimension", new XAttribute("ref", dimension)),
                    sheetData));
        }

        private static XElement BuildHeaderRow(IReadOnlyList<string> headers)
        {
            XElement row = new XElement(
                SpreadsheetNamespace + "row",
                new XAttribute("r", "1"));

            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                row.Add(BuildTextCell(headers[columnIndex], columnIndex, 1));

            return row;
        }

        private static XElement BuildCell(JToken token, int columnIndex, int rowNumber)
        {
            string reference = GetColumnName(columnIndex) + rowNumber.ToString(CultureInfo.InvariantCulture);
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return new XElement(SpreadsheetNamespace + "c", new XAttribute("r", reference));

            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            {
                return new XElement(
                    SpreadsheetNamespace + "c",
                    new XAttribute("r", reference),
                    new XElement(SpreadsheetNamespace + "v", TabularData.ToFlatString(token)));
            }

            if (token.Type == JTokenType.Boolean)
            {
                return new XElement(
                    SpreadsheetNamespace + "c",
                    new XAttribute("r", reference),
                    new XAttribute("t", "b"),
                    new XElement(SpreadsheetNamespace + "v", token.Value<bool>() ? "1" : "0"));
            }

            return BuildTextCell(TabularData.ToFlatString(token), columnIndex, rowNumber);
        }

        private static XElement BuildTextCell(string value, int columnIndex, int rowNumber)
        {
            ValidateCellText(value, rowNumber, columnIndex + 1);
            string reference = GetColumnName(columnIndex) + rowNumber.ToString(CultureInfo.InvariantCulture);
            XElement text = new XElement(SpreadsheetNamespace + "t", value);

            if (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])))
                text.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));

            return new XElement(
                SpreadsheetNamespace + "c",
                new XAttribute("r", reference),
                new XAttribute("t", "inlineStr"),
                new XElement(SpreadsheetNamespace + "is", text));
        }

        private static void ValidateDimensions(TabularData data)
        {
            if (data.Headers.Count > MaxWorksheetColumns)
                throw new InvalidDataException($"XLSX supports at most {MaxWorksheetColumns} columns.");

            if (data.Rows.Count + 1 > MaxWorksheetRows)
                throw new InvalidDataException($"XLSX supports at most {MaxWorksheetRows} rows including the header.");
        }

        private static string ValidateWorksheetName(string worksheetName)
        {
            string name = string.IsNullOrWhiteSpace(worksheetName) ? "Data" : worksheetName.Trim();
            if (name.Length > 31)
                throw new InvalidDataException("An XLSX worksheet name cannot exceed 31 characters.");

            if (name.IndexOfAny(new[] { '[', ']', ':', '*', '?', '/', '\\' }) >= 0)
                throw new InvalidDataException($"The XLSX worksheet name contains an invalid character: {name}");

            return name;
        }

        private static void ValidateCellText(string value, int rowNumber, int columnNumber)
        {
            if (value.Length > MaxCellTextLength)
            {
                throw new InvalidDataException(
                    $"Cell at row {rowNumber}, column {columnNumber} exceeds {MaxCellTextLength} characters.");
            }

            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException exception)
            {
                throw new InvalidDataException(
                    $"Cell at row {rowNumber}, column {columnNumber} contains an invalid XML character.",
                    exception);
            }
        }

        private static string GetColumnName(int columnIndex)
        {
            int value = columnIndex + 1;
            string name = string.Empty;
            while (value > 0)
            {
                value--;
                name = (char)('A' + value % 26) + name;
                value /= 26;
            }

            return name;
        }

        private static XDocument CreateDocument(XElement root)
        {
            return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
        }

        private static void WriteXml(ZipArchive archive, string entryPath, XDocument document)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            using (Stream stream = entry.Open())
            using (XmlWriter writer = XmlWriter.Create(
                       stream,
                       new XmlWriterSettings
                       {
                           Encoding = new System.Text.UTF8Encoding(false),
                           Indent = false,
                           CloseOutput = false
                       }))
            {
                document.Save(writer);
            }
        }
    }
}
