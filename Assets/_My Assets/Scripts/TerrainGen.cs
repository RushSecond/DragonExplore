using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TerrainGen : MonoBehaviour {

    [Tooltip("All the possible terrain variations that this can create")]
    public GameObject[] prefList_PossibleTerrains;

    [Tooltip("All the possible moutain variations that this can create")]
    public GameObject[] prefList_PossibleMountains;

    [Header("Global vars")]
    [Tooltip("How many tiles wide (x) the terrain is")]
    public int f_Width;
    [Tooltip("How many tiles long (z) the terrain is")]
    public int f_Length;
    [Tooltip("Source dimensions of each tile")]
    public float f_SourceTileSize;
    [Tooltip("Target dimensions of each tile, to scale them")]
    public float f_TargetTileSize;
    [Tooltip("Mountain Scaling")]
    public float f_MountainScale;
    [Tooltip("Perlin noise height range")]
    public float f_heightRange;
    [Tooltip("Size of perlion noise height chunks")]
    public float f_heightRangeSize;
    [Tooltip("Perlin noise offset (so it's a different pattern from height)")]
    public float f_shiftOffset;
    [Tooltip("Perlin noise shifting x and z")]
    public float f_shiftRange;
    [Tooltip("Size of perlion noise shift chunks")]
    public float f_shiftRangeSize;

    float[,] fList_randHeights;

    Vector3 startPoint;
    int randomIndex;
    GameObject chosenTerrain;
    GameObject newTerrain;
    Transform child;

    Mesh mesh;
    MeshCollider m_collider;

    Vector3[] verts;

    // Use this for initialization
    void Start () {
        CreateTerrain();
        CreateMountains();
	}

    void CreateTerrain()
    {
        startPoint = new Vector3(f_Width * -f_TargetTileSize / 2, 0, f_Length * -f_TargetTileSize / 2);
        Vector3 scaling = new Vector3(f_TargetTileSize / f_SourceTileSize, f_TargetTileSize / f_SourceTileSize, f_TargetTileSize / f_SourceTileSize);

        for (int x = 0; x < f_Width; x++)
        {
            for (int z = 0; z < f_Length; z++)
            {
                randomIndex = Mathf.FloorToInt(Random.Range(0f, prefList_PossibleTerrains.Length));
                chosenTerrain = prefList_PossibleTerrains[randomIndex];
                newTerrain = GameObject.Instantiate(chosenTerrain, this.transform);
                newTerrain.transform.position = startPoint + (Vector3.forward * z * f_TargetTileSize) + (Vector3.right * x * f_TargetTileSize);
                newTerrain.transform.localScale = scaling;

                SetLayersAndShifting(newTerrain, f_TargetTileSize / f_SourceTileSize);
            }
        }
    }

    void CreateMountains()
    {
        randomIndex = Mathf.FloorToInt(Random.Range(0f, prefList_PossibleMountains.Length));
        chosenTerrain = prefList_PossibleMountains[randomIndex];
        newTerrain = GameObject.Instantiate(chosenTerrain, this.transform);
        newTerrain.transform.position = new Vector3(f_Width * -f_TargetTileSize / 4, 0, f_Length * -f_TargetTileSize / 4); ;
        newTerrain.transform.localScale = new Vector3(f_MountainScale, f_MountainScale, f_MountainScale);

        SetLayersAndShifting(newTerrain, f_MountainScale);
    }

    void SetLayersAndShifting(GameObject terrain, float scale)
    {
        terrain.layer = LayerMask.NameToLayer("Terrain");
        float xPosition = terrain.transform.position.x - startPoint.x;
        float zPosition = terrain.transform.position.z - startPoint.z;

        float[,] perlinX = new float[2, 2];
        float[,] perlinY = new float[2, 2];
        float[,] perlinZ = new float[2, 2];
        

        float newX, newY, newZ;

        for (int x = 0; x < 2; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                perlinX[x, z] = Mathf.PerlinNoise((xPosition + x * f_TargetTileSize) / f_shiftRangeSize,
                    (zPosition + z * f_TargetTileSize) / f_shiftRangeSize);
                perlinY[x, z] = Mathf.PerlinNoise(f_shiftOffset + (xPosition + x * f_TargetTileSize) / f_heightRangeSize,
                    f_shiftOffset + (zPosition + z * f_TargetTileSize) / f_heightRangeSize);
                perlinZ[x, z] = Mathf.PerlinNoise(2* f_shiftOffset + (xPosition + x * f_TargetTileSize) / f_shiftRangeSize, 
                    2 * f_shiftOffset + (zPosition + z * f_TargetTileSize) / f_shiftRangeSize);
            }
        }

        float distanceFromEdge;

#if (YNITY_EDITOR)
        // LOD stuff
        LODGroup lodGroup = terrain.GetComponent<LODGroup>();
        if (lodGroup != null)
        {
            lodGroup.fadeMode = LODFadeMode.None;
            SerializedObject obj = new SerializedObject(lodGroup);

            SerializedProperty valArrProp = obj.FindProperty("m_LODs.Array");
            for (int i = 0; valArrProp.arraySize > i; i++)
            {
                SerializedProperty sHeight = obj.FindProperty("m_LODs.Array.data[" + i.ToString() + "].screenRelativeHeight");

                if (i == 0)
                {
                    sHeight.doubleValue = 0.25;
                }
                if (i == 1)
                {
                    sHeight.doubleValue = 0.18;
                }
                if (i == 2)
                {
                    sHeight.doubleValue = 0.14;
                }
            }
            obj.ApplyModifiedProperties();
        }
#endif

        for (int i = 0; i < terrain.transform.childCount; i++)
        {
            child = terrain.transform.GetChild(i);



            child.gameObject.layer = LayerMask.NameToLayer("Terrain");

            // ****Vertex modding to shift stuff around****
            mesh = child.GetComponent<MeshFilter>().mesh;
            verts = mesh.vertices;
            for (int j = 0; j < verts.Length; j++)
            {
                newX = f_shiftRange / scale * Mathf.Lerp(
                    Mathf.Lerp(perlinX[0, 0], perlinX[1, 0], verts[j].x / f_SourceTileSize),
                    Mathf.Lerp(perlinX[0, 1], perlinX[1, 1], verts[j].x / f_SourceTileSize),
                    verts[j].z / f_SourceTileSize);

                newY = f_heightRange / scale * Mathf.Lerp(
                    Mathf.Lerp(perlinY[0,0], perlinY[1,0], verts[j].x / f_SourceTileSize),
                    Mathf.Lerp(perlinY[0, 1], perlinY[1, 1], verts[j].x / f_SourceTileSize),
                    verts[j].z / f_SourceTileSize);

                newZ = f_shiftRange / scale * Mathf.Lerp(
                    Mathf.Lerp(perlinZ[0, 0], perlinZ[1, 0], verts[j].x / f_SourceTileSize),
                    Mathf.Lerp(perlinZ[0, 1], perlinZ[1, 1], verts[j].x / f_SourceTileSize),
                    verts[j].z / f_SourceTileSize);

                distanceFromEdge = 1f - 2f * Mathf.Max(
                    Mathf.Abs(verts[j].x / f_SourceTileSize - 0.5f),
                    Mathf.Abs(verts[j].z / f_SourceTileSize - 0.5f));

                /*newX = Mathf.Lerp(newX,
                    f_shiftRange / scale * Mathf.PerlinNoise((xPosition + verts[j].x * scale) / f_shiftRangeSize,
                    (zPosition + verts[j].z * scale) / f_shiftRangeSize),
                    distanceFromEdge);

                newY = Mathf.Lerp(newY, 
                    f_heightRange / scale * Mathf.PerlinNoise(f_shiftOffset + (xPosition + verts[j].x * scale) / f_heightRangeSize,
                    f_shiftOffset + (zPosition + verts[j].z * scale) / f_heightRangeSize),
                    distanceFromEdge);

                newZ = Mathf.Lerp(newZ,
                    f_shiftRange / scale * Mathf.PerlinNoise(2 * f_shiftOffset + (xPosition + verts[j].x * scale) / f_shiftRangeSize,
                    2 * f_shiftOffset + (zPosition + verts[j].z * scale) / f_shiftRangeSize),
                    distanceFromEdge);

                /*newX = 
                    f_shiftRange / scale * Mathf.PerlinNoise((xPosition + verts[j].x * scale) / f_shiftRangeSize,
                    (zPosition + verts[j].z * scale) / f_shiftRangeSize);

                newY = 
                    f_heightRange / scale * Mathf.PerlinNoise(f_shiftOffset + (xPosition + verts[j].x * scale) / f_heightRangeSize,
                    f_shiftOffset + (zPosition + verts[j].z * scale) / f_heightRangeSize);

                newZ =
                    f_shiftRange / scale * Mathf.PerlinNoise(2 * f_shiftOffset + (xPosition + verts[j].x * scale) / f_shiftRangeSize,
                    2 * f_shiftOffset + (zPosition + verts[j].z * scale) / f_shiftRangeSize);*/

                verts[j].x -= newX;
                verts[j].y -= newY;
                verts[j].z -= newZ;
            }

            mesh.vertices = verts;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            m_collider = child.GetComponent<MeshCollider>();
            m_collider.sharedMesh = mesh;

        }
    }

    void RandomizeHeight()
    {
        fList_randHeights = new float[f_Width+1, f_Length+1];
        for (int x = 0; x < f_Width+1; x++)
        {
            for (int z = 0; z < f_Length+1; z++)
            {
                fList_randHeights[x, z] = Mathf.PerlinNoise((float)x / (f_Width + 1), (float)z/ (f_Length + 1)) * 500f - 100f;
            }
        }
    }



	// Update is called once per frame
	void Update () {
		
	}
}
