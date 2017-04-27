using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls the coin which pops up on a successful trade
public class TradeVisualController : MonoBehaviour {

	private float ctime = 0;
	private float deathTime = 1.5f;
	private float rotationSpeed = 180f;

	private Vector3 force = new Vector3(0,20f,0);

	void Start() {
		GetComponent<Rigidbody>().AddForce(force,ForceMode.Impulse); // Add force once at the start of the lifetime of this gameobject
	}

	// Update is called once per frame
	void Update () {

		if (ctime > deathTime) Destroy(gameObject); // Destroy the gameobject if the given time has elapsed
		else {
			ctime += Time.deltaTime;
		}

		transform.Rotate(new Vector3(0, rotationSpeed * Time.deltaTime, 0)); //Rotate for visual effect
	}
}
