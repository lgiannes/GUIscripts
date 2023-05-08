
bool VERBOSE =true;

void ScriptMain(){
    System.Console.WriteLine("Enabling FEB...");
    TurnOnFEB();
    System.Console.WriteLine("Enabling HK");
    BoardLib.SetBoardId(0);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12, x_verbose:false);

    bool loop = true;
    double read=0;
    double pre_read=0;
    int count = 0;

    var watch = System.Diagnostics.Stopwatch.StartNew();
    var elapsedMs = watch.ElapsedMilliseconds;
    int mins = 20;
    double mins_sleep = 0.3;

    int SN = Dialog.ShowInputDialog<int>("Insert Serial number of FEB under test.");       


    System.Console.WriteLine("Starting loop");

    string HK_values = "/home/neutrino/FCT/FCT_SteadyState/SN_" +SN.ToString() + "steadystate.csv";
    while(elapsedMs<1000*60*mins){
        elapsedMs = watch.ElapsedMilliseconds;
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
        read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-FPGA-Current") );
        File.AppendAllText(@HK_values, (elapsedMs/1000).ToString() + ";"+read.ToString() + Environment.NewLine);
        System.Console.WriteLine("time:"+(elapsedMs/1000).ToString()+"s\tI = "+read.ToString()+" A");
        Sync.Sleep((int)(1000*60*mins_sleep));


    }

}


void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}