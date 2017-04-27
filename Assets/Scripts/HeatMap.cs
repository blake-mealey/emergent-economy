using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatMap : MonoBehaviour {

    public static HeatMap instance;

    private Texture2D texture;
    private Color[,] colors;

    public int resolution;

	// Use this for initialization
	void Start () {
        // singleton-ish
        instance = this;
        
        // initialization
        colors = new Color[resolution, resolution];
        texture = new Texture2D(resolution, resolution);
        GetComponent<Renderer>().material.mainTexture = texture;

        // start update loop
        StartCoroutine(UpdateFloor());
	}
    
    IEnumerator UpdateFloor() {
        while (true) {
            // Iterate the texture coords
            for (int y = 0; y < texture.height; y++) {
                for (int x = 0; x < texture.width; x++) {
                    // Get the color from the colors array and update the texture map
                    Color color = colors[x, y];
                    texture.SetPixel(x, y, color);
                }
            }
            // update the texture
            texture.Apply();

            // repeat every .25 seconds (times speed scale)
            yield return new WaitForSeconds(0.25f);
        }
    }

    // Add a bit of color to the map at a given position
    public void AddHeat(Vector3 pos, Color color) {
        // convert position to map coords
        pos *= (resolution / 100f);
        int x = resolution - (int)pos.x;
        int y = resolution - (int)pos.z;
        // add some of the color to the map
        colors[x, y] += color / 25f;
    }
}
