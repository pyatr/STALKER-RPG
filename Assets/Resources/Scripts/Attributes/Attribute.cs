using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Attribute
{
    public class AttributeGrade
    {
        public string name;
        public Dictionary<float, string> gradesAndNames = null;
        Color32[] gradeColors = null;
        public bool showPrecise;

        public AttributeGrade()
        {
            name = "Dummy";
            showPrecise = true;
        }

        public void LoadGradeColors(string[] gradeColors)
        {
            if (gradeColors != null)
            {
                this.gradeColors = new Color32[gradeColors.Length];
                for (int i = 0; i < gradeColors.Length; i++)
                {
                    string[] channels = gradeColors[i].Split('_');
                    this.gradeColors[i] = new Color32(byte.Parse(channels[0]), byte.Parse(channels[1]), byte.Parse(channels[2]), 255);
                }
            }
        }

        public string GiveValueRating(float n)
        {
            string value = n.ToString();
            if (!showPrecise)
            {
                int currentGrade;
                for (currentGrade = 0; currentGrade < gradesAndNames.Count; currentGrade++)
                    if (gradesAndNames.Keys.ElementAt(currentGrade) > n)
                        break;
                currentGrade = Mathf.Clamp(currentGrade, 0, gradesAndNames.Count - 1);
                value = gradesAndNames.Values.ElementAt(currentGrade);
            }
            return value;
        }

        public void WriteValue(float n, Text text)
        {
            string value = n.ToString();
            int currentGrade = -1;
            if (gradesAndNames != null && !showPrecise)
            {
                for (currentGrade = 0; currentGrade < gradesAndNames.Count; currentGrade++)
                    if (gradesAndNames.Keys.ElementAt(currentGrade) > n)
                        break;
                currentGrade = Mathf.Clamp(currentGrade, 0, gradesAndNames.Count - 1);
                if (!showPrecise)
                    value = gradesAndNames.Values.ElementAt(currentGrade);
            }
            text.text = value;
            if (gradeColors != null && currentGrade >= 0)
                text.color = gradeColors[currentGrade];
        }
    }

    public enum ChangeResult
    {
        None,
        BelowMin,
        AboveMax
    }

    public float value, minValue, maxValue;
    string _name = "Dummy";
    AttributeGrade grade = null;

    public string name
    {
        get
        {
            return _name;
        }
    }

    public Attribute(string name, float value, float minValue = 0, float maxValue = 100)
    {
        _name = name.Replace('_', ' ');
        this.value = value;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }

    public void SetGrade(AttributeGrade grade)
    {
        this.grade = grade;
    }

    public void DisplayPreciseValue(bool precise)
    {
        if (grade != null)
            grade.showPrecise = precise;
    }

    public void WriteToText(Text text)
    {
        if (grade != null)
            grade.WriteValue(value, text);
        else
            text.text = value.ToString();
    }

    public ChangeResult Modify(float modifier)
    {
        value += modifier;
        ChangeResult result = ChangeResult.None;
        if (value <= minValue)
            result = ChangeResult.BelowMin;
        else if (value >= maxValue)
            result = ChangeResult.AboveMax;
        value = Mathf.Clamp(value, minValue, maxValue);
        return result;
    }

    public ChangeResult Set(float number)
    {
        value = number;
        ChangeResult result = ChangeResult.None;
        if (value <= minValue)
            result = ChangeResult.BelowMin;
        else if (value >= maxValue)
            result = ChangeResult.AboveMax;
        value = Mathf.Clamp(value, minValue, maxValue);
        //Debug.Log(name + "=" + value.ToString() + "(" + minValue + ";" + maxValue + ")");
        return result;
    }
    public ChangeResult MaxOut()
    {
        //Debug.Log("Maxed out " + name + ": from " + value.ToString() + " to " + maxValue.ToString());
        value = maxValue;
        return ChangeResult.AboveMax;
    }

    public ChangeResult Minimize()
    {
        value = minValue;
        return ChangeResult.BelowMin;
    }

    public string GetValueNonprecise()
    {
        if (grade != null)        
            return grade.GiveValueRating(value);        
        return value.ToString();
    }

    public float GetValue()
    {
        //Debug.Log(name + " = " + value);
        return value;
    }

    public Attribute ShallowCopy()
    {
        return (Attribute)this.MemberwiseClone();
    }
}