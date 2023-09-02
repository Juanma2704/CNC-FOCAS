using System.Collections.Generic;

namespace FanucFocasTutorial1
{
    class ProgramManager
    {
        private ushort _handle = 0;
        private short _ret = 0;

        public ProgramManager()
        {
            // Initialize any necessary resources here
        }

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
        }
    }
}

