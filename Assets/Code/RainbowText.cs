using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class RainbowText : MonoBehaviour
{
    [SerializeField] private float m_changeDelayTime = 0.5f;

    private Color[] m_colorArr = {
        Color.red,
        new Color(1f, .65f, 0f),
        Color.yellow,
        Color.green,
        Color.blue,
        new Color( .294f, 0f, .51f),
        new Color( .93f, .51f, .93f )
    };
    private int m_colorIndex;

    private float m_timeSinceLastChange = 0f;
    private TextMeshProUGUI m_textMesh = null;

    private void Awake() {
        m_textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Update() {
        m_timeSinceLastChange += Time.unscaledDeltaTime;
        if ( m_timeSinceLastChange < m_changeDelayTime ) return;

        m_timeSinceLastChange = 0f;
        
        var currentColorArr = new Color[4];
        for ( var i = 0; i < 4; ++i ) {
            var index = ( m_colorIndex + i ) % m_colorArr.Length;
            currentColorArr[i] = m_colorArr[index];
        }
        //m_textMesh.color = m_colorArr[m_colorIndex];
        m_textMesh.colorGradient 
            = new VertexGradient( currentColorArr[0], currentColorArr[1], currentColorArr[2], currentColorArr[3] );

        ++m_colorIndex;
    }
}
