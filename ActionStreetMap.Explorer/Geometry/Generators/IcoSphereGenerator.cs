﻿using System.Collections.Generic;
using ActionStreetMap.Explorer.Utils;
using ActionStreetMap.Unity.Wrappers;
using UnityEngine;

namespace ActionStreetMap.Explorer.Geometry.Generators
{
    internal class IcoSphereGenerator
    {
        public static MeshData Generate(Context context)
        {
            var vertList = new List<Vector3>();
            var middlePointIndexCache = new Dictionary<long, int>();
            int index = 0;

            var radius = context.Radius;
            var recursionLevel = context.RecursionLevel;
         
            // create 12 vertices of a icosahedron
            float t = (1f + Mathf.Sqrt(5f)) / 2f;

            vertList.Add(new Vector3(-1f, t, 0f).normalized * radius);
            vertList.Add(new Vector3(1f, t, 0f).normalized * radius);
            vertList.Add(new Vector3(-1f, -t, 0f).normalized * radius);
            vertList.Add(new Vector3(1f, -t, 0f).normalized * radius);

            vertList.Add(new Vector3(0f, -1f, t).normalized * radius);
            vertList.Add(new Vector3(0f, 1f, t).normalized * radius);
            vertList.Add(new Vector3(0f, -1f, -t).normalized * radius);
            vertList.Add(new Vector3(0f, 1f, -t).normalized * radius);

            vertList.Add(new Vector3(t, 0f, -1f).normalized * radius);
            vertList.Add(new Vector3(t, 0f, 1f).normalized * radius);
            vertList.Add(new Vector3(-t, 0f, -1f).normalized * radius);
            vertList.Add(new Vector3(-t, 0f, 1f).normalized * radius);


            // create 20 triangles of the icosahedron
            var faces = new List<TriangleIndices>();

            // 5 faces around point 0
            faces.Add(new TriangleIndices(0, 11, 5));
            faces.Add(new TriangleIndices(0, 5, 1));
            faces.Add(new TriangleIndices(0, 1, 7));
            faces.Add(new TriangleIndices(0, 7, 10));
            faces.Add(new TriangleIndices(0, 10, 11));

            // 5 adjacent faces 
            faces.Add(new TriangleIndices(1, 5, 9));
            faces.Add(new TriangleIndices(5, 11, 4));
            faces.Add(new TriangleIndices(11, 10, 2));
            faces.Add(new TriangleIndices(10, 7, 6));
            faces.Add(new TriangleIndices(7, 1, 8));

            // 5 faces around point 3
            faces.Add(new TriangleIndices(3, 9, 4));
            faces.Add(new TriangleIndices(3, 4, 2));
            faces.Add(new TriangleIndices(3, 2, 6));
            faces.Add(new TriangleIndices(3, 6, 8));
            faces.Add(new TriangleIndices(3, 8, 9));

            // 5 adjacent faces 
            faces.Add(new TriangleIndices(4, 9, 5));
            faces.Add(new TriangleIndices(2, 4, 11));
            faces.Add(new TriangleIndices(6, 2, 10));
            faces.Add(new TriangleIndices(8, 6, 7));
            faces.Add(new TriangleIndices(9, 8, 1));

            // refine triangles
            for (int i = 0; i < recursionLevel; i++)
            {
               var faces2 = new List<TriangleIndices>();
                foreach (var tri in faces)
                {
                    // replace triangle by 4 triangles
                    int a = GetMiddlePoint(tri.V1, tri.V2, ref vertList, ref middlePointIndexCache, radius);
                    int b = GetMiddlePoint(tri.V2, tri.V3, ref vertList, ref middlePointIndexCache, radius);
                    int c = GetMiddlePoint(tri.V3, tri.V1, ref vertList, ref middlePointIndexCache, radius);

                    faces2.Add(new TriangleIndices(tri.V1, a, c));
                    faces2.Add(new TriangleIndices(tri.V2, b, a));
                    faces2.Add(new TriangleIndices(tri.V3, c, b));
                    faces2.Add(new TriangleIndices(a, b, c));
                }
                faces = faces2;
            }

            return GenerateMeshData(context, faces, vertList);
        }

        /// <summary> Generates mesh data in flat shading style. </summary>
        private static MeshData GenerateMeshData(Context context, List<TriangleIndices> faces, List<Vector3> vertList)
        {
            var colors = new List<Color>(faces.Count * 3);
            var vertices = new List<Vector3>(faces.Count * 3);
            var triangles = new List<int>(faces.Count * 3);
            var vertNoiseFreq = context.VertNoiseFreq;
            var useVertNoise = context.UseVertNoise;
            var color = Color.green;
            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];

                var v0 = vertList[face.V1];

                var noise = useVertNoise ? (Noise.Perlin2D(v0, vertNoiseFreq) + 1f) / 2f : 0;
                vertices.Add(new Vector3(v0.x + noise, v0.y + noise, v0.z + noise));

                var v1 = vertList[face.V2];
                noise = useVertNoise ? (Noise.Perlin2D(v1, vertNoiseFreq) + 1f) / 2f : 0;
                vertices.Add(new Vector3(v1.x + noise, v1.y + noise, v1.z + noise));

                var v2 = vertList[face.V3];
                noise = useVertNoise ? (Noise.Perlin2D(v2, vertNoiseFreq) + 1f) / 2f : 0;
                vertices.Add(new Vector3(v2.x + noise, v2.y + noise, v2.z + noise));

                var ggg = vertList.Count - 3;

                triangles.Add(ggg);
                triangles.Add(ggg + 2);
                triangles.Add(ggg + 1);

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
            }

            return new MeshData()
            {
                Vertices = vertices,
                Triangles = triangles,
                Colors = colors
            };   
        }

        // return index of point in the middle of p1 and p2
        private static int GetMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache, float radius)
        {
            // first check if we have it already
            bool firstIsSmaller = p1 < p2;
            long smallerIndex = firstIsSmaller ? p1 : p2;
            long greaterIndex = firstIsSmaller ? p2 : p1;
            long key = (smallerIndex << 32) + greaterIndex;

            int ret;
            if (cache.TryGetValue(key, out ret))
                return ret;

            // not in cache, calculate it
            Vector3 point1 = vertices[p1];
            Vector3 point2 = vertices[p2];
            Vector3 middle = new Vector3
            (
                (point1.x + point2.x) / 2f,
                (point1.y + point2.y) / 2f,
                (point1.z + point2.z) / 2f
            );

            // add vertex makes sure point is on unit sphere
            int i = vertices.Count;
            vertices.Add(middle.normalized * radius);

            // store it, return index
            cache.Add(key, i);

            return i;
        }

        #region Nested classes

        private struct TriangleIndices
        {
            public int V1;
            public int V2;
            public int V3;

            public TriangleIndices(int v1, int v2, int v3)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
            }
        }

        public class Context
        {
            public float Radius;
            public int RecursionLevel;

            public bool UseVertNoise;
            public float VertNoiseFreq = 0.2f;

            public bool UseColorNoise;
            public float ColorNoiseFreq;

            public GradientWrapper Gradient;
        }

        #endregion
    }
}