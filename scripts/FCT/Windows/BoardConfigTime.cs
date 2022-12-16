void ScriptMain(){
    BoardLib.SelectUsbDevice(1);     // BE CAREFUL: the 'usbdevice' number may change!
    BoardLib.SetBoardId(3);
    for(int i=0;i<16;i++){
        if(i==11){
            BoardLib.ActivateConfigDevice((byte)i,true);
        }else{
            BoardLib.ActivateConfigDevice((byte)i,false);
        }
    }
    System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
    System.Diagnostics.Stopwatch SUBstopWatch = new System.Diagnostics.Stopwatch();
    
    stopWatch.Start();
    BoardLib.BoardConfigure();
    SUBstopWatch.Start();
    BoardLib.SetVariable("Board.DirectParam.BaselineDACApply", true);
    BoardLib.SetVariable("Board.DirectParam.HvDACApply", false);
    BoardLib.SetDirectParameters();
    stopWatch.Stop();
    SUBstopWatch.Stop();
    TimeSpan ts = stopWatch.Elapsed;
    TimeSpan ts1 = SUBstopWatch.Elapsed;

    Dialog.ShowDialog(ts.ToString() + " - " + ts1.ToString());

}
