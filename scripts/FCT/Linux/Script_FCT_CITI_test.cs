//void ScriptMain(){
 void ScriptMainArgs(int SN){
//    int SN = 1; // to be set as argument when the script is launched from bash

    int LG =56;
    int HG =12;
    string data_path = Environment.GetEnvironmentVariable("GENERALDATADIR");  

    // CREATE THE DATA DIRECTORY BASED ON THE SERIAL NUMBER
    data_path = data_path + "SN_" + SN.ToString() + "/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);

    // System.Console.WriteLine("Preparing CITIROC trigger test ...");
    // BoardLib.Reconnect();
    // Sync.Sleep(5000);
    TurnOnFEB();
    System.Console.WriteLine("FEB is on");


    string config_folder = "/home/neutrino/FCT/code/config/";
    string default_config = config_folder + "config_FCT2_newGUI_V2.xml";
    string config="";
    // The default config is the same as the one used for the 256ch + baseline test
    // where the ADC starts on OR32 and enOR32=ON
    BoardLib.OpenConfigFile(default_config);
    SendGPIO();
    // Set the required Direct Parameters
    SetDefaultDirectParameters();
    
    // Send to board
    BoardLib.SetBoardId(0);
    //Sync.Sleep(10);
    
    BoardLib.SetDirectParameters(); //Sync.Sleep(3);

    bool Sync_good = false;
    Sync_good = SyncTest();
    if(!Sync_good){
        System.Console.WriteLine("Sync not working");
        return;
    }else{
         System.Console.WriteLine("Sync test Successful!");
    }
    //Restore initial config
    BoardLib.OpenConfigFile(default_config);
    SendGPIO();
    //Sync.Sleep(200);

    ActivateAllCh(LG,HG);
    // YOU MIGHT WANT TO CHANGE IT TO HAVE THE ADC STARTING AT GATE_CLOSE SIGNAL
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


    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 1. test VALID_event, Force Reset PSC, Force Reset PA (32 gates),
    // ADC starts on OR32 and enOR32=ON (default): 
    // signal expected ONLY in the first 8 gates (default config with OR32=ON)
    int OutputRun = -999;
    OutputRun = RunCITITriggerAcq_32gates("OR32ON_ValEv_ResetPSC_ResetPA",default_config, SN, data_path);
    
    if(OutputRun==-999){
        BoardLib.Reconnect();
        Sync.Sleep(3000);
        SetDefaultDirectParameters();
        BoardLib.SetBoardId(0);
        BoardLib.SetDirectParameters();
        //Sync.Sleep(3);
        OutputRun = RunCITITriggerAcq_32gates("OR32ON_ValEv_ResetPSC_ResetPA",default_config, SN, data_path);
    }




    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 2. ADC starts on OR32, enOR32=OFF -> no expected signal
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnOR32",false);
    }
    // SendFEB();
    config = "OR32OFF.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("OR32OFF",config_folder+config, SN, data_path);



    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    //SendFEB();
    //BoardLib.SetDirectParameters();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 3. ADC starts on NOR32, enNOR32=ON -> signal expected in each gate
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.ADC.AdcStartsignal","NOR32x8");
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnNOR32",true);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnOR32",false);
    }
    // SendFEB();
    config = "NOR32ON.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("NOR32ON",config_folder+config, SN, data_path);



    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    //SendFEB();
    //BoardLib.SetDirectParameters();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 4. ADC starts on NOR32_t, enNOR32_t=ON -> signal expected in each gate
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.ADC.AdcStartsignal","NOR32Tx8");
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnNOR32_t",true);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnOR32",false);
    }
    //SendFEB();
    config = "NOR32TON.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("NOR32TON",config_folder+config, SN, data_path);


    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    //SendFEB();
    //BoardLib.SetDirectParameters();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 5. PSCExtTrig: loopback Or32 to ExtTrigPSC, 
    // gates 8-11: disable ExtTrigPSC on the first 4 CITI -> NO expected signal
    // gates 12-15: disable ExtTrigPSC on the last 4 CITI -> NO expected signal
    // gates 0-7: enable ExtTrigPSC on all CITI -> signal expected in all gates
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Debug.OR32toTrigExtPSC",true);
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.SelTrigExtPSC",true);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.HG_SH_TimeConstant",3);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.LG_SH_TimeConstant",3);
    }
    //SendFEB();
    config = "PSCExtTrig.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_PSCExtTrig("PSCExtTrig",config_folder+config, SN, data_path);

  

    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    //SendFEB();
    //BoardLib.SetDirectParameters();
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    /*  Hold time and Shaping time setting:
    Hold Time = N_set * 2.5 ns | Range: 0 to 8191
    Sh.  Time = N_set * 12.5 ns| Range: 0 to 7
    */
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 6. Hold time SCA test (right Hold Time) -> signal expected in each gate
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.Hold.HoldHG",15);
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.Hold.HoldLG",15);
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.HG_SCAorPeakD",true);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.LG_SCAorPeakD",true);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.HG_SH_TimeConstant",3);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.LG_SH_TimeConstant",3);
    }
    //SendFEB();
    config = "SCA_RightHT.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("SCA_RightHT",config_folder+config, SN, data_path);



    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    SendFEB();
    BoardLib.SetDirectParameters();
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 7. Hold time SCA test (wrong Hold Time) -> no expected signal
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.Hold.HoldHG",60);
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.Hold.HoldLG",60);
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.HG_SCAorPeakD",true);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.LG_SCAorPeakD",true);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.HG_SH_TimeConstant",3);
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.LG_SH_TimeConstant",3);
    }
    //SendFEB();
    config = "SCA_WrongHT.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("SCA_WrongHT",config_folder+config, SN, data_path);


    BoardLib.SetVariable("Board.DirectParam.AdcFsmConfLock", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmReset", true);
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.SetDirectParameters(); //Sync.Sleep(1);
    TurnOffFEB();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Turn off Pulse Gen at the end
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    System.Console.WriteLine("Pulse Generator OFF");

    //Generate dummy file at the end of the script
    string[] o = {"END OF SCRIPT"};
    File.WriteAllLinesAsync(data_path+"EndOfScript_citi.txt",o); 
    return;
}




void RunCITITriggerAcq_8gates(string Test, string config, int SN,string data_path){
    //Sync.Sleep(100);                                                     
    BoardLib.OpenConfigFile(config);
    BoardLib.SetBoardId(0); 
    SendFEB();
    SetDefaultDirectParameters();
    BoardLib.SetDirectParameters();
    //Sync.Sleep(2);                                                     

    
    string file_name = "FCT_"+Test;



    data_path = data_path + "/CITI_trigger_tests/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);

    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(50);                                                                    
    BoardLib.SetBoardId(0); 
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }
    
    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }
    
    //Sync.Sleep(100);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(100);                                                                   
    int channel = 0;
    double Tot_KB_Previous_Iter = 0;
    double Tot_KB = 0;
    DateTime LastIter=DateTime.Now, ThisIter=DateTime.Now;

    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.WriteLine("rate: "+Math.Truncate(rate)+" kB/s");
        if((Tot_KB-Tot_KB_Previous_Iter)<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.WriteLine("Transferred "+BoardLib.XferKBytes+" kB");

    }
    EndOfRunProtocol();

}

void RunCITITriggerAcq_PSCExtTrig(string Test, string config, int SN, string data_path){
    //Sync.Sleep(100);                                                     
    BoardLib.OpenConfigFile(config);
    BoardLib.SetBoardId(0); 
    SendFEB();
    SetDefaultDirectParameters();

    BoardLib.SetDirectParameters();
    //Sync.Sleep(2);                                                     

    
    string file_name = "FCT_"+Test;


    data_path = data_path + "/CITI_trigger_tests/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);

    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(50);                                                                    
    BoardLib.SetBoardId(0); 
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }
    
    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }

    //Sync.Sleep(100);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(100);                                                                   
    int channel = 0;
    double Tot_KB_Previous_Iter = 0;
    double Tot_KB = 0;
    DateTime LastIter=DateTime.Now, ThisIter=DateTime.Now;

    for(int i=0;i<16;i++){        
        if(i==0){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",1);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }
        if(i==4){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",2);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }
        if(i==8){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",0);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }
        channel = (i%8)*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.WriteLine("rate: "+Math.Truncate(rate)+" kB/s");
        if((Tot_KB-Tot_KB_Previous_Iter)<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.WriteLine("Transferred "+BoardLib.XferKBytes+" kB");

    }
    EndOfRunProtocol();

    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",0);
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    //Sync.Sleep(50);                                                                    

}

int RunCITITriggerAcq_32gates(string Test, string config, int SN, string data_path){
    //Sync.Sleep(100);                                                     
    BoardLib.OpenConfigFile(config);
    BoardLib.SetBoardId(0); 
    SendFEB();           
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetDirectParameters();
    
    string file_name = "FCT_"+Test;


    data_path = data_path + "/CITI_trigger_tests/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);

    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.SetBoardId(0); 
    //Sync.Sleep(50);                                                                    
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }
    
    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }

    //Sync.Sleep(100);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(100);                                                                   
    int channel = 0;
    double Tot_KB_Previous_Iter = 0;
    double Tot_KB = 0;
    DateTime LastIter=DateTime.Now, ThisIter=DateTime.Now;

    //First bunch of 8 gates: default config: expect signal in all citirocs
    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.WriteLine("rate: "+Math.Truncate(rate)+" kB/s");
        if((Tot_KB-Tot_KB_Previous_Iter)<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.WriteLine("Transferred "+BoardLib.XferKBytes+" kB");

    }
    //Second bunch of 8 gates: disable valid event: expect no signal
    BoardLib.SetVariable("Board.DirectParam.AveEn", false);
    BoardLib.SetBoardId(0); 
    BoardLib.SetDirectParameters();
    //Sync.Sleep(3);
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("ERROR: DAQ stopped!");
        BoardLib.SetVariable("Board.DirectParam.AveEn", true);
        BoardLib.SetBoardId(0); 
        BoardLib.SetDirectParameters();
        //Sync.Sleep(3);
        return -999;
    }
    Tot_KB_Previous_Iter = 0;
    Tot_KB = 0;

    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.WriteLine("rate: "+Math.Truncate(rate)+" kB/s");
        if((Tot_KB-Tot_KB_Previous_Iter)<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.WriteLine("Transferred "+BoardLib.XferKBytes+" kB");

    }
    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetDirectParameters();
    //Sync.Sleep(100);
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("ERROR: DAQ stopped!");
        BoardLib.SetVariable("Board.DirectParam.AveEn", true);
        BoardLib.SetBoardId(0); //Sync.Sleep(1);
        BoardLib.SetDirectParameters();
        //Sync.Sleep(3);
        return -999;
    }
    //Third bunch of 8 gates: Force Reset PSC: expect no signal
    Tot_KB_Previous_Iter = 0;
    Tot_KB = 0;

    for(int i=0;i<8;i++){        

        if(i==0){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPSC",1);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }        
        if(i==4){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPSC",2);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }

        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.WriteLine("rate: "+Math.Truncate(rate)+" kB/s");
        if((Tot_KB-Tot_KB_Previous_Iter)<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.WriteLine("Transferred "+BoardLib.XferKBytes+" kB");

    }
    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPSC",0);
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    //Sync.Sleep(50);                                                                    
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("ERROR: DAQ stopped!");
        BoardLib.SetVariable("Board.DirectParam.AveEn", true);
        BoardLib.SetBoardId(0); 
        BoardLib.SetDirectParameters();
        //Sync.Sleep(3);
        return -999;
    }
    //Forth bunch of 8 gates: Force Reset PA: expect no signal
    Tot_KB_Previous_Iter = 0;
    Tot_KB = 0;
    LastIter=DateTime.Now;
    ThisIter=DateTime.Now;
    for(int i=0;i<8;i++){        

        if(i==0){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPA",1);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }        
        if(i==4){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPA",2);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }

        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.WriteLine("rate: "+Math.Truncate(rate)+" kB/s");
        if((Tot_KB-Tot_KB_Previous_Iter)<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.WriteLine("Transferred "+BoardLib.XferKBytes+" kB");

    }
    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPA",0);
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    //Sync.Sleep(50);                                                                    



    EndOfRunProtocol();

    return 0;
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

void SelectFEBdevices(){
    // Speak with FEB
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
}

void SendGPIO(){
    // SelectGPIOdevices();
    BoardLib.SetBoardId(126); //Sync.Sleep(3);
    // BoardLib.BoardConfigure();
    // Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
}

void SendFEB(){
    SelectFEBdevices();
    BoardLib.SetBoardId(0);
    BoardLib.BoardConfigure();
    //Sync.Sleep(400);
}

void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); 
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(2000);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1000);
}



void ActivateAllCh(int LG_gain,int HG_gain){
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


static string ExecuteBashCommand(string command)
{
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


void SetKaladin(int channel){
    int asic = channel/32;          // Asic number
    int loc_ch = channel%32;        // Channel within the ASIC
    int loc_MUX = loc_ch/8;         // Which MUX within the ASIC (4 MUXs per ASIC) 
    int Kal_MUX_output = loc_ch%8;  // Which channel output within the MUX (8 output, selected with 3bits number)
    int MUX = asic*4 + loc_MUX;     // Global MUX (32 in total, 4 per ASIC)
    uint Kal_En_hex=0;
    
    System.Console.WriteLine("-------------------------"); 
    System.Console.WriteLine("Kal Ch    :\t"+channel.ToString());
    
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-EN", Math.Pow(2,MUX)); // the GUI does automatically the conversion dec-to-hex. DO NOT FEED WITH A HEX VALUE
    //System.Console.WriteLine("MUX_EN hex: "+Convert.ToString((BoardLib.GetUInt32Variable("GPIO.GPIO-MISC.KAL-EN")),16)); // Manually convert to hex for displaying
    
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-MUX", Kal_MUX_output);
    // /System.Console.WriteLine("MUX_CH: "+BoardLib.GetByteVariable("GPIO.GPIO-MISC.KAL-MUX"));
    
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    //Sync.Sleep(10);

    //System.Console.WriteLine(BoardLib.ElapsedTime);
    //System.Console.WriteLine("average rate: "+BoardLib.AvgXferRate+" kB/s");


}

bool SyncTest(){
    bool success = true;
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(50);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    if(GateEn){
        success = false;
        return success;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(50);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.ReadStatus();
    GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    if(!GateEn){
        success = false;
        return success;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(50);
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    return success;
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

void EndOfRunProtocol(){
    BoardLib.StopAcquisition();
    BoardLib.WaitForEndOfTransfer(true);
    System.Console.WriteLine("END OF ACQUISITION");
    
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    //Sync.Sleep(10);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(100);
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.ReadStatus();
    int GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    while(GateEn){
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        //Sync.Sleep(200); 
        BoardLib.ReadStatus();
        GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");  
    }
    System.Console.WriteLine("Stopped GTS beacon");
}
