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
                using (Game game = new Game())
                {
                    game.Run();
                }
            }).Start();
        }
	}
}
