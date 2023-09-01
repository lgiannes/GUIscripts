
void TurnOnFEB(){    
    // This is a special "TurnOnFEB": it also enables the FUnctional Test, and thus sends an error 
    // This is necessary with the VST+QC fw.
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    System.Console.WriteLine("GPIO-MISC set up");

       BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    System.Console.WriteLine("sent Update GPIO MISC: FEB is on");
    Sync.Sleep(1000);
       BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
    System.Console.WriteLine("FPGA-MISC set up for Functional Test");
    Sync.Sleep(1000);
    
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
       BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 

    System.Console.WriteLine("updated FPGA MISC");
    Sync.Sleep(1500);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
       BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}


void ScriptMain(){
    TurnOnFEB();
       BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(126);
    BoardLib.GetFirmwareVersion();
       BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0);
    BoardLib.GetFirmwareVersion();
    System.Console.WriteLine("All is good");
}



