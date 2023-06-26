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



void ScriptMainArgs(int SN,int bl1, int bl2,bool calib_only =false, bool CITI_only=false){
    
    string config_path = config_folder+"config_FCT2_newGUI_V2.xml";
    int GPIO=Int32.Parse(Environment.GetEnvironmentVariable("GPIO_SN"));    
    string[] o = {"END OF SCRIPT"};

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
    //System.Console.WriteLine("FW version: "+BoardLib.GetFirmwareVersion());
    BoardLib.OpenConfigFile(config_path);
    // Set the required Direct Parameters
    SetDefaultDirectParameters();

    // Send to board

    BoardLib.SetDirectParameters(); //Sync.Sleep(3);

    if(calib_only){
        Calibration(SN,GPIO);
        //Generate dummy file at the end of the script
        File.WriteAllLinesAsync(data_path+"EndOfCalib.txt",o); 
        System.Console.WriteLine("END OF SCRIPT");
        return;
    }
    bool Sync_good = false;
    Sync_good = SyncTest();
    if(!Sync_good){
        System.Console.WriteLine("Sync not working");
        return;
    }else{
         System.Console.WriteLine("Sync test Successful!");
    }
    //Restore initial config
    BoardLib.OpenConfigFile(config_path);
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

    int AcqTag = -10;
    if(!CITI_only){
        AcqTag = RunAcquisition();
        if(AcqTag==-10){
            System.Console.WriteLine("");
            System.Console.WriteLine("Re-running 256-ch acquisition!");
            System.Console.WriteLine(" ");
            AcqTag = RunAcquisition();
        }

        //Restore initial config
        BoardLib.OpenConfigFile(config_path);
        SendGPIO(3);
        SetDefaultDirectParameters();

        BoardLib.SetBoardId(126); //Sync.Sleep(5);
        BoardLib.GetFirmwareVersion();
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(5);
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
        BoardLib.SetBoardId(126); //Sync.Sleep(5);
        BoardLib.GetFirmwareVersion();
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(5);
        BoardLib.SetDirectParameters(); //Sync.Sleep(3);
        //Sync.Sleep(250);
        ActivateAllCh(LG,HG);
        //Sync.Sleep(200);

        RunBaselineAcq(bl2);
    }

    
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    SetDefaultDirectParameters();
    BoardLib.SetBoardId(126); //Sync.Sleep(5);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.GetFirmwareVersion();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(5);
    BoardLib.SetDirectParameters();

    ////////////////////////////////////////////////////////////////////////////////////
    CITIROC_triggers_test(SN,LG,HG);

    ////////////////////////////////////////////////////////////////////////////////////

    // Turn off Pulse Gen at the end
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    System.Console.WriteLine("Pulse Generator OFF");
    // TurnOffFEB();

    if(!CITI_only){
        Calibration(SN,GPIO);
    }
    TurnOffFEB();
    //Generate dummy file at the end of the script
    File.WriteAllLinesAsync(data_path+"EndOfScript.txt",o); 
    System.Console.WriteLine("END OF SCRIPT");
    return;
}




int RunAcquisition(){
    //Sync.Sleep(500);                                                                    

    int baseline = 32786;
    var BashOutput = "";
    
    string file_name = "FCT_os_LG"+LG.ToString()+"HG"+HG.ToString()+"amp"+((int)1000*amplitude).ToString()+"mV_"+"bl"+baseline.ToString();
    

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
    for(int channel=0;channel<256;channel++){

        // BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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
        // BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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

        // BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.DeviceConfigure(8, x_verbose:false);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    //Sync.Sleep(5);
    BoardLib.SetDirectParameters(); //Sync.Sleep(3);

    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.ReadoutEn",true);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.GetFirmwareVersion();

    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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
    BoardLib.GetFirmwareVersion();

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
    BoardLib.GetFirmwareVersion();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    if(GateEn){
        success = false;
        return success;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126); //Sync.Sleep(1);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.GetFirmwareVersion();
    //Sync.Sleep(50);
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
    return success;
}



void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-SEL-IN", true);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    //Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); BoardLib.GetFirmwareVersion();
    Sync.Sleep(1500);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); //Sync.Sleep(1); //Sync.Sleep(1);
    //Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); BoardLib.GetFirmwareVersion();
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
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); BoardLib.GetFirmwareVersion();
    //Sync.Sleep(10);

    //System.Console.WriteLine(BoardLib.ElapsedTime);
    //System.Console.WriteLine("average rate: "+BoardLib.AvgXferRate+" kB/s");


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
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
    BoardLib.GetFirmwareVersion();
    //Sync.Sleep(50);                                                                    
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
    BoardLib.GetFirmwareVersion();

    //Sync.Sleep(50);                                                                    
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
            BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",2);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }
        if(i==4){
            BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DisableTrigExtPSC",1);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }
        if(i==8){
            BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
    BoardLib.SetBoardId(126); 
    BoardLib.GetFirmwareVersion();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
    BoardLib.GetFirmwareVersion();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
    BoardLib.GetFirmwareVersion();

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
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
    BoardLib.SetDirectParameters();
    //Sync.Sleep(3);
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("ERROR: DAQ stopped!");
        BoardLib.SetVariable("Board.DirectParam.AveEn", true);
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetDirectParameters();
    //Sync.Sleep(100);
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("ERROR: DAQ stopped!");
        BoardLib.SetVariable("Board.DirectParam.AveEn", true);
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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
            BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPSC",1);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }        
        if(i==4){
            BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPSC",0);
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
    //Sync.Sleep(50);                                                                    
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("ERROR: DAQ stopped!");
        BoardLib.SetVariable("Board.DirectParam.AveEn", true);
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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
            BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.ForceResetPA",1);
            BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
            BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
            //Sync.Sleep(50);                                                                    
            BoardLib.SetBoardId(126); 
        }        
        if(i==4){
            BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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

    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); 
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



void CITIROC_triggers_test(int SN, int LG, int HG){
//    int SN = 1; // to be set as argument when the script is launched from bash



    // CREATE THE DATA DIRECTORY BASED ON THE SERIAL NUMBER
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);



    string default_config = config_folder + "config_FCT2_newGUI_V2.xml";
    string config="";
    // The default config is the same as the one used for the 256ch + baseline test
    // where the ADC starts on OR32 and enOR32=ON
    BoardLib.OpenConfigFile(default_config);
    SendGPIO(3);
    // Set the required Direct Parameters
    SetDefaultDirectParameters();
    
    // Send to board
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0);
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
    SendGPIO(3);
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
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0);
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

    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.Hold.HoldHG",60);
    BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Global.Analog-path.Hold.HoldLG",60);
    for(int asic=0;asic<8;asic++){
        // Set the shaper time constant and the Hold time to NOT match: if the ADC starts, it should see nothing
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
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
    BoardLib.SetDirectParameters(); //Sync.Sleep(1);
    // TurnOffFEB();

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Turn off Pulse Gen at the end
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    //System.Console.WriteLine("Pulse Generator OFF");

    //Generate dummy file at the end of the script
    string[] o = {"END OF CITIROC TRIGGERS SCRIPT"};
    File.WriteAllLinesAsync(data_path+"EndOfScript_citi.txt",o); 
    return;
}


void SendGPIO(byte x_phase){
    BoardLib.SetBoardId(126);
	 BoardLib.DeviceConfigure(13, x_verbose:false);
	 System.Console.WriteLine("SendGPIO BoardConfigure done");
    Sync.Sleep(50);
	 BoardLib.SetVariable("GPIO.GPIO-MISC.PLL-PHASE", x_phase);
	 Console.WriteLine(" => GPIO Phase set to " + x_phase.ToString());
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); BoardLib.GetFirmwareVersion();
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
    BoardLib.GetFirmwareVersion();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); //Sync.Sleep(1);
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


void Calibration(int SN, int GPIO){
    //int SN=256;// to be passed as argument
    System.Console.WriteLine("Starting HV and Temperature calibration procedure ...");
    System.Console.WriteLine("Calibration FEB #"+SN.ToString()+". Using calibration paramters for GPIO #"+GPIO.ToString());
    //int GPIO=47;// SN of the GPIO used, to be passed as argument
    //string GPIO_calib_folder = "/DATA/neutrino/FCT/GPIO_cal/GPIO_SN"+GPIO.ToString()+"/";
    string GPIO_calib_folder = Environment.GetEnvironmentVariable("GPIO_CALIB_FOLDER")+"/GPIO_SN"+GPIO.ToString()+"/";
    string GPIO_calib_file = GPIO_calib_folder+"cal_GPIO_SN"+GPIO.ToString()+".csv";
    string VerifyGPIO_file = GPIO_calib_folder+"verify.csv"; // NOT USED during FCT !
    TurnOnFEB();
    string pathToCsvFiles = Environment.GetEnvironmentVariable("GENERALDATADIR")+"/FEBs/SN_"+SN.ToString()+"/"; 
    //string pathToCsvFiles = Environment.GetEnvironmentVariable("GENERALDATADIR")+"/FEBs/SN_"+SN.ToString()+"/"; 

    int samples = 20;

    System.IO.Directory.CreateDirectory(pathToCsvFiles);
    pathToCsvFiles = pathToCsvFiles + "Calibration/";
    System.IO.Directory.CreateDirectory(pathToCsvFiles);

    string GainOffsetCsv = pathToCsvFiles + "GainOffset.csv";
    var go = new FileStream(GainOffsetCsv, FileMode.Create);
    go.Dispose();

    System.Console.WriteLine("Saving calibration files in: "+pathToCsvFiles);

    // Step 1: set minimum HV and T
    SetMinMax(false);
    
    // Step 2: compute GM and raw values (MIN) 
    HV_T_8 GM_min = Compute_GM(false,GPIO_calib_file,samples,pathToCsvFiles+"RefValues_min.csv");
    // HV_T_8 GM_min_verify = Compute_GM(false,GPIO_calib_file,samples,pathToCsvFiles+"RefValues_min_verify.csv");
    HV_T_8 RawValues_min = Compute_RawValues(samples,pathToCsvFiles+"RawValues_min.csv");
    // HV_T_8 RawValues_min_verify = Compute_RawValues(samples,pathToCsvFiles+"RawValues_min_verify.csv");

    // Display results for checking 
    // // for(int i=0;i<8;i++){
    // //     System.Console.WriteLine("HV: "+GM_min.HV_8[i].ToString()+"  "+RawValues_min.HV_8[i].ToString());
    // //     System.Console.WriteLine("T:  "+GM_min.T_8[i].ToString("#.#####")+"  "+RawValues_min.T_8[i].ToString("#.#####"));
    // // }
    
    // Step 3: set maximum HV and T
    SetMinMax(true);
    
    // Step 4: compute GM and raw values (MAX) 
    HV_T_8 GM_max = Compute_GM(true,GPIO_calib_file,samples,pathToCsvFiles+"RefValues_max.csv");
    ////HV_T_8 GM_max_verify = Compute_GM(true,GPIO_calib_file,samples,pathToCsvFiles+"RefValues_max_verify.csv");
    HV_T_8 RawValues_max = Compute_RawValues(samples,pathToCsvFiles+"RawValues_max.csv");
    ////HV_T_8 RawValues_max_verify = Compute_RawValues(samples,pathToCsvFiles+"RawValues_max_verify.csv");
    
    // Display results for checking 
    // // for(int i=0;i<8;i++){
    // //     System.Console.WriteLine("HV: "+GM_max.HV_8[i].ToString()+"  "+RawValues_max.HV_8[i].ToString());
    // //     System.Console.WriteLine("T:  "+GM_max.T_8[i].ToString("#.#####")+"  "+RawValues_max.T_8[i].ToString("#.#####"));
    // // }
    
    System.Console.WriteLine("");
    System.Console.WriteLine("Computing gain and offset");
    System.Console.WriteLine("");

    double[] G_f_HV={0,0,0,0,0,0,0,0};
    double[] O_f_HV={0,0,0,0,0,0,0,0};
    double[] G_f_T={0,0,0,0,0,0,0,0};
    double[] O_f_T={0,0,0,0,0,0,0,0};
    UInt16[] G_U_HV={0,0,0,0,0,0,0,0};
    Int16[] O_I_HV ={0,0,0,0,0,0,0,0};
    UInt16[] G_U_T={0,0,0,0,0,0,0,0};
    Int16[] O_I_T ={0,0,0,0,0,0,0,0};
    


    File.AppendAllText(@GainOffsetCsv,"#ch;HV_gain[f];HV_offset[f];HV_gain[UI];HV_offset[I];T_gain[f];T_offset[f];T_gain[UI];T_offset[I]"+Environment.NewLine);

    // To convert the gain into an Unsigned Integer:
    int f_to_ui = 32768;

    for(int i=0;i<8;i++){
        G_f_HV[i] = (GM_max.HV_8[i]-GM_min.HV_8[i])/4/(RawValues_max.HV_8[i]-RawValues_min.HV_8[i]);
        O_f_HV[i] = (GM_min.HV_8[i])/4/(G_f_HV[i])-(RawValues_min.HV_8[i]);

        G_f_T[i] = (GM_max.T_8[i]-GM_min.T_8[i])/(RawValues_max.T_8[i]-RawValues_min.T_8[i]);
        O_f_T[i] = (GM_min.T_8[i])/(G_f_T[i])-(RawValues_min.T_8[i]);

        // Check that the gain and offset values are within the expected range
        if(G_f_HV[i]>1.99 || G_f_HV[i]<0){
            System.Console.WriteLine("");
            System.Console.WriteLine("FATAL: calibration problem on HV channel "+i.ToString());
            System.Console.WriteLine("Gain out of range");
            System.Console.WriteLine("Re-run calibration after the end of the test");
            System.Console.WriteLine("");
        }
        if(O_f_HV[i]<-32768 || O_f_HV[i]>32768){
            System.Console.WriteLine("");
            System.Console.WriteLine("FATAL: calibration problem on HV channel "+i.ToString());
            System.Console.WriteLine("Offset out of range");
            System.Console.WriteLine("Re-run calibration after the end of the test");
            System.Console.WriteLine("");
        }
        if(G_f_T[i]>1.99 || G_f_T[i]<0){
            System.Console.WriteLine("");
            System.Console.WriteLine("FATAL: calibration problem on T channel "+i.ToString());
            System.Console.WriteLine("Gain out of range");
            System.Console.WriteLine("Re-run calibration after the end of the test");
            System.Console.WriteLine("");
        }
        if(O_f_T[i]<-32768 || O_f_T[i]>32768){
            System.Console.WriteLine("");
            System.Console.WriteLine("FATAL: calibration problem on T channel "+i.ToString());
            System.Console.WriteLine("Offset out of range");
            System.Console.WriteLine("Re-run calibration after the end of the test");
            System.Console.WriteLine("");
        }
        // WARNINGS
        if(G_f_HV[i]>1.1 || G_f_HV[i]<0.9){
            System.Console.WriteLine("");
            System.Console.WriteLine("WARNING: Ridiculous gain  on HV channel "+i.ToString());
            System.Console.WriteLine("");
        }
        if(O_f_HV[i]<-100 || O_f_HV[i]>100){
            System.Console.WriteLine("");
            System.Console.WriteLine("WARNING: Ridiculous offset  on HV channel "+i.ToString());
            System.Console.WriteLine("");
        }
        if(G_f_T[i]>1.1 || G_f_T[i]<0.9){
            System.Console.WriteLine("");
            System.Console.WriteLine("WARNING: Ridiculous gain  on T channel "+i.ToString());
            System.Console.WriteLine("");
        }
        if(O_f_T[i]<-100 || O_f_T[i]>100){
            System.Console.WriteLine("");
            System.Console.WriteLine("WARNING: Ridiculous offset  on T channel "+i.ToString());
            System.Console.WriteLine("");
        }
        


        G_U_HV[i] = (UInt16)Math.Round(G_f_HV[i]*f_to_ui);
        O_I_HV[i] = (Int16)Math.Round(O_f_HV[i]);
        //System.Console.WriteLine("HV: "+G_U_HV[i].ToString()+" \t"+O_I_HV[i].ToString());

        G_U_T[i] = (UInt16)Math.Round(G_f_T[i]*f_to_ui);
        O_I_T[i] = (Int16)Math.Round(O_f_T[i]);
        //System.Console.WriteLine("T: "+G_U_T[i].ToString()+" \t"+O_I_T[i].ToString());
        File.AppendAllText(@GainOffsetCsv,i.ToString()+";"+G_f_HV[i].ToString()+";"+O_f_HV[i].ToString()+";"+
                                                         G_U_HV[i].ToString()+";"+O_I_HV[i].ToString()+";"+
                                                         G_f_T[i].ToString()+ ";"+O_f_T[i].ToString()+ ";"+
                                                         G_U_T[i].ToString()+ ";"+O_I_T[i].ToString()+
                                                         Environment.NewLine);

    }

    // Verify calibration
    SetMinMax(false);
    HV_T_8 GM_min_verify = Compute_GM(false,GPIO_calib_file,samples,pathToCsvFiles+"RefValues_min_verify.csv");
    // HV_T_8 GM_min_verify = Compute_GM(false,GPIO_calib_file,VerifyGPIO_file);
    // SetMinMax(false);// Be careful! if you run GM_min_verify with VerifyGPIO, you need to re-set the HV
    HV_T_8 RawValues_min_verify = Compute_RawValues(samples,pathToCsvFiles+"RawValues_min_verify.csv");
    SetMinMax(true);
    HV_T_8 GM_max_verify = Compute_GM(true,GPIO_calib_file,samples,pathToCsvFiles+"RefValues_max_verify.csv");
    HV_T_8 RawValues_max_verify = Compute_RawValues(samples,pathToCsvFiles+"RawValues_max_verify.csv");
    
    double[] converted_HV_min={0,0,0,0,0,0,0,0};
    double[] converted_HV_max={0,0,0,0,0,0,0,0};

    double[] converted_T_min={0,0,0,0,0,0,0,0};
    double[] converted_T_max={0,0,0,0,0,0,0,0};
    System.Console.WriteLine("");
    System.Console.WriteLine("");
    // System.Console.WriteLine("Compute residual differences:");
    string csvResiduals_min = pathToCsvFiles+"Residuals_min.csv";
    string csvResiduals_max = pathToCsvFiles+"Residuals_max.csv";
    var fs1 = new FileStream(csvResiduals_min, FileMode.Create);
    fs1.Dispose();
    var fs2 = new FileStream(csvResiduals_max, FileMode.Create);
    fs2.Dispose();
    File.AppendAllText(@csvResiduals_min,"#ch;HV_GM_ADC;HV_GM_V;HV_cal_ADC;HV_cal_V;HV_raw_ADC;HV_raw_V;T_GM_ADC;T_GM_V;T_cal_ADC;T_cal_V;T_raw_ADC;T_raw_V;"+Environment.NewLine);
    File.AppendAllText(@csvResiduals_max,"#ch;HV_GM_ADC;HV_GM_V;HV_cal_ADC;HV_cal_V;HV_raw_ADC;HV_raw_V;T_GM_ADC;T_GM_V;T_cal_ADC;T_cal_V;T_raw_ADC;T_raw_V;"+Environment.NewLine);

    double hv1,hv2,hv3,hv4,hv5,hv6;
    double t1,t2,t3,t4,t5,t6;

    for(int i=0;i<8;i++){

        hv1 = Math.Round(GM_min_verify.HV_8[i]/4); // HV ON THE GPIO IN ADC DIVIDED BY 4
        hv2 = Convert_HV_GPIO( GM_min_verify.HV_8[i] ); // HV ON THE GPIO CONVERTED IN VOLTS
        hv3 = Math.Round( ( (RawValues_min_verify.HV_8[i]) + O_I_HV[i] )*(double)G_U_HV[i]/f_to_ui ); // CALIBRATED HV ON THE FEB, IN ADC
        hv4 = Convert_HV_FEB(Math.Round( ( (RawValues_min_verify.HV_8[i]) + O_I_HV[i] )*(double)G_U_HV[i]/f_to_ui ) ); // CALIBRATED HV ON THE FEB CONVERTED IN VOLTS
        hv5 = RawValues_min_verify.HV_8[i]; //  UNCALIBRATED HV ON THE FEB, IN ADC
        hv6 = Convert_HV_FEB( ( (RawValues_min_verify.HV_8[i]) ) ); // UNCALIBRATED HV ON THE FEB, CONVERTEED IN V
        // repeat for T, all the values that you obtain are in Volts, not degrees
        t1 = Math.Round(GM_min_verify.T_8[i]); // no need to divide by 4 here, the ADC for temperature sensing in GPIO and FEB have the same range
        t2 = Convert_T_GPIO( GM_min_verify.T_8[i] );
        t3 = Math.Round( ( RawValues_min_verify.T_8[i] + O_I_T[i] )*(double)G_U_T[i]/f_to_ui );
        t4 = Convert_T_FEB(Math.Round(  ( (RawValues_min_verify.T_8[i]) + O_I_T[i] )*(double)G_U_T[i]/f_to_ui ) );
        t5 = RawValues_min_verify.T_8[i];
        t6 = Convert_T_FEB( RawValues_min_verify.T_8[i] );
        

        File.AppendAllText(@csvResiduals_min,i.ToString()+";"+hv1.ToString()+";"+hv2.ToString()+";"+hv3.ToString()+";"
                                                                +hv4.ToString()+";"+hv5.ToString()+";"+hv6.ToString()+";"
                                                                +t1.ToString()+";"+t2.ToString()+";"+t3.ToString()+";"
                                                                +t4.ToString()+";"+t5.ToString()+";"+t6.ToString()+";"
                                                                +Environment.NewLine);
        hv1 = Math.Round(GM_max_verify.HV_8[i]/4); // HV ON THE GPIO IN ADC DIVIDED BY 4
        hv2 = Convert_HV_GPIO( GM_max_verify.HV_8[i] ); // HV ON THE GPIO CONVERTED IN VOLTS
        hv3 = Math.Round( ( (RawValues_max_verify.HV_8[i]) + O_I_HV[i] )*(double)G_U_HV[i]/f_to_ui ) ; // CALIBRATED HV ON THE FEB, IN ADC
        hv4 = Convert_HV_FEB( Math.Round( ( (RawValues_max_verify.HV_8[i]) + O_I_HV[i] )*(double)G_U_HV[i]/f_to_ui ) ); // CALIBRATED HV ON THE FEB CONVERTED IN VOLTS
        hv5 = RawValues_max_verify.HV_8[i]; //  UNCALIBRATED HV ON THE FEB, IN ADC
        hv6 = Convert_HV_FEB( ( (RawValues_max_verify.HV_8[i]) ) ); // UNCALIBRATED HV ON THE FEB, CONVERTEED IN V
        // repeat for T, all the values that you obtain are in Volts, not degrees
        t1 = Math.Round(GM_max_verify.T_8[i]); // no need to divide by 4 here, the ADC for temperature sensing in GPIO and FEB have the same range
        t2 = Convert_T_GPIO( GM_max_verify.T_8[i] );
        t3 = Math.Round( ( RawValues_max_verify.T_8[i] + O_I_T[i] )*(double)G_U_T[i]/f_to_ui );
        t4 = Convert_T_FEB( Math.Round( ( (RawValues_max_verify.T_8[i]) + O_I_T[i] )*(double)G_U_T[i]/f_to_ui ) );
        t5 = RawValues_max_verify.T_8[i];
        t6 = Convert_T_FEB( RawValues_max_verify.T_8[i] );
        
        File.AppendAllText(@csvResiduals_max,i.ToString()+";"+hv1.ToString()+";"+hv2.ToString()+";"+hv3.ToString()+";"
                                                                +hv4.ToString()+";"+hv5.ToString()+";"+hv6.ToString()+";"
                                                                +t1.ToString()+";"+t2.ToString()+";"+t3.ToString()+";"
                                                                +t4.ToString()+";"+t5.ToString()+";"+t6.ToString()+";"
                                                                +Environment.NewLine);

    }

    //return;
        System.Console.Write("Saving calibration values in EEPROM registers...");

    // Save calibration values in the EEPROM registers
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Write",true);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Read",false);

    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",0);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",0);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",SN&0xFF);
    BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",1);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",(SN>>8)&0xFF);
    BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
    int address=0;
    for(int i=0;i<8;i++){
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",G_U_HV[i]&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",(G_U_HV[i]>>8)&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",O_I_HV[i]&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",(O_I_HV[i]>>8)&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
    }
    for(int i=0;i<8;i++){
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",G_U_T[i]&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",(G_U_T[i]>>8)&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",O_I_T[i]&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",(O_I_T[i]>>8)&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
    }
    System.Console.Write("  Done!\n");

    //Verify that the values written in the EEPROM are correct (after a power cycle) 
    TurnOffFEB();
    TurnOnFEB();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Write",false);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Read",true);

    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",0);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",0);
    BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
    BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
    if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==(SN&0xFF))){
        System.Console.WriteLine("Error in EEPROM reading! (SN) page 0, address 0");
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + (SN&0xFF).ToString());
    }    
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",1);
    BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
    BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
    if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==(SN>>8&0xFF))){
        System.Console.WriteLine("Error in EEPROM reading! (SN) page 0, address 1");
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + (SN>>8&0xFF).ToString());
    }
    address=0;
    for(int i=0;i<8;i++){
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==(G_U_HV[i]&0xFF))){
            System.Console.WriteLine("Error in EEPROM reading! page 1, address "+address.ToString());
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + (G_U_HV[i]&0xFF).ToString());
        }
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==((G_U_HV[i]>>8)&0xFF))){
            System.Console.WriteLine("Error in EEPROM reading! page 1, address "+address.ToString());
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + ((G_U_HV[i]>>8)&0xFF).ToString());
        }
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==(O_I_HV[i]&0xFF))){
            System.Console.WriteLine("Error in EEPROM reading! page 1, address "+address.ToString());
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + (O_I_HV[i]&0xFF).ToString());
        }
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==((O_I_HV[i]>>8)&0xFF))){
            System.Console.WriteLine("Error in EEPROM reading! page 1, address "+address.ToString());
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + ((O_I_HV[i]>>8)&0xFF).ToString());
        }
        address+=1;
    }
    for(int i=0;i<8;i++){
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==(G_U_T[i]&0xFF))){
            System.Console.WriteLine("Error in EEPROM reading! page 1, address "+address.ToString());
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + (G_U_T[i]&0xFF).ToString());
        }
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==((G_U_T[i]>>8)&0xFF))){
            System.Console.WriteLine("Error in EEPROM reading! page 1, address "+address.ToString());
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + ((G_U_T[i]>>8)&0xFF).ToString());
        }
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==(O_I_T[i]&0xFF))){
            System.Console.WriteLine("Error in EEPROM reading! page 1, address "+address.ToString());
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + (O_I_T[i]&0xFF).ToString());
        }
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",1);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.READ");
        if(!(BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value")==((O_I_T[i]>>8)&0xFF))){
            System.Console.WriteLine("Error in EEPROM reading! page 1, address "+address.ToString());
            System.Console.WriteLine("Read: " + BoardLib.GetByteVariable("FPGA-MISC.NIOS.READ.Value").ToString() + " Written: " + ((O_I_T[i]>>8)&0xFF).ToString());
        }
        address+=1;
    }

    // Finally, enable EEPROM WRITE PROTECT (HW action)

    
    return;
}

void SetMinMax(bool MAX){
    int HV_set;
    byte TSW_set;
    double HV_factor = 65535/102.46;
    if(MAX){
        TSW_set=255;//FF
        HV_set=(int) HV_factor*55;
    }else{
        TSW_set=0;
        HV_set=(int) HV_factor*5;
    }
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set);
    }
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); Sync.Sleep(1);
    // System.Console.WriteLine("sending configuration with HV");
    BoardLib.DeviceConfigure(11, x_verbose:false);
    Sync.Sleep(100);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);
    // System.Console.WriteLine("setting direct params with HV");
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    // Set T
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-MISC.TSEN-SW",TSW_set);
    // System.Console.WriteLine("updating parameters with TSEN-SW");
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); BoardLib.GetFirmwareVersion();
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0);
}

HV_T_8 Compute_RawValues(int samples=50,string csvFile="none"){
       System.Console.WriteLine("Measuring raw values");

    //if the string for csvFile is not "none", save measurements to a csv file
    if(csvFile!="none"){
        var fs = new FileStream(csvFile, FileMode.Create);
        fs.Dispose();        
        //4File.AppendAllText(@csvFile,"# Raw Values MPPC temperature and HV (FEB measurement)"+Environment.NewLine);
        File.AppendAllText(@csvFile,"#ch;HV[ADC];T[ADC];HV[V];T[V]"+Environment.NewLine);
    }
    
    // Measure raw values (y axis)

    UInt16 hv=0, t=0;
    double hv_d=0,t_d=0;
    double[] Raw_HV={0,0,0,0,0,0,0,0};
    double[] Raw_T={0,0,0,0,0,0,0,0};
    UInt32[] Raw_HV_b={0,0,0,0,0,0,0,0};
    UInt32[] Raw_T_b={0,0,0,0,0,0,0,0};
    //Measure HV (from FEB) and T
    BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12, x_verbose:false);
    for(int j=0;j<samples;j++){
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
        for(int i = 0;i<8;i++){
            hv = BoardLib.GetUInt16Variable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+i.ToString()+".MPPC-HV");
            t = ( BoardLib.GetUInt16Variable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+i.ToString()+".MPPC-Temp") );
            Raw_HV[i] = Raw_HV[i] + hv;
            Raw_T[i] = Raw_T[i] + t;
            if(csvFile!="none"){
                File.AppendAllText(@csvFile,i.ToString()+"; "+hv.ToString()+"; "+t.ToString()+"; "+Convert_HV_FEB(hv).ToString()+"; "+Convert_T_FEB(t).ToString()+Environment.NewLine);
            }
        }   
    }
    for(int i = 0;i<8;i++){
        Raw_HV[i] = Raw_HV[i]/samples;
        Raw_T[i] = Raw_T[i]/samples;
    }

    HV_T_8 result;
    result.HV_8=Raw_HV;
    result.T_8=Raw_T;

    System.Console.WriteLine("done");

    return result;

}

HV_T_8 Compute_GM(bool MAX, string GPIO_calib_file, int samples=50, string csvFile="none", string csv_GPIOresult_File="none"){
       //Compute/measure GM_min for HV and T (x axis)
       System.Console.WriteLine("obtaining GM");

    string csvFile_T = csvFile.Remove(csvFile.Length-4) + "_T.csv";
    csvFile = csvFile.Remove(csvFile.Length-4) + "_HV.csv";
    if(csvFile!="none"){
        var fs = new FileStream(csvFile, FileMode.Create);
        fs.Dispose();        
        //File.AppendAllText(@csvFile,"# Reference values for MPPC HV (GPIO measurement)"+Environment.NewLine);
        File.AppendAllText(@csvFile,"#ch;HV[ADC];HV[V]"+Environment.NewLine);
        var fs_T = new FileStream(csvFile_T, FileMode.Create);
        fs_T.Dispose();
        //File.AppendAllText(@csvFile_T,"# Reference values for MPPC T (GPIO measurement)"+Environment.NewLine);
        File.AppendAllText(@csvFile_T,"#ch;T[ADC];T[V]"+Environment.NewLine);
    }

    double f1 = 3000;
    double f2 = 3.3; 
    double[] R = {0,0,0,0,0,0,0,0};
    double T_sense, T_ADC;

    double[] GPIOgain = new double[8];
    double[] GPIOoffset = new double[8];
    double[] GPIORmax = new double[8];
    double[] GPIORmin = new double[8];

    //Get GPIO calibration info from file
    var reader = new System.IO.StreamReader(@GPIO_calib_file);
    int line_n=0;
    // System.Console.WriteLine("read csv file");
    while (!reader.EndOfStream){
            var line = reader.ReadLine();
            var values = line.Split(';');
            // System.Console.WriteLine(reader.EndOfStream);
            // System.Console.WriteLine(i);
            // System.Console.WriteLine(values[0]);

            if(line_n>0){
            // System.Console.WriteLine("filling.."+i);
                GPIOgain[line_n-1]=Convert.ToDouble(values[0]);
                GPIOoffset[line_n-1]=Convert.ToDouble(values[1]);
                GPIORmax[line_n-1]=Convert.ToDouble(values[3]);
                GPIORmin[line_n-1]=Convert.ToDouble(values[2]);
            }
            line_n++;
    }


    double[] GM_HV={0,0,0,0,0,0,0,0};
    UInt32[] GM_HV_int={0,0,0,0,0,0,0,0};
    double[] GM_HV_cal={0,0,0,0,0,0,0,0};
    UInt32 hv=0,t=0;
    double hv_d=0,t_d=0,hv_cal=0;

    double GM_T;
    double[] GM_T_vec={0,0,0,0,0,0,0,0};
    double[] GM_T_ADC={0,0,0,0,0,0,0,0};


    if(MAX){
        R=GPIORmax;
    }else{
        R=GPIORmin;
    }

    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",false);


    for(int j=0;j<samples;j++){
        BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
        BoardLib.UpdateUserParameters("GPIO.GPIO-ADC-DPRAM");
        for(int i = 0;i<8;i++){
            hv = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.HV-Channels.CH"+i.ToString()+".HV") ;
            hv_cal=(hv+GPIOoffset[i])*GPIOgain[i];
            GM_HV_cal[i] = GM_HV_cal[i] + hv_cal;
            if(csvFile!="none"){
                File.AppendAllText(@csvFile,i.ToString()+"; "+(hv_cal/4).ToString()+"; "+Convert_HV_GPIO(hv_cal).ToString()+Environment.NewLine);
            }
        }
    }

    for(int i = 0;i<8;i++){
        GM_HV_cal[i] = (double)GM_HV_cal[i]/samples;
    }

    for(int i=0;i<8;i++){
        // T
        T_sense = f1*f2/(f1+R[i]);
        T_ADC = T_sense*65535/3;
        GM_T_ADC[i] = T_ADC;
        GM_T_vec[i] = Convert_T_GPIO(T_ADC);
        if(csvFile!="none"){
            File.AppendAllText(@csvFile_T,i.ToString()+"; "+(T_ADC).ToString()+"; "+Convert_T_GPIO(T_ADC).ToString()+Environment.NewLine);
        }
    }

    // Verify GPIO calibration (optional, ideally run just once)
    UInt32 ver_int;
    double ver_V,ver_V_cal;
    if(csv_GPIOresult_File!="none"){
        var fs = new FileStream(@csv_GPIOresult_File, FileMode.Create);
        fs.Dispose();      
        File.AppendAllText(@csv_GPIOresult_File,"#ch;nom;raw;cal"+Environment.NewLine);
        System.Console.WriteLine("Verifying GPIO calibration");
        for(int i_hv=10;i_hv>0;i_hv--){
            double HV_factor = 65535/102.46;
            double HV_set_V = (i_hv+1)*5;
            int HV_set=(int)Math.Round(HV_factor*(HV_set_V));
            for(int i = 0;i<8;i++){
                BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set);
            }
            BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); Sync.Sleep(1);
            // System.Console.WriteLine("sending configuration with HV");
            BoardLib.DeviceConfigure(11, x_verbose:false);
            Sync.Sleep(100);
            BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
            BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);
            // System.Console.WriteLine("setting direct params with HV");
            BoardLib.SetDirectParameters();
            Sync.Sleep(1000);
            BoardLib.SetBoardId(126);
            BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
            BoardLib.UpdateUserParameters("GPIO.GPIO-ADC-DPRAM");
            for(int i = 0;i<8;i++){
                ver_int = BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.HV-Channels.CH"+i.ToString()+".HV") ;
                ver_V = Convert_HV_GPIO( ver_int );
                ver_V_cal = Convert_HV_GPIO( (ver_int+GPIOoffset[i])*GPIOgain[i]);
                File.AppendAllText(@csv_GPIOresult_File,i+";"+HV_set_V+";"+ver_V+";"+ver_V_cal+Environment.NewLine);
            }
        }
        for(int i = 0;i<8;i++){
            BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",0);
        }
        BoardLib.GetFirmwareVersion();BoardLib.SetBoardId(0); Sync.Sleep(1);
        // System.Console.WriteLine("sending configuration with HV");
        BoardLib.DeviceConfigure(11, x_verbose:false);
        Sync.Sleep(100);
        BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
        BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);
        // System.Console.WriteLine("setting direct params with HV");
        BoardLib.SetDirectParameters();
        Sync.Sleep(1000);
        System.Console.WriteLine("done");

    }

    HV_T_8 result;
    result.HV_8=GM_HV_cal;
    result.T_8 = GM_T_ADC;
       System.Console.WriteLine("done");

    return result;

}


double Convert_HV_FEB(double ADC){
    return ADC*1.602796e-3;
}
double Convert_T_FEB(double ADC){
    return 3.0/65535.0*ADC;
}
double Convert_T_GPIO(double ADC){
    return 3.0/65535.0*ADC;
}
double Convert_HV_GPIO(double ADC){
    return ADC/4*1.602796e-3; 
}




struct HV_T_8{
    public double[] HV_8;
    public double[] T_8;
};

struct HV_T_8_int{
    public UInt32[] HV_8;
    public UInt32[] T_8;
};


    