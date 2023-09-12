using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace FanucFocasTutorial1
{ /*
   This function is related to the following CNC parameter.
    See the manual of CNC parameter for details.

    0100#3
    3202#0,#4,#6
    3204#3,#4
    3210,3211
  */
    class Program
    {
        static ushort _handle = 0;
        static short _ret = 0;
        static string messg;
        static bool _exit = false;
        static int len;
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: FanucFocasTutorial1.exe <IP_ADDRESS> <FILENAME> <PROGRAMNAME>");
                return;
            }
            string ipAddress = args[0];
            string fileName = args[1];
            string programName = args[2];

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
                Console.WriteLine($"Unable to connect \n\nReturn Code: {_ret}\n\nExiting....");
                Console.Read();
            }
            else
            {
                

                //Ejecuta la función para descargar archivos desde el CNC
                StringBuilder file = new StringBuilder();
                file.Append("R:\\20. Digital\\CNC");
                file.Append(fileName);
                string filePath = file.ToString();

                string downloadOrNot = downloadFromCNC(programName, filePath) ;

                Console.WriteLine(downloadOrNot);
        
            }
        }
        public static string downloadFromCNC(string programPath,string filePath)
        {
            if (_handle == 0)
            {
                messg = ("Error: Handle do not exist");
                return "";
            }
            short typeOfData = 0;
            /*typeOf data define que archivo se quiere descargar desde el torno:
            0:NC program
            1:Tool offset data
            2:Parameter
            3:Pitch error compensation data
            4:Custom macro variables
            5:Work zero offset data
            7:Operation history data
            18:Rotary table dynamic fixture offset
            */
            const short BUFFSIZE = 1024;
            char[] buff = new char[BUFFSIZE + 1]; //Variable donde se va a guardar el archivo que se esta descargando

            /* programName va a ser el nombre del archivo que se quiere descargar, máximo de 241 characters. Si se le pasa solo un nombre de archivo descarga de la carpeta en 
             * la que este situado, si se le da solo la carpeta, todos los archivos de esa carpeta, o si se le da las dos cosas solo descarga ese archivo
             Ejemplo de programName: //CNC_MEM/USER/PATH1/FILE1*/
            _ret = Focas1.cnc_upstart4(_handle, typeOfData, programPath);
            if (_ret != Focas1.EW_OK) return $"Error: the _ret was:{_ret}";
            int lenLastWrite = 0;
            do
            {
                len = BUFFSIZE;
                _ret = Focas1.cnc_upload4(_handle, ref len, buff);
                if (_ret == (short)Focas1.focas_ret.EW_BUFFER) //Buffer vacío
                {
                    messg = "Error: EW_BUFFER - The Buffer is empty or full";
                    continue;
                }
                if (_ret == (short)Focas1.focas_ret.EW_OK)
                {
                    buff[len] = '\0'; //En la ultima posición de lo descargado se coloca '\0' señalizando el final del String leído
                    byte[] dataToWrite = Encoding.UTF8.GetBytes(new string(buff, 0, len)); // Convert char[] to bytes

                    using (FileStream oFS = File.Create(filePath)) // Remove the semicolon here
                    {
                        oFS.Write(dataToWrite, 0, dataToWrite.Length); // Write the bytes to the file
                    }
                    lenLastWrite = len;
                }
                if (buff[len - 1] == '%') //Si el último caracter es '%' significa que ya leyó todo el archivo
                {
                    break;
                }
                Array.Clear(buff, 0, buff.Length); //Como lo descargado ya se guardo en un archivo se borra todo y se empieza a descargar lo siguiente
            } while ((_ret == Focas1.EW_OK) || (_ret == (short)Focas1.focas_ret.EW_BUFFER));


            _ret = Focas1.cnc_upend4(_handle); 

            if (_ret != Focas1.EW_OK)
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
    }
}