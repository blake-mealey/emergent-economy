using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// Controls the time at which the simulation runs at
public class TimeController : MonoBehaviour {

	// The default fixedUpdate and maxDelta time values
	private float fixedUpdateTime = 0.02f; 
	private float maxDeltaTime = 0.5f;
	public Text timeText; //The text to update

	void Start() {
		//update the default values
		fixedUpdateTime = Time.fixedDeltaTime;
		maxDeltaTime = Time.maximumDeltaTime;
		setTimeText(1.0f);
	}

	public void setTime(float newTime) {
		newTime = newTime / 10f; // Use the new time to set the speed at which the simulation runs
		Time.timeScale = newTime;
		Time.fixedDeltaTime = fixedUpdateTime * newTime;
		Time.maximumDeltaTime = maxDeltaTime * newTime;
		setTimeText(newTime);
	}

	private void setTimeText(float time) { //Set the text accordingly
		timeText.text = Time.timeScale + "x";
	}
}
