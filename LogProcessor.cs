using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class LogProcessor
{
    // Паттерны для распознавания форматов логов
    private static readonly Regex Format1Pattern = new Regex(
        @"^(?<date>\d{2}\.\d{2}\.\d{4})\s(?<time>\d{2}:\d{2}:\d{2}\.\d{3})\s(?<level>[A-Z]+)\s+(?<message>.+)$",
        RegexOptions.Compiled);

    private static readonly Regex Format2Pattern = new Regex(
        @"^(?<date>\d{4}-\d{2}-\d{2})\s(?<time>\d{2}:\d{2}:\d{2}\.\d{4})\|\s*(?<level>[A-Z]+)\|\d+\|(?<method>[^|]+)\|(?<message>.+)$",
        RegexOptions.Compiled);

    // Маппинг уровней логирования
    private static readonly Dictionary<string, string> LogLevelMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["INFORMATION"] = "INFO",
        ["WARNING"] = "WARN",
        ["ERROR"] = "ERROR",
        ["DEBUG"] = "DEBUG",
        ["INFO"] = "INFO",
        ["WARN"] = "WARN"
    };

    public void ProcessLogs(string inputFilePath, string outputFilePath, string problemsFilePath)
    {
        var processedLines = 0;
        var problemLines = 0;

        using (var reader = new StreamReader(inputFilePath, Encoding.UTF8))
        using (var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
        using (var problemsWriter = new StreamWriter(problemsFilePath, false, Encoding.UTF8))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (TryProcessLine(line, out var standardizedLine))
                {
                    writer.WriteLine(standardizedLine);
                    processedLines++;
                }
                else
                {
                    problemsWriter.WriteLine(line);
                    problemLines++;
                }
            }
        }

        Console.WriteLine($"Обработка завершена. Успешно: {processedLines}, Проблемных: {problemLines}");
    }

    private bool TryProcessLine(string line, out string standardizedLine)
    {
        standardizedLine = null;

        // Пытаемся распознать первый формат
        var match = Format1Pattern.Match(line);
        if (match.Success)
        {
            return TryCreateStandardizedLine(
                match.Groups["date"].Value,
                match.Groups["time"].Value,
                match.Groups["level"].Value,
                "DEFAULT",
                match.Groups["message"].Value.Trim(),
                out standardizedLine);
        }

        // Пытаемся распознать второй формат
        match = Format2Pattern.Match(line);
        if (match.Success)
        {
            return TryCreateStandardizedLine(
                match.Groups["date"].Value,
                match.Groups["time"].Value,
                match.Groups["level"].Value,
                match.Groups["method"].Value.Trim(),
                match.Groups["message"].Value.Trim(),
                out standardizedLine);
        }

        return false;
    }

    private bool TryCreateStandardizedLine(
        string dateStr,
        string timeStr,
        string levelStr,
        string methodStr,
        string messageStr,
        out string standardizedLine)
    {
        standardizedLine = null;

        try
        {
            // Преобразование даты в единый формат
            DateTime date;
            if (dateStr.Contains('.'))
            {
                // Формат DD.MM.YYYY
                if (!DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    return false;
            }
            else
            {
                // Формат YYYY-MM-DD
                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    return false;
            }

            var formattedDate = date.ToString("dd-MM-yyyy");

            // Нормализация уровня логирования
            if (!LogLevelMapping.TryGetValue(levelStr, out var normalizedLevel))
                normalizedLevel = "UNKNOWN";

            // Формируем итоговую строку
            standardizedLine = $"{formattedDate}\t{timeStr}\t{normalizedLevel}\t{methodStr}\t{messageStr}";
            return true;
        }
        catch
        {
            return false;
        }
    }
}