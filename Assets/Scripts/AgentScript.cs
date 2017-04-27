using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AgentScript : MonoBehaviour {

	// Group this agent belongs too
    public int id { get; private set; }

	// All changeable in the ispector.
    public float timePerSearchDir = 5f; // How long does it wander in 1 direction
    public float speedMod = 1f; // How fast does it move
    public float infoRadius = 10f; // How far can this agent see
	public float mineRate = 5f; // How dast can it extract resources

    private float currentTime = 0f; //Used to determine how long an agent has been wandering for
    private Vector3 movementDir = new Vector3(); //Current direction to move

    public float health = 50f;

	// Used as a threshold for deciding if an agent should trade or mine. (Max that an agent can mine)
    public float maxResourceCount = 100f;

    //This is the resource array of the resources that are currently being carried by this agent
    public float[] resources;
	public float totalResources = 0; //Sum of the above array
    public ResourceValueTable myValueTable;
	public GameObject resourceMonitor;

    private GameObject[] closeAgents;
    private GameObject[] closeResources;

	private GameObject targetResource; // Resource to mine from (and move towards)
	public GameObject explosionEffect; // Kaboom

	private GameObject targetTradePartner; // Agent to move towards and attemp to trade with
	public bool inTrade = false; //Used by other agents to determine if I am already trading
	public bool initiator = false; //Am I the initiator of this trade?

	private float tradeCooldown = 0f; //Current wander time after failed trade
	private float cooldown = 2.5f; // How long to wander after failed trade

	enum Modes { Gather, Trade}; // States of agent
	private Modes mode = Modes.Gather; //Start in gather mode

	public bool dead = false;

	// Used to determine what type of deal I am looking for
	private int resourceToTrade = -1;
	private int resourceToReceive = -1;

	private WaitForSeconds lookws; //Used in enumeration for looking around
	private float healthLoss; //amount of health to lose per second

	// Use this for initialization
	void Start () {

		// initialize resource array to correct length
		resources = new float[SimManager.instance.numberOfGroups];
		totalResources = 0;
		inTrade = false;

		// create new resource table
		myValueTable = new ResourceValueTable();

		// Reset wander time
		currentTime = timePerSearchDir;

		//Register this agent with the SimManager
        SimManager.instance.RegisterAgent(gameObject, id);

		// Setup the enumeration timer and health
		lookws = new WaitForSeconds(Random.Range(0.4f, 0.6f));
		healthLoss = Random.Range(0.9f, 1.1f);

		// Start look around
        StartCoroutine(LookAround());

		// Create a personal resource monitor
		GameObject rm = Instantiate(resourceMonitor);
		rm.GetComponent<ResourceMonitor>().myAgent = this;
	}

    //Look at the surrounding information every X amount of time
    IEnumerator LookAround () {
        while (true) { //Get the information about my surroundings from the SimManager
            InfoData data = SimManager.instance.GetInfo(transform.position, infoRadius);
			closeAgents = data.agents;
			closeResources = data.resources;
            yield return lookws;

			if (dead) break;
        }
    }

	// Update is called once per frame
	void Update () {
		if (dead) return;

		//Update the heatmap with my current position
        HeatMap.instance.AddHeat(transform.position, SimManager.instance.colors[id] * Time.deltaTime);

		//Update my health and check if I should perform some other action
		health -= healthLoss * Time.deltaTime;
		if (health <= 0) Die();
        if (health >= 90) Reproduce();

		//Agent asks these questions to determine what to do
		// 1. Do I want to gather or trade?
		//    a. If gather, do I know where a resource I can gather from is?
		//       -If so, move to that resource. If not, wander.
		//    b. If trade, do I see an agent I want to trade with?
		//       -If so, move to that agent. If not, wander.

		//This is the state code. It determines what this agent would like to do.
		float desiredResourceLevel = maxResourceCount / (50f / health);
		desiredResourceLevel = desiredResourceLevel <= maxResourceCount ? desiredResourceLevel : maxResourceCount;

		//desired is how many resources this agent would like to be carrying before it attmepts to trade
		if (totalResources < desiredResourceLevel) {
			mode = Modes.Gather;
		} else {
			targetResource = null;
			mode = Modes.Trade;
		}


		//Determine if I want to trade by the total number of resources I am currently carrying.
		if (mode == Modes.Gather) {

			//Select resource
			if (targetResource == null) {
				float dist = float.MaxValue;

				//Go to closest resource that is not my resource
				for (int j = 0; j < closeResources.Length; j++) {
					if (closeResources[j].GetComponent<Resource>().id != id && Vector3.SqrMagnitude(transform.position - closeResources[j].transform.position) < dist) {
						dist = Vector3.SqrMagnitude(transform.position - closeResources[j].transform.position);
						targetResource = closeResources[j];
					}
				}
			} else if (targetResource != null && targetResource.GetComponent<Resource>().isEmpty()) { // If my current target resource is empty, move on
				targetResource = null;
			}

			//Acutally mine the resource
			if (targetResource != null) {
				movementDir = (targetResource.transform.position - transform.position).normalized;
			} else { //Or wander randomely
				Wander();
			}
		}

		//Actively look for a trade
		if (mode == Modes.Trade && !inTrade && tradeCooldown <= 0f) {

			// Targets with different priorities
			GameObject firstTarget = null;
			GameObject secondTarget = null;

			// Find the resource in my inventory that I have the most of (Will determine who to trade with with this resource)
			int mostNotMineId = 0;
			float mostNotMineAmount = 0;
			for (int i = 0; i < resources.Length; i++) {
				if (i != id && resources[i] > mostNotMineAmount) {
					mostNotMineId = i;
					mostNotMineAmount = resources[i];
				}
			}

			// first see who is the best person to trade with in my radius. 
			if (targetTradePartner == null) {
				float highestTradeQuantity = 0;
				float highestTradeQuantity2 = 0;
				bool foundPriorityDeal = false;

				for (int k = 0; k < closeAgents.Length; k++) {
					
					if (closeAgents[k] == gameObject) continue; //Skip myself

					AgentScript tempAgent = closeAgents[k].GetComponent<AgentScript>();
					if (!tempAgent.inTrade && !tempAgent.dead) { //The agent I wish to trade with may die while I am looking at them, this will handle that case.

						float min = Mathf.Min(tempAgent.resources[id], mostNotMineAmount);
						//Is there someone trading for my resource (most prefered)
						if (min > 5f && min > highestTradeQuantity) {
							firstTarget = closeAgents[k];
							highestTradeQuantity = min;
							foundPriorityDeal = true; 
						}

						//No point checking for the best deal if a priority deal was found.
						if (foundPriorityDeal) continue;

						//Otherwise find the best deal by resource value
						for (int i = 0; i < tempAgent.resources.Length; i++) { 
							float min2 = Mathf.Min(tempAgent.resources[i], mostNotMineAmount);
							float deal = min2 * myValueTable.getRatio(i, mostNotMineId);
							if (min2 > 5f && deal > highestTradeQuantity2) {
								secondTarget = closeAgents[k];
								highestTradeQuantity2 = deal;
								resourceToReceive = i;
							}
						}
					}
				}

				//Did I find a priority value?
				if (firstTarget != null) {
					targetTradePartner = firstTarget;
					resourceToTrade = mostNotMineId;
					resourceToReceive = id;
				} else if (secondTarget != null) { // No priority value, did I find another option?
					targetTradePartner = secondTarget;
					resourceToTrade = mostNotMineId;
				} 
			}

			//Move to that person
			if (targetTradePartner != null) {
				//If our current target gets too far away or gets into a trade before we get there, move to a new target.
				AgentScript temp = targetTradePartner.GetComponent<AgentScript>();
				if (Vector3.Distance(transform.position, targetTradePartner.transform.position) > 20f || temp.inTrade || temp.dead) targetTradePartner = null;
				else movementDir = (targetTradePartner.transform.position - transform.position).normalized;
			} else { // If no person to trade with found
				Wander();
			}
		}

		// If we failed a trade, then we will wander for the designated time
		if (tradeCooldown > 0) {
			if (mode == Modes.Trade) Wander();
			tradeCooldown -= Time.deltaTime;
		}

		//Change size based on health
        transform.localScale = new Vector3(health/50f+0.25f,1,health/50f+0.25f);
	}

	// Simply changes the direction I wander every X time units. Actual movement is done in FixedUpdate
	private void Wander() {
		currentTime += Time.deltaTime;
		if (currentTime > timePerSearchDir) {
			currentTime = 0;
            movementDir = (movementDir + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f))).normalized;
		}
	}

    // FixedUpdate is called every physics update
    void FixedUpdate() {
        //Move based on the movement vector
        GetComponent<Rigidbody>().AddForce(movementDir * speedMod);
    }

    //Called to destory the agent
    private void Die () {
		dead = true;

		if (inTrade && initiator) targetTradePartner.GetComponent<AgentScript>().endTrade(); // end the trade if I was the initiator

        SimManager.instance.DeregisterAgent(gameObject, id); //Deregister so other agents will no longer see me
		GameObject explosion = (GameObject)Instantiate(explosionEffect, transform.position, Quaternion.identity); //Kaboom
		explosion.GetComponent<ParticleSystem>().startColor = GetComponent<Renderer>().material.color;
		gameObject.SetActive(false); // hide this gameobject
    }

    //Called to duplicate the agent
    private void Reproduce () {
        health -= 50;
        GameObject dup = (GameObject) Instantiate(gameObject, transform.position + new Vector3(1f,0,0), Quaternion.identity);
        dup.GetComponent<AgentScript>().SetID(id, GetComponent<Renderer>().material.color); // Set the id of the new agent to be the same as me
		dup.GetComponent<AgentScript>().endTrade(); // Ensure that the new agent is not in a trade if I am in a trade
    }

    //Sets the id of this agent. Called on instantiation.
    public void SetID (int newID, Color myColor) {
        id = newID;
        GetComponent<Renderer>().material.color = myColor;
    }

	// Called if I maintain a collision
	void OnCollisionStay(Collision collision) {
		if (collision.gameObject.CompareTag("resource") && totalResources < maxResourceCount && collision.gameObject.GetComponent<Resource>().id != id) { // Am I colliding with a resource node that I can collect from
			//Do I want to collect from the resource?
			Resource resource = collision.gameObject.GetComponent<Resource>(); //Collect from that resource
			float toMine = mineRate * Time.deltaTime;
			float amountMined = resource.mineResource(toMine + totalResources > maxResourceCount ? maxResourceCount - totalResources : toMine);
			totalResources += amountMined;
			resources[resource.id] += amountMined;
		} else if (!inTrade && collision.gameObject == targetTradePartner && !targetTradePartner.GetComponent<AgentScript>().inTrade) { // Otherwise did I collide with an agent I want to trade with
			initiateTrade(collision.gameObject);
		}
	}

	// Called when a collision forst happend
	void OnCollisionEnter(Collision collision) {
		//Did I collide with the person I want trade with?
		if (!inTrade && collision.gameObject == targetTradePartner && !targetTradePartner.GetComponent<AgentScript>().inTrade) { //Did I collide with the person I want to trade with
			initiateTrade(collision.gameObject);
		} else if(collision.gameObject.CompareTag("wall") && targetResource == null && targetTradePartner == null) { //Did I collide with wall. If so, turn around.
            movementDir *= -1;
        }
	}

	//Freeze the object inplace.
	private void freeze () {
		GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
	}

	//Unfreeze the position of the object
	private void thaw () {
		GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
	}

	// ************************************************* Here is all of the code pertaining to the trading system *****************************************************
	//Called when a trade is first initiated
	private void initiateTrade (GameObject obj) {
		AgentScript otherAgent = obj.GetComponent<AgentScript>();
		inTrade = otherAgent.requestTrade(); // Request a trade with the other agent

		if (inTrade) { // If inTrade, then we can make an offer to the other agent
			freeze();
			initiator = true; // We initated the trade
			// Generate an offer
			float ratio = myValueTable.getRatio(resourceToReceive, resourceToTrade);
			ratio += ratio * (health - 40f) / 100f;
			if (otherAgent.recieveTradeOffer(resourceToReceive, resourceToTrade, ratio)) { //Propose the offer
				StartCoroutine(processTrade(resourceToReceive, resourceToTrade, ratio, otherAgent)); //If accepted, start the exchange with the other agent
				SimManager.instance.updateTradeRatio(resourceToReceive, resourceToTrade, ratio, transform.position);
			} else { //Otherwise we were refused, end the trade
				otherAgent.endTrade();
				endTrade();
			}
		} else {
			targetTradePartner = null; //We were rejected
		}
	}

	//Called when two agents collide on one of the agents.
	public bool requestTrade () {
		if (inTrade == false) { // If I am not already in a trade, then return true. Else return false
			freeze();
			//print("Agreeing to trade request!");
			inTrade = true;
			return true;
		}
		return false;
	}

	//Used to determine trades.
	//rid1 is what the other agent wants, rid2 is what they offer, ratio is the price.
	public bool recieveTradeOffer (int rid1, int rid2, float ratio) {

		//Do I want rid2 in the first place? If my health is low, dont trade for something that is not my resource
		if (health < 20f && rid2 != id) return false; 

		// Generate acceptable ratio
		float acceptableRatio = myValueTable.getRatio(rid1, rid2);
		acceptableRatio += acceptableRatio * (60 - health) / 100f;

		//If they offer a better ratio than what I want, accept.
		if (ratio < acceptableRatio) return true;

		return false;
	}

	IEnumerator processTrade (int rid1, int rid2, float ratio, AgentScript otherAgent) {
		//First we need to calculate how much of each resource can be traded. 
		while (resources[rid2] >= 1f && otherAgent.resources[rid1] >= ratio) {

			if (otherAgent.dead) break; // If they die during the exchange, continue on like nothing happened

			resources[rid1] += ratio; // Gain ratio resources
			resources[rid2] -= 1f; // Lose 1 resource
			otherAgent.resources[rid1] -= ratio; //Other agent loses ratio
			otherAgent.resources[rid2] += 1f; //Other agent gains 1

			processResourceChanges(); //Updates health for both agents
			otherAgent.processResourceChanges();

			yield return new WaitForSeconds(0.1f);
		}

		otherAgent.endTrade(); //End the trade
		endTrade();
	}

	//Checks the resources array to get a new total, and consumes all resources of my type
	public void processResourceChanges () {
		health += resources[id];
		resources[id] = 0;
		totalResources = 0;
		for (int j = 0; j < resources.Length; j++) {
			totalResources += resources[j];
		}
	}

	//Called to end a trade, successful or not.
	public void endTrade () {
		initiator = false;
		inTrade = false;
		targetTradePartner = null;
		tradeCooldown = cooldown; // Cannot trade for 2.5 seconds
		thaw();
	}
}
