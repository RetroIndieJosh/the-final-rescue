using UnityEngine;
using System.Collections;

public class Quit : MonoBehaviour {
    [SerializeField] private KeyCode m_key = KeyCode.Escape;

    public void QuitGame() { Application.Quit(); }

    private void Update() {
        if ( Input.GetKeyDown( m_key ) )
            QuitGame();
    }
}
