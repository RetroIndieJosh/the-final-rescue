using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartOnFloor : MonoBehaviour
{
    [SerializeField] private float m_height = 1.0f;

    private void Start() {
        if ( Physics.Raycast( transform.position, Vector3.down, out RaycastHit hit ) ) {
            transform.position = hit.point + Vector3.up * m_height * 0.5f;
        }
    }
}
