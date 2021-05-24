using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class RestrictToGrid : MonoBehaviour
{
    float cellSizeX = 0.40f;
    float cellSizeY = 0.56f;

#if UNITY_EDITOR
    void Update()
    {
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            var children = transform.GetComponentInChildren<Transform>();
            foreach (Transform child in children)
            {
                float posX = child.localPosition.x;
                float posY = child.localPosition.y;
                int modX = 1, modY = 1;
                if (posX < 0)
                    modX = -1;
                if (posY < 0)
                    modY = -1;
                float alignedX = posX - posX % cellSizeX;
                float alignedY = posY - posY % cellSizeY;
                child.localPosition = new Vector3(alignedX + 0.5f * cellSizeX * modX, alignedY + 0.5f * cellSizeY * modY, child.localPosition.z);
            }
        }
    }
#endif
}