using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public interface IModule
        {
            /// <summary>
            /// Called once after the module is created
            /// </summary>
            /// <returns>true if initialization was successful</returns>
            bool Initialize(MyGridProgram program);
            /// <summary>
            /// Called when it's time for a module
            /// </summary>
            /// <param name="argument">The argument the program was ran with</param>
            /// <param name="updateSource">The source of the update</param>
            /// <param name="program">The program calling the module</param>
            /// <returns>true if the processing is incomplete and needs to be called next tick again</returns>
            IEnumerator<bool> Process(string argument, UpdateType updateSource, MyGridProgram program);

            bool IsEnabled { get; set; }
        }
    }
}
