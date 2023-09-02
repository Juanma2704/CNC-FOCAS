using System;
using System.Collections.Generic;

namespace FanucFocasTutorial1
{
    public class ProgramManager
    {
        private ushort _handle = 0;
        private short _ret = 0;
        public Dictionary<string, string> GetProgramListing()
        {
            Dictionary<string, string> progList = new Dictionary<string, string>();
            string dirToRead = "//CNC_MEM/USER/PATH1/";
            short file_num = 1;
            Focas1.ODBPDFNFIL pdf_nfil = new Focas1.ODBPDFNFIL();
            _ret = Focas1.cnc_rdpdf_subdirn(_handle, dirToRead, pdf_nfil);
            if (_ret != Focas1.EW_OK)
            {
                Console.Write($"Failed to read the directory, error code: {_ret}");
                return null;
            }

            Focas1.ODBPDFADIR pdfadirout = new Focas1.ODBPDFADIR();

            Focas1.IDBPDFADIR pdfadirin = new Focas1.IDBPDFADIR();
            pdfadirin.path = dirToRead;
            pdfadirin.req_num = 0;
            pdfadirin.size_kind = 2;
            pdfadirin.type = 1;

            for (int i = 0; i < pdf_nfil.file_num; i++)
            {
                _ret = Focas1.cnc_rdpdf_alldir(_handle, ref file_num, ref pdfadirin, ref pdfadirout);

                if (_ret != Focas1.EW_OK)
                {
                    Console.Write($"Error reading the directory, ret: {_ret}");
                    return null;
                }
                if (pdfadirout.data_kind == 1)
                    progList.Add(pdfadirout.d_f, pdfadirout.comment);

                pdfadirin.req_num++;
            }
            return progList;
        }

        public bool ActiveMainProgram(_handle, string filePath)
        {
            if (_handle == 0)
                return false;
            var dirPath = "//CNC_MEM/USER/PATH1/";

            _ret = Focas1.cnc_pdf_slctmain(_handle, dirPath + filePath);

            if (_ret == Focas1.EW_OK)
                return false;
            return true;
            
        }

        public bool StartProgram(_handle, string filePath)
        {
            if (_handle == 0)
                return false;

            _ret = Focas1.cnc_start(_handle);

            if (_ret == Focas1.EW_OK)
                return false;
            return true;
        }


        public static void Main(string[] args)
        {
            ProgramManager programManager = new ProgramManager();
            Dictionary<string, string> progList = programManager.GetProgramListing();

            if (progList != null)
            {
                foreach (KeyValuePair<string, string> kvp in progList)
                {
                    Console.WriteLine($"Program Name: {kvp.Key}, Comment: {kvp.Value}");
                }
            }
            else
            {
                Console.WriteLine("Failed to retrieve program listing.");
            }

            string downloadOrNot = downloadToCNC();
            Console.WriteLine(downloadOrNot);
            Console.WriteLine("\nPlease select a program to activate")
            var num = Console.ReadLine();
            _fanuc= new ProgramManager()
            var activateSuccess = _fanuc.Programs.ActivateMainProgram(_handle, progList.ElementAt(int.Parse(num)).key);
            
            if (activateSuccess)
            {
                Console.WriteLine($"{progList.ElementAt(int.Parse(num)).key} se activo correctamente. Quisieras empezar el programa?");\
                var startPrg= Console.ReadLine()

                if startPrg= "Y"

            }
            else
            {
                Console.WriteLine($"{progList.ElementAt(int.Parse(num)).key} no se pudo activar correctamente");
            }
                
            Console.ReadLine()

        }
        
        
    }
}
