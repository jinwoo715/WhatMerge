using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;

namespace WhatMerge.EditorTools.DataConversion
{
    internal enum DataFileFormat
    {
        Xlsx,
        Csv,
        Json
    }

    internal readonly struct DataConversionResult
    {
        public int DataRowCount { get; }
        public int ColumnCount { get; }

        public DataConversionResult(int dataRowCount, int columnCount)
        {
            DataRowCount = dataRowCount;
            ColumnCount = columnCount;
        }
    }

    internal interface ITabularDataReader
    {
        TabularData Read(string path, string worksheetName = null);
    }

    internal interface ITabularDataWriter
    {
        void Write(string path, TabularData data, string worksheetName = null);
    }

    internal sealed class TabularData
    {
        private readonly List<string> _headers;
        private readonly List<IReadOnlyList<JToken>> _rows;

        public IReadOnlyList<string> Headers => _headers;
        public IReadOnlyList<IReadOnlyList<JToken>> Rows => _rows;

        public TabularData(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<JToken>> rows)
        {
            if (headers == null || headers.Count == 0)
                throw new InvalidDataException("The data has no header row.");

            _headers = CopyAndValidateHeaders(headers);
            _rows = CopyAndValidateRows(rows ?? Array.Empty<IReadOnlyList<JToken>>(), _headers.Count);
        }

        public static TabularData FromStringRows(
            IReadOnlyList<List<string>> sourceRows,
            bool allowMissingTrailingCells,
            bool trimTrailingHeaderCells)
        {
            if (sourceRows == null || sourceRows.Count == 0 || IsEmptyRow(sourceRows[0]))
                throw new InvalidDataException("The data has no header row.");

            int columnCount = sourceRows[0].Count;
            if (trimTrailingHeaderCells)
                columnCount = GetLastNonEmptyColumn(sourceRows[0]) + 1;

            if (columnCount <= 0)
                throw new InvalidDataException("The header row is empty.");

            List<string> headers = new List<string>(columnCount);
            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                headers.Add(sourceRows[0][columnIndex]);

            List<IReadOnlyList<JToken>> rows = new List<IReadOnlyList<JToken>>();
            for (int rowIndex = 1; rowIndex < sourceRows.Count; rowIndex++)
            {
                List<string> sourceRow = sourceRows[rowIndex];
                if (IsEmptyRow(sourceRow))
                    continue;

                int lastColumn = GetLastNonEmptyColumn(sourceRow);
                if (lastColumn >= columnCount)
                {
                    throw new InvalidDataException(
                        $"Row {rowIndex + 1} has data beyond the {columnCount}-column header.");
                }

                if (!allowMissingTrailingCells && sourceRow.Count != columnCount)
                {
                    throw new InvalidDataException(
                        $"Row {rowIndex + 1} has {sourceRow.Count} columns; expected {columnCount}.");
                }

                List<JToken> row = new List<JToken>(columnCount);
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    string value = columnIndex < sourceRow.Count
                        ? sourceRow[columnIndex] ?? string.Empty
                        : string.Empty;
                    row.Add(new JValue(value));
                }

                rows.Add(row);
            }

            return new TabularData(headers, rows);
        }

        public static string ToFlatString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return string.Empty;

            if (token is JArray array)
            {
                string[] values = new string[array.Count];
                for (int i = 0; i < array.Count; i++)
                    values[i] = ToScalarString(array[i]);

                return string.Join("&", values);
            }

            return ToScalarString(token);
        }

        public static JToken ToJsonToken(JToken token)
        {
            if (token == null)
                return JValue.CreateNull();

            if (token.Type != JTokenType.String)
                return token.DeepClone();

            string value = token.Value<string>() ?? string.Empty;
            if (value.IndexOf('&') < 0)
                return new JValue(value);

            string[] elements = value.Split(new[] { '&' }, StringSplitOptions.None);
            JArray array = new JArray();
            for (int i = 0; i < elements.Length; i++)
                array.Add(elements[i]);

            return array;
        }

        private static List<string> CopyAndValidateHeaders(IReadOnlyList<string> headers)
        {
            List<string> results = new List<string>(headers.Count);
            HashSet<string> uniqueHeaders = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < headers.Count; i++)
            {
                string header = (headers[i] ?? string.Empty).Trim().TrimStart('\uFEFF');
                if (string.IsNullOrEmpty(header))
                    throw new InvalidDataException($"Header column {i + 1} is empty.");

                if (!uniqueHeaders.Add(header))
                    throw new InvalidDataException($"Header '{header}' is duplicated.");

                results.Add(header);
            }

            return results;
        }

        private static List<IReadOnlyList<JToken>> CopyAndValidateRows(
            IReadOnlyList<IReadOnlyList<JToken>> rows,
            int columnCount)
        {
            List<IReadOnlyList<JToken>> results = new List<IReadOnlyList<JToken>>(rows.Count);
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<JToken> sourceRow = rows[rowIndex];
                if (sourceRow == null || sourceRow.Count != columnCount)
                {
                    int actualCount = sourceRow?.Count ?? 0;
                    throw new InvalidDataException(
                        $"Row {rowIndex + 2} has {actualCount} columns; expected {columnCount}.");
                }

                List<JToken> row = new List<JToken>(columnCount);
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    JToken value = sourceRow[columnIndex] ?? JValue.CreateNull();
                    ValidateCell(value, rowIndex + 2, columnIndex + 1);
                    row.Add(value.DeepClone());
                }

                results.Add(row);
            }

            return results;
        }

        private static void ValidateCell(JToken token, int rowNumber, int columnNumber)
        {
            if (token is JObject)
            {
                throw new InvalidDataException(
                    $"Nested JSON objects are not supported at row {rowNumber}, column {columnNumber}.");
            }

            if (!(token is JArray array))
                return;

            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] is JContainer)
                {
                    throw new InvalidDataException(
                        $"Only one-dimensional JSON arrays are supported at row {rowNumber}, column {columnNumber}.");
                }
            }
        }

        private static string ToScalarString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return string.Empty;

            if (token is JContainer)
                throw new InvalidDataException("A nested JSON value cannot be represented as a table cell.");

            if (token.Type == JTokenType.String)
                return token.Value<string>() ?? string.Empty;

            if (token.Type == JTokenType.Boolean)
                return token.Value<bool>() ? "TRUE" : "FALSE";

            object value = ((JValue)token).Value;
            if (value is DateTime dateTime)
                return dateTime.ToString("o", CultureInfo.InvariantCulture);

            if (value is DateTimeOffset dateTimeOffset)
                return dateTimeOffset.ToString("o", CultureInfo.InvariantCulture);

            return value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static bool IsEmptyRow(IReadOnlyList<string> row)
        {
            if (row == null)
                return true;

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
    }
}
