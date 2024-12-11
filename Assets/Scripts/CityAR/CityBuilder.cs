using System;
using System.Linq;
using DefaultNamespace;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using System.Collections.Generic;
using System.Collections;

namespace CityAR
{
    public class CityBuilder : MonoBehaviour
    {
        public GameObject districtPrefab;
        public GameObject housePrefab;
        public PinchSlider slider;
        public TextMeshPro name;
        public ToolTip tooltip;
        private DataObject _dataObject;
        private GameObject _platform;
        private Data _data;
        private int metric = 0;

        private void Start()
        {
            _platform = GameObject.Find("Platform");
            _data = _platform.GetComponent<Data>();
            _dataObject = _data.ParseData();
            BuildCity(_dataObject);
        }

        private void BuildCity(DataObject p)
        {
            if (p.project.files.Count > 0)
            {
                p.project.w = 1;
                p.project.h = 1;
                p.project.deepth = 1;
                BuildDistrict(p.project, false);
            }
        }

        /*
         * entry: Single entry from the data set. This can be either a folder or a single file.
         * splitHorizontal: Specifies whether the subsequent children should be split horizontally or vertically along the parent
         */
        private void BuildDistrict(Entry entry, bool splitHorizontal)
        {
            if (!entry.type.Equals("File"))
            {
                float x = entry.x;
                float z = entry.z;

                float dirLocs = GetMetricValue(entry);
                entry.color = GetColorForDepth(entry.deepth);

                BuildDistrictBlock(entry, false);

                foreach (Entry subEntry in entry.files)
                {
                    subEntry.x = x;
                    subEntry.z = z;

                    if (subEntry.type.Equals("Dir"))
                    {
                        float ratio = GetMetricValue(subEntry) / dirLocs;
                        subEntry.deepth = entry.deepth + 1;

                        if (splitHorizontal)
                        {
                            subEntry.w = ratio * entry.w; // split along horizontal axis
                            subEntry.h = entry.h;
                            x += subEntry.w;
                        }
                        else
                        {
                            subEntry.w = entry.w;
                            subEntry.h = ratio * entry.h; // split along vertical axis
                            z += subEntry.h;
                        }
                    }
                    else
                    {
                        subEntry.parentEntry = entry;
                    }
                    BuildDistrict(subEntry, !splitHorizontal);
                }

                if (!splitHorizontal)
                {
                    entry.x = x;
                    entry.z = z;
                    if (ContainsDirs(entry))
                    {
                        entry.h = 1f - z;
                    }
                    entry.deepth += 1;
                    BuildDistrictBlock(entry, true);
                }
                else
                {
                    entry.x = -x;
                    entry.z = z;
                    if (ContainsDirs(entry))
                    {
                        entry.w = 1f - x;
                    }
                    entry.deepth += 1;
                    BuildDistrictBlock(entry, true);
                }
            }
        }

        /*
         * entry: Single entry from the data set. This can be either a folder or a single file.
         * isBase: If true, the entry has no further subfolders. Buildings must be placed on top of the entry
         */
        private void BuildDistrictBlock(Entry entry, bool isBase)
        {
            if (entry == null)
            {
                return;
            }

            float w = entry.w; // w -> x coordinate
            float h = entry.h; // h -> z coordinate


            entry.color = GetColorForDepth(entry.deepth);

            if (w * h > 0)
            {
                GameObject prefabInstance = Instantiate(districtPrefab, _platform.transform, true);

                if (!isBase)
                {
                    prefabInstance.name = entry.name;
                    prefabInstance.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = entry.color;
                    prefabInstance.transform.localScale = new Vector3(entry.w, 1f, entry.h);
                    prefabInstance.transform.localPosition = new Vector3(entry.x, entry.deepth, entry.z);
                }
                else
                {
                    prefabInstance.name = entry.name + "Base";
                    float s = 0.005f;
                    prefabInstance.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = entry.color;
                    prefabInstance.transform.GetChild(0).rotation = Quaternion.Euler(90, 0, 0);
                    prefabInstance.transform.localScale = new Vector3(entry.w, 1, entry.h);
                    prefabInstance.transform.localPosition = new Vector3(entry.x, entry.deepth + 0.001f, entry.z);


                    Vector3 scale = prefabInstance.transform.localScale;
                    float scaleX = scale.x - (entry.deepth * 0.005f);
                    float scaleZ = scale.z - (entry.deepth * 0.005f);
                    float shiftX = (scale.x - scaleX) / 2f;
                    float shiftZ = (scale.z - scaleZ) / 2f;
                    prefabInstance.transform.localScale = new Vector3(scaleX, scale.y, scaleZ);
                    Vector3 position = prefabInstance.transform.localPosition;
                    prefabInstance.transform.localPosition = new Vector3(position.x - shiftX, position.y, position.z + shiftZ);

                    float filesCount = entry.files.Count;
                    List<Entry> files = new List<Entry>();
                    for (int i = 0; i < filesCount; i++)
                    {
                        Entry file = entry.files[i];
                        if (file.type.Equals("File"))
                        {
                            files.Add(file);
                        }
                    }

                    filesCount = files.Count;
                    int gridSize = Mathf.CeilToInt(Mathf.Sqrt(filesCount));
                    float gridWidth = gridSize;
                    float gridHeight = gridSize;
                    for (int i = 0; i < filesCount; i++)
                    {
                        Entry file = files[i];
                        int row = i / gridSize;
                        int column = i % gridSize;
                        float xOffset = (row - (gridSize - 1) / 2.0f) * 0.02f / prefabInstance.transform.localScale.x;
                        float zOffset = (column - (gridSize - 1) / 2.0f) * 0.02f / prefabInstance.transform.localScale.z;

                        BuildHouse(file, entry, prefabInstance, s, xOffset, zOffset);
                    }
                }
            }
        }

        private void BuildHouse(Entry entry, Entry parentEntry, GameObject parent, float s, float xOffset, float zOffset)
        {
            Vector3 scale = ScaleToParent(parent);
            Vector3 newPosition = new Vector3(-0.5f, 0f, 0.5f) + new Vector3(xOffset, 0, zOffset);
            entry.position = newPosition;
            GameObject prefabInstance = Instantiate(housePrefab, parent.transform, true);

            float h = GetMetricValue(entry);
            var houseData = prefabInstance.GetComponent<houseData>();
            houseData.value = h;
            houseData.name = entry.name;
            houseData.nol = entry.numberOfLines;
            houseData.nom = entry.numberOfMethods;
            houseData.noac = entry.numberOfAbstractClasses;
            houseData.noi = entry.numberOfInterfaces;

            
            prefabInstance.GetComponent<interactWithHouse>().setData(tooltip, prefabInstance);

            prefabInstance.name = entry.name + "_Build_" + parentEntry.deepth;
            prefabInstance.transform.localScale += new Vector3(scale.x, h, scale.z);
            prefabInstance.transform.localPosition = new Vector3(entry.position.x, h / 2f + 1f, entry.position.z);
        }

        private Vector3 ScaleToParent(GameObject parent)
        {
            Vector3 size = new Vector3(0.01f, 0.01f, 0.01f);
            Transform currentTransform = parent.transform;

            while (currentTransform != null && currentTransform != _platform.transform)
            {
                size = new Vector3(
                    size.x / currentTransform.localScale.x,
                    size.y / currentTransform.localScale.y,
                    size.z / currentTransform.localScale.z
                );
                currentTransform = currentTransform.parent;
            }

            return size;
        }

        public void scaleHouse(GameObject gameObject, float value){
            if (gameObject == null) return;

            var houseData = gameObject.GetComponent<houseData>();
            if (houseData != null){
            
            Vector3 scale = gameObject.transform.localScale;
            Vector3 position = gameObject.transform.localPosition;
            float h = houseData.value;
            gameObject.transform.localScale = new Vector3(scale.x, h*value*2f, scale.z);
            gameObject.transform.localPosition = new Vector3(position.x, h*value + 1f, position.z);
            }
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                scaleHouse(gameObject.transform.GetChild(i).gameObject,value);
            }
        }

        public void changeScale(SliderEventData sliderData){
            if (sliderData != null) scaleHouse(_platform, sliderData.NewValue*2f);
            _platform.GetComponent<BoundsControl>().UpdateBounds();
            
        } 

        private bool ContainsDirs(Entry entry)
        {
            foreach (Entry e in entry.files)
            {
                if (e.type.Equals("Dir"))
                {
                    return true;
                }
            }

            return false;
        }

        private Color GetColorForDepth(int depth)
        {
            float normalizedDepth = Mathf.Clamp(depth / 15.0f, 0, 1);
            float blue = 1.0f;
            float green = (float)((1 - normalizedDepth));
            float red = 0.0f;

            return new Color(red, green, blue);
        }

        private float GetMetricValue(Entry entry){
            if (metric == 0){
                name.text = "Number of Lines";
                return entry.numberOfLines;
            }
            if (metric == 1){
                name.text = "Number of Methods";
                return entry.numberOfMethods;
            }
            if (metric == 2){
                name.text = "Number of AbstractClasses";
                return entry.numberOfAbstractClasses;
            }
            if (metric == 3){
                name.text = "Number of Interfaces";
                return entry.numberOfInterfaces;
            }
            return 0f;
        }

        public void nextMetrcValue(){
            setMetricValue(metric+1);
        }

        public void prevMetrcValue(){
            setMetricValue(metric-1);
        }

        public void setMetricValue(int metricValue)
        {
            metric = (metricValue + 4 ) % 4;
            var boundsControl = _platform.GetComponent<BoundsControl>();
            boundsControl.enabled = false;
            var temp = _platform.transform.localRotation;
            slider.SliderValue = 0.5f;
            for (int i = 1; i < _platform.transform.childCount; i++)
            {
                Destroy(_platform.transform.GetChild(i).gameObject);
            }
            _platform.transform.localRotation = Quaternion.Euler(0f,0f,0f);
            BuildCity(_data.ParseData());
            _platform.transform.localRotation = temp;
            boundsControl.enabled = true;
            StartCoroutine(UpdateBoundsAfterDelay());
        }

        private IEnumerator UpdateBoundsAfterDelay()
    {
        yield return new WaitForSeconds(0.001f);

        _platform.GetComponent<BoundsControl>().UpdateBounds();
    }
    }
}