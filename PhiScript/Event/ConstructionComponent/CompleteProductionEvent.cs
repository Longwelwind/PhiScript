using System;

namespace PhiScript.Event.ConstructionComponent
{
    public class CompleteProductionEvent : EventArgs
    {
        public CompleteProductionEvent(Planetbase.ConstructionComponent constructionComponent)
        {
            ConstructionComponent = constructionComponent;
            Handled = false;
        }

        public Planetbase.ConstructionComponent ConstructionComponent { get; private set; }

        public bool Handled { get; set; }
    }
}