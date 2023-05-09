using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dev.ComradeVanti.EarClip
{
    public static class Triangulate
    {
        private readonly struct Triangle
        {
            public readonly int A;
            public readonly int B;
            public readonly int C;

            public Triangle(int a, int b, int c)
            {
                A = a;
                B = b;
                C = c;
            }
        }

        private static void Deconstruct(this Vector2 p, out float x, out float y)
        {
            x = p.x;
            y = p.y;
        }

        private static void Deconstruct(this Triangle triangle, out int a, out int b, out int c)
        {
            a = triangle.A;
            b = triangle.B;
            c = triangle.C;
        }

        public static IEnumerable<Vector2> Clockwise(this IEnumerable<Vector2> points)
        {
            var array = points.ToArray();
            var ((x1, y1), (x2, y2), (x3, y3)) = (array[0], array[1], array[2]);
            var isClockwise = (y2 - y1) * (x3 - x2) - (y3 - y2) * (x2 - x1) >= 0;
            return isClockwise ? array : array.Reverse();
        }

        public static IEnumerable<int> ConcaveNoHoles(IReadOnlyList<Vector2> vertices)
        {
            var vertexCount = vertices.Count;
            var allIndices = Enumerable.Range(0, vertexCount).ToList();

            int Next(int vertexIndex)
            {
                var index = allIndices.IndexOf(vertexIndex);
                var nextIndex = (index + 1) % allIndices.Count;
                return allIndices[nextIndex];
            }

            int Prev(int vertexIndex)
            {
                var index = allIndices.IndexOf(vertexIndex);
                var prevIndex = index > 0 ? index - 1 : allIndices.Count - 1;
                return allIndices[prevIndex];
            }

            Vector2 VertexAt(int index) =>
                vertices[index];

            Vector2 EdgeBetween(int a, int b) =>
                VertexAt(b) - VertexAt(a);

            Triangle TriangleWithTip(int index) =>
                new Triangle(Prev(index), index, Next(index));

            float AngleAt(int index)
            {
                var (edge1, edge2) =
                    (EdgeBetween(index, Prev(index)), EdgeBetween(index, Next(index)));
                var dot = Vector2.Dot(edge1.normalized, edge2.normalized);
                return Mathf.Acos(dot);
            }

            bool IsConvex(int index)
            {
                var angle = AngleAt(index);
                return angle < Mathf.PI;
            }

            var convexIndices = allIndices.Where(IsConvex).ToList();

            bool IsReflex(int index) =>
                !IsConvex(index);

            var reflexIndices = allIndices.Where(IsReflex).ToList();

            bool Contains(Triangle triangle, int index)
            {
                // https://stackoverflow.com/a/40959986

                var (va, vb, vc) = triangle;
                var (ax, ay) = VertexAt(va);
                var (bx, by) = VertexAt(vb);
                var (cx, cy) = VertexAt(vc);
                var (x, y) = VertexAt(index);

                var a = ((by - cy) * (x - cx) + (cx - bx) * (y - cy)) / ((by - cy) * (ax - cx) + (cx - bx) * (ay - cy));
                var b = ((cy - ay) * (x - cx) + (ax - cx) * (y - cy)) / ((by - cy) * (ax - cx) + (cx - bx) * (ay - cy));
                var c = 1 - a - b;

                return a is >= 0 and <= 1 && b is >= 0 and <= 1 && c is >= 0 and <= 1;
            }

            bool IsEar(int index)
            {
                var triangle = TriangleWithTip(index);
                return !reflexIndices.Any(i => Contains(triangle, i));
            }

            var earIndices = convexIndices.Where(IsEar).ToList();

            void UpdateEar(int index)
            {
                if (!IsEar(index)) earIndices.Remove(index);

                if (IsConvex(index)) return;
                convexIndices.Remove(index);
                reflexIndices.Add(index);
            }

            void UpdateConvex(int index)
            {
                if (!IsConvex(index))
                {
                    convexIndices.Remove(index);
                    reflexIndices.Add(index);
                }

                else if (IsEar(index))
                {
                    earIndices.Add(index);
                }
            }

            void UpdateReflex(int index)
            {
                if (!IsConvex(index)) return;

                reflexIndices.Remove(index);
                convexIndices.Add(index);

                if (IsEar(index))
                    earIndices.Add(index);
            }

            void UpdateIndex(int index)
            {
                if (earIndices.Contains(index))
                    UpdateEar(index);
                else if (convexIndices.Contains(index))
                    UpdateConvex(index);
                else if (reflexIndices.Contains(index))
                    UpdateReflex(index);
            }

            while (earIndices.Count > 0)
            {
                var earIndex = earIndices[0];
                var prevIndex = Prev(earIndex);
                var nextIndex = Next(earIndex);

                yield return prevIndex;
                yield return earIndex;
                yield return nextIndex;

                earIndices.RemoveAt(0);
                convexIndices.Remove(earIndex);
                allIndices.Remove(earIndex);

                if (allIndices.Count < 3) break;

                UpdateIndex(prevIndex);
                UpdateIndex(nextIndex);
            }
        }
    }
}