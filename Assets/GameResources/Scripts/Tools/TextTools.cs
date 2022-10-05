using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class TextTools
{
    public static string FirstCharToUpper(string input)
    {
        return input.First().ToString().ToUpper() + input.Substring(1);
    }

    public static List<string> GetTextFromFile(string url)
    {
        List<string> newList = new List<string>();
        if (File.Exists(url))
        {
            newList = File.ReadAllLines(url).ToList();
            //using (StreamReader reader = new StreamReader(url))
            //    while (!reader.EndOfStream)
            //        newList.Add(reader.ReadLine());
        }
        return newList;
    }

    public static string GetTextFromFileAsString(string url)
    {
        string text = "";
        if (File.Exists(url))
        {
            text = File.ReadAllText(url);
            //using (StreamReader reader = new StreamReader(url))
            //    text = reader.ReadToEnd();
        }
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
                sw.WriteLine(s);
            sw.Close();
        }
    }
}