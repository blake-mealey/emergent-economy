using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using System.Text;

public class PopulationLineGraph : MonoBehaviour {

    // prefabs
    public GameObject linePrefab;
    public GameObject labelPrefab;
    public GameObject ratioColumnPrefab;
    public GameObject ratioLabelPrefab;
    public GameObject eventMarkerPrefab;

    // object references
    RectTransform marker;
    GameObject ratiosPanel;
    
    // data structures
    List<GraphEventData> eventMarkers;
    List<Vector2>[] graphHistories;
    List<Vector2>[] currentGraphs;
    UILineRenderer[] lines;
    Text[] labels;
    Text[,] ratioLabels;
    int index;
    float highestValue = 1;
    float prevHighestValue;

    // graph options
    public bool fixedWidth;
    public int windowSize;
    public int maxWindowSize;

    // state information
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

    // save the population, trade table, and event data to a .csv file
    private void SaveData() {
        // get a out#.csv filename that is not already in the directory
        string baseFileName = "out";
        string fileName = baseFileName;
        string fileExt = "csv";
        int index = 2;
        while (File.Exists(fileName + "." + fileExt)) {
            fileName = baseFileName + index++;
        }
        // open for writing
        var sr = File.CreateText(fileName + "." + fileExt);

        // initialize our different lines for printing
        int groupCount = SimManager.instance.numberOfGroups;

        StringBuilder times = new StringBuilder("Time,");
        StringBuilder[] pops = new StringBuilder[groupCount];
        StringBuilder[,] ratios = new StringBuilder[groupCount,groupCount];
        StringBuilder eventTimes = new StringBuilder("Time,");
        StringBuilder[] events = new StringBuilder[groupCount];

        // initialize output lines (row names)
        for (int i = 0; i < groupCount; i++) {
            pops[i] = new StringBuilder("Population " + i + ",");
            events[i] = new StringBuilder("Group " + i + ",");
            for (int j = 0; j < groupCount; j++) {
                if (i == j) continue;
                ratios[i, j] = new StringBuilder(i + " to " + j + ",");
            }
        }

        // add time, population, and ratio data
        for (int i = 0; i < graphHistories[0].Count; i++) {
            float idx = (float)i;
            // add time reference
            times.Append(idx * 0.25f);
            times.Append(",");

            var snapshot = SimManager.instance.GetGlobalTradeRatioSnapshot(i);
            for (int x = 0; x < groupCount; x++) {
                // add population at time
                pops[x].Append(graphHistories[x][i].y);
                pops[x].Append(",");
                
                for (int y = 0; y < groupCount; y++) {
                    if (x == y) continue;
                    // add each ratio at time
                    ratios[x, y].Append(snapshot[x,y]);
                    ratios[x, y].Append(",");
                }
            }
        }

        // add events data
        for (int i = 0; i < eventMarkers.Count; i++) {
            // add time references
            eventTimes.Append(eventMarkers[i].timeStamp);
            eventTimes.Append(",");
            for (int j = 0; j < groupCount; j++) {
                // add each event if there was one at this time
                if (eventMarkers[i].groupId == j) {
                    events[j].Append(1);
                }
                events[j].Append(",");
            }
        }

        // write the data to the file

        // write the first times
        sr.WriteLine(times);

        // write all of the population data
        for (int i = 0; i < groupCount; i++) {
            sr.WriteLine(pops[i]);
        }

        // write all of the ratio data
        for (int i = 0; i < groupCount; i++) {
            for (int j = 0; j < groupCount; j++) {
                if (i == j) continue;
                sr.WriteLine(ratios[i, j]);
            }
        }

        // write a seperating line, then the event times
        sr.WriteLine();
        sr.WriteLine(eventTimes);

        // write all the event data
        for (int i = 0; i < groupCount; i++) {
            sr.WriteLine(events[i]);
        }

        // close the writer
        sr.Close();
    }

    IEnumerator UpdateGraph() {
        // Wait for SimManager to setup the simulation
        yield return new WaitForSeconds(1f);

        // initialize data structures
        int groupCount = SimManager.instance.numberOfGroups;
        lines = new UILineRenderer[groupCount];
        labels = new Text[groupCount];
        ratioLabels = new Text[groupCount,groupCount];
        graphHistories = new List<Vector2>[groupCount];
        currentGraphs = new List<Vector2>[groupCount];

        // initialize the ui
        Transform legend = transform.Find("Legend");

        // initialize the trade ratio UI
        for (int i = 0; i < groupCount; i++) {
            GameObject column = Instantiate(ratioColumnPrefab, ratiosPanel.transform);
            for (int j = 0; j < groupCount; j++) {
                if (i != j) {
                    GameObject label = Instantiate(ratioLabelPrefab, column.transform);
                    ratioLabels[i, j] = label.GetComponent<Text>();
                }
            }
        }

        // Make the first snapshot and update the UI
        SimManager.instance.MakeGlobalTradeRatioSnapshot();
        UpdateTradeRatios();

        // initialize the chart
        for (int i = 0; i < lines.Length; i++) {
            // initialize data structures
            graphHistories[i] = new List<Vector2>();
            currentGraphs[i] = new List<Vector2>();

            // Create a line for each group
            Color color = SimManager.instance.colors[i];

            GameObject line = Instantiate(linePrefab, transform);
            RectTransform rt = line.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(10, 10);
            rt.offsetMax = new Vector2(-10, -10);
            UILineRenderer lineRenderer = line.GetComponent<UILineRenderer>();
            lineRenderer.color = color;
            lines[i] = lineRenderer;
            line.GetComponent<Outline>().effectColor = color * 0.7f;

            // Create a label for each group
            GameObject label = Instantiate(labelPrefab, legend);
            labels[i] = label.GetComponent<Text>();
            labels[i].color = color;
            labels[i].GetComponent<Outline>().effectColor = color * 0.7f;
        }

        // Calculate UI dimensions and position
        UpdateWorldRect();

        // finished initialization
        initialized = true;

        // record start time
        tStart = Time.time;

        // know the number of dead resources before each iteration
        int lastDeadResourceCount = SimManager.instance.GetDeadResourceCount();

        while (true) {
            // update each line
            for (int i = 0; i < lines.Length; i++) {
                UILineRenderer lineRenderer = lines[i];

                // get the latest population
                float value = (float)SimManager.instance.populations[i];

                // if we aren't maximized, we update the label
                // if we are maximized, this is handled in Update
                if(!maximized)
                    UpdateLabel(i, value);

                // if we have a new high value we re-size the points to fit vertically
                if (value > highestValue) {
                    prevHighestValue = highestValue;
                    highestValue = value;
                    SquishPointsVertically();
                }
                // add the point to the chart
                AddPoint(i, value);
            }

            // fill the window correctly
            index++;
            if (fixedWidth) {// if fixed width and we have filled window, shift points left
                if (index >= windowSize + 2) {
                    index--;
                    ShiftPointsLeft();
                }
            } else {// else squish points to fill window
                fixedWidth = index == maxWindowSize;
                SquishPointsHorizontally();
            }
            
            // render the updates to the data
            UpdateLines();

            // Make a new snapshot of the global trade ratio table to get the latest data
            SimManager.instance.MakeGlobalTradeRatioSnapshot();
            // if we aren't maximized we update the ratios table UI
            // if we are maximized this is handled in Update
            if (!maximized) UpdateTradeRatios();

            // get the current dead resource count
            // if it is different from last iteration, we know a new event occurred
            int deadResourceCount = SimManager.instance.GetDeadResourceCount();
            if (deadResourceCount > lastDeadResourceCount) {
                for (int i = lastDeadResourceCount; i < deadResourceCount; i++) {
                    // make a new marker for the dead resource and add it to the chart
                    GraphEventData eventData = SimManager.instance.GetDeadResource(i);
                    GameObject eventMarker = Instantiate(eventMarkerPrefab, transform);
                    eventMarker.GetComponent<Image>().color = eventData.color;
                    eventData.transform = eventMarker.GetComponent<RectTransform>();
                    eventData.transform.offsetMin = new Vector2(eventData.transform.offsetMin.x, 5);
                    eventData.transform.offsetMax = new Vector2(eventData.transform.offsetMax.x, -5);
                    eventData.transform.sizeDelta = new Vector2(maximized ? 2 : 0, eventData.transform.sizeDelta.y);
                    eventMarkers.Add(eventData);
                }
                // update the current known dead resource count
                lastDeadResourceCount = deadResourceCount;
            }

            // if we are maximized, we update the event markers UI
            // if we aren't maximized we don't display the event markers
            if (maximized) UpdateEventMarkers();

            // if all the agents have died, we stop getting new data
            if (SimManager.instance.GetPopulation() == 0) break;

            // repeat every 0.25 seconds (times time speed)
            yield return new WaitForSeconds(0.25f);
        }

        // record the end time
        tEnd = Time.time;

        // we are no longer getting new data
        finished = true;

        // save all the data to a .csv file
        SaveData();
    }

    // Fit points vertically (highest value at 1)
    public void SquishPointsVertically() {
        foreach (var pointList in currentGraphs) {
            for (int i = 0; i < pointList.Count; i++) {
                Vector2 v = pointList[i];
                v.y = v.y * (prevHighestValue / highestValue);
                pointList[i] = v;
            }
        }
    }

    // Fit all points in the window
    public void SquishPointsHorizontally() {
        foreach (var pointList in currentGraphs) {
            for (int i = 0; i < pointList.Count; i++) {
                Vector2 v = pointList[i];
                v.x = (float)i / (float)index;
                pointList[i] = v;
            }
        }
    }

    // Shift all points left to make room for a new point (chop off first point)
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
    
    // Add a point to the data list
    public void AddPoint(int lineIndex, float y) {
        List<Vector2> pointList = currentGraphs[lineIndex];
        pointList.Add(new Vector2((float)index / (fixedWidth ? windowSize : (float)index), y / highestValue));
        graphHistories[lineIndex].Add(new Vector2(0f, y));
    }

    // update the trade ratio UI
    public void UpdateTradeRatios() {
        float[,] snapshot;

        // if we are maximized, we get the snapshot based on the cursor position
        // else we get the latest snapshot
        if (maximized)
            snapshot = SimManager.instance.GetGlobalTradeRatioSnapshot(
                (int)(markerRatio * (float)SimManager.instance.GetGlobalTradeRatioSnapshotsCount()));
        else
            snapshot = SimManager.instance.GetGlobalTradeRatioSnapshot();

        // update each of the UI labels with the snapshot's data
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

    // update a line's label with a new value
    public void UpdateLabel(int lineIndex, float value) {
        // if it is not already that value, update it
        if (!labels[lineIndex].text.Equals(value.ToString())) {
            labels[lineIndex].text = value.ToString();
        }
    }

    // update each graph line UI
    public void UpdateLines() {
        // wait until initialized
        if (!initialized) return;
        // go through each line
        for (int i = 0; i < lines.Length; i++) {
            // if not maximized then just set the line's points to the current window's data
            if (!maximized) {
                lines[i].Points = currentGraphs[i].ToArray();
            } else {
                // else, get the full history for this group
                List<Vector2> history = graphHistories[i];

                // declare an array to set the line's points to
                Vector2[] outPoints = new Vector2[history.Count];

                // squish veritcally and horizontally and store in the outPoints array
                float xdiv = (float)history.Count - 1;
                for (int j = 0; j < history.Count; j++) {
                    var v = history[j];
                    outPoints[j] = new Vector2((float)j / xdiv, v.y / highestValue);
                }

                // set the line's points to the outPoints array
                lines[i].Points = outPoints;
            }
        }
    }

    // calculate the position and dimensions of the chart
    public void UpdateWorldRect() {
        Vector3[] corners = new Vector3[4];
        GetComponent<RectTransform>().GetWorldCorners(corners);
        worldRect = new Rect(corners[0] + new Vector3(10, 10), corners[2] - corners[0] - new Vector3(20, 20));
    }

    // update the event markers UI
    public void UpdateEventMarkers() {
        // for each of the events stored, update the position in the chart
        foreach (GraphEventData eventData in eventMarkers) {
            float r = (eventData.timeStamp - tStart) / ((finished ? tEnd : Time.time) - tStart);
            float x = r * worldRect.width;
            eventData.transform.position = new Vector2(x + 20, eventData.transform.position.y);
        }
    }

    // switch to maximized mode
    public void maximize() {
        // re-size the UI
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMax = new Vector2(-65, rt.offsetMax.y);

        // make the marker visible (width 1)
        marker.sizeDelta = new Vector2(1, marker.sizeDelta.y);

        // for each event, make its marker visible (width 2)
        foreach (GraphEventData eventData in eventMarkers) {
            eventData.transform.sizeDelta = new Vector2(2, eventData.transform.sizeDelta.y);
        }

        // re-position the ratio panel UI
        ratiosPanel.GetComponent<RectTransform>().offsetMin = new Vector2(260, -210);
        ratiosPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 200);

        // re-calculate the rect
        UpdateWorldRect();

        // update the lines UI
        UpdateLines();
    }

    // switch from maximized mode
    public void minimize() {
        // re-size the UI
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(250, 100);

        // make the marker invisible (width 0)
        marker.sizeDelta = new Vector2(0, marker.sizeDelta.y);

        // for each event, make its marker invisible (width 0)
        foreach (GraphEventData eventData in eventMarkers) {
            eventData.transform.sizeDelta = new Vector2(0, eventData.transform.sizeDelta.y);
        }

        // update the labels to the correct values
        for (int i = 0; i < lines.Length; i++) {
            var points = currentGraphs[i];
            UpdateLabel(i, points[points.Count - 1].y);
        }

        // re-position the ratio panel UI
        ratiosPanel.GetComponent<RectTransform>().offsetMin = new Vector2(310, -100);
        ratiosPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 200);

        // re-calculate the rect
        UpdateWorldRect();

        // update the lines UI
        UpdateLines();
    }

    public void Update() {
        if (Input.GetKeyUp(KeyCode.Tab)) {
            // Toggle maximized mode when Tab is pressed
            maximized = !maximized;
            if (maximized) {
                maximize();
            } else {
                minimize();
            }
        } else if(Input.GetKeyUp(KeyCode.R)) {
            // toggle ratio panel visiblity when R is pressed
            ratiosPanel.SetActive(!ratiosPanel.activeSelf);
        }

        // Update marker
        if (maximized) {
            if (worldRect.Contains(Input.mousePosition)) {
                // if the mouse is in the rect, calculate the position within it
                float x = Input.mousePosition.x - worldRect.x;
                float r = x / worldRect.width;

                // set the marker position accordingly (+20 for padding)
                marker.position = new Vector2(x + 20, marker.position.y);

                // calculate the index in the histories array for where the marker is pointing
                int lineIndex = (int)(r * (float)graphHistories[0].Count);
                if (lineIndex != markerIndex) {
                    markerIndex = lineIndex;
                    markerRatio = r;
                    // update the labels accordingly
                    for (int i = 0; i < lines.Length; i++) {
                        UpdateLabel(i, graphHistories[i][lineIndex].y);
                    }
                }
            } else {
                // if we are maximized but the mouse is not in the rect
                // we still need to update because we are getting new data so the marker is pointing
                // at different data all the time (but we don't need to reposition the marker)
                int lineIndex = (int)(markerRatio * (float)graphHistories[0].Count);
                if (lineIndex != markerIndex) {
                    markerIndex = lineIndex;
                    for (int i = 0; i < lines.Length; i++) {
                        UpdateLabel(i, graphHistories[i][lineIndex].y);
                    }
                }
            }
            // update the trade ratio table either way
            UpdateTradeRatios();
        }

        // if we are finished and we are maximized we update the event markers
        // this is for when the simulation ends but the user leaves and reenters maximized mode
        // it should re-set the event markers UI
        if (finished && maximized) {
            UpdateEventMarkers();
        }
    }
}
