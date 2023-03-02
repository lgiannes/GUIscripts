void ScriptMain(){
    int SN=256;// to be passed as argument

    TurnOnFEB();
    string pathToCsvFiles = "/home/neutrino/FCT/FCT_database/FEBs/SN_"+SN.ToString()+"/"; 
//    string pathToCsvFiles = Environment.GetEnvironmentVariable("GENERALDATADIR")+"/FEBs/SN_"+SN.ToString()+"/"; 
    System.IO.Directory.CreateDirectory(pathToCsvFiles);
    pathToCsvFiles = pathToCsvFiles + "Calibration/";
    System.IO.Directory.CreateDirectory(pathToCsvFiles);

    System.Console.WriteLine(pathToCsvFiles);

    // Step 1: set minimum HV and T
    SetMinMax(false);
    
    // Step 2: compute GM and raw values (MIN) 
    HV_T_8 GM_min = Compute_GM(false);
    // HV_T_8 RawValues_min = Compute_RawValues();
    HV_T_8 RawValues_min = Compute_RawValues(pathToCsvFiles+"RawValues_min.csv");

    // Display results for checking 
    for(int i=0;i<8;i++){
        //System.Console.WriteLine("HV: "+GM_min.HV_8[i].ToString()+"  "+RawValues_min.HV_8[i].ToString());
        //System.Console.WriteLine("T:  "+GM_min.T_8[i].ToString("#.#####")+"  "+RawValues_min.T_8[i].ToString("#.#####"));
    }
    
    // Step 3: set maximum HV and T
    SetMinMax(true);
    
    // Step 4: compute GM and raw values (MAX) 
    HV_T_8 GM_max = Compute_GM(true);
    // HV_T_8 RawValues_max = Compute_RawValues();
    HV_T_8 RawValues_max = Compute_RawValues(pathToCsvFiles+"RawValues_max.csv");
    
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
    
    double a_HV = 1.60284e-3;//1.60284e-3
    double a_T = 4.5777e-5;//4.5777e-5

    for(int i=0;i<8;i++){
        G_f_HV[i] = (GM_max.HV_8[i]-GM_min.HV_8[i])/((RawValues_max.HV_8[i]-RawValues_min.HV_8[i])*a_HV);
        O_f_HV[i] = GM_min.HV_8[i]/(G_f_HV[i]*a_HV)-RawValues_min.HV_8[i];
        //System.Console.WriteLine("HV: "+G_f_HV[i].ToString()+" \t"+O_f_HV[i].ToString());
        G_f_T[i] = (GM_max.T_8[i]-GM_min.T_8[i])/((RawValues_max.T_8[i]-RawValues_min.T_8[i])*a_T);
        O_f_T[i] = GM_min.T_8[i]/(G_f_T[i]*a_T)-RawValues_min.T_8[i];
        //System.Console.WriteLine("T: "+G_f_T[i].ToString()+" \t"+O_f_T[i].ToString());
    }

    // Compute fixed point values for G and O
    UInt16[] G_U_HV={0,0,0,0,0,0,0,0};
    Int16[] O_U_HV ={0,0,0,0,0,0,0,0};
    UInt16[] G_U_T={0,0,0,0,0,0,0,0};
    Int16[] O_U_T ={0,0,0,0,0,0,0,0};
    
    for(int i=0;i<8;i++){
        G_U_HV[i] = (UInt16)Math.Round(G_f_HV[i]*32768);
        O_U_HV[i] = (Int16)Math.Round(O_f_HV[i]);
        System.Console.WriteLine("HV: "+G_U_HV[i].ToString()+" \t"+O_U_HV[i].ToString());

        G_U_T[i] = (UInt16)Math.Round(G_f_T[i]*32768);
        O_U_T[i] = (Int16)Math.Round(O_f_T[i]);
        System.Console.WriteLine("T: "+G_U_T[i].ToString()+" \t"+O_U_T[i].ToString());

    }

    // Verify calibration
    SetMinMax(false);
    HV_T_8 GM_min_verify = Compute_GM(false);
    HV_T_8 RawValues_min_verify = Compute_RawValues(pathToCsvFiles+"RawValues_min_verify.csv");
    SetMinMax(true);
    HV_T_8 GM_max_verify = Compute_GM(true);
    HV_T_8 RawValues_max_verify = Compute_RawValues(pathToCsvFiles+"RawValues_max_verify.csv");
    
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
    File.AppendAllText(@csvResiduals_min,"#ch; HV_raw; HV_cal_LSB; HV_cal; HV_GM; T_raw; T_cal; T_GM"+Environment.NewLine);
    File.AppendAllText(@csvResiduals_max,"#ch; HV_raw; HV_cal_LSB; HV_cal; HV_GM; T_raw; T_cal; T_GM"+Environment.NewLine);

    for(int i=0;i<8;i++){
        converted_HV_min[i] = a_HV*(RawValues_min_verify.HV_8[i]+O_f_HV[i])*G_f_HV[i];
        System.Console.WriteLine("HV min: "+(converted_HV_min[i]-GM_min_verify.HV_8[i]).ToString("0.#####"));
        converted_HV_max[i] = a_HV*(RawValues_max_verify.HV_8[i]+O_f_HV[i])*G_f_HV[i];
        System.Console.WriteLine("HV max: "+(converted_HV_max[i]-GM_max_verify.HV_8[i]).ToString("0.#####"));
        converted_T_min[i] = a_T*(RawValues_min_verify.T_8[i]+O_f_T[i])*G_f_T[i];
        System.Console.WriteLine("T min: "+(converted_T_min[i]-GM_min_verify.T_8[i]).ToString("0.#####"));
        converted_T_max[i] = a_T*(RawValues_max_verify.T_8[i]+O_f_T[i])*G_f_T[i];
        System.Console.WriteLine("T max: "+(converted_T_max[i]-GM_max_verify.T_8[i]).ToString("0.#####"));
        System.Console.WriteLine("");

        File.AppendAllText(@csvResiduals_min,i.ToString()+"; "+
            RawValues_min_verify.HV_8[i] +"; "+converted_HV_min[i]/a_HV+"; "+converted_HV_min[i]+"; "+GM_min_verify.HV_8[i]+"; "+
            RawValues_min_verify.T_8[i]  +"; "+converted_T_min[i] +"; "+GM_min_verify.T_8[i] +Environment.NewLine);
        File.AppendAllText(@csvResiduals_max,i.ToString()+"; "+
            RawValues_max_verify.HV_8[i] +"; "+converted_HV_max[i]/a_HV+"; "+converted_HV_max[i]+"; "+GM_max_verify.HV_8[i]+"; "+
            RawValues_max_verify.T_8[i]  +"; "+converted_T_max[i] +"; "+GM_max_verify.T_8[i] +Environment.NewLine);

    }


    System.Console.Write("Saving calibration values in EEPROM registers...");

    // Save calibration values in the EEPROM registers
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Write",true);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Page",0);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",0);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",SN&0xFF);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",1);
    BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",(SN>>8)&0xFF);
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
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",O_U_HV[i]&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",(O_U_HV[i]>>8)&0xFF);
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
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",O_U_T[i]&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Address",address);
        BoardLib.SetVariable("FPGA-MISC.NIOS.WRITE.Value",(O_U_T[i]>>8)&0xFF);
        BoardLib.UpdateUserParameters("FPGA-MISC.NIOS.WRITE");
        address+=1;
    }
    System.Console.Write("  Done!\n");



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
        HV_set=(int) HV_factor*35;
    }else{
        TSW_set=0;
        HV_set=(int) HV_factor*5;
    }
    for(int i = 0;i<8;i++){
        BoardLib.SetVariable("FPGA-HV-HK.FPGA-HV.HV-CH"+i.ToString()+".DAC",HV_set);
    }
    BoardLib.SetBoardId(0); Sync.Sleep(1);
    System.Console.WriteLine("sending configuration with HV");
    BoardLib.DeviceConfigure(11);
    Sync.Sleep(100);
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", true);
    System.Console.WriteLine("setting direct params with HV");
    BoardLib.SetDirectParameters();
    Sync.Sleep(1000);
    // Set T
    BoardLib.SetBoardId(126);
    BoardLib.SetVariable("GPIO.GPIO-MISC.TSEN-SW",TSW_set);
    System.Console.WriteLine("updating parameters with TSEN-SW");
    BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    BoardLib.SetBoardId(0);
}

HV_T_8 Compute_RawValues(string csvFile="none"){
    //if the string for csvFile is not "none, save measurements to a csv file
    if(csvFile!="none"){
        var fs = new FileStream(csvFile, FileMode.Create);
        fs.Dispose();        
        File.AppendAllText(@csvFile,"# Raw Values MPPC temperature and HV"+Environment.NewLine);
        File.AppendAllText(@csvFile,"# ch; HV; T"+Environment.NewLine);
    }
    
    // Measure raw values (y axis)
    int samples = 100;
    UInt16 hv=0, t=0;
    double[] Raw_HV={0,0,0,0,0,0,0,0};
    double[] Raw_T={0,0,0,0,0,0,0,0};
    UInt32[] Raw_HV_b={0,0,0,0,0,0,0,0};
    UInt32[] Raw_T_b={0,0,0,0,0,0,0,0};
    //Measure HV (from FEB) and T
    BoardLib.SetBoardId(0);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12);
    for(int i = 0;i<8;i++){
        for(int j=0;j<samples;j++){
            BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
            hv = BoardLib.GetUInt16Variable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+i.ToString()+".MPPC-HV");
            t = ( BoardLib.GetUInt16Variable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+i.ToString()+".MPPC-Temp") );
            Raw_HV_b[i] = Raw_HV_b[i] + hv;
            Raw_T_b[i] = Raw_T_b[i] + t;
            if(csvFile!="none"){
                File.AppendAllText(@csvFile,i.ToString()+"; "+hv.ToString()+"; "+t.ToString()+Environment.NewLine);
            }
            //Raw_HV_b[i] = ( BoardLib.GetUInt32Variable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+i.ToString()+".MPPC-HV") );
            //Raw_T_b[i] = ( BoardLib.GetUInt32Variable("FPGA-HV-HK.Housekeeping-DPRAM-V2.Group.Group"+i.ToString()+".MPPC-Temp") );
            Sync.Sleep(1);
        }
        Raw_HV[i] = (double)Raw_HV_b[i]/samples;
        Raw_T[i] = (double)Raw_T_b[i]/samples;

        // /System.Console.WriteLine("Raw_HV[i]= "+Raw_HV[i].ToString());
        //System.Console.WriteLine("Raw_T[i]= "+Raw_T[i].ToString());
    }

    HV_T_8 result;
    result.HV_8=Raw_HV;
    result.T_8=Raw_T;
    HV_T_8_int result_int;
    result_int.HV_8=Raw_HV_b;
    result_int.T_8=Raw_T_b;

    return result;

}

HV_T_8 Compute_GM(bool MAX, string csvFile="none"){
       //Compute/measure GM_min for HV and T (x axis)
    double f1 = 3000;
    double f2 = 3.3; 
    double R = 0;
    double T_sense, T_ADC;

    double[] GM_HV={0,0,0,0,0,0,0,0};
    double GM_T;
    double[] GM_T_vec={0,0,0,0,0,0,0,0};

    if(MAX){
        R=2210;
    }else{
        R=24300;
    }

    BoardLib.SetBoardId(126); Sync.Sleep(1);
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",true);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.SetVariable("GPIO.GPIO-ADC.InitOrStart",false);
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC");
    BoardLib.UpdateUserParameters("GPIO.GPIO-ADC-DPRAM");
    for(int i=0;i<8;i++){
        GM_HV[i] = Convert.ToDouble( BoardLib.GetFormulaVariable("GPIO.GPIO-ADC-DPRAM.HV-Channels.CH"+i.ToString()+".HV") );
        //GM_HV[i] =( BoardLib.GetUInt32Variable("GPIO.GPIO-ADC-DPRAM.HV-Channels.CH"+i.ToString()+".HV") );
        //System.Console.WriteLine("GM_min_HV["+i.ToString()+"]= "+GM_min_HV[i].ToString());
    }   

    T_sense = f1*f2/(f1+R);
    T_ADC = T_sense*65535/3;
    GM_T = 1/(Math.Log(0.3*(1.1*65535/T_ADC-1))/3435+1.0/298.15) - 273.15;
    //System.Console.WriteLine("GM_min_T[i]= "+GM_min_T.ToString());

    HV_T_8 result;
    result.HV_8=GM_HV;
    for(int i=0;i<8;i++){
        GM_T_vec[i]=GM_T;
    }
    result.T_8 = GM_T_vec;

    return result;

}

struct HV_T_8{
    public double[] HV_8;
    public double[] T_8;
};

struct HV_T_8_int{
    public UInt32[] HV_8;
    public UInt32[] T_8;
};

