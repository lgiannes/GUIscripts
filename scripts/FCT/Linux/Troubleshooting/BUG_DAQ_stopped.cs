// There are two types of "DAQ stops" error (yellow and orange). 
// orange: DAQ stops on timeout after timeout time even though gts and gate word should be pushed (as seen on LEDs)
// yellow: DAQ stops upon user or script request in proximity of UppdateUserParameter("FPGA-MISC-Config") 
// when ForceResetPSC is set to 2 (or 3) 

// This script reproduce the following bug:
// orange: DAQ stops on timeout after timeout time even though gts and gate word should be pushed (as seen on LEDs)
// By launching it, you can see that (sometimes) the acquisition stops around gate 4 (the fifth gate)
// and the daq file produced is empty


// Please change config and data path according to the machine
    string file_name = "/DATA/neutrino/FCT/Troubleshooting/test_drive_STOPDAQBUG";
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
    BoardLib.SetBoardId(0); 
    Sync.Sleep(10);

    BoardLib.SetDirectParameters(); Sync.Sleep(220);
 
        
    // Enable preamp and DAQ on all channels
    ActivateAllCh(56,12);
    System.Console.WriteLine("FEB is configured");

    
    RunAcquisition(); // See comments in function!


    return;
}




void RunAcquisition(){

    int baseline = 32786;
        
    // Set baseline
    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.DeviceConfigure(8);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    Sync.Sleep(5);
    BoardLib.SetDirectParameters(); Sync.Sleep(220);

    // Make sure that gate and gts are off, while Readout is on before starting the run
    BoardLib.SetBoardId(126); Sync.Sleep(1); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(200);                                                                    
    BoardLib.SetBoardId(0); Sync.Sleep(1);

    // Start acquisition
    if(BoardLib.StartAcquisition(file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }
    // If the DAQ stops RIGHT AFTER starting, re-start it right away
    // This was to fix a bug often happening on FEB v1, never seen on FEB v2
    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(file_name,true);
    }

    // If the gate is open when the acquisition starts, close it (again)
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    while(GateEn){
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(200); 
        GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");  
    }

    // Start GTS beacon
    Sync.Sleep(20);
    BoardLib.SetBoardId(126); Sync.Sleep(1); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(300);                                                                   

    // Start loop on channels
    for(int channel=0;channel<10;channel++){
        
        // Change Kaladin channel
        SetKaladin(channel);
        Sync.Sleep(50);                                                                   
        // Open gate
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); Sync.Sleep(1); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");     
        // Wait 100 ms to get ~100 signals   
        Sync.Sleep(100);

        // Close gate
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); Sync.Sleep(1); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");  
        Sync.Sleep(10);

    }
    // Stop GTS beacon
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    Sync.Sleep(10);
    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);

    // Make sure tha tgate is closed after data taking. If not, close it
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    BoardLib.ReadStatus();
    GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    while(GateEn){
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(200); 
        BoardLib.ReadStatus();
        GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");  
    }

    // Stop acquisition
    // NOTICE: the DAQ is not always stopped on "user or script request" as it should be. 
    // Sometimes it's closed on Timeout or Stop End Event timeout
    BoardLib.StopAcquisition();
    BoardLib.WaitForEndOfTransfer(true);
    Sync.Sleep(1100);
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