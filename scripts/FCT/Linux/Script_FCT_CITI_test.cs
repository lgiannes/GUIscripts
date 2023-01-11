void ScriptMain(){
// void ScriptMainArgs(int SN){
    int SN = 1; // to be set as argument when the script is launched from bash

    int LG =56;
    int HG =12;
    string data_path = "/home/neutrino/FCT/data_local/";

    // Set up communication with Pulse gen
    var BashOutput = ExecuteBashCommand("bash fg_setup.sh");
    Sync.Sleep(50);
    BashOutput = ExecuteBashCommand("echo \"OUTPUT ON\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT ON\" | cat > /dev/ttyACM0");
    Sync.Sleep(50);
    if(string.Compare(BashOutput,"error: no device connected\n")==0){
        System.Console.WriteLine(BashOutput);
    }else{
        System.Console.WriteLine("Pulse gen is configured");
    }

    TurnOnFEB();
    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("Board.DirectParam.ExtClkEn", true);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", false);
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetVariable("Board.DirectParam.GtEn", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmConfLock", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmReset", true);
    BoardLib.SetVariable("Board.DirectParam.IGEn", false);
    BoardLib.SetDirectParameters();

    string config_folder = "/home/neutrino/FCT/code/config/";
    string default_config = config_folder + "config_FCT2_newGUI.xml";
    string config="";
    // The default config is the same as the one used for the 256ch + baseline test
    // where the ADC starts on OR32 and enOR32=ON
    
    BoardLib.OpenConfigFile(default_config);
    SendFEB();
    System.Console.WriteLine("FEB is configured");


    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 1. test VALID_event, Force Reset PSC, Force Reset PA (32 gates),
    // ADC starts on OR32 and enOR32=ON (default): 
    // signal expected ONLY in the first 8 gates (default config with OR32=ON)
    RunCITITriggerAcq_32gates("OR32ON_ValEv_ResetPSC_ResetPA",default_config, SN);
    
    TurnOffFEB();
    TurnOnFEB();

    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    SendFEB();
    BoardLib.SetDirectParameters();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 2. ADC starts on OR32, enOR32=OFF -> no expected signal
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnOR32",false);
    }
    SendFEB();
    config = "OR32OFF.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("OR32OFF",config_folder+config, SN);

    TurnOffFEB();
    TurnOnFEB();

    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    SendFEB();
    BoardLib.SetDirectParameters();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 3. ADC starts on NOR32, enNOR32=ON -> signal expected in each gate
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.ADC.AdcStartsignal","NOR32x8");
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnNOR32",true);
    }
    SendFEB();
    config = "NOR32ON.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("NOR32ON",config_folder+config, SN, data_path);

    TurnOffFEB();
    TurnOnFEB();

    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    SendFEB();
    BoardLib.SetDirectParameters();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 4. ADC starts on NOR32_t, enNOR32_t=ON -> signal expected in each gate
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.ADC.AdcStartsignal","NOR32Tx8");
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnNOR32_t",true);
    }
    SendFEB();
    config = "NOR32TON.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("NOR32TON",config_folder+config, SN, data_path);

    TurnOffFEB();
    TurnOnFEB();

    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    SendFEB();
    BoardLib.SetDirectParameters();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 5. PSCExtTrig: loopback Or32 to ExtTrigPSC, 
    // gates 0-3: disable ExtTrigPSC on the first 4 CITI -> NO expected signal
    // gates 4-7: disable ExtTrigPSC on the last 4 CITI -> NO expected signal
    // gates 8-15: enable ExtTrigPSC on all CITI -> signal expected in all gates
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Debug.OR32toTrigExtPSC",true);
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.SelTrigExtPSC",true);
    }
    SendFEB();
    config = "PSCExtTrig.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_PSCExtTrig("PSCExtTrig",config_folder+config, SN, data_path);

    TurnOffFEB();
    TurnOnFEB();

    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    SendFEB();
    BoardLib.SetDirectParameters();
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
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".GlobalControl.HG_SH_TimeConstant",3);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".GlobalControl.LG_SH_TimeConstant",3);
    }
    SendFEB();
    config = "SCA_RightHT.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_PSCExtTrig("SCA_RightHT",config_folder+config, SN, data_path);

    TurnOffFEB();
    TurnOnFEB();

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
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".GlobalControl.HG_SH_TimeConstant",3);
        BoardLib.SetVariable("ASICS.ASIC"+i.ToString()+".GlobalControl.LG_SH_TimeConstant",3);
    }
    SendFEB();
    config = "SCA_WrongHT.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_PSCExtTrig("SCA_WrongHT",config_folder+config, SN, data_path);

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
    Sync.Sleep(100);                                                     
    BoardLib.OpenConfigFile(config);
    SendFEB();
    Sync.Sleep(200);                                                     

    
    string file_name = "FCT_"+Test;


    // CREATE THE DATA DIRECTORY BASED ON THE SERIAL NUMBER
    data_path = data_path + "SN_" + SN.ToString() + "/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);
    data_path = data_path + "/CITI_trigger_tests/";
    DATAfolder = System.IO.Directory.CreateDirectory(data_path);


    BoardLib.SetBoardId(0); 
    Sync.Sleep(50);                                                                    
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }
    
    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }
    
    Sync.Sleep(100);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);                                                                   
    int channel = 0;
    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(500);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(10);

                                                                        //System.Console.WriteLine("channel "+channel.ToString()+" done");
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);
    BoardLib.SetBoardId(0); 
    Sync.Sleep(100);
    BoardLib.StopAcquisition();
    Sync.SleepUntil( ()=>!BoardLib.IsTransferingData );
                                                                        System.Console.WriteLine("END OF ACQUISITION");
}

void RunCITITriggerAcq_PSCExtTrig(string Test, string config, int SN, string data_path){
    Sync.Sleep(100);                                                     
    BoardLib.OpenConfigFile(config);
    SendFEB();
    Sync.Sleep(200);                                                     

    
    string file_name = "FCT_"+Test;

    // CREATE THE DATA DIRECTORY BASED ON THE SERIAL NUMBER
    data_path = data_path + "SN_" + SN.ToString() + "/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);
    data_path = data_path + "/CITI_trigger_tests/";
    DATAfolder = System.IO.Directory.CreateDirectory(data_path);


    BoardLib.SetBoardId(0); 
    Sync.Sleep(50);                                                                    
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }
    
    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }

    Sync.Sleep(100);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);                                                                   
    int channel = 0;
    for(int i=0;i<16;i++){        
        if(i==0){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",1);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            BoardLib.SetBoardId(126); 
        }
        if(i==4){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",2);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            BoardLib.SetBoardId(126); 
        }
        if(i==8){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",0);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            BoardLib.SetBoardId(126); 
        }
        channel = (i%8)*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(500);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(10);

                                                                        //System.Console.WriteLine("channel "+channel.ToString()+" done");
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);
    BoardLib.SetBoardId(0); 
    Sync.Sleep(100);
    BoardLib.StopAcquisition();
    Sync.SleepUntil( ()=>!BoardLib.IsTransferingData );
                                                                        System.Console.WriteLine("END OF ACQUISITION");
}

void RunCITITriggerAcq_32gates(string Test, string config, int SN, data_path){
    Sync.Sleep(100);                                                     
    BoardLib.OpenConfigFile(config);
    SendFEB();
    Sync.Sleep(200);                                                     

    
    string file_name = "FCT_"+Test;

    // CREATE THE DATA DIRECTORY BASED ON THE SERIAL NUMBER
    data_path = data_path + "SN_" + SN.ToString() + "/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);
    data_path = data_path + "/CITI_trigger_tests/";
    DATAfolder = System.IO.Directory.CreateDirectory(data_path);


    BoardLib.SetBoardId(0); 
    Sync.Sleep(50);                                                                    
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }
    
    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }

    Sync.Sleep(100);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);                                                                   
    int channel = 0;

    //First bunch of 8 gates: default config: expect signal in all citirocs
    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(500);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(10);
    }
    //Second bunch of 8 gates: disable valid event: expect no signal
    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("Board.DirectParam.AveEn", false);
    BoardLib.SetDirectParameters();
    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(500);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(10);
    }
    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetDirectParameters();
    //Third bunch of 8 gates: Force Reset PSC: expect no signal
    for(int i=0;i<8;i++){        

        if(i==0){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPSC",1);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            BoardLib.SetBoardId(126); 
        }        
        if(i==4){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPSC",2);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            BoardLib.SetBoardId(126); 
        }

        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(500);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(10);
    }
    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPSC",0);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");

    //Forth bunch of 8 gates: Force Reset PA: expect no signal
    for(int i=0;i<8;i++){        

        if(i==0){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPA",1);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            BoardLib.SetBoardId(126); 
        }        
        if(i==4){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPA",2);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            BoardLib.SetBoardId(126); 
        }

        channel = i*32;
        System.Console.WriteLine("asic " + (channel/32).ToString() + " channel " + (channel%32).ToString());
        SetKaladin(channel);
        Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        Sync.Sleep(500);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        Sync.Sleep(10);
    }
    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPA",0);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");



    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);
    BoardLib.SetBoardId(0); 
    Sync.Sleep(100);
    BoardLib.StopAcquisition();
    Sync.SleepUntil( ()=>!BoardLib.IsTransferingData );
                                                                        System.Console.WriteLine("END OF ACQUISITION");
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
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
}

void SendFEB(byte FEBID=0){
    SelectFEBdevices(FEBID);
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
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
    System.Console.WriteLine("Ch    :\t"+channel.ToString());
    
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-EN", Math.Pow(2,MUX)); // the GUI does automatically the conversion dec-to-hex. DO NOT FEED WITH A HEX VALUE
    System.Console.WriteLine("MUX_EN hex: "+Convert.ToString((BoardLib.GetUInt32Variable("GPIO.GPIO-MISC.KAL-EN")),16)); // Manually convert to hex for displaying
    
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-MUX", Kal_MUX_output);
    System.Console.WriteLine("MUX_CH: "+BoardLib.GetByteVariable("GPIO.GPIO-MISC.KAL-MUX"));
    
    BoardLib.SetBoardId(126); 
    Sync.Sleep(10);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");

}
