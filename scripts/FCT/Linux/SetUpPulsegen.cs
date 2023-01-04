void ScriptMain(){
        var output = ExecuteBashCommand("bash fg.sh 0");
         System.Console.WriteLine(output);
         output = ExecuteBashCommand("bash fg.sh 1");
         System.Console.WriteLine(output);
         output = ExecuteBashCommand("bash fg.sh 0");
         System.Console.WriteLine(output);

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

        proc.Start();
        proc.WaitForExit();

        return proc.StandardOutput.ReadToEnd();
    }