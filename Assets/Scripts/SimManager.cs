using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimManager : MonoBehaviour {

    public static SimManager instance;

	//public values specified in the readme
    public int seed;

    public int numberOfGroups = 3;
    public int numberOfAgentsPerGroup = 10;

    public int resourceDepositsPerGroup = 3;
    public float resourcePerDeposit = 1000f;

    public Vector3 spawnAreaStart;
    public Vector3 spawnAreaEnd;

	// prefabs
    public GameObject resource;
    public GameObject agent;
	public GameObject confirmedTrade;

	//list of live agents and active resources
    private List<GameObject> agents = new List<GameObject>();
    private List<GameObject> resources = new List<GameObject>();
    
	// list of dead resources for graphing
    private List<GraphEventData> deadResources = new List<GraphEventData>();

	// The colours of each group and their current populations
    public Color[] colors;
    public int[] populations;

	//The global resource table, used for averaging
	private ResourceValueTable globalTable;
    private List<float[,]> globalTableSnapshots;

	// The number of trades that occured in the last 0.25 seconds
    private int tradeCount = 0;

    void Awake() {
        instance = this;
    }

    // Use this for initialization
    void Start () {

		// Set static variable so everything can access this
        instance = this;
        globalTableSnapshots = new List<float[,]>();
        globalTable = new ResourceValueTable();

		// Ensure at least 1 group and 1 agent are in the simulation
        if (numberOfGroups <= 0) numberOfGroups = 1;
        if (numberOfAgentsPerGroup <= 0) numberOfAgentsPerGroup = 1;

		// Generate a new seed if one is not specified
		System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        if (seed == 0) {
            seed = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
        }

		// Initialize the seed and print it to the console
		Random.InitState(seed);
		print("Seed: " + seed);

		// Setup the appriopriate array
        colors = new Color[numberOfGroups];
        populations = new int[numberOfGroups];
        for (int j = 0; j < colors.Length; j++) { // Generate colors for each group. Ensure that each colour is somewhat different. 
			bool validColor = false;
			int iter = 0;
			while (!validColor && iter < 100) {
				iter++;
				colors[j] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
				validColor = true;
				for (int k = 0; k < colors.Length; k++) {
					if (j == k || colors[k] == null) continue;
					else if (Vector3.Distance(new Vector3(colors[j].r, colors[j].g, colors[j].b), new Vector3(colors[k].r, colors[k].g, colors[k].b)) < 0.5f) { // Treat the colours as Vector3s in order to computer difference
						validColor = false;
						break;
					}
				}
			}
        }

		// Setup the rest of the simulation
        SpawnResources();
        SpawnAgents();
	}

    private void SpawnResources () {

        //Spawn the resources
		//Ensure the resources are not too close together by performing distance checks
        for (int i = 0; i < numberOfGroups; i++) {
            for (int j = 0; j < resourceDepositsPerGroup; j++) {

                int iterations = 0; // If we cannot find a valid position in 100 iterations skip placing this resource node. Rarely if ever happens, but can be caused by starting conditions.
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

				// Give resource appriopriate color and group
                GameObject resourceInstance = Instantiate(resource, randLoc, Quaternion.identity);
                resourceInstance.GetComponent<Renderer>().material.color = colors[i];
                resourceInstance.GetComponent<Resource>().initialize(i, resourcePerDeposit);
                resources.Add(resourceInstance);
            }
        }
    }

    private void SpawnAgents () {

        //Generate all of the people
		//Again cannot spawn too close to resource nodes, but can spawn inside eachother
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

	// Called whenever a trade occurs. Updates the global table and all agents within 15 distance of the trade
	public void updateTradeRatio (int rid1, int rid2, float ratio, Vector3 position) {
        tradeCount++;

        globalTable.UpdateValue(rid1, 1f, rid2, ratio);

		Instantiate(confirmedTrade, position, Quaternion.identity);

		for (int i = 0; i < agents.Count; i++) { // iterate through all live agents
			if (Vector3.Distance(agents[i].transform.position, position) < 15f) {
				agents[i].GetComponent<AgentScript>().myValueTable.UpdateValue(rid1, 1f, rid2, ratio);
			}
		}
		//globalTable.print();
	}

	// Returns the number of trades since the last call of this method
    public int GetTradeFrequency() {
        int trades = tradeCount;
        tradeCount = 0;
        return trades;
    }

	// returns the total number of currently living agents
    public int GetPopulation() {
        return agents.Count;
    }

	// Adds the given agent to the agents list, and increases the corresponding population
	public void RegisterAgent (GameObject agent, int id) {
        populations[id]++;
        agents.Add(agent);
    }

	// Removes the given agent from the agents list, and decreases the corresponding population
	public void DeregisterAgent (GameObject agent, int id) {
        populations[id]--;
        agents.Remove(agent);
    }

	// Removes a resource node from the leftover resources, and adds a time stamp to the deadResources list
	public void DeregisterResource (GameObject resource) {
        deadResources.Add(new GraphEventData(resource.GetComponent<Resource>().id, Time.time));
        resources.Remove(resource);
	}

    // Gets the snapshot of the global trade ratio table at the given index
    public float[,] GetGlobalTradeRatioSnapshot(int index) {
        return globalTableSnapshots[index];
    }

    // Gets the latest snapshot of the global trade ratio table
    public float[,] GetGlobalTradeRatioSnapshot() {
        return GetGlobalTradeRatioSnapshot(globalTableSnapshots.Count - 1);
    }

    // Gets the number of snapshots of the global trade ratio table
    public int GetGlobalTradeRatioSnapshotsCount() {
        return globalTableSnapshots.Count;
    }

    // Makes a snapshot (copy) of the global trade ratio table and stores it in the list of snapshots
    public float[,] MakeGlobalTradeRatioSnapshot() {
        float[,] snapshot = globalTable.resourceValues.Clone() as float[,];
        globalTableSnapshots.Add(snapshot);
        return snapshot;
    }

    // Gets the number of dead resources
    public int GetDeadResourceCount() {
        return deadResources.Count;
    }

    // Gets a dead resource from the list of them with the given index
    public GraphEventData GetDeadResource(int index) {
        return deadResources[index];
    }

	// Returns an InfoData object which contains the resource nodes and resources that were within the radius of the specified position.
    public InfoData GetInfo (Vector3 position, float radius) {
		radius *= radius;
        List<GameObject> tempR = new List<GameObject>(); 
        for (int i = 0; i < resources.Count; i++) { //Find the resource nodes within the radius
            if (Vector3.SqrMagnitude(position - resources[i].transform.position) < radius) {
                tempR.Add(resources[i]);
            }
        }
        List<GameObject> tempA = new List<GameObject>();
        for (int i = 0; i < agents.Count; i++) { //Find the agents within the radius
            if (Vector3.SqrMagnitude(position - agents[i].transform.position) < radius) {
                tempA.Add(agents[i]);
            }
        }
		//Convert to InfoData and return
        return new InfoData(tempR.ToArray(), tempA.ToArray());
    }
}

//Simply used as a data holder for the resources and agents arrays returned by GetInfo
public class InfoData {
    public GameObject[] resources;
    public GameObject[] agents;

    public InfoData (GameObject[] r, GameObject[] a) {
        resources = r;
        agents = a;
    }
}

// An event data struct for saving and displaying events on charts
public struct GraphEventData {
    public GraphEventData(int id, float time) {
        groupId = id;
        color = SimManager.instance.colors[groupId];
        timeStamp = time;
        transform = null;
    }

    public int groupId;
    public Color color;
    public float timeStamp;
    public RectTransform transform;
}