void ScriptMain(){
    // Linearity:
    //take data injecting channels with several amplitudes and gain values

    // first attempt:
    // HG,LG = 30, 40 50
    // Amps (as set on the pg)= 15, 30, 60, 120, 240, 480, 960, 1920 
 
    // The script takes a number of daq = N_amps*N_gains

    int SN=256;// to be passed as argument

    TurnOnFEB();
    string pathToCsvFiles = "/home/neutrino/FCT/FCT_database/FEBs/SN_"+SN.ToString()+"/"; 


    // System.IO.Directory.CreateDirectory(pathToCsvFiles);
    // pathToCsvFiles = pathToCsvFiles + "Linearity/";
    // System.IO.Directory.CreateDirectory(pathToCsvFiles);

    // string ResultsCsv = pathToCsvFiles + "Results.csv";
    // var go = new FileStream(ResultsCsv, FileMode.Create);
    // go.Dispose();

    // System.Console.WriteLine(pathToCsvFiles);



}