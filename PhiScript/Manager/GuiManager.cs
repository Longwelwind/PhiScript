using PhiScript.Event;
using Planetbase;
using System;

namespace PhiScript.Manager
{
    public class GuiManager
    {
        /// <summary>
        /// Raised when the game creates a GuiMenu.
        /// </summary>
        public event EventHandler<EventGui> GuiCreationEvent;

        /**
         * Those methods must be called from Assembly-CSharp at specific locations (refer to the PhiPatcher solution)
         */

        public void AddMessage(Message message)
        {
            Singleton<MessageLog>.getInstance().addMessage(message);
        }

        public static void OnGuiCreation(GuiMenu guiMenu, int guiTypeCode)
        {
            if (Phi.Instance.GuiManager.GuiCreationEvent != null)
                Phi.Instance.GuiManager.GuiCreationEvent(Phi.Instance, new EventGui(guiMenu, (EventGui.GuiType)guiTypeCode));
        }
    }
}
