void ScriptMain(){
    System.Console.WriteLine("1");
    BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.TrigODLoopbackEn",true);
    System.Console.WriteLine("2");
    BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");

}