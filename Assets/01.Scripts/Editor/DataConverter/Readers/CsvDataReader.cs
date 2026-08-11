using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WhatMerge.EditorTools.DataConversion.Readers
{
    internal sealed class CsvDataReader : ITabularDataReader
    {
        public TabularData Read(string path, string worksheetName = null)
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            List<List<string>> rows = Parse(content, path);
            return TabularData.FromStringRows(rows, false, false);
        }

        private static List<List<string>> Parse(string content, string path)
        {
            List<List<string>> records = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool insideQuotes = false;
            bool fieldWasQuoted = false;

            for (int i = 0; i < content.Length; i++)
            {
                char current = content[i];

                if (insideQuotes)
                {
                    if (current == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(current);
                    }

                    continue;
                }

                switch (current)
                {
                    case '"' when field.Length == 0 && !fieldWasQuoted:
                        insideQuotes = true;
                        fieldWasQuoted = true;
                        break;
                    case ',':
                        AddField(row, field, ref fieldWasQuoted);
                        break;
                    case '\r':
                    case '\n':
                        if (current == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                            i++;

                        AddField(row, field, ref fieldWasQuoted);
                        records.Add(row);
                        row = new List<string>();
                        break;
                    default:
                        if (fieldWasQuoted)
                        {
                            throw new InvalidDataException(
                                $"CSV has an unexpected character after a quoted field at character {i + 1}: {path}");
                        }

                        field.Append(current);
                        break;
                }
            }

            if (insideQuotes)
                throw new InvalidDataException($"CSV contains an unterminated quoted field: {path}");

            if (field.Length > 0 || fieldWasQuoted || row.Count > 0)
            {
                AddField(row, field, ref fieldWasQuoted);
                records.Add(row);
            }

            return records;
        }

        private static void AddField(List<string> row, StringBuilder field, ref bool fieldWasQuoted)
        {
            row.Add(field.ToString());
            field.Clear();
            fieldWasQuoted = false;
        }
    }
}
