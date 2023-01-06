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
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
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
    System.Console.WriteLine("Starting Loopback test.");
    File.AppendAllText(@OutFile_Name, "Starting Loopback test." + Environment.NewLine);
    byte address = 0;
    // STEP 1: Enable LOOPBACK MODE (already done loading the config file)
        // BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
        // SelectFEBdevices();
        // BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    bool MIBdebug_success=false, FEBbusy_success=false, FEBtrig_success=false, LB_FEBtrig_OD_success=false, SUM32_success=false;

    // STEP 2: MIB Debug test
    MIBdebug_success = MIB_Debug_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
                                                                        System.Console.WriteLine("----------------------------step 2 completed");       
    // STEP 3: LB test on FEB Busy 
    FEBbusy_success = FEB_busy_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
                                                                        System.Console.WriteLine("----------------------------step 3 completed");       
    // STEP 4: LB test on FEB TRIG
    FEBtrig_success = FEB_trig_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
                                                                        System.Console.WriteLine("----------------------------step 4 completed");       
    // STEP 4.1: hardware action
    Dialog.ShowDialog("Set Jumper on J13 to 2-3");
    // STEP 5: LB test on FEB TRIG OD
    LB_FEBtrig_OD_success = FEB_trigOD_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
                                                                        System.Console.WriteLine("----------------------------step 5 completed");       
    
    // STEP 5.1: hardware action
    Dialog.ShowDialog("Set Jumper on J13 to 1-2");
    // STEP 6: SUM_or32 test: FEB ADDRESS 
    SUM32_success = SUM_or32_test(FEB_BoardID,OutFile_Name);
    // Restore initial config at the end of the test
    Restore_Initial_Config(FEB_BoardID,config_path);
                                                                        System.Console.WriteLine("----------------------------step 6 completed");       
    
    
    return (MIBdebug_success && FEBbusy_success && FEBtrig_success && LB_FEBtrig_OD_success && SUM32_success);
    

}

bool SUM_or32_test(byte FEB_BoardID, string OutFile_Name){
    byte address;
    byte Initial_BID = FEB_BoardID;
    File.AppendAllText(@OutFile_Name, "starting 'SUM_or32 from FEB-ADDRESS' test."+ Environment.NewLine);
    // Test preparation
    bool SUM32_success = true;
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.SumOr32En",true);
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",false);

    //UInt32 ADC_read; //Not used if the formulae are already implemented in the GUI
    double sum32_V=0;
    for(int i=0;i<5;i++){
        address = (byte)(Math.Pow(2,i)-1); 
        BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",address);
        BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
        FEB_BoardID = address;
        BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
        BoardLib.UpdateUserParameters("GPIO.GPIO-ADC-DPRAM");
        sum32_V = Convert.ToDouble( BoardLib.GetFormulaVariable("GPIO.GPIO-ADC-DPRAM.Others.Sum32") );
        if( !read_IsInRange(address,sum32_V,OutFile_Name) ){
            File.AppendAllText(@OutFile_Name, "SUM_or32 ADC test failed at FEB-ADDRESS = "+ address.ToString() + Environment.NewLine + "----------------"  + Environment.NewLine);
            Dialog.ShowDialog("SUM_or32 FAILED");
            SUM32_success = false;
        }else{
            File.AppendAllText(@OutFile_Name, "SUM_or32 ADC test. FEB-ADDRESS = "+ address.ToString() + ": OK!" + Environment.NewLine + "----------------"  + Environment.NewLine);
        }
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
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
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
        Dialog.ShowDialog("Loopback on FEB-trig-OD failed");
        LB_FEBtrig_OD_success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    LB_FEBtrig_OD = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT-OD");
    if(LB_FEBtrig_OD){
        File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test FAILED" + Environment.NewLine);
        Dialog.ShowDialog("Loopback on FEB-trig-OD failed");
        LB_FEBtrig_OD_success = false;
    }
    if(LB_FEBtrig_OD_success){ File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack: TEST SUCCESSFUL" + Environment.NewLine); }
    
    return LB_FEBtrig_OD_success;
}

bool FEB_trig_test(byte FEB_BoardID, string OutFile_Name){
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.TrigLoopbackEn",true);
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    Sync.Sleep(500);
    bool FEBtrig_success = true;
    bool LB_FEBtrig = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT");
    if(!LB_FEBtrig){
        File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack test FAILED" + Environment.NewLine);
        Dialog.ShowDialog("Loopback on FEB-trig failed");
        FEBtrig_success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    Sync.Sleep(500);
    LB_FEBtrig = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT");
    if(LB_FEBtrig){
        File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack test FAILED" + Environment.NewLine);
        Dialog.ShowDialog("Loopback on FEB-trig failed");
        FEBtrig_success = false;
    }
    if(FEBtrig_success){ File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack: TEST SUCCESSFUL" + Environment.NewLine); }
    
    return FEBtrig_success;
}

bool FEB_busy_test(byte FEB_BoardID, string OutFile_Name){
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.BusyLoopbackEn",true);
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ForceBusyEn", true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    bool FEBbusy_success = true;
    bool LB_FEBbusy = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-BUSY-OD");
    if(!LB_FEBbusy){
        File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack test FAILED (1)" + Environment.NewLine);
        Dialog.ShowDialog("Loopback on FEB-busy failed (1)");
        FEBbusy_success = false;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ForceBusyEn", false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    LB_FEBbusy = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-BUSY-OD");
    if(LB_FEBbusy){
        File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack test FAILED (0)" + Environment.NewLine);
        Dialog.ShowDialog("Loopback on FEB-busy failed (0)");
        FEBbusy_success = false;
    }
    if(FEBbusy_success){ File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack: TEST SUCCESSFUL" + Environment.NewLine); }
    
    return FEBbusy_success;
}

void Restore_Initial_Config(byte FEB_BoardID,string config_path){
    BoardLib.OpenConfigFile(config_path);
    SendGPIO();
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); // This resets teh FEB Board ID to the value set in the config file (0)
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
}

bool MIB_Debug_test(byte FEB_BoardID,string OutFile_Name){
    
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.MIBdbgFromAddrEn",true);
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.MIBdbgAddr75Sel",false);
    BoardLib.SetBoardId(0);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);

    BoardLib.SetBoardId(126);
    byte address=0;
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",address);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    FEB_BoardID = address;
    BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    Sync.Sleep(500);
    bool MIBdebug_success = true;
    byte LB_address = BoardLib.GetByteVariable("GPIO.GPIO-STATUS.MIBDebug");
    if(address != LB_address){
        File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug failed: " + (address).ToString()+" != "+(LB_address).ToString() + Environment.NewLine);
        Dialog.ShowDialog("Loopback on MIB Debug failed: " + address.ToString()+" != "+LB_address.ToString());
    }
    for(int i=0;i<5;i++){
        address = (byte)Math.Pow(2,i);
        BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",address);
        BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
        FEB_BoardID = address;
        BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
        LB_address = BoardLib.GetByteVariable("GPIO.GPIO-STATUS.MIBDebug");
        if(address != LB_address){
            File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug failed: " + (address).ToString()+" != "+(LB_address).ToString() + Environment.NewLine);
            MIBdebug_success = false;
            Dialog.ShowDialog("Loopback on MIB Debug failed: " + address.ToString()+" != "+LB_address.ToString());
        }
    }
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.MIBdbgAddr75Sel",true);
    BoardLib.SetBoardId(0);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    Sync.Sleep(500);
    BoardLib.SetBoardId(126);
    for(int i=5;i<8;i++){
        address = (byte)Math.Pow(2,i);
        BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR",address);
        BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
        FEB_BoardID = address;
        BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
        LB_address = BoardLib.GetByteVariable("GPIO.GPIO-STATUS.MIBDebug");
        //LB_address = (byte) (LB_address << 3);
        if(address>>3 != LB_address){
            File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug failed: " + (address>>3).ToString()+" != "+(LB_address).ToString() + Environment.NewLine);
            MIBdebug_success = false;
            Dialog.ShowDialog("Loopback on MIB Debug failed: " + (address>>3).ToString()+" != "+(LB_address).ToString());
        }
    }
    if(MIBdebug_success){ File.AppendAllText(@OutFile_Name, "Loopback on MIB Debug: TEST SUCCESSFUL " + Environment.NewLine); }



    return MIBdebug_success;
}

bool HouseKeeping_test(string OutFile_Name,byte FEB_BoardID,string config_path){
    // Starting Housekeeping test
    System.Console.WriteLine("Starting Housekeeping test.");
    File.AppendAllText(@OutFile_Name, "-------------------------------------------------------" 
                                        + Environment.NewLine + "Starting Housekeeping test." 
                                        + Environment.NewLine);
    // Initialize: HV to 0 V;
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
    }
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.DeviceConfigure(11);
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
    double HighestHV = 40;//V
    double[] HVs_volts = new double[8];
    // Test two different HV value for each ASIC. -> two loops
    for(int i=0;i<8;i++){
        HVs_volts[i] = HighestHV/8*(i);
    }
    HV_ADC_success1 = HV_test(HVs_volts,OutFile_Name);
    for(int i=0;i<8;i++){
        HVs_volts[i] = HighestHV/8*(7-i);
    }
    HV_ADC_success2 = HV_test(HVs_volts,OutFile_Name);
    // SUCCESS if both are successful
    HV_ADC_success = (HV_ADC_success1 && HV_ADC_success2);
    if(!HV_ADC_success){
        Dialog.ShowDialog("HV setting/reading test FAILED");
    }

    // 2: HV short test 
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    bool HVShort_success = HVShort_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);

    // 3: test Temperature on the FPGA
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    bool FPGAtemp_success = FPGAtemp_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);

    // 4: test FEB temperature (0)
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    bool FEBtemp0_success = FEBtemp0_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);

    // 5: test FEB temperature (1)
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    bool FEBtemp1_success = FEBtemp1_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);


    // 6: test current in 12V from backplane
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    bool I12V_success = I12V_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);
    // So far this just prints the currents, need to implement some success/fail criterion

    // 7: test CITIROC temperatures (x8)
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    bool CITItemp_success = CITItemp_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);

    // 8: test backplane HV
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    bool bkpHV_success = bkpHV_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);

    // 9: FPGA current test
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    bool FPGAcurrent_success = FPGAcurrent_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);

    // 10: MPPC HV test (from FEB-HK side, to test the FEB MPPC-ADC )

    bool MPPC_HV_success = MPPCHV_test(OutFile_Name);
    Restore_Initial_Config(FEB_BoardID,config_path);

    return ( HV_ADC_success && HVShort_success && FPGAtemp_success && 
             I12V_success && FEBtemp0_success && FEBtemp1_success && 
             CITItemp_success && bkpHV_success && FPGAcurrent_success &&
             MPPC_HV_success
           ); // && of all HK tests
}

bool MPPCHV_test(string OutFile_Name){
    // Set high voltages
    bool HV_ADC_success,HV_ADC_success1,HV_ADC_success2;
    double HighestHV = 40;//V
    double[] HVs_volts = new double[8];
    // Test two different HV value for each ASIC. -> two loops
    for(int i=0;i<8;i++){
        HVs_volts[i] = HighestHV/8*(i);
    }
    HV_ADC_success1 = HV_test_FEBside(HVs_volts,OutFile_Name);
    for(int i=0;i<8;i++){
        HVs_volts[i] = HighestHV/8*(7-i);
    }
    HV_ADC_success2 = HV_test_FEBside(HVs_volts,OutFile_Name);
    // SUCCESS if both are successful
    HV_ADC_success = (HV_ADC_success1 && HV_ADC_success2);
    if(!HV_ADC_success){
        Dialog.ShowDialog("MPPC HV test (FEB side) FAILED");
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

//     BoardLib.SetBoardID(0);

//     for(int asic=0;asic<8;asic++){
//         File.AppendAllText(@OutFile_Name,"ONLY ASIC "+asic.ToString()+" ON"+Environment.NewLine);
//         for(int i=0;i<8;i++){
//             if(i==asic){
//                 BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set_GUI);
//             }else{
//                 BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
//             }
//         }
//         BoardLib.DeviceConfigure(11);
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


bool I12V_test(string OutFile_Name){
    bool success = true;
    System.Console.WriteLine("12V current test");
    File.AppendAllText(@OutFile_Name,"----Starting test of current in 12V." + Environment.NewLine);
    // 1. CITIROC power test: enable one citiroc at a time and check the current on the 12V-FEB
    // Enable PowerPulsing
    BoardLib.SetBoardId(0);
    for(int i=0;i<8;i++){
        BoardLib.SetVariable("ASICS.ASIC0.PowerModes.DiscriDisPP",false);
        BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+i.ToString()+".AllStagesPowerOn",false);
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
        BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");        Sync.Sleep(100);
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
        current12V = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-HK.FEB-12V-Current") );
        File.AppendAllText(@OutFile_Name,"Enabled CITIROC #"+i+". Current: "+ current12V.ToString() + " A" + Environment.NewLine);
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
        Sync.Sleep(100);
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
        current12V = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-HK.FEB-12V-Current") );
        File.AppendAllText(@OutFile_Name,"Enabled "+i+" CITIROCs. Current: "+ current12V.ToString() + " A" + Environment.NewLine);
    }
    return success;
}


bool FPGAtemp_test(string OutFile_Name){
    bool success = false;
    System.Console.WriteLine("FPGA temperature test");
    File.AppendAllText(@OutFile_Name, "----FPGA temperature test:"+ Environment.NewLine);
    Sync.Sleep(100);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 20;//degrees
    double Delta = 5;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-HK.FPGA-Temp") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "FPGA temperature: "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "FPGA temperature: "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        Dialog.ShowDialog("FPGA temperature test FAILED");
        success = false;
    }
    return success;
}


bool FEBtemp0_test(string OutFile_Name){
    bool success = false;
    System.Console.WriteLine("FEB temperature 0 test");
    File.AppendAllText(@OutFile_Name, "----FEB temperature 0 test:"+ Environment.NewLine);
    Sync.Sleep(100);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 36;//degrees
    double Delta = 3;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-HK.FEB-Temp0") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "FEB temperature (0): "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "FEB temperature (0): "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        Dialog.ShowDialog("FEB temperature 0 test FAILED");
        success = false;
    }
    return success;
}

bool FEBtemp1_test(string OutFile_Name){
    bool success = false;
    System.Console.WriteLine("FEB temperature 1 test");
    File.AppendAllText(@OutFile_Name, "----FEB temperature 1 test:"+ Environment.NewLine);
    Sync.Sleep(100);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 33;//degrees
    double Delta = 3;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-HK.FEB-Temp1") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "FEB temperature (1): "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "FEB temperature (1): "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        Dialog.ShowDialog("FEB temperature 1 test FAILED");
        success = false;
    }
    return success;
}

bool FPGAcurrent_test(string OutFile_Name){
    bool success = false;
    System.Console.WriteLine("FPGA current test");
    File.AppendAllText(@OutFile_Name, "----FPGA current test:"+ Environment.NewLine);
    Sync.Sleep(100);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 4.6;//Amps
    double Delta = 0.2;//Amps
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-HK.FEB-FPGA-Current") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "FPGA current: "+read+" A -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "FPGA current: "+read+" A -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        Dialog.ShowDialog("FPGA current test FAILED");
        success = false;
    }
    return success;
}


bool bkpHV_test(string OutFile_Name){
    bool success = false;
    System.Console.WriteLine("backplane HV test");
    File.AppendAllText(@OutFile_Name, "----backplane HV test:"+ Environment.NewLine);
    Sync.Sleep(100);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 41.5;//V
    double Delta = 2;//V
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-HK.FEB-BKP-HV") );
    if(read < mu+Delta && read > mu-Delta){
        File.AppendAllText(@OutFile_Name, "backplane HV: "+read+" V -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "backplane HV: "+read+" V -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        Dialog.ShowDialog("backplane HV test FAILED");
        success = false;
    }
    return success;
}

bool CITItemp_test(string OutFile_Name){
    bool success = false;
    System.Console.WriteLine("CITIROC temperature test");
    File.AppendAllText(@OutFile_Name, "----CITIROC temperature test:"+ Environment.NewLine);
    Sync.Sleep(100);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
    // Set current template (OK if [mu-Delta,mu+Delta])
    double mu = 24.5;//degrees
    double Delta = 5;//degrees
    //double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double read = 0;
    // UInt32 current_read_int = 0;
    // Read current
    // current_read_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.Channels10.Value");
    // current_read_uA = current_read_int*CF;
    bool[] vsuc = {false,false,false,false,false,false,false,false};
    int count=0;
    for(int asic=0;asic<8;asic++){
        read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.Group.Group"+asic.ToString()+".Citiroc-Temp") );
        if(read < mu+Delta && read > mu-Delta){
            File.AppendAllText(@OutFile_Name, "CITIROC "+asic.ToString()+" temperature: "+read+" °C -> SUCCESS. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
            vsuc[asic] = true;
        }else{
            File.AppendAllText(@OutFile_Name, "CITIROC "+asic.ToString()+" temperature: "+read+" °C -> FAILED. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
            vsuc[asic] = false;
        }
    }
    for(int asic=0;asic<8;asic++){
        if(!vsuc[asic]){
            success = false;
            Dialog.ShowDialog("CITIROC temperature test FAILED");

        }
        else{
            count++;
        }
    }
    if(count==8) success = true;
    return success;
}

bool HVShort_test(string OutFile_Name){
    bool success = false;
    System.Console.WriteLine("HV-short test");
    File.AppendAllText(@OutFile_Name, "----HV short test:"+ Environment.NewLine);
    // Setting up test:
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-MISC.HV-Short",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    // Set HV
    double CF_set = 65535/102.46;
    int HV_set_GUI;
    double mu = 35000;//uA
    double Delta = 5000;//uA
    double CF = 0.1716;// Conversion factor (UInt32 to uA)
    double current_read_uA = 0;
    double HV_set = 0; // to not make it trip before I have PS with 50 mA limit
    for(int i = 0;i<8;i++){
        HV_set_GUI = (int) (HV_set * CF_set);
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set_GUI);
        //System.Console.WriteLine(HV_set_GUI.ToString());
    }
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.DeviceConfigure(11);
    Sync.Sleep(100);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    // Set ADC to read values
    BoardLib.SetBoardId(126);
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
        File.AppendAllText(@OutFile_Name, "HV short test successful. "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        success = true;
    }else{
        File.AppendAllText(@OutFile_Name, "HV short test failed. -> Current: "+current_read_uA+" uA "+"Acc. range: "+(mu-Delta).ToString()+","+(mu+Delta).ToString() +Environment.NewLine);
        Dialog.ShowDialog("'HV Short' test FAILED");
        success = false;
    }
    return success;
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
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.DeviceConfigure(11);
    Sync.Sleep(500);
    File.AppendAllText(@OutFile_Name,Environment.NewLine + "----Starting HV DAC-ADC test" + Environment.NewLine);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    BoardLib.SetBoardId(126);
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
            File.AppendAllText(@OutFile_Name, "Hv measurement in ASIC"+ i.ToString()+": failed. -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+Environment.NewLine);
            success = false;
        }
    }

    //At the end, reset everything to 0
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
    }
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.DeviceConfigure(11);
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
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.DeviceConfigure(11);
    Sync.Sleep(500);
    File.AppendAllText(@OutFile_Name,Environment.NewLine + "----Starting MPPC HV test (FEB side)" + Environment.NewLine);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);  
    BoardLib.SetDirectParameters();
    Sync.Sleep(1500);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
    for(int i = 0;i<8;i++){
        HV_read_volts = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.Group.Group"+i.ToString()+".MPPC-HV") );
        if(HV_read_volts < HVs_volts[i]+Delta && HV_read_volts > HVs_volts[i]-Delta){
            File.AppendAllText(@OutFile_Name, "Hv measurement in ASIC"+ i.ToString() + ": successful! -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+ Environment.NewLine);
        }else{
            File.AppendAllText(@OutFile_Name, "Hv measurement in ASIC"+ i.ToString()+": failed. -> Set: "+HVs_volts[i].ToString()+"+-" +Delta.ToString()+" Measured: "+HV_read_volts.ToString()+Environment.NewLine);
            success = false;
        }
    }

    //At the end, reset everything to 0
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
    }
    BoardLib.SetBoardId((byte)((int)FEB_BoardID%128));
    BoardLib.DeviceConfigure(11);
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
    double A0 = 19.412;
    double A1 = 15.765;
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
    double A0 = 19.412;
    double A1 = 15.765;
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

void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
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
    BoardLib.SetBoardId(FEBID);
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
}

void SendGPIO(){
    SelectGPIOdevices();
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
}

void SendFEB(byte FEBID=0){
    SelectFEBdevices(FEBID);
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
}

// OLD LOOPBACK
    // // STEP 1: set DFT bit
    // // STEP 2: LB test on FEB busy
    // BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ForceBusyEn", true);
    // BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    // Sync.Sleep(500);
    // bool LB_FEBbusy = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-BUSY-OD");
    // if(!LB_FEBbusy){
    //     File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack test FAILED" + Environment.NewLine);
    //     Dialog.ShowDialog("Loopback on FEB-busy failed");
    //     return;
    // }else{
    //     File.AppendAllText(@OutFile_Name, "FEB-busy LoopBack test SUCCESFUL" + Environment.NewLine);
    // }
    // BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ForceBusyEn", false);
    // // STEP 3: LB test on FEB TRIG
    // BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", true);
    // BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    // Sync.Sleep(500);
    // bool LB_FEBtrig = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT");
    // if(!LB_FEBtrig){
    //     File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack test FAILED" + Environment.NewLine);
    //     Dialog.ShowDialog("Loopback on FEB-trig failed");
    //     return;
    // }else{
    //     File.AppendAllText(@OutFile_Name, "FEB-trig LoopBack test SUCCESFUL" + Environment.NewLine);
    // }
    // BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    // // STEP 3.1: hardware action
    // Dialog.ShowDialog("Set Jumper on J13 to 1-2");
    // // STEP 4: LB test on TRIG OD
    // BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", true);
    // BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");
    // Sync.Sleep(500);
    // bool LB_FEBtrig_OD = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.FEB-TRIGOUT-OD");
    // if(!LB_FEBtrig_OD){
    //     File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test FAILED" + Environment.NewLine);
    //     Dialog.ShowDialog("Loopback on FEB-trig-OD failed");
    //     return;
    // }else{
    //     File.AppendAllText(@OutFile_Name, "FEB-trig-OD LoopBack test SUCCESFUL" + Environment.NewLine);
    // }
    // BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-TRIGIN", false);
    // // STEP 4.1: hardware action
    // Dialog.ShowDialog("Set Jumper on J13 to 2-3");
    // // STEP 5: Set SUM_or32

//using system.IO;

void ScriptMain(){
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //                                    SETTINGS
    // Set the configuration file:
    //string config_path = "C:/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/GPIO_FEB/config/loopbacks.xml";
    string config_path = "/home/neutrino/FCT/code/config/loopbacks_newGUI.xml";


    // Set the output folder, 
    string output_path = "/DATA/Data_FEB/FCT_output/"; 

    // Serial number of FEB under test. To be inserted from user at the beginning of the script
    int SN = -999;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    TurnOnFEB();


    // send Board tab settings
    BoardLib.OpenConfigFile(config_path);
    SendGPIO();
    FEB_BoardID = 0;//Dialog.ShowInputDialog<byte>("Insert Board ID (default=0)"); 
    SendFEB(FEB_BoardID);
    Sync.Sleep(100);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);

    // Ask the user fot the FEB Serial Number
    SN = Dialog.ShowInputDialog<int>("Insert Serial number of FEB under test.");       

    // Generate output txt file
    string OutFile_Name = CreateOutputFile(SN,"IO",output_path);

    System.Console.WriteLine("----------------------------step 1 (initialization) completed");       
    bool LB_success=false;
    bool HK_success=false;

    //LB_success = Run_LoopBack_test(OutFile_Name,config_path);
    HK_success = HouseKeeping_test(OutFile_Name,FEB_BoardID,config_path);
    System.Console.WriteLine("end of test");       
    if (LB_success && HK_success){
        File.AppendAllText(@OutFile_Name, Environment.NewLine + 
                                          "//////////////////////////////////////////////////////////////////////"  + Environment.NewLine);
        File.AppendAllText(@OutFile_Name, "//                             GREEN LIGHT                          //"  + Environment.NewLine);
        File.AppendAllText(@OutFile_Name, "//////////////////////////////////////////////////////////////////////"  + Environment.NewLine);
    }else{
        File.AppendAllText(@OutFile_Name, Environment.NewLine + 
                                          "**********************************************************************"  + Environment.NewLine);
        File.AppendAllText(@OutFile_Name, "**                             TEST FAILED                          **"  + Environment.NewLine);
        File.AppendAllText(@OutFile_Name, "**********************************************************************"  + Environment.NewLine);
    }

    TurnOffFEB();

    return;
}