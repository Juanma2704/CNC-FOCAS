using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public static string downloadToCNC() {
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
                string program =
                        "\n" +
                        "<PROG123>\n" +
                        "M3 S1200\n" +
                        "G0 Z0\n" +
                        "G0 X0 Y0\n" +
                        "G1 F500 X120. Y-30.\n" +
                        "M30\n" +
                        "%";

                len = program.Length;

                _ret = Focas1.cnc_dwnstart4(_handle, typeOfData, "//CNC_MEM/USER/PATH1");

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