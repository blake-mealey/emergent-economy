using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour {

    public int id = -1;
    public float resourcesLeft = 1000f;
    public float startingResourceAmount = 1000f;

    private int miners = 0;

    private Vector3 startPos = new Vector3();


    void Start() {
        startPos = transform.position;
    }

    void Update() {
        

		if (resourcesLeft <= 0) {
			SimManager.instance.DeregisterResource(gameObject);
			transform.position = startPos - new Vector3(0, 5f, 0);
		} else {
			transform.position = Vector3.Lerp(startPos, startPos - new Vector3(0, 4f, 0), 1f - (resourcesLeft / startingResourceAmount));
		}
    }

    public void initialize (int nid, float nresources) {
        id = nid;
        resourcesLeft = nresources;
        startingResourceAmount = nresources;
    }

    public float mineResource (float amount) {
        if (resourcesLeft - amount < 0f) {
			float temp = resourcesLeft;
            resourcesLeft = 0;
            return temp;
        }
		resourcesLeft -= amount;
        return amount;
    }

	public bool isEmpty() { return resourcesLeft <= 0; }
}
