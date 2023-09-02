using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO
using System.Text

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
        static int len, fileLen;
        static int startPos = 0;
        static string messg;
        static bool _exit = false;
        static string pathFile = ""

        static void Main(string[] args)
        {
            Thread t = new Thread(new ThreadStart(ExitCheck));
            t.Start();

            _ret = Focas1.cnc_allclibhndl3("192.168.0.28", 8193, 6, out _handle);

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

                string mode = GetMode();
                Console.WriteLine($"\n\nMode is: {mode}");

                string status = GetStatus();
                Console.WriteLine($"\n\nStatus is: {status}\n\n");

                /*int partcount = GetPartCount();
                Console.WriteLine($"\n\nPart Count is: {partcount}\n\n");*/

                //Ejecuta la función para descargar un NC al CNC
                string downloadOrNot = downloadToCNC();
                Console.WriteLine(downloadOrNot);

                // Initialize your Fanuc class here if needed
                /*var _fanuc = new Fanuc();
                string prg_name = GetProgramName();
                Console.Write($"\n\nProgram Name: {prg_name}");
                short _programNumber = 182 ;
                string prg_comment = GetProgramComment(_programNumber);
                Console.Write($"\n\nProgram Comment: {prg_comment}");*/
        
            }
        }
        public static string downloadFromCNC(string programName)
        {
            if (_handle == 0)
            {
                Console.WriteLine("Error: Handle do not exist");
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
            _ret = Focas1.cnc_upstart(_handle, typeOfData, programName);
            if (_ret != Focas1.EW_OK) return $"Error: the _ret was:{_ret}";
            string path = ""; //Aca el path (con el nombre del archivo que se quiere crear) donde se va a guardar todo el archivo que se descargue
            int lenLastWrite = 0;
            do
            {
                len = BUFFSIZE;
                ret = Focas1.cnc_upload4(h, ref len, buf);
                if (ret == (short)Focas1.focas_ret.EW_BUFFER) //Buffer vacío
                {
                    messg = "Error: EW_BUFFER - The Buffer is empty or full"
                    continue;
                }
                if (ret == (short)Focas1.focas_ret.EW_OK)
                {
                    buf[len] = '\0'; //En la ultima posición de lo descargado se coloca '\0' señalizando el final del String leído
                    using (FileStream oFS = File.Create(path)) ;
                    {
                        byte[] dataToWrite = new UTF8Encoding(true).getBytes(buff);
                        oFS.Write(dataToWrite, lenLastWrite, dataToWrite.Length)
                    }
                    lenLastWrite = len;
                }
                if (buff[len - 1] == '%') //Si el último caracter es '%' significa que ya leyó todo el archivo
                {
                    break;
                }
                Array.Clear(buff, 0, buff.Length); //Como lo descargado ya se guardo en un archivo se borra todo y se empieza a descargar lo siguiente
            } while ((ret == Focas1.EW_OK) || (ret == (short)Focas1.focas_ret.EW_BUFFER));
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

        /*
        public static bool GetOpSignal()
        {
            if (_handle == 0)
            {
                Console.WriteLine("Error: Please obtain a handle before calling this method");
                return false;
            }

            short addr_kind = 1; // F
            short data_type = 0; // Byte
            ushort start = 0;
            ushort end = 0;
            ushort data_length = 9; // 8 + N
            Focas1.IODBPMC0 pmc = new Focas1.IODBPMC0();

            _ret = Focas1.pmc_rdpmcrng(_handle, addr_kind, data_type, start, end, data_length, pmc);

            if (_ret != Focas1.EW_OK)
            {
                Console.WriteLine($"Error: Unable to ontain the OP Signal");
                return false;
            }

            return pmc.cdata[0].GetBit(7);

        }*/

        public static string GetMode()
        {
            if (_handle == 0)
            {
                Console.WriteLine("9Error: Please obtain a handle before calling this method");
                return "";
            }

            Focas1.ODBST Mode = new Focas1.ODBST();

            _ret = Focas1.cnc_statinfo(_handle, Mode);

            if (_ret != 0)
            {
                Console.WriteLine($"Error: Unable to obtain mode.\nReturn Code: {_ret}");
                return "";
            }

            string modestr = ModeNumberToString(Mode.aut);

            return $"Mode is: {modestr}";
        }

        public static string ModeNumberToString(int num)
        {
            switch (num)
            {
                case 0: { return "MDI"; }
                case 1: { return "MEM"; }
                case 3: { return "EDIT"; }
                case 4: { return "HND"; }
                case 5: { return "JOG"; }
                case 6: { return "Teach in JOG"; }
                case 7: { return "Teach in HND"; }
                case 8: { return "INC"; }
                case 9: { return "REF"; }
                case 10: { return "RMT"; }
                default: { return "UNAVAILABLE"; }
            }
        }

        public static string GetStatus()
        {
            if (_handle == 0)
            {
                Console.WriteLine("Error: Please obtain a handle before calling this method");
                return "";
            }

            Focas1.ODBST Status = new Focas1.ODBST();

            _ret = Focas1.cnc_statinfo(_handle, Status);

            if (_ret != 0)
            {
                Console.WriteLine($"Error: Unable to obtain status.\nReturn Code: {_ret}");
                return "";
            }

            string statusstr = StatusNumberToString(Status.run);

            return $"Status is: {statusstr}";
        }

        public static string StatusNumberToString(int num)
        {
            switch (num)
            {
                case 0: { return "STOP"; }
                case 1: { return "HOLD"; }
                case 2: { return "START"; }
                case 3: { return "MDI"; }
                case 4: { return "MSTR"; }
                default: { return "UNAVAILABLE"; }
            }
        }
        /*public static string GetProgramName()
        {
            if (_handle == 0)
            {
                return "UNAVAILABLE";
            }

            Focas1.ODBEXEPRG rdProg = new Focas1.ODBEXEPRG();

            _ret = Focas1.cnc_exeprgname(_handle, rdProg);

            if (_ret != Focas1.EW_OK)
                return _ret.ToString();
            return new string(rdProg.name).Trim('\0');
        }
        public static int GetPartCount()
        {
            Focas1.IODBPSD_1 partcount = new Focas1.IODBPSD_1();
            _ret = Focas1.cnc_rdparam3(_handle, 6711, 0, 8, 0, partcount);
            if (_ret != Focas1.EW_OK)
                return 0;
            return partcount.ldata;
        }

        
        public static string GetProgramComment(short _programNumber)
        {
            Focas1.PRGDIR2 dir = new Focas1.PRGDIR2(); // array to hold the program directory information
            short num = 1; // How much programs to be read

            short ret = Focas1.cnc_rdprogdir2(_handle, 1, ref _programNumber, ref num, dir);

            if (ret != Focas1.EW_OK)
            {
                throw new Exception($"Cannot retrieve data about the program directory. Error {ret}");
            }
            else
            {
                // Convert the character array to a string
                StringBuilder commentBuilder = new StringBuilder();
                for (int i = 0; i < dir.dir1.comment.Length && dir.dir1.comment[i] != '\0'; i++)
                {
                    commentBuilder.Append(dir.dir1.comment[i]);
                }
                return commentBuilder.ToString();
            }
        }*/
    }
}