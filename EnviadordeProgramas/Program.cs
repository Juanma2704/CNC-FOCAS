using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace FanucFocasTutorial1
{
    class Program
    {
        static ushort _handle = 0;
        static short _ret = 0;
        static int len, fileLen;
        static int startPos = 0;
        static string messg;
        static bool _exit = false;

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: FanucFocasTutorial1.exe <IP_ADDRESS> <FILENAME> ");
                return;
            }
            string ipAddress = args[0];
            string fileName = args[1];


            Thread t = new Thread(new ThreadStart(ExitCheck));
            t.Start();

            _ret = Focas1.cnc_allclibhndl3(ipAddress, 8193, 6, out _handle);

            /*
             _ret code = -16 -> Error del socket de comunicación - Revisar tensión - cable etc...
             _ret code = -15 -> No existe un DDL para cada serie de CNC
             _ret code = -8 -> Guardado del nuemero _handle falló
            */

            if (_ret != Focas1.EW_OK)
            {
                Console.WriteLine($"Unable to connect to 192.168.0.26 on port 8193\n\nReturn Code: {_ret}\n\nExiting....");
                Console.Read();
            }
            else
            {
                Console.WriteLine($"Our Focas handle is {_handle}");

                //Ejecuta la función para descargar un NC al CNC
                string downloadOrNot = downloadToCNC(fileName);
                Console.WriteLine(downloadOrNot);
        
            }
        }
        public static string downloadToCNC(string fileName) {
            if (_handle == 0)
            {
                Console.WriteLine("Error: Handle do not exist");
                return "";
            }

            short typeOfData = 0;
            /* typeOfData puede ser:
                0:NC program
                1:Tool offset data
                2:Parameter
                3:Pitch error compensation data
                4:custom macro variables
                5:Work zero offset data
                18:Rotary table dynamic fixture offset
            */
            unsafe
            {

                //En este caso program es un programa NC, pero puede cambiar dependiendo de lo que queremos descargar
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("R:\\20. Digital\\CNC");
                stringBuilder.Append(fileName);
                string filePath = stringBuilder.ToString();

                string program = ReadFileToString(filePath); // Llama a la funcion que abre el archivo y lo concatena en una string

                if (program.StartsWith("Error:"))
                {
                    Console.WriteLine(program);
                }
                else
                {

                    len = program.Length;

                    _ret = Focas1.cnc_dwnstart4(_handle, typeOfData, fileName);

                    if (_ret != Focas1.EW_OK)
                    {
                        return $"Error,the return was: {_ret}";
                    }

                    while (len > 0)
                    {
                        char[] downProgram = new char[1024]; // 1460 - El máximo para ethernet; 1024-1400 - Recomendado
                        program.CopyTo(startPos, downProgram, 0, len);

                        fileLen = len;

                        _ret = Focas1.cnc_download4(_handle, ref fileLen, downProgram); //Al pasar fileLen como ref la función nos la va a modificar con la cantidad de bytes que se descargaron

                        if (_ret == (short)Focas1.focas_ret.EW_BUFFER)
                        { //No se pudo descargar ni 1 solo byte, se empieza el loop de nuevo
                            continue;
                        }
                        if (_ret == Focas1.EW_OK)
                        {
                            startPos += fileLen;
                            len -= fileLen; //Si se descargo todo len = 0 y va a terminar el loop. Sino se va a repetir y va a descargar lo que falta
                            if (len == 0)
                            {
                                messg = "Succes: All files were successfully downloaded";
                            }
                        }
                        else
                        {
                            messg = "Error: Cannot download all the files";
                            break;
                        }
                    }
                }
            }
            
            _ret = Focas1.cnc_dwnend4(_handle); // Termino la descargaa y chequeo que todo haya salido bien
            if( _ret != Focas1.EW_OK)
            {
                return $"{messg}, the error was: {_ret}";
            }
            return $"{messg}. {_ret}";

        }
        private static void ExitCheck()
        {
            while (Console.ReadLine() != "exit")
            {
                continue;
            }

            _exit = true;
        }
        public static string ReadFileToString(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);
                    return string.Join("\n", lines); // Concatena
                }
                else
                {
                    return "Error: File does not exist";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}