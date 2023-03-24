void ScriptMain(){
    string exe_path = "/home/neutrino/FCT/FunctionalTest/bin/FuncTest";
    string Data_path = "/home/neutrino/FCT/data_local/";
    string Data_file_name = "FCT_os_LG56HG12amp30mV_bl32786.daq";
    string setup_path = "/home/neutrino/FCT/FunctionalTest/setup.sh";
    string log_file = Data_path+"log.txt";
    int SerialNumber = 1;
    System.Console.WriteLine("starting..");
    var BashOutput = ExecuteBashCommand("source "+ setup_path);
    System.Console.WriteLine(exe_path + 
    " -f " + Data_path + Data_file_name +
    " -s" +SerialNumber.ToString() + " >> "+ log_file);

    BashOutput = ExecuteBashCommand(exe_path + 
    " -f " + Data_path + Data_file_name +
    " -s" +SerialNumber.ToString() + " >> "+ log_file);
    Sync.Sleep(10000);
    System.Console.WriteLine(BashOutput.ToString());

}


    static string ExecuteBashCommand(string command)
    {
        // according to: https://stackoverflow.com/a/15262019/637142
        // thans to this we will pass everything as one command
        command = command.Replace("\"","\"\"");

        var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \""+ command + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
    System.Console.WriteLine("1");

        proc.Start();
    System.Console.WriteLine("2");

        proc.WaitForExit();
    System.Console.WriteLine("3");

        return proc.StandardOutput.ReadToEnd();
    }