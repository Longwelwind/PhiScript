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

        public List<Mod> Mods = new List<Mod>();

        /// <summary>
        /// Raised the game processes a tick, even outside of the game (i.e. in menu).
        /// </summary>
        public event EventHandler TickEvent;

        public Phi()
        {
        }

        public static T GetPrivateField<T>(object obj, string field)
        {
            return (T) obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }

        public static void SetPrivateField(object obj, string field, object value)
        {
            obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        }

        public static T GetPrivateStaticField<T>(Type type, string field)
        {
            return (T) type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        }

        public void Launch()
        {
            Console.Write("test");
            return;

            var modsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Planetbase\\PhiMods";
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
            }

            string[] modsPaths = Directory.GetFiles(modsFolder, "*.dll");

            foreach (string modPath in modsPaths)
            {
                Assembly asm = Assembly.LoadFile(modPath);

                // We're looking for the first class that inherits from Mod
                Type modClass = asm.GetTypes().FirstOrDefault(type => type.IsSubclassOf(typeof(Mod)));

                if (modClass != null)
                {
                    Mod mod = (Mod)Activator.CreateInstance(modClass);

                    Mods.Add(mod);
                }
            }

            foreach (Mod mod in Mods)
            {
                mod.Init();
            }
        }

        /**
         * Those methods must be called from Assembly-CSharp at specific locations (refer to the PhiPatcher solution)
         */
        public static void OnTick()
        {
            Instance.TickEvent?.Invoke(Instance, new EventArgs());
        }

        public static void StaticLaunch()
        {
            Instance = new Phi();
            Instance.Launch();
        }
    }

    public abstract class Mod
    {
        public abstract void Init();
    }

    
}
