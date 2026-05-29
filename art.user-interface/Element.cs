using System;
using System.Collections;
using System.Collections.Generic;
using ArtFrameCore.SdlBindings;
using ArtFrameCore.DataType;

namespace ArtFrameCore.UserInterface
{
    /// <summary>
    /// Base class representing a node in the UI hierarchy tree.
    /// Provides declarative initialization and parent-relative positioning.
    /// </summary>
    public class Element : IEnumerable<Element>
    {
        public string Name { get; set; } = string.Empty;

        // Roblox UDim2-style Position
        public Position2D Position { get; set; } = new Position2D(0, 0, 0, 0);

        // Backward compatibility properties mapping to Position.XOffset/YOffset
        public float X
        {
            get => Position.XOffset;
            set => Position = new Position2D(Position.XScale, Position.YScale, value, Position.YOffset);
        }

        public float Y
        {
            get => Position.YOffset;
            set => Position = new Position2D(Position.XScale, Position.YScale, Position.XOffset, value);
        }

        // Roblox UDim2-style Size
        public Size2D Size { get; set; } = new Size2D(0, 0, 0, 0);

        // Backward compatibility properties mapping to Size.XOffset/YOffset
        public float Width
        {
            get => Size.XOffset;
            set => Size = new Size2D(Size.XScale, Size.YScale, value, Size.YOffset);
        }

        public float Height
        {
            get => Size.YOffset;
            set => Size = new Size2D(Size.XScale, Size.YScale, Size.XOffset, value);
        }
        
        public SDL_FColor Color { get; set; } = new SDL_FColor(1.0f, 1.0f, 1.0f, 1.0f);
        
        public Element? Parent { get; protected internal set; }

        // Real-time custom update delegate executed frame-by-frame
        public Action<Element>? UpdateAction { get; set; }

        // Backing list for storing children
        internal readonly List<Element> _children = new List<Element>();

        // Public helper collection supporting list and collection initializers!
        private ElementCollection _childrenCollection;
        public ElementCollection Children
        {
            get => _childrenCollection;
            set
            {
                _children.Clear();
                if (value != null)
                {
                    foreach (var child in value)
                    {
                        Add(child);
                    }
                }
            }
        }

        public Element()
        {
            _childrenCollection = new ElementCollection(this);
        }

        /// <summary>
        /// Adds a child element to this node. Sets the parent relationship.
        /// </summary>
        public void Add(Element child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            
            // Auto-generate name if empty
            if (string.IsNullOrEmpty(child.Name))
            {
                child.Name = $"Element_{_children.Count}";
            }

            // Remove from old parent if any
            if (child.Parent != null)
            {
                child.Parent._children.Remove(child);
            }
            
            child.Parent = this;
            _children.Add(child);
        }



        /// <summary>
        /// Executes recursive logical updates for this element and all its children.
        /// </summary>
        public virtual void Update()
        {
            // 1. Run the custom real-time Action delegate if assigned
            UpdateAction?.Invoke(this);

            // 2. Propagate updates to all children recursively
            foreach (var child in Children)
            {
                child.Update();
            }
        }

        /// <summary>
        /// Draws the element and its children recursively, applying parent-relative translation and parent size scaling.
        /// </summary>
        public virtual void Draw(float parentAbsoluteX = 0, float parentAbsoluteY = 0, float parentWidth = 800, float parentHeight = 600)
        {
            // Calculate absolute position on screen
            var (localX, localY) = Position.Calculate(parentWidth, parentHeight);
            float absoluteX = parentAbsoluteX + localX;
            float absoluteY = parentAbsoluteY + localY;

            // Calculate absolute size of this element
            var (absoluteWidth, absoluteHeight) = Size.Calculate(parentWidth, parentHeight);

            // Base rendering of the element if it has dimensions
            if (absoluteWidth > 0 && absoluteHeight > 0)
            {
                Renderer.DrawQuad(absoluteX, absoluteY, absoluteWidth, absoluteHeight, Color);
            }

            // Propagate layout size: if this element has no size (like a UIGroup), pass down parent size
            float currentWidth = absoluteWidth > 0 ? absoluteWidth : parentWidth;
            float currentHeight = absoluteHeight > 0 ? absoluteHeight : parentHeight;

            // Draw all children recursively
            foreach (var child in _children)
            {
                child.Draw(absoluteX, absoluteY, currentWidth, currentHeight);
            }
        }

        // IEnumerable implementation to support declarative C# collection initializers
        public IEnumerator<Element> GetEnumerator() => Children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Helper collection wrapping the Element's children list to enable clean hierarchical and collection initialization.
    /// </summary>
    public class ElementCollection : IEnumerable<Element>
    {
        private readonly Element? _owner;
        private readonly List<Element>? _tempElements;

        /// <summary>
        /// Constructor for the active collection owned by an Element.
        /// </summary>
        public ElementCollection(Element owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Parameterless constructor supporting C# 12+ collection expressions and temporary collection initialization.
        /// </summary>
        public ElementCollection()
        {
            _owner = null;
            _tempElements = new List<Element>();
        }

        public void Add(Element child)
        {
            if (_owner != null)
            {
                _owner.Add(child);
            }
            else
            {
                _tempElements!.Add(child);
            }
        }



        public void AddRange(IEnumerable<Element> elements)
        {
            if (elements == null) return;
            foreach (var element in elements)
            {
                Add(element);
            }
        }

        public void AddRange(params Element[] elements)
        {
            if (elements == null) return;
            foreach (var element in elements)
            {
                Add(element);
            }
        }

        public void Remove(Element child)
        {
            if (_owner != null)
            {
                _owner._children.Remove(child);
            }
            else
            {
                _tempElements!.Remove(child);
            }
        }

        public int Count => _owner != null ? _owner._children.Count : _tempElements!.Count;

        public Element this[int index] => _owner != null ? _owner._children[index] : _tempElements![index];

        public IEnumerator<Element> GetEnumerator()
        {
            return _owner != null ? _owner._children.GetEnumerator() : _tempElements!.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
