using PhiScript.Event;
using Planetbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PhiScript.Game
{
    public class PhiGame
    {
        public static PhiGame Instance
        {
            get;
            set;
        }

        public event EventHandler TickEvent;
        public event EventHandler<EventGui> GuiCreationEvent;

        public PhiGame()
        {
            if (PhiGame.Instance != null)
            {
                PhiGame.Instance = this;
            }
        }

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

        /**
         * Those methods most be called from Assembly-CSharp at specific locations (refer to Modifications.txt)
         */

        public void OnTick()
        {
            this.TickEvent(this, new EventArgs());
        }

        public void OnGuiCreation(GuiMenu guiMenu, int guiTypeCode)
        {
            this.GuiCreationEvent(this, new EventGui(guiMenu, (EventGui.GuiType)guiTypeCode));
        }

    }
}
