using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class InfoBlock
{
    public InfoBlock parent = null;
    public string name;
    public List<InfoBlock> subBlocks;
    public Dictionary<string, string> namesValues;
    public List<string> values;
    public static List<string> textForOutput = new List<string>();

    public InfoBlock()
    {
        name = "";
        subBlocks = new List<InfoBlock>();
        namesValues = new Dictionary<string, string>();
        values = new List<string>();
    }

    public void Copy(InfoBlock original)
    {
        //name = original.name;
        subBlocks = new List<InfoBlock>();
        namesValues = new Dictionary<string, string>();
        values = new List<string>();
        foreach (KeyValuePair<string, string> kvp in original.namesValues)        
            namesValues.Add(kvp.Key, kvp.Value);
        foreach (string value in original.values)
            values.Add(value);
        foreach(InfoBlock subBlock in original.subBlocks)
        {
            InfoBlock smallerCopy = new InfoBlock();
            smallerCopy.name = subBlock.name;
            smallerCopy.Copy(subBlock);
            subBlocks.Add(smallerCopy);
        }
    }

    public void Merge(InfoBlock source)
    {
        foreach (KeyValuePair<string, string> kvp in source.namesValues)
        {
            if (namesValues.ContainsKey(kvp.Key))
                namesValues[kvp.Key] = kvp.Value;
            else
                namesValues.Add(kvp.Key, kvp.Value);
        }
        foreach (string value in source.values)
            if (!values.Contains(value))
                values.Add(value);

        if (subBlocks.Count > 0)
        {
            foreach (InfoBlock sourceSubBlock in source.subBlocks)
            {
                InfoBlock localSubBlock = GetBlock(sourceSubBlock.name);
                if (localSubBlock != null)
                    localSubBlock.Merge(sourceSubBlock);
                else
                    subBlocks.Add(sourceSubBlock);
                //foreach (InfoBlock localSubBlock in subBlocks)
                //{
                //    if (localSubBlock.name == sourceSubBlock.name)
                //    {
                //        localSubBlock.Merge(sourceSubBlock);
                //    }
                //    else
                //    {
                //        subBlocks.Add(sourceSubBlock);
                //        break;
                //    }
                //}
            }
        }
        else
        {
            subBlocks.AddRange(source.subBlocks);
        }
    }

    public string GetFullName()
    {
        string s = name;
        if (parent != null)
            s += (" of " + parent.GetFullName());
        return s;
    }

    void PrepareText(int tabLevel = 0)
    {
        //if (block.subBlocks.Count > 0)
        //    Debug.Log("block " + block.name + " has " + block.subBlocks.Count + " blocks in it");
        string tab = "";
        for (int i = 0; i < tabLevel; i++)
            tab += "\t";
        foreach (KeyValuePair<string, string> kvp in namesValues)
        {
            textForOutput.Add(tab + kvp.Key + " = " + kvp.Value);
        }
        if (values.Count > 0)
        {
            string valuesLine = tab;
            foreach (string s in values)            
                valuesLine += (s + ' ');            
            textForOutput.Add(valuesLine);
        }
        tabLevel++;
        foreach (InfoBlock ib in subBlocks)
        {
            if (ib != null)
            {
                textForOutput.Add(tab + ib.name + " = ");
                textForOutput.Add(tab + "{");
                ib.PrepareText(tabLevel);
                textForOutput.Add(tab + "}");
            }
            else
                Debug.Log("Infoblock at " + GetFullName() + " is null");
        }
    }

    public void WriteToFile(string name)
    {
        textForOutput.Clear();
        PrepareText();
        string path = Application.persistentDataPath + "/" + name;
        if (File.Exists(path))
            File.Delete(path);
        using (StreamWriter sw = new StreamWriter(path))
        {
            foreach (string s in textForOutput)            
                sw.WriteLine(s);
        }
        //File.WriteAllLines(path, textForOutput.ToArray());
        Debug.Log("Wrote " + this.name + " to " + path);
    }

    public InfoBlock GetBlock(string name)
    {
        foreach (InfoBlock i in subBlocks)        
            if (i.name == name)
                return i;        
        return null;
    }

    public bool HasBlock(string name)
    {
        foreach (InfoBlock i in subBlocks)        
            if (i.name == name)
                return true;        
        return false;
    }

    public float GetFloatRange()
    {
        if (values.Count == 2)
            return Random.Range(float.Parse(values[0]), float.Parse(values[1]));        
        return 0f;
    }

    public int GetIntRange()
    {
        if (values.Count == 2)
            return Random.Range(int.Parse(values[0]), int.Parse(values[1]));        
        return 0;
    }

    public string GetRandomValue()
    {
        if (values.Count > 0)
            return values[Random.Range(0, values.Count - 1)];
        return "dummy";
    }

    public Color32 GetColor()
    {
        byte[] rgb = { 200, 200, 200 };
        foreach (InfoBlock subBlock in subBlocks)
        {
            if (subBlock.name == "color")
            {
                for (int i = 0; i < subBlock.values.Count || i < 3; i++)
                {
                    rgb[i] = byte.Parse(subBlock.values[i]);
                }
            }
        }
        return new Color32(rgb[0], rgb[1], rgb[2], 255);
    }

    public bool GetCoordinates(out Vector2 coordinates)
    {
        float x = Mathf.Infinity, y = Mathf.Infinity;
        coordinates = new Vector2();
        string sx, sy;
        namesValues.TryGetValue("x", out sx);
        namesValues.TryGetValue("y", out sy);
        float.TryParse(sx, out x);
        float.TryParse(sy, out y);
        if (x != Mathf.Infinity && y != Mathf.Infinity)
        {
            coordinates = new Vector2(x, y);
            return true;
        }
        else
        {
            if (x == Mathf.Infinity && y != Mathf.Infinity)
                Debug.Log("Couldn't find x at " + name + " parent of which is " + parent.name);
            if (y == Mathf.Infinity && x != Mathf.Infinity)
                Debug.Log("Couldn't find y at " + name + " parent of which is " + parent.name);
            if (y == Mathf.Infinity && x == Mathf.Infinity)            
                Debug.Log("Couldn't find both x and y at " + name + " parent of which is " + parent.name);            
        }
        return false;
    }    
}

public static class InfoBlockReader
{
    static List<string> asac;
    static int blockLevel;
    static InfoBlock baseBlock;
    static InfoBlock currentBlock;
    static string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    static string cyrillicAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
    static string permittedSymbols = "_.,-?!()";

    static void BraceOpen()
    {
        if (blockLevel == 1)
        {
            InfoBlock newBlock = new InfoBlock();
            baseBlock.subBlocks.Add(newBlock);
            currentBlock = baseBlock.subBlocks.Last();
            currentBlock.parent = baseBlock;
        }
        else
        {
            InfoBlock newBlock = new InfoBlock();
            newBlock.parent = currentBlock;
            currentBlock.subBlocks.Add(newBlock);
            currentBlock = newBlock.parent.subBlocks.Last();
        }
        blockLevel++;
    }

    static void BraceClosed()
    {
        if (blockLevel > 1)
        {
            currentBlock = currentBlock.parent;
            blockLevel--;
        }
    }

    static void AddLine(string name, string value)
    {
        currentBlock.namesValues.Add(name, value);
        //Debug.Log("Added line " + currentBlock.namesValues.Keys.Last() + "=" + currentBlock.namesValues.Values.Last() + " to block " + currentBlock.name + " on block level " + blockLevel);
    }

    static void AddValue(string value)
    {
        currentBlock.values.Add(value);
        //Debug.Log("Added value " + currentBlock.values.Last() + " to block " + currentBlock.name + " on block level " + blockLevel);
    }

    static void NameBlock(string name)
    {
        currentBlock.name = name;
        //Debug.Log("Named a block: " + currentBlock.name + " on block level " + blockLevel);
    }

    static bool CharacterIsLetterOrDigit(char C)
    {
        return alphabet.Contains(C) || cyrillicAlphabet.Contains(C) || permittedSymbols.Contains(C) || char.IsDigit(C);
    }

    static void ReadAllWordsAndCharacters(string data)
    {
        asac = new List<string>();//All strings and characters
        bool readingComment = false;
        string readLine = "";
        int line = 1;
        foreach (char C in data)
        {
            if (C == '\n')
                line++;
            if (!readingComment)
            {
                if (CharacterIsLetterOrDigit(C))
                {
                    if (C != '.')
                        readLine += C;
                    else
                    {
                        bool readingString = false;
                        foreach (char letter in alphabet + cyrillicAlphabet) 
                        {
                            if (readLine.Contains(letter))
                            {
                                readingString = true;
                                break;
                            }
                        }
                        if (readingString)
                            readLine += '.';
                        else
                            readLine += ',';
                    }
                }
                else
                {
                    if (readLine != "")
                    {
                        asac.Add(readLine);
                        readLine = "";
                    }
                    if (C == '=')
                        asac.Add(C.ToString());
                    if (C == '{')
                    {
                        if (asac[asac.Count - 1][0] == '{' || asac[asac.Count - 1][0] == '}')//No named block found
                        {
                            string suitableName = "";
                            for (int i = asac.Count - 1; i >= 0; i--)
                            {
                                if (asac[i] == "{" && asac[i - 1] == "=" && asac[i - 2].Length > 1)
                                {
                                    suitableName = asac[i - 2];
                                    i = 0;
                                }
                            }
                            if (suitableName != "")
                            {
                                if (suitableName.Contains("_child_"))
                                {
                                    string[] separator = { "_child_" };
                                    string[] ns = suitableName.Split(separator, System.StringSplitOptions.None);
                                    int n = -1;
                                    int.TryParse(ns[1], out n);
                                    n++;
                                    suitableName = ns[0] + "_child_" + n.ToString();
                                }
                                else
                                {
                                    suitableName += "_child_1";
                                }
                                //Debug.Log("seems like a nameless block at " + line.ToString() + ", going to name it " + suitableName);
                                asac.Add(suitableName);
                                asac.Add("=");
                                asac.Add(C.ToString());
                            }
                        }
                        else
                            asac.Add(C.ToString());
                    }
                    if (C == '}')
                        asac.Add(C.ToString());
                    if (C == '#')
                        readingComment = true;
                }
            }
            else if (C == '\n')
                readingComment = false;
        }
    }

    public static InfoBlock SplitTextIntoInfoBlocks(string data)
    {
        baseBlock = new InfoBlock { name = "Primary block" };
        currentBlock = baseBlock;
        blockLevel = 1;
        ReadAllWordsAndCharacters(data);
        for (int i = 0; i < asac.Count; i++)
        {
            char c = asac[i][0];
            if (c == '=')
            {
                if (asac[i + 1][0] == '{')//If next string is open brace
                {
                    BraceOpen();
                    NameBlock(asac[i - 1]);
                }
                else
                {
                    AddLine(asac[i - 1], asac[i + 1]);
                    i++;
                }
            }
            else if (c == '}')
            {
                BraceClosed();
            }
            else if (char.IsLetterOrDigit(asac[i + 1][0]) || asac[i + 1][0] == '}')//If we're at string and next line is also a string or the block ends
            {
                if (char.IsLetterOrDigit(c))
                {
                    AddValue(asac[i]);
                }
            }
        }
        //Debug.Log(baseBlock.name + " contains " + baseBlock.subBlocks.Count.ToString());
        return baseBlock;
    }
}