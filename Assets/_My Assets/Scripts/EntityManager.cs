using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : ChunkManager
{
    public int ChunkSize = 100;
    Hashtable AllEntityChunks;

    class EntityChunk
    {
        public HashSet<GameObject> entities;
        public int x;
        public int z;

        public EntityChunk(int xpos, int zpos)
        {
            x = xpos; z = zpos; //shown = false;
        }

        public void AddEntity(GameObject entity)
        {
            entities.Add(entity);
        }

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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
