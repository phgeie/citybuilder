using System;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class interactWithHouse : MonoBehaviour, IMixedRealityTouchHandler
{
    private ToolTip tooltip;
    private GameObject house;
    
    public void setData(ToolTip tooltip, GameObject house)
    {
        this.tooltip = tooltip;
        this.house = house;
    }

    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        tooltip.gameObject.SetActive(true);
        var houseData = house.GetComponent<houseData>();
        tooltip.ToolTipText = houseData.name + ":\n       Number of Lines: " + houseData.nol + ",\n       Number of Methods: " + houseData.nom + ",\n       Number of Abstract Classes: " + houseData.noac + ",\n       Number of Iterfaces: " + houseData.noi;
        house.GetComponent<MeshRenderer>().material.color = Color.blue;
    }

    public void OnTouchCompleted(HandTrackingInputEventData eventData)
    {
        tooltip.gameObject.SetActive(false);
        house.GetComponent<MeshRenderer>().material.color = Color.white;
    }

    public void OnTouchUpdated(HandTrackingInputEventData eventData){}
}