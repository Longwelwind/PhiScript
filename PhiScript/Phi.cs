using PhiScript.Event;
using Planetbase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PhiScript
{
    public class Phi
    {
        public static Phi Instance;

        private List<Mod> Mods = new List<Mod>();

        /// <summary>
        /// Raised the game processes a tick, even outside of the game (i.e. in menu).
        /// </summary>
        public event EventHandler TickEvent;

        /// <summary>
        /// Raised when the game creates a GuiMenu.
        /// </summary>
        public event EventHandler<EventGui> GuiCreationEvent;

        public List<ResourceType> GetResourceTypes()
        {
            return ResourceTypeList.get();
        }

        public void AddResourceType(ResourceType resourceType)
        {
            ResourceTypeList resourceTypeList = ResourceTypeList.getInstance();
            MethodInfo method = resourceTypeList.GetType().GetMethod("addResource", BindingFlags.NonPublic | BindingFlags.Instance);

            method.Invoke(resourceTypeList, new object[] { resourceType });
        }

        public void AddMessage(Message message)
        {
            Singleton<MessageLog>.getInstance().addMessage(message);
        }

        public void Launch()
        {
            var modsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Planetbase\\ModsPhiScript";
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
            }

            string[] modsPaths = Directory.GetFiles(modsFolder, "*.dll");

            foreach (string modPath in modsPaths)
            {
                System.Console.WriteLine(modPath);
                Assembly asm = Assembly.LoadFile(modPath);

                // We're looking for the first class that inherits from Mod
                Type modClass = asm.GetTypes().FirstOrDefault(type => type.IsSubclassOf(typeof(Mod)));

                if (modClass != null)
                {
                    Mod mod = (Mod)Activator.CreateInstance(modClass);

                    this.Mods.Add(mod);
                }
                else
                {
                    // No Mod class was found for this .dll
                    // Error display ?
                }
            }

            foreach (Mod mod in this.Mods)
            {
                mod.Init();
            }
        }

        /**
         * Those methods must be called from Assembly-CSharp at specific locations (refer to the PhiPatcher solution)
         */
        public static void OnTick()
        {
            if (Phi.Instance.TickEvent != null)
                Phi.Instance.TickEvent(Phi.Instance, new EventArgs());
        }

        public static void OnGuiCreation(GuiMenu guiMenu, int guiTypeCode)
        {
            if (Phi.Instance.GuiCreationEvent != null)
                Phi.Instance.GuiCreationEvent(Phi.Instance, new EventGui(guiMenu, (EventGui.GuiType)guiTypeCode));
        }

        public static void StaticLaunch()
        {
            Phi.Instance = new Phi();

            Phi.Instance.Launch();
        }
    }

    public abstract class Mod
    {
        public abstract void Init();
    }

    
}
