using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectAttributes : MonoBehaviour
{
    public List<Attribute> attributes = new List<Attribute>();

    public void LoadAttributes(List<Attribute> attributes)
    {
        foreach (Attribute a in attributes)
        {
            bool containsCopy = false;
            foreach (Attribute pa in this.attributes)
            {
                if (pa.name == a.name)
                {
                    containsCopy = true;
                    break;
                }
            }
            if (!containsCopy)
                this.attributes.Add(a.ShallowCopy());
        }
    }
    public Attribute.ChangeResult ModAttribute(string attributeName, float amount)
    {
        foreach (Attribute attribute in attributes)
            if (attribute.name == attributeName)
                return attribute.Modify(amount);
        return Attribute.ChangeResult.None;
    }

    public Attribute.ChangeResult SetAttribute(string attributeName, float number)
    {
        foreach (Attribute attribute in attributes)
            if (attribute.name == attributeName)
                return attribute.Set(number);
        return Attribute.ChangeResult.None;
    }

    public Attribute.ChangeResult MaximizeAttribute(string attributeName)
    {
        foreach (Attribute attribute in attributes)
            if (attribute.name == attributeName)
                return attribute.MaxOut();
        return Attribute.ChangeResult.None;
    }

    public Attribute.ChangeResult MinimizeAttribute(string attributeName)
    {
        foreach (Attribute attribute in attributes)
            if (attribute.name == attributeName)
                return attribute.Minimize();
        return Attribute.ChangeResult.None;
    }

    public float GetAttributeValue(string name)
    {
        foreach (Attribute a in attributes)
            if (a.name == name)         
                return a.GetValue();            
        return 0f;
    }

    public Attribute GetAttribute(string name)
    {
        foreach (Attribute a in attributes)
            if (a.name == name)
                return a;
        return null;
    }

    public void WriteAttributeToText(string attributeName, Text text)
    {
        foreach (Attribute a in attributes)
            if (a.name == attributeName)
                a.WriteToText(text);
    }
}