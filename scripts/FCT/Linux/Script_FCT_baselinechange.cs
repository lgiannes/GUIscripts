/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// INIT SETTINGS

//Load a given configuration
string config_path = "C:/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/GPIO_FEB/config/config_FCT2.xml";
                        //"/home/lorenzo/T2K-uniGe/FEB_GPIO/FEB-GPIO_firmware/UT_60charge/etc/config/linearity_one_channel.xml";
//Set the path to which data should be saved
string data_path   = "C:/Users/neutrino/Desktop/FPGA/working_folder/UT90_analog_time/dataFCT/";
                        //"/home/lorenzo/T2K-uniGe/FEB_GPIO/data/linearity_tests_citiroc/multichannelHGLG/";

int LG = 56;
int HG = 12;
double amplitude = 0.03;//V

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



void ScriptMain(){
    RunBaselineAcq(32786);
    RunBaselineAcq(50786);

}


void RunBaselineAcq(int baseline){
    
    string file_name = "FCT_BLTEST_LG"+LG.ToString()+"HG"+HG.ToString()+"amp"+((int)1000*amplitude).ToString()+"mV_"+"baseline"+baseline.ToString();
    
    for(int asic = 0;asic<8;asic++){
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.HG",baseline);
        BoardLib.SetVariable("FPGA-DAQ.FPGA-DAQ-Channels.ASIC"+asic.ToString()+".Thresholds.BaselineDAC.LG",baseline);
    }
    BoardLib.SetBoardId(0); 
    BoardLib.DeviceConfigure(8);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetDirectParameters();

   
    BoardLib.SetBoardId(0); 
    Sync.Sleep(500);                                                                    
    BoardLib.StartAcquisition(data_path + file_name,true); 
                                                                        System.Console.WriteLine("Asynchronous acquisition started");

    Sync.Sleep(500);
    BoardLib.SetBoardId(126); 
    BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GTSEn",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
    Sync.Sleep(500);                                                                   
    
    l_serialPort.WriteLine("OUTPUT ON");

    for(int i=0;i<8;i++){
        int channel = 0;
    //for(int channel=179;channel<181;channel++){
        channel = i*32 + 16*(i/4) + (int)(Math.Pow(2,i%4))-1;
        SetKaladin(channel);
        System.Console.WriteLine("asic " + i + " channel " + (16*(i/4) + (int)(Math.Pow(2,i%4))-1).ToString());
                                                                        //System.Console.WriteLine("Kaladin set");       
         Sync.Sleep(10);                                                                   
        BoardLib.SetVariable("GPIO.GPIO-DIRECT-PARAMS.GateOpen",true);
        BoardLib.UpdateUserParameters("GPIO.GPIO-DIRECT-PARAMS");
        Sync.Sleep(10);
        l_serialPort.WriteLine("OUTPUT ON");
        Sync.Sleep(500);
        l_serialPort.WriteLine("OUTPUT OFF");
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

    l_serialPort.Close();
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


System.IO.Ports.SerialPort SetUpPulseGenerator(double amplitude = 0.18){
    System.IO.Ports.SerialPort l_serialPort = new System.IO.Ports.SerialPort();
    // port to open: check the name of the USB port in the device manager
    l_serialPort.PortName = "COM18";
 
    // Set default pulse parameters (pulse shape, amplitude unit, rising and falling edges, delay, low level, period)
    l_serialPort.Open();
    l_serialPort.WriteLine("WAVE PULSE");
            Sync.Sleep(500);
    l_serialPort.WriteLine("AMPUNIT Vpp");
            Sync.Sleep(500);
    l_serialPort.WriteLine("PULSRISE 10E-9"); // 10 ns rising edge
            Sync.Sleep(500);
    l_serialPort.WriteLine("PULSFALL 99E-9"); // 99 ns falling edge
            Sync.Sleep(500);
    l_serialPort.WriteLine("PULSWID 1E-6");
            Sync.Sleep(500);        
    l_serialPort.WriteLine("PULSPER 5E-3");
            Sync.Sleep(500);
    // l_serialPort.WriteLine("PULSDLY 40E-6");
    //         Sync.Sleep(500);        
    // l_serialPort.WriteLine("LOLVL 0");
    //         Sync.Sleep(500);
    // l_serialPort.WriteLine("HILVL " + amplitude.ToString()); 
    //         Sync.Sleep(500);                                                             
    l_serialPort.WriteLine("AMPL "+amplitude.ToString());
            Sync.Sleep(500);
    l_serialPort.WriteLine("DCOFFS 0");
            Sync.Sleep(500);

    return l_serialPort;
    
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
    for(int i=0;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
    for(int i=14;i<15;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
}

void SelectFEBdevices(byte FEBID=0){
    // Speak with FEB
    BoardLib.SetBoardId(FEBID);
    for(int i=0;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
    for(int i=14;i<15;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
}

void SendGPIO(){
    SelectGPIOdevices();
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
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