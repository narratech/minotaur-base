﻿/*    
   Copyright (C) 2020-2023 Federico Peinado
   http://www.federicopeinado.com
   Este fichero forma parte del material de la asignatura Inteligencia Artificial para Videojuegos.
   Esta asignatura se imparte en la Facultad de Informática de la Universidad Complutense de Madrid (España).
   Autor: Federico Peinado 
   Contacto: email@federicopeinado.com
*/
namespace UCM.IAV.Navegacion
{

    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UCM.IAV.Movimiento;
    using UnityEditor;

    public class GraphGrid : Graph
    {
        const int MAX_TRIES = 1000;

        public GameObject wallPrefab1;
        public GameObject wallPrefab2;
        public GameObject wallPrefab3;

        public GameObject intersection3Prefab;
        public GameObject intersection4Prefab;
        public GameObject turnPrefab;

        public GameObject endPrefab;
        public GameObject pillarPrefab;

        public GameObject obstaclePrefab;

        public string mapsDir = "Maps"; // Directorio por defecto
        public string mapName = "10x10.map"; // Fichero por defecto
        public bool get8Vicinity = false;
        public float cellSize = 1f;
        [Range(0, Mathf.Infinity)]
        public float defaultCost = 1f;
        [Range(0, Mathf.Infinity)]
        public float maximumCost = Mathf.Infinity;


        GameObject[] vertexObjs;

        private void Awake()
        {
            mapName = GameManager.instance.getSize() + ".map";
        }

        private int GridToId(int x, int y)
        {
            return Math.Max(numRows, numCols) * y + x;
        }

        private Vector2 IdToGrid(int id)
        {
            Vector2 location = Vector2.zero;
            location.y = Mathf.Floor(id / numCols);
            location.x = Mathf.Floor(id % numCols);
            return location;
        }

        private void LoadMap(string filename)
        {
            string path;
            
            path = Application.dataPath + "/" + mapsDir + "/" + filename;

            try
            {
                StreamReader strmRdr = new StreamReader(path);
                using (strmRdr)
                {
                    int j = 0, i = 0, id = 0;
                    string line;

                    Vector3 position = Vector3.zero;
                    Vector3 scale = Vector3.zero;

                    line = strmRdr.ReadLine(); // non-important line
                    line = strmRdr.ReadLine(); // height
                    numRows = int.Parse(line.Split(' ')[1]);
                    line = strmRdr.ReadLine(); // width
                    numCols = int.Parse(line.Split(' ')[1]);
                    line = strmRdr.ReadLine(); // "map" line in file

                    vertices = new List<Vertex>(numRows * numCols);
                    neighbourVertex = new List<List<Vertex>>(numRows * numCols);
                    vertexObjs = new GameObject[numRows * numCols];
                    mapVertices = new bool[numRows, numCols];
                    costsVertices = new float[numRows, numCols];

                    // Leer mapa
                    for (i = 0; i < numRows; i++)
                    {
                        line = strmRdr.ReadLine();
                        for (j = 0; j < numCols; j++)
                        {
                            bool isGround = true;
                            if (line[j] == 'e')
                                GameManager.instance.SetExit(j, i, cellSize);
                            else if (line[j] == 's')
                                GameManager.instance.SetStart(j, i, cellSize);
                            else if (line[j] == 'T')
                                isGround = false;
                            mapVertices[i, j] = isGround;
                        }
                    }

                    //Generamos terreno
                    for (i = 0; i < numRows; i++)
                    {
                        for (j = 0; j < numCols; j++)
                        {
                            position.x = j * cellSize;
                            position.z = i * cellSize;
                            id = GridToId(j, i);

                            if (mapVertices[i, j])
                                vertexObjs[id] = Instantiate(vertexPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                            else
                                vertexObjs[id] = WallInstantiate(position, i, j);

                            vertexObjs[id].name = vertexObjs[id].name.Replace("(Clone)", id.ToString());
                            Vertex v = vertexObjs[id].AddComponent<Vertex>();
                            v.id = id;
                            vertices.Add(v);
                            neighbourVertex.Add(new List<Vertex>());
                            
                            vertexObjs[id].transform.localScale *= cellSize;
                        }
                    }

                    // Leemos vecinos
                    for (i = 0; i < numRows; i++)
                        for (j = 0; j < numCols; j++)
                            SetNeighbours(j, i);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public override void Load()
        {
            LoadMap(mapName);
        }

        protected void SetNeighbours(int x, int y, bool get8 = false)
        {
            int col = x;
            int row = y;

            int i, j;
            int vertexId = GridToId(x, y);
            neighbourVertex[vertexId] = new List<Vertex>();
            Vector2[] pos = new Vector2[0];
            if (get8) {
                pos = new Vector2[8];
                int c = 0;
                for (i = row - 1; i <= row + 1; i++) {
                    for (j = col - 1; j <= col; j++) {
                        pos[c] = new Vector2(j, i);
                        c++;
                    }
                }
            }
            else {
                pos = new Vector2[4];
                pos[0] = new Vector2(col, row - 1);
                pos[1] = new Vector2(col - 1, row);
                pos[2] = new Vector2(col + 1, row);
                pos[3] = new Vector2(col, row + 1);
            }

            foreach (Vector2 p in pos) {
                i = (int)p.y;
                j = (int)p.x;

                if (i < 0 || j < 0 ||
                    i >= numRows || j >= numCols ||
                    i == row && j == col ||
                    !mapVertices[i, j])
                    continue;

                int id = GridToId(j, i);
                neighbourVertex[vertexId].Add(vertices[id]);
                costsVertices[i, j] = defaultCost;
            }
        }

        public override Vertex GetNearestVertex(Vector3 position)
        {
            int col = (int) Math.Round(position.x / cellSize);
            int row = (int) Math.Round(position.z / cellSize);
            Vector2 p = new Vector2(col, row);
            List<Vector2> explored = new List<Vector2>();
            Queue<Vector2> queue = new Queue<Vector2>();
            queue.Enqueue(p);
            do
            {
                p = queue.Dequeue();
                col = (int)p.x;
                row = (int)p.y;
                int id = GridToId(col, row);
                if (mapVertices[row, col])
                    return vertices[id];

                if (!explored.Contains(p))
                {
                    explored.Add(p);
                    int i, j;
                    for (i = row - 1; i <= row + 1; i++)
                    {
                        for (j = col - 1; j <= col + 1; j++)
                        {
                            if (i < 0 || j < 0 ||
                                j >= numCols || i >= numRows ||
                                i == row && j == col)
                                continue;
                            queue.Enqueue(new Vector2(j, i));
                        }
                    }
                }
            } while (queue.Count != 0);
            return null;
        }

        public override GameObject GetRandomPos()
        {
            GameObject pos = null;
            int tries = 0;

            int i, j;
            do
            {
                i = UnityEngine.Random.Range(0, numRows);
                j = UnityEngine.Random.Range(0, numCols);
                tries++;
            } while (tries < MAX_TRIES && !mapVertices[i, j]);

            pos = vertexObjs[GridToId(j, i)];

            return pos;
        }

        public override void UpdateVertexCost(Vector3 position, float costMultiplier)
        {
            Vertex v = GetNearestVertex(position);

            Vector2 gridPos = IdToGrid(v.id);

            int x = (int) gridPos.y;
            int y = (int) gridPos.x;


            if (x > 0 && x < numRows - 1 && y > 0 && y < numCols - 1)
                costsVertices[x, y] = defaultCost * costMultiplier * costMultiplier;

            if(x > 0) costsVertices[x - 1, y] = defaultCost * costMultiplier;
            if(x < numRows - 1) costsVertices[x + 1, y] = defaultCost * costMultiplier;
            if(y > 0) costsVertices[x, y - 1] = defaultCost * costMultiplier;
            if(y < numCols - 1) costsVertices[x, y + 1] = defaultCost * costMultiplier;

        }

        private GameObject WallInstantiate(Vector3 position, int i, int j)
        {
            //Suelo base e independiente
            GameObject floor = Instantiate(vertexPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
            floor.transform.localScale *= cellSize;
            floor.name = floor.name.Replace("(Clone)", GridToId(j, i).ToString());

            //Derecha, Izquierda, Arriba, Abajo
            bool[] dirs = new bool[4] { i < numRows - 1 && !mapVertices[i+1, j], 
                                        i > 0 && !mapVertices[i - 1, j], 
                                        j < numCols - 1 && !mapVertices[i, j + 1], 
                                        j > 0 && !mapVertices[i, j - 1] };

            int connec = 0;
            for (int index = 0; index < dirs.Length; index++)
                if (dirs[index]) connec++;

            //Interseccion en 4
            if (dirs[0] && dirs[1] && dirs[2] && dirs[3])
                return Instantiate(intersection4Prefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;

            //Interseccion en 3
            if (dirs[0] && dirs[1] && dirs[2])
                return Instantiate(intersection3Prefab, position, Quaternion.Euler(0, 90, 0), this.gameObject.transform) as GameObject;
            if (dirs[0] && dirs[1] && dirs[3])
                return Instantiate(intersection3Prefab, position, Quaternion.Euler(0, 270, 0), this.gameObject.transform) as GameObject;
            if (dirs[0] && dirs[2] && dirs[3])
                return Instantiate(intersection3Prefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
            if (dirs[1] && dirs[2] && dirs[3])
                return Instantiate(intersection3Prefab, position, Quaternion.Euler(0, 180, 0), this.gameObject.transform) as GameObject;

            //Interseccion muro
            if(dirs[0] && dirs[1])
                return Instantiate(wallPrefab1, position, Quaternion.Euler(0,90,0), this.gameObject.transform) as GameObject;
            if (dirs[2] && dirs[3])
                return Instantiate(wallPrefab1, position, Quaternion.identity, this.gameObject.transform) as GameObject;
            
            //Interseccion en giro
            if (dirs[0] && dirs[2])
                return Instantiate(turnPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
            if (dirs[0] && dirs[3])
                return Instantiate(turnPrefab, position, Quaternion.Euler(0, 270, 0), this.gameObject.transform) as GameObject;
            if (dirs[1] && dirs[2])
                return Instantiate(turnPrefab, position, Quaternion.Euler(0, 90, 0), this.gameObject.transform) as GameObject;
            if (dirs[1] && dirs[3])
                return Instantiate(turnPrefab, position, Quaternion.Euler(0, 180, 0), this.gameObject.transform) as GameObject;
            
            //Muro libre
            if (!dirs[0] && !dirs[1] && !dirs[2] && !dirs[3])
                return Instantiate(pillarPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;

            //Laterales
            if (dirs[0])
                return Instantiate(endPrefab, position, Quaternion.Euler(0, 90, 0), this.gameObject.transform) as GameObject;
            if (dirs[1])
                return Instantiate(endPrefab, position, Quaternion.Euler(0, 270, 0), this.gameObject.transform) as GameObject;
            if (dirs[2])
                return Instantiate(endPrefab, position, Quaternion.Euler(0, 180, 0), this.gameObject.transform) as GameObject;
            if (dirs[3])
                return Instantiate(endPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;

            return Instantiate(obstaclePrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
        }

    }
}
