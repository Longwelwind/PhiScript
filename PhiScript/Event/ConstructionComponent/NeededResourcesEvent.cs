using System;
using System.Collections.Generic;
using Planetbase;

namespace PhiScript.Event.ConstructionComponent
{
    public class NeededResourcesEvent : EventArgs
    {
        public NeededResourcesEvent(Planetbase.ConstructionComponent constructionComponent)
        {
            ConstructionComponent = constructionComponent;
            ResourceTypes = null;
        }

        public Planetbase.ConstructionComponent ConstructionComponent { get; private set; }

        public List<ResourceType> ResourceTypes { get; set; }
    }
}