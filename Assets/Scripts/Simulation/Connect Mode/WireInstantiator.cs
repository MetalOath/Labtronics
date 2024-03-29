﻿// <header>
// File Name : WireInstantiator.cs
// Purpose :  This script carries the methods used to spawn a wire between any two connection points in the scene. 
// Created By  : Othman Arnaout   (arnaout.othman@gmail.com)
// Created On  : 2021/02/21
// Modified By : Othman Arnaout
// Modified On : 2021/12/19
// Modification Note : Comment Cleanup
// Other Notes :
// </header>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WireInstantiator : MonoBehaviour
{
    #region Static and Private variables/Fields/Properties

    private Transform 
        connectionPointOne, 
        connectionPointTwo, 
        wireContainerTransform;

    private GameObject 
        elementPrefab;

    private float 
        wirePrefabLength, 
        elementPrefabLength, 
        wireSegmentLength, 
        wireLength, 
        distanceBetweenPoints, 
        yFunction, 
        resistance;

    private int 
        numberOfSegments;

    SimulationMethods 
        Simulation;

    #endregion



    #region Public/Inspector variables

    [SerializeField] private GameObject 
        wirePrefab, 
        leadSegmentPrefab, 
        wireSegmentPrefab, 
        wireContainer, 
        wirePromptTMP, 
        cancelSpawn, 
        cancelSpawnZoomed;

    [SerializeField] public Material 
        redCPMat, 
        greenCPMat, 
        blueCPMat, 
        redWireMat;

    public bool 
        leadSpawnPhase = false;

    #endregion



    #region Private methods - Unity methods

    private void Start()
    {
        // Initialize Simulation Manager Script Reference
        Simulation = GameObject.Find("Simulation Event Handler").GetComponent<SimulationMethods>();
    }

    #endregion



    #region Private methods
    private void GetPrefabLength()
    {
        GameObject wirePrefabSegmentMeasurementInstance;
        GameObject elementPrefabMeasurementInstance;

        if (leadSpawnPhase)
        {
            wirePrefabSegmentMeasurementInstance = Instantiate(leadSegmentPrefab, Vector3.zero, Quaternion.identity);
            elementPrefabMeasurementInstance = Instantiate(elementPrefab, Vector3.zero, Quaternion.identity);
            wirePrefabLength = wirePrefabSegmentMeasurementInstance.GetComponent<Collider>().bounds.size.y;
            elementPrefabLength = elementPrefabMeasurementInstance.GetComponentInChildren<Collider>().bounds.size.y;
            Destroy(wirePrefabSegmentMeasurementInstance);
            Destroy(elementPrefabMeasurementInstance);
        }
        else
        {
            wirePrefabSegmentMeasurementInstance = Instantiate(wireSegmentPrefab, Vector3.zero, Quaternion.identity);
            wirePrefabLength = wirePrefabSegmentMeasurementInstance.GetComponent<Collider>().bounds.size.y;
            Destroy(wirePrefabSegmentMeasurementInstance);
        }
    }

    private void SpawnWire(Transform pointOne, Transform pointTwo)
    {
        GetPrefabLength();

        wireSegmentLength = Mathf.Abs(wirePrefabLength * 0.8f);

        distanceBetweenPoints = Vector3.Magnitude(pointTwo.position - pointOne.position);

        if (leadSpawnPhase)
            wireLength = Mathf.PI * distanceBetweenPoints / 3f + elementPrefabLength; // use pi*d/2 + Element length
        else
            wireLength = Mathf.PI * distanceBetweenPoints / 2f; // use pi*d/2

        numberOfSegments = (int)(wireLength / wireSegmentLength) + 1;

        GameObject wireInstance = Instantiate(wirePrefab, pointOne.position + (pointTwo.position - pointOne.position) / 2, Quaternion.identity);

        if (leadSpawnPhase)
        {
            wireInstance.name = elementPrefab.name;
            wireInstance.GetComponent<Conduction>().localResistance = resistance;
            wireInstance.transform.parent = wireContainer.transform.parent;
        }
        else
        {
            wireInstance.name = "Wire " + (int)Time.time;
            wireInstance.transform.parent = wireContainerTransform;
        }

        GameObject previousSegment = null;
        Quaternion pointTwoInvertedRotation = new Quaternion(pointTwo.rotation.x, pointTwo.rotation.y, pointTwo.rotation.z + 180f, pointTwo.rotation.w);

        for (int i = 0; i <= numberOfSegments; i++)
        {
            yFunction = Mathf.Sqrt(Mathf.Pow(distanceBetweenPoints / 2f, 2f) - Mathf.Pow((i * distanceBetweenPoints / numberOfSegments - distanceBetweenPoints / 2f), 2f));

            if (leadSpawnPhase)
                yFunction = yFunction / 3;

            Vector3 yVector = new Vector3(0f, yFunction, 0f);
            Vector3 pathToPointTwo = (pointTwo.position - pointOne.position) / numberOfSegments * i;
            Vector3 midPoint = pointOne.position + (pointTwo.position - pointOne.position) / 2f;

            Vector3 createPosition = pointOne.position + yVector + pathToPointTwo;
            Quaternion createRotation = Quaternion.FromToRotation(pointTwo.position - pointOne.position, midPoint - createPosition);

            GameObject currentSegment;

            if (leadSpawnPhase && i == numberOfSegments / 2)
            {
                currentSegment = Instantiate(elementPrefab, createPosition, createRotation);
            }
            else if (leadSpawnPhase && i != numberOfSegments / 2)
                currentSegment = Instantiate(leadSegmentPrefab, createPosition, createRotation);
            else
                currentSegment = Instantiate(wireSegmentPrefab, createPosition, createRotation);

            currentSegment.transform.parent = wireInstance.transform;

            if (i == 0)
            {
                currentSegment.GetComponent<Rigidbody>().isKinematic = true;
                previousSegment = currentSegment;
                currentSegment.transform.position = pointOne.position;
                currentSegment.transform.rotation = pointOne.rotation;

                CapsuleCollider wireStartCollider = wireInstance.AddComponent<CapsuleCollider>();
                wireStartCollider.isTrigger = true;
                wireStartCollider.center = currentSegment.transform.localPosition;
                wireStartCollider.radius = currentSegment.GetComponent<Collider>().bounds.size.x;
                wireStartCollider.height = currentSegment.GetComponent<Collider>().bounds.size.y;
            }

            if (i != 0)
            {
                if (leadSpawnPhase && i == numberOfSegments / 2)
                    currentSegment.GetComponentInChildren<ConfigurableJoint>().connectedBody = previousSegment.GetComponent<Rigidbody>();
                else if (leadSpawnPhase && i == numberOfSegments / 2 + 1)
                {
                    currentSegment.GetComponent<ConfigurableJoint>().connectedBody = previousSegment.GetComponentInChildren<Rigidbody>();
                    if (elementPrefab.name == "Resistor - 330 Ohm" || elementPrefab.name == "Resistor - 470 Ohm" || elementPrefab.name == "Resistor - 560 Ohm")
                        currentSegment.GetComponent<ConfigurableJoint>().connectedAnchor = new Vector3(0, 0, elementPrefabLength * 0.95f - elementPrefab.transform.Find("Model").localPosition.y);
                    if (elementPrefab.name == "LED Light - Red" || elementPrefab.name == "LED Light - Green" || elementPrefab.name == "LED Light - Blue")
                        currentSegment.GetComponent<ConfigurableJoint>().connectedAnchor = new Vector3(-0.00026f, -0.00589f, -0.0021f);
                    //currentSegment.GetComponent<ConfigurableJoint>().connectedAnchor = previousSegment.transform.Find("ConnectedAnchor").localPosition;
                }
                else
                    currentSegment.GetComponent<ConfigurableJoint>().connectedBody = previousSegment.GetComponent<Rigidbody>();

                previousSegment = currentSegment;
            }

            if (i == numberOfSegments)
            {
                currentSegment.GetComponent<Rigidbody>().isKinematic = true;
                currentSegment.transform.position = pointTwo.position;
                currentSegment.transform.rotation = pointTwoInvertedRotation;

                CapsuleCollider wireEndCollider = wireInstance.AddComponent<CapsuleCollider>();
                wireEndCollider.isTrigger = true;
                wireEndCollider.center = currentSegment.transform.localPosition;
                wireEndCollider.radius = currentSegment.GetComponent<Collider>().bounds.size.x;
                wireEndCollider.height = currentSegment.GetComponent<Collider>().bounds.size.y;
            }
        }
    }
    //public void Resize(GameObject objectToResize, float newSize)
    //{
    //    float size = objectToResize.GetComponentInChildren<Collider>().bounds.size.y;
    //    Vector3 rescale = objectToResize.transform.localScale;
    //    rescale.y = newSize * rescale.y / size;
    //    objectToResize.transform.localScale = rescale;
    //}
    #endregion



    #region Public methods
    
    /* This function executes when the player enters connect mode, and initiates the wire spawn phase unpon the player's
     * selection of the first connection point.
     */
    public void WireSpawnPhaseInitiator()
    {
        //wireContainerTransform = wireContainer.transform;

        // Start by casting a ray on the clicked object.
        Ray raycast = Simulation.SingleRayCastByPlatform();

        RaycastHit[] raycastHits = Physics.RaycastAll(raycast, 100f);

        // Filter out objects hit by the raycast.
        for (int i = 0; i < raycastHits.Length; i++)
        {
            // If an object with the Connection_Points tag is found, proceed.
            if (raycastHits[i].transform.gameObject.CompareTag("Connection_Points"))
            {
                RaycastHit raycastHit = raycastHits[i];
                // Check spawn point availablility status by checking its material color.
                if (raycastHit.transform.gameObject.GetComponent<MeshRenderer>().material.color == redCPMat.color)
                {
                    // If not in Wire Spawn Phase, proceed.
                    if (Simulation.inWireSpawnPhase == false)
                    {
                        raycastHit.transform.gameObject.GetComponent<MeshRenderer>().material = greenCPMat;
                        connectionPointOne = raycastHit.transform;
                        Simulation.inWireSpawnPhase = true;
                        wirePromptTMP.GetComponent<TextMeshProUGUI>().text = "Select 2nd Connection Point";
                        wirePromptTMP.GetComponent<TextMeshProUGUI>().color = Color.red;
                        cancelSpawn.SetActive(true);
                        cancelSpawnZoomed.SetActive(true);
                    }
                    // Otherwise, initiate second point selection phase.
                    else if (Simulation.inWireSpawnPhase == true && raycastHit.transform != connectionPointOne)
                    {
                        connectionPointTwo = raycastHit.transform;
                        SpawnWire(connectionPointOne, connectionPointTwo);
                        Simulation.inWireSpawnPhase = false;
                        connectionPointOne.gameObject.GetComponent<MeshRenderer>().material = blueCPMat;
                        connectionPointTwo.gameObject.GetComponent<MeshRenderer>().material = blueCPMat;
                        wirePromptTMP.GetComponent<TextMeshProUGUI>().text = "Select 1st Connection Point";
                        wirePromptTMP.GetComponent<TextMeshProUGUI>().color = Color.white;
                        cancelSpawn.SetActive(false);
                        cancelSpawnZoomed.SetActive(false);

                        // Checking if component wire or normal wire is beig spawned
                        if (leadSpawnPhase)
                        {
                            leadSpawnPhase = false;
                            Simulation.UIEventPublisher.ConnectModeUI();
                            Simulation.UIEventPublisher.EditModeUI();
                        }
                    }
                    break;
                }
            }
        }
    }

    // Component Wire Spawn Phase Initiator Method
    public void LeadSpawnPhaseInitiator(GameObject element, float resistanceValue)
    {
        leadSpawnPhase = true;
        elementPrefab = element;
        resistance = resistanceValue;
    }

    // Wire Color Setter Method
    public void SetWireColor([SerializeField] Material wireMaterial)
    {
        wireSegmentPrefab.GetComponent<MeshRenderer>().material = wireMaterial;
    }

    // End Wire Spawn Phase Method
    public void BreakWireSpawnPhase()
    {
        if (Simulation.inWireSpawnPhase == true)
        {
            connectionPointOne.gameObject.GetComponent<MeshRenderer>().material = redCPMat;
            Simulation.inWireSpawnPhase = false;
            wirePromptTMP.GetComponent<TextMeshProUGUI>().text = "Select 1st Connection Point";
            wirePromptTMP.GetComponent<TextMeshProUGUI>().color = Color.white;
            cancelSpawn.SetActive(false);
            cancelSpawnZoomed.SetActive(false);

            if (leadSpawnPhase)
            {
                leadSpawnPhase = false;
                Simulation.UIEventPublisher.ConnectModeUI();
                Simulation.UIEventPublisher.EditModeUI();
            }
        }
    }
    #endregion



    #region UI Button Delegates

    #endregion



    #region Public properties

    #endregion



    
}