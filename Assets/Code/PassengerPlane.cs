using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class PassengerPlaneStats
{
    public int speedRating = 0;
    public int foodCapacity = 30;
    public int personCapacity = 3;
}

[System.Serializable]
public class PlaneData
{
    public PlaneControllerData controllerData = null;
    public Vector3 position;
    public PassengerPlaneStats stats = new PassengerPlaneStats();
    public List<PersonData> passengerList = new List<PersonData>();
    public int food = 0;
}

[RequireComponent(typeof(PlaneController))]
public class PassengerPlane : MonoBehaviour
{
    [Header( "Audio" )]
    [SerializeField] private AudioClip m_lowFoodSound = null;
    [SerializeField] private AudioClip m_passengerFullSound = null;

    [Header( "Game Factors" )]
    [SerializeField] private int m_foodFromCorpse = 5;
    [SerializeField] private float m_speedAddPerRating = 5f;

    [Header("Eating")]
    [SerializeField] private int m_breakfastHour = 6;
    [SerializeField] private int m_lunchHour = 13;
    [SerializeField] private int m_dinnerHour = 20;

    [Header( "Upgrades" )]
    [SerializeField] private int m_speedRatingUpgradeIncrease = 1;
    [SerializeField] private int m_foodCapacityUpgradeIncrease = 5;
    [SerializeField] private int m_personCapacityUpgradeIncrease = 2;

    [Header( "Stats" )]
    [SerializeField] private PassengerPlaneStats m_stats = new PassengerPlaneStats();
    [SerializeField] private float m_lowFoodWarningPercent = 0.2f;

    [Header( "UI" )]
    [SerializeField] private TextMeshProUGUI m_statDisplay = null;

    private int m_food = 0;
    private List<PersonData> m_passengerList = new List<PersonData>();
    private PlaneController m_planeController = null;

    public bool CanAddFood { get { return m_food < m_stats.foodCapacity; } }
    public int PassengerCount {  get { return m_passengerList.Count; } }

    public void AddFood( int a_food ) {
        m_food += a_food;

        if ( m_food > m_stats.foodCapacity) {
            var excess = m_food - m_stats.foodCapacity;
            m_food -= excess;
            Console.instance.WriteMessage( $"Threw away {excess} food due to limited storage" );
        }
    }

    public bool AddPassenger(PersonData a_person) {
        if ( m_planeController.IsCrashing || PassengerCount >= m_stats.personCapacity ) return false;

        m_passengerList.Add( a_person );
        if( m_passengerList.Count == m_stats.personCapacity )
            PersonGenerator.instance.PlaySound( m_passengerFullSound );
        return true;
    }

    public bool CanAddPassenger {  get { return m_passengerList.Count < m_stats.personCapacity; } }

    public PersonData RemovePassenger() {
        if ( m_passengerList.Count == 0 ) return null;

        var randomIndex = Random.Range( 0, m_passengerList.Count );
        var targetPassenger = m_passengerList[randomIndex];
        m_passengerList.Remove( targetPassenger );
        return targetPassenger;
    }

    public void SetData( PlaneData a_data ) {
        m_food = a_data.food;
        m_passengerList = a_data.passengerList;
        m_stats = a_data.stats;

        transform.position = a_data.position;
        GetComponent<PlaneController>().SetData( a_data.controllerData );
    }

    private PlaneData ToData() {
        var data = new PlaneData() {
            controllerData = GetComponent<PlaneController>().ToData(),
            food = m_food,
            passengerList = m_passengerList,
            position = transform.position,
            stats = m_stats
        };

        return data;
    }

    public void UpgradeFoodCapacity() { m_stats.foodCapacity += m_foodCapacityUpgradeIncrease; }
    public void UpgradePersonCapacity() { m_stats.personCapacity += m_personCapacityUpgradeIncrease; }
    public void UpgradeSpeed() { m_stats.speedRating += m_speedRatingUpgradeIncrease; }

    private void Awake() {
        m_planeController = GetComponent<PlaneController>();
    }

    private void Start() {
        m_food = m_stats.foodCapacity;

        Console.instance.RegisterCallbackInt( "player.food", delegate ( int a_food ) { m_food = a_food; } );
        Console.instance.RegisterCallbackInt( "player.speed", delegate ( int a_speed ) { m_stats.speedRating = a_speed; } );
        Console.instance.RegisterCallbackInt( "player.personCap", 
            delegate ( int a_capacity ) { m_stats.personCapacity = a_capacity; } );
        Console.instance.RegisterCallbackInt( "player.foodCap", 
            delegate ( int a_capacity ) { m_stats.foodCapacity = a_capacity; } );
    }

    private void Update() {
        if ( m_planeController.IsCrashing ) return;

        if ( Input.GetKeyDown( KeyCode.F6 ) ) 
            PersonGenerator.instance.SaveGame( ToData() );
        if ( Input.GetKeyDown( KeyCode.F9 ) )
            PersonGenerator.instance.StartLoadGame();

        HandleEat();
        UpdateHud();

        m_planeController.SpeedAdd = m_stats.speedRating * m_speedAddPerRating;
    }

    private string ColorText( string a_text, string a_color ) {
        return $"<color={a_color}>{a_text}</color>";
    }

    private void UpdateHud() {
        var foodLine = $"Food: {( m_food >= 0 ? m_food : 0 )}/{m_stats.foodCapacity}\n";
        if( m_food < Mathf.CeilToInt( m_food * m_lowFoodWarningPercent ) )
            foodLine = ColorText( foodLine, "red" );

        var passengerLine = $"Passengers: {PassengerCount}/{m_stats.personCapacity}";
        if ( PassengerCount == m_stats.personCapacity ) passengerLine = ColorText( passengerLine, "red" ); 

        m_statDisplay.text = $"{PersonGenerator.instance.TimeStr}\nSpeed: {m_stats.speedRating}\n"
            + foodLine
            + passengerLine;
    }

    private int m_lastEatHour = 0;

    private bool ShouldEat(int a_hour ) {
        return m_lastEatHour != a_hour && PersonGenerator.instance.Hour == a_hour;
    }

    private void HandleEat() {
        if( ShouldEat(m_breakfastHour ) || ShouldEat(m_lunchHour ) || ShouldEat(m_dinnerHour) ) {
            m_lastEatHour = PersonGenerator.instance.Hour;
            m_food -= m_passengerList.Count + 1;

            if ( m_food <= m_lowFoodWarningPercent * m_stats.foodCapacity )
                PersonGenerator.instance.PlaySound( m_lowFoodSound );
        }

        var loopCount = 0;
        while( m_food < 0 ) {
            if ( m_passengerList.Count == 0 ) break;
            var targetPassenger = RemovePassenger();
            Console.instance.WriteMessage( 
                $"Out of food! Killed {targetPassenger.name} ({targetPassenger.age}) to save the others.", Color.red );

            m_food += m_foodFromCorpse;

            ++loopCount;
            if ( loopCount > 1000000 ) return;
        }

        if ( m_food < 0 ) {
            Console.instance.WriteMessage( $"No food and no passengers! You die.", Color.red );
            m_planeController.IsCrashing = true;
        }
    }
}
