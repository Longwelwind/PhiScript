using System;

namespace PhiScript.Event.ConstructionComponent
{
    public class CanProduceEvent : EventArgs
    {
        public CanProduceEvent(Planetbase.ConstructionComponent constructionComponent)
        {
            ConstructionComponent = constructionComponent;
            CanProduce = null;
        }

        public Planetbase.ConstructionComponent ConstructionComponent { get; private set; }

        public bool? CanProduce { get; set; }
    }
}
