using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "Song", menuName = "Song", order = 1)]
public class Song: ScriptableObject
{
    [SerializeField] float m_bpm;
    [SerializeField] private List<AudioClip> m_clipList = new List<AudioClip>();

    public List<AudioClip> ClipList {  get { return m_clipList; } }
    public float Bpm {  get { return m_bpm; } }
}
