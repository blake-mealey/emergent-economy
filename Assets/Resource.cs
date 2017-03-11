using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour {

    public int id = -1;
    public float resourcesLeft = 1000f;
    public float startingResourceAmount = 1000f;
    public int minerLimit = 10;

    private int miners = 0;

    private Vector3 startPos = new Vector3();


    void Start() {
        startPos = transform.position;
    }

    void Update() {
        transform.position = Vector3.Lerp(startPos,startPos - new Vector3(0,4f,0), 1f-(resourcesLeft/startingResourceAmount));
    }

    public void initialize (int nid, float nresources, int nminerLimit) {
        id = nid;
        resourcesLeft = nresources;
        startingResourceAmount = nresources;
        minerLimit = nminerLimit;
    }

    public bool requestMiningSlot () {
        if (minerLimit > miners && resourcesLeft > 0f) {
            miners++;
            return true;
        }
        return false;
    }

    public void releaseMiningSlot () {
        miners--;
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
}
