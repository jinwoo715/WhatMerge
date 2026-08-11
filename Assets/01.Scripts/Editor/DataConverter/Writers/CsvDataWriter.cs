using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace WhatMerge.EditorTools.DataConversion.Writers
{
    internal sealed class CsvDataWriter : ITabularDataWriter
    {
        public void Write(string path, TabularData data, string worksheetName = null)
        {
            StringBuilder csv = new StringBuilder();
            AppendRow(csv, data.Headers);

            for (int rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
            {
                IReadOnlyList<JToken> row = data.Rows[rowIndex];
                for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
                {
                    if (columnIndex > 0)
                        csv.Append(',');

                    AppendField(csv, TabularData.ToFlatString(row[columnIndex]));
                }

                csv.Append("\r\n");
            }

            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
        }

        private static void AppendRow(StringBuilder csv, IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    csv.Append(',');

                AppendField(csv, values[i] ?? string.Empty);
            }

            csv.Append("\r\n");
        }

        private static void AppendField(StringBuilder csv, string value)
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
    }
}
