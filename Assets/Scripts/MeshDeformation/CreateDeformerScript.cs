using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreateDeformerScript : MonoBehaviour
{   
    [Header("references")]
    public GameObject deformerToSpawn;
    GameObject deformer;

    DeformerScript deformerScript;

    void Start()
    {
        deformer = Instantiate(deformerToSpawn, transform.position, Quaternion.identity);
        deformer.SetActive(false);
        deformerScript = deformer.GetComponent<DeformerScript>();
    }
    void Update()
    {
        if(Input.GetMouseButtonDown(0)){ShootRay();}
    }

    /// <summary>
    /// Shoot A Physics Raycast and Create a Deformer on the hit Pos if the Mesh hit has the tag deformable
    /// </summary>
    /// <param name="verticesToDeformTo">
    /// the vertices we want to deform to
    /// </param> 
    public void ShootRay()
    {
        RaycastHit hit;
        if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
        {
            if(hit.collider.CompareTag("Deformable"))
            {
                CreateDeformer(hit.point, hit.transform.gameObject);
            }
        }
        
    }

    /// <summary>
    /// Create a deformer on the position provided and deform the mesh given
    /// </summary>
    /// <param name="pos">
    /// the position the deformer instantiates at
    /// </param> 
    /// <param name="meshToDeform">
    /// the mesh that will be deformed
    /// </param>
    
    public void CreateDeformer(Vector3 pos, GameObject meshToDeform)
    {
        deformer.transform.position = pos;
        deformerScript.ApplyImpactToMesh(meshToDeform);
    }
}