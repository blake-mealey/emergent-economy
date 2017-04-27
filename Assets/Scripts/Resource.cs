using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This class is the controller for each individual resource node
public class Resource : MonoBehaviour {

	// The group this resource belongs too
    public int id = -1;
    public float resourcesLeft = 1000f;
    public float startingResourceAmount = 1000f;

	// Used to lower this resource slowly
    private Vector3 startPos = new Vector3();


    void Start() {
        startPos = transform.position;
    }

    void Update() {
        
		// If there are no resources left, deregister with the sim manager and hide under the map.
		if (resourcesLeft <= 0) {
			SimManager.instance.DeregisterResource(gameObject);
			transform.position = startPos - new Vector3(0, 5f, 0);
            enabled = false;
        } else { //Otherwise reposition this resource based on the ratio of resources left to starting resources
			transform.position = Vector3.Lerp(startPos, startPos - new Vector3(0, 4f, 0), 1f - (resourcesLeft / startingResourceAmount));
		}
    }

	// Semi-constructor of the resource node
    public void initialize (int nid, float nresources) {
        id = nid;
        resourcesLeft = nresources;
        startingResourceAmount = nresources;
    }

	// Called by agents in order to mine from this resource. Returns either the amount requested or the amount left
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
