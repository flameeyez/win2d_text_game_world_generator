using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace win2d_text_game_world_generator
{
    public class PointInt
    {
        private int _x;
        public int X { get { return _x; } }
        private int _y;
        public int Y { get { return _y; } }

        public PointInt(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public override int GetHashCode()
        {
            int nReturn = 17;
            nReturn = nReturn * 23 + X.GetHashCode();
            nReturn = nReturn * 23 + Y.GetHashCode();
            return nReturn;
        }

        public override bool Equals(object obj)
        {
            PointInt compare = obj as PointInt;
            return (X == compare.X) && (Y == compare.Y);
        }
    }
}
