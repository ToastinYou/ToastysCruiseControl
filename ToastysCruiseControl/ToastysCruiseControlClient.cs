using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Config.Reader;

namespace ToastysCruiseControl
{
    public class ToastysCruiseControlClient : BaseScript
    {
        private bool _cruiseControl;
        private float _cruiseSpeed, _vehRpm;
        private readonly iniconfig _config = new iniconfig("ToastysCruiseControl", "ToastysCruiseControlConfig.ini");
        private readonly int _toggleCruiseControlKey;
        private VehicleClass _vehClass;
        private static Ped LocalPed => Game.PlayerPed;
        private static Vehicle LocalVehicle => LocalPed.CurrentVehicle;

        private enum InputGroups
        {
            CONTROLLER_DPAD_UP = 0,
            CONTROLLER_X = 0,
            W = 27,
            S = 27
        }

        private enum Controls
        {
            CONRTOLLER_DPAD_UP = 27,
            CONTROLLER_X = 99,
            W = 71,
            S = 72
        }

        private static bool IsKeyJustPressed(InputGroups inputGroups, Controls control) => Game.IsControlJustPressed((int)inputGroups, (Control)control);
        private static bool IsKeyPressed(InputGroups inputGroups, Controls control) => Game.IsControlPressed((int)inputGroups, (Control)control);

        public ToastysCruiseControlClient()
        {
            _toggleCruiseControlKey = _config.GetIntValue("keybinds", "togglecruisecontrol", 168);
            API.DisableControlAction(0, _toggleCruiseControlKey, true);
            Main();
        }

        private async void Main()
        {
            List<VehicleClass> vehClassesWithoutCruiseControl = new List<VehicleClass>
            {
                VehicleClass.Cycles,
                VehicleClass.Motorcycles,
                VehicleClass.Planes,
                VehicleClass.Helicopters,
                VehicleClass.Boats,
                VehicleClass.Trains
            };

            while (true)
            {
                await Delay(0);

                if (LocalVehicle == null) continue;
                _vehClass = LocalVehicle.ClassType;
                _cruiseSpeed = LocalVehicle.Speed;
                _vehRpm = LocalVehicle.CurrentRPM;

                if (LocalVehicle.IsInWater || !LocalVehicle.IsEngineRunning || LocalVehicle.Driver != LocalPed || LocalVehicle.IsInAir || LocalVehicle.HasCollided ||
                    LocalVehicle.SteeringScale >= 0.675f || LocalVehicle.SteeringScale <= -0.675f || IsKeyJustPressed(InputGroups.S, Controls.S) || _cruiseSpeed * 2.23694 + 0.5 < 20 ||
                    _cruiseSpeed * 2.23694 + 0.5 > 150 || vehClassesWithoutCruiseControl.IndexOf(_vehClass) != -1)
                {
                    _cruiseControl = false;
                    continue;
                }

                if (API.IsControlJustPressed(0, _toggleCruiseControlKey) ||
                    IsKeyJustPressed(InputGroups.CONTROLLER_DPAD_UP, Controls.CONRTOLLER_DPAD_UP) &&
                    IsKeyJustPressed(InputGroups.CONTROLLER_X, Controls.CONTROLLER_X))
                {
                    if (!_cruiseControl)
                    {
                        Debug.Write($"[TOASTYSCRUISECONTROL] - KEYBIND: { _toggleCruiseControlKey }.");
                        SetSpeed();
                        _cruiseControl = true;
                    }
                    else _cruiseControl = false;
                }
                
                if (_cruiseControl && IsKeyPressed(InputGroups.W, Controls.W))
                {
                    _cruiseControl = false;
                    AcceleratingToNewSpeed();
                }
            }
        }

        private async void SetSpeed()
        {
            while (true)
            {
                await Delay(0);
                
                if (!_cruiseControl) break;
                LocalVehicle.Speed = _cruiseSpeed;
                LocalVehicle.CurrentRPM = _vehRpm;
            }
        }

        private async void AcceleratingToNewSpeed()
        {
            while (IsKeyPressed(InputGroups.W, Controls.W))
            {
                await Delay(0);
            }

            _cruiseControl = true;
            SetSpeed();
        }
    }
}