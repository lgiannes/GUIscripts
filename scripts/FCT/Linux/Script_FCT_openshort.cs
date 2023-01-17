/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// INIT SETTINGS (should put this in a config file or similar)

//Name of default configuration
string config_folder = "/home/neutrino/FCT/code/config/";
                        //"/home/lorenzo/T2K-uniGe/FEB_GPIO/FEB-GPIO_firmware/UT_60charge/etc/config/linearity_one_channel.xml";
//Set the path to which data should be saved
string data_path   =    "/home/neutrino/FCT/data_local/";
                        //"/DATA/dataFCT/";
                        //"/home/lorenzo/T2K-uniGe/FEB_GPIO/data/linearity_tests_citiroc/multichannelHGLG/";

int LG = 56;
int HG = 12;
double amplitude = 0.03;//V

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



void ScriptMainArgs(int SN,int bl1, int bl2){
    
    string config_path = config_folder+"config_FCT2_newGUI_V2.xml";

    // // Delete "EndOfScript.txt" dummy file if it exists in the data directory
    // NO NEED TO DO. ALREADY DONE IN THE BASH SCRIPT 
    // if (File.Exists(Path.Combine(data_path, "EndOfScript.txt"))){    
    //     // If file found, delete it    
    //     File.Delete(Path.Combine(data_path, "EndOfScript.txt"));    
    // }    

    // CREATE THE DATA DIRECTORY BASED ON THE SERIAL NUMBER
    data_path = data_path + "SN_" + SN.ToString() + "/";
    var DATAfolder = System.IO.Directory.CreateDirectory(data_path);

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
    BoardLib.SetVariable("Board.DirectParam.AdcFsmConfLock", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmReset", true);
    BoardLib.SetVariable("Board.DirectParam.IGEn", false);
    // Send to board
    BoardLib.SetBoardId(0);
    BoardLib.SetDirectParameters();

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
    SendGPIO();
    Sync.Sleep(200);
        
    // Enable preamp and DAQ on all channels
    ActivateAllCh(LG,HG);
    // YOU MIGHT WANT TO CHANGE IT TO HAVE THE ADC STARTING AT GATE_CLOSE SIGNAL
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


    RunAcquisition();
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
    SendGPIO();
    BoardLib.SetVariable("Board.DirectParam.ExtClkEn", true);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", false);
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetVariable("Board.DirectParam.GtEn", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmConfLock", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmReset", true);
    BoardLib.SetVariable("Board.DirectParam.IGEn", false);
    BoardLib.SetBoardId(0);
    BoardLib.SetDirectParameters();
    Sync.Sleep(200);
    ActivateAllCh(LG,HG);
    Sync.Sleep(200);

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
    SendGPIO();
    BoardLib.SetVariable("Board.DirectParam.ExtClkEn", true);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", false);
    BoardLib.SetVariable("Board.DirectParam.AveEn", true);
    BoardLib.SetVariable("Board.DirectParam.GtEn", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmConfLock", true);
    BoardLib.SetVariable("Board.DirectParam.AdcFsmReset", true);
    BoardLib.SetVariable("Board.DirectParam.IGEn", false);
    BoardLib.SetBoardId(0);
    BoardLib.SetDirectParameters();
    Sync.Sleep(200);
    ActivateAllCh(LG,HG);
    Sync.Sleep(200);

    RunBaselineAcq(bl2);

    Sync.Sleep(500);
    //TurnOffFEB();

    // Turn off Pulse Gen at the end
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
    System.Console.WriteLine("Pulse Generator OFF");

    //Generate dummy file at the end of the script
    string[] o = {"END OF SCRIPT"};
    File.WriteAllLinesAsync(data_path+"EndOfScript.txt",o); 
    return;
}




void RunAcquisition(){
    Sync.Sleep(500);                                                                    

    int baseline = 32786;
    var BashOutput = "";
    
    string file_name = "FCT_os_LG"+LG.ToString()+"HG"+HG.ToString()+"amp"+((int)1000*amplitude).ToString()+"mV_"+"bl"+baseline.ToString();
    

    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.SetBoardId(0); 
    BoardLib.DeviceConfigure(8);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetDirectParameters();

    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.SetBoardId(0); 
    Sync.Sleep(20);                                                                    
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }

    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }

    Sync.Sleep(20);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(300);                                                                   

    for(int channel=0;channel<256;channel++){
    //for(int channel=179;channel<181;channel++){
        
        SetKaladin(channel);
                                                                        //System.Console.WriteLine("Kaladin set");       
        Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");     
        if( !BoardLib.GetBoolVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen") ){
            System.Console.WriteLine("ERROR: GATE NOT OPEN");
            break;
        }   
        // Sync.Sleep(50);
        // BashOutput = ExecuteBashCommand("echo \"OUTPUT ON\" | cat > /dev/ttyACM0");
        // BashOutput = ExecuteBashCommand("echo \"OUTPUT ON\" | cat > /dev/ttyACM0");
        // //BashOutput = ExecuteBashCommand("bash fgON.sh");
        Sync.Sleep(100);
        // BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
        // BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
        // //BashOutput = ExecuteBashCommand("bash fgOFF.sh");
        // Sync.Sleep(50);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");  
        if( BoardLib.GetBoolVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen") ){
            System.Console.WriteLine("ERROR: GATE NOT CLOSED");
            break;
        }        
        Sync.Sleep(10);

                                                                        //System.Console.WriteLine("channel "+channel.ToString()+" done");
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    Sync.Sleep(10);
    BoardLib.SetBoardId(126); 
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.SetBoardId(0); 
    BoardLib.StopAcquisition();
    BoardLib.WaitForEndOfTransfer(true);
    Sync.Sleep(1100);
    Sync.SleepUntil( ()=>!BoardLib.IsTransferingData );
    
    System.Console.WriteLine("END OF ACQUISITION");




}


void RunBaselineAcq(int baseline){
    
    string file_name = "FCT_BLTEST_LG"+LG.ToString()+"HG"+HG.ToString()+"amp"+((int)1000*amplitude).ToString()+"mV_"+"baseline"+baseline.ToString();
    var BashOutput = "";
    
    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.SetBoardId(0); 
    BoardLib.DeviceConfigure(8);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetDirectParameters();
    Sync.Sleep(50);                                                                    

    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.SetBoardId(0); 
    Sync.Sleep(20);                                                                    
    if(BoardLib.StartAcquisition(data_path + file_name,true)){ 
        System.Console.WriteLine("Asynchronous acquisition started");
    }

    Sync.Sleep(300);                                                                   
    if(!BoardLib.IsTransferingData){
        System.Console.WriteLine("WARNING: DAQ stopped right after starting. RESTART daq");
        BoardLib.StartAcquisition(data_path + file_name,true);
    }

    Sync.Sleep(20);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(100);                                                                   

    for(int i=0;i<8;i++){
        int channel = 0;
        // WARNING: if you change the definition of channel here, you need to change also the ROOT analysis:
        // function at "Gate_to_Kal_Ch" defined at line 369 of "FCTbaseline.cpp". Then, recompile ROOT analysis
        channel = i*32;// + 16*(i/4) + (int)(Math.Pow(2,i%4))-1;
        SetKaladin(channel);
        System.Console.WriteLine("asic " + channel/32 + " channel " + (channel%32).ToString());
                                                                        //System.Console.WriteLine("Kaladin set");       
        Sync.Sleep(50);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("opening gate");       
        if( !BoardLib.GetBoolVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen") ){
            System.Console.WriteLine("ERROR: GATE NOT OPEN");
            break;
        }   // Sync.Sleep(10);
        //BashOutput = ExecuteBashCommand("echo \"OUTPUT ON\" | cat > /dev/ttyACM0");
        Sync.Sleep(100);
        //BashOutput = ExecuteBashCommand("echo \"OUTPUT OFF\" | cat > /dev/ttyACM0");
        // Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.SetBoardId(126); 
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
                        System.Console.WriteLine("closing gate");       
        if( BoardLib.GetBoolVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen") ){
            System.Console.WriteLine("ERROR: GATE NOT CLOSED");
            break;
        }        Sync.Sleep(10);

                                                                        //System.Console.WriteLine("channel "+channel.ToString()+" done");
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    Sync.Sleep(10);                                                                   
    BoardLib.SetBoardId(126); 
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    BoardLib.SetBoardId(0); 
    BoardLib.StopAcquisition();
    BoardLib.WaitForEndOfTransfer(true);
    Sync.Sleep(1100);
    Sync.SleepUntil( ()=>!BoardLib.IsTransferingData );
    
    System.Console.WriteLine("END OF ACQUISITION");

}




bool SyncTest(){
    bool success = true;
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(50);
    BoardLib.SetBoardId(0);
    BoardLib.ReadStatus();
    bool GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    if(GateEn){
        success = false;
        return success;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
    BoardLib.SetBoardId(126);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(50);
    BoardLib.SetBoardId(0);
    BoardLib.ReadStatus();
    GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    if(!GateEn){
        success = false;
        return success;
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(50);
    BoardLib.SetBoardId(0);
    return success;
}



void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); 
    Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}
void TurnOffFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetBoardId(126); 
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
    
    BoardLib.SetBoardId(126); 
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(10);

}




void ActivateAllCh(int LG_gain,int HG_gain){
    BoardLib.SetBoardId(0);
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
    BoardLib.SetBoardId(126);
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
}

void SendFEB(byte FEBID=0){
    SelectFEBdevices(FEBID);
    BoardLib.SetBoardId(0);
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
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