using UnityEngine;

public class RagdollDevSpawn : MonoBehaviour
{
    [SerializeField] private GameObject ragdoll;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Instantiate(ragdoll, transform.position, Quaternion.identity);  
        }
    }
}
