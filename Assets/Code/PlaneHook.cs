using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneHook : MonoBehaviour
{
    enum CaptureState
    {
        CaptureMiss,
        Cooldown,
        Ready
    }

    [SerializeField] private Camera m_hookCamera = null;
    [SerializeField] private KeyCode m_captureKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode m_hookCameraKey = KeyCode.Mouse1;

    [Header( "Audio" )]
    [SerializeField] private AudioClip m_captureSound = null;

    [Header( "Capture" )]
    [SerializeField] private float m_sphereCaptureRadius = 0.5f;
    [SerializeField] private float m_captureDistanceMax = 1000f;
    [SerializeField] private LayerMask m_personLayer;
    [SerializeField] private float m_captureTime = 1f;
    [SerializeField] private float m_cooldownTime = 2f;
    [SerializeField] private float m_captureIntensity = 5f;
    [SerializeField] private Color m_captureColor = Color.green;
    [SerializeField] private Color m_invalidColor = Color.red;
    [SerializeField] private Color m_cooldownColor = Color.blue;

    private CaptureState m_state = CaptureState.Ready;

    private float m_captureTimeElapsed = 0f;
    private float m_cooldownTimeElapsed = 0f;

    private Light m_light = null;
    private Camera m_prevCamera = null;

    private Person m_targetPerson = null;

    private void Awake() {
        m_light = GetComponentInChildren<Light>();
    }

    private void Start() {
        HookEnabled = false;
    }

    private bool m_hookEnabled = false;

    private bool HookEnabled {
        set {
            m_hookEnabled = value;

            if ( m_hookEnabled ) {
                FindObjectOfType<Canvas>().worldCamera = m_hookCamera;
                m_prevCamera = Camera.main;
            } else {
                if( m_prevCamera != null )
                    FindObjectOfType<Canvas>().worldCamera = m_prevCamera;
            }

            m_light.enabled = m_hookEnabled;
            if( m_prevCamera != null ) m_prevCamera.enabled = !m_hookEnabled;
            m_hookCamera.enabled = m_hookEnabled;
        }
    }

    private void Update() {
        UpdateState();

        if ( Input.GetKeyDown( m_hookCameraKey ) || Input.GetKeyDown( KeyCode.Joystick1Button4 ) )
            HookEnabled = !m_hookEnabled;
    }

    private void UpdateState() {
        if ( m_light == null ) return;

        if ( m_state == CaptureState.Cooldown ) {
            m_light.color = m_cooldownColor;
            m_cooldownTimeElapsed += Time.deltaTime;
            if ( m_cooldownTimeElapsed > m_cooldownTime )
                m_state = CaptureState.Ready;
            return;
        }

        if ( Input.GetKeyDown( m_captureKey ) && m_targetPerson == null )
            m_state = CaptureState.CaptureMiss;

        m_light.color = m_invalidColor;
        if ( m_state == CaptureState.CaptureMiss ) {
            m_captureTimeElapsed += Time.deltaTime;
            var t = m_captureTimeElapsed / m_captureTime;
            if ( t < 0.5f ) m_light.intensity = ( m_captureIntensity - 1 ) * 2 * t + 1f;
            else if ( t > 0.5f ) m_light.intensity = ( m_captureIntensity - 1 ) * 2 * ( 1f - t ) + 1f;

            if ( m_captureTimeElapsed >= m_captureTime ) {
                m_cooldownTimeElapsed = 0f;
                m_state = CaptureState.Cooldown;
            }
            return;
        }

        CheckCapture();
        if ( Input.GetKeyDown( m_captureKey ) || Input.GetKeyDown( KeyCode.Joystick1Button5 ) ) {
            if ( m_targetPerson != null ) {
                m_captureTimeElapsed = 0f;
                m_state = CaptureState.CaptureMiss;

                CaptureTarget();
            }
        }
    }

    private void CheckCapture() {
        var hit = Physics.SphereCast( transform.position, m_sphereCaptureRadius, m_hookCamera.transform.forward,
            out RaycastHit hitInfo, m_captureDistanceMax, m_personLayer );

        if ( hit == false ) {
            m_targetPerson = null;
            return;
        }

        //Console.instance.WriteMessage( $"Hit {hitInfo.collider.name}" );
        var person = hitInfo.collider.GetComponent<Person>();
        if ( person == null ) {
            m_targetPerson = null;
            return;
        }

        // check line of sight
        var hit2 = Physics.Raycast( transform.position, person.transform.position - transform.position,
            out RaycastHit hitInfo2 );
        if ( hit2 && hitInfo2.collider != hitInfo.collider ) {
            Console.instance.WriteMessage( "No line of sight to target" );
            m_targetPerson = null;
            return;
        }

        m_light.color = m_captureColor;
        m_targetPerson = person;
    }

    private void CaptureTarget() {
        var plane = GetComponent<PassengerPlane>();
        if ( plane.CanAddPassenger == false ) return;

        PersonGenerator.instance.PlaySound( m_captureSound );

        plane.AddPassenger( m_targetPerson.Data );
        Destroy( m_targetPerson.gameObject );
        Console.instance.WriteMessage( $"Picked up {m_targetPerson.Data}" );

        m_cooldownTimeElapsed = 0f;
        m_state = CaptureState.Cooldown;
        GetComponent<PlaneController>().CheckWin();

        m_targetPerson = null;
    }
}
