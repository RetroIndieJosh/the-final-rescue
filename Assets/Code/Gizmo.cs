using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GizmoType
{
    Cube,
    Sphere
}

public class Gizmo : MonoBehaviour
{
    [SerializeField] private GizmoType m_type = GizmoType.Sphere;
    [SerializeField] private bool m_isWired = true;
    [SerializeField] private Color m_color = Color.red;

    [SerializeField] private bool m_onlyWhenSelected = false;
    [SerializeField] private float m_radius = 0.5f;

    [SerializeField] private bool m_drawForward = false;

    private void OnDrawGizmos() {
        if ( m_onlyWhenSelected ) return;
        DrawGizmo();
    }

    private void OnDrawGizmosSelected() {
        if ( m_onlyWhenSelected == false ) return;
        DrawGizmo();
    }

    private void DrawGizmo() {
        Gizmos.color = m_color;
        //Gizmos.matrix = Matrix4x4.TRS( transform.position, transform.rotation, transform.lossyScale );
        if( m_type == GizmoType.Cube ) {
            if ( m_isWired ) Gizmos.DrawWireCube( transform.position, Vector3.one * m_radius );
            else Gizmos.DrawCube( transform.position, Vector3.one * m_radius );
        } else if( m_type == GizmoType.Sphere ) {
            if ( m_isWired ) Gizmos.DrawWireSphere( transform.position, m_radius );
            else Gizmos.DrawSphere( transform.position, m_radius );
        }

        if( m_drawForward ) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine( transform.position, transform.position + transform.forward * 5f );
        }
    }
}
