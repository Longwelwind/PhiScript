using PhiScript.Game;
using Planetbase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PhiScript
{
    public class Phi
    {
        public static Phi Instance
        {
            get;
            set;
        }

        public PhiGame PhiGame {
            get;
        }

        private List<Mod> Mods = new List<Mod>();

        public Phi()
        {
            this.PhiGame = new PhiGame();
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
                Assembly asm = Assembly.LoadFile(modPath);
                
                // We're looking for the first class that inherits from Mod
                foreach (Type type in asm.GetTypes())
                {
                    if (type.IsAssignableFrom(typeof(Mod)))
                    {
                        Mod mod = (Mod) Activator.CreateInstance(type);

                        mod.Init();

                        this.Mods.Add(mod);
                    }
                }

                foreach (Mod mod in this.Mods)
                {
                    mod.Init();
                }
            }
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
