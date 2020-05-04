using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopTrigger : MonoBehaviour
{
    private void OnTriggerEnter( Collider other ) {
        var passengerPlane = other.GetComponent<PassengerPlane>();
        if ( passengerPlane == null ) return;

        PersonGenerator.instance.StartShop( passengerPlane );
        Time.timeScale = 0f;
    }
}
