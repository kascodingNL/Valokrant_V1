using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Valokrant.V1
{
	class Program
	{
        [MTAThread]
        static void Main(string[] args)
        {
            new Thread(() =>
            {
                using (Game game = new Game("Gekke Valokrant V1 Test", 1))
                {
                    game.Run();
                }
            }).Start();
        }
	}
}
