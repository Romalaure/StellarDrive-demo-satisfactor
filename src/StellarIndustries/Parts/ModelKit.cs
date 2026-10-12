using System.Collections.Generic;
using System.Globalization;
using StellarIndustries.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// A model under construction: one mesh on the mod's atlas material (each shape picks its
    /// atlas tile), plus moving pieces, each its own child object around a pivot, named for
    /// <see cref="MachineVisuals"/>. Coordinates are always given in the part's frame; a moving
    /// piece converts them to its pivot.
    /// </summary>
    internal sealed class ModelKit
    {
        private readonly MeshBuilder _mesh = new MeshBuilder();
        private readonly List<ModelKit> _children = new List<ModelKit>();
        private readonly string _name;
        private readonly Vector3 _pivot;

        public ModelKit(string name, Vector3 pivot)
        {
            _name = name;
            _pivot = pivot;
        }

        // ---- Shapes (part frame)

        public ModelKit Box(Tile tile, Vector3 center, Vector3 size, Quaternion? rotation = null)
        {
            _mesh.Panel = IndustrialSkin.Uv(tile);
            _mesh.Box(center - _pivot, size, rotation);
            return this;
        }

        public ModelKit Cylinder(Tile tile, Vector3 a, Vector3 b, float ra, float rb, int sides = 16, bool capA = true, bool capB = true)
        {
            _mesh.Panel = IndustrialSkin.Uv(tile);
            _mesh.Cylinder(a - _pivot, b - _pivot, ra, rb, sides, capA, capB);
            return this;
        }

        public ModelKit Tube(Tile tile, Vector3 a, Vector3 b, float r, int sides = 10) => Cylinder(tile, a, b, r, r, sides);

        /// <summary>A bent pipe through several points, with a collar at each bend.</summary>
        public ModelKit Pipe(Tile tile, float r, params Vector3[] points)
        {
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Tube(tile, points[i], points[i + 1], r);
                if (i > 0)
                    Tube(Tile.DarkSteel, points[i] - (points[i + 1] - points[i]).normalized * r * 1.2f, points[i] + (points[i + 1] - points[i]).normalized * r * 1.2f, r * 1.35f);
            }
            return this;
        }

        /// <summary>A ring of bolt heads around an axis (flanges, hubs).</summary>
        public ModelKit Bolts(Vector3 center, Vector3 axis, float radius, int count, float size = 0.035f)
        {
            Vector3 n = axis.normalized;
            Vector3 u = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized, v = Vector3.Cross(n, u);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Vector3 p = center + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * radius;
                Cylinder(Tile.BoltHeads, p, p + n * size * 0.6f, size * 0.5f, size * 0.5f, 6);
            }
            return this;
        }

        /// <summary>A rectangular frame (4 bars) in a plane facing <paramref name="normal"/> (x, y or z axis).</summary>
        public ModelKit Frame(Tile tile, Vector3 center, Vector2 size, float bar, Vector3 normal, float depth = 0.03f)
        {
            Vector3 n = normal.normalized;
            Vector3 right = Mathf.Abs(n.y) > 0.9f ? Vector3.right : Vector3.Cross(Vector3.up, n).normalized;
            Vector3 up = Vector3.Cross(n, right).normalized;
            Vector3 Dim(Vector3 along, float length, Vector3 across, float width) =>
                Abs(along * length + across * width + n * depth);
            Box(tile, center + up * (size.y - bar) * 0.5f, Dim(right, size.x, up, bar));
            Box(tile, center - up * (size.y - bar) * 0.5f, Dim(right, size.x, up, bar));
            Box(tile, center + right * (size.x - bar) * 0.5f, Dim(up, size.y, right, bar));
            Box(tile, center - right * (size.x - bar) * 0.5f, Dim(up, size.y, right, bar));
            return this;
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        /// <summary>A ladder from <paramref name="bottom"/> up <paramref name="height"/>, rungs along x.</summary>
        public ModelKit Ladder(Vector3 bottom, float height, float width = 0.36f)
        {
            Tube(Tile.PaintedYellow, bottom + new Vector3(-width * 0.5f, 0f, 0f), bottom + new Vector3(-width * 0.5f, height, 0f), 0.018f, 6);
            Tube(Tile.PaintedYellow, bottom + new Vector3(width * 0.5f, 0f, 0f), bottom + new Vector3(width * 0.5f, height, 0f), 0.018f, 6);
            for (float y = 0.2f; y < height; y += 0.25f)
                Tube(Tile.DarkSteel, bottom + new Vector3(-width * 0.5f, y, 0f), bottom + new Vector3(width * 0.5f, y, 0f), 0.012f, 6);
            return this;
        }

        // ---- Moving pieces

        /// <summary>A moving piece turning or sliding around <paramref name="pivot"/> (part frame). Spec: "kind=spin axis=1,0,0 speed=180".</summary>
        public ModelKit Moving(string spec, Vector3 pivot)
        {
            var child = new ModelKit(MachineVisuals.AnimPrefix + " " + spec, pivot);
            _children.Add(child);
            return child;
        }

        /// <summary>A piece shown only while the machine works (or only while it does not).</summary>
        public ModelKit Status(bool on)
        {
            var child = new ModelKit(on ? MachineVisuals.StatusOn : MachineVisuals.StatusOff, _pivot);
            _children.Add(child);
            return child;
        }

        public static string Axis(Vector3 a) => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", a.x, a.y, a.z);

        // ---- Build

        /// <summary>Creates the objects under <paramref name="parent"/>; <paramref name="parentPivot"/> is the parent's pivot in the part frame.</summary>
        public void Build(Transform parent, int layer, Material material, Vector2? palette, Vector3 parentPivot)
        {
            var go = new GameObject(_name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = _pivot - parentPivot;
            if (_mesh.HasGeometry)
            {
                go.AddComponent<MeshFilter>().sharedMesh = _mesh.Build(_name, palette);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (ModelKit child in _children)
                child.Build(go.transform, layer, material, palette, _pivot);
        }
    }
}
