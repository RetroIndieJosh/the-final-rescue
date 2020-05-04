using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class PlaneControllerData
{
    public float pitchAngle = 0f;
    public float wingAngle = 0f;
    public float yRot = 0f;
}

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header( "Audio" )]
    [SerializeField] private AudioClip m_explodeSound = null;

    [Header("Visual")]
    [SerializeField] private GameObject m_mesh = null;
    [SerializeField] private float m_visualTiltDeg = 5f;
    [SerializeField] private GameObject m_deathEffect = null;

    [Header( "Input" )]
    [SerializeField] private KeyCode m_turnLeft = KeyCode.LeftArrow;
    [SerializeField] private KeyCode m_turnRight = KeyCode.RightArrow;
    [SerializeField] private KeyCode m_pitchUp = KeyCode.DownArrow;
    [SerializeField] private KeyCode m_pitchDown = KeyCode.UpArrow;
    [SerializeField] private KeyCode m_brake = KeyCode.LeftShift;

    [Header("Speed")]
    [SerializeField] private float m_forwardSpeed = 5f;
    [SerializeField] private float m_rotateSpeed = 5f;
    [SerializeField] private float m_tiltToTurnFactor = 1f;
    [SerializeField] private float m_pitchSpeed = 5f;
    [SerializeField] private float m_brakeFactor = 0.5f;
    [SerializeField] private float m_speedUpFactor = 1.5f;

    [Header("Acceleration/Deceleration")]
    [SerializeField] private float m_multAccelPerSec = 0.1f;
    [SerializeField] private float m_speedUpAccelMult = 5f;
    [SerializeField] private float m_slowDownAccelMult = 8f;

    [Header( "Angle Limits" )]
    [SerializeField] private float m_maxAngleX = 60f;
    [SerializeField] private float m_maxAngleZ = 60f;

    [Header( "World Limits" )]
    [SerializeField] private float m_crashDistanceAfterWarning = 100f;

    [Header( "UI" )]
    [SerializeField] private TextMeshProUGUI m_gameOverText = null;
    [SerializeField] private TextMeshProUGUI m_winText = null;
    [SerializeField] private TextMeshProUGUI m_scoreText = null;
    [SerializeField] private TextMeshProUGUI m_turnSpeedDisplay = null;

    public bool IsCrashing = false;
    public float SpeedMult = 1f;
    public float SpeedAdd = 1f;

    private float m_timeSinceLastWarning = 0f;

    private float m_pitchAngle = 0f;
    private float m_wingAngle = 0f;

    private Rigidbody m_body = null;

    private bool m_isGameOver = false;

    private float m_targetSpeedMult = 1f;

    private float ForwardSpeed { get { return ( m_forwardSpeed + SpeedAdd ) * SpeedMult; } }

    public void CheckWin() {
        if ( PersonGenerator.instance.PersonCount > 1 ) return;

        m_body.constraints = RigidbodyConstraints.FreezeAll;

        m_winText.gameObject.SetActive( true );
        m_isGameOver = true;

        ShowScore();
    }

    public void SetData( PlaneControllerData a_data ) {
        m_pitchAngle = a_data.pitchAngle;
        m_wingAngle = a_data.wingAngle;
        var rotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler( rotation.x, a_data.yRot, rotation.z );
    }

    public PlaneControllerData ToData() {
        return new PlaneControllerData() {
            pitchAngle = m_pitchAngle,
            wingAngle = m_wingAngle,
            yRot = transform.rotation.eulerAngles.y
        };
    }

    private void Awake() {
        m_body = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter( Collision collision ) {
        m_body.constraints = RigidbodyConstraints.FreezeAll;
        //Camera.main.transform.SetParent( null, true );

        //for ( var i = 0; i < transform.childCount; ++i )
        //Destroy( transform.GetChild( i ).gameObject );
        m_mesh.SetActive( false );
        m_gameOverText.gameObject.SetActive( true ); 
        m_isGameOver = true;
        PersonGenerator.instance.PlaySound( m_explodeSound );

        //Camera.main.transform.LookAt( transform.position );
        m_deathEffect.transform.position = transform.position;
        m_deathEffect.SetActive( true );

        ShowScore();
    }

    private void ShowScore() {
        m_scoreText.enabled = true;
        var isHighScore = PersonGenerator.instance.CheckHighScore();
        m_scoreText.text = $"Score: {PersonGenerator.instance.Score}\nHigh: {PersonGenerator.instance.HighScore}"
            + ( isHighScore ? "\nNew high score!" : "" );
    }

    private void Start() {
        m_gameOverText.gameObject.SetActive( false );
        m_winText.gameObject.SetActive( false );

        Console.instance.RegisterCallback( "game.checkwin", delegate(List<string> args) { CheckWin(); } );
        Console.instance.RegisterCallbackInt( "game.people", delegate ( int a_targetCount ) {
            var curCount = PersonGenerator.instance.PersonCount;
            if ( curCount <= a_targetCount ) return;
            var toDestroyCount = curCount - a_targetCount;
            Console.instance.WriteMessage( $"Destroying {toDestroyCount} people." );

            var toDestroyList = FindObjectsOfType<Person>();
            for( var i = 0; i < toDestroyCount; ++i ) {
                Destroy( toDestroyList[i] );
            }
        } );
    }

    private void Update() {
        if ( Input.GetKey( KeyCode.Escape ) ) {
            PersonGenerator.instance.RestartGame();
        }

        m_mesh.transform.localRotation = Quaternion.identity;

        m_timeSinceLastWarning += Time.deltaTime;

        var accelRate = m_multAccelPerSec;

        if ( Input.GetKey( m_brake ) || Input.GetAxis( "Left Trigger" ) > Mathf.Epsilon ) {
            m_targetSpeedMult = m_brakeFactor;
            accelRate *= m_slowDownAccelMult;
        } else if ( Input.GetKey( KeyCode.Space ) || Input.GetAxis( "Right Trigger" ) > Mathf.Epsilon ) {
            m_targetSpeedMult = m_speedUpFactor;
            accelRate *= m_speedUpAccelMult;
        } else m_targetSpeedMult = 1f;

        if ( SpeedMult < m_targetSpeedMult ) SpeedMult += accelRate * Time.deltaTime;
        else if( SpeedMult > m_targetSpeedMult ) SpeedMult -= accelRate * Time.deltaTime;

        if ( m_isGameOver ) {
            if ( Input.GetKeyDown( KeyCode.Space ) || Input.GetKeyDown( KeyCode.Joystick1Button7 ) ) {
                PersonGenerator.instance.RestartGame();
                return;
            }
        }

        if ( IsCrashing ) {
            m_pitchAngle -= m_pitchSpeed * Time.deltaTime;
        } else {
            HandlePitchInput();
            HandleTurnInput();
        }

        /*
        var farLimit = PersonGenerator.instance.WorldLimit;
        if ( transform.position.x < -farLimit.x ) {
            var overLimit = transform.position.x + farLimit.x;
            var pos = transform.position;
            pos.x = farLimit.x + overLimit;
            transform.position = pos;
        }
        if( transform.position.x > farLimit.x ) {
            var overLimit = transform.position.x - farLimit.x;
            var pos = transform.position;
            pos.x = -farLimit.x + overLimit;
            transform.position = pos;
        }
        if ( transform.position.z < -farLimit.z ) {
            var overLimit = transform.position.z + farLimit.z;
            var pos = transform.position;
            pos.z = farLimit.z + overLimit;
            transform.position = pos;
        }
        if( transform.position.z > farLimit.z ) {
            var overLimit = transform.position.z - farLimit.z;
            var pos = transform.position;
            pos.z = -farLimit.z + overLimit;
            transform.position = pos;
        }
        */

        var overLimitX = 0f;
        if( transform.position.x > PersonGenerator.instance.WorldLimit.x 
            || transform.position.x < -PersonGenerator.instance.WorldLimit.x ) {

            overLimitX = Mathf.Abs(transform.position.x ) - PersonGenerator.instance.WorldLimit.x;
        }

        var overLimitZ = 0f;
        if( transform.position.z > PersonGenerator.instance.WorldLimit.z 
            || transform.position.z < -PersonGenerator.instance.WorldLimit.z ) {

            overLimitZ = Mathf.Abs(transform.position.z ) - PersonGenerator.instance.WorldLimit.z;
        }

        if( overLimitX + overLimitZ > m_crashDistanceAfterWarning ) {
            IsCrashing = true;
        } else if( overLimitX + overLimitZ > Mathf.Epsilon ) {
            if ( m_timeSinceLastWarning > PersonGenerator.instance.TickLengthSec * 0.5f ) {
                m_timeSinceLastWarning -= PersonGenerator.instance.TickLengthSec * 0.5f;
                Console.instance.WriteMessage( "WARNING: TURN BACK NOW!", Color.yellow );
            }
        }

        UpdateRotation();

        m_body.velocity = transform.forward * ForwardSpeed * Time.timeScale;
    }
    
    private void ChangePitch( float a_mult ) {
        m_pitchAngle += m_pitchSpeed * Time.deltaTime * a_mult;
        var rotationEuler = m_mesh.transform.localRotation.eulerAngles;
        rotationEuler.x -= a_mult * m_visualTiltDeg;
        m_mesh.transform.localRotation = Quaternion.Euler( rotationEuler );
    }

    private void ChangeTurn( float a_mult ) {
        m_wingAngle += m_rotateSpeed * Time.deltaTime * a_mult;
        var rotationEuler = m_mesh.transform.localRotation.eulerAngles;
        rotationEuler.z -= a_mult * m_visualTiltDeg;
        m_mesh.transform.localRotation = Quaternion.Euler( rotationEuler );
    }

    private void HandlePitchInput() {
        if ( Input.GetKey( m_pitchDown ) || Input.GetAxis( "Vertical" ) > Mathf.Epsilon )
            ChangePitch( -1f );
        if ( Input.GetKey( m_pitchUp ) || Input.GetAxis("Vertical") < -Mathf.Epsilon )
            ChangePitch( 1f );
    }

    private void HandleTurnInput() {
        if ( Input.GetKey( m_turnLeft ) || Input.GetAxis( "Horizontal" ) < -Mathf.Epsilon )
            ChangeTurn( -1f );
        if ( Input.GetKey( m_turnRight ) || Input.GetAxis("Horizontal") > Mathf.Epsilon )
            ChangeTurn( 1f );
    }

    private void UpdateRotation() {
        if ( m_pitchAngle > Mathf.Epsilon && transform.position.y > PersonGenerator.instance.WorldLimit.y )
            m_pitchAngle = 0f;

        var yRot = transform.rotation.eulerAngles.y;
        var turnVel = m_wingAngle * Time.deltaTime * m_tiltToTurnFactor;
        yRot += turnVel;

        var hit = Physics.Raycast( transform.position, Vector3.down, out RaycastHit hitInfo );
        var groundDistanceStr = hit ? $"{hitInfo.distance:0.0}" : "unknown";

        m_turnSpeedDisplay.text = $"{ForwardSpeed:0.0} m/s\nAltitude {transform.position.y:0.0} m\n"
            + $"{groundDistanceStr} m to ground\n{PersonGenerator.instance.PersonCount} survivors remain";

        m_pitchAngle = Mathf.Clamp( m_pitchAngle, -m_maxAngleX, m_maxAngleX );
        m_wingAngle = Mathf.Clamp( m_wingAngle, -m_maxAngleZ, m_maxAngleZ );
        transform.rotation = Quaternion.Euler( -m_pitchAngle, yRot, -m_wingAngle );
    }
}
