﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using System.Text;

public class PopulationLineGraph : MonoBehaviour {

    public GameObject linePrefab;
    public GameObject labelPrefab;
    public GameObject ratioColumnPrefab;
    public GameObject ratioLabelPrefab;
    public GameObject eventMarkerPrefab;

    RectTransform marker;
    GameObject ratiosPanel;

    List<GraphEventData> eventMarkers;

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

    bool finished = false;
    float tStart;
    float tEnd;

    Rect worldRect;

    // Use this for initialization
    void Start () {
        index = 0;
        windowSize -= 1; // So positioning logic works correctly

        marker = transform.Find("Marker").GetComponent<RectTransform>();
        ratiosPanel = transform.Find("TradeRatios").gameObject;
        eventMarkers = new List<GraphEventData>();
    }

    private void SaveData() {
        string baseFileName = "out";
        string fileName = baseFileName;
        string fileExt = "csv";
        int index = 2;
        while (File.Exists(fileName + "." + fileExt)) {
            fileName = baseFileName + index++;
        }
        var sr = File.CreateText(fileName + "." + fileExt);

        int groupCount = SimManager.instance.numberOfGroups;
        StringBuilder times = new StringBuilder("Time,");
        StringBuilder[] pops = new StringBuilder[groupCount];
        StringBuilder[,] ratios = new StringBuilder[groupCount,groupCount];
        for (int i = 0; i < groupCount; i++) {
            pops[i] = new StringBuilder("Group " + i + " Population:,");
            for (int j = 0; j < groupCount; j++) {
                if (i == j) continue;
                ratios[i, j] = new StringBuilder(i + " to " + j + ",");
            }
        }

        for (int i = 0; i < graphHistories[0].Count; i++) {
            float idx = (float)i;
            times.Append(idx * 0.25f);
            times.Append(",");

            var snapshot = SimManager.instance.GetGlobalTradeRatioSnapshot(i);
            for (int x = 0; x < groupCount; x++) {
                pops[x].Append(graphHistories[x][i].y);
                pops[x].Append(",");
                
                for (int y = 0; y < groupCount; y++) {
                    if (x == y) continue;
                    ratios[x, y].Append(snapshot[x,y]);
                    ratios[x, y].Append(",");
                }
            }
        }
        
        sr.WriteLine(times);

        for (int i = 0; i < groupCount; i++) {
            sr.WriteLine(pops[i]);
        }

        for (int i = 0; i < groupCount; i++) {
            for (int j = 0; j < groupCount; j++) {
                if (i == j) continue;
                sr.WriteLine(ratios[i, j]);
            }
        }

        sr.Close();
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
        SimManager.instance.MakeGlobalTradeRatioSnapshot();
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

        tStart = Time.time;
        int lastDeadResourceCount = SimManager.instance.GetDeadResourceCount();

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
            
            UpdateLines();

            SimManager.instance.MakeGlobalTradeRatioSnapshot();
            if (!maximized) UpdateTradeRatios();

            int deadResourceCount = SimManager.instance.GetDeadResourceCount();
            if (deadResourceCount > lastDeadResourceCount) {
                for (int i = lastDeadResourceCount; i < deadResourceCount; i++) {
                    GraphEventData eventData = SimManager.instance.GetDeadResource(i);
                    GameObject eventMarker = Instantiate(eventMarkerPrefab, transform);
                    eventMarker.GetComponent<Image>().color = eventData.color;
                    eventData.transform = eventMarker.GetComponent<RectTransform>();
                    eventData.transform.offsetMin = new Vector2(eventData.transform.offsetMin.x, 5);
                    eventData.transform.offsetMax = new Vector2(eventData.transform.offsetMax.x, -5);
                    eventData.transform.sizeDelta = new Vector2(maximized ? 2 : 0, eventData.transform.sizeDelta.y);
                    eventMarkers.Add(eventData);
                }
                lastDeadResourceCount = deadResourceCount;
            }

            if (maximized) UpdateEventMarkers();

            if (SimManager.instance.GetPopulation() == 0) break;
            yield return new WaitForSeconds(0.25f);
        }

        tEnd = Time.time;
        finished = true;

        SaveData();
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
        float[,] snapshot;
        if (maximized)
            snapshot = SimManager.instance.GetGlobalTradeRatioSnapshot(
                (int)(markerRatio * (float)SimManager.instance.GetGlobalTradeRatioSnapshotsCount()));
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

    public void UpdateEventMarkers() {
        foreach (GraphEventData eventData in eventMarkers) {
            float r = (eventData.timeStamp - tStart) / ((finished ? tEnd : Time.time) - tStart);
            float x = r * worldRect.width;
            eventData.transform.position = new Vector2(x + 20, eventData.transform.position.y);
        }
    }

    public void maximize() {
        // TODO: Pause simulation

        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMax = new Vector2(-65, rt.offsetMax.y);

        marker.sizeDelta = new Vector2(1, marker.sizeDelta.y);

        foreach (GraphEventData eventData in eventMarkers) {
            eventData.transform.sizeDelta = new Vector2(2, eventData.transform.sizeDelta.y);
        }

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

        foreach (GraphEventData eventData in eventMarkers) {
            eventData.transform.sizeDelta = new Vector2(0, eventData.transform.sizeDelta.y);
        }

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
            UpdateTradeRatios();
        }

        if (finished && maximized) {
            UpdateEventMarkers();
        }
    }
}
