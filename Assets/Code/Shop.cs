using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField] private int m_foodPerPerson = 10;

    private PassengerPlane m_targetPlane = null;

    public void StartShop( PassengerPlane a_targetPlane ) {
        m_targetPlane = a_targetPlane;
        gameObject.SetActive( true );
    }

    private void Awake() {
        gameObject.SetActive( false );
    }

    private void Update() {
        if ( Input.GetKeyDown( KeyCode.A ) || Input.GetKeyDown( KeyCode.Joystick1Button0 ) ) {
            if ( m_targetPlane.CanAddFood == false ) {
                Console.instance.WriteMessage( "No more space for food!" );
                return;
            }
            var person = m_targetPlane.RemovePassenger();
            m_targetPlane.AddFood( m_foodPerPerson );
            Console.instance.WriteMessage( $"Traded {person} for {m_foodPerPerson} food" );
        } else if ( Input.GetKeyDown( KeyCode.B ) || Input.GetKeyDown( KeyCode.Joystick1Button1 ) ) {
            var person = m_targetPlane.RemovePassenger();
            m_targetPlane.UpgradeFoodCapacity();
            Console.instance.WriteMessage( $"Traded {person} for food storage upgrade" );
        } else if ( Input.GetKeyDown( KeyCode.X ) || Input.GetKeyDown( KeyCode.Joystick1Button2 ) ) {
            var person = m_targetPlane.RemovePassenger();
            m_targetPlane.UpgradePersonCapacity();
            Console.instance.WriteMessage( $"Traded {person} for passenger space upgrade" );
        } else if ( Input.GetKeyDown( KeyCode.Y ) || Input.GetKeyDown( KeyCode.Joystick1Button3 ) ) {
            var person = m_targetPlane.RemovePassenger();
            m_targetPlane.UpgradeSpeed();
            Console.instance.WriteMessage( $"Traded {person} for speed upgrade" );
        } else if ( Input.GetKeyDown( KeyCode.Escape ) ) {
            EndShop();
        }

        if ( m_targetPlane.PassengerCount == 0 ) EndShop();
    }

    private void EndShop() {
        Time.timeScale = 1f;
        gameObject.SetActive( false );
    }
}
