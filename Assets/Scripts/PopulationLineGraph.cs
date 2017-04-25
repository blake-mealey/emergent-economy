using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class PopulationLineGraph : MonoBehaviour {

    public GameObject linePrefab;
    public GameObject labelPrefab;
    public GameObject ratioColumnPrefab;
    public GameObject ratioLabelPrefab;

    RectTransform marker;
    GameObject ratiosPanel;

    List<Vector2>[] graphHistories;
    List<Vector2>[] currentGraphs;
    UILineRenderer[] lines;
    Text[] labels;
    Text[,] ratioLabels;
    int index;
    float highestValue = 1;
    float prevHighestValue;

    public bool fixedWidth;
    public int windowSize;
    public int maxWindowSize;

    float markerRatio = 0;
    int markerIndex = 0;
    bool maximized = false;
    bool initialized = false;

    Rect worldRect;

    // Use this for initialization
    void Start () {
        index = 0;
        windowSize -= 1; // So positioning logic works correctly

        marker = transform.Find("Marker").GetComponent<RectTransform>();
        ratiosPanel = transform.Find("TradeRatios").gameObject;
    }

    IEnumerator UpdateGraph() {
        yield return new WaitForSeconds(1f);

        int groupCount = SimManager.instance.numberOfGroups;
        lines = new UILineRenderer[groupCount];
        labels = new Text[groupCount];
        ratioLabels = new Text[groupCount,groupCount];
        graphHistories = new List<Vector2>[groupCount];
        currentGraphs = new List<Vector2>[groupCount];
        
        Transform legend = transform.Find("Legend");

        for (int i = 0; i < groupCount; i++) {
            GameObject column = Instantiate(ratioColumnPrefab, ratiosPanel.transform);

            for (int j = 0; j < groupCount; j++) {
                if (i != j) {
                    GameObject label = Instantiate(ratioLabelPrefab, column.transform);
                    ratioLabels[i, j] = label.GetComponent<Text>();
                }
            }
        }
        UpdateTradeRatios();

        for (int i = 0; i < lines.Length; i++) {
            graphHistories[i] = new List<Vector2>();
            currentGraphs[i] = new List<Vector2>();

            Color color = SimManager.instance.colors[i];

            GameObject line = Instantiate(linePrefab, transform);
            RectTransform rt = line.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(10, 10);
            rt.offsetMax = new Vector2(-10, -10);
            UILineRenderer lineRenderer = line.GetComponent<UILineRenderer>();
            lineRenderer.color = color;
            lines[i] = lineRenderer;
            line.GetComponent<Outline>().effectColor = color * 0.7f;

            GameObject label = Instantiate(labelPrefab, legend);
            labels[i] = label.GetComponent<Text>();
            labels[i].color = color;
            labels[i].GetComponent<Outline>().effectColor = color * 0.7f;
        }

        UpdateWorldRect();
        initialized = true;

        while (true) {
            for (int i = 0; i < lines.Length; i++) {
                UILineRenderer lineRenderer = lines[i];

                float value = (float)SimManager.instance.populations[i];

                if(!maximized)
                    UpdateLabel(i, value);

                if (value > highestValue) {
                    prevHighestValue = highestValue;
                    highestValue = value;
                    SquishPointsVertically();
                }
                AddPoint(i, value);
            }

            index++;
            if (fixedWidth) {
                if (index >= windowSize + 2) {
                    index--;
                    ShiftPointsLeft();
                }
            } else {
                fixedWidth = index == maxWindowSize;
                SquishPointsHorizontally();
            }
            
            //if(!maximized)
                UpdateLines();

            UpdateTradeRatios();

            if (SimManager.instance.GetPopulation() == 0) break;
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void SquishPointsVertically() {
        foreach (var pointList in currentGraphs) {
            for (int i = 0; i < pointList.Count; i++) {
                Vector2 v = pointList[i];
                v.y = v.y * (prevHighestValue / highestValue);
                pointList[i] = v;
            }
        }
    }

    public void SquishPointsHorizontally() {
        foreach (var pointList in currentGraphs) {
            for (int i = 0; i < pointList.Count; i++) {
                Vector2 v = pointList[i];
                v.x = (float)i / (float)index;
                pointList[i] = v;
            }
        }
    }

    public void ShiftPointsLeft() {
        foreach (var pointList in currentGraphs) {
            pointList.RemoveAt(0);
            for (int i = 0; i < pointList.Count; i++) {
                Vector2 v = pointList[i];
                v.x -= 1f / (float)windowSize;
                pointList[i] = v;
            }
        }
    }

    public void AddPoint(int lineIndex, float y) {
        List<Vector2> pointList = currentGraphs[lineIndex];
        pointList.Add(new Vector2((float)index / (fixedWidth ? windowSize : (float)index), y / highestValue));
        graphHistories[lineIndex].Add(new Vector2(0f, y));
    }

    public void UpdateTradeRatios() {
        SimManager.instance.MakeGlobalTradeRatioSnapshot();
        float[,] snapshot;
        if (maximized)
            snapshot = SimManager.instance.GetGlobalTradeRatioSnapshot(markerIndex);
        else
            snapshot = SimManager.instance.GetGlobalTradeRatioSnapshot();

        for (int i = 0; i < ratioLabels.GetLength(0); i++) {
            for (int j = 0; j < ratioLabels.GetLength(1); j++) {
                if (i != j) {
                    string c0 = ColorUtility.ToHtmlStringRGB(SimManager.instance.colors[i]);
                    string c1 = ColorUtility.ToHtmlStringRGB(SimManager.instance.colors[j]);
                    ratioLabels[i, j].text = string.Format(
                        "<color=#{0}>{1}</color> : <color=#{2}>{3}</color>",
                        c0, 1, c1, snapshot[i, j].ToString("F3"));
                }
            }
        }
    }

    public void UpdateLabel(int lineIndex, float value) {
        if (!labels[lineIndex].text.Equals(value.ToString())) {
            labels[lineIndex].text = value.ToString();
        }
    }

    public void UpdateLines() {
        if (!initialized) return;
        for (int i = 0; i < lines.Length; i++) {
            if (!maximized) {
                lines[i].Points = currentGraphs[i].ToArray();
            } else {
                List<Vector2> history = graphHistories[i];
                Vector2[] outPoints = new Vector2[history.Count];

                float xdiv = (float)history.Count - 1;
                for (int j = 0; j < history.Count; j++) {
                    var v = history[j];
                    outPoints[j] = new Vector2((float)j / xdiv, v.y / highestValue);
                }
                lines[i].Points = outPoints;
            }
        }
    }

    public void UpdateWorldRect() {
        Vector3[] corners = new Vector3[4];
        GetComponent<RectTransform>().GetWorldCorners(corners);
        worldRect = new Rect(corners[0] + new Vector3(10, 10), corners[2] - corners[0] - new Vector3(20, 20));
    }

    public void maximize() {
        // TODO: Pause simulation

        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMax = new Vector2(-65, rt.offsetMax.y);

        marker.sizeDelta = new Vector2(1, marker.sizeDelta.y);

        ratiosPanel.GetComponent<RectTransform>().offsetMin = new Vector2(260, -210);
        ratiosPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 200);

        UpdateWorldRect();
        UpdateLines();
    }

    public void minimize() {
        // TODO: Unpause simulation

        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(250, 100);

        marker.sizeDelta = new Vector2(0, marker.sizeDelta.y);

        for (int i = 0; i < lines.Length; i++) {
            var points = currentGraphs[i];
            UpdateLabel(i, points[points.Count - 1].y);
        }

        ratiosPanel.GetComponent<RectTransform>().offsetMin = new Vector2(310, -100);
        ratiosPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 200);

        UpdateWorldRect();
        UpdateLines();
    }

    public void Update() {
        // Toggle maximized when Tab is pressed
        if (Input.GetKeyUp(KeyCode.Tab)) {
            maximized = !maximized;
            if (maximized) {
                maximize();
            } else {
                minimize();
            }
        } else if(Input.GetKeyUp(KeyCode.R)) {
            ratiosPanel.SetActive(!ratiosPanel.activeSelf);
        }

        // Update marker
        if (maximized) {
            if (worldRect.Contains(Input.mousePosition)) {
                float x = Input.mousePosition.x - worldRect.x;
                float r = x / worldRect.width;
                marker.position = new Vector2(x + 20, marker.position.y);

                int lineIndex = (int)(r * (float)graphHistories[0].Count);
                if (lineIndex != markerIndex) {
                    markerIndex = lineIndex;
                    markerRatio = r;
                    for (int i = 0; i < lines.Length; i++) {
                        UpdateLabel(i, graphHistories[i][lineIndex].y);
                    }
                }
            } else {
                int lineIndex = (int)(markerRatio * (float)graphHistories[0].Count);
                if (lineIndex != markerIndex) {
                    markerIndex = lineIndex;
                    for (int i = 0; i < lines.Length; i++) {
                        UpdateLabel(i, graphHistories[i][lineIndex].y);
                    }
                }
            }
        }
    }
}
