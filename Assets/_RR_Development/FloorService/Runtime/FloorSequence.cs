using System;
using UnityEngine;

namespace RealityRoost.Shared.HapticFloor
{
    [Serializable]
    public class FloorSequenceStep
    {
        [Min(0)] public float time;
        public FloorTiles tiles;
        [Range(.01f, 1f)] public float intensity = .25f;
    }

    /// <summary>
    /// Defines a floor preset: which sound to play, which tiles to use, and when each group plays.
    /// </summary>
    [CreateAssetMenu(menuName = "Reality Roost/Floor Sequence", fileName = "Floor Sequence")]
    public sealed class FloorSequence : ScriptableObject
    {
        [Tooltip("Optional override. Leave empty to use Floor Service's AudioClip or Sound Id.")]
        public AudioClip audioClip;
        public bool repeat = true;
        [Min(.01f), Tooltip("Seconds between starts of each pass.")]
        public float cycleDuration = 1.5f;
        public FloorSequenceStep[] steps = {
            new FloorSequenceStep { time = 0, tiles = FloorTiles.Tile1 | FloorTiles.Tile2 },
            new FloorSequenceStep { time = .15f, tiles = FloorTiles.Tile3 | FloorTiles.Tile4 },
            new FloorSequenceStep { time = .30f, tiles = FloorTiles.Tile5 | FloorTiles.Tile6 }
        };

        public void Validate()
        {
            if (steps == null || steps.Length == 0 || steps.Length > 64) throw new ArgumentException("A floor sequence needs 1–64 steps.");
            float previous = -1;
            foreach (var step in steps)
            {
                if (step == null || float.IsNaN(step.time) || float.IsInfinity(step.time) || step.time < 0 || step.time <= previous)
                    throw new ArgumentException("Sequence step times must be finite, nonnegative and strictly increasing. Combine simultaneous tiles in one step.");
                if (step.tiles == FloorTiles.None || (step.tiles & ~FloorTiles.All) != 0 || float.IsNaN(step.intensity) || step.intensity <= 0 || step.intensity > 1)
                    throw new ArgumentException("Each step needs valid tiles and intensity greater than zero up to one.");
                previous = step.time;
            }
            if (float.IsNaN(cycleDuration) || float.IsInfinity(cycleDuration) || cycleDuration <= previous)
                throw new ArgumentException("Cycle Duration must be greater than the last step time.");
        }
    }
}
