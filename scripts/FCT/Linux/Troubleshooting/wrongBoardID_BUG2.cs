// BUG: Wrong board ID prompted on SetDirectParameters.
// This bugs happens every second time you launch the script, regardless of the sleep time
    string file_name = "/DATA/neutrino/FCT/Troubleshooting/test_drive_UpdateUserParam_sleeptime";
    string config_path = "/home/neutrino/FCT/code/config/config_FCT2_newGUI_V2.xml";


void ScriptMain(){
    

    TurnOnFEB();
    System.Console.WriteLine("FEB is on");
    
    BoardLib.OpenConfigFile(config_path);
    SendGPIO();
    // Set the required Direct Parameters
    BoardLib.SetVariable("Board.DirectParam.ExtClkEn", true);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", false);
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetVariable("Board.DirectParam.GtEn", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmConfLock", false);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmReset", false);
    BoardLib.SetVariable("Board.DirectParam.IGEn", false);
    // Send to board
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.SetDirectParameters(); Sync.Sleep(1);




    return;
}



void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); Sync.Sleep(1); Sync.Sleep(1);
    Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(150);
}

void SendGPIO(){
    SelectGPIOdevices();
    BoardLib.SetBoardId(126); Sync.Sleep(3);
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
}
void SelectGPIOdevices(){
    // Speak with GPIO
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
}
