using System;
using System.Collections.Generic;
using System.Text;

namespace CfgCompLib.classes
{
    public enum Shape { Start, End, Decision, Action }
    public class ShapeProperties
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public Shape shape;

        public ShapeProperties(float x, float y, float width, float height, Shape shape)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.shape = shape;
        }
    }
}
