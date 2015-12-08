using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Input;

namespace win2d_text_game_world_generator
{
    public delegate void ClickEventHandler(PointerPoint point);

    public abstract class win2d_Control
    {
        public Vector2 Position { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Color Color { get; set; }

        protected bool HasFocus { get; set; }

        public event ClickEventHandler Click;

        public win2d_Control(Vector2 position, int width, int height)
        {
            Position = position;
            Width = width;
            Height = height;

            HasFocus = false;
        }

        public abstract void Draw(CanvasAnimatedDrawEventArgs args);
        public virtual void Update(CanvasAnimatedUpdateEventArgs args) { }

        public virtual bool HitTest(Point point)
        {
            if (point.X < Position.X) { return false; }
            if (point.X >= Position.X + Width) { return false; }
            if (point.Y < Position.Y) { return false; }
            if (point.Y >= Position.Y + Height) { return false; }

            return true;
        }

        protected void OnClick(PointerPoint point) { if (Click != null) { Click(point); } }

        public virtual bool KeyDown(VirtualKey virtualKey) { return false; }
        public virtual bool KeyUp(VirtualKey virtualKey) { return false; }
        public virtual void MouseDown(PointerPoint p) { }
        public virtual void MouseUp(PointerPoint p) { if (HasFocus) { OnClick(p); } }
        public virtual void MouseEnter(PointerPoint p) { }
        public virtual void MouseLeave() { }

        public virtual void GiveFocus() { HasFocus = true; }
        public virtual void LoseFocus() { HasFocus = false; }
    }
}
