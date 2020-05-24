using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valokrant.V1
{
    public struct WindowInfo
    {
        public string WindowName;
        public DateTime startTime;
    }

    public class RenderingData
    {
        public int drawnFrames;
        public float timeSinceLastDraw;
    }
}
