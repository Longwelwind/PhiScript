using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PhiScript.Event;
using PhiScript.Event.ConstructionComponent;
using Planetbase;

namespace PhiScript.Manager
{
    public class ConstructionComponentManager
    {
        public event EventHandler<CanProduceEvent> CanProduceEvent;
        public event EventHandler<CompleteProductionEvent> CompleteProductionEvent;
        public event EventHandler<NeededResourcesEvent> NeededResourcesEvent;

        public static bool? OnCanProduce(ConstructionComponent constructionComponent)
        {
            if (Phi.Instance.ConstructionComponentManager.CanProduceEvent != null)
            {
                CanProduceEvent canProduceEvent = new CanProduceEvent(constructionComponent);
                Phi.Instance.ConstructionComponentManager.CanProduceEvent(Phi.Instance, canProduceEvent);

                return canProduceEvent.CanProduce;
            }
            return null;
        }

        public static bool OnCompleteProduction(ConstructionComponent constructionComponent)
        {
            if (Phi.Instance.ConstructionComponentManager.CompleteProductionEvent != null)
            {
                CompleteProductionEvent completeProductionEvent = new CompleteProductionEvent(constructionComponent);
                Phi.Instance.ConstructionComponentManager.CompleteProductionEvent(Phi.Instance, completeProductionEvent);

                return completeProductionEvent.Handled;
            }
            return false;
        }

        public static List<ResourceType> OnNeededResources(ConstructionComponent constructionComponent)
        {
            if (Phi.Instance.ConstructionComponentManager.NeededResourcesEvent != null)
            {
                NeededResourcesEvent neededResourcesEvent = new NeededResourcesEvent(constructionComponent);
                Phi.Instance.ConstructionComponentManager.NeededResourcesEvent(Phi.Instance, neededResourcesEvent);

                return neededResourcesEvent.ResourceTypes;
            }
            return null;
        }
    }
}
