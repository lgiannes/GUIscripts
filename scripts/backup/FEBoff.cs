void ScriptMain(){
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}