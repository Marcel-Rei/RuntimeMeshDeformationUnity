
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class DeformerScript : MonoBehaviour
{
    
    /// <summary>
    /// Deform the mesh of the specified GameObject based on the deformer vertices
    /// </summary>
    /// <param name="objectImpacted">
    /// The GameObject whose mesh will be deformed
    /// </param>

    public void ApplyImpactToMesh(GameObject objectImpacted)
    {
        transform.gameObject.SetActive(true);
        //get vertices Meshdeformation from object hit
        MeshDeformationScript meshDeformation = objectImpacted.GetComponent<MeshDeformationScript>();

        //Get own collider
        meshDeformation.impactCol = GetComponent<Collider>();

        if(meshDeformation == null)
        {
            Debug.LogWarning($"couldnt get MeshDeformation on the hit Gameobject in {this.name} in CreateDeformerScript in GetVerOnPosPlaneSode");
        }

        //get vertices from self
        Vector3[] meshVertices = GetComponent<MeshFilter>().mesh.vertices;
    
        Vector3[] meshVerticesGlobal = new Vector3[meshVertices.Length];

        //transform vertices into global space
        transform.TransformPoints(meshVertices, meshVerticesGlobal);

        if(meshVertices.Length != meshVertices.Length)
        {
            Debug.LogWarning($"Error occoured in calculating global vertices in {this.name} in CreateDeformerScript in GetVerOnPosPlaneSode");
        }
        
        //deform the mesh that was hit
        meshDeformation.StartImpact(meshVerticesGlobal.ToList()); //set to pos on impact
        transform.gameObject.SetActive(false);
    }
}
