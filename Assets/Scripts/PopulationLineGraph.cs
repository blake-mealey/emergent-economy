using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class PopulationLineGraph : MonoBehaviour {

    public GameObject linePrefab;

    List<UILineRenderer> lines;
    int index;
    float highestValue = 1;
    float prevHighestValue;

    public bool fixedWidth;
    public int windowSize;
    public int maxWindowSize;

    // Use this for initialization
    void Start () {
        lines = new List<UILineRenderer>();
        index = 0;
    }

    IEnumerator UpdateGraph() {
        yield return new WaitForSeconds(1f);

        foreach(Color color in SimManager.instance.colors) {
            GameObject line = Instantiate(linePrefab, transform);
            RectTransform rt = line.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(10, 10);
            rt.offsetMax = new Vector2(-10, -10);
            UILineRenderer lineRenderer = line.GetComponent<UILineRenderer>();
            lineRenderer.color = color;
            lines.Add(lineRenderer);
        }

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

            for (int i = 0; i < lines.Count; i++) {
                UILineRenderer lineRenderer = lines[i];

                float value = (float)SimManager.instance.populations[i];
                if (value > highestValue) {
                    prevHighestValue = highestValue;
                    highestValue = value;
                    SquishPointsVertically();
                }
                AddPoint(lineRenderer, value / highestValue);
            }

            yield return new WaitForSeconds(0.25f);
        }
    }

    public void SquishPointsVertically() {
        foreach (UILineRenderer lineRenderer in lines) {
            var pointList = new List<Vector2>(lineRenderer.Points);
            for (int i = 0; i < pointList.Count; i++) {
                Vector2 v = pointList[i];
                v.y = v.y * (prevHighestValue / highestValue);
                pointList[i] = v;
            }
            lineRenderer.Points = pointList.ToArray();
        }
    }

    public void SquishPointsHorizontally() {
        foreach (UILineRenderer lineRenderer in lines) {
            var pointList = new List<Vector2>(lineRenderer.Points);
            for (int i = 0; i < pointList.Count; i++) {
                Vector2 v = pointList[i];
                v.x = (float)i / (float)index;
                pointList[i] = v;
            }
            lineRenderer.Points = pointList.ToArray();
        }
    }

    public void ShiftPointsLeft() {
        foreach (UILineRenderer lineRenderer in lines) {
            var pointList = new List<Vector2>(lineRenderer.Points);
            pointList.RemoveAt(0);
            for (int i = 0; i < pointList.Count; i++) {
                Vector2 v = pointList[i];
                v.x -= 1f / (float)windowSize;
                pointList[i] = v;
            }
            lineRenderer.Points = pointList.ToArray();
        }
    }

    public void AddPoint(UILineRenderer lineRenderer, float y) {
        var pointList = new List<Vector2>(lineRenderer.Points);
        pointList.Add(new Vector2((float)index / (fixedWidth ? windowSize : (float)index), y));
        lineRenderer.Points = pointList.ToArray();
    }
}
