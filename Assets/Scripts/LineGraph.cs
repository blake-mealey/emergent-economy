using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

public class LineGraph : MonoBehaviour {

    UILineRenderer lineRenderer;
    int index;
    float highestValue = 1;
    float prevHighestValue;

    public bool fixedWidth;
    public int windowSize;
    public int maxWindowSize;

    public delegate int MyDelegate();
    public MyDelegate myDelegate;

    // Use this for initialization
    void Start () {
        lineRenderer = GetComponent<UILineRenderer>();
        lineRenderer.Points = new Vector2[0];
        index = 0;
	}

    IEnumerator UpdateGraph () {
        // wait for SimManager to set things up
        yield return new WaitForSeconds(1f);
        while (true) {
            index++;
            if (fixedWidth) {// if fixed width and we have filled window, shift points left
                if (index >= windowSize) {
                    index--;
                    ShiftPointsLeft();
                }
            } else {// else squish points to fill window
                fixedWidth = index == maxWindowSize;
                SquishPointsHorizontally();
            }

            // get the next data value
            float value = (float)myDelegate();
            if(value > highestValue) {
                // if we have a new highest value then re-fit the points vertically
                prevHighestValue = highestValue;
                highestValue = value;
                SquishPointsVertically();
            }
            // add the new point to the data-set
            AddPoint(value / highestValue);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // Fit points vertically (highest value at 1)
    public void SquishPointsVertically() {
        var pointList = new List<Vector2>(lineRenderer.Points);
        for (int i = 0; i < pointList.Count; i++) {
            Vector2 v = pointList[i];
            v.y = v.y * (prevHighestValue / highestValue);
            pointList[i] = v;
        }
        lineRenderer.Points = pointList.ToArray();
    }

    // Fit all points in the window
    public void SquishPointsHorizontally() {
        var pointList = new List<Vector2>(lineRenderer.Points);
        for (int i = 0; i < pointList.Count; i++) {
            Vector2 v = pointList[i];
            v.x = (float)i / (float)index;
            pointList[i] = v;
        }
        lineRenderer.Points = pointList.ToArray();
    }

    // Shift all points left to make room for a new point (chop off first point)
    public void ShiftPointsLeft() {
        var pointList = new List<Vector2>(lineRenderer.Points);
        pointList.RemoveAt(0);
        for (int i = 0; i < pointList.Count; i++) {
            Vector2 v = pointList[i];
            v.x -= 1f / (float)windowSize;
            pointList[i] = v;
        }
        lineRenderer.Points = pointList.ToArray();
    }

    // Add a point to the gui
    public void AddPoint(float y) {
        var pointList = new List<Vector2>(lineRenderer.Points);
        pointList.Add(new Vector2((float)index / (fixedWidth ? windowSize : (float)index), y));
        lineRenderer.Points = pointList.ToArray();
    }
}
