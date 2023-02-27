using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private Dictionary<string, List<IMyDoor>> _airlocks;
        private readonly List<IMyDoor> _allDoors;

        public Program()
        {
            _allDoors = new List<IMyDoor>(20);
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & UpdateType.Terminal) == UpdateType.Terminal)
            {
                GridTerminalSystem.GetBlocksOfType(_allDoors);

                _airlocks = new Dictionary<string, List<IMyDoor>>();

                foreach (var door in _allDoors)
                {
                    if (!door.IsSameConstructAs(Me))
                    {
                        continue;
                    }

                    List<IMyDoor> currentAirlock;
                    if (_airlocks.TryGetValue(door.CustomName, out currentAirlock))
                    {
                        currentAirlock.Add(door);
                    }
                    else
                    {
                        _airlocks.Add(door.CustomName, new List<IMyDoor>() { door });
                    }
                }

                _airlocks = _airlocks.Where(a => a.Value.Count > 1).ToDictionary(i => i.Key, i => i.Value);

                if (_airlocks.Count == 0)
                {
                    Echo("No airlocks found!");
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                }
                else
                {
                    Echo($"Initialization successful! Found {_airlocks.Count} airlocks!");
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                }

                return;
            }

            foreach (var airlock in _airlocks)
            {
                var doors = airlock.Value;

                bool foundOpen = false;
                IMyDoor openDoor = null;

                foreach (var door in doors)
                {
                    if (door.Status != DoorStatus.Closed)
                    {
                        if (foundOpen)
                        {
                            Echo("Multiple open doors found, fixing :(");
                            door.Enabled = true;
                            door.CloseDoor();
                            // We slow the updates, this is a bit hacky but should only occur when the script is first ran with multiple open doors
                            Runtime.UpdateFrequency = UpdateFrequency.Update100;
                            return;
                        }
                        else
                        {
                            openDoor = door;
                        }
                        foundOpen = true;
                    }
                }

                

                // No open door found, we just enable all doors
                if (!foundOpen)
                {
                    foreach (var door in doors)
                    {
                        door.Enabled = true;
                    }
                    continue;
                }

                foreach (var door in doors)
                {
                    if (door == openDoor)
                    {
                        continue;
                    }

                    door.Enabled = false;
                }

            }

            if (Runtime.UpdateFrequency == UpdateFrequency.Update100)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                Echo("Airlocks fixed!");
            }
        }
    }
}