using Planetbase;
using System;

namespace PhiScript.Event
{
    public class EventGui : EventArgs
    {
        public enum GuiType
        {
            BaseManagement = 1,
            Build = 2,
            BuildInterior = 3,
            BuildExterior = 4,
            Speed = 5,
            Action = 6
        }

        public GuiMenu GuiMenu
        {
            get;
        }

        public GuiType Type
        {
            get;
        }

        public EventGui(GuiMenu guiMenu, GuiType guiType)
        {
            this.GuiMenu = guiMenu;
            this.Type = guiType;
        }
    }
}
