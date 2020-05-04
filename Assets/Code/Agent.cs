using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

enum AgentMode
{
    Loop,
    Pace,
    Random
}

[DisallowMultipleComponent]
[RequireComponent( typeof( NavMeshAgent ) )]
public class Agent : MonoBehaviour
{
    [SerializeField] private float m_nodeArriveDistance = 0.5f;
    [SerializeField] private Node m_startNode = null;
    [SerializeField] private AgentMode m_mode = AgentMode.Loop;

    private Node m_curNode;

    private int m_nodeIndex = 0;
    private NavMeshAgent m_navMeshAgent = null;
    private bool m_isBacktracking = false;

    private void OnCollisionEnter( Collision collision ) {
        var node = collision.gameObject.GetComponent<Node>();
        if ( node == null ) return;

        if ( node == m_curNode ) {
            Console.instance.WriteMessage( $"{name} collided with target node." );
            GoToNext();
        }
    }

    private void Awake() {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void OnDrawGizmos() {
        if ( m_startNode == null ) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere( transform.position, 1f );
            return;
        }

        Gizmos.color = Color.cyan;
        var curNode = m_startNode;

        if ( m_mode == AgentMode.Random ) {
            var nodeList = new List<Node>();
            while ( curNode != null ) {
                nodeList.Add( curNode );
                curNode = curNode.Next;
            }
            foreach ( var node in nodeList ) {
                foreach ( var node2 in nodeList ) {
                    if ( node == node2 ) continue;
                    Gizmos.DrawLine( node.transform.position, node2.transform.position );
                }
            }

            return;
        }

        Gizmos.DrawLine( transform.position, curNode.transform.position );
        while ( curNode.Next != null ) {
            Gizmos.DrawLine( curNode.transform.position, curNode.Next.transform.position );
            curNode = curNode.Next;
        }

        if ( m_mode == AgentMode.Loop )
            Gizmos.DrawLine( curNode.transform.position, m_startNode.transform.position );
    }

    private void Start() {
        if ( m_startNode == null ) {
            Debug.LogError( $"Agent must have at least one node. Destroying {name}::Agent." );
            Destroy( this );
            return;
        }

        m_curNode = m_startNode;
    }

    private void Update() {
        if ( Time.timeScale < Mathf.Epsilon || m_curNode == null ) return;

        m_navMeshAgent.destination = m_curNode.transform.position;
        if ( TargetDistance <= m_nodeArriveDistance ) {
            Console.instance.WriteMessage( $"{name} arrived at target node." );
            GoToNext();
        }
    }

    private void GoToNext() {
        if ( m_mode == AgentMode.Random ) {
            var steps = Random.Range( 0, 100 );
            for ( var i = 0; i < steps; ++i ) {
                m_curNode = m_curNode.Next;
                if ( m_curNode == null )
                    m_curNode = m_startNode;
            }
            return;
        }

        var prevNode = m_curNode;
        if ( m_isBacktracking )
            m_curNode = m_curNode.Prev;
        else m_curNode = m_curNode.Next;
        if ( m_curNode != null ) return;

        if ( m_mode == AgentMode.Loop )
            m_curNode = m_startNode;
        else if ( m_mode == AgentMode.Pace ) {
            m_isBacktracking = !m_isBacktracking;
            m_curNode = prevNode;
            GoToNext();
        }
    }

    private float TargetDistance {
        get {
            if ( m_curNode == null ) return 0f;
            float xDiff = transform.position.x - m_curNode.transform.position.x;
            float zDiff = transform.position.z - m_curNode.transform.position.z;
            return Mathf.Sqrt( ( xDiff * xDiff ) + ( zDiff * zDiff ) );
        }
    }
}
