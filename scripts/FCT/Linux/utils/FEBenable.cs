
void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}


void ScriptMain(){
    TurnOnFEB();
    System.Console.WriteLine("FEB is ON");
    BoardLib.SetBoardId(126);
    BoardLib.GetFirmwareVersion();
    BoardLib.SetBoardId(0);
    BoardLib.GetFirmwareVersion();
    System.Console.WriteLine("All is good");
}



