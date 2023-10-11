
bool VERBOSE =true;

void ScriptMain(){
    
    bool loop = true;
    int countALL=0;
    double read;
    int count =0 ;

    while(countALL<20){

    //System.Console.WriteLine("Enabling FEB...");
    TurnOnFEB();

    //System.Console.WriteLine("Enabling HK");
    BoardLib.SetBoardId(0);
    BoardLib.SetVariable("FPGA-HV-HK.FPGA-HouseKeeping.HKEn",true);
    BoardLib.DeviceConfigure(12, x_verbose:false);
    countALL++;


        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V2");
        read = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V2.FEB-HK.FEB-BKP-HV") );
        System.Console.WriteLine(countALL.ToString()+": HV= "+read.ToString()+" V");

        if(read<5 || read>15){
            count++;
        }
        Sync.Sleep(100);

    //System.Console.WriteLine("Disabling FEB...");
    TurnOffFEB();
    Sync.Sleep(500);
    

    }
    System.Console.WriteLine("Wrong readings: "+count.ToString());
    System.Console.WriteLine("Total readings: "+countALL.ToString());


}


void TurnOnFEB(){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", true);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
    Sync.Sleep(1500);
}

void TurnOffFEB(int BID=0){    
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-En", false);
    BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-ADDR", BID);
    BoardLib.SetBoardId(126); Sync.Sleep(1); BoardLib.UpdateUserParameters("GPIO.GPIO-MISC"); BoardLib.GetFirmwareVersion();
    Sync.Sleep(3000);
}