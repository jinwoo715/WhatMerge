using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using Newtonsoft.Json;
using Enemies;
using Heros;

public class DataTransformer : EditorWindow
{
    public static string[] DataNames =
    {
        
    };


#if UNITY_EDITOR
    [MenuItem("Tools/Parse/CSV To Json %#K")]
    public static void ParseCSVDataToJson()
    {
        
        //ParseExcelDataToListJsonData<WaveData>("WaveData");
        //ParseExcelDataToListJsonData<StageData>("StageData");
        //ParseExcelDataToListJsonData<EnemyData>("EnemyData");
        //ParseExcelDataToListJsonData<ActiveSkillData>("ActiveSkillData");
        //ParseExcelDataToListJsonData<ATKData>("ATKData");
        //ParseExcelDataToListJsonData<HeroData>("HeroData");
        //ParseExcelDataToListJsonData<ProjectileData>("ProjectileData");
        //ParseExcelDataToListJsonData<SummonData>("SummonData");
        //ParseExcelDataToListJsonData<BuffData>("BuffData");
        //ParseExcelDataToListJsonData<BuffDataBundle>("BuffDataBundle");
        ParseExcelDataToListJsonData<MergeData>("MergeData");

        Debug.Log("DataTransformer Completed");
    }

    [MenuItem("Tools/Parse/Json To CSV %#XC")]
    public static void ParseJsonToCSV()
    {
        ParseExcelDataToList(typeof(WaveData), "WaveData");
    }

    #region To CSV From Json Helpers

    private static void ParseJsonDataToCSV(string fileName)
    {
        try
        {
            //string jsonData = File.ReadAllText($"{Application.dataPath}/01.Resources/Data/JsonData/{fileName}.json");

            //var array = JArray.Parse(jsonData);
            //string csvPath = $"{Application.dataPath}/01.Resources/Data/CSVData/{fileName}.csv";

            //using (var writer = new StreamWriter(csvPath))
            //{
            //    var headers = ((JObject)array[0]).Properties();
            //    writer.WriteLine(string.Join(",", headers.Select(h => h.Name)));

            //    foreach (var item in array)
            //    {
            //        var values = ((JObject)item).Properties().Select(p => p.Value.ToString());
            //        writer.WriteLine(string.Join(",", values));
            //    }
            //}
        }
        catch
        {
        }
    }

    #endregion

    #region To Json From CSV Helpers

    public static IList ParseExcelDataToList(Type parseType, string filename)
    {
        try
        {
            Type listType = typeof(List<>).MakeGenericType(parseType);
            IList loaderDatas = (IList)Activator.CreateInstance(listType);

            string path = Path.Combine(Application.dataPath, $"01.Resources/Data/CSV/{filename}.csv");

            string[] lines = File.ReadAllText(path).Split("\n");

            for (int l = 1; l < lines.Length; l++)
            {
                string[] row = lines[l].Replace("\r", "").Split(',');
                if (row.Length == 0 || string.IsNullOrEmpty(row[0]))
                    continue;

                object parseObj = Activator.CreateInstance(parseType);

                FieldInfo[] fields =
                    parseType.BaseType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Concat(parseType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    .ToArray();

                for (int f = 0; f < fields.Length; f++)
                {
                    FieldInfo field = parseType.GetField(fields[f].Name);
                    Type type = field.FieldType;
                    object value = null;

                    if (type.IsGenericType)
                    {
                        value = ConvertList(row[f], type, '&');
                    }
                    else if (type.IsEnum)
                    {
                        value = ConvertEnum(row[f], type);
                    }
                    else
                    {
                        value = ConvertValue(row[f], type);
                    }

                    field.SetValue(parseObj, value);
                }

                loaderDatas.Add(parseObj);
            }

            return loaderDatas;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
    }

    private static void ParseExcelDataToListJsonData<ParseClass>(string filename) where ParseClass : new()
    {
        List<ParseClass> convertData = ParseExcelDataToList<ParseClass>(filename);
        string jsonStr = JsonConvert.SerializeObject(convertData, Formatting.Indented);

        string path = Path.Combine(Application.dataPath, "06.Data/JSON");
        File.WriteAllText($"{path}/{filename}.json", jsonStr);
        AssetDatabase.Refresh();
    }
    private static List<ParseType> ParseExcelDataToList<ParseType>(string filename) where ParseType : new()
    {
        List<ParseType> loaderDatas = new List<ParseType>();

        string[] lines = File.ReadAllText($"{Application.dataPath}/06.Data/CSV/{filename}.csv").Split("\n");

        for (int l = 1; l < lines.Length; l++)
        {
            string[] row = lines[l].Replace("\r", "").Split(',');
            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;

            ParseType parseType = new ParseType();

            FieldInfo[] fields =
                typeof(ParseType).BaseType.GetFields(BindingFlags.Public | BindingFlags.Instance).
                Concat(typeof(ParseType).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();

            for (int f = 0; f < fields.Length; f++)
            {
                FieldInfo field = parseType.GetType().GetField(fields[f].Name);

                Type type = field.FieldType;

                object value = null;

                if(row.Length-1 < f)
                {
                    value = default;
                }
                else if (type.IsGenericType)
                {
                    value = ConvertList(row[f], type, '&');
                }
                else if (type.IsEnum)
                {
                    value = ConvertEnum(row[f], type);
                }
                else
                {
                    value = ConvertValue(row[f], type);
                }

                field.SetValue(parseType, value);
            }

            loaderDatas.Add(parseType);
        }

        return loaderDatas;
    }
    private static object ConvertList(string value, Type type, char splitChar)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        Type valueType = type.GetGenericArguments()[0];

        var genericList = Activator.CreateInstance(type) as IList;

        // Parse Excel
        var list = value.Split(splitChar).Select(x => ConvertValue(x, valueType)).ToList();

        foreach (var item in list)
            genericList.Add(item);

        return genericList;
    }
    private static object ConvertEnum(string value, Type type)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return Enum.Parse(type, value);
    }
    private static object ConvertValue(string value, Type type)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
                return null;

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFromString(value);
        }
        catch (Exception err)
        {
            Debug.LogError($"value : {value}, type : {type}, err : {err}");
            return null;
        }
    }
    #endregion

#endif
}