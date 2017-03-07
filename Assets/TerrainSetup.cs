using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSetup : MonoBehaviour {

    public float[,] heightRawData;// = new float[257, 257];
    public float[,] heightCondensedData;// = new float[26, 26];

    // Use this for initialization
    void Start() {

        Terrain myTerrain = GetComponent<Terrain>();
        heightRawData = new float[myTerrain.terrainData.heightmapWidth, myTerrain.terrainData.heightmapHeight];
        heightCondensedData = new float[myTerrain.terrainData.heightmapWidth / 10 + 1, myTerrain.terrainData.heightmapHeight / 10 + 1];

        Random.InitState(0);

        //Generate random data. This is where various terrain stuff would come into effect
        for (int j = 1; j < heightCondensedData.GetLength(0) - 1; j++) {
            for (int k = 1; k < heightCondensedData.GetLength(1) - 1; k++) {
                if (j == 1 || k == 1 || j == heightCondensedData.GetLength(0) - 2 || k == heightCondensedData.GetLength(1) - 2)
                    heightCondensedData[j, k] = 0.01f;
                else heightCondensedData[j, k] = 0f; // Random.Range(0f, 0.001f);// + 0.005f*j;
            }
        }

        //print2dArray(heightCondensedData);

        //Normalize data to get smooth slopes
        for (int j = 0; j < heightRawData.GetLength(0); j++) {
            for (int k = 0; k < heightRawData.GetLength(1); k++) {
                float tempx = j / 10f;
                float tempy = k / 10f;
                int basex = (int)tempx;
                int basey = (int)tempy;
                tempx -= basex;
                tempy -= basey;

                //Normalize the data
                heightRawData[j, k] = getCondensedHeightValue(basex, basey) * (1f - tempx) * (1f - tempy) + getCondensedHeightValue(basex + 1, basey) * tempx * (1f - tempy)
                    + getCondensedHeightValue(basex, basey + 1) * (1f - tempx) * tempy + getCondensedHeightValue(basex + 1, basey + 1) * tempx * tempy;
            }
        }

        //print2dArray(heightRawData);

        myTerrain.terrainData.SetHeightsDelayLOD(0, 0, heightRawData);
        myTerrain.ApplyDelayedHeightmapModification();
    }

    private float getCondensedHeightValue(int basex, int basey) {
        if (basex < heightCondensedData.GetLength(0) && basey < heightCondensedData.GetLength(1)) return heightCondensedData[basex, basey];
        return 0f;
    }
}
