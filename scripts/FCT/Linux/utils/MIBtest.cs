
void ScriptMainArgs(int SN){
    
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //                                    SETTINGS
    // Set the configuration file:
    //string config_path = "C:/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/GPIO_FEB/config/loopbacks.xml";
    string config_path = Environment.GetEnvironmentVariable("CONFIGFOLDER")+"/loopbacks_newGUI_V2.xml";
    string OpenShort_config = Environment.GetEnvironmentVariable("CONFIGFOLDER")+"/config_FCT2_newGUI_V2.xml";

    // Set the output folder, 
    string output_path = Environment.GetEnvironmentVariable("GENERALDATADIR")+"/MIBs/"; 

    // Serial number of FEB under test. To be inserted fsum32rom user at the beginning of the script
    //int SN = -999;

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    
    output_path = output_path + "SN_"+SN.ToString()+"/";
    var SNfolder = System.IO.Directory.CreateDirectory(output_path);

    System.Console.WriteLine("Preparing test ... ");
    TurnOnFEB();

    System.Console.WriteLine("FEB is on.");


    string OutFile_Name = GenerateMIBOutputFile(SN,output_path);

    if (HouseKeepingMIBtest(OutFile_Name,config_path)){
        File.AppendAllText(@OutFile_Name,"SUCCESSFUL"+Environment.NewLine);
    }

    if (MIB_Debug_test(0, OutFile_Name)){
        File.AppendAllText(@OutFile_Name,"SUCCESSFUL"+Environment.NewLine);
    }

    BoardLib.OpenConfigFile(OpenShort_config);
    SetDefaultDirectParameters();
    OpenShortMIBtest(SN, output_path);

    // Turn off FEB
    //TurnOffFEB();
    // Turn off Pulse Gen at the end
    var BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    System.Console.WriteLine("Pulse Generator OFF");

    //Generate dummy file at the end of the script
    string[] o = {"END OF SCRIPT"};
    File.WriteAllLinesAsync(output_path+"MIB_"+SN+"_EndOfScript.txt",o); 
    System.Console.WriteLine("END OF SCRIPT");

    return;


}

void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    //Sync.Sleep(50);
    BoardLib.SetVariable("GPIO.GPIO-MISC.PLL-PHASE", 3);
    Sync.Sleep(50);

    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(2000);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); Sync.Sleep(1); //Sync.Sleep(1);
    //Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(3000);
}

void SetDefaultDirectParameters(){
    BoardLib.SetVariable("Board.DirectParam.ExtClkEn", true);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", false);
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetVariable("Board.DirectParam.GtEn", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmConfLock", false);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmReset", true);
    BoardLib.SetVariable("Board.DirectParam.IGEn", false);
    BoardLib.SetVariable("Board.DirectParam.RstL1Fifo", true);
    BoardLib.SetVariable("Board.DirectParam.RstL1Fifo", true);
    BoardLib.SetVariable("Board.DirectParam.GateIdRst", true);
    BoardLib.SetVariable("Board.DirectParam.GtsIdRst", true);
    BoardLib.SetVariable("Board.DirectParam.ReadoutSMRst", true);
}

string GenerateMIBOutputFile(int SN,string output_path){
    string OutFile_Name = output_path + "MIB_"+SN+"_TestResult.txt";
    File.WriteAllText(@OutFile_Name, "TEST MIB Serial Number "+SN.ToString() + Environment.NewLine);
    return OutFile_Name;
}

bool HouseKeepingMIBtest(string OutFile_Name,string config_path){
    // Starting Housekeeping test
    System.Console.WriteLine("Starting Housekeeping test.");
    File.AppendAllText(@OutFile_Name,Environment.NewLine+"Housekeeping: ");
    // Initialize: HV to 0 V;
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
    }
    Sync.Sleep(50); BoardLib.SetBoardId(0); Sync.Sleep(50);
    BoardLib.DeviceConfigure(11, x_verbose:false);
    Sync.Sleep(500);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    Sync.Sleep(50);BoardLib.SetDirectParameters();
    Sync.Sleep(1000);

    // 1: High Voltage measurement DAC-ADC test
    System.Console.WriteLine("HV test");

    bool HVA_ADC_success1 = true;
    bool HVA_ADC_success2 = true;
    bool HVA_ADC_success = true;
    bool HVB_ADC_success1 = true;
    bool HVB_ADC_success2 = true;
    bool HVB_ADC_success = true;

    // Set high voltages
    double HighestHV = 3;//V (not used in MIB)
    double[] HVs_volts = new double[8];

    // Test HVA
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-MISC.HVAB-SW",true);//true=HVA
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    // Test two different HV value for each ASIC. -> two loops
    for(int i=0;i<8;i++){
        HVs_volts[i] = 1.8 + 1.2*(i%2);
    }
    HVA_ADC_success1 = HV_test(HVs_volts,OutFile_Name);
    for(int i=0;i<8;i++){
        HVs_volts[i] = 1.8 + 1.2*((i+1)%2);
    }
    HVA_ADC_success2 = HV_test(HVs_volts,OutFile_Name);
    // SUCCESS if both are successful
    HVA_ADC_success = (HVA_ADC_success1 && HVA_ADC_success2);
    if(!HVA_ADC_success){
        System.Console.WriteLine("HV(A) setting/reading test FAILED");
    }

    // Test HVB
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-MISC.HVAB-SW",false);//false=HVB   
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    // Test two different HV value for each ASIC. -> two loops
    for(int i=0;i<8;i++){
        HVs_volts[i] = 1.8 + 1.2*(i%2);
    }
    HVB_ADC_success1 = HV_test(HVs_volts,OutFile_Name);
    for(int i=0;i<8;i++){
        HVs_volts[i] = 1.8 + 1.2*((i+1)%2);
    }
    HVB_ADC_success2 = HV_test(HVs_volts,OutFile_Name);
    // SUCCESS if both are successful
    HVB_ADC_success = (HVB_ADC_success1 && HVB_ADC_success2);
    if(!HVB_ADC_success){
        System.Console.WriteLine("HV(B) setting/reading test FAILED");
    }


    // Reset HVs to 0 V;
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
    }
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.DeviceConfigure(11, x_verbose:false);
    Sync.Sleep(500);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    
    Restore_Initial_Config(config_path);

    bool MPPCTempA_success = MPPCTemp_test(OutFile_Name);


    return ( HVA_ADC_success && HVB_ADC_success && MPPCTempA_success);
}

bool MPPCTemp_test(string OutFile_Name){
    BoardLib.SetBoardId(0);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12, x_verbose:false);
    int SUT;//Sensor under test
    double temp_ON = 69.96;//degrees
    double temp_OFF = 3.67;//degrees
    double tolerance = 1;//degrees
    bool s1=false,s2=false;
    int s1_count=0,s2_count=0;
    bool MPPCTemp_success;
    double read, temp_ON_read=0,temp_OFF_read=0;
    //File.AppendAllText(@OutFile_Name,"----MPPC Temperature test starting" + Environment.NewLine);

    for(SUT=0;SUT<8;SUT++){
        s1=false;
        s2=false;
        BoardLib.SetBoardId(126);
        BoardLib.SetVariable("GPIO.GPIO-MISC.TSEN-SW",(byte)(Math.Pow(2,SUT)));
        BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
        BoardLib.SetBoardId(0);
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
        read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+SUT.ToString()+".MPPC-Temp") );
        if(read<temp_ON+tolerance && read>temp_ON-tolerance){
            s1=true;
            temp_ON_read = read;
            s1_count++;
        }else{
            File.AppendAllText(@OutFile_Name,Environment.NewLine+"Temperature sensor failed on Sensor "+SUT.ToString()+" - temp="+read.ToString()+" instead of "+temp_ON.ToString() + Environment.NewLine);
        }
        BoardLib.SetBoardId(126);
        BoardLib.SetVariable("GPIO.GPIO-MISC.TSEN-SW",0);
        BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
        BoardLib.SetBoardId(0);
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
        read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+SUT.ToString()+".MPPC-Temp") );
        if(read<temp_OFF+tolerance && read>temp_OFF-tolerance){
            s2=true;
            temp_OFF_read = read;
            s2_count++;
        }else{
            File.AppendAllText(@OutFile_Name,Environment.NewLine+"Temperature sensor failed on Sensor "+SUT.ToString()+" - temp="+read.ToString()+" instead of "+temp_OFF.ToString() + Environment.NewLine);
        }

        if(s1 && s2){
            //File.AppendAllText(@OutFile_Name,"Sensor "+SUT.ToString()+" OK. Temp_ON="+temp_ON_read.ToString()+" Temp_OFF="+temp_OFF_read.ToString() + Environment.NewLine);
        }
    }

    if(s1_count==8 && s2_count==8){
        MPPCTemp_success=true;
    }else{
        MPPCTemp_success=false;
    }
    return MPPCTemp_success;
}

bool HV_test(double[] HVs_volts,string OutFile_Name){
    bool success = true;
    // conversion factor: there are two different conversion, one to set the HV and one to get the HV measurement
    double CF_set = 65535/102.46;
    double CF_get = (1+1000/29.4)*3/Math.Pow(2,18);
    int HV_set_GUI;
    UInt32 HV_read_au;
    double HV_read_volts;

    double Delta = 0.4;//V
    for(int i = 0;i<8;i++){
        HV_set_GUI = (int) (HVs_volts[i] * CF_set);
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set_GUI);
        //System.Console.WriteLine(HV_set_GUI.ToString());
    }
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.DeviceConfigure(11, x_verbose:false);
    Sync.Sleep(500);
    //File.AppendAllText(@OutFile_Name,Environment.NewLine + "----Starting HV DAC-ADC test" + Environment.NewLine);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC-DPRAM");
    for(int i = 0;i<8;i++){
        // HV_read_au = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.HV-Channels.CH"+i.ToString()+".HV");
        // HV_read_volts = HV_read_au*CF_get;
        HV_read_volts = Convert.ToDouble( BoardLib.GetFormulaVariable("GPIO.GPIO-ADC-DPRAM.HV-Channels.CH"+i.ToString()+".HV") );
        if(HV_read_volts < HVs_volts[i]+Delta && HV_read_volts > HVs_volts[i]-Delta){
            //File.AppendAllText(@OutFile_Name, "Hv measurement in ASIC"+ i.ToString() + ": successful! -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+ Environment.NewLine);
        }else{
            File.AppendAllText(@OutFile_Name, Environment.NewLine + "Hv measurement in ASIC"+ i.ToString()+": failed. -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+Environment.NewLine);
            success = false;
        }
    }

    //At the end, reset everything to 0
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
    }
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.DeviceConfigure(11, x_verbose:false);
    Sync.Sleep(100);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    return success;

}

void Restore_Initial_Config(string config_path){
    BoardLib.OpenConfigFile(config_path);
    SendGPIO(3);
    SendFEB();
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); 
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config"); 
}

bool MIB_Debug_test(int FEB_BoardID, string OutFile_Name){
    File.AppendAllText(@OutFile_Name,Environment.NewLine+"Debug: ");
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.MIBdbgFromAddrEn",true);
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.MIBdbgAddr75Sel",false);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);

    BoardLib.SetBoardId(126); Sync.Sleep(1);
    byte address=0;
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",address);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    FEB_BoardID = address;
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    Sync.Sleep(500);
    bool MIBdebug_success = true;
    byte LB_address = BoardLib.GetByteVariable("GPIO.GPIO-STATUS.MIBDebug");
    if(address != LB_address){
        File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug FAILED: " + (address).ToString()+" != "+(LB_address).ToString() + Environment.NewLine);
        System.Console.WriteLine("Loopback on MIB Debug FAILED: " + address.ToString()+" != "+LB_address.ToString());
    }
    for(int i=0;i<5;i++){
        address = (byte)Math.Pow(2,i);
        BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",address);
        BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
        FEB_BoardID = address;
        BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
        LB_address = BoardLib.GetByteVariable("GPIO.GPIO-STATUS.MIBDebug");
        if(address != LB_address){
            File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug FAILED: " + (address).ToString()+" != "+(LB_address).ToString() + Environment.NewLine);
            MIBdebug_success = false;
            System.Console.WriteLine("Loopback on MIB Debug FAILED: " + address.ToString()+" != "+LB_address.ToString());
        }
    }
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.MIBdbgAddr75Sel",true);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    for(int i=5;i<8;i++){
        address = (byte)Math.Pow(2,i);
        BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",address);
        BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
        FEB_BoardID = address;
        BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
        LB_address = BoardLib.GetByteVariable("GPIO.GPIO-STATUS.MIBDebug");
        //LB_address = (byte) (LB_address << 3);
        if(address>>3 != LB_address){
            File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug FAILED: " + (address>>3).ToString()+" != "+(LB_address).ToString() + Environment.NewLine);
            MIBdebug_success = false;
            System.Console.WriteLine("Loopback on MIB Debug FAILED: " + (address>>3).ToString()+" != "+(LB_address).ToString());
        }
    }
    //if(MIBdebug_success){ File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug: TEST SUCCESSFUL " + Environment.NewLine); }



    return MIBdebug_success;
}

int OpenShortMIBtest(int SN,string output_path){
    string daqfile = output_path + "MIB_"+SN+"_openshort";

    // Enable preamp and DAQ on all channels
    ActivateAllCh(56,12);
    System.Console.WriteLine("FEB is configured");

    // Set up communication with Pulse gen
    var BashOutput = ExecuteBashCommand("bash fg_setup.sh");
    //Sync.Sleep(50);
    BashOutput = ExecuteBashCommand("echo \"OUTPUT ON\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT ON\" | cat > /dev/ttyACM0");
    //Sync.Sleep(50);
    if(string.Compare(BashOutput,"error: no device connected\n")==0){
        System.Console.WriteLine(BashOutput);
    }else{
        System.Console.WriteLine("Pulse gen is configured");
    }

    int AcqTag = RunMIBAcquisition(daqfile);

    return AcqTag;

}


int RunMIBAcquisition(string MIBdaqFileName){
    //Sync.Sleep(500);                                                                    

    int baseline = 32786;
    
    string file_name = MIBdaqFileName;
    
    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.DeviceConfigure(8, x_verbose:false);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    //Sync.Sleep(5);
    BoardLib.SetDirectParameters(); //Sync.Sleep(3);

    BoardLib.SetBoardId(126); //Sync.Sleep(1); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(200);                                                                    
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    if(BoardLib.StartAcquisition(file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }

    //Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(file_name,true);
    }

    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    while(GateEn){
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        //Sync.Sleep(200); 
        GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");  
    }

    //Sync.Sleep(20);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(300);                                                                   
    double Tot_KB_Previous_Iter = 0;
    double Tot_KB = 0;
    DateTime LastIter=DateTime.Now, ThisIter=DateTime.Now;
    
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n ");
    for(int i=0;i<32;i++){
        int channel = i;
        System.Console.Write("\r gate: "+channel.ToString()+" | ");
        SetKaladin_8chs(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); //Sync.Sleep(1); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");

        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); //Sync.Sleep(1); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.Write("rate: "+Math.Truncate(rate)+" kB/s | ");
        if(rate<10){
            System.Console.WriteLine("\n+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            return -999;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;

        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(channel,32));

    }
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n\n");

    EndOfRunProtocol();
    return 0;

}

void SetKaladin_8chs(int channel){
    if(channel>31){
        System.Console.WriteLine("Error in Kaladin set channel");
        return;
    }
    int loc_ch = channel;        // Channel within the ASIC
    int loc_MUX = loc_ch/8;         // Which MUX within the ASIC (4 MUXs per ASIC) 
    int Kal_MUX_output = loc_ch%8;  // Which channel output within the MUX (8 output, selected with 3bits number)
    uint Kal_En_hex=0;
    
    int MUX =0;
    uint HEX_value=0;
    for(int asic=0;asic<8;asic++){
        MUX = asic*4 + loc_MUX;     // Global MUX (32 in total, 4 per ASIC)
        HEX_value = HEX_value + (uint)Math.Pow(2,MUX);
    }

    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-EN", HEX_value); // the GUI does automatically the conversion dec-to-hex. DO NOT FEED WITH A HEX VALUE
    
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-MUX", Kal_MUX_output); // only depends on local channel
    
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");

}

void EndOfRunProtocol(){
    BoardLib.StopAcquisition();
    System.Console.WriteLine("END OF ACQUISITION");
    
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    //Sync.Sleep(10);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(100);
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    while(GateEn){
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        //Sync.Sleep(200); 
        BoardLib.ReadStatus();
        GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");  
    }
    System.Console.WriteLine("Stopped GTS beacon");
    BoardLib.WaitForEndOfTransfer(true);

}

void ActivateAllCh(int LG_gain,int HG_gain){
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    for (int i_ch = 0; i_ch < 256; i_ch++){
        int asic=i_ch/32;
        int local_ch=i_ch%32; 
    
        // En32Trigger
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].GlobalControl.En32Trigger", true);
        // EnOR32
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].GlobalControl.EnOR32", true);
        // EnNOR32
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].GlobalControl.EnNOR32", false);
        // DAC4bTrigger_t
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].DAC4bTrigger_t", 0);

        // DAC4bTrigger
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].DAC4bTrigger", 0);

        // inputDAC
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].inputDAC", 0);

        // inputDAC_En
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].inputDAC_En", true);
        
        // LG_Gain
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].LG_Gain", LG_gain);

        // HG_Gain
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].HG_Gain", HG_gain);

        // HG_CTest
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].HG_CTest", false);

        // LG_CTest
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].LG_CTest", false);

        // PA_DIS
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].PA_DIS", false);

        // DiscriMask
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].Channels[" + local_ch.ToString() +
                                "].DiscriMask", false);

        // Hit_En
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC" +
                                asic.ToString() +
                                ".Channels[" + local_ch.ToString() +
                                "].Hit_En", true);

        // HG_En
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC" +
                                asic.ToString() +
                                ".Channels[" + local_ch.ToString() +
                                "].HG_En", true);

        // LG_En
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC" +
                                asic.ToString() +
                                ".Channels[" + local_ch.ToString() +
                                "].LG_En", true);

        // OR256tAdcEn
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC" +
                                asic.ToString() +
                                ".Channels[" + local_ch.ToString() +
                                "].OR256tAdcEn", false);
    }

    SendFEB();    
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

void SelectFEBdevices(byte FEBID=0){
    // Speak with FEB
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
	 System.Console.WriteLine("SendGPIO BoardConfigure done");
    Sync.Sleep(50);
	 BoardLib.SetVariable("GPIO.GPIO-MISC.PLL-PHASE", x_phase);
	 Console.WriteLine(" => GPIO Phase set to " + x_phase.ToString());
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
	 BoardLib.UpdateUserParameters("GPIO.GPIO-PHASE-TUNE");
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
	 System.Console.WriteLine("SendGPIO done");
}

void SendFEB(byte FEBID=0){

    SelectFEBdevices(FEBID);
    BoardLib.SetBoardId(0); //Sync.Sleep(3);
    BoardLib.BoardConfigure();
    //Sync.Sleep(50);
}

static string ExecuteBashCommand(string command){
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

string GenerateProgressString(int p, int t){
    int percent4 = (int)Math.Ceiling( 25*((double)(p+1)/(double)t) );
    if(percent4>25) percent4 = 25;
    string progressString = "[";
    string bars = new string(char.Parse("|"),percent4);
    string spaces = new string(char.Parse(" "),25-percent4);

    progressString = progressString + bars + spaces + "]" + 4*percent4 + "%";

    return progressString;
}

