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

        public void OnTick()
        {
            this.TickEvent(this, new EventArgs());
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

            Singleton<MessageLog>.getInstance().addMessage(new Message("Is the mode installed ??", TypeList<ModuleType, ModuleTypeList>.find<ModuleTypeLab>().getIcon(), 1));

            this.GuiCreationEvent(this, new EventGui(null));
        }

        public void AddMessage(Message message)
        {
            Singleton<MessageLog>.getInstance().addMessage(message);
        }

    }
}
