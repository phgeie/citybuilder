using DefaultNamespace;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using TMPro;

// using https://assetstore.unity.com/packages/2d/textures-materials/diverse-space-skybox-11044 skybox

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

                    float filesCount = entry.files.Count;
                    List<Entry> files = entry.files;
                    int gridSize = Mathf.CeilToInt(Mathf.Sqrt(filesCount));
                    for (int i = 0; i < filesCount; i++)
                    {
                        Entry file = files[i];
                        int row = i / gridSize;
                        int column = i % gridSize;
                        float xOffset = (row - (gridSize - 1) / 2.0f) * 0.03f / prefabInstance.transform.localScale.x;
                        float zOffset = (column - (gridSize - 1) / 2.0f) * 0.03f / prefabInstance.transform.localScale.z;

                        BuildHouse(file, entry, prefabInstance, s, xOffset, zOffset);
                    }
                }
                
                Vector3 scale = prefabInstance.transform.localScale;
                float scaleX = scale.x - (entry.deepth * 0.005f);
                float scaleZ = scale.z - (entry.deepth * 0.005f);
                float shiftX = (scale.x - scaleX) / 2f;
                float shiftZ = (scale.z - scaleZ) / 2f;
                prefabInstance.transform.localScale = new Vector3(scaleX, scale.y, scaleZ);
                Vector3 position = prefabInstance.transform.localPosition;
                prefabInstance.transform.localPosition = new Vector3(position.x - shiftX, position.y, position.z + shiftZ);
            }
        }

        private void BuildHouse(Entry entry, Entry parentEntry, GameObject parent, float s, float xOffset, float zOffset){
            GameObject prefabInstance = Instantiate(housePrefab, parent.transform, true);
            prefabInstance.GetComponent<interactWithHouse>().setData(tooltip, prefabInstance);

            float height = GetMetricValue(entry);
            var houseData = prefabInstance.GetComponent<houseData>();
            houseData.value = height;
            houseData.name = entry.name;
            houseData.nol = entry.numberOfLines;
            houseData.nom = entry.numberOfMethods;
            houseData.noac = entry.numberOfAbstractClasses;
            houseData.noi = entry.numberOfInterfaces;

            prefabInstance.name = entry.name;
            Vector3 scale = new Vector3(0.015f / parent.transform.localScale.x, 0.015f / parent.transform.localScale.y, 0.015f / parent.transform.localScale.z);
            prefabInstance.transform.localScale += new Vector3(scale.x, height, scale.z);

            Vector3 newPosition = new Vector3(-0.5f, 0f, 0.5f) + new Vector3(xOffset, 0, zOffset);
            prefabInstance.transform.localPosition = new Vector3(newPosition.x, height / 2f + 1f, newPosition.z);
        }

        public void ChangeScale(SliderEventData sliderData){
            if (sliderData != null) ScaleHouse(_platform, sliderData.NewValue*10f);
            _platform.GetComponent<BoundsControl>().UpdateBounds();
        } 

        public void ScaleHouse(GameObject house, float value){
            var houseData = house.GetComponent<houseData>();

            if (houseData != null){
                Vector3 scale = house.transform.localScale;
                Vector3 position = house.transform.localPosition;
                float height = houseData.value;
                house.transform.localScale = new Vector3(scale.x, height*value, scale.z);
                house.transform.localPosition = new Vector3(position.x, height*value/2f + 1f, position.z);
            }

            for (int i = 0; i < house.transform.childCount; i++){
                ScaleHouse(house.transform.GetChild(i).gameObject, value);
            }
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

        private Color GetColorForDepth(int depth){
            float normalizedDepth = Mathf.Clamp(depth / 7.5f, 0, 1);
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

        public void NextMetrcValue(){
            SetMetricValue(metric+1);
        }

        public void PrevMetrcValue(){
            SetMetricValue(metric-1);
        }

        public void SetMetricValue(int metricValue){
            metric = (metricValue + 4) % 4;
            UpdateHouses();
        }

        public void UpdateHouses(){
            for (int i = 1; i < _platform.transform.childCount; i++){
                Destroy(_platform.transform.GetChild(i).gameObject);
            }

            var boundsControl = _platform.GetComponent<BoundsControl>();
            boundsControl.enabled = false;
            var prevRotation = _platform.transform.rotation;
            _platform.transform.rotation = Quaternion.Euler(0,0,0);
            _dataObject = _data.ParseData();
            BuildCity(_dataObject);
            _platform.transform.rotation = prevRotation;
            boundsControl.enabled = true;

            slider.SliderValue = 0.1f;
            StartCoroutine(UpdateBoundsAfterDelay());
        }

        private IEnumerator UpdateBoundsAfterDelay(){
            yield return new WaitForSeconds(0.001f);

            _platform.GetComponent<BoundsControl>().UpdateBounds();
        }
    }
}