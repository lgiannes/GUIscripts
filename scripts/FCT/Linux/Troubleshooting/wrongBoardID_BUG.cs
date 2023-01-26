// BUG: Wrong board ID prompted on SetDirectParameters.
// This bugs happens every second time you launch the script, regardless of the sleep time
    string file_name = "/DATA/neutrino/FCT/Troubleshooting/test_drive_UpdateUserParam_sleeptime";
    string config_path = "/home/neutrino/FCT/code/config/config_FCT2_newGUI_V2.xml";


void ScriptMain(){
    

    TurnOnFEB();
    System.Console.WriteLine("FEB is on");
    

    BoardLib.OpenConfigFile(config_path);
    SendGPIO();
    // Set the required Direct Parameters
    BoardLib.SetVariable("Board.DirectParam.ExtClkEn", true);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", false);
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetVariable("Board.DirectParam.GtEn", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmConfLock", false);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmReset", false);
    BoardLib.SetVariable("Board.DirectParam.IGEn", false);
    // Send to board
    BoardLib.SetBoardId(0); Sync.Sleep(250);
    BoardLib.SetDirectParameters(); Sync.Sleep(250);

        
    // Enable preamp and DAQ on all channels
    ActivateAllCh(56,12);
    // YOU MIGHT WANT TO CHANGE IT TO HAVE THE ADC STARTING AT GATE_CLOSE SIGNAL
    System.Console.WriteLine("FEB is configured");



    RunAcquisition();

    TurnOffFEB();
    return;
}




void RunAcquisition(){
    int channel = 0;
                                                              
    // Set baseline
    int baseline = 32786;    
    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.SetBoardId(0); Sync.Sleep(250); 
    BoardLib.DeviceConfigure(8);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    Sync.Sleep(250); 
    BoardLib.SetDirectParameters(); Sync.Sleep(250); 
    Sync.Sleep(3); 

    // Make sure GTS and Gate are off before starting data taking
    BoardLib.SetBoardId(126); Sync.Sleep(250); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(20);                                                                    
    BoardLib.SetBoardId(0); Sync.Sleep(250); 

    // Start data taking
    if(BoardLib.StartAcquisition(file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }

    // If gate is open, close it
    BoardLib.SetBoardId(0); Sync.Sleep(250);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    while(GateEn){
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(20); 
        GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");  
    }

    // Start GTS beacon
    Sync.Sleep(20);
    BoardLib.SetBoardId(126); Sync.Sleep(250); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(30);                                                                   

        // Select channel, open and close gate
        SetKaladin(channel);
        Sync.Sleep(10);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); Sync.Sleep(250); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");     
 
        Sync.Sleep(100);

        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); Sync.Sleep(250); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");  
     
        Sync.Sleep(10);

    // Stop GTS beacons
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    Sync.Sleep(10);
    BoardLib.SetBoardId(126); Sync.Sleep(250); 
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(10);
    BoardLib.SetBoardId(0); Sync.Sleep(250);
    BoardLib.ReadStatus();

    // Make sure Gate is close at the end of daq
    GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    while(GateEn){
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(20); 
        GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");  
    }

    // Stop acquisition
    // NOTICE: the DAQ is not always stopped on "user or script request" as it should be. 
    // Sometimes it's closed on Timeout or Stop End Event timeout
    BoardLib.StopAcquisition();
    BoardLib.WaitForEndOfTransfer(true);
    Sync.Sleep(10);
    Sync.SleepUntil( ()=>!BoardLib.IsTransferingData );
    
    System.Console.WriteLine("END OF ACQUISITION");



}


void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); Sync.Sleep(1); Sync.Sleep(1);
    Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}

void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); Sync.Sleep(1); Sync.Sleep(1);
    Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(500);
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
    
    BoardLib.SetBoardId(126); Sync.Sleep(1); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(10);

}

void ActivateAllCh(int LG_gain,int HG_gain){
    BoardLib.SetBoardId(0); Sync.Sleep(1);
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

void SendGPIO(){
    SelectGPIOdevices();
    BoardLib.SetBoardId(126); Sync.Sleep(3);
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
}

void SendFEB(byte FEBID=0){
    SelectFEBdevices(FEBID);
    BoardLib.SetBoardId(0); Sync.Sleep(3);
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
}