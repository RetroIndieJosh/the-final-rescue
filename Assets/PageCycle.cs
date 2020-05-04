using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageCycle : MonoBehaviour
{
    [SerializeField] private KeyCode m_nextPageKey = KeyCode.F2;
    [SerializeField] private List<GameObject> m_pageList = new List<GameObject>();

    private int m_index;
    private bool m_isVisible = false;

    private void Start() {
        foreach ( var go in m_pageList ) go.SetActive( false );
    }

    private void Update() {
        if ( Input.GetKeyDown( m_nextPageKey ) ) {
            Debug.Log( "Next page" );
            if ( m_isVisible ) {
                m_pageList[m_index].SetActive( false );
                ++m_index;
            } else {
                m_index = 0;
                m_isVisible = true;
            }
            if ( m_index >= m_pageList.Count ) {
                m_isVisible = false;
                return;
            }
            Debug.Log( $"Show page{m_index}" );
            m_pageList[m_index].SetActive( true );
        }
    }
}
