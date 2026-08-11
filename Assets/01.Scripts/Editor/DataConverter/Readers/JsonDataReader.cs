using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace WhatMerge.EditorTools.DataConversion.Readers
{
    internal sealed class JsonDataReader : ITabularDataReader
    {
        public TabularData Read(string path, string worksheetName = null)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            JToken root;
            try
            {
                root = JToken.Parse(json);
            }
            catch (Exception exception)
            {
                throw new InvalidDataException($"The JSON file is invalid: {path}", exception);
            }

            if (!(root is JArray array))
                throw new InvalidDataException("The JSON root must be an array of objects.");

            if (array.Count == 0)
                throw new InvalidDataException("An empty JSON array has no headers to convert.");

            if (!(array[0] is JObject firstObject))
                throw new InvalidDataException("Every JSON array item must be an object.");

            List<string> headers = new List<string>();
            foreach (JProperty property in firstObject.Properties())
                headers.Add(property.Name);

            if (headers.Count == 0)
                throw new InvalidDataException("The first JSON object has no properties.");

            HashSet<string> expectedHeaders = new HashSet<string>(headers, StringComparer.Ordinal);
            List<IReadOnlyList<JToken>> rows = new List<IReadOnlyList<JToken>>(array.Count);

            for (int rowIndex = 0; rowIndex < array.Count; rowIndex++)
            {
                if (!(array[rowIndex] is JObject item))
                    throw new InvalidDataException($"JSON item {rowIndex + 1} is not an object.");

                ValidateProperties(item, expectedHeaders, rowIndex + 1);

                List<JToken> row = new List<JToken>(headers.Count);
                for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                    row.Add(item[headers[columnIndex]] ?? JValue.CreateNull());

                rows.Add(row);
            }

            return new TabularData(headers, rows);
        }

        private static void ValidateProperties(JObject item, ISet<string> expectedHeaders, int itemNumber)
        {
            HashSet<string> actualHeaders = new HashSet<string>(StringComparer.Ordinal);
            foreach (JProperty property in item.Properties())
                actualHeaders.Add(property.Name);

            if (actualHeaders.SetEquals(expectedHeaders))
                return;

            List<string> missing = new List<string>();
            foreach (string header in expectedHeaders)
            {
                if (!actualHeaders.Contains(header))
                    missing.Add(header);
            }

            List<string> extra = new List<string>();
            foreach (string header in actualHeaders)
            {
                if (!expectedHeaders.Contains(header))
                    extra.Add(header);
            }

            string detail = string.Empty;
            if (missing.Count > 0)
                detail += $" Missing: {string.Join(", ", missing)}.";
            if (extra.Count > 0)
                detail += $" Extra: {string.Join(", ", extra)}.";

            throw new InvalidDataException($"JSON item {itemNumber} does not match the first object's fields.{detail}");
        }
    }
}
