using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For LINQ operations that simplify handling collections

/**
 * Represents an electrical conductor in a circuit.
 */
public class Conduction : MonoBehaviour
{
    public bool positivePassThrough = false;
    public bool negativePassThrough = false;
    public bool simulationActiveState = false;
    public bool loopIsClosed = false;
    public float voltage;
    public float current;
    public float resistance;
    public float localResistance;
    public int positiveNumberInSeries;
    public int negativeNumberInSeries;

    // Added: Collection to manage parallel connections
    private List<Conduction> parallelConnections = new List<Conduction>();

    private Simulation Simulation;

    private void Start()
    {
        Simulation = GameObject.Find("Simulation Event Handler").GetComponent<Simulation>();
    }

    public bool LoopIsClosed()
    {
        return loopIsClosed;
    }

    private void OnTriggerStay(Collider other)
    {
        simulationActiveState = Simulation.simulationActiveState;

        if (simulationActiveState)
        {
            ClosedLoopRoutine(other);
        }
        else
        {
            OpenLoopRoutine();
        }
    }

    private void ClosedLoopRoutine(Collider other)
    {
        UpdateParallelConnections(other); // Handles updating the state based on parallel connections

        if (positivePassThrough && negativePassThrough)
        {
            loopIsClosed = true;
        }

        GameObject otherObject = other.gameObject;

        if (otherObject.tag == "Power_Source_Positive" && positiveNumberInSeries == 0)
        {
            positivePassThrough = true;
            positiveNumberInSeries += 1;
            voltage = otherObject.GetComponent<PowerSource>().getPowerSourceVoltage();
        }

        if (otherObject.tag == "Power_Source_Negative" && negativeNumberInSeries == 0)
        {
            negativePassThrough = true;
            negativeNumberInSeries += 1;
        }

        if (otherObject.tag == "Power_Source_Negative" && current == 0)
        {
            current = otherObject.GetComponent<PowerSource>().getCurrent();
        }

        Conduction otherObjectConduction = otherObject.GetComponent<Conduction>();
        if (otherObjectConduction)
        {
            AggregateConductionProperties(otherObjectConduction);
        }
    }

    private void OpenLoopRoutine()
    {
        negativeNumberInSeries = 0;
        negativePassThrough = false;
        positiveNumberInSeries = 0;
        positivePassThrough = false;
        voltage = 0;
        current = 0;
        resistance = 0;
        loopIsClosed = false;
        parallelConnections.Clear();
    }

    // New Method: Handles updating the state based on parallel connections
    private void UpdateParallelConnections(Collider other)
    {
        Conduction otherConduction = other.GetComponent<Conduction>();
        if (otherConduction != null && !parallelConnections.Contains(otherConduction))
        {
            parallelConnections.Add(otherConduction);
            RecalculateParallelProperties();
        }
    }

    // New Method: Aggregates properties from parallel connections
    private void RecalculateParallelProperties()
    {
        voltage = parallelConnections.Max(conduction => conduction.voltage); // Assumes voltage is the same across parallel paths
        current = parallelConnections.Sum(conduction => conduction.current); // Total current is the sum of currents in parallel paths
        resistance = 1 / parallelConnections.Sum(conduction => 1 / conduction.resistance); // Reciprocal of the sum of reciprocals of resistances
    }

    // Refactored for clarity and efficiency
    private void AggregateConductionProperties(Conduction other)
    {
        positivePassThrough |= other.positivePassThrough;
        negativePassThrough |= other.negativePassThrough;
        positiveNumberInSeries = Mathf.Max(positiveNumberInSeries, other.positiveNumberInSeries + (positivePassThrough ? 1 : 0));
        negativeNumberInSeries = Mathf.Max(negativeNumberInSeries, other.negativeNumberInSeries + (negativePassThrough ? 1 : 0));

        if (voltage == 0) voltage = other.voltage;
        if (current == 0) current = other.current;
        if (resistance == 0) resistance = other.resistance + localResistance;
    }
}
