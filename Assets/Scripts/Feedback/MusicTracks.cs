using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Feedback
{
    [CreateAssetMenu(menuName = "Ruilay/Audio/Music Tracks")]
    public sealed class MusicTracks : ScriptableObject
    {
        [Header("Looping background music")]
        [SerializeField, InspectorName("Main / World Map")] private AudioClip main;
        [SerializeField] private AudioClip sakuraGarden;
        [SerializeField] private AudioClip bambooWorkshop;
        [SerializeField] private AudioClip moonShrine;
        public AudioClip Main => main;
        public AudioClip ForWorld(WorldId world) => world switch
        {
            WorldId.SakuraGarden => sakuraGarden,
            WorldId.BambooWorkshop => bambooWorkshop,
            WorldId.MoonShrine => moonShrine,
            _ => null
        };
    }
}
