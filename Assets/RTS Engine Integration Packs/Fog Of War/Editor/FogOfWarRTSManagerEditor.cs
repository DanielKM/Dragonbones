using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;
using FoW;
using UnityEditor;

[CustomEditor(typeof(FogOfWarRTSManager))]
public class RTS_FoWEditor : Editor {

    public override void OnInspectorGUI()
    {
        //draw the default inspector in the beginning
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        //create a button that (when clicked) checks if all units and buildings prefabs have correctly FoW set up

        if (GUILayout.Button("Check prefabs for Fog of War components"))
        {
            //go through all unit/building prefabs
            Object[] prefabs = Resources.LoadAll("Prefabs", typeof(GameObject));

            bool error = false;

            foreach (GameObject obj in prefabs)
            {
                Entity entity = obj.GetComponent<Entity>();
                if (entity == null)
                    continue;

                FactionEntity factionEntity = obj.GetComponent<FactionEntity>();

                //if this a unit or a building
                if (factionEntity)
                {
                    //check if it doesn't have the FoW Unit component:
                    if (obj.GetComponent<FogOfWarUnit>() == null)
                    {
                        //print an error:
                        Debug.LogError("[FogOfWarRTSManager] The FogOfWarUnit component of faction entity (unit/building) 'Name = " + factionEntity.GetName() + "' has not been assigned!");
                        error = true;
                    }
                    else
                    {
                        //if they do have that component make sure to have disabled per default
                        obj.GetComponent<FogOfWarUnit>().enabled = false;
                    }

                }

                //if the player selection of the prefab does not have the HideInFog component
                if (entity.GetSelection().gameObject.GetComponent<HideInFogRTS>() == null)
                {
                    //print an error:
                    Debug.LogError("[FogOfWarRTSManager] The HideInFogRTS component of the selection object of the faction entity (unit/building/resource) 'Name = " + entity.GetName() + "' has not been assigned!");
                    error = true;
                }
            }

            if (error == false)
                Debug.Log("[FogOfWarRTSManager] All Building, Unit and Resource prefabs have FoW correctly setup.");
        }
    }
}
