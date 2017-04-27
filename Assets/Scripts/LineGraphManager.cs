using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineGraphManager : MonoBehaviour {

    public LineGraph[] graphs;
    public PopulationLineGraph popGraph;

	// Use this for initialization
	void Start () {
        // setup the population graph
        popGraph.StartCoroutine("UpdateGraph");

        // setup the trade frequency graph
        LineGraph trades = graphs[0];
        trades.myDelegate = SimManager.instance.GetTradeFrequency;
        trades.StartCoroutine("UpdateGraph");
    }
	
}
