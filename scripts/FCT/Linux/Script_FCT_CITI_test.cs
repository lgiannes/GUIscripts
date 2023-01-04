void ScriptMain(){
    // 
    TurnOnFEB();
    int baseline = 32786;
    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.SetBoardId(0); 
    BoardLib.DeviceConfigure(8);
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
    BoardLib.OpenConfigFile(default_config);
    // DEFAULT: OR32=ON, NOR32=OFF, VALID_EVENT = ON, FORCERESET PSC = 00, FORCE RESET PA = 00, DISABLE TRIG EXT PSC = 00
    SendGPIO();
    ActivateAllCh(LG,HG);
    System.Console.WriteLine("FEB is configured");

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
    
    // 1. OR32=OFF -> no expected signal
    for(int asic=0;asic<8;asic++){
        BoardLib.SetVariable("ASICS.ASIC"+asic.ToString()+".GlobalControl.EnOR32",false);
    }
    config = "OR32OFF.xml";
    BoardLib.SaveConfigFile(config_folder + config);
    RunCITITriggerAcq_8gates("OR32OFF",config_folder+config);

    // Restore default config
    BoardLib.OpenConfigFile(default_config);
    SendGPIO();
    SendFEB();

    // 2. test VALID_event, Force Reset PSC, Force Reset PA (32 gates), 





}




void RunCITITriggerAcq_8gates(string Test, string config){
    Sync.Sleep(100);                                                     
    BoardLib.OpenConfigFile(config);
    SendGPIO();
    SendFEB();
    Sync.Sleep(200);                                                     

    
    string file_name = "FCT_"+Test;

    string data_path   =    "/home/neutrino/FCT/data_local/";
        // CREATE THE DATA DIRECTORY BASED ON THE SERIAL NUMBER
    data_path = data_path + "SN_" + SN.ToString() + "/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);
    data_path = data_path + "/CITI_trigger_tests/";
    DATAfolder = System.IO.Directory.CreateDirectory(data_path);


    BoardLib.SetBoardId(0); 
    Sync.Sleep(50);                                                                    
    BoardLib.StartAcquisition(data_path + file_name,true); 
                                                                        System.Console.WriteLine("Asynchronous acquisition started");

    Sync.Sleep(100);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);                                                                   
    int channel = 0;
    for(int i=0;i<8;i++){        
        channel = i*32 + 16*(i/4) + (int)(Math.Pow(2,i%4))-1;
        System.Console.WriteLine("asic " + i + " channel " + (channel-i*32).ToString());
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
