using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimManager : MonoBehaviour {

    public static SimManager instance;

    public int numberOfGroups = 3;
    public int numberOfAgentsPerGroup = 10;

    public int resourceDepositsPerGroup = 3;
    public float resourcePerDeposit = 1000f;

    public Vector3 spawnAreaStart;
    public Vector3 spawnAreaEnd;

    public GameObject resource;
    public GameObject agent;
	public GameObject confirmedTrade;

    private List<GameObject> agents = new List<GameObject>();
    private List<GameObject> resources = new List<GameObject>();
    public Color[] colors;
    public int[] populations;

	private ResourceValueTable globalTable;

    private int tradeCount = 0;

    void Awake() {
        instance = this;
    }

    // Use this for initialization
    void Start () {

        instance = this;
		globalTable = new ResourceValueTable();

        if (numberOfGroups <= 0) numberOfGroups = 1;
        if (numberOfAgentsPerGroup <= 0) numberOfAgentsPerGroup = 1;


		System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
		int seed = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

		// Here is a good seed: 1491256218 Run with 3 groups, 200 agents each, 3 deposits per group spawn range 10-70
		// Random.InitState(1491256218);


		Random.InitState(seed);
		print("Seed: " + seed);


        colors = new Color[numberOfGroups];
        populations = new int[numberOfGroups];
        for (int j = 0; j < colors.Length; j++) {
			bool validColor = false;
			int iter = 0;
			while (!validColor && iter < 100) {
				iter++;
				colors[j] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
				validColor = true;
				for (int k = 0; k < colors.Length; k++) {
					if (j == k || colors[k] == null) continue;
					else if (Vector3.Distance(new Vector3(colors[j].r, colors[j].g, colors[j].b), new Vector3(colors[k].r, colors[k].g, colors[k].b)) < 0.5f) {
						validColor = false;
						break;
					}
				}
			}
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

                GameObject resourceInstance = Instantiate(resource, randLoc, Quaternion.identity);
                resourceInstance.GetComponent<Renderer>().material.color = colors[i];
                resourceInstance.GetComponent<Resource>().initialize(i, resourcePerDeposit);
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
        tradeCount++;

        globalTable.UpdateValue(rid1, 1f, rid2, ratio);

		Instantiate(confirmedTrade, position, Quaternion.identity);

		for (int i = 0; i < agents.Count; i++) {
			if (Vector3.Distance(agents[i].transform.position, position) < 15f) {
				agents[i].GetComponent<AgentScript>().myValueTable.UpdateValue(rid1, 1f, rid2, ratio);
			}
		}
		//globalTable.print();
	}

    public int GetTradeFrequency() {
        int trades = tradeCount;
        tradeCount = 0;
        return trades;
    }

    public int GetPopulation() {
        return agents.Count;
    }

	public void RegisterAgent (GameObject agent, int id) {
        populations[id]++;
        agents.Add(agent);
		//print(agents.Count);
    }

    public void DeregisterAgent (GameObject agent, int id) {
        populations[id]--;
        agents.Remove(agent);
		//print(agents.Count);
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
