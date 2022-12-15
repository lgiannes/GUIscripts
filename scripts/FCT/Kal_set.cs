void ScriptMain(){
    BoardLib.StartAcquisition("C:/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/dataFCT/try2.daq",true); 
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-MUX", 2);
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-EN", 2);
    BoardLib.SetBoardId(126); 
    Sync.Sleep(1); // SLEEP BEFORE UPDATING
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.StopAcquisition();

}