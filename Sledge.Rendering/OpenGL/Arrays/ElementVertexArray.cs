﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Sledge.Rendering.Cameras;
using Sledge.Rendering.Interfaces;
using Sledge.Rendering.OpenGL.Shaders;
using Sledge.Rendering.OpenGL.Vertices;
using Sledge.Rendering.Scenes.Elements;
using Sledge.Rendering.Scenes.Renderables;

namespace Sledge.Rendering.OpenGL.Arrays
{
    public class ElementVertexArray : VertexArray<Element, SimpleVertex>
    {
        private const int WorldFacePolygons = 0;
        private const int WorldFaceWireframe = 1;

        private const int ScreenFacePolygons = 10;
        private const int ScreenFaceWireframe = 11;

        private const int AnchoredFacePolygons = 20;
        private const int AnchoredFaceWireframe = 21;

        private readonly IRenderer _renderer;
        private readonly IViewport _viewport;

        public ElementVertexArray(IRenderer renderer, IViewport viewport, IEnumerable<Element> data) : base(data)
        {
            _renderer = renderer;
            _viewport = viewport;
        }

        public void Render(IRenderer renderer, Passthrough shader, IViewport viewport)
        {
            var camera = viewport.Camera;
            const float near = -1000000;
            const float far = 1000000;
            var vpMatrix = Matrix4.CreateOrthographicOffCenter(0, viewport.Control.Width, viewport.Control.Height, 0, near, far);

            GL.Disable(EnableCap.DepthTest);

            shader.Bind();
            
            shader.SelectionTransform = renderer.SelectionTransform;
            shader.ModelMatrix = camera.GetModelMatrix();
            shader.CameraMatrix = camera.GetCameraMatrix();
            shader.ViewportMatrix = camera.GetViewportMatrix(viewport.Control.Width, viewport.Control.Height);
            shader.Orthographic = camera.Flags.HasFlag(CameraFlags.Orthographic);
            shader.UseAccentColor = false;

            RenderPositionType(renderer, shader, PositionType.World);

            shader.SelectionTransform = renderer.SelectionTransform;
            shader.ModelMatrix = Matrix4.Identity;
            shader.CameraMatrix = Matrix4.Identity;
            shader.ViewportMatrix = vpMatrix;
            shader.Orthographic = camera.Flags.HasFlag(CameraFlags.Orthographic);
            shader.UseAccentColor = false;

            RenderPositionType(renderer, shader, PositionType.Screen);

            shader.UseAccentColor = false;

            RenderPositionType(renderer, shader, PositionType.Anchored);

            shader.Unbind();

            GL.Enable(EnableCap.DepthTest);
        }

        private void RenderPositionType(IRenderer renderer, Passthrough shader, PositionType type)
        {
            var wireframeId = type == PositionType.Screen ? ScreenFaceWireframe : (type == PositionType.Anchored ? AnchoredFaceWireframe : WorldFaceWireframe);
            var polygonId = type == PositionType.Screen ? ScreenFacePolygons : (type == PositionType.Anchored ? AnchoredFacePolygons : WorldFacePolygons);

            // Render polygons
            string last = null;
            foreach (var subset in GetSubsets<string>(polygonId).Where(x => x.Instance != null).OrderBy(x => (string)x.Instance))
            {
                var mat = (string)subset.Instance;
                if (mat != last) renderer.Materials.Bind(mat);
                last = mat;

                Render(PrimitiveType.Triangles, subset);
            }

            shader.UseAccentColor = true;

            // Render wireframe
            foreach (var subset in GetSubsets<WireframeProperties>(wireframeId))
            {
                ((WireframeProperties)subset.Instance).Bind();
                Render(PrimitiveType.Lines, subset);
            }
            WireframeProperties.RestoreDefaults();
        }

        protected override void CreateArray(IEnumerable<Element> data)
        {
            var d = data.ToList();
            CreatePositionTypeArray(d, PositionType.Screen);
            CreatePositionTypeArray(d, PositionType.Anchored);
            CreatePositionTypeArray(d, PositionType.World);
        }

        private void CreatePositionTypeArray(IEnumerable<Element> data, PositionType type)
        {
            var wireframeId = type == PositionType.Screen ? ScreenFaceWireframe : (type == PositionType.Anchored ? AnchoredFaceWireframe : WorldFaceWireframe);
            var polygonId = type == PositionType.Screen ? ScreenFacePolygons : (type == PositionType.Anchored ? AnchoredFacePolygons : WorldFacePolygons);

            StartSubset(wireframeId);

            var list = data.Where(x => x.PositionType == type).Where(x => x.Viewport == null || x.Viewport == _viewport).ToList();

            foreach (var g in list.SelectMany(x => x.GetFaces(_viewport, _renderer)).GroupBy(x => x.Material.UniqueIdentifier))
            {
                StartSubset(polygonId);

                foreach (var face in g)
                {
                    PushOffset(face);
                    var index = PushData(Convert(face));
                    if (face.RenderFlags.HasFlag(RenderFlags.Polygon)) PushIndex(polygonId, index, Triangulate(face.Vertices.Count));
                    if (face.RenderFlags.HasFlag(RenderFlags.Wireframe)) PushIndex(wireframeId, index, Linearise(face.Vertices.Count));
                }

                PushSubset(polygonId, g.Key);
            }

            PushSubset(wireframeId, WireframeProperties.Default);

            foreach (var g in list.SelectMany(x => x.GetLines(_viewport, _renderer)).GroupBy(x => new { x.Width, x.DepthTested, x.Smooth, x.Stippled }))
            {
                StartSubset(wireframeId);
                foreach (var line in g)
                {
                    var index = PushData(Convert(line));
                    PushIndex(wireframeId, index, Line(line.Vertices.Count));
                }
                PushSubset(wireframeId, new WireframeProperties {Width = g.Key.Width, DepthTested = g.Key.DepthTested, Smooth = g.Key.Smooth, IsStippled = g.Key.Stippled });
            }
        }

        private IEnumerable<uint> Line(int num)
        {
            for (uint i = 0; i < num - 1; i++)
            {
                yield return i;
                yield return i + 1;
            }
        }

        private VertexFlags ConvertVertexFlags(Element element)
        {
            var flags = VertexFlags.None;
            if (!element.CameraFlags.HasFlag(CameraFlags.Orthographic)) flags |= VertexFlags.InvisibleOrthographic;
            if (!element.CameraFlags.HasFlag(CameraFlags.Perspective)) flags |= VertexFlags.InvisiblePerspective;
            if (element.IsSelected) flags |= VertexFlags.Selected;
            return flags;
        }

        private IEnumerable<SimpleVertex> Convert(FaceElement face)
        {
            return face.Vertices.Select(vert => new SimpleVertex
            {
                Position = Convert(vert.Position, face.PositionType),
                Texture = new Vector2(vert.TextureU, vert.TextureV),
                MaterialColor = face.Material.Color.ToAbgr(),
                AccentColor = face.AccentColor.ToAbgr(),
                TintColor = Color.White.ToAbgr(),
                Flags = ConvertVertexFlags(face)
            });
        }

        private IEnumerable<SimpleVertex> Convert(LineElement line)
        {
            return line.Vertices.Select(vert => new SimpleVertex
            {
                Position = Convert(vert, line.PositionType),
                MaterialColor = line.Color.ToAbgr(),
                AccentColor = line.Color.ToAbgr(),
                TintColor = Color.White.ToAbgr(),
                Flags = ConvertVertexFlags(line)
            });
        }

        private Vector3 Convert(Position position, PositionType type)
        {
            if (type == PositionType.World)
            {
                var e = position.Offset;
                var off = new Vector3(_viewport.Camera.PixelsToUnits(e.X), -_viewport.Camera.PixelsToUnits(e.Y), _viewport.Camera.PixelsToUnits(e.Z));
                return position.Location + _viewport.Camera.Expand(off);
            }
            else if (type == PositionType.Screen)
            {
                if (position.Normalised) return new Vector3(position.Location.X * _viewport.Control.Width, position.Location.Y * _viewport.Control.Height, 0) + position.Offset;
                else return position.Location + position.Offset;
            }
            else if (type == PositionType.Anchored)
            {
                var transformed = _viewport.Camera.WorldToScreen(position.Location, _viewport.Control.Width, _viewport.Control.Height);
                return new Vector3(transformed.X, _viewport.Control.Height - transformed.Y, transformed.Z) + position.Offset;
            }
            return Vector3.Zero;
        }
    }
}