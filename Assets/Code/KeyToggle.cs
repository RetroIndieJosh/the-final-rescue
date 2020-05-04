using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyToggle : MonoBehaviour
{
    [SerializeField] private List<KeyCode> m_keyList = new List<KeyCode>();
    [SerializeField] private bool m_initiallyVisible = false;
    [SerializeField] private bool m_pauseOnActive = false;

    bool m_visible = true;

    private bool Visible {
        get { return m_visible; }
        set {
            m_visible = value;
            for ( var i = 0; i < transform.childCount; ++i )
                transform.GetChild( i ).gameObject.SetActive( value );

            if ( m_pauseOnActive )
                Time.timeScale = m_visible ? 0f : 1f;
        }
    }

    private void Start() {
        Visible = m_initiallyVisible;
    }

    private void Update() {
        if ( Console.instance.IsVisible ) return;

        foreach( var key in m_keyList) {
            if( Input.GetKeyDown( key ) ) {
                Visible = !Visible;
                return;
            }
        }
    }
}
