using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;

[System.Serializable]
public class MusicLayer
{
    public bool IsPlaying { get { return m_source.volume > 0f; } }
    public string Name { get { return m_clip.name; } }

    public void DestroySource() { GameObject.Destroy( m_source ); }
    public void Play() { m_source.volume = 1.0f; }
    public void Stop() { m_source.volume = 0.0f; }

    public float Volume { get { return m_source.volume; } set { m_source.volume = value; } }

    private AudioClip m_clip;
    private AudioSource m_source;

    public MusicLayer( GameObject a_manager,  AudioClip a_clip, AudioMixerGroup a_mixer ) {
        m_source = a_manager.AddComponent<AudioSource>();
        m_clip = a_clip;

        m_source.loop = true;
        m_source.playOnAwake = false;
        m_source.clip = m_clip;
        m_source.volume = 0.0f;
        m_source.outputAudioMixerGroup = a_mixer;
        m_source.Play();

        Stop();
    }
}

[DisallowMultipleComponent]
public class MusicLayerManager : MonoBehaviour
{
    public static MusicLayerManager instance = null;

    [SerializeField] private AudioMixerGroup m_musicMixerGroup = null;
    [SerializeField] private List<Song> m_songList = new List<Song>();

    public Song CurrentSong {
        private get { return m_songList[m_songIndex]; }
        set {
            foreach( var layer in m_layerList ) 
                layer.DestroySource();
            m_layerList.Clear();

            foreach ( var clip in value.ClipList ) {
                var layer = new MusicLayer( gameObject, clip, m_musicMixerGroup );
                m_layerList.Add( layer );
            }
        }
    }
    public string CurrentSongInfo {
        get {
            return string.Format( "{0} ({1} BPM)\n{2}/{3} Layers", CurrentSong.name, CurrentSong.Bpm, PlayingLayerCount, TotalLayerCount );
        }
    }
    public int TotalLayerCount {  get { return m_layerList.Count; } }
    public int PlayingLayerCount {  get { return PlayingLayerList.Count; } }
    public MusicLayer TopLayer {  get { return PlayingLayerCount == 0 ? null : PlayingLayerList[0]; } }

    private List<MusicLayer> PlayingLayerList {
        get { return m_layerList.Where( layer => layer.IsPlaying ).ToList(); }
    }
    private List<MusicLayer> NotPlayingLayerList {
        get { return m_layerList.Where( layer => layer.IsPlaying == false ).ToList(); }
    }

    private List<MusicLayer> m_layerList = new List<MusicLayer>();
    private int m_songIndex = 0;

    public MusicLayer AddLayer( string a_name ) {
        var layer = GetLayer( a_name );
        if ( layer != null ) layer.Play();
        return layer;
    }

    public void AddNextLayer() {
        var notPlayingList = NotPlayingLayerList;
        if ( notPlayingList.Count == 0 ) return;

        AddLayer( notPlayingList[0].Name );
    }

    public MusicLayer AddRandomLayer() {
        var layer = GetRandomNotPlayingLayer();
        if ( layer == null ) return null;
        return AddLayer( layer.Name );
    }

    public MusicLayer GetRandomNotPlayingLayer() {
        var notPlayingList = NotPlayingLayerList;
        var max = notPlayingList.Count;
        if ( max == 0 ) return null;

        var i = Random.Range( 0, max );
        return notPlayingList[i];
    }

    public MusicLayer GetRandomPlayingLayer() {
        var playingList = PlayingLayerList;
        var max = playingList.Count;
        if ( max == 0 ) return null;

        var i = Random.Range( 0, max );
        return playingList[i];
    }

    public void ClearLayers() {
        while ( TopLayer != null )
            RemoveLayer( TopLayer.Name );
    }
    
    public void NextSong() {
        ++m_songIndex;
        if ( m_songIndex >= m_songList.Count )
            m_songIndex = 0;
        CurrentSong = m_songList[m_songIndex];
    }

    public void RemoveLayer( string a_name ) {
        var layer = GetLayer( a_name );
        if ( layer != null ) layer.Stop();
    }

    private void Awake() {
        if( instance != null ) {
            Debug.LogErrorFormat( "Duplicate MusicLayerManager in {0}.", name );
            Destroy( this );
            return;
        }

        instance = this;
    }

    private void Start() {
        CurrentSong = m_songList[0];
    }

    private MusicLayer GetLayer( string a_name ) {
        foreach ( var layer in m_layerList ) {
            if ( layer.Name == a_name )
                return layer;
        }
        return null;
    }
}
