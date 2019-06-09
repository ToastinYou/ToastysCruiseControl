using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Config.Reader;

namespace Client
{
    public class Main : BaseScript
    {
        private bool _cruising;
        private float _rpm;
        private readonly Control _cruiseKey;
        private readonly iniconfig _config = new iniconfig(API.GetCurrentResourceName(), "Config.ini");

        public Main()
        {
            _cruiseKey = (Control)_config.GetIntValue("keybinds", "toggle", 168);

            Debug.WriteLine($"{Common.Prefix} Cruise key: {(int) _cruiseKey}");
            Debug.WriteLine($"{Common.Prefix} Resource name: {API.GetCurrentResourceName()}");
        }

        private List<VehicleClass> _vehClassesWithoutCruiseControl = new List<VehicleClass>
        {
            VehicleClass.Cycles,
            VehicleClass.Motorcycles,
            VehicleClass.Planes,
            VehicleClass.Helicopters,
            VehicleClass.Boats,
            VehicleClass.Trains
        };

        [Tick]
        private async Task Process()
        {
            Vehicle v = Game.PlayerPed?.CurrentVehicle;

            if (v != null)
            {
                Game.DisableControlThisFrame(0, _cruiseKey);

                if ((Game.IsDisabledControlJustReleased(0, _cruiseKey) || Game.IsControlJustReleased(0, _cruiseKey)) &&
                    v.CurrentGear != 0 && !_vehClassesWithoutCruiseControl.Contains(v.ClassType)) // current gear of 0 is reverse.
                {
                    _cruising = !_cruising;

                    if (_cruising)
                    {
                        CruiseAtSpeed(v.Speed);
                        _rpm = v.CurrentRPM;
                    }
                }
            }
            else if (_cruising)
            {
                _cruising = false;
            }
            else
            { // Not in a veh, check periodically.
                await Delay(100);
            }
        }

        private async void CruiseAtSpeed(float s)
        {
            while (_cruising)
            {
                Vehicle v = Game.PlayerPed?.CurrentVehicle;

                if (v != null)
                {
                    v.Speed = s; // try: void SetDriveTaskCruiseSpeed(int /* Ped */ driver, float cruiseSpeed);
                    v.CurrentRPM = _rpm;

                    if (v.Driver == null || v.Driver != Game.PlayerPed || v.IsInWater || v.IsInBurnout || !v.IsEngineRunning || 
                        v.IsInAir || v.HasCollided ||
                        GTASpeedToMPH(v.Speed) <= 25f || GTASpeedToMPH(v.Speed) >= 100f ||
                        HaveAnyTiresBurst(v) ||
                        Game.IsControlPressed(0, Control.VehicleHandbrake) || Game.IsDisabledControlPressed(0, Control.VehicleHandbrake) ||
                        Game.IsControlPressed(0, Control.VehicleBrake) || Game.IsDisabledControlPressed(0, Control.VehicleBrake))
                    { // Disable cruise if any of these.... ^
                        _cruising = false;
                    }

                    if (Game.IsControlPressed(0, Control.VehicleAccelerate) ||
                        Game.IsDisabledControlPressed(0, Control.VehicleAccelerate))
                    { // Accelerating to new speed..
                        AcceleratingToNewSpeed();
                    }
                }
                else
                {
                    return;
                }

                await Delay(0);
            }
        }

        private async void AcceleratingToNewSpeed()
        {
            _cruising = false;

            while ((Game.IsControlPressed(0, Control.VehicleAccelerate) ||
                   Game.IsDisabledControlPressed(0, Control.VehicleAccelerate)) &&
                   Game.PlayerPed.CurrentVehicle != null)
            {
                await Delay(100); // Wait for client to stop accelerating.
            }

            if (Game.PlayerPed.CurrentVehicle != null)
            {
                _cruising = true;
                CruiseAtSpeed(Game.PlayerPed.CurrentVehicle.Speed); // Cruise at new speed.
            }
        }

        /// <summary>
        /// Converts GTA's speeds (meters per second) to MPH.
        /// </summary>
        private float GTASpeedToMPH(float s)
        {
            return s * 2.23694f + 0.5f;
        }

        // Index of tires: 0, 1, 2, 3, 4, 5, 45, 47.
        private bool HaveAnyTiresBurst(Vehicle v)
        {
            List<bool> tiresBurst = new List<bool>();

            for (int i = 0; i < 48; i++)
            {
                if (i == 6)
                {
                    i = 45;
                }

                if (i == 46)
                {
                    i = 47;
                }

                tiresBurst.Add(API.IsVehicleTyreBurst(v.Handle, i, false));
            }

            return tiresBurst.Contains(true);
        }
    }
}