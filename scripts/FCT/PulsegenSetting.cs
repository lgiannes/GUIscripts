void ScriptMain(){
    System.IO.Ports.SerialPort l_serialPort = SetUpPulseGenerator();

}

System.IO.Ports.SerialPort SetUpPulseGenerator(double amplitude = 0.18){
                                                            // Conversion factors with filter: p0 = 0.455881; p1 = 0.284171;
                                                            // 180 mV corresponds to ~52 mV after the filter:
                                                            // HG = 12 -> around 2000 ADC
                                                            // LG = 56 -> around 1000 ADC

    System.IO.Ports.SerialPort l_serialPort = new System.IO.Ports.SerialPort();
    // port to open: check the name of the USB port in the device manager
    l_serialPort.PortName = "COM18";
 
    // Set default pulse parameters (pulse shape, amplitude unit, rising and falling edges, delay, low level, period)
    l_serialPort.Open();
    l_serialPort.WriteLine("CHN 1");
            Sync.Sleep(500);
    l_serialPort.WriteLine("WAVE PULSE");
            Sync.Sleep(500);
    l_serialPort.WriteLine("AMPUNIT Vpp");
            Sync.Sleep(500);
    l_serialPort.WriteLine("PULSRISE 10E-9"); // 10 ns rising edge
            Sync.Sleep(500);
    l_serialPort.WriteLine("PULSFALL 99E-9"); // 99 ns falling edge
            Sync.Sleep(500);
    l_serialPort.WriteLine("PULSDLY 0");
            Sync.Sleep(500);
    l_serialPort.WriteLine("PULSPER 10");
            Sync.Sleep(500);
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