using Planetbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhiScript.Event
{
    public class EventGui : EventArgs
    {
        public enum GuiType
        {
            BaseManagement,
            Build,
            BuildInterior,
            BuildExterior
        }

        public GuiMenu GuiMenu
        {
            get;
        }

        public GuiType Type;

        public EventGui(GuiMenu guiMenu, GuiType guiType)
        {
            this.GuiMenu = guiMenu;
            this.Type = guiType;
        }
    }
}
