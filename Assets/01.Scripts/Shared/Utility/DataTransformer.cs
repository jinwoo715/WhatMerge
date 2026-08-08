using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using WhatMerge.Enemies;

public class DataTransformer : EditorWindow
{
#if UNITY_EDITOR
    [MenuItem("Tools/Parse/CSV To Json %#K")]
    public static void ParseCSVDataToJson()
    {
        //ParseCsvDataToJson<EnemyRewardData>("EnemyRewardData");
        ParseCsvDataToJson<EnemyData>("EnemyData");
        ParseCsvDataToJson<HeroData>("HeroData");
        ParseCsvDataToJson<ATKData>("ATKData");

        Debug.Log("DataTransformer Completed");
    }

    public static IList ParseExcelDataToList(Type parseType, string filename)
    {
        try
        {
            return ParseCsvDataToList(parseType, filename);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            return null;
        }
    }

    private static void ParseCsvDataToJson<T>(string filename) where T : new()
    {
        IList parsed = ParseCsvDataToList(typeof(T), filename);
        string json = JsonConvert.SerializeObject(parsed, Formatting.Indented);
        string directory = Path.Combine(Application.dataPath, "06.Data/JSON");

        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"{filename}.json"), json, new UTF8Encoding(false));
        AssetDatabase.Refresh();
    }

    private static IList ParseCsvDataToList(Type parseType, string filename)
    {
        string path = Path.Combine(Application.dataPath, "06.Data/CSV", $"{filename}.csv");
        List<List<string>> records = ReadCsv(path);

        if (records.Count == 0)
            throw new InvalidDataException($"CSV file has no header: {path}");

        Dictionary<string, int> headerIndices = BuildHeaderIndices(records[0], path);
        Dictionary<string, FieldInfo> fields = GetPublicFields(parseType);
        ValidateSchema(headerIndices, fields, parseType, path);

        Type listType = typeof(List<>).MakeGenericType(parseType);
        IList results = (IList)Activator.CreateInstance(listType);

        for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
        {
            List<string> row = records[rowIndex];
            if (IsEmptyRow(row))
                continue;

            if (row.Count != headerIndices.Count)
            {
                throw new InvalidDataException(
                    $"{filename}.csv row {rowIndex + 1} has {row.Count} columns; expected {headerIndices.Count}.");
            }

            object instance = Activator.CreateInstance(parseType);
            foreach (KeyValuePair<string, FieldInfo> pair in fields)
            {
                int columnIndex = headerIndices[pair.Key];
                object value = ConvertCell(row[columnIndex], pair.Value.FieldType);
                pair.Value.SetValue(instance, value);
            }

            results.Add(instance);
        }

        return results;
    }

    private static Dictionary<string, int> BuildHeaderIndices(IReadOnlyList<string> header, string path)
    {
        Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < header.Count; i++)
        {
            string name = header[i].Trim().TrimStart('\uFEFF');
            if (string.IsNullOrEmpty(name))
                throw new InvalidDataException($"CSV header at column {i + 1} is empty: {path}");

            if (!indices.TryAdd(name, i))
                throw new InvalidDataException($"Duplicate CSV header '{name}': {path}");
        }

        return indices;
    }

    private static Dictionary<string, FieldInfo> GetPublicFields(Type parseType)
    {
        Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        FieldInfo[] publicFields = parseType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 0; i < publicFields.Length; i++)
            fields.Add(publicFields[i].Name, publicFields[i]);

        return fields;
    }

    private static void ValidateSchema(
        IReadOnlyDictionary<string, int> headers,
        IReadOnlyDictionary<string, FieldInfo> fields,
        Type parseType,
        string path)
    {
        foreach (string header in headers.Keys)
        {
            if (!fields.ContainsKey(header))
                throw new InvalidDataException($"CSV header '{header}' does not exist on {parseType.Name}: {path}");
        }

        foreach (string field in fields.Keys)
        {
            if (!headers.ContainsKey(field))
                throw new InvalidDataException($"CSV is missing field '{field}' for {parseType.Name}: {path}");
        }
    }

    private static object ConvertCell(string value, Type type)
    {
        if (type == typeof(string))
            return value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return type.IsValueType ? Activator.CreateInstance(type) : null;

        string trimmedValue = value.Trim();
        if (type.IsEnum)
            return Enum.Parse(type, trimmedValue, true);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return ConvertList(trimmedValue, type, '&');

        TypeConverter converter = TypeDescriptor.GetConverter(type);
        return converter.ConvertFromInvariantString(trimmedValue);
    }

    private static object ConvertList(string value, Type listType, char separator)
    {
        Type elementType = listType.GetGenericArguments()[0];
        IList list = (IList)Activator.CreateInstance(listType);
        string[] elements = value.Split(separator);

        for (int i = 0; i < elements.Length; i++)
            list.Add(ConvertCell(elements[i], elementType));

        return list;
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

    private static List<List<string>> ReadCsv(string path)
    {
        string content = File.ReadAllText(path);
        List<List<string>> records = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder field = new StringBuilder();
        bool insideQuotes = false;

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
                case '"' when field.Length == 0:
                    insideQuotes = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                case '\n':
                    if (current == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                        i++;

                    row.Add(field.ToString());
                    field.Clear();
                    records.Add(row);
                    row = new List<string>();
                    break;
                default:
                    field.Append(current);
                    break;
            }
        }

        if (insideQuotes)
            throw new InvalidDataException($"CSV contains an unterminated quoted field: {path}");

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            records.Add(row);
        }

        return records;
    }
#endif
}
