﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Forgery.BspEditor.Tools.Vertex.Selection;
using Forgery.DataStructures.Geometric;

namespace Forgery.BspEditor.Tools.Vertex.Errors
{
    [Export(typeof(IVertexErrorCheck))]
    public class OverlappingVertices : IVertexErrorCheck
    {
        private const string Key = "Forgery.BspEditor.Tools.Vertex.Errors.OverlappingVertices";

        public IEnumerable<VertexError> GetErrors(VertexSolid solid)
        {
            foreach (var face in solid.Copy.Faces)
            {
                var overlapping = face.Vertices.GroupBy(x => x.Position.Round(2)).Where(x => x.Count() > 1).ToList();
                foreach (var ol in overlapping)
                {
                    yield return new VertexError(Key, solid).Add(face).Add(ol);
                }
            }
        }
    }
}