void ScriptMain(){
    int SN=256;// to be passed as argument
    System.Console.WriteLine("Starting HV and Temperature calibration procedure ...");
    int GPIO=47;// SN of the GPIO used, to be passed as argument
    string GPIO_calib_folder = "/DATA/neutrino/FCT/GPIO_cal/GPIO_SN"+GPIO.ToString()+"/";
    string GPIO_calib_file = GPIO_calib_folder+"cal_GPIO_SN"+GPIO.ToString()+".csv";
    string VerifyGPIO_file = GPIO_calib_folder+"verify.csv";
    TurnOnFEB();
    string pathToCsvFiles = "/home/neutrino/FCT/FCT_database/FEBs/SN_"+SN.ToString()+"/"; 
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
    // HV_T_8 RawValues_min = Compute_RawValues();
    HV_T_8 RawValues_min = Compute_RawValues(samples,pathToCsvFiles+"RawValues_min.csv");

    // Display results for checking 
    for(int i=0;i<8;i++){
        //System.Console.WriteLine("HV: "+GM_min.HV_8[i].ToString()+"  "+RawValues_min.HV_8[i].ToString());
        //System.Console.WriteLine("T:  "+GM_min.T_8[i].ToString("#.#####")+"  "+RawValues_min.T_8[i].ToString("#.#####"));
    }
    
    // Step 3: set maximum HV and T
    SetMinMax(true);
    
    // Step 4: compute GM and raw values (MAX) 
    HV_T_8 GM_max = Compute_GM(true,GPIO_calib_file,samples,pathToCsvFiles+"RefValues_max.csv");
    // HV_T_8 RawValues_max = Compute_RawValues();
    HV_T_8 RawValues_max = Compute_RawValues(samples,pathToCsvFiles+"RawValues_max.csv");
    
    // Display results for checking 
    for(int i=0;i<8;i++){
        //System.Console.WriteLine("HV: "+GM_max.HV_8[i].ToString()+"  "+RawValues_max.HV_8[i].ToString());
        //System.Console.WriteLine("T:  "+GM_max.T_8[i].ToString("#.#####")+"  "+RawValues_max.T_8[i].ToString("#.#####"));
    }
    
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
            System.Console.WriteLine(G_f_HV[i]);
            System.Console.WriteLine("");
        }
        if(O_f_HV[i]<-80 || O_f_HV[i]>80){
            System.Console.WriteLine("");
            System.Console.WriteLine("WARNING: Ridiculous offset  on HV channel "+i.ToString());
            System.Console.WriteLine(O_f_HV[i]);
            System.Console.WriteLine("");
        }
        if(G_f_T[i]>1.1 || G_f_T[i]<0.9){
            System.Console.WriteLine("");
            System.Console.WriteLine("WARNING: Ridiculous gain  on T channel "+i.ToString());
            System.Console.WriteLine(G_f_T[i]);
            System.Console.WriteLine("");
        }
        if(O_f_T[i]<-80 || O_f_T[i]>80){
            System.Console.WriteLine("");
            System.Console.WriteLine("WARNING: Ridiculous offset  on T channel "+i.ToString());
            System.Console.WriteLine(O_f_T[i]);
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
    System.Console.WriteLine("Compute residual differences:");
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

        hv1 = GM_min_verify.HV_8[i]/4; // HV ON THE GPIO IN ADC DIVIDED BY 4
        hv2 = Convert_HV_GPIO( GM_min_verify.HV_8[i] ); // HV ON THE GPIO CONVERTED IN VOLTS
        hv3 = Math.Round( ( (RawValues_min_verify.HV_8[i]) + O_I_HV[i] )*(double)G_U_HV[i]/f_to_ui ); // CALIBRATED HV ON THE FEB, IN ADC
        hv4 = Convert_HV_FEB(Math.Round( ( (RawValues_min_verify.HV_8[i]) + O_I_HV[i] )*(double)G_U_HV[i]/f_to_ui ) ); // CALIBRATED HV ON THE FEB CONVERTED IN VOLTS
        hv5 = RawValues_min_verify.HV_8[i]; //  UNCALIBRATED HV ON THE FEB, IN ADC
        hv6 = Convert_HV_FEB( ( (RawValues_min_verify.HV_8[i]) ) ); // UNCALIBRATED HV ON THE FEB, CONVERTEED IN V
        // repeat for T, all the values that you obtain are in Volts, not degrees
        t1 = GM_min_verify.T_8[i]; // no need to divide by 4 here, the ADC for temperature sensing in GPIO and FEB have the same range
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
        hv1 = GM_max_verify.HV_8[i]/4; // HV ON THE GPIO IN ADC DIVIDED BY 4
        hv2 = Convert_HV_GPIO( GM_max_verify.HV_8[i] ); // HV ON THE GPIO CONVERTED IN VOLTS
        hv3 = Math.Round( ( (RawValues_max_verify.HV_8[i]) + O_I_HV[i] )*(double)G_U_HV[i]/f_to_ui ) ; // CALIBRATED HV ON THE FEB, IN ADC
        hv4 = Convert_HV_FEB( Math.Round( ( (RawValues_max_verify.HV_8[i]) + O_I_HV[i] )*(double)G_U_HV[i]/f_to_ui ) ); // CALIBRATED HV ON THE FEB CONVERTED IN VOLTS
        hv5 = RawValues_max_verify.HV_8[i]; //  UNCALIBRATED HV ON THE FEB, IN ADC
        hv6 = Convert_HV_FEB( ( (RawValues_max_verify.HV_8[i]) ) ); // UNCALIBRATED HV ON THE FEB, CONVERTEED IN V
        // repeat for T, all the values that you obtain are in Volts, not degrees
        t1 = GM_max_verify.T_8[i]; // no need to divide by 4 here, the ADC for temperature sensing in GPIO and FEB have the same range
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

    //Verify that the values written in teh EEPROM are correct (after a power cycle 
    //TurnOffFEB();
    //TurnOnFEB();
    BoardLib.SetBoardId(0);
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

    TurnOffFEB();
    return;
}

void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
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
void SetMinMax(bool MAX){
    int HV_set;
    byte TSW_set;
    double HV_factor = 65535/102.46;
    if(MAX){
        TSW_set=255;//FF
        HV_set=(int) HV_factor*39;
    }else{
        TSW_set=0;
        HV_set=(int) HV_factor*5;
    }
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set);
    }
    BoardLib.SetBoardId(0); Sync.Sleep(1);
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
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.SetBoardId(0);
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
    BoardLib.SetBoardId(0);
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
        T_sense = f1*f2/(f1+R[i]);// V 
        T_ADC = T_sense*65535/3;// ADC
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
            BoardLib.SetBoardId(0); Sync.Sleep(1);
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
        BoardLib.SetBoardId(0); Sync.Sleep(1);
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


    