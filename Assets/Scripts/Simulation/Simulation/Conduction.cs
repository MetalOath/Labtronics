using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Represents an electrical conductor in a circuit.
 */
public class Conduction : MonoBehaviour
{
    /**
     * Whether a positive charge can pass through this conductor.
     */
    public bool positivePassThrough = false;

    /**
     * Whether a negative charge can pass through this conductor.
     */
    public bool negativePassThrough = false;

    /**
     * Whether the simulation is actively running.
     */
    public bool simulationActiveState = false;

    /**
     * Whether the loop is closed.
     */
    public bool loopIsClosed = false;

    /**
     * The voltage of the conductor.
     */
    public float voltage;

    /**
     * The current of the conductor.
     */
    public float current;

    /**
     * The resistance of the conductor.
     */
    public float resistance;

    /**
     * The local resistance of the conductor.
     */
    public float localResistance;

    /**
     * The number of positive connections in series.
     */
    public int positiveNumberInSeries;

    /**
     * The number of negative connections in series.
     */
    public int negativeNumberInSeries;

    /**
     * A reference to the Simulation object.
     */
    Simulation Simulation;

    /**
     * Initializes the Simulation variable.
     */
    private void Start()
    {
        Simulation = GameObject.Find("Simulation Event Handler").GetComponent<Simulation>();
    }

    /**
     * Returns whether the loop is closed.
     */
    public bool LoopIsClosed()
    {
        return loopIsClosed;
    }

    /**
     * Starts the electrical circuit when the simulation state is active.
     * Electrical circuit will pass all the properties it needs to mimic current, 
     * voltage, and resistance passing through an electrical component.
     */
    private void OnTriggerStay(Collider other)
    {
        // Sets the simulationActiveState variable based on the state of the simulation.
        simulationActiveState = Simulation.simulationActiveState;

        if (simulationActiveState == true)
        {
            ClosedLoopRoutine(other);
        }
        else if (simulationActiveState == false)
        {
            OpenLoopRoutine();
        }
    }

    /**
     * Initializes the variables when the loop is closed.
     */
    private void ClosedLoopRoutine(Collider other)
    {
        if (positivePassThrough == true && negativePassThrough == true)
        {
            loopIsClosed = true;
        }

        // The instance of the GameObject that is being collided with.
        GameObject otherObject = other.gameObject;

        // The red wire must touch the positive side of the power source.
        if (otherObject.tag == "Power_Source_Positive" && positiveNumberInSeries == 0)
        {
            positivePassThrough = true;
            positiveNumberInSeries += 1;

            voltage = otherObject.GetComponent<PowerSource>().getPowerSourceVoltage();
        }

        // The black wire must touch the negative side of the power source.
        if (otherObject.tag == "Power_Source_Negative" && negativeNumberInSeries == 0)
        {
            negativePassThrough = true;
            negativeNumberInSeries += 1;
        }

        if (otherObject.tag == "Power_Source_Negative" && current == 0)
        {
            current = otherObject.GetComponent<PowerSource>().getCurrent();
            //Debug.Log("getting battery current");
        }

        // The instance of the Conduction property of the "other" object.
        Conduction otherObjectConduction = otherObject.GetComponent<Conduction>();

        if (otherObjectConduction)
        {
            if(positiveNumberInSeries > otherObjectConduction.positiveNumberInSeries)
            {
                if(otherObjectConduction.voltage != 0 && voltage == 0)
                {
                    voltage = otherObjectConduction.voltage;
                }
                if (otherObjectConduction.resistance != 0 && resistance == 0)
                {
                    resistance = otherObjectConduction.resistance + localResistance;
                    //Debug.Log("getting resistance from resistor");
                }
                if (otherObjectConduction.resistance == 0 && resistance == 0 && localResistance != 0)
                {
                    resistance = localResistance;
                    //Debug.Log("Setting local resistance");
                }
            }

            if(negativeNumberInSeries > otherObjectConduction.negativeNumberInSeries)
            {
                if (otherObjectConduction.current != 0 && current == 0)
                {
                    current = otherObjectConduction.current;
                }
            }

            //Negative Check
            if (otherObjectConduction.negativePassThrough == true && negativePassThrough == false)
            {
                negativePassThrough = true;
                if (negativeNumberInSeries == 0 && otherObjectConduction.negativeNumberInSeries != 0)
                {
                    negativeNumberInSeries = otherObjectConduction.negativeNumberInSeries + 1;
                }
            }

            //Positive Check
            if (otherObjectConduction.positivePassThrough == true && positivePassThrough == false)
            {
                positivePassThrough = true;
                if (positiveNumberInSeries == 0 && otherObjectConduction.positiveNumberInSeries != 0)
                {
                    positiveNumberInSeries = otherObjectConduction.positiveNumberInSeries + 1;
                }
            }
        }
    }

    /**
     * Resets all variables when the loop is open.
     */
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
    }
}