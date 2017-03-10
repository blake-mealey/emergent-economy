using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(ResourceValueTable))]
public class AgentScript : MonoBehaviour {

    public int id { get; private set; }
    public float timePerSearchDir = 5f;
    public float speedMod = 1f;
    public float infoRadius = 10f;
    public float tradeRadius = 5f;

    private bool randomMovement = true;
    private float currentTime = 0f;
    private Vector3 movementDir = new Vector3();

    public float health = 50f;

    public float maxResourceCount = 100f;
    //This is the resource array of the resources that are currently being carried by this user
    public float[] resources;
	public float totalResources = 0;
    ResourceValueTable myValueTable;

    private GameObject[] closeAgents;
    private GameObject[] closeResources;

	public GameObject explosionEffect;

	// Use this for initialization
	void Start () {
        resources = new float[SimManager.instance.numberOfGroups];
        myValueTable = GetComponent<ResourceValueTable>();

		currentTime = timePerSearchDir;
        SimManager.instance.RegisterAgent(this.gameObject);
        StartCoroutine("HealthDec");
        StartCoroutine("LookAround");
	}
	
    IEnumerator HealthDec () {
        while (health > 0) {
            health -= UnityEngine.Random.Range(0f, 5f);
            yield return new WaitForSeconds(1f);
        }
    }

    //Look at the surrounding information every X amount of time
    IEnumerator LookAround () {
        while (true) {
            InfoData data = SimManager.instance.GetInfo(transform.position, infoRadius);
			closeAgents = data.agents;
			closeResources = data.resources;
            yield return new WaitForSeconds(0.5f);
        }
    }

	// Update is called once per frame
	void Update () {

        if (health <= 0) Die();
        if (health >= 100) Reproduce();

		//Need to ask a few things in order:
		// 1. Do I want to gather or trade?
		//    a. If gather, do I know where a resource I can gather from is?
		//       -If so, move to that resource. If not, wander.
		//    b. If trade, do I see an agent I want to trade with?
		//       -If so, move to that agent. If not, wander.


		//Determine if I want to trade by the total number of resources I am currently carrying.
		if (totalResources < maxResourceCount / 10f) {

			//Move to a resource
			if (closeResources.Length > 0) {
				randomMovement = false;
				GameObject myTargetResource = null;
				float dist = float.MaxValue;

				//Go to closest resource that is not my resource
				for (int j = 0; j < closeResources.Length; j++) {
					if (closeResources[j].GetComponent<Resource>().id != id && Vector3.SqrMagnitude(transform.position - closeResources[j].transform.position) < dist) {
						dist = Vector3.SqrMagnitude(transform.position - closeResources[j].transform.position);
						myTargetResource = closeResources[j];
					}
				}

				if (myTargetResource == null) randomMovement = true;
				else movementDir = (myTargetResource.transform.position - transform.position).normalized;//Move to the resource
			}

			//Wander
			if (randomMovement) {
				currentTime += Time.deltaTime;
				if (currentTime > timePerSearchDir) {
					currentTime = 0;
					movementDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
				}
			}


		} else {



		}

        transform.localScale = new Vector3(health/50f+0.25f,1,health/50f+0.25f);
	}

    // FixedUpdate is called every physics update
    void FixedUpdate() {

        //Move based on the movement vector
        GetComponent<Rigidbody>().AddForce(movementDir * speedMod);

    }

    //Called to destory the agent
    private void Die () {
        SimManager.instance.DeregisterAgent(this.gameObject);
		GameObject explosion = (GameObject)Instantiate(explosionEffect, transform.position, Quaternion.identity);
		explosion.GetComponent<ParticleSystem>().startColor = GetComponent<Renderer>().material.color;
        Destroy(gameObject);
    }

    //Called to duplicate the agent
    private void Reproduce () {
        health -= 50;
        GameObject dup = (GameObject) Instantiate(gameObject, transform.position + new Vector3(1f,0,0), Quaternion.identity);
        dup.GetComponent<AgentScript>().SetID(id, GetComponent<Renderer>().material.color);
    }

    //Sets the id of this agent. Called on instantiation.
    public void SetID (int newID, Color myColor) {
        id = newID;
        GetComponent<Renderer>().material.color = myColor;
    }
}
