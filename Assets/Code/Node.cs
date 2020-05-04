using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] private Node m_next = null;

    public Node Next { get { return m_next; } }
    public Node Prev { get; private set; }

    private void Awake() {
        if ( m_next == null ) return;
        m_next.Prev = this;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere( transform.position, 1.0f );
    }
}
