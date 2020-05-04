using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MusicLayerRandomizer : MonoBehaviour
{
    [SerializeField] private int m_layerCountMin = 0;
    [SerializeField] private int m_layerCountMax = 5;

    [SerializeField] private float m_layerAddTime = 60f;
    [SerializeField] private float m_layerFadeTime = 1f;

    [SerializeField] private float m_layerVolume = 0.7f;

    private bool m_isAdding = true;
    private float m_timeSinceLastChange = 0f;

    private void Update() {
        m_timeSinceLastChange += Time.deltaTime;
        if( m_timeSinceLastChange >= m_layerAddTime ) {
            m_timeSinceLastChange -= m_layerAddTime;

            if( m_isAdding ) {
                AddLayer();
                if ( MusicLayerManager.instance.PlayingLayerCount >= m_layerCountMax )
                    m_isAdding = false;
            } else {
                RemoveLayer();
                if ( MusicLayerManager.instance.PlayingLayerCount <= m_layerCountMin )
                    m_isAdding = true;
            }
        }
    }

    private void Start() {
        for ( var i = 0; i < m_layerCountMin; ++i )
            AddLayer();
    }

    private void AddLayer() {
        var layer = MusicLayerManager.instance.AddRandomLayer();
        StartCoroutine( FadeIn( layer ) );
    }

    private IEnumerator FadeIn( MusicLayer a_layer ) {
        var timeElapsed = 0f;
        while ( timeElapsed < m_layerFadeTime ) {
            var t = timeElapsed / m_layerFadeTime;
            a_layer.Volume = Mathf.Lerp( 0f, m_layerVolume, t );
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeOut( MusicLayer a_layer ) {
        var timeElapsed = 0f;
        while ( timeElapsed < m_layerFadeTime ) {
            var t = 1f - timeElapsed / m_layerFadeTime;
            a_layer.Volume = Mathf.Lerp( 0f, m_layerVolume, t );
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        MusicLayerManager.instance.RemoveLayer( a_layer.Name );
    }

    private void RemoveLayer() {
        var layer = MusicLayerManager.instance.GetRandomPlayingLayer();
        StartCoroutine( FadeOut( layer ) );
    }
}
