using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KaymakGames___Networking.Parsers
{
    class ObjFileParser
    {
        public Vector3[] vertexArr;
        public int[] indices;

        public ObjFileParser(string path)
        {
            var obj = new ObjParse.ObjParser();

            obj.LoadObj(path);

            vertexArr = new Vector3[obj.VertexList.Count];
            indices = new int[obj.FaceList.Count];
            int count = 0;
            foreach(Vertex vertex in obj.VertexList)
            {
                vertexArr[count] = new Vector3((float)vertex.X, (float)vertex.Y, (float)vertex.Z);
                
                count++;
            }

            int count1 = 0;
            foreach (Face face in obj.FaceList)
            {
                indices[count1] = face.VertexIndexList[count1];
                count++;
            }
        }
    }
}
