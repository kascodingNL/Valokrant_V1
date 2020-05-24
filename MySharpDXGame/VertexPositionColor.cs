using SharpDX;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Valokrant.V1
{
	[StructLayoutAttribute(LayoutKind.Sequential)]
	public struct VertexPositionColor
	{
		public SharpDX.Vector3 Position;
		public Color4 Color;

		public VertexPositionColor(SharpDX.Vector3 position, Color4 color)
		{
			Position = position;
			Color = color;
		}
	}
}