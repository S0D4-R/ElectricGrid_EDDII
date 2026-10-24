using System;

namespace ElectricGrid.ConsoleApp.Tui
{
    /// <summary>
    /// Computes node positions (in terminal cells) with a Fruchterman-Reingold force-directed
    /// layout: every pair of nodes repels, connected nodes attract, and a weak gravity keeps
    /// disconnected zones inside the frame. A deterministic start makes the picture stable
    /// between runs. Afterwards positions are stretched to fill the canvas and label overlaps
    /// are resolved.
    /// </summary>
    internal static class GraphLayout
    {
        private const int Iterations = 300;

        /// <param name="labelWidth">Width in cells of a node label (brackets included).</param>
        /// <param name="x">Output: left column of each label, relative to the canvas.</param>
        /// <param name="y">Output: row of each label, relative to the canvas.</param>
        public static void Compute(GraphSnapshot graph, int width, int height, int labelWidth, out int[] x, out int[] y)
        {
            int n = graph.Nodes.Length;
            x = new int[n];
            y = new int[n];
            if (n == 0)
            {
                return;
            }

            int usableW = Math.Max(1, width - labelWidth);
            int usableH = Math.Max(1, height - 1);

            if (n == 1)
            {
                x[0] = usableW / 2;
                y[0] = usableH / 2;
                return;
            }

            // Physics runs in "virtual units" where one row is worth two columns, because a terminal
            // cell is roughly twice as tall as it is wide. This keeps distances visually isotropic.
            double areaW = usableW;
            double areaH = usableH * 2.0;
            double[] px = new double[n];
            double[] py = new double[n];
            SeedPositions(px, py, areaW, areaH);

            double k = 0.9 * Math.Sqrt(areaW * areaH / n);
            double temperature = areaW / 6.0;
            double[] dx = new double[n];
            double[] dy = new double[n];

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                Array.Clear(dx, 0, n);
                Array.Clear(dy, 0, n);

                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        double vx = px[i] - px[j];
                        double vy = py[i] - py[j];
                        double distance = Math.Max(0.01, Math.Sqrt(vx * vx + vy * vy));
                        double force = k * k / distance;
                        dx[i] += vx / distance * force;
                        dy[i] += vy / distance * force;
                        dx[j] -= vx / distance * force;
                        dy[j] -= vy / distance * force;
                    }
                }

                for (int e = 0; e < graph.EdgeFrom.Length; e++)
                {
                    int u = graph.EdgeFrom[e];
                    int v = graph.EdgeTo[e];
                    double vx = px[u] - px[v];
                    double vy = py[u] - py[v];
                    double distance = Math.Max(0.01, Math.Sqrt(vx * vx + vy * vy));
                    double force = distance * distance / k;
                    dx[u] -= vx / distance * force;
                    dy[u] -= vy / distance * force;
                    dx[v] += vx / distance * force;
                    dy[v] += vy / distance * force;
                }

                for (int i = 0; i < n; i++)
                {
                    dx[i] -= (px[i] - areaW / 2) * 0.15;
                    dy[i] -= (py[i] - areaH / 2) * 0.15;

                    double length = Math.Max(0.01, Math.Sqrt(dx[i] * dx[i] + dy[i] * dy[i]));
                    double step = Math.Min(length, temperature);
                    px[i] = Clamp(px[i] + dx[i] / length * step, 0, areaW);
                    py[i] = Clamp(py[i] + dy[i] / length * step, 0, areaH);
                }

                temperature = (areaW / 6.0) * (1.0 - (double)(iteration + 1) / Iterations);
            }

            StretchToCanvas(px, py, usableW, usableH, x, y);
            SeparateLabels(x, y, labelWidth, usableW, usableH);
        }

        private static void SeedPositions(double[] px, double[] py, double areaW, double areaH)
        {
            int n = px.Length;
            for (int i = 0; i < n; i++)
            {
                double angle = 2 * Math.PI * i / n;
                double radius = 0.38 * (0.7 + 0.3 * ((i * 7) % 5) / 4.0);
                px[i] = areaW / 2 + radius * areaW * Math.Cos(angle);
                py[i] = areaH / 2 + radius * areaH * Math.Sin(angle);
            }
        }

        private static void StretchToCanvas(double[] px, double[] py, int usableW, int usableH, int[] x, int[] y)
        {
            double minX = px[0], maxX = px[0], minY = py[0], maxY = py[0];
            for (int i = 1; i < px.Length; i++)
            {
                minX = Math.Min(minX, px[i]);
                maxX = Math.Max(maxX, px[i]);
                minY = Math.Min(minY, py[i]);
                maxY = Math.Max(maxY, py[i]);
            }

            double spanX = maxX - minX;
            double spanY = maxY - minY;

            for (int i = 0; i < px.Length; i++)
            {
                x[i] = spanX < 1e-6 ? usableW / 2 : (int)Math.Round((px[i] - minX) / spanX * usableW);
                y[i] = spanY < 1e-6 ? usableH / 2 : (int)Math.Round((py[i] - minY) / spanY * usableH);
            }
        }

        /// <summary>Pushes apart labels that would overlap on the same row.</summary>
        private static void SeparateLabels(int[] x, int[] y, int labelWidth, int usableW, int usableH)
        {
            int n = x.Length;
            int minGap = labelWidth + 1;

            for (int pass = 0; pass < 60; pass++)
            {
                bool moved = false;

                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        if (y[i] != y[j] || Math.Abs(x[j] - x[i]) >= minGap)
                        {
                            continue;
                        }

                        moved = true;

                        if (pass >= 20)
                        {
                            // Horizontal room ran out (e.g. both stuck against an edge): change row instead.
                            y[j] = y[j] < usableH ? y[j] + 1 : y[j] - 1;
                            continue;
                        }

                        int direction = x[j] > x[i] ? 1 : (x[j] < x[i] ? -1 : (j % 2 == 0 ? 1 : -1));
                        int push = (minGap - Math.Abs(x[j] - x[i]) + 1) / 2;
                        x[i] = (int)Clamp(x[i] - direction * push, 0, usableW);
                        x[j] = (int)Clamp(x[j] + direction * push, 0, usableW);
                    }
                }

                if (!moved)
                {
                    break;
                }
            }
        }

        private static double Clamp(double value, double min, double max) => value < min ? min : (value > max ? max : value);
    }
}
