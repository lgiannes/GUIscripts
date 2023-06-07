byte FEB_BoardID = 0;

void ActivateGPIO(){
    // Speak with GPIO and select GPIO devices:
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
}

void ActivateFEB(){
    // Speak with FEB+oldGPIO and select FEB devices:
    BoardLib.SetBoardId(0); Sync.Sleep(1);
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


bool Run_LoopBack_test(string OutFile_Name, string config_path){
        // Starting Loopback test
    // System.Console.WriteLine("Starting Loopback test.");
    // File.AppendAllText(@OutFile_Name, "Starting Loopback test." + Environment.NewLine);
    byte address = 0;
    // STEP 1: Enable LOOPBACK MODE (already done loading the config file)
        // BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
        // SelectFEBdevices();
        // BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    bool MIBdebug_success=false, FEBbusy_success=false, FEBtrig_success=false, LB_FEBtrig_OD_success=false, SUM32_success=false, SEL_test_success=false;

    // STEP 2: MIB Debug test
    MIBdebug_success = MIB_Debug_test(FEB_BoardID,OutFile_Name);
    // STEP 2.1: SEL test. Leave this test here, DO NOT restore initial config, it need the same config as before
    SEL_test_success = SEL_test(OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
                                                                        System.Console.WriteLine("----------------------------step 3 completed");       
    // STEP 3: LB test on FEB Busy 
    FEBbusy_success = FEB_busy_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
                                                                        System.Console.WriteLine("----------------------------step 4 completed");       
    // STEP 4: LB test on FEB TRIG
    FEBtrig_success = FEB_trig_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
   

    // STEP 6: SUM_or32 test: FEB ADDRESS 
    SUM32_success = SUM_or32_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
                                                                        System.Console.WriteLine("----------------------------step 5 completed");       
    
    System.Console.WriteLine(MIBdebug_success+" "+FEBbusy_success+" "+FEBtrig_success+" "+SUM32_success+" "+SEL_test_success);
    return (MIBdebug_success && FEBbusy_success && FEBtrig_success && SUM32_success && SEL_test_success);
    

}

bool SUM_or32_test(byte FEB_BoardID, string OutFile_Name){
    byte address;
    byte Initial_BID = FEB_BoardID;
    File.AppendAllText(@OutFile_Name, "starting 'SUM_or32 from FEB-ADDRESS' test."+ Environment.NewLine);
    // Test preparation
    bool SUM32_success = true;
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.SumOr32En",true);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",false);

    //UInt32 ADC_read; //Not used if the formulae are already implemented in the GUI
    double sum32_V=0;
    for(int i=0;i<5;i++){
        address = (byte)(Math.Pow(2,i)-1); 
        // here enable feb
        BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",address);
        BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
        FEB_BoardID = address;
        BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
        BoardLib.UpdateUserParameters("GPIO.GPIO-ADC-DPRAM");
        sum32_V = Convert.ToDouble( BoardLib.GetFormulaVariable("GPIO.GPIO-ADC-DPRAM.Others.Sum32") );
        if( !read_IsInRange(address,sum32_V,OutFile_Name) ){
            File.AppendAllText(@OutFile_Name, "SUM_or32 ADC test FAILED at FEB-ADDRESS = "+ address.ToString() + Environment.NewLine + "----------------"  + Environment.NewLine);
            System.Console.WriteLine("SUM_or32 FAILED");
            SUM32_success = false;
        }else{
            File.AppendAllText(@OutFile_Name, "SUM_or32 ADC test. FEB-ADDRESS = "+ address.ToString() + ": OK!" + Environment.NewLine + "----------------"  + Environment.NewLine);
        }
        //here disable feb
    }

    //Reset the BoardID at the end 
    // (Done already in "restore_initial_config", but double check)
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",Initial_BID);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.SetBoardId((byte)((int)Initial_BID%128));
    System.Console.WriteLine("BID reset to "+ Initial_BID.ToString());       



    return SUM32_success;
}

bool FEB_trigOD_test(byte FEB_BoardID, string OutFile_Name){
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.TrigODLoopbackEn",true);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    bool LB_FEBtrig_OD_success = true; 
    bool LB_FEBtrig_OD = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT-OD");
    if(!LB_FEBtrig_OD){
        File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test FAILED" + Environment.NewLine);
        System.Console.WriteLine("Loopback on FEB-trig-OD FAILED");
        LB_FEBtrig_OD_success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    LB_FEBtrig_OD = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT-OD");
    if(LB_FEBtrig_OD){
        File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test FAILED" + Environment.NewLine);
        System.Console.WriteLine("Loopback on FEB-trig-OD FAILED");
        LB_FEBtrig_OD_success = false;
    }
    if(LB_FEBtrig_OD_success){ File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack: TEST SUCCESSFUL" + Environment.NewLine); }
    
    return LB_FEBtrig_OD_success;
}

bool FEB_trig_test(byte FEB_BoardID, string OutFile_Name){
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.TrigLoopbackEn",true);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    Sync.Sleep(500);
    bool FEBtrig_success = true;
    bool LB_FEBtrig = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT");
    if(!LB_FEBtrig){
        File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack test FAILED" + Environment.NewLine);
        System.Console.WriteLine("Loopback on FEB-trig FAILED");
        FEBtrig_success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    Sync.Sleep(500);
    LB_FEBtrig = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT");
    if(LB_FEBtrig){
        File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack test FAILED" + Environment.NewLine);
        System.Console.WriteLine("Loopback on FEB-trig FAILED");
        FEBtrig_success = false;
    }
    if(FEBtrig_success){ File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack: TEST SUCCESSFUL" + Environment.NewLine); }
    
    return FEBtrig_success;
}

bool FEB_busy_test(byte FEB_BoardID, string OutFile_Name){
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.BusyLoopbackEn",true);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ForceBusyEn", true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    bool FEBbusy_success = true;
    bool LB_FEBbusy = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-BUSY-OD");
    if(!LB_FEBbusy){
        File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack test FAILED (1)" + Environment.NewLine);
        System.Console.WriteLine("Loopback on FEB-busy FAILED (1)");
        FEBbusy_success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ForceBusyEn", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    LB_FEBbusy = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-BUSY-OD");
    if(LB_FEBbusy){
        File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack test FAILED (0)" + Environment.NewLine);
        System.Console.WriteLine("Loopback on FEB-busy FAILED (0)");
        FEBbusy_success = false;
    }
    if(FEBbusy_success){ File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack: TEST SUCCESSFUL" + Environment.NewLine); }
    
    return FEBbusy_success;
}

void Restore_Initial_Config(byte FEB_BoardID,string config_path){
    BoardLib.OpenConfigFile(config_path);
    SendGPIO(3);
    SendFEB();
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); 
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config"); 
}

bool MIB_Debug_test(byte FEB_BoardID,string OutFile_Name){
    
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
            File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug FAILED (ADDR:"+address+"): " + (address).ToString()+" != "+(LB_address).ToString() +" ->Missing pull up"+ Environment.NewLine);
            MIBdebug_success = false;
            System.Console.WriteLine("Loopback on MIB Debug FAILED (ADDR:"+address+"): " + address.ToString()+" != "+LB_address.ToString()+" ->Missing pull up");
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
        // bit 1 is the SEL line, you want to ignore it. Set bit 1 to 0
        LB_address = (byte)((int)LB_address&0b11111101);
        if(address>>3 != LB_address){
            File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug FAILED (ADDR:"+address+"): " + (address>>3).ToString()+" != "+(LB_address).ToString() +" ->Missing pull up"+ Environment.NewLine);
            MIBdebug_success = false;
            System.Console.WriteLine("Loopback on MIB Debug FAILED (ADDR:"+address+"): " + (address>>3).ToString()+" != "+(LB_address).ToString()+" ->Missing pull up");
        }
    }
    if(MIBdebug_success){ File.AppendAllText(@OutFile_Name, "Address loopback on MIB Debug (pull up resistors test): TEST SUCCESSFUL " + Environment.NewLine); }

    //Reset adress to 0 at the end
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",0);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");


    return MIBdebug_success;
}

bool SEL_test(string OutFile_Name){
    bool success = false,success1 = false;
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    // SEL line is loopbacked to bit 1 of the MIB debug connector
    byte SEL_LB = BoardLib.GetByteVariable("GPIO.GPIO-STATUS.MIBDebug");
    if ( SEL_LB == 2 ){
        File.AppendAllText(@OutFile_Name, "SEL line high test (LB on MIBdebug) SUCCESSFUL. Reading from MIB: 0b"+Convert.ToString(SEL_LB,2)+ Environment.NewLine);
        success = true;
    }else{
        System.Console.WriteLine("SEL line loopback test FAILED");
        File.AppendAllText(@OutFile_Name, "SEL line high test (LB on MIBdebug) FAILED. Reading from MIB: 0b"+Convert.ToString(SEL_LB,2)+ Environment.NewLine);
        success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    // SEL line is loopbacked to bit 1 of the MIB debug connector
    SEL_LB = BoardLib.GetByteVariable("GPIO.GPIO-STATUS.MIBDebug");
    if ( SEL_LB == 0 ){
        File.AppendAllText(@OutFile_Name, "SEL line low test (LB on MIBdebug) SUCCESSFUL. Reading from MIB: 0b"+Convert.ToString(SEL_LB,2)+ Environment.NewLine);
        success1 = true;
    }else{
        System.Console.WriteLine("SEL line loopback test FAILED");
        File.AppendAllText(@OutFile_Name, "SEL line low test (LB on MIBdebug) FAILED. Reading from MIB: 0b"+Convert.ToString(SEL_LB,2)+ Environment.NewLine);
        success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    return success&&success1;
}

bool HouseKeeping_test(string OutFile_Name,byte FEB_BoardID,string config_path, string HK_values){
    // Starting Housekeeping test
    System.Console.WriteLine("Starting Housekeeping test.");
    File.AppendAllText(@OutFile_Name,     "**************************************************************************" + Environment.NewLine 
                                        + "                     Starting Housekeeping test." 
                                        + Environment.NewLine
                                        + "**************************************************************************"
                                        + Environment.NewLine
                                        );
    // Initialize: HV to 0 V;
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
    }
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.DeviceConfigure(11, x_verbose:false);
    Sync.Sleep(500);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);



    // 1: High Voltage measurement DAC-ADC test
    System.Console.WriteLine("HV 'from GPIO' test");

    bool HV_ADC_success1 = true;
    bool HV_ADC_success2 = true;
    bool HV_ADC_success = true;

    // Set high voltages
    double HighestHV = 35;//V
    double[] HVs_volts = new double[8];
    // Test two different HV value for each ASIC. -> two loops
    for(int i=0;i<8;i++){
        HVs_volts[i] = HighestHV/8*(i)+1;
    }
    HV_ADC_success1 = HV_test(HVs_volts,OutFile_Name, HK_values);
    for(int i=0;i<8;i++){
        HVs_volts[i] = HighestHV/8*(7-i)+1;
    }
    HV_ADC_success2 = HV_test(HVs_volts,OutFile_Name, HK_values);
    // SUCCESS if both are successful
    HV_ADC_success = (HV_ADC_success1 && HV_ADC_success2);
    if(!HV_ADC_success){
        System.Console.WriteLine("HV setting/reading test FAILED");
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

    // 1.5: MPPC Temperature test
    bool MPPCTemp_success = MPPCTemp_test(OutFile_Name);

    // 2: HV short test 
    bool HVShort_success = HVShort_test(OutFile_Name, HK_values, 35); 
    
    Restore_Initial_Config(FEB_BoardID,config_path);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    // Send back the HV on the CITIROC to 0
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(500);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12, x_verbose:false);
    Sync.Sleep(200);
    
    // 3: test Temperature on the FPGA
    bool FPGAtemp_success = FPGAtemp_test(OutFile_Name, HK_values);
    // 4: test FEB temperature (0)
    bool FEBtemp0_success = FEBtemp0_test(OutFile_Name, HK_values);
    // 5: test FEB temperature (1)
    bool FEBtemp1_success = FEBtemp1_test(OutFile_Name, HK_values);
    // 6: test PMezza 2V2 temperature
    bool PMezza_2V2_success = PMezza_2V2_test(OutFile_Name, HK_values);
    // 7: test PMezza 0V9 temperature
    bool PMezza_0V9_success = PMezza_0V9_test(OutFile_Name, HK_values);
    // 8: test backplane HV
    bool bkpHV_success = bkpHV_test(OutFile_Name, HK_values);
    // 9: FPGA current test
    bool FPGAcurrent_success = FPGAcurrent_test(OutFile_Name, HK_values);
    // 10: test current in 12V from backplane
    bool I12V_success = I12V_test(OutFile_Name, HK_values); // So far this just prints the currents
    // 11: test CITIROC temperatures (x8)
    bool CITItemp_success = CITItemp_test(OutFile_Name, HK_values);
    // 12: MPPC HV test (from FEB-HK side, to test the FEB MPPC-ADC )
    bool MPPC_HV_success = MPPCHV_test(OutFile_Name);
    
    Restore_Initial_Config(FEB_BoardID,config_path);

    System.Console.WriteLine(HV_ADC_success+" "+FPGAcurrent_success+" "+MPPCTemp_success+" "+HVShort_success+" "+FPGAtemp_success+" "+FEBtemp0_success+" "+FEBtemp1_success+" "+I12V_success+" "+bkpHV_success+" "+CITItemp_success+" "+MPPC_HV_success+" "+PMezza_0V9_success+" "+PMezza_2V2_success);
    
    return ( HV_ADC_success && HVShort_success && FPGAtemp_success && MPPCTemp_success &&
             I12V_success && FEBtemp0_success && FEBtemp1_success && 
             CITItemp_success && bkpHV_success && FPGAcurrent_success &&
             MPPC_HV_success && PMezza_0V9_success && PMezza_2V2_success
           ); // && of all HK tests
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
        File.AppendAllText(@OutFile_Name,"----MPPC Temperature test starting" + Environment.NewLine);

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
            File.AppendAllText(@OutFile_Name,"Temperature sensor failed on Sensor "+SUT.ToString()+" - temp="+read.ToString()+" instead of "+temp_ON.ToString() + Environment.NewLine);
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
            File.AppendAllText(@OutFile_Name,"Temperature sensor failed on Sensor "+SUT.ToString()+" - temp="+read.ToString()+" instead of "+temp_OFF.ToString() + Environment.NewLine);
        }

        if(s1 && s2){
            File.AppendAllText(@OutFile_Name,"Sensor "+SUT.ToString()+" OK. Temp_ON="+temp_ON_read.ToString()+" Temp_OFF="+temp_OFF_read.ToString() + Environment.NewLine);
        }
    }

    if(s1_count==8 && s2_count==8){
        MPPCTemp_success=true;
    }else{
        MPPCTemp_success=false;
    }
    return MPPCTemp_success;
}

bool MPPCHV_test(string OutFile_Name){
    // Set high voltages
    bool HV_ADC_success,HV_ADC_success1,HV_ADC_success2;
    double HighestHV = 35;//V
    double[] HVs_volts = new double[8];
    // Test two different HV value for each ASIC. -> two loops
    for(int i=0;i<8;i++){
        HVs_volts[i] = HighestHV/8*(i)+1;
    }
    HV_ADC_success1 = HV_test_FEBside(HVs_volts,OutFile_Name);
    for(int i=0;i<8;i++){
        HVs_volts[i] = HighestHV/8*(7-i)+1;
    }
    HV_ADC_success2 = HV_test_FEBside(HVs_volts,OutFile_Name);
    // SUCCESS if both are successful
    HV_ADC_success = (HV_ADC_success1 && HV_ADC_success2);
    if(!HV_ADC_success){
        System.Console.WriteLine("MPPC HV test (FEB side) FAILED");
    }
    return HV_ADC_success;
}

// ANOTHER VERSION
// bool MPPCHV_test(string OutFile_Name){
//     double HV_set = 35;// V
//     double Delta = 0.5;// V
//     double CF = 65535/102.46;
//     double HV_set_GUI = HV_set*CF;
//     double read=0;
//     bool success = true;

//     System.Console.WriteLine("MPPC HV test");
//     File.AppendAllText(@OutFile_Name,"----Starting test of HV set on MPPC and 'MPPC_ADC'." + Environment.NewLine);

//     BoardLib.SetBoardId(0); Sync.Sleep(1);

//     for(int asic=0;asic<8;asic++){
//         File.AppendAllText(@OutFile_Name,"ONLY ASIC "+asic.ToString()+" ON"+Environment.NewLine);
//         for(int i=0;i<8;i++){
//             if(i==asic){
//                 BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set_GUI);
//             }else{
//                 BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
//             }
//         }
//         BoardLib.DeviceConfigure(11, x_verbose:false);
//         Sync.Sleep(500);
//         BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
//         BoardLib.SetDirectParameters();
//         Sync.Sleep(1000);
//         for(int i=0;i<8;i++){
//             read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC") );
//             if(i==asic){
//                 if(read>HV_set-Delta && read<HV_set+Delta){
//                     File.AppendAllText(@OutFile_Name,"HV on asic"+asic.ToString()+": "+read+" : SUCCESS"+Environment.NewLine);
//                 }else{
//                     File.AppendAllText(@OutFile_Name,"HV on asic"+asic.ToString()+": "+read+" : FAILED"+Environment.NewLine);
//                 }
//             }else{
//                 if(read>0-Delta && read<0+Delta){
//                     //File.AppendAllText(@OutFile_Name,"HV on asic"+asic.ToString()+": "+read+" : SUCCESS"+Environment.NewLine);
//                 }else{
//                     File.AppendAllText(@OutFile_Name,"HV on asic"+asic.ToString()+": "+read+" : FAILED"+Environment.NewLine);
//                 }
//             }
//         }

//     }


// }


bool I12V_test(string OutFile_Name, string HK_values){
    bool success = true;
    System.Console.WriteLine("12V current test");
    File.AppendAllText(@OutFile_Name,"----Starting test of current in 12V." + Environment.NewLine);
    double[] currents={0,0,0,0,0,0,0,0,0};
    double Delta = 0.12;//[A] expected current difference between 0 and 8 enabled CITIROCs
    // 1. CITIROC power test: enable one citiroc at a time and check the current on the 12V-FEB
    // Enable PowerPulsing
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    for(int i=0;i<8;i++){
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.DiscriDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.Discri_tDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.FastShaperDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.FastShaperFollowerDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.DAC4bDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.DAC4b_tDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.TempDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.BandGapDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.DAC10bDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.DAC10b_tDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.ProbeOTAqDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.ValEvtRxDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.RazChnRxDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.HG_T_H_DisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.HG_PdetDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.HG_SlowShaperDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.HG_PA_DisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.HG_OTAqDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.LG_TH_DisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.LG_PdetDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.LG_SlowShaperDisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.LG_PA_DisPP",false);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".PowerModes.LG_OTAqDisPP",false);
    
        BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+i.ToString()+".AllStagesPowerOn",false);
        BoardLib.DeviceConfigure((byte)i,x_verbose:false);
        Sync.Sleep(10);
    }
    // Check the current that each single CITI draws:
    double current12V=0;
    for(int i=0;i<8;i++){
        for(int j=0;j<8;j++){
            if(i==j){
                BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+j.ToString()+".AllStagesPowerOn",true);
            }else{
                BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+j.ToString()+".AllStagesPowerOn",false);
            }
        }
        BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");        
        Sync.Sleep(20);
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
        current12V = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-12V-Current") );
        File.AppendAllText(@OutFile_Name,"Enabled CITIROC #"+i+". Current: "+ current12V.ToString() + " A" + Environment.NewLine);
        File.AppendAllText(@HK_values,"I_citi" + i + "; " + current12V.ToString() + "; A; current flowing in 12V when only CITIROC " + i + " is enabled" + Environment.NewLine);
    }
    // Check current for 0,1,2,...8 CITIs
    for(int i=0;i<9;i++){
        for(int j=0;j<8;j++){
            if(j<i){
                BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+j.ToString()+".AllStagesPowerOn",true);
            }else{
                BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+j.ToString()+".AllStagesPowerOn",false);
            }
        }
        BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
        Sync.Sleep(20);
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
        current12V = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-12V-Current") );
        currents[i]=current12V;
        File.AppendAllText(@OutFile_Name,"Enabled "+i+" CITIROCs. Current: "+ current12V.ToString() + " A" + Environment.NewLine);
        if(i==8){
            File.AppendAllText(@HK_values,"I12V; " + current12V.ToString() + "; A; current flowing in 12V with all CITIROCs enabled" + Environment.NewLine);
        }
    }
    if( Math.Abs(currents[0]-currents[8])<Delta ){
        success = false;
        File.AppendAllText(@OutFile_Name,"I12V test FAILED"+ Environment.NewLine);
    }else{
        File.AppendAllText(@OutFile_Name,"I12V test: success"+ Environment.NewLine);
    }
    
    return success;
}


bool FPGAtemp_test(string OutFile_Name, string HK_values){
    bool success = false;
    System.Console.WriteLine("FPGA temperature test");
    File.AppendAllText(@OutFile_Name, "----FPGA temperature test:"+ Environment.NewLine);
     
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 35;//degrees
    double Delta = 20;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FPGA-Temp") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "FPGA temperature: "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "FPGA temperature: "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        System.Console.WriteLine("FPGA temperature test FAILED");
        success = false;
    }
    File.AppendAllText(@HK_values, "T_FPGA; "+read+"; °C; FPGA temperature" +Environment.NewLine);

    return success;
}


bool FEBtemp0_test(string OutFile_Name, string HK_values){
    bool success = false;
    System.Console.WriteLine("FEB temperature 0 test");
    File.AppendAllText(@OutFile_Name, "----FEB temperature 0 test:"+ Environment.NewLine);
     
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 25;//degrees
    double Delta = 15;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-Temp0") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "FEB temperature (0): "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "FEB temperature (0): "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        System.Console.WriteLine("FEB temperature 0 test FAILED");
        success = false;
    }
    File.AppendAllText(@HK_values, "T_FEB0; "+read+"; °C; FEB temperature, sensor 0" +Environment.NewLine);

    return success;
}

bool FEBtemp1_test(string OutFile_Name, string HK_values){
    bool success = false;
    System.Console.WriteLine("FEB temperature 1 test");
    File.AppendAllText(@OutFile_Name, "----FEB temperature 1 test:"+ Environment.NewLine);
     
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 25;//degrees
    double Delta = 15;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-Temp1") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "FEB temperature (1): "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "FEB temperature (1): "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        System.Console.WriteLine("FEB temperature 1 test FAILED");
        success = false;
    }
    File.AppendAllText(@HK_values, "T_FEB1; "+read+"; °C; FEB temperature, sensor 1" +Environment.NewLine);

    return success;
}

bool PMezza_2V2_test(string OutFile_Name, string HK_values){
    bool success = false;
    System.Console.WriteLine("PMezza 2V2 test");
    File.AppendAllText(@OutFile_Name, "----PMezza 2V2 test:"+ Environment.NewLine);
     
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 35;//degrees
    double Delta = 10;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.PMezza-2V2-Temp") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "PMezza 2V2 temperature: "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "PMezza 2V2  temperature: "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        System.Console.WriteLine("PMezza 2V2  test FAILED");
        success = false;
    }
    File.AppendAllText(@HK_values, "T_2V2; "+read+"; °C; Temperature on Power mezzanine 2.2 V line" +Environment.NewLine);

    return success;
}

bool PMezza_0V9_test(string OutFile_Name, string HK_values){
    bool success = false;
    System.Console.WriteLine("PMezza 0V9 test");
    File.AppendAllText(@OutFile_Name, "----PMezza 0V9 test:"+ Environment.NewLine);
     
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 25;//degrees
    double Delta = 15;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.PMezza-0V9-Temp") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "PMezza 0V9 temperature: "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "PMezza 0V9  temperature: "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        System.Console.WriteLine("PMezza 0V9  test FAILED");
        success = false;
    }
    File.AppendAllText(@HK_values, "T_0V9; "+read+"; °C; Temperature on Power mezzanine 0.9 V line" +Environment.NewLine);

    return success;
}

bool FPGAcurrent_test(string OutFile_Name, string HK_values){
    bool success = false;
    System.Console.WriteLine("FPGA current test");
    File.AppendAllText(@OutFile_Name, "----FPGA current test:"+ Environment.NewLine);
     
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 4.82;//Amps
    double Delta = 1;//Amps
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-FPGA-Current") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "FPGA current: "+read+" A -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "FPGA current: "+read+" A -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        System.Console.WriteLine("FPGA current test FAILED");
        success = false;
    }
    File.AppendAllText(@HK_values, "I_FPGA; "+read+"; A; Current flowing in FPGA" +Environment.NewLine);

    return success;
}


bool bkpHV_test(string OutFile_Name, string HK_values){
    bool success = false;
    System.Console.WriteLine("backplane HV test");
    File.AppendAllText(@OutFile_Name, "----backplane HV test:"+ Environment.NewLine);
     
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = Convert.ToDouble(Environment.GetEnvironmentVariable("PS_HV"));//V
    double Delta = 2;//V
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-BKP-HV") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "backplane HV: "+read+" V -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "backplane HV: "+read+" V -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        System.Console.WriteLine("backplane HV test FAILED");
        success = false;
        System.Console.WriteLine("This test will always fail if the voltage on the power supply is not "+mu.ToString()+ " V");
        System.Console.WriteLine("Check that the Voltage on the Power supply is "+mu.ToString()+". Otherwise, change option \"PS_HV\" in setup.sh.");
    }
    File.AppendAllText(@HK_values, "HV_bkp; "+read+"; V; Backplane HV" +Environment.NewLine);

    return success;
}

bool CITItemp_test(string OutFile_Name, string HK_values){
    bool success = false;
    System.Console.WriteLine("CITIROC temperature test");
    File.AppendAllText(@OutFile_Name, "----CITIROC temperature test:"+ Environment.NewLine);
     
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 25;//degrees
    double Delta = 15;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    bool[] vsuc = {false,false,false,false,false,false,false,false};
    int count=0;
    for(int asic=0;asic<8;asic++){
        read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+asic.ToString()+".Citiroc-Temp") );
        if(read < mu+Delta && read > mu-Delta){
            File.AppendAllText(@OutFile_Name, "CITIROC "+asic.ToString()+" temperature: "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
            vsuc[asic] = true;
        }else{
            File.AppendAllText(@OutFile_Name, "CITIROC "+asic.ToString()+" temperature: "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
            vsuc[asic] = false;
        }
        File.AppendAllText(@HK_values, "T_citi" + asic + "; "+read+"; °C; Temperature of CITIROC " + asic +Environment.NewLine);

    }

    for(int asic=0;asic<8;asic++){
        if(!vsuc[asic]){
            success = false;
            System.Console.WriteLine("CITIROC temperature test FAILED");

        }
        else{
            count++;
        }
    }
    if(count==8) success = true;
    return success;
}

bool HVShort_test(string OutFile_Name, string HK_values, double HV_set=35){ 
    bool success = false;
    System.Console.WriteLine("HV-short test");
    File.AppendAllText(@OutFile_Name, "----HV short test:"+ Environment.NewLine);
    // Setting up test:
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-MISC.HV-Short",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    // Set HV
    double CF_set = 65535/102.46;
    int HV_set_GUI;
    double mu = 30000;//uA
    double Delta = 10000;//uA
    double CF = 0.1716;// Conversion factor (UInt32 to uA) [unused]
    double current_read_uA = 0;
    for(int i = 0;i<8;i++){
        HV_set_GUI = (int) (HV_set * CF_set);
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set_GUI);
        //System.Console.WriteLine(HV_set_GUI.ToString());
    }
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.DeviceConfigure(11, x_verbose:false);
    Sync.Sleep(100);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    // Set ADC to read values
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC-DPRAM");
    // Set current template (OK if [mu-Delta,mu+Delta])

    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    current_read_uA = Convert.ToDouble( BoardLib.GetFormulaVariable("GPIO.GPIO-ADC-DPRAM.Others.HV-Current") );
    if(current_read_uA < mu+Delta && current_read_uA > mu-Delta){
        File.AppendAllText(@OutFile_Name, "HV short test successful. -> Current: "+current_read_uA+" uA "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "HV short test FAILED. -> Current: "+current_read_uA+" uA "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        System.Console.WriteLine("'HV Short' test FAILED");
        success = false;
    }
    File.AppendAllText(@HK_values, "I_HVshort; "+current_read_uA+"; uA; Current in HV circuit when shorted (35 V default load, see HV_bkp)" +Environment.NewLine);

    
    return success;
}

bool HV_test(double[] HVs_volts,string OutFile_Name, string HK_values){
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
    File.AppendAllText(@OutFile_Name,Environment.NewLine + "----Starting HV DAC-ADC test" + Environment.NewLine);
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
            File.AppendAllText(@OutFile_Name, "Hv measurement in ASIC"+ i.ToString() + ": successful! -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+ Environment.NewLine);
        }else{
            File.AppendAllText(@OutFile_Name, "Hv measurement in ASIC"+ i.ToString()+": FAILED. -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+Environment.NewLine);
            success = false;
        }
        File.AppendAllText(@HK_values, "V_citi"+i+"; "+HV_read_volts+"; V; Voltage on CITIROC" + i + " (Set:" + HVs_volts[i].ToString() + " V) " + Environment.NewLine);
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


bool HV_test_FEBside(double[] HVs_volts,string OutFile_Name){
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
    File.AppendAllText(@OutFile_Name,Environment.NewLine + "----Starting MPPC HV test (FEB side)" + Environment.NewLine);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1500);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12, x_verbose:false);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
    for(int i = 0;i<8;i++){
        HV_read_volts = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+i.ToString()+".MPPC-HV") );
        if(HV_read_volts < HVs_volts[i]+Delta && HV_read_volts > HVs_volts[i]-Delta){
            File.AppendAllText(@OutFile_Name, "Hv measurement in ASIC"+ i.ToString() + ": successful! -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+ Environment.NewLine);
        }else{
            File.AppendAllText(@OutFile_Name, "Hv measurement in ASIC"+ i.ToString()+": FAILED. -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+Environment.NewLine);
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

bool read_IsInRange(byte address_set,UInt32 ADC_read,string OutFile_Name){
    bool accept = false;
        // Compute expected voltage for a given FEB address:
    double A0 = 3.3;
    double A1 = 6;
        // Compute the bit SUM:
        int SUM = 0;
        int BITS = 5;
        for(int i=0;i<BITS;i++){
            if( (address_set & ( 1 << i) )!= 0 ){
                SUM++;
            }
        }
                                                                        //System.Console.WriteLine("----------------------------address="+address_set.ToString());      
                                                                        //System.Console.WriteLine("----------------------------SUM="+SUM.ToString());
    File.AppendAllText(@OutFile_Name, "address="+address_set.ToString() + Environment.NewLine);
    File.AppendAllText(@OutFile_Name, "SUM="+SUM.ToString() + Environment.NewLine);

    double Expected = A0/(A1-SUM);
    double Delta=0.04;//V
    double UpperLimit, LowerLimit;
    UpperLimit = Expected + Delta;
    LowerLimit = Expected - Delta;
                                                                        //System.Console.WriteLine("----------------------------expected="+LowerLimit.ToString()+" - "+UpperLimit.ToString() +" V");
                                                                        //System.Console.WriteLine("----------------------------adc_read="+ADC_read.ToString());
    File.AppendAllText(@OutFile_Name,"expected="+LowerLimit.ToString()+" - "+UpperLimit.ToString() +" V" + Environment.NewLine);
    File.AppendAllText(@OutFile_Name, "adc_read="+ADC_read.ToString() + Environment.NewLine);


    // Function to convert from a.u. (HEX) to Volts:
    double read_Volts = 3*ADC_read/(Math.Pow(2,18));
                                                                        //System.Console.WriteLine("----------------------------after conversion="+read_Volts.ToString() +" V");
    File.AppendAllText(@OutFile_Name, "After conversion="+read_Volts.ToString() +" V" + Environment.NewLine);

    if(read_Volts<UpperLimit && read_Volts>LowerLimit){
        accept = true;
    }
                                                                        //System.Console.WriteLine("----------------------------accepted? "+accept.ToString());
    File.AppendAllText(@OutFile_Name,"accepted? "+accept.ToString() + Environment.NewLine);

    return accept;
}

bool read_IsInRange(byte address_set,double ADC_read,string OutFile_Name){
    bool accept = false;
        // Compute expected voltage for a given FEB address:
    double A0 = 3.3;
    double A1 = 6;
        // Compute the bit SUM:
        int SUM = 0;
        int BITS = 5;
        for(int i=0;i<BITS;i++){
            if( (address_set & ( 1 << i) )!= 0 ){
                SUM++;
            }
        }
                                                                        //System.Console.WriteLine("----------------------------address="+address_set.ToString());      
                                                                        //System.Console.WriteLine("----------------------------SUM="+SUM.ToString());
    File.AppendAllText(@OutFile_Name, "address="+address_set.ToString() + Environment.NewLine);
    File.AppendAllText(@OutFile_Name, "SUM="+SUM.ToString() + Environment.NewLine);

    double Expected = A0/(A1-SUM);
    double Delta=0.04;//V
    double UpperLimit, LowerLimit;
    UpperLimit = Expected + Delta;
    LowerLimit = Expected - Delta;
                                                                        //System.Console.WriteLine("----------------------------expected="+LowerLimit.ToString()+" - "+UpperLimit.ToString() +" V");
                                                                        //System.Console.WriteLine("----------------------------adc_read="+ADC_read.ToString());
    File.AppendAllText(@OutFile_Name,"expected="+LowerLimit.ToString()+" - "+UpperLimit.ToString() +" V" + Environment.NewLine);


    // Do ot convert in this override of the function
    double read_Volts = ADC_read;
                                                                        //System.Console.WriteLine("----------------------------after conversion="+read_Volts.ToString() +" V");
    File.AppendAllText(@OutFile_Name, "After conversion="+read_Volts.ToString() +" V" + Environment.NewLine);

    if(read_Volts<UpperLimit && read_Volts>LowerLimit){
        accept = true;
    }
                                                                        //System.Console.WriteLine("----------------------------accepted? "+accept.ToString());
    File.AppendAllText(@OutFile_Name,"accepted? "+accept.ToString() + Environment.NewLine);

    return accept;
}

void TurnOnFEB(int BID=0){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR", BID);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}
void TurnOffFEB(int BID=0){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR", BID);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(3000);
}

void SelectGPIOdevices(){
    // Speak with GPIO
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
}

void SelectFEBdevices(byte FEBID=0){
    // Speak with FEB
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
    // Retrieve output txt file generated at part 1
    var DATADIR = output_path+"SN_"+SN.ToString()+"/IO_TEST/";


    var directory = new System.IO.DirectoryInfo(DATADIR);
    var FilesList = directory.GetFiles();
    
    var myFile = LatestFileWritten(FilesList);
    
    string OutFile_Name = myFile.FullName;

    bool LB_success=false;
    bool HK_success=false;

    DateTime now = DateTime.Now;
    string HK_values = output_path + "SN_" +SN.ToString() + "/IO_TEST/HouseKeeping_"+
                                                        now.Year.ToString() + "_"+
                                                        now.Month.ToString() + "_"+
                                                        now.Day.ToString() + "-"+
                                                        now.Hour.ToString() + "_"+
                                                        now.Minute.ToString() + "_"+
                                                        now.Second.ToString() +".csv";
    File.AppendAllText(@HK_values, "SN "+SN.ToString() + Environment.NewLine);
    File.AppendAllText(@HK_values, "measurement; value; unit; comment;  " + Environment.NewLine);
    /////////////////////////////////////////////////////////////////////////////////////
 
    TurnOnFEB();

    LB_success = Run_LoopBack_test(OutFile_Name,config_path);
    HK_success = HouseKeeping_test(OutFile_Name,FEB_BoardID,config_path, HK_values);

    int fails = CountFailedTests(OutFile_Name);

    System.Console.WriteLine("end of HK/LB test");   

    //TurnOffFEB();

    Sync.Sleep(5);
    


    if (fails == 0){
        File.AppendAllText(@OutFile_Name, Environment.NewLine + 
                                          "/////////////////////" + Environment.NewLine);
        File.AppendAllText(@OutFile_Name, "// TEST SUCCESSFUL //"  + Environment.NewLine);
        File.AppendAllText(@OutFile_Name, "/////////////////////"  + Environment.NewLine);
    }else{
        File.AppendAllText(@OutFile_Name, Environment.NewLine + 
                                          "|||||||||||||||||||||" + Environment.NewLine);
        File.AppendAllText(@OutFile_Name, "||   TEST FAILED   ||"  + Environment.NewLine);
        File.AppendAllText(@OutFile_Name, "|||||||||||||||||||||"  + Environment.NewLine);
    }

    return;
}

System.IO.FileInfo LatestFileWritten(System.IO.FileInfo[] FilesList){
    DateTime dt0 = FilesList[0].CreationTime;
    System.IO.FileInfo lf = FilesList[0];
    foreach (var File in FilesList) {
        //System.Console.WriteLine(File);
        DateTime dt = File.CreationTime;
        if (dt>dt0){
            dt0 = dt;
            lf = File;
        }
    }
    return lf;
}

int CountFailedTests(string pathToFile){
    int count = 0;
    foreach (var line in File.ReadAllLines(pathToFile)){
        if (line.Contains("FAIL") || line.Contains("fail"))
            count++;
    }
    return count;
}

