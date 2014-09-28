﻿namespace Sledge.Gui.Controls
{
    public interface IBox : IContainer
    {
        bool Uniform { get; set; }
        int ControlPadding { get; set; }
        void Insert(int index, IControl child, bool fill);
    }
}