using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Destroys the attached gameobject after the given time. Used to remove explosion gameobjects.
public class DestroyTimer : MonoBehaviour {

	public float timeToDestory = 5f;

	// Use this for initialization
	void Start () {
		StartCoroutine(KillTimer());
	}
	
	IEnumerator KillTimer () {
		yield return new WaitForSeconds(timeToDestory);
		Destroy(gameObject);
	}
}
