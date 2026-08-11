using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WhatMerge.EditorTools.DataConversion.Writers
{
    internal sealed class JsonDataWriter : ITabularDataWriter
    {
        public void Write(string path, TabularData data, string worksheetName = null)
        {
            JArray array = new JArray();
            for (int rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
            {
                JObject item = new JObject();
                for (int columnIndex = 0; columnIndex < data.Headers.Count; columnIndex++)
                {
                    item.Add(
                        data.Headers[columnIndex],
                        TabularData.ToJsonToken(data.Rows[rowIndex][columnIndex]));
                }

                array.Add(item);
            }

            File.WriteAllText(path, array.ToString(Formatting.Indented), new UTF8Encoding(false));
        }
    }
}
