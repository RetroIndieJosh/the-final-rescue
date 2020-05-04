using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAngleSelection : MonoBehaviour
{
    [SerializeField] private KeyCode m_nextAngleKey = KeyCode.Period;
    [SerializeField] private KeyCode m_prevAngleKey = KeyCode.Comma;

    [SerializeField] private int m_initialAngleIndex = 0;
    [SerializeField] private List<Transform> m_cameraAngleList = new List<Transform>();

    private int m_angleIndex = 0;

    private int AngleIndex {
        get { return m_angleIndex; }
        set {
            if ( value < 0 ) m_angleIndex = m_cameraAngleList.Count - 1;

            m_angleIndex = value % m_cameraAngleList.Count;
            transform.localPosition = m_cameraAngleList[m_angleIndex].localPosition;
            transform.localRotation = m_cameraAngleList[m_angleIndex].localRotation;
        }
    }

    private void Start() {
        AngleIndex = m_initialAngleIndex;
    }

    private void Update() {
        if ( m_cameraAngleList.Count == 0 ) return;

        for ( var i = 0; i < m_cameraAngleList.Count; ++i ) {
            var alphaKey = KeyCode.Alpha1 + i;
            var numKey = KeyCode.Keypad1 + i;

            if ( Input.GetKeyDown( alphaKey ) || Input.GetKeyDown( numKey ) ) {
                AngleIndex = i;
                break;
            }
        }

        if ( Input.GetKeyDown( m_nextAngleKey ) || Input.GetKeyDown( KeyCode.Joystick1Button8 ) ) ++AngleIndex;
    }
}
