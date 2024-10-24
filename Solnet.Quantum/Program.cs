using System;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.Simulation.Core;
using QSOL;


namespace Solnet.Quantum
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var sim = new QuantumSimulator())
            {
                // Generate a random number with 8 bits (0 to 255)
                var result = GenerateRandomNumberInRange.Run(sim, 8).Result;
                Console.WriteLine($"Quantum Random Number: {result}");
               
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

    }
}
