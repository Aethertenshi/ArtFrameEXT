using System;

namespace ArtFrameCore.Modules
{
    /// <summary>
    /// A non-visual grouping container that aggregates multiple child elements under a single node.
    /// </summary>
    public class UIGroup : Element
    {
        // UIGroups act as folders or structural containers, meaning they don't render their own backgrounds.
        public UIGroup()
        {
            Width = 0;
            Height = 0;
        }
    }
}
