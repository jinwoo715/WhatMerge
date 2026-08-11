using System;
using System.Collections.Generic;
using System.IO;
using WhatMerge.EditorTools.DataConversion.Readers;
using WhatMerge.EditorTools.DataConversion.Writers;

namespace WhatMerge.EditorTools.DataConversion
{
    internal static class DataConversionService
    {
        public static DataFileFormat GetFormat(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
                return DataFileFormat.Xlsx;

            if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
                return DataFileFormat.Csv;

            if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
                return DataFileFormat.Json;

            throw new NotSupportedException($"Unsupported data file extension: '{extension}'.");
        }

        public static string GetExtension(DataFileFormat format)
        {
            switch (format)
            {
                case DataFileFormat.Xlsx:
                    return ".xlsx";
                case DataFileFormat.Csv:
                    return ".csv";
                case DataFileFormat.Json:
                    return ".json";
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        public static IReadOnlyList<string> GetWorksheetNames(string xlsxPath)
        {
            return XlsxDataReader.GetWorksheetNames(xlsxPath);
        }

        public static DataConversionResult Convert(
            string sourcePath,
            string outputPath,
            string sourceWorksheetName = null,
            string outputWorksheetName = null)
        {
            ValidateSourcePath(sourcePath);

            DataFileFormat sourceFormat = GetFormat(sourcePath);
            DataFileFormat outputFormat = GetFormat(outputPath);
            if (sourceFormat == outputFormat)
                throw new InvalidOperationException("The source and output formats must be different.");

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new InvalidOperationException("The output directory is invalid.");

            ITabularDataReader reader = CreateReader(sourceFormat);
            ITabularDataWriter writer = CreateWriter(outputFormat);
            TabularData data = reader.Read(sourcePath, sourceWorksheetName);

            Directory.CreateDirectory(outputDirectory);
            writer.Write(outputPath, data, outputWorksheetName);
            return new DataConversionResult(data.Rows.Count, data.Headers.Count);
        }

        private static ITabularDataReader CreateReader(DataFileFormat format)
        {
            switch (format)
            {
                case DataFileFormat.Xlsx:
                    return new XlsxDataReader();
                case DataFileFormat.Csv:
                    return new CsvDataReader();
                case DataFileFormat.Json:
                    return new JsonDataReader();
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static ITabularDataWriter CreateWriter(DataFileFormat format)
        {
            switch (format)
            {
                case DataFileFormat.Xlsx:
                    return new XlsxDataWriter();
                case DataFileFormat.Csv:
                    return new CsvDataWriter();
                case DataFileFormat.Json:
                    return new JsonDataWriter();
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static void ValidateSourcePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("The source data file was not found.", path);
        }
    }
}
