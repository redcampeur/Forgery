﻿using System;
using System.Drawing;

namespace Sledge.Gui.Interfaces.Shell
{
    public interface IToolbarItem : IDisposable
    {
        string TextKey { get; set; }
        string Text { get; set; }
        Image Icon { set; }
        bool IsActive { get; set; }
        event EventHandler Clicked;
    }
}