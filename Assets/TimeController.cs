using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TimeController : MonoBehaviour {

	private float fixedUpdateTime = 0.02f;
	private float maxDeltaTime = 0.5f;
	public Text timeText;

	void Start() {
		fixedUpdateTime = Time.fixedDeltaTime;
		maxDeltaTime = Time.maximumDeltaTime;
		setTimeText(1.0f);
	}

	public void setTime(float newTime) {
		newTime = newTime / 10f;
		Time.timeScale = newTime;
		Time.fixedDeltaTime = fixedUpdateTime * newTime;
		Time.maximumDeltaTime = maxDeltaTime * newTime;
		setTimeText(newTime);
	}

	private void setTimeText(float time) {
		timeText.text = Time.timeScale + "x";
	}
}
