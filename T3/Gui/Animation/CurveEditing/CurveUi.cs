﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Animation.Curves;
using T3.Gui.Graph;

namespace T3.Gui.Animation
{
    /// <summary>
    /// A graphical representation of a <see cref="CurveEditing"/>. Handles style and selection states.
    /// </summary>
    public class CurveUi
    {
        public bool IsHighlighted { get; set; }
        public List<CurvePointUi> CurvePoints { get; set; }

        public CurveUi(Curve curve, CurveEditCanvas curveEditor)
        {
            _curveEditor = curveEditor;
            _curve = curve;

            CurvePoints = new List<CurvePointUi>();
            foreach (var pair in curve.GetPoints())
            {
                var key = pair.Value;
                CurvePoints.Add(new CurvePointUi(key, curve, curveEditor));
            }
        }


        public void Draw()
        {
            foreach (var p in CurvePoints)
            {
                p.Draw();
            }
            DrawLine();
        }


        private void DrawLine()
        {
            var step = 3f;
            var width = (float)ImGui.GetWindowWidth();

            double dU = _curveEditor.InverseTransformDirection(new Vector2(step, 0)).X;
            double u = _curveEditor.InverseTransformPosition(_curveEditor.WindowPos).X;
            float x = _curveEditor.WindowPos.X;

            var steps = (int)(width / step);
            if (_points.Length != steps)
            {
                _points = new Vector2[steps];
            }

            for (int i = 0; i < steps; i++)
            {
                _points[i] = new Vector2(
                    x,
                    _curveEditor.TransformPosition(new Vector2(0, (float)_curve.GetSampledValue(u))).Y
                    );

                u += dU;
                x += step;
            }
            _curveEditor.DrawList.AddPolyline(ref _points[0], steps, Color.Gray, false, 1);
        }

        private Curve _curve;
        private static Vector2[] _points = new Vector2[2];
        private CurveEditCanvas _curveEditor;
    }
}