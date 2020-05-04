using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class CommandEvent: UnityEvent<List<string>> { }

[System.Serializable]
class Command
{
    public Command(string a_key ) { key = a_key; }

    public string key = "";
    public CommandEvent callback = new CommandEvent();
}

public class Console : MonoBehaviour
{
    public static Console instance = null;

    [SerializeField] private bool m_silentCommandChanges = false;

    [Header("Default Colors")]
    [SerializeField] private Color m_colorDefault = Color.white;
    [SerializeField] private Color m_colorError = Color.red;
    [SerializeField] private Color m_colorWarn = Color.yellow;

    [Header( "Display" )]
    [SerializeField] private TMP_InputField m_inputField = null;
    [SerializeField] private List<Log> m_outputLogList = null;
    [SerializeField] private TextMeshProUGUI m_autoCompleteTextMesh = null;

    [Header( "Input" )]
    [SerializeField] private KeyCode m_activateKey = KeyCode.BackQuote;
    [SerializeField] private KeyCode m_autoCompleteKey = KeyCode.Tab;
    [SerializeField] private KeyCode m_deactivateKey = KeyCode.Escape;

    [Header( "Commands" )]
    [SerializeField] private List<Command> m_commandList = new List<Command>();

    private bool m_isVisible = false;
    private string m_outputText = "";
    private bool m_stayOnSubmit = false;

    private bool m_isAutoCompleting = false;
    private List<Command> m_autoCompleteCandidateList = new List<Command>();
    private int m_autoCompleteCandidateIndex = -1;

    public bool IsVisible {
        get { return m_isVisible; }
        private set {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if ( m_isVisible == value ) return;

            ClearAll();
            m_inputField.gameObject.SetActive( value );

            if ( value ) {
                Time.timeScale = 0f;
                m_inputField.ActivateInputField();
            } else {
                Time.timeScale = 1f;
                m_inputField.DeactivateInputField();
            }

            m_isVisible = value;
        }
    }

    public void DeterminePotentialAutoComplete( string a_input ) {
        if ( m_isAutoCompleting ) return;

        m_autoCompleteTextMesh.text = "";
        if ( a_input.Length == 0 ) return;

        m_autoCompleteCandidateList.Clear();
        foreach ( var command in m_commandList ) {
            if ( command.key.StartsWith( a_input ) )
                m_autoCompleteCandidateList.Add( command );
        }

        if ( m_autoCompleteCandidateList.Count == 0 ) {
            m_autoCompleteCandidateIndex = -1;
            m_autoCompleteTextMesh.text = "";
            return;
        }

        m_autoCompleteTextMesh.text = m_autoCompleteCandidateList[0].key;
    }

    public void ProcessInput( string a_input ) {
        WriteMessage( ">> " + m_inputField.text );

        var wordArr = a_input.Split( ' ' );
        var key = wordArr[0];
        var command = GetCommand( key );
        if ( command == null ) {
            WriteMessage( $"Unknown command <i>{key}</i>." );
        } else {
            var argList = wordArr.ToList();
            argList.RemoveAt( 0 );
            command.callback.Invoke( argList );
        }

        m_inputField.text = "";
        m_inputField.ActivateInputField();

        if( m_stayOnSubmit == false )
            IsVisible = false;
    }

    public void RemoveCallback( string a_key ) {
        var command = GetCommand( a_key );
        if( command == null ) {
            Debug.LogWarning( $"[Console] Tried to remove callback for {a_key} but doesn't exist." );
            return;
        }
        m_commandList.Remove( command );
    }

    public void RegisterCallback( string a_key, UnityAction<List<string>> a_callback ) {
        if( ContainsKey(a_key)) {
            RemoveCallback( a_key );
            if( m_silentCommandChanges == false ) WriteMessage( $"Overwrite command for <i>{a_key}</i>." );
        } else {
            if( m_silentCommandChanges == false ) WriteMessage( $"New command for <i>{a_key}</i>." );
        }

        var command = new Command( a_key );
        command.callback.AddListener( a_callback );
        m_commandList.Add( command );
    }

    public void RegisterCallbackFloat( string a_key, UnityAction<float> a_callback ) {
        RegisterCallback( a_key, delegate(List<string> a_args) { FloatCommand( a_callback, a_args ); } );
    }

    public void RegisterCallbackInt( string a_key, UnityAction<int> a_callback ) {
        RegisterCallback( a_key, delegate(List<string> a_args) { IntCommand( a_callback, a_args ); } );
    }

    public void RegisterCallbackString( string a_key, UnityAction<string> a_callback ) {
        RegisterCallback( a_key, delegate(List<string> a_args) { StringCommand( a_callback, a_args ); } );
    }

    public void WriteMessage( string a_message, bool a_newline = true ) {
        WriteMessage( a_message, a_newline, m_colorDefault );
    }

    public void WriteMessage( string a_message, Color a_color ) {
        WriteMessage( a_message, true, a_color );
    }

    public void WriteMessage( string a_message, bool a_newline, Color a_color ) {
        if ( a_newline ) a_message += "\n";

        foreach ( var outputLog in m_outputLogList )
            outputLog.AddEntry( a_message, a_color );
    }

    private void Awake() {
        if ( instance != null ) {
            Debug.LogErrorFormat( $"Duplicate console in {name}. Destroying." );
            Destroy( this );
            return;
        }

        instance = this;
    }

    private void Start() {
        m_isVisible = true;
        IsVisible = false;
    }

    private void Update() {
        if ( m_isAutoCompleting && Input.anyKeyDown ) {
            if ( Input.GetKey( m_autoCompleteKey )  == false )
                m_isAutoCompleting = false;
        }

        if ( Input.GetKeyDown( KeyCode.Return ) ) {
            m_stayOnSubmit = Input.GetKey( KeyCode.LeftShift );
            if ( m_stayOnSubmit ) WriteMessage( "Stay on submit" );
            ProcessInput( m_inputField.text );
        }
        if ( Input.GetKeyDown( m_activateKey ) ) IsVisible = true;
        if ( Input.GetKeyDown( m_deactivateKey ) ) IsVisible = false;

        if ( Input.GetKeyDown( m_autoCompleteKey ) ) AutoComplete();
    }

    private void AutoComplete() {
        m_isAutoCompleting = true;
        ++m_autoCompleteCandidateIndex;

        if ( m_autoCompleteCandidateIndex >= m_autoCompleteCandidateList.Count )
            m_autoCompleteCandidateIndex = 0;

        var completion = m_autoCompleteCandidateList[m_autoCompleteCandidateIndex].key + " ";
        m_inputField.text = completion;
        m_autoCompleteTextMesh.text = completion;
        m_inputField.MoveToEndOfLine( false, false );
    }

    private void FloatCommand( UnityAction<float> a_command, List<string> a_args ) {
        if ( a_args.Count != 1 ) {
            WriteMessage( "Command requires one float arg.", m_colorWarn );
            return;
        }

        if ( float.TryParse( a_args[0], out float floatVal ) ) {
            a_command.Invoke( floatVal );
            return;
        }

        WriteMessage( $"Illegal arg {a_args[0]}: expected float.", m_colorError );
    }

    private void IntCommand( UnityAction<int> a_command, List<string> a_args ) {
        if ( a_args.Count != 1 ) {
            WriteMessage( "Command requires one int arg.", m_colorWarn );
            return;
        }

        if ( int.TryParse( a_args[0], out int intVal ) ) {
            a_command.Invoke( intVal );
            return;
        }

        WriteMessage( $"Illegal arg {a_args[0]}: expected int.", m_colorError );
    }

    private void StringCommand( UnityAction<string> a_command, List<string> a_args ) {
        if ( a_args.Count != 1 ) {
            WriteMessage( "Command requires one string arg.", m_colorWarn );
            return;
        }

        WriteMessage( $"Illegal arg {a_args[0]}: expected string.", m_colorError );
    }

    private void ClearAll() {
        m_inputField.text = "";
        m_autoCompleteTextMesh.text = "";
        m_autoCompleteCandidateIndex = -1;
        m_autoCompleteCandidateList.Clear();
    }

    private bool ContainsKey(string a_key ) {
        return GetCommand( a_key ) != null;
    }

    private Command GetCommand(string a_key ) {
        foreach( var command in m_commandList ) {
            if ( command.key == a_key ) return command;
        }
        return null;
    }
}
