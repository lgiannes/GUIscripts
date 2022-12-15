
void ActivateGPIO(){
    // Speak with GPIO and select GPIO devices:
    BoardLib.SelectUsbDevice(0);    // BE CAREFUL: the 'usbdevice' number may change!
    BoardLib.SetBoardId(126);
    for(int i=0;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
    for(int i=14;i<16;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
}

void ActivateFEB(){
    // Speak with FEB+oldGPIO and select FEB devices:
    BoardLib.SelectUsbDevice(1);     // BE CAREFUL: the 'usbdevice' number may change!
    BoardLib.SetBoardId(3);
    for(int i=0;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
    for(int i=14;i<16;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
}

string CreateOutputFile(int SN,string whichtest, string output_path){
    
    string SNfolder_path = output_path + "SN_" +SN.ToString();
    string TESTfolder_path = output_path + "SN_" +SN.ToString() + "/" +whichtest+"_TEST/";
    var SNfolder = System.IO.Directory.CreateDirectory(SNfolder_path);
    var TESTfolder = System.IO.Directory.CreateDirectory(TESTfolder_path);
    DateTime now = DateTime.Now;
    string OutFile_Name = TESTfolder_path + whichtest+"_TEST_"+ now.Year.ToString() + "_"+
                                                        now.Month.ToString() + "_"+
                                                        now.Day.ToString() + "-"+
                                                        now.Hour.ToString() + "_"+
                                                        now.Minute.ToString() + "_"+
                                                        now.Second.ToString() +".txt";
    
    File.AppendAllText(@OutFile_Name, "IO TEST for FEB Serial Number "+SN.ToString() + Environment.NewLine);
    return OutFile_Name;

}


//using system.IO;

void ScriptMain(){
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //                                    SETTINGS
    // Set the configuration file:
    string config_path = "C:/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/GPIO_FEB/config/loopbacks.xml";


    // Set the output folder, 
    string output_path = "E:/Data_FEB/FCT_output/"; 

    // Serial number of FEB under test. To be inserted from user at the beginning of the script
    int SN = -999;

    // Set the folder where data are stored 
    string data_path = "E:/Data_FEB/FCT_data/";

    // Size of data file
    int FileSize = 4 * (int) 1e3; // in kB

    // name of the output daq file 
    string file_name = "try1.daq";
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    // send Board tab settings
    BoardLib.OpenConfigFile(config_path);
    BoardLib.SetVariable("Board.UsbEthParam.FileLimit", FileSize);

    // Ask the user fot the FEB Serial Number
    SN = Dialog.ShowInputDialog<int>("Insert Serial number of FEB under test.");

    // Generate output txt file
    string OutFile_Name = CreateOutputFile(SN,"IO",output_path);

    // Show available devices
    //Dialog.ShowDialog("Devices:\n" + BoardLib.GetUsbDevices());
    ActivateGPIO();

    // STEP 1: set DFT bit
    // STEP 2: LB test on FEB busy
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ForceBusyEn", true);
    BoardLib.BoardConfigure();
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    bool LB_FEBbusy = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-BUSY-OD");
    if(!LB_FEBbusy){
        File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack test FAILED" + Environment.NewLine);
        Dialog.ShowDialog("Loopback on FEB-busy failed");
        return;
    }else{
        File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack test SUCCESFUL" + Environment.NewLine);
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ForceBusyEn", false);
    // STEP 3: LB test on FEB TRIG
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", true);
    BoardLib.BoardConfigure();
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    bool LB_FEBtrig = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT");
    if(!LB_FEBtrig){
        File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack test FAILED" + Environment.NewLine);
        Dialog.ShowDialog("Loopback on FEB-trig failed");
        return;
    }else{
        File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack test SUCCESFUL" + Environment.NewLine);
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    // STEP 3.1: hardware action
    Dialog.ShowDialog("Set Jumper on J13 to 1-2");
    // STEP 4: LB test on TRIG OD
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", true);
    BoardLib.BoardConfigure();
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    bool LB_FEBtrig_OD = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT-OD");
    if(!LB_FEBtrig_OD){
        File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test FAILED" + Environment.NewLine);
        Dialog.ShowDialog("Loopback on FEB-trig-OD failed");
        return;
    }else{
        File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test SUCCESFUL" + Environment.NewLine);
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    // STEP 4.1: hardware action
    Dialog.ShowDialog("Set Jumper on J13 to 2-3");
    // STEP 5: Set SUM_or32

    
    
}