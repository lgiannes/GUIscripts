
bool VERBOSE =true;

void ScriptMain(){
    System.Console.WriteLine("Enabling FEB...");
    TurnOnFEB();
    System.Console.WriteLine("Enabling HK");
    BoardLib.SetBoardId(0);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12, x_verbose:false);
    System.Console.WriteLine("Starting loop: update and read FPGA temperature. \nIf the value is EXACTLY the same for 10 iterations in a row (10 'strikes'), exit the loop.");

    bool loop = true;
    double read=0;
    double pre_read=0;
    int count = 0;

    System.Console.Write("prev");
    System.Console.Write("\t");
    System.Console.Write("this");
    System.Console.Write("\t");
    System.Console.Write("strike");
    System.Console.WriteLine("");

    while(loop){
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
        read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FPGA-Temp") );
        if(read==pre_read){
            count++;
        }else{
            count=0;
        }
        pre_read=read;

        if(count==10){
            loop=false;
        }
        if(VERBOSE){
            System.Console.Write(pre_read);
            System.Console.Write("\t");
            System.Console.Write(read);
            System.Console.Write("\t");
            System.Console.Write(count);
            System.Console.WriteLine("");
        }
        Sync.Sleep(100);


    }
    System.Console.WriteLine("Housekeeping stuck!");
}


void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}