using PhiScript;
using PhiScript.Event;
using Planetbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PermissionLandingPadButton
{
    public class ButtonMod : Mod
    {
        public override void Init()
        {
            Phi.Instance.GuiManager.GuiCreationEvent += this.OnGuiCreation;
        }

        public void OnGuiCreation(object sender, EventGui e)
        {
            if (e.Type == EventGui.GuiType.Action)
            {
                // We check if the selected module is a landing pad
                Selectable selected = Phi.Instance.GetSelection();
                if (selected is Module)
                {
                    Module module = (Module) selected;
                    if (Phi.Instance.ModuleManager.GetModuleType(module) is ModuleTypeLandingPad)
                    {
                        // The player has selected a Landing Pad, we add the button !
                        // First, we need to retrieve the callback that opens
                        // the GuiLandingPermissions when we click the button
                        GameStateGame gameState = Phi.Instance.GetGameManager().getGameState() as GameStateGame;
                        GuiDefinitions.Callback callback = gameState.toggleWindow<GuiLandingPermissions>;

                        // We now add the button
                        e.GuiMenu.addItem(
                            new GuiMenuItem(
                                ResourceList.getInstance().Icons.LandingPermissions,
                                StringList.get("landing_permissions"),
                                callback
                            )
                        );
                    }
                }
            }
        }
    }
}
