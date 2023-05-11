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

        private static float Orientation(Vector2 a, Vector2 b, Vector2 c)
        {
            var ((x1, y1), (x2, y2), (x3, y3)) = (a, b, c);
            return (y2 - y1) * (x3 - x2) - (y3 - y2) * (x2 - x1);
        }

        public static IEnumerable<T> Clockwise<T>(this IEnumerable<T> items, Func<T, Vector2> selector)
        {
            var array = items.ToArray();
            var points = array.Take(3).Select(selector).ToArray();
            var (a, b, c) = (points[0], points[1], points[2]);
            return Orientation(a, b, c) >= 0 ? array : array.Reverse();
        }

        public static IEnumerable<Vector2> Clockwise(this IEnumerable<Vector2> points)
        {
            return Clockwise(points, it => it);
        }

        private static float Cross2D(Vector2 a, Vector2 b) =>
            a.x * b.y - a.y * b.x;

        private static bool Intersects(Vector2 a1, Vector2 b1, Vector2 a2, Vector2 b2)
        {
            // https://ideone.com/PnPJgb
            Vector2 CmP = new Vector2(a2.x - a1.x, a2.y - a1.y);
            Vector2 r = new Vector2(b1.x - a1.x, b1.y - a1.y);
            Vector2 s = new Vector2(b2.x - a2.x, b2.y - a2.y);

            float CmPxr = CmP.x * r.y - CmP.y * r.x;
            float CmPxs = CmP.x * s.y - CmP.y * s.x;
            float rxs = r.x * s.y - r.y * s.x;

            if (CmPxr == 0f)
            {
                // Lines are collinear, and so intersect if they have any overlap

                return ((a2.x - a1.x < 0f) != (a2.x - b1.x < 0f))
                       || ((a2.y - a1.y < 0f) != (a2.y - b1.y < 0f));
            }

            if (rxs == 0f)
                return false; // Lines are parallel.

            float rxsr = 1f / rxs;
            float t = CmPxs * rxsr;
            float u = CmPxr * rxsr;

            return (t > 0f) && (t < 1f) && (u > 0f) && (u < 1f);
        }

        public static Vector2 MidPoint(Vector2 a, Vector2 b)
        {
            return Vector2.Lerp(a, b, 0.5f);
        }

        public static IEnumerable<int> ConcaveNoHoles(IReadOnlyList<Vector2> vertices)
        {
            if (vertices.Count == 3)
            {
                yield return 0;
                yield return 1;
                yield return 2;
                yield break;
            }

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

            var segments = allIndices
                .Select(i => (VertexAt(i), VertexAt(Next(i))))
                .ToArray();
            var infx = vertices.Max(it => it.x) + 1;

            Vector2 EdgeBetween(int a, int b) =>
                VertexAt(b) - VertexAt(a);

            Triangle TriangleWithTip(int index) =>
                new Triangle(Prev(index), index, Next(index));

            float AngleAt(int index)
            {
                var (a, b) = (
                    EdgeBetween(index, Prev(index)).normalized,
                    EdgeBetween(index, Next(index)).normalized);

                return Mathf.Atan2(Cross2D(a, b), Vector2.Dot(a, b));
            }

            bool IsReflex(int index)
            {
                var angle = AngleAt(index);
                return angle is >= Mathf.PI or < 0;
            }

            bool IsDiagonal(Vector2 a, Vector2 b)
            {
                var intersections = 0;

                foreach (var (a2, b2) in segments)
                {
                    if (!(a == a2 || a == b2 || b == a2 || b == b2) && Intersects(a, b, a2, b2)) return false;
                    var midPoint = MidPoint(a, b);
                    var infPoint = new Vector2(infx, midPoint.y);

                    if (Intersects(midPoint, infPoint, a2, b2))
                        intersections++;
                }

                return intersections % 2 == 1;
            }

            bool IsConvex(int index)
            {
                var reflex = IsReflex(index);
                var diagonal = IsDiagonal(
                    VertexAt(Prev(index)),
                    VertexAt(Next(index)));
                return !reflex && diagonal;
            }

            var convexIndices = allIndices.Where(IsConvex).ToList();
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

                bool IsPartOfTriangle(int i)
                {
                    return i == triangle.A || i == triangle.B || i == triangle.C;
                }

                return !reflexIndices
                    .Where(i => !IsPartOfTriangle(i))
                    .Any(i => Contains(triangle, i));
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