using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatMap : MonoBehaviour {

    public static HeatMap instance;

    private Texture2D texture;
    private float[,] heat;
    private Color[,] colors;

    public float disipateRate;

    public int resolution;

	// Use this for initialization
	void Start () {
        instance = this;

        heat = new float[resolution, resolution];
        colors = new Color[resolution, resolution];

        texture = new Texture2D(resolution, resolution);
        GetComponent<Renderer>().material.mainTexture = texture;

        StartCoroutine(UpdateFloor());
	}

    IEnumerator UpdateFloor() {
        while (true) {

            for (int y = 0; y < texture.height; y++) {
                for (int x = 0; x < texture.width; x++) {
                    //Color color = new Color(heat[x,y]/100f, 0f, 0f);
                    Color color = colors[x, y];
                    texture.SetPixel(x, y, color);

                    heat[x, y] *= (1 - disipateRate);
                }
            }
            texture.Apply();

            yield return new WaitForSeconds(0.25f);
        }
    }

    public void AddHeat(Vector3 pos, Color color) {
        pos *= (resolution / 100f);
        int x = resolution - (int)pos.x;
        int y = resolution - (int)pos.z;
        heat[x, y] += 1f;
        colors[x, y] += color / 1000f;
    }
}
