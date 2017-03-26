using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimManager : MonoBehaviour {

    public static SimManager instance;

    public int numberOfGroups = 3;
    public int numberOfAgentsPerGroup = 10;

    public int resourceDepositsPerGroup = 3;
    public float resourcePerDeposit = 1000f;
    public int resourceMiningLimit = 10;

    public Vector3 spawnAreaStart;
    public Vector3 spawnAreaEnd;

    public GameObject resource;
    public GameObject agent;

    private List<GameObject> agents = new List<GameObject>();
    private List<GameObject> resources = new List<GameObject>();
    public Color[] colors;

	private ResourceValueTable globalTable;

	// Use this for initialization
	void Start () {

        instance = this;
		globalTable = new ResourceValueTable();

        if (numberOfGroups <= 0) numberOfGroups = 1;
        if (numberOfAgentsPerGroup <= 0) numberOfAgentsPerGroup = 1;

        Random.InitState(System.DateTime.Now.Millisecond);

        colors = new Color[numberOfGroups];
        for (int j = 0; j < colors.Length; j++) {
            colors[j] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        SpawnResources();
        SpawnAgents();
	}

    private void SpawnResources () {

        //Spawn the resources
        for (int i = 0; i < numberOfGroups; i++) {
            for (int j = 0; j < resourceDepositsPerGroup; j++) {

                int iterations = 0;
                bool valid = false;
                Vector3 randLoc = new Vector3();

                while (valid == false && iterations < 100) {
                    randLoc = new Vector3(Random.Range(spawnAreaStart.x, spawnAreaEnd.x), 2f, Random.Range(spawnAreaStart.z, spawnAreaEnd.z));
                    valid = true;

                    for (int l = 0; l < resources.Count; l++) {
                        if (Vector3.Distance(randLoc, resources[l].transform.position) < 5f) {
                            valid = false;
                            break;
                        }
                    }
                    iterations++;
                }

                GameObject resourceInstance = (GameObject)Instantiate(resource, randLoc, Quaternion.identity);
                resourceInstance.GetComponent<Renderer>().material.color = colors[i];
                resourceInstance.GetComponent<Resource>().initialize(i, resourcePerDeposit, resourceMiningLimit);
                resources.Add(resourceInstance);
            }
        }
    }

    private void SpawnAgents () {

        //Generate all of the people
        for (int j = 0; j < numberOfGroups; j++) {
            for (int k = 0; k < numberOfAgentsPerGroup; k++) {

                int iterations = 0;
                bool valid = false;
                Vector3 randLoc = new Vector3();

                while (valid == false && iterations < 100) {
                    randLoc = new Vector3(Random.Range(spawnAreaStart.x, spawnAreaEnd.x), 0, Random.Range(spawnAreaStart.z, spawnAreaEnd.z));
                    valid = true;

                    for (int l = 0; l < resources.Count; l++) {
                        if (Vector3.Distance(randLoc, resources[l].transform.position) < 5f) {
                            valid = false;
                            break;
                        }
                    }
                    iterations++;
                }

                //Spawn agents and set their ID
                GameObject agentInstance = Instantiate(agent, randLoc, Quaternion.identity);
                agentInstance.GetComponent<AgentScript>().SetID(j, colors[j]);
            }
        }
    }

	public void updateTradeRatio (int rid1, int rid2, float ratio, Vector3 position) {
		globalTable.UpdateValue(rid1, 1f, rid2, ratio);
		for (int i = 0; i < agents.Count; i++) {
			if (Vector3.Distance(agents[i].transform.position, position) < 15f) {
				agents[i].GetComponent<AgentScript>().myValueTable.UpdateValue(rid1, 1f, rid2, ratio);
			}
		}
		globalTable.print();
		print(agents.Count);
	}

	public void RegisterAgent (GameObject agent) {
        agents.Add(agent);
    }

    public void DeregisterAgent (GameObject agent) {
        agents.Remove(agent);
    }

	public void DeregisterResource (GameObject resource) {
		resources.Remove(resource);
	}

    public InfoData GetInfo (Vector3 position, float radius) {
		radius *= radius;
        List<GameObject> tempR = new List<GameObject>();
        for (int i = 0; i < resources.Count; i++) {
            if (Vector3.SqrMagnitude(position - resources[i].transform.position) < radius) {
                tempR.Add(resources[i]);
            }
        }
        List<GameObject> tempA = new List<GameObject>();
        for (int i = 0; i < agents.Count; i++) {
            if (Vector3.SqrMagnitude(position - agents[i].transform.position) < radius) {
                tempA.Add(agents[i]);
            }
        }

        return new InfoData(tempR.ToArray(), tempA.ToArray());
    }
}

public class InfoData {
    public GameObject[] resources;
    public GameObject[] agents;

    public InfoData (GameObject[] r, GameObject[] a) {
        resources = r;
        agents = a;
    }
}
