void ScriptMain()
{
    string config_folder = "/home/neutrino/FCT/software/GUIscripts/config/";
    string config_path = config_folder+"config_FCT2_newGUI_V2.xml";
    TurnOnFEB();
    BoardLib.OpenConfigFile(config_path);
    //Sync.Sleep(200);
        
       
    // Enable preamp and DAQ on all channels
    ActivateAllCh(56,12);
    // YOU MIGHT WANT TO CHANGE IT TO HAVE THE ADC STARTING AT GATE_CLOSE SIGNAL
    System.Console.WriteLine("FEB is configured");


    RunAcquisition();
    return;
}


void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    //Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); BoardLib.GetFirmwareVersion();
    Sync.Sleep(1500);
}

int RunAcquisition(){
    //Sync.Sleep(500);                                                                    

    int baseline = 32786;
    var BashOutput = "";
    
    string data_path = "/DATA/";

    string file_name = "dummy";
    

    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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
    BoardLib.GetFirmwareVersion();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }

    //Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }

    BoardLib.GetFirmwareVersion();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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
    BoardLib.GetFirmwareVersion();
    //Sync.Sleep(300);                                                                   
    double Tot_KB_Previous_Iter = 0;
    double Tot_KB = 0;
    DateTime LastIter=DateTime.Now, ThisIter=DateTime.Now;
    
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n ");
    for(int channel=0;channel<18;channel++){

        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); //Sync.Sleep(1); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); //Sync.Sleep(1); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        //System.Console.WriteLine("closing gate");  
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

        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(channel,256));

    }
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n\n");

    EndOfRunProtocol();
    return 0;

}


void EndOfRunProtocol(){
    Sync.Sleep(20);
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    System.Console.WriteLine("Closing gate.");
    while(GateEn){
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(126); //Sync.Sleep(1);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
        BoardLib.ReadStatus();
        GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn"); 
        Sync.Sleep(10);
    }
    Sync.Sleep(500);
    System.Console.WriteLine("Gate closed. Stopping GTS.");
    Sync.Sleep(500);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    System.Console.WriteLine("Stopped GTS beacon");
    Sync.Sleep(500);
    BoardLib.StopAcquisition();
    Sync.Sleep(600);
    BoardLib.WaitForEndOfTransfer(true);
    Sync.Sleep(500);
    System.Console.WriteLine("END OF ACQUISITION");
    Sync.Sleep(500);

}


void SetKaladin(int channel){
    int asic = channel/32;          // Asic number
    int loc_ch = channel%32;        // Channel within the ASIC
    int loc_MUX = loc_ch/8;         // Which MUX within the ASIC (4 MUXs per ASIC) 
    int Kal_MUX_output = loc_ch%8;  // Which channel output within the MUX (8 output, selected with 3bits number)
    int MUX = asic*4 + loc_MUX;     // Global MUX (32 in total, 4 per ASIC)
    uint Kal_En_hex=0;
    
    // /System.Console.WriteLine("-------------------------"); 
    //System.Console.WriteLine("Kal Ch    :\t"+channel.ToString());
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-EN", Math.Pow(2,MUX)); // the GUI does automatically the conversion dec-to-hex. DO NOT FEED WITH A HEX VALUE
    //System.Console.WriteLine("MUX_EN hex: "+Convert.ToString((BoardLib.GetUInt32Variable("GPIO.GPIO-MISC.KAL-EN")),16)); // Manually convert to hex for displaying
    
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL-MUX", Kal_MUX_output);
    // /System.Console.WriteLine("MUX_CH: "+BoardLib.GetByteVariable("GPIO.GPIO-MISC.KAL-MUX"));
    
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); BoardLib.GetFirmwareVersion();
    //Sync.Sleep(10);

    //System.Console.WriteLine(BoardLib.ElapsedTime);
    //System.Console.WriteLine("average rate: "+BoardLib.AvgXferRate+" kB/s");


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


void ActivateAllCh(int LG_gain,int HG_gain){
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
    for (int i_ch = 0; i_ch < 256; i_ch++){
        int asic=i_ch/32;
        int local_ch=i_ch%32; 
            // DAC10b
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].GlobalControl.DAC10b", 300);
        // DAC10b_t
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].GlobalControl.DAC10b_t", 300);

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


void SendFEB(byte FEBID=0){
    SelectFEBdevices(FEBID);
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(3);
    BoardLib.BoardConfigure();
    //Sync.Sleep(50);
}
