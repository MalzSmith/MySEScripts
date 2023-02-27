using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Ingame;
using VRage;

namespace IngameScript
{
    partial class Program
    {
        public class HydrogenSaving : IModule
        {
            // Thrusters get turned off after this much time
            private const int MinimumDelayInSeconds = 30;

            // This many thrusters get processed in one tick
            private const int BatchSize = 20;

            // All thrusters
            private readonly List<IMyThrust> _thrusters;

            // Active thrusters with the time they were found to be active
            private readonly Dictionary<IMyThrust, DateTime> _activeThrusters;

            public HydrogenSaving()
            {
                _thrusters = new List<IMyThrust>(50);
                _activeThrusters = new Dictionary<IMyThrust, DateTime>(50);
            }

            public bool Initialize(MyGridProgram program)
            {
                return true;
            }

            public IEnumerator<bool> Process(string argument, UpdateType updateSource, MyGridProgram program)
            {
                if (!IsEnabled)
                {
                    program.Echo("Disabled module was called! This should not happen!");
                    yield break;
                }

                // Collect all thrusters on first tick
                program.GridTerminalSystem.GetBlocksOfType(_thrusters);

                yield return true;

                int counter = 0;

                var currentDateTime = DateTime.Now;
                // Thrusters added before the cutoff time will get removed
                var cutoffTime = currentDateTime.AddSeconds(-MinimumDelayInSeconds);

                // Process them one by one
                foreach (var thruster in _thrusters)
                {
                    // If thruster isn't in the world anymore, we just move on...
                    if (thruster.Closed)
                    {
                        continue;
                    }

                    // We also ignore Non-hydrogen thrusters
                    if (!thruster.BlockDefinition.SubtypeName.EndsWith("HydrogenThrust"))
                    {
                        continue;
                    }

                    // First check if we have reached the batch size limit
                    ++counter;
                    if (counter == BatchSize + 1) // Hopefully this gets optimized by the compiler...
                    {
                        counter = 0;
                        yield return true;
                    }

                    // If the thruster is disabled, we remove it from the dictionary (if it's there), then move on to the next one
                    if (!thruster.Enabled)
                    {
                        _activeThrusters.Remove(thruster);
                        continue;
                    }

                    // If it's enabled, we check if it has a time saved with it.
                    // If it does, we compare it to the cutoff time and disable it if enough time has passed
                    DateTime thrusterDateTime;
                    if (_activeThrusters.TryGetValue(thruster, out thrusterDateTime))
                    {
                        if (thrusterDateTime < cutoffTime)
                        {
                            thruster.Enabled = false;
                            _activeThrusters.Remove(thruster);
                        }

                        continue;
                    }

                    // If not, we add it to the dictionary with the current tim
                    _activeThrusters.Add(thruster, currentDateTime);
                }

                yield return true;

                // As a last step we clear the dictionary of any items that were not contained in the list

                // We get the thrusters not in our list
                // (this creates a new list object but idc)
                var keysToRemove = _activeThrusters.Select(t => t.Key).Where(t => !_thrusters.Contains(t)).ToList();

                yield return true; // Maaaybe this is too much waiting? Anyways, it CAN wait so it probably should.

                // Then we remove the thrusters from the dictionary
                foreach (var thruster in keysToRemove)
                {
                    _activeThrusters.Remove(thruster);
                }
            }

            public bool IsEnabled { get; set; }
        }
    }
}