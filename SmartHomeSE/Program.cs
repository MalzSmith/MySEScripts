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

#pragma warning disable IDE0060
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable InconsistentNaming

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const string DefaultConfig = @"# Lines starting with a '#' get ignored in the configuration file
# Enabled modules: if you don't want one of these, set it to false:
HydrogenSaving:true";


        private readonly List<IModule> _modules;
        private readonly IEnumerator<IModule> moduleEnumerator;
        private IEnumerator<bool> currentProcess;


        public Program()
        {
            if (string.IsNullOrEmpty(Me.CustomData))
            {
                Me.CustomData = DefaultConfig;
            }

            // We create the modules used
            _modules = new List<IModule>
            {
                new HydrogenSaving(),
            };

            foreach (var module in _modules)
            {
                module.Initialize(this);
                module.IsEnabled = true;
            }

            moduleEnumerator = LoopOverModules();
        }


        public void Save()
        {
            // This could be used to save the current task we are running but why bother. It will eventually run again
        }


        public void Main(string argument, UpdateType updateSource)
        {
            // If the program was called manually, we initialize the configuration and reset to the initial state
            if ((updateSource & UpdateType.Terminal) == UpdateType.Terminal)
            {
                // TODO load configuration

                Runtime.UpdateFrequency = UpdateFrequency.Update100;

                Echo("Initialization successful!");
            }

            if (currentProcess != null)
            {
                RunCurrentProcess();
            }
            else
            {
                if (moduleEnumerator.MoveNext())
                {
                    currentProcess = moduleEnumerator.Current?.Process(argument, updateSource, this);
                }
                else
                {
                    Echo("State machine terminated!");
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                }
            }
        }

        private IEnumerator<IModule> LoopOverModules()
        {
            while (true)
            {
                var foundActive = false;
                foreach (var module in _modules)
                {
                    if (module.IsEnabled)
                    {
                        foundActive = true;
                        yield return module;
                    }
                }

                if (!foundActive)
                {
                    Echo("No active modules found! Please check your configuration!");
                    yield break;
                }
            }
        }


        public void RunCurrentProcess()
        {
            if (currentProcess == null)
            {
                return;
            }

            var hasMoreSteps = currentProcess.MoveNext();

            if (hasMoreSteps)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
            }
            else
            {
                currentProcess.Dispose();
                currentProcess = null;
            }
        }
    }
}