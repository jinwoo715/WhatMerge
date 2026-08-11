using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using WhatMerge.EditorTools.DataConversion;

namespace WhatMerge.EditorTools
{
    public sealed class DataConverterWindow : EditorWindow
    {
        private const string DefaultSourcePath = "Assets/06.Data/Origin/EnemyData.xlsx";

        private readonly List<string> _worksheetNames = new List<string>();
        private UnityEngine.Object _sourceAsset;
        private DataFileFormat _sourceFormat;
        private bool _hasSourceFormat;
        private DataFileFormat _outputFormat = DataFileFormat.Csv;
        private int _selectedWorksheet;
        private string _outputAssetPath = string.Empty;
        private string _outputWorksheetName = "Data";
        private string _statusMessage;
        private MessageType _statusType = MessageType.None;

        [MenuItem("Tools/Data/Data Converter")]
        public static void Open()
        {
            DataConverterWindow window = GetWindow<DataConverterWindow>("Data Converter");
            window.minSize = new Vector2(540f, 310f);
        }

        private void OnEnable()
        {
            if (_sourceAsset == null)
                _sourceAsset = AssetDatabase.LoadMainAssetAtPath(DefaultSourcePath);

            ReloadSource(true);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Data Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            DrawSourceField();
            DrawSourceOptions();
            DrawOutputOptions();

            EditorGUILayout.Space(8f);
            DrawFormatNotice();
            DrawActions();
            DrawStatus();
        }

        private void DrawSourceField()
        {
            EditorGUI.BeginChangeCheck();
            UnityEngine.Object sourceAsset = EditorGUILayout.ObjectField(
                "Source",
                _sourceAsset,
                typeof(UnityEngine.Object),
                false);

            if (!EditorGUI.EndChangeCheck())
                return;

            _sourceAsset = sourceAsset;
            ReloadSource(true);
        }

        private void DrawSourceOptions()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Source Format",
                    _hasSourceFormat ? GetFormatLabel(_sourceFormat) : string.Empty);
            }

            if (!_hasSourceFormat || _sourceFormat != DataFileFormat.Xlsx)
                return;

            using (new EditorGUI.DisabledScope(_worksheetNames.Count == 0))
            {
                _selectedWorksheet = EditorGUILayout.Popup(
                    "Source Worksheet",
                    Mathf.Clamp(_selectedWorksheet, 0, Mathf.Max(0, _worksheetNames.Count - 1)),
                    _worksheetNames.ToArray());
            }
        }

        private void DrawOutputOptions()
        {
            DataFileFormat[] formats = GetAvailableOutputFormats();
            string[] labels = new string[formats.Length];
            int selectedIndex = 0;

            for (int i = 0; i < formats.Length; i++)
            {
                labels[i] = GetFormatLabel(formats[i]);
                if (formats[i] == _outputFormat)
                    selectedIndex = i;
            }

            using (new EditorGUI.DisabledScope(!_hasSourceFormat))
            {
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Output Format", selectedIndex, labels);
                if (EditorGUI.EndChangeCheck())
                {
                    _outputFormat = formats[newIndex];
                    UpdateDefaultOutput();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Output");
                _outputAssetPath = EditorGUILayout.TextField(_outputAssetPath);
                using (new EditorGUI.DisabledScope(!_hasSourceFormat))
                {
                    if (GUILayout.Button("...", GUILayout.Width(32f)))
                        SelectOutputPath();
                }
            }

            if (_hasSourceFormat && _outputFormat == DataFileFormat.Xlsx)
                _outputWorksheetName = EditorGUILayout.TextField("Output Worksheet", _outputWorksheetName);
        }

        private void DrawFormatNotice()
        {
            if (!_hasSourceFormat)
                return;

            if (_sourceFormat == DataFileFormat.Json || _outputFormat == DataFileFormat.Json)
            {
                EditorGUILayout.HelpBox(
                    "JSON uses a root array of flat objects. One-dimensional arrays are represented with '&' in CSV/XLSX.",
                    MessageType.Info);
                return;
            }

            if (_sourceFormat == DataFileFormat.Xlsx || _outputFormat == DataFileFormat.Xlsx)
            {
                EditorGUILayout.HelpBox(
                    "XLSX conversion handles cell values only. Formulas and workbook formatting are not converted.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("CSV output is encoded as UTF-8 with BOM.", MessageType.Info);
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_sourceAsset == null))
                {
                    if (GUILayout.Button("Open Source", GUILayout.Height(28f)))
                        AssetDatabase.OpenAsset(_sourceAsset);
                }

                using (new EditorGUI.DisabledScope(!CanConvert()))
                {
                    if (GUILayout.Button("Convert", GUILayout.Height(28f)))
                        Convert();
                }
            }
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(_statusMessage))
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        private void ReloadSource(bool updateOutput)
        {
            _worksheetNames.Clear();
            _selectedWorksheet = 0;
            _hasSourceFormat = false;
            ClearStatus();

            if (_sourceAsset == null)
            {
                if (updateOutput)
                    _outputAssetPath = string.Empty;
                return;
            }

            string sourceAssetPath = AssetDatabase.GetAssetPath(_sourceAsset);
            try
            {
                _sourceFormat = DataConversionService.GetFormat(sourceAssetPath);
                _hasSourceFormat = true;
                EnsureDifferentOutputFormat();

                if (_sourceFormat == DataFileFormat.Xlsx)
                {
                    string sourceFullPath = GetFullProjectPath(sourceAssetPath);
                    _worksheetNames.AddRange(DataConversionService.GetWorksheetNames(sourceFullPath));
                    if (_worksheetNames.Count == 0)
                        throw new InvalidDataException("The XLSX workbook has no worksheets.");
                }

                _outputWorksheetName = CreateWorksheetName(Path.GetFileNameWithoutExtension(sourceAssetPath));
                if (updateOutput)
                    UpdateDefaultOutput();
            }
            catch (Exception exception)
            {
                _hasSourceFormat = false;
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        private void EnsureDifferentOutputFormat()
        {
            if (_outputFormat != _sourceFormat)
                return;

            _outputFormat = _sourceFormat == DataFileFormat.Xlsx
                ? DataFileFormat.Csv
                : DataFileFormat.Xlsx;
        }

        private void UpdateDefaultOutput()
        {
            if (!_hasSourceFormat || _sourceAsset == null)
                return;

            string sourceAssetPath = AssetDatabase.GetAssetPath(_sourceAsset);
            string sourceDirectory = Path.GetDirectoryName(sourceAssetPath)?.Replace('\\', '/');
            string outputDirectory = GetOutputDirectory(sourceDirectory, _outputFormat);
            string filename = Path.GetFileNameWithoutExtension(sourceAssetPath)
                + DataConversionService.GetExtension(_outputFormat);

            _outputAssetPath = string.IsNullOrEmpty(outputDirectory)
                ? filename
                : outputDirectory.TrimEnd('/') + "/" + filename;
            ClearStatus();
            Repaint();
        }

        private void SelectOutputPath()
        {
            string extension = DataConversionService.GetExtension(_outputFormat).TrimStart('.');
            string currentFullPath = GetFullProjectPath(_outputAssetPath);
            string selectedPath = EditorUtility.SaveFilePanel(
                $"Select {GetFormatLabel(_outputFormat)} Output",
                Path.GetDirectoryName(currentFullPath),
                Path.GetFileNameWithoutExtension(currentFullPath),
                extension);

            if (string.IsNullOrEmpty(selectedPath))
                return;

            string projectRelativePath = FileUtil.GetProjectRelativePath(selectedPath);
            if (string.IsNullOrEmpty(projectRelativePath)
                || !projectRelativePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                SetStatus("The output must be inside the project's Assets directory.", MessageType.Error);
                return;
            }

            _outputAssetPath = projectRelativePath.Replace('\\', '/');
            ClearStatus();
            Repaint();
        }

        private bool CanConvert()
        {
            if (!_hasSourceFormat || _sourceAsset == null || string.IsNullOrWhiteSpace(_outputAssetPath))
                return false;

            if (_sourceFormat == DataFileFormat.Xlsx && _worksheetNames.Count == 0)
                return false;

            if (_outputFormat == DataFileFormat.Xlsx && string.IsNullOrWhiteSpace(_outputWorksheetName))
                return false;

            return true;
        }

        private void Convert()
        {
            try
            {
                ValidateOutputPath();
                string sourceAssetPath = AssetDatabase.GetAssetPath(_sourceAsset);
                string sourceFullPath = GetFullProjectPath(sourceAssetPath);
                string outputFullPath = GetFullProjectPath(_outputAssetPath);

                if (File.Exists(outputFullPath)
                    && !EditorUtility.DisplayDialog(
                        "Overwrite Data File",
                        $"'{_outputAssetPath}' already exists. Overwrite it?",
                        "Overwrite",
                        "Cancel"))
                {
                    return;
                }

                string sourceWorksheet = _sourceFormat == DataFileFormat.Xlsx
                    ? _worksheetNames[_selectedWorksheet]
                    : null;
                string outputWorksheet = _outputFormat == DataFileFormat.Xlsx
                    ? _outputWorksheetName
                    : null;

                DataConversionResult result = DataConversionService.Convert(
                    sourceFullPath,
                    outputFullPath,
                    sourceWorksheet,
                    outputWorksheet);

                AssetDatabase.ImportAsset(_outputAssetPath, ImportAssetOptions.ForceUpdate);
                SetStatus(
                    $"Converted {result.DataRowCount} row(s), {result.ColumnCount} column(s) to {GetFormatLabel(_outputFormat)}.",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
        }

        private void ValidateOutputPath()
        {
            string expectedExtension = DataConversionService.GetExtension(_outputFormat);
            if (string.IsNullOrWhiteSpace(_outputAssetPath)
                || !_outputAssetPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !string.Equals(Path.GetExtension(_outputAssetPath), expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The output must be a {expectedExtension} file inside the project's Assets directory.");
            }

            string outputFullPath = GetFullProjectPath(_outputAssetPath);
            string assetsDirectory = Path.GetFullPath(Application.dataPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!outputFullPath.StartsWith(assetsDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The output path resolves outside the project's Assets directory.");
        }

        private static DataFileFormat[] GetAvailableOutputFormats(DataFileFormat sourceFormat)
        {
            List<DataFileFormat> formats = new List<DataFileFormat>(2);
            foreach (DataFileFormat format in Enum.GetValues(typeof(DataFileFormat)))
            {
                if (format != sourceFormat)
                    formats.Add(format);
            }

            return formats.ToArray();
        }

        private DataFileFormat[] GetAvailableOutputFormats()
        {
            return _hasSourceFormat
                ? GetAvailableOutputFormats(_sourceFormat)
                : new[] { DataFileFormat.Csv };
        }

        private static string GetOutputDirectory(string sourceDirectory, DataFileFormat outputFormat)
        {
            if (string.IsNullOrEmpty(sourceDirectory))
                return sourceDirectory;

            string directoryName = Path.GetFileName(sourceDirectory);
            if (!string.Equals(directoryName, "Origin", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(directoryName, "CSV", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(directoryName, "JSON", StringComparison.OrdinalIgnoreCase))
            {
                return sourceDirectory;
            }

            string parent = Path.GetDirectoryName(sourceDirectory)?.Replace('\\', '/');
            string targetDirectory;
            switch (outputFormat)
            {
                case DataFileFormat.Xlsx:
                    targetDirectory = "Origin";
                    break;
                case DataFileFormat.Csv:
                    targetDirectory = "CSV";
                    break;
                case DataFileFormat.Json:
                    targetDirectory = "JSON";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outputFormat), outputFormat, null);
            }

            return string.IsNullOrEmpty(parent) ? targetDirectory : parent.TrimEnd('/') + "/" + targetDirectory;
        }

        private static string CreateWorksheetName(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return "Data";

            char[] invalidCharacters = { '[', ']', ':', '*', '?', '/', '\\' };
            string name = filename.Trim();
            for (int i = 0; i < invalidCharacters.Length; i++)
                name = name.Replace(invalidCharacters[i], '_');

            return name.Length <= 31 ? name : name.Substring(0, 31);
        }

        private static string GetFormatLabel(DataFileFormat format)
        {
            switch (format)
            {
                case DataFileFormat.Xlsx:
                    return "XLSX";
                case DataFileFormat.Csv:
                    return "CSV";
                case DataFileFormat.Json:
                    return "JSON";
                default:
                    return format.ToString();
            }
        }

        private static string GetFullProjectPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath ?? string.Empty));
        }

        private void ClearStatus()
        {
            _statusMessage = null;
            _statusType = MessageType.None;
        }

        private void SetStatus(string message, MessageType messageType)
        {
            _statusMessage = message;
            _statusType = messageType;
            Repaint();
        }
    }
}
