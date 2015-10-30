using PhiScript;
using PhiScript.Event;
using Planetbase;
using System;

namespace PhiScriptExample
{
    public class PhiScriptExample : Mod
    {
        public override void Init()
        {
            Phi.Instance.GuiManager.GuiCreationEvent += new EventHandler<EventGui>(this.OnGuiCreation);
        }

        public void OnButtonClick(object sender)
        {
            
        }

        public void OnGuiCreation(object sender, EventGui e)
        {
            if (e.Type == EventGui.GuiType.BuildExterior)
            {
                e.GuiMenu.addItem(new GuiMenuItem(ResourceList.getInstance().Icons.BuildExterior, "Test button", new GuiDefinitions.Callback(this.OnButtonClick)));
            }
        }
    }
}
