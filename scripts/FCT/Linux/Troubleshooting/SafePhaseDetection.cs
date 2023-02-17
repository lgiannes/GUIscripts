void ScriptMain()
{	
	const byte NB_STEPS = 24;
	const byte NB_MEAS = 5;
	bool[][] l_result = new bool[NB_STEPS][];
	for (int l_i = 0; l_i<NB_STEPS; l_i++)
	{
		l_result[l_i] = new bool[NB_MEAS];
	}

	Console.WriteLine("Script begins...");
	Console.WriteLine("Nb of phase steps = " + NB_STEPS.ToString() + "\n");
	Console.WriteLine("Nb of meas. / step = " + NB_MEAS.ToString() + "\n");
	
	BoardLib.NewConfig();

	// Turn On FEB
	BoardLib.SetBoardId(126);
	BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-EN", true);
	BoardLib.SetVariable("GPIO.GPIO-MISC.PLL-PHASE", 0);
	BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
   Sync.Sleep(1500);
	
	// select FEB ext clock
	BoardLib.SetBoardId(0);		
	BoardLib.SetVariable("Board.DirectParam.ExtClkEn", true);
	BoardLib.SetDirectParameters();
	Sync.Sleep(10);
	
	// Enable FEB Oscillator
	BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.GlobalEnable",true);
	BoardLib.SetVariable("FPGA-MISC.FPGA-Misc-Config.FunctionalTesting.DAQSerOscModeEn",true);
	BoardLib.UpdateUserParameters("FPGA-MISC.FPGA-Misc-Config");
	Sync.Sleep(1);
	
	for (int l_step = 0; l_step<NB_STEPS; l_step++)
	{	
		// Set Phase
		BoardLib.SetBoardId(126);			
		BoardLib.SetVariable("GPIO.GPIO-MISC.PLL-PHASE", l_step);
		BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
		Sync.Sleep(1);		
		
		for (int l_i = 0; l_i<NB_MEAS; l_i++)
		{
			// start PLL Phase change
			BoardLib.SetBoardId(126);
			BoardLib.UpdateUserParameters("GPIO.GPIO-PHASE-TUNE");
			Sync.Sleep(10);
			
			// read phase
			BoardLib.UpdateUserParameters("GPIO.GPIO-STATUS");	
			l_result[l_step][l_i] = BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.PLL-PHASE-DETECT");
			//Console.WriteLine("PLL_PHASE_DETECT = " + BoardLib.GetBoolVariable("GPIO.GPIO-STATUS.PLL-PHASE-DETECT"));
			Sync.Sleep(1);	
		}
		
		string l_str = "Step = " + l_step.ToString() + ": ";
		int l_sum=0;
		for (int l_i = 0; l_i<NB_MEAS; l_i++)
		{
			l_str += l_result[l_step][l_i]?"1":"0";
			if(l_result[l_step][l_i])
				l_sum++;			
		}
		if(NB_MEAS>1) 
			l_str += "  Sum= " + l_sum.ToString();
		
		Console.WriteLine(l_str);
		//Dialog.ShowDialog("Continue");
	}
	
	// Turn Off FEB
	BoardLib.SetBoardId(126);
	BoardLib.SetVariable("GPIO.GPIO-MISC.FEB-EN", false);
	BoardLib.SetVariable("GPIO.GPIO-MISC.PLL-PHASE", 0);
	BoardLib.UpdateUserParameters("GPIO.GPIO-MISC");
   Sync.Sleep(3000);

	
	Console.WriteLine("\n1- Find the transitions to '1', take the middle step");
	Console.WriteLine("2- Take the middle step 'ST'");
	Console.WriteLine("3- Compute ideal PHASE = (ST+12) modulo 24, must be within [0-23]");
	Console.WriteLine("4- Set this PHASE number to GPIO.GPIO-MISC.PLL-PHASE");
	Console.WriteLine("      BoardLib.SetVariable(\"GPIO.GPIO-MISC.PLL-PHASE\", PHASE);");
	Console.WriteLine("5- Update GPIO.GPIO-PHASE-TUNE");
	Console.WriteLine("      BoardLib.UpdateUserParameters(\"GPIO.GPIO-PHASE-TUNE\");");
	
	// We're done!
	Console.WriteLine("\nScript ended");
}
