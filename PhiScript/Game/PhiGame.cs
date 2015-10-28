using PhiScript.Event;
using Planetbase;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PhiScript.Game
{
    public class PhiGame
    {
        public static PhiGame _instance;
        public static PhiGame Instance;
        
        public event EventHandler TickEvent;
        public event EventHandler<EventGui> GuiCreationEvent;

        public PhiGame()
        {
            if (PhiGame.Instance == null)
                PhiGame.Instance = this;
            else
                throw new Exception("PhiGame instanced twice");
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
        public static void OnTick()
        {
            if (PhiGame.Instance.TickEvent != null)
                PhiGame.Instance.TickEvent(PhiGame.Instance, new EventArgs());
        }

        public static void OnGuiCreation(GuiMenu guiMenu, int guiTypeCode)
        {
            if (PhiGame.Instance.GuiCreationEvent != null)
                PhiGame.Instance.GuiCreationEvent(PhiGame.Instance, new EventGui(guiMenu, (EventGui.GuiType) guiTypeCode));
        }

    }
}
