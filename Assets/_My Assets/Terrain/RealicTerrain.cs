using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

public class RealicTerrain : ChunkManager
{
#if (UNITY_EDITOR)
    [CustomEditor(typeof(RealicTerrain))]
    public class TerrainBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RealicTerrain myScript = (RealicTerrain)target;

            if (GUILayout.Button("Redraw Terrain"))
            {
                myScript.RedrawTerrain(false);
            }
            if (GUILayout.Button("Redraw Terrain with Max LOD (Might take a while)"))
            {
                myScript.RedrawTerrain(true);
            }
        }
    }
#endif

    [Header("Global vars")]
    [Tooltip("Camera distance to generate new chunks")]
    public float f_CreateRange = 4000f;
    float f_DestroyRange;
    public float[] f_LODRanges = { 3500f, 8000f };
    const int i_numberofLODs = 3;
    public float f_TerrainCheckTime = 0.5f;

    public float f_TargetTileSize = 100f;
    public bool b_waterOn = true;
    bool b_waterOnLastFrame;

    /*[Tooltip("How many tiles wide (x) the terrain is")]
    public int f_Width;
    [Tooltip("How many tiles long (z) the terrain is")]
    public int f_Length;*/
    public float f_SourceTileSize = 10f;

    [Header("1st pass: General Biome Variance")]
    public bool b_do1stPass = true;
    public float f_1stPassHeight = 20f;
    public float f_1stPassRange = 200f;

    [Header("Lake pass: Creating lakes")]
    public bool b_doLakePass = true;
    public float f_lakeWaterHeight = -800f;
    public float f_lakeDepth = 300f;
    public float f_lakePassRange = 300f;
    public float f_lakePassFloor = 0.2f;

    [Header("2nd pass: Hills and valleys")]
    public bool b_do2ndPass = true;
    public float f_2ndPassHeight = 50f;
    public float f_2ndPassFloor = 20f;
    public float f_2ndPassRange = 100f;

    [Header("3rd pass: Hill Bumpiness")]
    public bool b_do3rdPass = true;
    public float f_3rdPassHeight = 50f;
    public float f_3rdPassFloor = 20f;
    public float f_3rdPassRange = 100f;

    [Header("4th pass: Terrain uneveness")]
    public bool b_do4thPass = true;
    public float f_4thPassHeight = 50f;
    public float f_4thPassRange = 100f;

    float[] rand;
    float f_chunkScaling;

    float maxHeight = -20000f, minHeight = 20000f;
    public GameObject TerrainPrefab;
    [FormerlySerializedAs("RockPrefab")]
    public GameObject SheepPrefab;

    Camera mainCam;
    HashSet<Chunk> EdgeChunks;
    HashSet<Chunk> AddEdgeChunks;
    HashSet<Chunk> RemoveEdgeChunks;

    HashSet<Chunk>[] LOD_EdgeChunks;
    HashSet<Chunk>[] LOD_AddEdgeChunks;
    HashSet<Chunk>[] LOD_RemoveEdgeChunks;

    protected Hashtable AllChunks;

    protected class Chunk
    {
        public GameObject data;
        public int x;
        public int z;

        public bool[] b_hasLODMeshes;

        public Chunk(GameObject e, int xpos, int zpos)
        {
            data = e; x = xpos; z = zpos;
            b_hasLODMeshes = new bool[i_numberofLODs];
        }

        /*public bool hasLODMesh(int LOD)
        {
            if (LOD < 0 || LOD >= i_numberofLODs)
                return false;
            return LODMeshes[LOD];
        }*/

        public string Hash()
        {
            return HashFromCoords(x, z);
        }

        public static string HashFromCoords(int x, int z)
        {
            string ret = x.ToString() + "," + z.ToString();
            return ret;
        }
    }

    // Start is called before the update
    void Start()
    {
        f_chunkScaling = f_TargetTileSize / f_SourceTileSize;
        mainCam = Camera.main;
        f_DestroyRange = f_CreateRange + f_TargetTileSize + 100f;
        b_waterOnLastFrame = b_waterOn;

        this.transform.position = new Vector3(mainCam.transform.position.x, 0, mainCam.transform.position.z);

        AllChunks = new Hashtable();
        EdgeChunks = new HashSet<Chunk>();
        RemoveEdgeChunks = new HashSet<Chunk>();
        AddEdgeChunks = new HashSet<Chunk>();

        LOD_EdgeChunks = new HashSet<Chunk>[i_numberofLODs-1] { new HashSet<Chunk>(), new HashSet<Chunk>() };
        LOD_AddEdgeChunks = new HashSet<Chunk>[i_numberofLODs-1] { new HashSet<Chunk>(), new HashSet<Chunk>() };
        LOD_RemoveEdgeChunks = new HashSet<Chunk>[i_numberofLODs-1] { new HashSet<Chunk>(), new HashSet<Chunk>() };

        rand = new float[]{Random.Range(20000f, 40000f), Random.Range(20000f, 40000f), Random.Range(20000f, 40000f), Random.Range(20000f, 40000f), Random.Range(20000f, 40000f) };

        StartingTerrainCheck();
        StartCoroutine(TerrainCheckRoutine());
    }

    /*protected Vector3 GetRandomPointOnChunk(Chunk chunk)
    {
        float randX = Random.value;
        float randZ = Random.value;
        GameObject terrain = (GameObject)chunk.data;
        Vector3 returnPosition = terrain.transform.position;
        returnPosition += new Vector3((randX + randZ / 2) * f_TargetTileSize, 0, randZ * f_TargetTileSize);

        //will store info of successful ray cast
        RaycastHit hitInfo;

        //terrain should have mesh collider and be on custom terrain 
        //layer so we don't hit other objects with our raycast
        LayerMask layer = 1 << LayerMask.NameToLayer("Terrain");


        Vector3 worldterrainDetect;
        Vector3 worldterrainDir;

        worldterrainDetect = returnPosition + Vector3.up * 1000f;
        worldterrainDir = Vector3.down;
        if (Physics.Raycast(worldterrainDetect, worldterrainDir, out hitInfo, 5000f, layer))
        {
            returnPosition = hitInfo.point;
        }

        return returnPosition;
    }*/

    protected bool IsChunkInRange(Chunk chunk, Vector3 cameraPosition, float distance)
    {
        float xPos = chunk.x * f_TargetTileSize;
        float zPos = chunk.z * f_TargetTileSize;
        Vector3 chunkPosition = new Vector3(xPos, cameraPosition.y, zPos);
        float sqrDistance = Vector3.SqrMagnitude(chunkPosition - cameraPosition);

        return (sqrDistance < distance * distance);
    }

    protected void DestroyChunk(Chunk chunk)
    {
        if (chunk == null) return;

        RemoveEdgeChunks.Add(chunk);
        for (int LOD = i_numberofLODs - 2; LOD >= 0; LOD--)
            LOD_RemoveEdgeChunks[LOD].Add(chunk);
        AllChunks.Remove(chunk.Hash());
        if (chunk.data != null)
            GameObject.Destroy(chunk.data);
    }

    Chunk CreateTerrainChunk(int x, int z)
    {
        if (AllChunks.ContainsKey(Chunk.HashFromCoords(x,z))) { return null; }

        Vector3 position = new Vector3(x * f_TargetTileSize, 0f, z * f_TargetTileSize);

        GameObject newTerrainObject;
        Vector3 scaling = new Vector3(f_chunkScaling, 1f, f_chunkScaling);

        newTerrainObject = GameObject.Instantiate(TerrainPrefab, this.transform);
        newTerrainObject.transform.position = position;
        newTerrainObject.transform.localScale = scaling;

        Transform waterPlane = newTerrainObject.transform.GetChild(1);
        waterPlane.gameObject.SetActive(false);
        SetLayersAndShifting(newTerrainObject, x, z, i_numberofLODs - 1);

        /*if (groundTerrain.childCount > i_numberofLODs)
        {
            Transform waterPlane = newTerrainObject.transform.GetChild(i_numberofLODs);
            waterPlane.gameObject.SetActive(false);
            SetLayersAndShifting(newTerrainObject, x, z, i_numberofLODs - 1);
        }*/

        Chunk newChunk = new Chunk(newTerrainObject, x, z);
        string chunkKey = newChunk.Hash();
        AllChunks.Add(chunkKey, newChunk);
        AddEdgeChunks.Add(newChunk);

        //CreateRocksAndTrees(newChunk, 1);

        return newChunk;
    }

    void AddChunkToEdge(int x, int z, int LOD)
    {
        string hash = Chunk.HashFromCoords(x, z);
        if (AllChunks.ContainsKey(hash))
        {
            Chunk thisChunk = (Chunk)AllChunks[hash];
            if (LOD == i_numberofLODs-1)
                AddEdgeChunks.Add(thisChunk);
            else if (!thisChunk.b_hasLODMeshes[LOD])
                LOD_AddEdgeChunks[LOD].Add(thisChunk);
        }
    }

    void StartingTerrainCheck()
    {   
        Vector3 cameraPosition = mainCam.transform.position;
        Chunk startChunk = CreateTerrainChunk((int)(cameraPosition.x / f_TargetTileSize), (int)(cameraPosition.z / f_TargetTileSize));
        EdgeChunks.Add(startChunk);
        for (int LOD = i_numberofLODs - 2; LOD >= 0; LOD --)
            LOD_EdgeChunks[LOD].Add(startChunk);
        bool thingsToDo = true;

        while (thingsToDo)
        {
            thingsToDo = TerrainCheck(cameraPosition);
        }
    }

    IEnumerator TerrainCheckRoutine()
    {
        Vector3 cameraPosition;

        while (true)
        {
            cameraPosition = mainCam.transform.position;
            TerrainCheck(cameraPosition);

            yield return new WaitForSeconds(f_TerrainCheckTime);
        }
    }

    /// <summary>
    /// Returns true if this successfully generates at least 1 new chunk
    /// </summary>
    /// <param name="cameraPosition"></param>
    /// <returns></returns>
    bool TerrainCheck(Vector3 cameraPosition)
    {
        bool ret = false;

        foreach (Chunk iterChunk in EdgeChunks)
        {
            if (IsChunkInRange(iterChunk, cameraPosition, f_CreateRange))
            {
                //ShowChunk(iterChunk);
                RemoveEdgeChunks.Add(iterChunk);

                int x = iterChunk.x; int z = iterChunk.z;
                // Check adjacent chunks to create more
                CreateTerrainChunk(x - 1, z);
                CreateTerrainChunk(x + 1, z);
                CreateTerrainChunk(x, z - 1);
                CreateTerrainChunk(x, z + 1);

                ret = true;
            }
            else if (!IsChunkInRange(iterChunk, cameraPosition, f_DestroyRange))
            {
                DestroyChunk(iterChunk);

                int x = iterChunk.x; int z = iterChunk.z;
                // Check adjacent chunks to make them edge chunks
                AddChunkToEdge(x - 1, z, i_numberofLODs-1);
                AddChunkToEdge(x + 1, z, i_numberofLODs - 1);
                AddChunkToEdge(x, z - 1, i_numberofLODs - 1);
                AddChunkToEdge(x, z + 1, i_numberofLODs - 1);
            }
        }

        EdgeChunks.UnionWith(AddEdgeChunks);
        EdgeChunks.ExceptWith(RemoveEdgeChunks);
        AddEdgeChunks.Clear(); RemoveEdgeChunks.Clear();

        // LODs
        for (int LOD = i_numberofLODs - 2; LOD >= 0; LOD--)
        {
            foreach (Chunk iterChunk in LOD_EdgeChunks[LOD])
            {
                if (IsChunkInRange(iterChunk, cameraPosition, f_LODRanges[LOD]))
                {
                    iterChunk.b_hasLODMeshes[LOD] = true;
                    LOD_RemoveEdgeChunks[LOD].Add(iterChunk);
                    int x = iterChunk.x; int z = iterChunk.z;
                    SetLayersAndShifting((GameObject)iterChunk.data, x, z, LOD);

                    AddChunkToEdge(x - 1, z, LOD);
                    AddChunkToEdge(x + 1, z, LOD);
                    AddChunkToEdge(x, z - 1, LOD);
                    AddChunkToEdge(x, z + 1, LOD);
                }
            }

            LOD_EdgeChunks[LOD].UnionWith(LOD_AddEdgeChunks[LOD]);
            LOD_EdgeChunks[LOD].ExceptWith(LOD_RemoveEdgeChunks[LOD]);
            LOD_AddEdgeChunks[LOD].Clear();
        }
          
        return ret;
    }

    void EditMesh(Mesh mesh, int xPosition, int zPosition)
    {
        bool throwaway;
        EditMesh(mesh, xPosition, zPosition, out throwaway);
    }

    void EditMesh(Mesh mesh, int xPosition, int zPosition, out bool lakeActive)
    {
        lakeActive = false;
        Vector3[] verts = mesh.vertices;
        float firstPass = 0f, secondPass = 0f, thirdPass = 0f, fourthPass = 0f, lakePass = 0f;
        float lakeOverride;
        for (int j = 0; j < verts.Length; j++)
        {
            lakeOverride = 0f;
            if (b_do1stPass)
            {
                firstPass = DoPerlin(f_1stPassRange, xPosition, zPosition, verts[j].x, verts[j].z, rand[0]);

                if (b_doLakePass)
                {
                    // Lakes!
                    lakePass = DoPerlin(f_lakePassRange, xPosition, zPosition, verts[j].x, verts[j].z, rand[4]);
                    if (lakePass + firstPass < f_lakePassFloor)
                    {                     
                        lakePass = Mathf.SmoothStep(0f, 1f, (lakePass + firstPass) / f_lakePassFloor);
                        lakeOverride = 1 - lakePass;
                        lakePass = (lakePass-1) * f_lakeDepth;                
                    }
                }

                firstPass *= f_1stPassHeight;
            }

            if (b_do2ndPass)
            {
                secondPass = DoPerlin(f_2ndPassRange, xPosition, zPosition, verts[j].x, verts[j].z, rand[1]);

                if (secondPass < f_2ndPassFloor + lakeOverride)
                    secondPass = 0f;
                else
                {
                    secondPass = Mathf.SmoothStep(0f, 1f, (secondPass - f_2ndPassFloor - lakeOverride) / (1.1f - f_2ndPassFloor + lakeOverride));

                    if (b_do3rdPass)
                    {
                        thirdPass = DoPerlin(f_3rdPassRange, xPosition, zPosition, verts[j].x, verts[j].z, rand[2]);

                        if (thirdPass > f_3rdPassFloor)
                        {
                            thirdPass = Mathf.SmoothStep(0f, 1f, (thirdPass - f_3rdPassFloor) / (1.1f - f_3rdPassFloor));
                            secondPass = Mathf.Lerp(secondPass, MathHelpers.MinusXSquaredCurve(secondPass), thirdPass);
                        }
                    }

                    secondPass *= f_2ndPassHeight;
                    //secondPass = Mathf.Pow(secondPass - f_2ndPassFloor, 3f) / Mathf.Pow(secondPass, 2) + secondPass;
                }
            }

            if (b_do4thPass)
                fourthPass = f_4thPassHeight * DoPerlin(f_4thPassRange, xPosition, zPosition, verts[j].x, verts[j].z, rand[3]);

            verts[j].y = firstPass + lakePass + secondPass + fourthPass - (f_1stPassHeight + f_2ndPassHeight) * 0.51f;
            if (verts[j].y < f_lakeWaterHeight)
                lakeActive = true;

            maxHeight = Mathf.Max(maxHeight, verts[j].y);
            minHeight = Mathf.Min(minHeight, verts[j].y);
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    void SetLayersAndShifting(GameObject terrain, int xPosition, int zPosition, int LOD)
    {
        Mesh mesh;
        MeshCollider m_collider;
        MeshFilter currentModel;

        // If the terrain object is just itself with no children. Not in use anymore
        if (terrain.TryGetComponent<MeshFilter>(out currentModel))
        {
            mesh = currentModel.mesh;
            EditMesh(mesh, xPosition, zPosition);

            m_collider = terrain.GetComponent<MeshCollider>();
            m_collider.sharedMesh = mesh;
        }
        else //Go into LODs
        {
            GameObject currentObject;
            bool turnOnLake;

            currentObject = terrain.transform.GetChild(0).GetChild(LOD).gameObject;
            mesh = currentObject.GetComponent<MeshFilter>().mesh;
            EditMesh(mesh, xPosition, zPosition, out turnOnLake);

            if (b_waterOn && turnOnLake && LOD == i_numberofLODs - 1)
            {
                Transform waterPlane = terrain.transform.GetChild(1);
                waterPlane.gameObject.SetActive(true);
                waterPlane.localPosition = new Vector3(waterPlane.localPosition.x, f_lakeWaterHeight, waterPlane.localPosition.z);
            }

            if (currentObject.TryGetComponent<MeshCollider>(out m_collider))
            {           
                m_collider = currentObject.GetComponent<MeshCollider>();
                m_collider.sharedMesh = mesh;           
            }
                
        }
            
        
    }

    /*void CreateRocksAndTrees(Chunk chunk, int numSheep)
    {
        if(SheepPrefab == null) return;

        Vector3 position = Vector3.zero;
        for (int i = 0; i < numSheep; i++)
        {
            position = GetRandomPointOnChunk(chunk);
            GameObject newSheep = GameObject.Instantiate(SheepPrefab);
            newSheep.transform.position = position;
        }
    }*/

    void CreateRocksAndTrees(GameObject terrain, int xPosition, int zPosition, float scale)
    {
        if (SheepPrefab == null) return;

        //will store info of successful ray cast
        RaycastHit hitInfo;

        //terrain should have mesh collider and be on custom terrain 
        //layer so we don't hit other objects with our raycast
        LayerMask layer = 1 << LayerMask.NameToLayer("Terrain");

        Vector3 worldterrainDetect;
        Vector3 worldterrainDir;

        worldterrainDetect = terrain.transform.position + Vector3.up * 1000f;
        worldterrainDir = Vector3.down;
        if (Physics.Raycast(worldterrainDetect, worldterrainDir, out hitInfo, 5000f, layer))
        {
            Vector3 position = hitInfo.point;
            GameObject newRock = GameObject.Instantiate(SheepPrefab);
            newRock.transform.position = position;
            //newRock.transform.localScale = new Vector3(15f,15f,15f);
        }
    }

    /// <summary>
    /// Returns a perlin value between 0 and 1 (can go below 0 sometimes)
    /// </summary>
    /// <param name="range"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="vertX"></param>
    /// <param name="vertZ"></param>
    /// <returns></returns>
    float DoPerlin(float range, int x, int z, float vertX, float vertZ, float random)
    {
        return Mathf.PerlinNoise(random/range + ((x + vertX / f_SourceTileSize) * f_TargetTileSize / range),
                random / range + ((z + vertZ / f_SourceTileSize) * f_TargetTileSize / range));
    }

    public void RedrawTerrain(bool forceMaxLOD)
    {
        foreach (DictionaryEntry entry in AllChunks)
        {
            Chunk iterChunk = (Chunk)entry.Value;
            SetLayersAndShifting((GameObject)iterChunk.data, iterChunk.x, iterChunk.z, 2);
            for (int LOD = i_numberofLODs - 2; LOD >= 0; LOD--)
            {
                if (forceMaxLOD || iterChunk.b_hasLODMeshes[LOD])
                    SetLayersAndShifting((GameObject)iterChunk.data, iterChunk.x, iterChunk.z, LOD);
            }
        } 
    }

    
}
