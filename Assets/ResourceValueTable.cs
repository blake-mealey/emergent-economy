using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceValueTable : MonoBehaviour {

    public float[,] resourceValues;
    private float adjustmentRate = 0.1f; //How quickly value adjustments should be made

	// Use this for initialization
	void Start () {

        //Initial setup of the resource table
        int temp = SimManager.instance.numberOfGroups;
        resourceValues = new float[temp, temp];
        for (int i = 0; i < temp; i++) {
            for (int j = 0; j < temp; j ++) {
                if (i == j) resourceValues[i, j] = 0f; //ensure trading the same resource for itself yields no value
                else resourceValues[i, j] = 1f; //otherwise default value of one
            }
        }
	}
	
    //Adjusts the table based on how quick the table reacts to trades
    public void UpdateValue (int resource1, float value1, int resource2, float value2) {
        float ratio = value2 / value1;
        resourceValues[resource1, resource2] = resourceValues[resource1, resource2] * (1f - adjustmentRate) + ratio * adjustmentRate;
        ratio = 1f / ratio;
        resourceValues[resource2, resource1] = resourceValues[resource2, resource1] * (1f - adjustmentRate) + ratio * adjustmentRate;
    }

    //Used to set the adjustment rate of the table
    public void SetAdjustmentRate (float rate) {
        if (rate >= 0 && rate <= 1f) {
            adjustmentRate = rate;
        }
    }
}
