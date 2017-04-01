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
        yield return new WaitForSeconds(1f);
        while (true) {
            index++;
            if (fixedWidth) {
                if (index >= windowSize) {
                    index--;
                    ShiftPointsLeft();
                }
            } else {
                fixedWidth = index == maxWindowSize;
                SquishPointsHorizontally();
            }
            float value = (float)myDelegate();
            if(value > highestValue) {
                prevHighestValue = highestValue;
                highestValue = value;
                SquishPointsVertically();
            }
            AddPoint(value / highestValue);
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void SquishPointsVertically() {
        var pointList = new List<Vector2>(lineRenderer.Points);
        for (int i = 0; i < pointList.Count; i++) {
            Vector2 v = pointList[i];
            v.y = v.y * (prevHighestValue / highestValue);
            pointList[i] = v;
        }
        lineRenderer.Points = pointList.ToArray();
    }

    public void SquishPointsHorizontally() {
        var pointList = new List<Vector2>(lineRenderer.Points);
        for (int i = 0; i < pointList.Count; i++) {
            Vector2 v = pointList[i];
            v.x = (float)i / (float)index;
            pointList[i] = v;
        }
        lineRenderer.Points = pointList.ToArray();
    }

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

    public void AddPoint(float y) {
        var pointList = new List<Vector2>(lineRenderer.Points);
        pointList.Add(new Vector2((float)index / (fixedWidth ? windowSize : (float)index), y));
        lineRenderer.Points = pointList.ToArray();
    }
}
