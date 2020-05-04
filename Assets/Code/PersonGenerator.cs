using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class GameData
{
    public List<PersonData> personDataList = new List<PersonData>();
    public List<Vector3> personPosList = new List<Vector3>();
    public List<Vector3> shopPosList = new List<Vector3>();
    public PlaneData playerData = null;
    public float timeElapsed = 0f;
}

[System.Serializable]
public class PersonData
{
    public string name = "UnknownName";
    public string surname = "UnknownFamily";
    public string profession = "UnknownProfession";
    public int age = 35;

    public override string ToString() {
        return $"{name} {surname} ({age}) the {profession.ToLower()}";
    }
}

static public class Utility {
    static public List<string> SplitAndTrimToList( this string a_str, char a_split ) {
        var list = a_str.Split( a_split ).ToList();
        var trimmedList = new List<string>();
        foreach ( var s in list ) trimmedList.Add( s.Trim() );
        return trimmedList;
    }

    static public T RandomElement<T>(this List<T> a_list ) {
        var i = Random.Range( 0, a_list.Count );
        return a_list[i];
    }

    static public List<T> ChildComponentList<T>( this GameObject a_go ) where T: MonoBehaviour {
        var list = new List<T>();
        foreach( Transform child in a_go.transform ) {
            var person = child.GetComponent<T>();
            if ( person == null ) continue;
            list.Add( person );
        }
        return list;
    }
}

public class PersonGenerator : MonoBehaviour
{
    const string LOADING_GAME_DATA_PLAYER_PREFS_KEY = "loading game from file";
    const string GAME_SAVE_FILENAME = "save.json";

    static public PersonGenerator instance = null; 

    [SerializeField] private bool m_isEndless = false;
    [SerializeField] private float m_dayLengthSec = 15f;

    [Header( "UI" )]
    [SerializeField] private TextMeshProUGUI m_loadingTextMesh = null;
    [SerializeField] private TextMeshProUGUI m_highScoreDisplay = null;
    [SerializeField] private GameObject m_titleStuff = null;

    [Header( "Debug" )]
    [SerializeField] private bool m_bypassLoad = false;

    [Header("Shops")]
    [SerializeField] private int m_shopCount = 10;
    [SerializeField] private Shop m_shop = null;
    [SerializeField] private int m_minShopDistance = 50;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_personPrefab = null;
    [SerializeField] private GameObject m_shopPrefab = null;

    [Header("Files")]
    [SerializeField] private TextAsset m_nameListFile = null;
    [SerializeField] private TextAsset m_surnameListFile = null;
    [SerializeField] private TextAsset m_professionListFile = null;

    [Header("Person Limits")]
    [SerializeField] private int m_ageMin = 18;
    [SerializeField] private int m_ageMax = 65;
    [SerializeField] private int m_personCountMin = 5;
    [SerializeField] private int m_minPersonDistance = 5;

    [Header( "World Limits" )]
    [SerializeField] private float m_personGenerationRadius = 500f;
    [SerializeField] private Vector3 m_worldLimit = Vector3.one * 1000f;

    public Vector3 WorldLimit {  get { return m_worldLimit; } }
    public float TickLengthSec {  get { return m_dayLengthSec / 3f; } }

    private List<string> m_nameList = new List<string>();
    private List<string> m_surnameList = new List<string>();
    private List<string> m_professionList = new List<string>();

    private bool m_gameStarted = false;
    private bool m_isLoading = false;

    private float m_startTimeElapsed = 0f;

    private float TimeElapsed {
        get { return Time.timeSinceLevelLoad + m_startTimeElapsed; }
    }

    public int Day {
        get { return Mathf.FloorToInt( TimeElapsed / m_dayLengthSec ); }
    }

    public int Hour {
        get {
            var remainingDayFraction = TimeElapsed / m_dayLengthSec - Day;
            return Mathf.FloorToInt( remainingDayFraction * 24f );
        }
    }

    public int Minute {
        get {
            var remainingDayFraction = TimeElapsed / m_dayLengthSec - Day;
            var remainingHourFraction = remainingDayFraction * 24f - Hour;
            return Mathf.FloorToInt( remainingHourFraction * 60f );
        }
    }

    public int PersonCount {
        get { return gameObject.ChildComponentList<Person>().Count; }
    }

    public string TimeStr {
        get { return $"Day {Day + 1:##} {Hour:00}:{Minute:00}"; }
    }

    private bool IsLoadingFromFile {
        get { return PlayerPrefs.GetInt( LOADING_GAME_DATA_PLAYER_PREFS_KEY ) == 1; }
    }

    private float m_timeSinceLastTick = 0f;

    private Coroutine m_personGenerationCoroutine = null;

    public int HighScore {
        get { return PlayerPrefs.GetInt( "high score", 0 ); }
        set { PlayerPrefs.SetInt( "high score", value ); }
    }

    public int Score {
        get {
            var rescueCount = m_personCountMin - PersonCount;
            var rescueScore = rescueCount * 100;
            if ( PersonCount == 0 ) rescueScore *= 2;
            return Day * 24 * 60 + Hour * 60 + Minute + rescueScore;
        }
    }

    public bool CheckHighScore() {
        if ( Score > HighScore ) HighScore = Score;
        return Score > HighScore;
    }

    private Queue<AudioClip> m_soundQueue = new Queue<AudioClip>();

    public void PlaySound( AudioClip a_clip ) {
        m_soundQueue.Enqueue( a_clip );
    }

    public void StartLoadGame() {
        if ( IsLoadingFromFile ) return;

        if ( File.Exists( GAME_SAVE_FILENAME ) == false ) {
            Console.instance.WriteMessage( "No save file found; aborting load" );
            return;
        }

        PlayerPrefs.SetInt( LOADING_GAME_DATA_PLAYER_PREFS_KEY, 1 );
        RestartGame();
    }

    private void FinishLoadGame() {
        if ( IsLoadingFromFile == false ) return;

        Console.instance.WriteMessage( "Loading game from file..." );
        m_loadingTextMesh.enabled = true;
        m_titleStuff.SetActive( false );

        var jsonStr = File.ReadAllText( GAME_SAVE_FILENAME );
        var data = JsonUtility.FromJson<GameData>( jsonStr );

        // TODO make this less hacky
        FindObjectOfType<PassengerPlane>().SetData( data.playerData );

        for( var i = 0; i < data.personPosList.Count; ++i )
            CreatePerson( data.personPosList[i], data.personDataList[i] );

        foreach ( var shopPos in data.shopPosList )
            CreateShop( shopPos );

        m_startTimeElapsed = data.timeElapsed;

        PlayerPrefs.SetInt( LOADING_GAME_DATA_PLAYER_PREFS_KEY, 0 );
        Console.instance.WriteMessage( "Load from file complete!" );
        m_loadingTextMesh.enabled = false;
        m_gameStarted = true;
        Time.timeScale = 1;
    }

    public void RestartGame() {
        SceneManager.LoadScene( SceneManager.GetActiveScene().buildIndex );
    }

    private void CreatePerson( Vector3 m_pos, PersonData m_data ) {
        var person = Instantiate( m_personPrefab, m_pos, Quaternion.identity );
        person.transform.SetParent( transform, true );
        person.GetComponent<Person>().Data = m_data;
    }

    private void CreateShop( Vector3 m_pos ) {
        var shop = Instantiate( m_shopPrefab, m_pos, Quaternion.identity );
        shop.transform.SetParent( transform, true );
    }

    private void GenerateShop() {
        var shopPos = Vector3.zero;
        var loopCount = 0;
        while ( true ) {
            shopPos = RandomWorldPos();
            ++loopCount;
            if ( loopCount > 1000 * 1000 * 1000 ) {
                Console.instance.WriteMessage( "Cannot generate person; too many iterations." );
                break;
            }

            var tooClose = false;
            foreach( Transform child in transform ) {
                if ( child.GetComponentInChildren<ShopTrigger>() == null ) continue;
                var distance = Vector3.Distance( shopPos, child.transform.position );
                if( distance < m_minShopDistance ) {
                    tooClose = true;
                    //Console.instance.WriteMessage( $"Reroll for shop placement ({distance}/{m_minShopDistance})" );
                    break;
                }
            }

            if ( tooClose == false ) break;
        }

        CreateShop( shopPos );
    }

    private void GeneratePerson() {
        var personPos = Vector3.zero;
        var loopCount = 0;
        while ( true ) {
            personPos = RandomWorldPos();
            ++loopCount;
            if ( loopCount > 1000 * 1000 * 1000 ) {
                Console.instance.WriteMessage( "Cannot generate person; too many iterations." );
                break;
            }

            var tooClose = false;
            foreach( Transform child in transform ) {
                if ( child.GetComponentInChildren<Person>() == null ) continue;
                var distance = Vector3.Distance( personPos, child.transform.position );
                if( distance < m_minPersonDistance ) {
                    tooClose = true;
                    Console.instance.WriteMessage( $"Reroll for person placement ({distance}/{m_minPersonDistance})" );
                    break;
                }
            }

            if ( tooClose == false ) break;
        }

        var personData = new PersonData() {
            name = m_nameList.RandomElement(),
            surname = m_surnameList.RandomElement(),
            profession = m_professionList.RandomElement(),
            age = Random.Range( m_ageMin, m_ageMax )
        };

        CreatePerson( personPos, personData );
    }

    public void SaveGame( PlaneData a_playerData ) {
        if ( m_isLoading || IsLoadingFromFile ) {
            Console.instance.WriteMessage( "Can't save now", Color.yellow );
            return;
        }

        var gameData = new GameData() {
            playerData = a_playerData,
            timeElapsed = TimeElapsed
        };

        var personList = gameObject.ChildComponentList<Person>();
        foreach( var person in personList ) {
            gameData.personDataList.Add( person.Data );
            gameData.personPosList.Add( person.transform.position );
        }

        var shopList = gameObject.ChildComponentList<ShopTrigger>();
        foreach( var shop in shopList )
            gameData.shopPosList.Add( shop.transform.position );

        var jsonStr = JsonUtility.ToJson( gameData );
        File.WriteAllText( GAME_SAVE_FILENAME, jsonStr );

        Console.instance.WriteMessage( $"Saved to {GAME_SAVE_FILENAME}." );
    }

    public void StartShop( PassengerPlane m_plane ) {
        m_shop.StartShop( m_plane );
    }

    private void Awake() {
        instance = this;
    }

    private void OnApplicationQuit() {
        PlayerPrefs.SetInt( LOADING_GAME_DATA_PLAYER_PREFS_KEY, 0 );
    }

    private void Start() {
        m_titleStuff.SetActive( true );

        m_nameList = m_nameListFile.text.SplitAndTrimToList( '\n' );
        m_surnameList = m_surnameListFile.text.SplitAndTrimToList( '\n' );
        m_professionList = m_professionListFile.text.SplitAndTrimToList( '\n' );

        if ( IsLoadingFromFile ) {
            FinishLoadGame();
            return;
        }

        m_personGenerationCoroutine = StartCoroutine( GeneratePeople() );
        StartCoroutine( GenerateShops() );

        if ( m_bypassLoad ) {
            m_loadingTextMesh.enabled = false;
        } else {
            m_isLoading = true;
            m_loadingTextMesh.enabled = true;
        }
    }

    private void Update() {
        if ( m_soundQueue.Count > 0 ) {
            var source = GetComponent<AudioSource>();
            if ( !source.isPlaying ) {
                source.clip = m_soundQueue.Dequeue();
                source.Play();
            }
        }

        m_highScoreDisplay.text = $"High {HighScore:00000000}";
        if( Input.GetKey(KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl ) ) {
            if ( Input.GetKeyDown( KeyCode.Backspace ) )
                HighScore = 0;
        }

        if( m_isLoading ) {
            var curPersonCount = PersonCount;
            if ( curPersonCount == m_personCountMin ) {
                Time.timeScale = 1f;
                m_isLoading = false;
                m_loadingTextMesh.enabled = false;
                Console.instance.WriteMessage( "Loading complete!" );
            } else {
                m_loadingTextMesh.text = $"Loading {(float)curPersonCount / m_personCountMin * 100f:###}%";
                Time.timeScale = 0f;
            }
        }

        if ( IsLoadingFromFile ) Time.timeScale = 0f;

        if ( m_gameStarted == false ) {
            if ( Input.GetKeyDown( KeyCode.Space ) || Input.GetKeyDown(KeyCode.Joystick1Button7) ) {
                Time.timeScale = 1f;
                m_gameStarted = true;
                m_titleStuff.SetActive( false );
            } else {
                Time.timeScale = 0f;
                return;
            }
        }

        if ( m_isEndless == false ) return;

        m_timeSinceLastTick += Time.deltaTime;
        if( m_timeSinceLastTick > TickLengthSec ) {
            m_timeSinceLastTick -= TickLengthSec;
            m_personGenerationCoroutine = StartCoroutine( GeneratePeople() );
        }
    }

    private IEnumerator GeneratePeople() {
        if ( m_personGenerationCoroutine != null ) yield break;

        var loopCount = 0;
        while ( PersonCount < m_personCountMin ) {
            GeneratePerson();
            ++loopCount;
            if ( loopCount > 100000 ) yield break;
            yield return null;
        }

        m_personGenerationCoroutine = null;
    }

    private IEnumerator GenerateShops() {
        for ( var i = 0; i < m_shopCount; ++i ) {
            GenerateShop();
            yield return null;
        }
    }

    private Vector3 RandomWorldPos() {
        var x = Random.Range( -m_personGenerationRadius, m_personGenerationRadius );
        var z = Random.Range( -m_personGenerationRadius, m_personGenerationRadius );
        return new Vector3( x, m_worldLimit.y, z );
    }
}
