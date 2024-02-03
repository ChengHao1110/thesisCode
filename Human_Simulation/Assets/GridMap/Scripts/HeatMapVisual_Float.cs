/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatMapVisual_Float : MonoBehaviour {

    private Grid_Float grid;
    private Mesh mesh;
    private bool updateMesh;

    private void Awake() {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void SetGrid(Grid_Float grid) {
        this.grid = grid;
        UpdateHeatMapVisual();

        grid.OnGridValueChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid_Float.OnGridValueChangedEventArgs e) {
        //UpdateHeatMapVisual();
        updateMesh = true;
    }

    private void LateUpdate() {
        if (updateMesh) {
            updateMesh = false;
            UpdateHeatMapVisual();
        }
    }

    private void UpdateHeatMapVisual() {
        MeshUtils.CreateEmptyMeshArrays(grid.GetWidth() * grid.GetHeight(), out Vector3[] vertices, out Vector2[] uv, out int[] triangles);

        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                int index = x * grid.GetHeight() + y;
                Vector3 quadSize = new Vector3(1, 1) * grid.GetCellSize();

                float gridValue = grid.GetValue(x, y);
                float gridValueNormalized = 1.0f - gridValue / grid.HEAT_MAP_MAX_VALUE;
                /*
                if(dynamicSystem.instance.heatmapFilename.Contains("stay")){
                    if(gridValue < 0.02f)
                    {
                        gridValueNormalized = 1.0f;
                    }
                    else
                    {
                        gridValueNormalized = gridValue / grid.HEAT_MAP_MAX_VALUE;
                    }
                    
                }
                */
                if (gridValueNormalized < 0.02f) gridValueNormalized = 0.02f;
                Vector2 gridValueUV = new Vector2(gridValueNormalized, 0f);
                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, grid.GetWorldPosition(x, y) + quadSize * .5f, 0f, quadSize, gridValueUV, gridValueUV);
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }

}
