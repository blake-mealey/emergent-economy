using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public float sensitivity = 20f;
	public float scrollSpeed = 1f;

    private float horzInput = 0;
    private float forwardInput = 0;

	// Update is called once per frame
	void Update() {

        //Use raw axis to allow for time manipulation
        float forward = Input.GetAxisRaw("Vertical");
        float sideways = Input.GetAxisRaw("Horizontal");


        //Performs weighted movement in the forward direction
        if (forward != 0) {
            forwardInput += forward * Time.unscaledDeltaTime * 2f;
            if (forwardInput > 1) forwardInput = 1;
            else if (forwardInput < -1) forwardInput = -1;
        } else if (forwardInput < 0){
            forwardInput += Time.unscaledDeltaTime * 2;
            forwardInput = forwardInput > 0 ? 0 : forwardInput;
        } else if (forwardInput > 0) {
            forwardInput -= Time.unscaledDeltaTime * 2;
            forwardInput = forwardInput < 0 ? 0 : forwardInput;
        }

        //Performs weighted movement in the horizontal direction. 
        if (sideways != 0) {
            horzInput += sideways * Time.unscaledDeltaTime * 2f;
            if (horzInput > 1) horzInput = 1;
            else if (horzInput < -1) horzInput = -1;
        } else if (horzInput < 0) {
            horzInput += Time.unscaledDeltaTime * 2;
            horzInput = horzInput > 0 ? 0 : horzInput;
        } else if (horzInput > 0) {
            horzInput -= Time.unscaledDeltaTime * 2;
            horzInput = horzInput < 0 ? 0 : horzInput;
        }

        //Use leftshift to control sensitivity if we want to move the camera faster
		if (Input.GetKey(KeyCode.LeftShift)) {
			sensitivity = 45f;
		} else {
			sensitivity = 20f;
		}
			

        // Move, rotate the camera only if the right mouse button is clicked
		if (Input.GetMouseButton(1)) {
            transform.Translate(Vector3.right * horzInput * sensitivity * Time.unscaledDeltaTime);
            transform.Translate(Vector3.forward * forwardInput * sensitivity * Time.unscaledDeltaTime);
			float horzAxis = Input.GetAxis("Mouse X");
			float vertAxis = Input.GetAxis("Mouse Y");

            //Camera rotation controls
			float vertRot = transform.localEulerAngles.x + -1f * vertAxis;
			if (vertRot > 180) {
				vertRot = Mathf.Max(271f, vertRot);
			} else {
				vertRot = Mathf.Min(vertRot, 89f);
			}
			transform.eulerAngles = new Vector3(vertRot, transform.localEulerAngles.y + 1f * horzAxis, 0);
		}

        //Scroll wheel controls
		if (Input.GetAxis("Mouse ScrollWheel") > 0) {
			transform.Translate(Vector3.forward * scrollSpeed);
		}
		if (Input.GetAxis("Mouse ScrollWheel") < 0) {
			transform.Translate(-Vector3.forward * scrollSpeed);
		}

        //Spacebar controls
		if (Input.GetKey(KeyCode.Space)) {
			transform.Translate(Vector3.up * sensitivity * Time.unscaledDeltaTime);
		}
	}
}
