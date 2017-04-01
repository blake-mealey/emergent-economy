using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineGraphManager : MonoBehaviour {

    public LineGraph[] graphs;
    public PopulationLineGraph popGraph;

	// Use this for initialization
	void Start () {
        /*LineGraph population = graphs[0];
        population.fixedWidth = true;
        population.maxWindowSize = population.windowSize = 300;
        population.myDelegate = SimManager.instance.GetPopulation;
        population.StartCoroutine("UpdateGraph");*/

        popGraph.StartCoroutine("UpdateGraph");

        LineGraph trades = graphs[0];
        trades.myDelegate = SimManager.instance.GetTradeFrequency;
        trades.StartCoroutine("UpdateGraph");
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
