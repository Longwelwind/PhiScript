using Planetbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhiScript.Manager
{
    public class ModuleManager
    {
        public ModuleType GetModuleType(Planetbase.Module module)
        {
            return Phi.GetPrivateField<ModuleType>(module, "mModuleType");
        }

        /**
         * Those methods must be called from Assembly-CSharp at specific locations (refer to the PhiPatcher solution)
         */
        public static void OnModuleBuild(Module module)
        {

        }
    }
}