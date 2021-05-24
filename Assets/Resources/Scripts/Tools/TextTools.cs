using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class TextTools
{
    static string[] supportedFormats = { "txt", "csv", "lua" };

    static bool IsValidFile(string url)
    {
        for (int i = 0; i < 3; i++)
        {
            Int32 count = 2;
            char[] separator = { '.' };
            string[] s = url.Split(separator, count);
            if (s[1] == supportedFormats[i])
                return true;
        }
        return false;
    }

    public static List<string> GetTextFromFile(string url)
    {
        List<string> newList = new List<string>();
        if (!IsValidFile(url))
            return newList;
        using (StreamReader reader = new StreamReader(url))
            while (!reader.EndOfStream)
                newList.Add(reader.ReadLine());
        return newList;
    }

    public static string GetTextFromFileAsString(string url)
    {
        string text = "";
        if (!IsValidFile(url))
            return text;
        using (StreamReader reader = new StreamReader(url))
            text = reader.ReadToEnd();
        return text;
    }

    public static string ParseStringForNumber(string s)
    {
        string s2 = "";
        foreach (char c in s)
            if (char.IsNumber(c))
                s2 += c;
        return s2;
    }

    public static void WriteToFile(string name, List<string> lines)
    {
        string path = Application.persistentDataPath + @"\" + name + ".txt";
        StreamWriter sw;
        if (File.Exists(path))
            File.Delete(path);
        using (sw = File.CreateText(path))
        {
            foreach (string s in lines)
            {
                sw.WriteLine(s);
            }
            sw.Close();
        }
    }
}