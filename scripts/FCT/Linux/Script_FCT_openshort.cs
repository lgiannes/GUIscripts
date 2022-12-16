/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// INIT SETTINGS

//Load a given configuration
string config_path = "/home/neutrino/FCT/code/config/config_FCT2_newGUI.xml";
                        //"/home/lorenzo/T2K-uniGe/FEB_GPIO/FEB-GPIO_firmware/UT_60charge/etc/config/linearity_one_channel.xml";
//Set the path to which data should be saved
string data_path   = "/DATA/dataFCT/";
                        //"/home/lorenzo/T2K-uniGe/FEB_GPIO/data/linearity_tests_citiroc/multichannelHGLG/";

int LG = 56;
int HG = 12;
double amplitude = 0.03;//V

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



void ScriptMain(){
    TurnOnFEB();
    System.Console.WriteLine("FEB is on");
    
    //Send config (need to make two configs?)
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

    bool Sync = false;
    Sync = SyncTest();
    if(!Sync){
        System.Console.WriteLine("Sync not working");
        return;
    }else{
         System.Console.WriteLine("Sync test Successful!");
    }
    
    RunAcquisition();
}


void RunAcquisition(){

    int baseline = 32786;
    
    string file_name = "FCT_os_LG"+LG.ToString()+"HG"+HG.ToString()+"amp"+((int)1000*amplitude).ToString()+"mV_"+"bl"+baseline.ToString();
    

    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    
    // Enable preamp and DAQ on all channels
    ActivateAllCh(LG,HG);
    // YOU MIGHT WANT TO CHANGE IT TO HAVE THE ADC STARTING AT GATE_CLOSE SIGNAL
                                                                        System.Console.WriteLine("FEB is configured");

    // Set up communication with Pulse gen
    var BashOutput = ExecuteBashCommand("fg_setup.sh");
                                                                        System.Console.WriteLine("Pulse gen is configured");
    
    BoardLib.SetBoardId(0); 
    Sync.Sleep(500);                                                                    
    BoardLib.StartAcquisition(data_path + file_name,true); 
                                                                        System.Console.WriteLine("Asynchronous acquisition started");

    Sync.Sleep(500);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(500);                                                                   
    

    for(int channel=0;channel<256;channel++){
    //for(int channel=179;channel<181;channel++){
    
        SetKaladin(channel);
                                                                        //System.Console.WriteLine("Kaladin set");       
        Sync.Sleep(10);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(10);
        BashOutput = ExecuteBashCommand("bash fg.sh 1");
        Sync.Sleep(500);
        BashOutput = ExecuteBashCommand("bash fg.sh 0");
        Sync.Sleep(100);
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(10);

                                                                        //System.Console.WriteLine("channel "+channel.ToString()+" done");
    }
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",false);
    Sync.Sleep(500);                                                                   
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(500);
    BoardLib.SetBoardId(0); 
    Sync.Sleep(500);
    BoardLib.StopAcquisition();
    Sync.SleepUntil( ()=>!BoardLib.IsTransferingData );
                                                                        System.Console.WriteLine("END OF ACQUISITION");

}


bool SyncTest(){
    bool success = true;
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",false);
    BoardLib.SetBoardId(126);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
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
    BoardLib.SetBoardId(0);
    BoardLib.ReadStatus();
    GateEn = BoardLib.GetBoolVariable("Board.StatusParam.GateEn");
    if(!GateEn){
        success = false;
        return success;
    }

    return success;
}



void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
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