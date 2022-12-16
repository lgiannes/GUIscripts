vodi ScriptMain(){
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
    BoardLib.SetDirectParameters();

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // INIT SETTINGS

    //Load a given configuration
    string config_path = "C:/Users/neutrino/Desktop/FPGA/working_folder/UT60_charge/etc/config/";
                         //"/home/lorenzo/T2K-uniGe/FEB_GPIO/FEB-GPIO_firmware/UT_60charge/etc/config/linearity_one_channel.xml";
    //Set the path to which data should be saved
    string data_path   = "C:/Users/neutrino/Desktop/FPGA/working_folder/UT60_charge/data/";
                         //"/home/lorenzo/T2K-uniGe/FEB_GPIO/data/linearity_tests_citiroc/multichannelHGLG/";

    int LG = 56;
    int HG = 12;
    double amplitude = 0.18;//V

    string file_name = "FCT_os_LG"+LG.ToString()+"HG"+HG.ToString()+"amp"+amplitude.ToString()+"_";
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Send config (need to make two configs?)
    SendFEB();
    SendGPIO();

    // Enable preamp and DAQ on all channels
    ActivateAllCh(LG,HG);
    // YOU MIGHT WANT TO CHANGE IT TO HAVE THE ADC STARTING AT GATE_CLOSE SIGNAL

    // Set up communication with Pulse gen
    SetUpPulseGenerator(amplitude);

    BoardLib.StartAsyncAcquisition(data_path + file_name); 
    for(int channel=0;channel<256;channel++){
        SetKaladin(channel);
        Sync.Sleep(50);
        OpenGate();
        l_serialPort.WriteLine("OUTPUT ON");
        Sync.Sleep(1);
        l_serialPort.WriteLine("OUTPUT OFF");
        CloseGate();
        Sync.Sleep(1);
    }
    BoardLib.StopAcquisition();


}

void SetKaladin(int channel){
    int asic = channel/32;          // Asic number
    int loc_ch = channel%32;        // Channel within the ASIC
    int loc_MUX = loc_ch/8;         // Which MUX within the ASIC (4 MUXs per ASIC) 
    int Kal_MUX_output = loc_MUX%8; // Which channel output within the MUX (8 output, selected with 3bits number)
    int MUX = asic*4 + loc_MUX;     // Global MUX (32 in total, 4 per ASIC)
    
    // Two ways: 
        // PASS BY BINARY:
            uint Kal_En_bin = 1;
            for(int i=0;i<MUX;++){
                Kal_En_bin = Kal_En_bin << 1;
            }
            // Kal_En_hex = Convert.ToInt32(Kal_En_bin.ToString(),2);
        // PASS BY DECIMAL:
            uint Kal_En_dec=0;
            Kal_En_dec = pow(2,MUX);
            Kal_En_hex = Convert.ToInt32(Kal_En_dec.ToString(),10);

    // TEST
    System.Console.WriteLine(channel.ToString());
    System.Console.WriteLine(Kal_En_bin.ToString());
    System.Console.WriteLine(Kal_En_dec.ToString());
    System.Console.WriteLine(Kal_En_hex.ToString());

    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL_MUX", Kal_MUX_output);
    BoardLib.SetVariable("GPIO.GPIO-MISC.KAL_EN", Kal_EN_hex);
    SendGPIO();
}


void SetUpPulseGenerator(double amplitude = 0.18){
    System.IO.Ports.SerialPort l_serialPort = new System.IO.Ports.SerialPort();
    // port to open: check the name of the USB port in the device manager
    l_serialPort.PortName = "COM17";
 
    // Set default pulse parameters (pulse shape, amplitude unit, rising and falling edges, delay, low level, period)
    l_serialPort.Open();
    l_serialPort.WriteLine("WAVE PULSE");
            Sync.Sleep(50);
    l_serialPort.WriteLine("AMPUNIT Vpp");
            Sync.Sleep(50);
    l_serialPort.WriteLine("PULSRISE 10E-9"); // 10 ns rising edge
            Sync.Sleep(50);
    l_serialPort.WriteLine("PULSFALL 99E-9"); // 99 ns falling edge
            Sync.Sleep(50);
    l_serialPort.WriteLine("PULSDLY 0");
            Sync.Sleep(50);
    l_serialPort.WriteLine("LOLVL 0");
            Sync.Sleep(50);
    l_serialPort.WriteLine("HILVL " + amplitude.ToString()); // Conversion factors with filter: p0 = 0.455881; p1 = 0.284171;
            Sync.Sleep(50);                                  // 180 mV corresponds to ~52 mV after the filter:
                                                             // HG = 12 -> around 2000 ADC
                                                             // LG = 56 -> around 1000 ADC
    l_serialPort.WriteLine("PER 10");
            Sync.Sleep(50);
    
}


void ActivateAllCh(int LG_gain,int HG_gain){
    for (int i_ch = 0; i_ch < 256; i_ch++){
        asic=i_ch/32;
        local_ch=i_ch%32; 
    
        // En32Trigger
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].GlobalControl.En32Trigger", true);
        // EnOR32
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].GlobalControl.EnOR32", true);
        // EnNOR32
        BoardLib.SetVariable("Asics[" + asic.ToString() +
                                "].GlobalControl.EnNOR32", true);
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
    for(int i=14;i<16;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
}

void SelectFEBdevices(){
    // Speak with FEB
    BoardLib.SetBoardId(3);
    for(int i=0;i<14;i++){
        BoardLib.ActivateConfigDevice((byte)i,true);
    }
    for(int i=14;i<16;i++){
        BoardLib.ActivateConfigDevice((byte)i,false);
    }
}

void SendGPIO(){
    SelectGPIOdevices();
    BoardLib.BoardConfigure();
    Sync.Sleep(50);
}

void SendFEB(){
    SelectFEBdevices();
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