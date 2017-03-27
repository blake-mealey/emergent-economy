using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceMonitor : MonoBehaviour {

	public GameObject stackCube;
	public AgentScript myAgent;
	private GameObject[] resourceStacks;

	private float prevTotal = 0f;

	private void Start() {
		resourceStacks = new GameObject[SimManager.instance.numberOfGroups];

		for (int j = 0; j < resourceStacks.Length; j++) {
			resourceStacks[j] = Instantiate(stackCube, transform);
			resourceStacks[j].transform.localPosition = new Vector3(0, 0.5f, 0);
			resourceStacks[j].GetComponent<Renderer>().material.color = SimManager.instance.colors[j];
			resourceStacks[j].transform.localScale = new Vector3(0.5f, 0f, 0.5f);
			resourceStacks[j].SetActive(false);
		}
	}

	// Update is called once per frame
	void Update () {
		if (!myAgent.gameObject.activeSelf) Destroy(gameObject);

		transform.position = myAgent.transform.position + new Vector3(0, 0.5f, 0);

		if (prevTotal == myAgent.totalResources) return; //Do nothing if the total resources has not changed

		float[] resources = myAgent.resources;

		float currentY = 0.5f;
		for (int j = 0; j < resources.Length; j++) {
			if (resources[j] > 0) {
				resourceStacks[j].SetActive(true);
				resourceStacks[j].transform.localScale = new Vector3(0.5f, resources[j] / 40f, 0.5f);
				resourceStacks[j].transform.localPosition = new Vector3(0, currentY + (resourceStacks[j].transform.localScale.y / 2f), 0);
				currentY += resourceStacks[j].transform.localScale.y;
			} else {
				resourceStacks[j].SetActive(false);
			}
		}
	}
}
