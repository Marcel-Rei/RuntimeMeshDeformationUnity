
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mesh))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshDeformationScript : MonoBehaviour
{
    [Header("references")]
    public Material normalMat;
    public Material impactMat;

    [Header("internal references")]
    public Collider impactCol;
    public Mesh mesh;
    public Renderer comRenderer;
    public List<Material> renderMaterials;
    public Vector3[] startNormals;

    public MeshCollider meshCollider;
    public bool isDeformed;

    [Header("internal vertices data")]
    public List<int> trianglesImpactSubMesh;

    [Header("Coroutine")]
    Coroutine impactCoroutine;

    void Start() 
    { 
        isDeformed = false;

        //get renderer Component and set its normal texture
        comRenderer = GetComponent<Renderer>();
        renderMaterials = new List<Material>() {normalMat};
        comRenderer.materials = renderMaterials.ToArray();

        //Get normals 
        mesh = GetComponent<MeshFilter>().mesh;
        startNormals = new Vector3[mesh.normals.Length];
        startNormals = mesh.normals;

        meshCollider = GetComponent<MeshCollider>();
    }

    #region ErrorHandling

    #endregion

    #region MeshImpact

    /// <summary>
    /// Start the Coroutine to Deform the Mesh based on the given vertices
    /// </summary>
    /// <param name="verticesToDeformTo">
    /// the vertices we want to deform to
    /// </param> 
    public void StartImpact(List<Vector3> verticesToDeformTo)
    {   

        if(impactCoroutine != null)
        {
            StopCoroutine(impactCoroutine);
        }
        impactCoroutine = StartCoroutine(HandleImpact(verticesToDeformTo));
        
    }

    /// <summary>
    /// Deformes the mesh based on the vertices given
    /// </summary>
    /// <param name="verticesToDeformTo">
    /// the vertices we want to deform to in global space
    /// </param> 
    public IEnumerator HandleImpact(List<Vector3> verticesToDeformTo)
    {

        Vector3[] meshVertices = mesh.vertices; //current vertices

        List<Vector3> hitVertices; //vertices that are inside the impact
        List<int> hitVerticesIndex; //Index of vertices that are inside the impact
        List<Vector3> hitVerticeNormals; //Normals from vertices that are inside the impact

        GetVerticesInCollider(meshVertices, impactCol, startNormals, out hitVertices, out hitVerticesIndex, out hitVerticeNormals); //get the Data explained above

        List<Vector3> deformedVertices; //vertices with the deformed data
        List<int> deformedVerticesIndex; //index of the original vertex position the deformed

        //fill the deformed vertices with data 
        GetDeformerVertices(verticesToDeformTo, hitVertices, hitVerticesIndex, hitVerticeNormals, out deformedVertices, out deformedVerticesIndex);

        //deforms our current mesh
        DeformAndUpdateMesh(meshVertices, deformedVertices, deformedVerticesIndex);

        //Create Submesh to apply different textures to the Mesh
        if(!isDeformed)
        {
            CreateSubMesh(mesh, deformedVerticesIndex);
            isDeformed = true;
        }
        else
        {
            AddToSubMesh(mesh, 1,deformedVerticesIndex);
        }

        RecalculateMeshData(mesh);

        //performance heavy
        UpdateMeshCollider(); 

        yield return null;

    }
   
    #endregion

    #region MeshData

    /// <summary>
    /// Returns a List of vertices with the corresponding indices and the normals that were inside the collider given.
    /// </summary>
    /// <param name="vertices">
    /// The vertices we want to check are inside the collider.
    /// </param> 
    /// <param name="collider">
    /// The collider where we check if the vertices are inside
    /// </param> 
    /// <param name="verticesNormal">
    /// The normals corresponding to the vertices
    /// </param>
    /// <param name="hitVertices">
    /// the vertices that were inside the collider in global space.
    /// </param> 
    /// <param name="hitVerticesIndex">
    /// the indeces of the vertices that were inside the collider.
    /// </param> 
    /// <param name="hitVerticeNormals">
    /// the normals from the unmodified mesh of the vertices that were inside the collider in global space.
    /// </param>

    public void GetVerticesInCollider(Vector3[] vertices, Collider collider, Vector3[] verticesNormal, out List<Vector3> hitVertices, out List<int> hitVerticesIndex, out List<Vector3> hitVerticeNormals)
    {
        hitVertices = new List<Vector3>();
        hitVerticesIndex = new List<int>();
        hitVerticeNormals = new List<Vector3>();

        if(vertices == null || vertices.Length == 0)
        {
            Debug.LogError("Vertices array is null or empty in GetVerticesInCollider");
            return;
        }

        //transform vertices and normals to worldspace
        Vector3[] worldVertices = new Vector3[vertices.Length];
        Vector3[] worldStartVerticeNormals = new Vector3[vertices.Length];
        transform.TransformPoints(vertices, worldVertices);
        transform.TransformPoints(verticesNormal, worldStartVerticeNormals);


        //iterate through vertices
        for(int i = 0; i < vertices.Length; i++)
        {
            //check if the vertex in world space is inside the deformer collider
            if (collider.bounds.Contains(worldVertices[i]))
            {
                //add to list of vertices to deform
                hitVertices.Add(worldVertices[i]);

                //add index of the vertices
                hitVerticesIndex.Add(i);

                //add the global normal from the unmodified Mesh
                hitVerticeNormals.Add(worldStartVerticeNormals[i]);

            }
        }

        if(hitVertices.Count == 0)
        {
            Debug.LogWarning("No vertices found inside the deformer collider bounds in GetVerticesInCollider");
        }
        if(hitVertices.Count != hitVerticesIndex.Count || hitVertices.Count != hitVerticeNormals.Count)
        {
            Debug.LogError("Count mismatch between hitVertices, hitVerticesIndex or hitVerticesNormals in GetVerticesInCollider");
        }
    }

    /// <summary>
    /// Returns a List of vertices deformed to the nearest deformerVertices. 
    /// This is based on a Raycast along the flipped normal from the original Data Mesh 
    /// of the given vertex from hitVertices.
    /// </summary>
    /// <param name="verticesToDeformTo">
    /// the vertices we want to deform to in global space
    /// </param> 
    /// <param name="hitVertices">
    /// the vertices we want to deform to the deformer Vertices
    /// </param> 
    /// <param name="hitVerticesIndex">
    /// the Index of the vertices we want to deform to the deformer Vertices
    /// </param> 
    /// <param name="hitVerticesNormals">
    /// the Normals of the vertices we want to deform to the deformer Vertices
    /// </param> 
    /// <param name="deformedVertices">
    /// The vertices deformed to the nearest deformer vertex
    /// </param> 
    /// <param name="deformedVerticesIndex">
    /// The Index of the vertices we deformed to the nearest deformer vertex
    /// </param>

    public void GetDeformerVertices(List<Vector3> verticesToDeformTo, List<Vector3> hitVertices, List<int> hitVerticesIndex, List<Vector3> hitVerticesNormals, out List<Vector3> deformedVertices, out List<int> deformedVerticesIndex)
    {

        deformedVertices = new List<Vector3>();
        deformedVerticesIndex = new List<int>();

        int defaultLayer = gameObject.layer; 
        Physics.queriesHitBackfaces = true;

        int ignoreRaycastIndex = LayerMask.NameToLayer("Ignore Raycast");
        if(0 > ignoreRaycastIndex)
        {
            Debug.LogWarning("Ignore Raycast Layer doesnt exist, not all vertices may be deformed in GetDeformerVertices");
        }
        else
        {
            gameObject.layer = ignoreRaycastIndex; 
        }

        for(int i = 0; i < hitVertices.Count; i++)
        {
            RaycastHit hit;

            if(Physics.Raycast(hitVertices[i], hitVerticesNormals[i] * -1, out hit, Mathf.Infinity))
            {
                if(hit.transform.CompareTag("Deformer")) 
                {
                    Vector3 localHitPoint = hit.point; 

                    Vector3 closestVertex = GetClosestVertex(verticesToDeformTo, localHitPoint); 

                    deformedVertices.Add(closestVertex);
                    deformedVerticesIndex.Add(hitVerticesIndex[i]);

                    //Debug, Raycast from the original vertex to the hit.point 
                    //Debug.DrawLine(hitVertices[i], hit.point, Color.green, 5); 

                }
            }

        }

        gameObject.layer = defaultLayer; 
        Physics.queriesHitBackfaces = false;
    }
    
    /// <summary>
    /// Gives back the vertex in local transform with the least distance to the PointToCheck from the impact Vertices
    /// </summary>
    /// <param name="globalVertices">
    /// the vertices we want to compare the distance of in global space
    /// </param> 
    /// /// <param name="pointToCheck">
    /// the vertex we want to use to get the least distance to
    /// </param> 

    public Vector3 GetClosestVertex(List<Vector3> globalVertices, Vector3 pointToCheck) 
    {
        if(globalVertices == null || globalVertices.Count == 0)
        {
            Debug.LogWarning("Impact vertices list is null or empty in GetClosestVertex");
            return Vector3.zero;
        }

        float minDistance = float.MaxValue;
        Vector3 nearestVertex = new Vector3();

        for(int i = 0; i < globalVertices.Count; i++)
        {

            float distance = Vector3.Distance(pointToCheck, globalVertices[i]);

            if(distance < minDistance)
            {
                minDistance = distance;
                nearestVertex = globalVertices[i];
            }
        }

        return transform.InverseTransformPoint(nearestVertex);
    }
    
    #endregion

    #region DeformMesh

    /// <summary>
    /// Replaces vertices in a mesh with the deformed vertices at the specified indices and updates the mesh afterwards
    /// </summary>
    /// <param name="meshVertices">
    /// The list of vertices where replacements will occur
    /// </param> 
    /// <param name="deformedVertices">
    /// The vertices to replace in the mesh
    /// </param> 
    /// <param name="deformedVerticesIndex">
    /// The indices of the deformed vertices
    /// </param> 

    public void DeformAndUpdateMesh(Vector3[] meshVertices, List<Vector3> deformedVertices, List<int> deformedVerticesIndex)
    {
        if (meshVertices == null || deformedVertices == null || deformedVerticesIndex == null)
        {
            Debug.LogError("vertices list are null in DeformAndUpdateMesh");
            return;
        }

        if (deformedVertices.Count != deformedVerticesIndex.Count)
        {
            Debug.LogError("Count mismatch between deformedVertices and hitVerticesIndex in DeformAndUpdateMesh");
            return;
        }

        for(int i = 0; i < deformedVertices.Count; i++)
        {
            //replace the vertex
            meshVertices[deformedVerticesIndex[i]] = deformedVertices[i];

        }

        //update the mesh
        mesh.vertices = meshVertices;
    }


    /// <summary>
    /// Update the Normals and the Bounds of the mesh given
    /// </summary>
    /// <param name="mesh">
    /// The mesh getting updated
    /// </param> 
    public void RecalculateMeshData(Mesh mesh)
    {
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();     
    }

    /// <summary>
    /// Set our current Mesh in the Mesh Collider
    /// </summary>

    public void UpdateMeshCollider()
    {
        meshCollider.sharedMesh = mesh;
    }

    #endregion

    #region Submesh

    /// <summary>
    /// Creates a submesh within the provided mesh based on the specified vertex indices.
    /// </summary>
    /// <param name="mesh">
    /// The mesh in which the submesh will be created.
    /// </param> 
    /// <param name="verticesIndex">
    /// The indices of the vertices used to create the submesh.
    /// </param> 
    
    public void CreateSubMesh(Mesh mesh, List<int> verticesIndex) 
    {

        //Get the current triangles
        int[] meshTriangles = mesh.triangles;

        //for each triangles from the mesh (i+3 because 1 Triangle is 3 vertices)
        for(int i = 0; i < meshTriangles.Length; i += 3)
        {
            int vertexIndex1 = meshTriangles[i];
            int vertexIndex2 = meshTriangles[i + 1];
            int vertexIndex3 = meshTriangles[i + 2];

            //check if one of the vertices from the triangle was impactes
            if( verticesIndex.Contains(vertexIndex1) || 
                verticesIndex.Contains(vertexIndex2) || 
                verticesIndex.Contains(vertexIndex3))
            {
                //add to impacted
                trianglesImpactSubMesh.Add(vertexIndex1);
                trianglesImpactSubMesh.Add(vertexIndex2);
                trianglesImpactSubMesh.Add(vertexIndex3);
            }
        }
            
        //If a second material doesnt exist yet
        if(mesh.subMeshCount < 2)
        {
            //add a new one
            mesh.subMeshCount = 2;
            SetMaterials();
        }

        //Create a new submesh for the affected triangles to apply the updated Material
        mesh.SetTriangles(trianglesImpactSubMesh, 1); 

    }

    /// <summary>
    /// Add to a submesh within the provided mesh based on the specified vertex indices.
    /// </summary>
    /// <param name="mesh">
    /// The mesh in which the vertices will be added to the submesh.
    /// </param> 
    /// <param name="subMeshIndex">
    /// The index of the submesh where we want to add vertices
    /// </param> 
    /// <param name="verticesIndex">
    /// The indices of the vertices to add to the submesh.
    /// </param> 
    public void AddToSubMesh(Mesh mesh, int subMeshIndex, List<int> verticesIndex)
    {
        
        //Get current vertices from Submesh
        List<int> mainSubMesh = new List<int>();
        List<int> impactSubMesh = new List<int>();
        mesh.GetIndices(mainSubMesh, 0);
        mesh.GetIndices(impactSubMesh, 1);

        //for each vertex in the mainMesh
        for(int i = 0; i < mainSubMesh.Count; i += 3)
        {
            int vertexIndex1 = mainSubMesh[i];
            int vertexIndex2 = mainSubMesh[i + 1];
            int vertexIndex3 = mainSubMesh[i + 2];

            bool verticesInImpactSubMesh = false;

            //check if one of the vertices from the triangle was impactes
            if( verticesIndex.Contains(vertexIndex1) || 
                verticesIndex.Contains(vertexIndex2) || 
                verticesIndex.Contains(vertexIndex3))
            {
                for(int j = 0; j < impactSubMesh.Count; j += 3)
                {
                    if(impactSubMesh[j] == vertexIndex1 && impactSubMesh[j + 1] == vertexIndex2 && impactSubMesh[j + 2] == vertexIndex3)
                    {
                        verticesInImpactSubMesh = true;
                        break;
                    }
                }

                if(!verticesInImpactSubMesh)
                {
                    //add to impacted
                    trianglesImpactSubMesh.Add(vertexIndex1);
                    trianglesImpactSubMesh.Add(vertexIndex2);
                    trianglesImpactSubMesh.Add(vertexIndex3);
                }
            }
        }

        //Add to the second submesh 
        mesh.SetTriangles(trianglesImpactSubMesh, subMeshIndex); 

    }

    /// <summary>
    /// Adds the impactMat to the material list of the renderer.
    /// </summary>
    public void SetMaterials()
    {
        renderMaterials.Add(impactMat);
        comRenderer.materials = renderMaterials.ToArray();   
    }
    #endregion
   
}
