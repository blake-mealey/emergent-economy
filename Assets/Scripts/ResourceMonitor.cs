using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class controls the little cubes that appear above the agents heads when they are holding a resource
public class ResourceMonitor : MonoBehaviour {

	public GameObject stackCube; //The prefab of the cube
	public AgentScript myAgent; //The agent I am attached to
	private GameObject[] resourceStacks; //The array of stacks. 1 cube for each resource type

	// Used to determine if the resource amount my agent is carrying has changed since the last time I checked
	private float prevTotal = 0f;

	private void Start() {
		// Create all of the cube prefabs and give them the appriopriate colour
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
		if (!myAgent.gameObject.activeSelf) Destroy(gameObject); // If my agent is dead, destory me

		transform.position = myAgent.transform.position + new Vector3(0, 0.5f, 0); //Move this gameobject to the position of the agent, but a bit higher.

		if (prevTotal == myAgent.totalResources) return; //Do nothing if the total resources has not changed

		float[] resources = myAgent.resources; //Otherwise copy the agents resource numbers

		float currentY = 0.5f; // The starting Y
		for (int j = 0; j < resources.Length; j++) {
			if (resources[j] > 0.25f) { //If the agent has over 0.25 of a resource, show that cube
				resourceStacks[j].SetActive(true);
				resourceStacks[j].transform.localScale = new Vector3(0.5f, resources[j] / 40f, 0.5f); //Set the position appriopriately
				resourceStacks[j].transform.localPosition = new Vector3(0, currentY + (resourceStacks[j].transform.localScale.y / 2f), 0);
				currentY += resourceStacks[j].transform.localScale.y; //Increae the Y appriopriately
			} else { //optherwise hide the cube
				resourceStacks[j].SetActive(false);
			}
		}
	}
}
