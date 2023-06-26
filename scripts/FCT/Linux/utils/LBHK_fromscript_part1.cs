byte FEB_BoardID = 0;

void ActivateGPIO(){
    // Speak with GPIO and select GPIO devices:
    BoardLib.SetBoardId(126);
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
}

void ActivateFEB(){
    // Speak with FEB+oldGPIO and select FEB devices:
    BoardLib.SetBoardId(0);
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
}

string CreateOutputFile(int SN,string whichtest, string output_path){
    
    string SNfolder_path = output_path + "SN_" +SN.ToString();
    string TESTfolder_path = output_path + "SN_" +SN.ToString() + "/" +whichtest+"_TEST/";
    System.Console.WriteLine("LB/HK test result in: "+TESTfolder_path);
    //var SNfolder = System.IO.Directory.CreateDirectory(SNfolder_path);
    //var TESTfolder = System.IO.Directory.CreateDirectory(TESTfolder_path);
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


bool Run_LoopBack_test(string OutFile_Name, string config_path){
    // Starting Loopback test
    System.Console.WriteLine("Starting Loopback test.");
    File.AppendAllText(@OutFile_Name, "Starting Loopback test." + Environment.NewLine);
    
    // Test EEPROM write/read 
    bool EEPROM_success = EEPROM_test(OutFile_Name);
    
    // STEP 2: LB test on FEB TRIG OD
    bool LB_FEBtrig_OD_success = FEB_trigOD_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
    BoardLib.SetBoardId(0);


    System.Console.WriteLine("----------------------------step 2 completed");       
    return LB_FEBtrig_OD_success && EEPROM_success;

}

bool EEPROM_test(string OutFile_Name){
    BoardLib.SetBoardId(0);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Write",true);
    byte value = 0,value_read=0;
    byte EEPROMaddress = 0,EEPROMaddress_read=1;
    bool EEPROM_success = false;
    int count_EEPROM = 0;
    for(int i=0;i<8;i++){
        value = (byte)(Math.Pow(2,i+1)-1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",0);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",EEPROMaddress);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",value);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        EEPROMaddress_read = BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Address");
        value_read = BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value");
        if (EEPROMaddress==EEPROMaddress_read && value==value_read){
            count_EEPROM++;
        }else{
            File.AppendAllText(@OutFile_Name, "EEPROM: address " + EEPROMaddress + " not read back correctly. Set: "+value+" read: "+value_read + Environment.NewLine);
        }
    }
    if(count_EEPROM<8){
        System.Console.WriteLine("EEPROM failed");       
        File.AppendAllText(@OutFile_Name, "EEPROM read/write test FAILED" + Environment.NewLine);
    }else if(count_EEPROM==8){
        EEPROM_success = true;
        System.Console.WriteLine("EEPROM successful");       
    }
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",0);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",EEPROMaddress);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",0);
    BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Write",false);

    return EEPROM_success;
}


bool FEB_trigOD_test(byte FEB_BoardID, string OutFile_Name){
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.TrigODLoopbackEn",true);
    BoardLib.SetBoardId(0);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    bool LB_FEBtrig_OD_success = true; 
    bool LB_FEBtrig_OD = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT-OD");
    if(!LB_FEBtrig_OD){
        File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test FAILED" + Environment.NewLine);
        System.Console.WriteLine("Loopback on FEB-trig-OD failed");
        LB_FEBtrig_OD_success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    LB_FEBtrig_OD = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT-OD");
    if(LB_FEBtrig_OD){
        File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test FAILED" + Environment.NewLine);
        System.Console.WriteLine("Loopback on FEB-trig-OD failed");
        LB_FEBtrig_OD_success = false;
    }
    if(LB_FEBtrig_OD_success){ 
        File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack: TEST SUCCESSFUL" + Environment.NewLine); 
        System.Console.WriteLine("Loopback on FEB-trig-OD successful");
        }
    
    return LB_FEBtrig_OD_success;
}


void Restore_Initial_Config(byte FEB_BoardID,string config_path){
    BoardLib.OpenConfigFile(config_path);
    SendGPIO(3);
    SendFEB();
    BoardLib.SetBoardId(126);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); 
    BoardLib.SetBoardId(0);
    BoardLib.SetBoardId(126);
    BoardLib.GetFirmwareVersion();
    BoardLib.SetBoardId(0);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config"); 

}



void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.SetBoardId(0); 
    Sync.Sleep(1500);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}

void SelectGPIOdevices(){
    // Speak with GPIO
    BoardLib.SetBoardId(126);
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
}

void SelectFEBdevices(byte FEBID=0){
    // Speak with FEB
    BoardLib.SetBoardId(126);
    BoardLib.GetFirmwareVersion();
    BoardLib.SetBoardId(FEBID);
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
}

void SendGPIO(byte x_phase){
    BoardLib.SetBoardId(126);
	 BoardLib.DeviceConfigure(13, x_verbose:false);
	 //System.Console.WriteLine("SendGPIO BoardConfigure done");
    Sync.Sleep(50);
	 BoardLib.SetVariable("GPIO.GPIO-MISC.PLL-PHASE", x_phase);
	 //Console.WriteLine(" => GPIO Phase set to " + x_phase.ToString());
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
	 BoardLib.UpdateUserParameters("GPIO.GPIO-PHASE-TUNE");
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
	 //System.Console.WriteLine("SendGPIO done");
}

void SendFEB(byte FEBID=0){
    SelectFEBdevices(FEBID);
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
}

void ScriptMainArgs(int SN){
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //                                    SETTINGS
    // Set the configuration file:
    //string config_path = "C:/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/GPIO_FEB/config/loopbacks.xml";
    string config_path = Environment.GetEnvironmentVariable("CONFIGFOLDER")+"/loopbacks_newGUI_V2.xml";


    // Set the output folder, 
    string output_path = Environment.GetEnvironmentVariable("GENERALDATADIR")+"/FEBs/"; 

    // Serial number of FEB under test. To be inserted fsum32rom user at the beginning of the script
    //int SN = -999;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    System.Console.WriteLine("Data directory: "+output_path);

    TurnOnFEB();
    System.Console.WriteLine("FEB is ON.");



    // send Board tab settings
    BoardLib.OpenConfigFile(config_path);
    SendGPIO(3);
    FEB_BoardID = 0;//Dialog.ShowInputDialog<byte>("Insert Board ID (default=0)"); 
    SendFEB(FEB_BoardID);
    Sync.Sleep(100);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);

    // Ask the user fot the FEB Serial Number
    //int SN = Dialog.ShowInputDialog<int>("Insert Serial number of FEB under test.");       
    // Generate output txt file
    string OutFile_Name = CreateOutputFile(SN,"IO",output_path);
    Sync.Sleep(5000);
    // Check 150k resistor. DO NOT GO ON if this test fails
    // HV on PS: 10 V
    // Set the HV on the channels to 9 V
    // Check that the HV is 0 on all channels (or less than 2 V)
    bool R150K=false;
    double thr=3;//V
    int count = 0;
    double HV_set_d = 9;
    UInt16 HV_set = Convert.ToUInt16(Math.Round(HV_set_d*65535/102.46));
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set);
    }

    BoardLib.SetBoardId(126);
    BoardLib.GetFirmwareVersion();
    BoardLib.SetBoardId(0);
    BoardLib.DeviceConfigure(11, x_verbose:false);
    Sync.Sleep(500);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12, x_verbose:false);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    double Bkp_HV = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-BKP-HV") );
    double HV_read_volts=0;
    if(Bkp_HV>11.7 || Bkp_HV<8.3){
        System.Console.WriteLine("\n\n");       
        System.Console.WriteLine("Error on input HV. Set input HV to 10 V and re start the test. ");       
        System.Console.WriteLine("    Use ctrl C twice to abort.");       
        System.Console.WriteLine("");       
        return;
    }
    for(int i = 0;i<8;i++){
        HV_read_volts = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+i.ToString()+".MPPC-HV") );
        if( (HV_read_volts > thr) ){
            System.Console.WriteLine("Warning: HV on channel "+i.ToString()+" is above "+thr.ToString()+" V. (Measured: "+HV_read_volts+" V). bkp HV is "+Bkp_HV.ToString()+" V.");       
            R150K = false;
        }else{
            System.Console.WriteLine("Info: On channel "+i.ToString()+" Measured: "+HV_read_volts+" V.\t bkp HV is "+Bkp_HV.ToString()+" V.");       
            count++;
        }
    }
    if (count==8){
        R150K=true;
    }
    if(!R150K){
        System.Console.WriteLine("");       
        System.Console.WriteLine("Resistor at HV input is NOT 150K! Abort test for protection.");  
        System.Console.WriteLine(" DO NOT APPLY 55 V ");  
        File.AppendAllText(@OutFile_Name,"ERROR: WRONG RESISTOR ON CURRENT LIMITER DETECTED.");
        System.Console.WriteLine("");       
        return;
    }

    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",false);
    BoardLib.DeviceConfigure(12, x_verbose:false);




    System.Console.WriteLine("----------------------------step 1 (initialization) completed");       
    bool LB_success=false;

    LB_success = Run_LoopBack_test(OutFile_Name,config_path);

    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    

    return;
}



