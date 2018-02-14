using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Config.Reader;

namespace ToastysCruiseControlCSharpClient
{
    public class ToastysCruiseControlClient : BaseScript
    {
        private bool cruiseControl = false;
        private float cruiseSpeed, vehRPM;
        private int toggleCruiseControlKey;
        private VehicleClass vehClass;
        private Ped LocalPed => Game.PlayerPed;
        private Vehicle LocalVehicle => LocalPed.CurrentVehicle;
        private iniconfig config = new iniconfig("ToastysCruiseControl", "ToastysCruiseControlConfig.ini");

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

        private bool IsKeyJustPressed(InputGroups inputGroups, Controls control) => Game.IsControlJustPressed((int)inputGroups, (Control)control);
        private bool IsKeyPressed(InputGroups inputGroups, Controls control) => Game.IsControlPressed((int)inputGroups, (Control)control);

        public ToastysCruiseControlClient()
        {
            toggleCruiseControlKey = config.GetIntValue("keybinds", "togglecruisecontrol", 168);
            API.DisableControlAction(0, toggleCruiseControlKey, true);
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

                if (LocalPed.CurrentVehicle != null)
                {
                    vehClass = LocalPed.CurrentVehicle.ClassType;
                    cruiseSpeed = LocalVehicle.Speed;
                    vehRPM = LocalVehicle.CurrentRPM;
                }

                if ((API.IsControlJustPressed(0, toggleCruiseControlKey) ||
                    (IsKeyJustPressed(InputGroups.CONTROLLER_DPAD_UP, Controls.CONRTOLLER_DPAD_UP) && IsKeyJustPressed(InputGroups.CONTROLLER_X, Controls.CONTROLLER_X))) &&
                    LocalVehicle != null && LocalVehicle.Driver == LocalPed && cruiseSpeed * 2.23694 + 0.5 > 20 && cruiseSpeed * 2.23694 + 0.5 < 150 && vehClassesWithoutCruiseControl.IndexOf(vehClass) == -1)
                {
                    if (!cruiseControl)
                    {
                        Debug.Write($"[TOASTYSCRUISECONTROL] - KEYBIND: { toggleCruiseControlKey.ToString() }.");
                        SetSpeed();
                        cruiseControl = true;
                    }
                    else cruiseControl = false;
                }

                if (cruiseControl && (LocalVehicle == null || LocalVehicle.IsInWater || !LocalVehicle.IsEngineRunning || LocalVehicle.Driver != LocalPed || LocalVehicle.IsInAir ||
                    LocalVehicle.HasCollided || LocalVehicle.SteeringScale >= 0.675f || LocalVehicle.SteeringScale <= -0.675f))
                {
                    cruiseControl = false;
                }

                if (cruiseControl)
                {
                    if (IsKeyPressed(InputGroups.W, Controls.W))
                    {
                        cruiseControl = false;
                        AcceleratingToNewSpeed();
                    }

                    if (IsKeyJustPressed(InputGroups.S, Controls.S) || cruiseSpeed * 2.23694 + 0.5 < 20 || cruiseSpeed * 2.23694 + 0.5 > 150) cruiseControl = false;
                }
            }
        }

        private async void SetSpeed()
        {
            while (true)
            {
                await Delay(0);

                if (cruiseControl)
                {
                    LocalVehicle.Speed = cruiseSpeed;
                    LocalVehicle.CurrentRPM = vehRPM;
                }
                else break;
            }
        }

        private async void AcceleratingToNewSpeed()
        {
            while (IsKeyPressed(InputGroups.W, Controls.W))
            {
                await Delay(0);
                continue;
            }

            cruiseControl = true;
            SetSpeed();
        }
    }
}