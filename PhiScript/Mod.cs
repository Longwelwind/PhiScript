using Planetbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PhiScript
{
    public class Mod
    {
        public void Init()
        {
            TypeList<Tech, TechList>.get().Add(new MyNewTech());
            
        }

        public void Update()
        {
            Singleton<MessageLog>.getInstance().addMessage(new Message("Is the mode installed ??", TypeList<ModuleType, ModuleTypeList>.find<ModuleTypeLab>().getIcon(), 1));
        }
    }

    public class MyNewTech : Tech
    {
        public MyNewTech()
        {
            //base.load();
            this.mValue = 400;
            this.mMerchantCategory = MerchantCategory.Electronics;

            this.mName = "Test";
            this.mDescription = "Je suis une recherche !";
            this.mIcon = ResourceUtil.loadIconColor("Techs/icon_colossal_panel", Color.cyan);


        }
    }
}
