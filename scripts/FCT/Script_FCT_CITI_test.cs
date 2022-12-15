void ScriptMain(){
    // 1. CITIROC power test: enable one citiroc at a time and check the current on the 12V-FEB
    // Enable PowerPulsing
    for(int i=0;i<8;i++){
        BoardLib.SetVariable("ASICS.ASIC0.PowerModes.DiscriDisPP",false);
        BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+i.ToString()+".AllStagesPowerOn",false);
    }
    // Check the current that each single CITI draws:
    double current12V=0;
    for(int i=0;i<8;i++){
        for(int j=0;j<8;j++){
            if(i==j){
                BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+j.ToString()+".AllStagesPowerOn",true);
            }else{
                BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+j.ToString()+".AllStagesPowerOn",false);
            }
        }
        BoardLib.DeviceConfigure(13);
        Sync.Sleep(100);
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
        current12V = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-12V-Current") );
        System.Console.WriteLine(current12V.ToString());
    }
    // Check current for 0,1,2,...8 CITIs
    for(int i=0;i<9;i++){
        for(int j=0;j<8;j++){
            if(j<i){
                BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+j.ToString()+".AllStagesPowerOn",true);
            }else{
                BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.AsicsPowerSavingDisable.Asics"+j.ToString()+".AllStagesPowerOn",false);
            }
        }
        BoardLib.DeviceConfigure(13);
        Sync.Sleep(100);
        BoardLib.UpdateUserParameters("FPGA-HV-HK.Housekeeping-DPRAM-V1");
        current12V = Convert.ToDouble( BoardLib.GetFormulaVariable("FPGA-HV-HK.Housekeeping-DPRAM-V1.FEB-12V-Current") );
        System.Console.WriteLine(current12V.ToString());
    }

}