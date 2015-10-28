using PhiScript;
using PhiScript.Event;
using PhiScript.Game;
using Planetbase;
using System;

namespace PhiScriptExample
{
    public class PhiScriptExample : Mod
    {
        public override void Init()
        {
            PhiGame.Instance.GuiCreationEvent += this.OnGuiCreation;
            PhiGame.Instance.TickEvent += this.OnTick;
        }

        public void OnTick(object sender, EventArgs e)
        {
            Phi.Instance.PhiGame.AddMessage(new Message("Test", TypeList<ModuleType, ModuleTypeList>.find<ModuleTypeLab>().getIcon(), 1));
        }

        public void OnGuiCreation(object sender, EventGui e)
        {
            if (e.Type == EventGui.GuiType.BaseManagement)
            {
                e.GuiMenu.addItem(new GuiMenuItem(ResourceList.getInstance().Icons.Camera, "Test button", null));
            }
        }
    }
}
