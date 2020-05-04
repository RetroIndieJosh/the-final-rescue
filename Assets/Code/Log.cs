using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
class Entry
{
    public string message = "";
    public float timestamp = 0f;
    public float alpha = 1f;
    public Color color = Color.white;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class Log : MonoBehaviour
{
    [SerializeField] private bool m_showTimestamp = false;
    [SerializeField, Tooltip("0 for always stay")] private float m_entryStayTimeSec = 0f;

    private TextMeshProUGUI m_textMesh = null;
    private List<Entry> m_entryList = new List<Entry>();
    private float m_fadeTime = 0f;

    public void AddEntry( string a_message ) {
        AddEntry( a_message, Color.white );
    }

    public void AddEntry(string a_message, Color a_color ) {
        m_entryList.Add( new Entry() {
            color = a_color,
            message = a_message,
            timestamp = Time.realtimeSinceStartup
        } );
    }

    private void Awake() {
        m_textMesh = GetComponent<TextMeshProUGUI>();
        m_fadeTime = 0.2f * m_entryStayTimeSec;
    }

    private void Update() {
        RemoveTimedOutEntries();

        m_textMesh.text = "";
        foreach ( var entry in m_entryList ) {
            var alphaHex = $"{(int)( entry.alpha * 255f ):X2}";
            m_textMesh.text += $"<color=#{ColorHex( entry.color )}{alphaHex}>";
            if ( m_showTimestamp )
                m_textMesh.text += $"[{entry.timestamp:0.00}] ";
            m_textMesh.text += $"{entry.message}<alpha=#FF>";
        }
    }

    private string ColorHex( Color a_color ) {
        return $"{(int)( a_color.r * 255f ):X2}{(int)( a_color.g * 255f ):X2}{(int)( a_color.b * 255f ):X2}";
    }

    private void RemoveTimedOutEntries() {
        if ( m_entryStayTimeSec < Mathf.Epsilon ) return;

        var toRemove = new List<Entry>();
        foreach( var entry in m_entryList ) {
            var timeDiff = Time.realtimeSinceStartup - entry.timestamp;
            var timeRemaining = m_entryStayTimeSec - timeDiff;
            if ( timeRemaining < Mathf.Epsilon ) {
                toRemove.Add( entry );
                continue;
            }
            if( timeRemaining < m_fadeTime ) 
                entry.alpha = timeRemaining / m_fadeTime;
        }

        foreach ( var entry in toRemove )
            m_entryList.Remove( entry );
    }
}
