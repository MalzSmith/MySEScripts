using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Game.GUI.TextPanel;


namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const float PowerLevelTolerance = 20f;

        private IEnumerator<bool> _mainTask;
        private readonly List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>(20);
        private readonly List<IMyGasTank> _gasTanks = new List<IMyGasTank>(20);
        private readonly List<IMyShipConnector> _connectors = new List<IMyShipConnector>(10);
        private readonly List<IMyPowerProducer> _generators = new List<IMyPowerProducer>(10);
        private readonly List<IMyTextSurfaceProvider> _surfaceProviders = new List<IMyTextSurfaceProvider>(20);
        private readonly List<IMyTextSurface> _textSurfaces = new List<IMyTextSurface>(20);

        private bool _connected;

        private float _batteryCapacity;
        private float _batteryCharge;
        private float _batteryPercentage;

        private IMyBatteryBlock _backupBattery;


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Configure();
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
            if (_mainTask == null)
            {
                _mainTask = Iterate();
            }

            if (!_mainTask.MoveNext())
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            else
            {
                PrintStatus();
            }
        }

        private IEnumerator<bool> Iterate()
        {
            while (true)
            {
                // Get blocks
                GridTerminalSystem.GetBlocksOfType(_batteries, t => t.Enabled && Me.IsSameConstructAs(t));
                GridTerminalSystem.GetBlocksOfType(_gasTanks, t => t.Enabled && Me.IsSameConstructAs(t));
                GridTerminalSystem.GetBlocksOfType(_connectors, t => t.Enabled && Me.IsSameConstructAs(t));
                GridTerminalSystem.GetBlocksOfType(_generators,
                    t => !(t is IMyBatteryBlock) && t.Enabled && Me.IsSameConstructAs(t));
                GridTerminalSystem.GetBlocksOfType(_surfaceProviders, t =>
                {
                    var x = t as IMyTerminalBlock;
                    return x != null && Me.IsSameConstructAs(x);
                });


                yield return true;

                // Check if connected

                _connected = _connectors.Any(c => c.Status == MyShipConnectorStatus.Connected);

                // Set Batteries

                _batteries.Sort((b1, b2) => (int)(b1.CurrentStoredPower - b2.CurrentStoredPower));

                if (_batteries.Count > 1)
                {
                    _backupBattery = _batteries.Last();
                }
                else
                {
                    _backupBattery = null;
                }

                _batteryCharge = 0;
                _batteryCapacity = 0;

                foreach (var b in _batteries)
                {
                    _batteryCapacity += b.MaxStoredPower;
                    _batteryCharge += b.CurrentStoredPower;
                    b.ChargeMode = _connected && b != _backupBattery ? ChargeMode.Recharge : ChargeMode.Auto;
                }

                if (_batteryCapacity > 0)
                {
                    _batteryPercentage = (_batteryCharge / _batteryCapacity) * 100;
                }

                // Set Tanks

                _gasTanks.ForEach(t => t.Stockpile = _connected);

                yield return true;

                // Set Hydrogen engines

                if (!_connected && _batteryPercentage < PowerLevelTolerance)
                {
                    _generators.ForEach(g => g.Enabled = true);
                }
                else
                {
                    _generators.ForEach(g => g.Enabled = false);
                }

                yield return true;

                // Set screens

                _textSurfaces.Clear();

                foreach (var surfaceProvider in _surfaceProviders)
                {
                    var tb = (IMyTerminalBlock)surfaceProvider;
                    if (string.IsNullOrEmpty(tb.CustomData)) continue;

                    var splitData = tb.CustomData.Split('#');
                    if (splitData.Length < 3) continue;
                    for (int i = 1; i < splitData.Length; ++i)
                    {
                        int idx;
                        if (!int.TryParse(splitData[i], out idx)) continue;
                        if (idx < 0 || idx >= surfaceProvider.SurfaceCount) continue;
                        _textSurfaces.Add(surfaceProvider.GetSurface(idx));
                    }
                }

                yield return true;
            }
        }

        private void PrintStatus()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append($"Power: {_batteryPercentage:F2}% ({_batteries.Count} batteries)\n");
            stringBuilder.Append($"{_gasTanks.Count} gas tanks\n");
            stringBuilder.Append($"{_generators.Count} power generators\n");
            stringBuilder.Append($"Last run: {Runtime.LastRunTimeMs}\n");

            var status = stringBuilder.ToString();
            Echo(status);

            foreach (var surface in _textSurfaces)
            {
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
                if (surface.TextureSize.X < 300)
                {
                    surface.FontSize = 3;
                }
                else if (surface.TextureSize.X < 600)
                {
                    surface.FontSize = 6;
                }

                surface.FontSize = 10;

                surface.FontSize = 3;
                surface.WriteText(status);
                surface.WriteText("\nScreen size: " + surface.TextureSize, true);
            }
        }

        private void Configure()
        {
        }
    }
}