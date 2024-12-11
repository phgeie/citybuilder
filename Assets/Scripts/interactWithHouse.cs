using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class interactWithHouse : MonoBehaviour, IMixedRealityTouchHandler
{

    private ToolTip tooltip;
    private GameObject house;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void setData(ToolTip tooltip, GameObject house)
    {
        this.tooltip = tooltip;
        this.house = house;
    }

    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        Debug.Log("1");
        tooltip.gameObject.SetActive(true);
        var houseData = house.GetComponent<houseData>();
        tooltip.ToolTipText = houseData.name + ":\n Number of Lines: " + houseData.nol + ":\n Number of Methods: " + houseData.nom + ":\n Number of Abstract Classes: " + houseData.noac + ":\n Number of Iterfaces: " + houseData.noi;
        gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
    }

    public void OnTouchCompleted(HandTrackingInputEventData eventData)
    {
        Debug.Log("2");
        tooltip.gameObject.SetActive(false);
        tooltip.ToolTipText = "";
        gameObject.GetComponent<MeshRenderer>().material.color = Color.white;
    }

    public void OnTouchUpdated(HandTrackingInputEventData eventData){
        Debug.Log("3");}
}
