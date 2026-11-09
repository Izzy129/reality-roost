using RealityRoost.Shared.HapticFloor;
using UnityEngine;

namespace RealityRoost.WorldRail
{
    [AddComponentMenu("Reality Roost/Examples/Train Rumble Effect")]
    public sealed class TrainRumbleEffect : MonoBehaviour
    {
        [SerializeField] private WorldRailTunnelJourney train;
        [SerializeField] private RRFloorPlayer floorPlayer;

        private float defaultSpeed;
        private bool isPlaying;
        private RRFloorService service;

        private void OnEnable()
        {
            if (train == null || floorPlayer == null) return;
            if (defaultSpeed == 0) defaultSpeed = train.speed;
            train.SpeedChanged += OnSpeedChanged;
            service = floorPlayer.floorService;
            if (service == null) return;
            service.ReadyChanged += OnReadyChanged;
            OnReadyChanged(service.IsReady);
        }

        private void OnReadyChanged(bool ready)
        {
            if (ready && train != null) OnSpeedChanged(train.speed);
            else isPlaying = false;
        }

        private void OnSpeedChanged(float speed)
        {
            if (floorPlayer == null || !floorPlayer.CanPlay || service == null || !service.IsReady) return;

            if (speed > 0)
            {
                if (defaultSpeed == 0) defaultSpeed = speed;
                floorPlayer.SetPlaybackRate(speed / defaultSpeed);
                if (!isPlaying)
                {
                    isPlaying = true;
                    floorPlayer.BeginPlay();
                }
            }
            else
            {
                StopSound();
            }
        }

        private void StopSound()
        {
            if (!isPlaying) return;
            isPlaying = false;
            if (floorPlayer != null) floorPlayer.Stop();
        }

        private void OnDisable()
        {
            if (train != null) train.SpeedChanged -= OnSpeedChanged;
            if (service != null) service.ReadyChanged -= OnReadyChanged;
            StopSound();
        }
    }
}
