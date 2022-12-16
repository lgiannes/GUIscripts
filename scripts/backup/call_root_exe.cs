void ScriptMain(){

    System.Diagnostics.Process p = new System.Diagnostics.Process();
    System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo{
        FileName = "cmd.exe", RedirectStandardInput = true, UseShellExecute = false
    };
    p.StartInfo = info;
    p.Start();

    //System.Console.WriteLine(p.StandardInput.BaseStream.CanWrite);
    
    if(p.StandardInput.BaseStream.CanWrite)
    {
        //p.StandardInput.WriteLine(@"wsl cd; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f HV34859_freq10M_pow530.daq > output.txt; exit");
        if(Environment.Is64BitProcess)
        {
            System.Console.WriteLine("is 64bit process");
            //p.StandardInput.WriteLine(@"bash -c ""cd; echo dcscas > output.txt; exit"" ");
            //p.StandardInput.WriteLine(@"bash -c ""cd; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f HV34859_freq10M_pow530.daq > unpacking_output.txt; exit"" ");
            p.StandardInput.WriteLine(@"bash -c ""cd; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f /mnt/c/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/root_exe/daq_test_root_exe/HV34859_freq10M_pow530.daq > unpacking_output.txt; exit"" ");
            p.StandardInput.Flush();
            p.StandardInput.Close();
        }else{
            System.Console.WriteLine("is a 32bit process");
            //p.StandardInput.WriteLine(@"C:\Windows\Sysnative\bash.exe -c ""cd; echo dcscas > output.txt; exit"" ");
            //p.StandardInput.WriteLine(@"C:\Windows\Sysnative\bash.exe -c ""cd; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f HV34859_freq10M_pow530.daq > output.txt; exit"" ");
            //p.StandardInput.WriteLine(@"C:\Windows\Sysnative\bash.exe -c ""cd; cp /mnt/c/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/root_exe/daq_test_root_exe/HV34859_freq10M_pow530.daq . "" ");
            p.StandardInput.WriteLine(@"C:\Windows\Sysnative\bash.exe -c ""cd; cp /mnt/c/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/root_exe/daq_test_root_exe/HV34859_freq10M_pow530.daq .; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f HV34859_freq10M_pow530.daq > output.txt; exit"" ");
            p.StandardInput.Flush();
            p.StandardInput.Close();            
        }
    }
    
}