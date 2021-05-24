using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Tilemaps;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]
public class DynamicWallCaps : MonoBehaviour
{
#if UNITY_EDITOR
    void Update()
    {
        //Tilemap.tilemapTileChanged += (tilemap, Synctile) => AddOrRemoveNeighbouringCaps(tilemap, Synctile);
    }

    Vector3Int RotateVector(Vector3Int vector3Int)
    {
        return new Vector3Int(vector3Int.y, vector3Int.x, vector3Int.z);
    }

    void CheckForAdjacentWallsAndFill(Vector3Int tilePosition, Tilemap capsTilemap, Tilemap verticalTilemap, Tilemap horizontalTilemap, Sprite tileSprite, bool vertical = true)
    {
        Vector3Int[] cornerWallOffsets = { Vector3Int.zero, Vector3Int.left };
        //Vector3Int[] wallOffsets = { Vector3Int.up, Vector3Int.down };
        Vector3Int[] capPositions = { Vector3Int.zero, Vector3Int.up };
        Vector3Int offsetPosition;
        Tilemap primaryTilemap;
        Tilemap cornerTileMap;
        if (vertical)
        {
            primaryTilemap = verticalTilemap;
            cornerTileMap = horizontalTilemap;
        }
        else
        {
            primaryTilemap = horizontalTilemap;
            cornerTileMap = verticalTilemap;
        }
        for (int k = 0; k < capPositions.Length; k++)
        {
            Vector3Int currentCapPosition = tilePosition;
            if (vertical)
                currentCapPosition += capPositions[k];
            else
                currentCapPosition += RotateVector(capPositions[k]);
            if (capsTilemap.GetTile(currentCapPosition) != null)
                continue;
            bool capHasCornerWalls = false;
            for (int i = 0; i < cornerWallOffsets.Length; i++)
            {
                if (vertical)
                    offsetPosition = tilePosition + cornerWallOffsets[i] + capPositions[k];
                else
                    offsetPosition = tilePosition + (cornerWallOffsets[i] + capPositions[k]) * -1;
                if (cornerTileMap.GetSprite(offsetPosition) != null)
                {
                    Debug.Log("Adjacent secondary tile is at " + offsetPosition);
                    capHasCornerWalls = true;
                }
            }
            //bool capHasAdjacentWalls = false;
            //for (int i = 0; i < wallOffsets.Length; i++)
            //{
            //    if (vertical)
            //        offsetPosition = tilePosition + wallOffsets[i];
            //    else
            //        offsetPosition = tilePosition + RotateVector(wallOffsets[i]);
            //    if (primaryTilemap.GetSprite(offsetPosition) != null)
            //    {
            //        Debug.Log("Adjacent primary tile is at " + offsetPosition);
            //        capHasAdjacentWalls = true;
            //    }
            //}
            if (capHasCornerWalls/* && !capHasAdjacentWalls*/)
            {
                string tilename = tileSprite.name.Remove(tileSprite.name.Length - 1, 1) + "cap";
                Tile newTile = (Tile)AssetDatabase.LoadAssetAtPath("Assets/Resources/Graphics/Walls/" + tilename + ".asset", typeof(Tile));
                Debug.Log("Setting tile " + tilename + " at " + currentCapPosition);
                if (newTile != null)
                    capsTilemap.SetTile(currentCapPosition, newTile);
                else
                    Debug.Log("Could not find tile " + tilename);
            }
            else
                Debug.Log("No need for caps");
        }
    }

    void AddOrRemoveNeighbouringCaps(Tilemap tilemap, Tilemap.SyncTile[] syncTiles)
    {
        for (int i = 0; i < 1/*syncTiles.Length*/; i++)
        {
            string objectName = tilemap.gameObject.name;
            Tilemap verticalTileMap = transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Tilemap>();
            if (verticalTileMap == null)
                return;
            Tilemap horizontalTileMap = transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<Tilemap>();
            if (horizontalTileMap == null)
                return;
            Tilemap capsTilemap = transform.GetChild(0).GetChild(2).GetChild(0).GetComponent<Tilemap>();
            if (capsTilemap == null)
                return;
            switch (objectName)
            {
                case "WallsVertical":
                    if (syncTiles[i].tile == null)
                    {
                        Debug.Log("Will remove unneeded caps... some day");
                        return;
                    }
                    CheckForAdjacentWallsAndFill(syncTiles[i].position, capsTilemap, verticalTileMap, horizontalTileMap, syncTiles[i].tileData.sprite);
                    break;
                case "WallsHorizontal":
                    if (syncTiles[i].tile == null)
                    {
                        Debug.Log("Will remove unneeded caps... some day");
                        return;
                    }
                    CheckForAdjacentWallsAndFill(syncTiles[i].position, capsTilemap, verticalTileMap, horizontalTileMap, syncTiles[i].tileData.sprite, false);
                    break;
            }
        }
    }
#endif
}