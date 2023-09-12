using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ReadFanucProgramDemo
{
    internal class Program
    {
        private static short _ret = 0;
        private static Fanuc _fanuc;

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: FanucFocasTutorial1.exe <IP_ADDRESS> <FILENAME> ");
                return;
            }
            _fanuc = new Fanuc();
            string ipAddress = args[0];
            string path = args[1];

            var handle = _fanuc.Connect(ipAddress);

            var progList = _fanuc.Programs.GetProgramListing(handle, path);

            int index = 0;

            foreach (var prg in progList)
            {
                Console.WriteLine($"{index}) Name: {prg.Key}\tComment: {prg.Value}");
                index++;
            }
            Console.ReadLine();
            // All lines from progList are printed, so end the program
            Environment.Exit(0);
        }
    }
}