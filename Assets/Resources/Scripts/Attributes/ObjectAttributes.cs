using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectAttributes : MonoBehaviour
{
    public List<Attribute> attributes = new List<Attribute>();

    public void LoadAttributes(List<Attribute> newAttributes)
    {
        //Debug.Log(newAttributes.Count);
        foreach (Attribute a in newAttributes)
        {
            bool containsCopy = false;
            foreach (Attribute pa in attributes)
            {
                if (pa.Name == a.Name)
                {
                    containsCopy = true;
                    break;
                }
            }
            if (!containsCopy)
            {
                attributes.Add(a.ShallowCopy());
            }
        }
    }
    public Attribute.ChangeResult ModAttribute(string attributeName, float amount)
    {
        foreach (Attribute attribute in attributes)
            if (attribute.Name == attributeName)
                return attribute.Modify(amount);
        return Attribute.ChangeResult.None;
    }

    public Attribute.ChangeResult SetAttribute(string attributeName, float number)
    {
        foreach (Attribute attribute in attributes)
            if (attribute.Name == attributeName)
                return attribute.Set(number);
        return Attribute.ChangeResult.None;
    }

    public Attribute.ChangeResult MaximizeAttribute(string attributeName)
    {
        foreach (Attribute attribute in attributes)
            if (attribute.Name == attributeName)
                return attribute.MaxOut();
        return Attribute.ChangeResult.None;
    }

    public Attribute.ChangeResult MinimizeAttribute(string attributeName)
    {
        foreach (Attribute attribute in attributes)
            if (attribute.Name == attributeName)
                return attribute.Minimize();
        return Attribute.ChangeResult.None;
    }

    public float GetAttributeValue(string name)
    {
        foreach (Attribute a in attributes)
            if (a.Name == name)
                return a.Value;
        return 0f;
    }

    public Attribute GetAttribute(string name)
    {
        foreach (Attribute a in attributes)
            if (a.Name == name)
                return a;
        //Debug.Log("Attribute " + name + " not found " + attributes.Count);
        return null;
    }

    public void WriteAttributeToText(string attributeName, Text text)
    {
        foreach (Attribute a in attributes)
            if (a.Name == attributeName)
                a.WriteToText(text);
    }
}