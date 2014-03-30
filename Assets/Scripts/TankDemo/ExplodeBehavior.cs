﻿using System.Collections;
using UnityEngine;

namespace Assets.Scripts.TankDemo
{
    public class ExplodeBehavior: MonoBehaviour
    {
        IEnumerator SplitMesh()
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            Mesh m = mf.mesh;
            Vector3[] verts = m.vertices;
            Vector3[] normals = m.normals;
            Vector2[] uvs = m.uv;
            for (int submesh = 0; submesh < m.subMeshCount; submesh++)
            {
                int[] indices = m.GetTriangles(submesh);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var newVerts = new Vector3[3];
                    var newNormals = new Vector3[3];
                    var newUvs = new Vector2[3];
                    for (int n = 0; n < 3; n++)
                    {
                        int index = indices[i + n];
                        newVerts[n] = verts[index];
                        newUvs[n] = uvs[index];
                        newNormals[n] = normals[index];
                    }
                    var mesh = new Mesh();
                    mesh.vertices = newVerts;
                    mesh.normals = newNormals;
                    mesh.uv = newUvs;

                    mesh.triangles = new int[] { 0, 1, 2, 2, 1, 0 };

                    var go = new GameObject("Triangle " + (i / 3));
                    go.transform.position = transform.position;
                    go.transform.rotation = transform.rotation;
                    go.AddComponent<MeshRenderer>().material = mr.materials[submesh];
                    go.AddComponent<MeshFilter>().mesh = mesh;
                    go.AddComponent<BoxCollider>();
                    go.AddComponent<Rigidbody>().AddExplosionForce(1, transform.position, 10);

                    Destroy(go, 5 + Random.Range(0.0f, 5.0f));
                }
            }
            mr.enabled = false;

            Time.timeScale = 0.2f;
            yield return new WaitForSeconds(0.0f);
            Time.timeScale = 1.0f;
            Destroy(gameObject);
        }
        
        void OnMouseDown()
        {
            Debug.LogWarning("Destroy!");
            StartCoroutine(SplitMesh());
        }
    }
}