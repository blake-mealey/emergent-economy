using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AgentScript : MonoBehaviour {

    public int id { get; private set; }
    public float timePerSearchDir = 5f;
    public float speedMod = 1f;
    public float infoRadius = 10f;
    public float tradeRadius = 5f;
	public float mineRate = 5f;

    private float currentTime = 0f;
    private Vector3 movementDir = new Vector3();

    public float health = 50f;

    public float maxResourceCount = 100f;
    //This is the resource array of the resources that are currently being carried by this user
    public float[] resources;
	public float totalResources = 0;
    public ResourceValueTable myValueTable;
	public GameObject resourceMonitor;

    private GameObject[] closeAgents;
    private GameObject[] closeResources;

	private GameObject targetResource;
	public GameObject explosionEffect;

	private GameObject targetTradePartner;
	public bool inTrade = false; //Used by other agents to determine if I am already trading
	public bool initiator = false;
	private float tradeCooldown = 0f;

	enum Modes { Gather, Trade};
	private Modes mode = Modes.Gather;

	public bool dead = false;

	private int resourceToTrade = -1;
	private int resourceToReceive = -1;

	//used to determine if the agent should trade for a resource of another type
	private float tradeSearchTime = 20f;
	private float currentTradeSearchTime = 0f;

	private WaitForSeconds lookws;
	private WaitForSeconds healthws;

	// Use this for initialization
	void Start () {

		resources = new float[SimManager.instance.numberOfGroups];
		totalResources = 0;
		inTrade = false;

		myValueTable = new ResourceValueTable();

		currentTime = timePerSearchDir;
        SimManager.instance.RegisterAgent(gameObject, id);

		lookws = new WaitForSeconds(Random.Range(0.4f, 0.6f));
		healthws = new WaitForSeconds(Random.Range(0.9f, 1.1f));

		StartCoroutine(HealthDec());
        StartCoroutine(LookAround());

		GameObject rm = Instantiate(resourceMonitor);
		rm.GetComponent<ResourceMonitor>().myAgent = this;
	}
	
    IEnumerator HealthDec () {
        while (health > 0) {
			health -= 1f; // UnityEngine.Random.Range(0f, 1f);
            yield return healthws;
        }
    }

    //Look at the surrounding information every X amount of time
    IEnumerator LookAround () {
        while (true) {
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

        HeatMap.instance.AddHeat(transform.position, SimManager.instance.colors[id]);


        if (health <= 0) Die();
        if (health >= 90) Reproduce();

		//Need to ask a few things in order:
		// 1. Do I want to gather or trade?
		//    a. If gather, do I know where a resource I can gather from is?
		//       -If so, move to that resource. If not, wander.
		//    b. If trade, do I see an agent I want to trade with?
		//       -If so, move to that agent. If not, wander.

		//This is the state machine code. It determines what this agent would like to do.
		float desiredResourceLevel = maxResourceCount / (50f / health);
		desiredResourceLevel = desiredResourceLevel <= maxResourceCount ? desiredResourceLevel : maxResourceCount;

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
			} else if (targetResource != null && targetResource.GetComponent<Resource>().isEmpty()) {
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

			GameObject firstTarget = null;
			GameObject secondTarget = null;

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
					//For now, just look for other agents with the resource I wish to collect
					//The agent I wish to trade with may die while I am looking at them, this will handle that case.
					if (closeAgents[k] == gameObject) continue;

					AgentScript tempAgent = closeAgents[k].GetComponent<AgentScript>();
					if (!tempAgent.inTrade && !tempAgent.dead) {

						float min = Mathf.Min(tempAgent.resources[id], mostNotMineAmount);
						//Is there someone trading for my resource (most prefered)
						if (min > 5f && min > highestTradeQuantity) {
							firstTarget = closeAgents[k];
							highestTradeQuantity = min;
							foundPriorityDeal = true;
						}

						//No point checking for the best deal if a priority deal was found.
						if (foundPriorityDeal) continue;

						//Otherwise find the best deal
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

				if (firstTarget != null) {
					targetTradePartner = firstTarget;
					AgentScript tempAgent = firstTarget.GetComponent<AgentScript>();
					resourceToTrade = mostNotMineId;
					resourceToReceive = id;
				} else if (secondTarget != null) {
					targetTradePartner = secondTarget;
					AgentScript tempAgent = secondTarget.GetComponent<AgentScript>();
					resourceToTrade = mostNotMineId;
				} 
			}

			//Move to that person
			if (targetTradePartner != null) {
				//If our current target gets too far away or gets into a trade before we get there, move to a new target.
				AgentScript temp = targetTradePartner.GetComponent<AgentScript>();
				if (Vector3.Distance(transform.position, targetTradePartner.transform.position) > 20f || temp.inTrade || temp.dead) targetTradePartner = null;
				else movementDir = (targetTradePartner.transform.position - transform.position).normalized;
			} else {
				Wander();
			}
		}

		if (tradeCooldown > 0) {
			if (mode == Modes.Trade) Wander();
			tradeCooldown -= Time.deltaTime;
		}

		//This happens regardless of anything else
        transform.localScale = new Vector3(health/50f+0.25f,1,health/50f+0.25f);
	}

	private void Wander() {
		currentTime += Time.deltaTime;
		if (currentTime > timePerSearchDir) {
			currentTime = 0;
            movementDir = (movementDir + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f))).normalized;
			//movementDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
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

		if (inTrade && initiator) targetTradePartner.GetComponent<AgentScript>().endTrade();

        SimManager.instance.DeregisterAgent(gameObject, id);
		GameObject explosion = (GameObject)Instantiate(explosionEffect, transform.position, Quaternion.identity);
		explosion.GetComponent<ParticleSystem>().startColor = GetComponent<Renderer>().material.color;
		gameObject.SetActive(false);
    }

    //Called to duplicate the agent
    private void Reproduce () {
        health -= 50;
        GameObject dup = (GameObject) Instantiate(gameObject, transform.position + new Vector3(1f,0,0), Quaternion.identity);
        dup.GetComponent<AgentScript>().SetID(id, GetComponent<Renderer>().material.color);
		dup.GetComponent<AgentScript>().endTrade();
    }

    //Sets the id of this agent. Called on instantiation.
    public void SetID (int newID, Color myColor) {
        id = newID;
        GetComponent<Renderer>().material.color = myColor;
    }

	void OnCollisionStay(Collision collision) {
		if (collision.gameObject.CompareTag("resource") && totalResources < maxResourceCount && collision.gameObject.GetComponent<Resource>().id != id) {
			//Do I want to collect from the resource?
			Resource resource = collision.gameObject.GetComponent<Resource>();
			float toMine = mineRate * Time.deltaTime;
			float amountMined = resource.mineResource(toMine + totalResources > maxResourceCount ? maxResourceCount - totalResources : toMine);
			totalResources += amountMined;
			resources[resource.id] += amountMined;
		} else if (!inTrade && collision.gameObject == targetTradePartner && !targetTradePartner.GetComponent<AgentScript>().inTrade) {
			initiateTrade(collision.gameObject);
		}
	}

	void OnCollisionEnter(Collision collision) {
		//Did I collide with the person I want trade with?
		if (!inTrade && collision.gameObject == targetTradePartner && !targetTradePartner.GetComponent<AgentScript>().inTrade) {
			initiateTrade(collision.gameObject);
		} else if(collision.gameObject.CompareTag("wall") && targetResource == null && targetTradePartner == null) {
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
		inTrade = otherAgent.requestTrade();

		if (inTrade) {
			freeze();
			initiator = true;
			//id and otherAgent.id need to be changed to the actual id of the resources being traded
			float ratio = myValueTable.getRatio(resourceToReceive, resourceToTrade);
			ratio += ratio * (health - 40f) / 100f;
			//print("Proposing ratio: " + ratio);
			if (otherAgent.recieveTradeOffer(resourceToReceive, resourceToTrade, ratio)) {
				//print("deal made: " + resourceToReceive + "->" + resourceToTrade + ": " + ratio);
				StartCoroutine(processTrade(resourceToReceive, resourceToTrade, ratio, otherAgent));
				SimManager.instance.updateTradeRatio(resourceToReceive, resourceToTrade, ratio, transform.position);
			} else {
				//print("no deal made: " + ratio);
				otherAgent.endTrade();
				endTrade();
			}
			//We need to generate an initial offer
		} else {
			targetTradePartner = null;
		}
	}

	//Called when two agents collide on one of the agents.
	public bool requestTrade () {
		if (inTrade == false) {
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

		float acceptableRatio = myValueTable.getRatio(rid1, rid2);
		acceptableRatio += acceptableRatio * (60 - health) / 100f;

		//print("Acceptable ratio: " + acceptableRatio);
		//If they offer a better ratio than what I want, accept.
		if (ratio < acceptableRatio) return true;

		return false;
	}

	IEnumerator processTrade (int rid1, int rid2, float ratio, AgentScript otherAgent) {
		//First we need to calculate how much of each resource can be traded. 
		while (resources[rid2] >= 1f && otherAgent.resources[rid1] >= ratio) {

			if (otherAgent.dead) break;

			resources[rid1] += ratio;
			resources[rid2] -= 1f;
			otherAgent.resources[rid1] -= ratio;
			otherAgent.resources[rid2] += 1f;

			processResourceChanges();
			otherAgent.processResourceChanges();

			yield return new WaitForSeconds(0.1f);
		}

		otherAgent.endTrade();
		endTrade();
	}

	//Checks the resources arrayt to get a new total, and consumes all resources of my type
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
		tradeCooldown = 4f;
		thaw();
	}
}
