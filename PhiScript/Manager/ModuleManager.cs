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
    }
}