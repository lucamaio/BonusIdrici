using System;
using System.Collections.Generic;
using System.IO;

public class FileReader
{
    public static List<string> ReadLines(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file at path {filePath} was not found.");
        }

        var lines = new List<string>();
        using (var reader = new StreamReader(filePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
        }
        return lines;
    }
}