using UnityEngine;

public class MapaColi : MonoBehaviour
{
    void Start()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            GameObject go = mf.gameObject;

            if (go.GetComponent<MeshCollider>() == null)
            {
                MeshCollider mc = go.AddComponent<MeshCollider>();
                mc.convex = false;
            }
        }

        Debug.Log("✅ Mesh Colliders adicionados a todos os objetos com malha.");
    }
}
