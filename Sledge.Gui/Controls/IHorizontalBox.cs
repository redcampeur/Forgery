﻿using System.Collections.Generic;
using Sledge.Gui.Attributes;

namespace Sledge.Gui.Controls
{
    /// <summary>
    /// Horizontal Box: contains multiple children stacked horizontally with the same height. The stack will always use 100% of the available width.
    /// </summary>
    [ControlInterface]
    public interface IHorizontalBox : IBox
    {
    }

    public class ResizableTableConfiguration
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<Rectangle> Rectangles { get; set; }

        public ResizableTableConfiguration()
        {
            Rectangles = new List<Rectangle>();
        }

        public bool IsValid()
        {
            var cells = new bool[Rows, Columns];
            var set = 0;
            foreach (var r in Rectangles)
            {
                if (r.X < 0 || r.X + r.Width > Columns) return false;
                if (r.Y < 0 || r.Y + r.Height > Rows) return false;
                for (var i = r.X; i < r.X + r.Width; i++)
                {
                    for (var j = r.Y; j < r.Y + r.Height; j++)
                    {
                        if (cells[j, i]) return false;
                        cells[j, i] = true;
                        set++;
                    }
                }
            }
            return set == Columns * Rows;
        }

        public static ResizableTableConfiguration Default()
        {
            return new ResizableTableConfiguration
            {
                Columns = 2,
                Rows = 2,
                Rectangles =
                {
                    new Rectangle(0, 0, 1, 1),
                    new Rectangle(1, 0, 1, 1),
                    new Rectangle(0, 1, 1, 1),
                    new Rectangle(1, 1, 1, 1)
                }
            };
        }
    }
}