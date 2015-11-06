using Planetbase;

namespace PhiScript.Manager
{
    public class ModuleManager
    {
        public ModuleType GetModuleType(Module module)
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