using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TradeVisualController : MonoBehaviour {

	private float ctime = 0;
	private float deathTime = 1.5f;
	private float rotationSpeed = 180f;

	private Vector3 force = new Vector3(0,20f,0);

	void Start() {
		GetComponent<Rigidbody>().AddForce(force,ForceMode.Impulse);
	}

	// Update is called once per frame
	void Update () {

		if (ctime > deathTime) Destroy(gameObject);
		else {
			ctime += Time.deltaTime;
		}

		transform.Rotate(new Vector3(0, rotationSpeed * Time.deltaTime, 0));
	}
}
