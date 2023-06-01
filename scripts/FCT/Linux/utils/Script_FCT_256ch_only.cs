/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// INIT SETTINGS (should put this in a config file or similar)

//Name of default configuration
string config_folder = Environment.GetEnvironmentVariable("CONFIGFOLDER");
                        //"/home/lorenzo/T2K-uniGe/FEB_GPIO/FEB-GPIO_firmware/UT_60charge/etc/config/linearity_one_channel.xml";
//Set the path to which data should be saved
string data_path   =    Environment.GetEnvironmentVariable("GENERALDATADIR")+"/FEBs/";  
                        //"/DATA/dataFCT/";
                        //"/home/lorenzo/T2K-uniGe/FEB_GPIO/data/linearity_tests_citiroc/multichannelHGLG/";

int LG = 56;
int HG = 12;
double amplitude = 0.03;//V

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



void ScriptMainArgs(int SN,int bl1, int bl2){
    
    string config_path = config_folder+"config_FCT2_newGUI_V2.xml";
    string nochannels_config_path = config_folder+"NOCHANNELS.xml";

    // // Delete "EndOfScript.txt" dummy file if it exists in the data directory
    // NO NEED TO DO. ALREADY DONE IN THE BASH SCRIPT 
    // if (File.Exists(Path.Combine(data_path, "EndOfScript.txt"))){    
    //     // If file found, delete it    
    //     File.Delete(Path.Combine(data_path, "EndOfScript.txt"));    
    // }    

    // CREATE THE DATA DIRECTORY BASED ON THE SERIAL NUMBER
    data_path = data_path + "SN_" + SN.ToString() + "/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);

    // BoardLib.Reconnect();
    System.Console.Write("Preparing 256-chs test ...  3\r");
    Sync.Sleep(1500);
    System.Console.Write("Preparing 256-chs test ...  2\r");
    Sync.Sleep(1500);
    System.Console.Write("Preparing 256-chs test ...  1\r");


    TurnOnFEB();
    System.Console.WriteLine("FEB is on.                           ");
    BoardLib.GetFirmwareVersion();
    BoardLib.OpenConfigFile(config_path);
    // Set the required Direct Parameters
    SetDefaultDirectParameters();

    // Send to board
    BoardLib.SetBoardId(0); 
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
    BoardLib.OpenConfigFile(nochannels_config_path);
    //Sync.Sleep(200);
        
    // Enable preamp and DAQ on all channels
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


    int AcqTag = RunAcquisition();
    if(AcqTag==-10){
        System.Console.WriteLine("Re-running 256-ch acquisition!");
        AcqTag = RunAcquisition();
    }
    //BoardLib.Reconnect();
    // Sync.Sleep(500);
    // TurnOffFEB();
    // Sync.Sleep(1000);
    // TurnOnFEB();
    // Sync_good = false;
    // Sync_good = SyncTest();
    // if(!Sync_good){
    //     System.Console.WriteLine("Sync not working");
    //     return;
    // }else{
    //     System.Console.WriteLine("Sync test Successful!");
    // }

    //Restore initial config
    BoardLib.OpenConfigFile(config_path);
    SendGPIO(3);
    SetDefaultDirectParameters();

    BoardLib.SetBoardId(0); //Sync.Sleep(5);
    BoardLib.SetDirectParameters(); //Sync.Sleep(3);
    //Sync.Sleep(200);
    ActivateAllCh(LG,HG);
    //Sync.Sleep(200);

    RunBaselineAcq(bl1);

    //BoardLib.Reconnect();
    // Sync.Sleep(500);
    // TurnOffFEB();
    // Sync.Sleep(1000);
    // TurnOnFEB();
    // Sync_good = false;
    // Sync_good = SyncTest();
    // if(!Sync_good){
    //     System.Console.WriteLine("Sync not working");
    //     return;
    // }else{
    //     System.Console.WriteLine("Sync test Successful!");
    // }

    //Restore initial config
    BoardLib.OpenConfigFile(config_path);
    SendGPIO(3);
    SetDefaultDirectParameters();

    BoardLib.SetBoardId(0); //Sync.Sleep(5);
    BoardLib.SetDirectParameters(); //Sync.Sleep(3);
    //Sync.Sleep(250);
    ActivateAllCh(LG,HG);
    //Sync.Sleep(200);

    RunBaselineAcq(bl2);

    
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    SetDefaultDirectParameters();
    BoardLib.SetDirectParameters();

    ////////////////////////////////////////////////////////////////////////////////////
    //CITIROC_triggers_test(SN,LG,HG);

    ////////////////////////////////////////////////////////////////////////////////////

    // Turn off Pulse Gen at the end
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    System.Console.WriteLine("Pulse Generator OFF");

    //Generate dummy file at the end of the script
    string[] o = {"END OF SCRIPT"};
    File.WriteAllLinesAsync(data_path+"EndOfScript_256only.txt",o); 
    System.Console.WriteLine("END OF SCRIPT");
    return;
}




int RunAcquisition(){
    //Sync.Sleep(500);                                                                    

    int baseline = 32786;
    var BashOutput = "";
    
    string file_name = "FCT_os_LG"+LG.ToString()+"HG"+HG.ToString()+"amp"+((int)1000*amplitude).ToString()+"mV_"+"bl"+baseline.ToString();
    

    for(int asic = 0;asic<8;asic++){
        if(asic==0){
            BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
            BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
        }else{
            BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
            BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
        }
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
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }

    //Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
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
    for(int channel=0;channel<256;channel++){

        // BoardLib.SetBoardId(0); //Sync.Sleep(1);
        // BoardLib.ReadStatus();
        // bool Gate_is_open = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
        // if(Gate_is_open){
        //     System.Console.WriteLine("++++++++++++++++++++++++++++++++");
        //     System.Console.WriteLine("++++ ERROR in gate sequence ++++");
        //     System.Console.WriteLine("++++++++++++++++++++++++++++++++");
        //     System.Console.WriteLine("(gate not closed)");
                        
        //     return -10;
        // }
        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); //Sync.Sleep(1); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        //System.Console.WriteLine("opening gate");  
        // BoardLib.SetBoardId(0); //Sync.Sleep(1);
        // BoardLib.ReadStatus();
        // Gate_is_open = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
        // if(!Gate_is_open){
        //     System.Console.WriteLine("++++++++++++++++++++++++++++++++");
        //     System.Console.WriteLine("++++ ERROR in gate sequence ++++");
        //     System.Console.WriteLine("++++++++++++++++++++++++++++++++");
        //     BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        //     BoardLib.SetBoardId(126); //Sync.Sleep(1); 
        //     BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        //     System.Console.WriteLine("RE-opening gate"); 
        // }   
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

        // BoardLib.SetBoardId(0); //Sync.Sleep(1);
        // BoardLib.ReadStatus();
        // Gate_is_open = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
        // if(Gate_is_open){
        //     System.Console.WriteLine("ERROR in gate sequence");
        //     return -10;
        // }
        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(channel,256));

    }
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n\n");

    EndOfRunProtocol();
    return 0;

}


void RunBaselineAcq(int baseline){
    
    string file_name = "FCT_BLTEST_LG"+LG.ToString()+"HG"+HG.ToString()+"amp"+((int)1000*amplitude).ToString()+"mV_"+"baseline"+baseline.ToString();
    var BashOutput = "";
    
    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.DeviceConfigure(8, x_verbose:false);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    //Sync.Sleep(5);
    BoardLib.SetDirectParameters(); //Sync.Sleep(3);

    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    //Sync.Sleep(200);                                                                    
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }

    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }

    //Sync.Sleep(20);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(100);                                                                   
    double Tot_KB_Previous_Iter = 0;
    double Tot_KB = 0;
    DateTime LastIter=DateTime.Now, ThisIter=DateTime.Now;
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n ");
    for(int i=0;i<8;i++){
        int channel = 0;
        // WARNING: if you change the definition of channel here, you need to change also the ROOT analysis:
        // function at "Gate_to_Kal_Ch" defined at line 369 of "FCTbaseline.cpp". Then, recompile ROOT analysis
        channel = i*32;// + 16*(i/4) + (int)(Math.Pow(2,i%4))-1;
        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //System.Console.WriteLine("asic " + channel/32 + " channel " + (channel%32).ToString());
                                                                        //System.Console.WriteLine("Kaladin set");       
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); //Sync.Sleep(1); Sync.Sleep(1);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("opening gate");       
        if( !BoardLib.GetBoolVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen") ){
            System.Console.WriteLine("ERROR: GATE NOT OPEN");
            break;
        }
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("closing gate");       
        if( BoardLib.GetBoolVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen") ){
            System.Console.WriteLine("ERROR: GATE NOT CLOSED");
            break;
        }        //Sync.Sleep(10);

        
        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.Write("rate: "+Math.Truncate(rate)+" kB/s | ");
        if(rate<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(i,8));
    }
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n\n");

    EndOfRunProtocol();


}




bool SyncTest(){
    bool success = true;
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(50);
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    if(GateEn){
        success = false;
        return success;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(50);
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.ReadStatus();
    GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    if(!GateEn){
        success = false;
        return success;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    //Sync.Sleep(50);
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
    return success;
}



void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    //Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    //Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(3000);
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
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    //Sync.Sleep(10);

    //System.Console.WriteLine(BoardLib.ElapsedTime);
    //System.Console.WriteLine("average rate: "+BoardLib.AvgXferRate+" kB/s");


}




void ActivateAllCh(int LG_gain,int HG_gain){
    BoardLib.SetBoardId(0); //Sync.Sleep(1);
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
    for(int i=0;i<13;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
    for(int i=13;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
}


void SendFEB(byte FEBID=0){
    SelectFEBdevices(FEBID);
    BoardLib.SetBoardId(0); //Sync.Sleep(3);
    BoardLib.BoardConfigure();
    //Sync.Sleep(50);
}

void CallRootExe_unpack(){
    System.Diagnostics.Process p = new System.Diagnostics.Process();
    System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo{
        FileName = "cmd.exe", RedirectStandardInput = true, UseShellExecute = false
    };
    p.StartInfo = info;
    p.Start();

    //System.Console.WriteLine(p.StandardInput.BaseStream.CanWrite);
    
    if(p.StandardInput.BaseStream.CanWrite)
    {
        //p.StandardInput.WriteLine(@"wsl cd; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f HV34859_freq10M_pow530.daq > output.txt; exit");
        if(Environment.Is64BitProcess)
        {
            System.Console.WriteLine("is 64bit process");
            //p.StandardInput.WriteLine(@"bash -c ""cd; echo dcscas > output.txt; exit"" ");
            //p.StandardInput.WriteLine(@"bash -c ""cd; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f HV34859_freq10M_pow530.daq > unpacking_output.txt; exit"" ");
            p.StandardInput.WriteLine(@"bash -c ""cd; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f /mnt/c/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/root_exe/daq_test_root_exe/HV34859_freq10M_pow530.daq > unpacking_output.txt; exit"" ");
            p.StandardInput.Flush();
            p.StandardInput.Close();
        }else{
            System.Console.WriteLine("is a 32bit process");
            //p.StandardInput.WriteLine(@"C:\Windows\Sysnative\bash.exe -c ""cd; echo dcscas > output.txt; exit"" ");
            //p.StandardInput.WriteLine(@"C:\Windows\Sysnative\bash.exe -c ""cd; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f HV34859_freq10M_pow530.daq > output.txt; exit"" ");
            //p.StandardInput.WriteLine(@"C:\Windows\Sysnative\bash.exe -c ""cd; cp /mnt/c/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/root_exe/daq_test_root_exe/HV34859_freq10M_pow530.daq . "" ");
            p.StandardInput.WriteLine(@"C:\Windows\Sysnative\bash.exe -c ""cd; cp /mnt/c/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/root_exe/daq_test_root_exe/HV34859_freq10M_pow530.daq .; ./Root_SW_FCTEST/sFGD_unpacking/bin/unpack -f HV34859_freq10M_pow530.daq > output.txt; exit"" ");
            p.StandardInput.Flush();
            p.StandardInput.Close();            
        }
    }
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
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n ");
    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.Write("rate: "+Math.Truncate(rate)+" kB/s | ");
        if(rate<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(i,8));

    }
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n\n");

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
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n ");
    for(int i=0;i<16;i++){        
        if(i==0){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",2);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }
        if(i==4){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",1);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }
        if(i==8){
            BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",3);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }
        channel = (i%8)*32;
        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.Write("rate: "+Math.Truncate(rate)+" kB/s | ");
        if(rate<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(i,16));

    }
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n\n");

    EndOfRunProtocol();
                                                                 

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
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n ");

    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.Write("rate: "+Math.Truncate(rate)+" kB/s | ");
        if(rate<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(i,32));

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
    Tot_KB_Previous_Iter = Convert.ToDouble(BoardLib.XferKBytes);
    Tot_KB = 0;
    LastIter = DateTime.Now;
    for(int i=0;i<8;i++){        
        channel = i*32;
        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.Write("rate: "+Math.Truncate(rate)+" kB/s | ");
        if(rate<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(i+8,32));

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
    Tot_KB_Previous_Iter = Convert.ToDouble(BoardLib.XferKBytes);
    Tot_KB = 0;
    LastIter = DateTime.Now;

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
        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.Write("rate: "+Math.Truncate(rate)+" kB/s | ");
        if(rate<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(i+16,32));

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
    Tot_KB_Previous_Iter = Convert.ToDouble(BoardLib.XferKBytes);
    Tot_KB = 0;
    LastIter=DateTime.Now;
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
        System.Console.Write("\r Kal Ch: "+channel.ToString()+" | ");
        SetKaladin(channel);
        //Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("opening gate");       
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        // System.Console.WriteLine("closing gate");       
        Sync.Sleep(70);

        Tot_KB = Convert.ToDouble(BoardLib.XferKBytes);
        ThisIter = DateTime.Now;
        double rate = (Tot_KB-Tot_KB_Previous_Iter)*1000/(double)(ThisIter-LastIter).TotalMilliseconds;
        System.Console.Write("rate: "+Math.Truncate(rate)+" kB/s | ");
        if(rate<10){
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+  FATAL ERROR: NOT PUSHING GTS/Gate! +");
            System.Console.WriteLine("+                                     +");
            System.Console.WriteLine("+++++++++++++++++++++++++++++++++++++++");
            break;
        }
        Tot_KB_Previous_Iter = Tot_KB;
        LastIter = DateTime.Now;
        
        System.Console.Write("Transferred "+BoardLib.XferKBytes+" kB \t"+GenerateProgressString(i+24,32));

    }
    System.Console.WriteLine("\n\n-----------------------------------------------------------\n\n");

    BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPA",0);
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    //Sync.Sleep(50);                                                                    



    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    EndOfRunProtocol();
    return 0;
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

string GenerateProgressString(int p, int t){
    int percent4 = (int)Math.Ceiling( 25*((double)(p+1)/(double)t) );
    if(percent4>25) percent4 = 25;
    string progressString = "[";
    string bars = new string(char.Parse("|"),percent4);
    string spaces = new string(char.Parse(" "),25-percent4);

    progressString = progressString + bars + spaces + "]" + 4*percent4 + "%";

    return progressString;
}