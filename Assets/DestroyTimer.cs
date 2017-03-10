using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
