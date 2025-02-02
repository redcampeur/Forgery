﻿using System.Numerics;
using Forgery.DataStructures.Geometric;

namespace Forgery.BspEditor.Grid
{
    public class GridLine
    {
        public GridLineType Type { get; private set; }
        public Line Line { get; set; }

        public GridLine(GridLineType type, Vector3 start, Vector3 end)
        {
            Type = type;
            Line = new Line(start, end);
        }
    }
}
